import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import {
  SelectedCity,
  WeatherResponse,
  WeatherRefreshEvent,
  TemperatureUnit,
  LoadingState,
  CurrentWeatherView,
  DailyWeatherView,
  API_CONFIG
} from '../../models/types';
import { WeatherService } from '../../services/weather.service';
import { WeatherCardComponent } from '../weather-card/weather-card.component';
import { 
  formatTemperature, 
  formatWindSpeed, 
  formatLocalTime, 
  formatLocationDisplay, 
  formatDate, 
  getDayName,
  formatPrecipitation
} from '../../utils/formatters';
import { getWeatherIconUrl } from '../../utils/weather-code';

@Component({
  selector: 'app-weather-panel',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    WeatherCardComponent
  ],
  templateUrl: './weather-panel.component.html',
  styleUrl: './weather-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WeatherPanelComponent implements OnInit, OnDestroy, OnChanges {
  @Input() selectedCity: SelectedCity | null = null;
  @Input() forecastDays: number = API_CONFIG.DEFAULTS.FORECAST_DAYS;
  @Input() temperatureUnit: TemperatureUnit = TemperatureUnit.CELSIUS;

  @Output() weatherLoaded = new EventEmitter<WeatherResponse>();
  @Output() weatherError = new EventEmitter<string>();
  @Output() refreshRequested = new EventEmitter<WeatherRefreshEvent>();

  // Inject services
  private weatherService = inject(WeatherService);
  private cdr = inject(ChangeDetectorRef);

  // Component state
  loading: LoadingState = 'idle';
  weatherData: WeatherResponse | null = null;
  currentWeatherView: CurrentWeatherView | null = null;
  dailyForecastViews: DailyWeatherView[] = [];
  error: string | null = null;
  lastUpdated: Date | null = null;

  // Destroy subject for cleanup
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    if (this.selectedCity) {
      this.loadWeatherData(this.selectedCity);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedCity'] && changes['selectedCity'].currentValue) {
      this.loadWeatherData(this.selectedCity!);
    }
    
    if (changes['temperatureUnit'] && this.weatherData) {
      // Re-transform view models with new temperature unit
      this.transformToViewModels(this.weatherData);
      this.cdr.markForCheck();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load weather data for selected city
   */
  private loadWeatherData(city: SelectedCity): void {
    this.loading = 'loading';
    this.error = null;
    this.cdr.markForCheck();

    const params = {
      lat: city.lat,
      lon: city.lon,
      days: this.forecastDays,
      cityName: city.name,
      countryName: city.country
    };

    this.weatherService.getWeather(params)
      .pipe(
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => this.handleWeatherSuccess(response),
        error: (error) => this.handleWeatherError(error)
      });
  }

  /**
   * Handle successful weather data load
   */
  private handleWeatherSuccess(response: WeatherResponse): void {
    this.weatherData = response;
    this.transformToViewModels(response);
    this.lastUpdated = new Date();
    this.error = null;
    this.loading = 'success';
    
    this.weatherLoaded.emit(response);
    this.cdr.markForCheck();
  }

  /**
   * Handle weather loading error
   */
  private handleWeatherError(error: any): void {
    this.loading = 'error';
    this.weatherData = null;
    this.currentWeatherView = null;
    this.dailyForecastViews = [];

    let errorMessage = 'Failed to load weather data';

    if (error instanceof Error) {
      errorMessage = error.message;
    } else if (typeof error === 'string') {
      errorMessage = error;
    }

    this.error = errorMessage;
    this.weatherError.emit(errorMessage);
    this.cdr.markForCheck();
  }

  /**
   * Transform API response to view models
   */
  private transformToViewModels(response: WeatherResponse): void {
    console.log('🌍 transformToViewModels - response.location:', response.location);
    console.log('🌍 transformToViewModels - selectedCity:', this.selectedCity);
    
    // Backend now provides location data, but keep fallback for safety
    const locationName = response.location.name || this.selectedCity?.name || 'Unknown Location';
    const locationCountry = response.location.country || this.selectedCity?.country || 'Unknown';
    
    console.log('🌍 Final location:', locationName, locationCountry);
    
    // Current weather transformation
    this.currentWeatherView = {
      temperature: formatTemperature(response.current.temperatureC, this.temperatureUnit),
      condition: response.current.condition,
      icon: response.current.icon,
      windSpeed: formatWindSpeed(response.current.windSpeedKph),
      localTime: formatLocalTime(response.current.time, response.location.timezone),
      location: formatLocationDisplay(locationName, locationCountry)
    };

    // Daily forecast transformation
    this.dailyForecastViews = response.daily.map((day, index) => ({
      date: formatDate(day.date),
      dayName: getDayName(day.date, index),
      temperatureRange: `${Math.round(day.temperatureMinC)}° / ${Math.round(day.temperatureMaxC)}°`,
      temperatureMax: `${Math.round(day.temperatureMaxC)}°`,
      temperatureMin: `${Math.round(day.temperatureMinC)}°`,
      condition: day.condition,
      icon: day.icon,
      precipitationChance: formatPrecipitation(day.precipitationProbabilityPct),
      windSpeed: formatWindSpeed(day.windSpeedMaxKph),
      weatherCode: day.weatherCode
    }));
  }

  /**
   * Get weather icon URL
   */
  getWeatherIconUrl(iconCode: string): string {
    return getWeatherIconUrl(iconCode);
  }

  /**
   * Handle refresh request
   */
  onRefreshWeather(): void {
    if (this.selectedCity) {
      this.refreshRequested.emit({ location: this.selectedCity });
      this.loadWeatherData(this.selectedCity);
    }
  }

  /**
   * Track by function for daily forecast
   */
  trackByDate(index: number, day: DailyWeatherView): string {
    return day.date;
  }

  /**
   * Handle weather card click
   */
  onWeatherCardClick(dayData: DailyWeatherView): void {
    // Could be used for future detail view functionality
    console.log('Weather card clicked:', dayData);
  }

  /**
   * Handle weather card focus
   */
  onWeatherCardFocus(dayData: DailyWeatherView): void {
    // Could be used for accessibility announcements
    console.log('Weather card focused:', dayData);
  }

  /**
   * Get formatted last updated time
   */
  getLastUpdatedText(): string {
    if (!this.lastUpdated) return '';
    
    const now = new Date();
    const diffMs = now.getTime() - this.lastUpdated.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    
    if (diffMins < 1) {
      return 'Updated just now';
    } else if (diffMins < 60) {
      return `Updated ${diffMins} minute${diffMins === 1 ? '' : 's'} ago`;
    } else {
      const diffHours = Math.floor(diffMins / 60);
      return `Updated ${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    }
  }

  /**
   * Check if data is loading
   */
  get isLoading(): boolean {
    return this.loading === 'loading';
  }

  /**
   * Check if there's an error
   */
  get hasError(): boolean {
    return this.loading === 'error' && !!this.error;
  }

  /**
   * Check if data is loaded successfully
   */
  get hasData(): boolean {
    return this.loading === 'success' && !!this.currentWeatherView && this.dailyForecastViews.length > 0;
  }

  get showEmptyState(): boolean {
    return this.loading === 'idle' && !this.selectedCity;
  }

  /**
   * Get aria label for current weather section
   */
  getCurrentWeatherAriaLabel(): string {
    if (!this.currentWeatherView) return '';
    
    return `Current weather for ${this.currentWeatherView.location}. 
      ${this.currentWeatherView.condition}. 
      Temperature ${this.currentWeatherView.temperature}. 
      Wind speed ${this.currentWeatherView.windSpeed}. 
      Local time ${this.currentWeatherView.localTime}.`.replace(/\s+/g, ' ').trim();
  }
}
