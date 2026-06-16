# Favilonia.API. Контекст сборки в docker-compose: папка ./Backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Favilonia.API/Favilonia.API.csproj
RUN dotnet publish Favilonia.API/Favilonia.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Favilonia.API.dll"]
