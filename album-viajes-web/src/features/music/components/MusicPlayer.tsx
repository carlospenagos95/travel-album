import { useEffect, useRef, useState } from 'react'
import type { MusicSource } from '../../../api/types'

interface MusicPlayerProps {
  source: MusicSource
}

/**
 * Reproduce la fuente que suena al abrir la ciudad.
 *
 * La radio pasa por el proxy del propio servidor: muchas emisoras siguen en http
 * plano y el navegador las bloquearia al servir el album por https.
 *
 * Los navegadores rechazan reproducir sonido sin que el usuario haya interactuado
 * antes con la pagina. Ese rechazo se recoge y se convierte en un boton de play,
 * en lugar de dejar el reproductor mudo sin explicacion.
 */
export function MusicPlayer({ source }: MusicPlayerProps) {
  if (source.kind === 'YouTube' && source.youTubeVideoId !== null) {
    return <YouTubePlayer videoId={source.youTubeVideoId} label={source.label} />
  }

  if (source.streamUrl !== null) {
    return <RadioPlayer streamUrl={source.streamUrl} label={source.label} />
  }

  return <p className="hint">Esta fuente de musica no se puede reproducir.</p>
}

function RadioPlayer({ streamUrl, label }: { streamUrl: string; label: string }) {
  const audioRef = useRef<HTMLAudioElement>(null)
  const [isBlocked, setIsBlocked] = useState(false)

  useEffect(() => {
    const audio = audioRef.current

    if (audio === null) {
      return
    }

    audio
      .play()
      .then(() => setIsBlocked(false))
      .catch(() => setIsBlocked(true))
  }, [streamUrl])

  const play = () => {
    audioRef.current
      ?.play()
      .then(() => setIsBlocked(false))
      .catch(() => setIsBlocked(true))
  }

  return (
    <div className="player">
      <p className="hint">Suena {label}</p>

      {isBlocked && (
        <button type="button" onClick={play}>
          Reproducir
        </button>
      )}

      {/* controls deja pausar y bajar el volumen sin salir de la ficha. */}
      <audio ref={audioRef} src={streamUrl} controls preload="none">
        Tu navegador no puede reproducir audio.
      </audio>
    </div>
  )
}

function YouTubePlayer({ videoId, label }: { videoId: string; label: string }) {
  return (
    <div className="player">
      <p className="hint">Suena {label}</p>
      <iframe
        className="youtube"
        // nocookie evita que YouTube deje rastro en quien solo viene a ver el album.
        src={`https://www.youtube-nocookie.com/embed/${videoId}?autoplay=1`}
        title={label}
        allow="autoplay; encrypted-media; picture-in-picture"
        allowFullScreen
      />
    </div>
  )
}
