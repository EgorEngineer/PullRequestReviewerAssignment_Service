FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["PullRequest_Service.csproj", "./"]
RUN dotnet restore "PullRequest_Service.csproj"

COPY . .

RUN dotnet build "PullRequest_Service.csproj" -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish "PullRequest_Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache wget

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R appuser /app/logs

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PullRequest_Service.dll"]