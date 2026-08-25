import type { CitySummary } from '../../../api/types'

interface CityListProps {
  cities: CitySummary[]
  selectedId: string | null
  onSelect: (id: string) => void
}

export function CityList({ cities, selectedId, onSelect }: CityListProps) {
  if (cities.length === 0) {
    return <p className="empty">Todavia no hay ciudades. Agrega la primera.</p>
  }

  return (
    <ul className="city-list">
      {cities.map((city) => (
        <li key={city.id}>
          <button
            type="button"
            className={city.id === selectedId ? 'selected' : ''}
            onClick={() => onSelect(city.id)}
          >
            {city.coverThumbnailUrl !== null && (
              <img className="cover" src={city.coverThumbnailUrl} alt="" loading="lazy" />
            )}
            <span className="city-line">
              <strong>{city.name}</strong>
              <span>
                {city.country}
                {/* Un vistazo a lo que ya tiene la ficha, sin abrirla. */}
                {city.photoCount > 0 && ` - ${city.photoCount} fotos`}
                {city.hasMusic && ' - suena'}
              </span>
            </span>
          </button>
        </li>
      ))}
    </ul>
  )
}
