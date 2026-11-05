namespace svg;

/// <summary>Creates independent SVG documents with secure-static executable and resource content removed.</summary>
public class SvgSanitizer : ISvgSanitizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSanitizer()
    {
    }

    /// <summary>Sanitizes one SVG document using secure-static defaults.</summary>
    /// <param name="document">The editable source document, which is not mutated.</param>
    /// <returns>The independent sanitized document and removal summary.</returns>
    public SvgSanitizationResult SanitizeWithDocument(SvgDocument document)
    {
        return new SvgSanitizer().SanitizeWithDocumentAndOptions(document, new SvgSanitizationOptions());
    }

    /// <summary>Sanitizes one SVG document using explicit bounds and resource policy.</summary>
    /// <param name="document">The editable source document, which is not mutated.</param>
    /// <param name="options">The traversal, CSS, URI, resource, and link policy.</param>
    /// <returns>The independent sanitized document and removal summary.</returns>
    public SvgSanitizationResult SanitizeWithDocumentAndOptions(SvgDocument? document, SvgSanitizationOptions options)
    {
        SvgSanitizationResult result = new SvgSanitizationResult(document);
        if (!new SvgSanitizer().Valid(document, options)) return new SvgSanitizer().Fail(result, SvgSanitizationError.InvalidArgument, "SVG sanitization arguments are invalid.", document == null ? null : document.SourceSpan);
        result.Document = new SvgSanitizer().CloneDocument((document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        SvgSanitizationState state = new SvgSanitizationState(result, options);
        new SvgSanitizer().SanitizeChildren(document.DocumentNode, result.Document.DocumentNode, state);
        if (result.Error != SvgSanitizationError.None) return result;
        new SvgSanitizer().CleanupEmpty(result.Document, state);
        if (result.Error != SvgSanitizationError.None) return result;
        new SvgSanitizer().RepairReferences(result.Document, state);
        if (result.Error != SvgSanitizationError.None) return result;
        new SvgSanitizer().CleanupEmpty(result.Document, state);
        if (result.Error != SvgSanitizationError.None) return result;
        new SvgSanitizer().RepairReferences(result.Document, state);
        if (result.Error != SvgSanitizationError.None) return result;
        new SvgSanitizer().RepairNamespaces(result.Document, state);
        if (result.Error != SvgSanitizationError.None) return result;
        result.Success = true;
        return result;
    }
void SanitizeChildren(SvgNode sourceParent, SvgNode targetParent, SvgSanitizationState state)
    {
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < sourceParent.Children.Count && state.Result.Error == SvgSanitizationError.None)
        {
            SvgNode source = sourceParent.Children[sourceIndex];
            SvgNode target = targetParent.Children[targetIndex];
            if (!new SvgSanitizer().TakeNode(source, state)) return;
            if (new SvgSanitizer().RemoveNode(source, state))
            {
                new SvgSanitizer().CountRemovedDescendants(source, state);
                if (state.Result.Error != SvgSanitizationError.None) return;
                SvgMutationResult mutation = state.Nodes.RemoveChild(targetParent, target);
                if (!new SvgSanitizer().Mutation(mutation, source.SourceSpan, state)) return;
                state.Result.RemovedNodes = state.Result.RemovedNodes + 1;
                state.Result.Changed = true;
                string reason = "Removed unsafe node '" + source.QualifiedName + "'.";
                new SvgSanitizer().Record(state, SvgSanitizationChangeKind.NodeRemoved, source.QualifiedName, "", source.Value, "", reason, source.SourceSpan);
            }
            else
            {
                if (source.Kind == SvgNodeKind.Element)
                {
                    new SvgSanitizer().SanitizeAttributes(source, target, state);
                    if (state.Result.Error != SvgSanitizationError.None) return;
                    if (source.ElementKind == SvgElementKind.Style) new SvgSanitizer().SanitizeStyleElement(source, target, state);
                    else new SvgSanitizer().SanitizeChildren(source, target, state);
                }
                else new SvgSanitizer().SanitizeChildren(source, target, state);
                targetIndex = targetIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
    }
void SanitizeAttributes(SvgNode source, SvgNode target, SvgSanitizationState state)
    {
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < source.Attributes.Count && state.Result.Error == SvgSanitizationError.None)
        {
            SvgAttribute sourceAttribute = source.Attributes[sourceIndex];
            SvgAttribute targetAttribute = target.Attributes[targetIndex];
            bool remove = !new SvgSanitizer().AllowedAttribute(sourceAttribute, state.Options.AllowList) || new SvgSanitizer().EventAttribute(sourceAttribute) || new SvgSanitizer().UnsafeHref(source, sourceAttribute, state) || new SvgSanitizer().UnsafePresentation(source, sourceAttribute, state);
            if (remove)
            {
                SvgMutationResult mutation = state.Nodes.RemoveAttribute(target, targetAttribute);
                if (!new SvgSanitizer().Mutation(mutation, sourceAttribute.SourceSpan, state)) return;
                state.Result.RemovedAttributes = state.Result.RemovedAttributes + 1;
                state.Result.Changed = true;
                string reason = "Removed unsafe attribute '" + sourceAttribute.QualifiedName + "'.";
                new SvgSanitizer().Record(state, SvgSanitizationChangeKind.AttributeRemoved, source.QualifiedName, sourceAttribute.QualifiedName, sourceAttribute.Value, "", reason, sourceAttribute.SourceSpan);
            }
            else if (sourceAttribute.NamespaceUri.Length == 0 && sourceAttribute.LocalName == SvgAttributeNames.Style)
            {
                string sanitized = new SvgSanitizer().SanitizeStyleAttribute(sourceAttribute.Value, source, sourceAttribute, state);
                if (sanitized != sourceAttribute.Value)
                {
                    if (sanitized.Length == 0)
                    {
                        SvgMutationResult mutation = state.Nodes.RemoveAttribute(target, targetAttribute);
                        if (!new SvgSanitizer().Mutation(mutation, sourceAttribute.SourceSpan, state)) return;
                        state.Result.RemovedAttributes = state.Result.RemovedAttributes + 1;
                    }
                    else
                    {
                        SvgAttribute replacement = new SvgSanitizer().ReplacementAttribute(targetAttribute, sanitized);
                        SvgMutationResult mutation = state.Nodes.ReplaceAttribute(target, targetAttribute, replacement);
                        if (!new SvgSanitizer().Mutation(mutation, sourceAttribute.SourceSpan, state)) return;
                    }
                    state.Result.Changed = true;
                    SvgSanitizationChangeKind kind = sanitized.Length == 0 ? SvgSanitizationChangeKind.AttributeRemoved : SvgSanitizationChangeKind.AttributeChanged;
                    new SvgSanitizer().Record(state, kind, source.QualifiedName, sourceAttribute.QualifiedName, sourceAttribute.Value, sanitized, "Removed unsafe inline CSS content.", sourceAttribute.SourceSpan);
                    if (sanitized.Length != 0) targetIndex = targetIndex + 1;
                }
                else targetIndex = targetIndex + 1;
            }
            else targetIndex = targetIndex + 1;
            sourceIndex = sourceIndex + 1;
        }
    }
void SanitizeStyleElement(SvgNode source, SvgNode target, SvgSanitizationState state)
    {
        string css = new SvgNodeApi().GetTextContent(source);
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, state.Options.CssLimits);
        string sanitized = "";
        bool removed = false;
        if (parsed.Success && parsed.StyleSheet != null)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int index = 0;
            while (index < parsed.StyleSheet.Rules.Count)
            {
                SvgCssRule rule = parsed.StyleSheet.Rules[index];
                if (new SvgSanitizer().SafeRule(rule, source, state)) builder.Append(rule.RawText);
                else
                {
                    removed = true;
                    state.Result.RemovedCssRules = state.Result.RemovedCssRules + 1;
                    new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssRuleRemoved, source.QualifiedName, "", rule.RawText, "", "Removed a CSS rule containing a denied resource or script URL.", rule.Source == null ? source.SourceSpan : rule.Source);
                }
                index = index + 1;
            }
            sanitized = builder.ToString();
            if (!removed) sanitized = css;
        }
        else if (css.Length != 0)
        {
            state.Result.RemovedCssRules = state.Result.RemovedCssRules + 1;
            new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssRuleRemoved, source.QualifiedName, "", css, "", "Removed a stylesheet that could not be parsed within the configured bounds.", source.SourceSpan);
        }
        if (sanitized == css) return;
        SvgMutationResult mutation = state.Nodes.SetTextContent(target, sanitized);
        if (!new SvgSanitizer().Mutation(mutation, source.SourceSpan, state)) return;
        state.Result.Changed = true;
    }
