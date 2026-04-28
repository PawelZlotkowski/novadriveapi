import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

// Admin app — runs on http://localhost:5174
export default defineConfig({
  root: path.resolve(__dirname, 'admin'),
  plugins: [react()],
  resolve: {
    alias: { '@shared': path.resolve(__dirname, 'shared') },
  },
  server: { port: 5174 },
  build: { outDir: path.resolve(__dirname, 'dist/admin'), emptyOutDir: true },
});
