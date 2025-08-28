# Frontend TypeScript Types

This file contains all TypeScript interfaces and enums for the Weather UI Angular application, derived from the OpenAPI specification and frontend requirements.

## API Types

### City Search

```typescript
// City Search Request Parameters
export interface CitySearchParams {
  q: string;                    // City name to search (min 2 chars)
  count?: number;              // Max results (1-10, default: 5)
  language?: string;           // Language code (ISO 639-1, default: "en")
}

// City Search Response
export interface CitySearchResponse {
  cities: CityResult[];
}

export interface CityResult {
  name: string;                // City name
  country: string;             // Country name
  latitude: number;            // Latitude (-90 to 90)
  longitude: number;           // Longitude (-180 to 180)
  region?: string;             // Administrative region/state/province
  population?: number;         // Population count
}
```

### Weather Forecast

```typescript
// Weather Request Parameters
export interface WeatherParams {
  lat: number;                 // Latitude (-90 to 90)
  lon: number;                 // Longitude (-180 to 180)
  days?: number;               // Forecast days (1-7, default: 5)
}

// Weather Response
export interface WeatherResponse {
  location: LocationData;
  current: CurrentWeatherData;
  daily: DailyWeatherData[];
  source: SourceInfo;
}

export interface LocationData {
  name: string;                // Location name (city or region)
  country: string;             // Country name
  latitude: number;            // Latitude in decimal degrees
  longitude: number;           // Longitude in decimal degrees
  timezone: string;            // IANA timezone identifier
}

export interface CurrentWeatherData {
  time: string;                // Current observation time (ISO 8601)
  temperatureC: number;        // Temperature in Celsius (1 decimal)
  windSpeedKph: number;        // Wind speed in km/h (1 decimal)
  weatherCode: number;         // WMO weather interpretation code (0-99)
  isDay: boolean;              // Whether it's currently day or night
  condition: string;           // Human-readable weather condition
  icon: string;                // Weather icon code (pattern: '^[0-9]{2}[dn]$')
}

export interface DailyWeatherData {
  date: string;                // Forecast date (YYYY-MM-DD)
  temperatureMaxC: number;     // Maximum daily temperature in Celsius
  temperatureMinC: number;     // Minimum daily temperature in Celsius
  precipitationProbabilityPct: number; // Max precipitation probability (0-100)
  windSpeedMaxKph: number;     // Maximum daily wind speed in km/h
  weatherCode: number;         // WMO weather interpretation code (0-99)
  condition: string;           // Human-readable weather condition
  icon: string;                // Weather icon code (pattern: '^[0-9]{2}[dn]$')
}

export interface SourceInfo {
  provider: string;            // Weather data provider (e.g., "open-meteo")
  model: string;               // Weather model used (e.g., "best_match")
}
```

### Error Handling

```typescript
// RFC7807 Problem Details for API errors
export interface ProblemDetails {
  type: string;                // Problem type identifier (URI)
  title: string;               // Short, human-readable summary
  status: number;              // HTTP status code (100-599)
  detail?: string;             // Human-readable explanation
  instance?: string;           // URI reference for this specific problem
  traceId?: string;            // Unique trace identifier
  correlationId?: string;      // Correlation ID for request tracing (UUID)
  timestamp?: string;          // Error occurrence timestamp (ISO 8601)
  errors?: Record<string, string[]>; // Validation errors by field
  context?: Record<string, any>; // Additional context information
}
```

## View Models

### Component Input/Output Types

```typescript
// For WeatherPanelComponent input
export interface SelectedCity {
  lat: number;
  lon: number;
  name: string;
  country: string;
}

// Current weather view model (optimized for UI display)
export interface CurrentWeatherView {
  temperature: string;         // Formatted temp with unit (e.g., "15.2°C")
  condition: string;           // Human-readable condition
  icon: string;                // Weather icon code
  windSpeed: string;           // Formatted wind speed (e.g., "12.6 km/h")
  localTime: string;           // Formatted local time
  location: string;            // Formatted location (e.g., "London, United Kingdom")
}

// Daily forecast view model (optimized for WeatherCardComponent)
export interface DailyWeatherView {
  date: string;                // Formatted date (e.g., "Mon, Jan 15")
  dayName: string;             // Day name (e.g., "Monday" or "Today")
  temperatureRange: string;    // Formatted temp range (e.g., "8° / 18°")
  temperatureMax: string;      // Formatted max temp (e.g., "18°")
  temperatureMin: string;      // Formatted min temp (e.g., "8°")
  condition: string;           // Human-readable condition
  icon: string;                // Weather icon code
  precipitationChance: string; // Formatted precipitation (e.g., "20%")
  windSpeed: string;           // Formatted max wind speed (e.g., "15.8 km/h")
  weatherCode: number;         // Raw weather code for styling/logic
}
```

