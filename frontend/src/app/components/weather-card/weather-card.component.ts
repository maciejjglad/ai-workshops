import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

import { DailyWeatherView, TemperatureUnit } from '../../models/types';
import { getWeatherIconUrl } from '../../utils/weather-code';
import { generateId } from '../../utils/formatters';

@Component({
  selector: 'app-weather-card',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule
  ],
  templateUrl: './weather-card.component.html',
  styleUrl: './weather-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WeatherCardComponent {
  @Input({ required: true }) dayData!: DailyWeatherView;
  @Input() temperatureUnit: TemperatureUnit = TemperatureUnit.CELSIUS;
  @Input() compact: boolean = false;
  @Input() selected: boolean = false;

  @Output() cardClicked = new EventEmitter<DailyWeatherView>();
  @Output() cardFocused = new EventEmitter<DailyWeatherView>();

  readonly cardId = generateId('weather-card');

  /**
   * Get comprehensive ARIA label for the card
   */
  getCardAriaLabel(): string {
    return `Weather for ${this.dayData.dayName}, ${this.dayData.date}. 
      ${this.dayData.condition}. 
      High ${this.dayData.temperatureMax}, low ${this.dayData.temperatureMin}. 
      ${this.dayData.precipitationChance} chance of precipitation. 
      Wind ${this.dayData.windSpeed}.`.replace(/\s+/g, ' ').trim();
  }

  /**
   * Get weather icon URL
   */
  getWeatherIconUrl(iconCode: string): string {
    return getWeatherIconUrl(iconCode);
  }

  /**
   * Handle card click
   */
  onCardClick(): void {
    this.cardClicked.emit(this.dayData);
  }

  /**
   * Handle card focus
   */
  onCardFocus(): void {
    this.cardFocused.emit(this.dayData);
  }

  /**
   * Handle keyboard navigation
   */
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onCardClick();
    }
  }
}
