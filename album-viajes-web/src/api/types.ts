/** Contratos que expone la API. Reflejan los DTO del backend, no sus entidades. */

export interface CitySummary {
  id: string
  name: string
  country: string
  countryCode: string
  latitude: number
  longitude: number
  visitedOn: string | null
  coverThumbnailUrl: string | null
  photoCount: number
  hasMusic: boolean
}

export interface CityDetail extends CitySummary {
  notes: string | null
  population: number | null
  populationSource: string | null
  shortDescription: string | null
  typicalFood: string | null
  wikidataId: string | null
  wikipediaUrl: string | null
  enrichedAt: string | null
  createdAt: string
  updatedAt: string
  photos: Photo[]
  musicSources: MusicSource[]
}

export interface SaveCityRequest {
  name: string
  country: string
  countryCode: string
  latitude: number
  longitude: number
  visitedOn: string | null
  notes: string | null
  shortDescription: string | null
  typicalFood: string | null
}

export interface Photo {
  id: string
  fileName: string
  caption: string | null
  width: number
  height: number
  takenAt: string | null
  sortOrder: number
  /** Siempre del propio servidor: el enlace de Google Photos caduca en una hora. */
  fileUrl: string
  thumbnailUrl: string
}

export interface PhotoPickerSession {
  sessionId: string
  pickerUri: string
  photosPicked: boolean
  pollIntervalSeconds: number
}

export interface PhotoImportResult {
  imported: number
  skipped: number
  photos: Photo[]
}

export type MusicKind = 'RadioStation' | 'Track'

export interface MusicSource {
  id: string
  kind: MusicKind
  label: string
  isDefault: boolean
  /** Siempre el proxy propio, tanto para la radio como para una cancion. */
  streamUrl: string
  artist: string | null
}

/**
 * Cancion del catalogo de iTunes, antes de asociarla a una ciudad. `audioUrl` es
 * una muestra de treinta segundos; `durationSeconds`, lo que dura la cancion
 * entera.
 */
export interface LibraryTrack {
  id: string
  name: string
  artistName: string | null
  audioUrl: string
  durationSeconds: number
  imageUrl: string | null
}

export interface RadioStation {
  uuid: string
  name: string
  streamUrl: string
  country: string | null
  countryCode: string | null
  tags: string | null
  votes: number
  codec: string | null
  bitrate: number
}

export interface CurrentUser {
  isOwner: boolean
  email: string | null
}
