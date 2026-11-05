namespace svg;

/// <summary>Defines stable failures produced by scalar-unit parsing.</summary>
internal enum SvgScalarUnitParseError
{
    /// <summary>No parse failure occurred.</summary>
    None = 0,

    /// <summary>A numeric value was not present.</summary>
    ExpectedValue = 1,

    /// <summary>The numeric component was malformed, too long, or non-finite.</summary>
    InvalidNumber = 2,

    /// <summary>The unit suffix was unknown.</summary>
    UnknownUnit = 3,

    /// <summary>A required unit suffix was absent.</summary>
    UnitRequired = 4,

    /// <summary>The complete token exceeded its configured bound.</summary>
    TokenTooLong = 5,

    /// <summary>Trailing non-whitespace text followed the value.</summary>
    TrailingCharacters = 6,

    /// <summary>A slash or denominator was missing from a ratio.</summary>
    ExpectedRatioComponent = 7,

    /// <summary>The parsed value was outside the type's permitted range.</summary>
    OutOfRange = 8,

    /// <summary>A parser argument was invalid.</summary>
    InvalidArgument = 9
}

/// <summary>Contains a non-throwing angle parse result.</summary>
internal class SvgAngleParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed angle, or <c>null</c> after failure.</summary>
    public SvgAngle? Angle;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates an angle parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="angle">The parsed angle, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgAngleParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, SvgAngle? angle, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Angle = angle;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing time parse result.</summary>
internal class SvgTimeParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed time, or <c>null</c> after failure.</summary>
    public SvgTime? Time;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a time parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="time">The parsed time, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgTimeParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, SvgTime? time, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Time = time;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing frequency parse result.</summary>
internal class SvgFrequencyParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed frequency, or <c>null</c> after failure.</summary>
    public SvgFrequency? Frequency;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a frequency parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="frequency">The parsed frequency, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgFrequencyParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, SvgFrequency? frequency, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Frequency = frequency;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing resolution parse result.</summary>
internal class SvgResolutionParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed resolution, or <c>null</c> after failure.</summary>
    public SvgResolution? Resolution;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a resolution parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="resolution">The parsed resolution, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgResolutionParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, SvgResolution? resolution, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Resolution = resolution;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing ratio parse result.</summary>
internal class SvgRatioParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed ratio, or <c>null</c> after failure.</summary>
    public SvgRatio? Ratio;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a ratio parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="ratio">The parsed ratio, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgRatioParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, SvgRatio? ratio, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Ratio = ratio;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing pathLength parse result.</summary>
internal class SvgPathLengthParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed pathLength, or <c>null</c> after failure.</summary>
    public SvgPathLength? PathLength;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a pathLength parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="pathLength">The parsed pathLength, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgPathLengthParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, SvgPathLength? pathLength, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        PathLength = pathLength;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a scanned finite number and optional alphabetic unit suffix.</summary>
internal class SvgDimensionParseResult
{
    /// <summary>Indicates whether dimension scanning succeeded.</summary>
    public bool Success;
    /// <summary>The scalar parse failure kind.</summary>
    public SvgScalarUnitParseError Error;
    /// <summary>The numeric parse failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The finite numeric component.</summary>
    public double Value;
    /// <summary>The numeric component offset.</summary>
    public int ValueOffset;
    /// <summary>The unit suffix, or an empty string.</summary>
    public string UnitText;
    /// <summary>The unit suffix offset.</summary>
    public int UnitOffset;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a dimension scan result.</summary>
    /// <param name="success">Whether scanning succeeded.</param>
    /// <param name="error">The scalar failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="value">The finite numeric component.</param>
    /// <param name="valueOffset">The numeric component offset.</param>
    /// <param name="unitText">The unit suffix.</param>
    /// <param name="unitOffset">The unit suffix offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgDimensionParseResult(bool success, SvgScalarUnitParseError error, SvgNumberParseError numberError, double value, int valueOffset, string unitText, int unitOffset, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Value = value;
        ValueOffset = valueOffset;
        UnitText = unitText;
        UnitOffset = unitOffset;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Parses typed angle, time, frequency, resolution, ratio, and pathLength values.</summary>
internal static class SvgScalarUnitParser
{
    /// <summary>Parses a complete angle.</summary>
    /// <param name="value">The angle text.</param>
    /// <param name="allowUnitless">Whether unitless SVG degrees are permitted.</param>
    /// <param name="maxTokenCharacters">The maximum complete token length.</param>
    /// <returns>An angle parse result.</returns>
    public static SvgAngleParseResult ParseAngle(string value, bool allowUnitless, int maxTokenCharacters)
    {
        SvgDimensionParseResult dimension = ParseDimension(value, maxTokenCharacters);
        if (!dimension.Success)
        {
            return new SvgAngleParseResult(false, dimension.Error, dimension.NumberError, null, dimension.ErrorOffset);
        }
        int unitValue = SvgScalarUnitNames.FindAngle(dimension.UnitText, allowUnitless);
        if (unitValue < 0)
        {
            SvgScalarUnitParseError error = dimension.UnitText.Length == 0 ? SvgScalarUnitParseError.UnitRequired : SvgScalarUnitParseError.UnknownUnit;
            return new SvgAngleParseResult(false, error, SvgNumberParseError.None, null, dimension.UnitOffset);
        }
        return new SvgAngleParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, new SvgAngle(dimension.Value, (SvgAngleUnit)unitValue), -1);
    }

