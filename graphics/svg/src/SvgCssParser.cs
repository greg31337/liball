namespace svg;

internal class SvgNamespacePreludeResult
{
    internal bool Success;
    internal string Prefix;
    internal string NamespaceUri;
    internal string Message;

    internal SvgNamespacePreludeResult(bool success, string prefix, string namespaceUri, string message)
    {
        Success = success;
        Prefix = prefix;
        NamespaceUri = namespaceUri;
        Message = message;
    }
}

internal class SvgImportPreludeResult
{
    internal bool Success;
    internal string Uri;
    internal string Media;
    internal string Message;

    internal SvgImportPreludeResult(bool success, string uri, string media, string message)
    {
        Success = success;
        Uri = uri;
        Media = media;
        Message = message;
    }
}

internal class SvgLayerPreludeResult
{
    internal bool Success;
    internal System.Collections.Generic.List<string> Names;
    internal string Message;

    internal SvgLayerPreludeResult(bool success, System.Collections.Generic.List<string> names, string message)
    {
        Success = success;
        Names = names;
        Message = message;
    }
}

internal class SvgKeyframeOffsetsResult
{
    internal bool Success;
    internal System.Collections.Generic.List<double> Offsets;
    internal string Message;

    internal SvgKeyframeOffsetsResult(bool success, System.Collections.Generic.List<double> offsets, string message)
    {
        Success = success;
        Offsets = offsets;
        Message = message;
    }
}

/// <summary>Maintains one bounded CSS parse operation.</summary>
internal class SvgCssParserState
{
    string text;
    System.Collections.Generic.List<SvgCssToken> tokens;
    SvgCssLimits limits;
    SvgSourceUri? source;
    SvgCssParseMode mode;
    int position;
    int parseEnd;
    int ruleCount;
    int declarationCount;
    int expandedDeclarationCount;
    SvgCssParseError error;
    int errorOffset;
    string message;
    System.Collections.Generic.List<SvgCssDeclaration> declarations;
    System.Collections.Generic.List<SvgCssUnknownSyntax> unknownSyntax;
    System.Collections.Generic.List<SvgDiagnostic> diagnostics;
    SvgSelectorNamespaceBindings namespaces;
    bool namespaceRulesAllowed;
    bool importRulesAllowed;
    bool importRuleSeen;
    bool namespaceRuleSeen;
    System.Collections.Generic.List<SvgCssLayer> layers;
    int anonymousLayerCount;
    string currentLayerName;
    SvgPropertyCatalog propertyCatalog;

    /// <summary>Creates parser state from a lexical result.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="tokenized">The lexical result.</param>
    /// <param name="cssLimits">The CSS limits.</param>
    /// <param name="sourceUri">The source resource.</param>
    /// <param name="parseMode">The parser mode.</param>
    internal SvgCssParserState(string value, SvgCssTokenizeResult tokenized, SvgCssLimits cssLimits, SvgSourceUri? sourceUri, SvgCssParseMode parseMode)
    {
        text = value;
        tokens = tokenized.Tokens;
        limits = cssLimits;
        source = sourceUri;
        mode = parseMode;
        position = 0;
        parseEnd = tokens.Count;
        ruleCount = 0;
        declarationCount = 0;
        expandedDeclarationCount = 0;
        error = MapTokenError(tokenized.Error);
        errorOffset = tokenized.ErrorOffset;
        message = tokenized.Message;
        declarations = new System.Collections.Generic.List<SvgCssDeclaration>();
        unknownSyntax = new System.Collections.Generic.List<SvgCssUnknownSyntax>();
        diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        namespaces = new SvgSelectorNamespaceBindings();
        namespaceRulesAllowed = true;
        importRulesAllowed = true;
        importRuleSeen = false;
        namespaceRuleSeen = false;
        layers = new System.Collections.Generic.List<SvgCssLayer>();
        anonymousLayerCount = 0;
        currentLayerName = "";
        propertyCatalog = SvgPropertyCatalog.CreateDefault();
        if (error != SvgCssParseError.None) AddFatalDiagnostic();
    }

    /// <summary>Parses the state as a stylesheet.</summary>
    /// <param name="origin">The stylesheet origin.</param>
    /// <returns>The stylesheet parse result.</returns>
    internal SvgCssParseResult ParseStyleSheet(SvgStyleOrigin origin)
    {
        System.Collections.Generic.List<SvgCssRule> rules = new System.Collections.Generic.List<SvgCssRule>();
        while (position < parseEnd && error == SvgCssParseError.None)
        {
            SkipTriviaAndTopLevelMarkers();
            if (position >= parseEnd) break;
            SvgCssRule? rule = tokens[position].Kind == SvgCssTokenKind.AtKeyword ? ParseAtRule() : ParseQualifiedRule();
            if (rule != null)
            {
                rule.SourceOrder = rules.Count;
                if (rule.AtRule != null) rule.AtRule.SourceOrder = rules.Count;
                ProcessTopLevelRule(rule);
                rules.Add(rule);
            }
        }
        if (error != SvgCssParseError.None) return Result(null);
        SvgSourceSpan span = CreateSpan(0, text.Length);
        SvgStyleSheet sheet = new SvgStyleSheet(rules, origin, "", -1, text, span, diagnostics);
        sheet.Namespaces = namespaces;
        sheet.Layers = new System.Collections.Generic.List<SvgCssLayer>(layers);
        return Result(sheet);
    }

    /// <summary>Parses the state as a declaration list or style attribute.</summary>
    /// <returns>The declaration parse result.</returns>
    internal SvgCssParseResult ParseDeclarations()
    {
        ParseDeclarationSequence(parseEnd, mode == SvgCssParseMode.StyleAttribute);
        return Result(null);
    }

