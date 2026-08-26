import { useEffect, type ReactNode } from 'react'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
}

/**
 * Dialogo centrado sobre el mapa.
 *
 * El fondo no captura clics a proposito: el formulario de ciudad invita a fijar
 * las coordenadas haciendo clic en el mapa, y un velo que se los tragara dejaria
 * esa via muerta. El velo solo atenua lo que hay detras.
 */
export function Modal({ title, onClose, children }: ModalProps) {
  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [onClose])

  return (
    <div className="modal-overlay">
      <section className="modal" role="dialog" aria-modal="true" aria-label={title}>
        <header className="panel-header">
          <h2>{title}</h2>
          <button type="button" className="icon" onClick={onClose} aria-label="Cerrar">
            x
          </button>
        </header>

        {children}
      </section>
    </div>
  )
}
