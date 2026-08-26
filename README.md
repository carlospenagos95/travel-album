# Album de Viajes

Mapa personal de ciudades visitadas: cada ciudad es un pin con su ficha, sus
fotos y musica del lugar. El propietario administra el album; cualquiera con el
enlace puede verlo.

## Repositorios

| Carpeta            | Que es                                          |
| ------------------ | ----------------------------------------------- |
| `album-viajes-api` | Backend .NET 10, arquitectura limpia, PostgreSQL |
| `album-viajes-web` | Frontend React + TypeScript + Vite + Leaflet     |

Cada uno se publica como su propio repositorio de GitHub y su propia imagen en GHCR.

## Estado

MVP completo. Lo que hace el album:

- **Ciudades y mapa**: alta, edicion y borrado, con busqueda de lugares por
  OpenStreetMap para no teclear coordenadas a mano.
- **Datos automaticos**: cada ficha se completa con lo que publican Wikipedia y
  Wikidata (poblacion con su ano y su fuente, resumen y enlace al articulo). Se
  dispara a mano desde la ficha, no en cada guardado: las fuentes externas son
  lentas y la ciudad debe poder guardarse aunque esten caidas.
- **Fotos**: se eligen en Google Photos y se copian al servidor. Se guarda el
  archivo y no el enlace porque el que da Google caduca en una hora; con la copia
  el album sigue viendose aunque Google no responda.
- **Musica**: una emisora del directorio Radio Browser o una cancion del
  catalogo de iTunes por ciudad, que suena al abrirla. De las canciones suenan
  **30 segundos en bucle**: es lo que entrega cualquier catalogo comercial sin
  pedirle una cuenta de pago a quien mira el album. Todo pasa por un proxy del
  propio servidor: muchas emisoras siguen en http plano y el navegador las
  bloquearia al servir el album por https, y iTunes marca sus muestras con un
  tipo de contenido que no todos los navegadores reconocen.
- **Acceso**: ver el album no pide identificarse. Editarlo si, con Google y una
  lista de correos autorizados.

## Configuracion del mapa

El mapa abre centrado en la ciudad que se defina en `config.js`, que el
navegador lee al cargar la pagina. `cityZoom` es lo cerca que se acerca al abrir
una ciudad; el boton "Vista general" devuelve el mapa a `center` y `zoom`. Para desarrollo, ese archivo es
`album-viajes-web/public/config.js`; en el servidor, el `config.js` de la raiz,
que `docker-compose.prod.yml` monta dentro del contenedor web.

```js
window.__ALBUM_VIAJES__ = {
  map: {
    centerLatitude: 4.711,    // Bogota
    centerLongitude: -74.0721,
    zoom: 6,                  // 6 encuadra Colombia; 11-12, una sola ciudad
  },
}
```

Al ser configuracion de ejecucion y no de compilacion, cambiar el centro es
editar el archivo y recargar: la misma imagen sirve para cualquier despliegue.
Si un valor falta o esta mal escrito, se usa el de por defecto (Bogota, zoom 6).

## Desarrollo

```bash
docker compose up -d                                  # PostgreSQL en el puerto 5433
cd album-viajes-api && dotnet run --project src/AlbumViajes.Api   # http://localhost:5120
cd album-viajes-web && npm install && npm run dev                 # http://localhost:5173
```

Sin credenciales de Google configuradas todo funciona salvo importar fotos. Para
habilitarlo en desarrollo, sin poner secretos en git:

```bash
cd album-viajes-api/src/AlbumViajes.Api
dotnet user-secrets init
dotnet user-secrets set "Auth:Google:ClientId" "..."
dotnet user-secrets set "Auth:Google:ClientSecret" "..."
dotnet user-secrets set "Auth:AllowedEmails:0" "tu-correo@gmail.com"
```

El URI de redireccion autorizado en Google debe ser
`http://localhost:5120/api/auth/google/callback`.

## Despliegue en el servidor propio

Cada push a `main` dispara `.github/workflows/deploy.yml`: prueba los dos
proyectos, publica sus imagenes en GHCR etiquetadas con el commit, las lleva al
servidor por SSH sobre Cloudflare Access y comprueba que el sitio responde. El
`.env` del servidor lo escribe el propio flujo con los secrets del repositorio.

El paso a paso, con las credenciales de Google, la lista de secrets, el proxy con
TLS y las copias de seguridad, esta en [`deploy/README.md`](deploy/README.md).

Para levantarlo a mano en el servidor:

```bash
docker compose pull
docker compose up -d
```

El servicio `web` escucha en `127.0.0.1:8081`; delante va un reverse proxy con
TLS (nginx + Let's Encrypt) del propio host, cuya configuracion esta en
`deploy/albumviajes.nginx.conf`. PostgreSQL no publica puertos: solo lo alcanza
la API por la red interna de Docker.

### Copias de seguridad

```bash
./deploy/backup.sh /var/backups/albumviajes
```

Respalda la base, el volumen de fotos y el volumen `dataprotection`, que guarda
las claves de cifrado de ASP.NET: si se pierde, se invalidan las sesiones y el
permiso de Google guardado.
