namespace units;

internal class UnitParserState
{
    internal UnitToken[] _tokens;
    internal int _index;
    internal IUnitCatalog _catalog;
    internal UnitParseOptions _options;
    internal bool _failed;
    internal UnitErrorCode _errorCode;
    internal string _message;
    internal int _position;
    internal UnitDefinition[] _candidates;

    internal UnitParserState(UnitToken[] tokens, IUnitCatalog catalog, UnitParseOptions options)
    {
        _tokens = tokens;
        _index = 0;
        _catalog = catalog;
        _options = options;
        _failed = false;
        _errorCode = UnitErrorCode.None;
        _message = "";
        _position = 0;
        _candidates = new UnitDefinition[0];
    }

    internal UnitParseResult Parse()
    {
        UnitDefinition? unit = ParseExpression();
        if (!_failed)
        {
            Expect(UnitTokenKind.End);
        }

        if (_failed)
        {
            return UnitParseResult.CreateFailure(_errorCode, _message, _position, _candidates);
        }

        if (unit == null)
        {
            return UnitParseResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit expression did not produce a unit.", 0, new UnitDefinition[0]);
        }

        return UnitParseResult.CreateSuccess(unit);
    }

    internal UnitDefinition? ParseExpression()
    {
        UnitDefinition? unit = ParseFactor();

        while (!_failed && (Current().GetKind() == UnitTokenKind.Star || Current().GetKind() == UnitTokenKind.Slash))
        {
            UnitToken operationToken = Current();
            UnitTokenKind operation = operationToken.GetKind();
            Advance();
            UnitDefinition? right = ParseFactor();

            if (unit == null || right == null)
            {
                return null;
            }

            if (!unit.CanUseInCompound() || !right.CanUseInCompound())
            {
                Fail(UnitErrorCode.ArithmeticInvalidOperation, "Only scale-only units can be used in compound unit expressions.", operationToken.GetPosition(), new UnitDefinition[0]);
                return null;
            }

            int direction = operation == UnitTokenKind.Star ? 1 : -1;
            if (!CanCombineDimensions(unit.GetDimension(), right.GetDimension(), direction))
            {
                Fail(UnitErrorCode.ArithmeticInvalidOperation, "Compound unit dimension exponents are outside the supported range.", operationToken.GetPosition(), new UnitDefinition[0]);
                return null;
            }

            if (operation == UnitTokenKind.Star)
            {
                unit = unit.Multiply(right);
            }
            else
            {
                unit = unit.Divide(right);
            }
        }

        return unit;
    }

    internal UnitDefinition? ParseFactor()
    {
        UnitDefinition? unit = ParsePrimary();
        if (unit == null || _failed)
        {
            return null;
        }

        if (Current().GetKind() == UnitTokenKind.Caret)
        {
            UnitToken powerToken = Current();
            Advance();
            int power = ParseInteger();
            if (_failed)
            {
                return null;
            }

            if (!unit.CanUseInCompound())
            {
                Fail(UnitErrorCode.ArithmeticInvalidOperation, "Only scale-only units can be raised to a power.", powerToken.GetPosition(), new UnitDefinition[0]);
                return null;
            }

            if (!CanRaiseToPower(unit.GetDimension(), power))
            {
                Fail(UnitErrorCode.ArithmeticInvalidOperation, "Powered unit dimension exponents are outside the supported range.", powerToken.GetPosition(), new UnitDefinition[0]);
                return null;
            }

            if (power == 1 && Current().GetKind() == UnitTokenKind.Slash && Peek().GetKind() == UnitTokenKind.Integer)
            {
                UnitToken rootToken = Current();
                Advance();
                int root = ParseInteger();
                if (_failed)
                {
                    return null;
                }

                if (root <= 0)
                {
                    Fail(UnitErrorCode.ParseInvalidInput, "Canonical unit root must be a positive integer.", rootToken.GetPosition(), new UnitDefinition[0]);
                    return null;
                }

                UnitRootResult rootResult = unit.Root(root);
                if (rootResult.IsFailure())
                {
                    Fail(rootResult.GetErrorCode(), rootResult.GetMessage(), rootToken.GetPosition(), new UnitDefinition[0]);
                    return null;
                }

                return rootResult.GetUnit();
            }

            return unit.Pow(power);
        }

        if (Current().GetKind() == UnitTokenKind.Integer)
        {
            UnitToken powerToken = Current();
            int power = ParseInteger();
            if (_failed)
            {
                return null;
            }

            if (!unit.CanUseInCompound())
            {
                Fail(UnitErrorCode.ArithmeticInvalidOperation, "Only scale-only units can be raised to a power.", powerToken.GetPosition(), new UnitDefinition[0]);
                return null;
            }

            if (!CanRaiseToPower(unit.GetDimension(), power))
            {
                Fail(UnitErrorCode.ArithmeticInvalidOperation, "Powered unit dimension exponents are outside the supported range.", powerToken.GetPosition(), new UnitDefinition[0]);
                return null;
            }

            return unit.Pow(power);
        }

        return unit;
    }

