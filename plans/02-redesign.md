# Plan: crear 4 specs para el rediseno del album

## Contexto

El album de viajes (frontend React+TS+Vite+Leaflet, backend .NET 10) necesita un
rediseno visual orientado a "album de fotos fisico" sin perder funcionalidad, mas
tres mejoras de comportamiento: spinner de carga en fotos, musica que sigue
sonando al cerrar la ficha (se corta solo al abrir otra ciudad), y zoom del mapa
en funcion del area urbana real de la ciudad (bounding box) en vez de un zoom fijo.

Se investigo el codigo actual:
- `CityDetailPanel.tsx` monta `MusicPanel` -> `MusicPlayer`; al cerrar la ficha
  (`onClose` -> `selectCity(null)` en `App.tsx`) el panel completo se desmonta,
  cortando el audio. Hay que separar "ciudad seleccionada para ver ficha" de
  "ciudad cuya musica esta sonando".
- `PhotoGallery.tsx` y `PhotoLightbox.tsx` usan `<img>` planas sin estado de
  carga; no hay spinner.
- `CityMap.tsx` usa un `zoom` fijo de `mapConfig` (de `config.js`) para todo el
  mapa; no hay zoom por ciudad ni bounding box en el modelo de datos.
- El backend (`City.cs`, `Coordinates.cs`, `CityContracts.cs`,
  `SaveCityRequestValidator.cs`) no guarda area urbana, solo `Coordinates`
  (lat/lng). `nominatim.ts` (busqueda de ciudad) llama a Nominatim, que siempre
  devuelve `boundingbox` en la respuesta aunque hoy no se use.
- No existe carpeta `specs/` todavia; se crea con esta tanda.

Dado que el trabajo toca 4 areas independientes (visual, spinner, musica,
zoom+backend), el usuario eligio dividir en 4 specs en vez de una sola.

Todas las decisiones de producto ya se resolvieron con el usuario via preguntas
(ver seccion Decisiones en cada spec). Este plan no implica escribir codigo:
al aprobarse, se crean los 4 archivos `specs/01..04-*.md` con el contenido
integro que aparece abajo (mas `specs/.spec-config.yml` si no existe).

## Archivos a crear

- `specs/01-redesign-album-fisico.md`
- `specs/02-spinner-carga-fotos.md`
- `specs/03-musica-persistente-al-cerrar.md`
- `specs/04-zoom-area-urbana.md`
- `specs/.spec-config.yml` (solo si no existe, con `AutoCreateBranch: true`)

Los 4 se guardan en estado `Draft`. El usuario los aprueba manualmente despues
de releerlos.

---

## Contenido: specs/01-redesign-album-fisico.md

