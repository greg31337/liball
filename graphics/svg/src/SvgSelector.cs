namespace svg;

/// <summary>Maintains selector parsing over one filtered token range.</summary>
internal class SvgSelectorParserState
{
    string text;
    System.Collections.Generic.List<SvgCssToken> tokens;
    SvgSourceUri source;
    SvgCssLimits limits;
    int pseudoDepth;
    bool relativeMode;
    bool insideHas;
    int position;
    SvgSelectorParseError error;
    int errorOffset;
    string message;
    System.Collections.Generic.List<SvgDiagnostic> warnings;

    /// <summary>Creates selector parser state and removes source-preserved comments.</summary>
    /// <param name="value">The complete CSS source.</param>
    /// <param name="allTokens">The complete token stream.</param>
    /// <param name="start">The first selected token.</param>
    /// <param name="end">The exclusive selected token.</param>
    /// <param name="sourceUri">The source resource.</param>
    /// <param name="cssLimits">The CSS security limits.</param>
    /// <param name="nestedPseudoDepth">The nested selector pseudo depth.</param>
    /// <param name="parseRelative">Whether relative selectors are accepted.</param>
    /// <param name="withinHas">Whether parsing occurs inside <c>:has()</c>.</param>
    internal SvgSelectorParserState(string value, System.Collections.Generic.List<SvgCssToken> allTokens, int start, int end, SvgSourceUri sourceUri, SvgCssLimits cssLimits, int nestedPseudoDepth, bool parseRelative, bool withinHas)
    {
        text = value;
        tokens = new System.Collections.Generic.List<SvgCssToken>();
        int index = start;
        while (index < end)
        {
            if (allTokens[index].Kind != SvgCssTokenKind.Comment) tokens.Add(allTokens[index]);
            index = index + 1;
        }
        source = sourceUri;
        limits = cssLimits;
        pseudoDepth = nestedPseudoDepth;
        relativeMode = parseRelative;
        insideHas = withinHas;
        position = 0;
        error = SvgSelectorParseError.None;
        errorOffset = -1;
        message = "";
        warnings = new System.Collections.Generic.List<SvgDiagnostic>();
    }

