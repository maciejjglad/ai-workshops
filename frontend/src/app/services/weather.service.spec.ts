import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { WeatherService } from './weather.service';
import { WeatherCacheService } from './weather-cache.service';
import { 
  CitySearchParams, 
  CitySearchResponse, 
  WeatherParams, 
  WeatherResponse,
  API_CONFIG 
} from '../models/types';

describe('WeatherService', () => {
  let service: WeatherService;
  let httpMock: HttpTestingController;
  let cacheService: jasmine.SpyObj<WeatherCacheService>;

  // Mock data
  const mockCitySearchResponse: CitySearchResponse = {
    cities: [
      {
        name: 'London',
        country: 'United Kingdom',
        latitude: 51.5074,
        longitude: -0.1278,
        region: 'England',
        population: 8982000
      }
    ]
  };

  const mockWeatherResponse: WeatherResponse = {
    location: {
      name: 'London',
      country: 'United Kingdom',
      latitude: 51.5074,
      longitude: -0.1278,
      timezone: 'Europe/London'
    },
    current: {
      time: '2024-01-15T14:30:00',
      temperatureC: 15.2,
      windSpeedKph: 12.6,
      weatherCode: 3,
      isDay: true,
      condition: 'Overcast',
      icon: '04d'
    },
    daily: [
      {
        date: '2024-01-15',
        temperatureMaxC: 18.1,
        temperatureMinC: 8.3,
        precipitationProbabilityPct: 20,
        windSpeedMaxKph: 15.8,
        weatherCode: 3,
        condition: 'Overcast',
        icon: '04d'
      }
    ],
    source: {
      provider: 'open-meteo',
      model: 'best_match'
    }
  };

  beforeEach(() => {
    const cacheServiceSpy = jasmine.createSpyObj('WeatherCacheService', [
      'getCitySearch',
      'setCitySearch',
      'getWeatherData',
      'setWeatherData',
      'setCorrelationId',
      'clearCitySearchCache',
      'clearWeatherDataCache',
      'clearAllCaches',
      'getCacheStatistics'
    ]);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        WeatherService,
        { provide: WeatherCacheService, useValue: cacheServiceSpy }
      ]
    });

    service = TestBed.inject(WeatherService);
    httpMock = TestBed.inject(HttpTestingController);
    cacheService = TestBed.inject(WeatherCacheService) as jasmine.SpyObj<WeatherCacheService>;
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('searchCities', () => {
    it('should search cities successfully', () => {
      const params: CitySearchParams = { q: 'London' };
      cacheService.getCitySearch.and.returnValue(null);

      service.searchCities(params).subscribe(response => {
        expect(response).toEqual(mockCitySearchResponse);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH) &&
        request.params.get('q') === 'London'
      );

      expect(req.request.method).toBe('GET');
      expect(req.request.headers.has('x-correlation-id')).toBe(true);
      req.flush(mockCitySearchResponse);

      expect(cacheService.setCitySearch).toHaveBeenCalled();
    });

    it('should return cached response when available', () => {
      const params: CitySearchParams = { q: 'London' };
      cacheService.getCitySearch.and.returnValue(mockCitySearchResponse);

      service.searchCities(params).subscribe(response => {
        expect(response).toEqual(mockCitySearchResponse);
      });

      httpMock.expectNone(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH)
      );
    });

    it('should handle validation errors', () => {
      const invalidParams: CitySearchParams = { q: 'a' }; // Too short

      service.searchCities(invalidParams).subscribe({
        next: () => fail('Should not succeed'),
        error: (error) => {
          expect(error.message).toContain('at least 2 characters');
        }
      });

      httpMock.expectNone(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH)
      );
    });

    it('should handle HTTP errors', () => {
      const params: CitySearchParams = { q: 'London' };
      cacheService.getCitySearch.and.returnValue(null);

      service.searchCities(params).subscribe({
        next: () => fail('Should not succeed'),
        error: (error) => {
          expect(error.message).toContain('No cities found');
        }
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH)
      );

      req.flush(
        { 
          type: 'https://example.com/problems/city-not-found',
          title: 'City Not Found',
          status: 404,
          detail: 'No cities found matching the search criteria.'
        },
        { status: 404, statusText: 'Not Found' }
      );
    });

    it('should build correct query parameters', () => {
      const params: CitySearchParams = { 
        q: 'London', 
        count: 3, 
        language: 'en' 
      };
      cacheService.getCitySearch.and.returnValue(null);

      service.searchCities(params).subscribe();

      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH)
      );

      expect(req.request.params.get('q')).toBe('London');
      expect(req.request.params.get('count')).toBe('3');
      expect(req.request.params.get('language')).toBe('en');

      req.flush(mockCitySearchResponse);
    });
  });

  describe('getWeather', () => {
    it('should get weather data successfully', () => {
      const params: WeatherParams = { lat: 51.5074, lon: -0.1278 };
      cacheService.getWeatherData.and.returnValue(null);

      service.getWeather(params).subscribe(response => {
        expect(response).toEqual(mockWeatherResponse);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.WEATHER) &&
        request.params.get('lat') === '51.5074' &&
        request.params.get('lon') === '-0.1278'
      );

      expect(req.request.method).toBe('GET');
      expect(req.request.headers.has('x-correlation-id')).toBe(true);
      req.flush(mockWeatherResponse);

      expect(cacheService.setWeatherData).toHaveBeenCalled();
    });

    it('should return cached weather data when available', () => {
      const params: WeatherParams = { lat: 51.5074, lon: -0.1278 };
      cacheService.getWeatherData.and.returnValue(mockWeatherResponse);

      service.getWeather(params).subscribe(response => {
        expect(response).toEqual(mockWeatherResponse);
      });

      httpMock.expectNone(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.WEATHER)
      );
    });

    it('should handle coordinate validation errors', () => {
      const invalidParams: WeatherParams = { lat: 91, lon: 0 }; // Invalid latitude

      service.getWeather(invalidParams).subscribe({
        next: () => fail('Should not succeed'),
        error: (error) => {
          expect(error.message).toContain('between -90 and 90');
        }
      });

      httpMock.expectNone(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.WEATHER)
      );
    });

    it('should handle weather API errors', () => {
      const params: WeatherParams = { lat: 51.5074, lon: -0.1278 };
      cacheService.getWeatherData.and.returnValue(null);

      service.getWeather(params).subscribe({
        next: () => fail('Should not succeed'),
        error: (error) => {
          expect(error.message).toContain('Service temporarily unavailable');
        }
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.WEATHER)
      );

      req.flush(
        {
          type: 'https://example.com/problems/upstream-error',
          title: 'External Service Error',
          status: 502,
          detail: 'The weather data provider is currently unavailable.'
        },
        { status: 502, statusText: 'Bad Gateway' }
      );
    });

    it('should include optional days parameter', () => {
      const params: WeatherParams = { lat: 51.5074, lon: -0.1278, days: 7 };
      cacheService.getWeatherData.and.returnValue(null);

      service.getWeather(params).subscribe();

      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.WEATHER)
      );

      expect(req.request.params.get('days')).toBe('7');
      req.flush(mockWeatherResponse);
    });
  });

  describe('request deduplication', () => {
    it('should deduplicate concurrent city search requests', () => {
      const params: CitySearchParams = { q: 'London' };
      cacheService.getCitySearch.and.returnValue(null);

      // Make two concurrent requests
      const request1$ = service.searchCities(params);
      const request2$ = service.searchCities(params);

      let response1: CitySearchResponse | undefined;
      let response2: CitySearchResponse | undefined;

      request1$.subscribe(response => response1 = response);
      request2$.subscribe(response => response2 = response);

      // Should only make one HTTP request
      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH)
      );

      req.flush(mockCitySearchResponse);

      expect(response1).toEqual(mockCitySearchResponse);
      expect(response2).toEqual(mockCitySearchResponse);
    });

    it('should deduplicate concurrent weather requests', () => {
      const params: WeatherParams = { lat: 51.5074, lon: -0.1278 };
      cacheService.getWeatherData.and.returnValue(null);

      // Make two concurrent requests
      const request1$ = service.getWeather(params);
      const request2$ = service.getWeather(params);

      let response1: WeatherResponse | undefined;
      let response2: WeatherResponse | undefined;

      request1$.subscribe(response => response1 = response);
      request2$.subscribe(response => response2 = response);

      // Should only make one HTTP request
      const req = httpMock.expectOne(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.WEATHER)
      );

      req.flush(mockWeatherResponse);

      expect(response1).toEqual(mockWeatherResponse);
      expect(response2).toEqual(mockWeatherResponse);
    });
  });

  describe('cache management', () => {
    it('should clear search cache', () => {
      service.clearSearchCache();
      expect(cacheService.clearCitySearchCache).toHaveBeenCalled();
    });

    it('should clear weather cache', () => {
      service.clearWeatherCache();
      expect(cacheService.clearWeatherDataCache).toHaveBeenCalled();
    });

    it('should clear all caches', () => {
      service.clearAllCaches();
      expect(cacheService.clearAllCaches).toHaveBeenCalled();
    });

    it('should get cache statistics', () => {
      const mockStats = {
        totalEntries: 10,
        citySearchEntries: 5,
        weatherDataEntries: 5,
        hitRate: 75.5,
        missRate: 24.5,
        memoryUsage: 15360
      };
      
      cacheService.getCacheStatistics.and.returnValue(mockStats);
      
      const stats = service.getCacheStatistics();
      expect(stats).toEqual(mockStats);
    });
  });

  describe('configuration', () => {
    it('should allow setting base URL', () => {
      const newBaseUrl = 'https://api.example.com';
      service.setBaseUrl(newBaseUrl);
      
      const config = service.getConfiguration();
      expect(config.baseUrl).toBe(newBaseUrl);
    });

    it('should remove trailing slash from base URL', () => {
      const baseUrlWithSlash = 'https://api.example.com/';
      service.setBaseUrl(baseUrlWithSlash);
      
      const config = service.getConfiguration();
      expect(config.baseUrl).toBe('https://api.example.com');
    });

    it('should allow setting request timeout', () => {
      const newTimeout = 5000;
      service.setRequestTimeout(newTimeout);
      
      const config = service.getConfiguration();
      expect(config.timeout).toBe(newTimeout);
    });
  });

  describe('health check', () => {
    it('should return true for healthy service', () => {
      service.checkHealth().subscribe(isHealthy => {
        expect(isHealthy).toBe(true);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes('/health')
      );

      expect(req.request.method).toBe('GET');
      req.flush('OK');
    });

    it('should return false for unhealthy service', () => {
      service.checkHealth().subscribe(isHealthy => {
        expect(isHealthy).toBe(false);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes('/health')
      );

      req.flush('', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('preload popular cities', () => {
    it('should preload popular cities successfully', () => {
      cacheService.getCitySearch.and.returnValue(null);

      service.preloadPopularCities().subscribe(() => {
        // Preloading completed
      });

      // Should make multiple requests for popular cities
      const requests = httpMock.match(request => 
        request.url.includes(API_CONFIG.ENDPOINTS.CITY_SEARCH)
      );

      expect(requests.length).toBeGreaterThan(0);

      // Flush all requests
      requests.forEach(req => req.flush(mockCitySearchResponse));
    });
  });
});
