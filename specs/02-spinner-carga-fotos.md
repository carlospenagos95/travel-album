# SPEC 02 — Spinner de carga en fotos

> **Status:** Approved
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

- [X] Al abrir el album con conexion lenta (throttling en DevTools), las
      miniaturas muestran spinner hasta que cada imagen termina de cargar.
- [X] Al abrir el lightbox, la foto grande muestra spinner hasta que carga.
- [X] Al navegar con flechas/teclado a una foto no cargada antes, el spinner
      vuelve a aparecer para esa foto.
- [X] Si una imagen falla al cargar (`onError`), el spinner desaparece (no gira
      indefinidamente).
- [X] Ninguna funcionalidad existente de la galeria (reordenar, poner pie,
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
