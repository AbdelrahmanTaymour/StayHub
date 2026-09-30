# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["StayHub.Api/StayHub.Api.csproj", "StayHub.Api/"]

RUN --mount=type=secret,id=sixlabors_license \
    dotnet restore "StayHub.Api/StayHub.Api.csproj" \
    -p:SixLaborsLicenseKey="$(cat /run/secrets/sixlabors_license)"

COPY . .

WORKDIR "/src/StayHub.Api"

RUN --mount=type=secret,id=sixlabors_license \
    dotnet build "./StayHub.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore \
    -p:SixLaborsLicenseKey="$(cat /run/secrets/sixlabors_license)"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN --mount=type=secret,id=sixlabors_license \
    dotnet publish "./StayHub.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore \
    -p:SixLaborsLicenseKey="$(cat /run/secrets/sixlabors_license)"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StayHub.Api.dll"]