using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ==================== Configuration ====================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ==================== Logging ====================
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ==================== JWT Authentication ====================
var jwtSecret = builder.Configuration["JWT_SECRET"] 
    ?? builder.Configuration["Jwt:Secret"]
    ?? throw new Exception("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? "goydagram",
            ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? "goydagram-users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==================== YARP Reverse Proxy ====================
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ==================== Health Checks ====================
builder.Services.AddHttpClient("HealthCheckClient", c => c.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddHealthChecks()
    .AddCheck<ServiceHealthCheck>("services");

// ==================== Swagger ====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GoydaGram API Gateway",
        Version = "v1",
        Description = "API Gateway для GoydaGram микросервисов",
        Contact = new OpenApiContact
        {
            Name = "GoydaGram Team",
            Email = "support@goydagram.com"
        }
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== Graceful Shutdown ====================
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// ==================== Middleware ====================
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GoydaGram API Gateway V1");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

// ==================== Health ====================
app.MapHealthChecks("/health");
app.MapHealthChecks("/healthz");
app.UseHttpMetrics();
app.MapMetrics();

// ==================== Graceful Shutdown ====================
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("API Gateway stopping gracefully...");
});

app.Run();

// ==================== ServiceHealthCheck ====================
public class ServiceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ServiceHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var services = new Dictionary<string, string>
        {
            ["User Service"] = _configuration["USER_SERVICE_URL"] ?? "http://user-service:8080",
            ["Content Service"] = _configuration["CONTENT_SERVICE_URL"] ?? "http://content-service:8080",
            ["Social Service"] = _configuration["SOCIAL_SERVICE_URL"] ?? "http://social-service:8080",
            ["Feed Service"] = _configuration["FEED_SERVICE_URL"] ?? "http://feed-service:8080",
            ["Search Service"] = _configuration["SEARCH_SERVICE_URL"] ?? "http://search-service:8000"
        };

        var errors = new List<string>();
        var httpClient = _httpClientFactory.CreateClient("HealthCheckClient");

        foreach (var (name, url) in services)
        {
            try
            {
                var response = await httpClient.GetAsync($"{url}/health", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    errors.Add($"{name} is unhealthy: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{name} is unreachable: {ex.Message}");
            }
        }

        return errors.Count == 0
            ? HealthCheckResult.Healthy("All services are healthy")
            : HealthCheckResult.Unhealthy(string.Join("; ", errors));
    }
}