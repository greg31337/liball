namespace svg;

/// <summary>Validates and expands supported CSS shorthand declarations.</summary>
public class SvgCssDeclarationParser : ISvgCssDeclarationParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCssDeclarationParser()
    {
    }

    /// <summary>Expands a recognized shorthand while preserving its cascade provenance.</summary>
    /// <param name="declaration">The authored declaration.</param>
    /// <param name="catalog">The active property catalog.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The ordered longhands or a located recoverable failure.</returns>
    public SvgCssDeclarationExpansionResult Expand(SvgCssDeclaration? declaration, SvgPropertyCatalog catalog, SvgCssLimits limits)
    {
        System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded = new System.Collections.Generic.List<SvgCssExpandedDeclaration>();
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        if (declaration == null || catalog == null || limits == null || limits.MaxDeclarations < 1 || limits.MaxTokens < 1 || limits.MaxCharacters < 1)
        {
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Shorthand expansion arguments are invalid.", declaration == null ? null : declaration.Source));
            return new SvgCssDeclarationExpansionResult(false, SvgCssDeclarationExpansionError.InvalidArgument, expanded, diagnostics);
        }
        SvgPropertySupportEntry? entry = catalog.Find(declaration.Name);
        declaration.IsShorthand = entry != null && entry.IsShorthand;
        declaration.ExpandedDeclarations.Clear();
        if (entry == null || !entry.IsShorthand) return new SvgCssDeclarationExpansionResult(true, SvgCssDeclarationExpansionError.None, expanded, diagnostics);
        if (entry.Longhands.Count > limits.MaxDeclarations)
        {
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The configured CSS declaration count was exceeded during shorthand expansion.", declaration.Source));
            return new SvgCssDeclarationParser().Failure(declaration, SvgCssDeclarationExpansionError.DeclarationLimitExceeded, expanded, diagnostics);
        }
        string value = declaration.ValueText.Trim();
        if (value.Length == 0)
        {
            diagnostics.Add(new SvgCssDeclarationParser().InvalidDiagnostic(declaration, "A shorthand value cannot be empty."));
            return new SvgCssDeclarationParser().Failure(declaration, SvgCssDeclarationExpansionError.InvalidValue, expanded, diagnostics);
        }
        if (new SvgCssDeclarationParser().IsCssWideKeyword(value))
        {
            new SvgCssDeclarationParser().ExpandSameValue(declaration, entry, value, expanded);
            return new SvgCssDeclarationParser().Success(declaration, expanded, diagnostics);
        }
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, limits.MaxTokens, limits.MaxCharacters);
        if (!tokenized.Success)
        {
            SvgDiagnosticCode code = tokenized.Error == SvgCssTokenizeError.InvalidArgument ? SvgDiagnosticCode.InvalidArgument : SvgDiagnosticCode.InputLimitExceeded;
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, tokenized.Message, declaration.Source));
            SvgCssDeclarationExpansionError expansionError = tokenized.Error == SvgCssTokenizeError.InvalidArgument ? SvgCssDeclarationExpansionError.InvalidArgument : SvgCssDeclarationExpansionError.DeclarationLimitExceeded;
            return new SvgCssDeclarationParser().Failure(declaration, expansionError, expanded, diagnostics);
        }
        if (new SvgCssDeclarationParser().ContainsVariableFunction(tokenized.Tokens))
        {
            new SvgCssDeclarationParser().ExpandSameValue(declaration, entry, value, expanded);
            return new SvgCssDeclarationParser().Success(declaration, expanded, diagnostics);
        }
        System.Collections.Generic.List<SvgShorthandToken> tokens = new SvgCssDeclarationParser().ShorthandTokens(value, tokenized.Tokens);
        bool valid = false;
        if (new SvgCssDeclarationParser().PropertyNameEquals(entry.Name, SvgCssPropertyNames.Marker)) valid = new SvgCssDeclarationParser().ExpandMarker(declaration, entry, tokens, expanded);
        else if (new SvgCssDeclarationParser().PropertyNameEquals(entry.Name, SvgCssPropertyNames.Overflow)) valid = new SvgCssDeclarationParser().ExpandOverflow(declaration, entry, tokens, expanded);
        else if (new SvgCssDeclarationParser().PropertyNameEquals(entry.Name, SvgCssPropertyNames.TextDecoration)) valid = new SvgCssDeclarationParser().ExpandTextDecoration(declaration, entry, tokens, catalog, limits, expanded);
        else if (new SvgCssDeclarationParser().PropertyNameEquals(entry.Name, SvgCssPropertyNames.Font)) valid = new SvgCssDeclarationParser().ExpandFont(declaration, entry, tokens, catalog, limits, expanded);
        else if (new SvgCssDeclarationParser().PropertyNameEquals(entry.Name, SvgCssPropertyNames.Animation)) valid = new SvgCssDeclarationParser().ExpandAnimation(declaration, entry, tokens, expanded);
        else
        {
            diagnostics.Add(new SvgCssDeclarationParser().InvalidDiagnostic(declaration, "The caller-defined shorthand has no expansion algorithm."));
            return new SvgCssDeclarationParser().Failure(declaration, SvgCssDeclarationExpansionError.UnsupportedShorthand, expanded, diagnostics);
        }
        if (!valid)
        {
            expanded.Clear();
            diagnostics.Add(new SvgCssDeclarationParser().InvalidDiagnostic(declaration, "The shorthand value does not match its property grammar."));
            return new SvgCssDeclarationParser().Failure(declaration, SvgCssDeclarationExpansionError.InvalidValue, expanded, diagnostics);
        }
        return new SvgCssDeclarationParser().Success(declaration, expanded, diagnostics);
    }
