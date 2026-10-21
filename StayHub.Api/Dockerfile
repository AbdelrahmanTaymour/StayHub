# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Force the SDK to run natively on the GitHub Actions host platform (amd64) for maximum speed
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Explicitly catch the target architecture passed by GitHub's Buildx (--platform linux/arm64)
ARG TARGETARCH
WORKDIR /src

# Copy project files first to preserve restore-layer caching
COPY ["StayHub.Api/*.csproj", "StayHub.Api/"]
COPY ["StayHub.Application/*.csproj", "StayHub.Application/"]
COPY ["StayHub.Domain/*.csproj", "StayHub.Domain/"]
COPY ["StayHub.Infrastructure/*.csproj", "StayHub.Infrastructure/"]

# Restore dependencies using native cross-compilation flags (-a $TARGETARCH)
RUN dotnet restore "StayHub.Api/StayHub.Api.csproj" -a $TARGETARCH

# Copy remaining source code
COPY . .

# Build and publish using native cross-compilation flags (-a $TARGETARCH)
WORKDIR /src/StayHub.Api
RUN --mount=type=secret,id=sixlabors_license,required=true \
    test -s /run/secrets/sixlabors_license && \
    SixLaborsLicenseKey="$(cat /run/secrets/sixlabors_license)" \
    dotnet publish "./StayHub.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -a $TARGETARCH \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StayHub.Api.dll"]