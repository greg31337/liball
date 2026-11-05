namespace ttf;

internal class SfntShapedRunMetricContext
{
    System.Func<int, int, bool, double>? contourPointResolver;
    SfntGposAdjustmentContext? baselineAdjustmentContext;
    SfntGposAdjustmentContext? caretAdjustmentContext;

    internal SfntShapedRunMetricContext(
        System.Func<int, int, bool, double>? contourPointResolver,
        SfntGposAdjustmentContext? baselineAdjustmentContext,
        SfntGposAdjustmentContext? caretAdjustmentContext)
    {
        this.contourPointResolver = contourPointResolver;
        this.baselineAdjustmentContext = baselineAdjustmentContext;
        this.caretAdjustmentContext = caretAdjustmentContext;
    }

    internal bool CanResolveContourPoint()
    {
        return contourPointResolver != null;
    }

    internal double ResolveContourPoint(
        int glyphId,
        int pointIndex,
        bool horizontalCoordinate)
    {
        if (contourPointResolver == null)
        {
            throw new System.InvalidOperationException(
                "No contour-point resolver is available.");
        }

        return contourPointResolver(
            glyphId,
            pointIndex,
            horizontalCoordinate);
    }

    internal double ResolveBaselineAdjustment(
        SfntDeviceOrVariationIndex? adjustment,
        bool horizontalCoordinate)
    {
        return ResolveAdjustment(
            adjustment,
            baselineAdjustmentContext,
            horizontalCoordinate,
            "A BASE adjustment requires an adjustment context.");
    }

    internal double ResolveCaretAdjustment(
        SfntDeviceOrVariationIndex? adjustment,
        bool horizontalCoordinate)
    {
        return ResolveAdjustment(
            adjustment,
            caretAdjustmentContext,
            horizontalCoordinate,
            "A GDEF caret adjustment requires an adjustment context.");
    }

    double ResolveAdjustment(
        SfntDeviceOrVariationIndex? adjustment,
        SfntGposAdjustmentContext? adjustmentContext,
        bool horizontalCoordinate,
        string missingContextMessage)
    {
        if (adjustment == null)
        {
            return 0;
        }

        if (adjustmentContext == null)
        {
            throw new System.InvalidOperationException(
                missingContextMessage);
        }

        return adjustmentContext.ResolveAdjustment(
            adjustment,
            horizontalCoordinate);
    }
}

internal class SfntShapedBaselineResult
{
    int scriptTag;
    int baselineTag;
    int coordinateFormat;
    double position;
    bool usedDefaultBaseline;
    bool usedDefaultScript;
    bool usedCoordinateFallback;

    internal SfntShapedBaselineResult(
        int scriptTag,
        int baselineTag,
        int coordinateFormat,
        double position,
        bool usedDefaultBaseline,
        bool usedDefaultScript,
        bool usedCoordinateFallback)
    {
        this.scriptTag = scriptTag;
        this.baselineTag = baselineTag;
        this.coordinateFormat = coordinateFormat;
        this.position = position;
        this.usedDefaultBaseline = usedDefaultBaseline;
        this.usedDefaultScript = usedDefaultScript;
        this.usedCoordinateFallback = usedCoordinateFallback;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal int GetBaselineTag()
    {
        return baselineTag;
    }

    internal int GetCoordinateFormat()
    {
        return coordinateFormat;
    }

    internal double GetPosition()
    {
        return position;
    }

    internal bool UsedDefaultBaseline()
    {
        return usedDefaultBaseline;
    }

    internal bool UsedDefaultScript()
    {
        return usedDefaultScript;
    }

    internal bool UsedCoordinateFallback()
    {
        return usedCoordinateFallback;
    }
}

internal class SfntShapedCaretResult
{
    int glyphIndex;
    int glyphId;
    int caretIndex;
    int caretFormat;
    int contourPointIndex;
    bool hasPosition;
    double position;

