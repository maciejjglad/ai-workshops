# Angular Weather UI Component Architecture

This document outlines the component architecture for the Weather UI application built with Angular 18 standalone components, following the specified requirements and leveraging Angular Material for UI components.

## Component Overview

The application consists of three main components in a parent-child relationship:
- **CitySearchComponent**: Autocomplete search for cities
- **WeatherPanelComponent**: Weather data display container
- **WeatherCardComponent**: Individual daily forecast cards

## 1. CitySearchComponent

### Responsibilities
- **Primary**: Provide city search with autocomplete functionality
- **Search Management**: Handle debounced user input (300ms)
- **Results Display**: Show top 5 city suggestions from backend
- **Selection Handling**: Emit selected city to parent component
- **State Management**: Manage search loading states and errors
- **Input Validation**: Ensure minimum 2 characters for search

### Component Architecture

#### Inputs
```typescript
// Optional initial search query
@Input() initialQuery?: string;

// Optional loading state control from parent
@Input() disabled?: boolean;

// Optional placeholder text customization
@Input() placeholder?: string = 'Search for a city...';
```

#### Outputs
```typescript
// Emitted when user selects a city from suggestions
@Output() citySelected = new EventEmitter<CitySelectedEvent>();

// Emitted when search input changes (for parent state management)
@Output() searchQueryChanged = new EventEmitter<string>();

// Emitted on search errors (for parent error handling)
@Output() searchError = new EventEmitter<string>();
```

#### Internal State
```typescript
interface ComponentState {
  searchQuery: string;
  searchResults: CityResult[];
  loading: LoadingState;
  error: string | null;
  showSuggestions: boolean;
  selectedIndex: number; // For keyboard navigation
}
```

### Autocomplete Behavior

#### Angular Material Autocomplete Integration
- Use `mat-autocomplete` with `mat-form-field` and `mat-input`
- Custom display function for city results: `"${city.name}, ${city.country}"`
- Filter options client-side after API response for instant feedback
- Maximum 5 suggestions displayed as per requirements

#### Debouncing Strategy
- **RxJS debounceTime(300)** for search input
- **distinctUntilChanged()** to prevent duplicate API calls
- **switchMap()** to cancel previous requests on new input
- Minimum 2 characters validation before API call

#### Search Logic Flow
```typescript
searchQuery$ = this.searchControl.valueChanges.pipe(
  debounceTime(API_CONFIG.TIMEOUTS.SEARCH_DEBOUNCE_MS),
  distinctUntilChanged(),
  filter(query => query && query.length >= VALIDATION.CITY_SEARCH.MIN_LENGTH),
  tap(() => this.loading = 'loading'),
  switchMap(query => this.weatherService.searchCities({ q: query })),
  catchError(error => this.handleSearchError(error))
);
```

### Accessibility Features

#### ARIA Labels and Roles
```html
<mat-form-field>
  <mat-label>City Search</mat-label>
  <input 
    matInput 
    [formControl]="searchControl"
    [matAutocomplete]="auto"
    aria-label="Search for a city"
    aria-describedby="search-hint"
    role="combobox"
    aria-expanded="{{showSuggestions}}"
    aria-autocomplete="list">
  <mat-hint id="search-hint">Type at least 2 characters to search</mat-hint>
</mat-form-field>

<mat-autocomplete 
  #auto="matAutocomplete"
  [displayWith]="displayCity"
  role="listbox"
  aria-label="City suggestions">
  <mat-option 
    *ngFor="let city of searchResults; let i = index" 
    [value]="city"
    role="option"
    [attr.aria-label]="getCityAriaLabel(city)"
    [attr.aria-selected]="i === selectedIndex">
    {{displayCity(city)}}
    <span class="city-details" *ngIf="city.region">
      {{city.region}}
    </span>
  </mat-option>
</mat-autocomplete>
```

#### Keyboard Navigation
- **Arrow Down/Up**: Navigate through suggestions
- **Enter**: Select highlighted suggestion
- **Escape**: Close suggestions dropdown
- **Tab**: Move focus to next element
- Support for screen readers with proper ARIA announcements

### Event Handling

#### City Selection Event
```typescript
onCitySelected(city: CityResult): void {
  const selectedCity: SelectedCity = {
    lat: city.latitude,
    lon: city.longitude,
    name: city.name,
    country: city.country
  };
  
  this.citySelected.emit({ city: selectedCity });
  this.searchQuery = this.displayCity(city);
  this.showSuggestions = false;
}
```

