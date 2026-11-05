namespace ttf;

internal class SfntGvarPhantomPoints
{
    double leftX;
    double rightX;
    double topY;
    double bottomY;

    internal SfntGvarPhantomPoints(
        double leftX,
        double rightX,
        double topY,
        double bottomY)
    {
        this.leftX = leftX;
        this.rightX = rightX;
        this.topY = topY;
        this.bottomY = bottomY;
    }

    internal double GetLeftX()
    {
        return leftX;
    }

    internal double GetRightX()
    {
        return rightX;
    }

    internal double GetTopY()
    {
        return topY;
    }

    internal double GetBottomY()
    {
        return bottomY;
    }

    internal SfntGvarPhantomPoints Apply(
        SfntGvarDeltaSet deltas,
        int firstPhantomPoint)
    {
        return new SfntGvarPhantomPoints(
            leftX + deltas.GetXDelta(firstPhantomPoint),
            rightX + deltas.GetXDelta(firstPhantomPoint + 1),
            topY + deltas.GetYDelta(firstPhantomPoint + 2),
            bottomY + deltas.GetYDelta(firstPhantomPoint + 3));
    }
}

internal class SfntVariedGlyph
{
    SfntResolvedGlyph resolvedGlyph;
    double[] effectiveComponentXOffsets;
    double[] effectiveComponentYOffsets;
    double advanceWidth;
    double leftSideBearing;
    double advanceHeight;
    double topSideBearing;
    double verticalOriginY;
    bool synthesizedVerticalAdvance;
    bool usesVorgOrigin;

    internal SfntVariedGlyph(
        SfntResolvedGlyph resolvedGlyph,
        double[] effectiveComponentXOffsets,
        double[] effectiveComponentYOffsets,
        double advanceWidth,
        double leftSideBearing,
        double advanceHeight,
        double topSideBearing,
        double verticalOriginY,
        bool synthesizedVerticalAdvance,
        bool usesVorgOrigin)
    {
        this.resolvedGlyph = resolvedGlyph;
        this.effectiveComponentXOffsets = effectiveComponentXOffsets;
        this.effectiveComponentYOffsets = effectiveComponentYOffsets;
        this.advanceWidth = advanceWidth;
        this.leftSideBearing = leftSideBearing;
        this.advanceHeight = advanceHeight;
        this.topSideBearing = topSideBearing;
        this.verticalOriginY = verticalOriginY;
        this.synthesizedVerticalAdvance = synthesizedVerticalAdvance;
        this.usesVorgOrigin = usesVorgOrigin;
    }

    internal SfntResolvedGlyph GetResolvedGlyph()
    {
        return resolvedGlyph;
    }

    internal double GetEffectiveComponentXOffset(int componentIndex)
    {
        ValidateComponentIndex(componentIndex);
        return effectiveComponentXOffsets[componentIndex];
    }

    internal double GetEffectiveComponentYOffset(int componentIndex)
    {
        ValidateComponentIndex(componentIndex);
        return effectiveComponentYOffsets[componentIndex];
    }

    internal double GetAdvanceWidth()
    {
        return advanceWidth;
    }

    internal double GetLeftSideBearing()
    {
        return leftSideBearing;
    }

    internal double GetAdvanceHeight()
    {
        return advanceHeight;
    }

    internal double GetTopSideBearing()
    {
        return topSideBearing;
    }

    internal double GetVerticalOriginY()
    {
        return verticalOriginY;
    }

    internal bool UsesSynthesizedVerticalAdvance()
    {
        return synthesizedVerticalAdvance;
    }

    internal bool UsesVorgOrigin()
    {
        return usesVorgOrigin;
    }

    void ValidateComponentIndex(int componentIndex)
    {
        if (componentIndex < 0 ||
            componentIndex >= effectiveComponentXOffsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }
    }
}

internal class SfntGvarNetDeltas
{
    double[] xDeltas;
    double[] yDeltas;

    internal SfntGvarNetDeltas(int pointCount)
    {
        xDeltas = new double[pointCount];
        yDeltas = new double[pointCount];
    }

    internal int GetPointCount()
    {
        return xDeltas.Length;
    }

    internal double GetXDelta(int pointIndex)
    {
        return xDeltas[pointIndex];
    }

    internal double GetYDelta(int pointIndex)
    {
        return yDeltas[pointIndex];
    }

