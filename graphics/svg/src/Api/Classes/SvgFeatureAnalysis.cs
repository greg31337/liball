namespace svg;

/// <summary>Analyzes the features dynamically used by an editable SVG document.</summary>
public class SvgDocumentFeatureAnalyzer : ISvgDocumentFeatureAnalyzer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgDocumentFeatureAnalyzer()
    {
    }

    /// <summary>Analyzes one document with bounded default policy and no host capabilities.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <returns>The dynamic feature analysis.</returns>
    public SvgFeatureAnalysisResult AnalyzeWithDocument(SvgDocument document)
    {
        return new SvgDocumentFeatureAnalyzer().AnalyzeWithDocumentAndOptions(document, new SvgFeatureAnalysisOptions());
    }

    /// <summary>Analyzes one document with explicit bounds, policy, and host capabilities.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <param name="options">The bounds, resource policy, and host capabilities.</param>
    /// <returns>The dynamic feature analysis.</returns>
    public SvgFeatureAnalysisResult AnalyzeWithDocumentAndOptions(SvgDocument? document, SvgFeatureAnalysisOptions options)
    {
        SvgFeatureAnalysisResult result = new SvgFeatureAnalysisResult(document);
        if (!new SvgDocumentFeatureAnalyzer().Valid(document, options)) return new SvgDocumentFeatureAnalyzer().Fail(result, SvgFeatureAnalysisError.InvalidArgument, "SVG feature-analysis arguments are invalid.", document == null ? null : document.SourceSpan);
        SvgFeatureAnalysisState state = new SvgFeatureAnalysisState(result, options);
        if (options.CancellationToken.IsCancellationRequested) return new SvgDocumentFeatureAnalyzer().Fail(result, SvgFeatureAnalysisError.Cancelled, "SVG feature analysis was cancelled.", document == null ? null : document.SourceSpan);
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.EditableDocument, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The editable SVG document model is dynamically used.", (document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        if (document.Source != null) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Loading, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The document was constructed from loaded SVG source.", document.SourceSpan);
        if (document.SourceSpan != null) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.SourceMapping, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Authored source provenance is available for this document.", document.SourceSpan);
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        int index = document.DocumentNode.Children.Count - 1;
        while (index >= 0)
        {
            pending.Add(document.DocumentNode.Children[index]);
            index = index - 1;
        }
        while (pending.Count != 0 && result.Error == SvgFeatureAnalysisError.None)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (!new SvgDocumentFeatureAnalyzer().TakeNode(state, node.SourceSpan)) break;
            if (node.Kind == SvgNodeKind.Element) new SvgDocumentFeatureAnalyzer().AnalyzeElement(node, state);
            index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        if (result.Error != SvgFeatureAnalysisError.None) return result;
        result.Success = true;
        return result;
    }
void AnalyzeElement(SvgNode node, SvgFeatureAnalysisState state)
    {
        new SvgDocumentFeatureAnalyzer().AnalyzeElementKind(node, state);
        new SvgDocumentFeatureAnalyzer().AnalyzeLegacyElement(node, state);
        int index = 0;
        while (index < node.Attributes.Count && state.Result.Error == SvgFeatureAnalysisError.None)
        {
            new SvgDocumentFeatureAnalyzer().AnalyzeAttribute(node, node.Attributes[index], state);
            index = index + 1;
        }
        if (node.ElementKind == SvgElementKind.Style) new SvgDocumentFeatureAnalyzer().AnalyzeStyleElement(node, state);
    }
void AnalyzeElementKind(SvgNode node, SvgFeatureAnalysisState state)
    {
        SvgElementKind kind = node.ElementKind;
        new SvgDocumentFeatureAnalyzer().AnalyzeGranularElement(node, state);
        if (new SvgDocumentFeatureAnalyzer().Shape(kind)) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Shapes, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "An SVG shape element is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Path) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Paths, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG path geometry is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Pattern || kind == SvgElementKind.Stop) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.PaintServers, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "An SVG paint-server feature is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Marker) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Markers, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG marker rendering is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.ClipPath) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Clipping, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG clipping is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Mask) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Masking, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG masking is dynamically used.", node.SourceSpan);
        if (new SvgDocumentFeatureAnalyzer().Filter(kind)) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Filters, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG filter effects are dynamically used.", node.SourceSpan);
        if (new SvgDocumentFeatureAnalyzer().FilterPrimitive(kind)) new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.FilterPrimitive, node.LocalName, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The individual SVG filter primitive is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Image || kind == SvgElementKind.FeImage) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Images, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG image content is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Text || kind == SvgElementKind.TSpan || kind == SvgElementKind.TextPath) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.TextLayout, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG text layout is dynamically used.", node.SourceSpan);
        if (new SvgDocumentFeatureAnalyzer().Animation(kind))
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.SmilAnimation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Declarative SMIL animation is dynamically used.", node.SourceSpan);
            new SvgDocumentFeatureAnalyzer().AnalyzeSmilValue(node, state);
        }
        if (kind == SvgElementKind.A) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Interaction, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG link interaction is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Title || kind == SvgElementKind.Desc) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Accessibility, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG accessible naming content is dynamically used.", node.SourceSpan);
        if (kind == SvgElementKind.Script)
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ScriptPreservation, SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Script content is preserved as inert authored data.", node.SourceSpan);
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ScriptExecution, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "Script execution is intentionally unsupported.", node.SourceSpan);
        }
        if (new SvgDocumentFeatureAnalyzer().HostContent(node))
        {
            string boundary = new SvgDocumentFeatureAnalyzer().HostContentName(node);
            string boundaryIdentifier = new SvgFeatureInventory().Identifier(SvgFeatureFamily.HostCallback, boundary);
            SvgFeatureStatus hostStatus = new SvgDocumentFeatureAnalyzer().HostStatusForPair(state, boundaryIdentifier, SvgFeatureIdentifiers.ForeignContent);
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ForeignContent, SvgFeatureIdentifiers.ProfileHostCallback, hostStatus, "Foreign or embedded host content requires an explicit host callback.", node.SourceSpan);
            new SvgDocumentFeatureAnalyzer().Add(state, boundaryIdentifier, SvgFeatureIdentifiers.ProfileHostCallback, hostStatus, "The embedded content kind requires an explicit host callback.", node.SourceSpan);
        }
        if (kind == SvgElementKind.Unknown && !new SvgDocumentFeatureAnalyzer().HostContent(node)) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.LegacyPreservation, SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Unknown SVG content is preserved without semantic processing.", node.SourceSpan);
    }
