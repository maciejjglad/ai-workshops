import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

import {
  SelectedCity,
  CitySelectedEvent,
  WeatherResponse,
  WeatherRefreshEvent,
  TemperatureUnit,
  UserPreferences
} from './models/types';
import { CitySearchComponent } from './components/city-search/city-search.component';
import { WeatherPanelComponent } from './components/weather-panel/weather-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    MatToolbarModule,
    MatSnackBarModule,
    MatIconModule,
    MatButtonModule,
    CitySearchComponent,
    WeatherPanelComponent
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent implements OnInit {
  title = 'Weather App';
  
  // Inject services
  private cdr = inject(ChangeDetectorRef);
  private snackBar = inject(MatSnackBar);

  // Application state
  selectedCity: SelectedCity | null = null;
  searchQuery = '';
  weatherData: WeatherResponse | null = null;
  userPreferences: UserPreferences = {
    temperatureUnit: TemperatureUnit.CELSIUS,
    language: 'en'
  };

  constructor() {
    console.log('AppComponent constructor called - Full weather app');
  }

  ngOnInit(): void {
    this.loadUserPreferences();
    this.loadLastSelectedCity();
    console.log('Weather App initialized successfully');
  }

  /**
   * Handle city selection from search component
   */
  onCitySelected(event: CitySelectedEvent): void {
    this.selectedCity = event.city;
    this.saveLastSelectedCity(event.city);
    this.cdr.markForCheck();
    
    this.snackBar.open(
      `Selected ${event.city.name}, ${event.city.country}`, 
      'Dismiss', 
      { 
        duration: 3000,
        verticalPosition: 'bottom',
        horizontalPosition: 'center'
      }
    );
  }

  /**
   * Handle search query changes
   */
  onSearchQueryChanged(query: string): void {
    this.searchQuery = query;
  }

  /**
   * Handle search errors
   */
  onSearchError(error: string): void {
    this.snackBar.open(
      `Search error: ${error}`, 
      'Dismiss', 
      { 
        duration: 5000,
        verticalPosition: 'bottom',
        horizontalPosition: 'center'
      }
    );
  }

  /**
   * Handle weather data loading
   */
  onWeatherLoaded(response: WeatherResponse): void {
    this.weatherData = response;
    this.cdr.markForCheck();
  }

  /**
   * Handle weather loading errors
   */
  onWeatherError(error: string): void {
    this.snackBar.open(
      `Weather error: ${error}`, 
      'Dismiss', 
      { 
        duration: 5000,
        verticalPosition: 'bottom',
        horizontalPosition: 'center'
      }
    );
  }

  /**
   * Handle weather refresh events
   */
  onWeatherRefresh(event: WeatherRefreshEvent): void {
    // Refresh is handled by the weather panel component
    // This is just for user feedback
  }

  /**
   * Toggle temperature unit between Celsius and Fahrenheit
   */
  toggleTemperatureUnit(): void {
    this.userPreferences.temperatureUnit = 
      this.userPreferences.temperatureUnit === TemperatureUnit.CELSIUS
        ? TemperatureUnit.FAHRENHEIT
        : TemperatureUnit.CELSIUS;
    
    this.saveUserPreferences();
    this.cdr.markForCheck();
    
    const unit = this.userPreferences.temperatureUnit === TemperatureUnit.CELSIUS ? 'Celsius' : 'Fahrenheit';
    this.snackBar.open(
      `Temperature unit switched to ${unit}`, 
      'Dismiss', 
      { 
        duration: 2000,
        verticalPosition: 'bottom',
        horizontalPosition: 'center'
      }
    );
  }

  /**
   * Get temperature unit label for display
   */
  getTemperatureUnitLabel(): string {
    return this.userPreferences.temperatureUnit === TemperatureUnit.CELSIUS ? '°C' : '°F';
  }

  /**
   * Clear current city selection
   */
  clearSelection(): void {
    this.selectedCity = null;
    this.weatherData = null;
    this.searchQuery = '';
    this.removeLastSelectedCity();
    this.cdr.markForCheck();
    
    this.snackBar.open(
      'Selection cleared', 
      'Dismiss', 
      { 
        duration: 2000,
        verticalPosition: 'bottom',
        horizontalPosition: 'center'
      }
    );
  }

  /**
   * Check if the app has a selected city
   */
  get hasSelection(): boolean {
    return this.selectedCity !== null;
  }

  /**
   * Get app status for accessibility
   */
  getAppStatus(): string {
    if (this.selectedCity) {
      return `Weather app loaded. Current city: ${this.selectedCity.name}, ${this.selectedCity.country}`;
    }
    return 'Weather app loaded. Ready to search for weather information';
  }

  /**
   * Get initial search query
   */
  getInitialSearchQuery(): string {
    return this.searchQuery;
  }

  /**
   * Load user preferences from localStorage
   */
  private loadUserPreferences(): void {
    try {
      const saved = localStorage.getItem('weather-app-preferences');
      if (saved) {
        const preferences = JSON.parse(saved) as UserPreferences;
        this.userPreferences = { ...this.userPreferences, ...preferences };
      }
    } catch (error) {
      console.warn('Failed to load user preferences:', error);
    }
  }

  /**
   * Save user preferences to localStorage
   */
  private saveUserPreferences(): void {
    try {
      localStorage.setItem('weather-app-preferences', JSON.stringify(this.userPreferences));
    } catch (error) {
      console.warn('Failed to save user preferences:', error);
    }
  }

  /**
   * Load last selected city from localStorage
   */
  private loadLastSelectedCity(): void {
    try {
      const saved = localStorage.getItem('weather-app-last-city');
      if (saved) {
        const city = JSON.parse(saved) as SelectedCity;
        this.selectedCity = city;
        this.cdr.markForCheck();
      }
    } catch (error) {
      console.warn('Failed to load last selected city:', error);
    }
  }

  /**
   * Save last selected city to localStorage
   */
  private saveLastSelectedCity(city: SelectedCity): void {
    try {
      localStorage.setItem('weather-app-last-city', JSON.stringify(city));
    } catch (error) {
      console.warn('Failed to save last selected city:', error);
    }
  }

  /**
   * Remove last selected city from localStorage
   */
  private removeLastSelectedCity(): void {
    try {
      localStorage.removeItem('weather-app-last-city');
    } catch (error) {
      console.warn('Failed to remove last selected city:', error);
    }
  }
}