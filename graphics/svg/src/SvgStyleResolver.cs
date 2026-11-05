namespace svg;

internal class SvgStyleResolverInput
{
    internal System.Collections.Generic.List<SvgStyleSheet>? StyleSheets;
    internal SvgStyleSheetCollection? Collection;

    internal SvgStyleResolverInput(System.Collections.Generic.List<SvgStyleSheet>? styleSheets, SvgStyleSheetCollection? collection)
    {
        StyleSheets = styleSheets;
        Collection = collection;
    }
}

internal class SvgStyleCacheEntry
{
    internal SvgNode Node;
    internal long NodeVersion;
    internal SvgComputedStyle? ParentStyle;
    internal SvgComputedStyle Style;
    internal System.Collections.Generic.List<SvgStyleSheet> StyleSheets;
    internal System.Collections.Generic.List<long> StyleSheetVersions;
    internal System.Collections.Generic.List<SvgCollectedStyleSheet> CollectedEntries;
    internal System.Collections.Generic.List<string> CollectedMedia;
    internal SvgStyleSheetCollection? Collection;
    internal int CollectionDiagnosticCount;
    internal bool CollectionValid;
    internal SvgPropertyCatalog Properties;
    internal SvgStyleEnvironment Environment;
    internal long EnvironmentVersion;
    internal string MediaType;
    internal double ViewportWidth;
    internal double ViewportHeight;
    internal double ResolutionDpi;
    internal int ColorBits;
    internal int MonochromeBits;
    internal bool ReducedMotion;
    internal double RootFontSize;
    internal ISvgDynamicSelectorState DynamicState;
    internal long DynamicVersion;
    internal string DynamicLanguage;
    internal SvgNode? Scope;
    internal SvgSelectorNamespaceBindings Namespaces;
    internal System.Collections.Generic.List<string> NamespaceKeys;
    internal System.Collections.Generic.List<string> NamespaceValues;
    internal bool HasDefaultNamespace;
    internal string DefaultNamespace;
    internal long MaxSelectorWork;
    internal bool IncludeUserAgentDefaults;
    internal int MaxPaintFallbacks;
    internal long FontVersion;
    internal long MaxCharacters;
    internal int MaxTokens;
    internal int MaxRules;
    internal int MaxDeclarations;
    internal int MaxSelectorTokens;
    internal int MaxBlockDepth;
    internal int MaxImportDepth;
    internal int MaxVariableExpansionTokens;
    internal long AccessOrder;

    internal SvgStyleCacheEntry(SvgNode node, SvgComputedStyle? parent, SvgComputedStyle style, SvgStyleResolverInput input, SvgStyleResolverOptions options, long accessOrder)
    {
        Node = node;
        NodeVersion = node.Version;
        ParentStyle = parent;
        Style = style;
        StyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        StyleSheetVersions = new System.Collections.Generic.List<long>();
        CollectedEntries = new System.Collections.Generic.List<SvgCollectedStyleSheet>();
        CollectedMedia = new System.Collections.Generic.List<string>();
        Collection = input.Collection;
        CollectionDiagnosticCount = input.Collection == null ? 0 : input.Collection.Diagnostics.Count;
        CollectionValid = input.Collection == null || input.Collection.IsValid;
        CaptureSheets(input);
        Properties = options.Properties;
        Environment = options.StyleEnvironment;
        EnvironmentVersion = Environment.Version;
        MediaType = Environment.MediaType;
        ViewportWidth = Environment.ViewportWidth;
        ViewportHeight = Environment.ViewportHeight;
        ResolutionDpi = Environment.ResolutionDpi;
        ColorBits = Environment.ColorBits;
        MonochromeBits = Environment.MonochromeBits;
        ReducedMotion = Environment.ReducedMotion;
        RootFontSize = Environment.RootFontSize;
        DynamicState = options.SelectorOptions.DynamicState;
        DynamicVersion = DynamicState.GetVersion();
        DynamicLanguage = DynamicState.GetLanguage();
        Scope = options.SelectorOptions.Scope;
        Namespaces = options.SelectorOptions.Namespaces;
        NamespaceKeys = new System.Collections.Generic.List<string>();
        NamespaceValues = new System.Collections.Generic.List<string>();
        CaptureNamespaces(Namespaces);
        HasDefaultNamespace = Namespaces.HasDefaultNamespace;
        DefaultNamespace = Namespaces.DefaultNamespace;
        MaxSelectorWork = options.SelectorOptions.MaxWork;
        IncludeUserAgentDefaults = options.IncludeUserAgentDefaults;
        MaxPaintFallbacks = options.MaxPaintFallbacks;
        FontVersion = options.FontVersion;
        MaxCharacters = options.Limits.MaxCharacters;
        MaxTokens = options.Limits.MaxTokens;
        MaxRules = options.Limits.MaxRules;
        MaxDeclarations = options.Limits.MaxDeclarations;
        MaxSelectorTokens = options.Limits.MaxSelectorTokens;
        MaxBlockDepth = options.Limits.MaxBlockDepth;
        MaxImportDepth = options.Limits.MaxImportDepth;
        MaxVariableExpansionTokens = options.Limits.MaxVariableExpansionTokens;
        AccessOrder = accessOrder;
    }

    internal bool Matches(SvgComputedStyle? parent, SvgStyleResolverInput input, SvgStyleResolverOptions options)
    {
        if (Node.Version != NodeVersion || !object.ReferenceEquals(ParentStyle, parent) || !object.ReferenceEquals(Properties, options.Properties)) return false;
        if (!MatchesInput(input) || !MatchesEnvironment(options.StyleEnvironment) || !MatchesSelector(options.SelectorOptions)) return false;
        if (IncludeUserAgentDefaults != options.IncludeUserAgentDefaults || MaxPaintFallbacks != options.MaxPaintFallbacks || FontVersion != options.FontVersion) return false;
        return MatchesLimits(options.Limits);
    }

