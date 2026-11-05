using System;
using System.Text;

namespace fake;

internal static class GeographyGenerator
{
    internal const double MaximumAltitudeMeters = 8848.0;
    internal const double MaximumDepthMeters = 10994.0;
    internal const double EarthRadiusMeters = 6371008.8;
    internal const int DefaultGeohashPrecision = 12;
    internal const int MaximumGeohashPrecision = 12;
    internal const string GeohashAlphabet = "0123456789bcdefghjkmnpqrstuvwxyz";

    internal static double Latitude(FakeRandom random)
    {
        return random.DoubleFromMinimumAndMaximumAndMaximumInclusive(-90.0, 90.0, true);
    }

    internal static double Latitude(
        FakeRandom random,
        double minimum,
        double maximum)
    {
        ValidateLatitude(minimum, "minimum");
        ValidateLatitude(maximum, "maximum");
        return random.DoubleFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, true);
    }

    internal static double Longitude(FakeRandom random)
    {
        return random.DoubleFromMinimumAndMaximumAndMaximumInclusive(-180.0, 180.0, true);
    }

    internal static double Longitude(
        FakeRandom random,
        double minimum,
        double maximum)
    {
        ValidateLongitude(minimum, "minimum");
        ValidateLongitude(maximum, "maximum");
        return random.DoubleFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, true);
    }

    internal static GeoCoordinate Coordinate(FakeRandom random)
    {
        return new GeoCoordinate(Latitude(random), Longitude(random));
    }

    internal static GeoCoordinate Coordinate(
        FakeRandom random,
        double minimumLatitude,
        double maximumLatitude,
        double minimumLongitude,
        double maximumLongitude)
    {
        ValidateLatitudeBounds(
            minimumLatitude,
            maximumLatitude,
            "minimumLatitude",
            "maximumLatitude");
        ValidateLongitudeBounds(
            minimumLongitude,
            maximumLongitude,
            "minimumLongitude",
            "maximumLongitude");
        double latitude = random.DoubleFromMinimumAndMaximumAndMaximumInclusive(
            minimumLatitude,
            maximumLatitude,
            true);
        double longitude = random.DoubleFromMinimumAndMaximumAndMaximumInclusive(
            minimumLongitude,
            maximumLongitude,
            true);
        return new GeoCoordinate(latitude, longitude);
    }

    internal static double Altitude(FakeRandom random)
    {
        return random.DoubleFromMinimumAndMaximumAndMaximumInclusive(0.0, MaximumAltitudeMeters, true);
    }

    internal static double Altitude(
        FakeRandom random,
        double minimum,
        double maximum)
    {
        ValidateNonNegative(minimum, "minimum");
        ValidateNonNegative(maximum, "maximum");
        return random.DoubleFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, true);
    }

    internal static double Depth(FakeRandom random)
    {
        return random.DoubleFromMinimumAndMaximum(-MaximumDepthMeters, 0.0);
    }

    internal static double Depth(
        FakeRandom random,
        double minimum,
        double maximum)
    {
        FloatingRandom.Finite(minimum, "minimum");
        FloatingRandom.Finite(maximum, "maximum");
        if (minimum >= 0.0)
        {
            throw new ArgumentOutOfRangeException("minimum", minimum, "minimum must be less than zero.");
        }
        if (maximum > 0.0)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be zero or less.");
        }
        return random.DoubleFromMinimumAndMaximum(minimum, maximum);
    }

    internal static string Direction(FakeRandom random, bool useAbbreviation)
    {
        int index = random.IntFromMaximumExclusive(8);
        return DirectionAt(index, useAbbreviation);
    }

    internal static string CardinalDirection(
        FakeRandom random,
        bool useAbbreviation)
    {
        int index = random.IntFromMaximumExclusive(4) * 2;
        return DirectionAt(index, useAbbreviation);
    }

    internal static string OrdinalDirection(
        FakeRandom random,
        bool useAbbreviation)
    {
        int index = (random.IntFromMaximumExclusive(4) * 2) + 1;
        return DirectionAt(index, useAbbreviation);
    }

    internal static double Bearing(FakeRandom random)
    {
        return random.DoubleFromMinimumAndMaximum(0.0, 360.0);
    }

    internal static GeoCoordinate BoundingBoxPoint(
        FakeRandom random,
        double southLatitude,
        double westLongitude,
        double northLatitude,
        double eastLongitude)
    {
        ValidateLatitudeBounds(
            southLatitude,
            northLatitude,
            "southLatitude",
            "northLatitude");
        ValidateLongitude(westLongitude, "westLongitude");
        ValidateLongitude(eastLongitude, "eastLongitude");
        double latitude = random.DoubleFromMinimumAndMaximumAndMaximumInclusive(
            southLatitude,
            northLatitude,
            true);
        double longitude;
        if (westLongitude <= eastLongitude)
        {
            longitude = random.DoubleFromMinimumAndMaximumAndMaximumInclusive(westLongitude, eastLongitude, true);
        }
        else
        {
            double westernSpan = 180.0 - westLongitude;
            double totalSpan = westernSpan + eastLongitude + 180.0;
            double offset = random.DoubleFromMinimumAndMaximumAndMaximumInclusive(0.0, totalSpan, true);
            if (offset <= westernSpan)
            {
                longitude = westLongitude + offset;
            }
            else
            {
                longitude = -180.0 + (offset - westernSpan);
            }
        }
        return new GeoCoordinate(latitude, longitude);
    }

    internal static GeoCoordinate NearbyPoint(
        FakeRandom random,
        double latitude,
        double longitude,
        double radiusMeters)
    {
        ValidateLatitude(latitude, "latitude");
        ValidateLongitude(longitude, "longitude");
        FloatingRandom.Finite(radiusMeters, "radiusMeters");
        double maximumRadius = Math.PI * EarthRadiusMeters;
        if (radiusMeters < 0.0 || radiusMeters > maximumRadius)
        {
            throw new ArgumentOutOfRangeException(
                "radiusMeters",
                radiusMeters,
                $"radiusMeters must be between 0 and {maximumRadius}, inclusive.");
        }
        if (radiusMeters == 0.0)
        {
            return new GeoCoordinate(latitude, longitude);
        }

        double angularRadius = radiusMeters / EarthRadiusMeters;
        double cosineDistance = 1.0 - (random.Double() * (1.0 - Math.Cos(angularRadius)));
        double angularDistance = Math.Acos(cosineDistance);
        double bearing = random.DoubleFromMinimumAndMaximum(0.0, Math.PI * 2.0);
        double latitudeRadians = DegreesToRadians(latitude);
        double longitudeRadians = DegreesToRadians(longitude);
        double sineLatitude = Math.Sin(latitudeRadians);
        double cosineLatitude = Math.Cos(latitudeRadians);
        double sineDistance = Math.Sin(angularDistance);
        double resultLatitude = Math.Asin(
            (sineLatitude * cosineDistance) +
            (cosineLatitude * sineDistance * Math.Cos(bearing)));
        double resultLongitude = longitudeRadians + Math.Atan2(
            Math.Sin(bearing) * sineDistance * cosineLatitude,
            cosineDistance - (sineLatitude * Math.Sin(resultLatitude)));
        resultLongitude = NormalizeLongitudeRadians(resultLongitude);
        return new GeoCoordinate(
            RadiansToDegrees(resultLatitude),
            RadiansToDegrees(resultLongitude));
    }

    internal static string Geohash(
        FakeRandom random,
        int precision,
        int maximumOutputSize)
    {
        GeoCoordinate coordinate = Coordinate(random);
        return Geohash(
            coordinate.Latitude,
            coordinate.Longitude,
            precision,
            maximumOutputSize);
    }

    internal static string Geohash(
        double latitude,
        double longitude,
        int precision,
        int maximumOutputSize)
    {
        ValidateLatitude(latitude, "latitude");
        ValidateLongitude(longitude, "longitude");
        if (precision < 1 || precision > MaximumGeohashPrecision)
        {
            throw new ArgumentOutOfRangeException(
                "precision",
                precision,
                $"precision must be between 1 and {MaximumGeohashPrecision}, inclusive.");
        }
        Guard.OutputSize(precision, maximumOutputSize, "precision");

        double minimumLatitude = -90.0;
        double maximumLatitude = 90.0;
        double minimumLongitude = -180.0;
        double maximumLongitude = 180.0;
        bool longitudeBit = true;
        int bit = 0;
        int value = 0;
        StringBuilder result = new StringBuilder(precision);
        while (result.Length < precision)
        {
            if (longitudeBit)
            {
                double midpoint = (minimumLongitude + maximumLongitude) / 2.0;
                if (longitude >= midpoint)
                {
                    value = value | (1 << (4 - bit));
                    minimumLongitude = midpoint;
                }
                else
                {
                    maximumLongitude = midpoint;
                }
            }
            else
            {
                double midpoint = (minimumLatitude + maximumLatitude) / 2.0;
                if (latitude >= midpoint)
                {
                    value = value | (1 << (4 - bit));
                    minimumLatitude = midpoint;
                }
                else
                {
                    maximumLatitude = midpoint;
                }
            }
            longitudeBit = !longitudeBit;
            if (bit == 4)
            {
                result.Append(GeohashAlphabet[value]);
                bit = 0;
                value = 0;
            }
            else
            {
                bit++;
            }
        }
        return result.ToString();
    }

    static void ValidateLatitudeBounds(
        double minimum,
        double maximum,
        string minimumParameterName,
        string maximumParameterName)
    {
        ValidateLatitude(minimum, minimumParameterName);
        ValidateLatitude(maximum, maximumParameterName);
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                maximumParameterName,
                maximum,
                $"{maximumParameterName} must be greater than or equal to {minimumParameterName}.");
        }
    }

    static void ValidateLongitudeBounds(
        double minimum,
        double maximum,
        string minimumParameterName,
        string maximumParameterName)
    {
        ValidateLongitude(minimum, minimumParameterName);
        ValidateLongitude(maximum, maximumParameterName);
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                maximumParameterName,
                maximum,
                $"{maximumParameterName} must be greater than or equal to {minimumParameterName}.");
        }
    }

    static void ValidateLatitude(double value, string parameterName)
    {
        FloatingRandom.Finite(value, parameterName);
        if (value < -90.0 || value > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be between -90 and 90, inclusive.");
        }
    }

    static void ValidateLongitude(double value, string parameterName)
    {
        FloatingRandom.Finite(value, parameterName);
        if (value < -180.0 || value > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be between -180 and 180, inclusive.");
        }
    }

    static void ValidateNonNegative(double value, string parameterName)
    {
        FloatingRandom.Finite(value, parameterName);
        if (value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be zero or greater.");
        }
    }

    static string DirectionAt(int index, bool useAbbreviation)
    {
        if (useAbbreviation)
        {
            switch (index)
            {
                case 0:
                    return "N";
                case 1:
                    return "NE";
                case 2:
                    return "E";
                case 3:
                    return "SE";
                case 4:
                    return "S";
                case 5:
                    return "SW";
                case 6:
                    return "W";
                default:
                    return "NW";
            }
        }
        switch (index)
        {
            case 0:
                return "North";
            case 1:
                return "Northeast";
            case 2:
                return "East";
            case 3:
                return "Southeast";
            case 4:
                return "South";
            case 5:
                return "Southwest";
            case 6:
                return "West";
            default:
                return "Northwest";
        }
    }

    static double DegreesToRadians(double value)
    {
        return value * Math.PI / 180.0;
    }

    static double RadiansToDegrees(double value)
    {
        return value * 180.0 / Math.PI;
    }

    static double NormalizeLongitudeRadians(double value)
    {
        double fullCircle = Math.PI * 2.0;
        return ((value + (Math.PI * 3.0)) % fullCircle) - Math.PI;
    }
}
