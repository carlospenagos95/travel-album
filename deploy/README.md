# Despliegue

## 1. Credenciales de Google (solo si quieres importar fotos)

El album funciona entero sin esto salvo la galeria. Para habilitarla:

1. En [Google Cloud Console](https://console.cloud.google.com/) crea un proyecto.
2. Habilita la **Photos Picker API**. Desde marzo de 2025 es la unica via
   soportada para que una aplicacion de terceros lea fotos: ya no se puede
   recorrer la biblioteca del usuario.
3. Configura la pantalla de consentimiento como **Externa**, en modo *Testing*, y
   anade tu propio correo como usuario de prueba. Para un album personal no hace
   falta pasar la verificacion de Google.
4. Crea credenciales **OAuth client ID** de tipo *Web application* con el URI de
   redireccion autorizado exacto:

   ```
   https://viajes.tu-dominio.com/api/auth/google/callback
   ```

5. Copia el client id y el secreto a `GOOGLE_CLIENT_ID` y `GOOGLE_CLIENT_SECRET`
   en el `.env` del servidor.

El unico permiso que pide la aplicacion es
`photospicker.mediaitems.readonly`: leer lo que tu elijas en el selector, nada mas.

## 2. Primer despliegue

```bash
git clone <este-repo> /opt/albumviajes && cd /opt/albumviajes

cp .env.example .env            # credenciales, dominio y correo del administrador
cp config.example.js config.js  # centro y zoom del mapa

docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

La API aplica sus migraciones al arrancar; no hay paso manual de base de datos.

## 3. Proxy con TLS

`docker-compose.prod.yml` publica el sitio solo en `127.0.0.1:8081`. Delante va
el nginx del host:

```bash
sudo cp deploy/albumviajes.nginx.conf /etc/nginx/sites-available/albumviajes
sudo ln -s /etc/nginx/sites-available/albumviajes /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d viajes.tu-dominio.com
```

Dos detalles que importan y no son evidentes:

- El bloque del stream de radio desactiva `proxy_buffering`. Un stream no termina
  nunca; con el buffer activado nginx espera a tener la respuesta completa y el
  audio no llega a sonar.
- El proxy manda `X-Forwarded-Proto`. Sin el, la API cree que la peticion llego
  por http y construye el `redirect_uri` de Google con ese esquema, que Google
  rechaza por no coincidir con el autorizado.

## 4. Actualizar

GitHub Actions publica una imagen nueva en GHCR con cada push a `main`.

```bash
cd /opt/albumviajes
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
docker image prune -f
```

## 5. Copias de seguridad

```bash
./deploy/backup.sh /var/backups/albumviajes
```

Respalda las tres cosas que hacen falta para restaurar: la base, el volumen de
fotos y las claves de Data Protection. Perder las ultimas no borra nada, pero
invalida las sesiones y obliga a volver a autorizar Google.

Para dejarlo en cron, diario a las 3:15:

```cron
15 3 * * * cd /opt/albumviajes && ./deploy/backup.sh /var/backups/albumviajes >> /var/log/albumviajes-backup.log 2>&1
```

## Restaurar

```bash
gunzip -c db-FECHA.sql.gz | docker compose -f docker-compose.prod.yml exec -T db \
  psql -U albumviajes albumviajes

docker run --rm -v albumviajes_photos:/data -v "$PWD":/backup \
  alpine tar xzf /backup/photos-FECHA.tar.gz -C /data
```
