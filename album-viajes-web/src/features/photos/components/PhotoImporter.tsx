import { usePhotoPicker } from '../hooks/usePhotoPicker'

interface PhotoImporterProps {
  cityId: string
  errorMessageOf: (error: unknown) => string
}

/**
 * Traer fotos desde Google Photos. La eleccion ocurre en una pestania de Google
 * (es la unica via que su API permite a una aplicacion de terceros), y el album
 * espera aqui hasta que termine.
 */
export function PhotoImporter({ cityId, errorMessageOf }: PhotoImporterProps) {
  const picker = usePhotoPicker(cityId)

  return (
    <div className="importer">
      <button type="button" className="secondary" disabled={picker.isStarting} onClick={picker.start}>
        {picker.stage === 'idle' || picker.stage === 'done' ? 'Traer fotos de Google Photos' : 'Volver a empezar'}
      </button>

      {picker.stage === 'choosing' && (
        <>
          <p className="hint">
            Elige las fotos en la pestania de Google. En cuanto termines se copian aqui solas.
          </p>
          {picker.pickerUri !== null && (
            <p className="hint">
              <a href={picker.pickerUri} target="_blank" rel="noreferrer">
                Abrir de nuevo el selector
              </a>
            </p>
          )}
          <button type="button" className="link" onClick={picker.cancel}>
            Dejarlo por ahora
          </button>
        </>
      )}

      {picker.stage === 'importing' && <p className="hint">Copiando las fotos al servidor...</p>}

      {picker.stage === 'done' && picker.result !== null && (
        <p className="hint">
          {picker.result.imported} {picker.result.imported === 1 ? 'foto copiada' : 'fotos copiadas'}
          {picker.result.skipped > 0 && `, ${picker.result.skipped} ya estaban`}.
        </p>
      )}

      {picker.errorMessage !== null && <p className="error">{errorMessageOf(picker.errorMessage)}</p>}
    </div>
  )
}
