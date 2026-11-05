namespace svg;

/// <summary>Defines stable failures produced by typed-length parsing.</summary>
internal enum SvgLengthParseError
{
    /// <summary>No parse failure occurred.</summary>
    None = 0,

    /// <summary>A numeric length was not present.</summary>
    ExpectedLength = 1,

    /// <summary>The numeric component was malformed, too long, or non-finite.</summary>
    InvalidNumber = 2,

    /// <summary>The unit suffix was unknown.</summary>
    UnknownUnit = 3,

    /// <summary>A unitless value was not permitted.</summary>
    UnitRequired = 4,

    /// <summary>A percentage was not permitted.</summary>
    PercentageNotAllowed = 5,

    /// <summary>The complete dimension token exceeded its configured bound.</summary>
    TokenTooLong = 6,

    /// <summary>Non-whitespace characters followed the typed length.</summary>
    TrailingCharacters = 7,

    /// <summary>A parser argument was invalid.</summary>
    InvalidArgument = 8
}

/// <summary>Contains a non-throwing typed-length parse result.</summary>
internal class SvgLengthParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>The typed-length failure kind.</summary>
    public SvgLengthParseError Error;

    /// <summary>The numeric component failure kind.</summary>
    public SvgNumberParseError NumberError;

    /// <summary>The parsed typed length, or <c>null</c> after failure.</summary>
    public SvgLength? Length;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a typed-length parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The typed-length failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="length">The parsed length, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgLengthParseResult(bool success, SvgLengthParseError error, SvgNumberParseError numberError, SvgLength? length, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Length = length;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Parses finite typed SVG and CSS lengths and percentages.</summary>
internal static class SvgLengthParser
{
    /// <summary>Parses a complete SVG length using SVG number grammar.</summary>
    /// <param name="value">The length text.</param>
    /// <param name="allowPercentage">Whether a percentage suffix is permitted.</param>
    /// <param name="allowUnitless">Whether SVG-compatible unitless values are permitted.</param>
    /// <param name="maxTokenCharacters">The maximum complete dimension token length.</param>
    /// <returns>A typed-length parse result.</returns>
    public static SvgLengthParseResult ParseLength(string value, bool allowPercentage, bool allowUnitless, int maxTokenCharacters)
    {
        return ParseLengthCore(value, allowPercentage, allowUnitless, maxTokenCharacters, false);
    }

    /// <summary>Parses a complete CSS length using CSS number-token boundaries.</summary>
    /// <param name="value">The length text.</param>
    /// <param name="allowPercentage">Whether a percentage suffix is permitted.</param>
    /// <param name="allowUnitless">Whether unitless values are permitted.</param>
    /// <param name="maxTokenCharacters">The maximum complete dimension token length.</param>
    /// <returns>A typed-length parse result.</returns>
    public static SvgLengthParseResult ParseCssLength(string value, bool allowPercentage, bool allowUnitless, int maxTokenCharacters)
    {
        return ParseLengthCore(value, allowPercentage, allowUnitless, maxTokenCharacters, true);
    }

    static SvgLengthParseResult ParseLengthCore(string? value, bool allowPercentage, bool allowUnitless, int maxTokenCharacters, bool cssMode)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return Failure(SvgLengthParseError.InvalidArgument, SvgNumberParseError.None, 0);
        }

        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        if (cssMode)
        {
            reader.SkipCssWhitespace();
        }
        else
        {
            reader.SkipWhitespace();
        }
        int tokenStart = reader.GetPosition();
        SvgNumberParseResult number = cssMode ? SvgNumberParser.ReadCssNumber(reader) : SvgNumberParser.ReadNumber(reader);
        if (!cssMode && !number.Success && number.Error == SvgNumberParseError.InvalidExponent)
        {
            SvgNumberParseResult dimensionNumber = SvgNumberParser.ReadCssNumber(reader);
            if (dimensionNumber.Success && StartsAlphabeticUnit(reader.GetInput().Span, dimensionNumber.NextOffset))
            {
                number = dimensionNumber;
            }
        }
        if (!number.Success)
        {
            SvgLengthParseError error = number.Error == SvgNumberParseError.ExpectedNumber ? SvgLengthParseError.ExpectedLength : SvgLengthParseError.InvalidNumber;
            return Failure(error, number.Error, number.ErrorOffset);
        }

        System.ReadOnlySpan<char> input = reader.GetInput().Span;
        int unitStart = reader.GetPosition();
        int position = unitStart;
        if (position < input.Length && input[position] == '%')
        {
            position = position + 1;
        }
        else
        {
            while (position < input.Length && IsAsciiLetter(input[position]))
            {
                position = position + 1;
            }
        }
        if (position - tokenStart > maxTokenCharacters)
        {
            return Failure(SvgLengthParseError.TokenTooLong, SvgNumberParseError.None, tokenStart + maxTokenCharacters);
        }

        string unitText = input.Slice(unitStart, position - unitStart).ToString();
        SvgLengthUnit unit = SvgLengthUnit.Number;
        if (unitText.Length == 0)
        {
            if (!allowUnitless)
            {
                return Failure(SvgLengthParseError.UnitRequired, SvgNumberParseError.None, unitStart);
            }
        }
        else
        {
            int unitValue = SvgLengthUnits.Find(unitText);
            if (unitValue < 0)
            {
                return Failure(SvgLengthParseError.UnknownUnit, SvgNumberParseError.None, unitStart);
            }
            unit = (SvgLengthUnit)unitValue;
            if (unit == SvgLengthUnit.Percentage && !allowPercentage)
            {
                return Failure(SvgLengthParseError.PercentageNotAllowed, SvgNumberParseError.None, unitStart);
            }
        }

        reader.SetPosition(position);
        if (cssMode)
        {
            reader.SkipCssWhitespace();
        }
        else
        {
            reader.SkipWhitespace();
        }
        if (!reader.IsEnd())
        {
            return Failure(SvgLengthParseError.TrailingCharacters, SvgNumberParseError.None, reader.GetPosition());
        }
        return new SvgLengthParseResult(true, SvgLengthParseError.None, SvgNumberParseError.None, new SvgLength(number.Value, unit), -1);
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool StartsAlphabeticUnit(System.ReadOnlySpan<char> input, int position)
    {
        return position + 1 < input.Length && (input[position] == 'e' || input[position] == 'E') && IsAsciiLetter(input[position + 1]);
    }

    static SvgLengthParseResult Failure(SvgLengthParseError error, SvgNumberParseError numberError, int errorOffset)
    {
        return new SvgLengthParseResult(false, error, numberError, null, errorOffset);
    }
}

