# Weather Proxy API – SPEC

## Business Goal
Provide a simple backend that:
1) Resolves a city name to coordinates.
2) Fetches a compact weather forecast.
3) Normalizes the response for a consumer UI.

## Users & Scenarios (pure business)
- **Traveler checks forecast for a destination**  
  Enters “Kraków” → gets current conditions and 5-day outlook.
- **City name ambiguity**  
  Entering “Springfield” returns a **collection** of likely matches (top 5) with `name`, `country`, `latitude`, `longitude`. The consumer selects the correct one, then requests weather.

## Scope (MVP)
- **GET** `/api/cities/search?q={name}`  
  - Returns up to 5 matches: `{ name, country, latitude, longitude, timezone? }`
- **GET** `/api/weather?lat={lat}&lon={lon}&days=5`  
  - Returns:
    - `location`: `{ name, country, latitude, longitude, timezone }`
    - `current`: `{ time, temperatureC, windSpeedKph, weatherCode, isDay }`
    - `daily[]` (length = `days`): `{ date, tMaxC, tMinC, precipitationProbPct, windMaxKph, weatherCode }`
    - `source`: `{ provider: "open-meteo", model?: string }`

## Non-Goals
- Historical data, alerts, air quality, maps, persistence/caching.
- Multi-language labels.
- Authentication/authorization.

## Functional Requirements
- City search delegates to Open-Meteo Geocoding.
- Forecast delegates to Open-Meteo Forecast (hourly/daily variables).
- Map external fields → internal DTOs that are lean and UI-friendly.
- Validate inputs; return RFC7807 ProblemDetails on errors.

## Quality Attributes
- **Reliability**: short timeouts, limited retries for transient failures.
- **Performance**: response < 800ms in dev env, minimal payload.
- **Observability**: structured logs with correlation id.
- **Security**: no secrets required; CORS configured for local dev.

## Error Handling
- 400: invalid query (missing/invalid `q`, `lat`, `lon`, `days`).
- 404: city not found (empty search results).
- 502: upstream error from provider (include `traceId`).
- All errors use RFC7807 `ProblemDetails`.

## Acceptance Criteria
- Given `q="Kraków"`, `/api/cities/search` yields at least one Polish city with lat/lon.
- Given valid `lat/lon`, `/api/weather` returns `current` and `daily` with 5 items.
- Fields and units match schema; unknown fields omitted.
- CORS allows `http://localhost:4200`.

## Stretch Ideas
- `?units=metric|imperial`.
- Optional backend label mapping for `weatherCode` → human label.
