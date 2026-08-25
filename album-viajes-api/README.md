# Album de Viajes — API

Backend en .NET 10 con arquitectura limpia. Sirve el mapa de ciudades visitadas:
lectura publica, escritura para el propietario del album.

## Estructura

```
src/AlbumViajes.Domain          Entidades, value objects y reglas. Sin dependencias.
src/AlbumViajes.Application     Casos de uso, DTOs, validadores e interfaces de puertos.
src/AlbumViajes.Infrastructure  EF Core, PostgreSQL y clientes de servicios externos.
src/AlbumViajes.Api             Endpoints, DI y traduccion a HTTP.

tests/AlbumViajes.Tests.Unit          Dominio y casos de uso con puertos simulados.
tests/AlbumViajes.Tests.Architecture  Verifica la regla de dependencia de las capas.
tests/AlbumViajes.Tests.Integration   API contra un PostgreSQL real (Testcontainers).
```

Las dependencias apuntan siempre hacia adentro. Los tests de arquitectura fallan
el build si alguien mete EF Core o ASP.NET en Domain o Application.

## Desarrollo

Requiere .NET 10 SDK y Docker.

```bash
# PostgreSQL (desde la raiz del proyecto, un nivel arriba)
docker compose up -d

# API en http://localhost:5120
dotnet run --project src/AlbumViajes.Api
```

La base escucha en el puerto **5433** del host a proposito: el 5432 suele estar
ocupado por un PostgreSQL instalado localmente.

Las migraciones se aplican solas al arrancar.

```bash
dotnet test                                    # todos los tests
dotnet build                                   # falla ante cualquier warning
```

### Tests de integracion y la version de Docker

Los tests de integracion levantan un PostgreSQL real con Testcontainers, que
habla la API 1.44 del daemon. Docker Desktop 24.x solo llega a la 1.43 y los
tests fallan con `client version 1.44 is too new`. Hasta actualizar Docker
Desktop, ejecutalos asi:

```bash
DOCKER_API_VERSION=1.43 dotnet test
```

### Nueva migracion

```bash
dotnet ef migrations add NombreDeLaMigracion \
  --project src/AlbumViajes.Infrastructure \
  --startup-project src/AlbumViajes.Infrastructure \
  --output-dir Persistence/Migrations
```

Usa `DesignTimeDbContextFactory`, de modo que el proyecto Api no necesita el
paquete `Microsoft.EntityFrameworkCore.Design`. Para apuntar a otra base, define
`ALBUMVIAJES_DESIGN_CONNECTION`.

## Endpoints

| Metodo | Ruta                | Descripcion                     |
| ------ | ------------------- | ------------------------------- |
| GET    | `/health`           | Estado del servicio y de la base |
| GET    | `/api/cities`       | Ciudades para el mapa            |
| GET    | `/api/cities/{id}`  | Ficha completa                   |
| POST   | `/api/cities`       | Crear                            |
| PUT    | `/api/cities/{id}`  | Actualizar                       |
| DELETE | `/api/cities/{id}`  | Eliminar                         |
| POST   | `/api/cities/{id}/enrich` | Completa la ficha desde Wikipedia y Wikidata |

Fotos, todas colgando de la ciudad porque una foto solo existe dentro de su album:

| Metodo | Ruta                                                    | Acceso  |
| ------ | ------------------------------------------------------- | ------- |
| GET    | `/api/cities/{id}/photos/{photoId}/file`                | Publico |
| GET    | `/api/cities/{id}/photos/{photoId}/thumbnail`           | Publico |
| POST   | `/api/cities/{id}/photos/picker-session`                | Duenio  |
| GET    | `/api/cities/{id}/photos/picker-session/{sessionId}`    | Duenio  |
| POST   | `/api/cities/{id}/photos/import?sessionId=`             | Duenio  |
| PUT    | `/api/cities/{id}/photos/order`                         | Duenio  |
| PUT    | `/api/cities/{id}/photos/{photoId}/caption`             | Duenio  |
| DELETE | `/api/cities/{id}/photos/{photoId}`                     | Duenio  |

Musica:

| Metodo | Ruta                                              | Acceso  |
| ------ | ------------------------------------------------- | ------- |
| GET    | `/api/cities/{id}/music/{musicId}/stream`         | Publico |
| GET    | `/api/stations/search?query=&countryCode=`        | Duenio  |
| POST   | `/api/cities/{id}/music/radio`                    | Duenio  |
| POST   | `/api/cities/{id}/music/youtube`                  | Duenio  |
| PUT    | `/api/cities/{id}/music/{musicId}/default`        | Duenio  |
| DELETE | `/api/cities/{id}/music/{musicId}`                | Duenio  |

