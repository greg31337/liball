using System.Collections.Generic;

namespace geo;

internal class ClippingOrdinateAccumulator
{
    internal Coordinate? Coordinate;

    internal void AddCoordinate(Coordinate coordinate)
    {
        if (Coordinate == null)
        {
            Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
            return;
        }
        OrdinateReconciler.ReconcileCoordinateInto(Coordinate, coordinate);
    }
}

internal static class RectangleClipper
{
    internal static Geometry ClipToEnvelope(
        Geometry geometry, Envelope envelope)
    {
        RelationGeometryParts sourceParts =
            new RelationGeometryParts(geometry);
        bool envelopeEmpty =
            EnvelopeAlgorithms.ValidateAndCheckEmpty(envelope);
        if (envelopeEmpty)
        {
            return CreateEmptyClipResult(geometry);
        }
        Envelope geometryBounds =
            EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
        if (EnvelopeAlgorithms.ValidateAndCheckEmpty(geometryBounds))
        {
            return CreateEmptyClipResult(geometry);
        }
        if (EnvelopeAlgorithms.CoversEnvelopeValue(
            envelope, geometryBounds))
        {
            return GeometryCopy.CopyGeometry(geometry);
        }
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            envelope, geometryBounds))
        {
            return CreateEmptyClipResult(geometry);
        }
        Geometry planarSource =
            GeometryTransforms.ConvertCoordinateLayoutValue(
                geometry, CoordinateLayout.XY, 0.0, 0.0);
        Geometry clipGeometry = CreateEnvelopeGeometry(
            envelope, planarSource.SpatialReferenceId);
        Geometry planarResult = ClipThroughIntersection(
            planarSource, clipGeometry);
        RestoreSourceOrdinates(
            planarResult, sourceParts, geometry.CoordinateLayout);
        return planarResult;
    }

    internal static Geometry ClipThroughIntersection(
        Geometry geometry, Geometry clipGeometry)
    {
        return OverlayEngine.GetIntersection(geometry, clipGeometry);
    }

    internal static Geometry[] SubdivideToGrid(
        Geometry geometry, int columns, int rows)
    {
        if (columns < 1 || rows < 1 ||
            columns > GeometryConstants.DefaultMaximumComponents / rows)
        {
            throw new InvalidGeometryException(
                "Grid subdivision counts must be positive and within the component limit.");
        }
        RelationGeometryParts parts = new RelationGeometryParts(geometry);
        Envelope bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(
            parts.Root);
        if (EnvelopeAlgorithms.ValidateAndCheckEmpty(bounds)) return [];
        int effectiveColumns = bounds.MinX == bounds.MaxX ? 1 : columns;
        int effectiveRows = bounds.MinY == bounds.MaxY ? 1 : rows;
        List<Geometry> pieces = new List<Geometry>();
        int row = 0;
        while (row < effectiveRows)
        {
            double minimumY = GetGridOrdinate(
                bounds.MinY, bounds.MaxY, row, effectiveRows);
            double maximumY = GetGridOrdinate(
                bounds.MinY, bounds.MaxY, row + 1, effectiveRows);
            int column = 0;
            while (column < effectiveColumns)
            {
                double minimumX = GetGridOrdinate(
                    bounds.MinX, bounds.MaxX, column, effectiveColumns);
                double maximumX = GetGridOrdinate(
                    bounds.MinX, bounds.MaxX, column + 1,
                    effectiveColumns);
                Envelope cell = EnvelopeAlgorithms.CreateBoundsEnvelope(
                    minimumX, minimumY, maximumX, maximumY);
                Geometry piece = ClipToEnvelope(parts.Root, cell);
                if (!GeometryInspection.IsEmptyValue(piece))
                {
                    pieces.Add(piece);
                }
                column++;
            }
            row++;
        }
        return pieces.ToArray();
    }

    internal static double GetGridOrdinate(
        double minimum, double maximum, int index, int count)
    {
        if (index == 0) return minimum;
        if (index == count) return maximum;
        double fraction = (double)index / (double)count;
        return RobustLineIntersection.InterpolateLineOrdinate(
            minimum, maximum, fraction);
    }

    internal static Geometry CreateEnvelopeGeometry(
        Envelope envelope, int spatialReferenceId)
    {
        Coordinate minimum = new Coordinate();
        minimum.X = envelope.MinX;
        minimum.Y = envelope.MinY;
        Coordinate maximum = new Coordinate();
        maximum.X = envelope.MaxX;
        maximum.Y = envelope.MaxY;
        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.SpatialReferenceId = spatialReferenceId;
        if (envelope.MinX == envelope.MaxX &&
            envelope.MinY == envelope.MaxY)
        {
            return GeometryBuilder.CreatePointValue(minimum, options);
        }
        if (envelope.MinX == envelope.MaxX ||
            envelope.MinY == envelope.MaxY)
        {
            return GeometryBuilder.CreateLineStringValue(
                [minimum, maximum], options);
        }
        return FactoryConstructions.CreateRectangleValue(
            minimum, maximum, options);
    }

    internal static Geometry CreateEmptyClipResult(Geometry geometry)
    {
        TopologicalDimension dimension =
            OverlayEngine.GetNominalDimension(geometry);
        GeometryType geometryType = GeometryType.GeometryCollection;
        if (dimension == TopologicalDimension.Point)
        {
            geometryType = GeometryType.Point;
        }
        else if (dimension == TopologicalDimension.Curve)
        {
            geometryType = GeometryType.LineString;
        }
        else if (dimension == TopologicalDimension.Surface)
        {
            geometryType = GeometryType.Polygon;
        }
        return GeometryBuilder.CreateEmptyValue(
            geometryType,
            GeometryNormalization.CreateNormalizationOptions(geometry));
    }

    internal static void RestoreSourceOrdinates(
        Geometry result, RelationGeometryParts sourceParts,
        CoordinateLayout sourceLayout)
    {
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            result,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        SegmentString[] sourceEdges =
            CollectionRelationGraphBuilder.CreateEdges(sourceParts);
        int index = 0;
        while (index < coordinates.Length)
        {
            ClippingOrdinateAccumulator accumulator =
                FindSourceOrdinates(
                    coordinates[index], sourceParts, sourceEdges);
            Coordinate? source = accumulator.Coordinate;
            coordinates[index].Z = double.NaN;
            coordinates[index].M = double.NaN;
            if (source != null)
            {
                if (GeometryTransforms.LayoutHasZ(sourceLayout))
                {
                    coordinates[index].Z = source.Z;
                }
                if (GeometryTransforms.LayoutHasM(sourceLayout))
                {
                    coordinates[index].M = source.M;
                }
            }
            index++;
        }
        GeometryTransforms.ReplaceCoordinateLayoutInTree(
            result, sourceLayout);
    }

    internal static ClippingOrdinateAccumulator FindSourceOrdinates(
        Coordinate point, RelationGeometryParts sourceParts,
        SegmentString[] sourceEdges)
    {
        ClippingOrdinateAccumulator accumulator =
            new ClippingOrdinateAccumulator();
        int pointIndex = 0;
        while (pointIndex < sourceParts.PointGeometries.Length)
        {
            Point sourcePoint = (Point)GeometryDispatch.GetSelectedPayload(
                sourceParts.PointGeometries[pointIndex]);
            if (sourcePoint.Coordinate != null &&
                RobustLineIntersection.CoordinatesEqualPlanar(
                    point, sourcePoint.Coordinate))
            {
                accumulator.AddCoordinate(sourcePoint.Coordinate);
            }
            pointIndex++;
        }
        int edgeIndex = 0;
        while (edgeIndex < sourceEdges.Length)
        {
            Coordinate[] edgeCoordinates =
                sourceEdges[edgeIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < edgeCoordinates.Length)
            {
                Coordinate start = edgeCoordinates[segmentIndex];
                Coordinate end = edgeCoordinates[segmentIndex + 1];
                if (SegmentDistanceAlgorithms.IsPointOnSegment(
                    point, start, end))
                {
                    double factor =
                        SegmentDistanceAlgorithms.GetExactProjectionFactor(
                            point, start, end);
                    factor = SegmentDistanceAlgorithms.ClampSegmentFactor(
                        factor);
                    accumulator.AddCoordinate(
                        VectorMath.InterpolateCoordinate(
                            start, end, factor));
                }
                segmentIndex++;
            }
            edgeIndex++;
        }
        return accumulator;
    }
}
