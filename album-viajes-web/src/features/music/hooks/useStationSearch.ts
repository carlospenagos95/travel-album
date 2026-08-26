import { musicApi } from '../../../api/client'
import type { RadioStation } from '../../../api/types'
import { useDebouncedSearch } from '../../../shared/useDebouncedSearch'

/** Busca emisoras mientras se escribe, acotadas al pais de la ciudad. */
export function useStationSearch(query: string, countryCode: string | null) {
  const { items, errorMessage, isSearching } = useDebouncedSearch<RadioStation>({
    query,
    minLength: 2,
    extraKey: countryCode ?? '',
    search: (current, signal) => musicApi.searchStations(current, countryCode, signal),
    errorMessage: 'No se pudo buscar emisoras.',
  })

  return { stations: items, errorMessage, isSearching }
}
