using System.Globalization;
using System.Text.RegularExpressions;
using MarineNav.Utility.Models;

namespace MarineNav.Utility.Parsing;

/// <summary>
/// Parses coordinate strings in various formats.
/// </summary>
public class CoordinateParser
{
    // Regex patterns for different coordinate formats
    private static readonly Regex DmsPattern = new(
        @"^\s*([NSEW])?\s*(-?\d{1,3})[°\s]+(\d{1,2})[′'\s]+(\d{1,2}(?:\.\d+)?)[″""\s]*([NSEW])?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DmPattern = new(
        @"^\s*([NSEW])?\s*(-?\d{1,3})[°\s]+(\d{1,2}(?:\.\d+)?)[′'\s]*([NSEW])?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DecimalPattern = new(
        @"^\s*([NSEW])?\s*(-?\d{1,3}(?:\.\d+)?)[°\s]*([NSEW])?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parses a coordinate component (latitude or longitude) from a string.
    /// </summary>
    /// <param name="input">Input string in any supported format</param>
    /// <param name="isLatitude">True if parsing latitude, false for longitude</param>
    /// <returns>Decimal degrees value</returns>
    public static double Parse(string input, bool isLatitude)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("Coordinate input cannot be empty");

        input = input.Trim().Replace(",", " ");

        // Try regex patterns first for formatted inputs
        if (TryParseWithRegex(input, out double regexValue))
        {
            double maxValue = isLatitude ? 90.0 : 180.0;
            if (Math.Abs(regexValue) > maxValue)
                throw new FormatException($"Coordinate value {regexValue} is out of range for {(isLatitude ? "latitude" : "longitude")}");
            return regexValue;
        }

        // Try parsing as space-separated tokens
        var tokens = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length >= 1 && tokens.Length <= 4)
        {
            double result = tokens.Length switch
            {
                1 => ParseSingleToken(tokens[0]),
                2 => ParseTwoTokens(tokens[0], tokens[1]),
                3 => ParseThreeTokens(tokens[0], tokens[1], tokens[2]),
                4 => ParseThreeTokens(tokens[0], tokens[1], tokens[2], tokens[3]),
                _ => throw new FormatException("Invalid coordinate format")
            };

            // Validate range
            double maxValue = isLatitude ? 90.0 : 180.0;
            if (Math.Abs(result) > maxValue)
                throw new FormatException($"Coordinate value {result} is out of range for {(isLatitude ? "latitude" : "longitude")}");

            return result;
        }

