namespace svg;

/// <summary>Applies CSS origins, layers, specificity, source order, and CSS-wide keywords.</summary>
public class SvgCascade : ISvgCascade
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCascade()
    {
    }

    /// <summary>Cascades an ordered list of parsed stylesheets with default options.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in collection order.</param>
    /// <returns>The cascaded property values and diagnostics.</returns>
    public SvgCascadeResult ResolveWithNodeAndStyleSheets(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets)
    {
        return new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(node, styleSheets, new SvgCascadeOptions());
    }

    /// <summary>Cascades an ordered list of parsed stylesheets.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in collection order.</param>
    /// <param name="options">The property, inheritance, environment, and limit options.</param>
    /// <returns>The cascaded property values and diagnostics.</returns>
    public SvgCascadeResult ResolveWithNodeAndStyleSheetsAndOptions(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgCascadeOptions options)
    {
        System.Collections.Generic.List<SvgCascadeSheetInput> inputs = new System.Collections.Generic.List<SvgCascadeSheetInput>();
        if (styleSheets == null)
        {
            inputs.Add(new SvgCascadeSheetInput(null));
        }
        else
        {
            int index = 0;
            while (index < styleSheets.Count)
            {
                inputs.Add(new SvgCascadeSheetInput(styleSheets[index]));
                index = index + 1;
            }
        }
        return new SvgCascade().ResolveInputs(node, inputs, options, null);
    }

    /// <summary>Cascades a collected set of embedded, imported, and caller stylesheets.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="collection">The ordered stylesheet collection.</param>
    /// <param name="options">The property, inheritance, environment, and limit options.</param>
    /// <returns>The cascaded property values and diagnostics.</returns>
    public SvgCascadeResult ResolveWithNodeAndCollectionAndOptions(SvgNode node, SvgStyleSheetCollection? collection, SvgCascadeOptions options)
    {
        System.Collections.Generic.List<SvgCascadeSheetInput> inputs = new System.Collections.Generic.List<SvgCascadeSheetInput>();
        if (collection == null)
        {
            inputs.Add(new SvgCascadeSheetInput(null));
        }
        else if (options != null && options.StyleEnvironment != null && options.Limits != null)
        {
            int index = 0;
            while (index < collection.StyleSheets.Count)
            {
                SvgCollectedStyleSheet entry = collection.StyleSheets[index];
                if (entry != null && entry.MatchesMedia(options.StyleEnvironment, options.Limits)) inputs.Add(new SvgCascadeSheetInput(entry.StyleSheet));
                index = index + 1;
            }
        }
        System.Collections.Generic.List<SvgDiagnostic>? diagnostics = collection == null ? null : collection.Diagnostics;
        return new SvgCascade().ResolveInputs(node, inputs, options, diagnostics);
    }
