namespace geo;

internal static class EnvelopeAlgorithms
{
    internal static Envelope CreateEmptyEnvelope()
    {
        return new Envelope();
    }

    internal static Envelope CreateCoordinateEnvelope(Coordinate coordinate)
    {
        ValidateCoordinate(coordinate);
        return CreateBoundsEnvelope(coordinate.X, coordinate.Y, coordinate.X, coordinate.Y);
    }

    internal static Envelope CreateBoundsEnvelope(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        if (!double.IsFinite(minimumX) || !double.IsFinite(minimumY) || !double.IsFinite(maximumX) || !double.IsFinite(maximumY))
        {
            throw new InvalidGeometryException("Envelope bounds must be finite.");
        }

        if (minimumX > maximumX || minimumY > maximumY)
        {
            throw new InvalidGeometryException("Envelope minimum bounds cannot exceed maximum bounds.");
        }

        Envelope envelope = new Envelope();
        envelope.MinX = minimumX;
        envelope.MinY = minimumY;
        envelope.MaxX = maximumX;
        envelope.MaxY = maximumY;
        return envelope;
    }

    internal static Envelope CreateGeometryEnvelope(Geometry geometry)
    {
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        Envelope envelope = CreateEmptyEnvelope();
        int index = 0;
        while (index < coordinates.Length)
        {
            envelope = ExpandEnvelopeByCoordinate(envelope, coordinates[index]);
            index++;
        }
        return ExpandEnvelopeByCircularArcs(geometry, envelope);
    }

    internal static Envelope ExpandEnvelopeByCircularArcs(
        Geometry geometry, Envelope envelope)
    {
        if (geometry.GeometryType == GeometryType.CircularString)
        {
            return CircularStringAlgorithms.ExpandEnvelope(
                envelope, (CircularString)
                    GeometryDispatch.GetSelectedPayload(geometry));
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            envelope = ExpandEnvelopeByCircularArcs(
                children[index], envelope);
            index++;
        }
        return envelope;
    }

    internal static Envelope CopyEnvelopeValue(Envelope envelope)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        if (empty)
        {
            return CreateEmptyEnvelope();
        }

        return CreateBoundsEnvelope(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
    }

    internal static Envelope ExpandEnvelopeByCoordinate(Envelope envelope, Coordinate coordinate)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        ValidateCoordinate(coordinate);
        if (empty)
        {
            return CreateCoordinateEnvelope(coordinate);
        }

