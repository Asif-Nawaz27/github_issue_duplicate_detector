import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Forwards to the API started via `dotnet run --project App/Api` (see
      // App/Api/Properties/launchSettings.json), using its HTTPS URL directly.
      // secure: false skips certificate validation against the ASP.NET Core
      // dev cert, which Node doesn't trust by default.
      '/api': {
        target: 'https://localhost:7094',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
