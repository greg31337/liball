namespace svg;

/// <summary>Parses bounded escaped function values.</summary>
public class SvgFunctionValueParser : ISvgFunctionValueParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFunctionValueParser()
    {
    }

    /// <summary>Parses one complete function while preserving its raw content.</summary>
    /// <param name="value">The function text.</param>
    /// <param name="maxNameCharacters">The maximum raw name length.</param>
    /// <param name="maxContentCharacters">The maximum raw content length.</param>
    /// <returns>A parsed function or stable failure.</returns>
    public SvgFunctionValueParseResult Parse(string? value, int maxNameCharacters, int maxContentCharacters)
    {
        if (value == null || maxNameCharacters < 1 || maxContentCharacters < 0) return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.InvalidArgument, 0);
        int start = SvgIriLexical.SkipWhitespace(value, 0);
        if (start == value.Length) return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.ExpectedValue, start);
        SvgSpanReader reader = new SvgSpanReader(value, maxNameCharacters);
        reader.SetPosition(start);
        SvgSpanTokenResult name = reader.ReadToken(IsNameCharacter, true);
        if (!name.Success)
        {
            SvgIriParseError error = name.Error == SvgSpanReadError.TokenTooLong ? SvgIriParseError.TokenTooLong : SvgIriParseError.InvalidEscape;
            return new SvgFunctionValueParser().FunctionFailure(error, name.ErrorOffset);
        }
        string decodedName = name.GetValue();
        char firstNameCharacter = decodedName.Length == 0 ? '\0' : decodedName[0];
        bool badStart = firstNameCharacter >= '0' && firstNameCharacter <= '9' || firstNameCharacter == '-' && (decodedName.Length == 1 || decodedName[1] >= '0' && decodedName[1] <= '9');
        if (badStart) return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.InvalidFunction, start);
        if (name.RawText.Length == 0 || reader.IsEnd() || reader.PeekCharacter().Value != '(') return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.InvalidFunction, reader.GetPosition());
        int open = reader.GetPosition();
        int close = SvgIriLexical.FindMatchingClose(value, open);
        if (close < 0) return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.ExpectedClose, value.Length);
        if (close - open - 1 > maxContentCharacters) return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.TokenTooLong, open + 1 + maxContentCharacters);
        int trailing = SvgIriLexical.SkipWhitespace(value, close + 1);
        if (trailing != value.Length) return new SvgFunctionValueParser().FunctionFailure(SvgIriParseError.TrailingCharacters, trailing);
        string content = value.Substring(open + 1, close - open - 1);
        SvgFunctionValue function = new SvgFunctionValue(decodedName, content, value, start, open + 1, close + 1);
        return new SvgFunctionValueParseResult(true, SvgIriParseError.None, function, -1);
    }
bool IsNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '_' || value >= 128;
    }
SvgFunctionValueParseResult FunctionFailure(SvgIriParseError error, int offset)
    {
        return new SvgFunctionValueParseResult(false, error, null, offset);
    }
}