#### Error Handling
```typescript
private handleSearchError(error: any): Observable<CitySearchResponse> {
  let errorMessage = 'Failed to search cities';
  
  if (error.status === 404) {
    errorMessage = 'No cities found matching your search';
  } else if (error.status >= 500) {
    errorMessage = 'Search service temporarily unavailable';
  }
  
  this.searchError.emit(errorMessage);
  return of({ cities: [] });
}
```

### Visual States

#### Loading State
- Show `mat-progress-spinner` inside input field
- Disable input during loading
- Clear previous results

#### Empty State
- Display "Start typing to search for cities" message
- Show search icon

#### No Results State
- Display "No cities found" with search tips
- Suggest checking spelling or trying broader terms

#### Error State
- Show error message using `mat-error`
- Provide retry mechanism
- Maintain user's input

---

## 2. WeatherPanelComponent

### Responsibilities
- **Weather Data Management**: Fetch and display weather information
- **Layout Orchestration**: Organize current weather and daily forecast sections
- **State Management**: Handle loading, success, and error states
- **Data Transformation**: Convert API responses to view models
- **Error Handling**: Display user-friendly error messages
- **Responsive Design**: Adapt layout for mobile and desktop

### Component Architecture

#### Inputs
```typescript
// Selected city coordinates and metadata
@Input() selectedCity: SelectedCity | null = null;

// Optional forecast days override
@Input() forecastDays?: number = API_CONFIG.DEFAULTS.FORECAST_DAYS;

// Optional temperature unit preference
@Input() temperatureUnit?: TemperatureUnit = TemperatureUnit.CELSIUS;
```

#### Outputs
```typescript
// Emitted when weather data is successfully loaded
@Output() weatherLoaded = new EventEmitter<WeatherResponse>();

// Emitted on weather loading errors
@Output() weatherError = new EventEmitter<string>();

// Emitted when user requests weather refresh
@Output() refreshRequested = new EventEmitter<WeatherRefreshEvent>();
```

#### Internal State
```typescript
interface ComponentState {
  loading: LoadingState;
  weatherData: WeatherResponse | null;
  currentWeatherView: CurrentWeatherView | null;
  dailyForecastViews: DailyWeatherView[];
  error: string | null;
  lastUpdated: Date | null;
}
```

### Loading and Error States

#### Loading State Management
```typescript
private loadWeatherData(city: SelectedCity): void {
  this.loading = 'loading';
  this.error = null;
  
  const params: WeatherParams = {
    lat: city.lat,
    lon: city.lon,
    days: this.forecastDays
  };
  
  this.weatherService.getWeather(params)
    .pipe(
      takeUntil(this.destroy$),
      finalize(() => this.loading = 'idle')
    )
    .subscribe({
      next: (response) => this.handleWeatherSuccess(response),
      error: (error) => this.handleWeatherError(error)
    });
}
```

#### Error State Handling
```typescript
private handleWeatherError(error: any): void {
  this.loading = 'error';
  
  let errorMessage = 'Failed to load weather data';
  
  if (error.status === 400) {
    errorMessage = 'Invalid location coordinates';
  } else if (error.status === 502) {
    errorMessage = 'Weather service temporarily unavailable';
  } else if (error.status === 0) {
    errorMessage = 'No internet connection';
  }
  
  this.error = errorMessage;
  this.weatherError.emit(errorMessage);
}
```

### Layout Structure

#### Responsive Grid Layout
```scss
.weather-panel {
  display: grid;
  gap: 1.5rem;
  max-width: 1200px;
  margin: 0 auto;
  padding: 1rem;
  
  // Desktop layout
  @media (min-width: 768px) {
    grid-template-columns: 1fr 2fr;
    grid-template-areas: 
      "current forecast"
      "current forecast";
  }
  
  // Mobile layout - stacked
  @media (max-width: 767px) {
    grid-template-columns: 1fr;
    grid-template-areas:
      "current"
      "forecast";
  }
}

.current-weather {
  grid-area: current;
}

.daily-forecast {
  grid-area: forecast;
}
```

