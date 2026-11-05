namespace svg;

/// <summary>Stores the result of reading one complete path parameter group.</summary>
internal class SvgPathGroupResult
{
    /// <summary>Whether the parameter group was valid.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgPathParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The successfully parsed numeric payload.</summary>
    public double[] Values;
    /// <summary>The first parameter offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a parameter-group result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="values">The parsed values.</param>
    /// <param name="startOffset">The group starting offset.</param>
    /// <param name="endOffset">The group ending offset.</param>
    internal SvgPathGroupResult(bool success, SvgPathParseError error, int errorOffset, double[] values, int startOffset, int endOffset)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        Values = values;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Implements stateful SVG path parsing without exposing mutable scanner state.</summary>
internal class SvgPathParserState
{
    string source;
    int maximumSegments;
    SvgSpanReader reader;
    System.Collections.Generic.List<SvgPathCommand> commands;
    System.Collections.Generic.List<SvgPathParseIssue> issues;
    bool hasCurrent;
    double currentX;
    double currentY;
    double subpathStartX;
    double subpathStartY;
    int subpathIndex;
    bool stopped;

    /// <summary>Creates bounded path parser state.</summary>
    /// <param name="value">The path data.</param>
    /// <param name="maxSegments">The maximum command count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    internal SvgPathParserState(string value, int maxSegments, int maxTokenCharacters)
    {
        source = value;
        maximumSegments = maxSegments;
        reader = new SvgSpanReader(value, maxTokenCharacters);
        commands = new System.Collections.Generic.List<SvgPathCommand>();
        issues = new System.Collections.Generic.List<SvgPathParseIssue>();
        hasCurrent = false;
        currentX = 0;
        currentY = 0;
        subpathStartX = 0;
        subpathStartY = 0;
        subpathIndex = -1;
        stopped = false;
    }

    /// <summary>Parses the configured source and creates its public result.</summary>
    /// <returns>The recovered path parse result.</returns>
    internal SvgPathParseResult Run()
    {
        reader.SkipWhitespace();
        while (!reader.IsEnd() && !stopped)
        {
            int commandOffset = reader.GetPosition();
            char command = reader.PeekCharacter().Value;
            if (!IsCommandLetter(command))
            {
                if (IsAsciiLetter(command))
                {
                    AddIssue(SvgPathParseError.UnknownCommand, commandOffset, commandOffset);
                    reader.ReadCharacter();
                }
                else
                {
                    AddIssue(SvgPathParseError.UnexpectedData, commandOffset, -1);
                }
                RecoverToCommand();
                reader.SkipWhitespace();
                continue;
            }

            reader.ReadCharacter();
            char upper = Upper(command);
            if (!hasCurrent && upper != 'M')
            {
                AddIssue(SvgPathParseError.ExpectedMoveTo, commandOffset, commandOffset);
                RecoverToCommand();
                reader.SkipWhitespace();
                continue;
            }
            if (upper == 'Z')
            {
                AddClose(command >= 'a' && command <= 'z', commandOffset);
            }
            else
            {
                ParseSeries(command, commandOffset);
            }
            reader.SkipWhitespace();
        }

        SvgPathData path = new SvgPathData(commands, source, issues);
        if (issues.Count == 0) return new SvgPathParseResult(true, SvgPathParseError.None, path, -1, reader.GetPosition());
        return new SvgPathParseResult(false, issues[0].Error, path, issues[0].ErrorOffset, reader.GetPosition());
    }

