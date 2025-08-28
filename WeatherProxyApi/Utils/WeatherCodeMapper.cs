namespace WeatherProxyApi.Utils;

public static class WeatherCodeMapper
{
    public static (string Condition, string Icon) GetWeatherInfo(int code, bool isDay)
    {
        return code switch
        {
            0 => ("Clear sky", isDay ? "01d" : "01n"),
            1 => ("Mainly clear", isDay ? "02d" : "02n"),
            2 => ("Partly cloudy", isDay ? "03d" : "03n"),
            3 => ("Overcast", "04d"),
            45 or 48 => ("Fog", "50d"),
            51 or 53 or 55 => ("Drizzle", "09d"),
            61 or 63 or 65 => ("Rain", "10d"),
            71 or 73 or 75 => ("Snow", "13d"),
            80 or 81 or 82 => ("Rain showers", "09d"),
            85 or 86 => ("Snow showers", "13d"),
            95 => ("Thunderstorm", "11d"),
            96 or 99 => ("Thunderstorm with hail", "11d"),
            _ => ("Unknown", isDay ? "01d" : "01n")
        };
    }
}
