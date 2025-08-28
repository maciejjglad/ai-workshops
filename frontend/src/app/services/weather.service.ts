import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of, EMPTY } from 'rxjs';
import { 
  catchError, 
  timeout, 
  retry, 
  retryWhen, 
  switchMap, 
  tap, 
  finalize,
  share,
  map,
  delay,
  take
} from 'rxjs/operators';

import {
  CitySearchParams,
  CitySearchResponse,
  WeatherParams,
  WeatherResponse,
  IWeatherService,
  API_CONFIG
} from '../models/types';

import { WeatherCacheService } from './weather-cache.service';

import {
  generateCorrelationId,
  buildApiHeaders,
  buildCitySearchParams,
  buildWeatherParams,
  generateCitySearchCacheKey,
  generateWeatherCacheKey,
  validateCitySearchParams,
  validateWeatherParams,
  createRequestId
} from '../utils/api-helpers';

import {
  processHttpError,
  createErrorHandler,
  shouldRetryError,
  createRetryObservable,
  ProcessedError,
  ErrorType
} from '../utils/error-handlers';

/**
 * WeatherService provides HTTP communication with the Weather Proxy API
 * Features: caching, error handling, request deduplication, retry logic
 */
@Injectable({
  providedIn: 'root'
})
export class WeatherService implements IWeatherService {
  private readonly http = inject(HttpClient);
  private readonly cacheService = inject(WeatherCacheService);
  
  // Configuration
  private baseUrl: string = API_CONFIG.BASE_URL;
  private requestTimeout: number = API_CONFIG.TIMEOUTS.REQUEST_TIMEOUT_MS;
  
  // Request deduplication maps
  private activeSearchRequests = new Map<string, Observable<CitySearchResponse>>();
  private activeWeatherRequests = new Map<string, Observable<WeatherResponse>>();

  /**
   * Search for cities by name with caching and deduplication
   */
  searchCities(params: CitySearchParams): Observable<CitySearchResponse> {
    // Validate parameters
    const validation = validateCitySearchParams(params);
    if (!validation.isValid) {
      return throwError(() => new Error(validation.errors.join('; ')));
    }

    const cacheKey = generateCitySearchCacheKey(params);
    
    // Check for active request
    if (this.activeSearchRequests.has(cacheKey)) {
      return this.activeSearchRequests.get(cacheKey)!;
    }
    
    // Check cache first
    const cachedResponse = this.cacheService.getCitySearch(cacheKey);
    if (cachedResponse) {
      return of(cachedResponse);
    }
    
    // Create new request
    const correlationId = generateCorrelationId();
    const request$ = this.executeCitySearchRequest(params, correlationId).pipe(
      tap(response => {
        this.cacheService.setCitySearch(cacheKey, response);
        this.cacheService.setCorrelationId(cacheKey, correlationId);
      }),
      finalize(() => this.activeSearchRequests.delete(cacheKey)),
      share() // Share among multiple subscribers
    );
    
    this.activeSearchRequests.set(cacheKey, request$);
    return request$;
  }

  /**
   * Get weather forecast for coordinates with caching and deduplication
   */
  getWeather(params: WeatherParams): Observable<WeatherResponse> {
    // Validate parameters
    const validation = validateWeatherParams(params);
    if (!validation.isValid) {
      return throwError(() => new Error(validation.errors.join('; ')));
    }

    const cacheKey = generateWeatherCacheKey(params);
    
    // Check for active request
    if (this.activeWeatherRequests.has(cacheKey)) {
      return this.activeWeatherRequests.get(cacheKey)!;
    }
    
    // Check cache first
    const cachedResponse = this.cacheService.getWeatherData(cacheKey);
    if (cachedResponse) {
      return of(cachedResponse);
    }
    
    // Create new request
    const correlationId = generateCorrelationId();
    const request$ = this.executeWeatherRequest(params, correlationId).pipe(
      tap(response => {
        this.cacheService.setWeatherData(cacheKey, response);
        this.cacheService.setCorrelationId(cacheKey, correlationId);
      }),
      finalize(() => this.activeWeatherRequests.delete(cacheKey)),
      share() // Share among multiple subscribers
    );
    
    this.activeWeatherRequests.set(cacheKey, request$);
    return request$;
  }

