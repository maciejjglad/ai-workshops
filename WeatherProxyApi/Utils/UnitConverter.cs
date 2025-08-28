namespace WeatherProxyApi.Utils;

public static class UnitConverter
{
    public static double ConvertWindSpeedToKmh(double windSpeedMps)
        => Math.Round(windSpeedMps * 3.6, 1);

    public static double ConvertToFahrenheit(double celsius)
        => Math.Round((celsius * 9.0 / 5.0) + 32, 1);
}
