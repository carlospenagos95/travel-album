import { useState } from 'react'
import type { LibraryTrack, RadioStation } from '../../../api/types'
import { useAddRadioStation, useAddTrack } from '../hooks/useMusicSources'
import { useStationSearch } from '../hooks/useStationSearch'
import { useTrackSearch } from '../hooks/useTrackSearch'

interface MusicEditorProps {
  cityId: string
  cityName: string
  countryCode: string
  errorMessageOf: (error: unknown) => string
}

function describeStation(station: RadioStation): string {
  const parts = [station.country, station.codec, station.bitrate > 0 ? `${station.bitrate} kbps` : null]

  return parts.filter((part) => part !== null && part !== '').join(' - ')
}

function describeTrack(track: LibraryTrack): string {
  const minutes = Math.floor(track.durationSeconds / 60)
  const seconds = (track.durationSeconds % 60).toString().padStart(2, '0')

  // La duracion es la de la cancion entera; lo que va a sonar son 30 segundos.
  const parts = [track.artistName, track.durationSeconds > 0 ? `${minutes}:${seconds}` : null]

  return parts.filter((part) => part !== null && part !== '').join(' - ')
}

/**
 * Asociar musica a la ciudad: una emisora del directorio o una cancion del
 * catalogo de iTunes. La emisora cambia de tema sola y suena entera; la cancion
 * es una muestra de treinta segundos que se repite en bucle.
 */
export function MusicEditor({ cityId, cityName, countryCode, errorMessageOf }: MusicEditorProps) {
  // Las busquedas arrancan por el nombre de la ciudad, que es lo que se suele querer.
  const [stationQuery, setStationQuery] = useState(cityName)
  const [trackQuery, setTrackQuery] = useState(cityName)

  const stations = useStationSearch(stationQuery, countryCode)
  const tracks = useTrackSearch(trackQuery)
  const addStation = useAddRadioStation(cityId)
  const addTrack = useAddTrack(cityId)

  return (
    <div className="music-editor">
      <label className="field">
        <span>Buscar emisora</span>
        <input
          type="search"
          value={stationQuery}
          placeholder="Nombre de la emisora o de la ciudad"
          onChange={(event) => setStationQuery(event.target.value)}
        />
      </label>

      {stations.isSearching && <small className="hint">Buscando emisoras...</small>}
      {stations.errorMessage !== null && <p className="error">{stations.errorMessage}</p>}

      {stations.stations.length > 0 && (
        <ul className="suggestions">
          {stations.stations.map((station) => (
            <li key={station.uuid}>
              <button type="button" disabled={addStation.isPending} onClick={() => addStation.mutate(station)}>
                <span className="track-line">
                  {station.name}
                  <span className="hint">{describeStation(station)}</span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <label className="field">
        <span>Buscar cancion</span>
        <input
          type="search"
          value={trackQuery}
          placeholder="Titulo o artista"
          onChange={(event) => setTrackQuery(event.target.value)}
        />
      </label>

      <small className="hint">Del catalogo de iTunes: suenan 30 segundos, en bucle.</small>

      {tracks.isSearching && <small className="hint">Buscando canciones...</small>}
      {tracks.errorMessage !== null && <p className="error">{tracks.errorMessage}</p>}

      {tracks.tracks.length > 0 && (
        <ul className="suggestions">
          {tracks.tracks.map((track) => (
            <li key={track.id}>
              <button type="button" disabled={addTrack.isPending} onClick={() => addTrack.mutate(track)}>
                {track.imageUrl !== null && <img className="cover" src={track.imageUrl} alt="" loading="lazy" />}
                <span className="track-line">
                  {track.name}
                  <span className="hint">{describeTrack(track)}</span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      {addStation.error !== null && <p className="error">{errorMessageOf(addStation.error)}</p>}
      {addTrack.error !== null && <p className="error">{errorMessageOf(addTrack.error)}</p>}
    </div>
  )
}
