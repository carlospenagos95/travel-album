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