    SvgCssRule? ParseAtRule()
    {
        int startIndex = position;
        SvgCssToken keyword = tokens[position];
        if (!ReserveRule(keyword.StartOffset)) return null;
        int sourceOrder = ruleCount - 1;
        position = position + 1;
        System.Collections.Generic.List<SvgCssComponentValue> prelude = new System.Collections.Generic.List<SvgCssComponentValue>();
        System.Collections.Generic.List<SvgCssComponentValue> block = new System.Collections.Generic.List<SvgCssComponentValue>();
        bool hasBlock = false;
        bool endedWithSemicolon = false;
        bool valid = true;
        int blockStartIndex = -1;
        int blockContentEnd = -1;
        while (position < parseEnd && error == SvgCssParseError.None)
        {
            SvgCssToken current = tokens[position];
            if (current.Kind == SvgCssTokenKind.Semicolon)
            {
                position = position + 1;
                endedWithSemicolon = true;
                break;
            }
            if (current.Kind == SvgCssTokenKind.OpenCurly)
            {
                hasBlock = true;
                blockStartIndex = position;
                SvgCssComponentValue? component = ParseComponentValue(0);
                valid = component != null && component.IsValid;
                if (component != null) CopyComponents(component.Values, block);
                blockContentEnd = component != null && component.EndToken != null ? position - 1 : position;
                break;
            }
            SvgCssComponentValue? value = ParseComponentValue(0);
            if (value == null) break;
            prelude.Add(value);
            if (!value.IsValid) valid = false;
        }
        int endIndex = position;
        string rawText = RawText(startIndex, endIndex);
        SvgSourceSpan span = SpanForTokens(startIndex, endIndex);
        SvgCssAtRuleKind kind = AtRuleKind(keyword.Value);
        SvgNamespacePreludeResult? namespaceResult = null;
        SvgImportPreludeResult? importResult = null;
        SvgMediaQueryParseResult? mediaResult = null;
        SvgSupportsParseResult? supportsResult = null;
        bool mediaBlockInvalid = false;
        bool supportsBlockInvalid = false;
        if (kind == SvgCssAtRuleKind.Namespace)
        {
            namespaceResult = ParseNamespacePrelude(prelude, hasBlock, endedWithSemicolon);
            if (!namespaceResult.Success) valid = false;
        }
        if (kind == SvgCssAtRuleKind.Import)
        {
            importResult = ParseImportPrelude(prelude, hasBlock, endedWithSemicolon);
            if (!importResult.Success) valid = false;
        }
        if (kind == SvgCssAtRuleKind.Media)
        {
            string mediaText = ComponentText(prelude);
            int mediaOffset = keyword.EndOffset;
            int mediaLine = keyword.EndLine;
            int mediaColumn = keyword.EndColumn;
            if (prelude.Count > 0)
            {
                mediaOffset = prelude[0].Token.StartOffset;
                mediaLine = prelude[0].Token.StartLine;
                mediaColumn = prelude[0].Token.StartColumn;
            }
            mediaResult = new SvgMediaQueryParser().ParseLocated(mediaText, limits, source, mediaOffset, mediaLine, mediaColumn);
            if (!mediaResult.Success || mediaResult.QueryList == null || !mediaResult.QueryList.IsValid) valid = false;
            if (!hasBlock)
            {
                valid = false;
                mediaBlockInvalid = true;
            }
        }
        if (kind == SvgCssAtRuleKind.Supports)
        {
            string supportsText = ComponentText(prelude);
            int supportsOffset = keyword.EndOffset;
            int supportsLine = keyword.EndLine;
            int supportsColumn = keyword.EndColumn;
            if (prelude.Count > 0)
            {
                supportsOffset = prelude[0].Token.StartOffset;
                supportsLine = prelude[0].Token.StartLine;
                supportsColumn = prelude[0].Token.StartColumn;
            }
            supportsResult = new SvgSupportsQueryParser().ParseLocated(supportsText, limits, source, supportsOffset, supportsLine, supportsColumn);
            if (!supportsResult.Success || supportsResult.Query == null || !supportsResult.Query.IsValid) valid = false;
            if (!hasBlock)
            {
                valid = false;
                supportsBlockInvalid = true;
            }
        }
        SvgCssAtRule atRule = new SvgCssAtRule(kind, keyword.Value, rawText, prelude, block, hasBlock, sourceOrder, span, valid);
        if (namespaceResult != null)
        {
            atRule.NamespacePrefix = namespaceResult.Prefix;
            atRule.NamespaceUri = namespaceResult.NamespaceUri;
        }
        if (importResult != null)
        {
            atRule.ImportUri = importResult.Uri;
            atRule.ImportMedia = importResult.Media;
        }
        if (mediaResult != null) atRule.MediaQueries = mediaResult.QueryList;
        if (supportsResult != null) atRule.SupportsQuery = supportsResult.Query;
        if (kind == SvgCssAtRuleKind.Unknown)
        {
            SvgCssUnknownSyntax unknown = Unknown(SvgCssUnknownSyntaxKind.AtRule, startIndex, endIndex, "Unknown at-rule.", valid);
            atRule.UnknownSyntax = unknown;
        }
        if (!valid && kind != SvgCssAtRuleKind.Namespace && kind != SvgCssAtRuleKind.Import && kind != SvgCssAtRuleKind.Media && kind != SvgCssAtRuleKind.Supports && error == SvgCssParseError.None) AddRecovery("The at-rule contains an unclosed or malformed component value.", span);
        SvgCssRule rule = new SvgCssRule(SvgCssRuleKind.AtRule, rawText, prelude, new System.Collections.Generic.List<SvgCssDeclaration>(), new System.Collections.Generic.List<SvgCssRule>(), atRule, sourceOrder, span, valid);
        if (namespaceResult != null && !namespaceResult.Success)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, namespaceResult.Message, span);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
        }
        if (importResult != null && !importResult.Success)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, importResult.Message, span);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
        }
        if (mediaResult != null && (mediaResult.QueryList == null || !mediaResult.QueryList.IsValid))
        {
            AppendMediaDiagnostics(mediaResult.Diagnostics, atRule, rule, span);
        }
        if (mediaBlockInvalid)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "A media rule requires a block.", span);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
        }
        if (supportsResult != null && (supportsResult.Query == null || !supportsResult.Query.IsValid))
        {
            AppendSupportsDiagnostics(supportsResult.Diagnostics, atRule, rule, span);
        }
        if (supportsBlockInvalid)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "A supports rule requires a block.", span);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
        }
        if (kind == SvgCssAtRuleKind.Layer) ParseLayerRule(rule, endedWithSemicolon, blockStartIndex, blockContentEnd);
        if ((kind == SvgCssAtRuleKind.Media || kind == SvgCssAtRuleKind.Supports) && hasBlock && blockStartIndex >= 0 && error == SvgCssParseError.None)
        {
            ParseNestedRuleRange(blockStartIndex + 1, blockContentEnd, currentLayerName, rule.ChildRules);
        }
        if (kind == SvgCssAtRuleKind.FontFace) ParseFontFaceRule(rule, endedWithSemicolon, blockStartIndex, blockContentEnd);
        if (kind == SvgCssAtRuleKind.Keyframes) ParseKeyframesRule(rule, endedWithSemicolon, blockStartIndex, blockContentEnd);
        return rule;
    }

    void ParseLayerRule(SvgCssRule rule, bool endedWithSemicolon, int blockStartIndex, int blockContentEnd)
    {
        SvgCssAtRule? atRule = rule.AtRule;
        SvgLayerPreludeResult prelude = ParseLayerPrelude((atRule ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Prelude, !atRule.HasBlock);
        if (!prelude.Success)
        {
            InvalidateSpecialized(rule, prelude.Message, rule.Source);
            return;
        }
        if (!atRule.HasBlock && !endedWithSemicolon)
        {
            InvalidateSpecialized(rule, "A layer statement requires a semicolon.", rule.Source);
            return;
        }
        if (atRule.HasBlock && blockStartIndex < 0)
        {
            InvalidateSpecialized(rule, "A layer rule requires a complete block.", rule.Source);
            return;
        }
        if (atRule.HasBlock && prelude.Names.Count == 0)
        {
            string anonymousName = AnonymousLayerName();
            RegisterLayer(anonymousName, true, (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            atRule.LayerNames.Add(anonymousName);
        }
        else
        {
            int index = 0;
            while (index < prelude.Names.Count)
            {
                string fullName = QualifyLayerName(prelude.Names[index]);
                RegisterLayer(fullName, false, (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                atRule.LayerNames.Add(fullName);
                index = index + 1;
            }
        }
        if (atRule.LayerNames.Count > 0)
        {
            atRule.LayerName = atRule.LayerNames[0];
            atRule.LayerOrder = LayerOrder(atRule.LayerName);
            rule.LayerName = atRule.LayerName;
            rule.LayerOrder = atRule.LayerOrder;
        }
        if (atRule.HasBlock && error == SvgCssParseError.None)
        {
            ParseNestedRuleRange(blockStartIndex + 1, blockContentEnd, atRule.LayerName, rule.ChildRules);
        }
    }

    static SvgLayerPreludeResult ParseLayerPrelude(System.Collections.Generic.List<SvgCssComponentValue> values, bool multipleAllowed)
    {
        System.Collections.Generic.List<SvgCssComponentValue> significant = SignificantComponents(values);
        System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
        if (significant.Count == 0)
        {
            if (multipleAllowed) return new SvgLayerPreludeResult(false, names, "A layer statement requires at least one layer name.");
            return new SvgLayerPreludeResult(true, names, "");
        }
        System.Text.StringBuilder current = new System.Text.StringBuilder();
        bool identifierExpected = true;
        int index = 0;
        while (index < significant.Count)
        {
            SvgCssComponentValue component = significant[index];
            if (identifierExpected)
            {
                if (component.Kind != SvgCssComponentValueKind.Token || component.Token.Kind != SvgCssTokenKind.Ident || IsReservedCustomIdentifier(component.Token.Value))
                {
                    return new SvgLayerPreludeResult(false, names, "A layer name must contain only non-reserved identifiers separated by dots.");
                }
                current.Append(component.Token.Value);
                identifierExpected = false;
            }
            else if (component.Kind == SvgCssComponentValueKind.Token && component.Token.Kind == SvgCssTokenKind.Delimiter && component.Token.Value == ".")
            {
                current.Append('.');
                identifierExpected = true;
            }
            else if (multipleAllowed && component.Kind == SvgCssComponentValueKind.Token && component.Token.Kind == SvgCssTokenKind.Comma)
            {
                names.Add(current.ToString());
                current.Clear();
                identifierExpected = true;
            }
            else
            {
                return new SvgLayerPreludeResult(false, names, multipleAllowed ? "A layer statement must contain a comma-separated list of layer names." : "A layer block accepts at most one layer name.");
            }
            index = index + 1;
        }
        if (identifierExpected) return new SvgLayerPreludeResult(false, names, "A layer name cannot end with a dot or comma.");
        names.Add(current.ToString());
        return new SvgLayerPreludeResult(true, names, "");
    }

    string AnonymousLayerName()
    {
        string localName = "<anonymous:" + anonymousLayerCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + ">";
        anonymousLayerCount = anonymousLayerCount + 1;
        return QualifyLayerName(localName);
    }

    string QualifyLayerName(string name)
    {
        if (currentLayerName.Length == 0) return name;
        return currentLayerName + "." + name;
    }

    void RegisterLayer(string name, bool anonymous, SvgSourceSpan layerSource)
    {
        int componentStart = 0;
        while (componentStart < name.Length)
        {
            int separator = name.IndexOf('.', componentStart);
            string prefix = separator < 0 ? name : name.Substring(0, separator);
            if (FindLayer(prefix) < 0)
            {
                int parentSeparator = prefix.LastIndexOf('.');
                string parent = parentSeparator < 0 ? "" : prefix.Substring(0, parentSeparator);
                bool isAnonymous = anonymous && separator < 0;
                layers.Add(new SvgCssLayer(prefix, parent, layers.Count, isAnonymous, layerSource));
            }
            if (separator < 0) break;
            componentStart = separator + 1;
        }
    }

    int FindLayer(string name)
    {
        int index = 0;
        while (index < layers.Count)
        {
            if (string.Equals(layers[index].Name, name, System.StringComparison.Ordinal)) return index;
            index = index + 1;
        }
        return -1;
    }

    int LayerOrder(string name)
    {
        int index = FindLayer(name);
        return index < 0 ? -1 : layers[index].Order;
    }

    void ParseNestedRuleRange(int start, int end, string layerName, System.Collections.Generic.List<SvgCssRule> destination)
    {
        int savedPosition = position;
        int savedEnd = parseEnd;
        string savedLayerName = currentLayerName;
        position = start;
        parseEnd = end;
        currentLayerName = layerName;
        while (position < parseEnd && error == SvgCssParseError.None)
        {
            SkipTriviaAndTopLevelMarkers();
            if (position >= parseEnd) break;
            SvgCssRule? child = tokens[position].Kind == SvgCssTokenKind.AtKeyword ? ParseAtRule() : ParseQualifiedRule();
            if (child != null)
            {
                if (child.LayerName.Length == 0)
                {
                    child.LayerName = layerName;
                    child.LayerOrder = LayerOrder(layerName);
                }
                destination.Add(child);
            }
        }
        position = savedPosition;
        parseEnd = savedEnd;
        currentLayerName = savedLayerName;
    }

    void ParseFontFaceRule(SvgCssRule rule, bool endedWithSemicolon, int blockStartIndex, int blockContentEnd)
    {
        SvgCssAtRule? atRule = rule.AtRule;
        System.Collections.Generic.List<SvgCssComponentValue> significant = SignificantComponents((atRule ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Prelude);
        if (!atRule.HasBlock || endedWithSemicolon || blockStartIndex < 0)
        {
            InvalidateSpecialized(rule, "A font-face rule requires a descriptor block.", rule.Source);
            atRule.FontFace = new SvgCssFontFace(new System.Collections.Generic.List<SvgCssDeclaration>(), (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), false);
            return;
        }
        if (significant.Count != 0) InvalidateSpecialized(rule, "A font-face rule cannot have a prelude.", rule.Source);
        System.Collections.Generic.List<SvgCssDeclaration> descriptors = ParseOwnedDeclarations(blockStartIndex + 1, blockContentEnd);
        bool descriptorValid = true;
        int index = 0;
        while (index < descriptors.Count)
        {
            SvgCssDeclaration descriptor = descriptors[index];
            if (descriptor.Important)
            {
                descriptor.IsValid = false;
                SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "A font-face descriptor cannot be important.", descriptor.Source);
                descriptor.Diagnostics.Add(diagnostic);
                diagnostics.Add(diagnostic);
                atRule.Diagnostics.Add(diagnostic);
                rule.Diagnostics.Add(diagnostic);
                descriptorValid = false;
            }
            index = index + 1;
        }
        rule.Declarations = new System.Collections.Generic.List<SvgCssDeclaration>(descriptors);
        bool valid = rule.IsValid && descriptorValid;
        atRule.FontFace = new SvgCssFontFace(descriptors, (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), valid);
        if (!descriptorValid)
        {
            atRule.IsValid = false;
            rule.IsValid = false;
        }
    }

    System.Collections.Generic.List<SvgCssDeclaration> ParseOwnedDeclarations(int start, int end)
    {
        int savedPosition = position;
        int declarationStart = declarations.Count;
        position = start;
        ParseDeclarationSequence(end, false);
        System.Collections.Generic.List<SvgCssDeclaration> result = new System.Collections.Generic.List<SvgCssDeclaration>();
        int index = declarationStart;
        while (index < declarations.Count)
        {
            result.Add(declarations[index]);
            index = index + 1;
        }
        position = savedPosition;
        return result;
    }

    void ParseKeyframesRule(SvgCssRule rule, bool endedWithSemicolon, int blockStartIndex, int blockContentEnd)
    {
        SvgCssAtRule? atRule = rule.AtRule;
        System.Collections.Generic.List<SvgCssComponentValue> significant = SignificantComponents((atRule ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Prelude);
        string name = "";
        string rawName = "";
        bool nameValid = significant.Count == 1 && significant[0].Kind == SvgCssComponentValueKind.Token && (significant[0].Token.Kind == SvgCssTokenKind.Ident || significant[0].Token.Kind == SvgCssTokenKind.String);
        if (nameValid)
        {
            name = significant[0].Token.Value;
            rawName = significant[0].Token.RawText;
            if (significant[0].Token.Kind == SvgCssTokenKind.Ident && IsReservedKeyframesName(name)) nameValid = false;
        }
        if (!nameValid) InvalidateSpecialized(rule, "A keyframes rule requires one non-reserved identifier or string name.", rule.Source);
        if (!atRule.HasBlock || endedWithSemicolon || blockStartIndex < 0)
        {
            InvalidateSpecialized(rule, "A keyframes rule requires a block.", rule.Source);
            atRule.Keyframes = new SvgCssKeyframes(name, rawName, new System.Collections.Generic.List<SvgCssKeyframe>(), (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), false);
            return;
        }
        System.Collections.Generic.List<SvgCssKeyframe> keyframes = ParseKeyframeRange(blockStartIndex + 1, blockContentEnd, rule);
        bool valid = rule.IsValid;
        int index = 0;
        while (index < keyframes.Count)
        {
            if (!keyframes[index].IsValid) valid = false;
            index = index + 1;
        }
        atRule.Keyframes = new SvgCssKeyframes(name, rawName, keyframes, (rule.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), valid);
        if (!valid)
        {
            atRule.IsValid = false;
            rule.IsValid = false;
        }
    }

    System.Collections.Generic.List<SvgCssKeyframe> ParseKeyframeRange(int start, int end, SvgCssRule owner)
    {
        System.Collections.Generic.List<SvgCssKeyframe> result = new System.Collections.Generic.List<SvgCssKeyframe>();
        int savedPosition = position;
        int savedEnd = parseEnd;
        position = start;
        parseEnd = end;
        while (position < parseEnd && error == SvgCssParseError.None)
        {
            SkipTriviaAndTopLevelMarkers();
            if (position >= parseEnd) break;
            int stepStart = position;
            if (!ReserveRule(tokens[position].StartOffset)) break;
            while (position < parseEnd && tokens[position].Kind != SvgCssTokenKind.OpenCurly && error == SvgCssParseError.None)
            {
                SvgCssComponentValue? component = ParseComponentValue(0);
                if (component == null) break;
            }
            int selectorEnd = position;
            if (position >= parseEnd || tokens[position].Kind != SvgCssTokenKind.OpenCurly)
            {
                SvgSourceSpan malformedSpan = SpanForTokens(stepStart, position);
                InvalidateSpecialized(owner, "A keyframe selector requires a declaration block.", malformedSpan);
                result.Add(new SvgCssKeyframe(new System.Collections.Generic.List<double>(), new System.Collections.Generic.List<SvgCssDeclaration>(), RawText(stepStart, position), malformedSpan, false));
                break;
            }
            SvgKeyframeOffsetsResult offsets = ParseKeyframeOffsets(stepStart, selectorEnd);
            int blockStart = position;
            SvgCssComponentValue? block = ParseComponentValue(0);
            int stepEnd = position;
            int declarationEnd = block != null && block.EndToken != null ? stepEnd - 1 : stepEnd;
            System.Collections.Generic.List<SvgCssDeclaration> owned = ParseOwnedDeclarations(blockStart + 1, declarationEnd);
            bool valid = offsets.Success && block != null && block.IsValid;
            int declarationIndex = 0;
            while (declarationIndex < owned.Count)
            {
                SvgCssDeclaration declaration = owned[declarationIndex];
                if (declaration.Important)
                {
                    declaration.IsValid = false;
                    SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "A keyframe declaration cannot be important.", declaration.Source);
                    declaration.Diagnostics.Add(diagnostic);
                    diagnostics.Add(diagnostic);
                    (owner.AtRule ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Diagnostics.Add(diagnostic);
                    owner.Diagnostics.Add(diagnostic);
                    valid = false;
                }
                declarationIndex = declarationIndex + 1;
            }
            SvgSourceSpan stepSpan = SpanForTokens(stepStart, stepEnd);
            if (!offsets.Success) InvalidateSpecialized(owner, offsets.Message, SpanForTokens(stepStart, selectorEnd));
            if (block == null || !block.IsValid) InvalidateSpecialized(owner, "A keyframe declaration block is malformed.", stepSpan);
            result.Add(new SvgCssKeyframe(offsets.Offsets, owned, RawText(stepStart, stepEnd), stepSpan, valid));
        }
        position = savedPosition;
        parseEnd = savedEnd;
        return result;
    }

    SvgKeyframeOffsetsResult ParseKeyframeOffsets(int start, int end)
    {
        System.Collections.Generic.List<double> offsets = new System.Collections.Generic.List<double>();
        bool valueExpected = true;
        int index = start;
        while (index < end)
        {
            SvgCssToken token = tokens[index];
            if (token.Kind == SvgCssTokenKind.Whitespace || token.Kind == SvgCssTokenKind.Comment)
            {
                index = index + 1;
                continue;
            }
            if (valueExpected)
            {
                if (token.Kind == SvgCssTokenKind.Ident && AsciiEquals(token.Value, "from")) offsets.Add(0);
                else if (token.Kind == SvgCssTokenKind.Ident && AsciiEquals(token.Value, "to")) offsets.Add(1);
                else if (token.Kind == SvgCssTokenKind.Percentage && token.NumberValue >= 0 && token.NumberValue <= 100) offsets.Add(token.NumberValue / 100);
                else return new SvgKeyframeOffsetsResult(false, offsets, "A keyframe selector must be from, to, or a percentage from zero through one hundred.");
                valueExpected = false;
            }
            else
            {
                if (token.Kind != SvgCssTokenKind.Comma) return new SvgKeyframeOffsetsResult(false, offsets, "Keyframe selectors must be comma-separated.");
                valueExpected = true;
            }
            index = index + 1;
        }
        if (valueExpected) return new SvgKeyframeOffsetsResult(false, offsets, "A keyframe selector cannot be empty or end with a comma.");
        return new SvgKeyframeOffsetsResult(true, offsets, "");
    }

    void InvalidateSpecialized(SvgCssRule rule, string invalidMessage, SvgSourceSpan? invalidSource)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, invalidMessage, invalidSource);
        diagnostics.Add(diagnostic);
        (rule.AtRule ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Diagnostics.Add(diagnostic);
        rule.Diagnostics.Add(diagnostic);
        rule.AtRule.IsValid = false;
        rule.IsValid = false;
    }

    static bool IsReservedCustomIdentifier(string value)
    {
        return AsciiEquals(value, "initial") || AsciiEquals(value, "inherit") || AsciiEquals(value, "unset") || AsciiEquals(value, "revert") || AsciiEquals(value, "revert-layer") || AsciiEquals(value, "default") || AsciiEquals(value, "none");
    }

    static bool IsReservedKeyframesName(string value)
    {
        return IsReservedCustomIdentifier(value);
    }

    void AppendSupportsDiagnostics(System.Collections.Generic.List<SvgDiagnostic> supportsDiagnostics, SvgCssAtRule atRule, SvgCssRule rule, SvgSourceSpan fallback)
    {
        if (supportsDiagnostics.Count == 0)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "The supports condition is invalid.", fallback);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
            return;
        }
        int index = 0;
        while (index < supportsDiagnostics.Count)
        {
            SvgDiagnostic diagnostic = supportsDiagnostics[index];
            if (diagnostic.Source == null) diagnostic = new SvgDiagnostic(diagnostic.Severity, diagnostic.Code, diagnostic.Message, fallback);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
            index = index + 1;
        }
    }

    void AppendMediaDiagnostics(System.Collections.Generic.List<SvgDiagnostic> mediaDiagnostics, SvgCssAtRule atRule, SvgCssRule rule, SvgSourceSpan fallback)
    {
        if (mediaDiagnostics.Count == 0)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "The media query list is invalid.", fallback);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
            return;
        }
        int index = 0;
        while (index < mediaDiagnostics.Count)
        {
            SvgDiagnostic diagnostic = mediaDiagnostics[index];
            if (diagnostic.Source == null) diagnostic = new SvgDiagnostic(diagnostic.Severity, diagnostic.Code, diagnostic.Message, fallback);
            diagnostics.Add(diagnostic);
            atRule.Diagnostics.Add(diagnostic);
            rule.Diagnostics.Add(diagnostic);
            index = index + 1;
        }
    }

    static string ComponentText(System.Collections.Generic.List<SvgCssComponentValue> values)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            result.Append(values[index].RawText);
            index = index + 1;
        }
        return result.ToString();
    }

    void ProcessTopLevelRule(SvgCssRule rule)
    {
        if (rule.Kind != SvgCssRuleKind.AtRule || rule.AtRule == null)
        {
            namespaceRulesAllowed = false;
            importRulesAllowed = false;
            return;
        }
        SvgCssAtRule atRule = rule.AtRule;
        if (atRule.Kind == SvgCssAtRuleKind.Import)
        {
            if (!importRulesAllowed)
            {
                InvalidateOrdering(rule, "An import rule must precede namespace, style, and other body rules.");
            }
            else if (rule.IsValid) importRuleSeen = true;
            return;
        }
        if (atRule.Kind == SvgCssAtRuleKind.Namespace)
        {
            importRulesAllowed = false;
            if (!namespaceRulesAllowed)
            {
                InvalidateOrdering(rule, "A namespace rule must precede style and other body rules.");
                return;
            }
            if (!rule.IsValid) return;
            namespaceRuleSeen = true;
            if (atRule.NamespacePrefix.Length == 0) namespaces.SetDefault(atRule.NamespaceUri);
            else namespaces.Bind(atRule.NamespacePrefix, atRule.NamespaceUri);
            return;
        }
        if (atRule.Kind == SvgCssAtRuleKind.Layer && !atRule.HasBlock && rule.IsValid && !importRuleSeen && !namespaceRuleSeen) return;
        if (atRule.Kind != SvgCssAtRuleKind.Charset)
        {
            namespaceRulesAllowed = false;
            importRulesAllowed = false;
        }
    }

    void InvalidateOrdering(SvgCssRule rule, string orderingMessage)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, orderingMessage, rule.Source);
        diagnostics.Add(diagnostic);
        (rule.AtRule ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Diagnostics.Add(diagnostic);
        rule.Diagnostics.Add(diagnostic);
        rule.AtRule.IsValid = false;
        rule.IsValid = false;
    }

    static SvgImportPreludeResult ParseImportPrelude(System.Collections.Generic.List<SvgCssComponentValue> prelude, bool hasBlock, bool endedWithSemicolon)
    {
        if (hasBlock) return new SvgImportPreludeResult(false, "", "", "An import rule cannot have a block.");
        if (!endedWithSemicolon) return new SvgImportPreludeResult(false, "", "", "An import rule requires a semicolon.");
        System.Collections.Generic.List<SvgCssComponentValue> significant = SignificantComponents(prelude);
        if (significant.Count == 0) return new SvgImportPreludeResult(false, "", "", "An import rule requires a string or URL resource.");
        SvgCssComponentValue uriComponent = significant[0];
        string? uri = NamespaceUri(uriComponent);
        if (uri == null || uri.Length == 0) return new SvgImportPreludeResult(false, "", "", "An import resource must be a nonempty string or URL value.");
        return new SvgImportPreludeResult(true, uri, ImportMediaText(prelude, uriComponent), "");
    }

    static string ImportMediaText(System.Collections.Generic.List<SvgCssComponentValue> prelude, SvgCssComponentValue uriComponent)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool afterUri = false;
        int index = 0;
        while (index < prelude.Count)
        {
            SvgCssComponentValue value = prelude[index];
            if (afterUri) result.Append(value.RawText);
            else if (object.ReferenceEquals(value, uriComponent)) afterUri = true;
            index = index + 1;
        }
        return TrimCssWhitespace(result.ToString());
    }

    static string TrimCssWhitespace(string value)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && IsCssWhitespace(value[start])) start = start + 1;
        while (end > start && IsCssWhitespace(value[end - 1])) end = end - 1;
        return value.Substring(start, end - start);
    }

    static bool IsCssWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r' || value == '\f';
    }

    static SvgNamespacePreludeResult ParseNamespacePrelude(System.Collections.Generic.List<SvgCssComponentValue> prelude, bool hasBlock, bool endedWithSemicolon)
    {
        if (hasBlock) return new SvgNamespacePreludeResult(false, "", "", "A namespace rule cannot have a block.");
        if (!endedWithSemicolon) return new SvgNamespacePreludeResult(false, "", "", "A namespace rule requires a semicolon.");
        System.Collections.Generic.List<SvgCssComponentValue> significant = SignificantComponents(prelude);
        if (significant.Count == 0 || significant.Count > 2) return new SvgNamespacePreludeResult(false, "", "", "A namespace rule requires an optional prefix and one string or URL namespace.");
        string prefix = "";
        SvgCssComponentValue uriComponent = significant[0];
        if (significant.Count == 2)
        {
            if (significant[0].Kind != SvgCssComponentValueKind.Token || significant[0].Token.Kind != SvgCssTokenKind.Ident) return new SvgNamespacePreludeResult(false, "", "", "A namespace prefix must be an identifier.");
            prefix = significant[0].Token.Value;
            uriComponent = significant[1];
        }
        string? namespaceUri = NamespaceUri(uriComponent);
        if (namespaceUri == null) return new SvgNamespacePreludeResult(false, "", "", "A namespace URI must be a string or URL value.");
        return new SvgNamespacePreludeResult(true, prefix, namespaceUri, "");
    }

    static System.Collections.Generic.List<SvgCssComponentValue> SignificantComponents(System.Collections.Generic.List<SvgCssComponentValue> values)
    {
        System.Collections.Generic.List<SvgCssComponentValue> result = new System.Collections.Generic.List<SvgCssComponentValue>();
        int index = 0;
        while (index < values.Count)
        {
            SvgCssComponentValue value = values[index];
            bool trivia = value.Kind == SvgCssComponentValueKind.Token && (value.Token.Kind == SvgCssTokenKind.Whitespace || value.Token.Kind == SvgCssTokenKind.Comment);
            if (!trivia) result.Add(value);
            index = index + 1;
        }
        return result;
    }

    static string? NamespaceUri(SvgCssComponentValue value)
    {
        if (value.Kind == SvgCssComponentValueKind.Token && (value.Token.Kind == SvgCssTokenKind.String || value.Token.Kind == SvgCssTokenKind.Url)) return value.Token.Value;
        if (value.Kind != SvgCssComponentValueKind.Function || !AsciiEquals(value.Name, "url")) return null;
        System.Collections.Generic.List<SvgCssComponentValue> nested = SignificantComponents(value.Values);
        if (nested.Count != 1 || nested[0].Kind != SvgCssComponentValueKind.Token || nested[0].Token.Kind != SvgCssTokenKind.String) return null;
        return nested[0].Token.Value;
    }

    SvgCssRule? ParseQualifiedRule()
    {
        int startIndex = position;
        int startOffset = tokens[position].StartOffset;
        if (!ReserveRule(startOffset)) return null;
        int sourceOrder = ruleCount - 1;
        System.Collections.Generic.List<SvgCssComponentValue> prelude = new System.Collections.Generic.List<SvgCssComponentValue>();
        while (position < parseEnd && error == SvgCssParseError.None && tokens[position].Kind != SvgCssTokenKind.OpenCurly)
        {
            SvgCssComponentValue? value = ParseComponentValue(0);
            if (value == null) break;
            prelude.Add(value);
        }
        if (position >= parseEnd || error != SvgCssParseError.None)
        {
            int endIndex = position;
            SvgCssUnknownSyntax unknown = Unknown(SvgCssUnknownSyntaxKind.Rule, startIndex, endIndex, "A qualified rule ended before its block.", false);
            AddRecovery("A qualified rule requires a curly block.", unknown.Source);
            return null;
        }
        int blockStart = position;
        SvgSelectorParseResult selectorResult = new SvgSelectorParser().ParseTokens(text, tokens, startIndex, blockStart, limits, source);
        SvgCssComponentValue? block = ParseComponentValue(0);
        int endIndexAfterBlock = position;
        bool valid = block != null && block.IsValid && selectorResult.Success;
        int declarationEnd = block != null && block.EndToken != null ? endIndexAfterBlock - 1 : endIndexAfterBlock;
        int savedPosition = position;
        System.Collections.Generic.List<SvgCssDeclaration> ownedDeclarations = new System.Collections.Generic.List<SvgCssDeclaration>();
        if (error == SvgCssParseError.None)
        {
            position = blockStart + 1;
            int declarationStart = declarations.Count;
            ParseDeclarationSequence(declarationEnd, false);
            int index = declarationStart;
            while (index < declarations.Count)
            {
                ownedDeclarations.Add(declarations[index]);
                index = index + 1;
            }
            position = savedPosition;
        }
        if (block != null && !block.IsValid && error == SvgCssParseError.None) AddRecovery("A qualified rule contains an unclosed block.", block.Source);
        string rawText = RawText(startIndex, endIndexAfterBlock);
        SvgSourceSpan span = SpanForTokens(startIndex, endIndexAfterBlock);
        SvgCssRule rule = new SvgCssRule(SvgCssRuleKind.Style, rawText, prelude, ownedDeclarations, new System.Collections.Generic.List<SvgCssRule>(), null, sourceOrder, span, valid);
        rule.Selectors = selectorResult.Selector;
        CopyDiagnostics(selectorResult.Selector.Diagnostics, rule.Diagnostics);
        CopyDiagnostics(selectorResult.Selector.Diagnostics, diagnostics);
        return rule;
    }

    void ParseDeclarationSequence(int end, bool styleAttribute)
    {
        int savedEnd = parseEnd;
        parseEnd = end;
        while (position < parseEnd && error == SvgCssParseError.None)
        {
            SkipDeclarationSeparators();
            if (position >= parseEnd) break;
            if (tokens[position].Kind == SvgCssTokenKind.AtKeyword)
            {
                int unknownStart = position;
                ConsumeDeclarationSegment();
                SvgCssUnknownSyntax retained = Unknown(SvgCssUnknownSyntaxKind.Declaration, unknownStart, position, styleAttribute ? "At-rules are not allowed in style attributes." : "At-rules are not declarations.", false);
                AddRecovery(styleAttribute ? "An at-rule was ignored in a style attribute." : "An at-rule was ignored in a declaration list.", retained.Source);
                continue;
            }
            ParseDeclaration();
        }
        parseEnd = savedEnd;
    }

    void ParseDeclaration()
    {
        int startIndex = position;
        SvgCssToken nameToken = tokens[position];
        if (nameToken.Kind != SvgCssTokenKind.Ident)
        {
            ConsumeDeclarationSegment();
            SvgCssUnknownSyntax retained = Unknown(SvgCssUnknownSyntaxKind.Declaration, startIndex, position, "A declaration must start with an identifier.", false);
            AddRecovery("A malformed declaration was ignored.", retained.Source);
            return;
        }
        position = position + 1;
        SkipTrivia();
        if (position >= parseEnd || tokens[position].Kind != SvgCssTokenKind.Colon)
        {
            ConsumeDeclarationSegment();
            SvgCssUnknownSyntax retained = Unknown(SvgCssUnknownSyntaxKind.Declaration, startIndex, position, "A declaration requires a colon after its name.", false);
            AddRecovery("A declaration without a colon was ignored.", retained.Source);
            return;
        }
        SvgCssToken colon = tokens[position];
        position = position + 1;
        int valueStartIndex = position;
        System.Collections.Generic.List<SvgCssComponentValue> values = new System.Collections.Generic.List<SvgCssComponentValue>();
        bool valid = true;
        while (position < parseEnd && tokens[position].Kind != SvgCssTokenKind.Semicolon && error == SvgCssParseError.None)
        {
            SvgCssComponentValue? value = ParseComponentValue(0);
            if (value == null) break;
            values.Add(value);
            if (!value.IsValid || IsUnexpectedClose(value.Token.Kind)) valid = false;
        }
        int valueEndIndex = position;
        if (position < parseEnd && tokens[position].Kind == SvgCssTokenKind.Semicolon) position = position + 1;
        int endIndex = position;
        if (error != SvgCssParseError.None) return;
        if (!valid && error == SvgCssParseError.None)
        {
            SvgCssUnknownSyntax retained = Unknown(SvgCssUnknownSyntaxKind.Declaration, startIndex, endIndex, "The declaration contains malformed component syntax.", false);
            AddRecovery("A declaration with malformed component syntax was ignored.", retained.Source);
            return;
        }
        if (!ReserveDeclaration(nameToken.StartOffset)) return;
        bool important = false;
        int valueTextEnd = valueEndIndex > valueStartIndex ? tokens[valueEndIndex - 1].EndOffset : colon.EndOffset;
        int last = PreviousSignificant(valueEndIndex - 1, valueStartIndex);
        if (last >= valueStartIndex && tokens[last].Kind == SvgCssTokenKind.Ident && AsciiEquals(tokens[last].Value, "important"))
        {
            int bang = PreviousSignificant(last - 1, valueStartIndex);
            if (bang >= valueStartIndex && tokens[bang].Kind == SvgCssTokenKind.Delimiter && tokens[bang].Value == "!")
            {
                important = true;
                valueTextEnd = tokens[bang].StartOffset;
                RemoveValuesAtOrAfter(values, tokens[bang].StartOffset);
            }
        }
        int valueTextStart = colon.EndOffset;
        string valueText = text.Substring(valueTextStart, valueTextEnd - valueTextStart);
        string rawText = RawText(startIndex, endIndex);
        System.Collections.Generic.List<SvgCssToken> originalTokens = TokenSlice(startIndex, endIndex);
        string name = NormalizePropertyName(nameToken.Value);
        SvgSourceSpan span = SpanForTokens(startIndex, endIndex);
        SvgCssDeclaration declaration = new SvgCssDeclaration(name, nameToken.RawText, rawText, valueText, originalTokens, values, important, declarationCount - 1, span, true);
        SvgCssDeclarationExpansionResult expansion = new SvgCssDeclarationParser().Expand(declaration, propertyCatalog, limits);
        if (expansion.Error == SvgCssDeclarationExpansionError.DeclarationLimitExceeded)
        {
            SetFatal(SvgCssParseError.DeclarationLimitExceeded, nameToken.StartOffset, "The configured CSS declaration count was exceeded during shorthand expansion.");
            return;
        }
        if (expansion.Success && expansion.Declarations.Count > 0)
        {
            int additional = expansion.Declarations.Count - 1;
            if (!ReserveExpandedDeclarations(additional, nameToken.StartOffset)) return;
        }
        CopyDiagnostics(expansion.Diagnostics, diagnostics);
        declarations.Add(declaration);
    }

    void ConsumeDeclarationSegment()
    {
        while (position < parseEnd && tokens[position].Kind != SvgCssTokenKind.Semicolon && error == SvgCssParseError.None)
        {
            ParseComponentValue(0);
        }
        if (position < parseEnd && tokens[position].Kind == SvgCssTokenKind.Semicolon) position = position + 1;
    }

    SvgCssComponentValue? ParseComponentValue(int depth)
    {
        if (position >= parseEnd) return null;
        SvgCssToken token = tokens[position];
        bool function = token.Kind == SvgCssTokenKind.Function;
        bool block = IsOpenBlock(token.Kind);
        if (!function && !block)
        {
            position = position + 1;
            bool validToken = token.Kind != SvgCssTokenKind.BadString && token.Kind != SvgCssTokenKind.BadUrl;
            SvgCssComponentValue leaf = new SvgCssComponentValue(SvgCssComponentValueKind.Token, token, null, "", token.RawText, new System.Collections.Generic.List<SvgCssComponentValue>(), SpanForToken(token), validToken);
            if (!validToken) leaf.UnknownSyntax = CreateUnknown(SvgCssUnknownSyntaxKind.ComponentValue, position - 1, position, "A bad lexical token was retained.", false);
            return leaf;
        }
        if (depth >= limits.MaxBlockDepth)
        {
            SetFatal(SvgCssParseError.BlockDepthExceeded, token.StartOffset, "The configured CSS block depth was exceeded.");
            return null;
        }
        int startIndex = position;
        SvgCssTokenKind closeKind = function ? SvgCssTokenKind.CloseParenthesis : MatchingClose(token.Kind);
        position = position + 1;
        System.Collections.Generic.List<SvgCssComponentValue> nested = new System.Collections.Generic.List<SvgCssComponentValue>();
        bool valid = true;
        while (position < parseEnd && tokens[position].Kind != closeKind && error == SvgCssParseError.None)
        {
            SvgCssComponentValue? child = ParseComponentValue(depth + 1);
            if (child == null) break;
            nested.Add(child);
            if (!child.IsValid) valid = false;
        }
        SvgCssToken? endToken = null;
        if (position < parseEnd && tokens[position].Kind == closeKind)
        {
            endToken = tokens[position];
            position = position + 1;
        }
        else valid = false;
        int endIndex = position;
        SvgCssComponentValueKind kind = function ? SvgCssComponentValueKind.Function : SvgCssComponentValueKind.SimpleBlock;
        string name = function ? token.Value : "";
        SvgCssComponentValue component = new SvgCssComponentValue(kind, token, endToken, name, RawText(startIndex, endIndex), nested, SpanForTokens(startIndex, endIndex), valid);
        if (!valid) component.UnknownSyntax = CreateUnknown(SvgCssUnknownSyntaxKind.ComponentValue, startIndex, endIndex, "An unclosed or malformed component value was retained.", false);
        return component;
    }

    void SkipTriviaAndTopLevelMarkers()
    {
        while (position < parseEnd)
        {
            SvgCssTokenKind kind = tokens[position].Kind;
            if (kind != SvgCssTokenKind.Whitespace && kind != SvgCssTokenKind.Comment && kind != SvgCssTokenKind.Cdo && kind != SvgCssTokenKind.Cdc) break;
            position = position + 1;
        }
    }

    void SkipDeclarationSeparators()
    {
        while (position < parseEnd)
        {
            SvgCssTokenKind kind = tokens[position].Kind;
            if (kind != SvgCssTokenKind.Whitespace && kind != SvgCssTokenKind.Comment && kind != SvgCssTokenKind.Semicolon) break;
            position = position + 1;
        }
    }

    void SkipTrivia()
    {
        while (position < parseEnd && (tokens[position].Kind == SvgCssTokenKind.Whitespace || tokens[position].Kind == SvgCssTokenKind.Comment)) position = position + 1;
    }

    bool ReserveRule(int offset)
    {
        if (ruleCount >= limits.MaxRules)
        {
            SetFatal(SvgCssParseError.RuleLimitExceeded, offset, "The configured CSS rule count was exceeded.");
            return false;
        }
        ruleCount = ruleCount + 1;
        return true;
    }

    bool ReserveDeclaration(int offset)
    {
        if (declarationCount + expandedDeclarationCount >= limits.MaxDeclarations)
        {
            SetFatal(SvgCssParseError.DeclarationLimitExceeded, offset, "The configured CSS declaration count was exceeded.");
            return false;
        }
        declarationCount = declarationCount + 1;
        return true;
    }

    bool ReserveExpandedDeclarations(int count, int offset)
    {
        if (count < 0 || declarationCount + expandedDeclarationCount + count > limits.MaxDeclarations)
        {
            SetFatal(SvgCssParseError.DeclarationLimitExceeded, offset, "The configured CSS declaration count was exceeded during shorthand expansion.");
            return false;
        }
        expandedDeclarationCount = expandedDeclarationCount + count;
        return true;
    }

    void SetFatal(SvgCssParseError fatal, int offset, string fatalMessage)
    {
        if (error != SvgCssParseError.None) return;
        error = fatal;
        errorOffset = offset;
        message = fatalMessage;
        AddFatalDiagnostic();
    }

    void AddFatalDiagnostic()
    {
        SvgDiagnosticCode code = error == SvgCssParseError.InvalidArgument ? SvgDiagnosticCode.InvalidArgument : SvgDiagnosticCode.InputLimitExceeded;
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, CreateSpan(errorOffset, errorOffset)));
    }

    void AddRecovery(string recoveryMessage, SvgSourceSpan? span)
    {
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, recoveryMessage, span));
    }

    SvgCssUnknownSyntax Unknown(SvgCssUnknownSyntaxKind kind, int startIndex, int endIndex, string reason, bool valid)
    {
        SvgCssUnknownSyntax unknown = CreateUnknown(kind, startIndex, endIndex, reason, valid);
        unknownSyntax.Add(unknown);
        return unknown;
    }

    SvgCssUnknownSyntax CreateUnknown(SvgCssUnknownSyntaxKind kind, int startIndex, int endIndex, string reason, bool valid)
    {
        return new SvgCssUnknownSyntax(kind, RawText(startIndex, endIndex), TokenSlice(startIndex, endIndex), reason, SpanForTokens(startIndex, endIndex), valid);
    }

    SvgCssParseResult Result(SvgStyleSheet? sheet)
    {
        bool success = error == SvgCssParseError.None;
        System.Collections.Generic.List<SvgCssDeclaration> resultDeclarations = mode == SvgCssParseMode.StyleSheet ? new System.Collections.Generic.List<SvgCssDeclaration>() : declarations;
        return new SvgCssParseResult(success, mode, error, success ? sheet : null, resultDeclarations, unknownSyntax, diagnostics, success ? -1 : errorOffset, success ? "" : message);
    }

    SvgSourceSpan SpanForToken(SvgCssToken token)
    {
        return new SvgSourceSpan((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), token.StartOffset, token.StartLine, token.StartColumn, token.EndOffset, token.EndLine, token.EndColumn);
    }

    SvgSourceSpan SpanForTokens(int startIndex, int endIndex)
    {
        if (startIndex < endIndex && startIndex >= 0 && endIndex <= tokens.Count)
        {
            SvgCssToken first = tokens[startIndex];
            SvgCssToken last = tokens[endIndex - 1];
            return new SvgSourceSpan((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), first.StartOffset, first.StartLine, first.StartColumn, last.EndOffset, last.EndLine, last.EndColumn);
        }
        int offset = startIndex >= 0 && startIndex < tokens.Count ? tokens[startIndex].StartOffset : text.Length;
        return CreateSpan(offset, offset);
    }

    SvgSourceSpan CreateSpan(int start, int end)
    {
        SvgCssLocation first = SvgCssTokenizerState.GetLocation(text, start);
        SvgCssLocation last = SvgCssTokenizerState.GetLocation(text, end);
        return new SvgSourceSpan((source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), start, first.Line, first.Column, end, last.Line, last.Column);
    }

    string RawText(int startIndex, int endIndex)
    {
        if (startIndex >= endIndex || startIndex < 0 || endIndex > tokens.Count) return "";
        int start = tokens[startIndex].StartOffset;
        int end = tokens[endIndex - 1].EndOffset;
        return text.Substring(start, end - start);
    }

    System.Collections.Generic.List<SvgCssToken> TokenSlice(int startIndex, int endIndex)
    {
        System.Collections.Generic.List<SvgCssToken> result = new System.Collections.Generic.List<SvgCssToken>();
        int index = startIndex;
        while (index < endIndex && index < tokens.Count)
        {
            result.Add(tokens[index]);
            index = index + 1;
        }
        return result;
    }

    int PreviousSignificant(int index, int minimum)
    {
        while (index >= minimum)
        {
            SvgCssTokenKind kind = tokens[index].Kind;
            if (kind != SvgCssTokenKind.Whitespace && kind != SvgCssTokenKind.Comment) return index;
            index = index - 1;
        }
        return minimum - 1;
    }

    void RemoveValuesAtOrAfter(System.Collections.Generic.List<SvgCssComponentValue> values, int offset)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (values[index].Token.StartOffset < offset) break;
            values.RemoveAt(index);
            index = index - 1;
        }
    }

    static void CopyComponents(System.Collections.Generic.List<SvgCssComponentValue> sourceValues, System.Collections.Generic.List<SvgCssComponentValue> destination)
    {
        int index = 0;
        while (index < sourceValues.Count)
        {
            destination.Add(sourceValues[index]);
            index = index + 1;
        }
    }

    static void CopyDiagnostics(System.Collections.Generic.List<SvgDiagnostic> sourceDiagnostics, System.Collections.Generic.List<SvgDiagnostic> destination)
    {
        int index = 0;
        while (index < sourceDiagnostics.Count)
        {
            destination.Add(sourceDiagnostics[index]);
            index = index + 1;
        }
    }

    static bool IsOpenBlock(SvgCssTokenKind kind)
    {
        return kind == SvgCssTokenKind.OpenParenthesis || kind == SvgCssTokenKind.OpenSquare || kind == SvgCssTokenKind.OpenCurly;
    }

    static bool IsUnexpectedClose(SvgCssTokenKind kind)
    {
        return kind == SvgCssTokenKind.CloseParenthesis || kind == SvgCssTokenKind.CloseSquare || kind == SvgCssTokenKind.CloseCurly;
    }

    static SvgCssTokenKind MatchingClose(SvgCssTokenKind kind)
    {
        if (kind == SvgCssTokenKind.OpenParenthesis) return SvgCssTokenKind.CloseParenthesis;
        if (kind == SvgCssTokenKind.OpenSquare) return SvgCssTokenKind.CloseSquare;
        return SvgCssTokenKind.CloseCurly;
    }

    static SvgCssAtRuleKind AtRuleKind(string name)
    {
        if (AsciiEquals(name, "charset")) return SvgCssAtRuleKind.Charset;
        if (AsciiEquals(name, "import")) return SvgCssAtRuleKind.Import;
        if (AsciiEquals(name, "namespace")) return SvgCssAtRuleKind.Namespace;
        if (AsciiEquals(name, "layer")) return SvgCssAtRuleKind.Layer;
        if (AsciiEquals(name, "media")) return SvgCssAtRuleKind.Media;
        if (AsciiEquals(name, "supports")) return SvgCssAtRuleKind.Supports;
        if (AsciiEquals(name, "font-face")) return SvgCssAtRuleKind.FontFace;
        if (AsciiEquals(name, "keyframes")) return SvgCssAtRuleKind.Keyframes;
        return SvgCssAtRuleKind.Unknown;
    }

    static string NormalizePropertyName(string name)
    {
        if (name.Length >= 2 && name[0] == '-' && name[1] == '-') return name;
        System.Text.StringBuilder normalized = new System.Text.StringBuilder(name.Length);
        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            if (value >= 'A' && value <= 'Z') value = (char)(value + 32);
            normalized.Append(value);
            index = index + 1;
        }
        return normalized.ToString();
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

    static SvgCssParseError MapTokenError(SvgCssTokenizeError tokenError)
    {
        if (tokenError == SvgCssTokenizeError.CharacterLimitExceeded) return SvgCssParseError.CharacterLimitExceeded;
        if (tokenError == SvgCssTokenizeError.TokenLimitExceeded) return SvgCssParseError.TokenLimitExceeded;
        if (tokenError == SvgCssTokenizeError.InvalidArgument) return SvgCssParseError.InvalidArgument;
        return SvgCssParseError.None;
    }
}
