namespace barcode;

internal class SvgColor
{
    internal int Red;
    internal int Green;
    internal int Blue;
    internal int Alpha;
}

internal class SvgWriteResult
{
    internal string Text = "";
    internal int RectangleCount;
    internal int GlyphRectangleCount;
    internal int TextRunCount;
}

internal static class SvgWriter
{
    internal static SvgWriteResult Write(
        RenderPlan? plan,
        BarcodeRenderOptions? options)
    {
        if (plan == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required to write an SVG image.");
        }

        ValidatePlan(plan);
        BarcodeRenderOptions settings = options == null
            ? new BarcodeRenderOptions()
            : options;
        SvgColor foreground = ResolveForeground(settings);
        SvgColor background = ResolveBackground(settings);
        RenderRectangleSet rectangles = RectangleCoalescer.Coalesce(plan);
        global::System.Text.StringBuilder output =
            new global::System.Text.StringBuilder();
        AppendDocumentStart(output, plan);
        AppendMetadata(output, plan.Symbol);
        if (!settings.TransparentBackground && background.Alpha != 0)
        {
            AppendBackground(output, plan, background);
        }

        if (foreground.Alpha != 0
            && rectangles.PixelRectangles.Length != 0)
        {
            AppendRectanglePath(
                output,
                rectangles.PixelRectangles,
                plan.Size.WidthPixels,
                plan.Size.HeightPixels,
                foreground);
        }

        int textRunCount = 0;
        int glyphRectangleCount = 0;
        if (foreground.Alpha != 0)
        {
            HumanReadableTextRenderResult text =
                HumanReadableTextRenderer.Render(plan);
            textRunCount = text.TextRunCount;
            glyphRectangleCount = text.GlyphRectangleCount;
            BarcodeRectangle[] drawnRectangles = settings.UseSvgText
                ? text.BarExtensionRectangles
                : text.PixelRectangles;
            if (drawnRectangles.Length != 0)
            {
                AppendRectanglePath(
                    output,
                    drawnRectangles,
                    plan.Size.WidthPixels,
                    plan.Size.HeightPixels,
                    foreground);
            }

            if (settings.UseSvgText)
            {
                textRunCount = AppendTextRuns(output, plan, foreground);
            }
        }

        output.Append("</svg>\n");
        SvgWriteResult result = new SvgWriteResult();
        result.Text = output.ToString();
        result.RectangleCount = rectangles.PixelRectangles.Length;
        result.GlyphRectangleCount = glyphRectangleCount;
        result.TextRunCount = textRunCount;
        return result;
    }

    static SvgColor ResolveForeground(BarcodeRenderOptions options)
    {
        uint rgba = options.ForegroundRgba == 0u
            ? RasterCanvas.DefaultForegroundRgba
            : options.ForegroundRgba;
        return ReadColor(rgba);
    }

    static SvgColor ResolveBackground(BarcodeRenderOptions options)
    {
        uint rgba = options.BackgroundRgba == 0u
            ? RasterCanvas.DefaultBackgroundRgba
            : options.BackgroundRgba;
        SvgColor result = ReadColor(rgba);
        if (options.TransparentBackground)
        {
            result.Alpha = 0;
        }

        return result;
    }

    static SvgColor ReadColor(uint rgba)
    {
        SvgColor result = new SvgColor();
        result.Red = (int)((rgba >> 24) & 255u);
        result.Green = (int)((rgba >> 16) & 255u);
        result.Blue = (int)((rgba >> 8) & 255u);
        result.Alpha = (int)(rgba & 255u);
        return result;
    }

    static void AppendDocumentStart(
        global::System.Text.StringBuilder output,
        RenderPlan plan)
    {
        output.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        output.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
        output.Append(" width=\"");
        output.Append(FormatNumber(plan.Size.WidthPixels));
        output.Append("\" height=\"");
        output.Append(FormatNumber(plan.Size.HeightPixels));
        output.Append("\" viewBox=\"0 0 ");
        output.Append(FormatNumber(plan.Size.WidthPixels));
        output.Append(" ");
        output.Append(FormatNumber(plan.Size.HeightPixels));
        output.Append("\" shape-rendering=\"crispEdges\">\n");
    }

