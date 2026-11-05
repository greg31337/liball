namespace ttf;

internal class SfntGvarDeltaSet
{
    double[] xDeltas;
    double[] yDeltas;
    bool[] explicitPoints;
    bool[] inferredPoints;

    internal SfntGvarDeltaSet(
        double[] xDeltas,
        double[] yDeltas,
        bool[] explicitPoints,
        bool[] inferredPoints)
    {
        this.xDeltas = xDeltas;
        this.yDeltas = yDeltas;
        this.explicitPoints = explicitPoints;
        this.inferredPoints = inferredPoints;
    }

    internal int GetPointCount()
    {
        return xDeltas.Length;
    }

    internal double GetXDelta(int pointIndex)
    {
        ValidatePointIndex(pointIndex);
        return xDeltas[pointIndex];
    }

    internal double GetYDelta(int pointIndex)
    {
        ValidatePointIndex(pointIndex);
        return yDeltas[pointIndex];
    }

    internal bool IsExplicit(int pointIndex)
    {
        ValidatePointIndex(pointIndex);
        return explicitPoints[pointIndex];
    }

    internal bool IsInferred(int pointIndex)
    {
        ValidatePointIndex(pointIndex);
        return inferredPoints[pointIndex];
    }

    void ValidatePointIndex(int pointIndex)
    {
        if (pointIndex < 0 || pointIndex >= xDeltas.Length)
        {
            throw new System.ArgumentOutOfRangeException("pointIndex");
        }
    }
}

internal static class SfntGvarTupleScalar
{
    internal static double Calculate(
        SfntGvarTupleVariation variation,
        double[] normalizedCoordinates)
    {
        if (variation == null)
        {
            throw new System.ArgumentNullException("variation");
        }

        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        SfntGvarTuple peakTuple = variation.GetPeakTuple();

        return Calculate(
            peakTuple,
            variation.GetIntermediateStartTuple(),
            variation.GetIntermediateEndTuple(),
            variation.HasIntermediateRegion(),
            normalizedCoordinates);
    }

    internal static double Calculate(
        SfntGvarTuple peakTuple,
        SfntGvarTuple? intermediateStartTuple,
        SfntGvarTuple? intermediateEndTuple,
        bool hasIntermediateRegion,
        double[] normalizedCoordinates)
    {
        if (peakTuple == null)
        {
            throw new System.ArgumentNullException("peakTuple");
        }

        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        if (hasIntermediateRegion &&
            (intermediateStartTuple == null || intermediateEndTuple == null))
        {
            throw new System.ArgumentNullException(
                intermediateStartTuple == null ?
                "intermediateStartTuple" : "intermediateEndTuple");
        }

        if (normalizedCoordinates.Length != peakTuple.GetAxisCount())
        {
            throw new System.ArgumentException(
                "The normalized coordinate count does not match the gvar tuple axis count.",
                "normalizedCoordinates");
        }

        double scalar = 1;
        int axisIndex = 0;

        while (axisIndex < normalizedCoordinates.Length && scalar != 0)
        {
            double coordinate = normalizedCoordinates[axisIndex];

            if (!double.IsFinite(coordinate) || coordinate < -1 || coordinate > 1)
            {
                throw new System.ArgumentOutOfRangeException(
                    "normalizedCoordinates");
            }

            double peak = peakTuple.GetCoordinate(axisIndex);
            double start = System.Math.Min(0, peak);
            double end = System.Math.Max(0, peak);
            if (hasIntermediateRegion)
            {
                System.ArgumentNullException.ThrowIfNull(intermediateStartTuple);
                System.ArgumentNullException.ThrowIfNull(intermediateEndTuple);
                start = intermediateStartTuple.GetCoordinate(axisIndex);
                end = intermediateEndTuple.GetCoordinate(axisIndex);
            }
            scalar *= CalculateAxisScalar(coordinate, start, peak, end);
            axisIndex += 1;
        }

        return scalar;
    }

    static double CalculateAxisScalar(
        double coordinate,
        double start,
        double peak,
        double end)
    {
        if (start > peak || peak > end ||
            start < 0 && end > 0 && peak != 0)
        {
            return 1;
        }

        if (peak == 0)
        {
            return 1;
        }

        if (coordinate < start || coordinate > end)
        {
            return 0;
        }

        if (coordinate == peak)
        {
            return 1;
        }

        if (coordinate < peak)
        {
            return (coordinate - start) / (peak - start);
        }

        return (end - coordinate) / (end - peak);
    }
}

