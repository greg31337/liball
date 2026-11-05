namespace barcode;

internal class HumanReadableTextRenderResult
{
    internal BarcodeRectangle[] PixelRectangles =
        new BarcodeRectangle[0];
    internal BarcodeRectangle[] GlyphRectangles =
        new BarcodeRectangle[0];
    internal BarcodeRectangle[] BarExtensionRectangles =
        new BarcodeRectangle[0];
    internal int GlyphCount;
    internal int GlyphRectangleCount;
    internal int BarExtensionRectangleCount;
    internal int TextRunCount;
}

internal static class HumanReadableTextRenderer
{
    internal static HumanReadableTextRenderResult Render(RenderPlan? plan)
    {
        if (plan == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required to draw human-readable text.");
        }

        ValidatePlan(plan);
        HumanReadableTextRenderResult result =
            new HumanReadableTextRenderResult();
        if (plan.TextLayout == null
            || (plan.TextLayout.Runs == null
                && plan.TextLayout.BarExtensions == null))
        {
            return result;
        }

        global::System.Collections.Generic.List<BarcodeRectangle> rectangles =
            new global::System.Collections.Generic.List<BarcodeRectangle>();
        int barExtensionCount = DrawBarExtensions(rectangles, plan);
        int glyphCount = 0;
        int textRunCount = 0;
        int index = 0;
        BarcodeTextRun[] runs = plan.TextLayout.Runs == null
            ? new BarcodeTextRun[0]
            : plan.TextLayout.Runs;
        while (index < runs.Length)
        {
            BarcodeTextRun run = runs[index];
            if (run != null && run.Text != null)
            {
                ValidateRun(run);
                string text = run.Text;
                int runGlyphCount = CountGlyphs(text);
                if (runGlyphCount != 0)
                {
                    DrawRun(
                        rectangles,
                        plan,
                        run,
                        text,
                        runGlyphCount);
                    glyphCount = AddCounts(glyphCount, runGlyphCount);
                }

                textRunCount = textRunCount + 1;
            }

            index = index + 1;
        }

        BarcodeRectangle[] unsorted = rectangles.ToArray();
        BarcodeRectangle[] extensions = CopyRectangleRange(
            unsorted,
            0,
            barExtensionCount);
        BarcodeRectangle[] glyphs = CopyRectangleRange(
            unsorted,
            barExtensionCount,
            unsorted.Length - barExtensionCount);
        BarcodeRectangle[] output = unsorted;
        SortRectangles(output);
        SortRectangles(extensions);
        SortRectangles(glyphs);
        result.PixelRectangles = output;
        result.GlyphRectangles = glyphs;
        result.BarExtensionRectangles = extensions;
        result.GlyphCount = glyphCount;
        result.GlyphRectangleCount = glyphs.Length;
        result.BarExtensionRectangleCount = barExtensionCount;
        result.TextRunCount = textRunCount;
        return result;
    }

    static BarcodeRectangle[] CopyRectangleRange(
        BarcodeRectangle[] source,
        int offset,
        int count)
    {
        BarcodeRectangle[] result = new BarcodeRectangle[count];
        int index = 0;
        while (index < count)
        {
            result[index] = source[offset + index];
            index = index + 1;
        }

        return result;
    }

    static int DrawBarExtensions(
        global::System.Collections.Generic.List<BarcodeRectangle> rectangles,
        RenderPlan plan)
    {
        if (plan.TextLayout == null
            || plan.TextLayout.BarExtensions == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < plan.TextLayout.BarExtensions.Length)
        {
            BarcodeRectangle source =
                plan.TextLayout.BarExtensions[index];
            ValidateBarExtension(source);
            int left = RoundCoordinate(source.X * plan.ModuleScale);
            int top = RoundCoordinate(source.Y * plan.ModuleScale);
            int right = RoundCoordinate(
                (source.X + source.Width) * plan.ModuleScale);
            int bottom = RoundCoordinate(
                (source.Y + source.Height) * plan.ModuleScale);
            if (left < 0
                || top < 0
                || right > plan.UnrotatedWidthPixels
                || bottom > plan.UnrotatedHeightPixels
                || right <= left
                || bottom <= top)
            {
                throw new BarcodeRenderingException(
                    "A bar extension is outside the render plan.");
            }

            rectangles.Add(CreateRotatedRectangle(
                left,
                top,
                right - left,
                bottom - top,
                plan));
            count = count + 1;
            index = index + 1;
        }

        return count;
    }