void AnalyzeLegacyElement(SvgNode node, SvgFeatureAnalysisState state)
    {
        SvgLegacyFeatureDefinition? definition = state.Legacy.FindElement(node.ElementKind);
        if (definition == null) return;
        new SvgDocumentFeatureAnalyzer().AddLegacy(state, definition, node.SourceSpan);
    }
void AnalyzeGranularElement(SvgNode node, SvgFeatureAnalysisState state)
    {
        if (node.NamespaceUri.Length != 0 && node.NamespaceUri != SvgNamespaces.Svg) return;
        SvgElementDefinition? current = state.Elements.Find(node.ElementKind);
        if (current != null)
        {
            SvgFeatureStatus status = SvgFeatureStatus.Implemented;
            string profile = SvgFeatureIdentifiers.ProfileSvg2;
            if (node.ElementKind == SvgElementKind.Script)
            {
                status = SvgFeatureStatus.PreserveOnly;
                profile = SvgFeatureIdentifiers.ProfileLegacyPreservation;
            }
            else if (current.RenderMode == SvgElementRenderMode.HostCallback)
            {
                string boundary = new SvgDocumentFeatureAnalyzer().HostContentName(node);
                status = new SvgDocumentFeatureAnalyzer().HostStatusForPair(state, new SvgFeatureInventory().Identifier(SvgFeatureFamily.HostCallback, boundary), SvgFeatureIdentifiers.ForeignContent);
                profile = SvgFeatureIdentifiers.ProfileHostCallback;
            }
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.Element, current.LocalName, profile, status, "The individual SVG element is dynamically used.", node.SourceSpan);
            return;
        }
        SvgLegacyFeatureDefinition? legacy = state.Legacy.FindElement(node.ElementKind);
        if (legacy != null)
        {
            SvgFeatureStatus status = legacy.Handling == SvgLegacyHandling.Implemented ? SvgFeatureStatus.Implemented : legacy.Handling == SvgLegacyHandling.Unsupported ? SvgFeatureStatus.Unsupported : SvgFeatureStatus.PreserveOnly;
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.Element, legacy.LocalName, SvgFeatureIdentifiers.ProfileLegacyPreservation, status, legacy.Detail, node.SourceSpan);
            return;
        }
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.Element, "unknown", SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "The unknown SVG element is preserved without semantic processing.", node.SourceSpan);
    }
void AnalyzeAttribute(SvgNode node, SvgAttribute attribute, SvgFeatureAnalysisState state)
    {
        SvgLegacyFeatureDefinition? legacy = state.Legacy.FindAttribute(attribute.NamespaceUri, attribute.LocalName);
        if (legacy != null) new SvgDocumentFeatureAnalyzer().AddLegacy(state, legacy, attribute.SourceSpan == null ? node.SourceSpan : attribute.SourceSpan);
        new SvgDocumentFeatureAnalyzer().AnalyzePresentationAttribute(node, attribute, state);
        if (attribute.NamespaceUri.Length == 0 && (attribute.LocalName == SvgAttributeNames.Style || attribute.LocalName == SvgAttributeNames.Class))
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssParsing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Authored CSS syntax is dynamically used.", new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute));
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssCascade, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The CSS cascade is dynamically used.", new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute));
            if (attribute.LocalName == SvgAttributeNames.Style) new SvgDocumentFeatureAnalyzer().AnalyzeInlineStyle(node, attribute, state);
        }
        if (attribute.NamespaceUri.Length == 0 && (attribute.LocalName == "role" || attribute.LocalName.StartsWith("aria-", System.StringComparison.Ordinal))) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Accessibility, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "ARIA accessibility metadata is dynamically used.", new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute));
        if (attribute.NamespaceUri.Length == 0 && attribute.LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase))
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ScriptPreservation, SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "An event-handler attribute is preserved as inert authored data.", new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute));
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ScriptExecution, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "Event-handler execution is intentionally unsupported.", new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute));
        }
        if (new SvgDocumentFeatureAnalyzer().Href(attribute)) new SvgDocumentFeatureAnalyzer().AnalyzeHref(node, attribute, state);
    }