    /// <summary>Parses a complete time.</summary>
    /// <param name="value">The time text.</param>
    /// <param name="allowUnitlessSeconds">Whether a unitless SMIL time count is permitted.</param>
    /// <param name="maxTokenCharacters">The maximum complete token length.</param>
    /// <returns>A time parse result.</returns>
    public static SvgTimeParseResult ParseTime(string value, bool allowUnitlessSeconds, int maxTokenCharacters)
    {
        SvgDimensionParseResult dimension = ParseDimension(value, maxTokenCharacters);
        if (!dimension.Success)
        {
            return new SvgTimeParseResult(false, dimension.Error, dimension.NumberError, null, dimension.ErrorOffset);
        }
        int unitValue = SvgScalarUnitNames.FindTime(dimension.UnitText, allowUnitlessSeconds);
        if (unitValue < 0)
        {
            SvgScalarUnitParseError error = dimension.UnitText.Length == 0 ? SvgScalarUnitParseError.UnitRequired : SvgScalarUnitParseError.UnknownUnit;
            return new SvgTimeParseResult(false, error, SvgNumberParseError.None, null, dimension.UnitOffset);
        }
        return new SvgTimeParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, new SvgTime(dimension.Value, (SvgTimeUnit)unitValue), -1);
    }

    /// <summary>Parses a complete nonnegative frequency.</summary>
    /// <param name="value">The frequency text.</param>
    /// <param name="maxTokenCharacters">The maximum complete token length.</param>
    /// <returns>A frequency parse result.</returns>
    public static SvgFrequencyParseResult ParseFrequency(string value, int maxTokenCharacters)
    {
        SvgDimensionParseResult dimension = ParseDimension(value, maxTokenCharacters);
        if (!dimension.Success)
        {
            return new SvgFrequencyParseResult(false, dimension.Error, dimension.NumberError, null, dimension.ErrorOffset);
        }
        int unitValue = SvgScalarUnitNames.FindFrequency(dimension.UnitText);
        if (unitValue < 0)
        {
            SvgScalarUnitParseError error = dimension.UnitText.Length == 0 ? SvgScalarUnitParseError.UnitRequired : SvgScalarUnitParseError.UnknownUnit;
            return new SvgFrequencyParseResult(false, error, SvgNumberParseError.None, null, dimension.UnitOffset);
        }
        if (dimension.Value < 0)
        {
            return new SvgFrequencyParseResult(false, SvgScalarUnitParseError.OutOfRange, SvgNumberParseError.None, null, dimension.ValueOffset);
        }
        return new SvgFrequencyParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, new SvgFrequency(dimension.Value, (SvgFrequencyUnit)unitValue), -1);
    }

    /// <summary>Parses a complete strictly positive resolution.</summary>
    /// <param name="value">The resolution text.</param>
    /// <param name="maxTokenCharacters">The maximum complete token length.</param>
    /// <returns>A resolution parse result.</returns>
    public static SvgResolutionParseResult ParseResolution(string value, int maxTokenCharacters)
    {
        SvgDimensionParseResult dimension = ParseDimension(value, maxTokenCharacters);
        if (!dimension.Success)
        {
            return new SvgResolutionParseResult(false, dimension.Error, dimension.NumberError, null, dimension.ErrorOffset);
        }
        int unitValue = SvgScalarUnitNames.FindResolution(dimension.UnitText);
        if (unitValue < 0)
        {
            SvgScalarUnitParseError error = dimension.UnitText.Length == 0 ? SvgScalarUnitParseError.UnitRequired : SvgScalarUnitParseError.UnknownUnit;
            return new SvgResolutionParseResult(false, error, SvgNumberParseError.None, null, dimension.UnitOffset);
        }
        if (dimension.Value <= 0)
        {
            return new SvgResolutionParseResult(false, SvgScalarUnitParseError.OutOfRange, SvgNumberParseError.None, null, dimension.ValueOffset);
        }
        return new SvgResolutionParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, new SvgResolution(dimension.Value, (SvgResolutionUnit)unitValue), -1);
    }

    /// <summary>Parses a complete positive ratio with an optional denominator.</summary>
    /// <param name="value">The ratio text.</param>
    /// <param name="maxTokenCharacters">The maximum complete ratio token length.</param>
    /// <returns>A ratio parse result.</returns>
    public static SvgRatioParseResult ParseRatio(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return new SvgRatioParseResult(false, SvgScalarUnitParseError.InvalidArgument, SvgNumberParseError.None, null, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipCssWhitespace();
        int start = reader.GetPosition();
        SvgNumberParseResult numerator = SvgNumberParser.ReadCssNumber(reader);
        if (!numerator.Success)
        {
            SvgScalarUnitParseError error = numerator.Error == SvgNumberParseError.ExpectedNumber ? SvgScalarUnitParseError.ExpectedValue : SvgScalarUnitParseError.InvalidNumber;
            return new SvgRatioParseResult(false, error, numerator.Error, null, numerator.ErrorOffset);
        }
        if (numerator.Value <= 0)
        {
            return new SvgRatioParseResult(false, SvgScalarUnitParseError.OutOfRange, SvgNumberParseError.None, null, start);
        }

        int tokenEnd = reader.GetPosition();
        reader.SkipCssWhitespace();
        double denominatorValue = 1;
        if (reader.Consume('/'))
        {
            reader.SkipCssWhitespace();
            int denominatorStart = reader.GetPosition();
            SvgNumberParseResult denominator = SvgNumberParser.ReadCssNumber(reader);
            if (!denominator.Success)
            {
                SvgScalarUnitParseError error = denominator.Error == SvgNumberParseError.ExpectedNumber ? SvgScalarUnitParseError.ExpectedRatioComponent : SvgScalarUnitParseError.InvalidNumber;
                return new SvgRatioParseResult(false, error, denominator.Error, null, denominator.ErrorOffset);
            }
            if (denominator.Value <= 0)
            {
                return new SvgRatioParseResult(false, SvgScalarUnitParseError.OutOfRange, SvgNumberParseError.None, null, denominatorStart);
            }
            denominatorValue = denominator.Value;
            tokenEnd = reader.GetPosition();
            reader.SkipCssWhitespace();
        }
        if (!reader.IsEnd())
        {
            return new SvgRatioParseResult(false, SvgScalarUnitParseError.TrailingCharacters, SvgNumberParseError.None, null, reader.GetPosition());
        }
        if (tokenEnd - start > maxTokenCharacters)
        {
            return new SvgRatioParseResult(false, SvgScalarUnitParseError.TokenTooLong, SvgNumberParseError.None, null, start + maxTokenCharacters);
        }
        return new SvgRatioParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, new SvgRatio(numerator.Value, denominatorValue), -1);
    }

    /// <summary>Parses a complete finite nonnegative pathLength.</summary>
    /// <param name="value">The pathLength text.</param>
    /// <param name="maxTokenCharacters">The maximum numeric token length.</param>
    /// <returns>A pathLength parse result.</returns>
    public static SvgPathLengthParseResult ParsePathLength(string value, int maxTokenCharacters)
    {
        SvgNumberParseResult number = SvgNumberParser.ParseNumber(value, maxTokenCharacters);
        if (!number.Success)
        {
            SvgScalarUnitParseError error = number.Error == SvgNumberParseError.ExpectedNumber ? SvgScalarUnitParseError.ExpectedValue : SvgScalarUnitParseError.InvalidNumber;
            if (number.Error == SvgNumberParseError.InvalidArgument)
            {
                error = SvgScalarUnitParseError.InvalidArgument;
            }
            return new SvgPathLengthParseResult(false, error, number.Error, null, number.ErrorOffset);
        }
        if (number.Value < 0)
        {
            return new SvgPathLengthParseResult(false, SvgScalarUnitParseError.OutOfRange, SvgNumberParseError.None, null, number.Start);
        }
        return new SvgPathLengthParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, new SvgPathLength(number.Value), -1);
    }

    static SvgDimensionParseResult ParseDimension(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return new SvgDimensionParseResult(false, SvgScalarUnitParseError.InvalidArgument, SvgNumberParseError.None, 0, 0, "", 0, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipCssWhitespace();
        int tokenStart = reader.GetPosition();
        SvgNumberParseResult number = SvgNumberParser.ReadCssNumber(reader);
        if (!number.Success)
        {
            SvgScalarUnitParseError error = number.Error == SvgNumberParseError.ExpectedNumber ? SvgScalarUnitParseError.ExpectedValue : SvgScalarUnitParseError.InvalidNumber;
            return new SvgDimensionParseResult(false, error, number.Error, 0, tokenStart, "", tokenStart, number.ErrorOffset);
        }
        System.ReadOnlySpan<char> input = reader.GetInput().Span;
        int unitStart = reader.GetPosition();
        int position = unitStart;
        while (position < input.Length && IsAsciiLetter(input[position]))
        {
            position = position + 1;
        }
        if (position - tokenStart > maxTokenCharacters)
        {
            return new SvgDimensionParseResult(false, SvgScalarUnitParseError.TokenTooLong, SvgNumberParseError.None, 0, tokenStart, "", unitStart, tokenStart + maxTokenCharacters);
        }
        string unitText = input.Slice(unitStart, position - unitStart).ToString();
        reader.SetPosition(position);
        reader.SkipCssWhitespace();
        if (!reader.IsEnd())
        {
            return new SvgDimensionParseResult(false, SvgScalarUnitParseError.TrailingCharacters, SvgNumberParseError.None, 0, tokenStart, unitText, unitStart, reader.GetPosition());
        }
        return new SvgDimensionParseResult(true, SvgScalarUnitParseError.None, SvgNumberParseError.None, number.Value, tokenStart, unitText, unitStart, -1);
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }
}