#### Current Weather Section
```html
<section class="current-weather" *ngIf="currentWeatherView">
  <mat-card class="current-weather-card">
    <mat-card-content>
      <div class="current-header">
        <h2>{{currentWeatherView.location}}</h2>
        <p class="local-time">{{currentWeatherView.localTime}}</p>
      </div>
      
      <div class="current-details">
        <div class="temperature">
          <span class="temp-value">{{currentWeatherView.temperature}}</span>
          <img [src]="getWeatherIconUrl(currentWeatherView.icon)" 
               [alt]="currentWeatherView.condition"
               class="weather-icon">
        </div>
        
        <div class="condition">{{currentWeatherView.condition}}</div>
        <div class="wind">Wind: {{currentWeatherView.windSpeed}}</div>
      </div>
    </mat-card-content>
  </mat-card>
</section>
```

#### Daily Forecast Section
```html
<section class="daily-forecast">
  <h3>5-Day Forecast</h3>
  <div class="forecast-grid">
    <app-weather-card
      *ngFor="let day of dailyForecastViews; trackBy: trackByDate"
      [dayData]="day"
      [temperatureUnit]="temperatureUnit">
    </app-weather-card>
  </div>
</section>
```

### Data Transformation

#### View Model Conversion
```typescript
private transformToViewModels(response: WeatherResponse): void {
  // Current weather transformation
  this.currentWeatherView = {
    temperature: this.formatTemperature(response.current.temperatureC),
    condition: response.current.condition,
    icon: response.current.icon,
    windSpeed: `${response.current.windSpeedKph} km/h`,
    localTime: this.formatLocalTime(response.current.time, response.location.timezone),
    location: `${response.location.name}, ${response.location.country}`
  };
  
  // Daily forecast transformation
  this.dailyForecastViews = response.daily.map((day, index) => ({
    date: this.formatDate(day.date),
    dayName: this.getDayName(day.date, index),
    temperatureRange: `${Math.round(day.temperatureMinC)}° / ${Math.round(day.temperatureMaxC)}°`,
    temperatureMax: `${Math.round(day.temperatureMaxC)}°`,
    temperatureMin: `${Math.round(day.temperatureMinC)}°`,
    condition: day.condition,
    icon: day.icon,
    precipitationChance: `${day.precipitationProbabilityPct}%`,
    windSpeed: `${day.windSpeedMaxKph} km/h`,
    weatherCode: day.weatherCode
  }));
}
```

### Loading States UI

#### Skeleton Loading
```html
<div class="loading-skeleton" *ngIf="loading === 'loading'">
  <mat-card class="skeleton-current">
    <mat-card-content>
      <div class="skeleton-line skeleton-title"></div>
      <div class="skeleton-line skeleton-temp"></div>
      <div class="skeleton-line skeleton-details"></div>
    </mat-card-content>
  </mat-card>
  
  <div class="skeleton-forecast">
    <div class="skeleton-card" *ngFor="let item of [1,2,3,4,5]">
      <div class="skeleton-line skeleton-day"></div>
      <div class="skeleton-line skeleton-icon"></div>
      <div class="skeleton-line skeleton-temp-range"></div>
    </div>
  </div>
</div>
```

#### Error State UI
```html
<div class="error-state" *ngIf="loading === 'error'">
  <mat-card class="error-card">
    <mat-card-content>
      <mat-icon color="warn">warning</mat-icon>
      <h3>Weather data unavailable</h3>
      <p>{{error}}</p>
      <button mat-raised-button color="primary" (click)="retryWeatherLoad()">
        <mat-icon>refresh</mat-icon>
        Try Again
      </button>
    </mat-card-content>
  </mat-card>
</div>
```

---

## 3. WeatherCardComponent

### Responsibilities
- **Daily Forecast Display**: Show single day weather information
- **Compact Layout**: Efficient use of space in card format
- **Interactive Elements**: Support hover states and click interactions
- **Accessibility**: Screen reader compatible with proper semantics
- **Responsive Design**: Adapt to different card container sizes

### Component Architecture

#### Inputs
```typescript
// Daily weather data to display
@Input() dayData!: DailyWeatherView;

// Optional temperature unit override
@Input() temperatureUnit?: TemperatureUnit = TemperatureUnit.CELSIUS;

// Optional compact mode for smaller screens
@Input() compact?: boolean = false;

// Optional selection state
@Input() selected?: boolean = false;
```

