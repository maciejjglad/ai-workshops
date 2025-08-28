import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { processHttpError, extractProblemDetails } from '../../utils/error-handlers';

/**
 * HTTP Interceptor for logging errors and collecting metrics
 */
@Injectable()
export class ErrorLoggingInterceptor implements HttpInterceptor {
  
  /**
   * Intercept HTTP requests and log errors with detailed information
   */
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const startTime = Date.now();
    const correlationId = request.headers.get('x-correlation-id');
    
    return next.handle(request).pipe(
      tap({
        next: (event) => {
          // Log successful responses (optional, for debugging)
          this.logSuccessfulRequest(request, event, startTime, correlationId);
        },
        error: (error: HttpErrorResponse) => {
          // Log error with detailed information
          this.logErrorRequest(request, error, startTime, correlationId);
        }
      })
    );
  }

  /**
   * Log successful API requests (for debugging and metrics)
   */
  private logSuccessfulRequest(
    request: HttpRequest<any>, 
    event: HttpEvent<any>, 
    startTime: number,
    correlationId: string | null
  ): void {
    const duration = Date.now() - startTime;
    
    // Only log API requests, not static assets
    if (this.isApiRequest(request.url)) {
      console.debug('API Request Successful', {
        method: request.method,
        url: request.url,
        duration: `${duration}ms`,
        correlationId,
        timestamp: new Date().toISOString()
      });
    }
  }

  /**
   * Log error requests with comprehensive error information
   */
  private logErrorRequest(
    request: HttpRequest<any>, 
    error: HttpErrorResponse, 
    startTime: number,
    correlationId: string | null
  ): void {
    const duration = Date.now() - startTime;
    const processedError = processHttpError(error, this.getOperationName(request));
    const problemDetails = extractProblemDetails(error);
    
    const errorLog = {
      // Request information
      method: request.method,
      url: request.url,
      duration: `${duration}ms`,
      
      // Error information
      status: error.status,
      statusText: error.statusText,
      errorType: processedError.type,
      userMessage: processedError.userMessage,
      technicalMessage: processedError.technicalMessage,
      
      // Correlation and tracing
      correlationId: correlationId || problemDetails?.correlationId,
      traceId: problemDetails?.traceId,
      
      // RFC7807 Problem Details
      problemDetails: problemDetails ? {
        type: problemDetails.type,
        title: problemDetails.title,
        detail: problemDetails.detail,
        instance: problemDetails.instance,
        context: problemDetails.context
      } : null,
      
      // Timing and context
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      requestHeaders: this.sanitizeHeaders(request.headers),
      responseHeaders: this.sanitizeHeaders(error.headers)
    };

    // Log at appropriate level based on error type
    if (error.status >= 500) {
      console.error('API Server Error', errorLog);
    } else if (error.status >= 400) {
      console.warn('API Client Error', errorLog);
    } else if (error.status === 0) {
      console.error('API Network Error', errorLog);
    } else {
      console.error('API Unknown Error', errorLog);
    }

    // Send to external logging service in production
    this.sendToExternalLogger(errorLog);
  }

  /**
   * Extract operation name from request URL
   */
  private getOperationName(request: HttpRequest<any>): string {
    const url = request.url;
    
    if (url.includes('/cities/search')) {
      return 'searchCities';
    } else if (url.includes('/weather')) {
      return 'getWeather';
    } else if (url.includes('/health')) {
      return 'healthCheck';
    }
    
    return 'unknown';
  }

  /**
   * Check if the request URL is an API request
   */
  private isApiRequest(url: string): boolean {
    const apiPatterns = [
      '/api/',
      'localhost:7071',
      '.azurewebsites.net'
    ];
    
    return apiPatterns.some(pattern => url.includes(pattern));
  }

  /**
   * Sanitize headers by removing sensitive information
   */
  private sanitizeHeaders(headers: any): Record<string, string> {
    if (!headers) return {};
    
    const sanitized: Record<string, string> = {};
    const sensitiveHeaders = ['authorization', 'cookie', 'x-api-key'];
    
    // Convert HttpHeaders to plain object
    if (headers.keys) {
      headers.keys().forEach((key: string) => {
        const lowerKey = key.toLowerCase();
        if (!sensitiveHeaders.includes(lowerKey)) {
          sanitized[key] = headers.get(key);
        } else {
          sanitized[key] = '[REDACTED]';
        }
      });
    }
    
    return sanitized;
  }

  /**
   * Send error logs to external logging service (placeholder)
   * In a real application, this would integrate with services like:
   * - Azure Application Insights
   * - Google Cloud Logging
   * - AWS CloudWatch
   * - Sentry
   * - LogRocket
   */
  private sendToExternalLogger(errorLog: any): void {
    // Only send to external logger in production
    if (this.isProduction()) {
      try {
        // Example: Send to Application Insights
        // (window as any).appInsights?.trackException({
        //   exception: new Error(errorLog.technicalMessage),
        //   properties: errorLog
        // });
        
        // Example: Send to Sentry
        // Sentry.captureException(new Error(errorLog.technicalMessage), {
        //   tags: {
        //     errorType: errorLog.errorType,
        //     operation: errorLog.operation
        //   },
        //   extra: errorLog
        // });
        
        console.debug('Error logged to external service', { correlationId: errorLog.correlationId });
      } catch (loggingError) {
        console.error('Failed to send error to external logging service', loggingError);
      }
    }
  }

  /**
   * Check if running in production environment
   */
  private isProduction(): boolean {
    return !window.location.hostname.includes('localhost') && 
           !window.location.hostname.includes('127.0.0.1');
  }
}
