FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Restore first so the layer caches until a project file actually changes.
COPY Kairos.sln global.json ./
COPY src/Kairos.Api/Kairos.Api.csproj src/Kairos.Api/
COPY tests/Kairos.Api.Tests/Kairos.Api.Tests.csproj tests/Kairos.Api.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Kairos.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# The base image ships a non-root user; run as it.
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Kairos.Api.dll"]