string SanitizeStyleAttribute(string css, SvgNode node, SvgAttribute attribute, SvgSanitizationState state)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(css, state.Options.CssLimits);
        if (!parsed.Success)
        {
            state.Result.RemovedCssDeclarations = state.Result.RemovedCssDeclarations + 1;
            new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssDeclarationRemoved, node.QualifiedName, attribute.QualifiedName, css, "", "Removed inline CSS that could not be parsed within the configured bounds.", attribute.SourceSpan);
            return "";
        }
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool removed = parsed.UnknownSyntax.Count != 0;
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            if (declaration.IsValid && new SvgSanitizer().SafeDeclaration(declaration, SvgResourceKind.Image, node, state))
            {
                if (result.Length != 0 && result[result.Length - 1] != ';') result.Append(';');
                result.Append(declaration.RawText);
            }
            else
            {
                removed = true;
                state.Result.RemovedCssDeclarations = state.Result.RemovedCssDeclarations + 1;
                new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssDeclarationRemoved, node.QualifiedName, attribute.QualifiedName, declaration.RawText, "", "Removed an unsafe inline CSS declaration.", declaration.Source == null ? attribute.SourceSpan : declaration.Source);
            }
            index = index + 1;
        }
        state.Result.RemovedCssDeclarations = state.Result.RemovedCssDeclarations + parsed.UnknownSyntax.Count;
        index = 0;
        while (index < parsed.UnknownSyntax.Count)
        {
            SvgCssUnknownSyntax unknown = parsed.UnknownSyntax[index];
            new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssDeclarationRemoved, node.QualifiedName, attribute.QualifiedName, unknown.RawText, "", "Removed an unrecognized inline CSS fragment.", unknown.Source == null ? attribute.SourceSpan : unknown.Source);
            index = index + 1;
        }
        return removed ? result.ToString() : css;
    }
bool SafeRule(SvgCssRule? rule, SvgNode node, SvgSanitizationState state)
    {
        if (rule == null || !rule.IsValid) return false;
        if (rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.Import)
        {
            return new SvgSanitizer().AllowedResource(rule.AtRule.ImportUri, SvgResourceKind.StyleSheet, node, state);
        }
        SvgResourceKind kind = rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.FontFace ? SvgResourceKind.Font : SvgResourceKind.Image;
        if (!new SvgSanitizer().SafeDeclarations(rule.Declarations, kind, node, state)) return false;
        if (rule.AtRule != null && rule.AtRule.Keyframes != null)
        {
            int keyframeIndex = 0;
            while (keyframeIndex < rule.AtRule.Keyframes.Keyframes.Count)
            {
                if (!new SvgSanitizer().SafeDeclarations(rule.AtRule.Keyframes.Keyframes[keyframeIndex].Declarations, SvgResourceKind.Image, node, state)) return false;
                keyframeIndex = keyframeIndex + 1;
            }
        }
        int childIndex = 0;
        while (childIndex < rule.ChildRules.Count)
        {
            if (!new SvgSanitizer().SafeRule(rule.ChildRules[childIndex], node, state)) return false;
            childIndex = childIndex + 1;
        }
        return true;
    }
bool SafeDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgResourceKind kind, SvgNode node, SvgSanitizationState state)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (!new SvgSanitizer().SafeDeclaration(declarations[index], kind, node, state)) return false;
            index = index + 1;
        }
        return true;
    }