bool ExpandMarker(SvgCssDeclaration declaration, SvgPropertySupportEntry entry, System.Collections.Generic.List<SvgShorthandToken> tokens, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        if (tokens.Count != 1) return false;
        SvgShorthandToken token = tokens[0];
        bool valid = token.Kind == SvgCssTokenKind.Ident && new SvgCssDeclarationParser().AsciiEquals(token.Value, "none") || token.Kind == SvgCssTokenKind.Url || token.IsFunction && new SvgCssDeclarationParser().AsciiEquals(token.Value, "url");
        if (!valid) return false;
        new SvgCssDeclarationParser().ExpandSameValue(declaration, entry, token.RawText, expanded);
        return true;
    }
bool ExpandAnimation(SvgCssDeclaration declaration, SvgPropertySupportEntry entry, System.Collections.Generic.List<SvgShorthandToken> tokens, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        if (tokens.Count == 0 || entry.Longhands.Count != 8) return false;
        System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> durations = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> easings = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> delays = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> iterations = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> directions = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> fills = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> states = new System.Collections.Generic.List<string>();
        int start = 0;
        int index = 0;
        while (index <= tokens.Count)
        {
            if (index == tokens.Count || new SvgCssDeclarationParser().IsComma(tokens[index]))
            {
                if (!new SvgCssDeclarationParser().ExpandAnimationItem(tokens, start, index, names, durations, easings, delays, iterations, directions, fills, states)) return false;
                start = index + 1;
            }
            index = index + 1;
        }
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[0], new SvgCssDeclarationParser().JoinComma(names), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[1], new SvgCssDeclarationParser().JoinComma(durations), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[2], new SvgCssDeclarationParser().JoinComma(easings), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[3], new SvgCssDeclarationParser().JoinComma(delays), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[4], new SvgCssDeclarationParser().JoinComma(iterations), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[5], new SvgCssDeclarationParser().JoinComma(directions), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[6], new SvgCssDeclarationParser().JoinComma(fills), expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[7], new SvgCssDeclarationParser().JoinComma(states), expanded);
        return true;
    }
