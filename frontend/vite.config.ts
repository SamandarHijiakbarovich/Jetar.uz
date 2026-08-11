import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: false,
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        // SignalR faqat bitim sahifasida kerak — alohida chunkda qoladi.
        manualChunks(id: string) {
          if (id.includes('@microsoft/signalr')) return 'signalr'
          if (id.includes('react-router')) return 'router'
          return undefined
        },
      },
    },
  },
})
