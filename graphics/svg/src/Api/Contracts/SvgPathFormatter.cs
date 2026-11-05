namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathFormatter"/>.</summary>
public interface ISvgPathFormatter
{
    /// <summary>Formats a path using explicit options.</summary>
    /// <param name="path">The valid path data.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>Formatted text or a stable failure.</returns>
    SvgPathFormatResult Format(SvgPathData path, SvgPathFormatOptions options);
    /// <summary>Formats a path canonically with exact shortest-round-trip values.</summary>
    /// <param name="path">The valid path data.</param>
    /// <returns>Canonical path text or a stable failure.</returns>
    SvgPathFormatResult FormatCanonicalWithPath(SvgPathData path);
    /// <summary>Formats a path canonically with a maximum fractional precision.</summary>
    /// <param name="path">The valid path data.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <returns>Canonical rounded path text or a stable failure.</returns>
    SvgPathFormatResult FormatCanonicalWithPathAndFractionalDigits(SvgPathData path, int fractionalDigits);
    /// <summary>Formats a path with compact exact numbers and separators.</summary>
    /// <param name="path">The valid path data.</param>
    /// <returns>Minified path text or a stable failure.</returns>
    SvgPathFormatResult FormatMinifiedWithPath(SvgPathData path);
    /// <summary>Formats a path compactly with a maximum fractional precision.</summary>
    /// <param name="path">The valid path data.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <returns>Minified rounded path text or a stable failure.</returns>
    SvgPathFormatResult FormatMinifiedWithPathAndFractionalDigits(SvgPathData path, int fractionalDigits);
}