bool ExpandAnimationItem(System.Collections.Generic.List<SvgShorthandToken> tokens, int start, int end, System.Collections.Generic.List<string> names, System.Collections.Generic.List<string> durations, System.Collections.Generic.List<string> easings, System.Collections.Generic.List<string> delays, System.Collections.Generic.List<string> iterations, System.Collections.Generic.List<string> directions, System.Collections.Generic.List<string> fills, System.Collections.Generic.List<string> states)
    {
        if (start >= end) return false;
        string name = "none";
        string duration = "0s";
        string easing = "ease";
        string delay = "0s";
        string iteration = "1";
        string direction = "normal";
        string fill = "none";
        string state = "running";
        bool nameSeen = false;
        bool durationSeen = false;
        bool delaySeen = false;
        bool easingSeen = false;
        bool iterationSeen = false;
        bool directionSeen = false;
        bool fillSeen = false;
        bool stateSeen = false;
        int index = start;
        while (index < end)
        {
            SvgShorthandToken token = tokens[index];
            if (new SvgCssDeclarationParser().IsTime(token))
            {
                if (!durationSeen) { if (token.NumberValue < 0) return false; duration = token.RawText; durationSeen = true; }
                else if (!delaySeen) { delay = token.RawText; delaySeen = true; }
                else return false;
            }
            else if (!easingSeen && new SvgCssDeclarationParser().IsAnimationEasing(token)) { easing = token.RawText; easingSeen = true; }
            else if (!iterationSeen && new SvgCssDeclarationParser().IsAnimationIteration(token)) { iteration = token.RawText; iterationSeen = true; }
            else if (!directionSeen && new SvgCssDeclarationParser().IsAnimationDirection(token)) { direction = token.RawText; directionSeen = true; }
            else if (!fillSeen && new SvgCssDeclarationParser().IsAnimationFill(token)) { fill = token.RawText; fillSeen = true; }
            else if (!stateSeen && new SvgCssDeclarationParser().IsAnimationState(token)) { state = token.RawText; stateSeen = true; }
            else if (!nameSeen && (token.Kind == SvgCssTokenKind.Ident || token.Kind == SvgCssTokenKind.String)) { name = token.RawText; nameSeen = true; }
            else return false;
            index = index + 1;
        }
        names.Add(name); durations.Add(duration); easings.Add(easing); delays.Add(delay); iterations.Add(iteration); directions.Add(direction); fills.Add(fill); states.Add(state);
        return true;
    }
bool ExpandOverflow(SvgCssDeclaration declaration, SvgPropertySupportEntry entry, System.Collections.Generic.List<SvgShorthandToken> tokens, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        if (tokens.Count < 1 || tokens.Count > 2 || entry.Longhands.Count != 2) return false;
        if (!new SvgCssDeclarationParser().IsOverflowKeyword(tokens[0]) || tokens.Count == 2 && !new SvgCssDeclarationParser().IsOverflowKeyword(tokens[1])) return false;
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[0], tokens[0].RawText, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[1], tokens.Count == 1 ? tokens[0].RawText : tokens[1].RawText, expanded);
        return true;
    }
bool ExpandTextDecoration(SvgCssDeclaration declaration, SvgPropertySupportEntry entry, System.Collections.Generic.List<SvgShorthandToken> tokens, SvgPropertyCatalog catalog, SvgCssLimits limits, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        if (tokens.Count == 0) return false;
        string line = "";
        string style = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.TextDecorationStyle);
        string color = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.TextDecorationColor);
        bool styleSeen = false;
        bool colorSeen = false;
        bool noneSeen = false;
        System.Collections.Generic.List<string> lineKeywords = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < tokens.Count)
        {
            SvgShorthandToken token = tokens[index];
            if (token.Kind == SvgCssTokenKind.Ident && new SvgCssDeclarationParser().IsTextDecorationLine(token.Value))
            {
                string normalized = token.Value.ToLowerInvariant();
                if (new SvgCssDeclarationParser().ContainsText(lineKeywords, normalized)) return false;
                if (normalized == "none")
                {
                    if (lineKeywords.Count != 0) return false;
                    noneSeen = true;
                }
                else
                {
                    if (noneSeen) return false;
                    lineKeywords.Add(normalized);
                }
            }
            else if (token.Kind == SvgCssTokenKind.Ident && new SvgCssDeclarationParser().IsTextDecorationStyle(token.Value))
            {
                if (styleSeen) return false;
                style = token.RawText;
                styleSeen = true;
            }
            else
            {
                if (colorSeen || !new SvgColorParser().Parse(token.RawText, limits.MaxSelectorTokens).Success) return false;
                color = token.RawText;
                colorSeen = true;
            }
            index = index + 1;
        }
        if (noneSeen) line = "none";
        else if (lineKeywords.Count == 0) line = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.TextDecorationLine);
        else line = new SvgCssDeclarationParser().Join(lineKeywords);
        if (entry.Longhands.Count != 3) return false;
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[0], line, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[1], style, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[2], color, expanded);
        return true;
    }
