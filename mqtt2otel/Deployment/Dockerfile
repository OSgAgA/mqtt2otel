FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM base AS final
WORKDIR /app

ARG USAGE_TELEMETRY_HEADER
ENV usage_telemetry_headers=${USAGE_TELEMETRY_HEADER}

COPY ./app/ .
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache tzdata

ENTRYPOINT ["dotnet", "mqtt2otel.Server.dll"]