    static void DrawRun(
        global::System.Collections.Generic.List<BarcodeRectangle> rectangles,
        RenderPlan plan,
        BarcodeTextRun run,
        string text,
        int glyphCount)
    {
        int left = RoundCoordinate(run.X * plan.ModuleScale);
        int right = RoundCoordinate(
            (run.X + run.Width) * plan.ModuleScale);
        int top = RoundCoordinate(
            (run.BaselineY - run.Height) * plan.ModuleScale);
        int bottom = RoundCoordinate(
            run.BaselineY * plan.ModuleScale);
        if (left < 0
            || top < 0
            || right > plan.UnrotatedWidthPixels
            || bottom > plan.UnrotatedHeightPixels)
        {
            throw new BarcodeRenderingException(
                "Human-readable text extends outside the render plan.");
        }

        int width = right - left;
        int height = bottom - top;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        int logicalWidth = ResolveLogicalWidth(glyphCount);
        char[] characters = ResolveCharacters(text, glyphCount);
        int fontRow = 0;
        while (fontRow < BuiltInGlyphs.Height)
        {
            int y0 = top + (int)(
                ((long)fontRow * (long)height)
                / BuiltInGlyphs.Height);
            int y1 = top + (int)(
                ((long)(fontRow + 1) * (long)height)
                / BuiltInGlyphs.Height);
            int logicalColumn = 0;
            while (logicalColumn < logicalWidth)
            {
                while (logicalColumn < logicalWidth
                    && !IsDarkColumn(
                        characters,
                        logicalColumn,
                        fontRow))
                {
                    logicalColumn = logicalColumn + 1;
                }

                int start = logicalColumn;
                while (logicalColumn < logicalWidth
                    && IsDarkColumn(
                        characters,
                        logicalColumn,
                        fontRow))
                {
                    logicalColumn = logicalColumn + 1;
                }

                if (start < logicalColumn && y1 > y0)
                {
                    int x0 = left + (int)(
                        ((long)start * (long)width)
                        / logicalWidth);
                    int x1 = left + (int)(
                        ((long)logicalColumn * (long)width)
                        / logicalWidth);
                    if (x1 > x0)
                    {
                        BarcodeRectangle rectangle =
                            CreateRotatedRectangle(
                                x0,
                                y0,
                                x1 - x0,
                                y1 - y0,
                                plan);
                        rectangles.Add(rectangle);
                    }
                }
            }

            fontRow = fontRow + 1;
        }
    }

