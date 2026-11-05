namespace svg;

/// <summary>Computes and explicitly invalidates bounded SVG computed-style caches.</summary>
public class SvgStyleResolver : ISvgStyleResolver
{
    System.Collections.Generic.List<SvgStyleCacheEntry> entries;
    SvgStyleResolverOptions defaultOptions;
    long cacheHits;
    long cacheMisses;
    long invalidatedEntries;
    long evictedEntries;
    long accessClock;

    /// <summary>Creates an empty computed-style cache.</summary>
    public SvgStyleResolver()
    {
        entries = new System.Collections.Generic.List<SvgStyleCacheEntry>();
        defaultOptions = new SvgStyleResolverOptions();
        cacheHits = 0;
        cacheMisses = 0;
        invalidatedEntries = 0;
        evictedEntries = 0;
        accessClock = 0;
    }

    /// <summary>Computes one element using ordered stylesheets and cache defaults.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in cascade order.</param>
    /// <returns>The cached or newly computed style.</returns>
    public SvgComputedStyle? Resolve(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets)
    {
        return ResolveWithOptions(node, styleSheets, defaultOptions);
    }

    /// <summary>Computes one element using ordered stylesheets and explicit options.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in cascade order.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The cached or newly computed style.</returns>
    public SvgComputedStyle? ResolveWithOptions(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgStyleResolverOptions options)
    {
        SvgStyleResolverInput input = new SvgStyleResolverInput(styleSheets, null);
        return ResolveInput(node, input, options);
    }

    /// <summary>Computes one element using a collected stylesheet set and explicit options.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="collection">The collected embedded, imported, and caller stylesheets.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The cached or newly computed style.</returns>
    public SvgComputedStyle? ResolveCollection(SvgNode node, SvgStyleSheetCollection collection, SvgStyleResolverOptions options)
    {
        SvgStyleResolverInput input = new SvgStyleResolverInput(null, collection);
        return ResolveInput(node, input, options);
    }

    /// <summary>Computes every document element using a collected stylesheet set.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="collection">The collected stylesheet set.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The preorder computed-style tree.</returns>
    public SvgComputedStyleTree ResolveDocument(SvgDocument document, SvgStyleSheetCollection collection, SvgStyleResolverOptions options)
    {
        SvgStyleResolverInput input = new SvgStyleResolverInput(null, collection);
        return ResolveDocumentInput(document, input, options);
    }

    /// <summary>Computes every document element using ordered stylesheets.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="styleSheets">The stylesheets in cascade order.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The preorder computed-style tree.</returns>
    public SvgComputedStyleTree ResolveDocumentFromStyleSheets(SvgDocument document, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgStyleResolverOptions options)
    {
        SvgStyleResolverInput input = new SvgStyleResolverInput(styleSheets, null);
        return ResolveDocumentInput(document, input, options);
    }

    /// <summary>Returns current cache counters.</summary>
    /// <returns>An independent statistics snapshot.</returns>
    public SvgStyleResolverStatistics GetStatistics()
    {
        return new SvgStyleResolverStatistics(entries.Count, cacheHits, cacheMisses, invalidatedEntries, evictedEntries);
    }

