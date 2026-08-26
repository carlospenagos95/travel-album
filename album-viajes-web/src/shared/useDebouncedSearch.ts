import { useEffect, useRef, useState } from 'react'

const DEFAULT_DELAY_MS = 500

interface SearchState<T> {
  key: string
  items: T[]
  errorMessage: string | null
}

interface DebouncedSearchOptions<T> {
  query: string
  /** Debajo de esta longitud no se busca: no vale la pena molestar al servicio. */
  minLength: number
  /**
   * Distingue dos consultas con el mismo texto cuando la busqueda depende de
   * algo mas (el pais de la ciudad, por ejemplo).
   */
  extraKey?: string
  delayMs?: number
  search: (query: string, signal: AbortSignal) => Promise<T[]>
  errorMessage: string
}

/**
 * Busca mientras se escribe, con debounce y cancelando la peticion anterior.
 *
 * El resultado recuerda a que consulta pertenece, de modo que lo que se muestra
 * se deriva en el render en vez de sincronizarse con setState dentro del efecto:
 * una respuesta que llega tarde nunca pisa a una consulta mas nueva.
 */
export function useDebouncedSearch<T>({
  query,
  minLength,
  extraKey = '',
  delayMs = DEFAULT_DELAY_MS,
  search,
  errorMessage,
}: DebouncedSearchOptions<T>) {
  const [state, setState] = useState<SearchState<T>>({ key: '', items: [], errorMessage: null })

  // Quien llama suele pasar una funcion nueva en cada render. Guardarla en una
  // referencia deja que el efecto dependa solo de la consulta: si no, cada
  // resultado provocaria un render, una funcion nueva y otra busqueda.
  const searchRef = useRef(search)

  useEffect(() => {
    searchRef.current = search
  }, [search])

  const trimmed = query.trim()
  const key = `${trimmed}|${extraKey}`
  const isLongEnough = trimmed.length >= minLength

  useEffect(() => {
    const current = query.trim()
    const currentKey = `${current}|${extraKey}`

    if (current.length < minLength) {
      return
    }

    const controller = new AbortController()

    const timer = setTimeout(() => {
      searchRef.current(current, controller.signal)
        .then((items) => setState({ key: currentKey, items, errorMessage: null }))
        .catch((error: unknown) => {
          // Cancelar la busqueda anterior no es un fallo que contarle a nadie.
          if (controller.signal.aborted) {
            return
          }

          // El servidor suele explicar mejor que paso (que falta configurar el
          // catalogo, por ejemplo); el mensaje generico es el ultimo recurso.
          setState({
            key: currentKey,
            items: [],
            errorMessage: error instanceof Error ? error.message : errorMessage,
          })
        })
    }, delayMs)

    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [query, extraKey, minLength, delayMs, errorMessage])

  const isCurrent = state.key === key

  return {
    items: isLongEnough && isCurrent ? state.items : [],
    errorMessage: isLongEnough && isCurrent ? state.errorMessage : null,
    isSearching: isLongEnough && !isCurrent,
  }
}
