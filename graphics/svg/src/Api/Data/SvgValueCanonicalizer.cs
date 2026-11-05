namespace svg;

/// <summary>Configures formatting and bounded rounding for one SVG value family.</summary>
public class SvgValueFormatOptions
{
    /// <summary>The serialization mode.</summary>
    public SvgValueFormatMode Mode;
    /// <summary>Whether fractional-digit rounding is attempted.</summary>
    public bool UseFractionalPrecision;
    /// <summary>The maximum fractional digit count.</summary>
    public int FractionalDigits;
    /// <summary>The greatest permitted absolute change to an individual numeric component.</summary>
    public double MaximumAbsoluteError;

    /// <summary>Creates exact canonical formatting settings.</summary>
    public SvgValueFormatOptions()
    {
        Mode = SvgValueFormatMode.Canonical;
        UseFractionalPrecision = false;
        FractionalDigits = 6;
        MaximumAbsoluteError = 0;
    }

    /// <summary>Creates explicit formatting and bounded rounding settings.</summary>
    /// <param name="mode">The serialization mode.</param>
    /// <param name="useFractionalPrecision">Whether rounding is attempted.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <param name="maximumAbsoluteError">The permitted absolute change per component.</param>
    /// <returns>New explicit formatting settings.</returns>
    internal static SvgValueFormatOptions Create(SvgValueFormatMode mode, bool useFractionalPrecision, int fractionalDigits, double maximumAbsoluteError)
    {
        SvgValueFormatOptions result = new SvgValueFormatOptions();
        result.Mode = mode;
        result.UseFractionalPrecision = useFractionalPrecision;
        result.FractionalDigits = fractionalDigits;
        result.MaximumAbsoluteError = maximumAbsoluteError;
        return result;
    }

    internal SvgValueFormatOptions Copy()
    {
        return Create(Mode, UseFractionalPrecision, FractionalDigits, MaximumAbsoluteError);
    }
}

/// <summary>Configures independent number, color, list, path, and transform formatting.</summary>
public class SvgCanonicalizeOptions
{
    /// <summary>Scalar number, length, and angle settings.</summary>
    public SvgValueFormatOptions Numbers;
    /// <summary>CSS color settings.</summary>
    public SvgValueFormatOptions Colors;
    /// <summary>Typed list and viewBox settings.</summary>
    public SvgValueFormatOptions Lists;
    /// <summary>Path-data settings.</summary>
    public SvgValueFormatOptions Paths;
    /// <summary>Transform-list settings.</summary>
    public SvgValueFormatOptions Transforms;

    /// <summary>Creates exact canonical settings for every value family.</summary>
    public SvgCanonicalizeOptions()
    {
        Numbers = new SvgValueFormatOptions();
        Colors = new SvgValueFormatOptions();
        Lists = new SvgValueFormatOptions();
        Paths = new SvgValueFormatOptions();
        Transforms = new SvgValueFormatOptions();
    }

    internal SvgCanonicalizeOptions Copy()
    {
        SvgCanonicalizeOptions copy = new SvgCanonicalizeOptions();
        copy.Numbers = Numbers == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : Numbers.Copy();
        copy.Colors = Colors == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : Colors.Copy();
        copy.Lists = Lists == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : Lists.Copy();
        copy.Paths = Paths == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : Paths.Copy();
        copy.Transforms = Transforms == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : Transforms.Copy();
        return copy;
    }
}
