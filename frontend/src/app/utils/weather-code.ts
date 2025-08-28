// WMO Weather Interpretation Codes mapping
// Based on Open-Meteo API documentation

export interface WeatherCodeInfo {
  condition: string;
  icon: string;
  description: string;
}

export const WEATHER_CODE_MAP: Record<number, WeatherCodeInfo> = {
  // Clear Sky
  0: {
    condition: 'Clear',
    icon: '01', // Will be suffixed with 'd' or 'n'
    description: 'Clear sky'
  },
  
  // Mainly Clear, Partly Cloudy, Overcast
  1: {
    condition: 'Mostly Clear',
    icon: '02',
    description: 'Mainly clear'
  },
  2: {
    condition: 'Partly Cloudy',
    icon: '03',
    description: 'Partly cloudy'
  },
  3: {
    condition: 'Overcast',
    icon: '04',
    description: 'Overcast'
  },
  
  // Fog
  45: {
    condition: 'Foggy',
    icon: '50',
    description: 'Fog'
  },
  48: {
    condition: 'Foggy',
    icon: '50',
    description: 'Depositing rime fog'
  },
  
  // Drizzle
  51: {
    condition: 'Light Drizzle',
    icon: '09',
    description: 'Drizzle: Light intensity'
  },
  53: {
    condition: 'Drizzle',
    icon: '09',
    description: 'Drizzle: Moderate intensity'
  },
  55: {
    condition: 'Heavy Drizzle',
    icon: '09',
    description: 'Drizzle: Dense intensity'
  },
  
  // Freezing Drizzle
  56: {
    condition: 'Freezing Drizzle',
    icon: '09',
    description: 'Freezing Drizzle: Light intensity'
  },
  57: {
    condition: 'Freezing Drizzle',
    icon: '09',
    description: 'Freezing Drizzle: Dense intensity'
  },
  
  // Rain
  61: {
    condition: 'Light Rain',
    icon: '10',
    description: 'Rain: Slight intensity'
  },
  63: {
    condition: 'Rain',
    icon: '10',
    description: 'Rain: Moderate intensity'
  },
  65: {
    condition: 'Heavy Rain',
    icon: '10',
    description: 'Rain: Heavy intensity'
  },
  
  // Freezing Rain
  66: {
    condition: 'Freezing Rain',
    icon: '10',
    description: 'Freezing Rain: Light intensity'
  },
  67: {
    condition: 'Freezing Rain',
    icon: '10',
    description: 'Freezing Rain: Heavy intensity'
  },
  
  // Snow
  71: {
    condition: 'Light Snow',
    icon: '13',
    description: 'Snow fall: Slight intensity'
  },
  73: {
    condition: 'Snow',
    icon: '13',
    description: 'Snow fall: Moderate intensity'
  },
  75: {
    condition: 'Heavy Snow',
    icon: '13',
    description: 'Snow fall: Heavy intensity'
  },
  
  // Snow Grains
  77: {
    condition: 'Snow Grains',
    icon: '13',
    description: 'Snow grains'
  },
  
  // Rain Showers
  80: {
    condition: 'Light Showers',
    icon: '09',
    description: 'Rain showers: Slight intensity'
  },
  81: {
    condition: 'Showers',
    icon: '09',
    description: 'Rain showers: Moderate intensity'
  },
  82: {
    condition: 'Heavy Showers',
    icon: '09',
    description: 'Rain showers: Violent intensity'
  },
  
  // Snow Showers
  85: {
    condition: 'Snow Showers',
    icon: '13',
    description: 'Snow showers: Slight intensity'
  },
  86: {
    condition: 'Heavy Snow Showers',
    icon: '13',
    description: 'Snow showers: Heavy intensity'
  },
  
  // Thunderstorms
  95: {
    condition: 'Thunderstorm',
    icon: '11',
    description: 'Thunderstorm: Slight or moderate'
  },
  96: {
    condition: 'Thunderstorm with Hail',
    icon: '11',
    description: 'Thunderstorm with slight hail'
  },
  99: {
    condition: 'Severe Thunderstorm',
    icon: '11',
    description: 'Thunderstorm with heavy hail'
  }
};

// Helper function to get weather info with day/night icon
export function getWeatherInfo(code: number, isDay: boolean): WeatherCodeInfo {
  const info = WEATHER_CODE_MAP[code];
  if (!info) {
    return {
      condition: 'Unknown',
      icon: '01d',
      description: 'Unknown weather condition'
    };
  }
  
  return {
    ...info,
    icon: `${info.icon}${isDay ? 'd' : 'n'}`
  };
}

// Helper function to get weather icon URL
export function getWeatherIconUrl(iconCode: string): string {
  // Using OpenWeatherMap icons as they're free and widely used
  return `https://openweathermap.org/img/wn/${iconCode}@2x.png`;
}
