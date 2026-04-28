import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

// Passenger app — runs on http://localhost:5173
export default defineConfig({
  root: path.resolve(__dirname, 'passenger'),
  plugins: [react()],
  resolve: {
    alias: { '@shared': path.resolve(__dirname, 'shared') },
  },
  server: { port: 5173 },
  build: { outDir: path.resolve(__dirname, 'dist/passenger'), emptyOutDir: true },
});