#### Outputs
```typescript
// Emitted when card is clicked (for potential detail view)
@Output() cardClicked = new EventEmitter<DailyWeatherView>();

// Emitted when card is focused (for accessibility)
@Output() cardFocused = new EventEmitter<DailyWeatherView>();
```

### CSS Grid Layout

#### Card Structure
```scss
.weather-card {
  display: grid;
  grid-template-areas:
    "day     icon"
    "temps   icon" 
    "details details";
  grid-template-columns: 1fr auto;
  grid-template-rows: auto auto 1fr;
  gap: 0.5rem;
  padding: 1rem;
  border-radius: 8px;
  background: white;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  transition: all 0.2s ease;
  cursor: pointer;
  
  &:hover {
    box-shadow: 0 4px 8px rgba(0,0,0,0.15);
    transform: translateY(-1px);
  }
  
  &:focus {
    outline: 2px solid #1976d2;
    outline-offset: 2px;
  }
  
  // Compact mode for mobile
  &.compact {
    grid-template-areas:
      "day icon temps"
      "details details details";
    grid-template-columns: 1fr auto 1fr;
    grid-template-rows: auto auto;
    padding: 0.75rem;
  }
}

.day-name {
  grid-area: day;
  font-weight: 500;
  color: #333;
}

.weather-icon {
  grid-area: icon;
  width: 48px;
  height: 48px;
  margin-left: 0.5rem;
}

.temperature-range {
  grid-area: temps;
  display: flex;
  align-items: center;
  gap: 0.25rem;
  
  .temp-max {
    font-weight: 600;
    font-size: 1.1em;
    color: #333;
  }
  
  .temp-min {
    color: #666;
    font-size: 0.9em;
  }
}

.weather-details {
  grid-area: details;
  display: flex;
  justify-content: space-between;
  font-size: 0.875rem;
  color: #666;
  margin-top: 0.5rem;
  
  .precipitation, .wind {
    display: flex;
    align-items: center;
    gap: 0.25rem;
  }
}
```

### Accessibility Features

#### Semantic HTML Structure
```html
<mat-card 
  class="weather-card"
  [class.compact]="compact"
  [class.selected]="selected"
  tabindex="0"
  role="button"
  [attr.aria-label]="getCardAriaLabel()"
  [attr.aria-describedby]="cardId + '-details'"
  (click)="onCardClick()"
  (keydown)="onKeyDown($event)"
  (focus)="onCardFocus()">
  
  <div class="day-name">
    {{dayData.dayName}}
    <span class="sr-only">{{dayData.date}}</span>
  </div>
  
  <img 
    class="weather-icon"
    [src]="getWeatherIconUrl(dayData.icon)"
    [alt]="dayData.condition"
    loading="lazy">
  
  <div class="temperature-range">
    <span class="temp-max" [attr.aria-label]="'High ' + dayData.temperatureMax">
      {{dayData.temperatureMax}}
    </span>
    <span class="temp-separator">/</span>
    <span class="temp-min" [attr.aria-label]="'Low ' + dayData.temperatureMin">
      {{dayData.temperatureMin}}
    </span>
  </div>
  
  <div class="weather-details" [id]="cardId + '-details'">
    <span class="precipitation" [attr.aria-label]="'Precipitation chance ' + dayData.precipitationChance">
      <mat-icon class="detail-icon">grain</mat-icon>
      {{dayData.precipitationChance}}
    </span>
    
    <span class="wind" [attr.aria-label]="'Wind speed ' + dayData.windSpeed">
      <mat-icon class="detail-icon">air</mat-icon>
      {{dayData.windSpeed}}
    </span>
    
    <span class="condition sr-only">{{dayData.condition}}</span>
  </div>
</mat-card>
```

#### ARIA Labels and Screen Reader Support
```typescript
getCardAriaLabel(): string {
  return `Weather for ${this.dayData.dayName}, ${this.dayData.date}. 
    ${this.dayData.condition}. 
    High ${this.dayData.temperatureMax}, low ${this.dayData.temperatureMin}. 
    ${this.dayData.precipitationChance} chance of precipitation. 
    Wind ${this.dayData.windSpeed}.`;
}

onKeyDown(event: KeyboardEvent): void {
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    this.onCardClick();
  }
}
```

### Interactive Features

#### Hover and Focus States
- Subtle elevation animation on hover
- Clear focus indicators for keyboard navigation
- Visual feedback for selection state
- Smooth transitions for all interactive states

