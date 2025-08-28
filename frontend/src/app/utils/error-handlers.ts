import { HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { ProblemDetails } from '../models/types';

/**
 * Error handling utilities for HTTP requests and API errors
 */

/**
 * Error classification enum for different error types
 */
export enum ErrorType {
  NETWORK = 'network',
  VALIDATION = 'validation',
  NOT_FOUND = 'not_found',
  RATE_LIMIT = 'rate_limit',
  SERVER = 'server',
  UPSTREAM = 'upstream',
  TIMEOUT = 'timeout',
  UNKNOWN = 'unknown'
}

/**
 * Processed error information interface
 */
export interface ProcessedError {
  type: ErrorType;
  userMessage: string;
  technicalMessage: string;
  shouldRetry: boolean;
  retryAfter?: number; // seconds
  correlationId?: string;
  statusCode?: number;
}

/**
 * Classify HTTP error and determine appropriate handling strategy
 */
export function classifyHttpError(error: HttpErrorResponse): ErrorType {
  // Network or connection errors
  if (error.status === 0 || (error as any).name === 'TimeoutError') {
    return ErrorType.NETWORK;
  }
  
  // Client validation errors
  if (error.status === 400) {
    return ErrorType.VALIDATION;
  }
  
  // Not found errors
  if (error.status === 404) {
    return ErrorType.NOT_FOUND;
  }
  
  // Rate limiting
  if (error.status === 429) {
    return ErrorType.RATE_LIMIT;
  }
  
  // Upstream service errors
  if (error.status === 502) {
    return ErrorType.UPSTREAM;
  }
  
  // Server errors
  if (error.status >= 500) {
    return ErrorType.SERVER;
  }
  
  return ErrorType.UNKNOWN;
}

/**
 * Process HTTP error and extract user-friendly information
 */
export function processHttpError(error: HttpErrorResponse, operation: string): ProcessedError {
  const errorType = classifyHttpError(error);
  const problemDetails = extractProblemDetails(error);
  
  let userMessage: string;
  let shouldRetry: boolean;
  let retryAfter: number | undefined;
  
  switch (errorType) {
    case ErrorType.NETWORK:
      userMessage = 'Connection problem. Please check your internet connection and try again.';
      shouldRetry = true;
      break;
      
    case ErrorType.VALIDATION:
      userMessage = problemDetails?.detail || 'Invalid request. Please check your input and try again.';
      shouldRetry = false;
      break;
      
    case ErrorType.NOT_FOUND:
      if (operation.includes('search')) {
        userMessage = 'No cities found. Try a different search term or check your spelling.';
      } else {
        userMessage = 'Weather data not available for this location. Please try a different city.';
      }
      shouldRetry = false;
      break;
      
    case ErrorType.RATE_LIMIT:
      retryAfter = extractRetryAfter(error);
      userMessage = retryAfter 
        ? `Too many requests. Please wait ${retryAfter} seconds and try again.`
        : 'Too many requests. Please wait a moment and try again.';
      shouldRetry = true;
      break;
      
    case ErrorType.UPSTREAM:
      userMessage = problemDetails?.detail || 'Weather service temporarily unavailable. Please try again later.';
      shouldRetry = false;
      break;
      
    case ErrorType.SERVER:
      userMessage = 'Service temporarily unavailable. Please try again in a few moments.';
      shouldRetry = true;
      break;
      
    case ErrorType.TIMEOUT:
      userMessage = 'Request timed out. Please check your connection and try again.';
      shouldRetry = true;
      break;
      
    default:
      userMessage = 'An unexpected error occurred. Please try again.';
      shouldRetry = false;
  }
  
  return {
    type: errorType,
    userMessage,
    technicalMessage: error.message || `HTTP ${error.status}: ${error.statusText}`,
    shouldRetry,
    retryAfter,
    correlationId: problemDetails?.correlationId,
    statusCode: error.status
  };
}

/**
 * Extract RFC7807 Problem Details from error response
 */
export function extractProblemDetails(error: HttpErrorResponse): ProblemDetails | null {
  try {
    if (error.error && typeof error.error === 'object') {
      const problemDetails = error.error as any;
      
      // Check if it looks like RFC7807 Problem Details
      if (problemDetails.type && problemDetails.title && problemDetails.status) {
        return problemDetails as ProblemDetails;
      }
    }
  } catch (e) {
    // Ignore parsing errors
  }
  
  return null;
}

/**
 * Extract Retry-After header value in seconds
 */
export function extractRetryAfter(error: HttpErrorResponse): number | undefined {
  const retryAfterHeader = error.headers?.get('Retry-After');
  if (!retryAfterHeader) {
    return undefined;
  }
  
  // Try to parse as seconds (integer)
  const seconds = parseInt(retryAfterHeader, 10);
  if (!isNaN(seconds) && seconds > 0) {
    return seconds;
  }
  
  // Try to parse as HTTP date
  const date = new Date(retryAfterHeader);
  if (!isNaN(date.getTime())) {
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    return Math.max(0, Math.ceil(diffMs / 1000));
  }
  
  return undefined;
}

/**
 * Create error handler function for RxJS error handling
 */
export function createErrorHandler(operation: string) {
  return (error: HttpErrorResponse): Observable<never> => {
    const processedError = processHttpError(error, operation);
    
    // Log technical details for debugging
    console.error(`Operation '${operation}' failed:`, {
      error: processedError.technicalMessage,
      type: processedError.type,
      statusCode: processedError.statusCode,
      correlationId: processedError.correlationId,
      originalError: error
    });
    
    // Throw user-friendly error
    return throwError(() => new Error(processedError.userMessage));
  };
}

/**
 * Calculate exponential backoff delay
 */
export function calculateBackoffDelay(attempt: number, baseDelay: number = 1000): number {
  // Exponential backoff with jitter: delay = baseDelay * 2^attempt + random(0, 1000)
  const exponentialDelay = baseDelay * Math.pow(2, attempt);
  const jitter = Math.random() * 1000;
  return Math.min(exponentialDelay + jitter, 30000); // Cap at 30 seconds
}

/**
 * Create retry observable with exponential backoff
 */
export function createRetryObservable(attempt: number, baseDelay: number = 1000): Observable<number> {
  const delay = calculateBackoffDelay(attempt, baseDelay);
  return timer(delay);
}

/**
 * Check if error should be retried based on type and attempt count
 */
export function shouldRetryError(processedError: ProcessedError, attempt: number, maxAttempts: number): boolean {
  if (attempt >= maxAttempts) {
    return false;
  }
  
  // Don't retry validation or not found errors
  if (processedError.type === ErrorType.VALIDATION || processedError.type === ErrorType.NOT_FOUND) {
    return false;
  }
  
  // Don't retry upstream service errors
  if (processedError.type === ErrorType.UPSTREAM) {
    return false;
  }
  
  return processedError.shouldRetry;
}

/**
 * Get user-friendly error message for toast notifications
 */
export function getToastErrorMessage(processedError: ProcessedError, operation: string): string {
  const actionContext = operation.includes('search') ? 'searching for cities' : 'loading weather data';
  
  switch (processedError.type) {
    case ErrorType.NETWORK:
      return `Connection lost while ${actionContext}. Check your internet connection.`;
      
    case ErrorType.RATE_LIMIT:
      return processedError.retryAfter 
        ? `Too many requests. Please wait ${processedError.retryAfter} seconds.`
        : 'Too many requests. Please wait a moment.';
        
    case ErrorType.SERVER:
    case ErrorType.UPSTREAM:
      return `Service temporarily unavailable while ${actionContext}. Please try again later.`;
      
    default:
      return processedError.userMessage;
  }
}

/**
 * Format validation errors from ProblemDetails
 */
export function formatValidationErrors(problemDetails: ProblemDetails): string[] {
  const errors: string[] = [];
  
  if (problemDetails.errors) {
    for (const [field, fieldErrors] of Object.entries(problemDetails.errors)) {
      if (Array.isArray(fieldErrors)) {
        fieldErrors.forEach(error => {
          errors.push(`${field}: ${error}`);
        });
      }
    }
  }
  
  if (errors.length === 0 && problemDetails.detail) {
    errors.push(problemDetails.detail);
  }
  
  return errors;
}

/**
 * Check if error indicates offline status
 */
export function isOfflineError(error: HttpErrorResponse): boolean {
  return error.status === 0 || 
         (error as any).name === 'TimeoutError' ||
         (error.error instanceof Error && error.error.message.includes('Failed to fetch'));
}

/**
 * Check if error is recoverable (can be retried)
 */
export function isRecoverableError(processedError: ProcessedError): boolean {
  const recoverableTypes = [
    ErrorType.NETWORK,
    ErrorType.TIMEOUT,
    ErrorType.RATE_LIMIT,
    ErrorType.SERVER
  ];
  
  return recoverableTypes.includes(processedError.type);
}
