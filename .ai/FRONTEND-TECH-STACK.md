# Weather UI – TECH STACK

## Framework
- **Angular 18** (standalone components)
- **TypeScript** strict
- **RxJS** for streams and debouncing

## UI
- Angular Material (autocomplete, cards, progress)
- Simple responsive layout with CSS grid

## HTTP & State
- Angular `HttpClient`
- Stateless services (no global store needed)
- Error handling via RxJS catch and UI toasts

## Testing
- **Vitest** (or Jest) for unit tests
- Angular Testing Library for components

## Structure
- `app/components/city-search/`
- `app/components/weather-panel/`
- `app/components/weather-card/`
- `app/services/weather.service.ts`
- `app/models/*` (frontend types)
- `app/utils/weather-code.ts` (label/icon mapping)

## API Contracts (from backend)
- `GET /api/cities/search?q=`
  - `[{ name, country, latitude, longitude }]`
- `GET /api/weather?lat=&lon=&days=5`
  - see backend spec

## Dev
- `ng serve` at `http://localhost:4200`
- `.env` (if used) → `VITE_API_BASE` or Angular environment.ts
