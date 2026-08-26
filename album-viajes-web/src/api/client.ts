import type {
  CityDetail,
  CitySummary,
  CurrentUser,
  LibraryTrack,
  MusicSource,
  Photo,
  PhotoImportResult,
  PhotoPickerSession,
  RadioStation,
  SaveCityRequest,
} from './types'

/**
 * Unico punto del frontend que sabe hablar HTTP. Los componentes nunca llaman
 * a fetch directamente: piden datos a los hooks, y los hooks a este cliente.
 */

const BASE_URL = '/api'

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | null = null

  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    problem = null
  }

  // Los errores de validacion llegan como un diccionario campo -> mensajes.
  const fieldMessages = problem?.errors ? Object.values(problem.errors).flat() : []

  const message =
    fieldMessages.length > 0
      ? fieldMessages.join(' ')
      : (problem?.detail ?? problem?.title ?? `La peticion fallo con codigo ${response.status}.`)

  return new ApiError(message, response.status)
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    // La sesion viaja en una cookie propia; sin esto no se enviaria.
    credentials: 'same-origin',
    headers: init?.body ? { 'Content-Type': 'application/json', ...init?.headers } : init?.headers,
  })

  if (!response.ok) {
    throw await toApiError(response)
  }

  // 204, o 200 sin cuerpo como el de cerrar sesion: no hay nada que interpretar.
  const isJson = response.headers.get('content-type')?.includes('application/json') === true

  if (response.status === 204 || !isJson) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const citiesApi = {
  list: () => request<CitySummary[]>('/cities'),

  get: (id: string) => request<CityDetail>(`/cities/${id}`),

  create: (city: SaveCityRequest) =>
    request<CityDetail>('/cities', { method: 'POST', body: JSON.stringify(city) }),

  update: (id: string, city: SaveCityRequest) =>
    request<CityDetail>(`/cities/${id}`, { method: 'PUT', body: JSON.stringify(city) }),

  remove: (id: string) => request<void>(`/cities/${id}`, { method: 'DELETE' }),

  enrich: (id: string) => request<CityDetail>(`/cities/${id}/enrich`, { method: 'POST' }),
}

export const photosApi = {
  startPickerSession: (cityId: string) =>
    request<PhotoPickerSession>(`/cities/${cityId}/photos/picker-session`, { method: 'POST' }),

  getPickerSession: (cityId: string, sessionId: string) =>
    request<PhotoPickerSession>(`/cities/${cityId}/photos/picker-session/${sessionId}`),

  importPicked: (cityId: string, sessionId: string) =>
    request<PhotoImportResult>(`/cities/${cityId}/photos/import?sessionId=${encodeURIComponent(sessionId)}`, {
      method: 'POST',
    }),

  reorder: (cityId: string, photoIds: string[]) =>
    request<Photo[]>(`/cities/${cityId}/photos/order`, {
      method: 'PUT',
      body: JSON.stringify({ photoIds }),
    }),

  caption: (cityId: string, photoId: string, caption: string | null) =>
    request<Photo>(`/cities/${cityId}/photos/${photoId}/caption`, {
      method: 'PUT',
      body: JSON.stringify({ caption }),
    }),

  remove: (cityId: string, photoId: string) =>
    request<void>(`/cities/${cityId}/photos/${photoId}`, { method: 'DELETE' }),
}

export const musicApi = {
  searchStations: (query: string, countryCode: string | null, signal?: AbortSignal) =>
    request<RadioStation[]>(
      `/stations/search?query=${encodeURIComponent(query)}` +
        (countryCode === null ? '' : `&countryCode=${encodeURIComponent(countryCode)}`),
      { signal },
    ),

  addRadio: (cityId: string, station: RadioStation) =>
    request<MusicSource>(`/cities/${cityId}/music/radio`, {
      method: 'POST',
      body: JSON.stringify({
        stationUuid: station.uuid,
        stationName: station.name,
        streamUrl: station.streamUrl,
      }),
    }),

  searchTracks: (query: string, signal?: AbortSignal) =>
    request<LibraryTrack[]>(`/tracks/search?query=${encodeURIComponent(query)}`, { signal }),

  addTrack: (cityId: string, track: LibraryTrack) =>
    request<MusicSource>(`/cities/${cityId}/music/track`, {
      method: 'POST',
      body: JSON.stringify({
        trackId: track.id,
        title: track.name,
        artist: track.artistName,
        audioUrl: track.audioUrl,
      }),
    }),

  setDefault: (cityId: string, musicSourceId: string) =>
    request<MusicSource[]>(`/cities/${cityId}/music/${musicSourceId}/default`, { method: 'PUT' }),

  remove: (cityId: string, musicSourceId: string) =>
    request<void>(`/cities/${cityId}/music/${musicSourceId}`, { method: 'DELETE' }),
}

export const authApi = {
  me: () => request<CurrentUser>('/auth/me'),

  /**
   * La entrada con Google no se puede hacer con fetch: es una redireccion del
   * navegador, que vuelve al album con la cookie de sesion puesta.
   */
  loginUrl: () => `${BASE_URL}/auth/login`,

  logout: () => request<void>('/auth/logout', { method: 'POST' }),
}
