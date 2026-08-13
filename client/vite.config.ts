import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/setupTests.ts'],
    // The default `forks` pool hangs waiting for a worker on this
    // Windows/Node combination; `threads` without file parallelism starts
    // reliably.
    pool: 'threads',
    fileParallelism: false,
  },
})
