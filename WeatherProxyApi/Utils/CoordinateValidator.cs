namespace WeatherProxyApi.Utils;

/// <summary>
/// Utility for validating geographic coordinates
/// </summary>
public static class CoordinateValidator
{
    /// <summary>
    /// Validates that coordinates are within valid geographic ranges
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <returns>True if coordinates are valid</returns>
    public static bool ValidateCoordinates(double latitude, double longitude)
    {
        return IsValidLatitude(latitude) && IsValidLongitude(longitude);
    }

    /// <summary>
    /// Validates latitude is within valid range
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <returns>True if latitude is valid</returns>
    public static bool IsValidLatitude(double latitude)
    {
        return latitude >= -90.0 && latitude <= 90.0 && !double.IsNaN(latitude) && !double.IsInfinity(latitude);
    }

    /// <summary>
    /// Validates longitude is within valid range
    /// </summary>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <returns>True if longitude is valid</returns>
    public static bool IsValidLongitude(double longitude)
    {
        return longitude >= -180.0 && longitude <= 180.0 && !double.IsNaN(longitude) && !double.IsInfinity(longitude);
    }

    /// <summary>
    /// Validates that coordinates are not at the null island (0,0) unless specifically allowed
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <param name="allowNullIsland">Whether to allow coordinates at (0,0)</param>
    /// <returns>True if coordinates are meaningful</returns>
    public static bool ValidateMeaningfulCoordinates(double latitude, double longitude, bool allowNullIsland = false)
    {
        if (!ValidateCoordinates(latitude, longitude))
            return false;

        if (!allowNullIsland && Math.Abs(latitude) < 0.001 && Math.Abs(longitude) < 0.001)
            return false;

        return true;
    }
}
