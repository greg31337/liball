namespace svg;

/// <summary>Configures bounded standalone value parsing and prefix recovery.</summary>
public class SvgValueParseOptions
{
    /// <summary>The maximum number of returned list items or points.</summary>
    public int MaxItems;
    /// <summary>The maximum character count of one number or integer token.</summary>
    public int MaxTokenCharacters;
    /// <summary>Whether a whitespace-only list is valid.</summary>
    public bool AllowEmpty;
    /// <summary>The behavior for a valid prefix followed by malformed input.</summary>
    public SvgValueRecoveryMode RecoveryMode;

    /// <summary>Creates strict, nonempty parsing settings with conservative bounds.</summary>
    public SvgValueParseOptions()
    {
        MaxItems = 1024;
        MaxTokenCharacters = 128;
        AllowEmpty = false;
        RecoveryMode = SvgValueRecoveryMode.Strict;
    }
}

/// <summary>Contains a finite standalone number or coordinate parse result.</summary>
public class SvgValueNumberResult
{
    /// <summary>Whether the complete input was valid.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgValueParseError Error;
    /// <summary>The parsed finite value, or zero when unavailable.</summary>
    public double Value;
    /// <summary>The first non-whitespace input offset.</summary>
    public int StartOffset;
    /// <summary>The next input offset after success or recovery.</summary>
    public int NextOffset;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;
    /// <summary>Whether <see cref="Value"/> contains a valid prefix of invalid input.</summary>
    public bool Recovered;

    /// <summary>Creates a finite standalone value result.</summary>
    /// <param name="success">Whether the complete input was valid.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="value">The parsed or recovered value.</param>
    /// <param name="startOffset">The first value offset.</param>
    /// <param name="nextOffset">The next input offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="recovered">Whether a valid prefix was retained.</param>
    public SvgValueNumberResult(bool success, SvgValueParseError error, double value, int startOffset, int nextOffset, int errorOffset, bool recovered)
    {
        Success = success; Error = error; Value = value; StartOffset = startOffset; NextOffset = nextOffset; ErrorOffset = errorOffset; Recovered = recovered;
    }
}

/// <summary>Contains a signed 32-bit standalone integer parse result.</summary>
public class SvgValueIntegerResult
{
    /// <summary>Whether the complete input was valid.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgValueParseError Error;
    /// <summary>The parsed integer, or zero when unavailable.</summary>
    public int Value;
    /// <summary>The first non-whitespace input offset.</summary>
    public int StartOffset;
    /// <summary>The next input offset after success or recovery.</summary>
    public int NextOffset;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;
    /// <summary>Whether <see cref="Value"/> contains a valid prefix of invalid input.</summary>
    public bool Recovered;

    /// <summary>Creates a standalone integer result.</summary>
    /// <param name="success">Whether the complete input was valid.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="value">The parsed or recovered value.</param>
    /// <param name="startOffset">The first value offset.</param>
    /// <param name="nextOffset">The next input offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="recovered">Whether a valid prefix was retained.</param>
    public SvgValueIntegerResult(bool success, SvgValueParseError error, int value, int startOffset, int nextOffset, int errorOffset, bool recovered)
    {
        Success = success; Error = error; Value = value; StartOffset = startOffset; NextOffset = nextOffset; ErrorOffset = errorOffset; Recovered = recovered;
    }
}

/// <summary>Contains one required finite number and one optional finite number.</summary>
public class SvgValueOptionalPairResult
{
    /// <summary>Whether the complete input was valid.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgValueParseError Error;
    /// <summary>The required first value, or zero when unavailable.</summary>
    public double First;
    /// <summary>The optional second value, or zero when absent.</summary>
    public double Second;
    /// <summary>Whether a second value was parsed.</summary>
    public bool HasSecond;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;
    /// <summary>Whether a valid prefix was retained after failure.</summary>
    public bool Recovered;

    /// <summary>Creates an optional-pair result.</summary>
    /// <param name="success">Whether the complete input was valid.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <param name="hasSecond">Whether the second value is present.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="recovered">Whether a valid prefix was retained.</param>
    public SvgValueOptionalPairResult(bool success, SvgValueParseError error, double first, double second, bool hasSecond, int errorOffset, bool recovered)
    {
        Success = success; Error = error; First = first; Second = second; HasSecond = hasSecond; ErrorOffset = errorOffset; Recovered = recovered;
    }
}

