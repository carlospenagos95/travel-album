import { useEffect, useState, type FormEvent } from 'react'
import type { CityDetail, SaveCityRequest } from '../../../api/types'
import { usePlaceSearch } from '../../geocoding/usePlaceSearch'
import { useCityForm } from '../hooks/useCityForm'

interface CityFormProps {
  editing: CityDetail | null
  isSaving: boolean
  errorMessage: string | null
  pickedCoordinates: { latitude: number; longitude: number } | null
  onSubmit: (city: SaveCityRequest) => void
  onCancel: () => void
}

export function CityForm({
  editing,
  isSaving,
  errorMessage,
  pickedCoordinates,
  onSubmit,
  onCancel,
}: CityFormProps) {
  const form = useCityForm(editing)
  const [query, setQuery] = useState('')
  const { suggestions, isSearching } = usePlaceSearch(query)

  // Las coordenadas elegidas con un clic en el mapa entran por props.
  const { setCoordinates } = form
  useEffect(() => {
    if (pickedCoordinates !== null) {
      setCoordinates(pickedCoordinates.latitude, pickedCoordinates.longitude)
    }
  }, [pickedCoordinates, setCoordinates])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSubmit(form.toRequest())
  }

  return (
    <form className="panel" onSubmit={handleSubmit}>
      <h2>{editing === null ? 'Nueva ciudad' : `Editar ${editing.name}`}</h2>

      {editing === null && (
        <label className="field">
          <span>Buscar en OpenStreetMap</span>
          <input
            type="search"
            value={query}
            placeholder="Escribe una ciudad y elige una sugerencia"
            onChange={(event) => setQuery(event.target.value)}
          />
          {isSearching && <small className="hint">Buscando...</small>}
          {suggestions.length > 0 && (
            <ul className="suggestions">
              {suggestions.map((place) => (
                <li key={place.id}>
                  <button
                    type="button"
                    onClick={() => {
                      form.applySuggestion(place)
                      setQuery('')
                    }}
                  >
                    {place.displayName}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </label>
      )}

      <div className="field-row">
        <label className="field">
          <span>Ciudad *</span>
          <input value={form.values.name} onChange={(event) => form.setField('name', event.target.value)} required />
        </label>
        <label className="field">
          <span>Pais *</span>
          <input
            value={form.values.country}
            onChange={(event) => form.setField('country', event.target.value)}
            required
          />
        </label>
        <label className="field field-small">
          <span>ISO *</span>
          <input
            value={form.values.countryCode}
            maxLength={2}
            placeholder="CO"
            onChange={(event) => form.setField('countryCode', event.target.value.toUpperCase())}
            required
          />
        </label>
      </div>

      <div className="field-row">
        <label className="field">
          <span>Latitud *</span>
          <input
            type="number"
            step="any"
            value={form.values.latitude}
            onChange={(event) => form.setField('latitude', event.target.value)}
            required
          />
        </label>
        <label className="field">
          <span>Longitud *</span>
          <input
            type="number"
            step="any"
            value={form.values.longitude}
            onChange={(event) => form.setField('longitude', event.target.value)}
            required
          />
        </label>
      </div>
      <small className="hint">Tambien puedes hacer clic en el mapa para fijar el punto.</small>

      <div className="field-row">
        <label className="field">
          <span>Fecha de visita</span>
          <input
            type="date"
            value={form.values.visitedOn}
            onChange={(event) => form.setField('visitedOn', event.target.value)}
          />
        </label>
        <label className="field">
          <span>Comida tipica</span>
          <input
            value={form.values.typicalFood}
            placeholder="Bandeja paisa"
            onChange={(event) => form.setField('typicalFood', event.target.value)}
          />
        </label>
      </div>

      <label className="field">
        <span>Descripcion</span>
        <textarea
          rows={3}
          value={form.values.shortDescription}
          onChange={(event) => form.setField('shortDescription', event.target.value)}
        />
      </label>

      <label className="field">
        <span>Notas del viaje</span>
        <textarea rows={2} value={form.values.notes} onChange={(event) => form.setField('notes', event.target.value)} />
      </label>

      {errorMessage !== null && <p className="error">{errorMessage}</p>}

      <div className="actions">
        <button type="submit" disabled={!form.isComplete || isSaving}>
          {isSaving ? 'Guardando...' : 'Guardar'}
        </button>
        <button type="button" className="secondary" onClick={onCancel}>
          Cancelar
        </button>
      </div>
    </form>
  )
}