/// <summary>Evaluates typed lengths against explicit context inputs.</summary>
internal static class SvgLengthEvaluator
{
    /// <summary>Evaluates one typed length to finite user units.</summary>
    /// <param name="length">The typed length.</param>
    /// <param name="context">The optional evaluation context.</param>
    /// <param name="percentageBasis">The percentage and object-bounding-box basis.</param>
    /// <returns>A finite user-unit result or stable failure.</returns>
    public static SvgLengthEvaluationResult Evaluate(SvgLength? length, SvgLengthContext? context, SvgLengthPercentageBasis percentageBasis)
    {
        if (length == null || !SvgFiniteMath.IsFinite(length.Value) || !SvgLengthUnits.IsKnown(length.Unit))
        {
            return Failure(SvgLengthEvaluationError.InvalidArgument);
        }

        double scale = 1;
        switch (length.Unit)
        {
            case SvgLengthUnit.Number:
                if (IsObjectBoundingBoxBasis(percentageBasis))
                {
                    SvgLengthEvaluationResult objectScale = ResolveBasis(context, percentageBasis);
                    if (!objectScale.Success)
                    {
                        return objectScale;
                    }
                    scale = objectScale.Value;
                }
                break;
            case SvgLengthUnit.Percentage:
                SvgLengthEvaluationResult percentageScale = ResolveBasis(context, percentageBasis);
                if (!percentageScale.Success)
                {
                    return percentageScale;
                }
                scale = percentageScale.Value / 100;
                break;
            case SvgLengthUnit.Px:
                break;
            case SvgLengthUnit.Em:
                if (context == null || !context.HasFontSize)
                {
                    return Failure(SvgLengthEvaluationError.MissingFontSize);
                }
                if (!IsValidContextValue(context.FontSize))
                {
                    return Failure(SvgLengthEvaluationError.InvalidContext);
                }
                scale = context.FontSize;
                break;
            case SvgLengthUnit.Ex:
                if (context == null || !context.HasXHeight)
                {
                    return Failure(SvgLengthEvaluationError.MissingFontMetric);
                }
                if (!IsValidContextValue(context.XHeight))
                {
                    return Failure(SvgLengthEvaluationError.InvalidContext);
                }
                scale = context.XHeight;
                break;
            case SvgLengthUnit.Ch:
                if (context == null || !context.HasZeroAdvance)
                {
                    return Failure(SvgLengthEvaluationError.MissingFontMetric);
                }
                if (!IsValidContextValue(context.ZeroAdvance))
                {
                    return Failure(SvgLengthEvaluationError.InvalidContext);
                }
                scale = context.ZeroAdvance;
                break;
            case SvgLengthUnit.Rem:
                if (context == null || !context.HasRootFontSize)
                {
                    return Failure(SvgLengthEvaluationError.MissingRootFontSize);
                }
                if (!IsValidContextValue(context.RootFontSize))
                {
                    return Failure(SvgLengthEvaluationError.InvalidContext);
                }
                scale = context.RootFontSize;
                break;
            case SvgLengthUnit.Vw:
                SvgLengthEvaluationResult viewportWidth = ResolveViewport(context, true);
                if (!viewportWidth.Success)
                {
                    return viewportWidth;
                }
                scale = viewportWidth.Value / 100;
                break;
            case SvgLengthUnit.Vh:
                SvgLengthEvaluationResult viewportHeight = ResolveViewport(context, false);
                if (!viewportHeight.Success)
                {
                    return viewportHeight;
                }
                scale = viewportHeight.Value / 100;
                break;
            case SvgLengthUnit.Vmin:
                SvgLengthEvaluationResult minimumViewport = ResolveViewportMinimum(context);
                if (!minimumViewport.Success)
                {
                    return minimumViewport;
                }
                scale = minimumViewport.Value / 100;
                break;
            case SvgLengthUnit.Vmax:
                SvgLengthEvaluationResult maximumViewport = ResolveViewportMaximum(context);
                if (!maximumViewport.Success)
                {
                    return maximumViewport;
                }
                scale = maximumViewport.Value / 100;
                break;
            case SvgLengthUnit.Cm:
                scale = 96.0 / 2.54;
                break;
            case SvgLengthUnit.Mm:
                scale = 96.0 / 25.4;
                break;
            case SvgLengthUnit.Q:
                scale = 96.0 / 101.6;
                break;
            case SvgLengthUnit.In:
                scale = 96;
                break;
            case SvgLengthUnit.Pt:
                scale = 96.0 / 72.0;
                break;
            case SvgLengthUnit.Pc:
                scale = 16;
                break;
            default:
                return Failure(SvgLengthEvaluationError.UnsupportedUnitOrBasis);
        }

        double result = length.Value * scale;
        if (!SvgFiniteMath.IsFinite(result))
        {
            return Failure(SvgLengthEvaluationError.NonFiniteResult);
        }
        return new SvgLengthEvaluationResult(true, SvgLengthEvaluationError.None, SvgFiniteMath.NormalizeZero(result));
    }

