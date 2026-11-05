using System.Collections.Generic;

namespace geo;

internal static class GeometryTransforms
{
    internal static Geometry ReverseGeometryValue(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        ReverseCopiedGeometry(copy);
        return copy;
    }

    internal static void ReverseCopiedGeometry(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                break;
            case GeometryType.LineString:
                LineString lineString = (LineString)payload;
                lineString.Coordinates = ReverseCoordinateSequence(lineString.Coordinates);
                break;
            case GeometryType.LinearRing:
                LinearRing linearRing = (LinearRing)payload;
                linearRing.Coordinates = ReverseCoordinateSequence(linearRing.Coordinates);
                break;
            case GeometryType.Polygon:
                ReversePolygonPayload((Polygon)payload);
                break;
            case GeometryType.MultiLineString:
                MultiLineString multiLineString = (MultiLineString)payload;
                int lineIndex = 0;
                while (lineIndex < multiLineString.LineStrings.Length)
                {
                    multiLineString.LineStrings[lineIndex].Coordinates = ReverseCoordinateSequence(multiLineString.LineStrings[lineIndex].Coordinates);
                    lineIndex++;
                }
                break;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                int polygonIndex = 0;
                while (polygonIndex < multiPolygon.Polygons.Length)
                {
                    ReversePolygonPayload(multiPolygon.Polygons[polygonIndex]);
                    polygonIndex++;
                }
                break;
            case GeometryType.GeometryCollection:
                ReverseGeometryComponents(((GeometryCollection)payload).Geometries, false);
                break;
            case GeometryType.Triangle:
                Triangle triangle = (Triangle)payload;
                triangle.Shell.Coordinates = ReverseCoordinateSequence(triangle.Shell.Coordinates);
                break;
            case GeometryType.Tin:
                Tin tin = (Tin)payload;
                int triangleIndex = 0;
                while (triangleIndex < tin.Triangles.Length)
                {
                    tin.Triangles[triangleIndex].Shell.Coordinates = ReverseCoordinateSequence(tin.Triangles[triangleIndex].Shell.Coordinates);
                    triangleIndex++;
                }
                break;
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface surface = (PolyhedralSurface)payload;
                int patchIndex = 0;
                while (patchIndex < surface.Patches.Length)
                {
                    ReversePolygonPayload(surface.Patches[patchIndex]);
                    patchIndex++;
                }
                break;
            case GeometryType.CircularString:
                CircularString circularString = (CircularString)payload;
                circularString.Coordinates = ReverseCoordinateSequence(circularString.Coordinates);
                break;
            case GeometryType.CompoundCurve:
                ReverseGeometryComponents(((CompoundCurve)payload).Curves, true);
                break;
            case GeometryType.CurvePolygon:
                CurvePolygon curvePolygon = (CurvePolygon)payload;
                if (curvePolygon.Shell != null) ReverseCopiedGeometry(curvePolygon.Shell);
                ReverseGeometryComponents(curvePolygon.Holes, false);
                break;
            case GeometryType.MultiCurve:
                ReverseGeometryComponents(((MultiCurve)payload).Curves, false);
                break;
            case GeometryType.MultiSurface:
                ReverseGeometryComponents(((MultiSurface)payload).Surfaces, false);
                break;
            default:
                throw new InvalidGeometryException("Geometry type is not supported for reversal.");
        }
    }

    internal static void ReversePolygonPayload(Polygon polygon)
    {
        if (polygon == null || polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon payload, shell, or hole array is null.");
        }

        polygon.Shell.Coordinates = ReverseCoordinateSequence(polygon.Shell.Coordinates);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            polygon.Holes[index].Coordinates = ReverseCoordinateSequence(polygon.Holes[index].Coordinates);
            index++;
        }
    }

    internal static void ReverseGeometryComponents(Geometry[] geometries, bool reverseOrder)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }

        int index = 0;
        while (index < geometries.Length)
        {
            ReverseCopiedGeometry(geometries[index]);
            index++;
        }

        if (reverseOrder)
        {
            int left = 0;
            int right = geometries.Length - 1;
            while (left < right)
            {
                Geometry value = geometries[left];
                geometries[left] = geometries[right];
                geometries[right] = value;
                left++;
                right--;
            }
        }
    }

    internal static Coordinate[] ReverseCoordinateSequence(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Coordinate sequence is null.");
        }

        Coordinate[] reversed = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            reversed[index] = coordinates[coordinates.Length - 1 - index];
            index++;
        }

        return reversed;
    }

    internal static Geometry OrientRingValues(Geometry geometry, RingOrientation shellOrientation)
    {
        ValidateRequestedOrientation(shellOrientation);
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        OrientCopiedGeometry(copy, shellOrientation);
        return copy;
    }

    internal static void OrientCopiedGeometry(Geometry geometry, RingOrientation shellOrientation)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        bool shellClockwise = shellOrientation == RingOrientation.Clockwise;
        switch (geometry.GeometryType)
        {
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                ring.Coordinates = OrientRingCoordinateSequence(ring.Coordinates, shellClockwise);
                break;
            case GeometryType.Polygon:
                OrientPolygonPayload((Polygon)payload, shellClockwise);
                break;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                int polygonIndex = 0;
                while (polygonIndex < multiPolygon.Polygons.Length)
                {
                    OrientPolygonPayload(multiPolygon.Polygons[polygonIndex], shellClockwise);
                    polygonIndex++;
                }
                break;
            case GeometryType.GeometryCollection:
                OrientGeometryComponents(((GeometryCollection)payload).Geometries, shellOrientation);
                break;
            case GeometryType.Triangle:
                Triangle triangle = (Triangle)payload;
                triangle.Shell.Coordinates = OrientRingCoordinateSequence(triangle.Shell.Coordinates, shellClockwise);
                break;
            case GeometryType.Tin:
                Tin tin = (Tin)payload;
                int triangleIndex = 0;
                while (triangleIndex < tin.Triangles.Length)
                {
                    tin.Triangles[triangleIndex].Shell.Coordinates = OrientRingCoordinateSequence(tin.Triangles[triangleIndex].Shell.Coordinates, shellClockwise);
                    triangleIndex++;
                }
                break;
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface surface = (PolyhedralSurface)payload;
                int patchIndex = 0;
                while (patchIndex < surface.Patches.Length)
                {
                    OrientPolygonPayload(surface.Patches[patchIndex], shellClockwise);
                    patchIndex++;
                }
                break;
            case GeometryType.CurvePolygon:
                OrientCurvePolygonPayload(
                    (CurvePolygon)payload, shellClockwise);
                break;
            case GeometryType.MultiSurface:
                OrientGeometryComponents(((MultiSurface)payload).Surfaces, shellOrientation);
                break;
            default:
                break;
        }
    }

    internal static void OrientPolygonPayload(Polygon polygon, bool shellClockwise)
    {
        if (polygon == null || polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon payload, shell, or hole array is null.");
        }

        polygon.Shell.Coordinates = OrientRingCoordinateSequence(polygon.Shell.Coordinates, shellClockwise);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            polygon.Holes[index].Coordinates = OrientRingCoordinateSequence(polygon.Holes[index].Coordinates, !shellClockwise);
            index++;
        }
    }

    internal static void OrientGeometryComponents(Geometry[] geometries, RingOrientation shellOrientation)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }

        int index = 0;
        while (index < geometries.Length)
        {
            OrientCopiedGeometry(geometries[index], shellOrientation);
            index++;
        }
    }

    internal static void OrientCurvePolygonPayload(
        CurvePolygon polygon, bool shellClockwise)
    {
        CurveAggregateAlgorithms.ValidateCurvePolygonValue(polygon);
        if (polygon.Shell == null) return;
        OrientCurveRingValue(polygon.Shell, shellClockwise);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            OrientCurveRingValue(
                polygon.Holes[index], !shellClockwise);
            index++;
        }
    }

    internal static void OrientCurveRingValue(
        Geometry curve, bool clockwise)
    {
        double area =
            CurveAggregateAlgorithms.GetSignedCurveRingArea(curve);
        if ((clockwise && area > 0.0) ||
            (!clockwise && area < 0.0))
        {
            ReverseCopiedGeometry(curve);
        }
    }

    internal static Coordinate[] OrientRingCoordinateSequence(Coordinate[] coordinates, bool clockwise)
    {
        GeometryBuilder.ValidateLinearRingCoordinates(coordinates);
        if (coordinates.Length == 0)
        {
            return coordinates;
        }

        double area = GeometryNormalization.GetSignedRingArea(coordinates);
        if ((clockwise && area > 0.0) || (!clockwise && area < 0.0))
        {
            return GeometryNormalization.ReverseClosedRingCoordinates(coordinates);
        }

        return coordinates;
    }

    internal static void ValidateRequestedOrientation(RingOrientation ringOrientation)
    {
        if (ringOrientation != RingOrientation.Clockwise && ringOrientation != RingOrientation.CounterClockwise)
        {
            throw new InvalidGeometryException("Ring orientation must be clockwise or counter-clockwise.");
        }
    }

    internal static Geometry ApplyAffineTransformValue(Geometry geometry, AffineTransform transform)
    {
        ValidateAffineTransform(transform);
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            copy,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            double sourceX = coordinate.X;
            double sourceY = coordinate.Y;
            coordinate.X = transform.M00 * sourceX + transform.M01 * sourceY + transform.M02;
            coordinate.Y = transform.M10 * sourceX + transform.M11 * sourceY + transform.M12;
            if (!double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y))
            {
                throw new InvalidGeometryException("Affine transformation produced non-finite coordinates.");
            }
            index++;
        }

        ValidateEditedGeometryStructure(copy);
        return copy;
    }

    internal static void ValidateAffineTransform(AffineTransform transform)
    {
        if (transform == null || !double.IsFinite(transform.M00) || !double.IsFinite(transform.M01) || !double.IsFinite(transform.M02) ||
            !double.IsFinite(transform.M10) || !double.IsFinite(transform.M11) || !double.IsFinite(transform.M12))
        {
            throw new InvalidGeometryException("Affine transformation coefficients must be finite.");
        }
    }

    internal static Geometry ReplaceSpatialReferenceValue(Geometry geometry, int spatialReferenceId)
    {
        if (spatialReferenceId < GeometryConstants.UnknownSpatialReferenceId)
        {
            throw new InvalidGeometryException("Spatial reference identifier cannot be negative.");
        }

        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        ReplaceSpatialReferenceInTree(copy, spatialReferenceId);
        return copy;
    }

    internal static void ReplaceSpatialReferenceInTree(Geometry geometry, int spatialReferenceId)
    {
        geometry.SpatialReferenceId = spatialReferenceId;
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            ReplaceSpatialReferenceInTree(children[index], spatialReferenceId);
            index++;
        }
    }

    internal static Geometry ConvertCoordinateLayoutValue(Geometry geometry, CoordinateLayout coordinateLayout, double addedZ, double addedM)
    {
        GeometryBuilder.ValidateCoordinateLayout(coordinateLayout);
        GeometryInspection.EnsureGeometryGraph(geometry);
        bool sourceHasZ = LayoutHasZ(geometry.CoordinateLayout);
        bool sourceHasM = LayoutHasM(geometry.CoordinateLayout);
        bool targetHasZ = LayoutHasZ(coordinateLayout);
        bool targetHasM = LayoutHasM(coordinateLayout);
        if (targetHasZ && !sourceHasZ && !double.IsFinite(addedZ))
        {
            throw new InvalidGeometryException("A finite Z ordinate is required when adding Z to a coordinate layout.");
        }
        if (targetHasM && !sourceHasM && !double.IsFinite(addedM))
        {
            throw new InvalidGeometryException("A finite M ordinate is required when adding M to a coordinate layout.");
        }

        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            copy,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            coordinate.Z = targetHasZ ? (sourceHasZ ? coordinate.Z : addedZ) : double.NaN;
            coordinate.M = targetHasM ? (sourceHasM ? coordinate.M : addedM) : double.NaN;
            index++;
        }

        ReplaceCoordinateLayoutInTree(copy, coordinateLayout);
        GeometryInspection.EnsureGeometryGraph(copy);
        return copy;
    }

    internal static void ReplaceCoordinateLayoutInTree(Geometry geometry, CoordinateLayout coordinateLayout)
    {
        geometry.CoordinateLayout = coordinateLayout;
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            ReplaceCoordinateLayoutInTree(children[index], coordinateLayout);
            index++;
        }
    }

    internal static bool LayoutHasZ(CoordinateLayout coordinateLayout)
    {
        return coordinateLayout == CoordinateLayout.XYZ || coordinateLayout == CoordinateLayout.XYZM;
    }

    internal static bool LayoutHasM(CoordinateLayout coordinateLayout)
    {
        return coordinateLayout == CoordinateLayout.XYM || coordinateLayout == CoordinateLayout.XYZM;
    }

    internal static Geometry SnapGeometryToGridValue(Geometry geometry, PrecisionModel precisionModel)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        GeometryFactoryOptions options = CreatePrecisionOptions(geometry, precisionModel);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            copy,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < coordinates.Length)
        {
            coordinates[index].X = GeometryBuilder.ApplyPrecision(coordinates[index].X, options.PrecisionOffsetX, options);
            coordinates[index].Y = GeometryBuilder.ApplyPrecision(coordinates[index].Y, options.PrecisionOffsetY, options);
            index++;
        }

        ValidateEditedGeometryStructure(copy);
        return copy;
    }

    internal static GeometryFactoryOptions CreatePrecisionOptions(Geometry geometry, PrecisionModel precisionModel)
    {
        if (precisionModel == null)
        {
            throw new PrecisionException("Precision model is null.");
        }

        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.CoordinateLayout = geometry.CoordinateLayout;
        options.SpatialReferenceId = geometry.SpatialReferenceId;
        options.PrecisionModelType = precisionModel.Type;
        options.PrecisionScale = precisionModel.Scale;
        options.PrecisionOffsetX = precisionModel.OffsetX;
        options.PrecisionOffsetY = precisionModel.OffsetY;
        GeometryBuilder.ValidateOptions(options);
        return options;
    }

    internal static Geometry RemoveRepeatedCoordinatesValue(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        RemoveRepeatedCoordinatesFromCopy(copy);
        ValidateEditedGeometryStructure(copy);
        return copy;
    }

    internal static void RemoveRepeatedCoordinatesFromCopy(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                break;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                line.Coordinates = RemoveRepeatedFromSequence(line.Coordinates, false);
                break;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                ring.Coordinates = RemoveRepeatedFromSequence(ring.Coordinates, true);
                break;
            case GeometryType.Polygon:
                RemoveRepeatedFromPolygon((Polygon)payload);
                break;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                int lineIndex = 0;
                while (lineIndex < multiLine.LineStrings.Length)
                {
                    multiLine.LineStrings[lineIndex].Coordinates = RemoveRepeatedFromSequence(multiLine.LineStrings[lineIndex].Coordinates, false);
                    lineIndex++;
                }
                break;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                int polygonIndex = 0;
                while (polygonIndex < multiPolygon.Polygons.Length)
                {
                    RemoveRepeatedFromPolygon(multiPolygon.Polygons[polygonIndex]);
                    polygonIndex++;
                }
                break;
            case GeometryType.GeometryCollection:
                RemoveRepeatedFromGeometryArray(((GeometryCollection)payload).Geometries);
                break;
            case GeometryType.Triangle:
                Triangle triangle = (Triangle)payload;
                triangle.Shell.Coordinates = RemoveRepeatedFromSequence(triangle.Shell.Coordinates, true);
                break;
            case GeometryType.Tin:
                Tin tin = (Tin)payload;
                int triangleIndex = 0;
                while (triangleIndex < tin.Triangles.Length)
                {
                    tin.Triangles[triangleIndex].Shell.Coordinates = RemoveRepeatedFromSequence(tin.Triangles[triangleIndex].Shell.Coordinates, true);
                    triangleIndex++;
                }
                break;
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface surface = (PolyhedralSurface)payload;
                int patchIndex = 0;
                while (patchIndex < surface.Patches.Length)
                {
                    RemoveRepeatedFromPolygon(surface.Patches[patchIndex]);
                    patchIndex++;
                }
                break;
            case GeometryType.CircularString:
                CircularString circular = (CircularString)payload;
                CircularStringAlgorithms.ValidateValue(circular);
                break;
            case GeometryType.CompoundCurve:
                RemoveRepeatedFromGeometryArray(((CompoundCurve)payload).Curves);
                break;
            case GeometryType.CurvePolygon:
                CurvePolygon curvePolygon = (CurvePolygon)payload;
                if (curvePolygon.Shell != null) RemoveRepeatedCoordinatesFromCopy(curvePolygon.Shell);
                RemoveRepeatedFromGeometryArray(curvePolygon.Holes);
                break;
            case GeometryType.MultiCurve:
                RemoveRepeatedFromGeometryArray(((MultiCurve)payload).Curves);
                break;
            case GeometryType.MultiSurface:
                RemoveRepeatedFromGeometryArray(((MultiSurface)payload).Surfaces);
                break;
            default:
                throw new InvalidGeometryException("Geometry type is not supported for repeated-coordinate removal.");
        }
    }

    internal static void RemoveRepeatedFromGeometryArray(Geometry[] geometries)
    {
        int index = 0;
        while (index < geometries.Length)
        {
            RemoveRepeatedCoordinatesFromCopy(geometries[index]);
            index++;
        }
    }

    internal static void RemoveRepeatedFromPolygon(Polygon polygon)
    {
        polygon.Shell.Coordinates = RemoveRepeatedFromSequence(polygon.Shell.Coordinates, true);
        if (polygon.Shell.Coordinates.Length == 0)
        {
            polygon.Holes = [];
            return;
        }

        List<LinearRing> holes = new List<LinearRing>();
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            LinearRing hole = polygon.Holes[index];
            hole.Coordinates = RemoveRepeatedFromSequence(hole.Coordinates, true);
            if (hole.Coordinates.Length != 0) holes.Add(hole);
            index++;
        }
        polygon.Holes = holes.ToArray();
    }

    internal static Coordinate[] RemoveRepeatedFromSequence(Coordinate[] coordinates, bool closed)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Coordinate sequence is null.");
        }
        if (coordinates.Length == 0) return [];
        if (closed) GeometryBuilder.ValidateLinearRingCoordinates(coordinates);

        List<Coordinate> retained = new List<Coordinate>();
        retained.Add(coordinates[0]);
        int index = 1;
        while (index < coordinates.Length)
        {
            Coordinate previous = retained[retained.Count - 1];
            Coordinate current = coordinates[index];
            if (current.X != previous.X || current.Y != previous.Y)
            {
                retained.Add(current);
            }
            index++;
        }

        if (closed)
        {
            Coordinate first = retained[0];
            Coordinate last = retained[retained.Count - 1];
            if (first.X != last.X || first.Y != last.Y)
            {
                retained.Add(CoordinateSequences.CopyCoordinateValue(first));
            }
            Coordinate[] ring = retained.ToArray();
            if (ring.Length < 4 || !GeometryInspection.HasThreeDistinctPlanarCoordinates(ring)) return [];
            return ring;
        }

        if (retained.Count < 2) return [];
        return retained.ToArray();
    }

    internal static void ValidateEditedGeometryStructure(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.LineString:
                GeometryNormalization.ValidateLineCoordinates(((LineString)payload).Coordinates);
                break;
            case GeometryType.LinearRing:
                GeometryBuilder.ValidateLinearRingCoordinates(((LinearRing)payload).Coordinates);
                break;
            case GeometryType.Polygon:
                Polygon polygon = (Polygon)payload;
                GeometryBuilder.CopyPolygonPayload(polygon.Shell, polygon.Holes, GeometryNormalization.CreateNormalizationOptions(geometry));
                break;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                int lineIndex = 0;
                while (lineIndex < multiLine.LineStrings.Length)
                {
                    GeometryNormalization.ValidateLineCoordinates(multiLine.LineStrings[lineIndex].Coordinates);
                    lineIndex++;
                }
                break;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                int polygonIndex = 0;
                while (polygonIndex < multiPolygon.Polygons.Length)
                {
                    Polygon component = multiPolygon.Polygons[polygonIndex];
                    GeometryBuilder.CopyPolygonPayload(component.Shell, component.Holes, GeometryNormalization.CreateNormalizationOptions(geometry));
                    polygonIndex++;
                }
                break;
            case GeometryType.GeometryCollection:
                ValidateEditedGeometryArray(((GeometryCollection)payload).Geometries);
                break;
            case GeometryType.Triangle:
                GeometryBuilder.ValidateLinearRingCoordinates(((Triangle)payload).Shell.Coordinates);
                break;
            case GeometryType.Tin:
                Tin tin = (Tin)payload;
                int triangleIndex = 0;
                while (triangleIndex < tin.Triangles.Length)
                {
                    GeometryBuilder.ValidateLinearRingCoordinates(tin.Triangles[triangleIndex].Shell.Coordinates);
                    triangleIndex++;
                }
                break;
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface surface = (PolyhedralSurface)payload;
                int patchIndex = 0;
                while (patchIndex < surface.Patches.Length)
                {
                    Polygon patch = surface.Patches[patchIndex];
                    GeometryBuilder.CopyPolygonPayload(patch.Shell, patch.Holes, GeometryNormalization.CreateNormalizationOptions(geometry));
                    patchIndex++;
                }
                break;
            case GeometryType.CircularString:
                CircularStringAlgorithms.ValidateValue(
                    (CircularString)payload);
                break;
            case GeometryType.CompoundCurve:
                ValidateEditedGeometryArray(((CompoundCurve)payload).Curves);
                CurveAggregateAlgorithms.ValidateCompoundValue(
                    (CompoundCurve)payload);
                break;
            case GeometryType.CurvePolygon:
                CurvePolygon curvePolygon = (CurvePolygon)payload;
                if (curvePolygon.Shell != null) ValidateEditedGeometryStructure(curvePolygon.Shell);
                ValidateEditedGeometryArray(curvePolygon.Holes);
                CurveAggregateAlgorithms.ValidateCurvePolygonValue(
                    curvePolygon);
                break;
            case GeometryType.MultiCurve:
                ValidateEditedGeometryArray(((MultiCurve)payload).Curves);
                CurveAggregateAlgorithms.ValidateExtendedAggregateValue(
                    geometry);
                break;
            case GeometryType.MultiSurface:
                ValidateEditedGeometryArray(((MultiSurface)payload).Surfaces);
                CurveAggregateAlgorithms.ValidateExtendedAggregateValue(
                    geometry);
                break;
            default:
                break;
        }
    }

    internal static void ValidateEditedGeometryArray(Geometry[] geometries)
    {
        int index = 0;
        while (index < geometries.Length)
        {
            ValidateEditedGeometryStructure(geometries[index]);
            index++;
        }
    }
}
