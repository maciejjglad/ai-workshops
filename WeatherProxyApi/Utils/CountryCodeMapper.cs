namespace WeatherProxyApi.Utils;

public static class CountryCodeMapper
{
    private static readonly Dictionary<string, string> CountryCodes = new()
    {
        ["US"] = "United States", ["CA"] = "Canada", ["GB"] = "United Kingdom",
        ["DE"] = "Germany", ["FR"] = "France", ["PL"] = "Poland", ["ES"] = "Spain",
        ["IT"] = "Italy", ["NL"] = "Netherlands", ["BE"] = "Belgium", ["AT"] = "Austria",
        ["CH"] = "Switzerland", ["CZ"] = "Czech Republic", ["DK"] = "Denmark",
        ["SE"] = "Sweden", ["NO"] = "Norway", ["FI"] = "Finland", ["IE"] = "Ireland",
        ["PT"] = "Portugal", ["GR"] = "Greece", ["HU"] = "Hungary", ["SK"] = "Slovakia",
        ["SI"] = "Slovenia", ["HR"] = "Croatia", ["BG"] = "Bulgaria", ["RO"] = "Romania",
        ["LT"] = "Lithuania", ["LV"] = "Latvia", ["EE"] = "Estonia", ["LU"] = "Luxembourg",
        ["MT"] = "Malta", ["CY"] = "Cyprus", ["AU"] = "Australia", ["NZ"] = "New Zealand",
        ["JP"] = "Japan", ["KR"] = "South Korea", ["CN"] = "China", ["IN"] = "India",
        ["BR"] = "Brazil", ["AR"] = "Argentina", ["MX"] = "Mexico", ["RU"] = "Russia"
    };

    public static string GetCountryName(string countryCode)
        => CountryCodes.TryGetValue(countryCode.ToUpperInvariant(), out var name) ? name : countryCode;
}
