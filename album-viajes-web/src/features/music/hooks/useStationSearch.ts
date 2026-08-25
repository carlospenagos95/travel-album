import { useEffect, useState } from 'react'
import { musicApi } from '../../../api/client'
import type { RadioStation } from '../../../api/types'

const DEBOUNCE_MS = 500
const MIN_QUERY_LENGTH = 2

interface SearchResult {
  key: string
  stations: RadioStation[]
  errorMessage: string | null
}

const NO_RESULT: SearchResult = { key: '', stations: [], errorMessage: null }

/**
 * Busca emisoras mientras se escribe. Sigue el mismo patron que la busqueda de
 * lugares: el resultado recuerda a que consulta pertenece y lo mostrado se
 * deriva en el render, en vez de sincronizarse con setState dentro del efecto.
 */
export function useStationSearch(query: string, countryCode: string | null) {
  const [result, setResult] = useState<SearchResult>(NO_RESULT)

  const trimmed = query.trim()
  const key = `${trimmed}|${countryCode ?? ''}`
  const isLongEnough = trimmed.length >= MIN_QUERY_LENGTH

  useEffect(() => {
    const current = query.trim()
    const currentKey = `${current}|${countryCode ?? ''}`

    if (current.length < MIN_QUERY_LENGTH) {
      return
    }

    const timer = setTimeout(() => {
      musicApi
        .searchStations(current, countryCode)
        .then((stations) => setResult({ key: currentKey, stations, errorMessage: null }))
        .catch((error: unknown) =>
          setResult({
            key: currentKey,
            stations: [],
            errorMessage: error instanceof Error ? error.message : 'No se pudo buscar emisoras.',
          }),
        )
    }, DEBOUNCE_MS)

    return () => clearTimeout(timer)
  }, [query, countryCode])

  const isCurrent = result.key === key

  return {
    stations: isLongEnough && isCurrent ? result.stations : [],
    errorMessage: isLongEnough && isCurrent ? result.errorMessage : null,
    isSearching: isLongEnough && !isCurrent,
  }
}
