namespace svg;

/// <summary>Resolves grouping, reference-only, link, and descriptive element behavior.</summary>
public class SvgStructuralResolver : ISvgStructuralResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgStructuralResolver()
    {
    }

    /// <summary>Resolves one supported structural SVG element with secure defaults.</summary>
    /// <param name="node">The group, definitions, symbol, view, link, or descriptive element.</param>
    /// <returns>The resolved structural semantics.</returns>
    public SvgStructuralResult ResolveWithNode(SvgNode node)
    {
        return new SvgStructuralResolver().ResolveWithNodeAndOptions(node, new SvgStructuralResolveOptions());
    }

    /// <summary>Resolves one supported structural SVG element with explicit context and limits.</summary>
    /// <param name="node">The group, definitions, symbol, view, link, or descriptive element.</param>
    /// <param name="options">The reference, style, parsing, and security context.</param>
    /// <returns>The resolved structural semantics or a located failure.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node has no supported structural role.</exception>
    public SvgStructuralResult ResolveWithNodeAndOptions(SvgNode node, SvgStructuralResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        SvgStructuralElementKind kind = new SvgStructuralResolver().GetKind(node);
        if (node.Kind != SvgNodeKind.Element || kind == SvgStructuralElementKind.None) throw new System.ArgumentOutOfRangeException("node");
        SvgStructuralResult result = new SvgStructuralResult(node);
        result.Kind = kind;
        result.Referenced = options.Referenced;
        if (!new SvgStructuralResolver().ValidOptions(options))
        {
            new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Structural resolution options are invalid.", node.SourceSpan);
            return result;
        }

        SvgElementDefinition? definition = options.ElementCatalog.Find(node.ElementKind);
        result.RenderMode = definition == null ? SvgElementRenderMode.Never : definition.RenderMode;
        result.DefinitionContainer = kind == SvgStructuralElementKind.Definitions;
        result.ReferenceOnly = kind == SvgStructuralElementKind.Symbol;
        result.Descriptive = kind == SvgStructuralElementKind.Metadata || kind == SvgStructuralElementKind.Title || kind == SvgStructuralElementKind.Description;
        bool displayed = options.AncestorRendered && new SvgStructuralResolver().IsDisplayed(node, options);
        result.Rendered = displayed && (kind == SvgStructuralElementKind.Group || kind == SvgStructuralElementKind.Link || kind == SvgStructuralElementKind.Symbol && options.Referenced);
        result.RendersChildren = result.Rendered;
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node)) result.ComputedTransform = options.ComputedStyle.GetTransformList();

        new SvgStructuralResolver().CollectChildren(node, options, result);
        new SvgStructuralResolver().ReadTransform(node, options, result);
        if (kind == SvgStructuralElementKind.Symbol || kind == SvgStructuralElementKind.View) new SvgStructuralResolver().ReadViewValues(node, options, result);
        if (kind == SvgStructuralElementKind.Symbol) new SvgStructuralResolver().ReadReferencePoint(node, options, result);
        if (kind == SvgStructuralElementKind.Link) new SvgStructuralResolver().ReadLink(node, options, result);
        if (result.Descriptive) result.TextContent = new SvgStructuralResolver().ReadText(node, options, result);
        if (result.TitleElements.Count != 0) result.TitleText = new SvgStructuralResolver().ReadText(result.TitleElements[0], options, result);
        if (result.DescriptionElements.Count != 0) result.DescriptionText = new SvgStructuralResolver().ReadText(result.DescriptionElements[0], options, result);
        result.Success = !new SvgStructuralResolver().HasError(result.Diagnostics);
        return result;
    }
bool ValidOptions(SvgStructuralResolveOptions options)
    {
        if (options.ElementCatalog == null || options.Attributes == null || options.TreeLimits == null) return false;
        if (options.TreeLimits.MaxDepth < 1 || options.TreeLimits.MaxNodes < 1 || options.TreeLimits.MaxTextCharacters < 1 || options.TreeLimits.MaxTextCharacters > int.MaxValue) return false;
        if (options.MaxRelationTokens < 1 || options.MaxTokenCharacters < 1 || options.MaxMediaTypeParameters < 0) return false;
        return true;
    }
SvgStructuralElementKind GetKind(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return SvgStructuralElementKind.None;
        switch (node.ElementKind)
        {
            case SvgElementKind.G:
                return SvgStructuralElementKind.Group;
            case SvgElementKind.Defs:
                return SvgStructuralElementKind.Definitions;
            case SvgElementKind.Symbol:
                return SvgStructuralElementKind.Symbol;
            case SvgElementKind.View:
                return SvgStructuralElementKind.View;
            case SvgElementKind.A:
                return SvgStructuralElementKind.Link;
            case SvgElementKind.Metadata:
                return SvgStructuralElementKind.Metadata;
            case SvgElementKind.Title:
                return SvgStructuralElementKind.Title;
            case SvgElementKind.Desc:
                return SvgStructuralElementKind.Description;
            default:
                return SvgStructuralElementKind.None;
        }
    }
