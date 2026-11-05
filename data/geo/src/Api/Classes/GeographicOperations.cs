namespace geo;

/// <summary>Provides spherical geographic measures, paths, locations, and circle approximations with explicit units.</summary>
public class GeographicOperations : IGeographicOperations
{
    /// <summary>Creates a geographic operation provider.</summary>
    public GeographicOperations()
    {
    }

    /// <summary>Converts an angular or earth-distance value between spatial units.</summary>
    /// <param name="value">The finite value to convert.</param>
    /// <param name="sourceUnit">The value's current unit.</param>
    /// <param name="targetUnit">The requested result unit.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidGeometryException">The value or a unit is invalid.</exception>
    public double ConvertUnits(
        double value, SpatialUnit sourceUnit, SpatialUnit targetUnit)
    {
        return GeographicMath.ConvertUnits(
            value, sourceUnit, targetUnit);
    }

    /// <summary>Computes the shortest spherical distance between two geographic coordinates.</summary>
    /// <param name="first">The first longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The second longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The shortest great-circle distance.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate or unit is invalid.</exception>
    public double HaversineDistance(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        return GeographicMath.GetHaversineDistance(
            first, second, outputUnit);
    }

    /// <summary>Computes the initial great-circle bearing from one geographic coordinate to another.</summary>
    /// <param name="first">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The destination longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested angular unit.</param>
    /// <returns>The normalized initial bearing.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate or unit is invalid.</exception>
    public double InitialBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        return GeographicMath.GetInitialBearing(
            first, second, outputUnit);
    }

    /// <summary>Computes the final great-circle bearing on arrival at a geographic coordinate.</summary>
    /// <param name="first">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The destination longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested angular unit.</param>
    /// <returns>The normalized final bearing.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate or unit is invalid.</exception>
    public double FinalBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        return GeographicMath.GetFinalBearing(
            first, second, outputUnit);
    }

    /// <summary>Computes a great-circle destination from a coordinate, distance, and bearing.</summary>
    /// <param name="origin">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="distance">The finite travel distance.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <param name="bearing">The finite bearing.</param>
    /// <param name="bearingUnit">The bearing's angular unit.</param>
    /// <returns>The normalized destination coordinate.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate, value, or unit is invalid.</exception>
    public Coordinate Destination(
        Coordinate origin, double distance, SpatialUnit distanceUnit,
        double bearing, SpatialUnit bearingUnit)
    {
        return GeographicMath.GetDestination(
            origin, distance, distanceUnit, bearing, bearingUnit);
    }

    /// <summary>Computes the midpoint of the shortest great-circle path.</summary>
    /// <param name="first">The first longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The second longitude/latitude coordinate in degrees.</param>
    /// <returns>The normalized geographic midpoint.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate is invalid or the pair is antipodal.</exception>
    public Coordinate GeographicMidpoint(
        Coordinate first, Coordinate second)
    {
        return GeographicMath.GetMidpoint(first, second);
    }

    /// <summary>Creates an evenly segmented shortest great-circle path.</summary>
    /// <param name="start">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="end">The ending longitude/latitude coordinate in degrees.</param>
    /// <param name="segmentCount">The positive number of path segments.</param>
    /// <returns>An owning CRS84 line string.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate or segment count is invalid.</exception>
    public Geometry GreatCircle(
        Coordinate start, Coordinate end, int segmentCount)
    {
        return GeographicLineAlgorithms.CreateGreatCircle(
            start, end, segmentCount);
    }

    /// <summary>Computes constant-bearing distance between two geographic coordinates.</summary>
    /// <param name="first">The first longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The second longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The shortest-longitude rhumb distance.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate or unit is invalid.</exception>
    public double RhumbDistance(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        return GeographicLineAlgorithms.GetRhumbDistance(
            first, second, outputUnit);
    }

    /// <summary>Computes the constant bearing between two geographic coordinates.</summary>
    /// <param name="first">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The destination longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested angular unit.</param>
    /// <returns>The normalized rhumb bearing.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate or unit is invalid.</exception>
    public double RhumbBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit)
    {
        return GeographicLineAlgorithms.GetRhumbBearing(
            first, second, outputUnit);
    }

    /// <summary>Computes a constant-bearing destination from a coordinate.</summary>
    /// <param name="origin">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="distance">The finite travel distance.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <param name="bearing">The finite bearing.</param>
    /// <param name="bearingUnit">The bearing's angular unit.</param>
    /// <returns>The normalized rhumb destination.</returns>
    /// <exception cref="InvalidGeometryException">A coordinate, value, or unit is invalid.</exception>
    public Coordinate RhumbDestination(
        Coordinate origin, double distance, SpatialUnit distanceUnit,
        double bearing, SpatialUnit bearingUnit)
    {
        return GeographicLineAlgorithms.GetRhumbDestination(
            origin, distance, distanceUnit, bearing, bearingUnit);
    }

    /// <summary>Computes total great-circle length across lineal components.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The total geographic length.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or unit is invalid.</exception>
    public double GeographicLength(
        Geometry lineal, SpatialUnit outputUnit)
    {
        return GeographicLineAlgorithms.GetGeographicLength(
            lineal, outputUnit);
    }

    /// <summary>Computes polygon or multi-polygon area on the sphere.</summary>
    /// <param name="polygonal">The geographic polygonal geometry.</param>
    /// <param name="outputUnit">The unit whose square is used for the result.</param>
    /// <returns>The unsigned spherical area with holes subtracted.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or unit is invalid.</exception>
    public double SphericalArea(
        Geometry polygonal, SpatialUnit outputUnit)
    {
        return SphericalAreaAlgorithm.GetSphericalArea(
            polygonal, outputUnit);
    }

    /// <summary>Gets a coordinate at a distance along lineal geography.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="distance">The finite distance, clamped to the line.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <returns>An owning interpolated coordinate, or null for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">The geometry, distance, or unit is invalid.</exception>
    public Coordinate? AlongGeographicLine(
        Geometry lineal, double distance, SpatialUnit distanceUnit)
    {
        return GeographicLineAlgorithms.GetCoordinateAlongLine(
            lineal, distance, distanceUnit);
    }

    /// <summary>Extracts a geographic line interval between two distances.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="startDistance">The finite start distance.</param>
    /// <param name="endDistance">The finite end distance.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <returns>An owning lineal slice in the requested direction.</returns>
    /// <exception cref="InvalidGeometryException">The geometry, a distance, or the unit is invalid.</exception>
    public Geometry SliceGeographicLine(
        Geometry lineal, double startDistance, double endDistance,
        SpatialUnit distanceUnit)
    {
        return GeographicLineAlgorithms.SliceGeographicLine(
            lineal, startDistance, endDistance, distanceUnit);
    }

    /// <summary>Finds the nearest point on lineal geography.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="coordinate">The query longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The nearest-point witness and distance.</returns>
    /// <exception cref="InvalidGeometryException">The geometry, coordinate, or unit is invalid.</exception>
    public DistanceResult NearestOnGeographicLine(
        Geometry lineal, Coordinate coordinate,
        SpatialUnit outputUnit)
    {
        return GeographicLineAlgorithms.GetNearestPointOnLine(
            lineal, coordinate, outputUnit);
    }

    /// <summary>Creates a geodesic circle approximation around a geographic coordinate.</summary>
    /// <param name="center">The center longitude/latitude coordinate in degrees.</param>
    /// <param name="radius">The finite positive radius below half the earth circumference.</param>
    /// <param name="radiusUnit">The radius distance or angular unit.</param>
    /// <param name="segmentCount">The number of boundary segments, at least three.</param>
    /// <returns>An owning CRS84 polygon.</returns>
    /// <exception cref="InvalidGeometryException">The center, radius, unit, or segment count is invalid.</exception>
    public Geometry GeographicCircle(
        Coordinate center, double radius,
        SpatialUnit radiusUnit, int segmentCount)
    {
        return GeographicCircleAlgorithm.CreateGeographicCircle(
            center, radius, radiusUnit, segmentCount);
    }
}