    internal SfntShapedCaretResult(
        int glyphIndex,
        int glyphId,
        int caretIndex,
        int caretFormat,
        int contourPointIndex,
        bool hasPosition,
        double position)
    {
        this.glyphIndex = glyphIndex;
        this.glyphId = glyphId;
        this.caretIndex = caretIndex;
        this.caretFormat = caretFormat;
        this.contourPointIndex = contourPointIndex;
        this.hasPosition = hasPosition;
        this.position = position;
    }

    internal int GetGlyphIndex()
    {
        return glyphIndex;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetCaretIndex()
    {
        return caretIndex;
    }

    internal int GetCaretFormat()
    {
        return caretFormat;
    }

    internal bool HasContourPointIndex()
    {
        return caretFormat == 2;
    }

    internal int GetContourPointIndex()
    {
        return contourPointIndex;
    }

    internal bool HasPosition()
    {
        return hasPosition;
    }

    internal double GetPosition()
    {
        if (!hasPosition)
        {
            throw new System.InvalidOperationException(
                "The caret position requires glyph contour points.");
        }

        return position;
    }
}

internal class SfntShapedRunMetrics
{
    bool horizontal;
    double advance;
    SfntShapedBaselineResult? baseline;
    SfntShapedCaretResult[] carets;

    internal SfntShapedRunMetrics(
        bool horizontal,
        double advance,
        SfntShapedBaselineResult? baseline,
        SfntShapedCaretResult[] carets)
    {
        this.horizontal = horizontal;
        this.advance = advance;
        this.baseline = baseline;
        this.carets = carets;
    }

    internal bool IsHorizontal()
    {
        return horizontal;
    }

    internal bool IsVertical()
    {
        return !horizontal;
    }

    internal double GetAdvance()
    {
        return advance;
    }

    internal SfntShapedBaselineResult? GetBaseline()
    {
        return baseline;
    }

    internal int GetCaretCount()
    {
        return carets.Length;
    }

    internal SfntShapedCaretResult GetCaret(int index)
    {
        if (index < 0 || index >= carets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return carets[index];
    }
}

internal static class SfntShapedRunMetricResolver
{
    const int DefaultScriptTag = 0x44464C54;

    internal static SfntShapedRunMetrics Resolve(
        SfntBaseTable baseTable,
        SfntGdefTable gdefTable,
        SfntGposPositionBuffer positionBuffer,
        int scriptTag,
        int baselineTag,
        bool horizontal,
        SfntShapedRunMetricContext context)
    {
        if (positionBuffer == null)
        {
            throw new System.ArgumentNullException("positionBuffer");
        }

        if (context == null)
        {
            context = new SfntShapedRunMetricContext(null, null, null);
        }

        SfntShapedBaselineResult? baseline = ResolveBaseline(
            baseTable,
            scriptTag,
            baselineTag,
            horizontal,
            context);
        System.Collections.Generic.List<SfntShapedCaretResult> carets =
            new System.Collections.Generic.List<SfntShapedCaretResult>();
        double penPosition = 0;
        int glyphIndex = 0;

        while (glyphIndex < positionBuffer.GetCount())
        {
            AddGlyphCarets(
                gdefTable,
                positionBuffer,
                glyphIndex,
                penPosition,
                horizontal,
                context,
                carets);
            penPosition += horizontal ?
                positionBuffer.GetXAdvance(glyphIndex) :
                positionBuffer.GetYAdvance(glyphIndex);
            glyphIndex += 1;
        }

        SfntShapedCaretResult[] results =
            new SfntShapedCaretResult[carets.Count];
        carets.CopyTo(results);
        return new SfntShapedRunMetrics(
            horizontal,
            penPosition,
            baseline,
            results);
    }

