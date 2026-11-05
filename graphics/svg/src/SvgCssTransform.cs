namespace svg;

/// <summary>Defines stable CSS transform parsing failures.</summary>
internal enum SvgCssTransformParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>A transform function or value was required.</summary>
    ExpectedValue = 1,
    /// <summary>A transform function name was unknown.</summary>
    UnknownFunction = 2,
    /// <summary>An opening parenthesis was required.</summary>
    ExpectedOpenParenthesis = 3,
    /// <summary>A closing parenthesis was required.</summary>
    ExpectedCloseParenthesis = 4,
    /// <summary>An argument was absent or malformed.</summary>
    InvalidArgumentValue = 5,
    /// <summary>A function had an invalid argument count.</summary>
    InvalidArgumentCount = 6,
    /// <summary>A function or argument token exceeded its bound.</summary>
    TokenTooLong = 7,
    /// <summary>The configured function count was exceeded.</summary>
    FunctionLimitExceeded = 8,
    /// <summary>Unexpected punctuation separated transform functions.</summary>
    UnexpectedSeparator = 9,
    /// <summary>Trailing text followed the none keyword.</summary>
    TrailingCharacters = 10,
    /// <summary>A parser argument was invalid.</summary>
    InvalidParserArgument = 11
}

/// <summary>Contains a CSS transform parse result with optional recovery.</summary>
internal class SvgCssTransformParseResult
{
    /// <summary>Indicates whether a complete or recovered value is available.</summary>
    public bool Success;
    /// <summary>Whether at least one invalid function was skipped.</summary>
    public bool Recovered;
    /// <summary>The first parse failure kind.</summary>
    public SvgCssTransformParseError Error;
    /// <summary>The exact first failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The parsed value or valid prefix.</summary>
    public SvgCssTransformList TransformList;

    /// <summary>Creates a CSS transform parse result.</summary>
    /// <param name="success">Whether a usable value is available.</param>
    /// <param name="recovered">Whether recovery skipped invalid syntax.</param>
    /// <param name="error">The first failure kind.</param>
    /// <param name="errorOffset">The first exact failure offset.</param>
    /// <param name="transformList">The parsed value or valid prefix.</param>
    public SvgCssTransformParseResult(bool success, bool recovered, SvgCssTransformParseError error, int errorOffset, SvgCssTransformList transformList)
    {
        Success = success;
        Recovered = recovered;
        Error = error;
        ErrorOffset = errorOffset;
        TransformList = transformList;
    }
}

/// <summary>Contains a transform-origin parse result.</summary>
internal class SvgTransformOriginParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssTransformParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The parsed origin, or <c>null</c>.</summary>
    public SvgTransformOrigin? Origin;

    /// <summary>Creates a transform-origin parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="origin">The parsed origin, or <c>null</c>.</param>
    public SvgTransformOriginParseResult(bool success, SvgCssTransformParseError error, int errorOffset, SvgTransformOrigin? origin)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        Origin = origin;
    }
}

/// <summary>Contains a transform-box parse result.</summary>
internal class SvgTransformBoxParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssTransformParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The parsed transform-box value.</summary>
    public SvgTransformBox TransformBox;

    /// <summary>Creates a transform-box parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="transformBox">The parsed box value.</param>
    public SvgTransformBoxParseResult(bool success, SvgCssTransformParseError error, int errorOffset, SvgTransformBox transformBox)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        TransformBox = transformBox;
    }
}