        double minimumX = envelope.MinX;
        double minimumY = envelope.MinY;
        double maximumX = envelope.MaxX;
        double maximumY = envelope.MaxY;
        if (coordinate.X < minimumX) minimumX = coordinate.X;
        if (coordinate.Y < minimumY) minimumY = coordinate.Y;
        if (coordinate.X > maximumX) maximumX = coordinate.X;
        if (coordinate.Y > maximumY) maximumY = coordinate.Y;
        return CreateBoundsEnvelope(minimumX, minimumY, maximumX, maximumY);
    }

    internal static Envelope ExpandEnvelopeByEnvelope(Envelope envelope, Envelope addition)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        bool additionEmpty = ValidateAndCheckEmpty(addition);
        if (empty)
        {
            return CopyEnvelopeValue(addition);
        }

        if (additionEmpty)
        {
            return CopyEnvelopeValue(envelope);
        }

        double minimumX = envelope.MinX;
        double minimumY = envelope.MinY;
        double maximumX = envelope.MaxX;
        double maximumY = envelope.MaxY;
        if (addition.MinX < minimumX) minimumX = addition.MinX;
        if (addition.MinY < minimumY) minimumY = addition.MinY;
        if (addition.MaxX > maximumX) maximumX = addition.MaxX;
        if (addition.MaxY > maximumY) maximumY = addition.MaxY;
        return CreateBoundsEnvelope(minimumX, minimumY, maximumX, maximumY);
    }

    internal static Envelope ExpandEnvelopeByDistance(Envelope envelope, double distance)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        if (!double.IsFinite(distance) || distance < 0.0)
        {
            throw new InvalidGeometryException("Envelope expansion distance must be finite and nonnegative.");
        }

        if (empty)
        {
            return CreateEmptyEnvelope();
        }

        double minimumX = envelope.MinX - distance;
        double minimumY = envelope.MinY - distance;
        double maximumX = envelope.MaxX + distance;
        double maximumY = envelope.MaxY + distance;
        if (!double.IsFinite(minimumX) || !double.IsFinite(minimumY) || !double.IsFinite(maximumX) || !double.IsFinite(maximumY))
        {
            throw new InvalidGeometryException("Envelope expansion produced non-finite bounds.");
        }

        return CreateBoundsEnvelope(minimumX, minimumY, maximumX, maximumY);
    }

    internal static bool ContainsEnvelopeCoordinate(Envelope envelope, Coordinate coordinate)
    {
        return CoversEnvelopeCoordinate(envelope, coordinate);
    }

    internal static bool ContainsEnvelopeValue(Envelope envelope, Envelope candidate)
    {
        return CoversEnvelopeValue(envelope, candidate);
    }

    internal static bool CoversEnvelopeCoordinate(Envelope envelope, Coordinate coordinate)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        ValidateCoordinate(coordinate);
        if (empty)
        {
            return false;
        }

        return coordinate.X >= envelope.MinX && coordinate.X <= envelope.MaxX && coordinate.Y >= envelope.MinY && coordinate.Y <= envelope.MaxY;
    }

    internal static bool CoversEnvelopeValue(Envelope envelope, Envelope candidate)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        bool candidateEmpty = ValidateAndCheckEmpty(candidate);
        if (empty || candidateEmpty)
        {
            return false;
        }

        return candidate.MinX >= envelope.MinX && candidate.MaxX <= envelope.MaxX && candidate.MinY >= envelope.MinY && candidate.MaxY <= envelope.MaxY;
    }

    internal static bool IntersectsEnvelopeValue(Envelope first, Envelope second)
    {
        bool firstEmpty = ValidateAndCheckEmpty(first);
        bool secondEmpty = ValidateAndCheckEmpty(second);
        if (firstEmpty || secondEmpty)
        {
            return false;
        }

        return first.MinX <= second.MaxX && first.MaxX >= second.MinX && first.MinY <= second.MaxY && first.MaxY >= second.MinY;
    }

    internal static bool IsDisjointEnvelopeValue(Envelope first, Envelope second)
    {
        return !IntersectsEnvelopeValue(first, second);
    }

    internal static Envelope IntersectEnvelopeValues(Envelope first, Envelope second)
    {
        if (!IntersectsEnvelopeValue(first, second))
        {
            return CreateEmptyEnvelope();
        }

        double minimumX = first.MinX;
        double minimumY = first.MinY;
        double maximumX = first.MaxX;
        double maximumY = first.MaxY;
        if (second.MinX > minimumX) minimumX = second.MinX;
        if (second.MinY > minimumY) minimumY = second.MinY;
        if (second.MaxX < maximumX) maximumX = second.MaxX;
        if (second.MaxY < maximumY) maximumY = second.MaxY;
        return CreateBoundsEnvelope(minimumX, minimumY, maximumX, maximumY);
    }

    internal static Envelope UnionEnvelopeValues(Envelope first, Envelope second)
    {
        return ExpandEnvelopeByEnvelope(first, second);
    }

    internal static double DistanceBetweenEnvelopes(Envelope first, Envelope second)
    {
        bool firstEmpty = ValidateAndCheckEmpty(first);
        bool secondEmpty = ValidateAndCheckEmpty(second);
        if (firstEmpty || secondEmpty)
        {
            return double.NaN;
        }

        if (IntersectsEnvelopeValue(first, second))
        {
            return 0.0;
        }

        double horizontal = 0.0;
        double vertical = 0.0;
        if (first.MaxX < second.MinX) horizontal = second.MinX - first.MaxX;
        if (second.MaxX < first.MinX) horizontal = first.MinX - second.MaxX;
        if (first.MaxY < second.MinY) vertical = second.MinY - first.MaxY;
        if (second.MaxY < first.MinY) vertical = first.MinY - second.MaxY;
        return ScaledDistance(horizontal, vertical);
    }

    internal static Coordinate? GetEnvelopeCenter(Envelope envelope)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        if (empty)
        {
            return null;
        }

        Coordinate center = new Coordinate();
        center.X = envelope.MinX + (envelope.MaxX - envelope.MinX) / 2.0;
        center.Y = envelope.MinY + (envelope.MaxY - envelope.MinY) / 2.0;
        if (!double.IsFinite(center.X) || !double.IsFinite(center.Y))
        {
            center.X = envelope.MinX / 2.0 + envelope.MaxX / 2.0;
            center.Y = envelope.MinY / 2.0 + envelope.MaxY / 2.0;
        }

        return center;
    }

    internal static double GetEnvelopeWidth(Envelope envelope)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        if (empty)
        {
            return 0.0;
        }

        return envelope.MaxX - envelope.MinX;
    }

    internal static double GetEnvelopeHeight(Envelope envelope)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        if (empty)
        {
            return 0.0;
        }

        return envelope.MaxY - envelope.MinY;
    }

    internal static double GetEnvelopeArea(Envelope envelope)
    {
        double width = GetEnvelopeWidth(envelope);
        double height = GetEnvelopeHeight(envelope);
        return width * height;
    }

    internal static Geometry ConvertEnvelopeToPolygon(Envelope envelope, GeometryFactoryOptions options)
    {
        bool empty = ValidateAndCheckEmpty(envelope);
        GeometryFactoryOptions optionsCopy = GeometryBuilder.CopyAndValidateOptions(options);
        if (empty)
        {
            return GeometryBuilder.CreateEmptyValue(GeometryType.Polygon, optionsCopy);
        }

        return FactoryConstructions.CreateEnvelopePolygonValue(envelope, optionsCopy);
    }

    internal static bool ValidateAndCheckEmpty(Envelope envelope)
    {
        if (envelope == null)
        {
            throw new InvalidGeometryException("Envelope is null.");
        }

        bool minimumXMissing = double.IsNaN(envelope.MinX);
        bool minimumYMissing = double.IsNaN(envelope.MinY);
        bool maximumXMissing = double.IsNaN(envelope.MaxX);
        bool maximumYMissing = double.IsNaN(envelope.MaxY);
        if (minimumXMissing && minimumYMissing && maximumXMissing && maximumYMissing)
        {
            return true;
        }

        if (minimumXMissing || minimumYMissing || maximumXMissing || maximumYMissing)
        {
            throw new InvalidGeometryException("An empty envelope must use no-data for all four bounds.");
        }

        if (!double.IsFinite(envelope.MinX) || !double.IsFinite(envelope.MinY) || !double.IsFinite(envelope.MaxX) || !double.IsFinite(envelope.MaxY))
        {
            throw new InvalidGeometryException("Envelope bounds must be finite.");
        }

        if (envelope.MinX > envelope.MaxX || envelope.MinY > envelope.MaxY)
        {
            throw new InvalidGeometryException("Envelope minimum bounds cannot exceed maximum bounds.");
        }

        return false;
    }

    internal static void ValidateCoordinate(Coordinate coordinate)
    {
        if (coordinate == null || !double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y))
        {
            throw new InvalidGeometryException("Envelope coordinates require finite X and Y ordinates.");
        }
    }

    internal static double ScaledDistance(double horizontal, double vertical)
    {
        if (double.IsPositiveInfinity(horizontal) ||
            double.IsPositiveInfinity(vertical))
        {
            return double.PositiveInfinity;
        }
        double maximum = horizontal;
        double minimum = vertical;
        if (vertical > horizontal)
        {
            maximum = vertical;
            minimum = horizontal;
        }

        if (maximum == 0.0)
        {
            return 0.0;
        }

        double ratio = minimum / maximum;
        return maximum * System.Math.Sqrt(1.0 + ratio * ratio);
    }
}