SvgCascadeResult ResolveInputs(SvgNode? node, System.Collections.Generic.List<SvgCascadeSheetInput> inputs, SvgCascadeOptions? options, System.Collections.Generic.List<SvgDiagnostic>? initialDiagnostics)
    {
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        new SvgCascade().AppendDiagnostics(diagnostics, initialDiagnostics);
        if (!new SvgCascade().ValidArguments(node, inputs, options))
        {
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Cascade arguments are invalid.", node == null ? null : node.SourceSpan));
            SvgCascadeResult invalid = new SvgCascadeResult(false, new System.Collections.Generic.List<SvgCascadedValue>(), new System.Collections.Generic.List<SvgCascadeCandidate>(), diagnostics);
            invalid.SourceNode = node;
            invalid.SourceVersion = node == null ? 0 : node.Version;
            return invalid;
        }
        SvgCascadeContext context = new SvgCascadeContext((node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), options, diagnostics);
        if (context.Success && (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludeUserAgentDefaults) new SvgCascade().CollectUserAgentDefaults(context);
        new SvgCascade().RegisterLayers(context, inputs);
        int sheetIndex = 0;
        while (sheetIndex < inputs.Count && context.Success)
        {
            SvgCascadeSheetInput input = inputs[sheetIndex];
            if (input != null && input.StyleSheet != null) new SvgCascade().CollectRules(context, input.StyleSheet, input.StyleSheet.Rules);
            sheetIndex = sheetIndex + 1;
        }
        if (context.Success) new SvgCascade().CollectPresentationAttributes(context);
        if (context.Success) new SvgCascade().CollectStyleAttribute(context);
        System.Collections.Generic.List<SvgCascadedValue> values = new SvgCascade().ResolveValues(context);
        SvgCustomPropertySet rawCustomProperties = new SvgCascade().BuildRawCustomProperties(context, values);
        new SvgCascade().AnalyzeCustomProperties(context, values, rawCustomProperties);
        new SvgCascade().SubstituteOrdinaryValues(context, values, rawCustomProperties);
        SvgCustomPropertySet computedCustomProperties = new SvgCascade().BuildComputedCustomProperties(context, values);
        SvgCascadeResult result = new SvgCascadeResult(context.Success, values, context.Candidates, diagnostics);
        result.SourceNode = node;
        result.SourceVersion = node.Version;
        result.CustomProperties = computedCustomProperties;
        return result;
    }
bool ValidArguments(SvgNode? node, System.Collections.Generic.List<SvgCascadeSheetInput>? inputs, SvgCascadeOptions? options)
    {
        if (node == null || node.Kind != SvgNodeKind.Element || inputs == null || options == null) return false;
        if (options.Properties == null || options.Limits == null || options.SelectorOptions == null || options.StyleEnvironment == null) return false;
        if (options.SelectorOptions.Namespaces == null || options.SelectorOptions.DynamicState == null || options.SelectorOptions.MaxWork <= 0) return false;
        if (options.Limits.MaxDeclarations <= 0 || options.Limits.MaxSelectorTokens <= 0 || options.Limits.MaxBlockDepth <= 0 || options.Limits.MaxVariableExpansionTokens <= 0) return false;
        int index = 0;
        while (index < inputs.Count)
        {
            if (inputs[index] == null || inputs[index].StyleSheet == null) return false;
            index = index + 1;
        }
        return true;
    }
void RegisterLayers(SvgCascadeContext context, System.Collections.Generic.List<SvgCascadeSheetInput> inputs)
    {
        int sheetIndex = 0;
        while (sheetIndex < inputs.Count)
        {
            SvgStyleSheet? sheet = inputs[sheetIndex].StyleSheet;
            if ((sheet ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LayerName.Length != 0) context.RegisterLayer(sheet.Origin, sheet.LayerName);
            int layerIndex = 0;
            while (layerIndex < sheet.Layers.Count)
            {
                SvgCssLayer layer = sheet.Layers[layerIndex];
                if (layer != null) context.RegisterLayer(sheet.Origin, layer.Name);
                layerIndex = layerIndex + 1;
            }
            new SvgCascade().RegisterRuleLayers(context, sheet.Origin, sheet.Rules);
            sheetIndex = sheetIndex + 1;
        }
    }
void RegisterRuleLayers(SvgCascadeContext context, SvgStyleOrigin origin, System.Collections.Generic.List<SvgCssRule> rules)
    {
        int index = 0;
        while (index < rules.Count)
        {
            SvgCssRule rule = rules[index];
            if (rule != null)
            {
                if (rule.LayerName.Length != 0) context.RegisterLayer(origin, rule.LayerName);
                new SvgCascade().RegisterRuleLayers(context, origin, rule.ChildRules);
            }
            index = index + 1;
        }
    }
void CollectUserAgentDefaults(SvgCascadeContext context)
    {
        if (context.Node.NamespaceUri != SvgNamespaces.Svg) return;
        SvgElementKind kind = context.Node.ElementKind;
        bool nestedSvg = kind == SvgElementKind.Svg && context.Node.Parent != null && context.Node.Parent.Kind == SvgNodeKind.Element;
        if (nestedSvg || kind == SvgElementKind.Image || kind == SvgElementKind.Marker || kind == SvgElementKind.Pattern || kind == SvgElementKind.Symbol)
        {
            new SvgCascade().AddCandidate(context, SvgCssPropertyNames.OverflowX, "hidden", false, SvgStyleOrigin.UserAgent, SvgCascadeSourceKind.UserAgentDefault, "", -1, 0, 0, nestedSvg ? 1 : 0, 1, null, SvgCssPropertyNames.Overflow);
            new SvgCascade().AddCandidate(context, SvgCssPropertyNames.OverflowY, "hidden", false, SvgStyleOrigin.UserAgent, SvgCascadeSourceKind.UserAgentDefault, "", -1, 0, 0, nestedSvg ? 1 : 0, 1, null, SvgCssPropertyNames.Overflow);
        }
        if (kind != SvgElementKind.Svg || nestedSvg && (context.Node.Parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind != SvgElementKind.ForeignObject)
        {
            new SvgCascade().AddCandidate(context, SvgCssPropertyNames.TransformOrigin, "0 0", false, SvgStyleOrigin.UserAgent, SvgCascadeSourceKind.UserAgentDefault, "", -1, 0, 0, 1, kind == SvgElementKind.Svg ? 2 : 0, null, "");
        }
        if (new SvgCascade().NeverRenderedByDefault(kind))
        {
            new SvgCascade().AddCandidate(context, SvgCssPropertyNames.Display, "none", true, SvgStyleOrigin.UserAgent, SvgCascadeSourceKind.UserAgentDefault, "", -1, 0, 0, 0, 1, null, "");
        }
        ISvgDynamicSelectorState state = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SelectorOptions.DynamicState;
        SvgSelectorLinkState link = state.GetLinkState(context.Node);
        if (link == SvgSelectorLinkState.Unvisited || link == SvgSelectorLinkState.Visited)
        {
            new SvgCascade().AddCandidate(context, SvgCssPropertyNames.Cursor, "pointer", false, SvgStyleOrigin.UserAgent, SvgCascadeSourceKind.UserAgentDefault, "", -1, 0, 0, 1, 0, null, "");
        }
        else if (link < SvgSelectorLinkState.None || link > SvgSelectorLinkState.Visited)
        {
            context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidArgument, "The host returned an invalid link state while applying SVG user-agent defaults.", context.Node.SourceSpan));
        }
    }
bool NeverRenderedByDefault(SvgElementKind kind)
    {
        return kind == SvgElementKind.Defs || kind == SvgElementKind.ClipPath || kind == SvgElementKind.Mask || kind == SvgElementKind.Marker || kind == SvgElementKind.Desc || kind == SvgElementKind.Title || kind == SvgElementKind.Metadata || kind == SvgElementKind.Pattern || kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Script || kind == SvgElementKind.Style || kind == SvgElementKind.Symbol;
    }
void CollectPresentationAttributes(SvgCascadeContext context)
    {
        int index = 0;
        while (index < context.Node.Attributes.Count && context.Success)
        {
            SvgAttribute attribute = context.Node.Attributes[index];
            if (attribute != null && attribute.NamespaceUri.Length == 0)
            {
                SvgPropertySupportEntry? entry = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.FindPresentationAttribute(attribute.LocalName);
                if (entry != null)
                {
                    SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(entry.Name + ":" + attribute.Value + ";", context.Options.Limits);
                    if (parsed.Success && parsed.Declarations.Count == 1 && parsed.Declarations[0].IsValid && !parsed.Declarations[0].Important)
                    {
                        new SvgCascade().AddDeclaration(context, parsed.Declarations[0], SvgStyleOrigin.Author, SvgCascadeSourceKind.PresentationAttribute, "", -1, 0, 0, 0, 0, attribute.SourceSpan);
                    }
                    else
                    {
                        context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "An invalid presentation attribute used the property's initial value.", attribute.SourceSpan));
                        new SvgCascade().AddPresentationInitial(context, entry, attribute.SourceSpan);
                    }
                }
            }
            index = index + 1;
        }
    }
