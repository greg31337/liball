namespace geo;

/// <summary>Computes spherical geographic measures, paths, locations, and circle approximations with explicit units.</summary>
public interface IGeographicOperations
{
    /// <summary>Converts an angular or earth-distance value between spatial units.</summary>
    /// <param name="value">The finite value to convert.</param>
    /// <param name="sourceUnit">The value's current unit.</param>
    /// <param name="targetUnit">The requested result unit.</param>
    /// <returns>The converted value.</returns>
    double ConvertUnits(
        double value, SpatialUnit sourceUnit, SpatialUnit targetUnit);

    /// <summary>Computes the shortest spherical distance between two geographic coordinates.</summary>
    /// <param name="first">The first longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The second longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The shortest great-circle distance.</returns>
    double HaversineDistance(
        Coordinate first, Coordinate second, SpatialUnit outputUnit);

    /// <summary>Computes the initial great-circle bearing from one geographic coordinate to another.</summary>
    /// <param name="first">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The destination longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested angular unit.</param>
    /// <returns>The normalized initial bearing.</returns>
    double InitialBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit);

    /// <summary>Computes the final great-circle bearing on arrival at a geographic coordinate.</summary>
    /// <param name="first">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The destination longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested angular unit.</param>
    /// <returns>The normalized final bearing.</returns>
    double FinalBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit);

    /// <summary>Computes a great-circle destination from a coordinate, distance, and bearing.</summary>
    /// <param name="origin">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="distance">The finite travel distance.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <param name="bearing">The finite bearing.</param>
    /// <param name="bearingUnit">The bearing's angular unit.</param>
    /// <returns>The normalized destination coordinate.</returns>
    Coordinate Destination(
        Coordinate origin, double distance, SpatialUnit distanceUnit,
        double bearing, SpatialUnit bearingUnit);

    /// <summary>Computes the midpoint of the shortest great-circle path.</summary>
    /// <param name="first">The first longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The second longitude/latitude coordinate in degrees.</param>
    /// <returns>The normalized geographic midpoint.</returns>
    Coordinate GeographicMidpoint(
        Coordinate first, Coordinate second);

    /// <summary>Creates an evenly segmented shortest great-circle path.</summary>
    /// <param name="start">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="end">The ending longitude/latitude coordinate in degrees.</param>
    /// <param name="segmentCount">The positive number of path segments.</param>
    /// <returns>An owning CRS84 line string.</returns>
    Geometry GreatCircle(
        Coordinate start, Coordinate end, int segmentCount);

    /// <summary>Computes constant-bearing distance between two geographic coordinates.</summary>
    /// <param name="first">The first longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The second longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The shortest-longitude rhumb distance.</returns>
    double RhumbDistance(
        Coordinate first, Coordinate second, SpatialUnit outputUnit);

    /// <summary>Computes the constant bearing between two geographic coordinates.</summary>
    /// <param name="first">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="second">The destination longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested angular unit.</param>
    /// <returns>The normalized rhumb bearing.</returns>
    double RhumbBearing(
        Coordinate first, Coordinate second, SpatialUnit outputUnit);

    /// <summary>Computes a constant-bearing destination from a coordinate.</summary>
    /// <param name="origin">The starting longitude/latitude coordinate in degrees.</param>
    /// <param name="distance">The finite travel distance.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <param name="bearing">The finite bearing.</param>
    /// <param name="bearingUnit">The bearing's angular unit.</param>
    /// <returns>The normalized rhumb destination.</returns>
    Coordinate RhumbDestination(
        Coordinate origin, double distance, SpatialUnit distanceUnit,
        double bearing, SpatialUnit bearingUnit);

    /// <summary>Computes total great-circle length across lineal components.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The total geographic length.</returns>
    double GeographicLength(
        Geometry lineal, SpatialUnit outputUnit);

    /// <summary>Computes polygon or multi-polygon area on the sphere.</summary>
    /// <param name="polygonal">The geographic polygonal geometry.</param>
    /// <param name="outputUnit">The unit whose square is used for the result.</param>
    /// <returns>The unsigned spherical area with holes subtracted.</returns>
    double SphericalArea(
        Geometry polygonal, SpatialUnit outputUnit);

    /// <summary>Gets a coordinate at a distance along lineal geography.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="distance">The finite distance, clamped to the line.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <returns>An owning interpolated coordinate, or null for empty linework.</returns>
    Coordinate? AlongGeographicLine(
        Geometry lineal, double distance, SpatialUnit distanceUnit);

    /// <summary>Extracts a geographic line interval between two distances.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="startDistance">The finite start distance.</param>
    /// <param name="endDistance">The finite end distance.</param>
    /// <param name="distanceUnit">The distance or angular unit.</param>
    /// <returns>An owning lineal slice in the requested direction.</returns>
    Geometry SliceGeographicLine(
        Geometry lineal, double startDistance, double endDistance,
        SpatialUnit distanceUnit);

    /// <summary>Finds the nearest point on lineal geography.</summary>
    /// <param name="lineal">The line string or multi-line string.</param>
    /// <param name="coordinate">The query longitude/latitude coordinate in degrees.</param>
    /// <param name="outputUnit">The requested distance or angular unit.</param>
    /// <returns>The nearest-point witness and distance.</returns>
    DistanceResult NearestOnGeographicLine(
        Geometry lineal, Coordinate coordinate,
        SpatialUnit outputUnit);

    /// <summary>Creates a geodesic circle approximation around a geographic coordinate.</summary>
    /// <param name="center">The center longitude/latitude coordinate in degrees.</param>
    /// <param name="radius">The finite positive radius below half the earth circumference.</param>
    /// <param name="radiusUnit">The radius distance or angular unit.</param>
    /// <param name="segmentCount">The number of boundary segments, at least three.</param>
    /// <returns>An owning CRS84 polygon.</returns>
    Geometry GeographicCircle(
        Coordinate center, double radius,
        SpatialUnit radiusUnit, int segmentCount);
}