bool SafeDeclaration(SvgCssDeclaration? declaration, SvgResourceKind kind, SvgNode node, SvgSanitizationState state)
    {
        if (declaration == null || !declaration.IsValid) return false;
        if (!new SvgSanitizer().AllowedIgnoreCase(state.Options.AllowList.CssProperties, declaration.Name)) return false;
        if (!new SvgSanitizer().AllowedFunctions(declaration.Values, state.Options.AllowList.CssFunctions)) return false;
        System.Collections.Generic.List<string> urls = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SvgSourceSpan> sources = new System.Collections.Generic.List<SvgSourceSpan>();
        SvgSecurityValidator.CollectUrls(declaration.Values, urls, sources, state.Options.References.MaxUriCharacters);
        int index = 0;
        while (index < urls.Count)
        {
            if (!new SvgSanitizer().AllowedResource(urls[index], kind, node, state)) return false;
            index = index + 1;
        }
        return true;
    }
bool UnsafeHref(SvgNode node, SvgAttribute attribute, SvgSanitizationState state)
    {
        if (!new SvgSanitizer().Href(attribute)) return false;
        string value = attribute.Value.Trim();
        if (value.Length == 0) return false;
        if (SvgSecurityValidator.ScriptScheme(value)) return true;
        if (node.ElementKind == SvgElementKind.A) return !new SvgSanitizer().AllowedLink(value, node, state);
        SvgResourceKind kind = SvgResourceKind.Document;
        if (node.ElementKind == SvgElementKind.Image || node.ElementKind == SvgElementKind.FeImage) kind = SvgResourceKind.Image;
        else if (node.ElementKind == SvgElementKind.FontFaceUri) kind = SvgResourceKind.Font;
        else if (node.ElementKind == SvgElementKind.Audio || node.ElementKind == SvgElementKind.Video || node.ElementKind == SvgElementKind.Iframe) kind = SvgResourceKind.Other;
        return !new SvgSanitizer().AllowedResource(value, kind, node, state);
    }
bool UnsafePresentation(SvgNode node, SvgAttribute attribute, SvgSanitizationState state)
    {
        if (attribute.NamespaceUri.Length != 0 || attribute.LocalName == SvgAttributeNames.Style) return false;
        SvgElementDefinition? element = state.Elements.Find(node.ElementKind);
        SvgElementAttributeDefinition? definition = element == null ? null : element.FindAttribute("", attribute.LocalName);
        if (definition == null || !definition.Presentation) return false;
        if (!new SvgSanitizer().AllowedIgnoreCase(state.Options.AllowList.CssProperties, attribute.LocalName)) return true;
        if (state.Options.AllowList.CssFunctions.Count == 0 && attribute.Value.IndexOf("url", System.StringComparison.OrdinalIgnoreCase) < 0) return false;
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(attribute.LocalName + ":" + attribute.Value, state.Options.CssLimits);
        if (!parsed.Success || parsed.Declarations.Count == 0) return true;
        return !new SvgSanitizer().SafeDeclaration(parsed.Declarations[0], SvgResourceKind.Image, node, state);
    }
bool AllowedLink(string authored, SvgNode node, SvgSanitizationState state)
    {
        if (!state.Options.AllowList.Links) return false;
        if (SvgSecurityValidator.ScriptScheme(authored) || SvgSecurityValidator.IsDataUri(authored)) return false;
        if (new SvgSanitizer().SameDocument(authored, node.OwnerDocument, state.Options.References.MaxUriCharacters)) return true;
        if (!state.Options.AllowExternalLinks) return false;
        SvgUriResult uri = new SvgSanitizer().ResolveUri(authored, node.OwnerDocument, state.Options.References.MaxUriCharacters);
        if (!uri.Success) return false;
        return new SvgSanitizer().AllowedLocation(uri.NormalizedIri, state.Options.AllowList);
    }
bool AllowedResource(string authored, SvgResourceKind kind, SvgNode? node, SvgSanitizationState state)
    {
        string value = authored.Trim();
        if (value.Length == 0 || SvgSecurityValidator.ScriptScheme(value)) return false;
        if (value.StartsWith("#", System.StringComparison.Ordinal)) return true;
        string mediaType = SvgSecurityValidator.DataMediaType(value);
        if (SvgSecurityValidator.IsDataUri(value) && (!state.Options.AllowList.DataUrls || SvgSecurityValidator.UnsafeDataType(mediaType, kind))) return false;
        SvgDocument? document = node == null ? state.Result.SourceDocument : node.OwnerDocument;
        SvgUriResult uri = new SvgSanitizer().ResolveUri(value, document, state.Options.References.MaxUriCharacters);
        if (!uri.Success) return false;
        string baseUri = new SvgSanitizer().BaseUri(document);
        SvgResourcePolicyRequest request = new SvgResourcePolicyRequest(value, baseUri, SvgReferenceUri.RemoveFragment(uri.NormalizedIri), kind);
        request.MediaType = mediaType.Length == 0 ? SvgSecurityValidator.MediaTypeForUri(uri.NormalizedIri, kind) : mediaType;
        SvgResourcePolicyDecision decision = state.Options.ResourcePolicy.Evaluate(request);
        if (!decision.Allowed) return false;
        return new SvgSanitizer().AllowedUri(decision.NormalizedUri, request.MediaType, state.Options.AllowList);
    }
bool AllowedAttribute(SvgAttribute attribute, SvgSanitizationAllowList allowList)
    {
        if (attribute.NamespaceUri == SvgNamespaces.Xmlns) return true;
        return new SvgSanitizer().AllowedOrdinal(allowList.Attributes, attribute.LocalName) && new SvgSanitizer().AllowedOrdinal(allowList.Namespaces, attribute.NamespaceUri);
    }
bool AllowedFunctions(System.Collections.Generic.List<SvgCssComponentValue> values, System.Collections.Generic.List<string> allowed)
    {
        if (allowed.Count == 0) return true;
        System.Collections.Generic.List<SvgCssComponentValue> pending = new System.Collections.Generic.List<SvgCssComponentValue>();
        int index = values.Count - 1;
        while (index >= 0)
        {
            pending.Add(values[index]);
            index = index - 1;
        }
        while (pending.Count != 0)
        {
            SvgCssComponentValue value = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (value.Kind == SvgCssComponentValueKind.Function && !new SvgSanitizer().AllowedIgnoreCase(allowed, value.Name)) return false;
            index = value.Values.Count - 1;
            while (index >= 0)
            {
                pending.Add(value.Values[index]);
                index = index - 1;
            }
        }
        return true;
    }
