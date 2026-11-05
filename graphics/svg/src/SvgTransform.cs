namespace svg;

/// <summary>Defines stable failures produced by SVG transform-list parsing.</summary>
internal enum SvgTransformParseError
{
    /// <summary>No parse failure occurred.</summary>
    None = 0,

    /// <summary>A transform function was required.</summary>
    ExpectedTransform = 1,

    /// <summary>The transform function name was unknown.</summary>
    UnknownTransform = 2,

    /// <summary>An opening parenthesis was required.</summary>
    ExpectedOpenParenthesis = 3,

    /// <summary>A numeric parameter was required.</summary>
    ExpectedParameter = 4,

    /// <summary>A numeric parameter was malformed, too long, or non-finite.</summary>
    InvalidNumber = 5,

    /// <summary>The transform function had the wrong parameter count.</summary>
    InvalidParameterCount = 6,

    /// <summary>A closing parenthesis was required.</summary>
    ExpectedCloseParenthesis = 7,

    /// <summary>A required parameter or transform separator was absent.</summary>
    MissingSeparator = 8,

    /// <summary>A function-name token exceeded its configured bound.</summary>
    TokenTooLong = 9,

    /// <summary>The configured transform count was exceeded.</summary>
    TransformLimitExceeded = 10,

    /// <summary>A parser argument was invalid.</summary>
    InvalidArgument = 11
}

/// <summary>Contains a non-throwing SVG transform-list parse result.</summary>
internal class SvgTransformParseResult
{
    /// <summary>Indicates whether the complete value parsed successfully.</summary>
    public bool Success;

    /// <summary>The transform-list failure kind.</summary>
    public SvgTransformParseError Error;

    /// <summary>The underlying numeric failure kind.</summary>
    public SvgNumberParseError NumberError;