#### Click Handling
```typescript
onCardClick(): void {
  this.cardClicked.emit(this.dayData);
  // Could trigger detail modal or navigation in future
}

onCardFocus(): void {
  this.cardFocused.emit(this.dayData);
  // Could announce details to screen readers
}
```

---

## 4. Data and Event Flow

### Application Architecture

#### Component Hierarchy
```
AppComponent (Root)
├── CitySearchComponent
└── WeatherPanelComponent
    └── WeatherCardComponent (×5)
```

#### Service Layer
```
WeatherService (Injectable)
├── searchCities(params: CitySearchParams): Observable<CitySearchResponse>
├── getWeather(params: WeatherParams): Observable<WeatherResponse>
└── HTTP interceptors for correlation IDs and error handling
```

### Event Flow Diagram

```
User Input → CitySearchComponent → AppComponent → WeatherPanelComponent → WeatherCardComponent
    ↓               ↓                    ↓                ↓                        ↓
Search API ← WeatherService ←── HTTP Client ←── Backend API ←── External APIs
    ↓               ↓                    ↓                ↓                        ↓
Results → Display Autocomplete → Select City → Load Weather → Display Cards
```

### State Management Pattern

#### Parent Component (AppComponent)
```typescript
interface AppState {
  selectedCity: SelectedCity | null;
  searchQuery: string;
  weatherData: WeatherResponse | null;
  loading: LoadingState;
  errors: {
    search: string | null;
    weather: string | null;
  };
}

// Event handlers
onCitySelected(event: CitySelectedEvent): void {
  this.selectedCity = event.city;
  // WeatherPanelComponent will react to input change
}

onSearchError(error: string): void {
  this.errors.search = error;
  // Show toast notification
}

onWeatherError(error: string): void {
  this.errors.weather = error;
  // Show error state in panel
}
```

