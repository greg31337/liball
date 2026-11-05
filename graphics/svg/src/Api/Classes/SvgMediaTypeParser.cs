namespace svg;

/// <summary>Parses bounded media types and parameters.</summary>
public class SvgMediaTypeParser : ISvgMediaTypeParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgMediaTypeParser()
    {
    }

    /// <summary>Parses a complete media type.</summary>
    /// <param name="value">The media type text.</param>
    /// <param name="maxParameters">The maximum parameter count.</param>
    /// <param name="maxTokenCharacters">The maximum token or decoded quoted value length.</param>
    /// <returns>A parsed media type or stable failure.</returns>
    public SvgMediaTypeParseResult Parse(string value, int maxParameters, int maxTokenCharacters)
    {
        if (value == null || maxParameters < 0 || maxTokenCharacters < 1) return new SvgMediaTypeParser().Failure(SvgIriParseError.InvalidArgument, 0);
        int start = SvgIriLexical.SkipWhitespace(value, 0);
        int end = SvgIriLexical.TrimEnd(value, start);
        if (start == end) return new SvgMediaTypeParser().Failure(SvgIriParseError.ExpectedValue, start);
        int position = start;
        int typeStart = position;
        while (position < end && SvgIriLexical.IsMimeTokenCharacter(value[position])) position = position + 1;
        if (position == typeStart || position - typeStart > maxTokenCharacters) return new SvgMediaTypeParser().Failure(position - typeStart > maxTokenCharacters ? SvgIriParseError.TokenTooLong : SvgIriParseError.InvalidMediaType, position == typeStart ? position : typeStart + maxTokenCharacters);
        string type = value.Substring(typeStart, position - typeStart);
        if (position >= end || value[position] != '/') return new SvgMediaTypeParser().Failure(SvgIriParseError.InvalidMediaType, position);
        position = position + 1;
        int subtypeStart = position;
        while (position < end && SvgIriLexical.IsMimeTokenCharacter(value[position])) position = position + 1;
        if (position == subtypeStart || position - subtypeStart > maxTokenCharacters) return new SvgMediaTypeParser().Failure(position - subtypeStart > maxTokenCharacters ? SvgIriParseError.TokenTooLong : SvgIriParseError.InvalidMediaType, position == subtypeStart ? position : subtypeStart + maxTokenCharacters);
        string subtype = value.Substring(subtypeStart, position - subtypeStart);
        System.Collections.Generic.List<SvgMediaTypeParameter> parameters = new System.Collections.Generic.List<SvgMediaTypeParameter>();
        position = SvgIriLexical.SkipWhitespace(value, position);
        while (position < end)
        {
            if (value[position] != ';') return new SvgMediaTypeParser().Failure(SvgIriParseError.InvalidMediaType, position);
            if (parameters.Count >= maxParameters) return new SvgMediaTypeParser().Failure(SvgIriParseError.ItemLimitExceeded, position);
            int parameterStart = position;
            position = SvgIriLexical.SkipWhitespace(value, position + 1);
            int nameStart = position;
            while (position < end && SvgIriLexical.IsMimeTokenCharacter(value[position])) position = position + 1;
            if (position == nameStart || position - nameStart > maxTokenCharacters) return new SvgMediaTypeParser().Failure(position - nameStart > maxTokenCharacters ? SvgIriParseError.TokenTooLong : SvgIriParseError.InvalidMediaType, position == nameStart ? position : nameStart + maxTokenCharacters);
            string name = value.Substring(nameStart, position - nameStart).ToLowerInvariant();
            position = SvgIriLexical.SkipWhitespace(value, position);
            if (position >= end || value[position] != '=') return new SvgMediaTypeParser().Failure(SvgIriParseError.InvalidMediaType, position);
            position = SvgIriLexical.SkipWhitespace(value, position + 1);
            SvgDecodedTextResult parameter = SvgIriLexical.ReadMimeValue(value, position, end, maxTokenCharacters);
            if (!parameter.Success) return new SvgMediaTypeParser().Failure(parameter.Error, parameter.ErrorOffset);
            position = parameter.EndOffset;
            parameters.Add(new SvgMediaTypeParameter(name, parameter.Value, parameterStart, position));
            position = SvgIriLexical.SkipWhitespace(value, position);
        }
        SvgMediaType mediaType = new SvgMediaType(type, subtype, parameters, value, start, end);
        return new SvgMediaTypeParseResult(true, SvgIriParseError.None, mediaType, -1);
    }
SvgMediaTypeParseResult Failure(SvgIriParseError error, int offset)
    {
        return new SvgMediaTypeParseResult(false, error, null, offset);
    }
}