void CollectRules(SvgCascadeContext context, SvgStyleSheet sheet, System.Collections.Generic.List<SvgCssRule> rules)
    {
        int index = 0;
        while (index < rules.Count && context.Success)
        {
            SvgCssRule rule = rules[index];
            if (rule != null)
            {
                if (rule.Kind == SvgCssRuleKind.Style && rule.IsValid && rule.Selectors != null)
                {
                    SvgSelectorMatchOptions? matchOptions = new SvgCascade().MatchOptions(context, sheet.Namespaces);
                    if (matchOptions == null)
                    {
                        context.Success = false;
                        context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The cascade selector work limit was exceeded.", rule.Source));
                    }
                    else
                    {
                        SvgSelectorMatchResult match = new SvgSelectorMatcher().Match(rule.Selectors, context.Node, matchOptions);
                        context.RemainingSelectorWork = context.RemainingSelectorWork - match.Work;
                        new SvgCascade().AppendDiagnostics(context.Diagnostics, match.Diagnostics);
                        if (match.LimitExceeded)
                        {
                            context.Success = false;
                        }
                        else if (match.Matches)
                        {
                            string layerName = rule.LayerName.Length == 0 ? sheet.LayerName : rule.LayerName;
                            int fallbackOrder = rule.LayerOrder < 0 ? sheet.LayerOrder : rule.LayerOrder;
                            int declarationIndex = 0;
                            while (declarationIndex < rule.Declarations.Count && context.Success)
                            {
                                new SvgCascade().AddDeclaration(context, rule.Declarations[declarationIndex], sheet.Origin, SvgCascadeSourceKind.StyleSheet, layerName, fallbackOrder, 0, match.IdSpecificity, match.ClassSpecificity, match.TypeSpecificity, null);
                                declarationIndex = declarationIndex + 1;
                            }
                        }
                    }
                }
                if (rule.ChildRules.Count != 0 && new SvgCascade().GroupRuleMatches(context, rule)) new SvgCascade().CollectRules(context, sheet, rule.ChildRules);
            }
            index = index + 1;
        }
    }
