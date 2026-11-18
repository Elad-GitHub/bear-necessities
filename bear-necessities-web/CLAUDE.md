# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Bear Necessities Web** is an Angular 22 single-page application for location-based discovery and community engagement. Users can explore places and events on an interactive Mapbox map, filter by categories, manage their profile, and submit listings.

## Build, Run, Test

### Install Dependencies
```bash
npm install
```

### Development Server
```bash
npm start
```
Runs `ng serve` on `http://localhost:4200`. Hot reload enabled.

### Production Build
```bash
npm run build
```
Output in `dist/`. Uses Webpack, tree-shaking, minification, and source maps for development.

### Run All Tests
```bash
npm test
```
Uses Vitest with jsdom. Config in `vitest.config.ts` (implicit Angular setup).

### Run Single Test
```bash
npm test -- src/app/app.spec.ts
npm test -- --run  # Single run instead of watch mode
npm test -- map.component.spec.ts  # Glob match
```

## Architecture

### Module Organization (Feature-Based)

- **src/app/core/** — Singleton services, interceptors, guards, shared models
  - `services/` — ApiService (HTTP wrapper), AuthService (state + storage), MapService (Mapbox), ToastService
  - `interceptors/` — AuthInterceptor (add Bearer token), ErrorInterceptor (handle HTTP errors)
  - `guards/` — AuthGuard (route protection)
  - `models/` — Data transfer objects (auth, place, event, review, category, media, RSVP)

- **src/app/features/** — Lazy-loaded feature modules; each owns its routes, components, and child modules
  - `map/` — Map view with category filtering and place markers
  - `place-detail/` — Single place detail, reviews, RSVPs
  - `event-detail/` — Event details
  - `auth/` — Login/register forms
  - `profile/` — User profile management
  - `submit-listing/` — Form to submit new places

- **src/app/shared/** — Reusable pipes, components, directives
  - `pipes/` — time-ago, format-date

- **src/main.ts** — Bootstrap entry point (platforms + AppModule)
- **src/app/app-module.ts** — Root module with interceptors, initializers
- **src/app/app-routing-module.ts** — Root routes with lazy loading
- **src/app/app.ts** — Root component (outlet)

### Data Flow & Patterns

1. **API Layer**: All HTTP goes through `ApiService.get/post/put/delete()` which prepends `environment.apiUrl` and handles `HttpParams`.

2. **Authentication State**: `AuthService` holds `currentUser` and `isAuthenticated` as Angular signals. Initializes from localStorage on app startup (via `APP_INITIALIZER`). AuthInterceptor adds Bearer token to all requests; ErrorInterceptor catches 401 and calls `AuthService.logout()`.

3. **Component State**: Uses Angular Signals (`signal()`, `computed()`, `effect()`) for reactive state. No RxJS subjects in components—subscribe directly to API calls or use effect() for side effects.

4. **Mapbox Integration**: `MapService` wraps the Mapbox GL library, exposes `getMap()`, `initializeMap()`, `addMarker()`, `clearMarkers()`, `getBounds()`. Components subscribe to map events (moveend, zoomend) to drive data reloads.

5. **Route Guards**: AuthGuard checks `isAuthenticated` to protect /profile and /submit routes.

6. **Error Handling**: ErrorInterceptor catches HTTP errors and publishes them via `ToastService` (Material Snackbar). Components assume success unless they override the error handler.

## Non-Obvious Conventions

### Module Naming & Structure
- Modules use `@NgModule` decorator (not standalone components). Routing modules are separate files (`*-routing-module.ts`).
- Non-standalone components must be declared in a module's `declarations` array.
- Lazy loading is done in root `app-routing-module.ts` with `loadChildren: () => import(...).then(m => m.SomeModule)`.

### Signal-Based State Management
- Components declare state as signals: `const isLoading = signal(false)`.
- Computed signals derive state: `isAuthenticated = computed(() => this.currentUser() !== null)`.
- No manual subscription cleanup needed—signals handle reactivity.
- In templates: `{{ isLoading() }}` (call as a function).

### API Contract Strict Typing
- All API responses are typed via DTOs in `src/app/core/models/`.
- `ApiService.get<T>()` and `post<T>()` return `Observable<T>` for strong typing.
- Query params are passed as `Record<string, any>` and serialized by `HttpParams.set()`.

### Auth Token Storage & Expiry
- Tokens stored in `localStorage` under keys `access_token` and `refresh_token`. User DTO stored as `auth_user` (JSON string).
- No automatic refresh token mechanism—assumes backend issues long-lived tokens or frontend handles 401 gracefully.
- `AuthService.loadFromStorage()` runs on app init; if parsing fails, logs out to avoid stale state.

### Mapbox Token & CORS
- Token hardcoded in `environment.ts` (not a secret—public Mapbox token). Only restricts usage by URL domain.
- Map initialized in `ngOnInit` with bounds-based place loading. Marker clicks navigate to place detail.

### Error Handling Strategy
- HTTP errors caught by ErrorInterceptor and shown as toasts. Components subscribe with `.subscribe({ next, error })` only when they need custom handling.
- Happy path assumes success; errors are exceptional.

### Form Input Conventions
- Component methods that set state don't return values; they mutate signals directly.
- `onCategoryChange(categoryId, selected)` updates `selectedCategories` signal and triggers `loadPlaces()`.

## Code Quality Standards

### No Duplication
- **Before writing new code, grep the codebase** for similar patterns. Consolidate repeated logic into utilities or services.
- Extract shared component patterns into base classes or shared components.
- Move cross-cutting concerns (logging, timing, caching) into interceptors or decorators.

### SOLID Principles
- **Single Responsibility**: Each class has one reason to change. ApiService handles HTTP, AuthService handles auth state, MapService handles Mapbox, ToastService handles notifications.
- **Open/Closed**: Extend via interceptors (auth, error handling), not by modifying existing services.
- **Liskov Substitution**: No `NotImplementedException` in implementations. If a method doesn't apply, split the interface.
- **Interface Segregation**: Don't force components to implement unused methods. Keep component inputs/outputs focused.
- **Dependency Injection**: Always inject services via constructor; never use `new MyService()`. DI container manages the singleton lifetime.

### Minimal External Calls
- **Never call the same endpoint twice per request**. Cache results in signals or component state.
- **No API calls in loops**. Batch data in one request (e.g., place search with bounding box, not per marker).
- **No N+1 queries**. Load aggregated data in a single API call, not one call per item.
- Use `OnDestroy` to cancel pending subscriptions if needed.

### Clear Naming
- **Methods**: `verb + noun` (e.g., `loadPlaces()`, `addMarker()`, `isAuthenticated`, `hasCategories()`).
- **Booleans**: Prefix with `is/has/can/should` (e.g., `isLoading`, `hasErrors`, `canActivate`).
- **Banned abbreviations**: `mgr`, `svc`, `val`, `obj`, `tmp`, `res` — spell them out.
- **Single-letter variables**: Allowed only for loop indices (`for (let i = 0; i < items.length; i++)`) and iterator destructuring (`const [key, value]`).
- **Avoid generic names**: `data`, `item`, `value`, `thing`, `result`, `temp`. Use specific names.

### Comments
- **Write only WHY**, not WHAT. Code reads; comments explain intent.
- Delete comments that restate code: `// set isLoading to false` is noise.
- Delete commented-out code blocks. Git has history; dead code clutters.
- Max one line per comment; use code to say how.

### Efficiency
- **Guard clauses** over nested if/else: return early to reduce nesting.
- **async/await throughout**: No `.Result`, `.Subscribe()` blocking calls, or callback chains.
- **Remove dead code**: Unused imports, unreachable branches, stub methods.
- **Magic strings**: Extract to `const` or `environment` (e.g., `apiUrl`, `TOKEN_KEY`, `USER_KEY`).
- **Avoid premature optimization**, but fix O(n²) loops and repeated work.

---

## Conventions by Feature

### Writing Tests
- Use Vitest + TestBed for component/service tests.
- Mock HTTP via `HttpTestingController` for services.
- Test behavior (happy path, edge cases), not implementation details.
- Keep tests focused: one assertion per test if possible, or group related assertions.

### Adding New Features
1. Define DTOs in `core/models/` matching the API contract.
2. Add API methods to `ApiService` (or create a feature-specific service that wraps it).
3. Create the feature module and lazy-load it in `app-routing-module.ts`.
4. Use signals for component state; injected services for shared state.
5. Add auth guard if the feature requires authentication.

### Debugging
- Enable source maps in `ng serve` (default in development mode).
- Use Chrome DevTools to inspect Network, Console, and Angular DevTools extension.
- Check `environment.ts` for API URL mismatches.
- Test with real backend; mock only for unit tests (not integration tests).
