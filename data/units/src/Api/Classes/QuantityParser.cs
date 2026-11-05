namespace units;

/// <summary>
/// Parses a numeric value followed by a unit expression.
/// </summary>
/// <remarks>
/// The numeric portion uses the configured culture. Invalid or ambiguous unit expressions are surfaced as argument errors by ParseOrThrow.
/// </remarks>
public class QuantityParser : IQuantityParser
{
    internal IUnitParser _unitParser;

    /// <summary>
    /// Initializes a quantity parser with the unit-expression parser to use.
    /// </summary>
    /// <param name="unitParser">The unit-expression parser dependency.</param>
    public QuantityParser(IUnitParser unitParser)
    {
        if (unitParser == null)
        {
            throw new System.ArgumentNullException("unitParser");
        }

        _unitParser = unitParser;
    }

    internal static QuantityParser CreateDefault()
    {
        return new QuantityParser(new UnitParser());
    }

    /// <summary>
    /// Parses the text without throwing for malformed, unknown, ambiguous, or incompatible quantities.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the parsed quantity or structured failure details.</returns>
    public QuantityParseResult Parse(string text, IUnitCatalog registry)
    {
        return ParseWithOptions(text, registry, QuantityParseOptions.CreateDefault());
    }

    /// <summary>
    /// Parses the text with options without throwing for malformed, unknown, ambiguous, or incompatible quantities.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the parsed quantity or structured failure details.</returns>
    public QuantityParseResult ParseWithOptions(string text, IUnitCatalog registry, QuantityParseOptions options)
    {
        if (text == null)
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Quantity expression must not be null.", 0, new UnitDefinition[0]);
        }

