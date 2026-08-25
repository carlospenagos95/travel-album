import { useEffect, useState } from 'react'
import { searchPlaces, type PlaceSuggestion } from './nominatim'

const DEBOUNCE_MS = 500
const MIN_QUERY_LENGTH = 3

interface SearchResult {
  query: string
  places: PlaceSuggestion[]
}

const NO_RESULT: SearchResult = { query: '', places: [] }

/**
 * Busca lugares mientras se escribe, con debounce para respetar el limite de una
 * peticion por segundo de Nominatim y cancelando la anterior.
 *
 * El resultado guarda la consulta a la que pertenece, de modo que las
 * sugerencias mostradas se derivan en el render en vez de sincronizarse con
 * llamadas a setState dentro del efecto.
 */
export function usePlaceSearch(query: string) {
  const [result, setResult] = useState<SearchResult>(NO_RESULT)
  const trimmed = query.trim()
  const isLongEnough = trimmed.length >= MIN_QUERY_LENGTH

  useEffect(() => {
    const current = query.trim()

    if (current.length < MIN_QUERY_LENGTH) {
      return
    }

    const controller = new AbortController()

    const timer = setTimeout(() => {
      searchPlaces(current, controller.signal)
        .then((places) => setResult({ query: current, places }))
        .catch(() => setResult({ query: current, places: [] }))
    }, DEBOUNCE_MS)

    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [query])

  const isCurrent = result.query === trimmed

  return {
    suggestions: isLongEnough && isCurrent ? result.places : [],
    isSearching: isLongEnough && !isCurrent,
  }
}