  /**
   * Execute city search HTTP request with retry logic
   */
  private executeCitySearchRequest(params: CitySearchParams, correlationId: string): Observable<CitySearchResponse> {
    const url = `${this.baseUrl}${API_CONFIG.ENDPOINTS.CITY_SEARCH}`;
    const httpParams = buildCitySearchParams(params);
    const headers = buildApiHeaders(correlationId);
    
    return this.http.get<CitySearchResponse>(url, { params: httpParams, headers }).pipe(
      timeout(this.requestTimeout),
      retryWhen(errors => this.createRetryStrategy('searchCities', 2)(errors)),
      catchError(createErrorHandler('searchCities'))
    );
  }

  /**
   * Execute weather request with retry logic
   */
  private executeWeatherRequest(params: WeatherParams, correlationId: string): Observable<WeatherResponse> {
    const url = `${this.baseUrl}${API_CONFIG.ENDPOINTS.WEATHER}`;
    const httpParams = buildWeatherParams(params);
    const headers = buildApiHeaders(correlationId);
    
    return this.http.get<any>(url, { params: httpParams, headers }).pipe(
      map(response => this.transformWeatherResponse(response)),
      timeout(this.requestTimeout),
      retryWhen(errors => this.createRetryStrategy('getWeather', 1)(errors)),
      catchError(createErrorHandler('getWeather'))
    );
  }

  /**
   * Create retry strategy with exponential backoff
   */
  private createRetryStrategy(operation: string, maxRetries: number) {
    return (errors: Observable<HttpErrorResponse>) => {
      return errors.pipe(
        switchMap((error: HttpErrorResponse, attempt: number) => {
          const processedError = processHttpError(error, operation);
          
          if (shouldRetryError(processedError, attempt, maxRetries)) {
            console.log(`Retrying ${operation} (attempt ${attempt + 1}/${maxRetries + 1})`);
            return createRetryObservable(attempt);
          }
          
          // Don't retry, propagate error
          return throwError(() => error);
        }),
        take(maxRetries) // Limit total retry attempts
      );
    };
  }

  /**
   * Clear search cache
   */
  clearSearchCache(): void {
    this.cacheService.clearCitySearchCache();
  }

  /**
   * Clear weather cache
   */
  clearWeatherCache(): void {
    this.cacheService.clearWeatherDataCache();
  }

  /**
   * Clear all caches
   */
  clearAllCaches(): void {
    this.cacheService.clearAllCaches();
  }

  /**
   * Set base URL for API requests
   */
  setBaseUrl(url: string): void {
    this.baseUrl = url.endsWith('/') ? url.slice(0, -1) : url;
  }

  /**
   * Set request timeout
   */
  setRequestTimeout(timeoutMs: number): void {
    this.requestTimeout = timeoutMs;
  }

  /**
   * Get cache statistics
   */
  getCacheStatistics() {
    return this.cacheService.getCacheStatistics();
  }

  /**
   * Preload popular cities for better UX
   */
  preloadPopularCities(): Observable<void> {
    const popularCities = [
      { q: 'London' },
      { q: 'New York' },
      { q: 'Tokyo' },
      { q: 'Paris' },
      { q: 'Berlin' }
    ];

    const requests = popularCities.map(params => 
      this.searchCities(params).pipe(
        catchError(() => of({ cities: [] })) // Ignore errors for preloading
      )
    );

    return new Observable(subscriber => {
      Promise.all(requests.map(req => req.toPromise()))
        .then(() => {
          subscriber.next();
          subscriber.complete();
        })
        .catch(error => {
          console.warn('Preloading popular cities failed:', error);
          subscriber.next(); // Complete successfully even if preloading fails
          subscriber.complete();
        });
    });
  }

