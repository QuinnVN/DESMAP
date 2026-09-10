# DESMAP

DESMAP is a SvelteKit frontend for AI × VR career orientation. The interface covers the landing experience, a 101-question career questionnaire, a local profile view, and a career library. It is currently frontend only: questionnaire answers and profile state are stored in the browser, AI evaluation is clearly labeled sample data, and VR experiences are informational recommendations without launch controls.

## Run locally

```sh
pnpm install
pnpm dev
```

The development server defaults to `http://localhost:5173/`.

Run the production checks with:

```sh
pnpm check
pnpm build
pnpm preview
```

If the local pnpm wrapper asks to recreate `node_modules` in a non interactive shell, run `pnpm install` first, then invoke the installed binaries directly:

```powershell
.\node_modules\.bin\svelte-check.cmd --tsconfig .\tsconfig.json
.\node_modules\.bin\vite.cmd build
```

## Frontend integration points

- The questionnaire source of truth lives in `src/lib/questionnaire/`; its dimension counts total 101 questions across D, E, S, M, A, and P.
- Browser persistence is handled by the questionnaire/profile modules under `src/lib/`. Replace that storage boundary with an authenticated API when accounts are available.
- The evaluation route currently presents local/demo profile output. Connect the submission boundary to an AI evaluation endpoint there, keeping credentials server side.
- The `/experiences` route is a frontend career library. Replace the local role data with a CMS or API response when the catalog is ready.
- `/questionnaire` and `/evaluation` are the primary CTA destinations from the shared landing header and footer.

The visual references used for QA remain in the ignored `tmp/pdfs/` folder. Shipped raster artwork is kept under `static/desmap/`.
