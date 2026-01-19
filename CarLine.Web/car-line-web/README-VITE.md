Quick notes for running this app with Vite

- Vite reads env files prefixed with `VITE_`. To configure port and API base when running locally, set either:
  - `VITE_PORT` (dev server port)
  - `VITE_API_BASE` (base URL used by the Vite proxy if Aspire-injected env vars are not available)

- When running under Aspire/local orchestration, Aspire injects service URLs as environment variables with names like `services__carlineapi__https__0` or `services__carlineapi__http__0`.
  - The `vite.config.js` will use those if present to proxy `/api` to the API service.

- Commands:

  Install deps:

```powershell
cd 'c:\Users\danyil.salivon\Desktop\TFM\CarLineProject\CarLine.Web\car-line-web'
npm install
```

  Run dev server (Vite):

```powershell
npm run dev
```

  Build for production:

```powershell
npm run vite-build
```

- If you prefer the existing CRA (`react-scripts`) workflow, keep using `npm start`. Vite scripts are added alongside CRA for faster dev feedback.
