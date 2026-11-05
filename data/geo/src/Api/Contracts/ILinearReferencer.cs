namespace geo;

/// <summary>Locates, projects, interpolates, and extracts positions on linear geometry.</summary>
public interface ILinearReferencer
{
    /// <summary>Compares two locations in component traversal order.</summary>
    /// <param name="first">The first location.</param>
    /// <param name="second">The second location.</param>
    /// <returns>A negative, zero, or positive value according to traversal order.</returns>
    int CompareLocations(LinearLocation first, LinearLocation second);

    /// <summary>Clamps and canonicalizes a location for a linear geometry.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to normalize.</param>
    /// <returns>The normalized location, or an invalid location for empty linework.</returns>
    LinearLocation NormalizeLocation(
        Geometry geometry, LinearLocation location);

    /// <summary>Gets the first usable location in component traversal order.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>The start location, or an invalid location for empty linework.</returns>
    LinearLocation GetStartLocation(Geometry geometry);

    /// <summary>Gets the last usable location in component traversal order.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>The end location, or an invalid location for empty linework.</returns>
    LinearLocation GetEndLocation(Geometry geometry);

    /// <summary>Resolves a component boundary to the selected coincident endpoint.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The component endpoint location.</param>
    /// <param name="options">The endpoint resolution policy.</param>
    /// <returns>The normalized selected endpoint.</returns>
    LinearLocation ResolveComponentEndpoint(
        Geometry geometry, LinearLocation location,
        LinearReferenceOptions options);

    /// <summary>Tests whether a location is the endpoint of its component.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to inspect.</param>
    /// <returns>True when the normalized location is a component endpoint.</returns>
    bool IsEndpoint(Geometry geometry, LinearLocation location);

    /// <summary>Computes total planar length across all components.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>The total planar length.</returns>
    double GetTotalLength(Geometry geometry);

    /// <summary>Locates an absolute planar length index.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="lengthIndex">The absolute length index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The indexed location, or an invalid location for empty linework.</returns>
    LinearLocation GetLocationAtAbsoluteIndex(
        Geometry geometry, double lengthIndex,
        LinearReferenceOptions options);

    /// <summary>Computes the absolute planar length index of a location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to index.</param>
    /// <returns>The absolute length index.</returns>
    double GetAbsoluteIndex(
        Geometry geometry, LinearLocation location);

    /// <summary>Locates a normalized planar length index from zero through one.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="normalizedIndex">The normalized index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The indexed location, or an invalid location for empty linework.</returns>
    LinearLocation GetLocationAtNormalizedIndex(
        Geometry geometry, double normalizedIndex,
        LinearReferenceOptions options);

    /// <summary>Computes the normalized planar length index of a location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to index.</param>
    /// <returns>The normalized index, or zero for zero-length linework.</returns>
    double GetNormalizedIndex(
        Geometry geometry, LinearLocation location);

    /// <summary>Projects a coordinate to its earliest nearest linear location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="coordinate">The coordinate to project.</param>
    /// <returns>The nearest location, or an invalid location for empty linework.</returns>
    LinearLocation ProjectCoordinate(
        Geometry geometry, Coordinate coordinate);

    /// <summary>Gets an owning coordinate interpolated at a location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to extract.</param>
    /// <returns>The interpolated coordinate, or null for an invalid or empty location.</returns>
    Coordinate? GetCoordinateAtLocation(
        Geometry geometry, LinearLocation location);

    /// <summary>Gets an owning coordinate offset to the left of a location's segment direction.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The base location.</param>
    /// <param name="offset">The signed planar offset.</param>
    /// <returns>The offset coordinate, or null for an invalid or empty location.</returns>
    Coordinate? GetOffsetCoordinate(
        Geometry geometry, LinearLocation location, double offset);

    /// <summary>Gets an owning coordinate interpolated at an absolute length index.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="lengthIndex">The absolute length index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The interpolated coordinate, or null for empty linework.</returns>
    Coordinate? GetCoordinateAtAbsoluteIndex(
        Geometry geometry, double lengthIndex,
        LinearReferenceOptions options);

    /// <summary>Gets an owning coordinate interpolated at a normalized length index.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="normalizedIndex">The normalized index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The interpolated coordinate, or null for empty linework.</returns>
    Coordinate? GetCoordinateAtNormalizedIndex(
        Geometry geometry, double normalizedIndex,
        LinearReferenceOptions options);

    /// <summary>Extracts an owning line substring between two locations.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="start">The substring start.</param>
    /// <param name="end">The substring end.</param>
    /// <returns>An owning lineal substring.</returns>
    Geometry ExtractLine(
        Geometry geometry, LinearLocation start, LinearLocation end);

    /// <summary>Tests whether measured ordinates are globally monotonic in traversal order.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>True for monotonic measured linework.</returns>
    bool IsMonotonicMeasure(Geometry geometry);

    /// <summary>Locates an M ordinate on globally monotonic measured linework.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="measure">The M index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The measured location, or an invalid location for empty linework.</returns>
    LinearLocation GetLocationAtMeasure(
        Geometry geometry, double measure,
        LinearReferenceOptions options);

    /// <summary>Gets the interpolated M ordinate at a location.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="location">The location to inspect.</param>
    /// <returns>The M ordinate, or no-data for an invalid or empty location.</returns>
    double GetMeasureAtLocation(
        Geometry geometry, LinearLocation location);

    /// <summary>Gets an owning coordinate interpolated at an M ordinate.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="measure">The M index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The interpolated coordinate, or null for empty linework.</returns>
    Coordinate? GetCoordinateAtMeasure(
        Geometry geometry, double measure,
        LinearReferenceOptions options);

    /// <summary>Extracts an owning line interval between two M ordinates.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="startMeasure">The interval start M.</param>
    /// <param name="endMeasure">The interval end M.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>An owning lineal measured interval.</returns>
    Geometry ExtractMeasureInterval(
        Geometry geometry, double startMeasure, double endMeasure,
        LinearReferenceOptions options);
}
