import { musicApi } from '../../../api/client'
import type { LibraryTrack } from '../../../api/types'
import { useDebouncedSearch } from '../../../shared/useDebouncedSearch'

/**
 * Busca canciones mientras se escribe. Si el catalogo no responde, su
 * explicacion llega en `errorMessage` y la radio sigue funcionando igual.
 */
export function useTrackSearch(query: string) {
  const { items, errorMessage, isSearching } = useDebouncedSearch<LibraryTrack>({
    query,
    minLength: 2,
    search: (current, signal) => musicApi.searchTracks(current, signal),
    errorMessage: 'No se pudo buscar canciones.',
  })

  return { tracks: items, errorMessage, isSearching }
}
