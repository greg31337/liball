namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathParser"/>.</summary>
public interface ISvgPathParser
{
    /// <summary>Parses every SVG path command and retains valid commands after recoverable failures.</summary>
    /// <param name="value">The authored path data.</param>
    /// <param name="maxSegments">The maximum parsed command count.</param>
    /// <param name="maxTokenCharacters">The maximum character count of one number.</param>
    /// <returns>A source-preserving path parse result.</returns>
    SvgPathParseResult Parse(string? value, int maxSegments, int maxTokenCharacters);
}
