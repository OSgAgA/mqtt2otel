# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN mkdir -p /etc/otelcol && chown $APP_UID /etc/otelcol
USER $APP_UID
WORKDIR /app
EXPOSE 8080

COPY --from=otel/opentelemetry-collector-contrib:0.164.0 /otelcol-contrib /usr/local/bin/otelcol-contrib

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["mqtt2otel.Server/mqtt2otel.Server.csproj", "mqtt2otel.Server/"]
COPY ["mqtt2otel/mqtt2otel.csproj", "mqtt2otel/"]
COPY ["mqtt2otel.Shared/mqtt2otel.Shared.csproj", "mqtt2otel.Shared/"]
RUN dotnet restore "./mqtt2otel.Server/mqtt2otel.Server.csproj"
COPY . .
WORKDIR "/src/mqtt2otel.Server"
RUN dotnet build "./mqtt2otel.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./mqtt2otel.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Add timezone data.
RUN apk add --no-cache tzdata

ENTRYPOINT ["dotnet", "mqtt2otel.Server.dll"]