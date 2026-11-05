namespace svg;

internal class SvgStyleTextPlan
{
    internal SvgNode? Node;
    internal string Value;

    internal SvgStyleTextPlan(SvgNode? node, string value)
    {
        Node = node;
        Value = value;
    }
}

internal class SvgStyleAttributePlan
{
    internal SvgNode Node;
    internal SvgAttribute? Attribute;
    internal string Value;

    internal SvgStyleAttributePlan(SvgNode node, SvgAttribute? attribute, string value)
    {
        Node = node;
        Attribute = attribute;
        Value = value;
    }
}

internal class SvgStyleRuleWork
{
    internal bool Style;
    internal string Selector;
    internal string Declarations;
    internal string Text;

    internal SvgStyleRuleWork(bool style, string selector, string declarations, string text)
    {
        Style = style;
        Selector = selector;
        Declarations = declarations;
        Text = text;
    }
}

internal static class SvgStyleCleanup
{
    internal static void CleanupStyles(SvgProcessContext context)
    {
        if (UnsafeBehavior(context.Document)) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        bool convertPresentation = !HasAuthoredStyleSheet(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element) CleanupStyleAttribute(context, node, convertPresentation);
            index = index + 1;
        }
        if (context.IsFailed()) return;
        System.Collections.Generic.List<SvgNode> elements = ElementNodes(nodes);
        index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style) CleanupStyleElement(context, node, elements);
            index = index + 1;
        }
    }

    internal static void InlineStyles(SvgProcessContext context)
    {
        if (UnsafeBehavior(context.Document) || ExternalStyleSheet(context.Document)) return;
        SvgCssLimits limits = new SvgCssLimits();
        System.Collections.Generic.List<SvgStyleSheet> caller = new System.Collections.Generic.List<SvgStyleSheet>();
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(context.Document, caller, limits);
        if (!collection.IsValid || collection.StyleSheets.Count == 0 || !StaticInlineCollection(collection)) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        if (!ValidInlineAttributes(nodes, limits)) return;
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();
        options.Limits = limits;
        options.IncludeUserAgentDefaults = false;
        SvgComputedStyleTree tree = new SvgStyleResolver().ResolveDocument(context.Document, collection, options);
        if (!tree.Success) return;
        System.Collections.Generic.List<SvgStyleAttributePlan> plans = new System.Collections.Generic.List<SvgStyleAttributePlan>();
        int styleIndex = 0;
        while (styleIndex < tree.Styles.Count)
        {
            SvgComputedStyle style = tree.Styles[styleIndex];
            if ((style.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind != SvgElementKind.Style)
            {
                SvgAttribute? attribute = FindAttribute(style.Node, SvgAttributeNames.Style);
                string current = attribute == null ? "" : attribute.Value;
                System.Text.StringBuilder builder = new System.Text.StringBuilder(current);
                int valueIndex = 0;
                while (valueIndex < (style.Cascade ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Values.Count)
                {
                    SvgCascadedValue value = style.Cascade.Values[valueIndex];
                    if (value.Winner != null && value.Winner.SourceKind == SvgCascadeSourceKind.StyleSheet && DirectCandidate(style.Cascade, value.Winner))
                    {
                        AppendDeclaration(builder, value.PropertyName, value.Winner.ValueText, value.Winner.Important);
                    }
                    valueIndex = valueIndex + 1;
                }
                string replacement = builder.ToString();
                if (replacement != current) plans.Add(new SvgStyleAttributePlan(style.Node, attribute, replacement));
            }
            styleIndex = styleIndex + 1;
        }
        int planIndex = 0;
        while (planIndex < plans.Count && !context.IsFailed())
        {
            ApplyStyleAttribute(context, plans[planIndex]);
            planIndex = planIndex + 1;
        }
        if (context.IsFailed()) return;
        int collectionIndex = collection.StyleSheets.Count - 1;
        while (collectionIndex >= 0 && !context.IsFailed())
        {
            SvgCollectedStyleSheet collected = collection.StyleSheets[collectionIndex];
            if (collected.SourceKind == SvgStyleSheetSourceKind.Embedded) RemoveNode(context, collected.SourceNode);
            collectionIndex = collectionIndex - 1;
        }
    }

    internal static void ResolveCustomProperties(SvgProcessContext context)
    {
        if (UnsafeBehavior(context.Document) || ExternalStyleSheet(context.Document)) return;
        SvgCssLimits limits = new SvgCssLimits();
        System.Collections.Generic.List<SvgStyleSheet> caller = new System.Collections.Generic.List<SvgStyleSheet>();
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(context.Document, caller, limits);
        if (!collection.IsValid || DynamicCollection(collection)) return;
        SvgStyleResolverOptions options = new SvgStyleResolverOptions();
        options.Limits = limits;
        options.IncludeUserAgentDefaults = false;
        SvgComputedStyleTree tree = new SvgStyleResolver().ResolveDocument(context.Document, collection, options);
        if (!tree.Success) return;
        System.Collections.Generic.List<SvgStyleAttributePlan> attributes = ResolveElementAttributes(tree, limits);
        System.Collections.Generic.List<SvgStyleTextPlan> sheets = ResolveStyleSheets(collection, tree, limits);
        int index = 0;
        while (index < attributes.Count && !context.IsFailed())
        {
            ApplyAttributePlan(context, attributes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < sheets.Count && !context.IsFailed())
        {
            SetText(context, sheets[index].Node, sheets[index].Value);
            index = index + 1;
        }
    }

    static void CleanupStyleAttribute(SvgProcessContext context, SvgNode node, bool convertPresentation)
    {
        SvgAttribute? styleAttribute = FindAttribute(node, SvgAttributeNames.Style);
        if (styleAttribute == null) return;
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(styleAttribute.Value, new SvgCssLimits());
        System.Collections.Generic.List<SvgCssDeclaration>? declarations = CleanDeclarations(parsed);
        if (declarations == null) return;
        bool[] converted = new bool[declarations.Count];
        bool changed = declarations.Count != parsed.Declarations.Count;
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        if (HasShorthand(declarations)) convertPresentation = false;
        int index = 0;
        while (index < declarations.Count && convertPresentation && !context.IsFailed())
        {
            SvgCssDeclaration declaration = declarations[index];
            SvgPropertySupportEntry? property = properties.Find(declaration.Name);
            if (!declaration.Important && !declaration.IsShorthand && property != null && property.IsPresentationAttribute)
            {
                SvgAttribute? current = FindAttribute(node, property.PresentationAttributeName);
                SetAttribute(context, node, current, property.PresentationAttributeName, declaration.ValueText.Trim());
                converted[index] = true;
                changed = true;
            }
            index = index + 1;
        }
        if (context.IsFailed() || !changed) return;
        string value = DeclarationText(declarations, converted);
        if (value.Length == 0) RemoveAttribute(context, node, styleAttribute);
        else ReplaceAttribute(context, node, styleAttribute, SvgAttributeNames.Style, value);
    }

    static void CleanupStyleElement(SvgProcessContext context, SvgNode style, System.Collections.Generic.List<SvgNode> elements)
    {
        string type = AttributeValue(style, SvgAttributeNames.Type).Trim();
        if (type.Length != 0 && !string.Equals(type, SvgMimeTypes.Css, System.StringComparison.OrdinalIgnoreCase)) return;
        string source = new SvgNodeApi().GetTextContent(style);
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(source, new SvgCssLimits());
        if (!parsed.Success || parsed.StyleSheet == null) return;
        System.Collections.Generic.List<SvgStyleRuleWork> rules = new System.Collections.Generic.List<SvgStyleRuleWork>();
        bool changed = false;
        int index = 0;
        while (index < parsed.StyleSheet.Rules.Count)
        {
            SvgCssRule rule = parsed.StyleSheet.Rules[index];
            if (rule.Kind != SvgCssRuleKind.Style || !rule.IsValid || rule.Selectors == null || rule.ChildRules.Count != 0)
            {
                rules.Add(new SvgStyleRuleWork(false, "", "", rule.RawText));
            }
            else
            {
                string selector = UsedSelectorText(rule.Selectors, parsed.StyleSheet.Namespaces, elements);
                if (selector.Length == 0)
                {
                    changed = true;
                    index = index + 1;
                    continue;
                }
                SvgCssParseResult declarationParse = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(DeclarationText(rule.Declarations, null), new SvgCssLimits());
                System.Collections.Generic.List<SvgCssDeclaration>? declarations = CleanDeclarations(declarationParse);
                if (declarations == null)
                {
                    rules.Add(new SvgStyleRuleWork(false, "", "", rule.RawText));
                }
                else
                {
                    string declarationText = DeclarationText(declarations, null);
                    bool ruleChanged = selector != rule.Selectors.RawText || declarations.Count != rule.Declarations.Count;
                    string text = ruleChanged ? RuleText(selector, declarationText) : rule.RawText;
                    rules.Add(new SvgStyleRuleWork(true, selector, declarationText, text));
                    if (ruleChanged) changed = true;
                }
            }
            index = index + 1;
        }
        index = 1;
        while (index < rules.Count)
        {
            SvgStyleRuleWork previous = rules[index - 1];
            SvgStyleRuleWork current = rules[index];
            if (previous.Style && current.Style && previous.Selector == current.Selector)
            {
                string declarations = CleanDeclarationText(previous.Declarations + current.Declarations);
                previous.Declarations = declarations;
                previous.Text = RuleText(previous.Selector, declarations);
                rules.RemoveAt(index);
                changed = true;
                continue;
            }
            if (previous.Style && current.Style && previous.Declarations == current.Declarations)
            {
                previous.Selector = previous.Selector + "," + current.Selector;
                previous.Text = RuleText(previous.Selector, previous.Declarations);
                rules.RemoveAt(index);
                changed = true;
                continue;
            }
            index = index + 1;
        }
        if (!changed) return;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        index = 0;
        while (index < rules.Count)
        {
            builder.Append(rules[index].Text);
            index = index + 1;
        }
        SetText(context, style, builder.ToString());
    }

    static System.Collections.Generic.List<SvgCssDeclaration>? CleanDeclarations(SvgCssParseResult parsed)
    {
        if (parsed == null || !parsed.Success || parsed.UnknownSyntax.Count != 0) return null;
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            if (declaration == null || !declaration.IsValid) return null;
            if (declaration.IsShorthand) return new System.Collections.Generic.List<SvgCssDeclaration>(parsed.Declarations);
            index = index + 1;
        }
        bool[] keep = new bool[parsed.Declarations.Count];
        index = 0;
        while (index < keep.Length)
        {
            keep[index] = true;
            index = index + 1;
        }
        index = 0;
        while (index < parsed.Declarations.Count)
        {
            if (keep[index])
            {
                int later = index + 1;
                while (later < parsed.Declarations.Count)
                {
                    if (keep[later] && SameProperty(parsed.Declarations[index].Name, parsed.Declarations[later].Name))
                    {
                        if (parsed.Declarations[index].Important && !parsed.Declarations[later].Important) keep[later] = false;
                        else
                        {
                            keep[index] = false;
                            break;
                        }
                    }
                    later = later + 1;
                }
            }
            index = index + 1;
        }
        System.Collections.Generic.List<SvgCssDeclaration> result = new System.Collections.Generic.List<SvgCssDeclaration>();
        index = 0;
        while (index < parsed.Declarations.Count)
        {
            if (keep[index]) result.Add(parsed.Declarations[index]);
            index = index + 1;
        }
        return result;
    }

    static string CleanDeclarationText(string value)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(value, new SvgCssLimits());
        System.Collections.Generic.List<SvgCssDeclaration>? declarations = CleanDeclarations(parsed);
        return declarations == null ? value : DeclarationText(declarations, null);
    }

    static bool SameProperty(string first, string second)
    {
        bool custom = first.StartsWith("--", System.StringComparison.Ordinal) || second.StartsWith("--", System.StringComparison.Ordinal);
        return custom ? first == second : string.Equals(first, second, System.StringComparison.OrdinalIgnoreCase);
    }

    static bool HasShorthand(System.Collections.Generic.List<SvgCssDeclaration> declarations)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].IsShorthand) return true;
            index = index + 1;
        }
        return false;
    }

    static bool DirectCandidate(SvgCascadeResult cascade, SvgCascadeCandidate candidate)
    {
        int index = 0;
        while (index < cascade.Candidates.Count)
        {
            if (object.ReferenceEquals(cascade.Candidates[index], candidate)) return true;
            index = index + 1;
        }
        return false;
    }

    static string DeclarationText(System.Collections.Generic.List<SvgCssDeclaration> declarations, bool[]? omitted)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < declarations.Count)
        {
            if (omitted == null || !omitted[index]) builder.Append(declarations[index].RawText);
            index = index + 1;
        }
        return builder.ToString();
    }

    static string UsedSelectorText(SvgSelector selector, SvgSelectorNamespaceBindings namespaces, System.Collections.Generic.List<SvgNode> elements)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < selector.Items.Count)
        {
            SvgComplexSelector item = selector.Items[index];
            bool used = SelectorDynamic(item) || SelectorMatches(item.RawText, namespaces, elements);
            if (used)
            {
                if (builder.Length != 0) builder.Append(',');
                builder.Append(item.RawText);
            }
            index = index + 1;
        }
        return builder.ToString();
    }

    static bool SelectorMatches(string text, SvgSelectorNamespaceBindings namespaces, System.Collections.Generic.List<SvgNode> elements)
    {
        SvgSelectorParseResult parsed = new SvgCssParser().ParseSelectorWithValueAndLimits(text, new SvgCssLimits());
        if (!parsed.Success || parsed.Selector == null) return true;
        int index = 0;
        while (index < elements.Count)
        {
            SvgSelectorMatchOptions options = new SvgSelectorMatchOptions();
            options.Namespaces = namespaces;
            SvgSelectorMatchResult match = new SvgSelectorMatcher().Match(parsed.Selector, elements[index], options);
            if (!match.Success) return true;
            if (match.Matches) return true;
            index = index + 1;
        }
        return false;
    }

    static bool SelectorDynamic(SvgComplexSelector selector)
    {
        int compoundIndex = 0;
        while (compoundIndex < selector.Compounds.Count)
        {
            SvgCompoundSelector compound = selector.Compounds[compoundIndex];
            int simpleIndex = 0;
            while (simpleIndex < compound.SimpleSelectors.Count)
            {
                if (compound.SimpleSelectors[simpleIndex].Kind == SvgSimpleSelectorKind.PseudoClass) return true;
                simpleIndex = simpleIndex + 1;
            }
            compoundIndex = compoundIndex + 1;
        }
        return false;
    }

    static bool SelectorDynamic(SvgSelector selector)
    {
        int index = 0;
        while (index < selector.Items.Count)
        {
            if (SelectorDynamic(selector.Items[index])) return true;
            index = index + 1;
        }
        return false;
    }

    static bool StaticInlineCollection(SvgStyleSheetCollection collection)
    {
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry.SourceKind != SvgStyleSheetSourceKind.Embedded || entry.Media.Trim().Length != 0) return false;
            int ruleIndex = 0;
            while (ruleIndex < entry.StyleSheet.Rules.Count)
            {
                SvgCssRule rule = entry.StyleSheet.Rules[ruleIndex];
                if (rule.Kind != SvgCssRuleKind.Style || !rule.IsValid || rule.Selectors == null || rule.ChildRules.Count != 0 || SelectorDynamic(rule.Selectors)) return false;
                if (!KnownDeclarations(rule.Declarations, properties)) return false;
                ruleIndex = ruleIndex + 1;
            }
            index = index + 1;
        }
        return true;
    }

    static bool DynamicCollection(SvgStyleSheetCollection collection)
    {
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry.SourceKind != SvgStyleSheetSourceKind.Embedded || entry.Media.Trim().Length != 0) return true;
            int ruleIndex = 0;
            while (ruleIndex < entry.StyleSheet.Rules.Count)
            {
                SvgCssRule rule = entry.StyleSheet.Rules[ruleIndex];
                if (rule.Kind == SvgCssRuleKind.Style && rule.Selectors != null && SelectorDynamic(rule.Selectors)) return true;
                if (rule.ChildRules.Count != 0 || rule.Kind == SvgCssRuleKind.AtRule) return true;
                ruleIndex = ruleIndex + 1;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ValidInlineAttributes(System.Collections.Generic.List<SvgNode> nodes, SvgCssLimits limits)
    {
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element)
            {
                SvgAttribute? attribute = FindAttribute(node, SvgAttributeNames.Style);
                if (attribute != null)
                {
                    SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(attribute.Value, limits);
                    if (!parsed.Success || parsed.UnknownSyntax.Count != 0) return false;
                    if (!KnownDeclarations(parsed.Declarations, properties) || HasCascadeRollback(parsed.Declarations)) return false;
                }
            }
            index = index + 1;
        }
        return true;
    }

    static bool KnownDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgPropertyCatalog properties)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            SvgCssDeclaration declaration = declarations[index];
            if (declaration == null || !declaration.IsValid) return false;
            if (!declaration.Name.StartsWith("--", System.StringComparison.Ordinal) && properties.Find(declaration.Name) == null) return false;
            index = index + 1;
        }
        return true;
    }

    static bool HasCascadeRollback(System.Collections.Generic.List<SvgCssDeclaration> declarations)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            string value = declarations[index].ValueText.Trim();
            if (string.Equals(value, "revert", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "revert-layer", System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }

    static System.Collections.Generic.List<SvgStyleAttributePlan> ResolveElementAttributes(SvgComputedStyleTree tree, SvgCssLimits limits)
    {
        System.Collections.Generic.List<SvgStyleAttributePlan> result = new System.Collections.Generic.List<SvgStyleAttributePlan>();
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        int index = 0;
        while (index < tree.Styles.Count)
        {
            SvgComputedStyle computed = tree.Styles[index];
            SvgNode node = (computed.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            SvgAttribute? style = FindAttribute((node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.Style);
            if (style != null)
            {
                SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(style.Value, limits);
                if (parsed.Success && parsed.UnknownSyntax.Count == 0)
                {
                    string? value = ResolveDeclarations(parsed.Declarations, computed.CustomProperties, limits);
                    if (value != null && value != style.Value) result.Add(new SvgStyleAttributePlan(node, style, value));
                }
            }
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                SvgAttribute attribute = node.Attributes[attributeIndex];
                SvgPropertySupportEntry? property = attribute.NamespaceUri.Length == 0 ? properties.FindPresentationAttribute(attribute.LocalName) : null;
                if (property != null && ContainsVariable(attribute.Value))
                {
                    SvgCssVariableResolveResult resolved = ResolveVariable(computed.CustomProperties, attribute.Value, limits);
                    if (resolved.Success && resolved.Text != attribute.Value) result.Add(new SvgStyleAttributePlan(node, attribute, resolved.Text));
                }
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<SvgStyleTextPlan> ResolveStyleSheets(SvgStyleSheetCollection collection, SvgComputedStyleTree tree, SvgCssLimits limits)
    {
        System.Collections.Generic.List<SvgStyleTextPlan> result = new System.Collections.Generic.List<SvgStyleTextPlan>();
        System.Collections.Generic.List<SvgNode> elements = StyleNodes(tree);
        int collectionIndex = 0;
        while (collectionIndex < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[collectionIndex];
            System.Text.StringBuilder sheet = new System.Text.StringBuilder();
            bool changed = false;
            int ruleIndex = 0;
            while (ruleIndex < entry.StyleSheet.Rules.Count)
            {
                SvgCssRule rule = entry.StyleSheet.Rules[ruleIndex];
                if (rule.Kind == SvgCssRuleKind.Style && rule.Selectors != null && rule.IsValid)
                {
                    string? declarations = ResolveRuleDeclarations(rule, entry.StyleSheet.Namespaces, elements, tree, limits);
                    if (declarations != null)
                    {
                        sheet.Append(RuleText(rule.Selectors.RawText, declarations));
                        changed = true;
                    }
                    else sheet.Append(rule.RawText);
                }
                else sheet.Append(rule.RawText);
                ruleIndex = ruleIndex + 1;
            }
            if (changed && entry.SourceKind == SvgStyleSheetSourceKind.Embedded) result.Add(new SvgStyleTextPlan(entry.SourceNode, sheet.ToString()));
            collectionIndex = collectionIndex + 1;
        }
        return result;
    }

    static string? ResolveRuleDeclarations(SvgCssRule rule, SvgSelectorNamespaceBindings namespaces, System.Collections.Generic.List<SvgNode> elements, SvgComputedStyleTree tree, SvgCssLimits limits)
    {
        System.Collections.Generic.List<SvgNode>? matches = MatchingNodes(rule.Selectors, namespaces, elements);
        if (matches == null || matches.Count == 0) return null;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool changed = false;
        int declarationIndex = 0;
        while (declarationIndex < rule.Declarations.Count)
        {
            SvgCssDeclaration declaration = rule.Declarations[declarationIndex];
            string? replacement = null;
            if (!declaration.Name.StartsWith("--", System.StringComparison.Ordinal) && ContainsVariable(declaration.ValueText))
            {
                int matchIndex = 0;
                while (matchIndex < matches.Count)
                {
                    SvgComputedStyle? computed = tree.Find(matches[matchIndex]);
                    SvgCssVariableResolveResult? resolved = computed == null ? null : ResolveVariable(computed.CustomProperties, declaration.ValueText, limits);
                    if (resolved == null || !resolved.Success)
                    {
                        replacement = null;
                        break;
                    }
                    string candidate = resolved.Text.Trim();
                    if (matchIndex == 0) replacement = candidate;
                    else if (replacement != candidate)
                    {
                        replacement = null;
                        break;
                    }
                    matchIndex = matchIndex + 1;
                }
            }
            if (replacement == null) builder.Append(declaration.RawText);
            else
            {
                AppendDeclaration(builder, declaration.Name, replacement, declaration.Important);
                changed = true;
            }
            declarationIndex = declarationIndex + 1;
        }
        return changed ? builder.ToString() : null;
    }

    static System.Collections.Generic.List<SvgNode>? MatchingNodes(SvgSelector? selector, SvgSelectorNamespaceBindings namespaces, System.Collections.Generic.List<SvgNode> elements)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < elements.Count)
        {
            SvgSelectorMatchOptions options = new SvgSelectorMatchOptions();
            options.Namespaces = namespaces;
            SvgSelectorMatchResult match = new SvgSelectorMatcher().Match(selector, elements[index], options);
            if (!match.Success) return null;
            if (match.Matches) result.Add(elements[index]);
            index = index + 1;
        }
        return result;
    }

    static string? ResolveDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgCustomPropertySet properties, SvgCssLimits limits)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool changed = false;
        int index = 0;
        while (index < declarations.Count)
        {
            SvgCssDeclaration declaration = declarations[index];
            if (!declaration.Name.StartsWith("--", System.StringComparison.Ordinal) && ContainsVariable(declaration.ValueText))
            {
                SvgCssVariableResolveResult resolved = ResolveVariable(properties, declaration.ValueText, limits);
                if (resolved.Success)
                {
                    AppendDeclaration(builder, declaration.Name, resolved.Text.Trim(), declaration.Important);
                    changed = true;
                }
                else builder.Append(declaration.RawText);
            }
            else builder.Append(declaration.RawText);
            index = index + 1;
        }
        return changed ? builder.ToString() : null;
    }

    static SvgCssVariableResolveResult ResolveVariable(SvgCustomPropertySet properties, string value, SvgCssLimits limits)
    {
        return new SvgCssVariableResolver().Resolve(properties, value, limits.MaxBlockDepth, limits.MaxVariableExpansionTokens, limits.MaxSelectorTokens);
    }

    static bool ContainsVariable(string value)
    {
        return value.IndexOf("var(", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void AppendDeclaration(System.Text.StringBuilder builder, string name, string value, bool important)
    {
        if (builder.Length != 0 && builder[builder.Length - 1] != ';') builder.Append(';');
        builder.Append(name);
        builder.Append(':');
        builder.Append(value.Trim());
        if (important) builder.Append("!important");
        builder.Append(';');
    }

    static string RuleText(string selector, string declarations)
    {
        return selector + "{" + declarations + "}";
    }

    static bool UnsafeBehavior(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element)
            {
                if (node.ElementKind == SvgElementKind.Script || AnimationKind(node.ElementKind)) return true;
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Count)
                {
                    if (node.Attributes[attributeIndex].LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase)) return true;
                    attributeIndex = attributeIndex + 1;
                }
            }
            index = index + 1;
        }
        return false;
    }

    static bool ExternalStyleSheet(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.ProcessingInstruction && string.Equals(node.LocalName, "xml-stylesheet", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (node.Kind == SvgNodeKind.Element && node.LocalName == "link" && AttributeValue(node, "rel").IndexOf("stylesheet", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            index = index + 1;
        }
        return false;
    }

    static bool HasAuthoredStyleSheet(SvgDocument document)
    {
        if (ExternalStyleSheet(document)) return true;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style && new SvgNodeApi().GetTextContent(node).Trim().Length != 0) return true;
            index = index + 1;
        }
        return false;
    }

    static bool AnimationKind(SvgElementKind kind)
    {
        return kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Set || kind == SvgElementKind.Discard;
    }

    static System.Collections.Generic.List<SvgNode> StyleNodes(SvgComputedStyleTree tree)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < tree.Styles.Count)
        {
            result.Add((tree.Styles[index].Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<SvgNode> ElementNodes(System.Collections.Generic.List<SvgNode> nodes)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index].Kind == SvgNodeKind.Element) result.Add(nodes[index]);
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(document.DocumentNode);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }

    static SvgAttribute? FindAttribute(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }

    static string AttributeValue(SvgNode node, string name)
    {
        SvgAttribute? attribute = FindAttribute(node, name);
        return attribute == null ? "" : attribute.Value;
    }

    static void ApplyStyleAttribute(SvgProcessContext context, SvgStyleAttributePlan plan)
    {
        if (plan.Attribute == null) SetAttribute(context, plan.Node, null, SvgAttributeNames.Style, plan.Value);
        else ReplaceAttribute(context, plan.Node, plan.Attribute, SvgAttributeNames.Style, plan.Value);
    }

    static void ApplyAttributePlan(SvgProcessContext context, SvgStyleAttributePlan plan)
    {
        ReplaceAttribute(context, plan.Node, plan.Attribute, (plan.Attribute ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LocalName, plan.Value);
    }

    static void SetAttribute(SvgProcessContext context, SvgNode node, SvgAttribute? current, string name, string value)
    {
        SvgAttribute replacement = context.Documents.CreateAttribute(name, "", name, "", value);
        SvgMutationResult mutation = current == null ? context.Mutations.AppendAttribute(node, replacement) : context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void ReplaceAttribute(SvgProcessContext context, SvgNode node, SvgAttribute? current, string name, string value)
    {
        if ((current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value == value) return;
        SvgAttribute replacement = context.Documents.CreateAttribute(name, current.Prefix, current.LocalName, current.NamespaceUri, value);
        SvgMutationResult mutation = context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void RemoveAttribute(SvgProcessContext context, SvgNode node, SvgAttribute attribute)
    {
        SvgMutationResult mutation = context.Mutations.RemoveAttribute(node, attribute);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void SetText(SvgProcessContext context, SvgNode? node, string value)
    {
        SvgMutationResult mutation = context.Mutations.SetTextContent(node, value);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void RemoveNode(SvgProcessContext context, SvgNode? node)
    {
        if ((node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parent == null) return;
        SvgMutationResult mutation = context.Mutations.RemoveChild(node.Parent, node);
        if (!mutation.Success) context.Fail(mutation.Message);
    }
}
