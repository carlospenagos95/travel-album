import { useState } from 'react'
import type { CityDetail } from '../../../api/types'
import { MusicPanel } from '../../music/components/MusicPanel'
import { PhotoGallery } from '../../photos/components/PhotoGallery'
import { PhotoImporter } from '../../photos/components/PhotoImporter'

interface CityDetailPanelProps {
  city: CityDetail
  canEdit: boolean
  isDeleting: boolean
  isEnriching: boolean
  enrichErrorMessage: string | null
  errorMessageOf: (error: unknown) => string
  onEdit: () => void
  onDelete: () => void
  onEnrich: () => void
  onClose: () => void
}

function formatDate(value: string | null): string {
  if (value === null) {
    return 'Sin registrar'
  }

  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}

function formatPopulation(value: number | null): string {
  return value === null ? 'Sin dato' : value.toLocaleString('es-CO')
}

function formatEnrichedAt(value: string | null): string {
  if (value === null) {
    return 'Todavia sin datos externos'
  }

  return `Datos externos del ${new Date(value).toLocaleDateString('es-CO')}`
}

export function CityDetailPanel({
  city,
  canEdit,
  isDeleting,
  isEnriching,
  enrichErrorMessage,
  errorMessageOf,
  onEdit,
  onDelete,
  onEnrich,
  onClose,
}: CityDetailPanelProps) {
  // La confirmacion vive dentro del panel en vez de en un window.confirm: no
  // bloquea el hilo del navegador y se puede estilar como el resto de la UI.
  const [isConfirmingDelete, setIsConfirmingDelete] = useState(false)

  return (
    <article className="panel">
      <header className="panel-header">
        <div>
          <h2>{city.name}</h2>
          <p className="subtitle">
            {city.country} ({city.countryCode})
          </p>
        </div>
        <button type="button" className="icon" onClick={onClose} aria-label="Cerrar">
          x
        </button>
      </header>

      <dl className="facts">
        <div>
          <dt>Visitada</dt>
          <dd>{formatDate(city.visitedOn)}</dd>
        </div>
        <div>
          <dt>Habitantes</dt>
          <dd>
            {formatPopulation(city.population)}
            {city.populationSource !== null && <small className="hint">{city.populationSource}</small>}
          </dd>
        </div>
        <div>
          <dt>Coordenadas</dt>
          <dd>
            {city.latitude.toFixed(4)}, {city.longitude.toFixed(4)}
          </dd>
        </div>
      </dl>

      {city.shortDescription !== null && (
        <section>
          <h3>Descripcion</h3>
          <p>{city.shortDescription}</p>
        </section>
      )}

      {city.typicalFood !== null && (
        <section>
          <h3>Comida tipica</h3>
          <p>{city.typicalFood}</p>
        </section>
      )}

      {city.notes !== null && (
        <section>
          <h3>Notas del viaje</h3>
          <p>{city.notes}</p>
        </section>
      )}

      {city.wikipediaUrl !== null && (
        <p>
          <a href={city.wikipediaUrl} target="_blank" rel="noreferrer">
            Ver en Wikipedia
          </a>
        </p>
      )}

      <MusicPanel
        cityId={city.id}
        cityName={city.name}
        countryCode={city.countryCode}
        sources={city.musicSources}
        canEdit={canEdit}
        errorMessageOf={errorMessageOf}
      />

      <section>
        <h3>Fotos</h3>
        <PhotoGallery cityId={city.id} photos={city.photos} canEdit={canEdit} />
        {canEdit && <PhotoImporter cityId={city.id} errorMessageOf={errorMessageOf} />}
      </section>

      {canEdit && (
        <section>
          <h3>Fuentes externas</h3>
          <p className="hint">{formatEnrichedAt(city.enrichedAt)}</p>
          <button type="button" className="secondary" onClick={onEnrich} disabled={isEnriching}>
            {isEnriching ? 'Consultando...' : 'Traer datos de Wikipedia'}
          </button>
          {enrichErrorMessage !== null && <p className="error">{enrichErrorMessage}</p>}
        </section>
      )}

      {canEdit &&
        (isConfirmingDelete ? (
          <div className="confirm">
            <p>
              Se eliminara <strong>{city.name}</strong> con sus fotos y no se puede deshacer.
            </p>
            <div className="actions">
              <button type="button" className="danger" onClick={onDelete} disabled={isDeleting}>
                {isDeleting ? 'Eliminando...' : 'Si, eliminar'}
              </button>
              <button type="button" className="secondary" onClick={() => setIsConfirmingDelete(false)}>
                Cancelar
              </button>
            </div>
          </div>
        ) : (
          <div className="actions">
            <button type="button" onClick={onEdit}>
              Editar
            </button>
            <button type="button" className="danger" onClick={() => setIsConfirmingDelete(true)}>
              Eliminar
            </button>
          </div>
        ))}
    </article>
  )
}
