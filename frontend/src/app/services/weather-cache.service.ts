import { Injectable } from '@angular/core';
import { CitySearchResponse, WeatherResponse } from '../models/types';

/**
 * Cache entry interface for storing data with expiration
 */
interface CacheEntry<T> {
  data: T;
  timestamp: number;
  expiresAt: number;
  etag?: string;
}

/**
 * Cache statistics interface for monitoring
 */
export interface CacheStatistics {
  totalEntries: number;
  citySearchEntries: number;
  weatherDataEntries: number;
  hitRate: number;
  missRate: number;
  memoryUsage: number;
}

/**
 * WeatherCacheService provides in-memory caching with TTL and LRU eviction
 * for city search and weather data responses
 */
@Injectable({
  providedIn: 'root'
})
export class WeatherCacheService {
  // Cache storage maps
  private citySearchCache = new Map<string, CacheEntry<CitySearchResponse>>();
  private weatherDataCache = new Map<string, CacheEntry<WeatherResponse>>();
  private correlationIdCache = new Map<string, string>();
  
  // Cache configuration
  private readonly CITY_SEARCH_TTL = 30 * 60 * 1000; // 30 minutes
  private readonly WEATHER_DATA_TTL = 10 * 60 * 1000; // 10 minutes
  private readonly MAX_CITY_ENTRIES = 100;
  private readonly MAX_WEATHER_ENTRIES = 50;
  
  // Cache statistics
  private hits = 0;
  private misses = 0;

  /**
   * Get cached city search response
   */
  getCitySearch(key: string): CitySearchResponse | null {
    const entry = this.citySearchCache.get(key);
    
    if (!entry) {
      this.misses++;
      return null;
    }
    
    if (this.isExpired(entry)) {
      this.citySearchCache.delete(key);
      this.misses++;
      return null;
    }
    
    // Move to end for LRU
    this.citySearchCache.delete(key);
    this.citySearchCache.set(key, entry);
    
    this.hits++;
    return entry.data;
  }

  /**
   * Cache city search response
   */
  setCitySearch(key: string, data: CitySearchResponse, etag?: string): void {
    const entry: CacheEntry<CitySearchResponse> = {
      data,
      timestamp: Date.now(),
      expiresAt: Date.now() + this.CITY_SEARCH_TTL,
      etag
    };
    
    // Ensure cache size limit
    this.evictLRU(this.citySearchCache, this.MAX_CITY_ENTRIES);
    
    this.citySearchCache.set(key, entry);
  }

  /**
   * Get cached weather data response
   */
  getWeatherData(key: string): WeatherResponse | null {
    const entry = this.weatherDataCache.get(key);
    
    if (!entry) {
      this.misses++;
      return null;
    }
    
    if (this.isExpired(entry)) {
      this.weatherDataCache.delete(key);
      this.misses++;
      return null;
    }
    
    // Move to end for LRU
    this.weatherDataCache.delete(key);
    this.weatherDataCache.set(key, entry);
    
    this.hits++;
    return entry.data;
  }

  /**
   * Cache weather data response
   */
  setWeatherData(key: string, data: WeatherResponse, etag?: string): void {
    const entry: CacheEntry<WeatherResponse> = {
      data,
      timestamp: Date.now(),
      expiresAt: Date.now() + this.WEATHER_DATA_TTL,
      etag
    };
    
    // Ensure cache size limit
    this.evictLRU(this.weatherDataCache, this.MAX_WEATHER_ENTRIES);
    
    this.weatherDataCache.set(key, entry);
  }

  /**
   * Check if cache entry has expired
   */
  private isExpired(entry: CacheEntry<any>): boolean {
    return Date.now() > entry.expiresAt;
  }

  /**
   * Evict least recently used entries to maintain size limit
   */
  private evictLRU<T>(cache: Map<string, CacheEntry<T>>, maxSize: number): void {
    while (cache.size >= maxSize) {
      const firstKey = cache.keys().next().value;
      if (firstKey) {
        cache.delete(firstKey);
      }
    }
  }

  /**
   * Clear all expired entries from both caches
   */
  clearExpiredEntries(): void {
    const now = Date.now();
    
    // Clear expired city search entries
    for (const [key, entry] of this.citySearchCache.entries()) {
      if (now > entry.expiresAt) {
        this.citySearchCache.delete(key);
      }
    }
    
    // Clear expired weather data entries
    for (const [key, entry] of this.weatherDataCache.entries()) {
      if (now > entry.expiresAt) {
        this.weatherDataCache.delete(key);
      }
    }
  }

  /**
   * Clear city search cache
   */
  clearCitySearchCache(): void {
    this.citySearchCache.clear();
  }

  /**
   * Clear weather data cache
   */
  clearWeatherDataCache(): void {
    this.weatherDataCache.clear();
  }

  /**
   * Clear all caches
   */
  clearAllCaches(): void {
    this.citySearchCache.clear();
    this.weatherDataCache.clear();
    this.correlationIdCache.clear();
    this.hits = 0;
    this.misses = 0;
  }

  /**
   * Get cache statistics
   */
  getCacheStatistics(): CacheStatistics {
    const totalRequests = this.hits + this.misses;
    const hitRate = totalRequests > 0 ? (this.hits / totalRequests) * 100 : 0;
    const missRate = totalRequests > 0 ? (this.misses / totalRequests) * 100 : 0;
    
    // Estimate memory usage (rough calculation)
    const citySearchMemory = this.citySearchCache.size * 1024; // ~1KB per entry
    const weatherDataMemory = this.weatherDataCache.size * 5120; // ~5KB per entry
    const memoryUsage = citySearchMemory + weatherDataMemory;
    
    return {
      totalEntries: this.citySearchCache.size + this.weatherDataCache.size,
      citySearchEntries: this.citySearchCache.size,
      weatherDataEntries: this.weatherDataCache.size,
      hitRate: Math.round(hitRate * 100) / 100,
      missRate: Math.round(missRate * 100) / 100,
      memoryUsage
    };
  }

  /**
   * Store correlation ID for request tracking
   */
  setCorrelationId(requestKey: string, correlationId: string): void {
    this.correlationIdCache.set(requestKey, correlationId);
  }

  /**
   * Get correlation ID for request tracking
   */
  getCorrelationId(requestKey: string): string | null {
    return this.correlationIdCache.get(requestKey) || null;
  }

  /**
   * Check if cache has entry (without affecting LRU order)
   */
  hasCitySearch(key: string): boolean {
    const entry = this.citySearchCache.get(key);
    return entry !== undefined && !this.isExpired(entry);
  }

  /**
   * Check if cache has weather data (without affecting LRU order)
   */
  hasWeatherData(key: string): boolean {
    const entry = this.weatherDataCache.get(key);
    return entry !== undefined && !this.isExpired(entry);
  }
}
