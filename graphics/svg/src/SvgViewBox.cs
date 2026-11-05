namespace svg;

/// <summary>Defines stable failures produced by viewBox and preserveAspectRatio parsing.</summary>
internal enum SvgViewBoxParseError
{
    /// <summary>No parse failure occurred.</summary>
    None = 0,

    /// <summary>A required numeric component or keyword was absent.</summary>
    ExpectedValue = 1,

    /// <summary>A numeric component was malformed, too long, or non-finite.</summary>
    InvalidNumber = 2,

    /// <summary>The view box did not contain exactly four components.</summary>
    ComponentCount = 3,

    /// <summary>A view-box width or height was negative.</summary>
    NegativeDimension = 4,

    /// <summary>An alignment keyword was unknown.</summary>
    UnknownAlignment = 5,

    /// <summary>A meet-or-slice keyword was unknown.</summary>
    UnknownScale = 6,

    /// <summary>A keyword exceeded its configured character bound.</summary>
    TokenTooLong = 7,

    /// <summary>Trailing non-whitespace text followed the complete value.</summary>
    TrailingCharacters = 8,

    /// <summary>A parser argument was invalid.</summary>
    InvalidArgument = 9
}

/// <summary>Contains a non-throwing viewBox parse result.</summary>
internal class SvgViewBoxParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The view-box parse failure kind.</summary>
    public SvgViewBoxParseError Error;
    /// <summary>The underlying number failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed view box, or <c>null</c> after failure.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a viewBox parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The view-box failure kind.</param>
    /// <param name="numberError">The number failure kind.</param>
    /// <param name="viewBox">The parsed view box, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgViewBoxParseResult(bool success, SvgViewBoxParseError error, SvgNumberParseError numberError, SvgViewBox? viewBox, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        ViewBox = viewBox;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing preserveAspectRatio parse result.</summary>
