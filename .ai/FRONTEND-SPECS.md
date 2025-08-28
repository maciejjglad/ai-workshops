# Weather UI – SPEC

## Business Goal
Allow a user to search a city, pick one, and view “Now + Next 5 days” at a glance.

## UX Flow
1) User types in a city name → autocomplete list (top 5).
2) On selection → page shows:
   - Current: temperature, wind, simple condition (icon/label), local time
   - Next 5 days: cards with date, max/min temp, precip prob, max wind, condition

## Pages / Components (Angular standalone)
- `CitySearchComponent`
  - Input box + debounced search (300ms)
  - Displays top 5 suggestions from **backend** `/api/cities/search`
- `WeatherPanelComponent`
  - Accepts `{ lat, lon, name, country }`
  - Calls **backend** `/api/weather?lat=&lon=&days=5`
  - Renders current and daily forecast
- `WeatherCardComponent`
  - Displays a single day’s info
- `WeatherService` (frontend)
  - Wraps HTTP calls to backend endpoints

## Accessibility
- Keyboard navigation for search results
- ARIA labels on inputs, buttons, and list items

## Acceptance Criteria
- Typing “Krak” shows “Kraków, PL” in suggestions
- Selecting first suggestion shows a 5-day panel
- Loading and error states are visible and localized
- Mobile: layout stacks; cards wrap

## Non-Goals
- Map views, favorites, offline mode

## Stretch
- Persist last selection in `localStorage`
- Unit toggle °C/°F (frontend only)
- Basic weather icons by code