bool AllowedUri(string normalized, string mediaType, SvgSanitizationAllowList allowList)
    {
        if (!new SvgSanitizer().AllowedLocation(normalized, allowList)) return false;
        string type = new SvgSanitizer().MediaType(mediaType);
        return new SvgSanitizer().AllowedIgnoreCase(allowList.MediaTypes, type);
    }
bool AllowedLocation(string normalized, SvgSanitizationAllowList allowList)
    {
        string scheme = new SvgSanitizer().UriScheme(normalized);
        if (!new SvgSanitizer().AllowedIgnoreCase(allowList.UriSchemes, scheme)) return false;
        string origin = new SvgSanitizer().UriOrigin(normalized);
        return new SvgSanitizer().AllowedIgnoreCase(allowList.Origins, origin);
    }
string UriScheme(string value)
    {
        int colon = value.IndexOf(':');
        if (colon < 1) return "";
        int slash = value.IndexOf('/');
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (slash >= 0 && slash < colon || query >= 0 && query < colon || fragment >= 0 && fragment < colon) return "";
        return value.Substring(0, colon).ToLowerInvariant();
    }
string UriOrigin(string value)
    {
        string scheme = new SvgSanitizer().UriScheme(value);
        if (scheme != "http" && scheme != "https") return "";
        int delimiter = value.IndexOf("://", System.StringComparison.Ordinal);
        if (delimiter < 0) return "";
        int start = delimiter + 3;
        int end = value.Length;
        int slash = value.IndexOf('/', start);
        int query = value.IndexOf('?', start);
        int fragment = value.IndexOf('#', start);
        if (slash >= 0 && slash < end) end = slash;
        if (query >= 0 && query < end) end = query;
        if (fragment >= 0 && fragment < end) end = fragment;
        string authority = value.Substring(start, end - start);
        int at = authority.LastIndexOf('@');
        if (at >= 0) authority = authority.Substring(at + 1);
        return scheme + "://" + authority.ToLowerInvariant();
    }
string MediaType(string value)
    {
        int semicolon = value.IndexOf(';');
        return (semicolon < 0 ? value : value.Substring(0, semicolon)).Trim();
    }
bool AllowedOrdinal(System.Collections.Generic.List<string> allowed, string value)
    {
        if (allowed.Count == 0) return true;
        int index = 0;
        while (index < allowed.Count)
        {
            if (string.Equals(allowed[index], value, System.StringComparison.Ordinal)) return true;
            index = index + 1;
        }
        return false;
    }
bool AllowedIgnoreCase(System.Collections.Generic.List<string> allowed, string value)
    {
        if (allowed.Count == 0) return true;
        int index = 0;
        while (index < allowed.Count)
        {
            if (string.Equals(allowed[index], value, System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }
void CleanupEmpty(SvgDocument document, SvgSanitizationState state)
    {
        System.Collections.Generic.List<SvgNode> nodes = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<bool> expanded = new System.Collections.Generic.List<bool>();
        int index = document.DocumentNode.Children.Count - 1;
        while (index >= 0)
        {
            nodes.Add(document.DocumentNode.Children[index]);
            expanded.Add(false);
            index = index - 1;
        }
        while (nodes.Count != 0 && state.Result.Error == SvgSanitizationError.None)
        {
            int last = nodes.Count - 1;
            SvgNode node = nodes[last];
            bool visited = expanded[last];
            nodes.RemoveAt(last);
            expanded.RemoveAt(last);
            if (!visited)
            {
                nodes.Add(node);
                expanded.Add(true);
                index = node.Children.Count - 1;
                while (index >= 0)
                {
                    nodes.Add(node.Children[index]);
                    expanded.Add(false);
                    index = index - 1;
                }
            }
            else if (node.Parent != null && new SvgSanitizer().EmptyCleanupCandidate(node) && !new SvgSanitizer().MeaningfulContent(node))
            {
                SvgNode parent = node.Parent;
                SvgMutationResult mutation = state.Nodes.RemoveChild(parent, node);
                if (!new SvgSanitizer().Mutation(mutation, node.SourceSpan, state)) return;
                state.Result.RemovedNodes = state.Result.RemovedNodes + 1;
                state.Result.Changed = true;
                new SvgSanitizer().Record(state, SvgSanitizationChangeKind.NodeRemoved, node.QualifiedName, "", node.Value, "", "Removed an empty container or definition.", node.SourceSpan);
            }
        }
    }
bool EmptyCleanupCandidate(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element) return false;
        SvgElementKind kind = node.ElementKind;
        return kind == SvgElementKind.G || kind == SvgElementKind.Defs || kind == SvgElementKind.Switch || kind == SvgElementKind.Symbol || kind == SvgElementKind.ClipPath || kind == SvgElementKind.Marker || kind == SvgElementKind.Mask || kind == SvgElementKind.Pattern || kind == SvgElementKind.Filter || kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Style;
    }
bool MeaningfulContent(SvgNode node)
    {
        SvgAttribute? href = new SvgSanitizer().FindHref(node);
        if (href != null && href.Value.Trim().Length != 0) return true;
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Element) return true;
            if ((child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && child.Value.Trim().Length != 0) return true;
            index = index + 1;
        }
        return false;
    }
void RepairReferences(SvgDocument document, SvgSanitizationState state)
    {
        System.Collections.Generic.List<SvgNode> nodes = new SvgSanitizer().CollectElements(document);
        int index = 0;
        while (index < nodes.Count && state.Result.Error == SvgSanitizationError.None)
        {
            SvgNode node = nodes[index];
            if (node.OwnerDocument != null)
            {
                new SvgSanitizer().RepairReferenceAttributes(node, document, state);
                if (state.Result.Error != SvgSanitizationError.None) return;
                if (node.ElementKind == SvgElementKind.Style) new SvgSanitizer().RepairStyleReferences(node, document, state);
            }
            index = index + 1;
        }
    }
void RepairReferenceAttributes(SvgNode node, SvgDocument document, SvgSanitizationState state)
    {
        int index = 0;
        while (index < node.Attributes.Count && state.Result.Error == SvgSanitizationError.None)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (new SvgSanitizer().Href(attribute) && new SvgSanitizer().BrokenLocalReference(attribute.Value, document, state.Options.References.MaxUriCharacters))
            {
                new SvgSanitizer().RemoveReferenceAttribute(node, attribute, "Removed a reference to a missing local target.", state);
            }
            else if (new SvgSanitizer().IdReferenceAttribute(attribute))
            {
                string repaired = new SvgSanitizer().RepairIdReferences(attribute.Value, document);
                if (repaired != attribute.Value) new SvgSanitizer().ReplaceReferenceAttribute(node, attribute, repaired, "Removed missing identifiers from an ID-reference list.", state);
                else index = index + 1;
            }
            else if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Style)
            {
                string repaired = new SvgSanitizer().RepairStyleAttributeReferences(attribute.Value, node, attribute, document, state);
                if (repaired != attribute.Value) new SvgSanitizer().ReplaceReferenceAttribute(node, attribute, repaired, "Removed inline CSS declarations that referenced missing local targets.", state);
                else index = index + 1;
            }
            else if (attribute.Value.IndexOf("url(", System.StringComparison.OrdinalIgnoreCase) >= 0 && new SvgSanitizer().BrokenCssReference(attribute.LocalName + ":" + attribute.Value, document, state))
            {
                new SvgSanitizer().RemoveReferenceAttribute(node, attribute, "Removed an attribute that referenced a missing local target.", state);
            }
            else index = index + 1;
        }
    }
