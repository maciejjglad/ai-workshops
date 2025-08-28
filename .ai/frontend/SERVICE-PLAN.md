# Frontend WeatherService Integration Plan

This document outlines the integration strategy for the Angular 18 frontend WeatherService to communicate with the Weather Proxy API backend, following modern Angular patterns and best practices.

## Overview

The WeatherService acts as the primary interface between the Angular frontend and the Azure Functions backend API, providing a clean abstraction layer with comprehensive error handling, caching, and optimization strategies.

## Architecture Principles

- **Single Responsibility**: Service focused solely on API communication
- **Reactive Patterns**: RxJS observables for all asynchronous operations
- **Type Safety**: Full TypeScript integration with backend contract
- **Error Resilience**: Comprehensive error handling with user-friendly messages
- **Performance**: Strategic caching and debouncing for optimal UX
- **Testability**: Mockable interfaces and dependency injection

---

## Service Interface & Endpoints

### Primary Service Interface

```typescript
interface IWeatherService {
  // Core API methods
  searchCities(params: CitySearchParams): Observable<CitySearchResponse>;
  getWeather(params: WeatherParams): Observable<WeatherResponse>;
  
  // Cache management
  clearSearchCache(): void;
  clearWeatherCache(): void;
  
  // Configuration
  setBaseUrl(url: string): void;
  setCorrelationId(id: string): void;
}
```

### Endpoint Method Signatures

#### City Search Endpoint
```typescript
searchCities(params: CitySearchParams): Observable<CitySearchResponse> {
  // GET /api/cities/search
  // Query params: q, count?, language?
  // Headers: x-correlation-id?
  // Returns: CitySearchResponse with cities array
}

interface CitySearchParams {
  q: string;                    // Required: min 2 chars, max 100
  count?: number;               // Optional: 1-10, default 5
  language?: string;            // Optional: ISO 639-1, default "en"
}
```

#### Weather Forecast Endpoint
```typescript
getWeather(params: WeatherParams): Observable<WeatherResponse> {
  // GET /api/weather
  // Query params: lat, lon, days?
  // Headers: x-correlation-id?
  // Returns: WeatherResponse with location, current, daily, source
}

interface WeatherParams {
  lat: number;                  // Required: -90 to 90
  lon: number;                  // Required: -180 to 180
  days?: number;                // Optional: 1-7, default 5
}
```

#### HTTP Configuration Methods
```typescript
private buildCitySearchRequest(params: CitySearchParams): HttpParams;
private buildWeatherRequest(params: WeatherParams): HttpParams;
private buildHeaders(): HttpHeaders;
private generateCorrelationId(): string;
```

---

## Error Handling Strategy

### Error Classification & Response Strategy

#### Network & Connection Errors (0, TimeoutError)
- **Strategy**: Retry with exponential backoff
- **User Feedback**: Toast notification with retry option
- **Retry Logic**: 2 attempts for search, 1 attempt for weather
- **Fallback**: Show offline message, cache last successful response

#### Client Validation Errors (400)
- **Strategy**: No retry, immediate user feedback
- **User Feedback**: Inline validation messages in forms
- **Handling**: Parse RFC7807 ProblemDetails for field-specific errors
- **Fallback**: Generic validation message if parsing fails

#### Not Found Errors (404)
- **Strategy**: No retry, show empty state
- **User Feedback**: "No cities found" with search suggestions
- **Handling**: Differentiate between "no results" vs "endpoint not found"
- **Fallback**: Graceful empty state with helpful messaging

#### Rate Limiting (429)
- **Strategy**: Exponential backoff with jitter
- **User Feedback**: Toast with "Please wait" message
- **Handling**: Respect Retry-After header if present
- **Fallback**: Disable search temporarily with countdown

#### Server Errors (5xx)
- **Strategy**: Limited retry with circuit breaker pattern
- **User Feedback**: Toast notification with manual retry
- **Handling**: Distinguish between 500, 502, 503 for different retry strategies
- **Fallback**: Show last cached data if available

#### Upstream Service Errors (502)
- **Strategy**: No retry, show service status
- **User Feedback**: "Weather service temporarily unavailable"
- **Handling**: Parse backend error context for specific service info
- **Fallback**: Suggest trying again later with estimated time

### Error Handling Implementation

