FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM base AS final
WORKDIR /app
COPY ./app/ .
RUN apk add --no-cache tzdata
ENTRYPOINT ["dotnet", "mqtt2otel.ManifestExplorer.dll"]