void RemoveReferenceAttribute(SvgNode node, SvgAttribute attribute, string reason, SvgSanitizationState state)
    {
        SvgMutationResult mutation = state.Nodes.RemoveAttribute(node, attribute);
        if (!new SvgSanitizer().Mutation(mutation, attribute.SourceSpan, state)) return;
        state.Result.RemovedAttributes = state.Result.RemovedAttributes + 1;
        state.Result.Changed = true;
        new SvgSanitizer().Record(state, SvgSanitizationChangeKind.ReferenceRepaired, node.QualifiedName, attribute.QualifiedName, attribute.Value, "", reason, attribute.SourceSpan);
    }
void ReplaceReferenceAttribute(SvgNode node, SvgAttribute attribute, string value, string reason, SvgSanitizationState state)
    {
        if (value.Length == 0)
        {
            new SvgSanitizer().RemoveReferenceAttribute(node, attribute, reason, state);
            return;
        }
        SvgAttribute replacement = new SvgSanitizer().ReplacementAttribute(attribute, value);
        SvgMutationResult mutation = state.Nodes.ReplaceAttribute(node, attribute, replacement);
        if (!new SvgSanitizer().Mutation(mutation, attribute.SourceSpan, state)) return;
        state.Result.Changed = true;
        new SvgSanitizer().Record(state, SvgSanitizationChangeKind.ReferenceRepaired, node.QualifiedName, attribute.QualifiedName, attribute.Value, value, reason, attribute.SourceSpan);
    }
bool IdReferenceAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        string name = attribute.LocalName;
        return name == "aria-labelledby" || name == "aria-describedby" || name == "aria-controls" || name == "aria-owns" || name == "aria-details" || name == "aria-errormessage";
    }
string RepairIdReferences(string value, SvgDocument document)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool removed = false;
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (start < index)
            {
                string id = value.Substring(start, index - start);
                if (document.Index.GetById(id) != null)
                {
                    if (result.Length != 0) result.Append(' ');
                    result.Append(id);
                }
                else removed = true;
            }
        }
        return removed ? result.ToString() : value;
    }
bool BrokenLocalReference(string authored, SvgDocument document, int maximum)
    {
        if (!new SvgSanitizer().SameDocument(authored, document, maximum)) return false;
        SvgUriResult resolved = new SvgSanitizer().ResolveUri(authored.Trim(), document, maximum);
        if (!resolved.Success || !resolved.HasFragment) return true;
        if (resolved.Fragment.Length == 0) return document.DocumentElement == null;
        string id = new SvgUri().DecodeFragment(resolved.Fragment);
        return id.Length == 0 || document.Index.GetById(id) == null;
    }
bool BrokenCssReference(string css, SvgDocument document, SvgSanitizationState state)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(css, state.Options.CssLimits);
        if (!parsed.Success || parsed.Declarations.Count == 0) return true;
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            if (!new SvgSanitizer().ReferencesExist(parsed.Declarations[index].Values, document, state.Options.References.MaxUriCharacters)) return true;
            index = index + 1;
        }
        return false;
    }
bool ReferencesExist(System.Collections.Generic.List<SvgCssComponentValue> values, SvgDocument document, int maximum)
    {
        System.Collections.Generic.List<string> urls = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SvgSourceSpan> sources = new System.Collections.Generic.List<SvgSourceSpan>();
        SvgSecurityValidator.CollectUrls(values, urls, sources, maximum);
        int index = 0;
        while (index < urls.Count)
        {
            if (new SvgSanitizer().BrokenLocalReference(urls[index], document, maximum)) return false;
            index = index + 1;
        }
        return true;
    }
string RepairStyleAttributeReferences(string css, SvgNode node, SvgAttribute attribute, SvgDocument document, SvgSanitizationState state)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(css, state.Options.CssLimits);
        if (!parsed.Success) return "";
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool removed = false;
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            if (declaration.IsValid && new SvgSanitizer().ReferencesExist(declaration.Values, document, state.Options.References.MaxUriCharacters))
            {
                if (result.Length != 0 && result[result.Length - 1] != ';') result.Append(';');
                result.Append(declaration.RawText);
            }
            else
            {
                removed = true;
                state.Result.RemovedCssDeclarations = state.Result.RemovedCssDeclarations + 1;
                new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssDeclarationRemoved, node.QualifiedName, attribute.QualifiedName, declaration.RawText, "", "Removed an inline CSS declaration that referenced a missing local target.", attribute.SourceSpan);
            }
            index = index + 1;
        }
        return removed ? result.ToString() : css;
    }