```typescript
private handleError(operation: string) {
  return (error: HttpErrorResponse): Observable<never> => {
    const userMessage = this.translateError(error, operation);
    
    // Log for debugging
    console.error(`${operation} failed:`, error);
    
    // Emit user-friendly error
    return throwError(() => new Error(userMessage));
  };
}

private translateError(error: HttpErrorResponse, operation: string): string {
  // Network errors
  if (error.status === 0 || error.name === 'TimeoutError') {
    return 'Connection problem. Please check your internet and try again.';
  }
  
  // Parse RFC7807 Problem Details
  if (error.error?.detail && typeof error.error.detail === 'string') {
    return error.error.detail;
  }
  
  // Fallback by status code
  const messages = {
    400: 'Invalid request. Please check your input.',
    404: operation.includes('search') 
      ? 'No cities found. Try a different search term.' 
      : 'Weather data not available for this location.',
    429: 'Too many requests. Please wait a moment.',
    502: 'Weather service temporarily unavailable. Please try again later.',
    503: 'Service maintenance in progress. Please try again shortly.'
  };
  
  return messages[error.status] || 'An unexpected error occurred. Please try again.';
}
```

### Toast Notification Strategy

- **Search Errors**: Inline in autocomplete component + toast for critical errors
- **Weather Errors**: Toast notification with retry button
- **Network Errors**: Persistent toast until connection restored
- **Rate Limiting**: Temporary toast with countdown timer
- **Server Errors**: Toast with manual retry option

---

## In-Memory Caching Strategy

### Cache Architecture

```typescript
interface CacheEntry<T> {
  data: T;
  timestamp: number;
  expiresAt: number;
  etag?: string;
}

interface WeatherServiceCache {
  citySearches: Map<string, CacheEntry<CitySearchResponse>>;
  weatherData: Map<string, CacheEntry<WeatherResponse>>;
  lastCorrelationIds: Map<string, string>;
}
```

### City Search Caching
- **Cache Key**: Normalized query string + language + count
- **TTL**: 30 minutes (searches change infrequently)
- **Size Limit**: 100 entries (LRU eviction)
- **Strategy**: Cache all successful responses
- **Invalidation**: Manual clear or TTL expiry

```typescript
private getCitySearchCacheKey(params: CitySearchParams): string {
  const normalized = params.q.toLowerCase().trim();
  return `${normalized}:${params.language || 'en'}:${params.count || 5}`;
}
```

### Weather Data Caching
- **Cache Key**: Lat/Lon coordinates + days parameter
- **TTL**: 10 minutes (weather data changes frequently)
- **Size Limit**: 50 entries (LRU eviction)
- **Strategy**: Cache successful responses only
- **Invalidation**: Manual refresh or TTL expiry

```typescript
private getWeatherCacheKey(params: WeatherParams): string {
  const lat = params.lat.toFixed(4); // 4 decimal precision
  const lon = params.lon.toFixed(4);
  return `${lat},${lon}:${params.days || 5}`;
}
```

### Cache Implementation Strategy
- **Storage**: In-memory Map for simplicity (no persistence needed)
- **Eviction**: Least Recently Used (LRU) algorithm
- **Preloading**: Cache popular cities on app bootstrap
- **Warming**: Background refresh before expiry for active data

### Cache Management Methods
```typescript
clearExpiredEntries(): void;
getCacheStats(): CacheStatistics;
preloadPopularCities(): Observable<void>;
warmCache(key: string): void;
```

---

## Debounce & Search Strategy

### Responsibility Distribution

#### Frontend Components (Owner of User Interaction)
- **CitySearchComponent**: Owns the debounce logic for user input
- **Debounce Period**: 300ms (balances responsiveness vs API calls)
- **Implementation**: RxJS `debounceTime` on form control `valueChanges`
- **Cancellation**: `switchMap` to cancel previous requests

```typescript
// In CitySearchComponent
this.searchControl.valueChanges.pipe(
  debounceTime(300),                    // Wait for pause in typing
  distinctUntilChanged(),               // Ignore duplicate values
  filter(query => query?.length >= 2), // Minimum length check
  switchMap(query => this.searchCities(query)) // Cancel previous
).subscribe(results => /* handle results */);
```

#### WeatherService (Owner of API Communication)
- **No Debouncing**: Service executes requests immediately when called
- **Caching**: Provides fast responses for repeated identical requests
- **Deduplication**: Prevents multiple concurrent requests for same parameters

