import { useEffect, type ReactNode } from 'react'

interface CityDetailModalProps {
  cityName: string
  onClose: () => void
  children: ReactNode
}

/**
 * La ficha de la ciudad, a mano izquierda y sobre la barra lateral, con el mapa
 * a la derecha para ver a la vez la ciudad y lo que se cuenta de ella.
 *
 * No lleva velo ni `aria-modal`: el mapa de al lado sigue vivo, y bloquearlo
 * seria justo lo contrario de lo que se busca aqui.
 */
export function CityDetailModal({ cityName, onClose, children }: CityDetailModalProps) {
  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      // Con una foto a pantalla completa, Escape es suya: cierra la foto y deja
      // la ficha abierta, que es lo que espera quien la estaba mirando.
      if (event.key === 'Escape' && document.querySelector('.lightbox') === null) {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [onClose])

  return (
    <div className="city-modal" role="dialog" aria-label={cityName}>
      {children}
    </div>
  )
}
