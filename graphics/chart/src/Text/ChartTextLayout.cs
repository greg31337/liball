namespace chart;
internal static class ChartTextLayoutOperations
{
    internal static string PrepareBuiltInText(string text, ChartTextDirection direction)
    {
        ChartTextBlock.ValidateText(text, "text");
        ValidateDirection(direction);
        string normalizedLineBreaks = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = ChartBitmapFont.SplitLines(normalizedLineBreaks);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            if (lineIndex > 0)
            {
                builder.Append('\n');
            }

            builder.Append(PrepareBuiltInLine(lines[lineIndex], direction));
            lineIndex = lineIndex + 1;
        }

        return builder.ToString();
    }

    internal static string PrepareBuiltInLine(string line, ChartTextDirection direction)
    {
        System.Collections.Generic.List<string> elements = TextElements(line);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (direction == ChartTextDirection.RightToLeft)
        {
            int index = elements.Count - 1;
            while (index >= 0)
            {
                builder.Append(NormalizeBuiltInElement(elements[index]));
                index = index - 1;
            }

            return builder.ToString();
        }

        int forward = 0;
        while (forward < elements.Count)
        {
            builder.Append(NormalizeBuiltInElement(elements[forward]));
            forward = forward + 1;
        }

        return builder.ToString();
    }

    internal static System.Collections.Generic.List<string> TextElements(string text)
    {
        System.Collections.Generic.List<string> elements = new System.Collections.Generic.List<string>();
        System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            elements.Add((string)enumerator.Current);
        }

        return elements;
    }

    internal static string NormalizeBuiltInElement(string element)
    {
        if (element.Length != 1)
        {
            return "?";
        }

        char value = element[0];
        if (value == '\t')
        {
            return " ";
        }

        char normalized = ChartBitmapFont.NormalizeGlyphChar(value);
        if (IsBuiltInCharacter(normalized))
        {
            return normalized.ToString();
        }

        return "?";
    }

    internal static bool IsBuiltInCharacter(char value)
    {
        if (value == ' ' || value == '.' || value == ',' || value == '-' || value == '+' || value == ':' || value == '%' || value == '/' || value == '(' || value == ')')
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        return value >= 'A' && value <= 'Z';
    }

    internal static string WrapBuiltInText(string preparedText, int maximumWidth, int scale)
    {
        ChartTextBlock.ValidateText(preparedText, "preparedText");
        ChartBitmapFont.ValidateScale(scale);
        if (maximumWidth <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumWidth");
        }

        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();
        int maximumCharacters = (maximumWidth + font.Spacing * scale) / font.GlyphAdvance(scale);
        if (maximumCharacters < 1)
        {
            maximumCharacters = 1;
        }

        string[] lines = ChartBitmapFont.SplitLines(preparedText);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < lines.Length)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder.Append(WrapBuiltInLine(lines[index], maximumCharacters));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string WrapBuiltInLine(string line, int maximumCharacters)
    {
        if (line.Length == 0)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int position = 0;
        bool first = true;
        while (position < line.Length)
        {
            while (position < line.Length && line[position] == ' ')
            {
                position = position + 1;
            }

            if (position >= line.Length)
            {
                break;
            }

            int remaining = line.Length - position;
            int take = maximumCharacters;
            if (take > remaining)
            {
                take = remaining;
            }

            if (position + take < line.Length)
            {
                int search = position + take - 1;
                while (search > position && line[search] != ' ')
                {
                    search = search - 1;
                }

                if (search > position)
                {
                    take = search - position;
                }
            }

            if (!first)
            {
                builder.Append('\n');
            }

            builder.Append(line.Substring(position, take));
            first = false;
            position = position + take;
        }

        return builder.ToString();
    }

    internal static ChartTextLayout AttachClip(ChartTextLayout layout, ChartRect clipBounds)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartAxis.ValidatePlotArea(clipBounds);
        ChartTextLayout copy = layout.Copy();
        copy.HasClip = true;
        copy.ClipBounds = ChartPlot.CopyRect(clipBounds);
        return copy;
    }

    internal static void ValidateDirection(ChartTextDirection direction)
    {
        int value = (int)direction;
        if (value < 0 || value > 1)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }
    }

    internal static void ValidateAlignment(ChartTextAlignment alignment, ChartVerticalTextAlignment verticalAlignment)
    {
        int horizontal = (int)alignment;
        int vertical = (int)verticalAlignment;
        if (horizontal < 0 || horizontal > 5)
        {
            throw new System.ArgumentOutOfRangeException("alignment");
        }

        if (vertical < 0 || vertical > 3)
        {
            throw new System.ArgumentOutOfRangeException("verticalAlignment");
        }
    }

    internal static ChartTextAlignment ResolveAlignment(ChartTextAlignment alignment, ChartTextDirection direction)
    {
        if (direction == ChartTextDirection.RightToLeft)
        {
            if (alignment == ChartTextAlignment.Start)
            {
                return ChartTextAlignment.Right;
            }

            if (alignment == ChartTextAlignment.End)
            {
                return ChartTextAlignment.Left;
            }
        }

        return alignment;
    }
}
