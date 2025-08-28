import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { generateCorrelationId } from '../../utils/api-helpers';

/**
 * HTTP Interceptor that automatically adds correlation IDs to API requests
 * for request tracing and debugging
 */
@Injectable()
export class CorrelationIdInterceptor implements HttpInterceptor {
  
  /**
   * Intercept HTTP requests and add correlation ID if not already present
   */
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Only add correlation ID to API requests (not external resources)
    if (this.isApiRequest(request.url)) {
      // Check if correlation ID is already present
      if (!request.headers.has('x-correlation-id')) {
        const correlationId = generateCorrelationId();
        
        // Clone request and add correlation ID header
        const correlatedRequest = request.clone({
          headers: request.headers.set('x-correlation-id', correlationId)
        });
        
        return next.handle(correlatedRequest);
      }
    }
    
    return next.handle(request);
  }

  /**
   * Check if the request URL is an API request that needs correlation ID
   */
  private isApiRequest(url: string): boolean {
    // Add correlation ID to requests to our weather API
    const apiPatterns = [
      '/api/',
      'localhost:7071',
      '.azurewebsites.net'
    ];
    
    return apiPatterns.some(pattern => url.includes(pattern));
  }
}
