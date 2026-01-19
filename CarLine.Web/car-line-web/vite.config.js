import {
  defineConfig,
  loadEnv,
} from 'vite';

import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    server:{
      port: parseInt(env.VITE_PORT),
      proxy: {
        // "carlineapi" is the name of the API in AppHost.cs.
        '/api': {
          target: env.CARLINEAPI_HTTP || env.services__carlineapi__http__0 || process.env.CARLINEAPI_HTTP || process.env.services__carlineapi__http__0,
          changeOrigin: true,
          secure: false,
          onProxyReq(proxyReq, req, res) {
            // eslint-disable-next-line no-console
            console.log('[vite][proxy] forwarding', req.method, req.url, '->', proxyReq.getHeader('host') || proxyReq.host || proxyReq.path);
          },
          onProxyRes(proxyRes, req, res) {
            // eslint-disable-next-line no-console
            console.log('[vite][proxy] response from target for', req.method, req.url, 'status', proxyRes.statusCode);
          }
        }
      }
    },
    // Log resolved proxy target for easier debugging when running Vite
    configResolved: (resolvedConfig) => {
      const proxyTarget = env.SERVICES__CARLINEAPI__HTTPS__0 || env.SERVICES__CARLINEAPI__HTTP__0 || process.env.services__carlineapi__https__0 || process.env.services__carlineapi__http__0 || 'http://localhost:5000';
      // eslint-disable-next-line no-console
      console.log('[vite] resolved proxy target for /api ->', proxyTarget);
    },
    build:{
      outDir: 'dist',
      rollupOptions: {
        // index.html lives in the `public` folder for this project
        input: './index.html'
      }
    }
  }
})