/// <summary>Parses CSS transform, transform-origin, and transform-box values.</summary>
internal static class SvgCssTransformParser
{
    /// <summary>Parses a CSS transform value with optional invalid-function recovery.</summary>
    /// <param name="value">The CSS transform text.</param>
    /// <param name="maxFunctions">The maximum function count.</param>
    /// <param name="maxTokenCharacters">The maximum function or argument token length.</param>
    /// <param name="recoverInvalidFunctions">Whether invalid functions are skipped.</param>
    /// <returns>A complete, recovered, or failed parse result.</returns>
    public static SvgCssTransformParseResult ParseTransform(string? value, int maxFunctions, int maxTokenCharacters, bool recoverInvalidFunctions)
    {
        System.Collections.Generic.List<SvgCssTransform> items = new System.Collections.Generic.List<SvgCssTransform>();
        if (value == null || maxFunctions < 1 || maxTokenCharacters < 1)
        {
            return TransformFailure(SvgCssTransformParseError.InvalidParserArgument, 0, items);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipCssWhitespace();
        if (reader.IsEnd())
        {
            return TransformFailure(SvgCssTransformParseError.ExpectedValue, reader.GetPosition(), items);
        }
        int firstOffset = reader.GetPosition();
        SvgSpanTokenResult first = reader.ReadToken(IsAsciiLetter, false);
        if (!first.Success)
        {
            return TransformFailure(TokenError(first), first.ErrorOffset, items);
        }
        if (Equal(first.GetValue(), "none"))
        {
            reader.SkipCssWhitespace();
            if (!reader.IsEnd())
            {
                return TransformFailure(SvgCssTransformParseError.TrailingCharacters, reader.GetPosition(), items);
            }
            return new SvgCssTransformParseResult(true, false, SvgCssTransformParseError.None, -1, new SvgCssTransformList(true, items));
        }
        reader.SetPosition(firstOffset);

        bool recovered = false;
        int encountered = 0;
        SvgCssTransformParseError firstError = SvgCssTransformParseError.None;
        int firstErrorOffset = -1;
        while (!reader.IsEnd())
        {
            if (encountered >= maxFunctions)
            {
                return TransformFailure(SvgCssTransformParseError.FunctionLimitExceeded, reader.GetPosition(), items);
            }
            encountered = encountered + 1;
            SvgCssFunctionParseResult function = ParseFunction(reader, maxTokenCharacters);
            if (!function.Success)
            {
                if (!recoverInvalidFunctions)
                {
                    return TransformFailure(function.Error, function.ErrorOffset, items);
                }
                if (!recovered)
                {
                    firstError = function.Error;
                    firstErrorOffset = function.ErrorOffset;
                }
                recovered = true;
                if (!FunctionEnded(reader))
                {
                    RecoverFunction(reader);
                }
            }
            else
            {
                items.Add((function.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            }
            reader.SkipCssWhitespace();
            if (!reader.IsEnd() && reader.PeekCharacter().Value == ',')
            {
                if (!recoverInvalidFunctions)
                {
                    return TransformFailure(SvgCssTransformParseError.UnexpectedSeparator, reader.GetPosition(), items);
                }
                if (!recovered)
                {
                    firstError = SvgCssTransformParseError.UnexpectedSeparator;
                    firstErrorOffset = reader.GetPosition();
                }
                recovered = true;
                reader.ReadCharacter();
                reader.SkipCssWhitespace();
            }
        }
        SvgCssTransformList list = new SvgCssTransformList(false, items);
        return new SvgCssTransformParseResult(true, recovered, firstError, firstErrorOffset, list);
    }

    /// <summary>Parses a CSS transform-origin value.</summary>
    /// <param name="value">The transform-origin text.</param>
    /// <param name="maxTokenCharacters">The maximum component token length.</param>
    /// <returns>A typed origin or stable failure.</returns>
    public static SvgTransformOriginParseResult ParseOrigin(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return OriginFailure(SvgCssTransformParseError.InvalidParserArgument, 0);
        }
        SvgCssTokenListResult tokens = TokenizeWhitespace(value, maxTokenCharacters, 3);
        if (!tokens.Success)
        {
            return OriginFailure(tokens.Error, tokens.ErrorOffset);
        }
        if (tokens.Tokens.Count < 1 || tokens.Tokens.Count > 3)
        {
            return OriginFailure(SvgCssTransformParseError.InvalidArgumentCount, tokens.EndOffset);
        }

        SvgLength? x = null;
        SvgLength? y = null;
        SvgLength? z = null;
        if (tokens.Tokens.Count == 1)
        {
            SvgCssArgumentToken token = tokens.Tokens[0];
            if (IsVerticalKeyword(token.Text))
            {
                x = Percentage(50);
                y = KeywordLength(token.Text);
            }
            else
            {
                SvgCssLengthTokenResult horizontal = ParsePositionToken(token);
                if (!horizontal.Success)
                {
                    return OriginFailure(horizontal.Error, horizontal.ErrorOffset);
                }
                x = horizontal.Length;
                y = Percentage(50);
            }
        }
        else
        {
            SvgCssArgumentToken firstToken = tokens.Tokens[0];
            SvgCssArgumentToken secondToken = tokens.Tokens[1];
            bool verticalFirst = IsVerticalKeyword(firstToken.Text) && !IsVerticalKeyword(secondToken.Text) || IsCenterKeyword(firstToken.Text) && IsHorizontalKeyword(secondToken.Text);
            SvgCssLengthTokenResult horizontal = ParsePositionToken(verticalFirst ? secondToken : firstToken);
            SvgCssLengthTokenResult vertical = ParsePositionToken(verticalFirst ? firstToken : secondToken);
            if (!horizontal.Success)
            {
                return OriginFailure(horizontal.Error, horizontal.ErrorOffset);
            }
            if (!vertical.Success)
            {
                return OriginFailure(vertical.Error, vertical.ErrorOffset);
            }
            if (!ValidHorizontalKeyword(verticalFirst ? secondToken.Text : firstToken.Text) || !ValidVerticalKeyword(verticalFirst ? firstToken.Text : secondToken.Text))
            {
                int offset = !ValidHorizontalKeyword(verticalFirst ? secondToken.Text : firstToken.Text) ? horizontal.SourceOffset : vertical.SourceOffset;
                return OriginFailure(SvgCssTransformParseError.InvalidArgumentValue, offset);
            }
            x = horizontal.Length;
            y = vertical.Length;
        }
        if (tokens.Tokens.Count == 3)
        {
            SvgCssLengthTokenResult depth = ParseLengthToken(tokens.Tokens[2], false);
            if (!depth.Success || !SvgCssTransformRules.ValidDepthLength(depth.Length))
            {
                int offset = depth.Success ? tokens.Tokens[2].Offset : depth.ErrorOffset;
                return OriginFailure(SvgCssTransformParseError.InvalidArgumentValue, offset);
            }
            z = depth.Length;
        }
        return new SvgTransformOriginParseResult(true, SvgCssTransformParseError.None, -1, new SvgTransformOrigin((x ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (y ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), z, z != null));
    }

    /// <summary>Parses a CSS transform-box keyword.</summary>
    /// <param name="value">The transform-box text.</param>
    /// <param name="maxTokenCharacters">The maximum keyword length.</param>
    /// <returns>A transform-box value or stable failure.</returns>
    public static SvgTransformBoxParseResult ParseTransformBox(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return new SvgTransformBoxParseResult(false, SvgCssTransformParseError.InvalidParserArgument, 0, SvgTransformBox.ViewBox);
        }
        string trimmed = value.Trim();
        int start = value.IndexOf(trimmed, System.StringComparison.Ordinal);
        if (trimmed.Length == 0)
        {
            return new SvgTransformBoxParseResult(false, SvgCssTransformParseError.ExpectedValue, value.Length, SvgTransformBox.ViewBox);
        }
        if (trimmed.Length > maxTokenCharacters)
        {
            return new SvgTransformBoxParseResult(false, SvgCssTransformParseError.TokenTooLong, start + maxTokenCharacters, SvgTransformBox.ViewBox);
        }
        int box = SvgCssTransformRules.FindBox(trimmed);
        if (box < 0)
        {
            return new SvgTransformBoxParseResult(false, SvgCssTransformParseError.InvalidArgumentValue, start, SvgTransformBox.ViewBox);
        }
        return new SvgTransformBoxParseResult(true, SvgCssTransformParseError.None, -1, (SvgTransformBox)box);
    }

    static SvgCssFunctionParseResult ParseFunction(SvgSpanReader reader, int maxTokenCharacters)
    {
        int start = reader.GetPosition();
        SvgSpanTokenResult name = reader.ReadToken(IsAsciiLetter, false);
        if (!name.Success)
        {
            return FunctionFailure(TokenError(name), name.ErrorOffset);
        }
        if (name.RawText.Length == 0)
        {
            return FunctionFailure(SvgCssTransformParseError.ExpectedValue, start);
        }
        int kindValue = SvgCssTransformRules.FindKind(name.GetValue());
        if (kindValue < 0)
        {
            return FunctionFailure(SvgCssTransformParseError.UnknownFunction, start);
        }
        reader.SkipCssWhitespace();
        if (!reader.Consume('('))
        {
            return FunctionFailure(SvgCssTransformParseError.ExpectedOpenParenthesis, reader.GetPosition());
        }
        SvgCssTokenListResult arguments = ReadArguments(reader, maxTokenCharacters);
        if (!arguments.Success)
        {
            return FunctionFailure(arguments.Error, arguments.ErrorOffset);
        }
        SvgCssTransformKind kind = (SvgCssTransformKind)kindValue;
        if (!SvgCssTransformRules.ValidTokenCount(kind, arguments.Tokens.Count))
        {
            int maximum = SvgCssTransformRules.MaximumTokenCount(kind);
            int offset = arguments.Tokens.Count > maximum ? arguments.Tokens[maximum].Offset : arguments.EndOffset;
            return FunctionFailure(SvgCssTransformParseError.InvalidArgumentCount, offset);
        }
        SvgCssTransform? transform = BuildFunction(kind, arguments, maxTokenCharacters);
        if (transform == null)
        {
            return FunctionFailure(SvgCssTransformParseError.InvalidArgumentValue, arguments.ErrorOffset);
        }
        return new SvgCssFunctionParseResult(true, SvgCssTransformParseError.None, -1, transform);
    }

    static SvgCssTransform? BuildFunction(SvgCssTransformKind kind, SvgCssTokenListResult arguments, int maxTokenCharacters)
    {
        int count = arguments.Tokens.Count;
        if (kind == SvgCssTransformKind.Matrix || kind == SvgCssTransformKind.Scale || kind == SvgCssTransformKind.ScaleX || kind == SvgCssTransformKind.ScaleY)
        {
            double[] numbers = new double[count];
            int index = 0;
            while (index < count)
            {
                SvgNumberParseResult number = SvgNumberParser.ParseCssNumber(arguments.Tokens[index].Text, maxTokenCharacters);
                if (!number.Success)
                {
                    arguments.ErrorOffset = arguments.Tokens[index].Offset + number.ErrorOffset;
                    return null;
                }
                numbers[index] = number.Value;
                index = index + 1;
            }
            return new SvgCssTransform(kind, numbers, new SvgLength[0], new SvgAngle[0]);
        }
        if (kind == SvgCssTransformKind.Translate || kind == SvgCssTransformKind.TranslateX || kind == SvgCssTransformKind.TranslateY)
        {
            SvgLength[] lengths = new SvgLength[count];
            int index = 0;
            while (index < count)
            {
                SvgCssLengthTokenResult length = ParseLengthToken(arguments.Tokens[index], true, maxTokenCharacters);
                if (!length.Success || !SvgCssTransformRules.ValidPositionLength(length.Length))
                {
                    arguments.ErrorOffset = length.ErrorOffset;
                    return null;
                }
                lengths[index] = (length.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                index = index + 1;
            }
            return new SvgCssTransform(kind, new double[0], lengths, new SvgAngle[0]);
        }
        SvgAngle[] angles = new SvgAngle[count];
        int angleIndex = 0;
        while (angleIndex < count)
        {
            SvgAngleParseResult angle = SvgScalarUnitParser.ParseAngle(arguments.Tokens[angleIndex].Text, true, maxTokenCharacters);
            if (!angle.Success || (angle.Angle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Unit == SvgAngleUnit.Number && angle.Angle.Value != 0)
            {
                arguments.ErrorOffset = arguments.Tokens[angleIndex].Offset + (angle.Success ? 0 : angle.ErrorOffset);
                return null;
            }
            angles[angleIndex] = angle.Angle;
            angleIndex = angleIndex + 1;
        }
        return new SvgCssTransform(kind, new double[0], new SvgLength[0], angles);
    }

    static SvgCssTokenListResult ReadArguments(SvgSpanReader reader, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgCssArgumentToken> tokens = new System.Collections.Generic.List<SvgCssArgumentToken>();
        reader.SkipCssWhitespace();
        if (reader.Consume(')'))
        {
            return new SvgCssTokenListResult(true, SvgCssTransformParseError.None, -1, reader.GetPosition() - 1, tokens);
        }
        while (true)
        {
            int start = reader.GetPosition();
            System.ReadOnlySpan<char> input = reader.GetInput().Span;
            int position = start;
            while (position < input.Length && !SvgSpanCharacters.IsCssWhitespace(input[position]) && input[position] != ',' && input[position] != ')')
            {
                if (position - start >= maxTokenCharacters)
                {
                    reader.SetPosition(position);
                    return new SvgCssTokenListResult(false, SvgCssTransformParseError.TokenTooLong, position, position, tokens);
                }
                position = position + 1;
            }
            if (position == start)
            {
                return new SvgCssTokenListResult(false, SvgCssTransformParseError.InvalidArgumentValue, start, start, tokens);
            }
            tokens.Add(new SvgCssArgumentToken(input.Slice(start, position - start).ToString(), start));
            reader.SetPosition(position);
            reader.SkipCssWhitespace();
            int end = reader.GetPosition();
            if (reader.Consume(')'))
            {
                return new SvgCssTokenListResult(true, SvgCssTransformParseError.None, -1, end, tokens);
            }
            if (reader.IsEnd())
            {
                return new SvgCssTokenListResult(false, SvgCssTransformParseError.ExpectedCloseParenthesis, reader.GetPosition(), reader.GetPosition(), tokens);
            }
            if (reader.Consume(','))
            {
                reader.SkipCssWhitespace();
                if (reader.IsEnd() || reader.PeekCharacter().Value == ')')
                {
                    return new SvgCssTokenListResult(false, SvgCssTransformParseError.InvalidArgumentValue, reader.GetPosition(), reader.GetPosition(), tokens);
                }
                continue;
            }
            return new SvgCssTokenListResult(false, SvgCssTransformParseError.UnexpectedSeparator, reader.GetPosition(), reader.GetPosition(), tokens);
        }
    }

    static SvgCssTokenListResult TokenizeWhitespace(string value, int maxTokenCharacters, int maxTokens)
    {
        System.Collections.Generic.List<SvgCssArgumentToken> tokens = new System.Collections.Generic.List<SvgCssArgumentToken>();
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipCssWhitespace();
        while (!reader.IsEnd())
        {
            if (tokens.Count >= maxTokens)
            {
                return new SvgCssTokenListResult(false, SvgCssTransformParseError.InvalidArgumentCount, reader.GetPosition(), reader.GetPosition(), tokens);
            }
            int start = reader.GetPosition();
            System.ReadOnlySpan<char> input = reader.GetInput().Span;
            int position = start;
            while (position < input.Length && !SvgSpanCharacters.IsCssWhitespace(input[position]))
            {
                if (position - start >= maxTokenCharacters)
                {
                    return new SvgCssTokenListResult(false, SvgCssTransformParseError.TokenTooLong, position, position, tokens);
                }
                position = position + 1;
            }
            tokens.Add(new SvgCssArgumentToken(input.Slice(start, position - start).ToString(), start));
            reader.SetPosition(position);
            reader.SkipCssWhitespace();
        }
        return new SvgCssTokenListResult(true, SvgCssTransformParseError.None, -1, reader.GetPosition(), tokens);
    }

    static SvgCssLengthTokenResult ParsePositionToken(SvgCssArgumentToken token)
    {
        SvgLength? keyword = KeywordLength(token.Text);
        if (keyword != null)
        {
            return new SvgCssLengthTokenResult(true, SvgCssTransformParseError.None, token.Offset, token.Offset, keyword);
        }
        return ParseLengthToken(token, true);
    }

    static SvgCssLengthTokenResult ParseLengthToken(SvgCssArgumentToken token, bool allowPercentage)
    {
        return ParseLengthToken(token, allowPercentage, 256);
    }

    static SvgCssLengthTokenResult ParseLengthToken(SvgCssArgumentToken token, bool allowPercentage, int maxTokenCharacters)
    {
        SvgLengthParseResult parsed = SvgLengthParser.ParseCssLength(token.Text, allowPercentage, true, maxTokenCharacters);
        if (!parsed.Success || (parsed.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Unit == SvgLengthUnit.Number && parsed.Length.Value != 0)
        {
            int errorOffset = token.Offset + (parsed.Success ? 0 : parsed.ErrorOffset);
            return new SvgCssLengthTokenResult(false, SvgCssTransformParseError.InvalidArgumentValue, token.Offset, errorOffset, null);
        }
        return new SvgCssLengthTokenResult(true, SvgCssTransformParseError.None, token.Offset, -1, parsed.Length);
    }

    static SvgLength? KeywordLength(string text)
    {
        if (Equal(text, "left") || Equal(text, "top")) return Percentage(0);
        if (Equal(text, "center")) return Percentage(50);
        if (Equal(text, "right") || Equal(text, "bottom")) return Percentage(100);
        return null;
    }

    static bool ValidHorizontalKeyword(string text)
    {
        return !IsVerticalKeyword(text);
    }

    static bool ValidVerticalKeyword(string text)
    {
        return !IsHorizontalKeyword(text);
    }

    static bool IsHorizontalKeyword(string text)
    {
        return Equal(text, "left") || Equal(text, "right");
    }

    static bool IsVerticalKeyword(string text)
    {
        return Equal(text, "top") || Equal(text, "bottom");
    }

    static bool IsCenterKeyword(string text)
    {
        return Equal(text, "center");
    }

    static SvgLength Percentage(double value)
    {
        return new SvgLength(value, SvgLengthUnit.Percentage);
    }

    static void RecoverFunction(SvgSpanReader reader)
    {
        while (!reader.IsEnd())
        {
            SvgSpanCharacterResult character = reader.ReadCharacter();
            if (character.Value == ')')
            {
                return;
            }
        }
    }

    static bool FunctionEnded(SvgSpanReader reader)
    {
        int position = reader.GetPosition();
        return position > 0 && reader.GetInput().Span[position - 1] == ')';
    }

    static SvgCssTransformParseError TokenError(SvgSpanTokenResult token)
    {
        return token.Error == SvgSpanReadError.TokenTooLong ? SvgCssTransformParseError.TokenTooLong : SvgCssTransformParseError.InvalidParserArgument;
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool Equal(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }

    static SvgCssTransformParseResult TransformFailure(SvgCssTransformParseError error, int offset, System.Collections.Generic.List<SvgCssTransform> items)
    {
        return new SvgCssTransformParseResult(false, false, error, offset, new SvgCssTransformList(false, items));
    }

    static SvgTransformOriginParseResult OriginFailure(SvgCssTransformParseError error, int offset)
    {
        return new SvgTransformOriginParseResult(false, error, offset, null);
    }

    static SvgCssFunctionParseResult FunctionFailure(SvgCssTransformParseError error, int offset)
    {
        return new SvgCssFunctionParseResult(false, error, offset, null);
    }
}

/// <summary>Contains one CSS function parse result.</summary>
internal class SvgCssFunctionParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssTransformParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The parsed transform, or <c>null</c>.</summary>
    public SvgCssTransform? Transform;

    /// <summary>Creates a CSS function parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="transform">The parsed transform, or <c>null</c>.</param>
    public SvgCssFunctionParseResult(bool success, SvgCssTransformParseError error, int errorOffset, SvgCssTransform? transform)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        Transform = transform;
    }
}

/// <summary>Contains one raw CSS argument token.</summary>
internal class SvgCssArgumentToken
{
    /// <summary>The token text.</summary>
    public string Text;
    /// <summary>The token start offset.</summary>
    public int Offset;

    /// <summary>Creates a raw argument token.</summary>
    /// <param name="text">The token text.</param>
    /// <param name="offset">The start offset.</param>
    public SvgCssArgumentToken(string text, int offset)
    {
        Text = text;
        Offset = offset;
    }
}

/// <summary>Contains a bounded raw CSS token list.</summary>
internal class SvgCssTokenListResult
{
    /// <summary>Indicates whether tokenization succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssTransformParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The closing or ending offset.</summary>
    public int EndOffset;
    /// <summary>The raw tokens.</summary>
    public System.Collections.Generic.List<SvgCssArgumentToken> Tokens;

    /// <summary>Creates a raw CSS token-list result.</summary>
    /// <param name="success">Whether tokenization succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="endOffset">The closing or ending offset.</param>
    /// <param name="tokens">The raw tokens.</param>
    public SvgCssTokenListResult(bool success, SvgCssTransformParseError error, int errorOffset, int endOffset, System.Collections.Generic.List<SvgCssArgumentToken> tokens)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        EndOffset = endOffset;
        Tokens = tokens;
    }
}

/// <summary>Contains one typed CSS length token result.</summary>
internal class SvgCssLengthTokenResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssTransformParseError Error;
    /// <summary>The source token offset.</summary>
    public int SourceOffset;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The parsed length, or <c>null</c>.</summary>
    public SvgLength? Length;

    /// <summary>Creates a typed CSS length result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="sourceOffset">The source token offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="length">The parsed length, or <c>null</c>.</param>
    public SvgCssLengthTokenResult(bool success, SvgCssTransformParseError error, int sourceOffset, int errorOffset, SvgLength? length)
    {
        Success = success;
        Error = error;
        SourceOffset = sourceOffset;
        ErrorOffset = errorOffset;
        Length = length;
    }
}

/// <summary>Defines CSS transform names, arities, copies, and validation.</summary>
internal static class SvgCssTransformRules
{
    /// <summary>Finds a case-insensitive CSS transform function name.</summary>
    /// <param name="name">The function name.</param>
    /// <returns>The kind value, or <c>-1</c>.</returns>
    public static int FindKind(string name)
    {
        if (Equal(name, "matrix")) return (int)SvgCssTransformKind.Matrix;
        if (Equal(name, "translate")) return (int)SvgCssTransformKind.Translate;
        if (Equal(name, "translateX")) return (int)SvgCssTransformKind.TranslateX;
        if (Equal(name, "translateY")) return (int)SvgCssTransformKind.TranslateY;
        if (Equal(name, "scale")) return (int)SvgCssTransformKind.Scale;
        if (Equal(name, "scaleX")) return (int)SvgCssTransformKind.ScaleX;
        if (Equal(name, "scaleY")) return (int)SvgCssTransformKind.ScaleY;
        if (Equal(name, "rotate")) return (int)SvgCssTransformKind.Rotate;
        if (Equal(name, "skew")) return (int)SvgCssTransformKind.Skew;
        if (Equal(name, "skewX")) return (int)SvgCssTransformKind.SkewX;
        if (Equal(name, "skewY")) return (int)SvgCssTransformKind.SkewY;
        return -1;
    }

    /// <summary>Finds a case-insensitive transform-box keyword.</summary>
    /// <param name="name">The keyword.</param>
    /// <returns>The box value, or <c>-1</c>.</returns>
    public static int FindBox(string name)
    {
        if (Equal(name, "content-box")) return (int)SvgTransformBox.ContentBox;
        if (Equal(name, "border-box")) return (int)SvgTransformBox.BorderBox;
        if (Equal(name, "fill-box")) return (int)SvgTransformBox.FillBox;
        if (Equal(name, "stroke-box")) return (int)SvgTransformBox.StrokeBox;
        if (Equal(name, "view-box")) return (int)SvgTransformBox.ViewBox;
        return -1;
    }

    /// <summary>Returns canonical transform-box keyword text.</summary>
    /// <param name="box">The box value.</param>
    /// <returns>The canonical keyword, or an empty string.</returns>
    public static string BoxText(SvgTransformBox box)
    {
        switch (box)
        {
            case SvgTransformBox.ContentBox: return "content-box";
            case SvgTransformBox.BorderBox: return "border-box";
            case SvgTransformBox.FillBox: return "fill-box";
            case SvgTransformBox.StrokeBox: return "stroke-box";
            case SvgTransformBox.ViewBox: return "view-box";
            default: return "";
        }
    }

    /// <summary>Tests whether a transform-box enum value is known.</summary>
    /// <param name="box">The value to test.</param>
    /// <returns><c>true</c> for a known transform box.</returns>
    public static bool IsBox(SvgTransformBox box)
    {
        return box >= SvgTransformBox.ContentBox && box <= SvgTransformBox.ViewBox;
    }

    /// <summary>Tests the raw token count for one CSS transform kind.</summary>
    /// <param name="kind">The function kind.</param>
    /// <param name="count">The token count.</param>
    /// <returns><c>true</c> when the count is accepted.</returns>
    public static bool ValidTokenCount(SvgCssTransformKind kind, int count)
    {
        switch (kind)
        {
            case SvgCssTransformKind.Matrix: return count == 6;
            case SvgCssTransformKind.Translate: return count == 1 || count == 2;
            case SvgCssTransformKind.TranslateX: return count == 1;
            case SvgCssTransformKind.TranslateY: return count == 1;
            case SvgCssTransformKind.Scale: return count == 1 || count == 2;
            case SvgCssTransformKind.ScaleX: return count == 1;
            case SvgCssTransformKind.ScaleY: return count == 1;
            case SvgCssTransformKind.Rotate: return count == 1;
            case SvgCssTransformKind.Skew: return count == 1 || count == 2;
            case SvgCssTransformKind.SkewX: return count == 1;
            case SvgCssTransformKind.SkewY: return count == 1;
            default: return false;
        }
    }

    /// <summary>Returns the maximum argument count for a CSS transform kind.</summary>
    /// <param name="kind">The function kind.</param>
    /// <returns>The maximum count, or zero for an unknown kind.</returns>
    public static int MaximumTokenCount(SvgCssTransformKind kind)
    {
        switch (kind)
        {
            case SvgCssTransformKind.Matrix: return 6;
            case SvgCssTransformKind.Translate: return 2;
            case SvgCssTransformKind.TranslateX: return 1;
            case SvgCssTransformKind.TranslateY: return 1;
            case SvgCssTransformKind.Scale: return 2;
            case SvgCssTransformKind.ScaleX: return 1;
            case SvgCssTransformKind.ScaleY: return 1;
            case SvgCssTransformKind.Rotate: return 1;
            case SvgCssTransformKind.Skew: return 2;
            case SvgCssTransformKind.SkewX: return 1;
            case SvgCssTransformKind.SkewY: return 1;
            default: return 0;
        }
    }

    /// <summary>Validates typed argument arrays for one CSS transform.</summary>
    /// <param name="kind">The function kind.</param>
    /// <param name="numbers">The numeric arguments.</param>
    /// <param name="lengths">The length arguments.</param>
    /// <param name="angles">The angle arguments.</param>
    /// <returns><c>true</c> for valid owned arguments.</returns>
    public static bool ValidArguments(SvgCssTransformKind kind, double[]? numbers, SvgLength[]? lengths, SvgAngle[]? angles)
    {
        if (numbers == null || lengths == null || angles == null || !ValidTokenCount(kind, numbers.Length + lengths.Length + angles.Length))
        {
            return false;
        }
        bool numeric = kind == SvgCssTransformKind.Matrix || kind == SvgCssTransformKind.Scale || kind == SvgCssTransformKind.ScaleX || kind == SvgCssTransformKind.ScaleY;
        bool length = kind == SvgCssTransformKind.Translate || kind == SvgCssTransformKind.TranslateX || kind == SvgCssTransformKind.TranslateY;
        if (numeric && (lengths.Length != 0 || angles.Length != 0) || length && (numbers.Length != 0 || angles.Length != 0) || !numeric && !length && (numbers.Length != 0 || lengths.Length != 0))
        {
            return false;
        }
        int index = 0;
        while (index < numbers.Length)
        {
            if (!SvgFiniteMath.IsFinite(numbers[index])) return false;
            index = index + 1;
        }
        index = 0;
        while (index < lengths.Length)
        {
            if (!ValidPositionLength(lengths[index])) return false;
            index = index + 1;
        }
        index = 0;
        while (index < angles.Length)
        {
            if (!ValidAngle(angles[index])) return false;
            index = index + 1;
        }
        return true;
    }

    /// <summary>Tests a length-percentage or unitless zero position component.</summary>
    /// <param name="length">The typed length.</param>
    /// <returns><c>true</c> for a valid position component.</returns>
    public static bool ValidPositionLength(SvgLength? length)
    {
        return length != null && SvgFiniteMath.IsFinite(length.Value) && length.Unit >= SvgLengthUnit.Number && length.Unit <= SvgLengthUnit.Pc && (length.Unit != SvgLengthUnit.Number || length.Value == 0);
    }

    /// <summary>Tests a non-percentage depth length or unitless zero.</summary>
    /// <param name="length">The typed depth length.</param>
    /// <returns><c>true</c> for a valid depth component.</returns>
    public static bool ValidDepthLength(SvgLength? length)
    {
        return ValidPositionLength(length) && (length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Unit != SvgLengthUnit.Percentage;
    }

    /// <summary>Tests a finite known angle, permitting only unitless zero.</summary>
    /// <param name="angle">The typed angle.</param>
    /// <returns><c>true</c> for a valid CSS angle.</returns>
    public static bool ValidAngle(SvgAngle angle)
    {
        return angle != null && SvgFiniteMath.IsFinite(angle.Value) && SvgScalarUnitNames.IsAngleUnit(angle.Unit) && (angle.Unit != SvgAngleUnit.Number || angle.Value == 0);
    }

    /// <summary>Copies and normalizes finite numbers.</summary>
    /// <param name="values">The values.</param>
    /// <returns>An independent copy.</returns>
    public static double[] CopyNumbers(double[] values)
    {
        double[] result = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = SvgFiniteMath.NormalizeZero(values[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Copies length references into a new array.</summary>
    /// <param name="values">The lengths.</param>
    /// <returns>An independent array.</returns>
    public static SvgLength[] CopyLengths(SvgLength[] values)
    {
        SvgLength[] result = new SvgLength[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }
        return result;
    }

    /// <summary>Copies angle references into a new array.</summary>
    /// <param name="values">The angles.</param>
    /// <returns>An independent array.</returns>
    public static SvgAngle[] CopyAngles(SvgAngle[] values)
    {
        SvgAngle[] result = new SvgAngle[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }
        return result;
    }

    static bool Equal(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Formats typed CSS transform values.</summary>
internal static class SvgCssTransformFormatter
{
    /// <summary>Formats one CSS transform function.</summary>
    /// <param name="transform">The function.</param>
    /// <returns>Canonical text, or an empty string for invalid state.</returns>
    public static string Format(SvgCssTransform? transform)
    {
        if (transform == null || !SvgCssTransformRules.ValidArguments(transform.Kind, transform.Numbers, transform.Lengths, transform.Angles))
        {
            return "";
        }
        string name = KindText(transform.Kind);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(name);
        builder.Append('(');
        int total = transform.Numbers.Length + transform.Lengths.Length + transform.Angles.Length;
        int index = 0;
        while (index < total)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }
            if (transform.Numbers.Length > 0)
            {
                builder.Append(SvgNumberFormatter.FormatShortest(transform.Numbers[index]).Text);
            }
            else if (transform.Lengths.Length > 0)
            {
                builder.Append(transform.Lengths[index].ToText());
            }
            else
            {
                builder.Append(transform.Angles[index].ToText());
            }
            index = index + 1;
        }
        builder.Append(')');
        return builder.ToString();
    }

    /// <summary>Formats one CSS transform list.</summary>
    /// <param name="list">The list.</param>
    /// <returns>Canonical text, or an empty string for invalid state.</returns>
    public static string FormatList(SvgCssTransformList? list)
    {
        if (list == null || list.Items == null || list.IsNone && list.Items.Count != 0)
        {
            return "";
        }
        if (list.IsNone) return "none";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < list.Items.Count)
        {
            string text = Format(list.Items[index]);
            if (text.Length == 0) return "";
            if (index > 0) builder.Append(' ');
            builder.Append(text);
            index = index + 1;
        }
        return builder.ToString();
    }

    static string KindText(SvgCssTransformKind kind)
    {
        switch (kind)
        {
            case SvgCssTransformKind.Matrix: return "matrix";
            case SvgCssTransformKind.Translate: return "translate";
            case SvgCssTransformKind.TranslateX: return "translateX";
            case SvgCssTransformKind.TranslateY: return "translateY";
            case SvgCssTransformKind.Scale: return "scale";
            case SvgCssTransformKind.ScaleX: return "scaleX";
            case SvgCssTransformKind.ScaleY: return "scaleY";
            case SvgCssTransformKind.Rotate: return "rotate";
            case SvgCssTransformKind.Skew: return "skew";
            case SvgCssTransformKind.SkewX: return "skewX";
            case SvgCssTransformKind.SkewY: return "skewY";
            default: return "";
        }
    }
}

/// <summary>Evaluates and composes typed two-dimensional CSS transforms.</summary>
internal static class SvgCssTransformMatrixBuilder
{
    /// <summary>Composes a CSS transform list around its resolved origin.</summary>
    /// <param name="list">The transform list.</param>
    /// <param name="context">The length and box context.</param>
    /// <param name="origin">The typed origin.</param>
    /// <param name="transformBox">The selected box.</param>
    /// <returns>A finite matrix or stable failure.</returns>
    public static SvgCssTransformMatrixResult Compose(SvgCssTransformList? list, SvgCssTransformContext? context, SvgTransformOrigin? origin, SvgTransformBox transformBox)
    {
        if (list == null || list.Items == null || list.IsNone && list.Items.Count != 0)
        {
            return Failure(SvgCssTransformError.InvalidArgument);
        }
        if (list.IsNone)
        {
            return Matrix(1, 0, 0, 1, 0, 0);
        }
        if (context == null || origin == null || !SvgCssTransformRules.IsBox(transformBox) || !SvgCssTransformRules.ValidPositionLength(origin.X) || !SvgCssTransformRules.ValidPositionLength(origin.Y) || origin.HasZ && !SvgCssTransformRules.ValidDepthLength(origin.Z) || !origin.HasZ && origin.Z != null)
        {
            return Failure(SvgCssTransformError.InvalidArgument);
        }
        SvgCssTransformReferenceBox? box = SelectBox(context, transformBox);
        if (box == null)
        {
            return Failure(SvgCssTransformError.InvalidArgument);
        }
        if (!box.Available)
        {
            return Failure(SvgCssTransformError.MissingReferenceBox);
        }
        if (!ValidBox(box))
        {
            return Failure(SvgCssTransformError.InvalidArgument);
        }
        SvgCssScalarResult originX = ResolveLength(origin.X, context.LengthContext, box.Width);
        SvgCssScalarResult originY = ResolveLength(origin.Y, context.LengthContext, box.Height);
        if (!originX.Success) return Failure(originX.Error);
        if (!originY.Success) return Failure(originY.Error);
        double x = box.X + originX.Value;
        double y = box.Y + originY.Value;
        SvgCssTransformMatrixResult current = Matrix(1, 0, 0, 1, x, y);
        if (!current.Success) return current;
        int index = 0;
        while (index < list.Items.Count)
        {
            SvgCssTransformMatrixResult next = Build(list.Items[index], context, box);
            if (!next.Success) return next;
            current = Multiply(current.Matrix, next.Matrix);
            if (!current.Success) return current;
            index = index + 1;
        }
        SvgCssTransformMatrixResult back = Matrix(1, 0, 0, 1, -x, -y);
        if (!back.Success) return back;
        return Multiply(current.Matrix, back.Matrix);
    }

    static SvgCssTransformMatrixResult Build(SvgCssTransform? transform, SvgCssTransformContext context, SvgCssTransformReferenceBox box)
    {
        if (transform == null || !SvgCssTransformRules.ValidArguments(transform.Kind, transform.Numbers, transform.Lengths, transform.Angles))
        {
            return Failure(SvgCssTransformError.InvalidArgument);
        }
        double[] numbers = transform.Numbers;
        if (transform.Kind == SvgCssTransformKind.Matrix) return Matrix(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);
        if (transform.Kind == SvgCssTransformKind.Scale) return Matrix(numbers[0], 0, 0, numbers.Length == 2 ? numbers[1] : numbers[0], 0, 0);
        if (transform.Kind == SvgCssTransformKind.ScaleX) return Matrix(numbers[0], 0, 0, 1, 0, 0);
        if (transform.Kind == SvgCssTransformKind.ScaleY) return Matrix(1, 0, 0, numbers[0], 0, 0);
        if (transform.Kind == SvgCssTransformKind.Translate || transform.Kind == SvgCssTransformKind.TranslateX || transform.Kind == SvgCssTransformKind.TranslateY)
        {
            double tx = 0;
            double ty = 0;
            if (transform.Kind != SvgCssTransformKind.TranslateY)
            {
                SvgCssScalarResult resolvedX = ResolveLength(transform.Lengths[0], context.LengthContext, box.Width);
                if (!resolvedX.Success) return Failure(resolvedX.Error);
                tx = resolvedX.Value;
            }
            if (transform.Kind == SvgCssTransformKind.TranslateY)
            {
                SvgCssScalarResult resolvedY = ResolveLength(transform.Lengths[0], context.LengthContext, box.Height);
                if (!resolvedY.Success) return Failure(resolvedY.Error);
                ty = resolvedY.Value;
            }
            else if (transform.Kind == SvgCssTransformKind.Translate && transform.Lengths.Length == 2)
            {
                SvgCssScalarResult resolvedY = ResolveLength(transform.Lengths[1], context.LengthContext, box.Height);
                if (!resolvedY.Success) return Failure(resolvedY.Error);
                ty = resolvedY.Value;
            }
            return Matrix(1, 0, 0, 1, tx, ty);
        }
        if (transform.Kind == SvgCssTransformKind.Rotate)
        {
            SvgCssScalarResult radians = ResolveAngle(transform.Angles[0]);
            if (!radians.Success) return Failure(radians.Error);
            double cosine = System.Math.Cos(radians.Value);
            double sine = System.Math.Sin(radians.Value);
            return Matrix(cosine, sine, -sine, cosine, 0, 0);
        }
        SvgCssScalarResult first = ResolveAngle(transform.Angles[0]);
        if (!first.Success) return Failure(first.Error);
        double xSkew = 0;
        double ySkew = 0;
        if (transform.Kind == SvgCssTransformKind.Skew || transform.Kind == SvgCssTransformKind.SkewX) xSkew = System.Math.Tan(first.Value);
        if (transform.Kind == SvgCssTransformKind.SkewY) ySkew = System.Math.Tan(first.Value);
        if (transform.Kind == SvgCssTransformKind.Skew && transform.Angles.Length == 2)
        {
            SvgCssScalarResult second = ResolveAngle(transform.Angles[1]);
            if (!second.Success) return Failure(second.Error);
            ySkew = System.Math.Tan(second.Value);
        }
        return Matrix(1, ySkew, xSkew, 1, 0, 0);
    }

    static SvgCssScalarResult ResolveLength(SvgLength? length, SvgLengthContext context, double percentageBasis)
    {
        if (!SvgCssTransformRules.ValidPositionLength(length)) return ScalarFailure(SvgCssTransformError.InvalidArgument);
        if ((length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Unit == SvgLengthUnit.Percentage) return Scalar(length.Value * percentageBasis / 100);
        if (length.Unit == SvgLengthUnit.Number) return Scalar(0);
        SvgLengthEvaluationResult evaluated = length.Evaluate(context, SvgLengthPercentageBasis.None);
        if (!evaluated.Success)
        {
            SvgCssTransformError error = evaluated.Error == SvgLengthEvaluationError.NonFiniteResult ? SvgCssTransformError.NonFiniteResult : SvgCssTransformError.MissingLengthContext;
            return ScalarFailure(error);
        }
        return Scalar(evaluated.Value);
    }

    static SvgCssScalarResult ResolveAngle(SvgAngle angle)
    {
        if (!SvgCssTransformRules.ValidAngle(angle)) return ScalarFailure(SvgCssTransformError.InvalidArgument);
        SvgUnitConversionResult converted = angle.ConvertTo(SvgAngleUnit.Rad);
        if (!converted.Success)
        {
            return ScalarFailure(converted.Error == SvgUnitConversionError.NonFiniteResult ? SvgCssTransformError.NonFiniteResult : SvgCssTransformError.InvalidArgument);
        }
        return Scalar(converted.Value);
    }

    static SvgCssTransformReferenceBox? SelectBox(SvgCssTransformContext context, SvgTransformBox box)
    {
        switch (box)
        {
            case SvgTransformBox.ContentBox: return context.ContentBox;
            case SvgTransformBox.BorderBox: return context.BorderBox;
            case SvgTransformBox.FillBox: return context.FillBox;
            case SvgTransformBox.StrokeBox: return context.StrokeBox;
            case SvgTransformBox.ViewBox: return context.ViewBox;
            default: return null;
        }
    }

    static bool ValidBox(SvgCssTransformReferenceBox box)
    {
        return box != null && box.Available && SvgFiniteMath.IsFinite(box.X) && SvgFiniteMath.IsFinite(box.Y) && SvgFiniteMath.IsFinite(box.Width) && SvgFiniteMath.IsFinite(box.Height) && box.Width >= 0 && box.Height >= 0;
    }

    static SvgCssTransformMatrixResult Multiply(SvgMatrix? left, SvgMatrix? right)
    {
        return Matrix((left ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A * (right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A + left.C * right.B, left.B * right.A + left.D * right.B, left.A * right.C + left.C * right.D, left.B * right.C + left.D * right.D, left.A * right.E + left.C * right.F + left.E, left.B * right.E + left.D * right.F + left.F);
    }

    static SvgCssTransformMatrixResult Matrix(double a, double b, double c, double d, double e, double f)
    {
        if (!SvgFiniteMath.IsFinite(a) || !SvgFiniteMath.IsFinite(b) || !SvgFiniteMath.IsFinite(c) || !SvgFiniteMath.IsFinite(d) || !SvgFiniteMath.IsFinite(e) || !SvgFiniteMath.IsFinite(f))
        {
            return Failure(SvgCssTransformError.NonFiniteResult);
        }
        return new SvgCssTransformMatrixResult(true, SvgCssTransformError.None, new SvgMatrix(a, b, c, d, e, f));
    }

    static SvgCssTransformMatrixResult Failure(SvgCssTransformError error)
    {
        return new SvgCssTransformMatrixResult(false, error, null);
    }

    static SvgCssScalarResult Scalar(double value)
    {
        if (!SvgFiniteMath.IsFinite(value)) return ScalarFailure(SvgCssTransformError.NonFiniteResult);
        return new SvgCssScalarResult(true, SvgCssTransformError.None, SvgFiniteMath.NormalizeZero(value));
    }

    static SvgCssScalarResult ScalarFailure(SvgCssTransformError error)
    {
        return new SvgCssScalarResult(false, error, 0);
    }
}

/// <summary>Contains one internal scalar evaluation result.</summary>
internal class SvgCssScalarResult
{
    /// <summary>Indicates whether evaluation succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssTransformError Error;
    /// <summary>The finite result or zero.</summary>
    public double Value;

    /// <summary>Creates a scalar result.</summary>
    /// <param name="success">Whether evaluation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The finite value or zero.</param>
    public SvgCssScalarResult(bool success, SvgCssTransformError error, double value)
    {
        Success = success;
        Error = error;
        Value = value;
    }
}