    internal UnitDefinition? ParsePrimary()
    {
        UnitToken token = Current();

        if (token.GetKind() == UnitTokenKind.Identifier)
        {
            Advance();
            return LookupUnit(token);
        }

        if (token.GetKind() == UnitTokenKind.Integer && string.Equals(token.GetText(), "1", System.StringComparison.Ordinal))
        {
            Advance();
            return CreateDimensionlessUnit();
        }

        if (token.GetKind() == UnitTokenKind.OpenParenthesis)
        {
            Advance();
            UnitDefinition? unit = ParseExpression();
            if (_failed)
            {
                return null;
            }

            Expect(UnitTokenKind.CloseParenthesis);
            if (_failed)
            {
                return null;
            }

            return unit;
        }

        if (token.GetKind() == UnitTokenKind.Unknown)
        {
            Fail(UnitErrorCode.ParseInvalidInput, "Unexpected character in unit expression at position " + FormatInt(token.GetPosition()) + ": " + token.GetText(), token.GetPosition(), new UnitDefinition[0]);
            return null;
        }

        Fail(UnitErrorCode.ParseInvalidInput, "Expected unit symbol or parenthesized expression at position " + FormatInt(token.GetPosition()) + ".", token.GetPosition(), new UnitDefinition[0]);
        return null;
    }

    internal UnitDefinition? LookupUnit(UnitToken token)
    {
        UnitLookupResult idResult = _catalog.FindUnitById(token.GetText());
        if (idResult.IsSuccess())
        {
            return idResult.GetUnit();
        }

        if (idResult.IsAmbiguous())
        {
            Fail(UnitErrorCode.ParseAmbiguousUnit, idResult.GetMessage(), token.GetPosition(), idResult.GetUnits());
            return null;
        }

        UnitLookupResult result = UnitParser.ApplyContext(_catalog.FindUnit(token.GetText()), _options, token.GetText(), _catalog);
        if (result.IsSuccess())
        {
            return result.GetUnit();
        }

        if (result.IsAmbiguous())
        {
            Fail(UnitErrorCode.ParseAmbiguousUnit, result.GetMessage(), token.GetPosition(), result.GetUnits());
            return null;
        }

        UnitLookupResult prefixedResult = UnitParser.ApplyContext(FindPrefixedUnit(token.GetText()), _options, token.GetText(), _catalog);
        if (prefixedResult.IsSuccess())
        {
            return prefixedResult.GetUnit();
        }

        if (prefixedResult.IsAmbiguous())
        {
            Fail(UnitErrorCode.ParseAmbiguousUnit, prefixedResult.GetMessage(), token.GetPosition(), prefixedResult.GetUnits());
            return null;
        }

        Fail(UnitErrorCode.ParseUnknownUnit, result.GetMessage(), token.GetPosition(), new UnitDefinition[0]);
        return null;
    }