void AnalyzePresentationAttribute(SvgNode node, SvgAttribute attribute, SvgFeatureAnalysisState state)
    {
        if (attribute.NamespaceUri.Length != 0) return;
        SvgElementDefinition? element = state.Elements.Find(node.ElementKind);
        SvgElementAttributeDefinition? definition = element == null ? null : element.FindAttribute("", attribute.LocalName);
        if (definition == null || !definition.Presentation) return;
        SvgSourceSpan? source = new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute);
        new SvgDocumentFeatureAnalyzer().AnalyzePropertyName(attribute.LocalName, source, state);
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssCascade, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A CSS presentation attribute participates in the cascade.", source);
        string name = attribute.LocalName;
        if (name == "clip-path") new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Clipping, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A clipping presentation property is dynamically used.", source);
        else if (name == "mask") new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Masking, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A masking presentation property is dynamically used.", source);
        else if (name == "filter") new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Filters, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A filter presentation property is dynamically used.", source);
        else if (name == "marker" || name == "marker-start" || name == "marker-mid" || name == "marker-end") new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Markers, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A marker presentation property is dynamically used.", source);
        else if (name == "fill" || name == "stroke") new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.PaintServers, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "An SVG paint presentation property is dynamically used.", source);
        if (attribute.Value.IndexOf("url(", System.StringComparison.OrdinalIgnoreCase) < 0) return;
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(name + ":" + attribute.Value, state.Options.CssLimits);
        if (parsed.Success && parsed.Declarations.Count != 0) new SvgDocumentFeatureAnalyzer().AnalyzeDeclaration(parsed.Declarations[0], SvgResourceKind.Image, node, source, state);
    }
void AnalyzeStyleElement(SvgNode node, SvgFeatureAnalysisState state)
    {
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssParsing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "An embedded CSS stylesheet is dynamically used.", node.SourceSpan);
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssSelectors, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "CSS selector matching is dynamically used.", node.SourceSpan);
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssCascade, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The CSS cascade is dynamically used.", node.SourceSpan);
        string css = new SvgNodeApi().GetTextContent(node);
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, state.Options.CssLimits);
        if (!parsed.Success || parsed.StyleSheet == null)
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssParsing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "The embedded stylesheet exceeds parser support or configured bounds.", node.SourceSpan);
            return;
        }
        new SvgDocumentFeatureAnalyzer().AnalyzeRules(parsed.StyleSheet.Rules, node, state);
    }
void AnalyzeInlineStyle(SvgNode node, SvgAttribute attribute, SvgFeatureAnalysisState state)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(attribute.Value, state.Options.CssLimits);
        if (!parsed.Success)
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssParsing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "The inline style exceeds parser support or configured bounds.", new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute));
            return;
        }
        int index = 0;
        while (index < parsed.Declarations.Count && state.Result.Error == SvgFeatureAnalysisError.None)
        {
            new SvgDocumentFeatureAnalyzer().AnalyzeDeclaration(parsed.Declarations[index], SvgResourceKind.Image, node, new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute), state);
            index = index + 1;
        }
    }
void AnalyzeRules(System.Collections.Generic.List<SvgCssRule> rules, SvgNode node, SvgFeatureAnalysisState state)
    {
        System.Collections.Generic.List<SvgCssRule> pending = new System.Collections.Generic.List<SvgCssRule>();
        int index = rules.Count - 1;
        while (index >= 0)
        {
            pending.Add(rules[index]);
            index = index - 1;
        }
        while (pending.Count != 0 && state.Result.Error == SvgFeatureAnalysisError.None)
        {
            SvgCssRule rule = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            SvgSourceSpan? source = node.SourceSpan;
            if (rule.AtRule != null) new SvgDocumentFeatureAnalyzer().AnalyzeAtRule(rule.AtRule, source, state);
            if (rule.Selectors != null) new SvgDocumentFeatureAnalyzer().AnalyzeSelectors(rule.Selectors, source, state);
            if (rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.Import) new SvgDocumentFeatureAnalyzer().AnalyzeUri(rule.AtRule.ImportUri, SvgResourceKind.StyleSheet, node, source, state);
            if (rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.Keyframes) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssAnimation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "CSS keyframe animation is dynamically used.", source);
            SvgResourceKind kind = rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.FontFace ? SvgResourceKind.Font : SvgResourceKind.Image;
            int declarationIndex = 0;
            while (declarationIndex < rule.Declarations.Count)
            {
                new SvgDocumentFeatureAnalyzer().AnalyzeDeclaration(rule.Declarations[declarationIndex], kind, node, source, state);
                declarationIndex = declarationIndex + 1;
            }
            if (rule.AtRule != null && rule.AtRule.Keyframes != null)
            {
                int frameIndex = 0;
                while (frameIndex < rule.AtRule.Keyframes.Keyframes.Count)
                {
                    SvgCssKeyframe frame = rule.AtRule.Keyframes.Keyframes[frameIndex];
                    declarationIndex = 0;
                    while (declarationIndex < frame.Declarations.Count)
                    {
                        new SvgDocumentFeatureAnalyzer().AnalyzeDeclaration(frame.Declarations[declarationIndex], SvgResourceKind.Image, node, source, state);
                        new SvgDocumentFeatureAnalyzer().AnalyzeAnimationDeclaration(frame.Declarations[declarationIndex], source, state);
                        declarationIndex = declarationIndex + 1;
                    }
                    frameIndex = frameIndex + 1;
                }
            }
            index = rule.ChildRules.Count - 1;
            while (index >= 0)
            {
                pending.Add(rule.ChildRules[index]);
                index = index - 1;
            }
        }
    }
void AnalyzeDeclaration(SvgCssDeclaration declaration, SvgResourceKind kind, SvgNode node, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        new SvgDocumentFeatureAnalyzer().AnalyzePropertyName(declaration.Name, source, state);
        if (declaration.Name.StartsWith("animation", System.StringComparison.OrdinalIgnoreCase)) new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.CssAnimation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "CSS animation properties are dynamically used.", source);
        System.Collections.Generic.List<string> urls = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SvgSourceSpan> sources = new System.Collections.Generic.List<SvgSourceSpan>();
        SvgSecurityValidator.CollectUrls(declaration.Values, urls, sources, 8192);
        int index = 0;
        while (index < urls.Count && state.Result.Error == SvgFeatureAnalysisError.None)
        {
            new SvgDocumentFeatureAnalyzer().AnalyzeUri(urls[index], kind, node, source, state);
            index = index + 1;
        }
    }
