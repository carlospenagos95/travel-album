import { useMutation, useQueryClient } from '@tanstack/react-query'
import { musicApi } from '../../../api/client'
import type { RadioStation } from '../../../api/types'
import { cityKeys } from '../../cities/hooks/useCities'

/**
 * Mutaciones de la musica de una ciudad. Como las fotos, viven dentro de la
 * ficha: no hay cache aparte, solo se invalida la de la ciudad.
 */
function useMusicMutation<TVariables, TResult>(
  cityId: string,
  mutationFn: (variables: TVariables) => Promise<TResult>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: cityKeys.detail(cityId) })
      // La lista del mapa marca que ciudades suenan.
      await queryClient.invalidateQueries({ queryKey: cityKeys.all })
    },
  })
}

export function useAddRadioStation(cityId: string) {
  return useMusicMutation(cityId, (station: RadioStation) => musicApi.addRadio(cityId, station))
}

export function useAddYouTubeVideo(cityId: string) {
  return useMusicMutation(cityId, ({ urlOrId, title }: { urlOrId: string; title: string | null }) =>
    musicApi.addYouTube(cityId, urlOrId, title),
  )
}

export function useSetDefaultMusicSource(cityId: string) {
  return useMusicMutation(cityId, (musicSourceId: string) => musicApi.setDefault(cityId, musicSourceId))
}

export function useRemoveMusicSource(cityId: string) {
  return useMusicMutation(cityId, (musicSourceId: string) => musicApi.remove(cityId, musicSourceId))
}
