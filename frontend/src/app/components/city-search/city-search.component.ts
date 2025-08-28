import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

import { Subject, Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, switchMap, catchError, takeUntil, tap, startWith, map } from 'rxjs/operators';

import { 
  CityResult, 
  CitySelectedEvent, 
  LoadingState, 
  SelectedCity,
  API_CONFIG,
  VALIDATION 
} from '../../models/types';
import { WeatherService } from '../../services/weather.service';
import { formatCityDisplay, validateSearchQuery } from '../../utils/formatters';

@Component({
  selector: 'app-city-search',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  templateUrl: './city-search.component.html',
  styleUrl: './city-search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CitySearchComponent implements OnInit, OnDestroy, OnChanges {
  @Input() initialQuery?: string;
  @Input() disabled: boolean = false;
  @Input() placeholder: string = 'Search for a city...';

  @Output() citySelected = new EventEmitter<CitySelectedEvent>();
  @Output() searchQueryChanged = new EventEmitter<string>();
  @Output() searchError = new EventEmitter<string>();

  // Inject services
  private weatherService = inject(WeatherService);
  private cdr = inject(ChangeDetectorRef);
  
  // Form control for search input
  searchControl = new FormControl({ value: '', disabled: false });
  
  // Component state
  searchResults: CityResult[] = [];
  loading: LoadingState = 'idle';
  error: string | null = null;
  showSuggestions = false;
  selectedIndex = -1;
  hasSelectedCity = false;
  selectedCityName: string | null = null;

  // Computed properties
  get isLoading(): boolean {
    return this.loading === 'loading';
  }

  get dynamicLabel(): string {
    if (this.selectedCityName) {
      return this.selectedCityName;
    }
    return 'City Search';
  }

  // Search results observable
  searchResults$: Observable<CityResult[]>;
  
  // Destroy subject for cleanup
  private destroy$ = new Subject<void>();

  constructor() {
    // Setup search observable
    this.searchResults$ = this.searchControl.valueChanges.pipe(
      startWith(''),
      debounceTime(API_CONFIG.TIMEOUTS.SEARCH_DEBOUNCE_MS),
      distinctUntilChanged(),
      tap(query => {
        this.searchQueryChanged.emit(query || '');
        this.resetState();
      }),
      filter(query => this.shouldPerformSearch(query)),
      tap(() => this.setLoading(true)),
      switchMap(query => this.performSearch(query || '')),
      takeUntil(this.destroy$)
    );
  }

  ngOnInit(): void {
    if (this.initialQuery) {
      this.searchControl.setValue(this.initialQuery);
    }
    
    // Set initial disabled state
    this.updateDisabledState();

    // Subscribe to search results
    this.searchResults$.subscribe({
      next: (results) => {
        this.searchResults = results || [];
        this.showSuggestions = (results && results.length > 0) || false;
        this.setLoading(false);
        this.error = null;
        // Trigger change detection
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.handleSearchError(error);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngOnChanges(): void {
    // Update disabled state when input changes
    this.updateDisabledState();
  }

  /**
   * Update the disabled state of the form control
   */
  private updateDisabledState(): void {
    if (this.disabled) {
      this.searchControl.disable();
    } else {
      this.searchControl.enable();
    }
  }

  /**
   * Determine if we should perform a search for the given query
   */
  private shouldPerformSearch(query: any): query is string {
    if (!query) {
      return false;
    }
    
    // Convert to string if it's a city object
    const queryString = typeof query === 'string' ? query : '';
    if (!queryString) {
      return false;
    }
    
    // Don't search if a city is selected and the query matches the display format
    if (this.hasSelectedCity) {
      // If the query looks like a formatted city display (contains comma), don't search
      if (queryString.includes(',')) {
        return false;
      }
      // If user is clearly editing (short query), reset selection and allow search
      if (queryString.length < VALIDATION.CITY_SEARCH.MIN_LENGTH) {
        this.hasSelectedCity = false;
        this.selectedCityName = null;
        return false;
      }
    }
    
    const validation = validateSearchQuery(queryString);
    if (!validation.isValid && validation.error) {
      // Don't show error for too short queries as user is still typing
      if (!validation.error.includes('at least 2 characters')) {
        this.error = validation.error;
        this.searchError.emit(validation.error);
      }
      return false;
    }
    
    this.error = null;
    return true;
  }

  /**
   * Perform city search
   */
  private performSearch(query: string): Observable<CityResult[]> {
    return this.weatherService.searchCities({
      q: query,
      count: API_CONFIG.DEFAULTS.SEARCH_COUNT
    }).pipe(
      map(response => {
        console.log('🌐 performSearch raw response:', response);
        // Handle case mismatch: backend returns "Cities" (capital C), frontend expects "cities"
        const cities = (response as any).Cities || response.cities || [];
        console.log('🌐 performSearch cities array:', cities);
        
        // Transform API response to match our interface (capital letters -> lowercase)
        const transformedCities = cities.map((city: any) => ({
          name: city.Name || city.name,
          country: city.Country || city.country,
          latitude: city.Latitude || city.latitude,
          longitude: city.Longitude || city.longitude,
          region: city.Region || city.region,
          population: city.Population || city.population
        }));
        console.log('🌐 performSearch transformed cities:', transformedCities);
        return transformedCities;
      }),
      catchError(error => {
        this.handleSearchError(error);
        return of([]);
      })
    );
  }

  /**
   * Handle search errors
   */
  private handleSearchError(error: any): void {
    this.setLoading(false);
    this.showSuggestions = false;
    
    let errorMessage = 'Failed to search cities';
    
    if (error instanceof Error) {
      errorMessage = error.message;
    } else if (typeof error === 'string') {
      errorMessage = error;
    }
    
    this.error = errorMessage;
    this.searchError.emit(errorMessage);
  }

  /**
   * Set loading state
   */
  private setLoading(loading: boolean): void {
    this.loading = loading ? 'loading' : 'idle';
  }

  /**
   * Reset component state
   */
  private resetState(): void {
    this.selectedIndex = -1;
    this.error = null;
    // Note: hasSelectedCity is now managed in shouldPerformSearch
  }

  /**
   * Format city for display
   */
  displayCity = (city: CityResult | null): string => {
    console.log('🏙️ displayCity called with:', city);
    if (!city) {
      console.log('🏙️ displayCity: city is null/undefined, returning empty string');
      return '';
    }
    if (!city.name || !city.country) {
      console.log('🏙️ displayCity: city has undefined name or country!', { name: city.name, country: city.country });
      return '';
    }
    const result = formatCityDisplay(city);
    console.log('🏙️ displayCity result:', result);
    return result;
  };

  /**
   * Get ARIA label for city option
   */
  getCityAriaLabel(city: CityResult): string {
    const display = this.displayCity(city);
    const population = city.population ? `, population ${city.population.toLocaleString()}` : '';
    return `${display}${population}`;
  }

  /**
   * Handle autocomplete option selection
   */
  onOptionSelected(event: MatAutocompleteSelectedEvent): void {
    const city = event.option.value as CityResult;
    this.onCitySelected(city);
  }

  /**
   * Handle city selection
   */
  onCitySelected(city: CityResult): void {
    const selectedCity: SelectedCity = {
      lat: city.latitude,
      lon: city.longitude,
      name: city.name,
      country: city.country
    };
    
    // Mark that a city has been selected
    this.hasSelectedCity = true;
    
    // Store the selected city name for the label
    this.selectedCityName = city.name;
    
    // Set the input value to the display string to prevent validation errors
    const displayString = this.displayCity(city);
    this.searchControl.setValue(displayString, { emitEvent: false });
    
    // Clear any errors since we have a valid selection
    this.error = null;
    
    this.citySelected.emit({ city: selectedCity });
    this.showSuggestions = false;
    this.selectedIndex = -1;
  }

  /**
   * Handle keyboard navigation in autocomplete
   */
  onKeyDown(event: KeyboardEvent): void {
    if (!this.showSuggestions || this.searchResults.length === 0) {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.selectedIndex = Math.min(this.selectedIndex + 1, this.searchResults.length - 1);
        break;
      
      case 'ArrowUp':
        event.preventDefault();
        this.selectedIndex = Math.max(this.selectedIndex - 1, -1);
        break;
      
      case 'Enter':
        event.preventDefault();
        if (this.selectedIndex >= 0 && this.selectedIndex < this.searchResults.length) {
          this.onCitySelected(this.searchResults[this.selectedIndex]);
        }
        break;
      
      case 'Escape':
        event.preventDefault();
        this.showSuggestions = false;
        this.selectedIndex = -1;
        break;
    }
  }

  /**
   * Handle input focus
   */
  onFocus(): void {
    if (this.searchResults.length > 0) {
      this.showSuggestions = true;
    }
  }

  /**
   * Handle input blur
   */
  onBlur(): void {
    // Delay hiding suggestions to allow for option selection
    setTimeout(() => {
      this.showSuggestions = false;
      this.selectedIndex = -1;
    }, 200);
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.searchControl.setValue('');
    this.resetState();
    this.showSuggestions = false;
    this.hasSelectedCity = false;
    this.selectedCityName = null;
  }

  /**
   * Get hint text based on current state
   */
  getHintText(): string {
    if (this.error) {
      return this.error;
    }
    
    if (this.loading === 'loading') {
      return 'Searching...';
    }
    
    // If a city has been selected, show selected state
    if (this.hasSelectedCity) {
      return 'City selected';
    }
    
    const query = this.searchControl.value;
    if (!query || query.length < VALIDATION.CITY_SEARCH.MIN_LENGTH) {
      return `Type at least ${VALIDATION.CITY_SEARCH.MIN_LENGTH} characters to search`;
    }
    
    // Only show "No cities found" if we've actually searched and found nothing
    if (this.searchResults.length === 0 && query.length >= VALIDATION.CITY_SEARCH.MIN_LENGTH && !this.hasSelectedCity) {
      return 'No cities found';
    }
    
    return `${this.searchResults.length} cities found`;
  }

  /**
   * Check if should show error state
   */
  get hasError(): boolean {
    return !!this.error && this.loading !== 'loading';
  }



  /**
   * Track by function for city results
   */
  trackByCity(index: number, city: CityResult): string {
    return `${city.name}-${city.country}-${city.latitude}-${city.longitude}`;
  }
}
