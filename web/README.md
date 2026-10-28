# DESMAP

DESMAP is a SvelteKit application for AI × VR career orientation. The interface covers the landing experience, a 101-question career questionnaire, a local profile view, and a career library. Completed questionnaire submissions are saved in the browser before the server stores them in MongoDB.

## Run locally

```sh
pnpm install
pnpm dev
```

The development server defaults to `http://localhost:5173/`.

Copy `.env.example` to `.env` when local configuration is needed. AI remains enabled for initial assessments by default. For deterministic assessment tests that must not contact AI, set:

```env
DISABLE_AI_INIT_ASSESSMENT=true
```

Missing, `false`, or invalid values keep the AI path enabled.

Set the private URL for the AI assessment service. Local development can use the loopback URL,
but Vercel must receive a URL that its functions can reach:

```env
AI_BACKEND_URL=http://127.0.0.1:8000/api/ai/initial-career-assessment
```

Set the private MongoDB connection values in `.env`:

```env
MONGODB_URI=mongodb://127.0.0.1:27017
MONGODB_DATABASE=desmap
```

The application writes to the `questionnaire_submissions` collection. It creates unique indexes for normalized participant email addresses and assessment identifiers.

## Deploy to Vercel

The project uses `@sveltejs/adapter-vercel`, Node.js 24, and pnpm 12. Configure these private
environment variables for both Preview and Production before deploying:

```env
AI_BACKEND_URL=https://your-ai-service.example/api/ai/initial-career-assessment
DISABLE_AI_INIT_ASSESSMENT=false
MONGODB_URI=mongodb+srv://...
MONGODB_DATABASE=desmap
```

If the deployment should use the built-in weighted calculation instead of the AI service, set
`DISABLE_AI_INIT_ASSESSMENT=true`; `AI_BACKEND_URL` is then optional. Use a MongoDB deployment that
accepts connections from Vercel Functions. Keep the database and functions in nearby regions where
possible.

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
- Browser persistence and MongoDB synchronization are handled by the questionnaire modules under `src/lib/`. MongoDB credentials remain in server-only code.
- The evaluation route currently presents local/demo profile output. Connect the submission boundary to an AI evaluation endpoint there, keeping credentials server side.
- The `/experiences` route is a frontend career library. Replace the local role data with a CMS or API response when the catalog is ready.
- `/questionnaire` and `/evaluation` are the primary CTA destinations from the shared landing header and footer.

The visual references used for QA remain in the ignored `tmp/pdfs/` folder. Shipped raster artwork is kept under `static/desmap/`.