void RepairStyleReferences(SvgNode node, SvgDocument document, SvgSanitizationState state)
    {
        string css = state.Nodes.GetTextContent(node);
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, state.Options.CssLimits);
        if (!parsed.Success || parsed.StyleSheet == null) return;
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool removed = false;
        int index = 0;
        while (index < parsed.StyleSheet.Rules.Count)
        {
            SvgCssRule rule = parsed.StyleSheet.Rules[index];
            if (new SvgSanitizer().ReferenceRuleValid(rule, document, state.Options.References.MaxUriCharacters)) result.Append(rule.RawText);
            else
            {
                removed = true;
                state.Result.RemovedCssRules = state.Result.RemovedCssRules + 1;
                new SvgSanitizer().Record(state, SvgSanitizationChangeKind.CssRuleRemoved, node.QualifiedName, "", rule.RawText, "", "Removed a CSS rule that referenced a missing local target.", node.SourceSpan);
            }
            index = index + 1;
        }
        if (!removed) return;
        SvgMutationResult mutation = state.Nodes.SetTextContent(node, result.ToString());
        if (!new SvgSanitizer().Mutation(mutation, node.SourceSpan, state)) return;
        state.Result.Changed = true;
    }
bool ReferenceRuleValid(SvgCssRule rule, SvgDocument document, int maximum)
    {
        System.Collections.Generic.List<SvgCssRule> pending = new System.Collections.Generic.List<SvgCssRule>();
        pending.Add(rule);
        while (pending.Count != 0)
        {
            SvgCssRule current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            int index = 0;
            while (index < current.Declarations.Count)
            {
                if (!new SvgSanitizer().ReferencesExist(current.Declarations[index].Values, document, maximum)) return false;
                index = index + 1;
            }
            if (current.AtRule != null && current.AtRule.Keyframes != null)
            {
                int frameIndex = 0;
                while (frameIndex < current.AtRule.Keyframes.Keyframes.Count)
                {
                    SvgCssKeyframe frame = current.AtRule.Keyframes.Keyframes[frameIndex];
                    index = 0;
                    while (index < frame.Declarations.Count)
                    {
                        if (!new SvgSanitizer().ReferencesExist(frame.Declarations[index].Values, document, maximum)) return false;
                        index = index + 1;
                    }
                    frameIndex = frameIndex + 1;
                }
            }
            index = current.ChildRules.Count - 1;
            while (index >= 0)
            {
                pending.Add(current.ChildRules[index]);
                index = index - 1;
            }
        }
        return true;
    }
System.Collections.Generic.List<SvgNode> CollectElements(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        int index = document.DocumentNode.Children.Count - 1;
        while (index >= 0)
        {
            pending.Add(document.DocumentNode.Children[index]);
            index = index - 1;
        }
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (node.Kind == SvgNodeKind.Element) result.Add(node);
            index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
void RepairNamespaces(SvgDocument document, SvgSanitizationState state)
    {
        System.Collections.Generic.List<SvgNode> nodes = new SvgSanitizer().CollectElements(document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count && state.Result.Error == SvgSanitizationError.None)
        {
            SvgNode node = nodes[nodeIndex];
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                SvgAttribute attribute = node.Attributes[attributeIndex];
                if (attribute.NamespaceUri == SvgNamespaces.Xmlns)
                {
                    string prefix = attribute.QualifiedName == "xmlns" ? "" : attribute.LocalName;
                    if (!new SvgSanitizer().NamespaceUsed(node, prefix, attribute.Value))
                    {
                        SvgMutationResult mutation = state.Nodes.RemoveAttribute(node, attribute);
                        if (!new SvgSanitizer().Mutation(mutation, attribute.SourceSpan, state)) return;
                        state.Result.RemovedAttributes = state.Result.RemovedAttributes + 1;
                        state.Result.Changed = true;
                        new SvgSanitizer().Record(state, SvgSanitizationChangeKind.NamespaceRemoved, node.QualifiedName, attribute.QualifiedName, attribute.Value, "", "Removed an unused namespace declaration.", attribute.SourceSpan == null ? node.SourceSpan : attribute.SourceSpan);
                    }
                    else attributeIndex = attributeIndex + 1;
                }
                else attributeIndex = attributeIndex + 1;
            }
            nodeIndex = nodeIndex + 1;
        }
        nodeIndex = 0;
        while (nodeIndex < nodes.Count && state.Result.Error == SvgSanitizationError.None)
        {
            SvgNode node = nodes[nodeIndex];
            if (node.OwnerDocument != null)
            {
                if (node.NamespaceUri.Length != 0 && node.Prefix != "xml") new SvgSanitizer().EnsureNamespace(node, node.Prefix, node.NamespaceUri, state);
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Count && state.Result.Error == SvgSanitizationError.None)
                {
                    SvgAttribute attribute = node.Attributes[attributeIndex];
                    if (attribute.NamespaceUri.Length != 0 && attribute.NamespaceUri != SvgNamespaces.Xmlns && attribute.NamespaceUri != SvgNamespaces.Xml && attribute.Prefix.Length != 0)
                    {
                        new SvgSanitizer().EnsureNamespace(node, attribute.Prefix, attribute.NamespaceUri, state);
                    }
                    attributeIndex = attributeIndex + 1;
                }
            }
            nodeIndex = nodeIndex + 1;
        }
    }
bool NamespaceUsed(SvgNode owner, string prefix, string namespaceUri)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(owner);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (node.Kind == SvgNodeKind.Element)
            {
                if (node.Prefix == prefix && node.NamespaceUri == namespaceUri) return true;
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Count)
                {
                    SvgAttribute attribute = node.Attributes[attributeIndex];
                    if (attribute.NamespaceUri != SvgNamespaces.Xmlns && prefix.Length != 0 && attribute.Prefix == prefix && attribute.NamespaceUri == namespaceUri) return true;
                    attributeIndex = attributeIndex + 1;
                }
            }
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                pending.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return false;
    }
void EnsureNamespace(SvgNode node, string prefix, string namespaceUri, SvgSanitizationState state)
    {
        if (new SvgSanitizer().NamespaceBinding(node, prefix) == namespaceUri) return;
        SvgAttribute? existing = new SvgSanitizer().NamespaceDeclaration(node, prefix);
        string qualifiedName = prefix.Length == 0 ? "xmlns" : "xmlns:" + prefix;
        string localName = prefix.Length == 0 ? "xmlns" : prefix;
        SvgAttribute declaration = new SvgAttribute(qualifiedName, prefix.Length == 0 ? "" : "xmlns", localName, SvgNamespaces.Xmlns, namespaceUri);
        SvgMutationResult mutation;
        string oldValue = "";
        if (existing == null) mutation = state.Nodes.AppendAttribute(node, declaration);
        else
        {
            oldValue = existing.Value;
            mutation = state.Nodes.ReplaceAttribute(node, existing, declaration);
        }
        if (!new SvgSanitizer().Mutation(mutation, node.SourceSpan, state)) return;
        state.Result.Changed = true;
        new SvgSanitizer().Record(state, SvgSanitizationChangeKind.NamespaceAdded, node.QualifiedName, qualifiedName, oldValue, namespaceUri, "Added or corrected a required namespace declaration.", node.SourceSpan);
    }