/// <summary>Provides finite conversion between scalar unit families.</summary>
internal static class SvgScalarUnitConverter
{
    /// <summary>Converts an angle numeric component to another angle unit.</summary>
    /// <param name="angle">The source angle.</param>
    /// <param name="targetUnit">The target unit.</param>
    /// <returns>The finite converted value.</returns>
    public static SvgUnitConversionResult ConvertAngle(SvgAngle? angle, SvgAngleUnit targetUnit)
    {
        if (angle == null || !SvgFiniteMath.IsFinite(angle.Value) || !SvgScalarUnitNames.IsAngleUnit(angle.Unit) || !SvgScalarUnitNames.IsAngleUnit(targetUnit))
        {
            return Failure(SvgUnitConversionError.InvalidArgument);
        }
        double factor = AngleDegrees(angle.Unit) / AngleDegrees(targetUnit);
        return Multiply(angle.Value, factor);
    }

    /// <summary>Converts a time numeric component to another time unit.</summary>
    /// <param name="time">The source time.</param>
    /// <param name="targetUnit">The target unit.</param>
    /// <returns>The finite converted value.</returns>
    public static SvgUnitConversionResult ConvertTime(SvgTime? time, SvgTimeUnit targetUnit)
    {
        if (time == null || !SvgFiniteMath.IsFinite(time.Value) || !SvgScalarUnitNames.IsTimeUnit(time.Unit) || !SvgScalarUnitNames.IsTimeUnit(targetUnit))
        {
            return Failure(SvgUnitConversionError.InvalidArgument);
        }
        double factor = TimeSeconds(time.Unit) / TimeSeconds(targetUnit);
        return Multiply(time.Value, factor);
    }

