import L from 'leaflet'
import { MapContainer, Marker, Popup, TileLayer, useMapEvents } from 'react-leaflet'
import type { CitySummary } from '../../../api/types'
import { mapConfig } from '../../../config/appConfig'

import 'leaflet/dist/leaflet.css'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'

// Leaflet resuelve los iconos por rutas relativas que el bundler no conoce;
// sin esto los pines salen rotos en produccion.
const defaultIcon = L.icon({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
})

L.Marker.prototype.options.icon = defaultIcon

interface CityMapProps {
  cities: CitySummary[]
  selectedId: string | null
  onSelect: (id: string) => void
  onPickCoordinates: ((latitude: number, longitude: number) => void) | null
}

function MapClickHandler({ onPick }: { onPick: (latitude: number, longitude: number) => void }) {
  useMapEvents({
    click: (event) => onPick(event.latlng.lat, event.latlng.lng),
  })

  return null
}

export function CityMap({ cities, selectedId, onSelect, onPickCoordinates }: CityMapProps) {
  return (
    <MapContainer center={mapConfig.center} zoom={mapConfig.zoom} className="map" worldCopyJump>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />

      {onPickCoordinates !== null && <MapClickHandler onPick={onPickCoordinates} />}

      {cities.map((city) => (
        <Marker
          key={city.id}
          position={[city.latitude, city.longitude]}
          opacity={selectedId === null || selectedId === city.id ? 1 : 0.55}
          eventHandlers={{ click: () => onSelect(city.id) }}
        >
          <Popup>
            <strong>{city.name}</strong>
            <br />
            {city.country}
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  )
}
