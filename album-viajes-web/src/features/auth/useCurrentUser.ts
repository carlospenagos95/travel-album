import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { authApi } from '../../api/client'
import type { CurrentUser } from '../../api/types'

const ANONYMOUS: CurrentUser = { isOwner: false, email: null }

const currentUserKey = ['auth', 'me'] as const

/**
 * Quien esta mirando el album. Solo decide que se muestra: la autorizacion de
 * verdad la aplica el servidor en cada peticion.
 */
export function useCurrentUser() {
  const query = useQuery({
    queryKey: currentUserKey,
    queryFn: authApi.me,
    // La sesion dura dias; no tiene sentido revalidarla continuamente.
    staleTime: 5 * 60_000,
  })

  return query.data ?? ANONYMOUS
}

export function useLogout() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: authApi.logout,
    // Al salir cambia lo que el album deja hacer, no solo quien eres.
    onSuccess: () => queryClient.invalidateQueries(),
  })
}