internal class SvgAspectRatioParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The preserveAspectRatio parse failure kind.</summary>
    public SvgViewBoxParseError Error;
    /// <summary>The parsed value, or <c>null</c> after failure.</summary>
    public SvgAspectRatio? AspectRatio;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a preserveAspectRatio parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="aspectRatio">The parsed value, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgAspectRatioParseResult(bool success, SvgViewBoxParseError error, SvgAspectRatio? aspectRatio, int errorOffset)
    {
        Success = success;
        Error = error;
        AspectRatio = aspectRatio;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Parses complete viewBox and preserveAspectRatio values.</summary>
internal static class SvgViewBoxParser
{
    /// <summary>Parses exactly four comma-whitespace separated view-box numbers.</summary>
    /// <param name="value">The viewBox text.</param>
    /// <param name="maxTokenCharacters">The maximum length of one number.</param>
    /// <returns>A validated view-box parse result.</returns>
    public static SvgViewBoxParseResult ParseViewBox(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return ViewBoxFailure(SvgViewBoxParseError.InvalidArgument, SvgNumberParseError.None, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        double[] components = new double[4];
        int[] offsets = new int[4];
        int index = 0;
        while (index < 4)
        {
            offsets[index] = reader.GetPosition();
            SvgNumberParseResult number = SvgNumberParser.ReadNumber(reader);
            if (!number.Success)
            {
                SvgViewBoxParseError error = number.Error == SvgNumberParseError.ExpectedNumber ? SvgViewBoxParseError.ExpectedValue : SvgViewBoxParseError.InvalidNumber;
                return ViewBoxFailure(error, number.Error, number.ErrorOffset);
            }
            components[index] = number.Value;
            index = index + 1;
            if (index < 4)
            {
                SvgSpanSeparatorResult separator = reader.ReadCommaWhitespace();
                if (!separator.Success)
                {
                    return ViewBoxFailure(SvgViewBoxParseError.ComponentCount, SvgNumberParseError.None, separator.ErrorOffset);
                }
            }
        }
        reader.SkipWhitespace();
        if (!reader.IsEnd())
        {
            return ViewBoxFailure(SvgViewBoxParseError.ComponentCount, SvgNumberParseError.None, reader.GetPosition());
        }
        if (components[2] < 0)
        {
            return ViewBoxFailure(SvgViewBoxParseError.NegativeDimension, SvgNumberParseError.None, offsets[2]);
        }
        if (components[3] < 0)
        {
            return ViewBoxFailure(SvgViewBoxParseError.NegativeDimension, SvgNumberParseError.None, offsets[3]);
        }
        return new SvgViewBoxParseResult(true, SvgViewBoxParseError.None, SvgNumberParseError.None, new SvgViewBox(components[0], components[1], components[2], components[3]), -1);
    }

    /// <summary>Parses a complete preserveAspectRatio value.</summary>
    /// <param name="value">The preserveAspectRatio text.</param>
    /// <param name="maxTokenCharacters">The maximum length of one keyword.</param>
    /// <returns>A validated preserveAspectRatio parse result.</returns>
    public static SvgAspectRatioParseResult ParseAspectRatio(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return AspectRatioFailure(SvgViewBoxParseError.InvalidArgument, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        int firstStart = reader.GetPosition();
        SvgSpanTokenResult first = reader.ReadToken(IsAsciiLetter, false);
        if (!first.Success)
        {
            return AspectRatioFailure(TokenError(first), first.ErrorOffset);
        }
        if (first.RawText.Length == 0)
        {
            return AspectRatioFailure(SvgViewBoxParseError.ExpectedValue, firstStart);
        }

        string alignmentText = first.GetValue();
        int alignmentOffset = firstStart;
        bool defer = false;
        if (alignmentText == "defer")
        {
            defer = true;
            int whitespace = reader.SkipWhitespace();
            if (whitespace == 0)
            {
                return AspectRatioFailure(SvgViewBoxParseError.ExpectedValue, reader.GetPosition());
            }
            alignmentOffset = reader.GetPosition();
            SvgSpanTokenResult alignmentToken = reader.ReadToken(IsAsciiLetter, false);
            if (!alignmentToken.Success)
            {
                return AspectRatioFailure(TokenError(alignmentToken), alignmentToken.ErrorOffset);
            }
            if (alignmentToken.RawText.Length == 0)
            {
                return AspectRatioFailure(SvgViewBoxParseError.ExpectedValue, alignmentOffset);
            }
            alignmentText = alignmentToken.GetValue();
        }

        int alignmentValue = SvgAspectRatioNames.FindAlignment(alignmentText);
        if (alignmentValue < 0)
        {
            return AspectRatioFailure(SvgViewBoxParseError.UnknownAlignment, alignmentOffset);
        }
        int scaleValue = (int)SvgAspectRatioScale.Meet;
        int scaleWhitespace = reader.SkipWhitespace();
        if (!reader.IsEnd())
        {
            if (scaleWhitespace == 0)
            {
                return AspectRatioFailure(SvgViewBoxParseError.TrailingCharacters, reader.GetPosition());
            }
            int scaleOffset = reader.GetPosition();
            SvgSpanTokenResult scaleToken = reader.ReadToken(IsAsciiLetter, false);
            if (!scaleToken.Success)
            {
                return AspectRatioFailure(TokenError(scaleToken), scaleToken.ErrorOffset);
            }
            if (scaleToken.RawText.Length == 0)
            {
                return AspectRatioFailure(SvgViewBoxParseError.UnknownScale, scaleOffset);
            }
            scaleValue = SvgAspectRatioNames.FindScale(scaleToken.GetValue());
            if (scaleValue < 0)
            {
                return AspectRatioFailure(SvgViewBoxParseError.UnknownScale, scaleOffset);
            }
            reader.SkipWhitespace();
            if (!reader.IsEnd())
            {
                return AspectRatioFailure(SvgViewBoxParseError.TrailingCharacters, reader.GetPosition());
            }
        }
        SvgAspectRatio result = new SvgAspectRatio((SvgAspectRatioAlignment)alignmentValue, (SvgAspectRatioScale)scaleValue, defer);
        return new SvgAspectRatioParseResult(true, SvgViewBoxParseError.None, result, -1);
    }

    static SvgViewBoxParseError TokenError(SvgSpanTokenResult token)
    {
        return token.Error == SvgSpanReadError.TokenTooLong ? SvgViewBoxParseError.TokenTooLong : SvgViewBoxParseError.InvalidArgument;
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static SvgViewBoxParseResult ViewBoxFailure(SvgViewBoxParseError error, SvgNumberParseError numberError, int offset)
    {
        return new SvgViewBoxParseResult(false, error, numberError, null, offset);
    }

    static SvgAspectRatioParseResult AspectRatioFailure(SvgViewBoxParseError error, int offset)
    {
        return new SvgAspectRatioParseResult(false, error, null, offset);
    }
}

/// <summary>Computes finite view-box fitting transforms.</summary>
internal static class SvgViewBoxFitter
{
    /// <summary>Fits a view box into one viewport.</summary>
    /// <param name="viewBox">The source view box.</param>
    /// <param name="aspectRatio">The alignment and scaling policy.</param>
    /// <param name="viewportX">The viewport x coordinate.</param>
    /// <param name="viewportY">The viewport y coordinate.</param>
    /// <param name="viewportWidth">The viewport width.</param>
    /// <param name="viewportHeight">The viewport height.</param>
    /// <returns>A finite affine matrix or a stable failure.</returns>
    public static SvgViewBoxFitResult Fit(SvgViewBox viewBox, SvgAspectRatio aspectRatio, double viewportX, double viewportY, double viewportWidth, double viewportHeight)
    {
        if (!ValidArguments(viewBox, aspectRatio, viewportX, viewportY, viewportWidth, viewportHeight))
        {
            return Failure(SvgViewBoxFitError.InvalidArgument);
        }
        if (viewBox.Width == 0 || viewBox.Height == 0)
        {
            return Failure(SvgViewBoxFitError.DegenerateViewBox);
        }

        double scaleX = viewportWidth / viewBox.Width;
        double scaleY = viewportHeight / viewBox.Height;
        double translateX;
        double translateY;
        if (aspectRatio.Alignment == SvgAspectRatioAlignment.None)
        {
            translateX = viewportX - viewBox.X * scaleX;
            translateY = viewportY - viewBox.Y * scaleY;
        }
        else
        {
            double scale = aspectRatio.Scale == SvgAspectRatioScale.Slice ? System.Math.Max(scaleX, scaleY) : System.Math.Min(scaleX, scaleY);
            scaleX = scale;
            scaleY = scale;
            double remainingX = viewportWidth - viewBox.Width * scale;
            double remainingY = viewportHeight - viewBox.Height * scale;
            int alignmentIndex = (int)aspectRatio.Alignment - 1;
            double xFactor = (alignmentIndex % 3) * 0.5;
            double yFactor = (alignmentIndex / 3) * 0.5;
            translateX = viewportX + remainingX * xFactor - viewBox.X * scale;
            translateY = viewportY + remainingY * yFactor - viewBox.Y * scale;
        }
        if (!SvgFiniteMath.IsFinite(scaleX) || !SvgFiniteMath.IsFinite(scaleY) || !SvgFiniteMath.IsFinite(translateX) || !SvgFiniteMath.IsFinite(translateY))
        {
            return Failure(SvgViewBoxFitError.NonFiniteResult);
        }
        SvgMatrix matrix = new SvgMatrix(scaleX, 0, 0, scaleY, translateX, translateY);
        return new SvgViewBoxFitResult(true, SvgViewBoxFitError.None, matrix);
    }

    static bool ValidArguments(SvgViewBox? viewBox, SvgAspectRatio? aspectRatio, double viewportX, double viewportY, double viewportWidth, double viewportHeight)
    {
        if (viewBox == null || aspectRatio == null)
        {
            return false;
        }
        if (!SvgFiniteMath.IsFinite(viewBox.X) || !SvgFiniteMath.IsFinite(viewBox.Y) || !SvgFiniteMath.IsFinite(viewBox.Width) || !SvgFiniteMath.IsFinite(viewBox.Height) || viewBox.Width < 0 || viewBox.Height < 0)
        {
            return false;
        }
        if (!SvgAspectRatioNames.IsAlignment(aspectRatio.Alignment) || !SvgAspectRatioNames.IsScale(aspectRatio.Scale))
        {
            return false;
        }
        return SvgFiniteMath.IsFinite(viewportX) && SvgFiniteMath.IsFinite(viewportY) && SvgFiniteMath.IsFinite(viewportWidth) && SvgFiniteMath.IsFinite(viewportHeight) && viewportWidth >= 0 && viewportHeight >= 0;
    }

    static SvgViewBoxFitResult Failure(SvgViewBoxFitError error)
    {
        return new SvgViewBoxFitResult(false, error, null);
    }
}

/// <summary>Maps preserveAspectRatio enums to grammar keywords.</summary>
internal static class SvgAspectRatioNames
{
    /// <summary>Tests whether an alignment enum value is known.</summary>
    /// <param name="alignment">The value to test.</param>
    /// <returns><c>true</c> for a known alignment.</returns>
    public static bool IsAlignment(SvgAspectRatioAlignment alignment)
    {
        return alignment >= SvgAspectRatioAlignment.None && alignment <= SvgAspectRatioAlignment.XMaxYMax;
    }

    /// <summary>Tests whether a scale enum value is known.</summary>
    /// <param name="scale">The value to test.</param>
    /// <returns><c>true</c> for a known scale mode.</returns>
    public static bool IsScale(SvgAspectRatioScale scale)
    {
        return scale >= SvgAspectRatioScale.Meet && scale <= SvgAspectRatioScale.Slice;
    }

    /// <summary>Finds an exact alignment keyword.</summary>
    /// <param name="text">The keyword text.</param>
    /// <returns>The alignment value, or <c>-1</c>.</returns>
    public static int FindAlignment(string text)
    {
        if (text == "none") return (int)SvgAspectRatioAlignment.None;
        if (text == "xMinYMin") return (int)SvgAspectRatioAlignment.XMinYMin;
        if (text == "xMidYMin") return (int)SvgAspectRatioAlignment.XMidYMin;
        if (text == "xMaxYMin") return (int)SvgAspectRatioAlignment.XMaxYMin;
        if (text == "xMinYMid") return (int)SvgAspectRatioAlignment.XMinYMid;
        if (text == "xMidYMid") return (int)SvgAspectRatioAlignment.XMidYMid;
        if (text == "xMaxYMid") return (int)SvgAspectRatioAlignment.XMaxYMid;
        if (text == "xMinYMax") return (int)SvgAspectRatioAlignment.XMinYMax;
        if (text == "xMidYMax") return (int)SvgAspectRatioAlignment.XMidYMax;
        if (text == "xMaxYMax") return (int)SvgAspectRatioAlignment.XMaxYMax;
        return -1;
    }

    /// <summary>Finds an exact meet-or-slice keyword.</summary>
    /// <param name="text">The keyword text.</param>
    /// <returns>The scale value, or <c>-1</c>.</returns>
    public static int FindScale(string text)
    {
        if (text == "meet") return (int)SvgAspectRatioScale.Meet;
        if (text == "slice") return (int)SvgAspectRatioScale.Slice;
        return -1;
    }

    /// <summary>Returns canonical alignment text.</summary>
    /// <param name="alignment">The alignment value.</param>
    /// <returns>The exact alignment keyword.</returns>
    public static string AlignmentText(SvgAspectRatioAlignment alignment)
    {
        switch (alignment)
        {
            case SvgAspectRatioAlignment.None: return "none";
            case SvgAspectRatioAlignment.XMinYMin: return "xMinYMin";
            case SvgAspectRatioAlignment.XMidYMin: return "xMidYMin";
            case SvgAspectRatioAlignment.XMaxYMin: return "xMaxYMin";
            case SvgAspectRatioAlignment.XMinYMid: return "xMinYMid";
            case SvgAspectRatioAlignment.XMidYMid: return "xMidYMid";
            case SvgAspectRatioAlignment.XMaxYMid: return "xMaxYMid";
            case SvgAspectRatioAlignment.XMinYMax: return "xMinYMax";
            case SvgAspectRatioAlignment.XMidYMax: return "xMidYMax";
            case SvgAspectRatioAlignment.XMaxYMax: return "xMaxYMax";
            default: return "none";
        }
    }

    /// <summary>Returns canonical meet-or-slice text.</summary>
    /// <param name="scale">The scale mode.</param>
    /// <returns>The exact scale keyword.</returns>
    public static string ScaleText(SvgAspectRatioScale scale)
    {
        return scale == SvgAspectRatioScale.Slice ? "slice" : "meet";
    }
}

/// <summary>Provides shortest invariant view-box number text.</summary>
internal static class SvgViewBoxText
{
    /// <summary>Formats one finite number.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>The shortest invariant number text.</returns>
    public static string Number(double value)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatShortest(value);
        return result.Text;
    }
}

/// <summary>Provides throwing validation for programmatically created view-box values.</summary>
internal static class SvgViewBoxValidation
{
    /// <summary>Requires one finite value.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The constructor parameter name.</param>
    public static void RequireFinite(double value, string parameterName)
    {
        if (!SvgFiniteMath.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
