namespace ttf;

internal enum SfntPathCommandKind
{
    Move = 0,
    Line = 1,
    Quadratic = 2,
    Close = 3,
    Cubic = 4
}

internal class SfntOutlinePoint
{
    double x;
    double y;
    bool onCurve;
    bool implied;
    int sourcePointIndex;

    internal SfntOutlinePoint(
        double x,
        double y,
        bool onCurve,
        bool implied,
        int sourcePointIndex)
    {
        this.x = x;
        this.y = y;
        this.onCurve = onCurve;
        this.implied = implied;
        this.sourcePointIndex = sourcePointIndex;
    }

    internal double GetX()
    {
        return x;
    }

    internal double GetY()
    {
        return y;
    }

    internal bool IsOnCurve()
    {
        return onCurve;
    }

    internal bool IsImplied()
    {
        return implied;
    }

    internal bool HasSourcePoint()
    {
        return sourcePointIndex >= 0;
    }

    internal int GetSourcePointIndex()
    {
        if (sourcePointIndex < 0)
        {
            throw new System.InvalidOperationException(
                "An implied outline point has no source point index.");
        }

        return sourcePointIndex;
    }
}

internal class SfntPathCommand
{
    SfntPathCommandKind kind;
    double controlX;
    double controlY;
    double secondControlX;
    double secondControlY;
    double endX;
    double endY;

    internal static SfntPathCommand Create(
        SfntPathCommandKind kind,
        double controlX,
        double controlY,
        double endX,
        double endY)
    {
        return new SfntPathCommand(
            kind,
            controlX,
            controlY,
            0,
            0,
            endX,
            endY);
    }

    internal SfntPathCommand(
        SfntPathCommandKind kind,
        double controlX,
        double controlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        this.kind = kind;
        this.controlX = controlX;
        this.controlY = controlY;
        this.secondControlX = secondControlX;
        this.secondControlY = secondControlY;
        this.endX = endX;
        this.endY = endY;
    }

    internal static SfntPathCommand Create(
        SfntPathCommandKind kind,
        double controlX,
        double controlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        return new SfntPathCommand(
            kind,
            controlX,
            controlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
    }

    internal SfntPathCommandKind GetKind()
    {
        return kind;
    }

    internal double GetControlX()
    {
        EnsureCurve();
        return controlX;
    }

    internal double GetControlY()
    {
        EnsureCurve();
        return controlY;
    }

    internal double GetSecondControlX()
    {
        EnsureCubic();
        return secondControlX;
    }

    internal double GetSecondControlY()
    {
        EnsureCubic();
        return secondControlY;
    }

    internal double GetEndX()
    {
        EnsureHasEndPoint();
        return endX;
    }

    internal double GetEndY()
    {
        EnsureHasEndPoint();
        return endY;
    }

    void EnsureCurve()
    {
        if (kind != SfntPathCommandKind.Quadratic &&
            kind != SfntPathCommandKind.Cubic)
        {
            throw new System.InvalidOperationException(
                "Only a curve path command has a control point.");
        }
    }

    void EnsureCubic()
    {
        if (kind != SfntPathCommandKind.Cubic)
        {
            throw new System.InvalidOperationException(
                "Only a cubic path command has a second control point.");
        }
    }

    void EnsureHasEndPoint()
    {
        if (kind == SfntPathCommandKind.Close)
        {
            throw new System.InvalidOperationException(
                "A close path command has no explicit end point.");
        }
    }
}

internal class SfntOutlineContour
{
    SfntOutlinePoint[] points;
    SfntPathCommand[] commands;
    int impliedPointCount;

    internal SfntOutlineContour(
        SfntOutlinePoint[] points,
        SfntPathCommand[] commands,
        int impliedPointCount)
    {
        this.points = points;
        this.commands = commands;
        this.impliedPointCount = impliedPointCount;
    }

    internal int GetPointCount()
    {
        return points.Length;
    }

