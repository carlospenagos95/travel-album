# Álbum de Viajes — Plan de MVP

## Context

Proyecto greenfield (directorio vacío). Objetivo: álbum personal de viajes donde cada ciudad visitada es un pin en un mapa, con fotos traídas de Google Photos, música local (radio o YouTube) que suena al abrir la ciudad, y una ficha con datos básicos (población, descripción, comida típica, sitios de interés). El usuario administra (crear/editar/eliminar ciudades); cualquiera con el link puede ver en modo lectura. Despliegue en servidor propio con Docker, repos en GitHub.

### Decisiones tomadas

| Área | Decisión |
|---|---|
| Backend | .NET 10 (LTS), ASP.NET Core Minimal APIs, Clean Architecture |
| Frontend | React + TypeScript + Vite, react-leaflet |
| DB | PostgreSQL 17 + EF Core 10 |
| Mapa | Leaflet + tiles OpenStreetMap (sin API key) |
| Fotos | Google Photos **Picker API** + copia local al servidor |
| Storage | Disco local en volumen Docker |
| Música | Radio Browser API + embed de YouTube |
| Datos ciudad | Wikidata + Wikipedia (auto), campos editables |
| Auth | Google OAuth, allowlist de un email; lectura pública sin login |
| Repos | Dos repos en GitHub (`album-viajes-api`, `album-viajes-web`) |

### Restricciones técnicas que condicionan el diseño

1. **Google Photos Library API ya no lista bibliotecas ajenas** (cambio de marzo 2025). La única vía soportada es la **Picker API**: el backend crea una sesión, el usuario abre `pickerUri`, elige fotos, el backend hace polling hasta `mediaItemsSet`, y recibe los `mediaItem`. Scope: `photospicker.mediaitems.readonly`. Los `baseUrl` **expiran (~60 min)** y la sesión también → por eso se descarga y guarda copia local inmediatamente. La descarga usa `baseUrl=d` con header `Authorization: Bearer <token>`.
2. **Muchos streams de Radio Browser son HTTP plano.** Si el frontend corre en HTTPS, el navegador bloquea el stream por mixed content. Solución: **el backend proxea el stream** (`GET /api/stations/{id}/stream`) reenviando los bytes sobre HTTPS.
3. **Radio Browser exige `User-Agent` propio** y recomienda resolver el host vía `all.api.radio-browser.info`.
4. **Comida típica no existe como dato estructurado en Wikidata.** Se autocompleta lo que sí existe (población P1082, coordenadas P625, descripción, sitios de interés vía Wikipedia geosearch) y comida típica queda como campo manual editable con valor sugerido vacío.

---

## Arquitectura

```
album-viajes-web (React+Vite)  ──HTTPS──▶  album-viajes-api (.NET 10)
        │                                        │
   Leaflet/OSM                          ┌────────┼────────┬──────────────┐
   <audio> / YouTube iframe             │        │        │              │
                                   PostgreSQL  /data   Google APIs   Wikidata/
                                              (fotos)  (OAuth+Picker) Wikipedia
                                                                    Radio Browser
```

