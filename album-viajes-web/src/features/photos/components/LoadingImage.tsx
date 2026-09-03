import { useState } from 'react'
import type { ImgHTMLAttributes } from 'react'

type LoadingImageProps = ImgHTMLAttributes<HTMLImageElement>

/** Envuelve un <img> mostrando un spinner mientras la imagen carga. */
export function LoadingImage({ className, onLoad, onError, ...imgProps }: LoadingImageProps) {
  const [isLoaded, setIsLoaded] = useState(false)

  return (
    <span className={`loading-image${isLoaded ? '' : ' is-loading'}`}>
      {!isLoaded && <span className="spinner" aria-hidden="true" />}
      <img
        {...imgProps}
        className={className}
        onLoad={(event) => {
          setIsLoaded(true)
          onLoad?.(event)
        }}
        onError={(event) => {
          setIsLoaded(true)
          onError?.(event)
        }}
      />
    </span>
  )
}