    /// <summary>The parsed list or successfully parsed prefix.</summary>
    public SvgTransformList TransformList;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a transform-list parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The transform-list failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="transformList">The parsed list or valid prefix.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgTransformParseResult(bool success, SvgTransformParseError error, SvgNumberParseError numberError, SvgTransformList transformList, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        TransformList = transformList;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Parses bounded SVG transform lists.</summary>
internal static class SvgTransformParser
{
    /// <summary>Parses a complete SVG transform list, including an empty list.</summary>
    /// <param name="value">The transform-list text.</param>
    /// <param name="maxTransforms">The maximum transform function count.</param>
    /// <param name="maxTokenCharacters">The maximum function-name or number token length.</param>
    /// <returns>A parsed transform list or stable failure with its valid prefix.</returns>
    public static SvgTransformParseResult Parse(string? value, int maxTransforms, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgTransform> transforms = new System.Collections.Generic.List<SvgTransform>();
        if (value == null || maxTransforms < 1 || maxTokenCharacters < 1)
        {
            return Failure(SvgTransformParseError.InvalidArgument, SvgNumberParseError.None, transforms, 0);
        }

        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        if (reader.IsEnd())
        {
            return new SvgTransformParseResult(true, SvgTransformParseError.None, SvgNumberParseError.None, new SvgTransformList(transforms), -1);
        }

        while (true)
        {
            if (transforms.Count >= maxTransforms)
            {
                return Failure(SvgTransformParseError.TransformLimitExceeded, SvgNumberParseError.None, transforms, reader.GetPosition());
            }
            SvgFunctionParseResult function = ParseFunction(reader);
            if (!function.Success)
            {
                return Failure(function.Error, function.NumberError, transforms, function.ErrorOffset);
            }
            transforms.Add((function.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));

            int whitespace = reader.SkipWhitespace();
            if (reader.IsEnd())
            {
                return new SvgTransformParseResult(true, SvgTransformParseError.None, SvgNumberParseError.None, new SvgTransformList(transforms), -1);
            }
            if (reader.Consume(','))
            {
                reader.SkipWhitespace();
                if (reader.IsEnd())
                {
                    return Failure(SvgTransformParseError.ExpectedTransform, SvgNumberParseError.None, transforms, reader.GetPosition());
                }
                continue;
            }
            if (whitespace == 0)
            {
                return Failure(SvgTransformParseError.MissingSeparator, SvgNumberParseError.None, transforms, reader.GetPosition());
            }
        }
    }

    static SvgFunctionParseResult ParseFunction(SvgSpanReader reader)
    {
        int nameOffset = reader.GetPosition();
        SvgSpanTokenResult nameToken = reader.ReadToken(IsAsciiLetter, false);
        if (!nameToken.Success)
        {
            return FunctionFailure(NameError(nameToken), SvgNumberParseError.None, nameToken.ErrorOffset);
        }
        if (nameToken.RawText.Length == 0)
        {
            return FunctionFailure(SvgTransformParseError.ExpectedTransform, SvgNumberParseError.None, nameOffset);
        }
        int kindValue = SvgTransformRules.FindKind(nameToken.GetValue());
        if (kindValue < 0)
        {
            return FunctionFailure(SvgTransformParseError.UnknownTransform, SvgNumberParseError.None, nameOffset);
        }
        SvgTransformKind kind = (SvgTransformKind)kindValue;
        reader.SkipWhitespace();
        if (!reader.Consume('('))
        {
            return FunctionFailure(SvgTransformParseError.ExpectedOpenParenthesis, SvgNumberParseError.None, reader.GetPosition());
        }
        reader.SkipWhitespace();

        System.Collections.Generic.List<double> values = new System.Collections.Generic.List<double>();
        System.Collections.Generic.List<int> offsets = new System.Collections.Generic.List<int>();
        int closeOffset = reader.GetPosition();
        if (reader.Consume(')'))
        {
            return FunctionFailure(SvgTransformParseError.InvalidParameterCount, SvgNumberParseError.None, closeOffset);
        }
        while (true)
        {
            int numberOffset = reader.GetPosition();
            SvgNumberParseResult number = SvgNumberParser.ReadNumber(reader);
            if (!number.Success)
            {
                SvgTransformParseError error = number.Error == SvgNumberParseError.ExpectedNumber ? SvgTransformParseError.ExpectedParameter : SvgTransformParseError.InvalidNumber;
                return FunctionFailure(error, number.Error, number.ErrorOffset);
            }
            values.Add(number.Value);
            offsets.Add(numberOffset);

            reader.SkipWhitespace();
            closeOffset = reader.GetPosition();
            if (reader.Consume(')'))
            {
                break;
            }
            if (reader.IsEnd())
            {
                return FunctionFailure(SvgTransformParseError.ExpectedCloseParenthesis, SvgNumberParseError.None, reader.GetPosition());
            }
            if (reader.Consume(','))
            {
                reader.SkipWhitespace();
                if (reader.IsEnd() || reader.PeekCharacter().Value == ')')
                {
                    return FunctionFailure(SvgTransformParseError.ExpectedParameter, SvgNumberParseError.None, reader.GetPosition());
                }
                continue;
            }
            continue;
        }

        if (!SvgTransformRules.IsParameterCount(kind, values.Count))
        {
            int maximum = SvgTransformRules.MaximumParameterCount(kind);
            int errorOffset = values.Count > maximum ? offsets[maximum] : closeOffset;
            return FunctionFailure(SvgTransformParseError.InvalidParameterCount, SvgNumberParseError.None, errorOffset);
        }
        SvgTransform transform = new SvgTransform(kind, values.ToArray());
        return new SvgFunctionParseResult(true, SvgTransformParseError.None, SvgNumberParseError.None, transform, -1);
    }

    static SvgTransformParseError NameError(SvgSpanTokenResult token)
    {
        return token.Error == SvgSpanReadError.TokenTooLong ? SvgTransformParseError.TokenTooLong : SvgTransformParseError.InvalidArgument;
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static SvgTransformParseResult Failure(SvgTransformParseError error, SvgNumberParseError numberError, System.Collections.Generic.List<SvgTransform> transforms, int errorOffset)
    {
        return new SvgTransformParseResult(false, error, numberError, new SvgTransformList(transforms), errorOffset);
    }

    static SvgFunctionParseResult FunctionFailure(SvgTransformParseError error, SvgNumberParseError numberError, int errorOffset)
    {
        return new SvgFunctionParseResult(false, error, numberError, null, errorOffset);
    }
}

/// <summary>Contains one internal transform-function parse result.</summary>
internal class SvgFunctionParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgTransformParseError Error;
    /// <summary>The numeric failure kind.</summary>
    public SvgNumberParseError NumberError;
    /// <summary>The parsed transform, or <c>null</c>.</summary>
    public SvgTransform? Transform;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;

    /// <summary>Creates a transform-function parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="numberError">The numeric failure kind.</param>
    /// <param name="transform">The parsed transform, or <c>null</c>.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgFunctionParseResult(bool success, SvgTransformParseError error, SvgNumberParseError numberError, SvgTransform? transform, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Transform = transform;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Defines SVG transform parameter counts and exact function names.</summary>
internal static class SvgTransformRules
{
    /// <summary>Tests whether a transform kind is known.</summary>
    /// <param name="kind">The kind to test.</param>
    /// <returns><c>true</c> for a known transform kind.</returns>
    public static bool IsKind(SvgTransformKind kind)
    {
        return kind >= SvgTransformKind.Matrix && kind <= SvgTransformKind.SkewY;
    }

    /// <summary>Tests whether a parameter count is valid for a transform kind.</summary>
    /// <param name="kind">The transform kind.</param>
    /// <param name="count">The parameter count.</param>
    /// <returns><c>true</c> when the count is accepted.</returns>
    public static bool IsParameterCount(SvgTransformKind kind, int count)
    {
        switch (kind)
        {
            case SvgTransformKind.Matrix: return count == 6;
            case SvgTransformKind.Translate: return count == 1 || count == 2;
            case SvgTransformKind.Scale: return count == 1 || count == 2;
            case SvgTransformKind.Rotate: return count == 1 || count == 3;
            case SvgTransformKind.SkewX: return count == 1;
            case SvgTransformKind.SkewY: return count == 1;
            default: return false;
        }
    }

    /// <summary>Returns the maximum accepted parameter count for a transform kind.</summary>
    /// <param name="kind">The transform kind.</param>
    /// <returns>The maximum count, or zero for an unknown kind.</returns>
    public static int MaximumParameterCount(SvgTransformKind kind)
    {
        switch (kind)
        {
            case SvgTransformKind.Matrix: return 6;
            case SvgTransformKind.Translate: return 2;
            case SvgTransformKind.Scale: return 2;
            case SvgTransformKind.Rotate: return 3;
            case SvgTransformKind.SkewX: return 1;
            case SvgTransformKind.SkewY: return 1;
            default: return 0;
        }
    }

    /// <summary>Finds an exact SVG transform function name.</summary>
    /// <param name="name">The function name.</param>
    /// <returns>The transform kind value, or <c>-1</c>.</returns>
    public static int FindKind(string name)
    {
        if (name == "matrix") return (int)SvgTransformKind.Matrix;
        if (name == "translate") return (int)SvgTransformKind.Translate;
        if (name == "scale") return (int)SvgTransformKind.Scale;
        if (name == "rotate") return (int)SvgTransformKind.Rotate;
        if (name == "skewX") return (int)SvgTransformKind.SkewX;
        if (name == "skewY") return (int)SvgTransformKind.SkewY;
        return -1;
    }

    /// <summary>Returns the exact SVG transform function name.</summary>
    /// <param name="kind">The transform kind.</param>
    /// <returns>The function name, or an empty string for an unknown kind.</returns>
    public static string KindText(SvgTransformKind kind)
    {
        switch (kind)
        {
            case SvgTransformKind.Matrix: return "matrix";
            case SvgTransformKind.Translate: return "translate";
            case SvgTransformKind.Scale: return "scale";
            case SvgTransformKind.Rotate: return "rotate";
            case SvgTransformKind.SkewX: return "skewX";
            case SvgTransformKind.SkewY: return "skewY";
            default: return "";
        }
    }
}

/// <summary>Formats SVG transform functions and lists.</summary>
internal static class SvgTransformFormatter
{
    /// <summary>Formats one transform function.</summary>
    /// <param name="transform">The transform to format.</param>
    /// <returns>Canonical text, or an empty string for invalid state.</returns>
    public static string Format(SvgTransform transform)
    {
        if (!ValidTransform(transform))
        {
            return "";
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(SvgTransformRules.KindText(transform.Kind));
        builder.Append('(');
        int index = 0;
        while (index < transform.Values.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }
            SvgNumberFormatResult number = SvgNumberFormatter.FormatShortest(transform.Values[index]);
            builder.Append(number.Text);
            index = index + 1;
        }
        builder.Append(')');
        return builder.ToString();
    }

    /// <summary>Formats one transform list.</summary>
    /// <param name="list">The transform list.</param>
    /// <returns>Canonical text, or an empty string for invalid state.</returns>
    public static string FormatList(SvgTransformList? list)
    {
        if (list == null || list.Items == null)
        {
            return "";
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < list.Items.Count)
        {
            string text = Format(list.Items[index]);
            if (text.Length == 0)
            {
                return "";
            }
            if (index > 0)
            {
                builder.Append(' ');
            }
            builder.Append(text);
            index = index + 1;
        }
        return builder.ToString();
    }

    static bool ValidTransform(SvgTransform? transform)
    {
        if (transform == null || transform.Values == null || !SvgTransformRules.IsKind(transform.Kind) || !SvgTransformRules.IsParameterCount(transform.Kind, transform.Values.Length))
        {
            return false;
        }
        int index = 0;
        while (index < transform.Values.Length)
        {
            if (!SvgFiniteMath.IsFinite(transform.Values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}

/// <summary>Builds and composes finite affine matrices for SVG transforms.</summary>
internal static class SvgTransformMatrixBuilder
{
    /// <summary>Builds the matrix for one transform.</summary>
    /// <param name="transform">The source transform.</param>
    /// <returns>A finite matrix or stable failure.</returns>
    public static SvgTransformMatrixResult Build(SvgTransform transform)
    {
        if (!ValidTransform(transform))
        {
            return Failure(SvgTransformMatrixError.InvalidArgument);
        }
        double[] values = transform.Values;
        switch (transform.Kind)
        {
            case SvgTransformKind.Matrix:
                return Matrix(values[0], values[1], values[2], values[3], values[4], values[5]);
            case SvgTransformKind.Translate:
                return Matrix(1, 0, 0, 1, values[0], values.Length == 2 ? values[1] : 0);
            case SvgTransformKind.Scale:
                return Matrix(values[0], 0, 0, values.Length == 2 ? values[1] : values[0], 0, 0);
            case SvgTransformKind.Rotate:
                return Rotation(values);
            case SvgTransformKind.SkewX:
                return Matrix(1, 0, System.Math.Tan(ToRadians(values[0])), 1, 0, 0);
            case SvgTransformKind.SkewY:
                return Matrix(1, System.Math.Tan(ToRadians(values[0])), 0, 1, 0, 0);
            default:
                return Failure(SvgTransformMatrixError.InvalidArgument);
        }
    }

    /// <summary>Composes a complete transform list in SVG order.</summary>
    /// <param name="list">The transform list.</param>
    /// <returns>The finite composed matrix or stable failure.</returns>
    public static SvgTransformMatrixResult Compose(SvgTransformList? list)
    {
        if (list == null || list.Items == null)
        {
            return Failure(SvgTransformMatrixError.InvalidArgument);
        }
        SvgTransformMatrixResult current = Matrix(1, 0, 0, 1, 0, 0);
        int index = 0;
        while (index < list.Items.Count)
        {
            SvgTransformMatrixResult next = Build(list.Items[index]);
            if (!next.Success)
            {
                return next;
            }
            current = Multiply(current.Matrix, next.Matrix);
            if (!current.Success)
            {
                return current;
            }
            index = index + 1;
        }
        return current;
    }

    static SvgTransformMatrixResult Rotation(double[] values)
    {
        double radians = ToRadians(values[0]);
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        double e = 0;
        double f = 0;
        if (values.Length == 3)
        {
            e = values[1] - cosine * values[1] + sine * values[2];
            f = values[2] - sine * values[1] - cosine * values[2];
        }
        return Matrix(cosine, sine, -sine, cosine, e, f);
    }

    static double ToRadians(double degrees)
    {
        return degrees / 180 * System.Math.PI;
    }

    static SvgTransformMatrixResult Multiply(SvgMatrix? left, SvgMatrix? right)
    {
        double a = (left ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A * (right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A + left.C * right.B;
        double b = left.B * right.A + left.D * right.B;
        double c = left.A * right.C + left.C * right.D;
        double d = left.B * right.C + left.D * right.D;
        double e = left.A * right.E + left.C * right.F + left.E;
        double f = left.B * right.E + left.D * right.F + left.F;
        return Matrix(a, b, c, d, e, f);
    }

    static SvgTransformMatrixResult Matrix(double a, double b, double c, double d, double e, double f)
    {
        if (!SvgFiniteMath.IsFinite(a) || !SvgFiniteMath.IsFinite(b) || !SvgFiniteMath.IsFinite(c) || !SvgFiniteMath.IsFinite(d) || !SvgFiniteMath.IsFinite(e) || !SvgFiniteMath.IsFinite(f))
        {
            return Failure(SvgTransformMatrixError.NonFiniteResult);
        }
        return new SvgTransformMatrixResult(true, SvgTransformMatrixError.None, new SvgMatrix(a, b, c, d, e, f));
    }

    static SvgTransformMatrixResult Failure(SvgTransformMatrixError error)
    {
        return new SvgTransformMatrixResult(false, error, null);
    }

    static bool ValidTransform(SvgTransform? transform)
    {
        if (transform == null || transform.Values == null || !SvgTransformRules.IsKind(transform.Kind) || !SvgTransformRules.IsParameterCount(transform.Kind, transform.Values.Length))
        {
            return false;
        }
        int index = 0;
        while (index < transform.Values.Length)
        {
            if (!SvgFiniteMath.IsFinite(transform.Values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
