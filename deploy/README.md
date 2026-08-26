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

5. Guarda el client id y el secreto como los secrets `GOOGLE_CLIENT_ID` y
   `GOOGLE_CLIENT_SECRET` del repositorio: el despliegue los escribe en el `.env`
   del servidor. Las mismas credenciales sirven para entrar al album y para
   Google Photos, porque son un unico cliente OAuth.

El `redirect_uri` no se configura en ningun sitio: la API lo deriva de la
peticion y del `X-Forwarded-Proto` que manda el proxy, asi que basta con que el
autorizado en Google coincida con el dominio publico.

El unico permiso que pide la aplicacion es
`photospicker.mediaitems.readonly`: leer lo que tu elijas en el selector, nada mas.

## 2. Preparar el servidor (una sola vez)

El despliegue lo hace GitHub Actions por SSH a traves de Cloudflare Access. En el
servidor solo hay que dejar el terreno listo:

```bash
sudo mkdir -p /opt/albumviajes
sudo chown "$USER" /opt/albumviajes

# El usuario del despliegue tiene que poder hablar con Docker sin sudo.
sudo usermod -aG docker "$USER"
```

El flujo copia `docker-compose.yml`, escribe el `.env` con los secrets del
repositorio y siembra `config.js` a partir de `config.example.js` la primera vez.
Ese `config.js` es del servidor: los despliegues siguientes no lo tocan, asi que
el centro y el zoom del mapa se editan alli y se conservan.

La API aplica sus migraciones al arrancar; no hay paso manual de base de datos.

### Secrets del repositorio

| Secret | Para que |
| --- | --- |
| `SSH_HOST`, `SSH_USER`, `SSH_KEY`, `SSH_KNOWN_HOSTS` | acceso al servidor |
| `CF_ACCESS_CLIENT_ID`, `CF_ACCESS_CLIENT_SECRET` | token de servicio de Cloudflare Access |
| `DEPLOY_PATH` | carpeta del despliegue, p. ej. `/opt/albumviajes` |
| `PUBLIC_URL` | dominio publico: CORS y la comprobacion final |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | base de datos |
| `OWNER_EMAIL` | unico correo que puede editar |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | entrada con Google y Google Photos |
| `WIKIMEDIA_USER_AGENT`, `RADIO_USER_AGENT` | Wikipedia y Radio Browser exigen identificarse |

`GITHUB_TOKEN` lo da la propia Action y el propietario sale de
`github.repository_owner`: no hacen falta como secrets.

## 3. Proxy con TLS

El compose de produccion publica el sitio solo en `127.0.0.1:8081`. Delante va
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

Cada push a `main` dispara `.github/workflows/deploy.yml`: compila y prueba los
dos proyectos, publica las imagenes en GHCR etiquetadas con el commit, las trae
al servidor y comprueba que el sitio responde. Tambien se puede lanzar a mano
desde la pestana Actions.

Si hiciera falta hacerlo a mano en el servidor:

```bash
cd /opt/albumviajes
docker compose pull
docker compose up -d
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
gunzip -c db-FECHA.sql.gz | docker compose exec -T db \
  psql -U albumviajes albumviajes

docker run --rm -v albumviajes_photos:/data -v "$PWD":/backup \
  alpine tar xzf /backup/photos-FECHA.tar.gz -C /data
```
