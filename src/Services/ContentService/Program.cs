using Serilog;
using ContentService.Application.Behaviors;
using ContentService.Application.Commands.UploadVideo;
using ContentService.Application.Interfaces;
using ContentService.Domain.Interfaces;
using ContentService.Infrastructure.Cache;
using ContentService.Infrastructure.Messaging;
using ContentService.Infrastructure.Persistence;
using ContentService.Infrastructure.Storage;
using ContentService.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;
using MediatR;
using StackExchange.Redis;
using Minio;
using Minio.DataModel.Args;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:hh:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ==================== Database ====================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());

// ==================== Redis ====================
var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? throw new Exception("Redis:ConnectionString is not configured");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ==================== MinIO ====================
builder.Services.AddMinio(client =>
{
    client.WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "minio:9000")
          .WithCredentials(
              builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
              builder.Configuration["Minio:SecretKey"] ?? "minioadmin123")
          .WithSSL(false);
});

// ==================== JWT ====================
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new Exception("Jwt:Secret is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// ==================== MediatR ====================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(UploadVideoCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

// ==================== DI ====================
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStorageService, MinIOStorageService>();
builder.Services.AddScoped<IThumbnailService, ThumbnailService>();

// RabbitMQ
builder.Services.AddSingleton<RabbitMQConnection>();
builder.Services.AddHostedService<RabbitMQConsumer>();

// ==================== Controllers + Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== Health Checks ====================
var rabbitConnectionString = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? throw new Exception("RabbitMQ:ConnectionString is not configured");

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq")
    .AddRedis(redisConnectionString, name: "redis");

// ==================== Graceful Shutdown ====================
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(20);
});

var app = builder.Build();

// ==================== Middleware ====================
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.UseHttpMetrics();
app.MapMetrics();

// ==================== Auto-migration ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ==================== Create MinIO bucket ====================
using (var scope = app.Services.CreateScope())
{
    var minio = scope.ServiceProvider.GetRequiredService<IMinioClient>();
    var bucketName = "videos";
    var beArgs = new BucketExistsArgs().WithBucket(bucketName);
    var found = await minio.BucketExistsAsync(beArgs);
    if (!found)
    {
        var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
        await minio.MakeBucketAsync(mbArgs);
        Log.Information("MinIO bucket '{BucketName}' created", bucketName);

        // Set public policy
        var policy = $@"{{
            ""Version"": ""2012-10-17"",
            ""Statement"": [
                {{
                    ""Effect"": ""Allow"",
                    ""Principal"": {{ ""AWS"": [""*""] }},
                    ""Action"": [""s3:GetObject""],
                    ""Resource"": [""arn:aws:s3:::{bucketName}/*""]
                }}
            ]
        }}";
        var psArgs = new SetPolicyArgs().WithBucket(bucketName).WithPolicy(policy);
        await minio.SetPolicyAsync(psArgs);
    }
}

// ==================== Graceful shutdown ====================
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("ContentService stopping gracefully...");
    var rabbit = app.Services.GetRequiredService<RabbitMQConnection>();
    rabbit.Dispose();
});

app.Run();