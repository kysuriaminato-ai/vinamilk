import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  root: resolve(__dirname, 'assets'),
  base: '/dist/',
  build: {
    outDir: resolve(__dirname, 'public/dist'),
    emptyOutDir: true,
    manifest: 'manifest.json',
    rollupOptions: {
      input: {
        app: resolve(__dirname, 'assets/js/app.js'),
        styles: resolve(__dirname, 'assets/scss/app.scss')
      },
      output: {
        entryFileNames: 'js/[name]-[hash].js',
        chunkFileNames: 'js/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name && assetInfo.name.endsWith('.css')) {
            return 'css/[name]-[hash].css';
          }
          return 'assets/[name]-[hash][extname]';
        }
      }
    },
    minify: 'esbuild'
  }
});