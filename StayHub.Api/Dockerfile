# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files from correct relative path under StayHub
COPY ["StayHub.Api/*.csproj", "StayHub.Api/"]
COPY ["StayHub.Application/*.csproj", "StayHub.Application/"]
COPY ["StayHub.Domain/*.csproj", "StayHub.Domain/"]
COPY ["StayHub.Infrastructure/*.csproj", "StayHub.Infrastructure/"]

# Restore dependencies
RUN dotnet restore StayHub/StayHub.Api/StayHub.Api.csproj

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR /src/StayHub/StayHub.Api
RUN --mount=type=secret,id=sixlabors_license \
    SixLaborsLicenseKey="$(cat /run/secrets/sixlabors_license)" \
    dotnet publish StayHub.Api.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StayHub.Api.dll"]