bool ExpandFont(SvgCssDeclaration declaration, SvgPropertySupportEntry entry, System.Collections.Generic.List<SvgShorthandToken> tokens, SvgPropertyCatalog catalog, SvgCssLimits limits, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        if (entry.Longhands.Count != 6) return false;
        if (tokens.Count == 1 && tokens[0].Kind == SvgCssTokenKind.Ident && new SvgCssDeclarationParser().IsSystemFont(tokens[0].Value))
        {
            new SvgCssDeclarationParser().ExpandSameValue(declaration, entry, tokens[0].RawText, expanded);
            return true;
        }
        if (tokens.Count < 2) return false;
        string style = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.FontStyle);
        string variant = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.FontVariant);
        string weight = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.FontWeight);
        string stretch = new SvgCssDeclarationParser().InitialValue(catalog, SvgCssPropertyNames.FontStretch);
        bool styleSeen = false;
        bool variantSeen = false;
        bool weightSeen = false;
        bool stretchSeen = false;
        int sizeIndex = -1;
        int index = 0;
        while (index < tokens.Count)
        {
            SvgShorthandToken token = tokens[index];
            if (new SvgCssDeclarationParser().IsFontSize(token))
            {
                sizeIndex = index;
                break;
            }
            if (token.Kind == SvgCssTokenKind.Number && new SvgCssDeclarationParser().IsFontWeight(token))
            {
                if (weightSeen) return false;
                weight = token.RawText;
                weightSeen = true;
                index = index + 1;
                continue;
            }
            if (token.Kind != SvgCssTokenKind.Ident) return false;
            if (new SvgCssDeclarationParser().AsciiEquals(token.Value, "normal"))
            {
                if (!styleSeen)
                {
                    style = token.RawText;
                    styleSeen = true;
                }
                else if (!variantSeen)
                {
                    variant = token.RawText;
                    variantSeen = true;
                }
                else if (!weightSeen)
                {
                    weight = token.RawText;
                    weightSeen = true;
                }
                else if (!stretchSeen)
                {
                    stretch = token.RawText;
                    stretchSeen = true;
                }
                else return false;
            }
            else if (new SvgCssDeclarationParser().IsFontStyle(token.Value))
            {
                if (styleSeen) return false;
                style = token.RawText;
                styleSeen = true;
                if (new SvgCssDeclarationParser().AsciiEquals(token.Value, "oblique") && index + 1 < tokens.Count && new SvgCssDeclarationParser().IsAngle(tokens[index + 1]))
                {
                    style = style + " " + tokens[index + 1].RawText;
                    index = index + 1;
                }
            }
            else if (new SvgCssDeclarationParser().AsciiEquals(token.Value, "small-caps"))
            {
                if (variantSeen) return false;
                variant = token.RawText;
                variantSeen = true;
            }
            else if (new SvgCssDeclarationParser().IsFontWeight(token))
            {
                if (weightSeen) return false;
                weight = token.RawText;
                weightSeen = true;
            }
            else if (new SvgCssDeclarationParser().IsFontStretch(token.Value))
            {
                if (stretchSeen) return false;
                stretch = token.RawText;
                stretchSeen = true;
            }
            else return false;
            index = index + 1;
        }
        if (sizeIndex < 0 || sizeIndex + 1 >= tokens.Count) return false;
        string size = tokens[sizeIndex].RawText;
        index = sizeIndex + 1;
        if (index < tokens.Count && new SvgCssDeclarationParser().IsSlash(tokens[index]))
        {
            index = index + 1;
            if (index >= tokens.Count || !new SvgCssDeclarationParser().IsLineHeight(tokens[index])) return false;
            index = index + 1;
        }
        if (index >= tokens.Count) return false;
        string family = new SvgCssDeclarationParser().JoinRaw(tokens, index);
        SvgFontFamilyParseResult familyResult = new SvgFontFamilyParser().Parse(family, limits.MaxDeclarations, limits.MaxSelectorTokens);
        if (!familyResult.Success) return false;
        new SvgCssDeclarationParser().AddExpanded(declaration, SvgCssPropertyNames.FontStyle, style, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, SvgCssPropertyNames.FontVariant, variant, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, SvgCssPropertyNames.FontWeight, weight, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, SvgCssPropertyNames.FontStretch, stretch, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, SvgCssPropertyNames.FontSize, size, expanded);
        new SvgCssDeclarationParser().AddExpanded(declaration, SvgCssPropertyNames.FontFamily, family, expanded);
        return true;
    }