    internal void Add(SfntGvarDeltaSet deltas, double scalar)
    {
        int pointIndex = 0;

        while (pointIndex < xDeltas.Length)
        {
            xDeltas[pointIndex] += deltas.GetXDelta(pointIndex) * scalar;
            yDeltas[pointIndex] += deltas.GetYDelta(pointIndex) * scalar;
            pointIndex += 1;
        }
    }

    internal SfntGvarDeltaSet CreateDeltaSet()
    {
        bool[] explicitPoints = new bool[xDeltas.Length];
        bool[] inferredPoints = new bool[xDeltas.Length];
        return new SfntGvarDeltaSet(
            xDeltas,
            yDeltas,
            explicitPoints,
            inferredPoints);
    }
}

internal static class SfntGvarGlyphResolver
{
    internal static SfntVariedGlyph Resolve(
        FontFace face,
        int glyphId,
        double[] normalizedCoordinates)
    {
        SfntMaxpTable? maxp = face == null ? null : face.GetMaxpTable();
        int glyphCount = maxp == null ? 0 : maxp.GetGlyphCount();
        return Resolve(
            face,
            glyphId,
            normalizedCoordinates,
            new SfntVariedGlyph[glyphCount]);
    }

    internal static SfntVariedGlyph Resolve(
        FontFace? face,
        int glyphId,
        double[] normalizedCoordinates,
        SfntVariedGlyph[] resolvedGlyphs)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }

        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        if (resolvedGlyphs == null)
        {
            throw new System.ArgumentNullException("resolvedGlyphs");
        }

        SfntGvarTable? gvar = face.GetGvarTable();

        if (gvar == null)
        {
            throw FontFormatException.Create(
                "Varied TrueType glyphs require a supported gvar table.");
        }

        if (normalizedCoordinates.Length != gvar.GetAxisCount())
        {
            throw FontFormatException.Create(
                "The gvar axes do not match the resolved coordinates.");
        }

        SfntLocaTable? loca = face.GetLocaTable();
        SfntMaxpTable? maxp = face.GetMaxpTable();
        SfntHmtxTable? horizontal = face.GetHmtxTable();

        if (loca == null || maxp == null || horizontal == null)
        {
            throw FontFormatException.Create(
                "Varied TrueType glyphs require loca, maxp, hhea, and hmtx tables.");
        }

        if (glyphId < 0 || glyphId >= loca.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        if (resolvedGlyphs.Length != loca.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The varied-glyph cache does not match the font glyph count.",
                "resolvedGlyphs");
        }

        SfntVerticalMetrics vertical = face.GetVerticalMetrics();
        FontLimits limits = face.GetTableCache().GetLimits();
        bool[] activeGlyphs = new bool[loca.GetGlyphCount()];
        return ResolveGlyph(
            loca,
            glyphId,
            maxp,
            limits,
            gvar,
            normalizedCoordinates,
            horizontal,
            vertical,
            activeGlyphs,
            resolvedGlyphs,
            1);
    }

    static SfntVariedGlyph ResolveGlyph(
        SfntLocaTable loca,
        int glyphId,
        SfntMaxpTable maxp,
        FontLimits limits,
        SfntGvarTable gvar,
        double[] coordinates,
        SfntHmtxTable horizontal,
        SfntVerticalMetrics vertical,
        bool[] activeGlyphs,
        SfntVariedGlyph[] resolvedGlyphs,
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
        SfntGlyfRecord record = loca.GetGlyphRecord(glyphId);
        SfntGlyphHeader? header = SfntGlyphHeaderParser.Parse(record);
        SfntGvarPhantomPoints basePhantoms = CreateBasePhantoms(
            glyphId,
            header,
            horizontal,
            vertical);
        SfntVariedGlyph result;

        if (record.IsEmpty())
        {
            result = ResolveEmptyGlyph(
                glyphId,
                gvar,
                coordinates,
                basePhantoms,
                vertical);
        }
        else
        {
            SfntSimpleGlyph? simple = loca.GetSimpleGlyph(glyphId);

            if (simple != null)
            {
                result = ResolveSimpleGlyph(
                    simple,
                    gvar,
                    coordinates,
                    basePhantoms,
                    vertical);
            }
            else
            {
                SfntCompositeGlyph? composite = loca.GetCompositeGlyph(glyphId);

                if (composite == null)
                {
                    throw FontFormatException.Create(
                        "A nonempty glyf record has no recognized outline form.");
                }

                ValidateNestingLevel(nestingLevel, maxp, limits);
                result = ResolveCompositeGlyph(
                    loca,
                    composite,
                    maxp,
                    limits,
                    gvar,
                    coordinates,
                    horizontal,
                    vertical,
                    activeGlyphs,
                    resolvedGlyphs,
                    nestingLevel,
                    basePhantoms);
            }
        }

        activeGlyphs[glyphId] = false;
        resolvedGlyphs[glyphId] = result;
        return result;
    }

    static SfntVariedGlyph ResolveEmptyGlyph(
        int glyphId,
        SfntGvarTable gvar,
        double[] coordinates,
        SfntGvarPhantomPoints basePhantoms,
        SfntVerticalMetrics vertical)
    {
        SfntGvarNetDeltas deltas = CalculateNetDeltas(
            gvar,
            glyphId,
            4,
            null,
            coordinates);
        SfntGvarPhantomPoints phantoms = basePhantoms.Apply(
            deltas.CreateDeltaSet(),
            0);
        SfntResolvedGlyph resolved = new SfntResolvedGlyph(
            glyphId,
            CreateOutline(glyphId, new SfntOutlineContour[0]),
            new SfntResolvedPoint[0],
            new int[0],
            0,
            glyphId,
            false);
        return CreateResult(
            resolved,
            new double[0],
            new double[0],
            phantoms,
            vertical);
    }

    static SfntVariedGlyph ResolveSimpleGlyph(
        SfntSimpleGlyph glyph,
        SfntGvarTable gvar,
        double[] coordinates,
        SfntGvarPhantomPoints basePhantoms,
        SfntVerticalMetrics vertical)
    {
        int outlinePointCount = glyph.GetPointCount();
        int targetPointCount = CheckedFontMath.AddOffset(outlinePointCount, 4);
        SfntGvarNetDeltas deltas = CalculateNetDeltas(
            gvar,
            glyph.GetGlyphId(),
            targetPointCount,
            glyph,
            coordinates);
        double[] xCoordinates = new double[outlinePointCount];
        double[] yCoordinates = new double[outlinePointCount];
        SfntResolvedPoint[] resolvedPoints =
            new SfntResolvedPoint[outlinePointCount];
        int pointIndex = 0;

        while (pointIndex < outlinePointCount)
        {
            SfntSimpleGlyphPoint point = glyph.GetPoint(pointIndex);
            double x = point.GetX() + deltas.GetXDelta(pointIndex);
            double y = point.GetY() + deltas.GetYDelta(pointIndex);
            ValidateCoordinate(x, y);
            xCoordinates[pointIndex] = x;
            yCoordinates[pointIndex] = y;
            resolvedPoints[pointIndex] = new SfntResolvedPoint(
                x,
                y,
                point.IsOnCurve());
            pointIndex += 1;
        }

        SfntGlyphOutline outline = SfntGvarSimpleGlyphNormalizer.Normalize(
            glyph,
            xCoordinates,
            yCoordinates);
        SfntResolvedGlyph resolved = new SfntResolvedGlyph(
            glyph.GetGlyphId(),
            outline,
            resolvedPoints,
            new int[0],
            0,
            glyph.GetGlyphId(),
            false);
        SfntGvarPhantomPoints phantoms = basePhantoms.Apply(
            deltas.CreateDeltaSet(),
            outlinePointCount);
        return CreateResult(
            resolved,
            new double[0],
            new double[0],
            phantoms,
            vertical);
    }

    static SfntVariedGlyph ResolveCompositeGlyph(
        SfntLocaTable loca,
        SfntCompositeGlyph glyph,
        SfntMaxpTable maxp,
        FontLimits limits,
        SfntGvarTable gvar,
        double[] coordinates,
        SfntHmtxTable horizontal,
        SfntVerticalMetrics vertical,
        bool[] activeGlyphs,
        SfntVariedGlyph[] resolvedGlyphs,
        int nestingLevel,
        SfntGvarPhantomPoints basePhantoms)
    {
        int componentCount = glyph.GetComponentCount();
        int targetPointCount = CheckedFontMath.AddOffset(componentCount, 4);
        SfntGvarNetDeltas deltas = CalculateNetDeltas(
            gvar,
            glyph.GetGlyphId(),
            targetPointCount,
            null,
            coordinates);
        System.Collections.Generic.List<SfntResolvedPoint> points =
            new System.Collections.Generic.List<SfntResolvedPoint>();
        System.Collections.Generic.List<SfntOutlineContour> contours =
            new System.Collections.Generic.List<SfntOutlineContour>();
        System.Collections.Generic.List<int> closure =
            new System.Collections.Generic.List<int>();
        bool[] closureMembers = new bool[loca.GetGlyphCount()];
        double[] effectiveXOffsets = new double[componentCount];
        double[] effectiveYOffsets = new double[componentCount];
        int componentDepth = 1;
        int metricsGlyphId = glyph.GetGlyphId();
        SfntVariedGlyph? metricsSource = null;
        int componentIndex = 0;

        while (componentIndex < componentCount)
        {
            SfntCompositeComponent component = glyph.GetComponent(componentIndex);
            SfntVariedGlyph childResult = ResolveGlyph(
                loca,
                component.GetGlyphId(),
                maxp,
                limits,
                gvar,
                coordinates,
                horizontal,
                vertical,
                activeGlyphs,
                resolvedGlyphs,
                nestingLevel + 1);
            SfntResolvedGlyph child = childResult.GetResolvedGlyph();
            int childDepth = child.GetComponentDepth() + 1;

            if (childDepth > componentDepth)
            {
                componentDepth = childDepth;
            }

            AddClosureGlyph(
                closure,
                closureMembers,
                component.GetGlyphId());
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
                double rawX = component.GetXOffset() +
                    deltas.GetXDelta(componentIndex);
                double rawY = component.GetYOffset() +
                    deltas.GetYDelta(componentIndex);

                if (component.GetOffsetScaling() == SfntCompositeOffsetScaling.Scaled)
                {
                    offsetX = component.GetTransform().TransformX(rawX, rawY);
                    offsetY = component.GetTransform().TransformY(rawX, rawY);
                }
                else
                {
                    offsetX = rawX;
                    offsetY = rawY;
                }

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

            effectiveXOffsets[componentIndex] = offsetX;
            effectiveYOffsets[componentIndex] = offsetY;
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
                metricsSource = childResult;
            }

            componentIndex += 1;
        }

        maxp.VerifyCompositeGlyph(
            points.Count,
            contours.Count,
            glyph.GetInstructionLength(),
            componentCount,
            componentDepth,
            limits);
        SfntResolvedGlyph resolved = new SfntResolvedGlyph(
            glyph.GetGlyphId(),
            CreateOutline(glyph.GetGlyphId(), contours.ToArray()),
            points.ToArray(),
            closure.ToArray(),
            componentDepth,
            metricsGlyphId,
            true);

        if (metricsSource != null)
        {
            return new SfntVariedGlyph(
                resolved,
                effectiveXOffsets,
                effectiveYOffsets,
                metricsSource.GetAdvanceWidth(),
                metricsSource.GetLeftSideBearing(),
                metricsSource.GetAdvanceHeight(),
                metricsSource.GetTopSideBearing(),
                metricsSource.GetVerticalOriginY(),
                metricsSource.UsesSynthesizedVerticalAdvance(),
                metricsSource.UsesVorgOrigin());
        }

        SfntGvarPhantomPoints phantoms = basePhantoms.Apply(
            deltas.CreateDeltaSet(),
            componentCount);
        return CreateResult(
            resolved,
            effectiveXOffsets,
            effectiveYOffsets,
            phantoms,
            vertical);
    }

    static SfntGvarNetDeltas CalculateNetDeltas(
        SfntGvarTable gvar,
        int glyphId,
        int targetPointCount,
        SfntSimpleGlyph? simple,
        double[] coordinates)
    {
        SfntGvarNetDeltas result = new SfntGvarNetDeltas(targetPointCount);
        SfntGlyphVariationData? glyphData = gvar.GetGlyphVariationData(
            glyphId,
            targetPointCount);

        if (glyphData == null)
        {
            return result;
        }

        int variationIndex = 0;

        while (variationIndex < glyphData.GetTupleVariationCount())
        {
            SfntGvarTupleVariation variation =
                glyphData.GetTupleVariation(variationIndex);
            double scalar = variation.CalculateScalar(coordinates);

            if (scalar != 0)
            {
                SfntGvarDeltaSet deltas = simple == null ?
                    SfntGvarIupInterpolator.CreateSelectedPointDeltas(
                        variation,
                        targetPointCount) :
                    variation.CreateAllPointDeltas(simple);
                result.Add(deltas, scalar);
            }

            variationIndex += 1;
        }

        return result;
    }

    static SfntGvarPhantomPoints CreateBasePhantoms(
        int glyphId,
        SfntGlyphHeader? header,
        SfntHmtxTable horizontal,
        SfntVerticalMetrics vertical)
    {
        double xMinimum = header == null ? 0 : header.GetXMinimum();
        double yMaximum = header == null ? 0 : header.GetYMaximum();
        double left = xMinimum - horizontal.GetLeftSideBearing(glyphId);
        double right = left + horizontal.GetAdvanceWidth(glyphId);
        double top = vertical.GetVerticalOriginY(glyphId, (int)yMaximum);
        double bottom = top - vertical.GetAdvanceHeight(glyphId);
        return new SfntGvarPhantomPoints(left, right, top, bottom);
    }

    static SfntVariedGlyph CreateResult(
        SfntResolvedGlyph resolved,
        double[] effectiveXOffsets,
        double[] effectiveYOffsets,
        SfntGvarPhantomPoints phantoms,
        SfntVerticalMetrics vertical)
    {
        SfntPathBounds bounds = resolved.GetOutline().GetBounds();
        double xMinimum = bounds.IsEmpty() ? 0 : bounds.GetXMinimum();
        double yMaximum = bounds.IsEmpty() ? 0 : bounds.GetYMaximum();
        return new SfntVariedGlyph(
            resolved,
            effectiveXOffsets,
            effectiveYOffsets,
            phantoms.GetRightX() - phantoms.GetLeftX(),
            xMinimum - phantoms.GetLeftX(),
            phantoms.GetTopY() - phantoms.GetBottomY(),
            phantoms.GetTopY() - yMaximum,
            phantoms.GetTopY(),
            vertical.UsesSynthesizedAdvanceHeights(),
            vertical.UsesVorgOrigins());
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

        if (pointCount > limits.GetMaxPointsPerGlyph() ||
            contourCount > limits.GetMaxContoursPerGlyph())
        {
            throw FontLimitException.Create(
                "A resolved composite exceeds configured outline limits.");
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
            int sourcePointIndex = point.HasSourcePoint() ?
                point.GetSourcePointIndex() + sourcePointOffset : -1;
            points[pointIndex] = new SfntOutlinePoint(
                transform.TransformX(point.GetX(), point.GetY()) + offsetX,
                transform.TransformY(point.GetX(), point.GetY()) + offsetY,
                point.IsOnCurve(),
                point.IsImplied(),
                sourcePointIndex);
            pointIndex += 1;
        }

        SfntPathCommand[] commands = new SfntPathCommand[source.GetCommandCount()];
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
            return SfntPathCommand.Create(kind, controlX, controlY, endX, endY);
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
        return System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    static void ValidateCoordinate(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw FontFormatException.Create(
                "A varied glyph contains a non-finite coordinate.");
        }
    }
}