    void ParseSeries(char command, int commandOffset)
    {
        char upper = Upper(command);
        bool relative = command >= 'a' && command <= 'z';
        bool firstGroup = true;
        reader.SkipWhitespace();
        while (!stopped)
        {
            if (commands.Count >= maximumSegments)
            {
                AddIssue(SvgPathParseError.SegmentLimitExceeded, reader.GetPosition(), commandOffset);
                stopped = true;
                return;
            }
            SvgPathGroupResult group = ReadGroup(upper);
            if (!group.Success)
            {
                AddIssue(group.Error, group.ErrorOffset, commandOffset);
                RecoverToCommand();
                return;
            }

            SvgPathCommandKind kind = Kind(upper);
            if (upper == 'M' && !firstGroup) kind = SvgPathCommandKind.LineTo;
            int sourceOffset = firstGroup ? commandOffset : group.StartOffset;
            if (!AddCommand(kind, relative, group.Values, sourceOffset, group.EndOffset, firstGroup, commandOffset))
            {
                if (!stopped) RecoverToCommand();
                return;
            }
            firstGroup = false;

            int separatorOffset = reader.GetPosition();
            int whitespace = reader.SkipWhitespace();
            bool comma = reader.Consume(',');
            if (comma) reader.SkipWhitespace();
            if (reader.IsEnd())
            {
                if (comma) AddIssue(SvgPathParseError.ExpectedParameter, reader.GetPosition(), commandOffset);
                return;
            }
            char next = reader.PeekCharacter().Value;
            if (IsAsciiLetter(next))
            {
                if (comma) AddIssue(SvgPathParseError.ExpectedParameter, separatorOffset + whitespace, commandOffset);
                return;
            }
            if (whitespace == 0 && !comma && !IsCompactNumberStart(next))
            {
                AddIssue(SvgPathParseError.UnexpectedData, reader.GetPosition(), commandOffset);
                RecoverToCommand();
                return;
            }
        }
    }

    SvgPathGroupResult ReadGroup(char command)
    {
        SvgPathCommandKind kind = Kind(command);
        int count = SvgPathCommandRules.ParameterCount(kind);
        double[] values = new double[count];
        int start = reader.GetPosition();
        int index = 0;
        while (index < count)
        {
            if (index > 0 && !PrepareParameter(command, index)) return GroupFailure(SvgPathParseError.ExpectedParameter, reader.GetPosition(), start);
            if (reader.IsEnd() || IsAsciiLetter(reader.PeekCharacter().Value)) return GroupFailure(SvgPathParseError.ExpectedParameter, reader.GetPosition(), start);
            int parameterOffset = reader.GetPosition();
            if (command == 'A' && (index == 3 || index == 4))
            {
                char flag = reader.PeekCharacter().Value;
                if (flag != '0' && flag != '1') return GroupFailure(SvgPathParseError.InvalidArcFlag, parameterOffset, start);
                values[index] = flag - '0';
                reader.ReadCharacter();
            }
            else
            {
                SvgNumberParseResult number = SvgNumberParser.ReadNumber(reader);
                if (!number.Success)
                {
                    SvgPathParseError error = number.Error == SvgNumberParseError.TokenTooLong ? SvgPathParseError.TokenTooLong : SvgPathParseError.InvalidNumber;
                    return GroupFailure(error, number.ErrorOffset, start);
                }
                if (command == 'A' && index < 2 && number.Value < 0) return GroupFailure(SvgPathParseError.NegativeArcRadius, parameterOffset, start);
                values[index] = number.Value;
            }
            index = index + 1;
        }
        return new SvgPathGroupResult(true, SvgPathParseError.None, -1, values, start, reader.GetPosition());
    }

    bool PrepareParameter(char command, int parameterIndex)
    {
        int whitespace = reader.SkipWhitespace();
        bool comma = reader.Consume(',');
        if (comma) reader.SkipWhitespace();
        if (reader.IsEnd()) return false;
        if (whitespace > 0 || comma) return true;
        char next = reader.PeekCharacter().Value;
        if (command == 'A' && parameterIndex == 4) return next >= '0' && next <= '9';
        if (command == 'A' && parameterIndex == 5) return IsNumberStart(next);
        return IsCompactNumberStart(next);
    }

    bool AddCommand(SvgPathCommandKind kind, bool relative, double[] values, int sourceOffset, int endOffset, bool explicitCommand, int commandOffset)
    {
        if (commands.Count >= maximumSegments)
        {
            AddIssue(SvgPathParseError.SegmentLimitExceeded, sourceOffset, commandOffset);
            stopped = true;
            return false;
        }

        double startX = hasCurrent ? currentX : 0;
        double startY = hasCurrent ? currentY : 0;
        double endX = startX;
        double endY = startY;
        double nextSubpathX = subpathStartX;
        double nextSubpathY = subpathStartY;
        int nextSubpathIndex = subpathIndex;
        if (kind == SvgPathCommandKind.HorizontalLineTo)
        {
            endX = relative ? startX + values[0] : values[0];
        }
        else if (kind == SvgPathCommandKind.VerticalLineTo)
        {
            endY = relative ? startY + values[0] : values[0];
        }
        else
        {
            int endpoint = EndpointIndex(kind);
            endX = relative ? startX + values[endpoint] : values[endpoint];
            endY = relative ? startY + values[endpoint + 1] : values[endpoint + 1];
        }
        if (!SvgFiniteMath.IsFinite(endX) || !SvgFiniteMath.IsFinite(endY))
        {
            AddIssue(SvgPathParseError.InvalidNumber, sourceOffset, commandOffset);
            return false;
        }
        if (kind == SvgPathCommandKind.MoveTo)
        {
            nextSubpathIndex = subpathIndex + 1;
            nextSubpathX = endX;
            nextSubpathY = endY;
        }

        SvgPathCommand parsed = new SvgPathCommand(kind, relative, values, sourceOffset, endOffset, explicitCommand, startX, startY, endX, endY, nextSubpathX, nextSubpathY, nextSubpathIndex);
        commands.Add(parsed);
        currentX = endX;
        currentY = endY;
        subpathStartX = nextSubpathX;
        subpathStartY = nextSubpathY;
        subpathIndex = nextSubpathIndex;
        hasCurrent = true;
        return true;
    }

