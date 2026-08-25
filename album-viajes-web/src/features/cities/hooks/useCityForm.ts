import { useCallback, useState } from 'react'
import type { CityDetail, SaveCityRequest } from '../../../api/types'
import type { PlaceSuggestion } from '../../geocoding/nominatim'

/** Todos los campos como texto: es lo que maneja un formulario HTML. */
export interface CityFormValues {
  name: string
  country: string
  countryCode: string
  latitude: string
  longitude: string
  visitedOn: string
  notes: string
  shortDescription: string
  typicalFood: string
}

const EMPTY_FORM: CityFormValues = {
  name: '',
  country: '',
  countryCode: '',
  latitude: '',
  longitude: '',
  visitedOn: '',
  notes: '',
  shortDescription: '',
  typicalFood: '',
}

function toFormValues(city: CityDetail): CityFormValues {
  return {
    name: city.name,
    country: city.country,
    countryCode: city.countryCode,
    latitude: String(city.latitude),
    longitude: String(city.longitude),
    visitedOn: city.visitedOn ?? '',
    notes: city.notes ?? '',
    shortDescription: city.shortDescription ?? '',
    typicalFood: city.typicalFood ?? '',
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

/**
 * Estado del formulario de ciudad. Vive en un hook para que el componente solo
 * se ocupe de pintar campos.
 *
 * El estado inicial se calcula una sola vez: para editar otra ciudad, quien use
 * el formulario le cambia la `key` y React lo remonta con los valores nuevos.
 * Asi se evita sincronizar props y estado dentro de un efecto.
 */
export function useCityForm(editing: CityDetail | null) {
  const [values, setValues] = useState<CityFormValues>(() =>
    editing === null ? EMPTY_FORM : toFormValues(editing),
  )

  const setField = <K extends keyof CityFormValues>(field: K, value: CityFormValues[K]) =>
    setValues((current) => ({ ...current, [field]: value }))

  const applySuggestion = (place: PlaceSuggestion) =>
    setValues((current) => ({
      ...current,
      name: place.name,
      country: place.country,
      countryCode: place.countryCode,
      latitude: String(place.latitude),
      longitude: String(place.longitude),
    }))

  // Estable entre renders: el formulario lo usa dentro de un efecto.
  const setCoordinates = useCallback(
    (latitude: number, longitude: number) =>
      setValues((current) => ({
        ...current,
        latitude: latitude.toFixed(6),
        longitude: longitude.toFixed(6),
      })),
    [],
  )

  const reset = () => setValues(EMPTY_FORM)

  const toRequest = (): SaveCityRequest => ({
    name: values.name.trim(),
    country: values.country.trim(),
    countryCode: values.countryCode.trim().toUpperCase(),
    latitude: Number(values.latitude),
    longitude: Number(values.longitude),
    visitedOn: emptyToNull(values.visitedOn),
    notes: emptyToNull(values.notes),
    shortDescription: emptyToNull(values.shortDescription),
    typicalFood: emptyToNull(values.typicalFood),
  })

  const hasCoordinates = values.latitude.trim() !== '' && values.longitude.trim() !== ''
  const isComplete =
    values.name.trim() !== '' && values.country.trim() !== '' && values.countryCode.trim().length === 2 && hasCoordinates

  return { values, setField, applySuggestion, setCoordinates, reset, toRequest, isComplete }
}
