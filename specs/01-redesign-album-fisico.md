# SPEC 01 — Rediseno visual: album fisico

> **Status:** Implemented
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

- [X] La sidebar y la ficha de ciudad muestran la nueva paleta/tipografia/textura.
- [X] Las miniaturas de `PhotoGallery` tienen marco tipo polaroid.
- [X] Abrir una ficha dispara una transicion visible (no aparece instantaneo).
- [X] Navegar entre fotos en el lightbox dispara una transicion visible.
- [X] Todas las acciones que existian antes (agregar/editar/eliminar ciudad,
      reordenar/poner pie/quitar foto, importar fotos, agregar/quitar musica,
      traer datos de Wikipedia, cerrar sesion) siguen funcionando igual.
- [X] No aparecen errores en consola del navegador al navegar por el album.

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
