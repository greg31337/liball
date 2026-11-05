namespace ttf;

internal class TextInkBoundsBuilder
{
    bool empty;
    double xMinimum;
    double yMinimum;
    double xMaximum;
    double yMaximum;

    internal TextInkBoundsBuilder()
    {
        empty = true;
    }

    internal void Include(
        double xMinimum,
        double yMinimum,
        double xMaximum,
        double yMaximum)
    {
        if (empty)
        {
            this.xMinimum = xMinimum;
            this.yMinimum = yMinimum;
            this.xMaximum = xMaximum;
            this.yMaximum = yMaximum;
            empty = false;
            return;
        }

        if (xMinimum < this.xMinimum) { this.xMinimum = xMinimum; }
        if (yMinimum < this.yMinimum) { this.yMinimum = yMinimum; }
        if (xMaximum > this.xMaximum) { this.xMaximum = xMaximum; }
        if (yMaximum > this.yMaximum) { this.yMaximum = yMaximum; }
    }

    internal void IncludeBounds(TextInkBounds bounds, double x, double y)
    {
        if (!bounds.IsEmpty())
        {
            Include(
                x + bounds.GetXMinimum(),
                y + bounds.GetYMinimum(),
                x + bounds.GetXMaximum(),
                y + bounds.GetYMaximum());
        }
    }

    internal TextInkBounds Build()
    {
        return empty ? TextInkBounds.Create() : TextInkBounds.Create(
            xMinimum,
            yMinimum,
            xMaximum,
            yMaximum);
    }
}

internal static class TextMeasurer
{
    internal static TextMeasurement MeasureGlyph(
        FontInstance instance,
        int glyphId,
        double pointSize)
    {
        return MeasureGlyph(instance, glyphId, new TextStyle(pointSize));
    }

