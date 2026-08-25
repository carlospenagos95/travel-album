import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { citiesApi } from '../../../api/client'
import type { SaveCityRequest } from '../../../api/types'

/**
 * Estado del servidor centralizado en React Query: cache, reintentos y estados
 * de carga viven aqui, no repartidos en useState por los componentes.
 */

/** Claves compartidas: fotos y musica invalidan la misma ficha que edita la ciudad. */
export const cityKeys = {
  all: ['cities'] as const,
  detail: (id: string) => ['cities', id] as const,
}

export function useCities() {
  return useQuery({ queryKey: cityKeys.all, queryFn: citiesApi.list })
}

export function useCity(id: string | null) {
  return useQuery({
    queryKey: cityKeys.detail(id ?? ''),
    queryFn: () => citiesApi.get(id!),
    enabled: id !== null,
  })
}

export function useCreateCity() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (city: SaveCityRequest) => citiesApi.create(city),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: cityKeys.all }),
  })
}

export function useUpdateCity() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, city }: { id: string; city: SaveCityRequest }) => citiesApi.update(id, city),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: cityKeys.all })
      queryClient.setQueryData(cityKeys.detail(updated.id), updated)
    },
  })
}

/** Rellena la ficha con Wikidata y Wikipedia. La respuesta ya trae la ciudad actualizada. */
export function useEnrichCity() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => citiesApi.enrich(id),
    onSuccess: (enriched) => queryClient.setQueryData(cityKeys.detail(enriched.id), enriched),
  })
}

export function useDeleteCity() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => citiesApi.remove(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: cityKeys.all })
      queryClient.removeQueries({ queryKey: cityKeys.detail(id) })
    },
  })
}
