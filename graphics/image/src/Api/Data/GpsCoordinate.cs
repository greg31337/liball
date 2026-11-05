using System;
using System.Globalization;

namespace image;

/// <summary>Stores a normalized geographic position extracted from image metadata.</summary>
/// <remarks>Latitude and longitude use signed decimal degrees. Altitude uses meters, with negative values below sea level.</remarks>
public class GpsCoordinate
{
    /// <summary>Indicates whether enough source values were available to form a coordinate.</summary>
    public bool IsValid;
    /// <summary>The signed latitude in decimal degrees, where south is negative.</summary>
    public double Latitude;
    /// <summary>The signed longitude in decimal degrees, where west is negative.</summary>
    public double Longitude;
    /// <summary>The signed altitude in meters, where values below sea level are negative.</summary>
    public double Altitude;
    /// <summary>The normalized latitude reference, normally <c>N</c> or <c>S</c>.</summary>
    public string LatitudeReference;
    /// <summary>The normalized longitude reference, normally <c>E</c> or <c>W</c>.</summary>
    public string LongitudeReference;
    /// <summary>The original altitude reference text when one was supplied.</summary>
    public string AltitudeReference;

    /// <summary>Creates an invalid coordinate whose numeric values are zero.</summary>
    public GpsCoordinate()
    {
        IsValid = false;
        Latitude = 0;
        Longitude = 0;
        Altitude = 0;
        LatitudeReference = "";
        LongitudeReference = "";
        AltitudeReference = "";
    }

    /// <summary>Formats the coordinate as latitude, longitude, and optional altitude text.</summary>
    /// <returns>An invariant-culture display string, or an empty string when <see cref="IsValid"/> is false.</returns>
    internal string ToDisplayText()
    {
        return FormatDisplay(this);
    }

    /// <summary>Builds a coordinate from the degrees, minutes, and seconds values commonly stored by EXIF GPS tags.</summary>
    /// <remarks>Cardinal references control the signs. Negative degree or altitude inputs are also preserved when no negative reference is supplied.</remarks>
    /// <param name="latitudeDegrees">The latitude degree component.</param>
    /// <param name="latitudeMinutes">The latitude minute component.</param>
    /// <param name="latitudeSeconds">The latitude second component.</param>
    /// <param name="latitudeReference">The latitude reference, such as <c>N</c> or <c>S</c>, or null to infer it from the numeric value.</param>
    /// <param name="longitudeDegrees">The longitude degree component.</param>
    /// <param name="longitudeMinutes">The longitude minute component.</param>
    /// <param name="longitudeSeconds">The longitude second component.</param>
    /// <param name="longitudeReference">The longitude reference, such as <c>E</c> or <c>W</c>, or null to infer it from the numeric value.</param>
    /// <param name="altitude">The altitude magnitude in meters.</param>
    /// <param name="altitudeReference">The altitude reference; <c>1</c> means below sea level in EXIF data. Null is treated as an empty reference.</param>
    /// <returns>A valid coordinate containing signed decimal values and normalized references.</returns>
    internal static GpsCoordinate FromDegreesMinutesSeconds(double latitudeDegrees, double latitudeMinutes, double latitudeSeconds, string? latitudeReference, double longitudeDegrees, double longitudeMinutes, double longitudeSeconds, string? longitudeReference, double altitude, string? altitudeReference)
    {
        GpsCoordinate coordinate = new GpsCoordinate();
        coordinate.IsValid = true;
        coordinate.Latitude = ToDecimalDegrees(latitudeDegrees, latitudeMinutes, latitudeSeconds, latitudeReference);
        coordinate.Longitude = ToDecimalDegrees(longitudeDegrees, longitudeMinutes, longitudeSeconds, longitudeReference);
        coordinate.Altitude = ApplyAltitudeReference(altitude, altitudeReference);
        coordinate.LatitudeReference = NormalizeLatitudeReference(coordinate.Latitude, latitudeReference);
        coordinate.LongitudeReference = NormalizeLongitudeReference(coordinate.Longitude, longitudeReference);
        coordinate.AltitudeReference = SafeText(altitudeReference);
        return coordinate;
    }

