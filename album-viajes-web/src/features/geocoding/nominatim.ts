/**
 * Busqueda de ciudades contra Nominatim (OpenStreetMap) para no teclear
 * coordenadas a mano. Su politica de uso pide identificarse y no bombardear el
 * servicio: de ahi el debounce en el hook que lo consume.
 */

const NOMINATIM_URL = 'https://nominatim.openstreetmap.org/search'

interface NominatimPlace {
  place_id: number
  lat: string
  lon: string
  display_name: string
  name?: string
  address?: {
    city?: string
    town?: string
    village?: string
    municipality?: string
    state?: string
    country?: string
    country_code?: string
  }
}

export interface PlaceSuggestion {
  id: number
  name: string
  country: string
  countryCode: string
  latitude: number
  longitude: number
  displayName: string
}

function pickCityName(place: NominatimPlace): string {
  const address = place.address
  return (
    address?.city ??
    address?.town ??
    address?.village ??
    address?.municipality ??
    place.name ??
    place.display_name.split(',')[0] ??
    ''
  )
}

export async function searchPlaces(query: string, signal: AbortSignal): Promise<PlaceSuggestion[]> {
  const params = new URLSearchParams({
    q: query,
    format: 'jsonv2',
    addressdetails: '1',
    limit: '6',
  })

  const response = await fetch(`${NOMINATIM_URL}?${params.toString()}`, { signal })

  if (!response.ok) {
    throw new Error('No se pudo buscar la ciudad en OpenStreetMap.')
  }

  const places = (await response.json()) as NominatimPlace[]

  return places
    .map((place) => ({
      id: place.place_id,
      name: pickCityName(place),
      country: place.address?.country ?? '',
      countryCode: (place.address?.country_code ?? '').toUpperCase(),
      latitude: Number(place.lat),
      longitude: Number(place.lon),
      displayName: place.display_name,
    }))
    .filter((place) => place.name !== '' && place.countryCode.length === 2)
}