/// <summary>Contains a bounded finite-number or coordinate-list parse result.</summary>
public class SvgValueNumberListResult
{
    /// <summary>Whether the complete input was valid.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgValueParseError Error;
    /// <summary>The parsed values or recovered prefix.</summary>
    public double[] Values;
    /// <summary>The first list-item offset.</summary>
    public int StartOffset;
    /// <summary>The next unread input offset.</summary>
    public int NextOffset;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;
    /// <summary>Whether a valid prefix was retained after failure.</summary>
    public bool Recovered;

    /// <summary>Creates a bounded number-list result.</summary>
    /// <param name="success">Whether the complete input was valid.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="values">The parsed values or prefix.</param>
    /// <param name="startOffset">The first item offset.</param>
    /// <param name="nextOffset">The next unread offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="recovered">Whether a valid prefix was retained.</param>
    public SvgValueNumberListResult(bool success, SvgValueParseError error, double[] values, int startOffset, int nextOffset, int errorOffset, bool recovered)
    {
        Success = success; Error = error; Values = values; StartOffset = startOffset; NextOffset = nextOffset; ErrorOffset = errorOffset; Recovered = recovered;
    }
}

/// <summary>Contains a bounded signed 32-bit integer-list parse result.</summary>
public class SvgValueIntegerListResult
{
    /// <summary>Whether the complete input was valid.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgValueParseError Error;
    /// <summary>The parsed values or recovered prefix.</summary>
    public int[] Values;
    /// <summary>The first list-item offset.</summary>
    public int StartOffset;
    /// <summary>The next unread input offset.</summary>
    public int NextOffset;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;
    /// <summary>Whether a valid prefix was retained after failure.</summary>
    public bool Recovered;

    /// <summary>Creates a bounded integer-list result.</summary>
    /// <param name="success">Whether the complete input was valid.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="values">The parsed values or prefix.</param>
    /// <param name="startOffset">The first item offset.</param>
    /// <param name="nextOffset">The next unread offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="recovered">Whether a valid prefix was retained.</param>
    public SvgValueIntegerListResult(bool success, SvgValueParseError error, int[] values, int startOffset, int nextOffset, int errorOffset, bool recovered)
    {
        Success = success; Error = error; Values = values; StartOffset = startOffset; NextOffset = nextOffset; ErrorOffset = errorOffset; Recovered = recovered;
    }
}

/// <summary>Contains a bounded SVG points-list parse result.</summary>
public class SvgValuePointListResult
{
    /// <summary>Whether the complete input was valid.</summary>
    public bool Success;
    /// <summary>The stable parsing failure.</summary>
    public SvgValueParseError Error;
    /// <summary>The complete parsed points or recovered prefix.</summary>
    public System.Collections.Generic.List<SvgPoint> Points;
    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;
    /// <summary>Whether a valid prefix was retained after failure.</summary>
    public bool Recovered;

    /// <summary>Creates a bounded point-list result.</summary>
    /// <param name="success">Whether the complete input was valid.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="points">The complete points or prefix.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="recovered">Whether a valid prefix was retained.</param>
    public SvgValuePointListResult(bool success, SvgValueParseError error, System.Collections.Generic.List<SvgPoint> points, int errorOffset, bool recovered)
    {
        Success = success; Error = error; Points = points; ErrorOffset = errorOffset; Recovered = recovered;
    }
}

/// <summary>Contains deterministic standalone value formatting text and destination counts.</summary>
public class SvgValueFormatResult
{
    /// <summary>Whether formatting and any requested destination copy succeeded.</summary>
    public bool Success;
    /// <summary>The stable formatting failure.</summary>
    public SvgValueFormatError Error;
    /// <summary>The complete formatted text, or an empty string for invalid input.</summary>
    public string Text;
    /// <summary>The required destination character count.</summary>
    public int RequiredLength;
    /// <summary>The copied destination character count, or zero after failure.</summary>
    public int WrittenLength;

    /// <summary>Creates a deterministic formatting result.</summary>
    /// <param name="success">Whether formatting and copying succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="text">The complete formatted text.</param>
    /// <param name="requiredLength">The required destination length.</param>
    /// <param name="writtenLength">The copied destination length.</param>
    public SvgValueFormatResult(bool success, SvgValueFormatError error, string text, int requiredLength, int writtenLength)
    {
        Success = success; Error = error; Text = text; RequiredLength = requiredLength; WrittenLength = writtenLength;
    }
}
