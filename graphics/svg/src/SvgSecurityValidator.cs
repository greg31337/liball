namespace svg;

internal static class SvgSecurityValidator
{
    internal static void Validate(SvgValidationWork work)
    {
        System.Collections.Generic.List<SvgNode> nodes = work.ReferenceNodes;
        if (nodes.Count == 0) nodes = CollectNodes((work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement);
        SvgSecurityValidationState state = new SvgSecurityValidationState(work, nodes);
        int index = 0;
        while (index < nodes.Count && work.Result.Error == SvgValidationError.None)
        {
            ValidateNode(nodes[index], state);
            index = index + 1;
        }
        if (work.Result.Error != SvgValidationError.None) return;
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits((work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), new System.Collections.Generic.List<SvgStyleSheet>(), (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Limits);
        ValidateStyleSheets(collection, state);
    }

    static void ValidateNode(SvgNode node, SvgSecurityValidationState state)
    {
        if (node.ElementKind == SvgElementKind.Script)
        {
            SvgAttribute? href = FindHref(node);
            string message = href == null ? "The script element retains inline executable content." : "The script element retains an external executable reference.";
            Add(state, SvgValidationRule.ScriptContent, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.PreservedFeature, message, node, href, href == null ? node.SourceSpan : href.SourceSpan, "Preserve script as inert metadata and do not execute it.");
        }
        if (node.ElementKind == SvgElementKind.ForeignObject)
        {
            Add(state, SvgValidationRule.ForeignContent, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CallbackRequired, "The foreignObject element requires host-managed foreign-content processing.", node, null, node.SourceSpan, "Preserve the foreign content without invoking a host renderer.");
        }
        else if (node.NamespaceUri.Length != 0 && node.NamespaceUri != SvgNamespaces.Svg && !HasForeignParent(node))
        {
            Add(state, SvgValidationRule.ForeignContent, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CallbackRequired, "A foreign-namespace subtree requires host-managed processing.", node, null, node.SourceSpan, "Preserve the foreign subtree without invoking host code.");
        }
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count && state.Work.Result.Error == SvgValidationError.None)
        {
            SvgAttribute attribute = node.Attributes[attributeIndex];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase))
            {
                Add(state, SvgValidationRule.EventHandler, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.PreservedFeature, "The event-handler attribute '" + attribute.LocalName + "' retains executable content.", node, attribute, attribute.SourceSpan, "Preserve the handler as inert metadata and do not execute it.");
            }
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Style) ValidateInlineStyle(node, attribute, state);
            else CountPresentation(node, attribute, state);
            attributeIndex = attributeIndex + 1;
        }
        SvgAttribute? hrefAttribute = FindHref(node);
        if (hrefAttribute != null) ValidateHref(node, hrefAttribute, state);
    }

    static void ValidateHref(SvgNode node, SvgAttribute attribute, SvgSecurityValidationState state)
    {
        string value = attribute.Value.Trim();
        if (value.Length == 0) return;
        if (ScriptScheme(value))
        {
            Add(state, SvgValidationRule.ScriptUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "The URI uses a script-capable scheme.", node, attribute, attribute.SourceSpan, "Ignore the script URL and retain no navigation or resource target.");
            return;
        }
        string dataMediaType = DataMediaType(value);
        if (node.ElementKind == SvgElementKind.A)
        {
            if (dataMediaType.Length != 0 || IsDataUri(value))
            {
                Add(state, SvgValidationRule.UnsafeDataUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "A link uses a data URL that can create an active navigation context.", node, attribute, attribute.SourceSpan, "Ignore the data link and require an explicit host-approved navigation target.");
                return;
            }
            SvgReferenceResult link = ResolveNodeReference(node, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References);
            if (link != null && link.Status == SvgReferenceStatus.External)
            {
                Add(state, SvgValidationRule.ExternalLink, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CallbackRequired, "The external link requires caller-controlled navigation.", node, attribute, attribute.SourceSpan, "Return the link target to the host without performing navigation.");
            }
            return;
        }
        if (!IsResourceElement(node.ElementKind)) return;
        SvgResourceKind kind = SvgResourceKind.Other;
        SvgValidationRule rule = SvgValidationRule.ExternalImage;
        if (node.ElementKind == SvgElementKind.FontFaceUri)
        {
            kind = SvgResourceKind.Font;
            rule = SvgValidationRule.ExternalFont;
        }
        else if (node.ElementKind == SvgElementKind.Image || node.ElementKind == SvgElementKind.FeImage)
        {
            kind = SvgResourceKind.Image;
        }
        if (IsDataUri(value) && UnsafeDataType(dataMediaType, kind))
        {
            Add(state, SvgValidationRule.UnsafeDataUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "The data URL media type is active, malformed, or incompatible with this resource.", node, attribute, attribute.SourceSpan, "Ignore the unsafe data URL and require a passive resource type.");
            return;
        }
        SvgReferenceResult reference = ResolveNodeReference(node, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References);
        if (reference == null || reference.Status != SvgReferenceStatus.External) return;
        ValidateResource(value, reference.BaseUri, reference.ResourceUri, kind, rule, dataMediaType, node, attribute, attribute.SourceSpan, state);
    }

    static bool IsResourceElement(SvgElementKind elementKind)
    {
        if (elementKind == SvgElementKind.Image || elementKind == SvgElementKind.FeImage || elementKind == SvgElementKind.Audio || elementKind == SvgElementKind.Video || elementKind == SvgElementKind.Iframe) return true;
        if (elementKind == SvgElementKind.FontFaceUri) return true;
        return false;
    }

    static void ValidateInlineStyle(SvgNode node, SvgAttribute attribute, SvgSecurityValidationState state)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(attribute.Value, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Limits);
        int index = 0;
        while (index < parsed.Declarations.Count && state.Work.Result.Error == SvgValidationError.None)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            TakeDeclaration(attribute.SourceSpan, state);
            ValidateDeclarationUrls(declaration, SvgResourceKind.Image, SvgValidationRule.ExternalImage, node, attribute, attribute.SourceSpan, state);
            index = index + 1;
        }
    }

    static void CountPresentation(SvgNode node, SvgAttribute attribute, SvgSecurityValidationState state)
    {
        if (attribute.NamespaceUri.Length != 0) return;
        SvgElementDefinition? element = (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Elements.Find(node.ElementKind);
        SvgElementAttributeDefinition? definition = element == null ? null : element.FindAttribute("", attribute.LocalName);
        if (definition == null || !definition.Presentation) return;
        TakeDeclaration(attribute.SourceSpan, state);
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(attribute.LocalName + ":" + attribute.Value, state.Work.Options.Css.Limits);
        if (parsed.Declarations.Count != 0) ValidateDeclarationUrls(parsed.Declarations[0], SvgResourceKind.Image, SvgValidationRule.ExternalImage, node, attribute, attribute.SourceSpan, state);
    }

    static void ValidateStyleSheets(SvgStyleSheetCollection collection, SvgSecurityValidationState state)
    {
        int index = 0;
        while (index < collection.StyleSheets.Count && state.Work.Result.Error == SvgValidationError.None)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry != null) ValidateRules(entry.StyleSheet.Rules, entry.SourceNode, state);
            index = index + 1;
        }
    }

    static void ValidateRules(System.Collections.Generic.List<SvgCssRule> rules, SvgNode? sourceNode, SvgSecurityValidationState state)
    {
        int index = 0;
        while (index < rules.Count && state.Work.Result.Error == SvgValidationError.None)
        {
            SvgCssRule rule = rules[index];
            if (rule != null)
            {
                if (rule.AtRule != null && rule.AtRule.Kind == SvgCssAtRuleKind.Import && rule.AtRule.ImportUri.Length != 0)
                {
                    ValidateCssResource(rule.AtRule.ImportUri, SvgResourceKind.StyleSheet, SvgValidationRule.ExternalStyleSheet, rule.AtRule.Source, sourceNode, state);
                }
                int declarationIndex = 0;
                while (declarationIndex < rule.Declarations.Count && state.Work.Result.Error == SvgValidationError.None)
                {
                    SvgCssDeclaration declaration = rule.Declarations[declarationIndex];
                    TakeDeclaration(declaration.Source, state);
                    SvgResourceKind kind = rule.AtRule != null && rule.AtRule.FontFace != null && string.Equals(declaration.Name, "src", System.StringComparison.OrdinalIgnoreCase) ? SvgResourceKind.Font : SvgResourceKind.Image;
                    SvgValidationRule resourceRule = kind == SvgResourceKind.Font ? SvgValidationRule.ExternalFont : SvgValidationRule.ExternalImage;
                    ValidateDeclarationUrls(declaration, kind, resourceRule, sourceNode, null, declaration.Source, state);
                    declarationIndex = declarationIndex + 1;
                }
                if (rule.AtRule != null && rule.AtRule.Keyframes != null) ValidateKeyframes(rule.AtRule.Keyframes, sourceNode, state);
                ValidateRules(rule.ChildRules, sourceNode, state);
            }
            index = index + 1;
        }
    }

    static void ValidateKeyframes(SvgCssKeyframes keyframes, SvgNode? sourceNode, SvgSecurityValidationState state)
    {
        int index = 0;
        while (index < keyframes.Keyframes.Count && state.Work.Result.Error == SvgValidationError.None)
        {
            SvgCssKeyframe keyframe = keyframes.Keyframes[index];
            int declarationIndex = 0;
            while (declarationIndex < keyframe.Declarations.Count && state.Work.Result.Error == SvgValidationError.None)
            {
                SvgCssDeclaration declaration = keyframe.Declarations[declarationIndex];
                TakeDeclaration(declaration.Source, state);
                ValidateDeclarationUrls(declaration, SvgResourceKind.Image, SvgValidationRule.ExternalImage, sourceNode, null, declaration.Source, state);
                declarationIndex = declarationIndex + 1;
            }
            index = index + 1;
        }
    }

    static void ValidateDeclarationUrls(SvgCssDeclaration? declaration, SvgResourceKind kind, SvgValidationRule rule, SvgNode? sourceNode, SvgAttribute? sourceAttribute, SvgSourceSpan? fallbackSource, SvgSecurityValidationState state)
    {
        if (declaration == null) return;
        System.Collections.Generic.List<string> urls = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SvgSourceSpan> sources = new System.Collections.Generic.List<SvgSourceSpan>();
        CollectUrls(declaration.Values, urls, sources, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References.MaxUriCharacters);
        int index = 0;
        while (index < urls.Count && state.Work.Result.Error == SvgValidationError.None)
        {
            SvgSourceSpan? source = sourceAttribute == null && sources[index] != null ? sources[index] : fallbackSource;
            string url = urls[index];
            if (ScriptScheme(url))
            {
                Add(state, SvgValidationRule.ScriptUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "A CSS URL uses a script-capable scheme.", sourceNode, sourceAttribute, source, "Ignore the declaration containing the script URL.");
            }
            else if (IsDataUri(url))
            {
                string mediaType = DataMediaType(url);
                if (UnsafeDataType(mediaType, kind)) Add(state, SvgValidationRule.UnsafeDataUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "A CSS URL uses an active, malformed, or incompatible data type.", sourceNode, sourceAttribute, source, "Ignore the declaration containing the unsafe data URL.");
                else ValidateCssResource(url, kind, rule, source, sourceNode, state);
            }
            else ValidateCssResource(url, kind, rule, source, sourceNode, state);
            index = index + 1;
        }
    }

    static void ValidateCssResource(string authoredUri, SvgResourceKind kind, SvgValidationRule rule, SvgSourceSpan? source, SvgNode? sourceNode, SvgSecurityValidationState state)
    {
        if (ScriptScheme(authoredUri))
        {
            Add(state, SvgValidationRule.ScriptUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "A CSS resource URL uses a script-capable scheme.", sourceNode, null, source, "Ignore the CSS resource URL.");
            return;
        }
        string mediaType = DataMediaType(authoredUri);
        if (IsDataUri(authoredUri) && UnsafeDataType(mediaType, kind))
        {
            Add(state, SvgValidationRule.UnsafeDataUrl, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, "A CSS resource data URL has an active, malformed, or incompatible media type.", sourceNode, null, source, "Ignore the unsafe CSS resource URL.");
            return;
        }
        string baseUri = BaseUri(source, (state.Work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        SvgUriResult resolved = baseUri.Length == 0 ? new SvgUri().NormalizeWithValueAndMaxCharacters(authoredUri, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References.MaxUriCharacters) : new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, authoredUri, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References.MaxUriCharacters);
        if (!resolved.Success) return;
        if (authoredUri.Trim().StartsWith("#", System.StringComparison.Ordinal)) return;
        ValidateResource(authoredUri, baseUri, SvgReferenceUri.RemoveFragment(resolved.NormalizedIri), kind, rule, mediaType, sourceNode, null, source, state);
    }

    static void ValidateResource(string authoredUri, string baseUri, string resolvedUri, SvgResourceKind kind, SvgValidationRule rule, string dataMediaType, SvgNode? node, SvgAttribute? attribute, SvgSourceSpan? source, SvgSecurityValidationState state)
    {
        string mediaType = dataMediaType.Length == 0 ? MediaTypeForUri(resolvedUri, kind) : dataMediaType;
        SvgResourcePolicyRequest request = new SvgResourcePolicyRequest(authoredUri, baseUri, resolvedUri, kind);
        request.MediaType = mediaType;
        request.Source = source;
        SvgResourcePolicyDecision decision = (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ResourcePolicy.Evaluate(request);
        if (!decision.Allowed)
        {
            string message = decision.Diagnostic == null ? "The resource policy denied the external resource." : decision.Diagnostic.Message;
            Add(state, rule, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ResourceDenied, message, node, attribute, source, "Do not acquire the denied external resource.");
        }
        else if (!decision.IsSameDocument && decision.Scheme != "data")
        {
            string message = ExternalMessage(kind);
            Add(state, rule, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CallbackRequired, message, node, attribute, source, "Require an explicit caller callback and policy approval before acquisition.");
        }
    }

    static string ExternalMessage(SvgResourceKind kind)
    {
        if (kind == SvgResourceKind.StyleSheet) return "The external stylesheet requires caller-mediated acquisition.";
        if (kind == SvgResourceKind.Font) return "The external font requires caller-mediated acquisition.";
        if (kind == SvgResourceKind.Image) return "The external image requires caller-mediated acquisition.";
        return "The external embedded resource requires caller-mediated acquisition.";
    }

    static void TakeDeclaration(SvgSourceSpan? source, SvgSecurityValidationState state)
    {
        state.DeclarationCount = state.DeclarationCount + 1;
        if (state.DeclarationCount <= (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumAuthoredDeclarations || state.DeclarationLimitReported) return;
        state.DeclarationLimitReported = true;
        SvgNode? node = NodeForSource(state.Nodes, source);
        SvgAttribute? attribute = AttributeForSource(node, source);
        Add(state, SvgValidationRule.ExcessiveDeclarations, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The authored CSS and presentation declaration count exceeds MaximumAuthoredDeclarations.", node, attribute, source, "Ignore declarations after the configured security bound.");
    }

    internal static void CollectUrls(System.Collections.Generic.List<SvgCssComponentValue> values, System.Collections.Generic.List<string> urls, System.Collections.Generic.List<SvgSourceSpan> sources, int maximum)
    {
        int index = 0;
        while (index < values.Count)
        {
            SvgCssComponentValue value = values[index];
            if (value.Token.Kind == SvgCssTokenKind.Url)
            {
                urls.Add(value.Token.Value);
                sources.Add((value.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            }
            else if (value.Kind == SvgCssComponentValueKind.Function && string.Equals(value.Name, "url", System.StringComparison.OrdinalIgnoreCase))
            {
                SvgIriParseResult parsed = new SvgIriParser().ParseUrl(value.RawText, maximum);
                if (parsed.Success)
                {
                    urls.Add((parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value);
                    sources.Add((value.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                }
            }
            else CollectUrls(value.Values, urls, sources, maximum);
            index = index + 1;
        }
    }

    static SvgReferenceResult ResolveNodeReference(SvgNode node, SvgReferenceResolveOptions source)
    {
        SvgReferenceResolveOptions options = new SvgReferenceResolveOptions();
        options.RequireReference = false;
        options.ExternalDocument = source.ExternalDocument;
        options.MaxUriCharacters = source.MaxUriCharacters;
        options.MaxBaseDepth = source.MaxBaseDepth;
        options.NestedDocumentOptions = null;
        return new SvgReferenceResolver().ResolveWithNodeAndOptions(node, options);
    }

    internal static string DataMediaType(string uri)
    {
        string value = uri.Trim();
        if (!value.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase)) return "";
        int comma = value.IndexOf(',', 5);
        if (comma < 0) return "";
        string metadata = value.Substring(5, comma - 5);
        if (metadata.Length == 0 || metadata[0] == ';') return "text/plain";
        int semicolon = metadata.IndexOf(';');
        string mediaType = semicolon < 0 ? metadata : metadata.Substring(0, semicolon);
        SvgMediaTypeParseResult parsed = new SvgMediaTypeParser().Parse(mediaType, 0, 4096);
        if (!parsed.Success) return "";
        return (parsed.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Type + "/" + parsed.MediaType.Subtype;
    }

    internal static bool UnsafeDataType(string mediaType, SvgResourceKind kind)
    {
        if (mediaType.Length == 0) return true;
        if (kind == SvgResourceKind.StyleSheet) return !string.Equals(mediaType, SvgMimeTypes.Css, System.StringComparison.OrdinalIgnoreCase);
        if (kind == SvgResourceKind.Font) return !FontMediaType(mediaType);
        if (kind == SvgResourceKind.Image)
        {
            if (string.Equals(mediaType, SvgMimeTypes.Svg, System.StringComparison.OrdinalIgnoreCase)) return true;
            return !RasterMediaType(mediaType);
        }
        return true;
    }

    static bool RasterMediaType(string value)
    {
        return string.Equals(value, SvgMimeTypes.Png, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Jpeg, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Gif, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.WebP, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Avif, System.StringComparison.OrdinalIgnoreCase);
    }

    static bool FontMediaType(string value)
    {
        return string.Equals(value, SvgMimeTypes.TrueType, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.OpenType, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Woff, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Woff2, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static string MediaTypeForUri(string uri, SvgResourceKind kind)
    {
        string lower = uri.ToLowerInvariant();
        int query = lower.IndexOf('?');
        if (query >= 0) lower = lower.Substring(0, query);
        if (kind == SvgResourceKind.StyleSheet && lower.EndsWith(".css", System.StringComparison.Ordinal)) return SvgMimeTypes.Css;
        if (kind == SvgResourceKind.Font)
        {
            if (lower.EndsWith(".ttf", System.StringComparison.Ordinal)) return SvgMimeTypes.TrueType;
            if (lower.EndsWith(".otf", System.StringComparison.Ordinal)) return SvgMimeTypes.OpenType;
            if (lower.EndsWith(".woff", System.StringComparison.Ordinal)) return SvgMimeTypes.Woff;
            if (lower.EndsWith(".woff2", System.StringComparison.Ordinal)) return SvgMimeTypes.Woff2;
        }
        if (kind == SvgResourceKind.Image)
        {
            if (lower.EndsWith(".svg", System.StringComparison.Ordinal)) return SvgMimeTypes.Svg;
            if (lower.EndsWith(".png", System.StringComparison.Ordinal)) return SvgMimeTypes.Png;
            if (lower.EndsWith(".jpg", System.StringComparison.Ordinal) || lower.EndsWith(".jpeg", System.StringComparison.Ordinal)) return SvgMimeTypes.Jpeg;
            if (lower.EndsWith(".gif", System.StringComparison.Ordinal)) return SvgMimeTypes.Gif;
            if (lower.EndsWith(".webp", System.StringComparison.Ordinal)) return SvgMimeTypes.WebP;
            if (lower.EndsWith(".avif", System.StringComparison.Ordinal)) return SvgMimeTypes.Avif;
        }
        return "";
    }

    internal static bool ScriptScheme(string uri)
    {
        string value = uri.Trim();
        int colon = value.IndexOf(':');
        if (colon < 1) return false;
        int slash = value.IndexOf('/');
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (slash >= 0 && slash < colon || query >= 0 && query < colon || fragment >= 0 && fragment < colon) return false;
        return SchemeEquals(value, colon, "javascript") || SchemeEquals(value, colon, "vbscript");
    }

    static bool SchemeEquals(string value, int length, string expected)
    {
        int valueIndex = 0;
        int expectedIndex = 0;
        while (valueIndex < length)
        {
            char character = value[valueIndex];
            valueIndex = valueIndex + 1;
            if (character <= ' ') continue;
            if (expectedIndex >= expected.Length || char.ToLowerInvariant(character) != expected[expectedIndex]) return false;
            expectedIndex = expectedIndex + 1;
        }
        return expectedIndex == expected.Length;
    }

    internal static bool IsDataUri(string value)
    {
        return value.Trim().StartsWith("data:", System.StringComparison.OrdinalIgnoreCase);
    }

    static string BaseUri(SvgSourceSpan? source, SvgDocument document)
    {
        if (source != null && source.Source != null)
        {
            SvgSourceUri resource = source.Source;
            if (resource.Parent != null) resource = resource.Parent;
            return SvgReferenceUri.RemoveFragment(resource.Uri);
        }
        if (document.BaseUri.Length != 0) return document.BaseUri;
        return document.Source == null ? "" : document.Source.Uri;
    }

    static bool HasForeignParent(SvgNode node)
    {
        SvgNode? current = node.Parent;
        while (current != null)
        {
            if (current.Kind == SvgNodeKind.Element && (current.ElementKind == SvgElementKind.ForeignObject || current.NamespaceUri.Length != 0 && current.NamespaceUri != SvgNamespaces.Svg)) return true;
            current = current.Parent;
        }
        return false;
    }

    static SvgAttribute? FindHref(SvgNode node)
    {
        SvgAttribute? result = FindAttribute(node, "", SvgAttributeNames.Href);
        return result == null ? FindAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href) : result;
    }

    static SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName)
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

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgNode? root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (root == null) return result;
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(root);
        while (stack.Count != 0)
        {
            SvgNode node = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (node.Kind == SvgNodeKind.Element) result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }

    static SvgNode? NodeForSource(System.Collections.Generic.List<SvgNode> nodes, SvgSourceSpan? source)
    {
        if (source == null) return null;
        int index = 0;
        while (index < nodes.Count)
        {
            int attributeIndex = 0;
            while (attributeIndex < nodes[index].Attributes.Count)
            {
                SvgAttribute attribute = nodes[index].Attributes[attributeIndex];
                if (attribute.SourceSpan != null && attribute.SourceSpan.ContainsSpan(source)) return nodes[index];
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index].SourceSpan != null && (nodes[index].SourceSpan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ContainsSpan(source)) return nodes[index];
            index = index + 1;
        }
        return null;
    }

    static SvgAttribute? AttributeForSource(SvgNode? node, SvgSourceSpan? source)
    {
        if (node == null || source == null) return null;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.SourceSpan != null && attribute.SourceSpan.ContainsSpan(source)) return attribute;
            index = index + 1;
        }
        return null;
    }

    static void Add(SvgSecurityValidationState state, SvgValidationRule rule, SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgNode? node, SvgAttribute? attribute, SvgSourceSpan? source, string recovery)
    {
        int index = 0;
        while (index < state.Work.Result.Issues.Count)
        {
            SvgValidationIssue issue = state.Work.Result.Issues[index];
            if (issue.Rule == rule && SameSpan(issue.Diagnostic.Source, source)) return;
            index = index + 1;
        }
        new SvgValidator().AddLocated(state.Work, rule, severity, code, message, node, attribute, source, recovery);
    }

    static bool SameSpan(SvgSourceSpan? left, SvgSourceSpan? right)
    {
        if (left == null || right == null) return false;
        return object.ReferenceEquals(left.Source, right.Source) && left.StartOffset == right.StartOffset && left.EndOffset == right.EndOffset;
    }
}

internal class SvgSecurityValidationState
{
    internal SvgValidationWork Work;
    internal System.Collections.Generic.List<SvgNode> Nodes;
    internal int DeclarationCount;
    internal bool DeclarationLimitReported;

    internal SvgSecurityValidationState(SvgValidationWork work, System.Collections.Generic.List<SvgNode> nodes)
    {
        Work = work;
        Nodes = nodes;
        DeclarationCount = 0;
        DeclarationLimitReported = false;
    }
}
