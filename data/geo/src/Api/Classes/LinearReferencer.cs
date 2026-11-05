namespace geo;

/// <summary>Provides owning linear-location, length-index, and measured-coordinate operations.</summary>
public class LinearReferencer : ILinearReferencer
{
    /// <summary>Creates a linear referencing provider.</summary>
    public LinearReferencer()
    {
    }

    /// <summary>Compares two locations in component traversal order.</summary>
    /// <param name="first">The first location.</param>
    /// <param name="second">The second location.</param>
    /// <returns>A negative, zero, or positive value according to traversal order.</returns>
    public int CompareLocations(
        LinearLocation first, LinearLocation second)
    {
        return LinearLocationMath.CompareLocations(first, second);
    }

    /// <summary>Clamps and canonicalizes a location for a linear geometry.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to normalize.</param>
    /// <returns>The normalized location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or location is invalid.</exception>
    public LinearLocation NormalizeLocation(
        Geometry geometry, LinearLocation location)
    {
        return LinearLocationMath.NormalizeLocation(geometry, location);
    }

    /// <summary>Gets the first usable location in component traversal order.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>The start location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is invalid or unsupported.</exception>
    public LinearLocation GetStartLocation(Geometry geometry)
    {
        return LinearLocationMath.CreateStartLocation(geometry);
    }

    /// <summary>Gets the last usable location in component traversal order.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>The end location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is invalid or unsupported.</exception>
    public LinearLocation GetEndLocation(Geometry geometry)
    {
        return LinearLocationMath.CreateEndLocation(geometry);
    }