### Loading and State Management

```typescript
// Loading states for async operations
export type LoadingState = 'idle' | 'loading' | 'success' | 'error';

// City search state
export interface CitySearchState {
  loading: LoadingState;
  cities: CityResult[];
  error: string | null;
  selectedCity: SelectedCity | null;
}

// Weather data state
export interface WeatherState {
  loading: LoadingState;
  data: WeatherResponse | null;
  error: string | null;
  lastUpdated: Date | null;
}

// Application state
export interface AppState {
  citySearch: CitySearchState;
  weather: WeatherState;
  preferences: UserPreferences;
}

// User preferences (for stretch features)
export interface UserPreferences {
  temperatureUnit: TemperatureUnit;
  language: string;
  lastSelectedCity?: SelectedCity;
}
```

## Enums and Constants

### Temperature Units

```typescript
export enum TemperatureUnit {
  CELSIUS = 'C',
  FAHRENHEIT = 'F'
}

export const TEMPERATURE_UNIT_LABELS: Record<TemperatureUnit, string> = {
  [TemperatureUnit.CELSIUS]: '°C',
  [TemperatureUnit.FAHRENHEIT]: '°F'
};
```

### Weather Code Mapping

```typescript
// WMO Weather Interpretation Codes mapping
// Based on Open-Meteo API documentation
export interface WeatherCodeInfo {
  condition: string;
  icon: string;
  description: string;
}

export const WEATHER_CODE_MAP: Record<number, WeatherCodeInfo> = {
  // Clear Sky
  0: {
    condition: 'Clear',
    icon: '01', // Will be suffixed with 'd' or 'n'
    description: 'Clear sky'
  },
  
  // Mainly Clear, Partly Cloudy, Overcast
  1: {
    condition: 'Mostly Clear',
    icon: '02',
    description: 'Mainly clear'
  },
  2: {
    condition: 'Partly Cloudy',
    icon: '03',
    description: 'Partly cloudy'
  },
  3: {
    condition: 'Overcast',
    icon: '04',
    description: 'Overcast'
  },
  
  // Fog
  45: {
    condition: 'Foggy',
    icon: '50',
    description: 'Fog'
  },
  48: {
    condition: 'Foggy',
    icon: '50',
    description: 'Depositing rime fog'
  },
  
  // Drizzle
  51: {
    condition: 'Light Drizzle',
    icon: '09',
    description: 'Drizzle: Light intensity'
  },
  53: {
    condition: 'Drizzle',
    icon: '09',
    description: 'Drizzle: Moderate intensity'
  },
  55: {
    condition: 'Heavy Drizzle',
    icon: '09',
    description: 'Drizzle: Dense intensity'
  },
  
  // Freezing Drizzle
  56: {
    condition: 'Freezing Drizzle',
    icon: '09',
    description: 'Freezing Drizzle: Light intensity'
  },
  57: {
    condition: 'Freezing Drizzle',
    icon: '09',
    description: 'Freezing Drizzle: Dense intensity'
  },
  
  // Rain
  61: {
    condition: 'Light Rain',
    icon: '10',
    description: 'Rain: Slight intensity'
  },
  63: {
    condition: 'Rain',
    icon: '10',
    description: 'Rain: Moderate intensity'
  },
  65: {
    condition: 'Heavy Rain',
    icon: '10',
    description: 'Rain: Heavy intensity'
  },
  
  // Freezing Rain
  66: {
    condition: 'Freezing Rain',
    icon: '10',
    description: 'Freezing Rain: Light intensity'
  },
  67: {
    condition: 'Freezing Rain',
    icon: '10',
    description: 'Freezing Rain: Heavy intensity'
  },
  
  // Snow
  71: {
    condition: 'Light Snow',
    icon: '13',
    description: 'Snow fall: Slight intensity'
  },
  73: {
    condition: 'Snow',
    icon: '13',
    description: 'Snow fall: Moderate intensity'
  },
  75: {
    condition: 'Heavy Snow',
    icon: '13',
    description: 'Snow fall: Heavy intensity'
  },
  
  // Snow Grains
  77: {
    condition: 'Snow Grains',
    icon: '13',
    description: 'Snow grains'
  },
  
  // Rain Showers
  80: {
    condition: 'Light Showers',
    icon: '09',
    description: 'Rain showers: Slight intensity'
  },
  81: {
    condition: 'Showers',
    icon: '09',
    description: 'Rain showers: Moderate intensity'
  },
  82: {
    condition: 'Heavy Showers',
    icon: '09',
    description: 'Rain showers: Violent intensity'
  },
  
  // Snow Showers
  85: {
    condition: 'Snow Showers',
    icon: '13',
    description: 'Snow showers: Slight intensity'
  },
  86: {
    condition: 'Heavy Snow Showers',
    icon: '13',
    description: 'Snow showers: Heavy intensity'
  },
  
  // Thunderstorms
  95: {
    condition: 'Thunderstorm',
    icon: '11',
    description: 'Thunderstorm: Slight or moderate'
  },
  96: {
    condition: 'Thunderstorm with Hail',
    icon: '11',
    description: 'Thunderstorm with slight hail'
  },
  99: {
    condition: 'Severe Thunderstorm',
    icon: '11',
    description: 'Thunderstorm with heavy hail'
  }
};

// Helper function to get weather info with day/night icon
export function getWeatherInfo(code: number, isDay: boolean): WeatherCodeInfo {
  const info = WEATHER_CODE_MAP[code];
  if (!info) {
    return {
      condition: 'Unknown',
      icon: '01d',
      description: 'Unknown weather condition'
    };
  }
  
  return {
    ...info,
    icon: `${info.icon}${isDay ? 'd' : 'n'}`
  };
}
```