void AnalyzeHref(SvgNode node, SvgAttribute attribute, SvgFeatureAnalysisState state)
    {
        string value = attribute.Value.Trim();
        if (value.Length == 0 || value.StartsWith("#", System.StringComparison.Ordinal)) return;
        SvgSourceSpan? source = new SvgDocumentFeatureAnalyzer().AttributeSource(node, attribute);
        if (SvgSecurityValidator.ScriptScheme(value))
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ScriptExecution, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "A script-capable URL is intentionally unsupported.", source);
            return;
        }
        if (node.ElementKind == SvgElementKind.A)
        {
            string navigation = new SvgFeatureInventory().Identifier(SvgFeatureFamily.HostCallback, "navigation");
            SvgFeatureStatus status = state.Options.AllowExternalLinks ? new SvgDocumentFeatureAnalyzer().HostStatusForPair(state, navigation, SvgFeatureIdentifiers.Interaction) : SvgFeatureStatus.PolicyDisabled;
            string detail = status == SvgFeatureStatus.PolicyDisabled ? "External navigation is blocked by the active policy." : "External navigation requires explicit host handling.";
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.Interaction, SvgFeatureIdentifiers.ProfileHostCallback, status, detail, source);
            new SvgDocumentFeatureAnalyzer().Add(state, navigation, SvgFeatureIdentifiers.ProfileHostCallback, status, detail, source);
            return;
        }
        SvgResourceKind kind = SvgResourceKind.Document;
        if (node.ElementKind == SvgElementKind.Image || node.ElementKind == SvgElementKind.FeImage) kind = SvgResourceKind.Image;
        else if (node.ElementKind == SvgElementKind.FontFaceUri) kind = SvgResourceKind.Font;
        else if (node.ElementKind == SvgElementKind.Audio || node.ElementKind == SvgElementKind.Video || node.ElementKind == SvgElementKind.Iframe) kind = SvgResourceKind.Other;
        new SvgDocumentFeatureAnalyzer().AnalyzeUri(value, kind, node, source, state);
    }
void AnalyzeUri(string authored, SvgResourceKind kind, SvgNode node, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string value = authored.Trim();
        if (value.Length == 0 || value.StartsWith("#", System.StringComparison.Ordinal)) return;
        string baseUri = new SvgDocumentFeatureAnalyzer().BaseUri(node.OwnerDocument);
        SvgUriResult uri = baseUri.Length == 0 ? new SvgUri().NormalizeWithValue(value) : new SvgUri().ResolveWithBaseUriAndReference(baseUri, value);
        if (!uri.Success)
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ExternalResources, SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.Unsupported, "The authored external resource URI is unsupported or invalid.", source);
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.HostCallback, "external-resource", SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.Unsupported, "The authored external resource URI is unsupported or invalid.", source);
            return;
        }
        string mediaType = SvgSecurityValidator.DataMediaType(value);
        SvgResourcePolicyRequest request = new SvgResourcePolicyRequest(value, baseUri, SvgReferenceUri.RemoveFragment(uri.NormalizedIri), kind);
        request.MediaType = mediaType.Length == 0 ? SvgSecurityValidator.MediaTypeForUri(uri.NormalizedIri, kind) : mediaType;
        request.Source = source;
        SvgResourcePolicyDecision decision = state.Options.ResourcePolicy.Evaluate(request);
        if (!decision.Allowed)
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ExternalResources, SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.PolicyDisabled, "A dynamically used resource is blocked by the active resource policy.", source);
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.HostCallback, "external-resource", SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.PolicyDisabled, "External resource acquisition is blocked by the active policy.", source);
            if (kind == SvgResourceKind.Image) new SvgDocumentFeatureAnalyzer().AnalyzeImageMediaTypeWithStatus(request.MediaType, SvgFeatureStatus.PolicyDisabled, source, state);
            if (kind == SvgResourceKind.Font) new SvgDocumentFeatureAnalyzer().AnalyzeFontMediaTypeWithStatus(request.MediaType, SvgFeatureStatus.PolicyDisabled, source, state);
            return;
        }
        if (kind == SvgResourceKind.Image) new SvgDocumentFeatureAnalyzer().AnalyzeImageMediaType(request.MediaType, source, state);
        if (kind == SvgResourceKind.Font) new SvgDocumentFeatureAnalyzer().AnalyzeFontMediaType(request.MediaType, source, state);
        if (decision.IsSameDocument) return;
        if (SvgSecurityValidator.IsDataUri(value)) return;
        string boundary = new SvgFeatureInventory().Identifier(SvgFeatureFamily.HostCallback, "external-resource");
        SvgFeatureStatus status = new SvgDocumentFeatureAnalyzer().HostStatusForPair(state, boundary, SvgFeatureIdentifiers.ExternalResources);
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.ExternalResources, SvgFeatureIdentifiers.ProfileHostCallback, status, "External resource acquisition requires an explicit caller callback.", source);
        new SvgDocumentFeatureAnalyzer().Add(state, boundary, SvgFeatureIdentifiers.ProfileHostCallback, status, "External resource acquisition requires an explicit caller callback.", source);
    }