    /// <summary>Converts a frequency numeric component to another frequency unit.</summary>
    /// <param name="frequency">The source frequency.</param>
    /// <param name="targetUnit">The target unit.</param>
    /// <returns>The finite converted value.</returns>
    public static SvgUnitConversionResult ConvertFrequency(SvgFrequency? frequency, SvgFrequencyUnit targetUnit)
    {
        if (frequency == null || !SvgFiniteMath.IsFinite(frequency.Value) || frequency.Value < 0 || !SvgScalarUnitNames.IsFrequencyUnit(frequency.Unit) || !SvgScalarUnitNames.IsFrequencyUnit(targetUnit))
        {
            return Failure(SvgUnitConversionError.InvalidArgument);
        }
        double factor = FrequencyHertz(frequency.Unit) / FrequencyHertz(targetUnit);
        return Multiply(frequency.Value, factor);
    }

    /// <summary>Converts a resolution numeric component to another resolution unit.</summary>
    /// <param name="resolution">The source resolution.</param>
    /// <param name="targetUnit">The target unit.</param>
    /// <returns>The finite converted value.</returns>
    public static SvgUnitConversionResult ConvertResolution(SvgResolution? resolution, SvgResolutionUnit targetUnit)
    {
        if (resolution == null || !SvgFiniteMath.IsFinite(resolution.Value) || resolution.Value <= 0 || !SvgScalarUnitNames.IsResolutionUnit(resolution.Unit) || !SvgScalarUnitNames.IsResolutionUnit(targetUnit))
        {
            return Failure(SvgUnitConversionError.InvalidArgument);
        }
        double factor = ResolutionDpi(resolution.Unit) / ResolutionDpi(targetUnit);
        return Multiply(resolution.Value, factor);
    }