Todo detrás de un Nginx reverse proxy en el servidor (TLS con Let's Encrypt). `docker-compose` con 4 servicios: `web`, `api`, `db`, `proxy`.

---

## Arquitectura limpia y código limpio

### Estructura del backend (4 proyectos + tests)

```
AlbumViajes.Domain          -- entidades, value objects, reglas de negocio, excepciones de dominio
   └─ SIN dependencias. Ni EF Core, ni ASP.NET, ni paquetes de terceros.

AlbumViajes.Application     -- casos de uso, DTOs, validadores, INTERFACES de puertos
   └─ depende solo de Domain. Aquí viven ICityRepository, IPhotoProvider,
      IWikidataService, IRadioDirectory, IFileStorage, IUnitOfWork.

AlbumViajes.Infrastructure  -- implementaciones: EF Core, HttpClients, disco, Google OAuth
   └─ depende de Application. Es el único que conoce Postgres, Google y Wikipedia.

AlbumViajes.Api             -- endpoints, DI, middleware, autenticación, mapeo HTTP
   └─ depende de Application e Infrastructure (solo para registrar el DI).

AlbumViajes.Tests.Unit         -- Domain y Application, sin infraestructura
AlbumViajes.Tests.Integration  -- Api + Postgres real vía Testcontainers
```

**Regla de dependencia**: las flechas apuntan siempre hacia adentro. Domain no sabe que existe una base de datos. Se verifica automáticamente con un test de **NetArchTest** o **ArchUnitNET** que falla el build si alguien mete `Microsoft.EntityFrameworkCore` en Domain o Application.

### Convenciones concretas

- **Un caso de uso = una clase**, con un solo método público: `CreateCityHandler`, `EnrichCityHandler`, `ImportPhotosHandler`. Nada de un `CityService` de 800 líneas. Orquestación vía **MediatR** o simple inyección directa del handler (para este tamaño de proyecto, inyección directa basta y evita magia).
- **Los endpoints no tienen lógica.** Un endpoint valida el request, invoca el handler y mapea el resultado a HTTP. Si un endpoint tiene un `if` de negocio, ese `if` va al Domain.
- **Entidades ricas, no anémicas.** `City.MarkEnriched(...)`, `City.Relocate(coords)`. Setters privados; el estado solo cambia por métodos con nombre de intención. `Photo.SortOrder` no se reasigna desde fuera: `City.ReorderPhotos(...)`.
- **Value objects** para lo que tiene invariantes: `Coordinates` (valida rango lat/lon en el constructor), `CountryCode` (ISO-3166 alfa-2). Elimina validaciones repetidas por toda la app.
- **DTOs en las fronteras.** Las entidades de dominio nunca se serializan a JSON ni se reciben del cliente. Request/Response records dedicados por endpoint.
- **Errores por Result, no por excepciones de control de flujo.** Excepciones solo para lo excepcional. Un `Result<T>` (o `OneOf`) para "ciudad no encontrada" o "coordenadas inválidas", traducido a 404/400 en un único lugar.
- **Nombres que se leen.** `ImportSelectedPhotosAsync` no `ProcImgs`. Sin abreviaturas, sin comentarios que expliquen qué hace el código — si hace falta un comentario para entender el *qué*, el método necesita mejor nombre o dividirse. Los comentarios se reservan para el *por qué* (ej. por qué se copia la foto en vez de guardar el `baseUrl`).
- **Funciones cortas, un nivel de abstracción por función**, sin parámetros booleanos de control (`Save(city, true)` → dos métodos distintos).
- **Sin estáticos ni singletons ocultos.** Todo por constructor. `IClock` inyectado en vez de `DateTime.UtcNow` regado, para que los tests sean deterministas.
- **`async` de punta a punta**, `CancellationToken` propagado en toda llamada a DB o red.
- **Nullable reference types activado** (`<Nullable>enable</Nullable>`) y `TreatWarningsAsErrors` en CI.
- **`.editorconfig`** con reglas de estilo + analizadores de .NET; el build de CI falla ante violaciones.

### Frontend limpio

- Carpetas **por feature**, no por tipo de archivo: `features/cities/`, `features/photos/`, `features/music/`. Nada de un cajón `components/` con 60 archivos.
- **Separar acceso a datos de la UI**: capa `api/` con un cliente tipado (tipos generados desde el OpenAPI que expone .NET) + **TanStack Query** para caché, reintentos y estados de carga. Los componentes no hacen `fetch`.
- **Componentes de presentación sin lógica de negocio**; la lógica reutilizable en hooks (`useCityForm`, `useRadioPlayer`).
- Sin `any`. `strict: true` en `tsconfig`.

### Definición de "terminado" para cada fase

Compila sin warnings, tests verdes, test de arquitectura verde, lint verde, y la funcionalidad verificada a mano en el entorno Docker.

---

## Modelo de datos

```
City
  Id (guid), Name, Country, CountryCode
  Latitude, Longitude
  VisitedOn (date, nullable), Notes
  Population (long?), PopulationSource, ShortDescription
  TypicalFood (text)             -- manual
  WikidataId, WikipediaUrl
  EnrichedAt (timestamp?)        -- null = nunca enriquecida
  CreatedAt, UpdatedAt

PointOfInterest        -- 1:N con City
  Id, CityId, Name, Description, Latitude?, Longitude?, WikipediaUrl, IsAutoGenerated

Photo                  -- 1:N con City
  Id, CityId, GooglePhotosMediaId, FileName, StoredPath, ThumbPath
  Width, Height, TakenAt?, Caption, SortOrder

MusicSource            -- 1:N con City (el MVP marca una como IsDefault)
  Id, CityId, Kind (RadioStation | YouTube), IsDefault
  RadioStationUuid, RadioStationName, RadioStreamUrl   -- si Kind=RadioStation
  YouTubeVideoId                                        -- si Kind=YouTube
```

Migraciones con EF Core (`dotnet ef migrations add`), aplicadas al arrancar el contenedor.

---

## API (endpoints principales)

**Público (sin auth):**
- `GET /api/cities` — lista para el mapa (id, nombre, coords, foto de portada)
- `GET /api/cities/{id}` — ficha completa con POIs, fotos y fuentes de música
- `GET /api/photos/{id}/file` y `/thumb` — sirve el archivo desde `/data`
- `GET /api/stations/{uuid}/stream` — proxy del stream de radio (resuelve el mixed content)

**Protegido (Google OAuth + allowlist):**
- `POST|PUT|DELETE /api/cities[/{id}]`
- `POST /api/cities/{id}/enrich` — dispara Wikidata/Wikipedia y devuelve propuesta editable
- `POST /api/cities/{id}/photos/picker-session` → devuelve `pickerUri`
- `GET  /api/cities/{id}/photos/picker-session/{sessionId}` → polling del estado
- `POST /api/cities/{id}/photos/import` → descarga y guarda las seleccionadas
- `DELETE /api/photos/{id}`
- `GET  /api/stations/search?city=&country=` — proxy de Radio Browser
- `POST|DELETE /api/cities/{id}/music`

**Auth:** `GET /api/auth/login`, `/api/auth/callback`, `/api/auth/me`, `/api/auth/logout`. Cookie de sesión `HttpOnly`+`Secure`+`SameSite=Lax`. El refresh token de Google se guarda cifrado en DB (Data Protection API) porque hace falta para descargar de la Picker API.

---

## Fases de implementación

### Fase 0 — Andamiaje — **COMPLETADA**
- Dos repos en GitHub, `.gitignore`, README.
- Solución .NET con los 4 proyectos en capas + los 2 de test (ver sección de arquitectura), referencias entre proyectos configuradas para forzar la regla de dependencia.
- Test de arquitectura (NetArchTest) desde el primer commit: falla si Domain o Application referencian infraestructura.
- `.editorconfig`, analizadores, `Nullable enable`, `TreatWarningsAsErrors` en CI.
- Vite + React + TS (`strict`), ESLint, Prettier, estructura por features.
- `docker-compose.yml` de desarrollo con Postgres.
- Dockerfiles multi-stage (API: `sdk` → `aspnet` runtime; Web: `node` build → `nginx:alpine`).
- Health check `GET /health`.

### Fase 1 — CRUD de ciudades + mapa (núcleo) — **COMPLETADA Y VERIFICADA**
- Entidad `City`, migración inicial, endpoints CRUD con validación (FluentValidation).
- Frontend: mapa Leaflet a pantalla completa con marcadores agrupados, panel lateral de detalle al hacer clic, formulario de alta/edición, confirmación de borrado.
- Búsqueda de ciudad al crear: **Nominatim (OSM)** para autocompletar nombre y coordenadas — evita que el usuario teclee lat/lon a mano. Respetar su política de uso (User-Agent, 1 req/s, debounce en el cliente).
- **Punto de corte útil**: acá ya hay algo desplegable y usable.

### Fase 2 — Enriquecimiento automático — **COMPLETADA**
- `WikidataService`: SPARQL o `wbsearchentities` + `wbgetentities` → P1082 (población), P625 (coords), sitelink a Wikipedia.
- `WikipediaService`: REST `/page/summary/{title}` para la descripción; `action=query&list=geosearch` (radio ~10 km) para candidatos a sitios de interés.
- El resultado se presenta como **propuesta editable en la UI**, no se escribe a ciegas. `EnrichedAt` marca la última corrida.
- Cachear respuestas en DB; nunca llamar a estas APIs en el render público.

### Fase 3 — Fotos (la pieza más costosa) — **COMPLETADA**
- Google Cloud project, pantalla de consentimiento, habilitar **Photos Picker API**, credenciales OAuth (client id/secret por variables de entorno).
- Flujo completo: crear sesión → mostrar `pickerUri` (nueva pestaña o QR) → polling con el `pollingConfig` que devuelve Google → importar.
- Al importar: descargar original, generar thumbnail (**ImageSharp**), guardar en `/data/photos/{cityId}/`, persistir `Photo`. Nombres de archivo por GUID, nunca por nombre de origen.
- Galería en el detalle de ciudad + lightbox. Reordenar y borrar.
- Servir con `Cache-Control: public, max-age=31536000, immutable`.

### Fase 4 — Música — **COMPLETADA**
- `GET /api/stations/search` → proxy a Radio Browser (`all.api.radio-browser.info`, User-Agent propio, resultados cacheados). Buscar por `state`/`name`, filtrar por `countrycode`, ordenar por `votes`.
- UI: buscador de emisoras al editar la ciudad, o pegar link de YouTube.
- Reproductor: `<audio>` apuntando al endpoint proxy, o `<iframe>` de YouTube.
- **Autoplay**: los navegadores bloquean el audio sin interacción previa del usuario. Manejar el rechazo de `play()` mostrando un botón de play en vez de fallar en silencio.

### Fase 5 — Despliegue — **COMPLETADA** (falta ejecutarla en el servidor real)
- `docker-compose.prod.yml`: `db` (con volumen), `api`, `web`, `proxy` (Nginx + certbot).
- Volúmenes: `pgdata`, `photos`.
- GitHub Actions por repo: build → test → `docker build` → push a **GHCR** (`ghcr.io/<user>/album-viajes-api:sha`).
- En el servidor: `docker compose pull && docker compose up -d`. Para el MVP basta hacerlo por SSH manualmente; el auto-deploy es post-MVP.
- Secretos por variables de entorno / `.env` fuera de git: cadena de conexión, Google client id/secret, email de la allowlist, clave de Data Protection.
- Backup: `pg_dump` en cron + copia de la carpeta `photos`.

---

## Recomendaciones

- **Empieza por Fase 1 y despliégala.** Tener el pipeline Docker+GHCR+servidor funcionando con algo trivial es mucho más barato que depurarlo al final con cuatro integraciones encima.
- **Cada API externa detrás de su propia interfaz** (`IWikidataService`, `IPhotoProvider`, `IRadioDirectory`). Google ya cambió su API una vez; va a volver a pasar.
- **Toda llamada externa con timeout, reintento y caché.** Usa `HttpClientFactory` con Polly. Nada de que el detalle de una ciudad dependa de que Wikipedia responda.
- **Persiste la Data Protection key en un volumen**, no en el contenedor efímero, o cada redeploy invalida sesiones y tokens cifrados.
- Post-MVP natural: línea de tiempo del viaje, rutas entre ciudades, PWA offline, export a PDF.

---

## Hallazgos durante la implementación

- **PostgreSQL local en el 5432**: el contenedor se mapea al **5433** del host para no
  chocar con el PostgreSQL instalado en la máquina.
- **La imagen `aspnet:10.0` no trae `wget` ni `curl`**: el healthcheck de compose
  necesitaba instalar curl en la imagen final.
- **Docker Desktop 24.0.2 habla API 1.43** y Testcontainers pide la 1.44: los tests
  de integración requieren `DOCKER_API_VERSION=1.43` hasta actualizar Docker.
- **Precisión de tiempo**: PostgreSQL guarda microsegundos y `DateTimeOffset` usa
  ticks de 100 ns, así que la misma entidad devolvía horas distintas en memoria y
  releída. `SystemClock` trunca a microsegundos en el origen.
- **Los value objects no se traducen por `.Value` en LINQ**: `ICityRepository`
  recibe `CountryCode`, no `string`.
- **`window.confirm` bloquea el hilo del navegador**: el borrado usa una
  confirmación inline dentro del panel.

## Estado final del MVP

Las cinco fases están implementadas. Lo que queda es operativo, no de código:
crear los dos repositorios en GitHub, crear el proyecto de Google Cloud con la
Photos Picker API y ejecutar el despliegue en el servidor con TLS. El
procedimiento está escrito en `deploy/README.md`.

Autenticación: Google OAuth con lista de correos autorizados, sesión en cookie
propia (`HttpOnly`, `SameSite=Lax`, `Secure` según el esquema) y el refresh token
de Google cifrado con Data Protection en la base. Lectura pública; toda
escritura exige la política `Owner`.

Pruebas: 108 en total (74 unitarias, 10 de arquitectura, 24 de integración
contra PostgreSQL real). Las de arquitectura ahora también verifican que cada
caso de uso tenga un único método público llamado `HandleAsync` con
`CancellationToken`.

## Hallazgos de las fases 3 a 5

- **EF Core convertía los INSERT de las entidades hijas en UPDATE.** Al añadir
  una `Photo` o un `MusicSource` al agregado, EF los tomaba por filas existentes
  y emitía un `UPDATE` que afectaba a 0 filas
  (`DbUpdateConcurrencyException`). La causa: por convención da los `Guid` de
  clave por generados en la base, y si la clave ya viene puesta concluye que la
  entidad existe. Se corrige declarando `ValueGeneratedNever()`, que además es
  la verdad: los identificadores los genera el dominio.
- **Un grupo de rutas aplica sus convenciones a todos sus endpoints**, también a
  los declarados antes de llamar a `RequireAuthorization`. Al reutilizar el mismo
  grupo, la galería pública y el stream de radio quedaban cerrados con 401. Lo
  público y lo protegido cuelgan ahora de dos grupos distintos sobre la misma ruta.
- **Un índice único parcial no sirve para "solo una fuente por ciudad suena".**
  Cambiar de fuente actualiza dos filas en el mismo lote y PostgreSQL no permite
  aplazar la comprobación de un índice parcial, así que el guardado fallaba o no
  según el orden que eligiera EF Core: un test que pasaba dos de cada tres veces.
  La invariante la garantiza el agregado, que se carga y se guarda entero.
- **Los volúmenes montados los crea Docker con dueño root**, y el proceso corre
  sin privilegios: sin crear `/data/photos` y el directorio de claves en la
  imagen con el dueño correcto, la API no puede escribir ni fotos ni claves.
- **Detrás del proxy con TLS hace falta `UseForwardedHeaders`.** La petición
  llega por http y, sin traducir `X-Forwarded-Proto`, la API construye el
  `redirect_uri` de Google con ese esquema y Google lo rechaza. La lista de
  proxies conocidos se vacía a propósito: la API no publica puertos fuera de la
  red interna de Docker, y por defecto solo confiaría en loopback.
- **El stream de radio necesita `proxy_buffering off` en nginx.** Un stream no
  termina nunca; con el buffer activado nginx espera la respuesta completa y el
  audio no llega a sonar. Por lo mismo, el `HttpClient` del proxy no lleva
  timeout global sino límite de conexión.
- **`Results.SignOut` con `RedirectUri` devuelve un 302** que `fetch` sigue hasta
  el HTML del sitio. El cierre de sesión no redirige: solo borra la cookie.
- El tipo `AudioStream` tuvo que renombrarse a `AudioFeed` (CA1711: un tipo que
  no deriva de `Stream` no debe llamarse así), y el value object `YouTubeVideoId`
  a `YouTubeVideo`, porque chocaba con la propiedad del mismo nombre.

## Verificación

1. `docker compose up` levanta los 4 servicios; `GET /health` responde 200.
2. Crear una ciudad desde la UI → aparece el pin en el mapa → recargar → sigue ahí.
3. Editar y eliminar la ciudad → cambios reflejados sin recargar a mano.
4. "Enriquecer" sobre una ciudad conocida (ej. Medellín) → devuelve población y descripción coherentes; los valores son editables antes de guardar.
5. Flujo de fotos completo: login Google → picker → seleccionar 3 fotos → aparecen en la galería → el archivo existe en el volumen → **desconectar internet y recargar: las fotos siguen viéndose** (confirma que la copia local funciona, no el `baseUrl` de Google).
6. Buscar emisora de esa ciudad → suena al pulsar play → verificar en DevTools que el stream sale del dominio propio, no de una URL `http://` externa.
7. Abrir la app en ventana de incógnito sin login: se ve el mapa y las fichas; los botones de edición no aparecen y `POST /api/cities` responde 401.
8. Tests: unitarios de Domain (invariantes de `Coordinates`, métodos de `City`) y de los handlers con puertos simulados; de integración del CRUD con Testcontainers (Postgres real).
9. Test de arquitectura verde: agregar a propósito una referencia de EF Core en Domain debe **romper el build**.
10. `dotnet build` y `npm run build` sin un solo warning.