System.Collections.Generic.List<SvgShorthandToken> ShorthandTokens(string value, System.Collections.Generic.List<SvgCssToken> sourceTokens)
    {
        System.Collections.Generic.List<SvgShorthandToken> result = new System.Collections.Generic.List<SvgShorthandToken>();
        int index = 0;
        while (index < sourceTokens.Count)
        {
            SvgCssToken token = sourceTokens[index];
            if (token.Kind == SvgCssTokenKind.Whitespace || token.Kind == SvgCssTokenKind.Comment)
            {
                index = index + 1;
                continue;
            }
            int end = index + 1;
            bool function = token.Kind == SvgCssTokenKind.Function;
            if (function || new SvgCssDeclarationParser().IsOpen(token.Kind))
            {
                int depth = 1;
                while (end < sourceTokens.Count && depth > 0)
                {
                    SvgCssToken nested = sourceTokens[end];
                    if (nested.Kind == SvgCssTokenKind.Function || new SvgCssDeclarationParser().IsOpen(nested.Kind)) depth = depth + 1;
                    else if (new SvgCssDeclarationParser().IsClose(nested.Kind)) depth = depth - 1;
                    end = end + 1;
                }
            }
            int startOffset = token.StartOffset;
            int endOffset = sourceTokens[end - 1].EndOffset;
            result.Add(new SvgShorthandToken(token.Kind, token.Value, value.Substring(startOffset, endOffset - startOffset), token.NumberValue, token.Unit, function));
            index = end;
        }
        return result;
    }
SvgCssDeclarationExpansionResult Success(SvgCssDeclaration declaration, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        new SvgCssDeclarationParser().CopyExpanded(expanded, declaration.ExpandedDeclarations);
        return new SvgCssDeclarationExpansionResult(true, SvgCssDeclarationExpansionError.None, expanded, diagnostics);
    }
SvgCssDeclarationExpansionResult Failure(SvgCssDeclaration declaration, SvgCssDeclarationExpansionError error, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        declaration.IsValid = false;
        int index = 0;
        while (index < diagnostics.Count)
        {
            declaration.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
        return new SvgCssDeclarationExpansionResult(false, error, expanded, diagnostics);
    }
SvgDiagnostic InvalidDiagnostic(SvgCssDeclaration declaration, string message)
    {
        return new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, message, declaration.Source);
    }
void ExpandSameValue(SvgCssDeclaration declaration, SvgPropertySupportEntry entry, string value, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        int index = 0;
        while (index < entry.Longhands.Count)
        {
            new SvgCssDeclarationParser().AddExpanded(declaration, entry.Longhands[index], value, expanded);
            index = index + 1;
        }
    }
void AddExpanded(SvgCssDeclaration declaration, string name, string value, System.Collections.Generic.List<SvgCssExpandedDeclaration> expanded)
    {
        expanded.Add(new SvgCssExpandedDeclaration(name, value, declaration.Important, declaration.SourceOrder, declaration.Name, declaration.Source, true));
    }
void CopyExpanded(System.Collections.Generic.List<SvgCssExpandedDeclaration> source, System.Collections.Generic.List<SvgCssExpandedDeclaration> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
string InitialValue(SvgPropertyCatalog catalog, string name)
    {
        SvgPropertySupportEntry? entry = catalog.Find(name);
        return entry == null || entry.InitialValue.Length == 0 ? "initial" : entry.InitialValue;
    }
bool IsOverflowKeyword(SvgShorthandToken token)
    {
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Value, "visible") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "hidden") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "clip") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "scroll") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "auto");
    }