```markdown
# SPEC 01 — Rediseno visual: album fisico

> **Status:** Draft
> **Depends on:** (ninguno)
> **Date:** 2026-09-02
> **Objective:** Reestructurar el layout y la piel visual del album para que se
> sienta como un album de fotos fisico (tapa/indice, pagina de ciudad, marcos
> tipo polaroid, transiciones de pagina), sin quitar ninguna funcionalidad actual.

## Scope

**In:**

- Nueva paleta, tipografia (incluye una fuente con caracter manuscrito para
  titulos) y textura sutil de papel en fondo de sidebar y ficha.
- Sidebar (`CityList.tsx`) rediseñada como "indice/tapa" del album.
- `CityDetailPanel.tsx` rediseñado como "pagina" de album: fotos con marco tipo
  polaroid, cabecera distinta.
- Transicion de apertura de ficha (efecto tipo "pagina que se voltea" al abrir
  `CityDetailPanel`) y transicion entre fotos en `PhotoLightbox.tsx` (fade o
  deslizamiento al pasar de una foto a otra).
- Estilos nuevos viven en `index.css` (o se separan por componente si el
  archivo crece demasiado; decision de implementacion, no de producto).
- Todos los componentes y textos existentes se mantienen: no se quita ninguna
  seccion de la ficha (Descripcion, Comida tipica, Notas, Wikipedia, Musica,
  Fotos, Fuentes externas, acciones Editar/Eliminar).

**Out of scope (for future specs):**

- Modo oscuro (se propuso y se descarta por ahora).
- El spinner de carga de fotos (SPEC 02).
- El comportamiento de la musica al cerrar ficha (SPEC 03).
- El zoom del mapa por area urbana (SPEC 04).
- Cambios de backend: este spec es 100% frontend/CSS/estructura de componentes.

## Data model

Esta funcionalidad no introduce estructuras de datos nuevas. Es un cambio
visual y de composicion de componentes existentes.

## Implementation plan

1. Definir paleta y tipografia nuevas como variables CSS (`:root`) en
   `index.css`; aplicar fondo/textura de papel al `.layout`.
2. Rediseñar `CityList.tsx` (o su CSS) como indice de album: portada mas
   protagonista, tipografia de titulo distinta.
3. Rediseñar `CityDetailPanel.tsx` (CSS) como pagina de album: marco de
   contenido, cabecera con estilo de "titulo de pagina".
4. Aplicar marco tipo polaroid a las miniaturas en `PhotoGallery.tsx`
   (solo CSS, sin tocar la logica de arrastre/orden/borrado existente).
5. Agregar transicion de apertura a `CityDetailPanel.tsx` (animacion CSS al
   montarse, ej. `@keyframes` de rotacion/escala breve).
6. Agregar transicion de cambio de foto en `PhotoLightbox.tsx` (fade/slide al
   cambiar `index`, vía CSS o una clase temporal en el `<figure>`).
7. Revisar visualmente cada seccion de la ficha para confirmar que ninguna
   funcionalidad quedo oculta o rota por el nuevo CSS.

## Acceptance criteria

- [ ] La sidebar y la ficha de ciudad muestran la nueva paleta/tipografia/textura.
- [ ] Las miniaturas de `PhotoGallery` tienen marco tipo polaroid.
- [ ] Abrir una ficha dispara una transicion visible (no aparece instantaneo).
- [ ] Navegar entre fotos en el lightbox dispara una transicion visible.
- [ ] Todas las acciones que existian antes (agregar/editar/eliminar ciudad,
      reordenar/poner pie/quitar foto, importar fotos, agregar/quitar musica,
      traer datos de Wikipedia, cerrar sesion) siguen funcionando igual.
- [ ] No aparecen errores en consola del navegador al navegar por el album.

## Decisions

- **Si:** reestructurar layout (no solo reskin). Motivo: el usuario pidio
  explicitamente que se sienta como album fisico, no solo cambiar colores.
- **Si:** direccion "album fisico" (tapa/indice, pagina, marcos polaroid,
  textura papel) sobre mosaico tipo Google Photos o timeline. Elegida por el
  usuario entre 3 opciones.
- **Si:** incluir transiciones/animaciones en este spec (apertura de ficha y
  cambio de foto). El usuario lo pidio como extra al cierre de preguntas.
- **No:** modo oscuro ahora. Se descarta explicitamente; puede ser spec futuro.
- **No:** tocar backend. El redesign es puramente visual/estructural en
  frontend.

## What is **not** in this spec

- Modo oscuro.
- Spinner de carga de fotos (SPEC 02).
- Persistencia de musica al cerrar ficha (SPEC 03).
- Zoom por area urbana (SPEC 04).

Cada uno de esos, si se hace, va en su propio spec.
```

---

## Contenido: specs/02-spinner-carga-fotos.md