internal static class SfntGvarIupInterpolator
{
    internal static SfntGvarDeltaSet CreateSelectedPointDeltas(
        SfntGvarTupleVariation variation,
        int targetPointCount)
    {
        if (variation == null)
        {
            throw new System.ArgumentNullException("variation");
        }

        if (targetPointCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("targetPointCount");
        }

        if (variation.GetDeltaCount() !=
            (variation.GetPointNumbers().UsesAllPoints() ?
            targetPointCount : variation.GetPointNumbers().GetCount()))
        {
            throw FontFormatException.Create(
                "A gvar tuple's delta count does not match its point selection.");
        }

        double[] xDeltas = new double[targetPointCount];
        double[] yDeltas = new double[targetPointCount];
        bool[] explicitPoints = new bool[targetPointCount];
        bool[] inferredPoints = new bool[targetPointCount];

        if (variation.GetPointNumbers().UsesAllPoints())
        {
            CopyAllPointDeltas(
                variation,
                xDeltas,
                yDeltas,
                explicitPoints);
        }
        else
        {
            CopySparseDeltas(
                variation,
                xDeltas,
                yDeltas,
                explicitPoints);
        }

        return new SfntGvarDeltaSet(
            xDeltas,
            yDeltas,
            explicitPoints,
            inferredPoints);
    }

    internal static SfntGvarDeltaSet CreateAllPointDeltas(
        SfntGvarTupleVariation variation,
        SfntSimpleGlyph glyph)
    {
        if (variation == null)
        {
            throw new System.ArgumentNullException("variation");
        }

        if (glyph == null)
        {
            throw new System.ArgumentNullException("glyph");
        }

        int outlinePointCount = glyph.GetPointCount();
        int targetPointCount = CheckedFontMath.AddOffset(outlinePointCount, 4);

        if (variation.GetDeltaCount() !=
            (variation.GetPointNumbers().UsesAllPoints() ?
            targetPointCount : variation.GetPointNumbers().GetCount()))
        {
            throw FontFormatException.Create(
                "A gvar tuple's delta count does not match its point selection.");
        }

        double[] xDeltas = new double[targetPointCount];
        double[] yDeltas = new double[targetPointCount];
        bool[] explicitPoints = new bool[targetPointCount];
        bool[] inferredPoints = new bool[targetPointCount];

        if (variation.GetPointNumbers().UsesAllPoints())
        {
            CopyAllPointDeltas(
                variation,
                xDeltas,
                yDeltas,
                explicitPoints);
        }
        else
        {
            CopySparseDeltas(
                variation,
                xDeltas,
                yDeltas,
                explicitPoints);
            InferContours(
                glyph,
                xDeltas,
                yDeltas,
                explicitPoints,
                inferredPoints);
        }

        return new SfntGvarDeltaSet(
            xDeltas,
            yDeltas,
            explicitPoints,
            inferredPoints);
    }

    static void CopyAllPointDeltas(
        SfntGvarTupleVariation variation,
        double[] xDeltas,
        double[] yDeltas,
        bool[] explicitPoints)
    {
        int pointIndex = 0;

        while (pointIndex < xDeltas.Length)
        {
            xDeltas[pointIndex] = variation.GetXDelta(pointIndex);
            yDeltas[pointIndex] = variation.GetYDelta(pointIndex);
            explicitPoints[pointIndex] = true;
            pointIndex += 1;
        }
    }

    static void CopySparseDeltas(
        SfntGvarTupleVariation variation,
        double[] xDeltas,
        double[] yDeltas,
        bool[] explicitPoints)
    {
        SfntGvarPointNumbers pointNumbers = variation.GetPointNumbers();
        int deltaIndex = 0;

        while (deltaIndex < pointNumbers.GetCount())
        {
            int pointIndex = pointNumbers.GetPointNumber(deltaIndex);

            if (pointIndex < 0 || pointIndex >= xDeltas.Length)
            {
                throw FontFormatException.Create(
                    "A gvar tuple references a point outside the target glyph.");
            }

            xDeltas[pointIndex] = variation.GetXDelta(deltaIndex);
            yDeltas[pointIndex] = variation.GetYDelta(deltaIndex);
            explicitPoints[pointIndex] = true;
            deltaIndex += 1;
        }
    }

