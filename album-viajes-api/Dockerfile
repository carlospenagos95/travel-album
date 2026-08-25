# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Se copian primero los archivos de proyecto para que la restauracion quede
# cacheada mientras no cambien las dependencias.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/AlbumViajes.Domain/*.csproj src/AlbumViajes.Domain/
COPY src/AlbumViajes.Application/*.csproj src/AlbumViajes.Application/
COPY src/AlbumViajes.Infrastructure/*.csproj src/AlbumViajes.Infrastructure/
COPY src/AlbumViajes.Api/*.csproj src/AlbumViajes.Api/
RUN dotnet restore src/AlbumViajes.Api/AlbumViajes.Api.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/AlbumViajes.Api/AlbumViajes.Api.csproj \
    -c Release \
    -o /app \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# La imagen base no trae wget ni curl, y el HEALTHCHECK de compose necesita uno.
RUN apt-get update     && apt-get install -y --no-install-recommends curl     && rm -rf /var/lib/apt/lists/*

# Las carpetas donde se montan los volumenes se crean aqui, ya con el duenio
# correcto: si las crea Docker al montar, quedan de root y el proceso sin
# privilegios no puede escribir ni las fotos ni las claves de Data Protection.
RUN mkdir -p /data/photos /home/app/.aspnet/DataProtection-Keys     && chown -R app:app /data /home/app

# Usuario sin privilegios: la imagen base ya define "app" (uid 1654).
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app .

ENTRYPOINT ["dotnet", "AlbumViajes.Api.dll"]