    static int CountGlyphs(string text)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsHighSurrogate(text[index])
                && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]))
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }

            count = AddCounts(count, 1);
        }

        return count;
    }

    static char[] ResolveCharacters(string text, int glyphCount)
    {
        char[] characters = new char[glyphCount];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < text.Length)
        {
            char character = text[sourceIndex];
            if (char.IsHighSurrogate(character)
                && sourceIndex + 1 < text.Length
                && char.IsLowSurrogate(text[sourceIndex + 1]))
            {
                characters[targetIndex] = '?';
                sourceIndex = sourceIndex + 2;
            }
            else
            {
                characters[targetIndex] = character;
                sourceIndex = sourceIndex + 1;
            }

            targetIndex = targetIndex + 1;
        }

        return characters;
    }

    static bool IsDarkColumn(
        char[] characters,
        int logicalColumn,
        int row)
    {
        int glyphIndex = logicalColumn / BuiltInGlyphs.Advance;
        int glyphColumn = logicalColumn % BuiltInGlyphs.Advance;
        if (glyphColumn >= BuiltInGlyphs.Width
            || glyphIndex >= characters.Length)
        {
            return false;
        }

        return BuiltInGlyphs.IsDark(
            characters[glyphIndex],
            glyphColumn,
            row);
    }

    static int ResolveLogicalWidth(int glyphCount)
    {
        long result = ((long)glyphCount * BuiltInGlyphs.Advance) - 1L;
        if (result <= 0L || result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "Human-readable text exceeds the supported glyph count.");
        }

        return (int)result;
    }

    static BarcodeRectangle CreateRotatedRectangle(
        int x,
        int y,
        int width,
        int height,
        RenderPlan plan)
    {
        BarcodeRectangle result = new BarcodeRectangle();
        switch (plan.Rotation)
        {
            case BarcodeRotation.None:
                result.X = x;
                result.Y = y;
                result.Width = width;
                result.Height = height;
                break;
            case BarcodeRotation.Clockwise90:
                result.X = plan.UnrotatedHeightPixels - y - height;
                result.Y = x;
                result.Width = height;
                result.Height = width;
                break;
            case BarcodeRotation.Clockwise180:
                result.X = plan.UnrotatedWidthPixels - x - width;
                result.Y = plan.UnrotatedHeightPixels - y - height;
                result.Width = width;
                result.Height = height;
                break;
            case BarcodeRotation.Clockwise270:
                result.X = y;
                result.Y = plan.UnrotatedWidthPixels - x - width;
                result.Width = height;
                result.Height = width;
                break;
            default:
                throw new BarcodeRenderingException(
                    "The human-readable text rotation is unsupported.");
        }

        return result;
    }

    static int RoundCoordinate(double value)
    {
        if (double.IsNaN(value)
            || double.IsInfinity(value)
            || value < 0.0
            || value > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "A human-readable text coordinate is unsupported.");
        }

        return (int)global::System.Math.Floor(value + 0.5);
    }

    static int AddCounts(int first, int second)
    {
        long result = (long)first + (long)second;
        if (result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "Human-readable text exceeds the supported glyph count.");
        }

        return (int)result;
    }

    static void SortRectangles(BarcodeRectangle[] rectangles)
    {
        int index = 1;
        while (index < rectangles.Length)
        {
            BarcodeRectangle current = rectangles[index];
            int previous = index - 1;
            while (previous >= 0
                && CompareRectangles(rectangles[previous], current) > 0)
            {
                rectangles[previous + 1] = rectangles[previous];
                previous = previous - 1;
            }

            rectangles[previous + 1] = current;
            index = index + 1;
        }
    }

    static int CompareRectangles(
        BarcodeRectangle first,
        BarcodeRectangle second)
    {
        if (first.Y != second.Y)
        {
            return first.Y < second.Y ? -1 : 1;
        }

        if (first.X != second.X)
        {
            return first.X < second.X ? -1 : 1;
        }

        if (first.Height != second.Height)
        {
            return first.Height < second.Height ? -1 : 1;
        }

        if (first.Width != second.Width)
        {
            return first.Width < second.Width ? -1 : 1;
        }

        return 0;
    }

    static void ValidatePlan(RenderPlan plan)
    {
        if (plan.Size == null
            || plan.Size.WidthPixels <= 0
            || plan.Size.HeightPixels <= 0
            || plan.UnrotatedWidthPixels <= 0
            || plan.UnrotatedHeightPixels <= 0
            || plan.ModuleScale <= 0)
        {
            throw new BarcodeRenderingException(
                "The render plan contains invalid text dimensions.");
        }
    }

    static void ValidateRun(BarcodeTextRun run)
    {
        if (double.IsNaN(run.X)
            || double.IsInfinity(run.X)
            || double.IsNaN(run.BaselineY)
            || double.IsInfinity(run.BaselineY)
            || double.IsNaN(run.Width)
            || double.IsInfinity(run.Width)
            || double.IsNaN(run.Height)
            || double.IsInfinity(run.Height)
            || run.X < 0.0
            || run.Width < 0.0
            || run.Height <= 0.0
            || run.BaselineY < run.Height)
        {
            throw new BarcodeRenderingException(
                "A human-readable text run contains invalid geometry.");
        }
    }

    static void ValidateBarExtension(BarcodeRectangle rectangle)
    {
        if (rectangle == null
            || double.IsNaN(rectangle.X)
            || double.IsInfinity(rectangle.X)
            || double.IsNaN(rectangle.Y)
            || double.IsInfinity(rectangle.Y)
            || double.IsNaN(rectangle.Width)
            || double.IsInfinity(rectangle.Width)
            || double.IsNaN(rectangle.Height)
            || double.IsInfinity(rectangle.Height)
            || rectangle.X < 0.0
            || rectangle.Y < 0.0
            || rectangle.Width <= 0.0
            || rectangle.Height <= 0.0)
        {
            throw new BarcodeRenderingException(
                "A bar extension contains invalid geometry.");
        }
    }
}