bool IsComma(SvgShorthandToken token)
    {
        return token.Kind == SvgCssTokenKind.Comma;
    }
bool IsTime(SvgShorthandToken token)
    {
        return token.Kind == SvgCssTokenKind.Dimension && (new SvgCssDeclarationParser().AsciiEquals(token.Unit, "s") || new SvgCssDeclarationParser().AsciiEquals(token.Unit, "ms")) || token.Kind == SvgCssTokenKind.Number && token.NumberValue == 0;
    }
bool IsAnimationEasing(SvgShorthandToken token)
    {
        if (token.IsFunction) return new SvgCssDeclarationParser().AsciiEquals(token.Value, "cubic-bezier") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "steps") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "linear");
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Value, "linear") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "ease") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "ease-in") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "ease-out") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "ease-in-out") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "step-start") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "step-end");
    }
bool IsAnimationIteration(SvgShorthandToken token)
    {
        return token.Kind == SvgCssTokenKind.Number && token.NumberValue >= 0 || token.Kind == SvgCssTokenKind.Ident && new SvgCssDeclarationParser().AsciiEquals(token.Value, "infinite");
    }
bool IsAnimationDirection(SvgShorthandToken token)
    {
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Value, "normal") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "reverse") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "alternate") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "alternate-reverse");
    }
bool IsAnimationFill(SvgShorthandToken token)
    {
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Value, "none") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "forwards") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "backwards") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "both");
    }
bool IsAnimationState(SvgShorthandToken token)
    {
        return token.Kind == SvgCssTokenKind.Ident && (new SvgCssDeclarationParser().AsciiEquals(token.Value, "running") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "paused"));
    }
string JoinComma(System.Collections.Generic.List<string> values)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index != 0) result.Append(", ");
            result.Append(values[index]);
            index = index + 1;
        }
        return result.ToString();
    }
bool IsTextDecorationLine(string value)
    {
        return new SvgCssDeclarationParser().AsciiEquals(value, "none") || new SvgCssDeclarationParser().AsciiEquals(value, "underline") || new SvgCssDeclarationParser().AsciiEquals(value, "overline") || new SvgCssDeclarationParser().AsciiEquals(value, "line-through") || new SvgCssDeclarationParser().AsciiEquals(value, "blink");
    }
bool IsTextDecorationStyle(string value)
    {
        return new SvgCssDeclarationParser().AsciiEquals(value, "solid") || new SvgCssDeclarationParser().AsciiEquals(value, "double") || new SvgCssDeclarationParser().AsciiEquals(value, "dotted") || new SvgCssDeclarationParser().AsciiEquals(value, "dashed") || new SvgCssDeclarationParser().AsciiEquals(value, "wavy");
    }
bool IsFontStyle(string value)
    {
        return new SvgCssDeclarationParser().AsciiEquals(value, "italic") || new SvgCssDeclarationParser().AsciiEquals(value, "oblique");
    }
bool IsFontWeight(SvgShorthandToken token)
    {
        if (token.Kind == SvgCssTokenKind.Number) return token.NumberValue >= 1 && token.NumberValue <= 1000;
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Value, "bold") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "bolder") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "lighter");
    }
bool IsFontStretch(string value)
    {
        return new SvgCssDeclarationParser().AsciiEquals(value, "ultra-condensed") || new SvgCssDeclarationParser().AsciiEquals(value, "extra-condensed") || new SvgCssDeclarationParser().AsciiEquals(value, "condensed") || new SvgCssDeclarationParser().AsciiEquals(value, "semi-condensed") || new SvgCssDeclarationParser().AsciiEquals(value, "semi-expanded") || new SvgCssDeclarationParser().AsciiEquals(value, "expanded") || new SvgCssDeclarationParser().AsciiEquals(value, "extra-expanded") || new SvgCssDeclarationParser().AsciiEquals(value, "ultra-expanded");
    }
