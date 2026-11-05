namespace svg;

/// <summary>Parses bounded SVG path data with command-level recovery.</summary>
public class SvgPathParser : ISvgPathParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathParser()
    {
    }

    /// <summary>Parses every SVG path command and retains valid commands after recoverable failures.</summary>
    /// <param name="value">The authored path data.</param>
    /// <param name="maxSegments">The maximum parsed command count.</param>
    /// <param name="maxTokenCharacters">The maximum character count of one number.</param>
    /// <returns>A source-preserving path parse result.</returns>
    public SvgPathParseResult Parse(string? value, int maxSegments, int maxTokenCharacters)
    {
        if (value == null || maxSegments < 1 || maxTokenCharacters < 1)
        {
            return new SvgPathParseResult(false, SvgPathParseError.InvalidArgument, null, 0, 0);
        }
        SvgPathParserState state = new SvgPathParserState(value, maxSegments, maxTokenCharacters);
        return state.Run();
    }
}