string NamespaceBinding(SvgNode node, string prefix)
    {
        SvgNode? current = node;
        while (current != null && current.Kind != SvgNodeKind.Document)
        {
            SvgAttribute? declaration = new SvgSanitizer().NamespaceDeclaration(current, prefix);
            if (declaration != null) return declaration.Value;
            current = current.Parent;
        }
        if (prefix == "xml") return SvgNamespaces.Xml;
        return "";
    }
SvgAttribute? NamespaceDeclaration(SvgNode node, string prefix)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == SvgNamespaces.Xmlns)
            {
                if (prefix.Length == 0 && attribute.QualifiedName == "xmlns") return attribute;
                if (prefix.Length != 0 && attribute.LocalName == prefix) return attribute;
            }
            index = index + 1;
        }
        return null;
    }
SvgAttribute? FindHref(SvgNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (new SvgSanitizer().Href(node.Attributes[index])) return node.Attributes[index];
            index = index + 1;
        }
        return null;
    }
SvgAttribute ReplacementAttribute(SvgAttribute source, string value)
    {
        SvgAttribute result = new SvgAttribute(source.QualifiedName, source.Prefix, source.LocalName, source.NamespaceUri, value);
        result.RawToken = source.RawToken;
        result.SourceSpan = source.SourceSpan;
        result.SourceMapKind = source.SourceMapKind;
        return result;
    }
bool SameDocument(string authored, SvgDocument? document, int maximum)
    {
        string value = authored.Trim();
        if (value.StartsWith("#", System.StringComparison.Ordinal)) return true;
        string baseUri = new SvgSanitizer().BaseUri(document);
        if (baseUri.Length == 0) return false;
        SvgUriResult target = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, value, maximum);
        SvgUriResult current = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, "", maximum);
        return target.Success && current.Success && target.HasFragment && target.ResourceIri == current.ResourceIri;
    }
SvgUriResult ResolveUri(string authored, SvgDocument? document, int maximum)
    {
        string baseUri = new SvgSanitizer().BaseUri(document);
        return baseUri.Length == 0 ? new SvgUri().NormalizeWithValueAndMaxCharacters(authored, maximum) : new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, authored, maximum);
    }
string BaseUri(SvgDocument? document)
    {
        if (document == null) return "";
        if (document.BaseUri.Length != 0) return document.BaseUri;
        return document.Source == null ? "" : document.Source.Uri;
    }
bool RemoveNode(SvgNode node, SvgSanitizationState state)
    {
        if (node.Kind == SvgNodeKind.ProcessingInstruction && string.Equals(node.LocalName, "xml-stylesheet", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (node.Kind != SvgNodeKind.Element) return false;
        if (node.ElementKind == SvgElementKind.Script || node.ElementKind == SvgElementKind.ForeignObject) return true;
        if (node.ElementKind == SvgElementKind.Metadata && !state.Options.AllowList.Metadata) return true;
        if (!new SvgSanitizer().AllowedOrdinal(state.Options.AllowList.Elements, node.LocalName)) return true;
        if (!new SvgSanitizer().AllowedOrdinal(state.Options.AllowList.Namespaces, node.NamespaceUri)) return true;
        if (new SvgSanitizer().UnsafeAnimation(node, state)) return true;
        return node.NamespaceUri.Length != 0 && node.NamespaceUri != SvgNamespaces.Svg;
    }
bool UnsafeAnimation(SvgNode node, SvgSanitizationState state)
    {
        if (node.ElementKind != SvgElementKind.Animate && node.ElementKind != SvgElementKind.Set) return false;
        SvgAttribute? name = new SvgSanitizer().FindAttribute(node, "attributeName");
        if (name == null) return false;
        string target = name.Value.Trim();
        if (target.StartsWith("on", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (target != SvgAttributeNames.Href && target != "xlink:href") return false;
        string[] names = new string[] { "from", "to", "by", "values" };
        int nameIndex = 0;
        while (nameIndex < names.Length)
        {
            SvgAttribute? value = new SvgSanitizer().FindAttribute(node, names[nameIndex]);
            if (value != null)
            {
                int start = 0;
                int index = 0;
                while (index <= value.Value.Length)
                {
                    if (index == value.Value.Length || value.Value[index] == ';')
                    {
                        string item = value.Value.Substring(start, index - start).Trim();
                        if (item.Length != 0 && !new SvgSanitizer().AllowedResource(item, SvgResourceKind.Document, node, state)) return true;
                        start = index + 1;
                    }
                    index = index + 1;
                }
            }
            nameIndex = nameIndex + 1;
        }
        return false;
    }
bool EventAttribute(SvgAttribute attribute)
    {
        return attribute.NamespaceUri.Length == 0 && attribute.LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase);
    }
bool Href(SvgAttribute attribute)
    {
        return attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink);
    }
SvgAttribute? FindAttribute(SvgNode node, string name)
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
bool TakeNode(SvgNode? node, SvgSanitizationState state)
    {
        if (state.VisitedNodes >= state.Options.MaximumNodes)
        {
            new SvgSanitizer().Fail(state.Result, SvgSanitizationError.LimitExceeded, "The SVG sanitization node limit was exceeded.", node == null ? null : node.SourceSpan);
            return false;
        }
        state.VisitedNodes = state.VisitedNodes + 1;
        return true;
    }
void CountRemovedDescendants(SvgNode node, SvgSanitizationState state)
    {
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        int index = node.Children.Count - 1;
        while (index >= 0)
        {
            stack.Add(node.Children[index]);
            index = index - 1;
        }
        while (stack.Count != 0 && state.Result.Error == SvgSanitizationError.None)
        {
            SvgNode current = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (!new SvgSanitizer().TakeNode(current, state)) return;
            index = current.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(current.Children[index]);
                index = index - 1;
            }
        }
    }
bool Mutation(SvgMutationResult mutation, SvgSourceSpan? source, SvgSanitizationState state)
    {
        if (mutation.Success) return true;
        new SvgSanitizer().Fail(state.Result, SvgSanitizationError.MutationFailed, mutation.Message, source);
        return false;
    }
void Add(SvgSanitizationState state, string message, SvgSourceSpan? source)
    {
        if (state.Result.Diagnostics.Count >= state.Options.MaximumDiagnostics) return;
        state.Result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.LossyChange, message, source));
    }