```typescript
// In WeatherService - no debouncing, immediate execution
searchCities(params: CitySearchParams): Observable<CitySearchResponse> {
  // 1. Check cache first
  // 2. Execute request if cache miss
  // 3. Update cache with result
  // 4. Return observable
}
```

### Search Optimization Strategies

#### Request Deduplication
```typescript
private activeSearchRequests = new Map<string, Observable<CitySearchResponse>>();

searchCities(params: CitySearchParams): Observable<CitySearchResponse> {
  const cacheKey = this.getCitySearchCacheKey(params);
  
  // Return existing request if in progress
  if (this.activeSearchRequests.has(cacheKey)) {
    return this.activeSearchRequests.get(cacheKey)!;
  }
  
  // Check cache
  const cached = this.getCachedResponse(cacheKey);
  if (cached) {
    return of(cached);
  }
  
  // Create new request
  const request$ = this.executeSearchRequest(params).pipe(
    tap(response => this.setCacheEntry(cacheKey, response)),
    finalize(() => this.activeSearchRequests.delete(cacheKey)),
    share() // Share single request among multiple subscribers
  );
  
  this.activeSearchRequests.set(cacheKey, request$);
  return request$;
}
```

#### Progressive Search Enhancement
- **Minimum Character Length**: 2 characters before API call
- **Local Filtering**: Filter cached results first for instant feedback
- **Predictive Caching**: Cache likely next searches based on user input
- **Background Refresh**: Update cache in background for better UX

#### Search UX Optimizations
- **Instant Cache Results**: Show cached results immediately
- **Loading States**: Skeleton UI while fetching fresh data
- **Error Recovery**: Fall back to cached results if refresh fails
- **Search History**: Remember user's recent searches

---

## File Structure & Organization

### Service Files Layout
```
src/app/services/
├── weather.service.ts              # Main service implementation
├── weather-service.interface.ts    # Service contract definition
├── weather-cache.service.ts        # Cache management logic
├── weather-error.handler.ts        # Error handling utilities
├── weather.service.spec.ts         # Unit tests
└── interceptors/
    ├── correlation-id.interceptor.ts    # Auto-add correlation IDs
    ├── error-logging.interceptor.ts     # Error logging & metrics
    └── cache-headers.interceptor.ts     # HTTP cache headers
```

### Models & Types
```
src/app/models/
├── api-types.ts                    # Backend API contracts
├── view-models.ts                  # Frontend-specific view models
├── cache-types.ts                  # Cache-related interfaces
└── error-types.ts                  # Error handling types
```

### Utilities & Helpers
```
src/app/utils/
├── api-client.utils.ts             # HTTP client utilities
├── cache.utils.ts                  # Cache management helpers
├── error-mappers.ts                # Error transformation logic
└── request-builders.ts             # HTTP request building helpers
```

### Configuration
```
src/environments/
├── environment.ts                  # Development config
├── environment.prod.ts            # Production config
└── api-config.ts                  # API-specific configuration
```

---

## Testing Strategy

### Unit Testing Approach

#### Service Testing with TestBed
```typescript
describe('WeatherService', () => {
  let service: WeatherService;
  let httpMock: HttpTestingController;
  let cacheService: jasmine.SpyObj<WeatherCacheService>;

  beforeEach(() => {
    const cacheSpy = jasmine.createSpyObj('WeatherCacheService', 
      ['get', 'set', 'clear', 'has']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        WeatherService,
        { provide: WeatherCacheService, useValue: cacheSpy },
        { provide: 'API_CONFIG', useValue: mockApiConfig }
      ]
    });

    service = TestBed.inject(WeatherService);
    httpMock = TestBed.inject(HttpTestingController);
    cacheService = TestBed.inject(WeatherCacheService) as jasmine.SpyObj<WeatherCacheService>;
  });

  afterEach(() => {
    httpMock.verify(); // Ensure no outstanding requests
  });
});
```

#### Test Categories

**Happy Path Testing**
- Successful API responses with various parameters
- Cache hit scenarios with fresh data
- Proper HTTP header construction
- Correct parameter serialization

**Error Scenario Testing**
- Network failures and timeout handling
- HTTP error status codes (400, 404, 429, 5xx)
- Malformed response handling
- Cache miss scenarios

**Edge Case Testing**
- Empty search results
- Invalid coordinates
- Concurrent request handling
- Cache expiry boundary conditions

