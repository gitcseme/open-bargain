/// <reference types="vitest/config" />
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(import.meta.dirname, './src') },
  },
  server: {
    // The API's http profile in launchSettings.json
    proxy: { '/api': 'http://localhost:5142' },
  },
  build: {
    // The API serves the built app from wwwroot
    outDir: '../OpenBargain.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.ts'],
  },
})