void Record(SvgSanitizationState state, SvgSanitizationChangeKind kind, string nodeName, string attributeName, string oldValue, string newValue, string reason, SvgSourceSpan? source)
    {
        state.Result.Changes.Add(new SvgSanitizationChange(kind, nodeName, attributeName, oldValue, newValue, reason, source));
        new SvgSanitizer().Add(state, reason, source);
    }
SvgDocument CloneDocument(SvgDocument source)
    {
        SvgDocument result = new SvgDocument();
        result.BaseUri = source.BaseUri;
        result.ContentType = source.ContentType;
        result.ProcessingMode = SvgProcessingMode.SecureStatic;
        result.IsFragment = source.IsFragment;
        result.Version = source.Version;
        result.IsDirty = false;
        SvgNodeApi nodes = new SvgNodeApi();
        int index = 0;
        while (index < source.DocumentNode.Children.Count)
        {
            SvgNode sourceChild = source.DocumentNode.Children[index];
            SvgNode clone = nodes.CloneNode(sourceChild, true);
            new SvgSanitizer().CopySourceMetadata(sourceChild, clone);
            new SvgSanitizer().AssignOwner(clone, result, result.Version);
            clone.Parent = result.DocumentNode;
            result.DocumentNode.Children.Add(clone);
            if (clone.Kind == SvgNodeKind.Element && result.DocumentElement == null) result.DocumentElement = clone;
            index = index + 1;
        }
        result.Index.Invalidate();
        return result;
    }
void CopySourceMetadata(SvgNode source, SvgNode target)
    {
        System.Collections.Generic.List<SvgNode> sources = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> targets = new System.Collections.Generic.List<SvgNode>();
        sources.Add(source);
        targets.Add(target);
        while (sources.Count != 0)
        {
            int last = sources.Count - 1;
            SvgNode sourceNode = sources[last];
            SvgNode targetNode = targets[last];
            sources.RemoveAt(last);
            targets.RemoveAt(last);
            targetNode.RawToken = sourceNode.RawToken;
            targetNode.EndToken = sourceNode.EndToken;
            targetNode.SourceSpan = sourceNode.SourceSpan;
            targetNode.SourceMapKind = sourceNode.SourceMapKind;
            int attributeIndex = 0;
            while (attributeIndex < sourceNode.Attributes.Count)
            {
                SvgAttribute sourceAttribute = sourceNode.Attributes[attributeIndex];
                SvgAttribute targetAttribute = targetNode.Attributes[attributeIndex];
                targetAttribute.RawToken = sourceAttribute.RawToken;
                targetAttribute.SourceSpan = sourceAttribute.SourceSpan;
                targetAttribute.SourceMapKind = sourceAttribute.SourceMapKind;
                attributeIndex = attributeIndex + 1;
            }
            int childIndex = sourceNode.Children.Count - 1;
            while (childIndex >= 0)
            {
                sources.Add(sourceNode.Children[childIndex]);
                targets.Add(targetNode.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
    }
void AssignOwner(SvgNode node, SvgDocument document, long version)
    {
        node.OwnerDocument = document;
        node.Version = version;
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            node.Attributes[attributeIndex].OwnerElement = node;
            node.Attributes[attributeIndex].OwnerDocument = document;
            node.Attributes[attributeIndex].Version = version;
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            node.Children[childIndex].Parent = node;
            new SvgSanitizer().AssignOwner(node.Children[childIndex], document, version);
            childIndex = childIndex + 1;
        }
    }
bool Valid(SvgDocument? document, SvgSanitizationOptions? options)
    {
        if (document == null || document.DocumentNode == null || options == null || options.CssLimits == null || options.References == null || options.ResourcePolicy == null || options.AllowList == null) return false;
        if (options.MaximumNodes < 1 || options.MaximumDiagnostics < 1 || options.References.MaxUriCharacters < 1 || options.References.MaxBaseDepth < 1) return false;
        if (options.ResourcePolicy.FileRootUri == null || options.ResourcePolicy.AllowedSchemes == null || options.ResourcePolicy.AllowedMediaTypes == null) return false;
        SvgSanitizationAllowList allowList = options.AllowList;
        if (!new SvgSanitizer().ValidStrings(allowList.Elements) || !new SvgSanitizer().ValidStrings(allowList.Attributes) || !new SvgSanitizer().ValidStrings(allowList.Namespaces) || !new SvgSanitizer().ValidStrings(allowList.CssProperties) || !new SvgSanitizer().ValidStrings(allowList.CssFunctions) || !new SvgSanitizer().ValidStrings(allowList.UriSchemes) || !new SvgSanitizer().ValidStrings(allowList.Origins) || !new SvgSanitizer().ValidStrings(allowList.MediaTypes)) return false;
        SvgCssLimits limits = options.CssLimits;
        return limits.MaxCharacters > 0 && limits.MaxTokens > 0 && limits.MaxRules > 0 && limits.MaxDeclarations > 0 && limits.MaxSelectorTokens > 0 && limits.MaxBlockDepth > 0 && limits.MaxImportDepth >= 0 && limits.MaxVariableExpansionTokens > 0;
    }
bool ValidStrings(System.Collections.Generic.List<string>? values)
    {
        if (values == null) return false;
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null) return false;
            index = index + 1;
        }
        return true;
    }
SvgSanitizationResult Fail(SvgSanitizationResult result, SvgSanitizationError error, string message, SvgSourceSpan? source)
    {
        result.Error = error;
        result.Success = false;
        SvgDiagnosticCode code = error == SvgSanitizationError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : error == SvgSanitizationError.InvalidArgument ? SvgDiagnosticCode.InvalidArgument : SvgDiagnosticCode.InternalInvariant;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
        return result;
    }
}
