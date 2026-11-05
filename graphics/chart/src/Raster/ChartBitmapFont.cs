namespace chart;

internal class ChartBitmapGlyph
{
    internal int Width;
    internal int Height;
    internal string[] Rows;

    internal ChartBitmapGlyph(int width, int height, string[] rows)
    {
        if (rows == null)
        {
            throw new System.ArgumentNullException("rows");
        }

        Width = width;
        Height = height;
        Rows = rows;
    }

    internal bool IsPixelSet(int x, int y)
    {
        if (x < 0 || y < 0 || y >= Rows.Length || x >= Rows[y].Length)
        {
            return false;
        }

        return Rows[y][x] != '0' && Rows[y][x] != ' ';
    }
}

internal class ChartBitmapFont
{
    internal int GlyphWidth;
    internal int GlyphHeight;
    internal int Spacing;
    internal int Baseline;

    internal ChartBitmapFont(int glyphWidth, int glyphHeight, int spacing)
    {
        GlyphWidth = glyphWidth;
        GlyphHeight = glyphHeight;
        Spacing = spacing;
        Baseline = glyphHeight;
    }

    internal static ChartBitmapFont FiveBySeven()
    {
        return new ChartBitmapFont(5, 7, 1);
    }

    internal int GlyphAdvance(int scale)
    {
        ValidateScale(scale);
        return (GlyphWidth + Spacing) * scale;
    }

    internal int TextWidth(string text, int scale)
    {
        ValidateText(text);
        ValidateScale(scale);

        int width = 0;
        int lineWidth = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\n')
            {
                width = System.Math.Max(width, System.Math.Max(0, lineWidth - Spacing * scale));
                lineWidth = 0;
            }
            else
            {
                lineWidth = lineWidth + GlyphAdvance(scale);
            }

            index = index + 1;
        }

