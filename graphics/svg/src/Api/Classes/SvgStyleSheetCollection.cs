namespace svg;

/// <summary>Collects embedded and caller-provided stylesheets with independent namespace scope.</summary>
public class SvgStyleSheetCollector : ISvgStyleSheetCollector
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgStyleSheetCollector()
    {
    }

    /// <summary>Collects stylesheets and resolves matching imports through the caller callback.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="callerStyleSheets">The already-parsed caller stylesheets in caller order.</param>
    /// <param name="options">The resource provider, policy, cancellation, and security limits.</param>
    /// <param name="mediaMatches">A media-condition evaluator, or <c>null</c> to match every condition.</param>
    /// <returns>The import-expanded stylesheet collection.</returns>
    public SvgStyleSheetCollection CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(SvgDocument document, System.Collections.Generic.List<SvgStyleSheet> callerStyleSheets, SvgLoadOptions options, System.Func<string, bool>? mediaMatches)
    {
        if (options == null || options.Limits == null || options.ResourcePolicy == null) throw new System.ArgumentException("Import collection options are invalid.", "options");
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(document, callerStyleSheets, options.Limits.Css);
        return new SvgStyleSheetImportResolver().Resolve(collection, options, mediaMatches);
    }

    /// <summary>Collects SVG style elements in preorder and then appends caller stylesheets.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="callerStyleSheets">The already-parsed caller stylesheets in caller order.</param>
    /// <param name="limits">The cumulative CSS limits for collection.</param>
    /// <returns>The collected stylesheets and provenance diagnostics.</returns>
    public SvgStyleSheetCollection CollectWithDocumentAndCallerStyleSheetsAndLimits(SvgDocument document, System.Collections.Generic.List<SvgStyleSheet> callerStyleSheets, SvgCssLimits limits)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        if (callerStyleSheets == null) throw new System.ArgumentNullException("callerStyleSheets");
        if (!new SvgStyleSheetCollector().ValidLimits(limits)) throw new System.ArgumentException("CSS collection limits are invalid.", "limits");
        System.Collections.Generic.List<SvgCollectedStyleSheet> collected = new System.Collections.Generic.List<SvgCollectedStyleSheet>();
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        SvgStyleCollectionBudget budget = new SvgStyleCollectionBudget();
        System.Collections.Generic.List<SvgNode> styles = new SvgStyleSheetCollector().FindStyleElements(document.DocumentNode);
        int styleIndex = 0;
        while (styleIndex < styles.Count)
        {
            SvgNode style = styles[styleIndex];
            string type = new SvgStyleSheetCollector().AttributeValue(style, SvgAttributeNames.Type);
            if (type.Length == 0 || new SvgStyleSheetCollector().AsciiEquals(new SvgStyleSheetCollector().TrimAsciiWhitespace(type), SvgMimeTypes.Css))
            {
                string css = new SvgNodeApi().GetTextContent(style);
                SvgCssLimits? remaining = new SvgStyleSheetCollector().RemainingLimits(limits, budget, css.Length, style.SourceSpan, diagnostics);
                if (remaining != null)
                {
                    SvgSourceUri source = new SvgStyleSheetCollector().EmbeddedSource(document, style, styleIndex);
                    SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, remaining, SvgStyleOrigin.Author, source);
                    new SvgStyleSheetCollector().AppendDiagnostics(diagnostics, parsed.Diagnostics);
                    if (parsed.StyleSheet != null)
                    {
                        int rules = new SvgStyleSheetCollector().CountRules(parsed.StyleSheet.Rules);
                        int declarations = new SvgStyleSheetCollector().CountDeclarations(parsed.StyleSheet.Rules);
                        budget.Characters = budget.Characters + css.Length;
                        budget.Rules = budget.Rules + rules;
                        budget.Declarations = budget.Declarations + declarations;
                        string media = new SvgStyleSheetCollector().AttributeValue(style, SvgAttributeNames.Media);
                        collected.Add(new SvgCollectedStyleSheet(parsed.StyleSheet, SvgStyleSheetSourceKind.Embedded, style, style.SourceSpan, media, collected.Count));
                    }
                }
            }
            else
            {
                diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.UnsupportedFeature, "The style element media type is not text/css and was not collected.", style.SourceSpan));
            }
            styleIndex = styleIndex + 1;
        }
        int callerIndex = 0;
        while (callerIndex < callerStyleSheets.Count)
        {
            SvgStyleSheet sheet = callerStyleSheets[callerIndex];
            if (sheet == null)
            {
                diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "A caller stylesheet entry is null.", null));
            }
            else if (new SvgStyleSheetCollector().ReserveCaller(sheet, limits, budget, diagnostics))
            {
                collected.Add(new SvgCollectedStyleSheet(sheet, SvgStyleSheetSourceKind.Caller, null, sheet.Source, "", collected.Count));
                new SvgStyleSheetCollector().AppendDiagnostics(diagnostics, sheet.Diagnostics);
            }
            callerIndex = callerIndex + 1;
        }
        return new SvgStyleSheetCollection(collected, diagnostics);
    }
bool ReserveCaller(SvgStyleSheet sheet, SvgCssLimits limits, SvgStyleCollectionBudget budget, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int rules = new SvgStyleSheetCollector().CountRules(sheet.Rules);
        int declarations = new SvgStyleSheetCollector().CountDeclarations(sheet.Rules);
        long characters = sheet.SourceText.Length;
        if (characters > limits.MaxCharacters - budget.Characters || rules > limits.MaxRules - budget.Rules || declarations > limits.MaxDeclarations - budget.Declarations)
        {
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The cumulative stylesheet collection limit was exceeded.", sheet.Source));
            return false;
        }
        budget.Characters = budget.Characters + characters;
        budget.Rules = budget.Rules + rules;
        budget.Declarations = budget.Declarations + declarations;
        return true;
    }
