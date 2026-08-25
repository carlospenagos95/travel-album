# Album de Viajes — Web

Frontend en React 19 + TypeScript + Vite. Mapa de ciudades visitadas con Leaflet
y OpenStreetMap.

## Estructura

```
src/api/                Cliente HTTP tipado y contratos de la API.
src/config/             Configuracion de ejecucion leida de public/config.js.
src/features/auth/      Sesion del administrador y visibilidad de los controles.
src/features/cities/    Componentes y hooks del mapa, la ficha y el formulario.
src/features/geocoding/ Busqueda de ciudades contra Nominatim.
src/features/photos/    Galeria, visor a pantalla completa e importacion.
src/features/music/     Reproductor y eleccion de emisora o video.
src/app/                Ensamblado de la pantalla.
```

Organizado por feature, no por tipo de archivo. Los componentes no llaman a
`fetch`: piden datos a los hooks de TanStack Query, que hablan con `api/client.ts`.

## Desarrollo

```bash
npm install
npm run dev      # http://localhost:5173
```

Vite proxea `/api` a `http://localhost:5120`, de modo que en desarrollo no hay
CORS que configurar. La API debe estar corriendo.

```bash
npm run lint
npm run build    # tsc en modo estricto + empaquetado
```

## Configuracion del mapa

El centro y el zoom iniciales salen de `public/config.js`, que `index.html`
carga antes del bundle. Es configuracion de ejecucion, no de compilacion: la
misma imagen sirve para cualquier despliegue y en produccion basta con montar
otro `config.js` sobre el del contenedor.

```js
window.__ALBUM_VIAJES__ = {
  map: { centerLatitude: 4.711, centerLongitude: -74.0721, zoom: 6 },
}
```

Un valor ausente o fuera de rango cae al de por defecto (Bogota, zoom 6), de
modo que un `config.js` a medias no deja la pagina en blanco.

## Que ve cada quien

`GET /api/auth/me` dice si quien mira puede editar. La respuesta solo decide que
controles se pintan: la autorizacion de verdad la aplica el servidor en cada
peticion, asi que ocultar un boton no es la medida de seguridad, solo evita
ofrecer algo que va a fallar.

Entrar con Google es una redireccion del navegador, no una llamada con `fetch`:
por eso es un enlace a `/api/auth/login`. Si el correo no esta autorizado, el
servidor devuelve al album con el motivo en la direccion y se muestra tal cual.

## Fotos y musica

- La eleccion de fotos ocurre en una pestania de Google. El album abre la sesion,
  pregunta cada pocos segundos si ya termino —al ritmo que marca la propia
  sesion, no uno inventado— y en cuanto termina las copia al servidor.
- Las fotos se sirven siempre desde la API, nunca desde Google: el enlace que
  devuelve el selector caduca en una hora.
- Los navegadores rechazan reproducir sonido sin interaccion previa del usuario.
  Ese rechazo se recoge y se convierte en un boton de play, en lugar de dejar el
  reproductor mudo sin explicacion.
- El reproductor se remonta con `key` al cambiar de fuente, para cerrar el stream
  anterior en vez de dejar dos peticiones de audio abiertas.

## Notas

- `strict` y `noUncheckedIndexedAccess` activos; `any` prohibido.
- Nominatim limita a una peticion por segundo: la busqueda va con debounce de
  500 ms y cancela la anterior.
- En produccion, nginx sirve los estaticos y proxea `/api` a la API, asi que todo
  vive en el mismo origen.
- `config.js` se sirve con `Cache-Control: no-store`: si se cachea, cambiar el
  centro del mapa no tendria efecto hasta que el navegador lo olvidara.
