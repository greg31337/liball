namespace geo;

internal static class FactoryConstructions
{
    internal static Geometry CreateRectangleValue(Coordinate minimum, Coordinate maximum, GeometryFactoryOptions options)
    {
        Coordinate minimumCopy = GeometryBuilder.CopyCoordinateForFactory(minimum, options);
        Coordinate maximumCopy = GeometryBuilder.CopyCoordinateForFactory(maximum, options);
        ValidateRectangleExtraOrdinates(minimumCopy, maximumCopy, options.CoordinateLayout);
        if (minimumCopy.X >= maximumCopy.X || minimumCopy.Y >= maximumCopy.Y)
        {
            throw new InvalidGeometryException("Rectangle corners must define positive width and height after precision is applied.");
        }

        LinearRing shell = CreateAxisAlignedRing(minimumCopy.X, minimumCopy.Y, maximumCopy.X, maximumCopy.Y, minimumCopy, options);
        return GeometryBuilder.CreatePolygonValue(shell, [], options);
    }

    internal static Geometry CreateEnvelopePolygonValue(Envelope envelope, GeometryFactoryOptions options)
    {
        if (envelope == null)
        {
            throw new InvalidGeometryException("Envelope is null.");
        }

        if (options.CoordinateLayout != CoordinateLayout.XY)
        {
            throw new InvalidGeometryException("An envelope supplies only X and Y ordinates and requires an XY factory layout.");
        }

        if (!double.IsFinite(envelope.MinX) || !double.IsFinite(envelope.MinY) || !double.IsFinite(envelope.MaxX) || !double.IsFinite(envelope.MaxY))
        {
            throw new InvalidGeometryException("Envelope ordinates must be finite.");
        }

        Coordinate minimum = new Coordinate();
        minimum.X = envelope.MinX;
        minimum.Y = envelope.MinY;
        Coordinate maximum = new Coordinate();
        maximum.X = envelope.MaxX;
        maximum.Y = envelope.MaxY;
        return CreateRectangleValue(minimum, maximum, options);
    }

    internal static Geometry CreateCircleValue(Coordinate center, double radius, int quadrantSegments, GeometryFactoryOptions options)
    {
        if (quadrantSegments < 1 || quadrantSegments > (GeometryConstants.DefaultMaximumCoordinates - 1) / 4)
        {
            throw new InvalidGeometryException("Circle quadrant segment count is outside the supported range.");
        }

        int vertexCount = quadrantSegments * 4;
        return CreateRegularPolygonValue(center, radius, vertexCount, options);
    }

    internal static Geometry CreateRegularPolygonValue(Coordinate center, double radius, int vertexCount, GeometryFactoryOptions options)
    {
        if (!double.IsFinite(radius) || radius <= 0.0)
        {
            throw new InvalidGeometryException("Polygon radius must be finite and positive.");
        }

        if (vertexCount < 3 || vertexCount >= GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException("Regular polygon vertex count is outside the supported range.");
        }

        Coordinate centerCopy = GeometryBuilder.CopyCoordinateForFactory(center, options);
        Coordinate[] coordinates = new Coordinate[vertexCount + 1];
        double direction = 1.0;
        if (options.RingOrientation == RingOrientation.Clockwise)
        {
            direction = -1.0;
        }

        int index = 0;
        while (index < vertexCount)
        {
            double angle = direction * 2.0 * System.Math.PI * (double)index / (double)vertexCount;
            coordinates[index] = CreateGeneratedCoordinate(centerCopy.X + radius * System.Math.Cos(angle), centerCopy.Y + radius * System.Math.Sin(angle), centerCopy);
            index++;
        }

        coordinates[vertexCount] = CreateGeneratedCoordinate(coordinates[0].X, coordinates[0].Y, centerCopy);
        LinearRing shell = new LinearRing();
        shell.Coordinates = coordinates;
        return GeometryBuilder.CreatePolygonValue(shell, [], options);
    }

    internal static Geometry CreateSegmentValue(Coordinate start, Coordinate end, GeometryFactoryOptions options)
    {
        return GeometryBuilder.CreateLineStringValue([start, end], options);
    }

