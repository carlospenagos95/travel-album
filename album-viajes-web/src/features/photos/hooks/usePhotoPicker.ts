import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { photosApi } from '../../../api/client'
import type { PhotoImportResult, PhotoPickerSession } from '../../../api/types'
import { cityKeys } from '../../cities/hooks/useCities'

export type PickerStage = 'idle' | 'choosing' | 'importing' | 'done'

/**
 * Ciclo completo del selector de Google Photos:
 * abrir sesion -> el usuario elige en la pestania de Google -> preguntar cada
 * pocos segundos si ya termino -> copiar al servidor lo elegido.
 *
 * El ritmo de las preguntas lo marca Google en la propia sesion, no nosotros.
 */
export function usePhotoPicker(cityId: string) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<PhotoPickerSession | null>(null)
  const [result, setResult] = useState<PhotoImportResult | null>(null)

  // Evita que el sondeo dispare la importacion mas de una vez por sesion.
  const importedSession = useRef<string | null>(null)

  const start = useMutation({
    mutationFn: () => photosApi.startPickerSession(cityId),
    onSuccess: (started) => {
      setResult(null)
      setSession(started)
      window.open(started.pickerUri, '_blank', 'noopener,noreferrer')
    },
  })

  const importPicked = useMutation({
    mutationFn: (sessionId: string) => photosApi.importPicked(cityId, sessionId),
    onSuccess: async (imported) => {
      setResult(imported)
      setSession(null)
      await queryClient.invalidateQueries({ queryKey: cityKeys.detail(cityId) })
      await queryClient.invalidateQueries({ queryKey: cityKeys.all })
    },
    onError: () => setSession(null),
  })

  const status = useQuery({
    queryKey: ['photo-picker', cityId, session?.sessionId ?? ''],
    queryFn: () => photosApi.getPickerSession(cityId, session!.sessionId),
    enabled: session !== null,
    refetchInterval: (session?.pollIntervalSeconds ?? 5) * 1000,
    staleTime: 0,
  })

  const sessionId = session?.sessionId ?? null
  const photosPicked = status.data?.photosPicked === true
  const { mutate: importFromSession } = importPicked

  useEffect(() => {
    if (sessionId === null || !photosPicked || importedSession.current === sessionId) {
      return
    }

    importedSession.current = sessionId
    importFromSession(sessionId)
  }, [sessionId, photosPicked, importFromSession])

  const stage: PickerStage = importPicked.isPending
    ? 'importing'
    : session !== null
      ? 'choosing'
      : result !== null
        ? 'done'
        : 'idle'

  return {
    stage,
    pickerUri: session?.pickerUri ?? null,
    result,
    errorMessage: start.error ?? importPicked.error,
    isStarting: start.isPending,
    start: () => start.mutate(),
    cancel: () => setSession(null),
  }
}
