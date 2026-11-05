namespace svg;

/// <summary>Configures canonical or minified SVG path serialization.</summary>
public class SvgPathFormatOptions
{
    /// <summary>The output style.</summary>
    public SvgPathFormatMode Mode;
    /// <summary>Whether numbers are rounded to a maximum fractional precision.</summary>
    public bool UseFractionalPrecision;
    /// <summary>The maximum fractional digit count when precision is enabled.</summary>
    public int FractionalDigits;

    /// <summary>Creates canonical shortest-round-trip formatting options.</summary>
    public SvgPathFormatOptions()
    {
        Mode = SvgPathFormatMode.Canonical;
        UseFractionalPrecision = false;
        FractionalDigits = 6;
    }

    /// <summary>Creates explicit path formatting options.</summary>
    /// <param name="mode">The output style.</param>
    /// <param name="useFractionalPrecision">Whether to apply fractional precision.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <returns>New explicit path formatting options.</returns>
    internal static SvgPathFormatOptions Create(SvgPathFormatMode mode, bool useFractionalPrecision, int fractionalDigits)
    {
        SvgPathFormatOptions result = new SvgPathFormatOptions();
        result.Mode = mode;
        result.UseFractionalPrecision = useFractionalPrecision;
        result.FractionalDigits = fractionalDigits;
        return result;
    }
}

/// <summary>Contains a non-throwing SVG path formatting result.</summary>
public class SvgPathFormatResult
{
    /// <summary>Whether formatting succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgPathFormatError Error;
    /// <summary>The formatted path data, or an empty string after failure.</summary>
    public string Text;

    /// <summary>Creates a path formatting result.</summary>
    /// <param name="success">Whether formatting succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="text">The formatted text.</param>
    public SvgPathFormatResult(bool success, SvgPathFormatError error, string text)
    {
        Success = success;
        Error = error;
        Text = text;
    }
}
