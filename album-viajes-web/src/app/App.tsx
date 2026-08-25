import { useState } from 'react'
import { ApiError } from '../api/client'
import type { SaveCityRequest } from '../api/types'
import { SessionBar } from '../features/auth/components/SessionBar'
import { useCurrentUser, useLogout } from '../features/auth/useCurrentUser'
import { CityDetailPanel } from '../features/cities/components/CityDetailPanel'
import { CityForm } from '../features/cities/components/CityForm'
import { CityList } from '../features/cities/components/CityList'
import { CityMap } from '../features/cities/components/CityMap'
import {
  useCities,
  useCity,
  useCreateCity,
  useDeleteCity,
  useEnrichCity,
  useUpdateCity,
} from '../features/cities/hooks/useCities'

type Mode = 'browsing' | 'creating' | 'editing'

function messageOf(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return 'Algo salio mal. Intenta de nuevo.'
}

/**
 * Si la entrada con Google se rechaza, el servidor devuelve al album con el
 * motivo en la direccion. Se lee una sola vez, al arrancar.
 */
function readSignInError(): string | null {
  return new URLSearchParams(window.location.search).get('authError')
}

export function App() {
  const [mode, setMode] = useState<Mode>('browsing')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [pickedCoordinates, setPickedCoordinates] = useState<{ latitude: number; longitude: number } | null>(null)
  const [signInError] = useState(readSignInError)

  const user = useCurrentUser()
  const logout = useLogout()
  const cities = useCities()
  const selectedCity = useCity(selectedId)
  const createCity = useCreateCity()
  const updateCity = useUpdateCity()
  const deleteCity = useDeleteCity()
  const enrichCity = useEnrichCity()

  const isEditing = mode === 'editing'
  const isFormOpen = mode !== 'browsing'
  const saveError = createCity.error ?? updateCity.error

  // El error de enriquecimiento pertenece a la ciudad que se estaba viendo:
  // al cambiar de ficha deja de tener sentido.
  const selectCity = (id: string | null) => {
    setSelectedId(id)
    enrichCity.reset()
  }

  const closeForm = () => {
    setMode('browsing')
    setPickedCoordinates(null)
    createCity.reset()
    updateCity.reset()
  }

  const handleSubmit = (city: SaveCityRequest) => {
    if (isEditing && selectedId !== null) {
      updateCity.mutate({ id: selectedId, city }, { onSuccess: closeForm })
      return
    }

    createCity.mutate(city, {
      onSuccess: (created) => {
        selectCity(created.id)
        closeForm()
      },
    })
  }

  // El panel de detalle ya pidio confirmacion antes de llamar aqui.
  const handleDelete = () => {
    if (selectedId === null) {
      return
    }

    deleteCity.mutate(selectedId, { onSuccess: () => selectCity(null) })
  }

  return (
    <div className="layout">
      <aside className="sidebar">
        <header className="sidebar-header">
          <h1>Album de viajes</h1>
          {user.isOwner && (
            <button type="button" onClick={() => (isFormOpen ? closeForm() : setMode('creating'))}>
              {isFormOpen ? 'Cerrar' : 'Agregar ciudad'}
            </button>
          )}
        </header>

        <SessionBar user={user} isLoggingOut={logout.isPending} onLogout={() => logout.mutate()} />

        {signInError !== null && <p className="error">{signInError}</p>}

        {cities.isPending && <p className="empty">Cargando ciudades...</p>}
        {cities.isError && <p className="error">{messageOf(cities.error)}</p>}

        {isFormOpen && isEditing && selectedCity.data === undefined && (
          <p className="empty">Cargando la ciudad...</p>
        )}

        {isFormOpen && (!isEditing || selectedCity.data !== undefined) ? (
          <CityForm
            // Remonta el formulario al cambiar de ciudad para que recargue sus valores.
            key={isEditing ? selectedId : 'new'}
            editing={isEditing ? (selectedCity.data ?? null) : null}
            isSaving={createCity.isPending || updateCity.isPending}
            errorMessage={saveError === null ? null : messageOf(saveError)}
            pickedCoordinates={pickedCoordinates}
            onSubmit={handleSubmit}
            onCancel={closeForm}
          />
        ) : isFormOpen ? null : (
          <>
            {cities.data !== undefined && (
              <CityList cities={cities.data} selectedId={selectedId} onSelect={selectCity} />
            )}

            {selectedCity.data !== undefined && (
              <CityDetailPanel
                city={selectedCity.data}
                canEdit={user.isOwner}
                isDeleting={deleteCity.isPending}
                isEnriching={enrichCity.isPending}
                enrichErrorMessage={enrichCity.error === null ? null : messageOf(enrichCity.error)}
                errorMessageOf={messageOf}
                onEdit={() => setMode('editing')}
                onDelete={handleDelete}
                onEnrich={() => enrichCity.mutate(selectedCity.data.id)}
                onClose={() => selectCity(null)}
              />
            )}

            {deleteCity.isError && <p className="error">{messageOf(deleteCity.error)}</p>}
          </>
        )}
      </aside>

      <main className="map-area">
        <CityMap
          cities={cities.data ?? []}
          selectedId={selectedId}
          onSelect={(id) => {
            selectCity(id)
            if (mode === 'creating') {
              closeForm()
            }
          }}
          onPickCoordinates={
            isFormOpen ? (latitude, longitude) => setPickedCoordinates({ latitude, longitude }) : null
          }
        />
      </main>
    </div>
  )
}