    internal bool ContainsStyleSheet(SvgStyleSheet styleSheet)
    {
        int index = 0;
        while (index < StyleSheets.Count)
        {
            if (object.ReferenceEquals(StyleSheets[index], styleSheet)) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool DependsOn(string customPropertyName)
    {
        if (Style.CustomProperties.Get(customPropertyName) != null) return true;
        int valueIndex = 0;
        while (valueIndex < Style.Values.Count)
        {
            int dependencyIndex = 0;
            while (dependencyIndex < Style.Values[valueIndex].Dependencies.Count)
            {
                if (Style.Values[valueIndex].Dependencies[dependencyIndex] == customPropertyName) return true;
                dependencyIndex = dependencyIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        return false;
    }

    void CaptureSheets(SvgStyleResolverInput input)
    {
        if (input.Collection == null)
        {
            int index = 0;
            while (index < (input.StyleSheets ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
            {
                StyleSheets.Add(input.StyleSheets[index]);
                StyleSheetVersions.Add(input.StyleSheets[index].Version);
                index = index + 1;
            }
            return;
        }
        int collectedIndex = 0;
        while (collectedIndex < input.Collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet collected = input.Collection.StyleSheets[collectedIndex];
            CollectedEntries.Add(collected);
            CollectedMedia.Add(collected.Media);
            StyleSheets.Add(collected.StyleSheet);
            StyleSheetVersions.Add(collected.StyleSheet.Version);
            collectedIndex = collectedIndex + 1;
        }
    }

    void CaptureNamespaces(SvgSelectorNamespaceBindings namespaces)
    {
        foreach (System.Collections.Generic.KeyValuePair<string, string> item in namespaces.NamedNamespaces)
        {
            NamespaceKeys.Add(item.Key);
            NamespaceValues.Add(item.Value);
        }
    }

    bool MatchesInput(SvgStyleResolverInput input)
    {
        if (input.Collection == null != (Collection == null)) return false;
        if (input.Collection == null)
        {
            if ((input.StyleSheets ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count != StyleSheets.Count) return false;
            return MatchesSheetVersions(input.StyleSheets);
        }
        if (!object.ReferenceEquals(input.Collection, Collection) || input.Collection.StyleSheets.Count != CollectedEntries.Count || input.Collection.Diagnostics.Count != CollectionDiagnosticCount || input.Collection.IsValid != CollectionValid) return false;
        int index = 0;
        while (index < CollectedEntries.Count)
        {
            SvgCollectedStyleSheet current = input.Collection.StyleSheets[index];
            if (!object.ReferenceEquals(current, CollectedEntries[index]) || current.Media != CollectedMedia[index] || !object.ReferenceEquals(current.StyleSheet, StyleSheets[index]) || current.StyleSheet.Version != StyleSheetVersions[index]) return false;
            index = index + 1;
        }
        return true;
    }

    bool MatchesSheetVersions(System.Collections.Generic.List<SvgStyleSheet> sheets)
    {
        if (sheets.Count != StyleSheets.Count) return false;
        int index = 0;
        while (index < sheets.Count)
        {
            if (!object.ReferenceEquals(sheets[index], StyleSheets[index]) || sheets[index].Version != StyleSheetVersions[index]) return false;
            index = index + 1;
        }
        return true;
    }

    bool MatchesEnvironment(SvgStyleEnvironment environment)
    {
        return object.ReferenceEquals(Environment, environment) && EnvironmentVersion == environment.Version && MediaType == environment.MediaType && ViewportWidth == environment.ViewportWidth && ViewportHeight == environment.ViewportHeight && ResolutionDpi == environment.ResolutionDpi && ColorBits == environment.ColorBits && MonochromeBits == environment.MonochromeBits && ReducedMotion == environment.ReducedMotion && RootFontSize == environment.RootFontSize;
    }

    bool MatchesSelector(SvgSelectorMatchOptions options)
    {
        if (!object.ReferenceEquals(DynamicState, options.DynamicState) || DynamicVersion != options.DynamicState.GetVersion() || DynamicLanguage != options.DynamicState.GetLanguage()) return false;
        if (!object.ReferenceEquals(Scope, options.Scope) || !object.ReferenceEquals(Namespaces, options.Namespaces) || MaxSelectorWork != options.MaxWork) return false;
        if (HasDefaultNamespace != options.Namespaces.HasDefaultNamespace || DefaultNamespace != options.Namespaces.DefaultNamespace || NamespaceKeys.Count != options.Namespaces.NamedNamespaces.Count) return false;
        int index = 0;
        while (index < NamespaceKeys.Count)
        {
            string key = NamespaceKeys[index];
            if (!options.Namespaces.NamedNamespaces.ContainsKey(key) || options.Namespaces.NamedNamespaces[key] != NamespaceValues[index]) return false;
            index = index + 1;
        }
        return true;
    }

    bool MatchesLimits(SvgCssLimits limits)
    {
        return MaxCharacters == limits.MaxCharacters && MaxTokens == limits.MaxTokens && MaxRules == limits.MaxRules && MaxDeclarations == limits.MaxDeclarations && MaxSelectorTokens == limits.MaxSelectorTokens && MaxBlockDepth == limits.MaxBlockDepth && MaxImportDepth == limits.MaxImportDepth && MaxVariableExpansionTokens == limits.MaxVariableExpansionTokens;
    }
}