        width = System.Math.Max(width, System.Math.Max(0, lineWidth - Spacing * scale));
        return width;
    }

    internal int TextHeight(string text, int scale)
    {
        ValidateText(text);
        ValidateScale(scale);

        int lines = 1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\n')
            {
                lines = lines + 1;
            }

            index = index + 1;
        }

        return lines * GlyphHeight * scale + (lines - 1) * Spacing * scale;
    }

    internal ChartTextMeasurement MeasureText(string text, int scale)
    {
        ValidateText(text);
        ValidateScale(scale);
        int lineCount = CountLines(text);
        int lineHeight = GlyphHeight * scale;
        int height = TextHeight(text, scale);
        int width = TextWidth(text, scale);
        return new ChartTextMeasurement(width, height, lineCount, lineHeight, Baseline * scale);
    }

    internal ChartTextLayout CreateTextLayout(
        string text,
        int anchorX,
        int anchorY,
        int scale,
        ChartTextAlignment textAlignment,
        ChartVerticalTextAlignment verticalAlignment)
    {
        ValidateText(text);
        ValidateScale(scale);

        string[] lines = SplitLines(text);
        ChartTextMeasurement measurement = MeasureText(text, scale);
        int left = AlignedLeft(anchorX, measurement.Width, textAlignment);
        int top = AlignedTop(anchorY, measurement.Height, measurement.Baseline, verticalAlignment);
        ChartTextLayoutLine[] layoutLines = new ChartTextLayoutLine[lines.Length];
        int lineStep = (GlyphHeight + Spacing) * scale;

        int index = 0;
        while (index < lines.Length)
        {
            int lineWidth = TextWidth(lines[index], scale);
            int lineX = LineX(left, measurement.Width, lineWidth, textAlignment);
            int lineY = top + index * lineStep;
            layoutLines[index] = new ChartTextLayoutLine(lines[index], lineX, lineY, lineWidth, lineY + Baseline * scale);
            index = index + 1;
        }

        return new ChartTextLayout(layoutLines, left, top, measurement.Width, measurement.Height);
    }

    internal ChartTextRotationPlan PlanRotation(ChartTextLayout layout, double angleRadians, double pivotX, double pivotY)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartPoint[] corners = new ChartPoint[4];
        corners[0] = RotatePoint(layout.Bounds.Left(), layout.Bounds.Top(), pivotX, pivotY, angleRadians);
        corners[1] = RotatePoint(layout.Bounds.Right(), layout.Bounds.Top(), pivotX, pivotY, angleRadians);
        corners[2] = RotatePoint(layout.Bounds.Right(), layout.Bounds.Bottom(), pivotX, pivotY, angleRadians);
        corners[3] = RotatePoint(layout.Bounds.Left(), layout.Bounds.Bottom(), pivotX, pivotY, angleRadians);

        double left = corners[0].X;
        double top = corners[0].Y;
        double right = corners[0].X;
        double bottom = corners[0].Y;
        int index = 1;
        while (index < corners.Length)
        {
            left = System.Math.Min(left, corners[index].X);
            top = System.Math.Min(top, corners[index].Y);
            right = System.Math.Max(right, corners[index].X);
            bottom = System.Math.Max(bottom, corners[index].Y);
            index = index + 1;
        }

        ChartRect rotatedBounds = new ChartRect(left, top, right - left, bottom - top);
        return new ChartTextRotationPlan(angleRadians, layout.Bounds, rotatedBounds, corners);
    }

    internal ChartBitmapGlyph GetGlyph(char value)
    {
        char c = NormalizeGlyphChar(value);

        if (c == ' ')
        {
            return Glyph("00000", "00000", "00000", "00000", "00000", "00000", "00000");
        }

        if (c == '0')
        {
            return Glyph("01110", "10001", "10011", "10101", "11001", "10001", "01110");
        }

        if (c == '1')
        {
            return Glyph("00100", "01100", "00100", "00100", "00100", "00100", "01110");
        }

        if (c == '2')
        {
            return Glyph("01110", "10001", "00001", "00010", "00100", "01000", "11111");
        }

        if (c == '3')
        {
            return Glyph("11110", "00001", "00001", "01110", "00001", "00001", "11110");
        }

        if (c == '4')
        {
            return Glyph("00010", "00110", "01010", "10010", "11111", "00010", "00010");
        }

        if (c == '5')
        {
            return Glyph("11111", "10000", "10000", "11110", "00001", "00001", "11110");
        }

        if (c == '6')
        {
            return Glyph("01110", "10000", "10000", "11110", "10001", "10001", "01110");
        }

        if (c == '7')
        {
            return Glyph("11111", "00001", "00010", "00100", "01000", "01000", "01000");
        }

        if (c == '8')
        {
            return Glyph("01110", "10001", "10001", "01110", "10001", "10001", "01110");
        }

        if (c == '9')
        {
            return Glyph("01110", "10001", "10001", "01111", "00001", "00001", "01110");
        }

        if (c == 'A')
        {
            return Glyph("01110", "10001", "10001", "11111", "10001", "10001", "10001");
        }

        if (c == 'B')
        {
            return Glyph("11110", "10001", "10001", "11110", "10001", "10001", "11110");
        }

        if (c == 'C')
        {
            return Glyph("01111", "10000", "10000", "10000", "10000", "10000", "01111");
        }

        if (c == 'D')
        {
            return Glyph("11110", "10001", "10001", "10001", "10001", "10001", "11110");
        }

        if (c == 'E')
        {
            return Glyph("11111", "10000", "10000", "11110", "10000", "10000", "11111");
        }

        if (c == 'F')
        {
            return Glyph("11111", "10000", "10000", "11110", "10000", "10000", "10000");
        }

        if (c == 'G')
        {
            return Glyph("01111", "10000", "10000", "10011", "10001", "10001", "01111");
        }

        if (c == 'H')
        {
            return Glyph("10001", "10001", "10001", "11111", "10001", "10001", "10001");
        }

        if (c == 'I')
        {
            return Glyph("01110", "00100", "00100", "00100", "00100", "00100", "01110");
        }

        if (c == 'J')
        {
            return Glyph("00111", "00010", "00010", "00010", "00010", "10010", "01100");
        }

        if (c == 'K')
        {
            return Glyph("10001", "10010", "10100", "11000", "10100", "10010", "10001");
        }

        if (c == 'L')
        {
            return Glyph("10000", "10000", "10000", "10000", "10000", "10000", "11111");
        }

        if (c == 'M')
        {
            return Glyph("10001", "11011", "10101", "10101", "10001", "10001", "10001");
        }

        if (c == 'N')
        {
            return Glyph("10001", "11001", "10101", "10011", "10001", "10001", "10001");
        }

        if (c == 'O')
        {
            return Glyph("01110", "10001", "10001", "10001", "10001", "10001", "01110");
        }

        if (c == 'P')
        {
            return Glyph("11110", "10001", "10001", "11110", "10000", "10000", "10000");
        }

        if (c == 'Q')
        {
            return Glyph("01110", "10001", "10001", "10001", "10101", "10010", "01101");
        }

        if (c == 'R')
        {
            return Glyph("11110", "10001", "10001", "11110", "10100", "10010", "10001");
        }

        if (c == 'S')
        {
            return Glyph("01111", "10000", "10000", "01110", "00001", "00001", "11110");
        }

        if (c == 'T')
        {
            return Glyph("11111", "00100", "00100", "00100", "00100", "00100", "00100");
        }

        if (c == 'U')
        {
            return Glyph("10001", "10001", "10001", "10001", "10001", "10001", "01110");
        }

        if (c == 'V')
        {
            return Glyph("10001", "10001", "10001", "10001", "10001", "01010", "00100");
        }

        if (c == 'W')
        {
            return Glyph("10001", "10001", "10001", "10101", "10101", "10101", "01010");
        }

        if (c == 'X')
        {
            return Glyph("10001", "10001", "01010", "00100", "01010", "10001", "10001");
        }

        if (c == 'Y')
        {
            return Glyph("10001", "10001", "01010", "00100", "00100", "00100", "00100");
        }

        if (c == 'Z')
        {
            return Glyph("11111", "00001", "00010", "00100", "01000", "10000", "11111");
        }

        if (c == '.')
        {
            return Glyph("00000", "00000", "00000", "00000", "00000", "01100", "01100");
        }

        if (c == ',')
        {
            return Glyph("00000", "00000", "00000", "00000", "00100", "00100", "01000");
        }

        if (c == '-')
        {
            return Glyph("00000", "00000", "00000", "11111", "00000", "00000", "00000");
        }

        if (c == '+')
        {
            return Glyph("00000", "00100", "00100", "11111", "00100", "00100", "00000");
        }

        if (c == ':')
        {
            return Glyph("00000", "01100", "01100", "00000", "01100", "01100", "00000");
        }

        if (c == '%')
        {
            return Glyph("11001", "11010", "00100", "01000", "10110", "00110", "00000");
        }

        if (c == '/')
        {
            return Glyph("00001", "00010", "00010", "00100", "01000", "01000", "10000");
        }

        if (c == '(')
        {
            return Glyph("00010", "00100", "01000", "01000", "01000", "00100", "00010");
        }

        if (c == ')')
        {
            return Glyph("01000", "00100", "00010", "00010", "00010", "00100", "01000");
        }

        return Glyph("01110", "10001", "00010", "00100", "00100", "00000", "00100");
    }

    internal static ChartBitmapGlyph Glyph(string row0, string row1, string row2, string row3, string row4, string row5, string row6)
    {
        return new ChartBitmapGlyph(5, 7, new string[] { row0, row1, row2, row3, row4, row5, row6 });
    }

    internal static char NormalizeGlyphChar(char value)
    {
        if (value >= 'a' && value <= 'z')
        {
            return (char)(value - 'a' + 'A');
        }

        return value;
    }

    internal static void ValidateText(string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }
    }

    internal static void ValidateScale(int scale)
    {
        if (scale <= 0)
        {
            throw new System.ArgumentOutOfRangeException("scale");
        }
    }

    internal static int CountLines(string text)
    {
        ValidateText(text);
        int count = 1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\n')
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static string[] SplitLines(string text)
    {
        ValidateText(text);
        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
        int lineStart = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\n')
            {
                lines.Add(text.Substring(lineStart, index - lineStart));
                lineStart = index + 1;
            }

            index = index + 1;
        }

        lines.Add(text.Substring(lineStart, text.Length - lineStart));
        return lines.ToArray();
    }

    internal static int AlignedLeft(int anchorX, int width, ChartTextAlignment textAlignment)
    {
        if (textAlignment == ChartTextAlignment.Center || textAlignment == ChartTextAlignment.Middle)
        {
            return anchorX - width / 2;
        }

        if (textAlignment == ChartTextAlignment.Right || textAlignment == ChartTextAlignment.End)
        {
            return anchorX - width;
        }

        return anchorX;
    }

    internal static int LineX(int layoutLeft, int layoutWidth, int lineWidth, ChartTextAlignment textAlignment)
    {
        if (textAlignment == ChartTextAlignment.Center || textAlignment == ChartTextAlignment.Middle)
        {
            return layoutLeft + (layoutWidth - lineWidth) / 2;
        }

        if (textAlignment == ChartTextAlignment.Right || textAlignment == ChartTextAlignment.End)
        {
            return layoutLeft + layoutWidth - lineWidth;
        }

        return layoutLeft;
    }

    internal static int AlignedTop(int anchorY, int height, int baseline, ChartVerticalTextAlignment verticalAlignment)
    {
        if (verticalAlignment == ChartVerticalTextAlignment.Middle)
        {
            return anchorY - height / 2;
        }

        if (verticalAlignment == ChartVerticalTextAlignment.Baseline)
        {
            return anchorY - baseline;
        }

        if (verticalAlignment == ChartVerticalTextAlignment.Bottom)
        {
            return anchorY - height;
        }

        return anchorY;
    }

    internal static ChartPoint RotatePoint(double x, double y, double pivotX, double pivotY, double angleRadians)
    {
        double cosine = System.Math.Cos(angleRadians);
        double sine = System.Math.Sin(angleRadians);
        double dx = x - pivotX;
        double dy = y - pivotY;
        return new ChartPoint(pivotX + dx * cosine - dy * sine, pivotY + dx * sine + dy * cosine);
    }
}
