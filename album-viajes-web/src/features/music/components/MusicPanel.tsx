import { useState } from 'react'
import type { MusicSource } from '../../../api/types'
import { useRemoveMusicSource, useSetDefaultMusicSource } from '../hooks/useMusicSources'
import { MusicEditor } from './MusicEditor'
import { MusicPlayer } from './MusicPlayer'

interface MusicPanelProps {
  cityId: string
  cityName: string
  countryCode: string
  sources: MusicSource[]
  canEdit: boolean
  errorMessageOf: (error: unknown) => string
}

export function MusicPanel({
  cityId,
  cityName,
  countryCode,
  sources,
  canEdit,
  errorMessageOf,
}: MusicPanelProps) {
  const [isEditing, setIsEditing] = useState(false)

  const setDefault = useSetDefaultMusicSource(cityId)
  const remove = useRemoveMusicSource(cityId)

  const playing = sources.find((source) => source.isDefault) ?? null

  // Con una sola fuente no hay lista donde elegir, asi que el boton de quitarla
  // se muestra suelto.
  const onlySource = sources.length === 1 ? sources[0] : undefined

  return (
    <section>
      <h3>Musica</h3>

      {playing === null ? (
        <p className="empty">Esta ciudad todavia no suena.</p>
      ) : (
        // Remontar al cambiar de fuente cierra el stream anterior en vez de
        // dejar dos peticiones de audio abiertas.
        <MusicPlayer key={playing.id} source={playing} />
      )}

      {sources.length > 1 && (
        <ul className="music-list">
          {sources.map((source) => (
            <li key={source.id}>
              <button
                type="button"
                className={source.isDefault ? 'link selected' : 'link'}
                disabled={source.isDefault || setDefault.isPending}
                onClick={() => setDefault.mutate(source.id)}
              >
                {source.label}
              </button>
              {canEdit && (
                <button
                  type="button"
                  className="icon danger"
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(source.id)}
                  aria-label={`Quitar ${source.label}`}
                >
                  x
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canEdit && onlySource !== undefined && (
        <button
          type="button"
          className="icon danger"
          disabled={remove.isPending}
          onClick={() => remove.mutate(onlySource.id)}
        >
          Quitar {onlySource.label}
        </button>
      )}

      {canEdit && (
        <>
          <button type="button" className="link" onClick={() => setIsEditing(!isEditing)}>
            {isEditing ? 'Cerrar' : 'Agregar musica'}
          </button>

          {isEditing && (
            <MusicEditor
              cityId={cityId}
              cityName={cityName}
              countryCode={countryCode}
              errorMessageOf={errorMessageOf}
            />
          )}
        </>
      )}

      {setDefault.error !== null && <p className="error">{errorMessageOf(setDefault.error)}</p>}
      {remove.error !== null && <p className="error">{errorMessageOf(remove.error)}</p>}
    </section>
  )
}