### API Configuration

```typescript
export const API_CONFIG = {
  BASE_URL: 'http://localhost:7071/api',
  ENDPOINTS: {
    CITY_SEARCH: '/cities/search',
    WEATHER: '/weather'
  },
  DEFAULTS: {
    SEARCH_COUNT: 5,
    FORECAST_DAYS: 5,
    LANGUAGE: 'en'
  },
  TIMEOUTS: {
    SEARCH_DEBOUNCE_MS: 300,
    REQUEST_TIMEOUT_MS: 10000
  }
} as const;
```

### Validation

```typescript
// Input validation constraints
export const VALIDATION = {
  CITY_SEARCH: {
    MIN_LENGTH: 2,
    MAX_LENGTH: 100,
    PATTERN: /^[\p{L}\p{N}\s\-''.]+$/u
  },
  COORDINATES: {
    LAT_MIN: -90,
    LAT_MAX: 90,
    LON_MIN: -180,
    LON_MAX: 180
  },
  FORECAST: {
    MIN_DAYS: 1,
    MAX_DAYS: 7
  }
} as const;

// Type guards
export function isCityResult(obj: any): obj is CityResult {
  return obj && 
    typeof obj.name === 'string' &&
    typeof obj.country === 'string' &&
    typeof obj.latitude === 'number' &&
    typeof obj.longitude === 'number';
}

export function isWeatherResponse(obj: any): obj is WeatherResponse {
  return obj &&
    obj.location &&
    obj.current &&
    Array.isArray(obj.daily) &&
    obj.source;
}
```

## Usage Examples

### Service Implementation Types

```typescript
// Weather Service interface
export interface IWeatherService {
  searchCities(params: CitySearchParams): Observable<CitySearchResponse>;
  getWeather(params: WeatherParams): Observable<WeatherResponse>;
}

// HTTP interceptor types
export interface CorrelationIdContext {
  correlationId: string;
}

// Component event types
export interface CitySelectedEvent {
  city: CityResult;
}

export interface WeatherRefreshEvent {
  location: SelectedCity;
}
```

### Form Types

```typescript
// Reactive form interfaces
export interface CitySearchForm {
  searchQuery: string;
}

export interface WeatherPreferencesForm {
  temperatureUnit: TemperatureUnit;
  language: string;
  autoRefresh: boolean;
}
```

---

## Notes

- All API types are derived from the OpenAPI 3.0 specification
- View models optimize data for UI consumption and formatting
- Weather codes follow WMO standards as implemented by Open-Meteo
- Icon codes are compatible with OpenWeatherMap icon sets
- All interfaces support strict TypeScript compilation
- Optional properties are marked with `?` for proper null checking
- Enums provide type safety for predefined values
