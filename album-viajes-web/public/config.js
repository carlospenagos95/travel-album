// Configuracion del album, leida al abrir la pagina y no en el momento de
// compilar. Asi se cambia el centro del mapa en el servidor editando este
// archivo, sin reconstruir la imagen.
//
// Los valores no definidos aqui caen a los de src/config/appConfig.ts.
window.__ALBUM_VIAJES__ = {
  map: {
    // Centro por defecto: Bogota, Colombia.
    centerLatitude: 4.7110,
    centerLongitude: -74.0721,

    // 6 encuadra Colombia entera; 11 o 12 muestran una sola ciudad.
    zoom: 6,

    // Acercamiento al abrir una ciudad, para ver su casco urbano.
    cityZoom: 12,
  },
}
