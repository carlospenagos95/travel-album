import { useDebouncedSearch } from '../../shared/useDebouncedSearch'
import { searchPlaces, type PlaceSuggestion } from './nominatim'

/**
 * Busca lugares mientras se escribe. El debounce del hook compartido es lo que
 * respeta el limite de una peticion por segundo que pide Nominatim.
 */
export function usePlaceSearch(query: string) {
  const { items, isSearching } = useDebouncedSearch<PlaceSuggestion>({
    query,
    minLength: 3,
    search: (current, signal) => searchPlaces(current, signal),
    errorMessage: 'No se pudo buscar lugares.',
  })

  return { suggestions: items, isSearching }
}