        throw new FormatException($"Unable to parse coordinate: {input}");
    }

    /// <summary>
    /// Attempts to parse a full coordinate pair (latitude, longitude).
    /// </summary>
    public static bool TryParseCoordinate(string latInput, string lonInput, out Coordinate? coordinate)
    {
        coordinate = null;
        try
        {
            double lat = Parse(latInput, true);
            double lon = Parse(lonInput, false);
            coordinate = new Coordinate(lat, lon);
            return coordinate.IsValid;
        }
        catch
        {
            return false;
        }
    }

    private static double ParseSingleToken(string token)
    {
        // Check for hemisphere suffix or prefix
        char? hemisphere = null;
        string numberPart = token;

        if (token.Length > 0)
        {
            // Check trailing hemisphere
            char last = char.ToUpper(token[^1]);
            if (last is 'N' or 'S' or 'E' or 'W')
            {
                hemisphere = last;
                numberPart = token[..^1];
            }
            // Check leading hemisphere
            else
            {
                char first = char.ToUpper(token[0]);
                if (first is 'N' or 'S' or 'E' or 'W')
                {
                    hemisphere = first;
                    numberPart = token[1..];
                }
            }
        }

        // Remove degree symbol
        numberPart = numberPart.Replace("°", "").Trim();

        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            throw new FormatException($"Cannot parse number: {token}");

        return ApplyHemisphere(value, hemisphere);
    }

    private static double ParseTwoTokens(string token1, string token2)
    {
        // DDD MM.MMM format
        char? hemisphere = ExtractHemisphere(ref token2);
        if (hemisphere == null)
            hemisphere = ExtractHemisphere(ref token1);

        string degStr = token1.Replace("°", "").Trim();
        string minStr = token2.Replace("°", "").Replace("′", "").Replace("'", "").Trim();

        if (!double.TryParse(degStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
            throw new FormatException($"Cannot parse degrees: {degStr}");

        if (!double.TryParse(minStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
            throw new FormatException($"Cannot parse minutes: {minStr}");

        if (minutes < 0 || minutes >= 60)
            throw new FormatException($"Minutes must be between 0 and 60: {minutes}");

        double value = Math.Abs(degrees) + minutes / 60.0;
        if (degrees < 0)
            value = -value;

        return ApplyHemisphere(value, hemisphere);
    }

    private static double ParseThreeTokens(string token1, string token2, string token3, string? token4 = null)
    {
        // DDD MM SS.S format
        char? hemisphere = null;
        if (token4 != null)
        {
            hemisphere = ExtractHemisphere(ref token4);
        }
        if (hemisphere == null)
            hemisphere = ExtractHemisphere(ref token3);
        if (hemisphere == null)
            hemisphere = ExtractHemisphere(ref token2);
        if (hemisphere == null)
            hemisphere = ExtractHemisphere(ref token1);

        string degStr = token1.Replace("°", "").Trim();
        string minStr = token2.Replace("°", "").Replace("′", "").Replace("'", "").Trim();
        string secStr = token3.Replace("°", "").Replace("″", "").Replace("\"", "").Replace("'", "").Trim();

        if (!double.TryParse(degStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
            throw new FormatException($"Cannot parse degrees: {degStr}");

        if (!double.TryParse(minStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
            throw new FormatException($"Cannot parse minutes: {minStr}");

        if (!double.TryParse(secStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
            throw new FormatException($"Cannot parse seconds: {secStr}");

        if (minutes < 0 || minutes >= 60)
            throw new FormatException($"Minutes must be between 0 and 60: {minutes}");

        if (seconds < 0 || seconds >= 60)
            throw new FormatException($"Seconds must be between 0 and 60: {seconds}");

        double value = Math.Abs(degrees) + minutes / 60.0 + seconds / 3600.0;
        if (degrees < 0)
            value = -value;

        return ApplyHemisphere(value, hemisphere);
    }

    private static char? ExtractHemisphere(ref string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        char last = char.ToUpper(token[^1]);
        if (last is 'N' or 'S' or 'E' or 'W')
        {
            token = token[..^1].Trim();
            return last;
        }

        // Check first character too
        char first = char.ToUpper(token[0]);
        if (first is 'N' or 'S' or 'E' or 'W')
        {
            token = token[1..].Trim();
            return first;
        }

        return null;
    }

    private static double ApplyHemisphere(double value, char? hemisphere)
    {
        if (hemisphere == null)
            return value;

        char h = char.ToUpper(hemisphere.Value);
        if (h is 'S' or 'W')
            return -Math.Abs(value);
        else if (h is 'N' or 'E')
            return Math.Abs(value);

        return value;
    }

    private static bool TryParseWithRegex(string input, out double value)
    {
        value = 0;

        // Try DMS pattern
        var match = DmsPattern.Match(input);
        if (match.Success)
        {
            char? hemispherePrefix = match.Groups[1].Success ? match.Groups[1].Value[0] : null;
            double degrees = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            double minutes = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            double seconds = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            char? hemisphereSuffix = match.Groups[5].Success ? match.Groups[5].Value[0] : null;
            char? hemisphere = hemisphereSuffix ?? hemispherePrefix;

            if (minutes < 0 || minutes >= 60)
                throw new FormatException($"Minutes must be between 0 and 60: {minutes}");
            if (seconds < 0 || seconds >= 60)
                throw new FormatException($"Seconds must be between 0 and 60: {seconds}");

            value = Math.Abs(degrees) + minutes / 60.0 + seconds / 3600.0;
            if (degrees < 0)
                value = -value;
            value = ApplyHemisphere(value, hemisphere);
            return true;
        }

        // Try DM pattern
        match = DmPattern.Match(input);
        if (match.Success)
        {
            char? hemispherePrefix = match.Groups[1].Success ? match.Groups[1].Value[0] : null;
            double degrees = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            double minutes = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            char? hemisphereSuffix = match.Groups[4].Success ? match.Groups[4].Value[0] : null;
            char? hemisphere = hemisphereSuffix ?? hemispherePrefix;

            if (minutes < 0 || minutes >= 60)
                throw new FormatException($"Minutes must be between 0 and 60: {minutes}");

            value = Math.Abs(degrees) + minutes / 60.0;
            if (degrees < 0)
                value = -value;
            value = ApplyHemisphere(value, hemisphere);
            return true;
        }

        // Try decimal pattern
        match = DecimalPattern.Match(input);
        if (match.Success)
        {
            char? hemispherePrefix = match.Groups[1].Success ? match.Groups[1].Value[0] : null;
            value = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            char? hemisphereSuffix = match.Groups[3].Success ? match.Groups[3].Value[0] : null;
            char? hemisphere = hemisphereSuffix ?? hemispherePrefix;
            value = ApplyHemisphere(value, hemisphere);
            return true;
        }

        return false;
    }
}
