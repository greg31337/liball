namespace svg;

internal static class SvgCssValidator
{
    internal static void Validate(SvgValidationWork work)
    {
        System.Collections.Generic.List<SvgStyleSheet> caller = new System.Collections.Generic.List<SvgStyleSheet>();
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits((work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), caller, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Limits);
        AddCollectionDiagnostics(collection, work);
        int index = 0;
        while (index < collection.StyleSheets.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry != null) ValidateRules(entry.StyleSheet.Rules, entry.SourceNode, work);
            index = index + 1;
        }
        ValidateElementCss(work.Result.Document.DocumentElement, work);
        if (work.Result.Error == SvgValidationError.None) ValidateComputed(collection, work);
    }

    static void AddCollectionDiagnostics(SvgStyleSheetCollection collection, SvgValidationWork work)
    {
        int index = 0;
        while (index < collection.Diagnostics.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgDiagnostic diagnostic = collection.Diagnostics[index];
            if (diagnostic != null)
            {
                SvgValidationRule rule = DiagnosticRule(collection, diagnostic);
                SvgNode? node = NodeForSource(collection, diagnostic.Source);
                string recovery = "Ignore the invalid CSS construct and continue parsing at the next recoverable boundary.";
                new SvgValidator().AddLocated(work, rule, diagnostic.Severity, diagnostic.Code, diagnostic.Message, node, null, diagnostic.Source, recovery);
            }
            index = index + 1;
        }
    }

    static SvgValidationRule DiagnosticRule(SvgStyleSheetCollection collection, SvgDiagnostic diagnostic)
    {
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            SvgValidationRule found = RuleDiagnostic(entry.StyleSheet.Rules, diagnostic);
            if (found != SvgValidationRule.CssDeclaration) return found;
            index = index + 1;
        }
        string message = diagnostic.Message.ToLowerInvariant();
        if (message.Contains("selector") || message.Contains("qualified rule") || message.Contains("pseudo-class")) return SvgValidationRule.CssSelector;
        if (message.Contains("at-rule") || message.Contains("media rule") || message.Contains("supports rule") || message.Contains("namespace rule") || message.Contains("import rule") || message.Contains("layer") || message.Contains("keyframe") || message.Contains("font-face")) return SvgValidationRule.CssAtRule;
        return SvgValidationRule.CssDeclaration;
    }

    static SvgValidationRule RuleDiagnostic(System.Collections.Generic.List<SvgCssRule> rules, SvgDiagnostic diagnostic)
    {
        int index = 0;
        while (index < rules.Count)
        {
            SvgCssRule rule = rules[index];
            if (rule != null)
            {
                if (rule.Selectors != null && ContainsDiagnostic(rule.Selectors.Diagnostics, diagnostic)) return SvgValidationRule.CssSelector;
                if (rule.AtRule != null && ContainsDiagnostic(rule.AtRule.Diagnostics, diagnostic)) return SvgValidationRule.CssAtRule;
                if (DeclarationsContain(rule.Declarations, diagnostic)) return SvgValidationRule.CssDeclaration;
                if (rule.AtRule != null && rule.AtRule.Keyframes != null && KeyframesContain(rule.AtRule.Keyframes, diagnostic)) return SvgValidationRule.CssDeclaration;
                SvgValidationRule child = RuleDiagnostic(rule.ChildRules, diagnostic);
                if (child != SvgValidationRule.CssDeclaration) return child;
            }
            index = index + 1;
        }
        return SvgValidationRule.CssDeclaration;
    }

    static bool ContainsDiagnostic(System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgDiagnostic target)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (object.ReferenceEquals(diagnostics[index], target)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool DeclarationsContain(System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgDiagnostic diagnostic)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index] != null && ContainsDiagnostic(declarations[index].Diagnostics, diagnostic)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool KeyframesContain(SvgCssKeyframes keyframes, SvgDiagnostic diagnostic)
    {
        int index = 0;
        while (index < keyframes.Keyframes.Count)
        {
            if (DeclarationsContain(keyframes.Keyframes[index].Declarations, diagnostic)) return true;
            index = index + 1;
        }
        return false;
    }

    static SvgNode? NodeForSource(SvgStyleSheetCollection collection, SvgSourceSpan? source)
    {
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry.SourceNode != null && source != null && entry.StyleSheet.Source != null && object.ReferenceEquals(source.Source, entry.StyleSheet.Source.Source)) return entry.SourceNode;
            index = index + 1;
        }
        return collection.StyleSheets.Count == 0 ? null : collection.StyleSheets[0].SourceNode;
    }

    static void ValidateRules(System.Collections.Generic.List<SvgCssRule> rules, SvgNode? node, SvgValidationWork work)
    {
        int index = 0;
        while (index < rules.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgCssRule rule = rules[index];
            if (rule != null)
            {
                if (rule.Kind == SvgCssRuleKind.AtRule && rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.Unknown)
                {
                    new SvgValidator().AddLocated(work, SvgValidationRule.CssAtRule, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.UnsupportedFeature, "The CSS at-rule '@" + rule.AtRule.Name + "' is unsupported and will be ignored.", node, null, rule.Source, "Preserve the at-rule text but omit it from the cascade.");
                }
                bool descriptors = rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.FontFace;
                if (!descriptors) ValidateDeclarations(rule.Declarations, node, null, work);
                if (rule.AtRule != null && rule.AtRule.Keyframes != null) ValidateKeyframes(rule.AtRule.Keyframes, node, work);
                ValidateRules(rule.ChildRules, node, work);
            }
            index = index + 1;
        }
    }

    static void ValidateKeyframes(SvgCssKeyframes keyframes, SvgNode? node, SvgValidationWork work)
    {
        int index = 0;
        while (index < keyframes.Keyframes.Count && work.Result.Error == SvgValidationError.None)
        {
            ValidateDeclarations(keyframes.Keyframes[index].Declarations, node, null, work);
            index = index + 1;
        }
    }

    static void ValidateDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgNode? node, SvgAttribute? sourceAttribute, SvgValidationWork work)
    {
        int index = 0;
        while (index < declarations.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgCssDeclaration declaration = declarations[index];
            if (declaration != null && declaration.IsValid) ValidateProperty(node, sourceAttribute, declaration.Name, declaration.ValueText, declaration.Source, work);
            index = index + 1;
        }
    }

    static void ValidateProperty(SvgNode? node, SvgAttribute? attribute, string name, string value, SvgSourceSpan? source, SvgValidationWork work)
    {
        if (name.StartsWith("--", System.StringComparison.Ordinal)) return;
        SvgPropertySupportEntry? entry = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Properties.Find(name);
        SvgSourceSpan? located = attribute == null ? source : SourceInAttribute(attribute, source);
        if (entry == null)
        {
            new SvgValidator().AddLocated(work, SvgValidationRule.CssProperty, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.UnsupportedFeature, "The CSS property '" + name + "' is unknown and will be ignored.", node, attribute, located, "Ignore the unknown property declaration and continue the cascade.");
            return;
        }
        SvgCapabilitySet capabilities = SvgCapabilitySet.Create(new string[] { SvgFeatureIdentifiers.CssParsing });
        if (work.Options.Css.Properties.Supports(name, value, work.Options.Css.Limits, capabilities)) return;
        bool unsupported = entry.RequiredCapability.Length != 0 && !capabilities.Contains(entry.RequiredCapability) || entry.ValueKind == SvgPropertyValueSupportKind.Keyword && SimpleIdentifier(value);
        SvgValidationRule rule = unsupported ? SvgValidationRule.CssUnsupportedValue : SvgValidationRule.CssProperty;
        SvgDiagnosticSeverity severity = unsupported ? SvgDiagnosticSeverity.Warning : SvgDiagnosticSeverity.Error;
        SvgDiagnosticCode code = unsupported ? SvgDiagnosticCode.UnsupportedFeature : SvgDiagnosticCode.InvalidCss;
        string message = unsupported ? "The value for CSS property '" + name + "' is syntactically valid but unsupported." : "The value for CSS property '" + name + "' does not match grammar " + entry.Grammar + ".";
        new SvgValidator().AddLocated(work, rule, severity, code, message, node, attribute, located, "Ignore the unsupported or invalid declaration and continue the cascade.");
    }

    static bool SimpleIdentifier(string value)
    {
        string text = value.Trim();
        if (text.Length == 0) return false;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (!(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') && character != '-') return false;
            index = index + 1;
        }
        return true;
    }

    static void ValidateElementCss(SvgNode? root, SvgValidationWork work)
    {
        if (root == null) return;
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(root);
        while (stack.Count > 0 && work.Result.Error == SvgValidationError.None)
        {
            SvgNode node = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (node.Kind == SvgNodeKind.Element) ValidateElementAttributes(node, work);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(node.Children[index]);
                index = index - 1;
            }
        }
    }

    static void ValidateElementAttributes(SvgNode node, SvgValidationWork work)
    {
        int index = 0;
        while (index < node.Attributes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute != null && attribute.NamespaceUri.Length == 0)
            {
                if (attribute.LocalName == SvgAttributeNames.Style) ValidateStyleAttribute(node, attribute, work);
                else
                {
                    SvgPropertySupportEntry? entry = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Properties.FindPresentationAttribute(attribute.LocalName);
                    if (entry != null) ValidateProperty(node, attribute, entry.Name, attribute.Value, attribute.SourceSpan, work);
                }
            }
            index = index + 1;
        }
    }

    static void ValidateStyleAttribute(SvgNode node, SvgAttribute attribute, SvgValidationWork work)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(attribute.Value, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Limits);
        int index = 0;
        while (index < parsed.Diagnostics.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgDiagnostic diagnostic = parsed.Diagnostics[index];
            int offset = diagnostic.Source == null ? 0 : (int)diagnostic.Source.StartOffset;
            new SvgValidator().AddLocated(work, SvgValidationRule.CssDeclaration, diagnostic.Severity, diagnostic.Code, diagnostic.Message, node, attribute, new SvgValidator().LocateValue(attribute, offset), "Ignore the malformed declaration and continue at the next semicolon.");
            index = index + 1;
        }
        ValidateDeclarations(parsed.Declarations, node, attribute, work);
    }

    static SvgSourceSpan? SourceInAttribute(SvgAttribute attribute, SvgSourceSpan? source)
    {
        if (source == null || source.StartOffset < 0 || source.StartOffset > 2147483647) return attribute.SourceSpan;
        return new SvgValidator().LocateValue(attribute, (int)source.StartOffset);
    }

    static void ValidateComputed(SvgStyleSheetCollection collection, SvgValidationWork work)
    {
        SvgStyleResolver resolver = new SvgStyleResolver();
        SvgComputedStyleTree tree = resolver.ResolveDocument((work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), collection, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css);
        int styleIndex = 0;
        while (styleIndex < tree.Styles.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgComputedStyle style = tree.Styles[styleIndex];
            ValidateVariableFailures(style, work);
            ValidateComputedFailures(style, work);
            styleIndex = styleIndex + 1;
        }
    }

    static void ValidateVariableFailures(SvgComputedStyle style, SvgValidationWork work)
    {
        int index = 0;
        while (index < (style.Cascade ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Values.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgCascadedValue value = style.Cascade.Values[index];
            if (value.InvalidAtComputedValue && value.Winner != null)
            {
                SvgValidationRule rule = value.VariableError == SvgCssVariableError.CyclicVariable ? SvgValidationRule.CssVariable : SvgValidationRule.CssComputedValue;
                string message = value.VariableError == SvgCssVariableError.CyclicVariable ? "The custom property '" + value.PropertyName + "' participates in a dependency cycle." : "The CSS property '" + value.PropertyName + "' is invalid at computed-value time after variable substitution.";
                AddComputedOnce(work, rule, message, (style.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), value.Winner.Source, value.PropertyName);
            }
            index = index + 1;
        }
    }

    static void ValidateComputedFailures(SvgComputedStyle style, SvgValidationWork work)
    {
        int index = 0;
        while (index < style.Values.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgComputedValue value = style.Values[index];
            if (value.Recovered) AddComputedOnce(work, SvgValidationRule.CssComputedValue, "The CSS property '" + value.PropertyName + "' failed typed computed-value conversion.", (style.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), value.Source, value.PropertyName);
            index = index + 1;
        }
    }

    static void AddComputedOnce(SvgValidationWork work, SvgValidationRule rule, string message, SvgNode node, SvgSourceSpan? source, string propertyName)
    {
        if (AlreadyAdded(work, rule, source, propertyName)) return;
        SvgAttribute? attribute = AttributeAtSource(node, source);
        new SvgValidator().AddLocated(work, rule, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, message, node, attribute, source, "Use the inherited value for an inherited property; otherwise use the property's initial value.");
    }

    static bool AlreadyAdded(SvgValidationWork work, SvgValidationRule rule, SvgSourceSpan? source, string propertyName)
    {
        int index = 0;
        while (index < work.Result.Issues.Count)
        {
            SvgValidationIssue issue = work.Result.Issues[index];
            if (issue.Rule == rule && SameSource(issue.Diagnostic.Source, source) && issue.Diagnostic.Message.Contains("'" + propertyName + "'")) return true;
            index = index + 1;
        }
        return false;
    }

    static bool SameSource(SvgSourceSpan? first, SvgSourceSpan? second)
    {
        if (first == null || second == null) return first == null && second == null;
        return object.ReferenceEquals(first.Source, second.Source) && first.StartOffset == second.StartOffset && first.EndOffset == second.EndOffset;
    }

    static SvgAttribute? AttributeAtSource(SvgNode? node, SvgSourceSpan? source)
    {
        if (node == null || source == null) return null;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute != null && SameSource(attribute.SourceSpan, source)) return attribute;
            index = index + 1;
        }
        return null;
    }
}
