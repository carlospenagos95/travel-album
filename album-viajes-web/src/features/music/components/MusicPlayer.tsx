import { useEffect, useRef, useState } from 'react'
import type { MusicSource } from '../../../api/types'

interface MusicPlayerProps {
  source: MusicSource
}

/**
 * Reproduce la fuente que suena al abrir la ciudad.
 *
 * Todo pasa por el proxy del propio servidor: muchas emisoras siguen en http
 * plano y el navegador las bloquearia al servir el album por https, y las
 * muestras de iTunes llegan con un tipo de contenido que no todos los
 * navegadores reconocen.
 *
 * Las canciones son muestras de treinta segundos, asi que se repiten: el
 * silencio a media galeria se nota mas que la repeticion. El bucle suena desde
 * lo que el navegador ya tiene en memoria, sin volver a pedirlo al servidor.
 *
 * Los navegadores rechazan reproducir sonido sin que el usuario haya interactuado
 * antes con la pagina. Abrir la ciudad es un clic, asi que normalmente arranca
 * sola; si aun asi se bloquea, el rechazo se recoge y se convierte en un boton
 * de play en lugar de dejar el reproductor mudo sin explicacion.
 */
export function MusicPlayer({ source }: MusicPlayerProps) {
  const audioRef = useRef<HTMLAudioElement>(null)
  const [isBlocked, setIsBlocked] = useState(false)

  const { streamUrl } = source

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
      <p className="hint">
        Suena {source.label}
        {source.artist !== null && ` - ${source.artist}`}
      </p>

      {isBlocked && (
        <button type="button" onClick={play}>
          Reproducir
        </button>
      )}

      {/* controls deja pausar y bajar el volumen sin salir de la ficha. */}
      <audio ref={audioRef} src={streamUrl} controls loop preload="none">
        Tu navegador no puede reproducir audio.
      </audio>
    </div>
  )
}