bool GroupRuleMatches(SvgCascadeContext context, SvgCssRule rule)
    {
        if (rule.Kind != SvgCssRuleKind.AtRule || !rule.IsValid || rule.AtRule == null || !rule.AtRule.IsValid) return false;
        if (rule.AtRule.Kind == SvgCssAtRuleKind.Layer) return true;
        if (rule.AtRule.Kind == SvgCssAtRuleKind.Media)
        {
            return new SvgMediaQueryEvaluator().Matches(rule.AtRule.MediaQueries, (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StyleEnvironment);
        }
        if (rule.AtRule.Kind == SvgCssAtRuleKind.Supports)
        {
            SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
            environment.Properties = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties;
            return new SvgSupportsQueryEvaluator().Matches(rule.AtRule.SupportsQuery, environment, context.Options.Limits);
        }
        return false;
    }
SvgSelectorMatchOptions? MatchOptions(SvgCascadeContext context, SvgSelectorNamespaceBindings namespaces)
    {
        if (context.RemainingSelectorWork <= 0) return null;
        SvgSelectorMatchOptions result = new SvgSelectorMatchOptions();
        result.Scope = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SelectorOptions.Scope;
        result.Namespaces = namespaces;
        result.DynamicState = context.Options.SelectorOptions.DynamicState;
        result.MaxWork = context.RemainingSelectorWork;
        return result;
    }
void CollectStyleAttribute(SvgCascadeContext context)
    {
        int index = 0;
        while (index < context.Node.Attributes.Count && context.Success)
        {
            SvgAttribute attribute = context.Node.Attributes[index];
            if (attribute != null && attribute.NamespaceUri.Length == 0 && string.Equals(attribute.LocalName, SvgAttributeNames.Style, System.StringComparison.Ordinal))
            {
                SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(attribute.Value, (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits);
                int declarationIndex = 0;
                while (declarationIndex < parsed.Declarations.Count && context.Success)
                {
                    new SvgCascade().AddDeclaration(context, parsed.Declarations[declarationIndex], SvgStyleOrigin.Author, SvgCascadeSourceKind.StyleAttribute, "", -1, 1, 0, 0, 0, attribute.SourceSpan);
                    declarationIndex = declarationIndex + 1;
                }
                if (!parsed.Success) new SvgCascade().AddInvalidDiagnostic(context, "The style attribute exceeded a CSS parsing limit.", attribute.SourceSpan);
                return;
            }
            index = index + 1;
        }
    }
void AddDeclaration(SvgCascadeContext context, SvgCssDeclaration? declaration, SvgStyleOrigin origin, SvgCascadeSourceKind sourceKind, string layerName, int fallbackLayerOrder, int inlineSpecificity, int idSpecificity, int classSpecificity, int typeSpecificity, SvgSourceSpan? sourceOverride)
    {
        if (declaration == null || !declaration.IsValid)
        {
            new SvgCascade().AddInvalidDiagnostic(context, "An invalid CSS declaration was ignored.", declaration == null ? sourceOverride : declaration.Source);
            return;
        }
        if (declaration.IsShorthand)
        {
            int expandedIndex = 0;
            while (expandedIndex < declaration.ExpandedDeclarations.Count && context.Success)
            {
                SvgCssExpandedDeclaration expanded = declaration.ExpandedDeclarations[expandedIndex];
                if (expanded != null && expanded.IsValid) new SvgCascade().AddCandidate(context, expanded.Name, expanded.ValueText, expanded.Important, origin, sourceKind, layerName, fallbackLayerOrder, inlineSpecificity, idSpecificity, classSpecificity, typeSpecificity, sourceOverride == null ? expanded.Source : sourceOverride, expanded.ShorthandName);
                expandedIndex = expandedIndex + 1;
            }
            if (declaration.ExpandedDeclarations.Count == 0)
            {
                SvgSourceSpan? source = sourceOverride == null ? declaration.Source : sourceOverride;
                if (sourceKind == SvgCascadeSourceKind.PresentationAttribute)
                {
                    SvgPropertySupportEntry? entry = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.Find(declaration.Name);
                    context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "An invalid presentation attribute used the property's initial value.", source));
                    new SvgCascade().AddPresentationInitial(context, entry, source);
                }
                else new SvgCascade().AddInvalidDiagnostic(context, "An invalid CSS shorthand declaration was ignored.", source);
            }
        }
        else new SvgCascade().AddCandidate(context, declaration.Name, declaration.ValueText, declaration.Important, origin, sourceKind, layerName, fallbackLayerOrder, inlineSpecificity, idSpecificity, classSpecificity, typeSpecificity, sourceOverride == null ? declaration.Source : sourceOverride, "");
    }
void AddCandidate(SvgCascadeContext context, string name, string value, bool important, SvgStyleOrigin origin, SvgCascadeSourceKind sourceKind, string layerName, int fallbackLayerOrder, int inlineSpecificity, int idSpecificity, int classSpecificity, int typeSpecificity, SvgSourceSpan? source, string shorthandName)
    {
        SvgPropertySupportEntry? entry = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.Find(name);
        bool valid = sourceKind == SvgCascadeSourceKind.PresentationAttribute ? new SvgCascade().ValidPresentationDeclarationValue(context, entry, name, value) : new SvgCascade().ValidDeclarationValue(context, name, value);
        if (!valid && sourceKind == SvgCascadeSourceKind.PresentationAttribute && entry != null)
        {
            value = entry.InitialValue;
            valid = new SvgCascade().ValidDeclarationValue(context, name, value);
            context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "An invalid presentation attribute used the property's initial value.", source));
        }
        if (!valid)
        {
            new SvgCascade().AddInvalidDiagnostic(context, "An unsupported or invalid CSS declaration was ignored.", source);
            return;
        }
        if (entry != null && entry.IsLegacy)
        {
            context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.PreservedFeature, "An SVG 1.1 legacy property was preserved. Property: " + entry.Name + ".", source));
        }
        if (context.Candidates.Count >= context.Options.Limits.MaxDeclarations)
        {
            context.Success = false;
            context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The cascade declaration limit was exceeded.", source));
            return;
        }
        string normalized = name.StartsWith("--", System.StringComparison.Ordinal) ? name : name.ToLowerInvariant();
        string layerKey = layerName;
        int layerOrder = -1;
        if (layerName.Length != 0)
        {
            layerOrder = context.LayerOrder(origin, layerName);
        }
        else if (fallbackLayerOrder >= 0)
        {
            layerKey = "#anonymous-" + ((int)origin).ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + fallbackLayerOrder.ToString(System.Globalization.CultureInfo.InvariantCulture);
            layerOrder = context.RegisterLayer(origin, layerKey);
        }
        string storedValue = normalized.StartsWith("--", System.StringComparison.Ordinal) ? value : value.Trim();
        SvgCascadeCandidate candidate = new SvgCascadeCandidate(normalized, storedValue, important, origin, sourceKind, layerName, layerOrder, inlineSpecificity, idSpecificity, classSpecificity, typeSpecificity, context.NextSourceOrder, source, shorthandName, layerKey);
        context.NextSourceOrder = context.NextSourceOrder + 1;
        context.Candidates.Add(candidate);
    }
