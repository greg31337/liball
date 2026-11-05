namespace barcode;

internal class LineSegment
{
    internal BarcodePoint Start;
    internal BarcodePoint End;

    internal LineSegment(BarcodePoint start, BarcodePoint end)
    {
        Start = GeometryMath.CopyPointValue(start);
        End = GeometryMath.CopyPointValue(end);
    }

    internal double CalculateLength()
    {
        return GeometryMath.DistanceBetweenPoints(Start, End);
    }

    internal BarcodePoint CalculatePointAt(double fraction)
    {
        return GeometryMath.InterpolatePoint(Start, End, fraction);
    }
}