internal static class SfntGvarSimpleGlyphNormalizer
{
    internal static SfntGlyphOutline Normalize(
        SfntSimpleGlyph glyph,
        double[] xCoordinates,
        double[] yCoordinates)
    {
        SfntOutlineContour[] contours =
            new SfntOutlineContour[glyph.GetContourCount()];
        System.Collections.Generic.List<SfntPathCommand> commands =
            new System.Collections.Generic.List<SfntPathCommand>();
        int normalizedPointCount = 0;
        int impliedPointCount = 0;
        int contourIndex = 0;

        while (contourIndex < contours.Length)
        {
            SfntOutlineContour contour = NormalizeContour(
                glyph,
                contourIndex,
                xCoordinates,
                yCoordinates);
            contours[contourIndex] = contour;
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

        if (normalizedPointCount > glyph.GetMaximumNormalizedPointCount())
        {
            throw FontLimitException.Create(
                "A varied simple glyph exceeds its normalized point limit.");
        }

        return new SfntGlyphOutline(
            glyph.GetGlyphId(),
            contours,
            commands.ToArray(),
            normalizedPointCount,
            impliedPointCount);
    }

    static SfntOutlineContour NormalizeContour(
        SfntSimpleGlyph glyph,
        int contourIndex,
        double[] xCoordinates,
        double[] yCoordinates)
    {
        int start = glyph.GetContourStartPoint(contourIndex);
        int end = glyph.GetContourEndPoint(contourIndex);
        int firstSource = glyph.GetPoint(start).IsOnCurve() ?
            start : glyph.GetPoint(end).IsOnCurve() ? end : -1;
        System.Collections.Generic.List<SfntOutlinePoint> points =
            new System.Collections.Generic.List<SfntOutlinePoint>();

        if (firstSource >= 0)
        {
            AddSourcePoint(points, glyph, firstSource, xCoordinates, yCoordinates);
            int pointIndex = firstSource == end ? start : firstSource + 1;

            while (pointIndex != firstSource)
            {
                int previousIndex = pointIndex == start ? end : pointIndex - 1;

                if (!glyph.GetPoint(previousIndex).IsOnCurve() &&
                    !glyph.GetPoint(pointIndex).IsOnCurve())
                {
                    AddImpliedPoint(
                        points,
                        previousIndex,
                        pointIndex,
                        xCoordinates,
                        yCoordinates);
                }

                AddSourcePoint(
                    points,
                    glyph,
                    pointIndex,
                    xCoordinates,
                    yCoordinates);
                pointIndex = pointIndex == end ? start : pointIndex + 1;
            }
        }
        else
        {
            AddImpliedPoint(points, end, start, xCoordinates, yCoordinates);
            int pointIndex = start;

            while (pointIndex <= end)
            {
                int previousIndex = pointIndex == start ? end : pointIndex - 1;

                if (pointIndex != start)
                {
                    AddImpliedPoint(
                        points,
                        previousIndex,
                        pointIndex,
                        xCoordinates,
                        yCoordinates);
                }

                AddSourcePoint(
                    points,
                    glyph,
                    pointIndex,
                    xCoordinates,
                    yCoordinates);
                pointIndex += 1;
            }
        }

        SfntOutlinePoint[] normalizedPoints = points.ToArray();
        SfntPathCommand[] commands = CreateCommands(normalizedPoints);
        int impliedCount = 0;
        int index = 0;

        while (index < normalizedPoints.Length)
        {
            if (normalizedPoints[index].IsImplied())
            {
                impliedCount += 1;
            }

            index += 1;
        }

        return new SfntOutlineContour(
            normalizedPoints,
            commands,
            impliedCount);
    }

    static void AddSourcePoint(
        System.Collections.Generic.List<SfntOutlinePoint> points,
        SfntSimpleGlyph glyph,
        int pointIndex,
        double[] xCoordinates,
        double[] yCoordinates)
    {
        points.Add(new SfntOutlinePoint(
            xCoordinates[pointIndex],
            yCoordinates[pointIndex],
            glyph.GetPoint(pointIndex).IsOnCurve(),
            false,
            pointIndex));
    }

    static void AddImpliedPoint(
        System.Collections.Generic.List<SfntOutlinePoint> points,
        int firstIndex,
        int secondIndex,
        double[] xCoordinates,
        double[] yCoordinates)
    {
        points.Add(new SfntOutlinePoint(
            (xCoordinates[firstIndex] + xCoordinates[secondIndex]) / 2,
            (yCoordinates[firstIndex] + yCoordinates[secondIndex]) / 2,
            true,
            true,
            -1));
    }

    static SfntPathCommand[] CreateCommands(SfntOutlinePoint[] points)
    {
        System.Collections.Generic.List<SfntPathCommand> commands =
            new System.Collections.Generic.List<SfntPathCommand>();
        SfntOutlinePoint start = points[0];
        SfntOutlinePoint? control = null;
        commands.Add(SfntPathCommand.Create(
            SfntPathCommandKind.Move,
            0,
            0,
            start.GetX(),
            start.GetY()));
        int pointIndex = 1;

        while (pointIndex < points.Length)
        {
            SfntOutlinePoint point = points[pointIndex];

            if (point.IsOnCurve())
            {
                if (control == null)
                {
                    commands.Add(SfntPathCommand.Create(
                        SfntPathCommandKind.Line,
                        0,
                        0,
                        point.GetX(),
                        point.GetY()));
                }
                else
                {
                    commands.Add(SfntPathCommand.Create(
                        SfntPathCommandKind.Quadratic,
                        control.GetX(),
                        control.GetY(),
                        point.GetX(),
                        point.GetY()));
                    control = null;
                }
            }
            else
            {
                control = point;
            }

            pointIndex += 1;
        }

        if (control != null)
        {
            commands.Add(SfntPathCommand.Create(
                SfntPathCommandKind.Quadratic,
                control.GetX(),
                control.GetY(),
                start.GetX(),
                start.GetY()));
        }

        commands.Add(SfntPathCommand.Create(
            SfntPathCommandKind.Close,
            0,
            0,
            0,
            0));
        return commands.ToArray();
    }
}
