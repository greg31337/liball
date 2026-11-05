namespace svg;

/// <summary>Contains one internal expression parse result.</summary>
internal class SvgCalcWorkResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCalcParseError Error;
    /// <summary>The parsed node.</summary>
    public SvgCalcExpression? Expression;
    /// <summary>The failure offset.</summary>
    public int ErrorOffset;

    /// <summary>Creates an internal expression result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="expression">The parsed node.</param>
    /// <param name="errorOffset">The failure offset.</param>
    internal SvgCalcWorkResult(bool success, SvgCalcParseError error, SvgCalcExpression? expression, int errorOffset)
    {
        Success = success;
        Error = error;
        Expression = expression;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Implements bounded recursive-descent CSS math parsing.</summary>
internal class SvgCalcParserState
{
    string text;
    SvgSpanReader reader;
    int maximumDepth;
    int maximumNodes;
    int maximumTokenCharacters;
    int nodeCount;

    /// <summary>Creates parser state over substituted CSS text.</summary>
    /// <param name="value">The substituted text.</param>
    /// <param name="maxDepth">The maximum nesting depth.</param>
    /// <param name="maxNodes">The maximum node count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    internal SvgCalcParserState(string value, int maxDepth, int maxNodes, int maxTokenCharacters)
    {
        text = value;
        reader = new SvgSpanReader(value, maxTokenCharacters);
        maximumDepth = maxDepth;
        maximumNodes = maxNodes;
        maximumTokenCharacters = maxTokenCharacters;
        nodeCount = 0;
    }

    /// <summary>Parses one complete expression.</summary>
    /// <returns>A complete expression or stable failure.</returns>
    internal SvgCalcWorkResult ParseComplete()
    {
        SkipSpaceAndComments();
        if (reader.IsEnd()) return Failure(SvgCalcParseError.ExpectedExpression, reader.GetPosition());
        SvgCalcWorkResult result = ParseSum(0);
        if (!result.Success) return result;
        SkipSpaceAndComments();
        if (!reader.IsEnd()) return Failure(SvgCalcParseError.UnexpectedToken, reader.GetPosition());
        return result;
    }

    SvgCalcWorkResult ParseSum(int depth)
    {
        SvgCalcWorkResult left = ParseProduct(depth);
        if (!left.Success) return left;
        while (true)
        {
            bool beforeSpace = SkipSpaceAndComments();
            if (reader.IsEnd()) return left;
            char operation = reader.PeekCharacter().Value;
            if (operation != '+' && operation != '-') return left;
            int operationOffset = reader.GetPosition();
            reader.ReadCharacter();
            bool afterSpace = SkipSpaceAndComments();
            if (!beforeSpace || !afterSpace) return Failure(SvgCalcParseError.UnexpectedToken, operationOffset);
            SvgCalcWorkResult right = ParseProduct(depth);
            if (!right.Success) return right;
            SvgCalcDimension dimension = AddDimension((left.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension, (right.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension);
            if (dimension == SvgCalcDimension.Any) return Failure(SvgCalcParseError.IncompatibleDimensions, operationOffset);
            System.Collections.Generic.List<SvgCalcExpression> children = Pair(left.Expression, right.Expression);
            SvgCalcWorkResult combined = Create(operation == '+' ? SvgCalcExpressionKind.Add : SvgCalcExpressionKind.Subtract, dimension, 0, "", children, left.Expression.StartOffset, right.Expression.EndOffset);
            if (!combined.Success) return combined;
            left = combined;
        }
    }

    SvgCalcWorkResult ParseProduct(int depth)
    {
        SvgCalcWorkResult left = ParsePrimary(depth);
        if (!left.Success) return left;
        while (true)
        {
            int separatorOffset = reader.GetPosition();
            SkipSpaceAndComments();
            if (reader.IsEnd())
            {
                reader.SetPosition(separatorOffset);
                return left;
            }
            char operation = reader.PeekCharacter().Value;
            if (operation != '*' && operation != '/')
            {
                reader.SetPosition(separatorOffset);
                return left;
            }
            int operationOffset = reader.GetPosition();
            reader.ReadCharacter();
            SkipSpaceAndComments();
            SvgCalcWorkResult right = ParsePrimary(depth);
            if (!right.Success) return right;
            SvgCalcDimension dimension;
            if (operation == '*')
            {
                if ((left.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension == SvgCalcDimension.Number) dimension = (right.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension;
                else if ((right.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension == SvgCalcDimension.Number) dimension = left.Expression.Dimension;
                else return Failure(SvgCalcParseError.InvalidProduct, operationOffset);
            }
            else
            {
                if ((right.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension != SvgCalcDimension.Number) return Failure(SvgCalcParseError.InvalidProduct, operationOffset);
                if (IsLiteralZero(right.Expression)) return Failure(SvgCalcParseError.DivisionByZero, right.Expression.StartOffset);
                dimension = (left.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension;
            }
            System.Collections.Generic.List<SvgCalcExpression> children = Pair(left.Expression, right.Expression);
            SvgCalcWorkResult combined = Create(operation == '*' ? SvgCalcExpressionKind.Multiply : SvgCalcExpressionKind.Divide, dimension, 0, "", children, left.Expression.StartOffset, right.Expression.EndOffset);
            if (!combined.Success) return combined;
            left = combined;
        }
    }

    SvgCalcWorkResult ParsePrimary(int depth)
    {
        if (depth > maximumDepth) return Failure(SvgCalcParseError.DepthExceeded, reader.GetPosition());
        SkipSpaceAndComments();
        if (reader.IsEnd()) return Failure(SvgCalcParseError.ExpectedExpression, reader.GetPosition());
        int start = reader.GetPosition();
        char current = reader.PeekCharacter().Value;
        if (current == '(')
        {
            reader.ReadCharacter();
            SvgCalcWorkResult grouped = ParseSum(depth + 1);
            if (!grouped.Success) return grouped;
            SkipSpaceAndComments();
            if (!reader.Consume(')')) return Failure(SvgCalcParseError.UnexpectedToken, reader.GetPosition());
            return grouped;
        }
        if ((current == '+' || current == '-') && !StartsNumber(reader.GetPosition()))
        {
            reader.ReadCharacter();
            SvgCalcWorkResult operand = ParsePrimary(depth + 1);
            if (!operand.Success) return operand;
            if (current == '+') return operand;
            System.Collections.Generic.List<SvgCalcExpression> children = CreateSingleChildList(operand.Expression);
            return Create(SvgCalcExpressionKind.Negate, (operand.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension, 0, "", children, start, operand.Expression.EndOffset);
        }
        if (IsAsciiLetter(current)) return ParseFunction(depth + 1);
        return ParseValue();
    }

    SvgCalcWorkResult ParseFunction(int depth)
    {
        int start = reader.GetPosition();
        int nameStart = start;
        while (!reader.IsEnd() && (IsAsciiLetter(reader.PeekCharacter().Value) || reader.PeekCharacter().Value == '-')) reader.ReadCharacter();
        string name = text.Substring(nameStart, reader.GetPosition() - nameStart).ToLowerInvariant();
        if (!reader.Consume('(')) return Failure(SvgCalcParseError.UnexpectedToken, reader.GetPosition());
        if (name == "calc")
        {
            SvgCalcWorkResult child = ParseSum(depth);
            if (!child.Success) return child;
            SkipSpaceAndComments();
            if (!reader.Consume(')')) return Failure(SvgCalcParseError.UnexpectedToken, reader.GetPosition());
            return Create(SvgCalcExpressionKind.Calc, (child.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension, 0, "", CreateSingleChildList(child.Expression), start, reader.GetPosition());
        }
        if (name != "min" && name != "max" && name != "clamp") return Failure(SvgCalcParseError.InvalidFunction, start);
        System.Collections.Generic.List<SvgCalcExpression> arguments = new System.Collections.Generic.List<SvgCalcExpression>();
        while (true)
        {
            SvgCalcWorkResult argument = ParseSum(depth);
            if (!argument.Success) return argument;
            arguments.Add((argument.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            SkipSpaceAndComments();
            if (reader.Consume(','))
            {
                SkipSpaceAndComments();
                continue;
            }
            if (!reader.Consume(')')) return Failure(SvgCalcParseError.UnexpectedToken, reader.GetPosition());
            break;
        }
        if (name == "clamp" && arguments.Count != 3 || name != "clamp" && arguments.Count < 1) return Failure(SvgCalcParseError.InvalidFunction, start);
        SvgCalcDimension dimension = arguments[0].Dimension;
        int index = 1;
        while (index < arguments.Count)
        {
            dimension = AddDimension(dimension, arguments[index].Dimension);
            if (dimension == SvgCalcDimension.Any) return Failure(SvgCalcParseError.IncompatibleDimensions, arguments[index].StartOffset);
            index = index + 1;
        }
        SvgCalcExpressionKind kind = name == "min" ? SvgCalcExpressionKind.Min : name == "max" ? SvgCalcExpressionKind.Max : SvgCalcExpressionKind.Clamp;
        return Create(kind, dimension, 0, "", arguments, start, reader.GetPosition());
    }

    SvgCalcWorkResult ParseValue()
    {
        int start = reader.GetPosition();
        SvgNumberParseResult number = SvgNumberParser.ReadCssNumber(reader);
        if (!number.Success)
        {
            SvgCalcParseError error = number.Error == SvgNumberParseError.TokenTooLong ? SvgCalcParseError.TokenTooLong : SvgCalcParseError.ExpectedExpression;
            return Failure(error, number.ErrorOffset);
        }
        int unitStart = reader.GetPosition();
        if (!reader.IsEnd() && reader.PeekCharacter().Value == '%') reader.ReadCharacter();
        else while (!reader.IsEnd() && IsAsciiLetter(reader.PeekCharacter().Value)) reader.ReadCharacter();
        if (reader.GetPosition() - start > maximumTokenCharacters) return Failure(SvgCalcParseError.TokenTooLong, start + maximumTokenCharacters);
        string unit = text.Substring(unitStart, reader.GetPosition() - unitStart);
        SvgCalcDimension dimension = UnitDimension(unit);
        if (dimension == SvgCalcDimension.Any) return Failure(SvgCalcParseError.UnknownUnit, unitStart);
        return Create(SvgCalcExpressionKind.Value, dimension, number.Value, unit, new System.Collections.Generic.List<SvgCalcExpression>(), start, reader.GetPosition());
    }

    SvgCalcWorkResult Create(SvgCalcExpressionKind kind, SvgCalcDimension dimension, double value, string unit, System.Collections.Generic.List<SvgCalcExpression> children, int start, int end)
    {
        if (nodeCount >= maximumNodes) return Failure(SvgCalcParseError.NodeLimitExceeded, start);
        nodeCount = nodeCount + 1;
        return new SvgCalcWorkResult(true, SvgCalcParseError.None, new SvgCalcExpression(kind, dimension, value, unit, children, start, end), -1);
    }

    bool SkipSpaceAndComments()
    {
        int start = reader.GetPosition();
        while (true)
        {
            reader.SkipCssWhitespace();
            int position = reader.GetPosition();
            if (position + 1 >= text.Length || text[position] != '/' || text[position + 1] != '*') break;
            position = position + 2;
            while (position + 1 < text.Length && (text[position] != '*' || text[position + 1] != '/')) position = position + 1;
            if (position + 1 >= text.Length) break;
            reader.SetPosition(position + 2);
        }
        return reader.GetPosition() > start;
    }

    bool StartsNumber(int position)
    {
        if (position >= text.Length) return false;
        char first = text[position];
        if (first >= '0' && first <= '9') return true;
        if (first == '.') return position + 1 < text.Length && text[position + 1] >= '0' && text[position + 1] <= '9';
        if (first != '+' && first != '-') return false;
        if (position + 1 >= text.Length) return false;
        char second = text[position + 1];
        if (second >= '0' && second <= '9') return true;
        return second == '.' && position + 2 < text.Length && text[position + 2] >= '0' && text[position + 2] <= '9';
    }

    static SvgCalcDimension UnitDimension(string unit)
    {
        if (unit.Length == 0) return SvgCalcDimension.Number;
        if (unit == "%") return SvgCalcDimension.Percentage;
        if (SvgLengthUnits.Find(unit) >= 0) return SvgCalcDimension.Length;
        if (SvgScalarUnitNames.FindAngle(unit, false) >= 0) return SvgCalcDimension.Angle;
        if (SvgScalarUnitNames.FindTime(unit, false) >= 0) return SvgCalcDimension.Time;
        if (SvgScalarUnitNames.FindFrequency(unit) >= 0) return SvgCalcDimension.Frequency;
        if (SvgScalarUnitNames.FindResolution(unit) >= 0) return SvgCalcDimension.Resolution;
        return SvgCalcDimension.Any;
    }

    static SvgCalcDimension AddDimension(SvgCalcDimension left, SvgCalcDimension right)
    {
        if (left == right) return left;
        bool leftLength = left == SvgCalcDimension.Length || left == SvgCalcDimension.Percentage || left == SvgCalcDimension.LengthPercentage;
        bool rightLength = right == SvgCalcDimension.Length || right == SvgCalcDimension.Percentage || right == SvgCalcDimension.LengthPercentage;
        if (leftLength && rightLength) return SvgCalcDimension.LengthPercentage;
        return SvgCalcDimension.Any;
    }

    static bool IsLiteralZero(SvgCalcExpression expression)
    {
        return expression.Kind == SvgCalcExpressionKind.Value && expression.Dimension == SvgCalcDimension.Number && expression.Value == 0;
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static System.Collections.Generic.List<SvgCalcExpression> CreateSingleChildList(SvgCalcExpression? value)
    {
        System.Collections.Generic.List<SvgCalcExpression> items = new System.Collections.Generic.List<SvgCalcExpression>();
        items.Add((value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        return items;
    }

    static System.Collections.Generic.List<SvgCalcExpression> Pair(SvgCalcExpression first, SvgCalcExpression second)
    {
        System.Collections.Generic.List<SvgCalcExpression> items = new System.Collections.Generic.List<SvgCalcExpression>();
        items.Add(first);
        items.Add(second);
        return items;
    }

    static SvgCalcWorkResult Failure(SvgCalcParseError error, int offset)
    {
        return new SvgCalcWorkResult(false, error, null, offset);
    }
}