```markdown
# SPEC 02 — Spinner de carga en fotos

> **Status:** Draft
> **Depends on:** (ninguno; compatible con SPEC 01 pero no depende de el)
> **Date:** 2026-09-02
> **Objective:** Mostrar un spinner circular sobre cada foto mientras su imagen
> carga, tanto en las miniaturas de la galeria como en la foto grande del
> lightbox.

## Scope

**In:**

- Spinner circular simple (CSS puro, sin libreria nueva) superpuesto sobre el
  espacio de la imagen mientras esta no termino de cargar.
- Aplica a las miniaturas de `PhotoGallery.tsx` (`<img className="thumb">`,
  archivo `album-viajes-web/src/features/photos/components/PhotoGallery.tsx`).
- Aplica a la imagen grande de `PhotoLightbox.tsx`
  (`album-viajes-web/src/features/photos/components/PhotoLightbox.tsx`), incluso
  al navegar de una foto a otra con las flechas o el teclado.
- El estado de carga se resuelve con el evento nativo `onLoad` (y `onError`
  para no dejar el spinner girando para siempre si la imagen falla).

**Out of scope (for future specs):**

- Precarga de fotos siguientes/anteriores en el lightbox (prefetch).
- Placeholder tipo blur-hash o skeleton (se eligio spinner simple, no skeleton).
- Cambios visuales del resto del album (eso es SPEC 01).

## Data model

Esta funcionalidad no introduce estructuras de datos nuevas ni cambia
`api/types.ts`. Es estado local de UI (por imagen, "cargando" o "lista").

## Implementation plan

1. Crear un componente pequeno reutilizable, ej.
   `album-viajes-web/src/features/photos/components/LoadingImage.tsx`, que
   envuelve un `<img>` con estado `isLoaded` (`useState`, default `false`) y
   pinta el spinner mientras `isLoaded` es `false`.
2. Usar `LoadingImage` en las miniaturas de `PhotoGallery.tsx` en lugar del
   `<img className="thumb">` actual, manteniendo `loading="lazy"` y el resto de
   props (`alt`, `src`).
3. Usar `LoadingImage` en la imagen del `<figure>` de `PhotoLightbox.tsx`,
   reiniciando el estado de carga cuando cambia `photo.id` (para que el spinner
   vuelva a aparecer al navegar a una foto que no estaba precargada).
4. Agregar el CSS del spinner (`@keyframes spin`, tamano fijo, centrado sobre
   el contenedor) a `index.css`.

## Acceptance criteria

- [ ] Al abrir el album con conexion lenta (throttling en DevTools), las
      miniaturas muestran spinner hasta que cada imagen termina de cargar.
- [ ] Al abrir el lightbox, la foto grande muestra spinner hasta que carga.
- [ ] Al navegar con flechas/teclado a una foto no cargada antes, el spinner
      vuelve a aparecer para esa foto.
- [ ] Si una imagen falla al cargar (`onError`), el spinner desaparece (no gira
      indefinidamente).
- [ ] Ninguna funcionalidad existente de la galeria (reordenar, poner pie,
      quitar foto, abrir lightbox) se rompe.

## Decisions

- **Si:** spinner circular CSS simple. Motivo: elegido por el usuario sobre
  skeleton pulsante; no requiere medir dimensiones de la imagen de antemano.
- **Si:** aplicar tanto a miniaturas como a lightbox. Motivo: elegido por el
  usuario para cubrir los dos puntos donde se nota la carga.
- **No:** libreria externa de lazy-loading/placeholders. El proyecto no usa
  ninguna hoy y el caso se resuelve con `onLoad`/`onError` nativos.

## What is **not** in this spec

- Precarga de fotos adyacentes en el lightbox.
- Skeleton/blur-hash como estilo de placeholder.
- Cualquier cambio visual fuera del propio indicador de carga (SPEC 01 cubre
  el resto del rediseno).
```

---

## Contenido: specs/03-musica-persistente-al-cerrar.md