    internal UnitLookupResult FindPrefixedUnit(string text)
    {
        System.Collections.Generic.List<UnitDefinition> matches = new System.Collections.Generic.List<UnitDefinition>();
        PrefixDefinition[] prefixes = _catalog.GetPrefixes();
        int index = 0;

        while (index < prefixes.Length)
        {
            PrefixDefinition prefix = prefixes[index];
            AddCanonicalPrefixedMatches(matches, prefix, text);
            AddPrefixedMatchesForText(matches, prefix, text, prefix.GetSymbol());
            AddPrefixedMatchesForText(matches, prefix, text, prefix.GetName());
            AddPrefixedMatchesForText(matches, prefix, text, prefix.GetId());

            index = index + 1;
        }

        if (matches.Count > 1)
        {
            return new UnitLookupResult(false, true, matches.ToArray(), UnitErrorCode.LookupAmbiguous, "Prefixed unit lookup was ambiguous: " + text);
        }

        return UnitLookupResult.FromMatches(matches.ToArray(), text);
    }

    internal bool AddCanonicalPrefixedMatches(System.Collections.Generic.List<UnitDefinition> matches, PrefixDefinition prefix, string text)
    {
        string prefixText = prefix.GetId() + "_";
        if (!text.StartsWith(prefixText, System.StringComparison.Ordinal) || text.Length <= prefixText.Length)
        {
            return false;
        }

        string remainder = text.Substring(prefixText.Length);
        UnitLookupResult result = _catalog.FindUnitById(remainder);
        if (result.IsSuccess())
        {
            AddPrefixedMatch(matches, prefix, result.GetUnit());
            return false;
        }

        if (result.IsAmbiguous())
        {
            return AddAmbiguousPrefixedMatches(matches, prefix, result.GetUnits());
        }

        return false;
    }

    internal bool AddPrefixedMatchesForText(System.Collections.Generic.List<UnitDefinition> matches, PrefixDefinition prefix, string text, string prefixText)
    {
        if (!StartsWithPrefix(text, prefixText))
        {
            return false;
        }

        string remainder = text.Substring(prefixText.Length);
        UnitLookupResult result = _catalog.FindUnit(remainder);
        if (result.IsSuccess())
        {
            AddPrefixedMatch(matches, prefix, result.GetUnit());
            return false;
        }

        if (result.IsAmbiguous())
        {
            return AddAmbiguousPrefixedMatches(matches, prefix, result.GetUnits());
        }

        return false;
    }

    internal bool AddAmbiguousPrefixedMatches(System.Collections.Generic.List<UnitDefinition> matches, PrefixDefinition prefix, UnitDefinition[] units)
    {
        bool added = false;
        int index = 0;

        while (index < units.Length)
        {
            if (CanApplyPrefix(units[index]))
            {
                AddUniquePrefixedUnit(matches, CreatePrefixedUnit(prefix, units[index]));
                added = true;
            }

            index = index + 1;
        }

        return added;
    }

    internal void AddPrefixedMatch(System.Collections.Generic.List<UnitDefinition> matches, PrefixDefinition prefix, UnitDefinition unit)
    {
        if (CanApplyPrefix(unit))
        {
            AddUniquePrefixedUnit(matches, CreatePrefixedUnit(prefix, unit));
        }
    }

    internal static bool StartsWithPrefix(string text, string prefixText)
    {
        if (string.IsNullOrEmpty(prefixText))
        {
            return false;
        }

        if (text.Length <= prefixText.Length)
        {
            return false;
        }

        return text.StartsWith(prefixText, System.StringComparison.Ordinal);
    }

