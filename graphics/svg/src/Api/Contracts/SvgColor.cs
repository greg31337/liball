namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgColorParser"/>.</summary>
public interface ISvgColorParser
{
    /// <summary>Parses one complete CSS color value.</summary>
    /// <param name="value">The color text.</param>
    /// <param name="maxTokenCharacters">The maximum characters in one name, number, or hexadecimal token.</param>
    /// <returns>A parsed color or a stable failure.</returns>
    SvgColorParseResult Parse(string? value, int maxTokenCharacters);
}