  /**
   * Warm cache by refreshing data in background
   */
  warmCache(cacheKey: string): void {
    // This is a fire-and-forget operation
    if (cacheKey.startsWith('city:')) {
      // Extract params from cache key and refresh
      const parts = cacheKey.split(':');
      if (parts.length >= 4) {
        const params: CitySearchParams = {
          q: parts[1],
          language: parts[2],
          count: parseInt(parts[3])
        };
        
        this.executeCitySearchRequest(params, generateCorrelationId()).pipe(
          tap(response => this.cacheService.setCitySearch(cacheKey, response)),
          catchError(() => EMPTY) // Ignore errors for background refresh
        ).subscribe();
      }
    } else if (cacheKey.startsWith('weather:')) {
      // Extract params from cache key and refresh
      const parts = cacheKey.split(':');
      if (parts.length >= 3) {
        const [lat, lon] = parts[1].split(',').map(Number);
        const days = parseInt(parts[2]);
        
        const params: WeatherParams = { lat, lon, days };
        
        this.executeWeatherRequest(params, generateCorrelationId()).pipe(
          tap(response => this.cacheService.setWeatherData(cacheKey, response)),
          catchError(() => EMPTY) // Ignore errors for background refresh
        ).subscribe();
      }
    }
  }

  /**
   * Transform weather API response to match frontend interface
   */
  private transformWeatherResponse(response: any): WeatherResponse {
    
    // Transform API response to match our interface (handle potential case mismatches)
    return {
      location: {
        name: response.location?.name || response.Location?.Name || '',
        country: response.location?.country || response.Location?.Country || '',
        latitude: response.location?.latitude || response.Location?.Latitude || 0,
        longitude: response.location?.longitude || response.Location?.Longitude || 0,
        timezone: response.location?.timezone || response.Location?.Timezone || ''
      },
      current: {
        time: response.current?.time || response.Current?.Time || '',
        temperatureC: response.current?.temperatureC || response.current?.temperature_2m || response.Current?.TemperatureC || 0,
        windSpeedKph: response.current?.windSpeedKph || response.current?.wind_speed_10m || response.Current?.WindSpeedKph || 0,
        weatherCode: response.current?.weatherCode || response.current?.weather_code || response.Current?.WeatherCode || 0,
        isDay: response.current?.isDay ?? response.current?.is_day ?? response.Current?.IsDay ?? true,
        condition: response.current?.condition || response.Current?.Condition || '',
        icon: response.current?.icon || response.Current?.Icon || ''
      },
      daily: (response.daily || response.Daily || []).map((day: any) => ({
        date: day.date || day.Date || '',
        temperatureMaxC: day.temperatureMaxC || day.temperature_2m_max || day.TemperatureMaxC || 0,
        temperatureMinC: day.temperatureMinC || day.temperature_2m_min || day.TemperatureMinC || 0,
        precipitationProbabilityPct: day.precipitationProbabilityPct || day.precipitation_probability_max || day.PrecipitationProbabilityPct || 0,
        windSpeedMaxKph: day.windSpeedMaxKph || day.wind_speed_10m_max || day.WindSpeedMaxKph || 0,
        weatherCode: day.weatherCode || day.weather_code || day.WeatherCode || 0,
        condition: day.condition || day.Condition || '',
        icon: day.icon || day.Icon || ''
      })),
      source: {
        provider: response.source?.provider || response.Source?.Provider || 'unknown',
        model: response.source?.model || response.Source?.Model || 'unknown'
      }
    };
  }

  /**
   * Check service health
   */
  checkHealth(): Observable<boolean> {
    const healthUrl = `${this.baseUrl}/health`;
    
    return this.http.get(healthUrl, { 
      headers: buildApiHeaders(),
      responseType: 'text'
    }).pipe(
      timeout(5000), // Short timeout for health check
      map(() => true),
      catchError(() => of(false))
    );
  }

  /**
   * Get current configuration
   */
  getConfiguration() {
    return {
      baseUrl: this.baseUrl,
      timeout: this.requestTimeout,
      endpoints: API_CONFIG.ENDPOINTS,
      defaults: API_CONFIG.DEFAULTS
    };
  }

  /**
   * Clear expired cache entries (maintenance operation)
   */
  performCacheMaintenance(): void {
    this.cacheService.clearExpiredEntries();
  }
}