bool ValidDeclarationValue(SvgCascadeContext context, string? name, string? value)
    {
        if (name == null || name.Length == 0 || value == null) return false;
        if (name.StartsWith("--", System.StringComparison.Ordinal)) return SvgCssVariableRules.IsCustomName(name);
        if (value.Trim().Length == 0) return false;
        SvgPropertySupportEntry? entry = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.Find(name);
        if (entry == null || entry.IsShorthand) return false;
        return context.Options.Properties.Supports(name, value, context.Options.Limits, context.Capabilities);
    }
bool ValidPresentationDeclarationValue(SvgCascadeContext context, SvgPropertySupportEntry? entry, string name, string? value)
    {
        if (new SvgCascade().ValidDeclarationValue(context, name, value)) return true;
        if (entry == null || !entry.IsPresentationAttribute || value == null) return false;
        if (entry.ValueKind != SvgPropertyValueSupportKind.Length) return false;
        SvgLengthParseResult parsed = SvgLengthParser.ParseLength(value.Trim(), true, true, (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits.MaxSelectorTokens);
        return parsed.Success;
    }
void AddPresentationInitial(SvgCascadeContext context, SvgPropertySupportEntry? entry, SvgSourceSpan? source)
    {
        if (entry == null) return;
        if (!entry.IsShorthand)
        {
            new SvgCascade().AddCandidate(context, entry.Name, entry.InitialValue, false, SvgStyleOrigin.Author, SvgCascadeSourceKind.PresentationAttribute, "", -1, 0, 0, 0, 0, source, "");
            return;
        }
        int index = 0;
        while (index < entry.Longhands.Count)
        {
            SvgPropertySupportEntry? longhand = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.Find(entry.Longhands[index]);
            if (longhand != null) new SvgCascade().AddCandidate(context, longhand.Name, longhand.InitialValue, false, SvgStyleOrigin.Author, SvgCascadeSourceKind.PresentationAttribute, "", -1, 0, 0, 0, 0, source, entry.Name);
            index = index + 1;
        }
    }
void AddInvalidDiagnostic(SvgCascadeContext context, string message, SvgSourceSpan? source)
    {
        context.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidCss, message, source));
    }
