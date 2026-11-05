namespace ttf;

internal class SfntResolvedPoint
{
    double x;
    double y;
    bool onCurve;

    internal SfntResolvedPoint(double x, double y, bool onCurve)
    {
        this.x = x;
        this.y = y;
        this.onCurve = onCurve;
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
}

internal class SfntResolvedGlyph
{
    int glyphId;
    SfntGlyphOutline outline;
    SfntResolvedPoint[] controlPoints;
    int[] componentClosure;
    int componentDepth;
    int metricsGlyphId;
    bool composite;

    internal SfntResolvedGlyph(
        int glyphId,
        SfntGlyphOutline outline,
        SfntResolvedPoint[] controlPoints,
        int[] componentClosure,
        int componentDepth,
        int metricsGlyphId,
        bool composite)
    {
        this.glyphId = glyphId;
        this.outline = outline;
        this.controlPoints = controlPoints;
        this.componentClosure = componentClosure;
        this.componentDepth = componentDepth;
        this.metricsGlyphId = metricsGlyphId;
        this.composite = composite;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal SfntGlyphOutline GetOutline()
    {
        return outline;
    }

    internal int GetControlPointCount()
    {
        return controlPoints.Length;
    }

    internal SfntResolvedPoint GetControlPoint(int index)
    {
        if (index < 0 || index >= controlPoints.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return controlPoints[index];
    }

    internal int GetComponentClosureCount()
    {
        return componentClosure.Length;
    }

    internal int GetComponentClosureGlyphId(int index)
    {
        if (index < 0 || index >= componentClosure.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return componentClosure[index];
    }

    internal int GetComponentDepth()
    {
        return componentDepth;
    }

    internal int GetMetricsGlyphId()
    {
        return metricsGlyphId;
    }

    internal bool UsesInheritedMetrics()
    {
        return metricsGlyphId != glyphId;
    }

    internal bool IsComposite()
    {
        return composite;
    }
}

internal static class SfntCompositeResolver
{
    internal static SfntResolvedGlyph Resolve(
        SfntLocaTable table,
        int glyphId,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (maxp == null)
        {
            throw new System.ArgumentNullException("maxp");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (glyphId < 0 || glyphId >= table.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        bool[] activeGlyphs = new bool[table.GetGlyphCount()];
        SfntResolvedGlyph[] resolvedGlyphs =
            new SfntResolvedGlyph[table.GetGlyphCount()];
        return ResolveGlyph(
            table,
            glyphId,
            maxp,
            limits,
            activeGlyphs,
            resolvedGlyphs,
            1);
    }

    static SfntResolvedGlyph ResolveGlyph(
        SfntLocaTable table,
        int glyphId,
        SfntMaxpTable maxp,
        FontLimits limits,
        bool[] activeGlyphs,
        SfntResolvedGlyph[] resolvedGlyphs,
        int nestingLevel)
    {
        if (activeGlyphs[glyphId])
        {
            throw FontFormatException.Create(
                "The composite glyph graph contains a cycle.");
        }

        if (resolvedGlyphs[glyphId] != null)
        {
            return resolvedGlyphs[glyphId];
        }

        activeGlyphs[glyphId] = true;
        SfntResolvedGlyph result;
        SfntGlyfRecord record = table.GetGlyphRecord(glyphId);

        if (record.IsEmpty())
        {
            result = CreateEmptyGlyph(glyphId);
        }
        else
        {
            SfntSimpleGlyph? simpleGlyph = table.GetSimpleGlyph(glyphId);

            if (simpleGlyph != null)
            {
                result = ResolveSimpleGlyph(simpleGlyph);
            }
            else
            {
                SfntCompositeGlyph? compositeGlyph =
                    table.GetCompositeGlyph(glyphId);

                if (compositeGlyph == null)
                {
                    throw FontFormatException.Create(
                        "A nonempty glyf record has no recognized outline form.");
                }

                ValidateNestingLevel(nestingLevel, maxp, limits);
                result = ResolveCompositeGlyph(
                    table,
                    compositeGlyph,
                    maxp,
                    limits,
                    activeGlyphs,
                    resolvedGlyphs,
                    nestingLevel);
            }
        }

        activeGlyphs[glyphId] = false;
        resolvedGlyphs[glyphId] = result;
        return result;
    }

    static SfntResolvedGlyph CreateEmptyGlyph(int glyphId)
    {
        return new SfntResolvedGlyph(
            glyphId,
            CreateEmptyOutline(glyphId),
            new SfntResolvedPoint[0],
            new int[0],
            0,
            glyphId,
            false);
    }

    static SfntResolvedGlyph ResolveSimpleGlyph(SfntSimpleGlyph glyph)
    {
        SfntResolvedPoint[] points = new SfntResolvedPoint[glyph.GetPointCount()];
        int pointIndex = 0;

        while (pointIndex < points.Length)
        {
            SfntSimpleGlyphPoint point = glyph.GetPoint(pointIndex);
            points[pointIndex] = new SfntResolvedPoint(
                point.GetX(),
                point.GetY(),
                point.IsOnCurve());
            pointIndex += 1;
        }

        return new SfntResolvedGlyph(
            glyph.GetGlyphId(),
            glyph.GetNormalizedOutline(),
            points,
            new int[0],
            0,
            glyph.GetGlyphId(),
            false);
    }

    static SfntResolvedGlyph ResolveCompositeGlyph(
        SfntLocaTable table,
        SfntCompositeGlyph glyph,
        SfntMaxpTable maxp,
        FontLimits limits,
        bool[] activeGlyphs,
        SfntResolvedGlyph[] resolvedGlyphs,
        int nestingLevel)
    {
        System.Collections.Generic.List<SfntResolvedPoint> points =
            new System.Collections.Generic.List<SfntResolvedPoint>();
        System.Collections.Generic.List<SfntOutlineContour> contours =
            new System.Collections.Generic.List<SfntOutlineContour>();
        System.Collections.Generic.List<int> closure =
            new System.Collections.Generic.List<int>();
        bool[] closureMembers = new bool[table.GetGlyphCount()];
        int componentDepth = 1;
        int metricsGlyphId = glyph.GetGlyphId();
        int componentIndex = 0;

        while (componentIndex < glyph.GetComponentCount())
        {
            SfntCompositeComponent component = glyph.GetComponent(componentIndex);
            int childGlyphId = component.GetGlyphId();
            SfntResolvedGlyph child = ResolveGlyph(
                table,
                childGlyphId,
                maxp,
                limits,
                activeGlyphs,
                resolvedGlyphs,
                nestingLevel + 1);
            int childDepth = child.GetComponentDepth() + 1;

            if (childDepth > componentDepth)
            {
                componentDepth = childDepth;
            }

            AddClosureGlyph(closure, closureMembers, childGlyphId);
            int closureIndex = 0;

            while (closureIndex < child.GetComponentClosureCount())
            {
                AddClosureGlyph(
                    closure,
                    closureMembers,
                    child.GetComponentClosureGlyphId(closureIndex));
                closureIndex += 1;
            }

            SfntResolvedPoint[] transformedPoints = TransformControlPoints(
                child,
                component.GetTransform());
            double offsetX;
            double offsetY;

            if (component.GetPlacementKind() == SfntCompositePlacementKind.Offset)
            {
                offsetX = component.GetEffectiveXOffset();
                offsetY = component.GetEffectiveYOffset();

                if (component.RoundsOffsetToGrid())
                {
                    offsetX = RoundToGrid(offsetX);
                    offsetY = RoundToGrid(offsetY);
                }
            }
            else
            {
                int parentPointIndex = component.GetParentPointIndex();
                int childPointIndex = component.GetChildPointIndex();

                if (parentPointIndex >= points.Count ||
                    childPointIndex >= transformedPoints.Length)
                {
                    throw FontFormatException.Create(
                        "A composite point attachment is outside the resolved control points.");
                }

                SfntResolvedPoint parentPoint = points[parentPointIndex];
                SfntResolvedPoint childPoint = transformedPoints[childPointIndex];
                offsetX = parentPoint.GetX() - childPoint.GetX();
                offsetY = parentPoint.GetY() - childPoint.GetY();
            }

            int sourcePointOffset = points.Count;
            ValidateExpandedCounts(
                sourcePointOffset,
                transformedPoints.Length,
                contours.Count,
                child.GetOutline().GetContourCount(),
                maxp,
                limits);
            AppendControlPoints(points, transformedPoints, offsetX, offsetY);
            AppendTransformedContours(
                contours,
                child.GetOutline(),
                component.GetTransform(),
                offsetX,
                offsetY,
                sourcePointOffset);

            if (component.UsesMetrics())
            {
                metricsGlyphId = child.GetMetricsGlyphId();
            }

            componentIndex += 1;
        }

        maxp.VerifyCompositeGlyph(
            points.Count,
            contours.Count,
            glyph.GetInstructionLength(),
            glyph.GetComponentCount(),
            componentDepth,
            limits);
        SfntGlyphOutline outline = CreateOutline(
            glyph.GetGlyphId(),
            contours.ToArray());
        return new SfntResolvedGlyph(
            glyph.GetGlyphId(),
            outline,
            points.ToArray(),
            closure.ToArray(),
            componentDepth,
            metricsGlyphId,
            true);
    }

    static void ValidateNestingLevel(
        int nestingLevel,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        if (nestingLevel > limits.GetMaxComponentDepth())
        {
            throw FontLimitException.Create(
                "A composite glyph exceeds the configured component-depth limit.");
        }

        if (nestingLevel > maxp.GetMaxComponentDepth())
        {
            throw FontFormatException.Create(
                "A composite glyph exceeds maxp maxComponentDepth.");
        }
    }

    static void ValidateExpandedCounts(
        int currentPointCount,
        int addedPointCount,
        int currentContourCount,
        int addedContourCount,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        int pointCount = CheckedFontMath.AddCounts(
            currentPointCount,
            addedPointCount);
        int contourCount = CheckedFontMath.AddCounts(
            currentContourCount,
            addedContourCount);

        if (pointCount > limits.GetMaxPointsPerGlyph())
        {
            throw FontLimitException.Create(
                "A resolved composite exceeds the configured point-count limit.");
        }

        if (contourCount > limits.GetMaxContoursPerGlyph())
        {
            throw FontLimitException.Create(
                "A resolved composite exceeds the configured contour-count limit.");
        }

        if (pointCount > maxp.GetMaxCompositePoints() ||
            contourCount > maxp.GetMaxCompositeContours())
        {
            throw FontFormatException.Create(
                "A resolved composite exceeds maxp point or contour declarations.");
        }
    }

    static SfntResolvedPoint[] TransformControlPoints(
        SfntResolvedGlyph child,
        SfntCompositeTransform transform)
    {
        SfntResolvedPoint[] points =
            new SfntResolvedPoint[child.GetControlPointCount()];
        int pointIndex = 0;

        while (pointIndex < points.Length)
        {
            SfntResolvedPoint point = child.GetControlPoint(pointIndex);
            points[pointIndex] = new SfntResolvedPoint(
                transform.TransformX(point.GetX(), point.GetY()),
                transform.TransformY(point.GetX(), point.GetY()),
                point.IsOnCurve());
            pointIndex += 1;
        }

        return points;
    }

    static void AppendControlPoints(
        System.Collections.Generic.List<SfntResolvedPoint> destination,
        SfntResolvedPoint[] source,
        double offsetX,
        double offsetY)
    {
        int pointIndex = 0;

        while (pointIndex < source.Length)
        {
            SfntResolvedPoint point = source[pointIndex];
            destination.Add(new SfntResolvedPoint(
                point.GetX() + offsetX,
                point.GetY() + offsetY,
                point.IsOnCurve()));
            pointIndex += 1;
        }
    }

    static void AppendTransformedContours(
        System.Collections.Generic.List<SfntOutlineContour> destination,
        SfntGlyphOutline source,
        SfntCompositeTransform transform,
        double offsetX,
        double offsetY,
        int sourcePointOffset)
    {
        int contourIndex = 0;

        while (contourIndex < source.GetContourCount())
        {
            destination.Add(TransformContour(
                source.GetContour(contourIndex),
                transform,
                offsetX,
                offsetY,
                sourcePointOffset));
            contourIndex += 1;
        }
    }

    static SfntOutlineContour TransformContour(
        SfntOutlineContour source,
        SfntCompositeTransform transform,
        double offsetX,
        double offsetY,
        int sourcePointOffset)
    {
        SfntOutlinePoint[] points = new SfntOutlinePoint[source.GetPointCount()];
        int pointIndex = 0;

        while (pointIndex < points.Length)
        {
            SfntOutlinePoint point = source.GetPoint(pointIndex);
            int sourcePointIndex = point.HasSourcePoint()
                ? point.GetSourcePointIndex() + sourcePointOffset
                : -1;
            points[pointIndex] = new SfntOutlinePoint(
                transform.TransformX(point.GetX(), point.GetY()) + offsetX,
                transform.TransformY(point.GetX(), point.GetY()) + offsetY,
                point.IsOnCurve(),
                point.IsImplied(),
                sourcePointIndex);
            pointIndex += 1;
        }

        SfntPathCommand[] commands =
            new SfntPathCommand[source.GetCommandCount()];
        int commandIndex = 0;

        while (commandIndex < commands.Length)
        {
            commands[commandIndex] = TransformCommand(
                source.GetCommand(commandIndex),
                transform,
                offsetX,
                offsetY);
            commandIndex += 1;
        }

        return new SfntOutlineContour(
            points,
            commands,
            source.GetImpliedPointCount());
    }

    static SfntPathCommand TransformCommand(
        SfntPathCommand source,
        SfntCompositeTransform transform,
        double offsetX,
        double offsetY)
    {
        SfntPathCommandKind kind = source.GetKind();

        if (kind == SfntPathCommandKind.Close)
        {
            return SfntPathCommand.Create(kind, 0, 0, 0, 0);
        }

        double endX = transform.TransformX(
            source.GetEndX(),
            source.GetEndY()) + offsetX;
        double endY = transform.TransformY(
            source.GetEndX(),
            source.GetEndY()) + offsetY;

        if (kind == SfntPathCommandKind.Quadratic)
        {
            double controlX = transform.TransformX(
                source.GetControlX(),
                source.GetControlY()) + offsetX;
            double controlY = transform.TransformY(
                source.GetControlX(),
                source.GetControlY()) + offsetY;
            return SfntPathCommand.Create(
                kind,
                controlX,
                controlY,
                endX,
                endY);
        }

        if (kind == SfntPathCommandKind.Cubic)
        {
            double controlX = transform.TransformX(
                source.GetControlX(),
                source.GetControlY()) + offsetX;
            double controlY = transform.TransformY(
                source.GetControlX(),
                source.GetControlY()) + offsetY;
            double secondControlX = transform.TransformX(
                source.GetSecondControlX(),
                source.GetSecondControlY()) + offsetX;
            double secondControlY = transform.TransformY(
                source.GetSecondControlX(),
                source.GetSecondControlY()) + offsetY;
            return SfntPathCommand.Create(
                kind,
                controlX,
                controlY,
                secondControlX,
                secondControlY,
                endX,
                endY);
        }

        return SfntPathCommand.Create(kind, 0, 0, endX, endY);
    }

    static SfntGlyphOutline CreateOutline(
        int glyphId,
        SfntOutlineContour[] contours)
    {
        System.Collections.Generic.List<SfntPathCommand> commands =
            new System.Collections.Generic.List<SfntPathCommand>();
        int normalizedPointCount = 0;
        int impliedPointCount = 0;
        int contourIndex = 0;

        while (contourIndex < contours.Length)
        {
            SfntOutlineContour contour = contours[contourIndex];
            normalizedPointCount = CheckedFontMath.AddCounts(
                normalizedPointCount,
                contour.GetPointCount());
            impliedPointCount = CheckedFontMath.AddCounts(
                impliedPointCount,
                contour.GetImpliedPointCount());
            int commandIndex = 0;

            while (commandIndex < contour.GetCommandCount())
            {
                commands.Add(contour.GetCommand(commandIndex));
                commandIndex += 1;
            }

            contourIndex += 1;
        }

        return new SfntGlyphOutline(
            glyphId,
            contours,
            commands.ToArray(),
            normalizedPointCount,
            impliedPointCount);
    }

    static SfntGlyphOutline CreateEmptyOutline(int glyphId)
    {
        return new SfntGlyphOutline(
            glyphId,
            new SfntOutlineContour[0],
            new SfntPathCommand[0],
            0,
            0);
    }

    static void AddClosureGlyph(
        System.Collections.Generic.List<int> closure,
        bool[] closureMembers,
        int glyphId)
    {
        if (!closureMembers[glyphId])
        {
            closureMembers[glyphId] = true;
            closure.Add(glyphId);
        }
    }

    static double RoundToGrid(double value)
    {
        return System.Math.Round(
            value,
            System.MidpointRounding.AwayFromZero);
    }
}
