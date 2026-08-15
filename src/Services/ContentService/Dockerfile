FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContentService.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# Install FFmpeg for HLS transcoding + curl for healthcheck
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y ffmpeg curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ContentService.dll"]