    /// <summary>Creates a successful finite conversion result.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>A successful result.</returns>
    public static SvgUnitConversionResult Success(double value)
    {
        return new SvgUnitConversionResult(true, SvgUnitConversionError.None, SvgFiniteMath.NormalizeZero(value));
    }

    /// <summary>Creates a failed conversion result.</summary>
    /// <param name="error">The failure kind.</param>
    /// <returns>A failed result containing zero.</returns>
    public static SvgUnitConversionResult Failure(SvgUnitConversionError error)
    {
        return new SvgUnitConversionResult(false, error, 0);
    }

    static SvgUnitConversionResult Multiply(double value, double factor)
    {
        double result = value * factor;
        if (!SvgFiniteMath.IsFinite(result))
        {
            return Failure(SvgUnitConversionError.NonFiniteResult);
        }
        return Success(result);
    }

    static double AngleDegrees(SvgAngleUnit unit)
    {
        switch (unit)
        {
            case SvgAngleUnit.Number:
            case SvgAngleUnit.Deg:
                return 1;
            case SvgAngleUnit.Grad:
                return 0.9;
            case SvgAngleUnit.Rad:
                return 180 / System.Math.PI;
            case SvgAngleUnit.Turn:
                return 360;
            default:
                return 0;
        }
    }

    static double TimeSeconds(SvgTimeUnit unit)
    {
        return unit == SvgTimeUnit.Milliseconds ? 0.001 : 1;
    }

    static double FrequencyHertz(SvgFrequencyUnit unit)
    {
        return unit == SvgFrequencyUnit.KHz ? 1000 : 1;
    }

    static double ResolutionDpi(SvgResolutionUnit unit)
    {
        if (unit == SvgResolutionUnit.Dpcm)
        {
            return 2.54;
        }
        if (unit == SvgResolutionUnit.Dppx)
        {
            return 96;
        }
        return 1;
    }
}

/// <summary>Provides scalar unit recognition and canonical text.</summary>
internal static class SvgScalarUnitNames
{
    /// <summary>Tests whether an angle unit is known.</summary>
    /// <param name="unit">The unit to test.</param>
    /// <returns><c>true</c> for a known unit.</returns>
    public static bool IsAngleUnit(SvgAngleUnit unit)
    {
        return unit >= SvgAngleUnit.Number && unit <= SvgAngleUnit.Turn;
    }

    /// <summary>Tests whether a time unit is known.</summary>
    /// <param name="unit">The unit to test.</param>
    /// <returns><c>true</c> for a known unit.</returns>
    public static bool IsTimeUnit(SvgTimeUnit unit)
    {
        return unit >= SvgTimeUnit.Seconds && unit <= SvgTimeUnit.Milliseconds;
    }

    /// <summary>Tests whether a frequency unit is known.</summary>
    /// <param name="unit">The unit to test.</param>
    /// <returns><c>true</c> for a known unit.</returns>
    public static bool IsFrequencyUnit(SvgFrequencyUnit unit)
    {
        return unit >= SvgFrequencyUnit.Hz && unit <= SvgFrequencyUnit.KHz;
    }

    /// <summary>Tests whether a resolution unit is known.</summary>
    /// <param name="unit">The unit to test.</param>
    /// <returns><c>true</c> for a known unit.</returns>
    public static bool IsResolutionUnit(SvgResolutionUnit unit)
    {
        return unit >= SvgResolutionUnit.Dpi && unit <= SvgResolutionUnit.Dppx;
    }

