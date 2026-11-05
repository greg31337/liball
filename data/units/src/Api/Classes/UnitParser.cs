namespace units;

/// <summary>
/// Parses unit expressions against a unit registry.
/// </summary>
/// <remarks>
/// Expressions may contain multiplication, division, parentheses, integer powers, exact roots, escaped canonical identifiers, and registered prefixes. The canonical subset is deterministic and whitespace-free; relaxed parsing additionally accepts symbols, names, aliases, and suffix powers. This is a compact UCUM-inspired grammar, not a complete UCUM implementation.
/// </remarks>
public class UnitParser : IUnitParser
{
    /// <summary>
    /// Initializes an empty unit parser with default values.
    /// </summary>
    public UnitParser()
    {
    }

    /// <summary>
    /// Parses the text without throwing for malformed, unknown, or ambiguous unit expressions.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the parsed unit or structured failure details.</returns>
    public UnitParseResult Parse(string text, IUnitCatalog registry)
    {
        return ParseWithOptions(text, registry, UnitParseOptions.CreateDefault());
    }

    /// <summary>
    /// Parses the text with options without throwing for malformed, unknown, or ambiguous unit expressions.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control whitespace and ambiguity narrowing.</param>
    /// <returns>A result containing the parsed unit or structured failure details.</returns>
    public UnitParseResult ParseWithOptions(string text, IUnitCatalog registry, UnitParseOptions options)
    {
        if (text == null)
        {
            return UnitParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit expression must not be null.", 0, new UnitDefinition[0]);
        }

        if (registry == null)
        {
            return UnitParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit catalog must not be null.", 0, new UnitDefinition[0]);
        }

        if (options == null)
        {
            return UnitParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit parse options must not be null.", 0, new UnitDefinition[0]);
        }

        if (options.IsStrict())
        {
            int whitespacePosition = FindFirstWhitespace(text);
            if (whitespacePosition >= 0)
            {
                return UnitParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Strict unit parsing does not allow whitespace.", whitespacePosition, new UnitDefinition[0]);
            }
        }

        string lookupText = text.Trim();
        int lookupPosition = FindFirstNonWhitespace(text);
        if (lookupText.Length > 0)
        {
            UnitLookupResult idResult = registry.FindUnitById(lookupText);
            if (idResult.IsSuccess())
            {
                return UnitParseResult.CreateSuccess(idResult.GetUnit());
            }

            if (idResult.IsAmbiguous())
            {
                return UnitParseResult.CreateFailure(UnitErrorCode.ParseAmbiguousUnit, idResult.GetMessage(), lookupPosition, idResult.GetUnits());
            }

            UnitLookupResult exactResult = registry.FindUnit(lookupText);
            if (exactResult.IsSuccess())
            {
                return UnitParseResult.CreateSuccess(exactResult.GetUnit());
            }

            if (exactResult.IsAmbiguous())
            {
                UnitLookupResult narrowedResult = ApplyContext(exactResult, options, lookupText, registry);
                if (narrowedResult.IsSuccess())
                {
                    return UnitParseResult.CreateSuccess(narrowedResult.GetUnit());
                }

                if (narrowedResult.IsAmbiguous())
                {
                    return UnitParseResult.CreateFailure(UnitErrorCode.ParseAmbiguousUnit, narrowedResult.GetMessage(), lookupPosition, narrowedResult.GetUnits());
                }

                return UnitParseResult.CreateFailure(UnitErrorCode.ParseUnknownUnit, "Unit was not found for the supplied parse context: " + lookupText, lookupPosition, new UnitDefinition[0]);
            }
        }

        UnitTokenizer tokenizer = new UnitTokenizer();
        UnitParserState state = new UnitParserState(tokenizer.Tokenize(text), registry, options);
        return state.Parse();
    }

    /// <summary>
    /// Parses the text and throws when it is not a valid, unambiguous unit expression.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The parsed unit definition.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown or the lookup is ambiguous.</exception>
    public UnitDefinition ParseOrThrow(string text, IUnitCatalog registry)
    {
        return ParseOrThrowWithOptions(text, registry, UnitParseOptions.CreateDefault());
    }

