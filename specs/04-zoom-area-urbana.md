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