    internal static Geometry CreateNarrowestCollectionValue(Geometry[] geometries, GeometryFactoryOptions options)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry array is null.");
        }

        GeometryBuilder.ValidateComponentCount(geometries.Length);
        if (geometries.Length == 0)
        {
            return GeometryBuilder.CreateGeometryCollectionValue([], options);
        }

        if (geometries.Length == 1)
        {
            return GeometryBuilder.CopyCoreGeometry(geometries[0], options);
        }

        bool allPoints = true;
        bool allLineStrings = true;
        bool allPolygons = true;
        int index = 0;
        while (index < geometries.Length)
        {
            Geometry geometry = geometries[index];
            GeometryDispatch.GetSelectedPayload(geometry);
            if (geometry.CoordinateLayout != options.CoordinateLayout || geometry.SpatialReferenceId != options.SpatialReferenceId)
            {
                throw new InvalidGeometryException("Promoted geometries must use the factory layout and spatial reference identifier.");
            }

            if (geometry.GeometryType != GeometryType.Point) allPoints = false;
            if (geometry.GeometryType != GeometryType.LineString) allLineStrings = false;
            if (geometry.GeometryType != GeometryType.Polygon) allPolygons = false;
            index++;
        }

        if (allPoints)
        {
            Point[] points = new Point[geometries.Length];
            index = 0;
            while (index < geometries.Length)
            {
                points[index] = (Point)GeometryDispatch.GetSelectedPayload(geometries[index]);
                index++;
            }

            return GeometryBuilder.CreateMultiPointValue(points, options);
        }

        if (allLineStrings)
        {
            LineString[] lineStrings = new LineString[geometries.Length];
            index = 0;
            while (index < geometries.Length)
            {
                lineStrings[index] = (LineString)GeometryDispatch.GetSelectedPayload(geometries[index]);
                index++;
            }

            return GeometryBuilder.CreateMultiLineStringValue(lineStrings, options);
        }

        if (allPolygons)
        {
            Polygon[] polygons = new Polygon[geometries.Length];
            index = 0;
            while (index < geometries.Length)
            {
                polygons[index] = (Polygon)GeometryDispatch.GetSelectedPayload(geometries[index]);
                index++;
            }

            return GeometryBuilder.CreateMultiPolygonValue(polygons, options);
        }

        return GeometryBuilder.CreateGeometryCollectionValue(geometries, options);
    }

    internal static LinearRing CreateAxisAlignedRing(double minimumX, double minimumY, double maximumX, double maximumY, Coordinate ordinateSource, GeometryFactoryOptions options)
    {
        Coordinate[] coordinates = new Coordinate[5];
        if (options.RingOrientation == RingOrientation.Clockwise)
        {
            coordinates[0] = CreateGeneratedCoordinate(minimumX, minimumY, ordinateSource);
            coordinates[1] = CreateGeneratedCoordinate(minimumX, maximumY, ordinateSource);
            coordinates[2] = CreateGeneratedCoordinate(maximumX, maximumY, ordinateSource);
            coordinates[3] = CreateGeneratedCoordinate(maximumX, minimumY, ordinateSource);
        }
        else
        {
            coordinates[0] = CreateGeneratedCoordinate(minimumX, minimumY, ordinateSource);
            coordinates[1] = CreateGeneratedCoordinate(maximumX, minimumY, ordinateSource);
            coordinates[2] = CreateGeneratedCoordinate(maximumX, maximumY, ordinateSource);
            coordinates[3] = CreateGeneratedCoordinate(minimumX, maximumY, ordinateSource);
        }

        coordinates[4] = CreateGeneratedCoordinate(coordinates[0].X, coordinates[0].Y, ordinateSource);
        LinearRing ring = new LinearRing();
        ring.Coordinates = coordinates;
        return ring;
    }

    internal static Coordinate CreateGeneratedCoordinate(double x, double y, Coordinate ordinateSource)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        coordinate.Z = ordinateSource.Z;
        coordinate.M = ordinateSource.M;
        return coordinate;
    }

    internal static void ValidateRectangleExtraOrdinates(Coordinate minimum, Coordinate maximum, CoordinateLayout coordinateLayout)
    {
        switch (coordinateLayout)
        {
            case CoordinateLayout.XY:
                return;
            case CoordinateLayout.XYZ:
                if (minimum.Z == maximum.Z) return;
                break;
            case CoordinateLayout.XYM:
                if (minimum.M == maximum.M) return;
                break;
            case CoordinateLayout.XYZM:
                if (minimum.Z == maximum.Z && minimum.M == maximum.M) return;
                break;
            default:
                break;
        }

        throw new InvalidGeometryException("Rectangle corners must carry equal configured Z and M ordinates.");
    }
}