Sesion:

| Metodo | Ruta                | Descripcion                                  |
| ------ | ------------------- | -------------------------------------------- |
| GET    | `/api/auth/me`      | Si quien mira puede editar                   |
| GET    | `/api/auth/login`   | Redirige a Google                            |
| POST   | `/api/auth/logout`  | Borra la cookie de sesion                    |

En desarrollo, la especificacion OpenAPI queda en `/openapi/v1.json`.

## Datos externos

`POST /api/cities/{id}/enrich` busca el articulo de Wikipedia mas cercano a las
coordenadas de la ciudad (no por nombre: los nombres se repiten entre paises, un
punto en el mapa no), toma de el el resumen, el enlace y el identificador de
Wikidata, y de Wikidata la poblacion mas reciente con su ano.

Va aparte del guardado a proposito: registrar una ciudad no puede depender de
que Wikipedia responda. Si la fuente falla, la peticion devuelve 502 y la ficha
se queda intacta.

```json
"Enrichment": {
  "WikipediaLanguage": "es",
  "UserAgent": "AlbumViajes/1.0 (https://tu-dominio.com; tu-correo@ejemplo.com)",
  "SearchRadiusMeters": 10000,
  "TimeoutSeconds": 15
}
```

Wikimedia exige un `User-Agent` que identifique la aplicacion y de un contacto:
con el valor por defecto puede responder 403. En el servidor se pasa por la
variable `Enrichment__UserAgent`.

## Fotos

Google cerro en marzo de 2025 la lectura de bibliotecas ajenas: una aplicacion de
terceros ya no puede recorrer las fotos del usuario. La unica via soportada es la
**Picker API**, y es la que implementa `GooglePhotosLibrary`: se abre una sesion,
el usuario elige en la interfaz de Google, se consulta cada pocos segundos si ya
termino y se lee lo elegido.

Los enlaces que devuelve caducan en torno a una hora, asi que **la foto se copia
al servidor** en el momento: guardar el enlace dejaria el album sin fotos al dia
siguiente. Del original se conservan los bytes tal cual, sin recodificar, y
aparte se genera una miniatura con ImageSharp. El archivo se nombra por GUID; el
nombre de origen solo se guarda para mostrarlo.

El permiso de Google se guarda como *refresh token* cifrado con Data Protection.
Es lo que evita tener que volver a autorizar cada hora, y por eso las claves de
Data Protection viven en un volumen.

```json
"Auth": {
  "AllowedEmails": [ "tu-correo@gmail.com" ],
  "PostLoginRedirect": "/",
  "Google": { "ClientId": "", "ClientSecret": "" }
},
"PhotoStorage": { "RootPath": "/data/photos", "ThumbnailWidth": 480 }
```

Sin `ClientId` la aplicacion arranca igual: el esquema de Google no se registra y
lo unico que queda deshabilitado es la entrada, y con ella las fotos.

## Musica

`GET /api/stations/search` es un proxy de Radio Browser, que exige un
`User-Agent` propio y recomienda entrar por `all.api.radio-browser.info` en vez
de fijar un servidor. Se pide con `hidebroken` y ordenado por votos: el catalogo
esta lleno de emisoras muertas.

`GET /api/cities/{id}/music/{musicId}/stream` reenvia el audio de la emisora.
Existe porque buena parte del directorio publica todavia URLs en http plano, que
el navegador bloquearia por contenido mixto al servir el album por https. El
cliente HTTP de este proxy no tiene timeout global —un stream no termina nunca—
sino un limite al conectar.

## Convenciones

- Un caso de uso por clase, con un solo metodo publico.
- Los endpoints no llevan logica: validan, delegan y traducen a HTTP.
- Los fallos esperados viajan en `Result<T>`; las excepciones se reservan para lo
  excepcional. `ResultExtensions` es el unico sitio que asigna codigos HTTP.
- Entidades con setters privados: el estado cambia por metodos con nombre de intencion.
- `IClock` inyectado en vez de `DateTime.UtcNow`, para que los tests sean deterministas.
- Los identificadores los genera el dominio, no la base: las configuraciones lo
  declaran con `ValueGeneratedNever()`. No es cosmetico. Si EF cree que los
  genera el, al descubrir una entidad nueva dentro del agregado la toma por
  existente y emite un `UPDATE` en lugar de un `INSERT`.
- Un grupo de rutas aplica sus convenciones a todos sus endpoints, tambien a los
  declarados antes de pedir autenticacion. Por eso lo publico y lo protegido
  cuelgan de dos grupos distintos sobre la misma ruta.