void AnalyzeImageMediaType(string mediaType, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string value = new SvgDocumentFeatureAnalyzer().MediaType(mediaType);
        if (value == SvgMimeTypes.Png)
        {
            new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.PngCodec, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The built-in PNG codec is dynamically used.", source);
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.ImageFormat, "png", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The managed PNG codec is dynamically used.", source);
            return;
        }
        if (value == SvgMimeTypes.Svg)
        {
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.ImageFormat, "svg", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Nested SVG image processing is dynamically used.", source);
            return;
        }
        if (value.Length == 0) return;
        string name = new SvgDocumentFeatureAnalyzer().ImageFormatName(value);
        string formatIdentifier = new SvgFeatureInventory().Identifier(SvgFeatureFamily.ImageFormat, name);
        string callbackIdentifier = new SvgFeatureInventory().Identifier(SvgFeatureFamily.HostCallback, "image-codec");
        SvgFeatureStatus status = new SvgDocumentFeatureAnalyzer().HostStatusForThree(state, formatIdentifier, callbackIdentifier, SvgFeatureIdentifiers.HostImageCodec);
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.ImageFormat, name, SvgFeatureIdentifiers.ProfileHostCallback, status, "This image format requires a host image decoder callback.", source);
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.HostImageCodec, SvgFeatureIdentifiers.ProfileHostCallback, status, "This image media type requires a host image decoder callback.", source);
        new SvgDocumentFeatureAnalyzer().Add(state, callbackIdentifier, SvgFeatureIdentifiers.ProfileHostCallback, status, "This image media type requires a host image decoder callback.", source);
    }
void AnalyzeImageMediaTypeWithStatus(string mediaType, SvgFeatureStatus status, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string value = new SvgDocumentFeatureAnalyzer().MediaType(mediaType);
        string name = new SvgDocumentFeatureAnalyzer().ImageFormatName(value);
        if (name.Length == 0) return;
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.ImageFormat, name, status == SvgFeatureStatus.PolicyDisabled ? SvgFeatureIdentifiers.ProfileHostCallback : SvgFeatureIdentifiers.ProfileSvg2, status, "The dynamically used image format is unavailable under the active resource policy.", source);
    }
void AnalyzeFontMediaType(string mediaType, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string name = new SvgDocumentFeatureAnalyzer().FontFormatName(new SvgDocumentFeatureAnalyzer().MediaType(mediaType));
        if (name.Length == 0) return;
        if (name != "host")
        {
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.FontFormat, name, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The managed font format is dynamically used.", source);
            return;
        }
        string callback = new SvgFeatureInventory().Identifier(SvgFeatureFamily.HostCallback, "text-shaping");
        SvgFeatureStatus status = new SvgDocumentFeatureAnalyzer().HostStatusForThree(state, new SvgFeatureInventory().Identifier(SvgFeatureFamily.FontFormat, "host"), callback, SvgFeatureIdentifiers.HostTextShaping);
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.FontFormat, "host", SvgFeatureIdentifiers.ProfileHostCallback, status, "The font format requires a host font callback.", source);
        new SvgDocumentFeatureAnalyzer().Add(state, callback, SvgFeatureIdentifiers.ProfileHostCallback, status, "The font format requires host text and font handling.", source);
    }
void AnalyzeFontMediaTypeWithStatus(string mediaType, SvgFeatureStatus status, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string name = new SvgDocumentFeatureAnalyzer().FontFormatName(new SvgDocumentFeatureAnalyzer().MediaType(mediaType));
        if (name.Length == 0) return;
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.FontFormat, name, SvgFeatureIdentifiers.ProfileHostCallback, status, "The dynamically used font format is unavailable under the active resource policy.", source);
    }
void AnalyzePropertyName(string name, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        if (name.StartsWith("--", System.StringComparison.Ordinal))
        {
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssProperty, "custom-property", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A CSS custom property is dynamically used.", source);
            return;
        }
        SvgPropertySupportEntry? entry = state.Properties.Find(name);
        if (entry == null)
        {
            new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssProperty, "unknown", SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "An unknown CSS property is preserved without computed semantics.", source);
            return;
        }
        SvgFeatureStatus status = entry.IsLegacy ? SvgFeatureStatus.PreserveOnly : SvgFeatureStatus.Implemented;
        string profile = entry.IsLegacy ? SvgFeatureIdentifiers.ProfileLegacyPreservation : SvgFeatureIdentifiers.ProfileSvg2;
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssProperty, entry.Name, profile, status, "The individual CSS property is dynamically used.", source);
    }
void AnalyzeAtRule(SvgCssAtRule rule, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string name = rule.Kind == SvgCssAtRuleKind.Unknown ? "unknown" : rule.Name;
        SvgFeatureStatus status = rule.Kind == SvgCssAtRuleKind.Unknown ? SvgFeatureStatus.PreserveOnly : SvgFeatureStatus.Implemented;
        string profile = rule.Kind == SvgCssAtRuleKind.Unknown ? SvgFeatureIdentifiers.ProfileLegacyPreservation : SvgFeatureIdentifiers.ProfileSvg2;
        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssAtRule, name, profile, status, "The individual CSS at-rule is dynamically used.", source);
    }
