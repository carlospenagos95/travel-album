#!/usr/bin/env bash
# Copia de seguridad del album: base de datos, fotos y claves de cifrado.
#
# Las tres cosas hacen falta para restaurar de verdad:
#   - la base tiene las fichas y a que archivo apunta cada foto,
#   - el volumen de fotos tiene esos archivos,
#   - las claves de Data Protection descifran el permiso guardado de Google;
#     sin ellas hay que volver a autorizar la aplicacion.
#
# Uso, desde la carpeta del proyecto en el servidor:
#   ./deploy/backup.sh /var/backups/albumviajes
#
# En cron, diario a las 3:15:
#   15 3 * * * cd /opt/albumviajes && ./deploy/backup.sh /var/backups/albumviajes >> /var/log/albumviajes-backup.log 2>&1

set -euo pipefail

DESTINO="${1:-./backups}"
COMPOSE="docker compose -f docker-compose.prod.yml"
FECHA="$(date +%Y%m%d-%H%M%S)"
# Los volumenes llevan el nombre del proyecto de compose como prefijo.
PROYECTO="$(basename "$PWD")"

# Las credenciales de la base salen del mismo .env que usa compose.
set -a
# shellcheck disable=SC1091
source .env
set +a

mkdir -p "$DESTINO"

echo "Respaldando la base de datos..."
$COMPOSE exec -T db pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" | gzip > "$DESTINO/db-$FECHA.sql.gz"

echo "Respaldando las fotos..."
docker run --rm \
  -v "${PROYECTO}_photos:/data:ro" \
  -v "$DESTINO:/backup" \
  alpine tar czf "/backup/photos-$FECHA.tar.gz" -C /data .

echo "Respaldando las claves de cifrado..."
docker run --rm \
  -v "${PROYECTO}_dataprotection:/keys:ro" \
  -v "$DESTINO:/backup" \
  alpine tar czf "/backup/dataprotection-$FECHA.tar.gz" -C /keys .

# Sin rotacion, el disco del servidor se llena en unos meses.
echo "Borrando copias de mas de 30 dias..."
find "$DESTINO" -name '*.gz' -type f -mtime +30 -delete

echo "Listo. Copias en $DESTINO"