SvgCssLimits? RemainingLimits(SvgCssLimits limits, SvgStyleCollectionBudget budget, int characters, SvgSourceSpan? source, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (characters > limits.MaxCharacters - budget.Characters || budget.Rules >= limits.MaxRules || budget.Declarations >= limits.MaxDeclarations)
        {
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The cumulative embedded stylesheet limit was exceeded.", source));
            return null;
        }
        SvgCssLimits result = new SvgStyleSheetCollector().CopyLimits(limits);
        result.MaxCharacters = limits.MaxCharacters - budget.Characters;
        result.MaxRules = limits.MaxRules - budget.Rules;
        result.MaxDeclarations = limits.MaxDeclarations - budget.Declarations;
        return result;
    }
SvgCssLimits CopyLimits(SvgCssLimits source)
    {
        SvgCssLimits result = new SvgCssLimits();
        result.MaxCharacters = source.MaxCharacters;
        result.MaxTokens = source.MaxTokens;
        result.MaxRules = source.MaxRules;
        result.MaxDeclarations = source.MaxDeclarations;
        result.MaxSelectorTokens = source.MaxSelectorTokens;
        result.MaxBlockDepth = source.MaxBlockDepth;
        result.MaxImportDepth = source.MaxImportDepth;
        result.MaxVariableExpansionTokens = source.MaxVariableExpansionTokens;
        return result;
    }
System.Collections.Generic.List<SvgNode> FindStyleElements(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.HashSet<SvgNode> visited = new System.Collections.Generic.HashSet<SvgNode>();
        stack.Add(root);
        while (stack.Count > 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (!visited.Add(node)) continue;
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style) result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
SvgSourceUri EmbeddedSource(SvgDocument document, SvgNode style, int index)
    {
        SvgSourceUri? parent = style.SourceSpan == null ? document.Source : style.SourceSpan.Source;
        string parentUri = parent == null ? document.BaseUri : parent.Uri;
        string uri = parentUri + "#embedded-style-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (parent != null && style.SourceSpan != null && object.ReferenceEquals(style.SourceSpan.Source, parent)) return SvgSourceUri.CreateNested(uri, parent, style.SourceSpan, index);
        return new SvgSourceUri(uri);
    }
string AttributeValue(SvgNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == localName) return attribute.Value;
            index = index + 1;
        }
        return "";
    }
int CountRules(System.Collections.Generic.List<SvgCssRule> rules)
    {
        int count = 0;
        System.Collections.Generic.List<SvgCssRule> stack = new System.Collections.Generic.List<SvgCssRule>(rules);
        while (stack.Count > 0)
        {
            int last = stack.Count - 1;
            SvgCssRule rule = stack[last];
            stack.RemoveAt(last);
            if (rule == null) continue;
            count = count + 1;
            if (rule.AtRule != null && rule.AtRule.Keyframes != null) count = count + rule.AtRule.Keyframes.Keyframes.Count;
            int index = 0;
            while (index < rule.ChildRules.Count)
            {
                stack.Add(rule.ChildRules[index]);
                index = index + 1;
            }
        }
        return count;
    }
int CountDeclarations(System.Collections.Generic.List<SvgCssRule> rules)
    {
        int count = 0;
        System.Collections.Generic.List<SvgCssRule> stack = new System.Collections.Generic.List<SvgCssRule>(rules);
        while (stack.Count > 0)
        {
            int last = stack.Count - 1;
            SvgCssRule rule = stack[last];
            stack.RemoveAt(last);
            if (rule == null) continue;
            count = count + rule.Declarations.Count;
            count = count + new SvgStyleSheetCollector().CountExpandedDeclarations(rule.Declarations);
            if (rule.AtRule != null && rule.AtRule.Keyframes != null)
            {
                int keyframeIndex = 0;
                while (keyframeIndex < rule.AtRule.Keyframes.Keyframes.Count)
                {
                    count = count + rule.AtRule.Keyframes.Keyframes[keyframeIndex].Declarations.Count;
                    count = count + new SvgStyleSheetCollector().CountExpandedDeclarations(rule.AtRule.Keyframes.Keyframes[keyframeIndex].Declarations);
                    keyframeIndex = keyframeIndex + 1;
                }
            }
            int index = 0;
            while (index < rule.ChildRules.Count)
            {
                stack.Add(rule.ChildRules[index]);
                index = index + 1;
            }
        }
        return count;
    }
int CountExpandedDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations)
    {
        int count = 0;
        int index = 0;
        while (index < declarations.Count)
        {
            SvgCssDeclaration declaration = declarations[index];
            if (declaration != null && declaration.ExpandedDeclarations.Count > 1) count = count + declaration.ExpandedDeclarations.Count - 1;
            index = index + 1;
        }
        return count;
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> destination, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
string TrimAsciiWhitespace(string value)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && new SvgStyleSheetCollector().IsWhitespace(value[start])) start = start + 1;
        while (end > start && new SvgStyleSheetCollector().IsWhitespace(value[end - 1])) end = end - 1;
        return value.Substring(start, end - start);
    }
bool AsciiEquals(string left, string right)
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
bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r' || value == '\f';
    }
bool ValidLimits(SvgCssLimits limits)
    {
        return limits != null && limits.MaxCharacters > 0 && limits.MaxTokens > 0 && limits.MaxRules > 0 && limits.MaxDeclarations > 0 && limits.MaxSelectorTokens > 0 && limits.MaxBlockDepth > 0;
    }
}
