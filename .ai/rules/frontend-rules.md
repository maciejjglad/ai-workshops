# Frontend AI Development Rules
***Angular 18 (Standalone Components) with TypeScript Strict***

## Principles
- **Standalone Components**: Use Angular 18 standalone components, avoid NgModules
- **Reactive Programming**: Leverage RxJS for all async operations and state management
- **Type Safety**: Use TypeScript strict mode, define all interfaces and types
- **Component Composition**: Build small, reusable components with clear responsibilities
- **Stateless Services**: No global state store, manage state through services and observables
- **Material Design**: Use Angular Material components for consistent UI/UX
- **Responsive First**: Design with CSS Grid for mobile-first responsive layouts
- **Error Boundaries**: Handle all HTTP errors gracefully with user-friendly feedback

## Folder/File Conventions

```
src/app/
├── components/
│   ├── city-search/
│   │   ├── city-search.component.ts    # Standalone component
│   │   ├── city-search.component.html
│   │   ├── city-search.component.scss
│   │   └── city-search.component.spec.ts
│   ├── weather-panel/
│   │   ├── weather-panel.component.ts
│   │   ├── weather-panel.component.html
│   │   ├── weather-panel.component.scss
│   │   └── weather-panel.component.spec.ts
│   └── weather-card/
│       ├── weather-card.component.ts
│       ├── weather-card.component.html
│       ├── weather-card.component.scss
│       └── weather-card.component.spec.ts
├── services/
│   ├── weather.service.ts              # HTTP services
│   └── weather.service.spec.ts
├── models/
│   ├── city.interface.ts               # Type definitions
│   ├── weather.interface.ts
│   └── api-response.interface.ts
├── utils/
│   ├── weather-code.ts                 # Helper functions
│   └── form-validators.ts
├── environments/
│   ├── environment.ts                  # Dev config
│   └── environment.prod.ts             # Prod config
└── shared/
    ├── pipes/                          # Custom pipes
    └── directives/                     # Custom directives
```

## Coding Conventions

### Naming
- **Components**: PascalCase with suffix (`CitySearchComponent`, `WeatherPanelComponent`)
- **Services**: PascalCase with suffix (`WeatherService`, `HttpErrorService`)
- **Interfaces**: PascalCase with `Interface` suffix (`WeatherInterface`, `CityInterface`)
- **Files**: kebab-case (`city-search.component.ts`, `weather.service.ts`)
- **Variables/Methods**: camelCase (`searchTerm`, `getWeatherData`)
- **Constants**: UPPER_SNAKE_CASE (`API_BASE_URL`, `DEFAULT_DEBOUNCE_TIME`)
- **CSS Classes**: kebab-case (`weather-card`, `search-input`)

### Error Handling
- Use RxJS `catchError` operator for HTTP error handling
- Show user-friendly error messages via Angular Material snackbar/toast
- Log errors to console with correlation context
- Provide fallback UI states for failed requests
- Handle network connectivity issues gracefully

```typescript
// Example error handling pattern
return this.http.get<WeatherData[]>(url).pipe(
  catchError((error: HttpErrorResponse) => {
    this.handleHttpError(error);
    return of([]); // Return empty array as fallback
  })
);
```

### Nullability/Strictness
- Enable TypeScript strict mode in `tsconfig.json`
- Use `?` for optional properties (`city?: string`)
- Prefer `undefined` over `null` for optional values
- Use type guards for runtime type checking
- Always handle async data with loading states

```typescript
interface WeatherData {
  temperature: number;
  humidity?: number;        // Optional
  description: string;
  timestamp: Date;
}
```

## Testing Conventions

### Structure
- **Unit Tests**: Test component logic, services, and utilities in isolation
- **Component Tests**: Use Angular Testing Library for user interaction testing
- **Test File Naming**: `{component/service}.spec.ts`
- **Test Describe Blocks**: Use component/service name as main describe block

### Tools & Patterns
```typescript
// Component testing with Angular Testing Library
import { render, screen, fireEvent } from '@testing-library/angular';

test('should search cities when user types', async () => {
  await render(CitySearchComponent);
  const input = screen.getByRole('textbox');
  fireEvent.input(input, { target: { value: 'London' } });
  expect(screen.getByText('London, UK')).toBeInTheDocument();
});

// Service testing with Vitest
import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

describe('WeatherService', () => {
  let service: WeatherService;
  let httpMock: HttpTestingController;
  
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(WeatherService);
    httpMock = TestBed.inject(HttpTestingController);
  });
});
```

### Coverage
- Minimum 80% code coverage
- Test happy path, error scenarios, and edge cases
- Mock HTTP calls with `HttpTestingController`
- Test RxJS streams with marble testing when complex

## Do/Don't Table

| ✅ DO | ❌ DON'T |
|--------|----------|
| Use standalone components with `bootstrapApplication` | Use NgModules for new components |
| Handle HTTP errors with RxJS `catchError` | Let HTTP errors crash the app |
| Use Angular Material components | Create custom UI components from scratch |
| Type all variables and function returns | Use `any` type |
| Use RxJS operators for async operations | Use Promises or async/await for HTTP |
| Implement loading states for async operations | Leave users without feedback during loading |
| Use `OnPush` change detection strategy | Rely on default change detection |
| Unsubscribe from observables in `ngOnDestroy` | Let observables cause memory leaks |
| Use CSS Grid for responsive layouts | Use complex flexbox for layout |
| Validate forms with Angular validators | Skip form validation |
| Use environment files for configuration | Hardcode API URLs and config |
| Write tests for components and services | Skip testing for "simple" components |

## "When in Doubt" Checklist

**Before Writing Components:**
- [ ] Is this a standalone component with proper imports?
- [ ] Does it have a single, clear responsibility?
- [ ] Are all inputs/outputs properly typed?
- [ ] Is `OnPush` change detection strategy used?

**RxJS & HTTP:**
- [ ] Using RxJS operators instead of nested subscriptions?
- [ ] Handling HTTP errors with `catchError`?
- [ ] Unsubscribing from observables in `ngOnDestroy`?
- [ ] Using `debounceTime` for search inputs?

**TypeScript & Types:**
- [ ] All variables and function returns properly typed?
- [ ] Using interfaces for object shapes?
- [ ] Avoiding `any` type completely?
- [ ] Handling optional properties with `?` operator?

**UI/UX:**
- [ ] Using Angular Material components consistently?
- [ ] Implementing loading states for async operations?
- [ ] Providing error feedback to users?
- [ ] Is the layout responsive with CSS Grid?

**Forms & Validation:**
- [ ] Using reactive forms with proper validation?
- [ ] Showing validation errors clearly to users?
- [ ] Disabling submit buttons during form submission?
- [ ] Handling form reset after successful submission?

**Performance:**
- [ ] Using `OnPush` change detection strategy?
- [ ] Implementing virtual scrolling for large lists?
- [ ] Lazy loading routes and components where appropriate?
- [ ] Optimizing bundle size with tree-shaking?

**Testing:**
- [ ] Unit tests for all service methods?
- [ ] Component tests for user interactions?
- [ ] Mocking HTTP calls in tests?
- [ ] Testing error scenarios and edge cases?

**Accessibility:**
- [ ] Using semantic HTML elements?
- [ ] Adding ARIA labels where needed?
- [ ] Supporting keyboard navigation?
- [ ] Testing with screen readers?
