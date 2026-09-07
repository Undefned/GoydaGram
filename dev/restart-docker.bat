@echo off
echo Stopping containers...
docker compose down

echo Rebuilding containers...
docker compose build

echo Starting containers...
docker compose up -d

echo Done! Containers are running.
pause