System.Collections.Generic.List<SvgCascadedValue> ResolveValues(SvgCascadeContext context)
    {
        System.Collections.Generic.List<SvgCascadedValue> result = new System.Collections.Generic.List<SvgCascadedValue>();
        int entryIndex = 0;
        while (entryIndex < (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.Entries.Count)
        {
            SvgPropertySupportEntry entry = context.Options.Properties.Entries[entryIndex];
            if (entry != null && !entry.IsShorthand && new SvgCascade().FindValue(result, entry.Name) == null) result.Add(new SvgCascade().ResolveProperty(context, entry.Name, entry));
            entryIndex = entryIndex + 1;
        }
        int candidateIndex = 0;
        while (candidateIndex < context.Candidates.Count)
        {
            SvgCascadeCandidate candidate = context.Candidates[candidateIndex];
            if (candidate.PropertyName.StartsWith("--", System.StringComparison.Ordinal) && new SvgCascade().FindValue(result, candidate.PropertyName) == null) result.Add(new SvgCascade().ResolveProperty(context, candidate.PropertyName, null));
            candidateIndex = candidateIndex + 1;
        }
        if (context.Options.Parent != null)
        {
            int parentIndex = 0;
            while (parentIndex < context.Options.Parent.Values.Count)
            {
                SvgCascadedValue parent = context.Options.Parent.Values[parentIndex];
                if (parent.PropertyName.StartsWith("--", System.StringComparison.Ordinal) && new SvgCascade().FindValue(result, parent.PropertyName) == null) result.Add(new SvgCascade().ResolveProperty(context, parent.PropertyName, null));
                parentIndex = parentIndex + 1;
            }
        }
        return result;
    }
SvgCascadedValue ResolveProperty(SvgCascadeContext context, string name, SvgPropertySupportEntry? entry)
    {
        System.Collections.Generic.List<SvgCascadeExclusion> exclusions = new System.Collections.Generic.List<SvgCascadeExclusion>();
        SvgCascadeCandidate? winner = new SvgCascade().BestCandidate(context.Candidates, name, exclusions);
        while (winner != null)
        {
            string keyword = winner.ValueText.Trim().ToLowerInvariant();
            if (keyword == "revert")
            {
                exclusions.Add(new SvgCascadeExclusion(winner.Origin, winner.Important, "", false));
                winner = new SvgCascade().BestCandidate(context.Candidates, name, exclusions);
                continue;
            }
            if (keyword == "revert-layer")
            {
                exclusions.Add(new SvgCascadeExclusion(winner.Origin, winner.Important, winner.LayerKey, true));
                winner = new SvgCascade().BestCandidate(context.Candidates, name, exclusions);
                continue;
            }
            if (keyword == "initial") return new SvgCascade().InitialValue(name, entry, winner);
            if (keyword == "inherit") return new SvgCascade().InheritedValue(context, name, entry, winner);
            if (keyword == "unset")
            {
                if (entry == null || entry.Inherited) return new SvgCascade().InheritedValue(context, name, entry, winner);
                return new SvgCascade().InitialValue(name, entry, winner);
            }
            return new SvgCascadedValue(name, winner.ValueText, SvgCascadedValueKind.Declaration, winner);
        }
        if (entry == null || entry.Inherited) return new SvgCascade().InheritedValue(context, name, entry, null);
        return new SvgCascade().InitialValue(name, entry, null);
    }
SvgCascadedValue InitialValue(string name, SvgPropertySupportEntry? entry, SvgCascadeCandidate? winner)
    {
        string value = entry == null ? "" : entry.InitialValue;
        return new SvgCascadedValue(name, value, SvgCascadedValueKind.Initial, winner);
    }
SvgCascadedValue InheritedValue(SvgCascadeContext context, string name, SvgPropertySupportEntry? entry, SvgCascadeCandidate? winner)
    {
        SvgCascadedValue? parent = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parent == null ? null : (context.Options.Parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Find(name);
        if (parent != null)
        {
            SvgCascadedValue inherited = new SvgCascadedValue(name, parent.ValueText, SvgCascadedValueKind.Inherited, winner);
            inherited.AuthoredValueText = parent.AuthoredValueText;
            inherited.VariableError = parent.VariableError;
            inherited.InvalidAtComputedValue = parent.InvalidAtComputedValue;
            new SvgCascade().CopyStrings(parent.Dependencies, inherited.Dependencies);
            return inherited;
        }
        return new SvgCascade().InitialValue(name, entry, winner);
    }
SvgCustomPropertySet BuildRawCustomProperties(SvgCascadeContext context, System.Collections.Generic.List<SvgCascadedValue> values)
    {
        SvgCustomPropertySet result = new SvgCustomPropertySet();
        int index = 0;
        while (index < values.Count)
        {
            SvgCascadedValue value = values[index];
            if (value.PropertyName.StartsWith("--", System.StringComparison.Ordinal) && value.Kind != SvgCascadedValueKind.Initial && !value.InvalidAtComputedValue)
            {
                SvgCustomPropertyStoreResult stored = result.Set(value.PropertyName, value.ValueText, (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits.MaxVariableExpansionTokens, context.Options.Limits.MaxSelectorTokens);
                if (!stored.Success) new SvgCascade().MarkVariableFailure(context, value, stored.Error, "A custom property could not be stored within the configured limits.");
            }
            index = index + 1;
        }
        return result;
    }
void AnalyzeCustomProperties(SvgCascadeContext context, System.Collections.Generic.List<SvgCascadedValue> values, SvgCustomPropertySet properties)
    {
        int index = 0;
        while (index < values.Count)
        {
            SvgCascadedValue value = values[index];
            if (value.PropertyName.StartsWith("--", System.StringComparison.Ordinal) && value.Kind != SvgCascadedValueKind.Initial && !value.InvalidAtComputedValue && properties.Get(value.PropertyName) != null)
            {
                SvgCssVariableResolveResult resolved = new SvgCascade().ResolveVariables(context, properties, value.ValueText);
                new SvgCascade().CopyStrings(resolved.Dependencies, value.Dependencies);
                if (resolved.Success) value.ValueText = resolved.Text;
                else new SvgCascade().MarkVariableFailure(context, value, resolved.Error, "A custom property is invalid at computed-value time.");
            }
            index = index + 1;
        }
    }
void SubstituteOrdinaryValues(SvgCascadeContext context, System.Collections.Generic.List<SvgCascadedValue> values, SvgCustomPropertySet properties)
    {
        int index = 0;
        while (index < values.Count)
        {
            SvgCascadedValue value = values[index];
            if (!value.PropertyName.StartsWith("--", System.StringComparison.Ordinal) && value.Winner != null && new SvgCascade().ContainsVariableFunction(value.AuthoredValueText))
            {
                SvgCssVariableResolveResult resolved = new SvgCascade().ResolveVariables(context, properties, value.AuthoredValueText);
                new SvgCascade().CopyStrings(resolved.Dependencies, value.Dependencies);
                new SvgCascade().ExpandDependencyClosure(values, value.Dependencies);
                SvgPropertySupportEntry? entry = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Properties.Find(value.PropertyName);
                if (!resolved.Success)
                {
                    new SvgCascade().ApplyInvalidComputedValue(context, value, entry, resolved.Error, "A variable reference made the declaration invalid at computed-value time.");
                }
                else
                {
                    SvgCascadeTextResult substituted = new SvgCascade().ResolveShorthandLonghand(context, value, resolved.Text);
                    if (!substituted.Success || !context.Options.Properties.Supports(value.PropertyName, substituted.Text, context.Options.Limits, context.Capabilities))
                    {
                        new SvgCascade().ApplyInvalidComputedValue(context, value, entry, SvgCssVariableError.InvalidComputedValue, "Variable substitution produced an invalid property value.");
                    }
                    else value.ValueText = substituted.Text;
                }
            }
            index = index + 1;
        }
    }
SvgCssVariableResolveResult ResolveVariables(SvgCascadeContext context, SvgCustomPropertySet properties, string value)
    {
        return new SvgCssVariableResolver().Resolve(properties, value, (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits.MaxBlockDepth, context.Options.Limits.MaxVariableExpansionTokens, context.Options.Limits.MaxSelectorTokens);
    }
SvgCascadeTextResult ResolveShorthandLonghand(SvgCascadeContext context, SvgCascadedValue value, string substituted)
    {
        if ((value.Winner ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ShorthandName.Length == 0) return new SvgCascadeTextResult(true, substituted);
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(value.Winner.ShorthandName + ":" + substituted + ";", (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits);
        if (!parsed.Success || parsed.Declarations.Count != 1 || !parsed.Declarations[0].IsValid || parsed.Declarations[0].Important) return new SvgCascadeTextResult(false, "");
        int index = 0;
        while (index < parsed.Declarations[0].ExpandedDeclarations.Count)
        {
            SvgCssExpandedDeclaration expanded = parsed.Declarations[0].ExpandedDeclarations[index];
            if (expanded != null && expanded.IsValid && string.Equals(expanded.Name, value.PropertyName, System.StringComparison.Ordinal)) return new SvgCascadeTextResult(true, expanded.ValueText);
            index = index + 1;
        }
        return new SvgCascadeTextResult(false, "");
    }
void ApplyInvalidComputedValue(SvgCascadeContext context, SvgCascadedValue value, SvgPropertySupportEntry? entry, SvgCssVariableError error, string message)
    {
        value.VariableError = error;
        value.InvalidAtComputedValue = true;
        SvgCascadedValue? parent = (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parent == null ? null : (context.Options.Parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Find(value.PropertyName);
        if (entry != null && entry.Inherited && parent != null)
        {
            value.ValueText = parent.ValueText;
            value.Kind = SvgCascadedValueKind.Inherited;
            new SvgCascade().CopyStrings(parent.Dependencies, value.Dependencies);
        }
        else
        {
            value.ValueText = entry == null ? "" : entry.InitialValue;
            value.Kind = SvgCascadedValueKind.Initial;
        }
        new SvgCascade().AddVariableDiagnostic(context, value, error, message);
    }
void MarkVariableFailure(SvgCascadeContext context, SvgCascadedValue value, SvgCssVariableError error, string message)
    {
        value.ValueText = "";
        value.Kind = SvgCascadedValueKind.Initial;
        value.VariableError = error;
        value.InvalidAtComputedValue = true;
        new SvgCascade().AddVariableDiagnostic(context, value, error, message);
    }
void AddVariableDiagnostic(SvgCascadeContext context, SvgCascadedValue value, SvgCssVariableError error, string message)
    {
        bool limit = new SvgCascade().VariableLimit(error);
        SvgDiagnosticSeverity severity = limit ? SvgDiagnosticSeverity.Error : SvgDiagnosticSeverity.Warning;
        SvgDiagnosticCode code = limit ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidCss;
        SvgSourceSpan? source = value.Winner == null ? null : value.Winner.Source;
        context.Diagnostics.Add(new SvgDiagnostic(severity, code, message + " Property: " + value.PropertyName + ".", source));
        if (limit) context.Success = false;
    }
bool VariableLimit(SvgCssVariableError error)
    {
        return error == SvgCssVariableError.TokenTooLong ||
            error == SvgCssVariableError.TokenLimitExceeded ||
            error == SvgCssVariableError.DepthExceeded ||
            error == SvgCssVariableError.ExpansionLimitExceeded;
    }
SvgCustomPropertySet BuildComputedCustomProperties(SvgCascadeContext context, System.Collections.Generic.List<SvgCascadedValue> values)
    {
        SvgCustomPropertySet result = new SvgCustomPropertySet();
        int index = 0;
        while (index < values.Count)
        {
            SvgCascadedValue value = values[index];
            if (value.PropertyName.StartsWith("--", System.StringComparison.Ordinal) && value.Kind != SvgCascadedValueKind.Initial && !value.InvalidAtComputedValue)
            {
                SvgCustomPropertyStoreResult stored = result.Set(value.PropertyName, value.ValueText, (context.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Limits.MaxVariableExpansionTokens, context.Options.Limits.MaxSelectorTokens);
                if (!stored.Success) new SvgCascade().MarkVariableFailure(context, value, stored.Error, "A computed custom property could not be stored within the configured limits.");
            }
            index = index + 1;
        }
        return result;
    }
void ExpandDependencyClosure(System.Collections.Generic.List<SvgCascadedValue> values, System.Collections.Generic.List<string> dependencies)
    {
        int index = 0;
        while (index < dependencies.Count)
        {
            SvgCascadedValue? custom = new SvgCascade().FindValue(values, dependencies[index]);
            if (custom != null) new SvgCascade().CopyStrings(custom.Dependencies, dependencies);
            index = index + 1;
        }
    }
bool ContainsVariableFunction(string value)
    {
        return value != null && value.IndexOf("var(", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
void CopyStrings(System.Collections.Generic.List<string> source, System.Collections.Generic.List<string> target)
    {
        int sourceIndex = 0;
        while (sourceIndex < source.Count)
        {
            string value = source[sourceIndex];
            int targetIndex = 0;
            while (targetIndex < target.Count && !string.Equals(target[targetIndex], value, System.StringComparison.Ordinal)) targetIndex = targetIndex + 1;
            if (targetIndex == target.Count) target.Add(value);
            sourceIndex = sourceIndex + 1;
        }
    }
SvgCascadedValue? FindValue(System.Collections.Generic.List<SvgCascadedValue> values, string name)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index].PropertyName, name, System.StringComparison.Ordinal)) return values[index];
            index = index + 1;
        }
        return null;
    }
SvgCascadeCandidate? BestCandidate(System.Collections.Generic.List<SvgCascadeCandidate> candidates, string name, System.Collections.Generic.List<SvgCascadeExclusion> exclusions)
    {
        SvgCascadeCandidate? best = null;
        int index = 0;
        while (index < candidates.Count)
        {
            SvgCascadeCandidate candidate = candidates[index];
            if (string.Equals(candidate.PropertyName, name, System.StringComparison.Ordinal) && !new SvgCascade().Excluded(candidate, exclusions))
            {
                if (best == null || new SvgCascade().Compare(candidate, best) > 0) best = candidate;
            }
            index = index + 1;
        }
        return best;
    }
bool Excluded(SvgCascadeCandidate candidate, System.Collections.Generic.List<SvgCascadeExclusion> exclusions)
    {
        int index = 0;
        while (index < exclusions.Count)
        {
            SvgCascadeExclusion exclusion = exclusions[index];
            if (candidate.Origin == exclusion.Origin && candidate.Important == exclusion.Important)
            {
                if (!exclusion.LayerOnly || string.Equals(candidate.LayerKey, exclusion.LayerKey, System.StringComparison.Ordinal)) return true;
            }
            index = index + 1;
        }
        return false;
    }
int Compare(SvgCascadeCandidate left, SvgCascadeCandidate right)
    {
        int leftOrigin = new SvgCascade().OriginRank(left.Origin, left.Important);
        int rightOrigin = new SvgCascade().OriginRank(right.Origin, right.Important);
        if (leftOrigin != rightOrigin) return leftOrigin < rightOrigin ? -1 : 1;
        int leftLayer = new SvgCascade().LayerRank(left);
        int rightLayer = new SvgCascade().LayerRank(right);
        if (leftLayer != rightLayer) return leftLayer < rightLayer ? -1 : 1;
        if (left.InlineSpecificity != right.InlineSpecificity) return left.InlineSpecificity < right.InlineSpecificity ? -1 : 1;
        if (left.IdSpecificity != right.IdSpecificity) return left.IdSpecificity < right.IdSpecificity ? -1 : 1;
        if (left.ClassSpecificity != right.ClassSpecificity) return left.ClassSpecificity < right.ClassSpecificity ? -1 : 1;
        if (left.TypeSpecificity != right.TypeSpecificity) return left.TypeSpecificity < right.TypeSpecificity ? -1 : 1;
        if (left.SourceOrder == right.SourceOrder) return 0;
        return left.SourceOrder < right.SourceOrder ? -1 : 1;
    }
int OriginRank(SvgStyleOrigin origin, bool important)
    {
        if (origin == SvgStyleOrigin.Transition) return 7;
        if (important)
        {
            if (origin == SvgStyleOrigin.UserAgent) return 6;
            if (origin == SvgStyleOrigin.User) return 5;
            return 4;
        }
        if (origin == SvgStyleOrigin.Animation) return 3;
        if (origin == SvgStyleOrigin.Author) return 2;
        if (origin == SvgStyleOrigin.User) return 1;
        return 0;
    }
int LayerRank(SvgCascadeCandidate candidate)
    {
        if (!candidate.Important) return candidate.LayerOrder < 0 ? 1000000000 : candidate.LayerOrder;
        return candidate.LayerOrder < 0 ? -1000000000 : -candidate.LayerOrder;
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> target, System.Collections.Generic.List<SvgDiagnostic>? source)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Count)
        {
            if (source[index] != null) target.Add(source[index]);
            index = index + 1;
        }
    }
}