    internal static bool CanApplyPrefix(UnitDefinition unit)
    {
        return unit.IsPrefixable() && unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static UnitDefinition CreatePrefixedUnit(PrefixDefinition prefix, UnitDefinition unit)
    {
        return new UnitDefinition(
            prefix.GetId() + "_" + unit.GetId(),
            prefix.GetName() + unit.GetName(),
            prefix.GetName() + unit.GetPluralName(),
            prefix.GetSymbol() + unit.GetSymbol(),
            new string[0],
            unit.GetDimension(),
            prefix.GetFactor().Multiply(unit.GetScaleToSystemUnit()),
            unit.GetOffsetToSystemUnit(),
            false,
            unit.GetConversionKind());
    }

    internal static void AddUniquePrefixedUnit(System.Collections.Generic.List<UnitDefinition> matches, UnitDefinition unit)
    {
        int index = 0;

        while (index < matches.Count)
        {
            if (string.Equals(matches[index].GetId(), unit.GetId(), System.StringComparison.Ordinal))
            {
                return;
            }

            index = index + 1;
        }

        matches.Add(unit);
    }

    internal static bool CanCombineDimensions(DimensionVector left, DimensionVector right, int direction)
    {
        int slot = 0;
        while (slot <= DimensionSlots.Count)
        {
            long value = (long)left.GetExponent(slot) + (long)right.GetExponent(slot) * direction;
            if (value < int.MinValue || value > int.MaxValue)
            {
                return false;
            }

            slot = slot + 1;
        }

        return true;
    }

    internal static bool CanRaiseToPower(DimensionVector dimension, int power)
    {
        if (power == int.MinValue)
        {
            return false;
        }

        int slot = 0;
        while (slot <= DimensionSlots.Count)
        {
            long value = (long)dimension.GetExponent(slot) * power;
            if (value < int.MinValue || value > int.MaxValue)
            {
                return false;
            }

            slot = slot + 1;
        }

        return true;
    }

    internal int ParseInteger()
    {
        UnitToken token = Current();
        if (token.GetKind() != UnitTokenKind.Integer)
        {
            Fail(UnitErrorCode.ParseInvalidInput, "Expected integer exponent at position " + FormatInt(token.GetPosition()) + ".", token.GetPosition(), new UnitDefinition[0]);
            return 0;
        }

        string text = token.GetText();
        bool negative = text[0] == '-';
        bool signed = negative || text[0] == '+';
        int index = signed ? 1 : 0;
        long magnitude = 0;

        while (index < text.Length)
        {
            int digit = text[index] - '0';
            long limitDigit = negative ? 8 : 7;
            if (magnitude > 214748364L || (magnitude == 214748364L && digit > limitDigit))
            {
                Fail(UnitErrorCode.ParseInvalidInput, "Integer exponent is outside the supported range at position " + FormatInt(token.GetPosition()) + ".", token.GetPosition(), new UnitDefinition[0]);
                return 0;
            }

            magnitude = magnitude * 10L + digit;
            index = index + 1;
        }

        Advance();
        if (negative && magnitude == 2147483648L)
        {
            return -2147483647 - 1;
        }

        if (negative)
        {
            return -(int)magnitude;
        }

        return (int)magnitude;
    }

    internal void Expect(UnitTokenKind kind)
    {
        UnitToken token = Current();
        if (token.GetKind() != kind)
        {
            Fail(UnitErrorCode.ParseInvalidInput, "Unexpected token at position " + FormatInt(token.GetPosition()) + ": " + token.GetText(), token.GetPosition(), new UnitDefinition[0]);
            return;
        }

        Advance();
    }

    internal void Fail(UnitErrorCode errorCode, string message, int position, UnitDefinition[] candidates)
    {
        if (_failed)
        {
            return;
        }

        _failed = true;
        _errorCode = errorCode;
        _message = message;
        _position = position;
        _candidates = candidates;
    }

    internal UnitToken Current()
    {
        return _tokens[_index];
    }

    internal UnitToken Peek()
    {
        if (_index < _tokens.Length - 1)
        {
            return _tokens[_index + 1];
        }

        return _tokens[_tokens.Length - 1];
    }

    internal void Advance()
    {
        if (_index < _tokens.Length - 1)
        {
            _index = _index + 1;
        }
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static UnitDefinition CreateDimensionlessUnit()
    {
        return new UnitDefinition(
            "one",
            "one",
            "one",
            "1",
            new string[0],
            DimensionVector.Dimensionless(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
