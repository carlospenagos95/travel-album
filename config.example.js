// Configuracion del mapa en el servidor. Copiar a config.js antes del primer
// despliegue: docker-compose.prod.yml lo monta dentro del contenedor web.
//
// El navegador lo carga al abrir la pagina, de modo que cambiar el centro es
// editar este archivo y recargar; no hay que reconstruir la imagen.
window.__ALBUM_VIAJES__ = {
  map: {
    // Centro por defecto: Bogota, Colombia. Cambialo por tu ciudad.
    centerLatitude: 4.7110,
    centerLongitude: -74.0721,

    // 6 encuadra Colombia entera; 11 o 12 muestran una sola ciudad.
    zoom: 6,
  },
}
