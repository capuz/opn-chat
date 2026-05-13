import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  return {
    plugins: [react(), tailwindcss()],
    server: { open: true },
    define: {
      'import.meta.env.VITE_GOOGLE_CLIENT_ID':  JSON.stringify(env.VITE_GOOGLE_CLIENT_ID),
      'import.meta.env.VITE_ADS_PROVIDER':      JSON.stringify(env.VITE_ADS_PROVIDER      ?? 'adsense'),
      'import.meta.env.VITE_ADSENSE_CLIENT':    JSON.stringify(env.VITE_ADSENSE_CLIENT    ?? 'ca-pub-7626774247344411'),
      'import.meta.env.VITE_ADSENSE_SLOT':      JSON.stringify(env.VITE_ADSENSE_SLOT      ?? '8267497706'),
      'import.meta.env.VITE_MOCK_AD_DELAY_MS':  JSON.stringify(env.VITE_MOCK_AD_DELAY_MS  ?? '800'),
      'import.meta.env.VITE_MOCK_AD_GRANT':     JSON.stringify(env.VITE_MOCK_AD_GRANT     ?? 'true'),
    }
  }
})
