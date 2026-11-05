namespace raw;

/// <summary>Describes one arbitrary repeating color filter array grid.</summary>
public class RawCfaPattern
{
    /// <summary>The standard layout identity or <see cref="RawCfaLayout.Arbitrary"/>.</summary>
    public RawCfaLayout Layout;
    /// <summary>The repeat width in sensor samples.</summary>
    public int RepeatWidth;
    /// <summary>The repeat height in sensor samples.</summary>
    public int RepeatHeight;
    /// <summary>Grid colors in row-major order.</summary>
    public RawCfaColor[] Colors;

    /// <summary>Creates a color filter array pattern.</summary>
    /// <param name="layout">The standard or arbitrary layout identity.</param>
    /// <param name="repeatWidth">The repeat width.</param>
    /// <param name="repeatHeight">The repeat height.</param>
    /// <param name="colors">Row-major grid colors.</param>
    public RawCfaPattern(RawCfaLayout layout, int repeatWidth, int repeatHeight, RawCfaColor[] colors)
    {
        Layout = layout;
        RepeatWidth = repeatWidth;
        RepeatHeight = repeatHeight;
        Colors = colors == null ? System.Array.Empty<RawCfaColor>() : colors;
    }

    /// <summary>Creates one standard Bayer pattern.</summary>
    /// <param name="layout">One of the four standard Bayer layouts.</param>
    /// <returns>The corresponding two-by-two pattern.</returns>
    /// <exception cref="RawFormatException">The layout is not a standard Bayer layout.</exception>
    internal static RawCfaPattern CreateBayer(RawCfaLayout layout)
    {
        if (layout == RawCfaLayout.Rggb)
        {
            return new RawCfaPattern(layout, 2, 2, new RawCfaColor[] { RawCfaColor.Red, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Blue });
        }
        if (layout == RawCfaLayout.Bggr)
        {
            return new RawCfaPattern(layout, 2, 2, new RawCfaColor[] { RawCfaColor.Blue, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Red });
        }
        if (layout == RawCfaLayout.Grbg)
        {
            return new RawCfaPattern(layout, 2, 2, new RawCfaColor[] { RawCfaColor.Green, RawCfaColor.Red, RawCfaColor.Blue, RawCfaColor.Green });
        }
        if (layout == RawCfaLayout.Gbrg)
        {
            return new RawCfaPattern(layout, 2, 2, new RawCfaColor[] { RawCfaColor.Green, RawCfaColor.Blue, RawCfaColor.Red, RawCfaColor.Green });
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A standard Bayer CFA layout is required.", -1);
    }

    /// <summary>Creates an arbitrary repeating color filter grid.</summary>
    /// <param name="repeatWidth">The repeat width.</param>
    /// <param name="repeatHeight">The repeat height.</param>
    /// <param name="colors">Row-major grid colors.</param>
    /// <returns>The arbitrary pattern.</returns>
    internal static RawCfaPattern CreateArbitrary(int repeatWidth, int repeatHeight, RawCfaColor[] colors)
    {
        return new RawCfaPattern(RawCfaLayout.Arbitrary, repeatWidth, repeatHeight, colors);
    }

    /// <summary>Returns the repeating color at one nonnegative sensor coordinate.</summary>
    /// <param name="x">The sensor column.</param>
    /// <param name="y">The sensor row.</param>
    /// <returns>The color at the coordinate.</returns>
    /// <exception cref="RawFormatException">The coordinate or pattern is invalid.</exception>
    internal RawCfaColor GetColor(int x, int y)
    {
        RawValidationReport validation = Validate();
        if (!validation.IsValid() || x < 0 || y < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A valid CFA pattern and nonnegative coordinate are required.", -1);
        }
        return GetColorUnchecked(x, y);
    }

    internal RawCfaColor GetColorUnchecked(int x, int y)
    {
        int gridX = x % RepeatWidth;
        int gridY = y % RepeatHeight;
        return Colors[gridY * RepeatWidth + gridX];
    }

    /// <summary>Validates dimensions, colors, and standard-layout identity.</summary>
    /// <returns>All CFA validation findings in deterministic order.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (!DefinedLayout(Layout) || Layout == RawCfaLayout.None)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Cfa.Layout", "A CFA pattern requires a standard Bayer or arbitrary layout.");
        }
        if (RepeatWidth <= 0 || RepeatHeight <= 0 || RepeatWidth > 64 || RepeatHeight > 64)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Cfa.Repeat", "CFA repeat dimensions must be between one and sixty-four.");
        }
        long expected = -1;
        if (RepeatWidth > 0 && RepeatHeight > 0 && RepeatWidth <= long.MaxValue / RepeatHeight)
        {
            expected = (long)RepeatWidth * RepeatHeight;
        }
        if (Colors == null || expected < 0 || Colors.Length != expected)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Cfa.Colors", "The CFA color count must match the repeat grid.");
        }
        if (Colors != null)
        {
            int index = 0;
            while (index < Colors.Length)
            {
                if (!DefinedColor(Colors[index]) || Colors[index] == RawCfaColor.Unknown)
                {
                    Add(messages, RawDiagnosticCode.InvalidDescriptor, "Cfa.Colors", "Every CFA grid position requires a defined color.");
                    break;
                }
                index++;
            }
        }
        if (Layout == RawCfaLayout.Rggb || Layout == RawCfaLayout.Bggr || Layout == RawCfaLayout.Grbg || Layout == RawCfaLayout.Gbrg)
        {
            RawCfaPattern expectedPattern = CreateBayer(Layout);
            if (RepeatWidth != 2 || RepeatHeight != 2 || !EqualColors(Colors, expectedPattern.Colors))
            {
                Add(messages, RawDiagnosticCode.InvalidDescriptor, "Cfa.Layout", "A named Bayer layout must use its canonical two-by-two grid.");
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static bool DefinedLayout(RawCfaLayout layout)
    {
        return layout == RawCfaLayout.None || layout == RawCfaLayout.Rggb || layout == RawCfaLayout.Bggr || layout == RawCfaLayout.Grbg || layout == RawCfaLayout.Gbrg || layout == RawCfaLayout.Arbitrary;
    }

    internal static bool DefinedColor(RawCfaColor color)
    {
        return color >= RawCfaColor.Unknown && color <= RawCfaColor.White;
    }

    internal static bool EqualColors(RawCfaColor[]? first, RawCfaColor[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static void Add(System.Collections.Generic.List<RawValidationMessage> messages, RawDiagnosticCode code, string context, string message)
    {
        messages.Add(new RawValidationMessage(RawValidationCategory.Semantic, code, RawSeverity.Error, RawFormat.GenericRaw, -1, context, message, false));
    }
}