    static SvgLengthEvaluationResult ResolveBasis(SvgLengthContext? context, SvgLengthPercentageBasis basis)
    {
        switch (basis)
        {
            case SvgLengthPercentageBasis.None:
                return Failure(SvgLengthEvaluationError.MissingPercentageBasis);
            case SvgLengthPercentageBasis.ViewportWidth:
                return ResolveViewport(context, true);
            case SvgLengthPercentageBasis.ViewportHeight:
                return ResolveViewport(context, false);
            case SvgLengthPercentageBasis.ViewportDiagonal:
                return ResolveViewportDiagonal((context ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            case SvgLengthPercentageBasis.FontSize:
                if (context == null || !context.HasFontSize)
                {
                    return Failure(SvgLengthEvaluationError.MissingFontSize);
                }
                if (!IsValidContextValue(context.FontSize))
                {
                    return Failure(SvgLengthEvaluationError.InvalidContext);
                }
                return Success(context.FontSize);
            case SvgLengthPercentageBasis.RootFontSize:
                if (context == null || !context.HasRootFontSize)
                {
                    return Failure(SvgLengthEvaluationError.MissingRootFontSize);
                }
                if (!IsValidContextValue(context.RootFontSize))
                {
                    return Failure(SvgLengthEvaluationError.InvalidContext);
                }
                return Success(context.RootFontSize);
            case SvgLengthPercentageBasis.ObjectBoundingBoxWidth:
                return ResolveObjectBoundingBox(context, true);
            case SvgLengthPercentageBasis.ObjectBoundingBoxHeight:
                return ResolveObjectBoundingBox(context, false);
            case SvgLengthPercentageBasis.ObjectBoundingBoxDiagonal:
                return ResolveObjectBoundingBoxDiagonal((context ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            default:
                return Failure(SvgLengthEvaluationError.UnsupportedUnitOrBasis);
        }
    }

    static SvgLengthEvaluationResult ResolveViewport(SvgLengthContext? context, bool width)
    {
        if (context == null || !context.HasViewport)
        {
            return Failure(SvgLengthEvaluationError.MissingViewport);
        }
        double value = width ? context.ViewportWidth : context.ViewportHeight;
        if (!IsValidContextValue(value))
        {
            return Failure(SvgLengthEvaluationError.InvalidContext);
        }
        return Success(value);
    }

    static SvgLengthEvaluationResult ResolveViewportMinimum(SvgLengthContext? context)
    {
        SvgLengthEvaluationResult width = ResolveViewport(context, true);
        if (!width.Success)
        {
            return width;
        }
        SvgLengthEvaluationResult height = ResolveViewport(context, false);
        if (!height.Success)
        {
            return height;
        }
        return Success(width.Value < height.Value ? width.Value : height.Value);
    }

    static SvgLengthEvaluationResult ResolveViewportMaximum(SvgLengthContext? context)
    {
        SvgLengthEvaluationResult width = ResolveViewport(context, true);
        if (!width.Success)
        {
            return width;
        }
        SvgLengthEvaluationResult height = ResolveViewport(context, false);
        if (!height.Success)
        {
            return height;
        }
        return Success(width.Value > height.Value ? width.Value : height.Value);
    }

    static SvgLengthEvaluationResult ResolveViewportDiagonal(SvgLengthContext context)
    {
        SvgLengthEvaluationResult width = ResolveViewport(context, true);
        if (!width.Success)
        {
            return width;
        }
        SvgLengthEvaluationResult height = ResolveViewport(context, false);
        if (!height.Success)
        {
            return height;
        }
        return ResolveNormalizedDiagonal(width.Value, height.Value);
    }

    static SvgLengthEvaluationResult ResolveObjectBoundingBox(SvgLengthContext? context, bool width)
    {
        if (context == null || !context.HasObjectBoundingBox)
        {
            return Failure(SvgLengthEvaluationError.MissingObjectBoundingBox);
        }
        double value = width ? context.ObjectBoundingBoxWidth : context.ObjectBoundingBoxHeight;
        if (!IsValidContextValue(value))
        {
            return Failure(SvgLengthEvaluationError.InvalidContext);
        }
        return Success(value);
    }

    static SvgLengthEvaluationResult ResolveObjectBoundingBoxDiagonal(SvgLengthContext context)
    {
        SvgLengthEvaluationResult width = ResolveObjectBoundingBox(context, true);
        if (!width.Success)
        {
            return width;
        }
        SvgLengthEvaluationResult height = ResolveObjectBoundingBox(context, false);
        if (!height.Success)
        {
            return height;
        }
        return ResolveNormalizedDiagonal(width.Value, height.Value);
    }

    static SvgLengthEvaluationResult ResolveNormalizedDiagonal(double width, double height)
    {
        double maximum = width > height ? width : height;
        double minimum = width > height ? height : width;
        if (maximum == 0)
        {
            return Success(0);
        }
        double ratio = minimum / maximum;
        double factor = System.Math.Sqrt(1 + ratio * ratio) / 1.4142135623730951;
        double value = maximum * factor;
        if (!SvgFiniteMath.IsFinite(value))
        {
            return Failure(SvgLengthEvaluationError.NonFiniteResult);
        }
        return Success(value);
    }

    static bool IsObjectBoundingBoxBasis(SvgLengthPercentageBasis basis)
    {
        return basis == SvgLengthPercentageBasis.ObjectBoundingBoxWidth || basis == SvgLengthPercentageBasis.ObjectBoundingBoxHeight || basis == SvgLengthPercentageBasis.ObjectBoundingBoxDiagonal;
    }

    static bool IsValidContextValue(double value)
    {
        return SvgFiniteMath.IsFinite(value) && value >= 0;
    }

    static SvgLengthEvaluationResult Success(double value)
    {
        return new SvgLengthEvaluationResult(true, SvgLengthEvaluationError.None, value);
    }

    static SvgLengthEvaluationResult Failure(SvgLengthEvaluationError error)
    {
        return new SvgLengthEvaluationResult(false, error, 0);
    }
}

/// <summary>Maps typed length units to and from stable CSS text.</summary>
internal static class SvgLengthUnits
{
    /// <summary>Tests whether a unit enum value is recognized.</summary>
    /// <param name="unit">The unit to test.</param>
    /// <returns><c>true</c> for a recognized unit.</returns>
    public static bool IsKnown(SvgLengthUnit unit)
    {
        return unit >= SvgLengthUnit.Number && unit <= SvgLengthUnit.Pc;
    }

    /// <summary>Returns the canonical suffix for a typed unit.</summary>
    /// <param name="unit">The typed unit.</param>
    /// <returns>The canonical unit suffix, or an empty string for unitless values.</returns>
    public static string GetText(SvgLengthUnit unit)
    {
        switch (unit)
        {
            case SvgLengthUnit.Number:
                return "";
            case SvgLengthUnit.Percentage:
                return "%";
            case SvgLengthUnit.Px:
                return "px";
            case SvgLengthUnit.Em:
                return "em";
            case SvgLengthUnit.Ex:
                return "ex";
            case SvgLengthUnit.Ch:
                return "ch";
            case SvgLengthUnit.Rem:
                return "rem";
            case SvgLengthUnit.Vw:
                return "vw";
            case SvgLengthUnit.Vh:
                return "vh";
            case SvgLengthUnit.Vmin:
                return "vmin";
            case SvgLengthUnit.Vmax:
                return "vmax";
            case SvgLengthUnit.Cm:
                return "cm";
            case SvgLengthUnit.Mm:
                return "mm";
            case SvgLengthUnit.Q:
                return "Q";
            case SvgLengthUnit.In:
                return "in";
            case SvgLengthUnit.Pt:
                return "pt";
            case SvgLengthUnit.Pc:
                return "pc";
            default:
                return "";
        }
    }

    /// <summary>Finds a case-insensitive CSS unit suffix.</summary>
    /// <param name="text">The suffix text.</param>
    /// <returns>The corresponding enum value, or <c>-1</c>.</returns>
    public static int Find(string text)
    {
        if (text == "%")
        {
            return (int)SvgLengthUnit.Percentage;
        }
        if (string.Equals(text, "px", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Px;
        }
        if (string.Equals(text, "em", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Em;
        }
        if (string.Equals(text, "ex", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Ex;
        }
        if (string.Equals(text, "ch", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Ch;
        }
        if (string.Equals(text, "rem", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Rem;
        }
        if (string.Equals(text, "vw", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Vw;
        }
        if (string.Equals(text, "vh", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Vh;
        }
        if (string.Equals(text, "vmin", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Vmin;
        }
        if (string.Equals(text, "vmax", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Vmax;
        }
        if (string.Equals(text, "cm", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Cm;
        }
        if (string.Equals(text, "mm", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Mm;
        }
        if (string.Equals(text, "q", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Q;
        }
        if (string.Equals(text, "in", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.In;
        }
        if (string.Equals(text, "pt", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Pt;
        }
        if (string.Equals(text, "pc", System.StringComparison.OrdinalIgnoreCase))
        {
            return (int)SvgLengthUnit.Pc;
        }
        return -1;
    }
}