    void AddClose(bool relative, int commandOffset)
    {
        if (commands.Count >= maximumSegments)
        {
            AddIssue(SvgPathParseError.SegmentLimitExceeded, commandOffset, commandOffset);
            stopped = true;
            return;
        }
        SvgPathCommand parsed = new SvgPathCommand(SvgPathCommandKind.ClosePath, relative, new double[0], commandOffset, reader.GetPosition(), true, currentX, currentY, subpathStartX, subpathStartY, subpathStartX, subpathStartY, subpathIndex);
        commands.Add(parsed);
        currentX = subpathStartX;
        currentY = subpathStartY;
    }

    void AddIssue(SvgPathParseError error, int errorOffset, int commandOffset)
    {
        issues.Add(new SvgPathParseIssue(error, errorOffset, commandOffset));
    }

    void RecoverToCommand()
    {
        while (!reader.IsEnd())
        {
            if (IsCommandLetter(reader.PeekCharacter().Value)) return;
            reader.ReadCharacter();
        }
    }

    static SvgPathGroupResult GroupFailure(SvgPathParseError error, int errorOffset, int startOffset)
    {
        return new SvgPathGroupResult(false, error, errorOffset, new double[0], startOffset, startOffset);
    }

    static SvgPathCommandKind Kind(char command)
    {
        if (command == 'M') return SvgPathCommandKind.MoveTo;
        if (command == 'Z') return SvgPathCommandKind.ClosePath;
        if (command == 'L') return SvgPathCommandKind.LineTo;
        if (command == 'H') return SvgPathCommandKind.HorizontalLineTo;
        if (command == 'V') return SvgPathCommandKind.VerticalLineTo;
        if (command == 'C') return SvgPathCommandKind.CubicCurveTo;
        if (command == 'S') return SvgPathCommandKind.SmoothCubicCurveTo;
        if (command == 'Q') return SvgPathCommandKind.QuadraticCurveTo;
        if (command == 'T') return SvgPathCommandKind.SmoothQuadraticCurveTo;
        if (command == 'A') return SvgPathCommandKind.ArcTo;
        return SvgPathCommandKind.Unknown;
    }

    static int EndpointIndex(SvgPathCommandKind kind)
    {
        if (kind == SvgPathCommandKind.MoveTo || kind == SvgPathCommandKind.LineTo || kind == SvgPathCommandKind.SmoothQuadraticCurveTo) return 0;
        if (kind == SvgPathCommandKind.SmoothCubicCurveTo || kind == SvgPathCommandKind.QuadraticCurveTo) return 2;
        if (kind == SvgPathCommandKind.CubicCurveTo) return 4;
        if (kind == SvgPathCommandKind.ArcTo) return 5;
        return 0;
    }

    static char Upper(char command)
    {
        if (command >= 'a' && command <= 'z') return (char)(command - 'a' + 'A');
        return command;
    }

    static bool IsCommandLetter(char value)
    {
        char upper = Upper(value);
        return upper == 'M' || upper == 'Z' || upper == 'L' || upper == 'H' || upper == 'V' || upper == 'C' || upper == 'S' || upper == 'Q' || upper == 'T' || upper == 'A';
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool IsNumberStart(char value)
    {
        return value >= '0' && value <= '9' || value == '+' || value == '-' || value == '.';
    }

    static bool IsCompactNumberStart(char value)
    {
        return value == '+' || value == '-' || value == '.';
    }
}
