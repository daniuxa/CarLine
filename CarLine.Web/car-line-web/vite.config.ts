import {
  defineConfig,
  loadEnv,
} from 'vite';

import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    server: {
      port: Number.parseInt(env.VITE_PORT || '5173', 10),
      proxy: {
        '/api': {
          target:
            env.CARLINEAPI_HTTP ||
            env.services__carlineapi__http__0 ||
            process.env.CARLINEAPI_HTTP ||
            process.env.services__carlineapi__http__0,
          changeOrigin: true,
          secure: false,
          onProxyReq(proxyReq, req) {
            console.log('[vite][proxy] forwarding', req.method, req.url, '->', proxyReq.getHeader('host') || proxyReq.host || proxyReq.path);
          },
          onProxyRes(proxyRes, req) {
            console.log('[vite][proxy] response from target for', req.method, req.url, 'status', proxyRes.statusCode);
          },
        },
      },
    },
    configResolved: () => {
      const proxyTarget =
        env.SERVICES__CARLINEAPI__HTTPS__0 ||
        env.SERVICES__CARLINEAPI__HTTP__0 ||
        process.env.services__carlineapi__https__0 ||
        process.env.services__carlineapi__http__0 ||
        'http://localhost:5000';

      console.log('[vite] resolved proxy target for /api ->', proxyTarget);
    },
    build: {
      outDir: 'dist',
      rollupOptions: {
        input: './index.html',
      },
    },
  };
});