    static SfntShapedBaselineResult? ResolveBaseline(
        SfntBaseTable baseTable,
        int requestedScriptTag,
        int requestedBaselineTag,
        bool horizontal,
        SfntShapedRunMetricContext context)
    {
        SfntBaseAxis? axis = baseTable == null ? null :
            horizontal ? baseTable.GetHorizontalAxis() :
            baseTable.GetVerticalAxis();

        if (axis == null)
        {
            return null;
        }

        int scriptTag = requestedScriptTag;
        SfntBaseScript? script = axis.FindScript(scriptTag);
        bool usedDefaultScript = false;

        if (script == null && requestedScriptTag != DefaultScriptTag)
        {
            scriptTag = DefaultScriptTag;
            script = axis.FindScript(scriptTag);
            usedDefaultScript = script != null;
        }

        if (script == null || script.GetValues() == null)
        {
            return null;
        }

        int baselineTag = requestedBaselineTag;
        SfntBaseCoordinate? coordinate = requestedBaselineTag == 0 ?
            null : axis.FindBaseline(scriptTag, requestedBaselineTag);
        bool usedDefaultBaseline = coordinate == null;

        if (coordinate == null)
        {
            coordinate = axis.FindDefaultBaseline(scriptTag);
            baselineTag = axis.FindDefaultBaselineTag(scriptTag);
        }

        if (coordinate == null)
        {
            return null;
        }

        bool usedCoordinateFallback = false;
        double position;

        if (coordinate.HasContourPointReference() &&
            context.CanResolveContourPoint())
        {
            position = context.ResolveContourPoint(
                coordinate.GetReferenceGlyphId(),
                coordinate.GetContourPointIndex(),
                !horizontal);
        }
        else
        {
            position = coordinate.GetCoordinate();
            usedCoordinateFallback =
                coordinate.HasContourPointReference();
        }

        position += context.ResolveBaselineAdjustment(
            coordinate.GetAdjustment(),
            !horizontal);
        return new SfntShapedBaselineResult(
            scriptTag,
            baselineTag,
            coordinate.GetFormat(),
            position,
            usedDefaultBaseline,
            usedDefaultScript,
            usedCoordinateFallback);
    }

    static void AddGlyphCarets(
        SfntGdefTable gdefTable,
        SfntGposPositionBuffer positionBuffer,
        int glyphIndex,
        double penPosition,
        bool horizontal,
        SfntShapedRunMetricContext context,
        System.Collections.Generic.List<SfntShapedCaretResult> results)
    {
        SfntGdefLigatureCaretList? caretList = gdefTable == null ? null :
            gdefTable.GetLigatureCaretList();

        if (caretList == null)
        {
            return;
        }

        int glyphId = positionBuffer.GetGlyphId(glyphIndex);
        SfntGdefLigatureGlyph? ligature =
            caretList.FindLigatureGlyph(glyphId);

        if (ligature == null)
        {
            return;
        }

        double placement = horizontal ?
            positionBuffer.GetXPlacement(glyphIndex) :
            positionBuffer.GetYPlacement(glyphIndex);
        int caretIndex = 0;

        while (caretIndex < ligature.GetCaretCount())
        {
            SfntGdefCaretValue caret =
                ligature.GetCaretValue(caretIndex);
            bool hasPosition = true;
            double coordinate = 0;

            if (caret.HasContourPointIndex())
            {
                if (context.CanResolveContourPoint())
                {
                    coordinate = context.ResolveContourPoint(
                        glyphId,
                        caret.GetContourPointIndex(),
                        horizontal);
                }
                else
                {
                    hasPosition = false;
                }
            }
            else
            {
                coordinate = caret.GetCoordinate();
                coordinate += context.ResolveCaretAdjustment(
                    caret.GetAdjustment(),
                    horizontal);
            }

            results.Add(new SfntShapedCaretResult(
                glyphIndex,
                glyphId,
                caretIndex,
                caret.GetFormat(),
                caret.GetContourPointIndex(),
                hasPosition,
                hasPosition ? penPosition + placement + coordinate : 0));
            caretIndex += 1;
        }
    }
}
