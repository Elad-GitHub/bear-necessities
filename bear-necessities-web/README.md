<p align="center">
  <img src="public/brand/paw-trail.svg" alt="" width="180">
</p>

# Bear Necessities – web app

The Angular front end of **Bear Necessities**, a map for conscious travellers: yoga, healing, ecstatic dance, eco-villages, festivals and the communities around them. The project overview, screenshots and the API live in the [main README](../README.md).

## Run it

You need Node 22+ and the API running on `http://localhost:5130` (see the [API project](../BearNecessities)).

```bash
npm install
npm start
```

The app opens on `http://localhost:4200`. The API address and the public Mapbox token are in `src/environments/environment.ts`.

```bash
npm run build   # production build into dist/
npm test        # unit tests (Vitest)
```

## How it's built

- **Angular 22 with NgModules** – each feature under `src/app/features/` (map, place and event pages, trip planner, profile, admin) is a lazy-loaded module; components use signals for state.
- **Mapbox GL** draws the map: clustered category pins, heat-style density at low zoom, the trip route with numbered stops, and pulsing rings for filter matches.
- **`src/app/core/`** holds the services (API, auth, map state, categories), the auth and error interceptors, and the shared models that mirror the API's DTOs.
- **`src/app/shared/`** holds reusable pieces: the navbar, category chips, bear icons, avatars, photo credits and pipes.
- **Branding** – the bear icons and the paw-trail mark are in `public/brand/`; text is set in Nunito, headings in Fredoka.
