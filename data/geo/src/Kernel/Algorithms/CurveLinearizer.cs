using System;
using System.Collections.Generic;

namespace geo;

internal static class CurveLinearizer
{
    internal static Geometry LinearizeGeometry(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        ValidateParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(geometry);
        return LinearizeGeometryValue(geometry, parameters);
    }

    internal static Geometry LinearizeGeometryValue(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
                return GeometryBuilder.CreateLineStringValue(
                    LinearizeCurveGeometry(geometry, parameters), options);
            case GeometryType.CurvePolygon:
                return LinearizeCurvePolygonValue(
                    (CurvePolygon)payload, parameters, options);
            case GeometryType.MultiCurve:
                return LinearizeMultiCurveValue(
                    (MultiCurve)payload, parameters, options);
            case GeometryType.MultiSurface:
                return LinearizeMultiSurfaceValue(
                    (MultiSurface)payload, parameters, options);
            case GeometryType.GeometryCollection:
                return LinearizeCollectionValue(
                    (GeometryCollection)payload, geometry, parameters);
            default:
                return GeometryCopy.CopyGeometry(geometry);
        }
    }

    internal static Geometry LinearizeCurvePolygonValue(
        CurvePolygon polygon,
        CurveLinearizationParameters parameters,
        GeometryFactoryOptions options)
    {
        if (polygon == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "CurvePolygon payload or hole array is null.");
        }
        if (polygon.Shell == null)
        {
            if (polygon.Holes.Length != 0)
            {
                throw new InvalidGeometryException(
                    "An empty CurvePolygon cannot contain holes.");
            }
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        LinearRing shell = CreateLinearizedRing(
            polygon.Shell, parameters);
        LinearRing[] holes = new LinearRing[polygon.Holes.Length];
        int index = 0;
        while (index < holes.Length)
        {
            holes[index] = CreateLinearizedRing(
                polygon.Holes[index], parameters);
            index++;
        }
        return GeometryBuilder.CreatePolygonValue(
            shell, holes, options);
    }

    internal static LinearRing CreateLinearizedRing(
        Geometry curve, CurveLinearizationParameters parameters)
    {
        Coordinate[] coordinates =
            LinearizeCurveGeometry(curve, parameters);
        GeometryBuilder.ValidateLinearRingCoordinates(coordinates);
        LinearRing ring = new LinearRing();
        ring.Coordinates = CoordinateSequences.CopyCoordinateSequence(
            coordinates);
        return ring;
    }

    internal static Geometry LinearizeMultiCurveValue(
        MultiCurve multiCurve,
        CurveLinearizationParameters parameters,
        GeometryFactoryOptions options)
    {
        if (multiCurve == null || multiCurve.Curves == null)
        {
            throw new InvalidGeometryException(
                "MultiCurve payload or component array is null.");
        }
        LineString[] lines = new LineString[multiCurve.Curves.Length];
        int index = 0;
        while (index < lines.Length)
        {
            LineString line = new LineString();
            line.Coordinates = LinearizeCurveGeometry(
                multiCurve.Curves[index], parameters);
            lines[index] = line;
            index++;
        }
        return GeometryBuilder.CreateMultiLineStringValue(lines, options);
    }

    internal static Geometry LinearizeMultiSurfaceValue(
        MultiSurface multiSurface,
        CurveLinearizationParameters parameters,
        GeometryFactoryOptions options)
    {
        if (multiSurface == null || multiSurface.Surfaces == null)
        {
            throw new InvalidGeometryException(
                "MultiSurface payload or component array is null.");
        }
        List<Polygon> polygons = new List<Polygon>();
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            Geometry surface = multiSurface.Surfaces[index];
            if (surface.GeometryType == GeometryType.Polygon &&
                surface.Polygon != null)
            {
                polygons.Add(GeometryCopy.CopyPolygonValue(
                    surface.Polygon));
            }
            else if (surface.GeometryType == GeometryType.Triangle &&
                surface.Triangle != null)
            {
                Polygon triangle = new Polygon();
                triangle.Shell = GeometryCopy.CopyLinearRingValue(
                    surface.Triangle.Shell);
                polygons.Add(triangle);
            }
            else if (surface.GeometryType == GeometryType.CurvePolygon)
            {
                Geometry linearized = LinearizeGeometryValue(
                    surface, parameters);
                if (linearized.Polygon == null)
                {
                    throw new InvalidGeometryException(
                        "Linearized CurvePolygon payload is missing.");
                }
                polygons.Add(linearized.Polygon);
            }
            else if (surface.GeometryType == GeometryType.Tin &&
                surface.Tin != null)
            {
                int triangleIndex = 0;
                while (triangleIndex < surface.Tin.Triangles.Length)
                {
                    Polygon triangle = new Polygon();
                    triangle.Shell = GeometryCopy.CopyLinearRingValue(
                        surface.Tin.Triangles[triangleIndex].Shell);
                    polygons.Add(triangle);
                    triangleIndex++;
                }
            }
            else if (surface.GeometryType ==
                GeometryType.PolyhedralSurface &&
                surface.PolyhedralSurface != null)
            {
                int patchIndex = 0;
                while (patchIndex <
                    surface.PolyhedralSurface.Patches.Length)
                {
                    polygons.Add(GeometryCopy.CopyPolygonValue(
                        surface.PolyhedralSurface.Patches[patchIndex]));
                    patchIndex++;
                }
            }
            else
            {
                throw new InvalidGeometryException(
                    "MultiSurface linearization requires Polygon, Triangle, or CurvePolygon components.");
            }
            index++;
        }
        return GeometryBuilder.CreateMultiPolygonValue(
            polygons.ToArray(), options);
    }

    internal static Geometry LinearizeCollectionValue(
        GeometryCollection collection, Geometry metadataSource,
        CurveLinearizationParameters parameters)
    {
        if (collection == null || collection.Geometries == null)
        {
            throw new InvalidGeometryException(
                "GeometryCollection payload or component array is null.");
        }
        Geometry[] geometries = new Geometry[collection.Geometries.Length];
        int index = 0;
        while (index < geometries.Length)
        {
            geometries[index] = LinearizeGeometryValue(
                collection.Geometries[index], parameters);
            index++;
        }
        return GeometryCollections.CreateRawCollection(
            geometries, metadataSource.CoordinateLayout,
            metadataSource.SpatialReferenceId);
    }

    internal static Coordinate[] LinearizeCurveGeometry(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        ValidateParameters(parameters);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.LineString:
                return CopyAndValidateCoordinates(((LineString)payload).Coordinates);
            case GeometryType.LinearRing:
                return CopyAndValidateCoordinates(((LinearRing)payload).Coordinates);
            case GeometryType.CircularString:
                return LinearizeCircularStringValue((CircularString)payload, parameters);
            case GeometryType.CompoundCurve:
                return LinearizeCompoundCurveValue((CompoundCurve)payload, parameters);
            default:
                throw new InvalidGeometryException("Geometry is not a supported curve.");
        }
    }

    internal static Coordinate[] LinearizeCircularStringValue(
        CircularString circularString, CurveLinearizationParameters parameters)
    {
        ValidateParameters(parameters);
        CircularStringAlgorithms.ValidateValue(circularString);
        Coordinate[] coordinates = circularString.Coordinates;
        if (coordinates.Length == 0) return [];
        List<Coordinate> output = new List<Coordinate>();
        output.Add(RobustLineIntersection.CopyCoordinate(coordinates[0]));
        int index = 0;
        while (index + 2 < coordinates.Length)
        {
            AppendArc(coordinates[index], coordinates[index + 1],
                coordinates[index + 2], parameters, output);
            index += 2;
        }
        return output.ToArray();
    }

    internal static Coordinate[] LinearizeCompoundCurveValue(
        CompoundCurve compoundCurve, CurveLinearizationParameters parameters)
    {
        ValidateParameters(parameters);
        if (compoundCurve == null || compoundCurve.Curves == null)
        {
            throw new InvalidGeometryException("CompoundCurve payload or component array is null.");
        }
        List<Coordinate> output = new List<Coordinate>();
        int index = 0;
        while (index < compoundCurve.Curves.Length)
        {
            Coordinate[] component = LinearizeCurveGeometry(
                compoundCurve.Curves[index], parameters);
            AppendCurveCoordinates(output, component);
            index++;
        }
        return output.ToArray();
    }

    internal static void AppendArc(
        Coordinate start, Coordinate middle, Coordinate end,
        CurveLinearizationParameters parameters, List<Coordinate> output)
    {
        CircularArcDefinition arc = CircularStringAlgorithms.CreateArc(
            start, middle, end);
        if (arc.IsLinear)
        {
            AppendDistinctCoordinate(output, middle);
            AppendDistinctCoordinate(output, end);
            return;
        }
        int firstSegments = GetArcSegmentCount(
            arc.Radius, Math.Abs(arc.FirstSweep), parameters);
        int secondSegments = GetArcSegmentCount(
            arc.Radius, Math.Abs(arc.SecondSweep), parameters);
        if (firstSegments > parameters.MaximumSegments - secondSegments)
        {
            throw new InvalidGeometryException(
                "Preserving an arc control point would exceed the maximum segment count.");
        }
        AppendArcPart(
            arc, start, middle, arc.StartAngle, arc.FirstSweep,
            firstSegments, output);
        AppendArcPart(
            arc, middle, end, arc.StartAngle + arc.FirstSweep,
            arc.SecondSweep, secondSegments, output);
    }

    internal static void AppendArcPart(
        CircularArcDefinition arc, Coordinate start, Coordinate end,
        double startAngle, double sweep, int segments,
        List<Coordinate> output)
    {
        int segment = 1;
        while (segment <= segments)
        {
            if (output.Count >= GeometryConstants.DefaultMaximumCoordinates)
            {
                throw new InvalidGeometryException("Maximum linearized coordinate count was exceeded.");
            }
            double factor = (double)segment / segments;
            if (segment == segments)
            {
                AppendDistinctCoordinate(output, end);
            }
            else
            {
                double angle = startAngle + sweep * factor;
                Coordinate coordinate = new Coordinate();
                coordinate.X = VectorMath.RequireFiniteResult(
                    arc.CenterX + arc.Radius * Math.Cos(angle),
                    "Linearized arc X ordinate");
                coordinate.Y = VectorMath.RequireFiniteResult(
                    arc.CenterY + arc.Radius * Math.Sin(angle),
                    "Linearized arc Y ordinate");
                coordinate.Z = VectorMath.InterpolateOptionalOrdinate(start.Z, end.Z, factor);
                coordinate.M = VectorMath.InterpolateOptionalOrdinate(start.M, end.M, factor);
                output.Add(coordinate);
            }
            segment++;
        }
    }

    internal static int GetArcSegmentCount(
        double radius, double sweep, CurveLinearizationParameters parameters)
    {
        double angleLimit = 0.0;
        switch (parameters.Mode)
        {
            case CurveLinearizationMode.MaximumDeviation:
                if (parameters.Tolerance >= radius * 2.0)
                {
                    angleLimit = Math.PI * 2.0;
                }
                else
                {
                    double cosine = 1.0 - parameters.Tolerance / radius;
                    if (cosine < -1.0) cosine = -1.0;
                    if (cosine > 1.0) cosine = 1.0;
                    angleLimit = 2.0 * Math.Acos(cosine);
                }
                break;
            case CurveLinearizationMode.MaximumSegmentLength:
                if (parameters.Tolerance >= radius * 2.0)
                {
                    angleLimit = Math.PI;
                }
                else
                {
                    angleLimit = 2.0 * Math.Asin(
                        parameters.Tolerance / (radius * 2.0));
                }
                break;
            case CurveLinearizationMode.MaximumAngle:
                angleLimit = parameters.Tolerance;
                break;
            default:
                throw new InvalidGeometryException("Curve linearization mode is not supported.");
        }
        if (!double.IsFinite(angleLimit) || angleLimit <= 0.0)
        {
            throw new InvalidGeometryException("Curve tolerance cannot produce a finite arc subdivision.");
        }
        double ratio = sweep / angleLimit;
        double rounded = Math.Round(ratio);
        if (Math.Abs(ratio - rounded) <= 1.0e-12 * Math.Abs(ratio))
        {
            ratio = rounded;
        }
        double required = Math.Ceiling(ratio);
        if (required < 1.0) required = 1.0;
        if (!double.IsFinite(required) || required > parameters.MaximumSegments)
        {
            throw new InvalidGeometryException("Curve linearization maximum segment count was exceeded.");
        }
        return (int)required;
    }

    internal static Coordinate[] CopyAndValidateCoordinates(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Curve coordinate sequence is null.");
        }
        Coordinate[] copy = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            VectorMath.ValidateCoordinateValue(coordinates[index], "Curve coordinate");
            copy[index] = RobustLineIntersection.CopyCoordinate(coordinates[index]);
            index++;
        }
        return copy;
    }

    internal static void AppendCurveCoordinates(
        List<Coordinate> output, Coordinate[] component)
    {
        if (component.Length == 0) return;
        if (output.Count > 0 && !RobustLineIntersection.CoordinatesEqualPlanar(
            output[output.Count - 1], component[0]))
        {
            throw new InvalidGeometryException("CompoundCurve components are not continuous.");
        }
        int index = output.Count == 0 ? 0 : 1;
        while (index < component.Length)
        {
            if (output.Count >= GeometryConstants.DefaultMaximumCoordinates)
            {
                throw new InvalidGeometryException("Maximum linearized coordinate count was exceeded.");
            }
            output.Add(component[index]);
            index++;
        }
    }

    internal static void AppendDistinctCoordinate(
        List<Coordinate> output, Coordinate coordinate)
    {
        if (output.Count == 0 || !RobustLineIntersection.CoordinatesEqualPlanar(
            output[output.Count - 1], coordinate))
        {
            output.Add(RobustLineIntersection.CopyCoordinate(coordinate));
        }
    }

    internal static void ValidateParameters(CurveLinearizationParameters parameters)
    {
        if (parameters == null || !double.IsFinite(parameters.Tolerance) ||
            parameters.Tolerance <= 0.0 || parameters.MaximumSegments < 1 ||
            parameters.MaximumSegments > GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Curve linearization requires a positive finite tolerance and segment limit.");
        }
        switch (parameters.Mode)
        {
            case CurveLinearizationMode.MaximumDeviation:
            case CurveLinearizationMode.MaximumSegmentLength:
            case CurveLinearizationMode.MaximumAngle:
                break;
            default:
                throw new InvalidGeometryException("Curve linearization mode is not supported.");
        }
    }
}
