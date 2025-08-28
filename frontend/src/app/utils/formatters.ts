// Utility functions for formatting data for UI display

import { TemperatureUnit } from '../models/types';

/**
 * Format temperature with unit
 */
export function formatTemperature(tempC: number, unit: TemperatureUnit = TemperatureUnit.CELSIUS): string {
  if (unit === TemperatureUnit.FAHRENHEIT) {
    const tempF = (tempC * 9/5) + 32;
    return `${Math.round(tempF * 10) / 10}°F`;
  }
  return `${Math.round(tempC * 10) / 10}°C`;
}

/**
 * Format wind speed
 */
export function formatWindSpeed(speedKph: number): string {
  return `${Math.round(speedKph * 10) / 10} km/h`;
}

/**
 * Format precipitation chance
 */
export function formatPrecipitation(percentage: number): string {
  return `${Math.round(percentage)}%`;
}

/**
 * Format local time from ISO string and timezone
 */
export function formatLocalTime(isoTime: string, timezone: string): string {
  try {
    const date = new Date(isoTime);
    return new Intl.DateTimeFormat('en-US', {
      timeZone: timezone,
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    }).format(date);
  } catch (error) {
    console.warn('Error formatting local time:', error);
    return new Date(isoTime).toLocaleTimeString([], { 
      hour: 'numeric', 
      minute: '2-digit', 
      hour12: true 
    });
  }
}

/**
 * Format date for display
 */
export function formatDate(dateString: string): string {
  try {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    }).format(date);
  } catch (error) {
    console.warn('Error formatting date:', error);
    return dateString;
  }
}

/**
 * Get day name for forecast (Today, Tomorrow, or day name)
 */
export function getDayName(dateString: string, index: number): string {
  if (index === 0) return 'Today';
  if (index === 1) return 'Tomorrow';
  
  try {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      weekday: 'long'
    }).format(date);
  } catch (error) {
    console.warn('Error formatting day name:', error);
    return `Day ${index + 1}`;
  }
}

/**
 * Format city display name
 */
export function formatCityDisplay(city: { name: string; country: string; region?: string }): string {
  console.log('🏛️ formatCityDisplay called with city:', city);
  console.log('🏛️ city.name:', city.name, 'city.country:', city.country, 'city.region:', city.region);
  
  if (city.region && city.region !== city.name) {
    const result = `${city.name}, ${city.region}, ${city.country}`;
    console.log('🏛️ formatCityDisplay result (with region):', result);
    return result;
  }
  const result = `${city.name}, ${city.country}`;
  console.log('🏛️ formatCityDisplay result (without region):', result);
  return result;
}

/**
 * Format location display
 */
export function formatLocationDisplay(name: string, country: string): string {
  return `${name}, ${country}`;
}

/**
 * Generate unique ID for components
 */
export function generateId(prefix: string = 'weather'): string {
  return `${prefix}-${Math.random().toString(36).substr(2, 9)}`;
}

/**
 * Debounce function for search input
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout>;
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => func.apply(null, args), delay);
  };
}

/**
 * Validate search query
 */
export function validateSearchQuery(query: string): { isValid: boolean; error?: string } {
  if (!query || query.trim().length === 0) {
    return { isValid: false, error: 'Please enter a city name' };
  }
  
  if (query.trim().length < 2) {
    return { isValid: false, error: 'Please enter at least 2 characters' };
  }
  
  if (query.length > 100) {
    return { isValid: false, error: 'Search query is too long' };
  }
  
  // Basic pattern validation for city names - allow letters, numbers, spaces, common punctuation
  // Allow Unicode letters and numbers, spaces, hyphens, apostrophes, dots, commas, parentheses
  const pattern = /^[\p{L}\p{N}\s\-''.,()&]+$/u;
  if (!pattern.test(query)) {
    return { isValid: false, error: 'Search query contains invalid characters' };
  }
  
  return { isValid: true };
}