        if (registry == null)
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit catalog must not be null.", 0, new UnitDefinition[0]);
        }

        if (options == null)
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Quantity parse options must not be null.", 0, new UnitDefinition[0]);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Quantity expression must not be empty.", 0, new UnitDefinition[0]);
        }

        if (options.IsStrict() && char.IsWhiteSpace(text[0]))
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Strict quantity parsing does not allow leading whitespace.", 0, new UnitDefinition[0]);
        }

        int valueStart = SkipWhitespace(text, 0);
        int valueEnd = ReadNumberEndForResult(text, valueStart, options.GetCulture());
        if (valueEnd < 0)
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Expected quantity value at position " + FormatInt(valueStart) + ".", valueStart, new UnitDefinition[0]);
        }

        if (valueEnd < text.Length && IsMalformedExponentStart(text, valueEnd))
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Invalid quantity exponent at position " + FormatInt(valueEnd) + ".", valueEnd, new UnitDefinition[0]);
        }

        bool hasSeparator = valueEnd < text.Length && char.IsWhiteSpace(text[valueEnd]);
        if (options.IsStrict() && !hasSeparator)
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Strict quantity parsing requires whitespace between value and unit expression.", valueEnd, new UnitDefinition[0]);
        }

        int unitStart = SkipWhitespace(text, valueEnd);
        if (unitStart >= text.Length)
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Expected unit expression after quantity value.", unitStart, new UnitDefinition[0]);
        }

        if (options.IsStrict() && char.IsWhiteSpace(text[text.Length - 1]))
        {
            return QuantityParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Strict quantity parsing does not allow trailing whitespace.", text.Length - 1, new UnitDefinition[0]);
        }

        string valueText = text.Substring(valueStart, valueEnd - valueStart);
        double value = double.Parse(valueText, System.Globalization.NumberStyles.Float, options.GetCulture());
        string unitText = text.Substring(unitStart);
        QuantityKindDefinition? quantityKindContext = null;
        UnitSystemDefinition? unitSystemContext = null;
        if (options.HasQuantityKind())
        {
            quantityKindContext = options.GetQuantityKind();
        }

        if (options.HasUnitSystem())
        {
            unitSystemContext = options.GetUnitSystem();
        }

        UnitParseOptions unitOptions = new UnitParseOptions(options.IsStrict(), quantityKindContext, unitSystemContext);
        UnitParseResult unitResult = _unitParser.ParseWithOptions(unitText, registry, unitOptions);
        if (unitResult.IsFailure())
        {
            return QuantityParseResult.CreateFailure(unitResult.GetErrorCode(), unitResult.GetMessage(), unitStart + unitResult.GetPosition(), unitResult.GetCandidates());
        }

        UnitDefinition unit = unitResult.GetUnit();
        if (options.HasQuantityKind())
        {
            QuantityKindDefinition quantityKind = options.GetQuantityKind();
            if (!QuantityFactory.IsAcceptedByQuantityKind(unit, quantityKind, registry))
            {
                return QuantityParseResult.CreateFailure(UnitErrorCode.ConversionIncompatibleDimensions, "Parsed unit is not accepted for quantity kind: " + quantityKind.GetId(), unitStart, new UnitDefinition[0]);
            }
        }

        return QuantityParseResult.CreateSuccess(new QuantityValue(value, unit));
    }

    /// <summary>
    /// Parses the text and throws when it is not a valid, unambiguous quantity.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The parsed quantity.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown, ambiguous, or incompatible with the requested quantity kind.</exception>
    public QuantityValue ParseOrThrow(string text, IUnitCatalog registry)
    {
        return ParseOrThrowWithOptions(text, registry, QuantityParseOptions.CreateDefault());
    }

    /// <summary>
    /// Parses the text and throws when it is not a valid, unambiguous quantity.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The parsed quantity.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown, ambiguous, or incompatible with the requested quantity kind.</exception>
    public QuantityValue ParseOrThrowWithOptions(string text, IUnitCatalog registry, QuantityParseOptions options)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        QuantityParseResult result = ParseWithOptions(text, registry, options);
        if (result.IsSuccess())
        {
            return result.GetQuantity();
        }

        if (result.GetErrorCode() == UnitErrorCode.ParseInvalidInput)
        {
            throw new System.ArgumentException(result.GetMessage());
        }

        throw new System.InvalidOperationException(result.GetMessage());
    }

    internal static int ReadNumberEndForResult(string text, int start, System.Globalization.CultureInfo culture)
    {
        int namedEnd = ReadNamedNumberEnd(text, start, culture);
        if (namedEnd >= 0)
        {
            return namedEnd;
        }

        int index = start;

        if (index < text.Length && IsSign(text[index]))
        {
            index = index + 1;
        }

        int digitCount = 0;
        int beforeDigits = index;
        index = ReadDigits(text, index);
        digitCount = digitCount + index - beforeDigits;

        string decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        if (StartsWithText(text, index, decimalSeparator))
        {
            index = index + decimalSeparator.Length;
            beforeDigits = index;
            index = ReadDigits(text, index);
            digitCount = digitCount + index - beforeDigits;
        }

        if (digitCount == 0)
        {
            return -1;
        }

        return ReadExponent(text, index);
    }

    internal static int FindValueStart(string text, QuantityParseOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new System.ArgumentException("Quantity expression must not be empty.");
        }

        if (options.IsStrict() && char.IsWhiteSpace(text[0]))
        {
            throw new System.ArgumentException("Strict quantity parsing does not allow leading whitespace.");
        }

        return SkipWhitespace(text, 0);
    }

    internal static int FindUnitStart(string text, int valueEnd, QuantityParseOptions options)
    {
        bool hasSeparator = valueEnd < text.Length && char.IsWhiteSpace(text[valueEnd]);

        if (options.IsStrict() && !hasSeparator)
        {
            throw new System.ArgumentException("Strict quantity parsing requires whitespace between value and unit expression.");
        }

        int unitStart = SkipWhitespace(text, valueEnd);
        if (unitStart >= text.Length)
        {
            throw new System.ArgumentException("Expected unit expression after quantity value.");
        }

        if (options.IsStrict() && char.IsWhiteSpace(text[text.Length - 1]))
        {
            throw new System.ArgumentException("Strict quantity parsing does not allow trailing whitespace.");
        }

        return unitStart;
    }

    internal static int ReadNumberEnd(string text, int start, System.Globalization.CultureInfo culture)
    {
        int namedEnd = ReadNamedNumberEnd(text, start, culture);
        if (namedEnd >= 0)
        {
            return namedEnd;
        }

        int index = start;

        if (index < text.Length && IsSign(text[index]))
        {
            index = index + 1;
        }

        int digitCount = 0;
        int beforeDigits = index;
        index = ReadDigits(text, index);
        digitCount = digitCount + index - beforeDigits;

        string decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        if (StartsWithText(text, index, decimalSeparator))
        {
            index = index + decimalSeparator.Length;
            beforeDigits = index;
            index = ReadDigits(text, index);
            digitCount = digitCount + index - beforeDigits;
        }

        if (digitCount == 0)
        {
            throw new System.ArgumentException("Expected quantity value at position " + FormatInt(start) + ".");
        }

        index = ReadExponent(text, index);
        return index;
    }

    internal static int ReadNamedNumberEnd(string text, int start, System.Globalization.CultureInfo culture)
    {
        string negativeInfinity = culture.NumberFormat.NegativeInfinitySymbol;
        if (StartsWithText(text, start, negativeInfinity))
        {
            return start + negativeInfinity.Length;
        }

        string positiveInfinity = culture.NumberFormat.PositiveInfinitySymbol;
        if (StartsWithText(text, start, positiveInfinity))
        {
            return start + positiveInfinity.Length;
        }

        string notANumber = culture.NumberFormat.NaNSymbol;
        if (StartsWithText(text, start, notANumber))
        {
            return start + notANumber.Length;
        }

        return -1;
    }

    internal static int ReadExponent(string text, int start)
    {
        if (start >= text.Length || !IsExponentMarker(text[start]))
        {
            return start;
        }

        int index = start + 1;
        if (index < text.Length && IsSign(text[index]))
        {
            index = index + 1;
        }

        int digitStart = index;
        index = ReadDigits(text, index);
        if (index == digitStart)
        {
            return start;
        }

        return index;
    }

    internal static int ReadDigits(string text, int start)
    {
        int index = start;

        while (index < text.Length && IsAsciiDigit(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static void ValidateQuantityKind(UnitDefinition unit, QuantityParseOptions options)
    {
        if (!options.HasQuantityKind())
        {
            return;
        }

        QuantityKindDefinition quantityKind = options.GetQuantityKind();
        if (quantityKind.AcceptsUnit(unit))
        {
            return;
        }

        if (unit.GetConversionKind() == UnitConversionKind.Scale && quantityKind.GetDimension().EqualsDimension(unit.GetDimension()))
        {
            return;
        }

        throw new System.InvalidOperationException("Parsed unit is not accepted for quantity kind: " + quantityKind.GetId());
    }

    internal static int SkipWhitespace(string text, int start)
    {
        int index = start;

        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool StartsWithText(string text, int start, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (start + value.Length > text.Length)
        {
            return false;
        }

        return string.Equals(text.Substring(start, value.Length), value, System.StringComparison.Ordinal);
    }

    internal static bool IsSign(char ch)
    {
        return ch == '-' || ch == '+';
    }

    internal static bool IsExponentMarker(char ch)
    {
        return ch == 'e' || ch == 'E';
    }

    internal static bool IsMalformedExponentStart(string text, int start)
    {
        if (!IsExponentMarker(text[start]))
        {
            return false;
        }

        int next = start + 1;
        if (next >= text.Length || char.IsWhiteSpace(text[next]) || text[next] == '+')
        {
            return true;
        }

        if (text[next] == '-')
        {
            return next + 1 >= text.Length || !char.IsLetter(text[next + 1]);
        }

        return false;
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
