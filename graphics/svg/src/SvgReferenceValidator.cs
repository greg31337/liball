namespace svg;

internal static class SvgReferenceValidator
{
    internal static void Validate(SvgValidationWork work)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes((work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement);
        work.ReferenceNodes = nodes;
        ValidateDuplicateIds(nodes, work);
        if (work.Result.Error != SvgValidationError.None) return;
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(work.Result.Document, new System.Collections.Generic.List<SvgStyleSheet>(), (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Limits);
        System.Collections.Generic.List<SvgStyleSheet> styleSheets = StyleSheets(collection);
        SvgComputedStyleTree styles = new SvgStyleResolver().ResolveDocument(work.Result.Document, collection, work.Options.Css);
        int index = 0;
        while (index < nodes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgNode node = nodes[index];
            ValidateNodeReference(node, styleSheets, work);
            SvgComputedStyle? style = styles.Find(node);
            if (style != null) ValidateStyleReferences(node, style, work);
            index = index + 1;
        }
        if (work.Result.Error == SvgValidationError.None) ValidateResourceMediaTypes(nodes, work);
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

    static void ValidateDuplicateIds(System.Collections.Generic.List<SvgNode> nodes, SvgValidationWork work)
    {
        System.Collections.Generic.Dictionary<string, SvgAttribute> first = new System.Collections.Generic.Dictionary<string, SvgAttribute>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgAttribute? id = FindAttribute(nodes[index], "", SvgAttributeNames.Id);
            if (id != null && id.Value.Length != 0)
            {
                if (!first.ContainsKey(id.Value)) first[id.Value] = id;
                else
                {
                    string message = "Duplicate ID '" + id.Value + "' makes fragment references ambiguous.";
                    Add(work, SvgValidationRule.DuplicateId, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.DuplicateId, message, nodes[index], id, id.SourceSpan, "Keep the first ID in document order and rename this duplicate together with its references.", first[id.Value].SourceSpan);
                }
            }
            index = index + 1;
        }
    }

    static void ValidateNodeReference(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgValidationWork work)
    {
        if (IsAnimation(node.ElementKind))
        {
            ValidateAnimation(node, work);
            return;
        }
        if (node.ElementKind == SvgElementKind.Use)
        {
            SvgUseResolveOptions options = new SvgUseResolveOptions();
            options.Attributes = work.TypedValues;
            options.ReferenceOptions = CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References);
            options.Limits = work.Options.ReferenceLimits;
            options.StyleSheets = styleSheets;
            options.StyleOptions = work.Options.Css;
            options.ComputedStyle = null;
            SvgUseResult result = new SvgUseResolver().ResolveWithNodeAndOptions(node, options);
            AddDiagnostics(result.Diagnostics, SvgValidationRule.HrefReference, node, work);
            return;
        }
        if (IsPaintServer(node.ElementKind))
        {
            if (FindHref(node) == null) return;
            SvgPaintServerResolveOptions options = new SvgPaintServerResolveOptions();
            options.Attributes = work.TypedValues;
            options.ReferenceOptions = CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References);
            options.Limits = work.Options.ReferenceLimits;
            options.StyleSheets = styleSheets;
            options.StyleOptions = work.Options.Css;
            SvgPaintServerResult result = new SvgPaintServerResolver().ResolveWithNodeAndOptions(node, options);
            AddDiagnostics(result.Diagnostics, SvgValidationRule.HrefReference, node, work);
            return;
        }
        if (node.ElementKind == SvgElementKind.Filter)
        {
            SvgFilterResolveOptions options = new SvgFilterResolveOptions();
            options.Attributes = work.TypedValues;
            options.ReferenceOptions = CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References);
            options.ReferenceLimits = work.Options.ReferenceLimits;
            options.StyleSheets = styleSheets;
            options.StyleOptions = work.Options.Css;
            SvgFilterResult result = new SvgFilterResolver().ResolveWithNodeAndOptions(node, options);
            AddDiagnostics(result.Diagnostics, SvgValidationRule.HrefReference, node, work);
            return;
        }
        if (IsEffectResource(node.ElementKind))
        {
            SvgEffectResolveOptions options = new SvgEffectResolveOptions();
            options.Attributes = work.TypedValues;
            options.ReferenceOptions = CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References);
            options.Limits = work.Options.ReferenceLimits;
            options.StyleSheets = styleSheets;
            options.StyleOptions = work.Options.Css;
            SvgEffectResult result = new SvgEffectResolver().ResolveWithNodeAndOptions(node, options);
            AddDiagnostics(result.Diagnostics, SvgValidationRule.EffectReference, node, work);
        }
        SvgAttribute? href = FindHref(node);
        if (href == null) return;
        SvgReferenceResult reference = new SvgReferenceResolver().ResolveWithNodeAndOptions(node, CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References));
        AddReference(reference, SvgValidationRule.HrefReference, node, href, work);
        if (reference.Status == SvgReferenceStatus.Resolved && reference.Target != null && !CompatibleHrefTarget(node.ElementKind, reference.Target))
        {
            Add(work, SvgValidationRule.WrongReferenceType, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidReference, "The '" + node.LocalName + "' href identifies an incompatible element kind.", node, href, href.SourceSpan, "Ignore the incompatible href reference.", reference.TargetSource);
        }
    }

    static void ValidateAnimation(SvgNode node, SvgValidationWork work)
    {
        SvgAnimationTargetOptions options = new SvgAnimationTargetOptions();
        options.PropertyCatalog = (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Css.Properties;
        options.ElementCatalog = work.Options.Elements;
        options.ReferenceOptions = CopyReferenceOptions(work.Options.References);
        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSourceAndOptions(node, options);
        if (animation.TargetReference != null) AddReference(animation.TargetReference, SvgValidationRule.AnimationTarget, node, FindHref(node), work);
        bool targetFailure = animation.Error == SvgAnimationTargetError.MissingTarget || animation.Error == SvgAnimationTargetError.InvalidTarget;
        int index = 0;
        while (index < animation.Diagnostics.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgDiagnostic diagnostic = animation.Diagnostics[index];
            if (targetFailure && diagnostic.Code != SvgDiagnosticCode.InvalidReference && diagnostic.Code != SvgDiagnosticCode.ReferenceCycle)
            {
                AddImported(work, SvgValidationRule.AnimationTarget, diagnostic, node, "Ignore the animation whose target or target value slot is invalid.");
            }
            index = index + 1;
        }
    }

    static void ValidateStyleReferences(SvgNode node, SvgComputedStyle style, SvgValidationWork work)
    {
        ValidatePaint(node, style.Find(SvgCssPropertyNames.Fill), work);
        ValidatePaint(node, style.Find(SvgCssPropertyNames.Stroke), work);
        ValidateEffect(node, style.Find(SvgCssPropertyNames.ClipPath), SvgElementKind.ClipPath, work);
        ValidateEffect(node, style.Find(SvgCssPropertyNames.Mask), SvgElementKind.Mask, work);
        ValidateEffect(node, style.Find(SvgCssPropertyNames.Filter), SvgElementKind.Filter, work);
        ValidateEffect(node, style.Find(SvgCssPropertyNames.MarkerStart), SvgElementKind.Marker, work);
        ValidateEffect(node, style.Find(SvgCssPropertyNames.MarkerMid), SvgElementKind.Marker, work);
        ValidateEffect(node, style.Find(SvgCssPropertyNames.MarkerEnd), SvgElementKind.Marker, work);
    }

    static void ValidatePaint(SvgNode node, SvgComputedValue? value, SvgValidationWork work)
    {
        if (value == null || value.Paint == null) return;
        ValidatePaintAtom(node, value.Paint, value.Source, work);
        int index = 0;
        while (index < value.Paint.Fallbacks.Count && work.Result.Error == SvgValidationError.None)
        {
            ValidatePaintAtom(node, value.Paint.Fallbacks[index], value.Source, work);
            index = index + 1;
        }
    }

    static void ValidatePaintAtom(SvgNode node, SvgPaint? paint, SvgSourceSpan? source, SvgValidationWork work)
    {
        if (paint == null || paint.Kind != SvgPaintKind.Url) return;
        SvgReferenceResult reference = new SvgReferenceResolver().ResolveValue(node, paint.Url, source, CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References));
        AddReference(reference, SvgValidationRule.PaintReference, node, AttributeForSource(node, source), work);
        if (reference.Status == SvgReferenceStatus.Resolved && reference.Target != null && !IsPaintServer(reference.Target.ElementKind))
        {
            Add(work, SvgValidationRule.PaintReference, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidReference, "A paint URL must identify a gradient or pattern element.", node, AttributeForSource(node, source), source, "Ignore this URL paint and use its next fallback or no paint.", reference.TargetSource);
        }
    }

    static void ValidateEffect(SvgNode node, SvgComputedValue? value, SvgElementKind expected, SvgValidationWork work)
    {
        if (value == null) return;
        SvgIriReference? iri = value.Reference;
        if (iri == null && value.CssText.Length != 0 && !string.Equals(value.CssText, "none", System.StringComparison.OrdinalIgnoreCase))
        {
            SvgIriParseResult parsed = new SvgIriParser().ParseUrl(value.CssText, (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References.MaxUriCharacters);
            if (parsed.Success) iri = parsed.Reference;
        }
        if (iri == null) return;
        SvgReferenceResult reference = new SvgReferenceResolver().ResolveValue(node, iri.Value, value.Source, CopyReferenceOptions((work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).References));
        AddReference(reference, SvgValidationRule.EffectReference, node, AttributeForSource(node, value.Source), work);
        if (reference.Status == SvgReferenceStatus.Resolved && reference.Target != null && reference.Target.ElementKind != expected)
        {
            Add(work, SvgValidationRule.EffectReference, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidReference, "The '" + value.PropertyName + "' URL identifies an incompatible effect resource.", node, AttributeForSource(node, value.Source), value.Source, "Ignore the incompatible effect URL and use the property's initial value.", reference.TargetSource);
        }
    }

    static void AddReference(SvgReferenceResult? reference, SvgValidationRule fallback, SvgNode node, SvgAttribute? attribute, SvgValidationWork work)
    {
        if (reference == null || reference.Status == SvgReferenceStatus.Missing || reference.Status == SvgReferenceStatus.External || reference.Status == SvgReferenceStatus.Resolved) return;
        SvgValidationRule rule = reference.Status == SvgReferenceStatus.TargetNotFound ? SvgValidationRule.MissingReference : fallback;
        int index = 0;
        while (index < reference.Diagnostics.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgDiagnostic diagnostic = reference.Diagnostics[index];
            string recovery = rule == SvgValidationRule.MissingReference ? "Ignore the unresolved reference or add an element with the requested ID." : "Ignore the invalid reference value.";
            AddImported(work, rule, diagnostic, node, recovery);
            index = index + 1;
        }
        if (reference.Diagnostics.Count == 0)
        {
            string message = rule == SvgValidationRule.MissingReference ? "The reference does not identify an available element." : "The reference is invalid.";
            Add(work, rule, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidReference, message, node, attribute, reference.ReferenceSource, "Ignore the invalid or unresolved reference.", null);
        }
    }

    static void AddDiagnostics(System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgValidationRule fallback, SvgNode node, SvgValidationWork work)
    {
        int index = 0;
        while (index < diagnostics.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgDiagnostic diagnostic = diagnostics[index];
            if (diagnostic.Code == SvgDiagnosticCode.InvalidReference || diagnostic.Code == SvgDiagnosticCode.ReferenceCycle)
            {
                SvgValidationRule rule = DiagnosticRule(diagnostic, fallback);
                string recovery = rule == SvgValidationRule.FilterInput ? "Resolve the missing input to transparent black, as required by filter processing." : rule == SvgValidationRule.ReferenceCycle ? "Break the reference cycle and ignore the edge that re-enters the active chain." : "Ignore the invalid reference edge and stop following this chain.";
                AddImported(work, rule, diagnostic, node, recovery);
            }
            index = index + 1;
        }
    }

    static SvgValidationRule DiagnosticRule(SvgDiagnostic diagnostic, SvgValidationRule fallback)
    {
        if (diagnostic.Code == SvgDiagnosticCode.ReferenceCycle) return SvgValidationRule.ReferenceCycle;
        string message = diagnostic.Message.ToLowerInvariant();
        if (message.Contains("filter input")) return SvgValidationRule.FilterInput;
        if (message.Contains("incompatible") || message.Contains("must identify") || message.Contains("cannot receive")) return SvgValidationRule.WrongReferenceType;
        if (message.Contains("does not identify") || message.Contains("could not be resolved") || message.Contains("missing") || message.Contains("unavailable")) return SvgValidationRule.MissingReference;
        return fallback;
    }

    static void ValidateResourceMediaTypes(System.Collections.Generic.List<SvgNode> nodes, SvgValidationWork work)
    {
        int index = 0;
        while (index < (work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Resources.Count && work.Result.Error == SvgValidationError.None)
        {
            SvgResourceAcquisitionResult resource = work.Options.Resources[index];
            if (resource.DeclaredMediaType.Length != 0 && resource.ActualMediaType.Length != 0)
            {
                SvgMediaTypeParseResult declared = new SvgMediaTypeParser().Parse(resource.DeclaredMediaType, 64, 4096);
                SvgMediaTypeParseResult actual = new SvgMediaTypeParser().Parse(resource.ActualMediaType, 64, 4096);
                if (declared.Success && actual.Success && ((declared.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Type != (actual.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Type || declared.MediaType.Subtype != actual.MediaType.Subtype))
                {
                    SvgSourceSpan? source = resource.Request == null ? null : resource.Request.Source;
                    SvgNode? node = NodeForSource(nodes, source);
                    string message = "Resource media metadata declares '" + declared.MediaType.Type + "/" + declared.MediaType.Subtype + "' but reports actual type '" + actual.MediaType.Type + "/" + actual.MediaType.Subtype + "'.";
                    Add(work, SvgValidationRule.ResourceMediaType, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.ResourceFailed, message, node, AttributeForSource(node, source), source, "Use the actual media type and correct the declared resource metadata.", null);
                }
            }
            index = index + 1;
        }
    }

    static void AddImported(SvgValidationWork work, SvgValidationRule rule, SvgDiagnostic? diagnostic, SvgNode fallbackNode, string recovery)
    {
        if (diagnostic == null || Exists(work, diagnostic.Code, diagnostic.Source)) return;
        SvgNode? node = NodeForSource(work.ReferenceNodes, diagnostic.Source);
        if (node == null) node = fallbackNode;
        SvgAttribute? attribute = AttributeForSource(node, diagnostic.Source);
        Add(work, rule, diagnostic.Severity, diagnostic.Code, diagnostic.Message, node, attribute, diagnostic.Source, recovery, null);
        if (work.Result.Issues.Count == 0) return;
        SvgDiagnostic added = work.Result.Issues[work.Result.Issues.Count - 1].Diagnostic;
        int index = 0;
        while (index < diagnostic.RelatedSpans.Count)
        {
            added.AddRelatedSpan(diagnostic.RelatedSpans[index]);
            index = index + 1;
        }
    }

    static void Add(SvgValidationWork work, SvgValidationRule rule, SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgNode? node, SvgAttribute? attribute, SvgSourceSpan? source, string recovery, SvgSourceSpan? related)
    {
        if (Exists(work, code, source)) return;
        new SvgValidator().AddLocated(work, rule, severity, code, message, node, attribute, source, recovery);
        if (related != null && work.Result.Issues.Count != 0) work.Result.Issues[work.Result.Issues.Count - 1].Diagnostic.AddRelatedSpan(related);
    }

    static bool Exists(SvgValidationWork work, SvgDiagnosticCode code, SvgSourceSpan? source)
    {
        int index = 0;
        while (index < work.Result.Issues.Count)
        {
            SvgDiagnostic diagnostic = work.Result.Issues[index].Diagnostic;
            if (diagnostic.Code == code && SameSpan(diagnostic.Source, source)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool SameSpan(SvgSourceSpan? left, SvgSourceSpan? right)
    {
        if (left == null || right == null) return false;
        return object.ReferenceEquals(left.Source, right.Source) && left.StartOffset == right.StartOffset && left.EndOffset == right.EndOffset;
    }

    static System.Collections.Generic.List<SvgStyleSheet> StyleSheets(SvgStyleSheetCollection collection)
    {
        System.Collections.Generic.List<SvgStyleSheet> result = new System.Collections.Generic.List<SvgStyleSheet>();
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            if (entry != null && entry.StyleSheet != null) result.Add(entry.StyleSheet);
            index = index + 1;
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
        SvgNode? found = null;
        long foundLength = long.MaxValue;
        index = 0;
        while (index < nodes.Count)
        {
            SvgSourceSpan? candidate = nodes[index].SourceSpan;
            if (candidate != null && candidate.ContainsSpan(source))
            {
                long length = candidate.EndOffset - candidate.StartOffset;
                if (found == null || length < foundLength)
                {
                    found = nodes[index];
                    foundLength = length;
                }
            }
            index = index + 1;
        }
        if (found != null) return found;
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

    static SvgAttribute? FindHref(SvgNode node)
    {
        SvgAttribute? href = FindAttribute(node, "", SvgAttributeNames.Href);
        return href == null ? FindAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href) : href;
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

    static SvgReferenceResolveOptions CopyReferenceOptions(SvgReferenceResolveOptions source)
    {
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.RequireReference = source.RequireReference;
        result.ExternalDocument = source.ExternalDocument;
        result.MaxUriCharacters = source.MaxUriCharacters;
        result.MaxBaseDepth = source.MaxBaseDepth;
        result.NestedDocumentOptions = null;
        return result;
    }

    static bool IsAnimation(SvgElementKind kind)
    {
        return kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateColor || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Set || kind == SvgElementKind.Discard;
    }

    static bool IsPaintServer(SvgElementKind kind)
    {
        return kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Pattern;
    }

    static bool IsEffectResource(SvgElementKind kind)
    {
        return kind == SvgElementKind.Marker || kind == SvgElementKind.ClipPath || kind == SvgElementKind.Mask;
    }

    static bool CompatibleHrefTarget(SvgElementKind source, SvgNode target)
    {
        if (source == SvgElementKind.TextPath || source == SvgElementKind.MPath) return target.ElementKind == SvgElementKind.Path;
        return true;
    }
}
