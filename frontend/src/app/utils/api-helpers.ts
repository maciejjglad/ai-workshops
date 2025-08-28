import { HttpParams, HttpHeaders } from '@angular/common/http';
import { CitySearchParams, WeatherParams, VALIDATION } from '../models/types';

/**
 * Pure helper functions for API request building and validation
 */

/**
 * Generate a UUID v4 correlation ID for request tracking
 */
export function generateCorrelationId(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

/**
 * Build HTTP headers with correlation ID and content type
 */
export function buildApiHeaders(correlationId?: string): HttpHeaders {
  let headers = new HttpHeaders({
    'Content-Type': 'application/json'
  });
  
  if (correlationId) {
    headers = headers.set('x-correlation-id', correlationId);
  }
  
  return headers;
}

/**
 * Build HTTP parameters for city search endpoint
 */
export function buildCitySearchParams(params: CitySearchParams): HttpParams {
  let httpParams = new HttpParams()
    .set('q', params.q.trim());
  
  if (params.count !== undefined && params.count > 0) {
    httpParams = httpParams.set('count', params.count.toString());
  }
  
  if (params.language && params.language.length === 2) {
    httpParams = httpParams.set('language', params.language.toLowerCase());
  }
  
  return httpParams;
}

/**
 * Build HTTP parameters for weather endpoint
 */
export function buildWeatherParams(params: WeatherParams): HttpParams {
  let httpParams = new HttpParams()
    .set('lat', params.lat.toString())
    .set('lon', params.lon.toString());
  
  if (params.days !== undefined && params.days > 0) {
    httpParams = httpParams.set('days', params.days.toString());
  }
  
  if (params.cityName) {
    httpParams = httpParams.set('cityName', params.cityName);
  }
  
  if (params.countryName) {
    httpParams = httpParams.set('countryName', params.countryName);
  }
  
  return httpParams;
}

/**
 * Generate normalized cache key for city search
 */
export function generateCitySearchCacheKey(params: CitySearchParams): string {
  const normalizedQuery = params.q.toLowerCase().trim();
  const language = params.language || 'en';
  const count = params.count || 5;
  
  return `city:${normalizedQuery}:${language}:${count}`;
}

/**
 * Generate cache key for weather data
 */
export function generateWeatherCacheKey(params: WeatherParams): string {
  // Round coordinates to 4 decimal places for cache efficiency
  const lat = Number(params.lat.toFixed(4));
  const lon = Number(params.lon.toFixed(4));
  const days = params.days || 5;
  
  return `weather:${lat},${lon}:${days}`;
}

/**
 * Validate city search parameters
 */
export function validateCitySearchParams(params: CitySearchParams): { isValid: boolean; errors: string[] } {
  const errors: string[] = [];
  
  // Validate query
  if (!params.q || typeof params.q !== 'string') {
    errors.push('Search query is required');
  } else {
    const trimmedQuery = params.q.trim();
    if (trimmedQuery.length < VALIDATION.CITY_SEARCH.MIN_LENGTH) {
      errors.push(`Search query must be at least ${VALIDATION.CITY_SEARCH.MIN_LENGTH} characters`);
    }
    if (trimmedQuery.length > VALIDATION.CITY_SEARCH.MAX_LENGTH) {
      errors.push(`Search query must be no more than ${VALIDATION.CITY_SEARCH.MAX_LENGTH} characters`);
    }
    if (!VALIDATION.CITY_SEARCH.PATTERN.test(trimmedQuery)) {
      errors.push('Search query contains invalid characters');
    }
  }
  
  // Validate count
  if (params.count !== undefined) {
    if (!Number.isInteger(params.count) || params.count < 1 || params.count > 10) {
      errors.push('Count must be an integer between 1 and 10');
    }
  }
  
  // Validate language
  if (params.language !== undefined) {
    if (typeof params.language !== 'string' || params.language.length !== 2) {
      errors.push('Language must be a 2-character ISO code');
    }
  }
  
  return {
    isValid: errors.length === 0,
    errors
  };
}

/**
 * Validate weather parameters
 */
export function validateWeatherParams(params: WeatherParams): { isValid: boolean; errors: string[] } {
  const errors: string[] = [];
  
  // Validate latitude
  if (typeof params.lat !== 'number' || isNaN(params.lat)) {
    errors.push('Latitude must be a valid number');
  } else if (params.lat < VALIDATION.COORDINATES.LAT_MIN || params.lat > VALIDATION.COORDINATES.LAT_MAX) {
    errors.push(`Latitude must be between ${VALIDATION.COORDINATES.LAT_MIN} and ${VALIDATION.COORDINATES.LAT_MAX}`);
  }
  
  // Validate longitude
  if (typeof params.lon !== 'number' || isNaN(params.lon)) {
    errors.push('Longitude must be a valid number');
  } else if (params.lon < VALIDATION.COORDINATES.LON_MIN || params.lon > VALIDATION.COORDINATES.LON_MAX) {
    errors.push(`Longitude must be between ${VALIDATION.COORDINATES.LON_MIN} and ${VALIDATION.COORDINATES.LON_MAX}`);
  }
  
  // Validate days
  if (params.days !== undefined) {
    if (!Number.isInteger(params.days) || params.days < VALIDATION.FORECAST.MIN_DAYS || params.days > VALIDATION.FORECAST.MAX_DAYS) {
      errors.push(`Days must be an integer between ${VALIDATION.FORECAST.MIN_DAYS} and ${VALIDATION.FORECAST.MAX_DAYS}`);
    }
  }
  
  return {
    isValid: errors.length === 0,
    errors
  };
}

/**
 * Create a request identifier for deduplication
 */
export function createRequestId(method: string, url: string, params?: Record<string, any>): string {
  const paramString = params ? JSON.stringify(params) : '';
  return `${method}:${url}:${paramString}`;
}

/**
 * Sanitize search query by removing extra whitespace and invalid characters
 */
export function sanitizeSearchQuery(query: string): string {
  return query
    .trim()
    .replace(/\s+/g, ' ') // Replace multiple spaces with single space
    .substring(0, VALIDATION.CITY_SEARCH.MAX_LENGTH); // Truncate if too long
}

/**
 * Check if coordinates are valid without throwing errors
 */
export function isValidCoordinates(lat: number, lon: number): boolean {
  return !isNaN(lat) && 
         !isNaN(lon) && 
         lat >= VALIDATION.COORDINATES.LAT_MIN && 
         lat <= VALIDATION.COORDINATES.LAT_MAX &&
         lon >= VALIDATION.COORDINATES.LON_MIN && 
         lon <= VALIDATION.COORDINATES.LON_MAX;
}

/**
 * Calculate distance between two coordinate points (Haversine formula)
 * Useful for cache optimization and deduplication
 */
export function calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371; // Earth's radius in kilometers
  const dLat = toRadians(lat2 - lat1);
  const dLon = toRadians(lon2 - lon1);
  
  const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
            
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

/**
 * Convert degrees to radians
 */
function toRadians(degrees: number): number {
  return degrees * (Math.PI / 180);
}

/**
 * Normalize coordinates to reduce cache fragmentation
 */
export function normalizeCoordinates(lat: number, lon: number, precision: number = 4): { lat: number; lon: number } {
  return {
    lat: Number(lat.toFixed(precision)),
    lon: Number(lon.toFixed(precision))
  };
}

/**
 * Check if a search query is likely to be a city name
 */
export function isLikelyCityName(query: string): boolean {
  const trimmed = query.trim();
  
  // Too short or too long
  if (trimmed.length < 2 || trimmed.length > 50) {
    return false;
  }
  
  // Contains mostly numbers (likely coordinates)
  const numberCount = (trimmed.match(/\d/g) || []).length;
  if (numberCount / trimmed.length > 0.5) {
    return false;
  }
  
  // Contains invalid characters for city names
  const invalidChars = /[<>@#$%^&*()+=\[\]{}|\\;:"/?,`~]/;
  if (invalidChars.test(trimmed)) {
    return false;
  }
  
  return true;
}