void AnalyzeSelectors(SvgSelector selectors, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        System.Collections.Generic.List<SvgSelector> pending = new System.Collections.Generic.List<SvgSelector>();
        pending.Add(selectors);
        while (pending.Count != 0 && state.Result.Error == SvgFeatureAnalysisError.None)
        {
            SvgSelector current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            int itemIndex = 0;
            while (itemIndex < current.Items.Count)
            {
                SvgComplexSelector complex = current.Items[itemIndex];
                int combinator = 0;
                while (combinator < complex.Combinators.Count)
                {
                    string combinatorName = new SvgDocumentFeatureAnalyzer().SelectorCombinatorName(complex.Combinators[combinator]);
                    new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssSelector, combinatorName, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A CSS selector combinator is dynamically used.", source);
                    combinator = combinator + 1;
                }
                int compound = 0;
                while (compound < complex.Compounds.Count)
                {
                    int simpleIndex = 0;
                    while (simpleIndex < complex.Compounds[compound].SimpleSelectors.Count)
                    {
                        SvgSimpleSelector simple = complex.Compounds[compound].SimpleSelectors[simpleIndex];
                        string simpleName = new SvgDocumentFeatureAnalyzer().SimpleSelectorName(simple);
                        SvgFeatureStatus status = simple.PseudoClass == SvgPseudoClassKind.UnsupportedDynamic ? SvgFeatureStatus.PreserveOnly : SvgFeatureStatus.Implemented;
                        string profile = status == SvgFeatureStatus.PreserveOnly ? SvgFeatureIdentifiers.ProfileLegacyPreservation : SvgFeatureIdentifiers.ProfileSvg2;
                        new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssSelector, simpleName, profile, status, "The individual CSS selector form is dynamically used.", source);
                        string namespaceName = new SvgDocumentFeatureAnalyzer().SelectorNamespaceName(simple);
                        if (namespaceName.Length != 0) new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssSelector, namespaceName, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A CSS selector namespace form is dynamically used.", source);
                        string attributeName = new SvgDocumentFeatureAnalyzer().SelectorAttributeName(simple);
                        if (attributeName.Length != 0) new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssSelector, attributeName, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A CSS attribute selector operator is dynamically used.", source);
                        string caseName = new SvgDocumentFeatureAnalyzer().SelectorCaseName(simple);
                        if (caseName.Length != 0) new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.CssSelector, caseName, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A CSS attribute selector case mode is dynamically used.", source);
                        if (simple.SelectorArgument != null) pending.Add(simple.SelectorArgument);
                        if (simple.Nth != null && simple.Nth.OfSelector != null) pending.Add(simple.Nth.OfSelector);
                        simpleIndex = simpleIndex + 1;
                    }
                    compound = compound + 1;
                }
                itemIndex = itemIndex + 1;
            }
        }
    }
void AnalyzeSmilValue(SvgNode node, SvgFeatureAnalysisState state)
    {
        string name = "";
        if (node.ElementKind == SvgElementKind.AnimateMotion) name = "path";
        else if (node.ElementKind == SvgElementKind.AnimateTransform) name = "transform";
        else if (node.ElementKind == SvgElementKind.AnimateColor) name = "color";
        else if (node.ElementKind == SvgElementKind.Set) name = "string";
        else if (node.ElementKind == SvgElementKind.Animate)
        {
            string attributeName = "";
            int index = 0;
            while (index < node.Attributes.Count)
            {
                if (node.Attributes[index].NamespaceUri.Length == 0 && node.Attributes[index].LocalName == "attributeName") attributeName = node.Attributes[index].Value;
                index = index + 1;
            }
            name = new SvgDocumentFeatureAnalyzer().AnimationNameForProperty(attributeName, state);
        }
        if (name.Length != 0) new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.AnimationValue, name, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A typed SMIL animation value family is dynamically used.", node.SourceSpan);
    }
void AnalyzeAnimationDeclaration(SvgCssDeclaration declaration, SvgSourceSpan? source, SvgFeatureAnalysisState state)
    {
        string name = new SvgDocumentFeatureAnalyzer().AnimationNameForProperty(declaration.Name, state);
        if (name.Length != 0) new SvgDocumentFeatureAnalyzer().AddGranular(state, SvgFeatureFamily.AnimationValue, name, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "A typed CSS animation value family is dynamically used.", source);
    }
void AddLegacy(SvgFeatureAnalysisState state, SvgLegacyFeatureDefinition definition, SvgSourceSpan? source)
    {
        SvgFeatureStatus status = SvgFeatureStatus.Implemented;
        if (definition.Handling == SvgLegacyHandling.PreserveOnly || definition.Handling == SvgLegacyHandling.Deprecated) status = SvgFeatureStatus.PreserveOnly;
        else if (definition.Handling == SvgLegacyHandling.Unsupported) status = SvgFeatureStatus.Unsupported;
        new SvgDocumentFeatureAnalyzer().Add(state, SvgFeatureIdentifiers.LegacyPreservation, SvgFeatureIdentifiers.ProfileLegacyPreservation, status, definition.Detail, source);
    }
SvgFeatureStatus HostStatusForPair(SvgFeatureAnalysisState state, string identifier, string fallback)
    {
        return state.Options.HostCapabilities.Contains(identifier) || state.Options.HostCapabilities.Contains(fallback) ? SvgFeatureStatus.Implemented : SvgFeatureStatus.CallbackRequired;
    }
SvgFeatureStatus HostStatusForThree(SvgFeatureAnalysisState state, string first, string second, string fallback)
    {
        return state.Options.HostCapabilities.Contains(first) || state.Options.HostCapabilities.Contains(second) || state.Options.HostCapabilities.Contains(fallback) ? SvgFeatureStatus.Implemented : SvgFeatureStatus.CallbackRequired;
    }
void AddGranular(SvgFeatureAnalysisState state, SvgFeatureFamily family, string name, string profile, SvgFeatureStatus status, string detail, SvgSourceSpan? source)
    {
        new SvgDocumentFeatureAnalyzer().Add(state, new SvgFeatureInventory().Identifier(family, name), profile, status, detail, source);
    }
