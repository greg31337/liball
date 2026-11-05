using System;

namespace geo;

internal static class GeographicMath
{
    internal const double EarthRadiusMeters = 6371008.8;

    internal static double ConvertUnits(
        double value, SpatialUnit sourceUnit, SpatialUnit targetUnit)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException(
                "The unit conversion value must be finite.");
        }
        double sourceRadians = GetRadiansPerUnit(sourceUnit);
        double targetRadians = GetRadiansPerUnit(targetUnit);
        double result = value * sourceRadians / targetRadians;
        if (!double.IsFinite(result))
        {
            throw new InvalidGeometryException(
                "The unit conversion result is not finite.");
        }
        return result;
    }

    internal static double GetRadiansPerUnit(SpatialUnit unit)
    {
        switch (unit)
        {
            case SpatialUnit.Radians:
                return 1.0;
            case SpatialUnit.Degrees:
                return Math.PI / 180.0;
            case SpatialUnit.Meters:
                return 1.0 / EarthRadiusMeters;
            case SpatialUnit.Kilometers:
                return 1000.0 / EarthRadiusMeters;
            case SpatialUnit.Miles:
                return 1609.344 / EarthRadiusMeters;
            case SpatialUnit.NauticalMiles:
                return 1852.0 / EarthRadiusMeters;
            case SpatialUnit.Feet:
                return 0.3048 / EarthRadiusMeters;
            default:
                throw new InvalidGeometryException(
                    "The spatial unit is not supported.");
        }
    }

    internal static double NormalizeLongitude(double longitude)
    {
        if (!double.IsFinite(longitude))
        {
            throw new InvalidGeometryException(
                "Longitude must be finite.");
        }
        double normalized = longitude % 360.0;
        if (normalized < -180.0)
        {
            normalized += 360.0;
        }
        else if (normalized >= 180.0)
        {
            normalized -= 360.0;
        }
        if (normalized == 0.0) return 0.0;
        return normalized;
    }

    internal static double NormalizeLatitude(double latitude)
    {
        if (!double.IsFinite(latitude))
        {
            throw new InvalidGeometryException(
                "Latitude must be finite.");
        }
        double normalized = latitude % 360.0;
        if (normalized < -180.0)
        {
            normalized += 360.0;
        }
        else if (normalized >= 180.0)
        {
            normalized -= 360.0;
        }
        if (normalized > 90.0)
        {
            normalized = 180.0 - normalized;
        }
        else if (normalized < -90.0)
        {
            normalized = -180.0 - normalized;
        }
        if (normalized == 0.0) return 0.0;
        return normalized;
    }

    internal static Coordinate NormalizeCoordinate(Coordinate coordinate)
    {
        if (coordinate == null)
        {
            throw new InvalidGeometryException(
                "Geographic coordinate is null.");
        }
        if (!double.IsFinite(coordinate.X) ||
            !double.IsFinite(coordinate.Y))
        {
            throw new InvalidGeometryException(
                "Geographic coordinate ordinates must be finite.");
        }

        double latitude = coordinate.Y % 360.0;
        if (latitude < -180.0)
        {
            latitude += 360.0;
        }
        else if (latitude >= 180.0)
        {
            latitude -= 360.0;
        }
        double longitude = coordinate.X;
        if (latitude > 90.0)
        {
            latitude = 180.0 - latitude;
            longitude += 180.0;
        }
        else if (latitude < -90.0)
        {
            latitude = -180.0 - latitude;
            longitude += 180.0;
        }

        Coordinate result =
            CoordinateSequences.CopyCoordinateValue(coordinate);
        result.X = NormalizeLongitude(longitude);
        result.Y = latitude == 0.0 ? 0.0 : latitude;
        return result;
    }

    internal static double GetHaversineDistance(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        double centralAngle = GetCentralAngle(first, second);
        return ConvertUnits(
            centralAngle, SpatialUnit.Radians, outputUnit);
    }

    internal static double GetCentralAngle(
        Coordinate first, Coordinate second)
    {
        ValidateGeographicCoordinate(first);
        ValidateGeographicCoordinate(second);
        if (Math.Abs(first.Y) == 90.0 && first.Y == second.Y)
        {
            return 0.0;
        }
        double firstLatitude = DegreesToRadians(first.Y);
        double secondLatitude = DegreesToRadians(second.Y);
        double latitudeDelta = secondLatitude - firstLatitude;
        double longitudeDelta = GetLongitudeDeltaRadians(
            first, second);
        double latitudeSine = Math.Sin(latitudeDelta / 2.0);
        double longitudeSine = Math.Sin(longitudeDelta / 2.0);
        double haversine = latitudeSine * latitudeSine +
            Math.Cos(firstLatitude) * Math.Cos(secondLatitude) *
            longitudeSine * longitudeSine;
        haversine = Clamp(haversine, 0.0, 1.0);
        return 2.0 * Math.Atan2(
            Math.Sqrt(haversine), Math.Sqrt(1.0 - haversine));
    }

    internal static double GetInitialBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        ValidateAngularUnit(outputUnit);
        ValidateGeographicCoordinate(first);
        ValidateGeographicCoordinate(second);
        if (GetCentralAngle(first, second) == 0.0) return 0.0;
        double firstLatitude = DegreesToRadians(first.Y);
        double secondLatitude = DegreesToRadians(second.Y);
        double longitudeDelta = GetLongitudeDeltaRadians(
            first, second);
        double y = Math.Sin(longitudeDelta) *
            Math.Cos(secondLatitude);
        double x = Math.Cos(firstLatitude) *
            Math.Sin(secondLatitude) -
            Math.Sin(firstLatitude) * Math.Cos(secondLatitude) *
            Math.Cos(longitudeDelta);
        double bearing = NormalizeRadians(Math.Atan2(y, x));
        return ConvertAngularResult(bearing, outputUnit);
    }

    internal static double GetFinalBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        ValidateAngularUnit(outputUnit);
        if (GetCentralAngle(first, second) == 0.0) return 0.0;
        double reverse = GetInitialBearing(
            second, first, SpatialUnit.Radians);
        double bearing = NormalizeRadians(reverse + Math.PI);
        return ConvertAngularResult(bearing, outputUnit);
    }

    internal static Coordinate GetDestination(
        Coordinate origin, double distance, SpatialUnit distanceUnit,
        double bearing, SpatialUnit bearingUnit)
    {
        ValidateGeographicCoordinate(origin);
        ValidateAngularUnit(bearingUnit);
        if (!double.IsFinite(distance) || !double.IsFinite(bearing))
        {
            throw new InvalidGeometryException(
                "Destination distance and bearing must be finite.");
        }
        double angularDistance = ConvertUnits(
            distance, distanceUnit, SpatialUnit.Radians);
        double bearingRadians = ConvertUnits(
            bearing, bearingUnit, SpatialUnit.Radians);
        double latitude = DegreesToRadians(origin.Y);
        double longitude = DegreesToRadians(
            NormalizeLongitude(origin.X));
        double latitudeResult = Math.Asin(Clamp(
            Math.Sin(latitude) * Math.Cos(angularDistance) +
            Math.Cos(latitude) * Math.Sin(angularDistance) *
            Math.Cos(bearingRadians), -1.0, 1.0));
        double longitudeResult = longitude + Math.Atan2(
            Math.Sin(bearingRadians) * Math.Sin(angularDistance) *
                Math.Cos(latitude),
            Math.Cos(angularDistance) -
                Math.Sin(latitude) * Math.Sin(latitudeResult));

        Coordinate result = new Coordinate();
        result.X = NormalizeLongitude(
            RadiansToDegrees(longitudeResult));
        result.Y = RadiansToDegrees(latitudeResult);
        return result;
    }

    internal static Coordinate GetMidpoint(
        Coordinate first, Coordinate second)
    {
        ValidateGeographicCoordinate(first);
        ValidateGeographicCoordinate(second);
        double firstLatitude = DegreesToRadians(first.Y);
        double secondLatitude = DegreesToRadians(second.Y);
        double firstLongitude = DegreesToRadians(
            NormalizeLongitude(first.X));
        double longitudeDelta = GetLongitudeDeltaRadians(
            first, second);
        double x = Math.Cos(secondLatitude) *
            Math.Cos(longitudeDelta);
        double y = Math.Cos(secondLatitude) *
            Math.Sin(longitudeDelta);
        double denominatorX = Math.Cos(firstLatitude) + x;
        double denominator = Math.Sqrt(
            denominatorX * denominatorX + y * y);
        double numerator = Math.Sin(firstLatitude) +
            Math.Sin(secondLatitude);
        if (Math.Abs(numerator) <= 1.0e-15 &&
            denominator <= 1.0e-15)
        {
            throw new InvalidGeometryException(
                "Antipodal coordinates do not have a unique midpoint.");
        }
        double latitude = Math.Atan2(numerator, denominator);
        double longitude = firstLongitude + Math.Atan2(
            y, denominatorX);

        Coordinate result = new Coordinate();
        result.X = NormalizeLongitude(RadiansToDegrees(longitude));
        result.Y = RadiansToDegrees(latitude);
        return result;
    }

    internal static void ValidateGeographicCoordinate(
        Coordinate coordinate)
    {
        if (coordinate == null ||
            !double.IsFinite(coordinate.X) ||
            !double.IsFinite(coordinate.Y) ||
            coordinate.Y < -90.0 || coordinate.Y > 90.0)
        {
            throw new InvalidGeometryException(
                "Geographic coordinates require finite longitude and latitude from -90 through 90 degrees.");
        }
    }

    internal static void ValidateAngularUnit(SpatialUnit unit)
    {
        if (unit != SpatialUnit.Radians && unit != SpatialUnit.Degrees)
        {
            throw new InvalidGeometryException(
                "Bearing units must be radians or degrees.");
        }
    }

    internal static double NormalizeRadians(double radians)
    {
        double normalized = radians % (2.0 * Math.PI);
        if (normalized < -Math.PI)
        {
            normalized += 2.0 * Math.PI;
        }
        else if (normalized >= Math.PI)
        {
            normalized -= 2.0 * Math.PI;
        }
        if (normalized == 0.0) return 0.0;
        return normalized;
    }

    internal static double GetLongitudeDeltaRadians(
        Coordinate first, Coordinate second)
    {
        double firstLongitude = NormalizeLongitude(first.X);
        double secondLongitude = NormalizeLongitude(second.X);
        return NormalizeRadians(DegreesToRadians(
            secondLongitude - firstLongitude));
    }

    internal static double ConvertAngularResult(
        double radians, SpatialUnit unit)
    {
        if (unit == SpatialUnit.Radians) return radians;
        return RadiansToDegrees(radians);
    }

    internal static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    internal static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    internal static double Clamp(
        double value, double minimum, double maximum)
    {
        if (value < minimum) return minimum;
        if (value > maximum) return maximum;
        return value;
    }
}
