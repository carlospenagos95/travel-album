/**
 * Configuracion de la aplicacion. Llega en tiempo de ejecucion desde
 * `public/config.js`, que el navegador carga antes del bundle: el mismo build
 * sirve para cualquier despliegue y el centro del mapa se cambia editando ese
 * archivo.
 *
 * Todo valor que falte o venga mal escrito cae al valor por defecto, para que un
 * config.js a medias no deje la pagina en blanco.
 */

export interface MapConfig {
  /** Centro del mapa al abrir la pagina, en [latitud, longitud]. */
  center: [number, number]
  zoom: number
  /** Acercamiento al abrir una ciudad: lo bastante cerca para ver el casco urbano. */
  cityZoom: number
}

interface RuntimeConfig {
  map?: {
    centerLatitude?: unknown
    centerLongitude?: unknown
    zoom?: unknown
    cityZoom?: unknown
  }
}

declare global {
  interface Window {
    __ALBUM_VIAJES__?: RuntimeConfig
  }
}

// Bogota: el album es colombiano, asi que el mapa abre sobre Colombia.
const DEFAULT_MAP: MapConfig = {
  center: [4.711, -74.0721],
  zoom: 6,
  cityZoom: 12,
}

const LATITUDE_RANGE = 90
const LONGITUDE_RANGE = 180
const MIN_ZOOM = 1
const MAX_ZOOM = 19

function numberWithin(value: unknown, min: number, max: number): number | null {
  return typeof value === 'number' && Number.isFinite(value) && value >= min && value <= max ? value : null
}

function readMapConfig(): MapConfig {
  const map = window.__ALBUM_VIAJES__?.map

  const latitude = numberWithin(map?.centerLatitude, -LATITUDE_RANGE, LATITUDE_RANGE)
  const longitude = numberWithin(map?.centerLongitude, -LONGITUDE_RANGE, LONGITUDE_RANGE)
  const zoom = numberWithin(map?.zoom, MIN_ZOOM, MAX_ZOOM)
  const cityZoom = numberWithin(map?.cityZoom, MIN_ZOOM, MAX_ZOOM)

  return {
    // Latitud y longitud van juntas: media coordenada valida no es un centro.
    center: latitude !== null && longitude !== null ? [latitude, longitude] : DEFAULT_MAP.center,
    zoom: zoom ?? DEFAULT_MAP.zoom,
    cityZoom: cityZoom ?? DEFAULT_MAP.cityZoom,
  }
}

export const mapConfig: MapConfig = readMapConfig()