    /// <summary>Finds an angle suffix.</summary>
    /// <param name="text">The suffix text.</param>
    /// <param name="allowUnitless">Whether empty text means unitless degrees.</param>
    /// <returns>The unit value, or <c>-1</c>.</returns>
    public static int FindAngle(string text, bool allowUnitless)
    {
        if (text.Length == 0)
        {
            return allowUnitless ? (int)SvgAngleUnit.Number : -1;
        }
        if (Equal(text, "deg"))
        {
            return (int)SvgAngleUnit.Deg;
        }
        if (Equal(text, "grad"))
        {
            return (int)SvgAngleUnit.Grad;
        }
        if (Equal(text, "rad"))
        {
            return (int)SvgAngleUnit.Rad;
        }
        if (Equal(text, "turn"))
        {
            return (int)SvgAngleUnit.Turn;
        }
        return -1;
    }

    /// <summary>Finds a time suffix.</summary>
    /// <param name="text">The suffix text.</param>
    /// <param name="allowUnitless">Whether empty text means seconds.</param>
    /// <returns>The unit value, or <c>-1</c>.</returns>
    public static int FindTime(string text, bool allowUnitless)
    {
        if (text.Length == 0)
        {
            return allowUnitless ? (int)SvgTimeUnit.Seconds : -1;
        }
        if (Equal(text, "s"))
        {
            return (int)SvgTimeUnit.Seconds;
        }
        if (Equal(text, "ms"))
        {
            return (int)SvgTimeUnit.Milliseconds;
        }
        return -1;
    }

    /// <summary>Finds a frequency suffix.</summary>
    /// <param name="text">The suffix text.</param>
    /// <returns>The unit value, or <c>-1</c>.</returns>
    public static int FindFrequency(string text)
    {
        if (Equal(text, "hz"))
        {
            return (int)SvgFrequencyUnit.Hz;
        }
        if (Equal(text, "khz"))
        {
            return (int)SvgFrequencyUnit.KHz;
        }
        return -1;
    }

    /// <summary>Finds a resolution suffix, including the CSS <c>x</c> alias.</summary>
    /// <param name="text">The suffix text.</param>
    /// <returns>The unit value, or <c>-1</c>.</returns>
    public static int FindResolution(string text)
    {
        if (Equal(text, "dpi"))
        {
            return (int)SvgResolutionUnit.Dpi;
        }
        if (Equal(text, "dpcm"))
        {
            return (int)SvgResolutionUnit.Dpcm;
        }
        if (Equal(text, "dppx") || Equal(text, "x"))
        {
            return (int)SvgResolutionUnit.Dppx;
        }
        return -1;
    }

    /// <summary>Returns canonical angle unit text.</summary>
    /// <param name="unit">The angle unit.</param>
    /// <returns>The canonical suffix.</returns>
    public static string AngleText(SvgAngleUnit unit)
    {
        switch (unit)
        {
            case SvgAngleUnit.Number:
                return "";
            case SvgAngleUnit.Deg:
                return "deg";
            case SvgAngleUnit.Grad:
                return "grad";
            case SvgAngleUnit.Rad:
                return "rad";
            case SvgAngleUnit.Turn:
                return "turn";
            default:
                return "";
        }
    }

    /// <summary>Returns canonical time unit text.</summary>
    /// <param name="unit">The time unit.</param>
    /// <returns>The canonical suffix.</returns>
    public static string TimeText(SvgTimeUnit unit)
    {
        return unit == SvgTimeUnit.Milliseconds ? "ms" : "s";
    }

    /// <summary>Returns canonical frequency unit text.</summary>
    /// <param name="unit">The frequency unit.</param>
    /// <returns>The canonical suffix.</returns>
    public static string FrequencyText(SvgFrequencyUnit unit)
    {
        return unit == SvgFrequencyUnit.KHz ? "kHz" : "Hz";
    }

    /// <summary>Returns canonical resolution unit text.</summary>
    /// <param name="unit">The resolution unit.</param>
    /// <returns>The canonical suffix.</returns>
    public static string ResolutionText(SvgResolutionUnit unit)
    {
        if (unit == SvgResolutionUnit.Dpcm)
        {
            return "dpcm";
        }
        if (unit == SvgResolutionUnit.Dppx)
        {
            return "dppx";
        }
        return "dpi";
    }

    static bool Equal(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Provides shortest invariant number text for scalar typed values.</summary>
internal static class SvgScalarUnitText
{
    /// <summary>Formats one finite scalar number.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>The shortest invariant text.</returns>
    public static string Number(double value)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatShortest(value);
        return result.Text;
    }
}
