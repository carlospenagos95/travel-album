import { useState } from 'react'
import type { Photo } from '../../../api/types'
import { useCaptionPhoto, useDeletePhoto, useReorderPhotos } from '../hooks/usePhotos'
import { PhotoLightbox } from './PhotoLightbox'

interface PhotoGalleryProps {
  cityId: string
  photos: Photo[]
  canEdit: boolean
}

/** Mueve una foto una posicion y devuelve el nuevo orden completo. */
function moved(photos: Photo[], from: number, to: number): string[] {
  const ids = photos.map((photo) => photo.id)
  const picked = ids.splice(from, 1)[0]

  if (picked === undefined) {
    return ids
  }

  ids.splice(to, 0, picked)

  return ids
}

export function PhotoGallery({ cityId, photos, canEdit }: PhotoGalleryProps) {
  const [openedIndex, setOpenedIndex] = useState<number | null>(null)
  const [editingCaptionOf, setEditingCaptionOf] = useState<string | null>(null)
  const [caption, setCaption] = useState('')

  const reorder = useReorderPhotos(cityId)
  const remove = useDeletePhoto(cityId)
  const describe = useCaptionPhoto(cityId)

  if (photos.length === 0) {
    return <p className="empty">Todavia no hay fotos de esta ciudad.</p>
  }

  const startCaptioning = (photo: Photo) => {
    setEditingCaptionOf(photo.id)
    setCaption(photo.caption ?? '')
  }

  const saveCaption = (photoId: string) => {
    describe.mutate(
      { photoId, caption: caption.trim() === '' ? null : caption.trim() },
      { onSuccess: () => setEditingCaptionOf(null) },
    )
  }

  return (
    <>
      <ul className="gallery">
        {photos.map((photo, index) => (
          <li key={photo.id}>
            <button
              type="button"
              className="thumb"
              onClick={() => setOpenedIndex(index)}
              aria-label={`Ver ${photo.caption ?? photo.fileName}`}
            >
              <img src={photo.thumbnailUrl} alt={photo.caption ?? photo.fileName} loading="lazy" />
              {index === 0 && <span className="badge">Portada</span>}
            </button>

            {canEdit && (
              <div className="thumb-actions">
                <button
                  type="button"
                  className="icon"
                  disabled={index === 0 || reorder.isPending}
                  onClick={() => reorder.mutate(moved(photos, index, index - 1))}
                  aria-label="Mover antes"
                >
                  {'<'}
                </button>
                <button
                  type="button"
                  className="icon"
                  disabled={index === photos.length - 1 || reorder.isPending}
                  onClick={() => reorder.mutate(moved(photos, index, index + 1))}
                  aria-label="Mover despues"
                >
                  {'>'}
                </button>
                <button type="button" className="icon" onClick={() => startCaptioning(photo)} aria-label="Poner pie">
                  Aa
                </button>
                <button
                  type="button"
                  className="icon danger"
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(photo.id)}
                  aria-label="Quitar foto"
                >
                  x
                </button>
              </div>
            )}

            {canEdit && editingCaptionOf === photo.id && (
              <div className="caption-editor">
                <input
                  value={caption}
                  placeholder="Pie de foto"
                  onChange={(event) => setCaption(event.target.value)}
                />
                <button type="button" disabled={describe.isPending} onClick={() => saveCaption(photo.id)}>
                  Guardar
                </button>
                <button type="button" className="secondary" onClick={() => setEditingCaptionOf(null)}>
                  Cancelar
                </button>
              </div>
            )}
          </li>
        ))}
      </ul>

      {openedIndex !== null && (
        <PhotoLightbox
          photos={photos}
          index={openedIndex}
          onMove={setOpenedIndex}
          onClose={() => setOpenedIndex(null)}
        />
      )}
    </>
  )
}