    internal SfntOutlinePoint GetPoint(int index)
    {
        if (index < 0 || index >= points.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return points[index];
    }

    internal int GetCommandCount()
    {
        return commands.Length;
    }

    internal SfntPathCommand GetCommand(int index)
    {
        if (index < 0 || index >= commands.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return commands[index];
    }

    internal int GetImpliedPointCount()
    {
        return impliedPointCount;
    }
}

internal class SfntGlyphOutline
{
    int glyphId;
    SfntOutlineContour[] contours;
    SfntPathCommand[] commands;
    int normalizedPointCount;
    int impliedPointCount;
    SfntPathBounds? bounds;
    object boundsSync;

    internal SfntGlyphOutline(
        int glyphId,
        SfntOutlineContour[] contours,
        SfntPathCommand[] commands,
        int normalizedPointCount,
        int impliedPointCount)
    {
        this.glyphId = glyphId;
        this.contours = contours;
        this.commands = commands;
        this.normalizedPointCount = normalizedPointCount;
        this.impliedPointCount = impliedPointCount;
        boundsSync = new object();
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetContourCount()
    {
        return contours.Length;
    }

    internal SfntOutlineContour GetContour(int index)
    {
        if (index < 0 || index >= contours.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return contours[index];
    }

    internal int GetCommandCount()
    {
        return commands.Length;
    }

    internal SfntPathCommand GetCommand(int index)
    {
        if (index < 0 || index >= commands.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return commands[index];
    }

    internal int GetNormalizedPointCount()
    {
        return normalizedPointCount;
    }

    internal int GetImpliedPointCount()
    {
        return impliedPointCount;
    }

    internal SfntPathBounds GetBounds()
    {
        lock (boundsSync)
        {
            if (bounds == null)
            {
                bounds = SfntPathBoundsCalculator.Calculate(this);
            }

            return bounds;
        }
    }
}

internal static class SfntSimpleGlyphNormalizer
{
    internal static SfntGlyphOutline Normalize(SfntSimpleGlyph glyph)
    {
        if (glyph == null)
        {
            throw new System.ArgumentNullException("glyph");
        }

        int expectedPointCount = CountNormalizedPoints(glyph);

        if (expectedPointCount > glyph.GetMaximumNormalizedPointCount())
        {
            throw FontLimitException.Create(
                "A normalized simple glyph exceeds the configured point-count limit.");
        }

        int contourCount = glyph.GetContourCount();
        SfntOutlineContour[] contours = new SfntOutlineContour[contourCount];
        System.Collections.Generic.List<SfntPathCommand> allCommands =
            new System.Collections.Generic.List<SfntPathCommand>();
        int normalizedPointCount = 0;
        int impliedPointCount = 0;
        int contourIndex = 0;

        while (contourIndex < contourCount)
        {
            SfntOutlineContour contour = NormalizeContour(glyph, contourIndex);
            contours[contourIndex] = contour;
            normalizedPointCount += contour.GetPointCount();
            impliedPointCount += contour.GetImpliedPointCount();
            int commandIndex = 0;

            while (commandIndex < contour.GetCommandCount())
            {
                allCommands.Add(contour.GetCommand(commandIndex));
                commandIndex += 1;
            }

            contourIndex += 1;
        }

        return new SfntGlyphOutline(
            glyph.GetGlyphId(),
            contours,
            allCommands.ToArray(),
            normalizedPointCount,
            impliedPointCount);
    }

    static int CountNormalizedPoints(SfntSimpleGlyph glyph)
    {
        int totalPointCount = glyph.GetPointCount();
        int contourIndex = 0;

        while (contourIndex < glyph.GetContourCount())
        {
            int startPointIndex = glyph.GetContourStartPoint(contourIndex);
            int endPointIndex = glyph.GetContourEndPoint(contourIndex);
            SfntSimpleGlyphPoint previousPoint = glyph.GetPoint(endPointIndex);
            int pointIndex = startPointIndex;

            while (pointIndex <= endPointIndex)
            {
                SfntSimpleGlyphPoint point = glyph.GetPoint(pointIndex);

                if (!previousPoint.IsOnCurve() && !point.IsOnCurve())
                {
                    totalPointCount = CheckedFontMath.AddCounts(
                        totalPointCount,
                        1);
                }

                previousPoint = point;
                pointIndex += 1;
            }

            contourIndex += 1;
        }

        return totalPointCount;
    }

    static SfntOutlineContour NormalizeContour(
        SfntSimpleGlyph glyph,
        int contourIndex)
    {
        int startPointIndex = glyph.GetContourStartPoint(contourIndex);
        int endPointIndex = glyph.GetContourEndPoint(contourIndex);
        SfntSimpleGlyphPoint firstPoint = glyph.GetPoint(startPointIndex);
        SfntSimpleGlyphPoint lastPoint = glyph.GetPoint(endPointIndex);
        System.Collections.Generic.List<SfntOutlinePoint> points =
            new System.Collections.Generic.List<SfntOutlinePoint>();

        if (firstPoint.IsOnCurve())
        {
            AddSourcePoint(points, firstPoint);
            AddPointRange(
                points,
                glyph,
                startPointIndex + 1,
                endPointIndex,
                firstPoint);
        }
        else if (lastPoint.IsOnCurve())
        {
            AddSourcePoint(points, lastPoint);
            AddPointRange(
                points,
                glyph,
                startPointIndex,
                endPointIndex - 1,
                lastPoint);
        }
        else
        {
            points.Add(CreateImpliedPoint(lastPoint, firstPoint));
            AddPointRange(
                points,
                glyph,
                startPointIndex,
                endPointIndex,
                null);
        }

        SfntOutlinePoint[] normalizedPoints = points.ToArray();
        SfntPathCommand[] commands = CreateCommands(normalizedPoints);
        int impliedPointCount = 0;
        int pointIndex = 0;

        while (pointIndex < normalizedPoints.Length)
        {
            if (normalizedPoints[pointIndex].IsImplied())
            {
                impliedPointCount += 1;
            }

            pointIndex += 1;
        }

        return new SfntOutlineContour(
            normalizedPoints,
            commands,
            impliedPointCount);
    }

    static void AddPointRange(
        System.Collections.Generic.List<SfntOutlinePoint> points,
        SfntSimpleGlyph glyph,
        int startPointIndex,
        int endPointIndex,
        SfntSimpleGlyphPoint? previousPoint)
    {
        int pointIndex = startPointIndex;

        while (pointIndex <= endPointIndex)
        {
            SfntSimpleGlyphPoint point = glyph.GetPoint(pointIndex);

            if (previousPoint != null &&
                !previousPoint.IsOnCurve() &&
                !point.IsOnCurve())
            {
                points.Add(CreateImpliedPoint(previousPoint, point));
            }

            AddSourcePoint(points, point);
            previousPoint = point;
            pointIndex += 1;
        }
    }

    static void AddSourcePoint(
        System.Collections.Generic.List<SfntOutlinePoint> points,
        SfntSimpleGlyphPoint point)
    {
        points.Add(new SfntOutlinePoint(
            point.GetX(),
            point.GetY(),
            point.IsOnCurve(),
            false,
            point.GetPointIndex()));
    }

    static SfntOutlinePoint CreateImpliedPoint(
        SfntSimpleGlyphPoint first,
        SfntSimpleGlyphPoint second)
    {
        double x = (first.GetX() + second.GetX()) / 2.0;
        double y = (first.GetY() + second.GetY()) / 2.0;
        return new SfntOutlinePoint(x, y, true, true, -1);
    }

    static SfntPathCommand[] CreateCommands(SfntOutlinePoint[] points)
    {
        if (points.Length == 0 || !points[0].IsOnCurve())
        {
            throw FontFormatException.Create(
                "A normalized TrueType contour requires an on-curve start point.");
        }

        System.Collections.Generic.List<SfntPathCommand> commands =
            new System.Collections.Generic.List<SfntPathCommand>();
        SfntOutlinePoint startPoint = points[0];
        SfntOutlinePoint? controlPoint = null;
        commands.Add(CreateMove(startPoint));
        int pointIndex = 1;

        while (pointIndex < points.Length)
        {
            SfntOutlinePoint point = points[pointIndex];

            if (point.IsOnCurve())
            {
                if (controlPoint == null)
                {
                    commands.Add(CreateLine(point));
                }
                else
                {
                    commands.Add(CreateQuadratic(controlPoint, point));
                    controlPoint = null;
                }
            }
            else
            {
                if (controlPoint != null)
                {
                    throw FontFormatException.Create(
                        "A normalized TrueType contour has consecutive off-curve points.");
                }

                controlPoint = point;
            }

            pointIndex += 1;
        }

        if (controlPoint != null)
        {
            commands.Add(CreateQuadratic(controlPoint, startPoint));
        }

        commands.Add(SfntPathCommand.Create(
            SfntPathCommandKind.Close,
            0,
            0,
            0,
            0));
        return commands.ToArray();
    }

    static SfntPathCommand CreateMove(SfntOutlinePoint point)
    {
        return SfntPathCommand.Create(
            SfntPathCommandKind.Move,
            0,
            0,
            point.GetX(),
            point.GetY());
    }

    static SfntPathCommand CreateLine(SfntOutlinePoint point)
    {
        return SfntPathCommand.Create(
            SfntPathCommandKind.Line,
            0,
            0,
            point.GetX(),
            point.GetY());
    }

    static SfntPathCommand CreateQuadratic(
        SfntOutlinePoint controlPoint,
        SfntOutlinePoint endPoint)
    {
        return SfntPathCommand.Create(
            SfntPathCommandKind.Quadratic,
            controlPoint.GetX(),
            controlPoint.GetY(),
            endPoint.GetX(),
            endPoint.GetY());
    }
}