bool IsSystemFont(string value)
    {
        return new SvgCssDeclarationParser().AsciiEquals(value, "caption") || new SvgCssDeclarationParser().AsciiEquals(value, "icon") || new SvgCssDeclarationParser().AsciiEquals(value, "menu") || new SvgCssDeclarationParser().AsciiEquals(value, "message-box") || new SvgCssDeclarationParser().AsciiEquals(value, "small-caption") || new SvgCssDeclarationParser().AsciiEquals(value, "status-bar");
    }
bool IsFontSize(SvgShorthandToken token)
    {
        if (token.Kind == SvgCssTokenKind.Dimension || token.Kind == SvgCssTokenKind.Percentage) return token.NumberValue >= 0;
        if (token.Kind == SvgCssTokenKind.Number) return token.NumberValue == 0;
        if (token.IsFunction) return new SvgCssDeclarationParser().AsciiEquals(token.Value, "calc") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "min") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "max") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "clamp");
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Value, "xx-small") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "x-small") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "small") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "medium") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "large") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "x-large") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "xx-large") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "xxx-large") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "larger") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "smaller") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "math");
    }
bool IsLineHeight(SvgShorthandToken token)
    {
        if (token.Kind == SvgCssTokenKind.Ident) return new SvgCssDeclarationParser().AsciiEquals(token.Value, "normal");
        if (token.Kind == SvgCssTokenKind.Number || token.Kind == SvgCssTokenKind.Dimension || token.Kind == SvgCssTokenKind.Percentage) return token.NumberValue >= 0;
        return token.IsFunction && (new SvgCssDeclarationParser().AsciiEquals(token.Value, "calc") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "min") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "max") || new SvgCssDeclarationParser().AsciiEquals(token.Value, "clamp"));
    }
bool IsAngle(SvgShorthandToken token)
    {
        if (token.Kind != SvgCssTokenKind.Dimension) return false;
        return new SvgCssDeclarationParser().AsciiEquals(token.Unit, "deg") || new SvgCssDeclarationParser().AsciiEquals(token.Unit, "grad") || new SvgCssDeclarationParser().AsciiEquals(token.Unit, "rad") || new SvgCssDeclarationParser().AsciiEquals(token.Unit, "turn");
    }
bool IsSlash(SvgShorthandToken token)
    {
        return token.Kind == SvgCssTokenKind.Delimiter && token.Value == "/";
    }
bool ContainsText(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], value, System.StringComparison.Ordinal)) return true;
            index = index + 1;
        }
        return false;
    }
string Join(System.Collections.Generic.List<string> values)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index != 0) result.Append(' ');
            result.Append(values[index]);
            index = index + 1;
        }
        return result.ToString();
    }
string JoinRaw(System.Collections.Generic.List<SvgShorthandToken> tokens, int start)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = start;
        while (index < tokens.Count)
        {
            if (result.Length != 0 && tokens[index].Kind != SvgCssTokenKind.Comma) result.Append(' ');
            result.Append(tokens[index].RawText);
            index = index + 1;
        }
        return result.ToString().Trim();
    }
bool IsCssWideKeyword(string value)
    {
        return new SvgCssDeclarationParser().AsciiEquals(value, "initial") || new SvgCssDeclarationParser().AsciiEquals(value, "inherit") || new SvgCssDeclarationParser().AsciiEquals(value, "unset") || new SvgCssDeclarationParser().AsciiEquals(value, "revert") || new SvgCssDeclarationParser().AsciiEquals(value, "revert-layer");
    }
bool ContainsVariableFunction(System.Collections.Generic.List<SvgCssToken> tokens)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index].Kind == SvgCssTokenKind.Function && new SvgCssDeclarationParser().AsciiEquals(tokens[index].Value, "var")) return true;
            index = index + 1;
        }
        return false;
    }
bool IsOpen(SvgCssTokenKind kind)
    {
        return kind == SvgCssTokenKind.OpenParenthesis || kind == SvgCssTokenKind.OpenSquare || kind == SvgCssTokenKind.OpenCurly;
    }
bool IsClose(SvgCssTokenKind kind)
    {
        return kind == SvgCssTokenKind.CloseParenthesis || kind == SvgCssTokenKind.CloseSquare || kind == SvgCssTokenKind.CloseCurly;
    }
bool PropertyNameEquals(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }
bool AsciiEquals(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }
}
