# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Avval faqat loyiha fayllari — restore keshini saqlash uchun.
# 4-qatlam: Domain -> Application -> Infrastructure -> API.
COPY Directory.Build.props ./
COPY src/Jetar.Domain/Jetar.Domain.csproj src/Jetar.Domain/
COPY src/Jetar.Application/Jetar.Application.csproj src/Jetar.Application/
COPY src/Jetar.Infrastructure/Jetar.Infrastructure.csproj src/Jetar.Infrastructure/
COPY src/Jetar.API/Jetar.API.csproj src/Jetar.API/
RUN dotnet restore src/Jetar.API/Jetar.API.csproj

COPY . .
RUN dotnet publish src/Jetar.API/Jetar.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl — healthcheck uchun; root bo'lmagan foydalanuvchi — xavfsizlik uchun.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" --uid 5678 jetar \
    && mkdir -p /app/wwwroot/uploads \
    && chown -R jetar /app
USER jetar

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["/bin/sh", "-c", "curl -fsS http://localhost:8080/health || exit 1"]

ENTRYPOINT ["dotnet", "Jetar.API.dll"]