**Performance Testing**
- Request deduplication verification
- Cache efficiency measurements
- Memory leak detection
- Large response handling

#### Mock Strategies
```typescript
// HTTP Response Mocking
const mockCityResponse: CitySearchResponse = {
  cities: [
    { name: 'London', country: 'UK', latitude: 51.5, longitude: -0.1 }
  ]
};

// Error Response Mocking
const mockErrorResponse: ProblemDetails = {
  type: 'https://example.com/problems/validation-error',
  title: 'Validation Failed',
  status: 400,
  detail: 'Search query too short'
};

// Service Method Testing
it('should search cities successfully', () => {
  service.searchCities({ q: 'London' }).subscribe(response => {
    expect(response).toEqual(mockCityResponse);
  });

  const req = httpMock.expectOne(request => 
    request.url.includes('/cities/search') && 
    request.params.get('q') === 'London'
  );
  
  expect(req.request.method).toBe('GET');
  req.flush(mockCityResponse);
});
```

### Integration Testing

#### Component Integration Tests
```typescript
describe('WeatherService Integration', () => {
  let component: CitySearchComponent;
  let weatherService: WeatherService;
  
  it('should handle debounced search with real service', fakeAsync(() => {
    // Test real debouncing behavior with service integration
    component.searchControl.setValue('Lond');
    tick(300); // Wait for debounce
    
    expect(weatherService.searchCities).toHaveBeenCalledWith({
      q: 'Lond',
      count: 5
    });
  }));
});
```

#### API Contract Testing
- Verify request/response formats match OpenAPI spec
- Test all documented error scenarios
- Validate header handling (correlation IDs, content types)
- Ensure proper HTTP method usage

### End-to-End Testing Strategy

#### User Journey Testing
- Complete search-to-weather flow
- Error recovery scenarios
- Cache behavior validation
- Performance characteristics

#### Test Data Management
- Mock API server for consistent testing
- Predefined test datasets for different scenarios
- Error injection capabilities for resilience testing

### Testing Tools & Configuration

**Primary Testing Stack**
- **Vitest**: Fast unit test runner
- **Angular Testing Library**: Component testing utilities
- **MSW (Mock Service Worker)**: API mocking for integration tests
- **Testing Library Jest DOM**: Extended matchers

**Test Configuration**
```typescript
// vitest.config.ts
export default defineConfig({
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['src/test/setup.ts'],
    coverage: {
      reporter: ['text', 'lcov', 'html'],
      threshold: {
        global: {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80
        }
      }
    }
  }
});
```

### Continuous Integration Testing

#### Automated Test Pipeline
- Unit tests run on every commit
- Integration tests on pull requests
- E2E tests on release candidates
- Performance regression testing

#### Quality Gates
- Minimum 80% code coverage
- All error scenarios tested
- API contract compliance verified
- Performance benchmarks met

---

## Implementation Checklist

### Core Service Implementation
- [ ] Basic HTTP client setup with proper typing
- [ ] Error handling with RFC7807 support
- [ ] Correlation ID generation and header injection
- [ ] Request parameter validation and building
- [ ] Response transformation and type safety

### Caching Implementation
- [ ] In-memory cache with TTL support
- [ ] LRU eviction strategy
- [ ] Cache key generation and normalization
- [ ] Cache statistics and monitoring
- [ ] Preloading and warming strategies

### Error Handling
- [ ] Comprehensive error classification
- [ ] User-friendly message translation
- [ ] Retry logic with exponential backoff
- [ ] Circuit breaker for resilience
- [ ] Offline scenario handling

### Performance Optimization
- [ ] Request deduplication
- [ ] Concurrent request management
- [ ] Background cache refresh
- [ ] Memory usage optimization
- [ ] Bundle size optimization

### Testing Suite
- [ ] Unit tests for all service methods
- [ ] Error scenario coverage
- [ ] Cache behavior validation
- [ ] Integration tests with components
- [ ] Performance benchmark tests

### Documentation & Maintenance
- [ ] API documentation with examples
- [ ] Error handling guide
- [ ] Performance tuning guide
- [ ] Debugging and troubleshooting guide
- [ ] Migration guide for future versions

This comprehensive service plan ensures a robust, performant, and maintainable integration between the Angular frontend and the Weather Proxy API backend, following Angular 18 best practices and modern web development standards.
