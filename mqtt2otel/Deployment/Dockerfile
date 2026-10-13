FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM base AS final
WORKDIR /app

COPY ./app/ .
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Add timezone data.
RUN apk add --no-cache tzdata

# Prepare usage telemetry.
ARG USAGE_TELEMETRY_HEADER
ENV usage_telemetry_headers=${USAGE_TELEMETRY_HEADER}

ENTRYPOINT ["dotnet", "mqtt2otel.Server.dll"]
