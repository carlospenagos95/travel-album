import { useEffect } from 'react'
import type { Photo } from '../../../api/types'

interface PhotoLightboxProps {
  photos: Photo[]
  index: number
  onMove: (index: number) => void
  onClose: () => void
}

/** Foto a pantalla completa, navegable con el teclado y con los botones. */
export function PhotoLightbox({ photos, index, onMove, onClose }: PhotoLightboxProps) {
  const photo = photos[index]

  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }

      if (event.key === 'ArrowRight') {
        onMove((index + 1) % photos.length)
      }

      if (event.key === 'ArrowLeft') {
        onMove((index - 1 + photos.length) % photos.length)
      }
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [index, photos.length, onMove, onClose])

  if (photo === undefined) {
    return null
  }

  return (
    <div className="lightbox" role="dialog" aria-modal="true" aria-label={photo.caption ?? photo.fileName}>
      <button type="button" className="lightbox-close" onClick={onClose} aria-label="Cerrar">
        x
      </button>

      <button
        type="button"
        className="lightbox-nav lightbox-prev"
        onClick={() => onMove((index - 1 + photos.length) % photos.length)}
        aria-label="Anterior"
      >
        {'<'}
      </button>

      <figure className="lightbox-figure">
        <img src={photo.fileUrl} alt={photo.caption ?? photo.fileName} />
        <figcaption>
          {photo.caption ?? photo.fileName}
          <span className="hint">
            {index + 1} de {photos.length}
          </span>
        </figcaption>
      </figure>

      <button
        type="button"
        className="lightbox-nav lightbox-next"
        onClick={() => onMove((index + 1) % photos.length)}
        aria-label="Siguiente"
      >
        {'>'}
      </button>
    </div>
  )
}