    /// <summary>
    /// Parses the text with options and throws when it is not a valid, unambiguous unit expression.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control whitespace and ambiguity narrowing.</param>
    /// <returns>The parsed unit definition.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown or the lookup is ambiguous.</exception>
    public UnitDefinition ParseOrThrowWithOptions(string text, IUnitCatalog registry, UnitParseOptions options)
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

        UnitParseResult result = ParseWithOptions(text, registry, options);
        if (result.IsSuccess())
        {
            return result.GetUnit();
        }

        if (result.GetErrorCode() == UnitErrorCode.ParseInvalidInput)
        {
            throw new System.ArgumentException(result.GetMessage());
        }

        throw new System.InvalidOperationException(result.GetMessage());
    }

    internal static UnitLookupResult ApplyContext(UnitLookupResult result, UnitParseOptions options, string text, IUnitCatalog catalog)
    {
        if (!result.IsAmbiguous() || (!options.HasQuantityKind() && !options.HasUnitSystem()))
        {
            return result;
        }

        UnitDefinition[] narrowed = NarrowCandidates(result.GetUnits(), options, catalog);
        return UnitLookupResult.FromMatches(narrowed, text);
    }

    internal static UnitDefinition[] NarrowCandidates(UnitDefinition[] candidates, UnitParseOptions options, IUnitCatalog catalog)
    {
        UnitDefinition[] narrowed = UnitLookupResult.CopyUnits(candidates);

        if (options.HasQuantityKind())
        {
            QuantityKindDefinition quantityKind = options.GetQuantityKind();
            System.Collections.Generic.List<UnitDefinition> accepted = new System.Collections.Generic.List<UnitDefinition>();
            int acceptedIndex = 0;
            while (acceptedIndex < narrowed.Length)
            {
                if (quantityKind.AcceptsUnit(narrowed[acceptedIndex]))
                {
                    accepted.Add(narrowed[acceptedIndex]);
                }

                acceptedIndex = acceptedIndex + 1;
            }

            if (accepted.Count == 0)
            {
                acceptedIndex = 0;
                while (acceptedIndex < narrowed.Length)
                {
                    UnitDefinition candidate = narrowed[acceptedIndex];
                    if (QuantityFactory.IsAcceptedByQuantityKind(candidate, quantityKind, catalog))
                    {
                        accepted.Add(candidate);
                    }

                    acceptedIndex = acceptedIndex + 1;
                }
            }

            narrowed = accepted.ToArray();
        }

        if (options.HasUnitSystem())
        {
            UnitSystemDefinition unitSystem = options.GetUnitSystem();
            System.Collections.Generic.List<UnitDefinition> systemMatches = new System.Collections.Generic.List<UnitDefinition>();
            string? preferredUnitId = null;
            if (options.HasQuantityKind())
            {
                preferredUnitId = unitSystem.FindPreferredUnitId(options.GetQuantityKind().GetId());
            }

            int systemIndex = 0;
            while (systemIndex < narrowed.Length)
            {
                UnitDefinition candidate = narrowed[systemIndex];
                bool matches = false;
                if (preferredUnitId != null)
                {
                    matches = string.Equals(candidate.GetId(), preferredUnitId, System.StringComparison.Ordinal);
                }
                else
                {
                    matches = UnitBelongsToSystem(candidate.GetId(), unitSystem);
                }

                if (matches)
                {
                    systemMatches.Add(candidate);
                }

                systemIndex = systemIndex + 1;
            }

            narrowed = systemMatches.ToArray();
        }

        return narrowed;
    }

    internal static bool UnitBelongsToSystem(string unitId, UnitSystemDefinition unitSystem)
    {
        if (unitSystem.UsesBaseUnitId(unitId))
        {
            return true;
        }

        string[] preferredUnitIds = unitSystem.GetPreferredUnitIds();
        int index = 0;
        while (index < preferredUnitIds.Length)
        {
            if (string.Equals(preferredUnitIds[index], unitId, System.StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int FindFirstWhitespace(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindFirstNonWhitespace(string text)
    {
        int index = 0;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }
}