```markdown
# SPEC 03 — Musica persiste al cerrar la ficha

> **Status:** Draft
> **Depends on:** (ninguno)
> **Date:** 2026-09-02
> **Objective:** La musica de una ciudad debe seguir sonando aunque se cierre su
> ficha de detalle, y solo cortarse cuando se abre la ficha de otra ciudad.

## Scope

**In:**

- Separar el estado "ciudad seleccionada para mostrar ficha" (`selectedId` en
  `album-viajes-web/src/app/App.tsx`) de un nuevo estado "ciudad cuya musica
  esta activa" (ej. `activeMusicCityId`).
- `activeMusicCityId` solo cambia cuando se abre la ficha de una ciudad
  **distinta** a la que esta sonando. Cerrar la ficha (`onClose`) o
  deseleccionar (volver a `null`) NO cambia `activeMusicCityId`: la musica sigue.
- Reabrir la ficha de la misma ciudad que ya suena no reinicia el reproductor
  (no debe cortar y volver a arrancar el audio).
- Mientras `activeMusicCityId` no sea `null` y la ficha de detalle este
  cerrada, se muestra una barra mini flotante fija (ej. abajo del todo de la
  pantalla) con el nombre de la ciudad/fuente sonando y un boton de
  pausa/reproducir. Al reabrir la ficha de esa misma ciudad, la barra mini
  desaparece (el reproductor completo vuelve a verse dentro de `MusicPanel`).
- El reproductor real (`MusicPlayer.tsx`) se monta una sola vez por
  `activeMusicCityId` (fuera del árbol de `CityDetailPanel`, en `App.tsx` o un
  componente hermano) para que desmontar/montar la ficha no reinicie el audio.

**Out of scope (for future specs):**

- Sincronizar volumen/estado de pausa entre la barra mini y el reproductor
  completo de forma mas alla de lo minimo (un solo estado de audio compartido,
  no dos reproductores independientes).
- Recordar la musica activa entre recargas de pagina (F5) o pestañas nuevas.
- Cambios en como se elige o edita la musica de una ciudad (`MusicEditor.tsx`,
  `MusicPanel.tsx` siguen igual salvo el punto donde se monta `MusicPlayer`).

## Data model

Esta funcionalidad no introduce estructuras de datos nuevas en la API. Agrega
un estado de React nuevo en el frontend:

```ts
// App.tsx (o un contexto/hook dedicado)
const [activeMusicCityId, setActiveMusicCityId] = useState<string | null>(null)
```

## Implementation plan

1. En `App.tsx`, agregar el estado `activeMusicCityId` y la funcion
   `selectCity` actualizada: al seleccionar un `id` no nulo distinto del
   `activeMusicCityId` actual, actualizar tambien `activeMusicCityId`. Al
   cerrar/deseleccionar (`id === null`), NO tocar `activeMusicCityId`.
2. Extraer el reproductor de `MusicPanel.tsx` (`<MusicPlayer key={playing.id}
   source={playing} />`) para que en `App.tsx` se monte un `MusicPlayer` que
   dependa de `activeMusicCityId` (usando `useCity(activeMusicCityId)` para
   obtener sus `musicSources`), en vez de depender de que `CityDetailPanel`
   este montado.
3. Ajustar `MusicPanel.tsx` para que, cuando la ficha esta abierta y
   corresponde a `activeMusicCityId`, muestre el reproductor ya montado en
   `App.tsx` (o una vista de "esta sonando" sin volver a montar `<audio>`/
   `<iframe>`), evitando doble reproduccion.
4. Crear el componente de barra mini flotante (ej.
   `album-viajes-web/src/features/music/components/MiniPlayerBar.tsx`),
   visible cuando `activeMusicCityId !== null && selectedId !==
   activeMusicCityId` (ficha cerrada o mostrando otra ciudad), con nombre de la
   fuente y boton de pausa que controla el mismo elemento de audio/iframe.
5. Verificar manualmente: abrir ciudad A (suena), cerrar ficha (sigue sonando,
   aparece barra mini), reabrir A (barra desaparece, sigue sonando sin
   reiniciar), abrir ciudad B (corta A, empieza B).

## Acceptance criteria

- [ ] Al cerrar la ficha de una ciudad con musica, el audio/video sigue
      sonando.
- [ ] Al abrir la ficha de OTRA ciudad, la musica de la anterior se corta y
      empieza (o no, si no tiene) la de la nueva.
- [ ] Al reabrir la ficha de la MISMA ciudad que sigue sonando, el audio no se
      reinicia (no hay corte perceptible).
- [ ] Con la ficha cerrada y musica activa, se ve una barra mini flotante con
      el nombre de la ciudad/fuente y un boton de pausa funcional.
- [ ] La barra mini desaparece al reabrir la ficha de la ciudad que esta
      sonando.
- [ ] El boton de "Reproducir" para navegadores que bloquean autoplay sigue
      funcionando igual que hoy.

## Decisions

- **Si:** el corte de musica ocurre solo al abrir OTRA ciudad, nunca al
  simplemente cerrar la ficha o deseleccionar. Elegido explicitamente por el
  usuario sobre la alternativa de cortar tambien al deseleccionar.
- **Si:** mostrar barra mini flotante como indicador. Elegido por el usuario
  sobre "sin indicador visible", para que no se olvide que algo sigue sonando.
- **No:** persistir la musica activa entre recargas de pagina. No se pidio y
  agrega complejidad (habria que sincronizar con localStorage y el ciclo de
  vida del `<audio>`/iframe de YouTube).

## What is **not** in this spec

- Persistencia entre recargas de pagina o pestañas.
- Doble reproductor independiente (mini + completo desincronizados).
- Cambios a como se agrega/edita/quita musica de una ciudad.
```

