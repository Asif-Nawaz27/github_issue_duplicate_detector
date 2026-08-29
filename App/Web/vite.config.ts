import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Forwards to the API started via `dotnet run --project App/Api` (see
      // App/Api/Properties/launchSettings.json). Avoids needing CORS configured
      // on the API for local development.
      '/api': {
        target: 'http://localhost:5100',
        changeOrigin: true,
      },
    },
  },
})