    /// <summary>Parses the complete selector list.</summary>
    /// <returns>The selector result.</returns>
    internal SvgSelectorParseResult Parse()
    {
        System.Collections.Generic.List<SvgComplexSelector> items = new System.Collections.Generic.List<SvgComplexSelector>();
        SkipWhitespace();
        int listStart = position;
        if (position >= tokens.Count) SetError(SvgSelectorParseError.ExpectedSelector, CurrentOffset(), "A selector is required.");
        while (position < tokens.Count && error == SvgSelectorParseError.None)
        {
            SvgComplexSelector? item = ParseComplexSelector();
            if (item != null) items.Add(item);
            SkipWhitespace();
            if (position >= tokens.Count) break;
            if (tokens[position].Kind != SvgCssTokenKind.Comma)
            {
                SetError(SvgSelectorParseError.UnexpectedToken, tokens[position].StartOffset, "A comma or the end of the selector list was expected.");
                break;
            }
            position = position + 1;
            SkipWhitespace();
            if (position >= tokens.Count)
            {
                SetError(SvgSelectorParseError.ExpectedSelector, CurrentOffset(), "A selector is required after the comma.");
                break;
            }
        }
        int listEnd = LastSignificantIndex(position);
        SvgSourceSpan span = SpanForRange(listStart, listEnd);
        string rawText = RawText(listStart, listEnd);
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(warnings);
        if (error != SvgSelectorParseError.None)
        {
            SvgCssLocation location = SvgCssTokenizerState.GetLocation(text, errorOffset);
            SvgSourceSpan errorSpan = new SvgSourceSpan(source, errorOffset, location.Line, location.Column, errorOffset, location.Line, location.Column);
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, message, errorSpan));
        }
        SvgSelector selector = new SvgSelector(items, rawText, span, error == SvgSelectorParseError.None, diagnostics);
        SvgSelectorSpecificityCalculator.Populate(selector);
        return new SvgSelectorParseResult(error == SvgSelectorParseError.None, error, selector, error == SvgSelectorParseError.None ? -1 : errorOffset, error == SvgSelectorParseError.None ? "" : message);
    }

    SvgComplexSelector? ParseComplexSelector()
    {
        int start = position;
        System.Collections.Generic.List<SvgCompoundSelector> compounds = new System.Collections.Generic.List<SvgCompoundSelector>();
        System.Collections.Generic.List<SvgSelectorCombinator> combinators = new System.Collections.Generic.List<SvgSelectorCombinator>();
        SvgSelectorCombinator leading = SvgSelectorCombinator.Descendant;
        if (relativeMode && position < tokens.Count && IsCombinator(tokens[position]))
        {
            leading = Combinator(tokens[position]);
            position = position + 1;
            SkipWhitespace();
            if (position >= tokens.Count || tokens[position].Kind == SvgCssTokenKind.Comma)
            {
                SetError(SvgSelectorParseError.TrailingCombinator, CurrentOffset(), "A relative selector is required after the leading combinator.");
                return null;
            }
        }
        SvgCompoundSelector? first = ParseCompoundSelector();
        if (first == null) return null;
        compounds.Add(first);
        int end = position;
        while (position < tokens.Count && error == SvgSelectorParseError.None)
        {
            bool whitespace = SkipWhitespace();
            if (position >= tokens.Count || tokens[position].Kind == SvgCssTokenKind.Comma) break;
            SvgSelectorCombinator combinator;
            if (IsCombinator(tokens[position]))
            {
                combinator = Combinator(tokens[position]);
                position = position + 1;
                SkipWhitespace();
                if (position >= tokens.Count || tokens[position].Kind == SvgCssTokenKind.Comma)
                {
                    SetError(SvgSelectorParseError.TrailingCombinator, CurrentOffset(), "A selector is required after the combinator.");
                    break;
                }
            }
            else if (whitespace) combinator = SvgSelectorCombinator.Descendant;
            else
            {
                SetError(SvgSelectorParseError.UnexpectedToken, tokens[position].StartOffset, "The token cannot continue this compound selector.");
                break;
            }
            SvgCompoundSelector? compound = ParseCompoundSelector();
            if (compound == null) break;
            combinators.Add(combinator);
            compounds.Add(compound);
            end = position;
        }
        if (error != SvgSelectorParseError.None) return null;
        SvgComplexSelector result = new SvgComplexSelector(compounds, combinators, RawText(start, end), SpanForRange(start, end));
        result.IsRelative = relativeMode;
        result.LeadingCombinator = leading;
        return result;
    }

    SvgCompoundSelector? ParseCompoundSelector()
    {
        int start = position;
        System.Collections.Generic.List<SvgSimpleSelector> simple = new System.Collections.Generic.List<SvgSimpleSelector>();
        SvgSimpleSelector? type = ParseTypeOrUniversal();
        if (type != null) simple.Add(type);
        while (position < tokens.Count && error == SvgSelectorParseError.None)
        {
            SvgCssToken token = tokens[position];
            if (token.Kind == SvgCssTokenKind.Hash)
            {
                int idStart = position;
                if (token.HashType != SvgCssHashType.Id)
                {
                    SetError(SvgSelectorParseError.ExpectedName, token.StartOffset, "An ID selector requires an identifier hash.");
                    break;
                }
                position = position + 1;
                simple.Add(Simple(SvgSimpleSelectorKind.Id, token.Value, SvgSelectorNamespaceKind.Default, "", idStart, position));
                continue;
            }
            if (IsDelimiter(token, "."))
            {
                int classStart = position;
                position = position + 1;
                if (position >= tokens.Count || tokens[position].Kind != SvgCssTokenKind.Ident)
                {
                    SetError(SvgSelectorParseError.ExpectedName, CurrentOffset(), "A class selector requires an identifier.");
                    break;
                }
                string name = tokens[position].Value;
                position = position + 1;
                simple.Add(Simple(SvgSimpleSelectorKind.Class, name, SvgSelectorNamespaceKind.Default, "", classStart, position));
                continue;
            }
            if (token.Kind == SvgCssTokenKind.OpenSquare)
            {
                SvgSimpleSelector? attribute = ParseAttributeSelector();
                if (attribute != null) simple.Add(attribute);
                continue;
            }
            if (token.Kind == SvgCssTokenKind.Colon)
            {
                SvgSimpleSelector? pseudo = ParsePseudoClass();
                if (pseudo != null) simple.Add(pseudo);
                continue;
            }
            break;
        }
        if (simple.Count == 0 && error == SvgSelectorParseError.None) SetError(SvgSelectorParseError.ExpectedSelector, CurrentOffset(), "A compound selector is required.");
        if (error != SvgSelectorParseError.None) return null;
        return new SvgCompoundSelector(simple, RawText(start, position), SpanForRange(start, position));
    }

    SvgSimpleSelector? ParseTypeOrUniversal()
    {
        if (position >= tokens.Count) return null;
        int start = position;
        SvgCssToken first = tokens[position];
        SvgSelectorNamespaceKind namespaceKind = SvgSelectorNamespaceKind.Default;
        string prefix = "";
        string name = "";
        bool universal = false;
        if (first.Kind == SvgCssTokenKind.Ident)
        {
            if (HasNamespaceName(position + 1))
            {
                namespaceKind = SvgSelectorNamespaceKind.Named;
                prefix = first.Value;
                position = position + 2;
                universal = IsDelimiter(tokens[position], "*");
                name = universal ? "*" : tokens[position].Value;
                position = position + 1;
            }
            else
            {
                name = first.Value;
                position = position + 1;
            }
        }
        else if (IsDelimiter(first, "*"))
        {
            if (HasNamespaceName(position + 1))
            {
                namespaceKind = SvgSelectorNamespaceKind.Any;
                position = position + 2;
                universal = IsDelimiter(tokens[position], "*");
                name = universal ? "*" : tokens[position].Value;
                position = position + 1;
            }
            else
            {
                universal = true;
                name = "*";
                position = position + 1;
            }
        }
        else if (IsDelimiter(first, "|"))
        {
            if (position + 1 >= tokens.Count || tokens[position + 1].Kind != SvgCssTokenKind.Ident && !IsDelimiter(tokens[position + 1], "*"))
            {
                SetError(SvgSelectorParseError.ExpectedName, first.StartOffset, "A namespace separator requires a type or universal name.");
                return null;
            }
            namespaceKind = SvgSelectorNamespaceKind.None;
            position = position + 1;
            universal = IsDelimiter(tokens[position], "*");
            name = universal ? "*" : tokens[position].Value;
            position = position + 1;
        }
        else return null;
        SvgSimpleSelectorKind kind = universal ? SvgSimpleSelectorKind.Universal : SvgSimpleSelectorKind.Type;
        return Simple(kind, name, namespaceKind, prefix, start, position);
    }

    SvgSimpleSelector? ParsePseudoClass()
    {
        int start = position;
        position = position + 1;
        if (position >= tokens.Count || tokens[position].Kind == SvgCssTokenKind.Colon)
        {
            SetError(SvgSelectorParseError.ExpectedPseudoClass, CurrentOffset(), "A supported pseudo-class name is required.");
            return null;
        }
        SvgCssToken nameToken = tokens[position];
        if (nameToken.Kind == SvgCssTokenKind.Ident)
        {
            SvgPseudoClassKind simpleKind = SimplePseudoKind(nameToken.Value);
            if (simpleKind == SvgPseudoClassKind.None && IsUnsupportedDynamicPseudo(nameToken.Value)) simpleKind = SvgPseudoClassKind.UnsupportedDynamic;
            if (simpleKind == SvgPseudoClassKind.None)
            {
                SetError(SvgSelectorParseError.ExpectedPseudoClass, nameToken.StartOffset, "The pseudo-class is not supported by this selector grammar.");
                return null;
            }
            position = position + 1;
            SvgSimpleSelector simple = Pseudo(simpleKind, nameToken.Value, start, position);
            ApplyPseudoSpecificity(simple);
            if (simpleKind == SvgPseudoClassKind.UnsupportedDynamic) AddUnsupportedDynamicWarning(simple);
            return simple;
        }
        if (nameToken.Kind != SvgCssTokenKind.Function)
        {
            SetError(SvgSelectorParseError.ExpectedPseudoClass, nameToken.StartOffset, "A supported pseudo-class name is required.");
            return null;
        }
        SvgPseudoClassKind kind = FunctionalPseudoKind(nameToken.Value);
        if (kind == SvgPseudoClassKind.None)
        {
            SetError(SvgSelectorParseError.ExpectedPseudoClass, nameToken.StartOffset, "The functional pseudo-class is not supported by this selector grammar.");
            return null;
        }
        if (kind == SvgPseudoClassKind.Has && insideHas)
        {
            SetError(SvgSelectorParseError.NestedHasNotAllowed, nameToken.StartOffset, "A :has() pseudo-class cannot be nested inside :has().");
            return null;
        }
        int functionIndex = position;
        int close = FindMatchingClose(functionIndex);
        if (close < 0)
        {
            SetError(SvgSelectorParseError.ExpectedPseudoArgument, text.Length, "The functional pseudo-class is not closed.");
            return null;
        }
        int argumentStart = functionIndex + 1;
        int argumentEnd = close;
        SvgSimpleSelector pseudo = Pseudo(kind, nameToken.Value, start, close + 1);
        if (kind == SvgPseudoClassKind.Lang)
        {
            System.Collections.Generic.List<string>? languages = ParseLanguages(argumentStart, argumentEnd);
            if (languages == null) return null;
            pseudo.Languages = languages;
        }
        else if (IsNthPseudo(kind))
        {
            SvgNthExpression? nth = ParseNthExpression(kind, argumentStart, argumentEnd);
            if (nth == null) return null;
            pseudo.Nth = nth;
        }
        else
        {
            if (pseudoDepth >= limits.MaxBlockDepth)
            {
                SetError(SvgSelectorParseError.PseudoDepthExceeded, nameToken.StartOffset, "The configured nested selector pseudo depth was exceeded.");
                return null;
            }
            bool relative = kind == SvgPseudoClassKind.Has;
            bool forgiving = kind == SvgPseudoClassKind.Is || kind == SvgPseudoClassKind.Where || kind == SvgPseudoClassKind.Has;
            SvgSelector? argument = forgiving ? ParseForgivingSelectorList(argumentStart, argumentEnd, relative, insideHas || relative) : ParseNestedSelector(argumentStart, argumentEnd, relative, insideHas || relative);
            if (argument == null) return null;
            pseudo.SelectorArgument = argument;
        }
        position = close + 1;
        ApplyPseudoSpecificity(pseudo);
        return pseudo;
    }

    System.Collections.Generic.List<string>? ParseLanguages(int start, int end)
    {
        System.Collections.Generic.List<string> languages = new System.Collections.Generic.List<string>();
        int index = start;
        while (index < end)
        {
            while (index < end && tokens[index].Kind == SvgCssTokenKind.Whitespace) index = index + 1;
            if (index >= end) break;
            SvgCssToken token = tokens[index];
            if (token.Kind != SvgCssTokenKind.Ident && token.Kind != SvgCssTokenKind.String && !IsDelimiter(token, "*"))
            {
                SetError(SvgSelectorParseError.ExpectedPseudoArgument, token.StartOffset, "A :lang() argument must be an identifier, string, or wildcard.");
                return null;
            }
            languages.Add(IsDelimiter(token, "*") ? "*" : token.Value);
            index = index + 1;
            while (index < end && tokens[index].Kind == SvgCssTokenKind.Whitespace) index = index + 1;
            if (index >= end) break;
            if (tokens[index].Kind != SvgCssTokenKind.Comma)
            {
                SetError(SvgSelectorParseError.ExpectedPseudoArgument, tokens[index].StartOffset, "Language ranges must be comma-separated.");
                return null;
            }
            index = index + 1;
            int next = index;
            while (next < end && tokens[next].Kind == SvgCssTokenKind.Whitespace) next = next + 1;
            if (next >= end)
            {
                SetError(SvgSelectorParseError.ExpectedPseudoArgument, tokens[index - 1].EndOffset, "A language range is required after the comma.");
                return null;
            }
        }
        if (languages.Count == 0)
        {
            int offset = start < tokens.Count ? tokens[start].StartOffset : CurrentOffset();
            SetError(SvgSelectorParseError.ExpectedPseudoArgument, offset, "A :lang() argument is required.");
            return null;
        }
        return languages;
    }

    SvgNthExpression? ParseNthExpression(SvgPseudoClassKind kind, int start, int end)
    {
        int formulaEnd = end;
        int selectorStart = -1;
        int index = start;
        while (index < end)
        {
            if (tokens[index].Kind == SvgCssTokenKind.Ident && AsciiEquals(tokens[index].Value, "of") && index > start && tokens[index - 1].Kind == SvgCssTokenKind.Whitespace)
            {
                int next = index + 1;
                if (next < end && tokens[next].Kind == SvgCssTokenKind.Whitespace)
                {
                    formulaEnd = index;
                    while (formulaEnd > start && tokens[formulaEnd - 1].Kind == SvgCssTokenKind.Whitespace) formulaEnd = formulaEnd - 1;
                    selectorStart = next + 1;
                    while (selectorStart < end && tokens[selectorStart].Kind == SvgCssTokenKind.Whitespace) selectorStart = selectorStart + 1;
                    break;
                }
            }
            index = index + 1;
        }
        if (selectorStart >= 0 && kind != SvgPseudoClassKind.NthChild && kind != SvgPseudoClassKind.NthLastChild)
        {
            int offset = selectorStart < end ? tokens[selectorStart].StartOffset : tokens[formulaEnd].StartOffset;
            SetError(SvgSelectorParseError.InvalidNthExpression, offset, "Only child-index pseudo-classes accept an of selector list.");
            return null;
        }
        string formula = NthFormulaText(start, formulaEnd);
        SvgNthCoefficients coefficients = ParseNthFormula(formula);
        if (!coefficients.Success)
        {
            int offset = start < end ? tokens[start].StartOffset : CurrentOffset();
            SetError(SvgSelectorParseError.InvalidNthExpression, offset, "The pseudo-class requires a valid An+B expression.");
            return null;
        }
        SvgSelector? ofSelector = null;
        if (selectorStart >= 0)
        {
            if (selectorStart >= end)
            {
                SetError(SvgSelectorParseError.ExpectedPseudoArgument, tokens[formulaEnd].StartOffset, "An of selector list is required.");
                return null;
            }
            if (pseudoDepth >= limits.MaxBlockDepth)
            {
                SetError(SvgSelectorParseError.PseudoDepthExceeded, tokens[selectorStart].StartOffset, "The configured nested selector pseudo depth was exceeded.");
                return null;
            }
            ofSelector = ParseNestedSelector(selectorStart, end, false, insideHas);
            if (ofSelector == null) return null;
        }
        return new SvgNthExpression(coefficients.A, coefficients.B, ofSelector, RawText(start, end));
    }

    SvgSelector? ParseNestedSelector(int start, int end, bool relative, bool nestedInsideHas)
    {
        SvgSelectorParserState nested = new SvgSelectorParserState(text, tokens, start, end, source, limits, pseudoDepth + 1, relative, nestedInsideHas);
        SvgSelectorParseResult result = nested.Parse();
        if (!result.Success)
        {
            SetError(result.Error, result.ErrorOffset, result.Message);
            return null;
        }
        AppendWarnings(result.Selector.Diagnostics);
        return result.Selector;
    }

    SvgSelector? ParseForgivingSelectorList(int start, int end, bool relative, bool nestedInsideHas)
    {
        System.Collections.Generic.List<SvgComplexSelector> items = new System.Collections.Generic.List<SvgComplexSelector>();
        int segmentStart = start;
        int index = start;
        SvgSelectorParseError nestedError = SvgSelectorParseError.None;
        int nestedErrorOffset = -1;
        string nestedMessage = "";
        System.Collections.Generic.List<SvgCssTokenKind> expected = new System.Collections.Generic.List<SvgCssTokenKind>();
        while (index <= end)
        {
            bool boundary = index == end || expected.Count == 0 && tokens[index].Kind == SvgCssTokenKind.Comma;
            if (boundary)
            {
                int first = segmentStart;
                int last = index;
                while (first < last && tokens[first].Kind == SvgCssTokenKind.Whitespace) first = first + 1;
                while (last > first && tokens[last - 1].Kind == SvgCssTokenKind.Whitespace) last = last - 1;
                if (first < last)
                {
                    SvgSelectorParserState nested = new SvgSelectorParserState(text, tokens, first, last, source, limits, pseudoDepth + 1, relative, nestedInsideHas);
                    SvgSelectorParseResult result = nested.Parse();
                    if (result.Success)
                    {
                        AppendWarnings(result.Selector.Diagnostics);
                        int itemIndex = 0;
                        while (itemIndex < result.Selector.Items.Count)
                        {
                            items.Add(result.Selector.Items[itemIndex]);
                            itemIndex = itemIndex + 1;
                        }
                    }
                    else if (nestedError == SvgSelectorParseError.None && IsNonForgivingNestedError(result.Error))
                    {
                        nestedError = result.Error;
                        nestedErrorOffset = result.ErrorOffset;
                        nestedMessage = result.Message;
                    }
                }
                segmentStart = index + 1;
            }
            if (index < end)
            {
                SvgCssTokenKind kind = tokens[index].Kind;
                if (kind == SvgCssTokenKind.Function || kind == SvgCssTokenKind.OpenParenthesis) expected.Add(SvgCssTokenKind.CloseParenthesis);
                else if (kind == SvgCssTokenKind.OpenSquare) expected.Add(SvgCssTokenKind.CloseSquare);
                else if (kind == SvgCssTokenKind.OpenCurly) expected.Add(SvgCssTokenKind.CloseCurly);
                else if (expected.Count > 0 && kind == expected[expected.Count - 1]) expected.RemoveAt(expected.Count - 1);
            }
            index = index + 1;
        }
        if (items.Count == 0)
        {
            if (nestedError != SvgSelectorParseError.None)
            {
                SetError(nestedError, nestedErrorOffset, nestedMessage);
                return null;
            }
            int offset = start < end ? tokens[start].StartOffset : CurrentOffset();
            SetError(SvgSelectorParseError.ExpectedSelector, offset, "The forgiving selector list requires at least one valid selector.");
            return null;
        }
        SvgSourceSpan span = SpanForRange(start, end);
        return new SvgSelector(items, RawText(start, end), span, true, new System.Collections.Generic.List<SvgDiagnostic>());
    }

    void AddUnsupportedDynamicWarning(SvgSimpleSelector pseudo)
    {
        string messageText = "The dynamic pseudo-class :" + pseudo.PseudoName + " is preserved but is unsupported and will not match.";
        warnings.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.UnsupportedFeature, messageText, pseudo.Source));
    }

    void AppendWarnings(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity != SvgDiagnosticSeverity.Error) warnings.Add(diagnostics[index]);
            index = index + 1;
        }
    }

    static bool IsNonForgivingNestedError(SvgSelectorParseError error)
    {
        return error == SvgSelectorParseError.InvalidArgument ||
            error == SvgSelectorParseError.CharacterLimitExceeded ||
            error == SvgSelectorParseError.TokenLimitExceeded ||
            error == SvgSelectorParseError.PseudoDepthExceeded ||
            error == SvgSelectorParseError.NestedHasNotAllowed;
    }

    int FindMatchingClose(int open)
    {
        System.Collections.Generic.List<SvgCssTokenKind> expected = new System.Collections.Generic.List<SvgCssTokenKind>();
        expected.Add(SvgCssTokenKind.CloseParenthesis);
        int index = open + 1;
        while (index < tokens.Count)
        {
            SvgCssTokenKind kind = tokens[index].Kind;
            if (kind == SvgCssTokenKind.Function || kind == SvgCssTokenKind.OpenParenthesis) expected.Add(SvgCssTokenKind.CloseParenthesis);
            else if (kind == SvgCssTokenKind.OpenSquare) expected.Add(SvgCssTokenKind.CloseSquare);
            else if (kind == SvgCssTokenKind.OpenCurly) expected.Add(SvgCssTokenKind.CloseCurly);
            else if (expected.Count > 0 && kind == expected[expected.Count - 1])
            {
                expected.RemoveAt(expected.Count - 1);
                if (expected.Count == 0) return index;
            }
            index = index + 1;
        }
        return -1;
    }

    string NthFormulaText(int start, int end)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = start;
        while (index < end)
        {
            SvgCssToken token = tokens[index];
            if (token.Kind == SvgCssTokenKind.Ident) builder.Append(token.Value);
            else if (token.Kind == SvgCssTokenKind.Dimension)
            {
                builder.Append(token.Value);
                builder.Append(token.Unit);
            }
            else if (token.Kind == SvgCssTokenKind.Number || token.Kind == SvgCssTokenKind.Delimiter) builder.Append(token.Value);
            else if (token.Kind != SvgCssTokenKind.Whitespace) builder.Append(token.RawText);
            index = index + 1;
        }
        return AsciiLower(builder.ToString());
    }

    static SvgNthCoefficients ParseNthFormula(string formula)
    {
        if (formula == "odd") return new SvgNthCoefficients(true, 2, 1);
        if (formula == "even") return new SvgNthCoefficients(true, 2, 0);
        int n = formula.IndexOf('n');
        if (n < 0)
        {
            SvgNthInteger integer = ParseNthInteger(formula, true);
            return integer.Success ? new SvgNthCoefficients(true, 0, integer.Value) : new SvgNthCoefficients(false, 0, 0);
        }
        if (formula.IndexOf('n', n + 1) >= 0) return new SvgNthCoefficients(false, 0, 0);
        string aText = formula.Substring(0, n);
        string bText = formula.Substring(n + 1);
        int a;
        if (aText.Length == 0 || aText == "+") a = 1;
        else if (aText == "-") a = -1;
        else
        {
            SvgNthInteger parsedA = ParseNthInteger(aText, true);
            if (!parsedA.Success) return new SvgNthCoefficients(false, 0, 0);
            a = parsedA.Value;
        }
        int b = 0;
        if (bText.Length > 0)
        {
            if (bText[0] != '+' && bText[0] != '-') return new SvgNthCoefficients(false, 0, 0);
            SvgNthInteger parsedB = ParseNthInteger(bText, true);
            if (!parsedB.Success) return new SvgNthCoefficients(false, 0, 0);
            b = parsedB.Value;
        }
        return new SvgNthCoefficients(true, a, b);
    }

    static SvgNthInteger ParseNthInteger(string value, bool allowSign)
    {
        if (value.Length == 0) return new SvgNthInteger(false, 0);
        int index = 0;
        bool negative = false;
        if (allowSign && (value[index] == '+' || value[index] == '-'))
        {
            negative = value[index] == '-';
            index = index + 1;
        }
        if (index >= value.Length) return new SvgNthInteger(false, 0);
        long maximum = negative ? 2147483648L : 2147483647L;
        long magnitude = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < '0' || current > '9') return new SvgNthInteger(false, 0);
            int digit = current - '0';
            if (magnitude > (maximum - digit) / 10) return new SvgNthInteger(false, 0);
            magnitude = magnitude * 10 + digit;
            index = index + 1;
        }
        if (negative && magnitude == 2147483648L) return new SvgNthInteger(true, -2147483647 - 1);
        return new SvgNthInteger(true, negative ? -(int)magnitude : (int)magnitude);
    }

    void ApplyPseudoSpecificity(SvgSimpleSelector pseudo)
    {
        if (pseudo.PseudoClass == SvgPseudoClassKind.Where) return;
        if (pseudo.PseudoClass == SvgPseudoClassKind.Is || pseudo.PseudoClass == SvgPseudoClassKind.Not || pseudo.PseudoClass == SvgPseudoClassKind.Has)
        {
            SvgSelectorSpecificity maximum = MaximumSpecificity(pseudo.SelectorArgument);
            pseudo.SpecificityIds = maximum.Ids;
            pseudo.SpecificityClasses = maximum.Classes;
            pseudo.SpecificityTypes = maximum.Types;
            return;
        }
        pseudo.SpecificityClasses = 1;
        if (pseudo.Nth != null && pseudo.Nth.OfSelector != null)
        {
            SvgSelectorSpecificity maximum = MaximumSpecificity(pseudo.Nth.OfSelector);
            pseudo.SpecificityIds = maximum.Ids;
            pseudo.SpecificityClasses = pseudo.SpecificityClasses + maximum.Classes;
            pseudo.SpecificityTypes = maximum.Types;
        }
    }

    static SvgSelectorSpecificity MaximumSpecificity(SvgSelector? selector)
    {
        SvgSelectorSpecificity maximum = new SvgSelectorSpecificity(0, 0, 0);
        if (selector == null) return maximum;
        int index = 0;
        while (index < selector.Items.Count)
        {
            SvgSelectorSpecificity current = ComplexSpecificity(selector.Items[index]);
            if (current.IsGreaterThan(maximum)) maximum = current;
            index = index + 1;
        }
        return maximum;
    }

    static SvgSelectorSpecificity ComplexSpecificity(SvgComplexSelector selector)
    {
        int ids = 0;
        int classes = 0;
        int types = 0;
        int compoundIndex = 0;
        while (compoundIndex < selector.Compounds.Count)
        {
            System.Collections.Generic.List<SvgSimpleSelector> simple = selector.Compounds[compoundIndex].SimpleSelectors;
            int simpleIndex = 0;
            while (simpleIndex < simple.Count)
            {
                SvgSimpleSelector item = simple[simpleIndex];
                if (item.Kind == SvgSimpleSelectorKind.Id) ids = ids + 1;
                else if (item.Kind == SvgSimpleSelectorKind.Class || item.Kind == SvgSimpleSelectorKind.Attribute) classes = classes + 1;
                else if (item.Kind == SvgSimpleSelectorKind.Type) types = types + 1;
                else if (item.Kind == SvgSimpleSelectorKind.PseudoClass)
                {
                    ids = ids + item.SpecificityIds;
                    classes = classes + item.SpecificityClasses;
                    types = types + item.SpecificityTypes;
                }
                simpleIndex = simpleIndex + 1;
            }
            compoundIndex = compoundIndex + 1;
        }
        return new SvgSelectorSpecificity(ids, classes, types);
    }

    SvgSimpleSelector Pseudo(SvgPseudoClassKind kind, string name, int start, int end)
    {
        SvgSimpleSelector pseudo = new SvgSimpleSelector(SvgSimpleSelectorKind.PseudoClass, name, SvgSelectorNamespaceKind.Default, "", SvgAttributeSelectorOperator.Exists, "", SvgAttributeSelectorCase.Default, false, RawText(start, end), SpanForRange(start, end));
        pseudo.PseudoClass = kind;
        pseudo.PseudoName = name;
        return pseudo;
    }

    static bool IsNthPseudo(SvgPseudoClassKind kind)
    {
        return kind == SvgPseudoClassKind.NthChild || kind == SvgPseudoClassKind.NthLastChild || kind == SvgPseudoClassKind.NthOfType || kind == SvgPseudoClassKind.NthLastOfType;
    }

    static SvgPseudoClassKind SimplePseudoKind(string name)
    {
        if (AsciiEquals(name, "root")) return SvgPseudoClassKind.Root;
        if (AsciiEquals(name, "scope")) return SvgPseudoClassKind.Scope;
        if (AsciiEquals(name, "empty")) return SvgPseudoClassKind.Empty;
        if (AsciiEquals(name, "first-child")) return SvgPseudoClassKind.FirstChild;
        if (AsciiEquals(name, "last-child")) return SvgPseudoClassKind.LastChild;
        if (AsciiEquals(name, "only-child")) return SvgPseudoClassKind.OnlyChild;
        if (AsciiEquals(name, "first-of-type")) return SvgPseudoClassKind.FirstOfType;
        if (AsciiEquals(name, "last-of-type")) return SvgPseudoClassKind.LastOfType;
        if (AsciiEquals(name, "only-of-type")) return SvgPseudoClassKind.OnlyOfType;
        if (AsciiEquals(name, "link")) return SvgPseudoClassKind.Link;
        if (AsciiEquals(name, "visited")) return SvgPseudoClassKind.Visited;
        if (AsciiEquals(name, "hover")) return SvgPseudoClassKind.Hover;
        if (AsciiEquals(name, "active")) return SvgPseudoClassKind.Active;
        if (AsciiEquals(name, "focus")) return SvgPseudoClassKind.Focus;
        if (AsciiEquals(name, "target")) return SvgPseudoClassKind.Target;
        return SvgPseudoClassKind.None;
    }

    static bool IsUnsupportedDynamicPseudo(string name)
    {
        return AsciiEquals(name, "any-link") ||
            AsciiEquals(name, "local-link") ||
            AsciiEquals(name, "target-within") ||
            AsciiEquals(name, "focus-visible") ||
            AsciiEquals(name, "focus-within") ||
            AsciiEquals(name, "enabled") ||
            AsciiEquals(name, "disabled") ||
            AsciiEquals(name, "checked") ||
            AsciiEquals(name, "indeterminate") ||
            AsciiEquals(name, "default") ||
            AsciiEquals(name, "required") ||
            AsciiEquals(name, "optional") ||
            AsciiEquals(name, "valid") ||
            AsciiEquals(name, "invalid") ||
            AsciiEquals(name, "in-range") ||
            AsciiEquals(name, "out-of-range") ||
            AsciiEquals(name, "read-only") ||
            AsciiEquals(name, "read-write") ||
            AsciiEquals(name, "blank") ||
            AsciiEquals(name, "placeholder-shown") ||
            AsciiEquals(name, "user-valid") ||
            AsciiEquals(name, "user-invalid") ||
            AsciiEquals(name, "autofill") ||
            AsciiEquals(name, "defined") ||
            AsciiEquals(name, "fullscreen") ||
            AsciiEquals(name, "modal") ||
            AsciiEquals(name, "open") ||
            AsciiEquals(name, "closed") ||
            AsciiEquals(name, "popover-open") ||
            AsciiEquals(name, "picture-in-picture") ||
            AsciiEquals(name, "current") ||
            AsciiEquals(name, "past") ||
            AsciiEquals(name, "future") ||
            AsciiEquals(name, "playing") ||
            AsciiEquals(name, "paused") ||
            AsciiEquals(name, "seeking") ||
            AsciiEquals(name, "buffering") ||
            AsciiEquals(name, "stalled") ||
            AsciiEquals(name, "muted") ||
            AsciiEquals(name, "volume-locked");
    }

    static SvgPseudoClassKind FunctionalPseudoKind(string name)
    {
        if (AsciiEquals(name, "nth-child")) return SvgPseudoClassKind.NthChild;
        if (AsciiEquals(name, "nth-last-child")) return SvgPseudoClassKind.NthLastChild;
        if (AsciiEquals(name, "nth-of-type")) return SvgPseudoClassKind.NthOfType;
        if (AsciiEquals(name, "nth-last-of-type")) return SvgPseudoClassKind.NthLastOfType;
        if (AsciiEquals(name, "lang")) return SvgPseudoClassKind.Lang;
        if (AsciiEquals(name, "not")) return SvgPseudoClassKind.Not;
        if (AsciiEquals(name, "is")) return SvgPseudoClassKind.Is;
        if (AsciiEquals(name, "where")) return SvgPseudoClassKind.Where;
        if (AsciiEquals(name, "has")) return SvgPseudoClassKind.Has;
        return SvgPseudoClassKind.None;
    }

    static string AsciiLower(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current >= 'A' && current <= 'Z') current = (char)(current + 32);
            result.Append(current);
            index = index + 1;
        }
        return result.ToString();
    }

    SvgSimpleSelector? ParseAttributeSelector()
    {
        int start = position;
        position = position + 1;
        SkipWhitespace();
        SvgSelectorNamespaceKind namespaceKind = SvgSelectorNamespaceKind.None;
        string prefix = "";
        string name = "";
        if (position < tokens.Count && tokens[position].Kind == SvgCssTokenKind.Ident)
        {
            if (position + 2 < tokens.Count && IsDelimiter(tokens[position + 1], "|") && tokens[position + 2].Kind == SvgCssTokenKind.Ident)
            {
                namespaceKind = SvgSelectorNamespaceKind.Named;
                prefix = tokens[position].Value;
                name = tokens[position + 2].Value;
                position = position + 3;
            }
            else
            {
                name = tokens[position].Value;
                position = position + 1;
            }
        }
        else if (position + 2 < tokens.Count && IsDelimiter(tokens[position], "*") && IsDelimiter(tokens[position + 1], "|") && tokens[position + 2].Kind == SvgCssTokenKind.Ident)
        {
            namespaceKind = SvgSelectorNamespaceKind.Any;
            name = tokens[position + 2].Value;
            position = position + 3;
        }
        else if (position + 1 < tokens.Count && IsDelimiter(tokens[position], "|") && tokens[position + 1].Kind == SvgCssTokenKind.Ident)
        {
            namespaceKind = SvgSelectorNamespaceKind.None;
            name = tokens[position + 1].Value;
            position = position + 2;
        }
        else
        {
            SetError(SvgSelectorParseError.ExpectedName, CurrentOffset(), "An attribute selector requires an attribute name.");
            return null;
        }
        SkipWhitespace();
        SvgAttributeSelectorOperator attributeOperator = SvgAttributeSelectorOperator.Exists;
        string value = "";
        bool valueIsString = false;
        SvgAttributeSelectorCase attributeCase = SvgAttributeSelectorCase.Default;
        if (position < tokens.Count && tokens[position].Kind == SvgCssTokenKind.CloseSquare)
        {
            position = position + 1;
            return Attribute(name, namespaceKind, prefix, attributeOperator, value, attributeCase, valueIsString, start, position);
        }
        attributeOperator = ParseAttributeOperator();
        if (error != SvgSelectorParseError.None) return null;
        SkipWhitespace();
        if (position >= tokens.Count || tokens[position].Kind != SvgCssTokenKind.Ident && tokens[position].Kind != SvgCssTokenKind.String)
        {
            SetError(SvgSelectorParseError.ExpectedAttributeValue, CurrentOffset(), "An attribute selector comparison value is required.");
            return null;
        }
        valueIsString = tokens[position].Kind == SvgCssTokenKind.String;
        value = tokens[position].Value;
        position = position + 1;
        bool flagWhitespace = SkipWhitespace();
        if (position < tokens.Count && tokens[position].Kind == SvgCssTokenKind.Ident)
        {
            if (!flagWhitespace)
            {
                SetError(SvgSelectorParseError.ExpectedCloseBracket, tokens[position].StartOffset, "An attribute selector flag requires separating whitespace.");
                return null;
            }
            if (AsciiEquals(tokens[position].Value, "i")) attributeCase = SvgAttributeSelectorCase.Insensitive;
            else if (AsciiEquals(tokens[position].Value, "s")) attributeCase = SvgAttributeSelectorCase.Sensitive;
            else
            {
                SetError(SvgSelectorParseError.ExpectedCloseBracket, tokens[position].StartOffset, "Only the i or s attribute selector flag is allowed.");
                return null;
            }
            position = position + 1;
            SkipWhitespace();
        }
        if (position >= tokens.Count || tokens[position].Kind != SvgCssTokenKind.CloseSquare)
        {
            SetError(SvgSelectorParseError.ExpectedCloseBracket, CurrentOffset(), "An attribute selector requires a closing bracket.");
            return null;
        }
        position = position + 1;
        return Attribute(name, namespaceKind, prefix, attributeOperator, value, attributeCase, valueIsString, start, position);
    }

    SvgAttributeSelectorOperator ParseAttributeOperator()
    {
        if (position >= tokens.Count)
        {
            SetError(SvgSelectorParseError.ExpectedAttributeOperator, CurrentOffset(), "An attribute selector operator was expected.");
            return SvgAttributeSelectorOperator.Exists;
        }
        if (IsDelimiter(tokens[position], "="))
        {
            position = position + 1;
            return SvgAttributeSelectorOperator.Equals;
        }
        if (position + 1 >= tokens.Count || !IsDelimiter(tokens[position + 1], "="))
        {
            SetError(SvgSelectorParseError.ExpectedAttributeOperator, tokens[position].StartOffset, "An attribute selector operator was expected.");
            return SvgAttributeSelectorOperator.Exists;
        }
        string value = tokens[position].Value;
        position = position + 2;
        if (value == "~") return SvgAttributeSelectorOperator.Includes;
        if (value == "|") return SvgAttributeSelectorOperator.DashMatch;
        if (value == "^") return SvgAttributeSelectorOperator.PrefixMatch;
        if (value == "$") return SvgAttributeSelectorOperator.SuffixMatch;
        if (value == "*") return SvgAttributeSelectorOperator.SubstringMatch;
        SetError(SvgSelectorParseError.ExpectedAttributeOperator, tokens[position - 2].StartOffset, "The attribute selector operator is not recognized.");
        return SvgAttributeSelectorOperator.Exists;
    }

    SvgSimpleSelector Simple(SvgSimpleSelectorKind kind, string name, SvgSelectorNamespaceKind namespaceKind, string prefix, int start, int end)
    {
        return new SvgSimpleSelector(kind, name, namespaceKind, prefix, SvgAttributeSelectorOperator.Exists, "", SvgAttributeSelectorCase.Default, false, RawText(start, end), SpanForRange(start, end));
    }

    SvgSimpleSelector Attribute(string name, SvgSelectorNamespaceKind namespaceKind, string prefix, SvgAttributeSelectorOperator attributeOperator, string value, SvgAttributeSelectorCase attributeCase, bool valueIsString, int start, int end)
    {
        return new SvgSimpleSelector(SvgSimpleSelectorKind.Attribute, name, namespaceKind, prefix, attributeOperator, value, attributeCase, valueIsString, RawText(start, end), SpanForRange(start, end));
    }

    bool HasNamespaceName(int pipeIndex)
    {
        if (pipeIndex < 0 || pipeIndex + 1 >= tokens.Count || !IsDelimiter(tokens[pipeIndex], "|")) return false;
        return tokens[pipeIndex + 1].Kind == SvgCssTokenKind.Ident || IsDelimiter(tokens[pipeIndex + 1], "*");
    }

    bool SkipWhitespace()
    {
        bool skipped = false;
        while (position < tokens.Count && tokens[position].Kind == SvgCssTokenKind.Whitespace)
        {
            skipped = true;
            position = position + 1;
        }
        return skipped;
    }

    int LastSignificantIndex(int end)
    {
        int index = end;
        while (index > 0 && index <= tokens.Count && tokens[index - 1].Kind == SvgCssTokenKind.Whitespace) index = index - 1;
        return index;
    }

    void SetError(SvgSelectorParseError parseError, int offset, string errorMessage)
    {
        if (error != SvgSelectorParseError.None) return;
        error = parseError;
        errorOffset = offset;
        message = errorMessage;
    }

    int CurrentOffset()
    {
        return position < tokens.Count ? tokens[position].StartOffset : text.Length;
    }

    string RawText(int start, int end)
    {
        if (start < 0 || end <= start || start >= tokens.Count) return "";
        int actualEnd = end > tokens.Count ? tokens.Count : end;
        int startOffset = tokens[start].StartOffset;
        int endOffset = tokens[actualEnd - 1].EndOffset;
        return text.Substring(startOffset, endOffset - startOffset);
    }

    SvgSourceSpan SpanForRange(int start, int end)
    {
        if (start >= 0 && end > start && start < tokens.Count)
        {
            int actualEnd = end > tokens.Count ? tokens.Count : end;
            SvgCssToken first = tokens[start];
            SvgCssToken last = tokens[actualEnd - 1];
            return new SvgSourceSpan(source, first.StartOffset, first.StartLine, first.StartColumn, last.EndOffset, last.EndLine, last.EndColumn);
        }
        int offset = CurrentOffset();
        SvgCssLocation location = SvgCssTokenizerState.GetLocation(text, offset);
        return new SvgSourceSpan(source, offset, location.Line, location.Column, offset, location.Line, location.Column);
    }

    static bool IsDelimiter(SvgCssToken token, string value)
    {
        return token.Kind == SvgCssTokenKind.Delimiter && token.Value == value;
    }

    static bool IsCombinator(SvgCssToken token)
    {
        return IsDelimiter(token, ">") || IsDelimiter(token, "+") || IsDelimiter(token, "~");
    }

    static SvgSelectorCombinator Combinator(SvgCssToken token)
    {
        if (token.Value == ">") return SvgSelectorCombinator.Child;
        if (token.Value == "+") return SvgSelectorCombinator.AdjacentSibling;
        return SvgSelectorCombinator.GeneralSibling;
    }

    static bool AsciiEquals(string left, string right)
    {
        if (left.Length != right.Length) return false;
        int index = 0;
        while (index < left.Length)
        {
            char first = left[index];
            char second = right[index];
            if (first >= 'A' && first <= 'Z') first = (char)(first + 32);
            if (second >= 'A' && second <= 'Z') second = (char)(second + 32);
            if (first != second) return false;
            index = index + 1;
        }
        return true;
    }
}