    /// <summary>Resolves a component boundary to the selected coincident endpoint.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The component endpoint location.</param>
    /// <param name="options">The endpoint resolution policy.</param>
    /// <returns>The normalized selected endpoint.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public LinearLocation ResolveComponentEndpoint(
        Geometry geometry, LinearLocation location,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        return LinearLocationMath.ResolveComponentEndpoint(
            geometry, location, options.ResolveLowerLocation);
    }

    /// <summary>Tests whether a location is the endpoint of its component.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to inspect.</param>
    /// <returns>True when the normalized location is a component endpoint.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public bool IsEndpoint(
        Geometry geometry, LinearLocation location)
    {
        return LinearLocationMath.IsEndpoint(geometry, location);
    }

    /// <summary>Computes total planar length across all components.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>The total planar length.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is invalid or unsupported.</exception>
    public double GetTotalLength(Geometry geometry)
    {
        return LengthIndex.GetTotalLength(geometry);
    }

    /// <summary>Locates an absolute planar length index.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="lengthIndex">The absolute length index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The indexed location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, or outside a strict range.</exception>
    public LinearLocation GetLocationAtAbsoluteIndex(
        Geometry geometry, double lengthIndex,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        return LengthIndex.GetLocationAtAbsoluteIndex(
            geometry, lengthIndex, options.ClampOutOfRange,
            options.ResolveLowerLocation);
    }

    /// <summary>Computes the absolute planar length index of a location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to index.</param>
    /// <returns>The absolute length index.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public double GetAbsoluteIndex(
        Geometry geometry, LinearLocation location)
    {
        return LengthIndex.GetAbsoluteIndex(geometry, location);
    }

    /// <summary>Locates a normalized planar length index from zero through one.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="normalizedIndex">The normalized index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The indexed location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, or outside a strict range.</exception>
    public LinearLocation GetLocationAtNormalizedIndex(
        Geometry geometry, double normalizedIndex,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        return LengthIndex.GetLocationAtNormalizedIndex(
            geometry, normalizedIndex, options.ClampOutOfRange,
            options.ResolveLowerLocation);
    }

    /// <summary>Computes the normalized planar length index of a location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to index.</param>
    /// <returns>The normalized index, or zero for zero-length linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public double GetNormalizedIndex(
        Geometry geometry, LinearLocation location)
    {
        return LengthIndex.GetNormalizedIndex(geometry, location);
    }

    /// <summary>Projects a coordinate to its earliest nearest linear location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="coordinate">The coordinate to project.</param>
    /// <returns>The nearest location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public LinearLocation ProjectCoordinate(
        Geometry geometry, Coordinate coordinate)
    {
        return LinearLocationMath.ProjectCoordinate(
            geometry, coordinate);
    }

    /// <summary>Gets an owning coordinate interpolated at a location.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The location to extract.</param>
    /// <returns>The interpolated coordinate, or null for an invalid or empty location.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public Coordinate? GetCoordinateAtLocation(
        Geometry geometry, LinearLocation location)
    {
        return LinearLocationMath.GetCoordinateAtLocation(
            geometry, location);
    }

    /// <summary>Gets an owning coordinate offset to the left of a location's segment direction.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="location">The base location.</param>
    /// <param name="offset">The signed planar offset.</param>
    /// <returns>The offset coordinate, or null for an invalid or empty location.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public Coordinate? GetOffsetCoordinate(
        Geometry geometry, LinearLocation location, double offset)
    {
        return LinearLocationMath.GetOffsetCoordinate(
            geometry, location, offset);
    }

    /// <summary>Gets an owning coordinate interpolated at an absolute length index.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="lengthIndex">The absolute length index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The interpolated coordinate, or null for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, or outside a strict range.</exception>
    public Coordinate? GetCoordinateAtAbsoluteIndex(
        Geometry geometry, double lengthIndex,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        LinearLocation location =
            LengthIndex.GetLocationAtAbsoluteIndex(
                geometry, lengthIndex, options.ClampOutOfRange,
                options.ResolveLowerLocation);
        return LinearLocationMath.GetCoordinateAtLocation(
            geometry, location);
    }

    /// <summary>Gets an owning coordinate interpolated at a normalized length index.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="normalizedIndex">The normalized index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The interpolated coordinate, or null for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, or outside a strict range.</exception>
    public Coordinate? GetCoordinateAtNormalizedIndex(
        Geometry geometry, double normalizedIndex,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        LinearLocation location =
            LengthIndex.GetLocationAtNormalizedIndex(
                geometry, normalizedIndex, options.ClampOutOfRange,
                options.ResolveLowerLocation);
        return LinearLocationMath.GetCoordinateAtLocation(
            geometry, location);
    }

    /// <summary>Extracts an owning line substring between two locations.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <param name="start">The substring start.</param>
    /// <param name="end">The substring end.</param>
    /// <returns>An owning lineal substring.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid or unsupported.</exception>
    public Geometry ExtractLine(
        Geometry geometry, LinearLocation start, LinearLocation end)
    {
        return LineSubstringBuilder.ExtractLine(
            geometry, start, end);
    }

    /// <summary>Tests whether measured ordinates are globally monotonic in traversal order.</summary>
    /// <param name="geometry">The source linear geometry.</param>
    /// <returns>True for monotonic measured linework.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is invalid or unsupported.</exception>
    public bool IsMonotonicMeasure(Geometry geometry)
    {
        return MeasureIndex.IsMonotonicMeasure(geometry);
    }

    /// <summary>Locates an M ordinate on globally monotonic measured linework.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="measure">The M index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The measured location, or an invalid location for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, nonmonotonic, or outside a strict range.</exception>
    public LinearLocation GetLocationAtMeasure(
        Geometry geometry, double measure,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        return MeasureIndex.GetLocationAtMeasure(
            geometry, measure, options.ClampOutOfRange,
            options.ResolveLowerLocation);
    }

    /// <summary>Gets the interpolated M ordinate at a location.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="location">The location to inspect.</param>
    /// <returns>The M ordinate, or no-data for an invalid or empty location.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, or nonmonotonic.</exception>
    public double GetMeasureAtLocation(
        Geometry geometry, LinearLocation location)
    {
        return MeasureIndex.GetMeasureAtLocation(
            geometry, location);
    }

    /// <summary>Gets an owning coordinate interpolated at an M ordinate.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="measure">The M index.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>The interpolated coordinate, or null for empty linework.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, nonmonotonic, or outside a strict range.</exception>
    public Coordinate? GetCoordinateAtMeasure(
        Geometry geometry, double measure,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        return MeasureIndex.GetCoordinateAtMeasure(
            geometry, measure, options.ClampOutOfRange,
            options.ResolveLowerLocation);
    }

    /// <summary>Extracts an owning line interval between two M ordinates.</summary>
    /// <param name="geometry">The source measured linear geometry.</param>
    /// <param name="startMeasure">The interval start M.</param>
    /// <param name="endMeasure">The interval end M.</param>
    /// <param name="options">The range and coincident-location policies.</param>
    /// <returns>An owning lineal measured interval.</returns>
    /// <exception cref="InvalidGeometryException">An argument is invalid, unsupported, nonmonotonic, or outside a strict range.</exception>
    public Geometry ExtractMeasureInterval(
        Geometry geometry, double startMeasure, double endMeasure,
        LinearReferenceOptions options)
    {
        ValidateOptions(options);
        return MeasureIndex.ExtractMeasureInterval(
            geometry, startMeasure, endMeasure,
            options.ClampOutOfRange,
            options.ResolveLowerLocation);
    }

    internal static void ValidateOptions(
        LinearReferenceOptions options)
    {
        if (options == null)
        {
            throw new InvalidGeometryException(
                "Linear reference options are null.");
        }
    }
}
