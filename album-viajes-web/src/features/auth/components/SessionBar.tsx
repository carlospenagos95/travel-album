import { authApi } from '../../../api/client'
import type { CurrentUser } from '../../../api/types'

interface SessionBarProps {
  user: CurrentUser
  isLoggingOut: boolean
  onLogout: () => void
}

/**
 * Estado de la sesion. Entrar es una redireccion del navegador a Google, no una
 * llamada de la aplicacion, asi que aqui es un enlace y no un boton con onClick.
 */
export function SessionBar({ user, isLoggingOut, onLogout }: SessionBarProps) {
  if (!user.isOwner) {
    return (
      <p className="session">
        <a href={authApi.loginUrl()}>Entrar como administrador</a>
      </p>
    )
  }

  return (
    <p className="session">
      <span className="hint">{user.email}</span>
      <button type="button" className="link" onClick={onLogout} disabled={isLoggingOut}>
        {isLoggingOut ? 'Saliendo...' : 'Salir'}
      </button>
    </p>
  )
}