void Add(SvgFeatureAnalysisState state, string identifier, string profile, SvgFeatureStatus status, string detail, SvgSourceSpan? source)
    {
        if (state.Result.Error != SvgFeatureAnalysisError.None) return;
        if (state.Result.FeatureReport.Entries.Count >= state.Options.MaximumEntries)
        {
            new SvgDocumentFeatureAnalyzer().Fail(state.Result, SvgFeatureAnalysisError.LimitExceeded, "The SVG feature-analysis entry limit was exceeded.", source);
            return;
        }
        SvgFeatureEntry entry = new SvgFeatureEntry(identifier, profile, status, detail, source);
        state.Result.FeatureReport.Add(entry);
        new SvgDocumentFeatureAnalyzer().AddDynamic(state.Result.DynamicallyUsedFeatures, identifier);
        if (status == SvgFeatureStatus.Implemented) state.Result.ImplementedCount = state.Result.ImplementedCount + 1;
        else if (status == SvgFeatureStatus.CallbackRequired) state.Result.CallbackRequiredCount = state.Result.CallbackRequiredCount + 1;
        else if (status == SvgFeatureStatus.PreserveOnly) state.Result.PreserveOnlyCount = state.Result.PreserveOnlyCount + 1;
        else if (status == SvgFeatureStatus.PolicyDisabled) state.Result.PolicyDisabledCount = state.Result.PolicyDisabledCount + 1;
        else state.Result.UnsupportedCount = state.Result.UnsupportedCount + 1;
        SvgDiagnostic? diagnostic = new SvgDocumentFeatureAnalyzer().Diagnostic(status, detail, source);
        if (diagnostic != null) state.Result.FeatureReport.AddDiagnostic(diagnostic);
    }
void AddDynamic(System.Collections.Generic.List<string> identifiers, string identifier)
    {
        int index = 0;
        while (index < identifiers.Count)
        {
            if (identifiers[index] == identifier) return;
            index = index + 1;
        }
        identifiers.Add(identifier);
    }
SvgDiagnostic? Diagnostic(SvgFeatureStatus status, string detail, SvgSourceSpan? source)
    {
        if (status == SvgFeatureStatus.Implemented) return null;
        SvgDiagnosticSeverity severity = status == SvgFeatureStatus.PreserveOnly ? SvgDiagnosticSeverity.Info : SvgDiagnosticSeverity.Warning;
        SvgDiagnosticCode code = status == SvgFeatureStatus.CallbackRequired ? SvgDiagnosticCode.CallbackRequired : status == SvgFeatureStatus.PreserveOnly ? SvgDiagnosticCode.PreservedFeature : status == SvgFeatureStatus.PolicyDisabled ? SvgDiagnosticCode.ResourceDenied : SvgDiagnosticCode.UnsupportedFeature;
        return new SvgDiagnostic(severity, code, detail, source);
    }
bool TakeNode(SvgFeatureAnalysisState state, SvgSourceSpan? source)
    {
        if (state.Options.CancellationToken.IsCancellationRequested)
        {
            new SvgDocumentFeatureAnalyzer().Fail(state.Result, SvgFeatureAnalysisError.Cancelled, "SVG feature analysis was cancelled.", source);
            return false;
        }
        if (state.Result.InspectedNodes >= state.Options.MaximumNodes)
        {
            new SvgDocumentFeatureAnalyzer().Fail(state.Result, SvgFeatureAnalysisError.LimitExceeded, "The SVG feature-analysis node limit was exceeded.", source);
            return false;
        }
        state.Result.InspectedNodes = state.Result.InspectedNodes + 1;
        return true;
    }
bool Shape(SvgElementKind kind)
    {
        return kind == SvgElementKind.Circle || kind == SvgElementKind.Ellipse || kind == SvgElementKind.Line || kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline || kind == SvgElementKind.Rect;
    }
bool Filter(SvgElementKind kind)
    {
        return kind >= SvgElementKind.FeBlend && kind <= SvgElementKind.Filter;
    }
bool FilterPrimitive(SvgElementKind kind)
    {
        if (kind < SvgElementKind.FeBlend || kind > SvgElementKind.FeTurbulence) return false;
        return kind != SvgElementKind.FeFuncA && kind != SvgElementKind.FeFuncB && kind != SvgElementKind.FeFuncG && kind != SvgElementKind.FeFuncR && kind != SvgElementKind.FeDistantLight && kind != SvgElementKind.FePointLight && kind != SvgElementKind.FeSpotLight && kind != SvgElementKind.FeMergeNode;
    }
bool Animation(SvgElementKind kind)
    {
        return kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Discard || kind == SvgElementKind.MPath || kind == SvgElementKind.Set || kind == SvgElementKind.AnimateColor;
    }
bool HostContent(SvgNode node)
    {
        if (node.NamespaceUri.Length != 0 && node.NamespaceUri != SvgNamespaces.Svg) return true;
        SvgElementKind kind = node.ElementKind;
        return kind == SvgElementKind.ForeignObject || kind == SvgElementKind.Audio || kind == SvgElementKind.Video || kind == SvgElementKind.Iframe || kind == SvgElementKind.Canvas;
    }
string HostContentName(SvgNode node)
    {
        if (node.ElementKind == SvgElementKind.Audio) return "audio";
        if (node.ElementKind == SvgElementKind.Video) return "video";
        if (node.ElementKind == SvgElementKind.Iframe) return "iframe";
        if (node.ElementKind == SvgElementKind.Canvas) return "canvas";
        return "foreign-content";
    }
bool Href(SvgAttribute attribute)
    {
        return attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink);
    }
SvgSourceSpan? AttributeSource(SvgNode node, SvgAttribute attribute)
    {
        return attribute.SourceSpan == null ? node.SourceSpan : attribute.SourceSpan;
    }
string BaseUri(SvgDocument? document)
    {
        if (document == null) return "";
        if (document.BaseUri.Length != 0) return document.BaseUri;
        return document.Source == null ? "" : document.Source.Uri;
    }
string MediaType(string value)
    {
        int semicolon = value.IndexOf(';');
        return (semicolon < 0 ? value : value.Substring(0, semicolon)).Trim().ToLowerInvariant();
    }
