namespace geo;

internal static class ExtendedGeometryBuilder
{
    internal static Geometry CreateCircularStringValue(
        Coordinate[] coordinates, GeometryFactoryOptions options)
    {
        CircularString circular = new CircularString();
        circular.Coordinates = GeometryBuilder.CopyCoordinateArray(
            coordinates, options);
        CircularStringAlgorithms.ValidateValue(circular);
        Geometry geometry = GeometryBuilder.CreateGeometryShell(
            GeometryType.CircularString, options);
        geometry.CircularString = circular;
        return geometry;
    }

    internal static Geometry CreateCompoundCurveValue(
        Geometry[] curves, GeometryFactoryOptions options)
    {
        CompoundCurve compound = new CompoundCurve();
        compound.Curves = CopyComponentArrayForFactory(
            curves, options);
        Geometry geometry = GeometryBuilder.CreateGeometryShell(
            GeometryType.CompoundCurve, options);
        geometry.CompoundCurve = compound;
        ValidateConstructedExtendedValue(geometry);
        return geometry;
    }

    internal static Geometry CreateCurvePolygonValue(
        Geometry? shell, Geometry[] holes,
        GeometryFactoryOptions options)
    {
        if (holes == null)
        {
            throw new InvalidGeometryException(
                "CurvePolygon hole array is null.");
        }
        GeometryBuilder.ValidateComponentCount(holes.Length);
        CurvePolygon polygon = new CurvePolygon();
        if (shell != null)
        {
            polygon.Shell = CopyGeometryForFactory(shell, options);
        }
        polygon.Holes = CopyComponentArrayForFactory(holes, options);
        Geometry geometry = GeometryBuilder.CreateGeometryShell(
            GeometryType.CurvePolygon, options);
        geometry.CurvePolygon = polygon;
        ValidateConstructedExtendedValue(geometry);
        return geometry;
    }

    internal static Geometry CreateMultiCurveValue(
        Geometry[] curves, GeometryFactoryOptions options)
    {
        MultiCurve multiCurve = new MultiCurve();
        multiCurve.Curves = CopyComponentArrayForFactory(
            curves, options);
        Geometry geometry = GeometryBuilder.CreateGeometryShell(
            GeometryType.MultiCurve, options);
        geometry.MultiCurve = multiCurve;
        ValidateConstructedExtendedValue(geometry);
        return geometry;
    }

    internal static Geometry CreateMultiSurfaceValue(
        Geometry[] surfaces, GeometryFactoryOptions options)
    {
        MultiSurface multiSurface = new MultiSurface();
        multiSurface.Surfaces = CopyComponentArrayForFactory(
            surfaces, options);
        Geometry geometry = GeometryBuilder.CreateGeometryShell(
            GeometryType.MultiSurface, options);
        geometry.MultiSurface = multiSurface;
        ValidateConstructedExtendedValue(geometry);
        return geometry;
    }

    internal static Geometry[] CopyComponentArrayForFactory(
        Geometry[] components, GeometryFactoryOptions options)
    {
        if (components == null)
        {
            throw new InvalidGeometryException(
                "Geometry component array is null.");
        }
        GeometryBuilder.ValidateComponentCount(components.Length);
        Geometry[] copies = new Geometry[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            copies[index] = CopyGeometryForFactory(
                components[index], options);
            index++;
        }
        return copies;
    }

    internal static Geometry CopyGeometryForFactory(
        Geometry source, GeometryFactoryOptions options)
    {
        if (source == null)
        {
            throw new InvalidGeometryException(
                "Geometry component is null.");
        }
        GeometryInspection.EnsureGeometryGraph(source);
        if (source.CoordinateLayout != options.CoordinateLayout ||
            source.SpatialReferenceId != options.SpatialReferenceId)
        {
            throw new InvalidGeometryException(
                "Geometry components must use the factory layout and spatial reference identifier.");
        }
        Geometry copy = GeometryCopy.CopyGeometry(source);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            copy, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            GeometryBuilder.ValidateCoordinateForLayout(
                coordinate, options.CoordinateLayout);
            coordinate.X = GeometryBuilder.ApplyPrecision(
                coordinate.X, options.PrecisionOffsetX, options);
            coordinate.Y = GeometryBuilder.ApplyPrecision(
                coordinate.Y, options.PrecisionOffsetY, options);
            index++;
        }
        return copy;
    }

    internal static void ValidateConstructedExtendedValue(
        Geometry geometry)
    {
        ValidationResult structural =
            StructuralValidator.ValidateGeometry(geometry);
        if (!structural.IsValid)
        {
            throw new InvalidGeometryException(
                "Extended geometry construction failed: " +
                structural.Message);
        }
        CurveAggregateAlgorithms.ValidateExtendedTreeValue(
            geometry);
    }
}
