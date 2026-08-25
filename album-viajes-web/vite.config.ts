import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // En desarrollo el frontend habla con la API por el mismo origen, asi que
    // no hay CORS que configurar ni URLs absolutas repartidas por el codigo.
    proxy: {
      '/api': {
        target: 'http://localhost:5120',
        changeOrigin: true,
      },
    },
  },
})