    static void AppendMetadata(
        global::System.Text.StringBuilder output,
        BarcodeSymbol symbol)
    {
        if (symbol.SymbologyIdentifier != null)
        {
            output.Append("<title>");
            AppendEscapedText(output, symbol.SymbologyIdentifier);
            output.Append("</title>\n");
        }

        if (symbol.Payload != null && symbol.Payload.Text != null)
        {
            output.Append("<desc>");
            AppendEscapedText(output, symbol.Payload.Text);
            output.Append("</desc>\n");
        }
    }

    static void AppendBackground(
        global::System.Text.StringBuilder output,
        RenderPlan plan,
        SvgColor color)
    {
        output.Append("<rect width=\"");
        output.Append(FormatNumber(plan.Size.WidthPixels));
        output.Append("\" height=\"");
        output.Append(FormatNumber(plan.Size.HeightPixels));
        output.Append("\"");
        AppendFill(output, color);
        output.Append("/>\n");
    }

    static void AppendRectanglePath(
        global::System.Text.StringBuilder output,
        BarcodeRectangle[] rectangles,
        int width,
        int height,
        SvgColor color)
    {
        output.Append("<path d=\"");
        int index = 0;
        while (index < rectangles.Length)
        {
            BarcodeRectangle rectangle = rectangles[index];
            ValidateRectangle(rectangle, width, height);
            int x = (int)rectangle.X;
            int y = (int)rectangle.Y;
            int rectangleWidth = (int)rectangle.Width;
            int rectangleHeight = (int)rectangle.Height;
            if (index != 0)
            {
                output.Append(" ");
            }

            output.Append("M");
            output.Append(FormatNumber(x));
            output.Append(" ");
            output.Append(FormatNumber(y));
            output.Append("h");
            output.Append(FormatNumber(rectangleWidth));
            output.Append("v");
            output.Append(FormatNumber(rectangleHeight));
            output.Append("h-");
            output.Append(FormatNumber(rectangleWidth));
            output.Append("Z");
            index = index + 1;
        }

        output.Append("\"");
        AppendFill(output, color);
        output.Append("/>\n");
    }

