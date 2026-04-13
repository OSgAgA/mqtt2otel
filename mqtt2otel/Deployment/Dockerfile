FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM base AS final
WORKDIR /app
COPY ./app/ .
ENTRYPOINT ["dotnet", "mqtt2otel.Server.dll"]
