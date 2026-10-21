import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const apiTarget = process.env.API_HTTPS || process.env.API_HTTP
  || process.env.services__server__https__0 || process.env.services__server__http__0
  || 'https://localhost:7538';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Proxy API calls to the app service
      '/api': {
        target: apiTarget,
        changeOrigin: true
      },
      '/hubs': {
        target: apiTarget,
        changeOrigin: true,
        ws: true
      }
    }
  }
});
