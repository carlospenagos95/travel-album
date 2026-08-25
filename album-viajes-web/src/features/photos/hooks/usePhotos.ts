import { useMutation, useQueryClient } from '@tanstack/react-query'
import { photosApi } from '../../../api/client'
import { cityKeys } from '../../cities/hooks/useCities'

/**
 * Mutaciones de la galeria. Todas invalidan la ficha de la ciudad, que es donde
 * viven las fotos: no hay una cache de fotos aparte que pueda desincronizarse.
 */
function useGalleryMutation<TVariables, TResult>(
  cityId: string,
  mutationFn: (variables: TVariables) => Promise<TResult>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: cityKeys.detail(cityId) })
      // La lista del mapa muestra portada y numero de fotos.
      await queryClient.invalidateQueries({ queryKey: cityKeys.all })
    },
  })
}

export function useDeletePhoto(cityId: string) {
  return useGalleryMutation(cityId, (photoId: string) => photosApi.remove(cityId, photoId))
}

export function useReorderPhotos(cityId: string) {
  return useGalleryMutation(cityId, (photoIds: string[]) => photosApi.reorder(cityId, photoIds))
}

export function useCaptionPhoto(cityId: string) {
  return useGalleryMutation(cityId, ({ photoId, caption }: { photoId: string; caption: string | null }) =>
    photosApi.caption(cityId, photoId, caption),
  )
}