bool IsDisplayed(SvgNode node, SvgStructuralResolveOptions options)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            string computed = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.Display);
            if (computed.Length != 0) return !string.Equals(computed, "none", System.StringComparison.OrdinalIgnoreCase);
        }
        SvgAttribute? display = new SvgStructuralResolver().FindAttribute(node, "", SvgCssPropertyNames.Display);
        if (display == null) return true;
        return !string.Equals(display.Value.Trim(), "none", System.StringComparison.OrdinalIgnoreCase);
    }
void CollectChildren(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Element)
            {
                if (result.ElementChildren.Count >= options.TreeLimits.MaxNodes)
                {
                    new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The structural element child limit was exceeded.", child.SourceSpan);
                    return;
                }
                result.ElementChildren.Add(child);
                if (child.ElementKind == SvgElementKind.Title) result.TitleElements.Add(child);
                else if (child.ElementKind == SvgElementKind.Desc) result.DescriptionElements.Add(child);
                else if (child.ElementKind == SvgElementKind.Metadata) result.MetadataElements.Add(child);
                if (result.RendersChildren && new SvgStructuralResolver().IsRenderingChild(child, options)) result.RenderingChildren.Add(child);
            }
            index = index + 1;
        }
    }
bool IsRenderingChild(SvgNode child, SvgStructuralResolveOptions options)
    {
        SvgElementDefinition? definition = options.ElementCatalog.Find(child.ElementKind);
        if (definition == null) return false;
        return definition.RenderMode == SvgElementRenderMode.Direct || definition.RenderMode == SvgElementRenderMode.Container || definition.RenderMode == SvgElementRenderMode.Text || definition.RenderMode == SvgElementRenderMode.HostCallback;
    }
void ReadTransform(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        if (new SvgStructuralResolver().FindAttribute(node, "", SvgAttributeNames.Transform) == null) return;
        SvgTypedAttributeValue value = options.Attributes.Get(node, SvgAttributeNames.Transform);
        new SvgStructuralResolver().AppendTyped(value, result);
        if (value.Success && value.HasValue && value.ValueKind == SvgTypedAttributeKind.TransformList) result.Transform = (SvgTransformList?)value.Value;
    }
void ReadViewValues(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgTypedAttributeValue viewBox = options.Attributes.Get(node, SvgAttributeNames.ViewBox);
        new SvgStructuralResolver().AppendTyped(viewBox, result);
        if (viewBox.Success && viewBox.HasValue) result.ViewBox = (SvgViewBox?)viewBox.Value;
        SvgTypedAttributeValue aspect = options.Attributes.Get(node, SvgAttributeNames.PreserveAspectRatio);
        new SvgStructuralResolver().AppendTyped(aspect, result);
        if (aspect.Success && aspect.HasValue) result.AspectRatio = (SvgAspectRatio?)aspect.Value;
    }
void ReadReferencePoint(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgTypedAttributeValue x = options.Attributes.Get(node, SvgAttributeNames.RefX);
        SvgTypedAttributeValue y = options.Attributes.Get(node, SvgAttributeNames.RefY);
        new SvgStructuralResolver().AppendTyped(x, result);
        new SvgStructuralResolver().AppendTyped(y, result);
        if (x.Success && x.HasValue && x.ValueKind == SvgTypedAttributeKind.Length) result.RefX = (SvgLength?)x.Value;
        if (y.Success && y.HasValue && y.ValueKind == SvgTypedAttributeKind.Length) result.RefY = (SvgLength?)y.Value;
    }