string ImageFormatName(string mediaType)
    {
        if (mediaType == SvgMimeTypes.Svg) return "svg";
        if (mediaType == SvgMimeTypes.Png) return "png";
        if (mediaType == SvgMimeTypes.Jpeg || mediaType == "image/jpg" || mediaType == "image/pjpeg") return "jpeg";
        if (mediaType == SvgMimeTypes.Gif) return "gif";
        if (mediaType == SvgMimeTypes.WebP) return "webp";
        if (mediaType == SvgMimeTypes.Avif) return "avif";
        return mediaType.Length == 0 ? "" : "host";
    }
string FontFormatName(string mediaType)
    {
        if (mediaType == SvgMimeTypes.TrueType || mediaType == "application/x-font-ttf") return "truetype";
        if (mediaType == SvgMimeTypes.OpenType || mediaType == "application/x-font-opentype") return "opentype";
        if (mediaType == SvgMimeTypes.Woff || mediaType == "application/font-woff") return "woff";
        if (mediaType == SvgMimeTypes.Woff2) return "woff2";
        if (mediaType == SvgMimeTypes.Svg || mediaType == "image/svg+xml;charset=utf-8") return "svg";
        return mediaType.Length == 0 ? "" : "host";
    }
string SelectorCombinatorName(SvgSelectorCombinator combinator)
    {
        if (combinator == SvgSelectorCombinator.Child) return "combinator-child";
        if (combinator == SvgSelectorCombinator.AdjacentSibling) return "combinator-adjacent-sibling";
        if (combinator == SvgSelectorCombinator.GeneralSibling) return "combinator-general-sibling";
        return "combinator-descendant";
    }
string SimpleSelectorName(SvgSimpleSelector selector)
    {
        if (selector.Kind == SvgSimpleSelectorKind.Type) return "type";
        if (selector.Kind == SvgSimpleSelectorKind.Universal) return "universal";
        if (selector.Kind == SvgSimpleSelectorKind.Id) return "id";
        if (selector.Kind == SvgSimpleSelectorKind.Class) return "class";
        if (selector.Kind == SvgSimpleSelectorKind.Attribute) return "attribute";
        if (selector.PseudoClass == SvgPseudoClassKind.UnsupportedDynamic) return "pseudo-unsupported-dynamic";
        return "pseudo-" + selector.PseudoName;
    }
string SelectorNamespaceName(SvgSimpleSelector selector)
    {
        if (selector.Kind != SvgSimpleSelectorKind.Type && selector.Kind != SvgSimpleSelectorKind.Universal && selector.Kind != SvgSimpleSelectorKind.Attribute) return "";
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.None) return "namespace-none";
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Any) return "namespace-any";
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Named) return "namespace-named";
        return "namespace-default";
    }
string SelectorAttributeName(SvgSimpleSelector selector)
    {
        if (selector.Kind != SvgSimpleSelectorKind.Attribute) return "";
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.Equals) return "attribute-equals";
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.Includes) return "attribute-includes";
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.DashMatch) return "attribute-dash-match";
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.PrefixMatch) return "attribute-prefix-match";
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.SuffixMatch) return "attribute-suffix-match";
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.SubstringMatch) return "attribute-substring-match";
        return "attribute-exists";
    }
string SelectorCaseName(SvgSimpleSelector selector)
    {
        if (selector.Kind != SvgSimpleSelectorKind.Attribute) return "";
        if (selector.AttributeCase == SvgAttributeSelectorCase.Insensitive) return "attribute-case-insensitive";
        if (selector.AttributeCase == SvgAttributeSelectorCase.Sensitive) return "attribute-case-sensitive";
        return "";
    }
string AnimationNameForProperty(string name, SvgFeatureAnalysisState state)
    {
        string value = name.Trim();
        if (value == "d") return "path";
        if (value == "points") return "points";
        if (value == "transform") return "transform";
        if (value == "viewBox") return "view-box";
        SvgPropertySupportEntry? property = state.Properties.Find(value);
        if (property == null) return "string";
        if (property.AnimationType == SvgPropertyAnimationType.NotAnimatable) return "";
        if (property.AnimationType == SvgPropertyAnimationType.Number) return "number";
        if (property.AnimationType == SvgPropertyAnimationType.Length) return "length";
        if (property.AnimationType == SvgPropertyAnimationType.Color) return "color";
        if (property.AnimationType == SvgPropertyAnimationType.Paint) return "paint";
        if (property.AnimationType == SvgPropertyAnimationType.Transform) return "transform";
        return "string";
    }
bool Valid(SvgDocument? document, SvgFeatureAnalysisOptions? options)
    {
        if (document == null || document.DocumentNode == null || options == null || options.CssLimits == null || options.ResourcePolicy == null || options.HostCapabilities == null || options.HostCapabilities.Identifiers == null) return false;
        if (options.MaximumNodes < 1 || options.MaximumEntries < 1) return false;
        if (options.ResourcePolicy.FileRootUri == null || options.ResourcePolicy.AllowedSchemes == null || options.ResourcePolicy.AllowedMediaTypes == null) return false;
        SvgCssLimits limits = options.CssLimits;
        return limits.MaxCharacters > 0 && limits.MaxTokens > 0 && limits.MaxRules > 0 && limits.MaxDeclarations > 0 && limits.MaxSelectorTokens > 0 && limits.MaxBlockDepth > 0 && limits.MaxImportDepth >= 0 && limits.MaxVariableExpansionTokens > 0;
    }
SvgFeatureAnalysisResult Fail(SvgFeatureAnalysisResult result, SvgFeatureAnalysisError error, string message, SvgSourceSpan? source)
    {
        result.Error = error;
        result.Success = false;
        SvgDiagnosticCode code = error == SvgFeatureAnalysisError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : error == SvgFeatureAnalysisError.Cancelled ? SvgDiagnosticCode.Cancelled : SvgDiagnosticCode.InvalidArgument;
        result.FeatureReport.AddDiagnostic(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
        return result;
    }
}