    internal static double ToDecimalDegrees(double degrees, double minutes, double seconds, string? reference)
    {
        double value = Math.Abs(degrees) + (Math.Abs(minutes) / 60.0) + (Math.Abs(seconds) / 3600.0);

        if (IsNegativeReference(reference))
        {
            return -value;
        }

        if (degrees < 0)
        {
            return -value;
        }

        return value;
    }

    internal static double ApplyAltitudeReference(double altitude, string? reference)
    {
        double value = Math.Abs(altitude);

        if (IsNegativeReference(reference))
        {
            return -value;
        }

        if (altitude < 0)
        {
            return -value;
        }

        return value;
    }

    internal static string FormatDisplay(GpsCoordinate? coordinate)
    {
        if (coordinate == null || !coordinate.IsValid)
        {
            return "";
        }

        string text = FormatLatitude(coordinate.Latitude, coordinate.LatitudeReference);
        text = text + ", ";
        text = text + FormatLongitude(coordinate.Longitude, coordinate.LongitudeReference);

        if (coordinate.Altitude != 0 || !IsEmpty(coordinate.AltitudeReference))
        {
            text = text + ", ";
            text = text + FormatAltitude(coordinate.Altitude, coordinate.AltitudeReference);
        }

        return text;
    }

    internal static string FormatLatitude(double latitude, string? reference)
    {
        return FormatCoordinateValue(latitude, reference, "N", "S");
    }

    internal static string FormatLongitude(double longitude, string? reference)
    {
        return FormatCoordinateValue(longitude, reference, "E", "W");
    }

    internal static string FormatCoordinateValue(double value, string? reference, string positiveReference, string negativeReference)
    {
        string normalizedReference = NormalizeCoordinateReference(value, reference, positiveReference, negativeReference);
        double signedValue = ApplyCoordinateReference(value, normalizedReference, negativeReference);
        double magnitude = Math.Abs(signedValue);
        return FormatNumber(magnitude, "0.######") + " " + normalizedReference;
    }

    internal static string FormatAltitude(double altitude, string? reference)
    {
        double signedAltitude = ApplyAltitudeReference(altitude, reference);
        string text = FormatNumber(signedAltitude, "0.##") + " m";

        if (!IsEmpty(reference) && !IsNegativeReference(reference))
        {
            text = text + " " + reference;
        }

        return text;
    }

    internal static double ApplyCoordinateReference(double value, string? reference, string negativeReference)
    {
        double magnitude = Math.Abs(value);

        if (IsReference(reference, negativeReference))
        {
            return -magnitude;
        }

        return magnitude;
    }

    internal static string NormalizeLatitudeReference(double value, string? reference)
    {
        return NormalizeCoordinateReference(value, reference, "N", "S");
    }

    internal static string NormalizeLongitudeReference(double value, string? reference)
    {
        return NormalizeCoordinateReference(value, reference, "E", "W");
    }

    internal static string NormalizeCoordinateReference(double value, string? reference, string positiveReference, string negativeReference)
    {
        if (IsReference(reference, positiveReference))
        {
            return positiveReference;
        }

        if (IsReference(reference, negativeReference))
        {
            return negativeReference;
        }

        if (value < 0)
        {
            return negativeReference;
        }

        return positiveReference;
    }

    internal static bool IsNegativeReference(string? reference)
    {
        if (IsReference(reference, "S"))
        {
            return true;
        }

        if (IsReference(reference, "South"))
        {
            return true;
        }

        if (IsReference(reference, "W"))
        {
            return true;
        }

        if (IsReference(reference, "West"))
        {
            return true;
        }

        if (IsReference(reference, "1"))
        {
            return true;
        }

        if (IsReference(reference, "Below"))
        {
            return true;
        }

        if (IsReference(reference, "BelowSeaLevel"))
        {
            return true;
        }

        if (IsReference(reference, "Below Sea Level"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsReference(string? left, string? right)
    {
        return string.Equals(SafeText(left), SafeText(right), StringComparison.OrdinalIgnoreCase);
    }

    internal static string FormatNumber(double value, string format)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    internal static string SafeText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }

    internal static bool IsEmpty(string? text)
    {
        return text == null || text.Length == 0;
    }
}