    /// <summary>Invalidates every cached computed style.</summary>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateAll()
    {
        int removed = entries.Count;
        entries.Clear();
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.All, removed);
    }

    /// <summary>Invalidates styles sharing a tree with a changed node.</summary>
    /// <param name="node">A node in the changed tree.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateTree(SvgNode node)
    {
        int removed = RemoveTree(node);
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.Tree, removed);
    }

    /// <summary>Invalidates selector and inheritance dependencies for one changed attribute.</summary>
    /// <param name="node">The owning element.</param>
    /// <param name="attributeName">The case-sensitive XML local name.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateAttribute(SvgNode node, string attributeName)
    {
        SvgStyleInvalidationKind kind = attributeName == "class" || attributeName == "id" ? SvgStyleInvalidationKind.ClassOrId : SvgStyleInvalidationKind.Attribute;
        int removed = node == null || attributeName == null || attributeName.Length == 0 ? 0 : RemoveTree(node);
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(kind, removed);
    }

    /// <summary>Invalidates entries that used one stylesheet identity.</summary>
    /// <param name="styleSheet">The changed stylesheet.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateStyleSheet(SvgStyleSheet styleSheet)
    {
        int removed = 0;
        int index = entries.Count - 1;
        while (index >= 0)
        {
            if (entries[index].ContainsStyleSheet(styleSheet))
            {
                entries.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.StyleSheet, removed);
    }

    /// <summary>Invalidates dependent styles below one custom-property definition.</summary>
    /// <param name="node">The defining element.</param>
    /// <param name="customPropertyName">The case-sensitive custom-property name.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateCustomProperty(SvgNode node, string customPropertyName)
    {
        int removed = 0;
        if (node != null && customPropertyName != null && customPropertyName.StartsWith("--", System.StringComparison.Ordinal))
        {
            int index = entries.Count - 1;
            while (index >= 0)
            {
                SvgStyleCacheEntry entry = entries[index];
                if (IsDescendantOrSelf(entry.Node, node) && entry.DependsOn(customPropertyName))
                {
                    entries.RemoveAt(index);
                    removed = removed + 1;
                }
                index = index - 1;
            }
        }
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.CustomProperty, removed);
    }

    /// <summary>Invalidates entries using one viewport environment.</summary>
    /// <param name="environment">The changed environment.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateViewport(SvgStyleEnvironment environment)
    {
        int removed = RemoveEnvironment(environment);
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.Viewport, removed);
    }

    /// <summary>Invalidates entries using one media environment.</summary>
    /// <param name="environment">The changed environment.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateMedia(SvgStyleEnvironment environment)
    {
        int removed = RemoveEnvironment(environment);
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.Media, removed);
    }

    /// <summary>Invalidates all styles that can depend on caller font data.</summary>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateFonts()
    {
        int removed = entries.Count;
        entries.Clear();
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.Fonts, removed);
    }

    /// <summary>Invalidates entries using one host interaction-state object.</summary>
    /// <param name="state">The changed interaction state.</param>
    /// <returns>The invalidation result.</returns>
    public SvgStyleInvalidationResult InvalidateInteraction(ISvgDynamicSelectorState state)
    {
        int removed = 0;
        int index = entries.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(entries[index].DynamicState, state))
            {
                entries.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        AddInvalidated(removed);
        return new SvgStyleInvalidationResult(SvgStyleInvalidationKind.Interaction, removed);
    }

    SvgComputedStyle? ResolveInput(SvgNode node, SvgStyleResolverInput input, SvgStyleResolverOptions options)
    {
        if (!ValidArguments(node, input, options)) return Failure(node, "Style resolution arguments are invalid.");
        System.Collections.Generic.List<SvgNode> chain = ElementChain(node);
        SvgComputedStyle? parent = null;
        int index = chain.Count - 1;
        while (index >= 0)
        {
            parent = ResolveSingle(chain[index], parent, input, options);
            index = index - 1;
        }
        return parent;
    }

    SvgComputedStyle ResolveSingle(SvgNode node, SvgComputedStyle? parent, SvgStyleResolverInput input, SvgStyleResolverOptions options)
    {
        int found = FindEntry(node, parent, input, options);
        if (found >= 0)
        {
            cacheHits = cacheHits + 1;
            entries[found].AccessOrder = NextAccess();
            return entries[found].Style;
        }
        cacheMisses = cacheMisses + 1;
        SvgCascadeOptions cascadeOptions = new SvgCascadeOptions();
        cascadeOptions.Properties = options.Properties;
        cascadeOptions.Limits = options.Limits;
        cascadeOptions.SelectorOptions = options.SelectorOptions;
        cascadeOptions.StyleEnvironment = options.StyleEnvironment;
        cascadeOptions.Parent = parent == null ? null : parent.Cascade;
        cascadeOptions.IncludeUserAgentDefaults = options.IncludeUserAgentDefaults;
        SvgCascadeResult cascade = input.Collection == null ? new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(node, (input.StyleSheets ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), cascadeOptions) : new SvgCascade().ResolveWithNodeAndCollectionAndOptions(node, input.Collection, cascadeOptions);
        SvgComputedStyleOptions computedOptions = new SvgComputedStyleOptions();
        computedOptions.Properties = options.Properties;
        computedOptions.Limits = options.Limits;
        computedOptions.Parent = parent;
        computedOptions.MaxPaintFallbacks = options.MaxPaintFallbacks;
        SvgComputedStyle style = new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(node, cascade, computedOptions);
        AddEntry(new SvgStyleCacheEntry(node, parent, style, input, options, NextAccess()), options.MaxCacheEntries);
        return style;
    }

    SvgComputedStyleTree ResolveDocumentInput(SvgDocument document, SvgStyleResolverInput input, SvgStyleResolverOptions options)
    {
        SvgComputedStyleTree result = new SvgComputedStyleTree(document);
        if (document == null || document.DocumentElement == null || !ValidInput(input) || !ValidOptions(options))
        {
            result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Document style resolution arguments are invalid.", document == null ? null : document.SourceSpan));
            return result;
        }
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(document.DocumentElement);
        bool success = true;
        while (stack.Count != 0)
        {
            SvgNode node = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (node.Kind == SvgNodeKind.Element)
            {
                SvgComputedStyle? style = ResolveInput(node, input, options);
                result.Styles.Add((style ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                AppendDiagnostics(result.Diagnostics, style.Diagnostics);
                if (!style.Success) success = false;
            }
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                if (node.Children[childIndex].Kind == SvgNodeKind.Element) stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        result.Success = success;
        return result;
    }

    int FindEntry(SvgNode node, SvgComputedStyle? parent, SvgStyleResolverInput input, SvgStyleResolverOptions options)
    {
        int index = entries.Count - 1;
        while (index >= 0)
        {
            SvgStyleCacheEntry entry = entries[index];
            if (object.ReferenceEquals(entry.Node, node))
            {
                if (entry.Matches(parent, input, options)) return index;
                entries.RemoveAt(index);
            }
            index = index - 1;
        }
        return -1;
    }

    void AddEntry(SvgStyleCacheEntry entry, int maximum)
    {
        while (entries.Count >= maximum)
        {
            int oldest = 0;
            int index = 1;
            while (index < entries.Count)
            {
                if (entries[index].AccessOrder < entries[oldest].AccessOrder) oldest = index;
                index = index + 1;
            }
            entries.RemoveAt(oldest);
            evictedEntries = evictedEntries + 1;
        }
        entries.Add(entry);
    }

    int RemoveTree(SvgNode node)
    {
        if (node == null) return 0;
        int removed = 0;
        int index = entries.Count - 1;
        while (index >= 0)
        {
            if (SameTree(entries[index].Node, node))
            {
                entries.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        return removed;
    }

    int RemoveEnvironment(SvgStyleEnvironment environment)
    {
        int removed = 0;
        int index = entries.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(entries[index].Environment, environment))
            {
                entries.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        return removed;
    }

    long NextAccess()
    {
        if (accessClock == System.Int64.MaxValue)
        {
            int index = 0;
            while (index < entries.Count)
            {
                entries[index].AccessOrder = index + 1;
                index = index + 1;
            }
            accessClock = entries.Count;
        }
        accessClock = accessClock + 1;
        return accessClock;
    }

    void AddInvalidated(int count)
    {
        invalidatedEntries = invalidatedEntries + count;
    }

    static bool ValidArguments(SvgNode node, SvgStyleResolverInput input, SvgStyleResolverOptions options)
    {
        return node != null && node.Kind == SvgNodeKind.Element && ValidInput(input) && ValidOptions(options);
    }

    static bool ValidInput(SvgStyleResolverInput? input)
    {
        if (input == null) return false;
        if (input.Collection != null) return input.Collection.StyleSheets != null;
        if (input.StyleSheets == null) return false;
        int index = 0;
        while (index < input.StyleSheets.Count)
        {
            if (input.StyleSheets[index] == null) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidOptions(SvgStyleResolverOptions options)
    {
        return options != null && options.Properties != null && options.Limits != null && options.SelectorOptions != null && options.SelectorOptions.Namespaces != null && options.SelectorOptions.DynamicState != null && options.SelectorOptions.DynamicState.GetVersion() >= 0 && options.SelectorOptions.DynamicState.GetLanguage() != null && options.StyleEnvironment != null && options.MaxCacheEntries > 0 && options.MaxPaintFallbacks >= 0 && options.FontVersion >= 0;
    }

    static System.Collections.Generic.List<SvgNode> ElementChain(SvgNode node)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        SvgNode? current = node;
        while (current != null)
        {
            if (current.Kind == SvgNodeKind.Element) result.Add(current);
            current = current.Parent;
        }
        return result;
    }

    static bool SameTree(SvgNode? first, SvgNode? second)
    {
        if (first == null || second == null) return false;
        if (first.OwnerDocument != null || second.OwnerDocument != null) return object.ReferenceEquals(first.OwnerDocument, second.OwnerDocument);
        return object.ReferenceEquals(Root(first), Root(second));
    }

    static SvgNode Root(SvgNode node)
    {
        SvgNode current = node;
        while (current.Parent != null) current = current.Parent;
        return current;
    }

    static bool IsDescendantOrSelf(SvgNode node, SvgNode ancestor)
    {
        SvgNode? current = node;
        while (current != null)
        {
            if (object.ReferenceEquals(current, ancestor)) return true;
            current = current.Parent;
        }
        return false;
    }

    static SvgComputedStyle Failure(SvgNode? node, string message)
    {
        SvgComputedStyle result = new SvgComputedStyle(node, null);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, message, node == null ? null : node.SourceSpan));
        return result;
    }

    static void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> target, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
}