    static void InferContours(
        SfntSimpleGlyph glyph,
        double[] xDeltas,
        double[] yDeltas,
        bool[] explicitPoints,
        bool[] inferredPoints)
    {
        int contourIndex = 0;

        while (contourIndex < glyph.GetContourCount())
        {
            int startPoint = glyph.GetContourStartPoint(contourIndex);
            int endPoint = glyph.GetContourEndPoint(contourIndex);
            int firstExplicit = FindFirstExplicit(
                explicitPoints,
                startPoint,
                endPoint);

            if (firstExplicit >= 0)
            {
                InferContour(
                    glyph,
                    startPoint,
                    endPoint,
                    firstExplicit,
                    xDeltas,
                    yDeltas,
                    explicitPoints,
                    inferredPoints);
            }

            contourIndex += 1;
        }
    }

    static int FindFirstExplicit(
        bool[] explicitPoints,
        int startPoint,
        int endPoint)
    {
        int pointIndex = startPoint;

        while (pointIndex <= endPoint)
        {
            if (explicitPoints[pointIndex])
            {
                return pointIndex;
            }

            pointIndex += 1;
        }

        return -1;
    }

    static void InferContour(
        SfntSimpleGlyph glyph,
        int startPoint,
        int endPoint,
        int firstExplicit,
        double[] xDeltas,
        double[] yDeltas,
        bool[] explicitPoints,
        bool[] inferredPoints)
    {
        int preceding = firstExplicit;
        int search = NextPoint(preceding, startPoint, endPoint);

        while (search != firstExplicit)
        {
            if (explicitPoints[search])
            {
                InferGap(
                    glyph,
                    preceding,
                    search,
                    startPoint,
                    endPoint,
                    xDeltas,
                    yDeltas,
                    inferredPoints);
                preceding = search;
            }

            search = NextPoint(search, startPoint, endPoint);
        }

        InferGap(
            glyph,
            preceding,
            firstExplicit,
            startPoint,
            endPoint,
            xDeltas,
            yDeltas,
            inferredPoints);
    }

    static void InferGap(
        SfntSimpleGlyph glyph,
        int preceding,
        int following,
        int startPoint,
        int endPoint,
        double[] xDeltas,
        double[] yDeltas,
        bool[] inferredPoints)
    {
        SfntSimpleGlyphPoint precedingPoint = glyph.GetPoint(preceding);
        SfntSimpleGlyphPoint followingPoint = glyph.GetPoint(following);
        int target = NextPoint(preceding, startPoint, endPoint);

        while (target != following)
        {
            SfntSimpleGlyphPoint targetPoint = glyph.GetPoint(target);
            xDeltas[target] = InterpolateDelta(
                targetPoint.GetX(),
                precedingPoint.GetX(),
                xDeltas[preceding],
                followingPoint.GetX(),
                xDeltas[following]);
            yDeltas[target] = InterpolateDelta(
                targetPoint.GetY(),
                precedingPoint.GetY(),
                yDeltas[preceding],
                followingPoint.GetY(),
                yDeltas[following]);
            inferredPoints[target] = true;
            target = NextPoint(target, startPoint, endPoint);
        }
    }

    static int NextPoint(int pointIndex, int startPoint, int endPoint)
    {
        return pointIndex == endPoint ? startPoint : pointIndex + 1;
    }

    static double InterpolateDelta(
        double targetCoordinate,
        double precedingCoordinate,
        double precedingDelta,
        double followingCoordinate,
        double followingDelta)
    {
        if (precedingCoordinate == followingCoordinate)
        {
            return precedingDelta == followingDelta ? precedingDelta : 0;
        }

        double minimum = System.Math.Min(
            precedingCoordinate,
            followingCoordinate);
        double maximum = System.Math.Max(
            precedingCoordinate,
            followingCoordinate);

        if (targetCoordinate <= minimum)
        {
            return precedingCoordinate < followingCoordinate ?
                precedingDelta : followingDelta;
        }

        if (targetCoordinate >= maximum)
        {
            return precedingCoordinate > followingCoordinate ?
                precedingDelta : followingDelta;
        }

        double proportion = (targetCoordinate - precedingCoordinate) /
            (followingCoordinate - precedingCoordinate);
        return (1 - proportion) * precedingDelta +
            proportion * followingDelta;
    }
}