void ReadLink(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgAttribute? hrefAttribute = new SvgStructuralResolver().FindAttribute(node, "", SvgAttributeNames.Href);
        if (hrefAttribute != null)
        {
            SvgTypedAttributeValue href = options.Attributes.Get(node, SvgAttributeNames.Href);
            new SvgStructuralResolver().AppendTyped(href, result);
            result.HrefText = href.EffectiveText;
            if (href.Success && href.HasValue) result.Href = (SvgIriReference?)href.Value;
        }
        SvgAttribute? xlinkAttribute = new SvgStructuralResolver().FindAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
        if (xlinkAttribute != null)
        {
            SvgTypedAttributeValue href = options.Attributes.GetNamespaced(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
            new SvgStructuralResolver().AppendTyped(href, result);
            result.XLinkHrefText = href.EffectiveText;
            if (href.Success && href.HasValue) result.XLinkHref = (SvgIriReference?)href.Value;
        }
        SvgTypedAttributeValue target = options.Attributes.Get(node, SvgAttributeNames.Target);
        new SvgStructuralResolver().AppendTyped(target, result);
        if (target.Success && target.HasValue) result.Target = ((string?)target.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgAttribute? download = new SvgStructuralResolver().FindAttribute(node, "", SvgAttributeNames.Download);
        if (download != null)
        {
            result.HasDownload = true;
            result.Download = download.Value;
        }
        new SvgStructuralResolver().ReadLanguage(node, options, result);
        new SvgStructuralResolver().ReadMediaType(node, options, result);
        new SvgStructuralResolver().ReadRelations(node, options, result);
    }
void ReadLanguage(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgAttribute? attribute = new SvgStructuralResolver().FindAttribute(node, "", SvgAttributeNames.HrefLanguage);
        if (attribute == null || attribute.Value.Trim().Length == 0) return;
        SvgTextTokenListParseResult parsed = new SvgLanguageListParser().Parse(attribute.Value, 1, options.MaxTokenCharacters);
        if (parsed.Success && parsed.Items.Count == 1)
        {
            result.HrefLanguage = parsed.Items[0].Value;
            return;
        }
        new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The link hreflang value is not a valid language tag.", attribute.SourceSpan);
    }
void ReadMediaType(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgAttribute? attribute = new SvgStructuralResolver().FindAttribute(node, "", SvgAttributeNames.Type);
        if (attribute == null || attribute.Value.Trim().Length == 0) return;
        SvgMediaTypeParseResult parsed = new SvgMediaTypeParser().Parse(attribute.Value, options.MaxMediaTypeParameters, options.MaxTokenCharacters);
        if (parsed.Success)
        {
            result.MediaType = parsed.MediaType;
            return;
        }
        new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The link type value is not a valid media type.", attribute.SourceSpan);
    }
void ReadRelations(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgAttribute? attribute = new SvgStructuralResolver().FindAttribute(node, "", SvgAttributeNames.Rel);
        if (attribute == null) return;
        string value = attribute.Value;
        int position = 0;
        while (position < value.Length)
        {
            while (position < value.Length && SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            if (position == value.Length) return;
            int start = position;
            while (position < value.Length && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            int length = position - start;
            if (result.Relations.Count >= options.MaxRelationTokens)
            {
                new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The link relation-token limit was exceeded.", attribute.SourceSpan);
                return;
            }
            if (length > options.MaxTokenCharacters)
            {
                new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "A link relation token exceeded its character limit.", attribute.SourceSpan);
                return;
            }
            string source = value.Substring(start, length);
            result.Relations.Add(new SvgTextToken(source.ToLowerInvariant(), source, start, position));
        }
    }
string ReadText(SvgNode node, SvgStructuralResolveOptions options, SvgStructuralResult result)
    {
        SvgStructuralTextState state = new SvgStructuralTextState();
        new SvgStructuralResolver().AppendText(node, 0, options, state);
        if (state.LimitExceeded)
        {
            new SvgStructuralResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Descriptive text exceeded the configured tree or text limit.", node.SourceSpan);
        }
        return state.Builder.ToString();
    }
void AppendText(SvgNode node, int depth, SvgStructuralResolveOptions options, SvgStructuralTextState state)
    {
        if (state.LimitExceeded) return;
        state.Nodes = state.Nodes + 1;
        if (depth > options.TreeLimits.MaxDepth || state.Nodes > options.TreeLimits.MaxNodes)
        {
            state.LimitExceeded = true;
            return;
        }
        if (node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData)
        {
            long remaining = options.TreeLimits.MaxTextCharacters - state.Characters;
            if (node.Value.Length > remaining)
            {
                if (remaining > 0) state.Builder.Append(node.Value.Substring(0, (int)remaining));
                state.LimitExceeded = true;
                return;
            }
            state.Builder.Append(node.Value);
            state.Characters = state.Characters + node.Value.Length;
            return;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData || child.Kind == SvgNodeKind.Element) new SvgStructuralResolver().AppendText(child, depth + 1, options, state);
            if (state.LimitExceeded) return;
            index = index + 1;
        }
    }
SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }
void AppendTyped(SvgTypedAttributeValue value, SvgStructuralResult result)
    {
        int index = 0;
        while (index < value.Diagnostics.Count)
        {
            result.Diagnostics.Add(value.Diagnostics[index]);
            index = index + 1;
        }
    }
bool HasError(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
void AddError(SvgStructuralResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
