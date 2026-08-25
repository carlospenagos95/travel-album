import { useState } from 'react'
import type { RadioStation } from '../../../api/types'
import { useAddRadioStation, useAddYouTubeVideo } from '../hooks/useMusicSources'
import { useStationSearch } from '../hooks/useStationSearch'

interface MusicEditorProps {
  cityId: string
  cityName: string
  countryCode: string
  errorMessageOf: (error: unknown) => string
}

function describe(station: RadioStation): string {
  const parts = [station.country, station.codec, station.bitrate > 0 ? `${station.bitrate} kbps` : null]

  return parts.filter((part) => part !== null && part !== '').join(' - ')
}

/** Asociar musica a la ciudad: una emisora del directorio o un video de YouTube. */
export function MusicEditor({ cityId, cityName, countryCode, errorMessageOf }: MusicEditorProps) {
  // La busqueda arranca por el nombre de la ciudad, que es lo que se suele querer.
  const [query, setQuery] = useState(cityName)
  const [videoUrl, setVideoUrl] = useState('')

  const search = useStationSearch(query, countryCode)
  const addStation = useAddRadioStation(cityId)
  const addVideo = useAddYouTubeVideo(cityId)

  const submitVideo = () => {
    addVideo.mutate({ urlOrId: videoUrl, title: null }, { onSuccess: () => setVideoUrl('') })
  }

  return (
    <div className="music-editor">
      <label className="field">
        <span>Buscar emisora</span>
        <input
          type="search"
          value={query}
          placeholder="Nombre de la emisora o de la ciudad"
          onChange={(event) => setQuery(event.target.value)}
        />
      </label>

      {search.isSearching && <small className="hint">Buscando emisoras...</small>}
      {search.errorMessage !== null && <p className="error">{search.errorMessage}</p>}

      {search.stations.length > 0 && (
        <ul className="suggestions">
          {search.stations.map((station) => (
            <li key={station.uuid}>
              <button type="button" disabled={addStation.isPending} onClick={() => addStation.mutate(station)}>
                {station.name}
                <span className="hint">{describe(station)}</span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <label className="field">
        <span>O pega un enlace de YouTube</span>
        <input
          value={videoUrl}
          placeholder="https://www.youtube.com/watch?v=..."
          onChange={(event) => setVideoUrl(event.target.value)}
        />
      </label>

      <button type="button" className="secondary" disabled={videoUrl.trim() === '' || addVideo.isPending} onClick={submitVideo}>
        Agregar video
      </button>

      {addStation.error !== null && <p className="error">{errorMessageOf(addStation.error)}</p>}
      {addVideo.error !== null && <p className="error">{errorMessageOf(addVideo.error)}</p>}
    </div>
  )
}
