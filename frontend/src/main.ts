import 'zone.js';  // Must be first import
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { importProvidersFrom } from '@angular/core';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { CorrelationIdInterceptor } from './app/services/interceptors/correlation-id.interceptor';
import { ErrorLoggingInterceptor } from './app/services/interceptors/error-logging.interceptor';

console.log('Main.ts: Starting Weather App bootstrap');

bootstrapApplication(AppComponent, {
  providers: [
    // HTTP Client with interceptors
    provideHttpClient(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CorrelationIdInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorLoggingInterceptor,
      multi: true
    },
    
    // Angular Material and animations
    provideAnimations(),
    importProvidersFrom(MatSnackBarModule),
  ]
}).then(() => {
  console.log('Main.ts: Weather App bootstrap successful');
}).catch(err => {
  console.error('Main.ts: Bootstrap error:', err);
});