#### Service Integration
```typescript
// WeatherService with RxJS error handling
@Injectable({ providedIn: 'root' })
export class WeatherService implements IWeatherService {
  
  searchCities(params: CitySearchParams): Observable<CitySearchResponse> {
    return this.http.get<CitySearchResponse>(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.CITY_SEARCH}`,
      { params: this.buildSearchParams(params) }
    ).pipe(
      timeout(API_CONFIG.TIMEOUTS.REQUEST_TIMEOUT_MS),
      retry(2),
      catchError(this.handleError)
    );
  }
  
  getWeather(params: WeatherParams): Observable<WeatherResponse> {
    return this.http.get<WeatherResponse>(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.WEATHER}`,
      { params: this.buildWeatherParams(params) }
    ).pipe(
      timeout(API_CONFIG.TIMEOUTS.REQUEST_TIMEOUT_MS),
      retry(1),
      catchError(this.handleError)
    );
  }
}
```

### Error Propagation Strategy

#### Service Level Errors
- HTTP errors caught and transformed to user-friendly messages
- Network timeout handling with retry logic
- Proper error typing with `ProblemDetails` interface

#### Component Level Errors
- Local error state management
- Error event emission to parent components
- User notification through Angular Material snackbars

#### Global Error Handling
- HTTP error interceptor for correlation IDs
- Centralized error logging for monitoring
- Graceful degradation for offline scenarios

---

## 5. Test Cases (Angular Testing Library)

### CitySearchComponent Tests

#### Unit Tests
```typescript
describe('CitySearchComponent', () => {
  let component: CitySearchComponent;
  let mockWeatherService: jest.Mocked<WeatherService>;
  
  beforeEach(() => {
    mockWeatherService = createMockWeatherService();
    
    render(CitySearchComponent, {
      providers: [
        { provide: WeatherService, useValue: mockWeatherService }
      ]
    });
  });
  
  describe('Search Functionality', () => {
    it('should debounce search input', async () => {
      const searchInput = screen.getByLabelText(/search for a city/i);
      
      await userEvent.type(searchInput, 'Lond');
      
      // Should not call API immediately
      expect(mockWeatherService.searchCities).not.toHaveBeenCalled();
      
      // Wait for debounce
      await waitFor(() => {
        expect(mockWeatherService.searchCities).toHaveBeenCalledWith({
          q: 'Lond',
          count: 5
        });
      }, { timeout: 400 });
    });
    
    it('should not search with less than 2 characters', async () => {
      const searchInput = screen.getByLabelText(/search for a city/i);
      
      await userEvent.type(searchInput, 'L');
      await waitFor(() => {}, { timeout: 400 });
      
      expect(mockWeatherService.searchCities).not.toHaveBeenCalled();
    });
    
    it('should display search results', async () => {
      const mockResults = {
        cities: [
          { name: 'London', country: 'United Kingdom', latitude: 51.5, longitude: -0.1 }
        ]
      };
      
      mockWeatherService.searchCities.mockReturnValue(of(mockResults));
      
      const searchInput = screen.getByLabelText(/search for a city/i);
      await userEvent.type(searchInput, 'London');
      
      await waitFor(() => {
        expect(screen.getByText('London, United Kingdom')).toBeInTheDocument();
      });
    });
  });
  
  describe('Accessibility', () => {
    it('should support keyboard navigation', async () => {
      const mockResults = {
        cities: [
          { name: 'London', country: 'UK', latitude: 51.5, longitude: -0.1 },
          { name: 'Los Angeles', country: 'USA', latitude: 34.0, longitude: -118.2 }
        ]
      };
      
      mockWeatherService.searchCities.mockReturnValue(of(mockResults));
      
      const searchInput = screen.getByLabelText(/search for a city/i);
      await userEvent.type(searchInput, 'Lo');
      
      await waitFor(() => {
        expect(screen.getByText('London, UK')).toBeInTheDocument();
      });
      
      // Test arrow key navigation
      await userEvent.keyboard('{ArrowDown}');
      await userEvent.keyboard('{Enter}');
      
      // Should emit city selected event
      expect(component.citySelected.emit).toHaveBeenCalledWith({
        city: expect.objectContaining({ name: 'London' })
      });
    });
    
    it('should have proper ARIA attributes', () => {
      const searchInput = screen.getByLabelText(/search for a city/i);
      
      expect(searchInput).toHaveAttribute('role', 'combobox');
      expect(searchInput).toHaveAttribute('aria-autocomplete', 'list');
      expect(searchInput).toHaveAttribute('aria-expanded', 'false');
    });
  });
  
  describe('Error Handling', () => {
    it('should handle search API errors gracefully', async () => {
      const errorResponse = { status: 500, message: 'Server Error' };
      mockWeatherService.searchCities.mockReturnValue(throwError(errorResponse));
      
      const searchInput = screen.getByLabelText(/search for a city/i);
      await userEvent.type(searchInput, 'London');
      
      await waitFor(() => {
        expect(component.searchError.emit).toHaveBeenCalledWith(
          'Search service temporarily unavailable'
        );
      });
    });
    
    it('should handle no results scenario', async () => {
      mockWeatherService.searchCities.mockReturnValue(of({ cities: [] }));
      
      const searchInput = screen.getByLabelText(/search for a city/i);
      await userEvent.type(searchInput, 'Nonexistent');
      
      await waitFor(() => {
        expect(screen.getByText(/no cities found/i)).toBeInTheDocument();
      });
    });
  });
});
```

### WeatherPanelComponent Tests

#### Integration Tests
```typescript
describe('WeatherPanelComponent', () => {
  let mockWeatherService: jest.Mocked<WeatherService>;
  
  const mockSelectedCity: SelectedCity = {
    lat: 51.5074,
    lon: -0.1278,
    name: 'London',
    country: 'United Kingdom'
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
    source: { provider: 'open-meteo', model: 'best_match' }
  };
  
  beforeEach(() => {
    mockWeatherService = createMockWeatherService();
    
    render(WeatherPanelComponent, {
      componentInputs: { selectedCity: mockSelectedCity },
      providers: [
        { provide: WeatherService, useValue: mockWeatherService }
      ]
    });
  });
  
  describe('Weather Data Loading', () => {
    it('should load weather data when city is selected', async () => {
      mockWeatherService.getWeather.mockReturnValue(of(mockWeatherResponse));
      
      await waitFor(() => {
        expect(mockWeatherService.getWeather).toHaveBeenCalledWith({
          lat: 51.5074,
          lon: -0.1278,
          days: 5
        });
      });
      
      expect(screen.getByText('London, United Kingdom')).toBeInTheDocument();
      expect(screen.getByText('15.2°C')).toBeInTheDocument();
      expect(screen.getByText('Overcast')).toBeInTheDocument();
    });
    
    it('should display loading state', () => {
      mockWeatherService.getWeather.mockReturnValue(NEVER); // Never resolves
      
      expect(screen.getByTestId('weather-loading')).toBeInTheDocument();
      expect(screen.getByText(/loading weather data/i)).toBeInTheDocument();
    });
    
    it('should display error state', async () => {
      const errorResponse = { status: 502, message: 'Bad Gateway' };
      mockWeatherService.getWeather.mockReturnValue(throwError(errorResponse));
      
      await waitFor(() => {
        expect(screen.getByText(/weather service temporarily unavailable/i))
          .toBeInTheDocument();
      });
      
      const retryButton = screen.getByRole('button', { name: /try again/i });
      expect(retryButton).toBeInTheDocument();
    });
  });
  
  describe('Responsive Layout', () => {
    it('should render current weather and forecast cards', async () => {
      mockWeatherService.getWeather.mockReturnValue(of(mockWeatherResponse));
      
      await waitFor(() => {
        expect(screen.getByTestId('current-weather')).toBeInTheDocument();
        expect(screen.getByTestId('daily-forecast')).toBeInTheDocument();
      });
      
      const weatherCards = screen.getAllByTestId('weather-card');
      expect(weatherCards).toHaveLength(1); // One day in mock data
    });
  });
  
  describe('Data Transformation', () => {
    it('should format current weather data correctly', async () => {
      mockWeatherService.getWeather.mockReturnValue(of(mockWeatherResponse));
      
      await waitFor(() => {
        expect(screen.getByText('15.2°C')).toBeInTheDocument();
        expect(screen.getByText('Wind: 12.6 km/h')).toBeInTheDocument();
        expect(screen.getByText('Overcast')).toBeInTheDocument();
      });
    });
  });
});
```

### WeatherCardComponent Tests

#### Component Tests
```typescript
describe('WeatherCardComponent', () => {
  const mockDayData: DailyWeatherView = {
    date: 'Mon, Jan 15',
    dayName: 'Today',
    temperatureRange: '8° / 18°',
    temperatureMax: '18°',
    temperatureMin: '8°',
    condition: 'Overcast',
    icon: '04d',
    precipitationChance: '20%',
    windSpeed: '15.8 km/h',
    weatherCode: 3
  };
  
  beforeEach(() => {
    render(WeatherCardComponent, {
      componentInputs: { dayData: mockDayData }
    });
  });
  
  describe('Data Display', () => {
    it('should display all weather information', () => {
      expect(screen.getByText('Today')).toBeInTheDocument();
      expect(screen.getByText('18°')).toBeInTheDocument();
      expect(screen.getByText('8°')).toBeInTheDocument();
      expect(screen.getByText('20%')).toBeInTheDocument();
      expect(screen.getByText('15.8 km/h')).toBeInTheDocument();
      
      const weatherIcon = screen.getByAltText('Overcast');
      expect(weatherIcon).toBeInTheDocument();
      expect(weatherIcon).toHaveAttribute('src', expect.stringContaining('04d'));
    });
    
    it('should handle compact mode', () => {
      render(WeatherCardComponent, {
        componentInputs: { dayData: mockDayData, compact: true }
      });
      
      const card = screen.getByTestId('weather-card');
      expect(card).toHaveClass('compact');
    });
  });
  
  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      const card = screen.getByRole('button');
      
      expect(card).toHaveAttribute('aria-label', expect.stringContaining('Today'));
      expect(card).toHaveAttribute('aria-label', expect.stringContaining('Overcast'));
      expect(card).toHaveAttribute('aria-label', expect.stringContaining('18°'));
      expect(card).toHaveAttribute('aria-label', expect.stringContaining('8°'));
    });
    
    it('should support keyboard interaction', async () => {
      const card = screen.getByRole('button');
      
      card.focus();
      await userEvent.keyboard('{Enter}');
      
      expect(component.cardClicked.emit).toHaveBeenCalledWith(mockDayData);
    });
    
    it('should be focusable', () => {
      const card = screen.getByRole('button');
      
      expect(card).toHaveAttribute('tabindex', '0');
      card.focus();
      expect(card).toHaveFocus();
    });
  });
  
  describe('Interactions', () => {
    it('should emit click events', async () => {
      const card = screen.getByRole('button');
      
      await userEvent.click(card);
      
      expect(component.cardClicked.emit).toHaveBeenCalledWith(mockDayData);
    });
    
    it('should emit focus events', () => {
      const card = screen.getByRole('button');
      
      card.focus();
      
      expect(component.cardFocused.emit).toHaveBeenCalledWith(mockDayData);
    });
  });
});
```

### End-to-End Integration Tests

#### Full User Journey Tests
```typescript
describe('Weather App E2E', () => {
  let mockWeatherService: jest.Mocked<WeatherService>;
  
  beforeEach(() => {
    mockWeatherService = createMockWeatherService();
    
    render(AppComponent, {
      providers: [
        { provide: WeatherService, useValue: mockWeatherService }
      ]
    });
  });
  
  it('should complete full user journey from search to weather display', async () => {
    // Mock API responses
    const searchResponse = {
      cities: [
        { name: 'Kraków', country: 'Poland', latitude: 50.0647, longitude: 19.9450 }
      ]
    };
    
    const weatherResponse = createMockWeatherResponse();
    
    mockWeatherService.searchCities.mockReturnValue(of(searchResponse));
    mockWeatherService.getWeather.mockReturnValue(of(weatherResponse));
    
    // Step 1: Search for city
    const searchInput = screen.getByLabelText(/search for a city/i);
    await userEvent.type(searchInput, 'Krak');
    
    // Step 2: Wait for suggestions and select
    await waitFor(() => {
      expect(screen.getByText('Kraków, Poland')).toBeInTheDocument();
    });
    
    const suggestion = screen.getByText('Kraków, Poland');
    await userEvent.click(suggestion);
    
    // Step 3: Verify weather panel loads
    await waitFor(() => {
      expect(screen.getByText('Kraków, Poland')).toBeInTheDocument();
      expect(screen.getByTestId('current-weather')).toBeInTheDocument();
      expect(screen.getByTestId('daily-forecast')).toBeInTheDocument();
    });
    
    // Step 4: Verify weather cards are displayed
    const weatherCards = screen.getAllByTestId('weather-card');
    expect(weatherCards).toHaveLength(5); // 5-day forecast
    
    // Step 5: Test weather card interaction
    await userEvent.click(weatherCards[0]);
    // Could open detail modal in future iterations
  });
  
  it('should handle error scenarios gracefully', async () => {
    // Test search error
    mockWeatherService.searchCities.mockReturnValue(
      throwError({ status: 500, message: 'Server Error' })
    );
    
    const searchInput = screen.getByLabelText(/search for a city/i);
    await userEvent.type(searchInput, 'London');
    
    await waitFor(() => {
      expect(screen.getByText(/search service temporarily unavailable/i))
        .toBeInTheDocument();
    });
    
    // Test weather loading error
    const searchResponse = {
      cities: [{ name: 'London', country: 'UK', latitude: 51.5, longitude: -0.1 }]
    };
    
    mockWeatherService.searchCities.mockReturnValue(of(searchResponse));
    mockWeatherService.getWeather.mockReturnValue(
      throwError({ status: 502, message: 'Bad Gateway' })
    );
    
    const suggestion = screen.getByText('London, UK');
    await userEvent.click(suggestion);
    
    await waitFor(() => {
      expect(screen.getByText(/weather service temporarily unavailable/i))
        .toBeInTheDocument();
    });
  });
});
```

---

## Testing Strategy Summary

### Test Coverage Goals
- **Unit Tests**: 95%+ coverage for component logic
- **Integration Tests**: API integration and data flow
- **Accessibility Tests**: Screen reader compatibility and keyboard navigation
- **Error Handling**: All error scenarios and edge cases
- **Responsive Design**: Mobile and desktop layout validation

### Testing Tools
- **Angular Testing Library**: Component testing with user-centric approach
- **Jest**: Test runner and assertion library
- **MSW (Mock Service Worker)**: API mocking for integration tests
- **Axe-core**: Automated accessibility testing
- **Cypress**: Optional E2E testing for critical user journeys

### Continuous Integration
- Tests run on every commit and pull request
- Coverage reports generated and tracked over time
- Accessibility tests as part of CI pipeline
- Performance budgets to ensure app remains fast

This component architecture provides a solid foundation for building a maintainable, accessible, and user-friendly weather application with Angular 18, following modern best practices and ensuring comprehensive test coverage.