    static int AppendTextRuns(
        global::System.Text.StringBuilder output,
        RenderPlan plan,
        SvgColor color)
    {
        if (plan.TextLayout == null || plan.TextLayout.Runs == null)
        {
            return 0;
        }

        BarcodeTextRun[] runs = plan.TextLayout.Runs;
        int count = 0;
        int index = 0;
        while (index < runs.Length)
        {
            BarcodeTextRun run = runs[index];
            if (run != null && run.Text != null)
            {
                ValidateTextRun(run);
                AppendTextRun(output, plan, run, color);
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static void AppendTextRun(
        global::System.Text.StringBuilder output,
        RenderPlan plan,
        BarcodeTextRun run,
        SvgColor color)
    {
        string transform = ResolveTextTransform(plan);
        output.Append("<text x=\"");
        output.Append(FormatNumber(run.X * plan.ModuleScale));
        output.Append("\" y=\"");
        output.Append(FormatNumber(run.BaselineY * plan.ModuleScale));
        output.Append("\" font-family=\"monospace\" font-size=\"");
        output.Append(FormatNumber(run.Height * plan.ModuleScale));
        output.Append("\" textLength=\"");
        output.Append(FormatNumber(run.Width * plan.ModuleScale));
        output.Append("\" lengthAdjust=\"spacingAndGlyphs\"");
        if (transform.Length != 0)
        {
            output.Append(" transform=\"");
            output.Append(transform);
            output.Append("\"");
        }

        AppendFill(output, color);
        output.Append(">");
        AppendEscapedText(output, run.Text == null ? "" : run.Text);
        output.Append("</text>\n");
    }

    static string ResolveTextTransform(RenderPlan plan)
    {
        switch (plan.Rotation)
        {
            case BarcodeRotation.None:
                return "";
            case BarcodeRotation.Clockwise90:
                return "matrix(0 1 -1 0 "
                    + FormatNumber(plan.UnrotatedHeightPixels)
                    + " 0)";
            case BarcodeRotation.Clockwise180:
                return "matrix(-1 0 0 -1 "
                    + FormatNumber(plan.UnrotatedWidthPixels)
                    + " "
                    + FormatNumber(plan.UnrotatedHeightPixels)
                    + ")";
            case BarcodeRotation.Clockwise270:
                return "matrix(0 -1 1 0 0 "
                    + FormatNumber(plan.UnrotatedWidthPixels)
                    + ")";
            default:
                throw new BarcodeRenderingException(
                    "The SVG rotation is unsupported.");
        }
    }

    static void AppendFill(
        global::System.Text.StringBuilder output,
        SvgColor color)
    {
        output.Append(" fill=\"#");
        AppendHexByte(output, color.Red);
        AppendHexByte(output, color.Green);
        AppendHexByte(output, color.Blue);
        output.Append("\"");
        if (color.Alpha != 255)
        {
            output.Append(" fill-opacity=\"");
            output.Append(FormatNumber((double)color.Alpha / 255.0));
            output.Append("\"");
        }
    }

    static void AppendHexByte(
        global::System.Text.StringBuilder output,
        int value)
    {
        const string digits = "0123456789ABCDEF";
        output.Append(digits[(value >> 4) & 15]);
        output.Append(digits[value & 15]);
    }

    static string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new BarcodeRenderingException(
                "An SVG coordinate must be finite.");
        }

        return value.ToString(
            "0.######",
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    static void AppendEscapedText(
        global::System.Text.StringBuilder output,
        string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length
                    || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new BarcodeRenderingException(
                        "SVG text contains an invalid Unicode surrogate.");
                }

                output.Append(current);
                output.Append(value[index + 1]);
                index = index + 2;
                continue;
            }

            if (char.IsLowSurrogate(current)
                || (current < 32
                    && current != 9
                    && current != 10
                    && current != 13)
                || current == 65534
                || current == 65535)
            {
                throw new BarcodeRenderingException(
                    "SVG text contains a character that XML cannot represent.");
            }

            switch (current)
            {
                case '&':
                    output.Append("&amp;");
                    break;
                case '<':
                    output.Append("&lt;");
                    break;
                case '>':
                    output.Append("&gt;");
                    break;
                case '"':
                    output.Append("&quot;");
                    break;
                case '\'':
                    output.Append("&apos;");
                    break;
                default:
                    output.Append(current);
                    break;
            }

            index = index + 1;
        }
    }

    static void ValidatePlan(RenderPlan plan)
    {
        if (plan.Size == null
            || plan.Size.WidthPixels <= 0
            || plan.Size.HeightPixels <= 0)
        {
            throw new BarcodeRenderingException(
                "The render plan must contain positive SVG dimensions.");
        }
    }

    static void ValidateTextRun(BarcodeTextRun run)
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
            || run.BaselineY < 0.0
            || run.Width < 0.0
            || run.Height <= 0.0)
        {
            throw new BarcodeRenderingException(
                "An SVG text run contains invalid coordinates.");
        }
    }

    static void ValidateRectangle(
        BarcodeRectangle rectangle,
        int width,
        int height)
    {
        if (rectangle == null
            || rectangle.X != global::System.Math.Floor(rectangle.X)
            || rectangle.Y != global::System.Math.Floor(rectangle.Y)
            || rectangle.Width
                != global::System.Math.Floor(rectangle.Width)
            || rectangle.Height
                != global::System.Math.Floor(rectangle.Height)
            || rectangle.X < 0.0
            || rectangle.Y < 0.0
            || rectangle.Width <= 0.0
            || rectangle.Height <= 0.0
            || rectangle.X + rectangle.Width > width
            || rectangle.Y + rectangle.Height > height)
        {
            throw new BarcodeRenderingException(
                "A coalesced rectangle is outside the SVG canvas.");
        }
    }
}