    internal static TextMeasurement MeasureGlyph(
        FontInstance instance,
        int glyphId,
        TextStyle style)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        int unitsPerEm = GetUnitsPerEm(instance);
        double pointSize = style.GetPointSize();
        ValidatePointSize(pointSize, unitsPerEm);
        GlyphMetrics metrics = instance.GetGlyphMetrics(glyphId);
        TextInkBoundsBuilder ink = new TextInkBoundsBuilder();
        IncludeGlyphInk(
            ink,
            instance.GetGlyphOutline(glyphId),
            0,
            style.GetBaselineShift(),
            unitsPerEm,
            style);
        double xAdvance = Scale(
            metrics.GetAdvanceWidth(),
            unitsPerEm,
            pointSize) * style.GetHorizontalScale();
        EnsureFinite(xAdvance);
        return new TextMeasurement(
            pointSize,
            xAdvance,
            0,
            ink.Build());
    }

    internal static TextMeasurement MeasureRun(
        ShapedRun run,
        FontInstance instance,
        double pointSize)
    {
        return MeasureRun(run, instance, new TextStyle(pointSize));
    }

    internal static TextMeasurement MeasureRun(
        ShapedRun run,
        FontInstance instance,
        TextStyle style)
    {
        if (run == null)
        {
            throw new System.ArgumentNullException("run");
        }

        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        int unitsPerEm = GetUnitsPerEm(instance);
        double pointSize = style.GetPointSize();
        ValidatePointSize(pointSize, unitsPerEm);
        TextInkBoundsBuilder ink = new TextInkBoundsBuilder();
        double penX = 0;
        double penY = 0;
        double horizontalScale = style.GetHorizontalScale();
        bool vertical = run.GetDirection() == ShapingDirection.TopToBottom ||
            run.GetDirection() == ShapingDirection.BottomToTop;
        int index = 0;
        while (index < run.GetGlyphCount())
        {
            ShapedGlyph glyph = run.GetGlyph(index);
            double originX = penX + (Scale(
                glyph.GetXOffset(),
                unitsPerEm,
                pointSize) * horizontalScale);
            double originY = penY + Scale(
                glyph.GetYOffset(),
                unitsPerEm,
                pointSize);
            if (vertical)
            {
                GlyphMetrics metrics = instance.GetGlyphMetrics(
                    glyph.GetGlyphId());
                originX -= Scale(
                    metrics.GetAdvanceWidth() / 2.0,
                    unitsPerEm,
                    pointSize) * horizontalScale;
                originY -= Scale(
                    metrics.GetVerticalOriginY(),
                    unitsPerEm,
                    pointSize);
                originX += style.GetBaselineShift();
            }
            else
            {
                originY += style.GetBaselineShift();
            }

            IncludeGlyphInk(
                ink,
                instance.GetGlyphOutline(glyph.GetGlyphId()),
                originX,
                originY,
                unitsPerEm,
                style);
            penX += Scale(
                glyph.GetXAdvance(),
                unitsPerEm,
                pointSize) * horizontalScale;
            penY += Scale(
                glyph.GetYAdvance(),
                unitsPerEm,
                pointSize);

            if (IsClusterEnd(run, index))
            {
                double spacing = style.GetCharacterSpacing();
                ShapedCluster cluster = run.GetCluster(
                    glyph.GetClusterIndex());
                if (IsWordSpace(cluster))
                {
                    spacing += style.GetWordSpacing();
                }

                if (vertical)
                {
                    if (run.GetDirection() == ShapingDirection.TopToBottom)
                    {
                        penY -= spacing;
                    }
                    else
                    {
                        penY += spacing;
                    }
                }
                else
                {
                    penX += spacing * horizontalScale;
                }
            }

            EnsureFinite(penX);
            EnsureFinite(penY);
            index += 1;
        }

        return new TextMeasurement(
            pointSize,
            penX,
            penY,
            ink.Build());
    }

    internal static TextMeasurement MeasureResult(
        TextShapingResult result,
        double pointSize)
    {
        return MeasureResult(result, new TextStyle(pointSize));
    }

    internal static TextMeasurement MeasureResult(
        TextShapingResult result,
        TextStyle style)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        double pointSize = style.GetPointSize();
        ValidatePointSize(pointSize, 1);
        TextInkBoundsBuilder ink = new TextInkBoundsBuilder();
        double penX = 0;
        double penY = 0;
        int index = 0;
        while (index < result.GetRunCount())
        {
            TextMeasurement run = result.GetRun(index).MeasureWithStyle(style);
            ink.IncludeBounds(run.GetInkBounds(), penX, penY);
            penX += run.GetXAdvance();
            penY += run.GetYAdvance();
            EnsureFinite(penX);
            EnsureFinite(penY);
            index += 1;
        }

        return new TextMeasurement(
            pointSize,
            penX,
            penY,
            ink.Build());
    }

    static void IncludeGlyphInk(
        TextInkBoundsBuilder ink,
        GlyphOutline outline,
        double originX,
        double originY,
        int unitsPerEm,
        TextStyle style)
    {
        SfntPathBounds bounds = GetTransformedBounds(
            outline,
            unitsPerEm,
            style);
        if (bounds.IsEmpty())
        {
            return;
        }

        ink.Include(
            originX + bounds.GetXMinimum(),
            originY + bounds.GetYMinimum(),
            originX + bounds.GetXMaximum(),
            originY + bounds.GetYMaximum());
    }

    static SfntPathBounds GetTransformedBounds(
        GlyphOutline outline,
        int unitsPerEm,
        TextStyle style)
    {
        int commandCount = outline.GetCommandCount();
        SfntPathCommand[] commands = new SfntPathCommand[commandCount];
        double pointSize = style.GetPointSize();
        double horizontalScale = style.GetHorizontalScale();
        double slant = style.GetSyntheticSlantFactor();
        int index = 0;
        while (index < commandCount)
        {
            GlyphPathCommand command = outline.GetCommand(index);
            GlyphPathCommandKind kind = command.GetKind();
            SfntPathCommandKind sfntKind = ConvertKind(kind);

            if (kind == GlyphPathCommandKind.Cubic)
            {
                commands[index] = SfntPathCommand.Create(
                    sfntKind,
                    TransformX(
                        command.GetControlX(),
                        command.GetControlY(),
                        unitsPerEm,
                        pointSize,
                        horizontalScale,
                        slant),
                    Scale(command.GetControlY(), unitsPerEm, pointSize),
                    TransformX(
                        command.GetSecondControlX(),
                        command.GetSecondControlY(),
                        unitsPerEm,
                        pointSize,
                        horizontalScale,
                        slant),
                    Scale(
                        command.GetSecondControlY(),
                        unitsPerEm,
                        pointSize),
                    TransformX(
                        command.GetEndX(),
                        command.GetEndY(),
                        unitsPerEm,
                        pointSize,
                        horizontalScale,
                        slant),
                    Scale(command.GetEndY(), unitsPerEm, pointSize));
            }
            else if (kind == GlyphPathCommandKind.Quadratic)
            {
                commands[index] = SfntPathCommand.Create(
                    sfntKind,
                    TransformX(
                        command.GetControlX(),
                        command.GetControlY(),
                        unitsPerEm,
                        pointSize,
                        horizontalScale,
                        slant),
                    Scale(command.GetControlY(), unitsPerEm, pointSize),
                    TransformX(
                        command.GetEndX(),
                        command.GetEndY(),
                        unitsPerEm,
                        pointSize,
                        horizontalScale,
                        slant),
                    Scale(command.GetEndY(), unitsPerEm, pointSize));
            }
            else if (kind == GlyphPathCommandKind.Close)
            {
                commands[index] = SfntPathCommand.Create(
                    sfntKind,
                    0,
                    0,
                    0,
                    0);
            }
            else
            {
                commands[index] = SfntPathCommand.Create(
                    sfntKind,
                    0,
                    0,
                    TransformX(
                        command.GetEndX(),
                        command.GetEndY(),
                        unitsPerEm,
                        pointSize,
                        horizontalScale,
                        slant),
                    Scale(command.GetEndY(), unitsPerEm, pointSize));
            }

            index += 1;
        }

        SfntGlyphOutline transformed = new SfntGlyphOutline(
            outline.GetGlyphId(),
            new SfntOutlineContour[0],
            commands,
            0,
            0);
        return transformed.GetBounds();
    }

    static SfntPathCommandKind ConvertKind(GlyphPathCommandKind kind)
    {
        if (kind == GlyphPathCommandKind.Move)
        {
            return SfntPathCommandKind.Move;
        }

        if (kind == GlyphPathCommandKind.Line)
        {
            return SfntPathCommandKind.Line;
        }

        if (kind == GlyphPathCommandKind.Quadratic)
        {
            return SfntPathCommandKind.Quadratic;
        }

        if (kind == GlyphPathCommandKind.Cubic)
        {
            return SfntPathCommandKind.Cubic;
        }

        return SfntPathCommandKind.Close;
    }

    static double TransformX(
        double x,
        double y,
        int unitsPerEm,
        double pointSize,
        double horizontalScale,
        double slant)
    {
        double value = Scale(x + (slant * y), unitsPerEm, pointSize) *
            horizontalScale;
        EnsureFinite(value);
        return value;
    }

    static bool IsClusterEnd(ShapedRun run, int glyphIndex)
    {
        if (glyphIndex + 1 >= run.GetGlyphCount())
        {
            return true;
        }

        return run.GetGlyph(glyphIndex).GetClusterIndex() !=
            run.GetGlyph(glyphIndex + 1).GetClusterIndex();
    }

    static bool IsWordSpace(ShapedCluster cluster)
    {
        int index = 0;
        while (index < cluster.GetUnicodeScalarCount())
        {
            int scalar = cluster.GetUnicodeScalar(index);
            if (scalar == 0x0020 ||
                scalar == 0x00A0 ||
                scalar == 0x1680 ||
                (scalar >= 0x2000 && scalar <= 0x200A) ||
                scalar == 0x202F ||
                scalar == 0x205F ||
                scalar == 0x3000)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static int GetUnitsPerEm(FontInstance instance)
    {
        SfntHeadTable? head = instance.GetFace().GetHeadTable();
        if (head == null)
        {
            throw FontFormatException.Create(
                "Scaled text measurement requires a supported head table.");
        }

        return head.GetUnitsPerEm();
    }

    static void ValidatePointSize(double pointSize, int unitsPerEm)
    {
        CheckedFontMath.ScaleDesignUnits(0, unitsPerEm, pointSize);
    }

    static double Scale(
        double value,
        int unitsPerEm,
        double pointSize)
    {
        ValidatePointSize(pointSize, unitsPerEm);
        double result = value * pointSize / unitsPerEm;
        if (!double.IsFinite(result))
        {
            throw FontFormatException.Create(
                "A scaled text measurement is outside the supported range.");
        }

        return result;
    }

    static void EnsureFinite(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A styled text measurement is outside the supported range.");
        }
    }
}
