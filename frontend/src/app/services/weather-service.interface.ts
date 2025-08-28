import { Observable } from 'rxjs';
import { CitySearchParams, CitySearchResponse, WeatherParams, WeatherResponse } from '../models/types';
import { CacheStatistics } from './weather-cache.service';

/**
 * Interface defining the contract for WeatherService
 * Useful for testing and dependency injection
 */
export interface IWeatherService {
  // Core API methods
  searchCities(params: CitySearchParams): Observable<CitySearchResponse>;
  getWeather(params: WeatherParams): Observable<WeatherResponse>;
  
  // Cache management
  clearSearchCache(): void;
  clearWeatherCache(): void;
  clearAllCaches(): void;
  getCacheStatistics(): CacheStatistics;
  
  // Configuration
  setBaseUrl(url: string): void;
  setRequestTimeout(timeoutMs: number): void;
  
  // Maintenance and utilities
  preloadPopularCities(): Observable<void>;
  warmCache(cacheKey: string): void;
  checkHealth(): Observable<boolean>;
  performCacheMaintenance(): void;
  getConfiguration(): ServiceConfiguration;
}

/**
 * Service configuration interface
 */
export interface ServiceConfiguration {
  baseUrl: string;
  timeout: number;
  endpoints: {
    CITY_SEARCH: string;
    WEATHER: string;
  };
  defaults: {
    SEARCH_COUNT: number;
    FORECAST_DAYS: number;
    LANGUAGE: string;
  };
}

/**
 * Extended interface for testing with additional debugging methods
 */
export interface IWeatherServiceDebug extends IWeatherService {
  // Internal state access for testing
  getActiveRequestCount(): number;
  getCacheKeys(): string[];
  isRequestActive(cacheKey: string): boolean;
  
  // Force methods for testing
  forceExpireCacheEntry(cacheKey: string): void;
  forceClearActiveRequests(): void;
}