/// <summary>Contains a non-throwing signed nth-integer parse.</summary>
internal class SvgNthInteger
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The parsed integer.</summary>
    public int Value;

    /// <summary>Creates an nth-integer result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="value">The parsed value.</param>
    internal SvgNthInteger(bool success, int value)
    {
        Success = success;
        Value = value;
    }
}

/// <summary>Contains parsed An+B coefficients.</summary>
internal class SvgNthCoefficients
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The integer coefficient.</summary>
    public int A;
    /// <summary>The integer offset.</summary>
    public int B;

    /// <summary>Creates an An+B coefficient result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="a">The coefficient.</param>
    /// <param name="b">The offset.</param>
    internal SvgNthCoefficients(bool success, int a, int b)
    {
        Success = success;
        A = a;
        B = b;
    }
}

/// <summary>Contains selector specificity components for pseudo argument selection.</summary>
internal class SvgSelectorSpecificity
{
    /// <summary>The ID component.</summary>
    public int Ids;
    /// <summary>The class-like component.</summary>
    public int Classes;
    /// <summary>The type component.</summary>
    public int Types;

    /// <summary>Creates specificity components.</summary>
    /// <param name="ids">The ID component.</param>
    /// <param name="classes">The class-like component.</param>
    /// <param name="types">The type component.</param>
    internal SvgSelectorSpecificity(int ids, int classes, int types)
    {
        Ids = ids;
        Classes = classes;
        Types = types;
    }

    /// <summary>Tests lexicographic specificity ordering.</summary>
    /// <param name="other">The specificity to compare.</param>
    /// <returns><c>true</c> when this specificity is greater.</returns>
    internal bool IsGreaterThan(SvgSelectorSpecificity other)
    {
        if (Ids != other.Ids) return Ids > other.Ids;
        if (Classes != other.Classes) return Classes > other.Classes;
        return Types > other.Types;
    }
}