---

## Contenido: specs/04-zoom-area-urbana.md

```markdown
# SPEC 04 — Zoom del mapa segun area urbana

> **Status:** Draft
> **Depends on:** (ninguno)
> **Date:** 2026-09-02
> **Objective:** Al abrir la ficha de una ciudad, el mapa hace zoom ajustado al
> area urbana real de esa ciudad (bounding box de OpenStreetMap) en vez de usar
> siempre el mismo nivel de zoom fijo.

## Scope

**In:**

- Guardar el bounding box (sur, oeste, norte, este) de la ciudad al crearla,
  cuando se eligio desde la busqueda de OpenStreetMap/Nominatim
  (`album-viajes-web/src/features/geocoding/nominatim.ts`). Nominatim ya
  devuelve `boundingbox` en cada resultado; hoy se descarta.
- Backend: nuevo value object `BoundingBox` (analogo a
  `Coordinates` en `album-viajes-api/src/AlbumViajes.Domain/ValueObjects/`),
  propiedad opcional en `City` (`album-viajes-api/src/
  AlbumViajes.Domain/Entities/City.cs`), columna nullable via migracion EF Core,
  y los DTOs correspondientes (`SaveCityRequest`, `CitySummaryResponse`/
  `CityDetailResponse` en `CityContracts.cs`).
- Frontend: `PlaceSuggestion` (en `nominatim.ts`) incluye el bounding box; al
  crear una ciudad eligiendo una sugerencia, se envia al backend junto con
  `SaveCityRequest`.
- `CityMap.tsx`: al seleccionar una ciudad con bounding box guardado, el mapa
  hace `fitBounds` a esa area (con un margen/padding pequeño) en vez de
  `setView` a un zoom fijo.
- Si la ciudad NO tiene bounding box guardado (creada antes de este cambio, o
  ubicada con clic manual en el mapa en vez de busqueda), el mapa usa el zoom
  fijo actual (`mapConfig.zoom` de `config.js`) como hoy, sin llamadas
  adicionales a servicios externos.

**Out of scope (for future specs):**

- Backfill automatico de bounding box para ciudades ya creadas sin el (se
  descarta: fallback a zoom fijo es suficiente segun el usuario).
- Recalcular o mostrar el bounding box en el formulario de edicion de ciudad.
- Cambiar el bounding box al reubicar una ciudad manualmente (`Relocate`): si
  se reubica a mano, el bounding box guardado (si existia) queda obsoleto y se
  descarta (pasa a `null`), volviendo al zoom fijo para esa ciudad.

## Data model

```csharp
// album-viajes-api/src/AlbumViajes.Domain/ValueObjects/BoundingBox.cs
public readonly record struct BoundingBox
{
    public double South { get; }
    public double West { get; }
    public double North { get; }
    public double East { get; }

    public static Result<BoundingBox> Create(double south, double west, double north, double east);
}
```

```csharp
// City.cs — nueva propiedad opcional
public BoundingBox? UrbanAreaBounds { get; private set; }
```

```ts
// nominatim.ts — PlaceSuggestion extendido
export interface PlaceSuggestion {
  // ...campos actuales...
  boundingBox: { south: number; west: number; north: number; east: number } | null
}
```

```ts
// api/types.ts — CitySummary/CityDetail extendidos
boundingBox: { south: number; west: number; north: number; east: number } | null
```

## Implementation plan

1. Backend: crear `BoundingBox.cs` (value object, valida sur<norte y
   oeste<este, rangos de lat/lng) siguiendo el patron de `Coordinates.cs`.
2. Backend: agregar `UrbanAreaBounds` (nullable) a `City.cs`, con un metodo de
   intencion para fijarlo al crear (`Create`) y limpiarlo al `Relocate`.
   Actualizar `CityConfiguration.cs` (mapeo EF Core como propiedades sueltas o
   `OwnsOne`, igual que `Coordinates`) y generar la migracion.
3. Backend: agregar el bounding box opcional a `SaveCityRequest`,
   `CitySummaryResponse`/`CityDetailResponse` y sus mapeos en
   `CityMappings`; ajustar `SaveCityRequestValidator.cs` para validar el
   bounding box solo si viene informado.
4. Frontend: extender `PlaceSuggestion` en `nominatim.ts` para incluir
   `boundingBox` a partir del campo `boundingbox` de Nominatim.
5. Frontend: al enviar `SaveCityRequest` desde el flujo de creacion con
   sugerencia elegida (`useCityForm.ts`/`CityForm.tsx`), incluir el
   `boundingBox` de la sugerencia; si la ciudad se ubica con clic manual, se
   envia `null`.
6. Frontend: extender `CitySummary`/`CityDetail` en `api/types.ts` con
   `boundingBox`.
7. Frontend: en `CityMap.tsx`, al cambiar `selectedId` a una ciudad con
   `boundingBox` no nulo, usar la API de Leaflet (`map.fitBounds`) para
   encuadrar esa area; si es `null`, mantener el comportamiento actual
   (zoom fijo de `mapConfig`).

## Acceptance criteria

- [ ] Crear una ciudad eligiendo una sugerencia de Nominatim guarda su
      bounding box.
- [ ] Ubicar una ciudad con clic manual en el mapa no guarda bounding box
      (queda `null`).
- [ ] Al abrir la ficha de una ciudad con bounding box guardado, el mapa
      encuadra su area urbana (zoom distinto al fijo, ajustado al tamaño real
      de la ciudad).
- [ ] Al abrir la ficha de una ciudad sin bounding box, el mapa usa el zoom
      fijo de siempre.
- [ ] Reubicar una ciudad (`Relocate`) descarta su bounding box previo.
- [ ] Las ciudades existentes (creadas antes de este cambio) siguen
      funcionando sin error, con zoom fijo.

## Decisions

- **Si:** guardar el bounding box en el backend al crear la ciudad (requiere
  migracion EF Core), en vez de pedirlo en vivo a Nominatim cada vez o
  aproximarlo por poblacion. Elegido por el usuario por ser mas preciso y no
  depender de un servicio externo en cada apertura de ficha.
- **Si:** fallback a zoom fijo actual para ciudades sin bounding box (ya
  creadas, o ubicadas con clic manual). Elegido por el usuario sobre hacer
  backfill automatico en el primer acceso.
- **No:** backfill automatico de bounding box para ciudades viejas. Se
  descarta explicitamente para mantener el spec simple; podria ser un spec
  futuro si hace falta.
- **No:** recalcular bounding box al reubicar manualmente. Se limpia en vez de
  recalcularse, porque recalcular requeriria llamar a Nominatim de nuevo desde
  el backend.

## What is **not** in this spec

- Backfill de bounding box para ciudades ya existentes.
- Edicion manual del bounding box desde el formulario.
- Cualquier llamada a Nominatim en tiempo de apertura de ficha (todo el
  bounding box se resuelve al crear la ciudad).
```

---

## Verificacion tras crear los archivos

1. Confirmar que `specs/` contiene los 4 archivos con estado `Draft`.
2. Confirmar/crear `specs/.spec-config.yml` con `AutoCreateBranch: true` si no
   existia.
3. Informar al usuario la ruta de cada archivo y recordar que debe releerlos y
   pasarlos a `Approved` antes de correr `/spec-impl NN-slug` sobre cada uno.
4. No se ejecuta ningun `/spec-impl` ni se escribe codigo de produccion en este
   paso: eso queda para cuando el usuario apruebe cada spec por separado.
