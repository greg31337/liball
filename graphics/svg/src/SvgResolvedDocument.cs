namespace svg;

internal class SvgResolvedCacheEntry
{
    internal SvgResolvedDocument Result;
    internal long AccessOrder;

    internal SvgResolvedCacheEntry(SvgResolvedDocument result, long accessOrder)
    {
        Result = result;
        AccessOrder = accessOrder;
    }
}

internal class SvgResolvedCacheKey
{
    internal SvgDocument Document;
    internal SvgResolveOptions Options;
    internal SvgStyleEnvironment Environment;
    internal long EnvironmentVersion;
    internal string EnvironmentKey;
    internal ISvgDynamicSelectorState DynamicState;
    internal long DynamicVersion;
    internal string DynamicLanguage;
    internal SvgPropertyCatalog Properties;
    internal SvgNode? SelectorScope;
    internal SvgSelectorNamespaceBindings SelectorNamespaces;
    internal string SelectorNamespaceKey;
    internal long MaxSelectorWork;
    internal bool IncludeUserAgentDefaults;
    internal int MaxPaintFallbacks;
    internal SvgCapabilitySet Capabilities;
    internal System.Collections.Generic.List<string> CapabilitySnapshot;
    internal SvgCapabilitySet Extensions;
    internal System.Collections.Generic.List<string> ExtensionSnapshot;
    internal System.Collections.Generic.List<string> LanguageSnapshot;
    internal System.Collections.Generic.List<SvgStyleSheet> CallerStyleSheets;
    internal System.Collections.Generic.List<long> StyleSheetVersions;
    internal string LimitsKey;
    internal string PolicyKey;
    internal double ViewportWidth;
    internal double ViewportHeight;
    internal string FragmentIdentifier;
    internal double AnimationTime;
    internal SvgDiagnosticBehavior DiagnosticBehavior;
    internal int MaxDiagnostics;
    internal bool ImageContentAvailable;
    internal bool ForeignContentCallbackAvailable;
    internal System.Func<SvgHostContentCallbackRequest, SvgHostContentCallbackResult>? HostContentCallback;
    internal SvgIntrinsicSize? ImageIntrinsicSize;
    internal string ImageIntrinsicSizeKey;
    internal SvgDocument? ExternalDocument;
    internal long ExternalDocumentVersion;
    internal ISvgResourceProvider? ResourceProvider;
    internal long FontVersion;

    internal SvgResolvedCacheKey(SvgDocument document, SvgResolveOptions options)
    {
        Document = document;
        Options = options;
        Environment = options.StyleOptions.StyleEnvironment;
        EnvironmentVersion = Environment.Version;
        EnvironmentKey = CreateEnvironmentKey(Environment);
        DynamicState = options.StyleOptions.SelectorOptions.DynamicState;
        DynamicVersion = DynamicState.GetVersion();
        DynamicLanguage = DynamicState.GetLanguage();
        Properties = options.StyleOptions.Properties;
        SelectorScope = options.StyleOptions.SelectorOptions.Scope;
        SelectorNamespaces = options.StyleOptions.SelectorOptions.Namespaces;
        SelectorNamespaceKey = CreateNamespaceKey(SelectorNamespaces);
        MaxSelectorWork = options.StyleOptions.SelectorOptions.MaxWork;
        IncludeUserAgentDefaults = options.StyleOptions.IncludeUserAgentDefaults;
        MaxPaintFallbacks = options.StyleOptions.MaxPaintFallbacks;
        Capabilities = options.Capabilities;
        CapabilitySnapshot = Capabilities.Snapshot();
        Extensions = options.Extensions;
        ExtensionSnapshot = Extensions.Snapshot();
        LanguageSnapshot = CopyStrings(options.LanguagePreferences);
        CallerStyleSheets = options.CallerStyleSheets;
        StyleSheetVersions = SheetVersions(CallerStyleSheets);
        LimitsKey = CreateLimitsKey(options.Limits);
        PolicyKey = CreatePolicyKey(options.ResourcePolicy);
        ViewportWidth = options.ViewportWidth;
        ViewportHeight = options.ViewportHeight;
        FragmentIdentifier = options.FragmentIdentifier;
        AnimationTime = options.AnimationTime;
        DiagnosticBehavior = options.DiagnosticBehavior;
        MaxDiagnostics = options.MaxDiagnostics;
        ImageContentAvailable = options.ImageContentAvailable;
        ForeignContentCallbackAvailable = options.ForeignContentCallbackAvailable;
        HostContentCallback = options.HostContentCallback;
        ImageIntrinsicSize = options.ImageIntrinsicSize;
        ImageIntrinsicSizeKey = CreateIntrinsicSizeKey(ImageIntrinsicSize);
        ExternalDocument = options.ExternalDocument;
        ExternalDocumentVersion = ExternalDocument == null ? 0 : ExternalDocument.Version;
        ResourceProvider = options.ResourceProvider;
        FontVersion = options.StyleOptions.FontVersion;
    }

    internal bool Matches(SvgDocument document, SvgResolveOptions options)
    {
        if (!object.ReferenceEquals(Document, document)) return false;
        if (!object.ReferenceEquals(Environment, options.StyleOptions.StyleEnvironment) || EnvironmentVersion != options.StyleOptions.StyleEnvironment.Version || EnvironmentKey != CreateEnvironmentKey(options.StyleOptions.StyleEnvironment)) return false;
        if (!object.ReferenceEquals(DynamicState, options.StyleOptions.SelectorOptions.DynamicState) || DynamicVersion != DynamicState.GetVersion() || DynamicLanguage != DynamicState.GetLanguage()) return false;
        if (!object.ReferenceEquals(Properties, options.StyleOptions.Properties) || !object.ReferenceEquals(SelectorScope, options.StyleOptions.SelectorOptions.Scope)) return false;
        if (!object.ReferenceEquals(SelectorNamespaces, options.StyleOptions.SelectorOptions.Namespaces) || SelectorNamespaceKey != CreateNamespaceKey(options.StyleOptions.SelectorOptions.Namespaces) || MaxSelectorWork != options.StyleOptions.SelectorOptions.MaxWork) return false;
        if (IncludeUserAgentDefaults != options.StyleOptions.IncludeUserAgentDefaults || MaxPaintFallbacks != options.StyleOptions.MaxPaintFallbacks) return false;
        if (!SameStrings(CapabilitySnapshot, options.Capabilities.Snapshot()) || !SameStrings(ExtensionSnapshot, options.Extensions.Snapshot())) return false;
        if (!SameStrings(LanguageSnapshot, options.LanguagePreferences)) return false;
        if (!SameSheets(CallerStyleSheets, StyleSheetVersions, options.CallerStyleSheets)) return false;
        if (LimitsKey != CreateLimitsKey(options.Limits) || PolicyKey != CreatePolicyKey(options.ResourcePolicy)) return false;
        if (ViewportWidth != options.ViewportWidth || ViewportHeight != options.ViewportHeight || FragmentIdentifier != options.FragmentIdentifier || AnimationTime != options.AnimationTime) return false;
        if (DiagnosticBehavior != options.DiagnosticBehavior || MaxDiagnostics != options.MaxDiagnostics) return false;
        if (ImageContentAvailable != options.ImageContentAvailable || ForeignContentCallbackAvailable != options.ForeignContentCallbackAvailable) return false;
        if (!object.ReferenceEquals(HostContentCallback, options.HostContentCallback)) return false;
        if (!object.ReferenceEquals(ImageIntrinsicSize, options.ImageIntrinsicSize) || ImageIntrinsicSizeKey != CreateIntrinsicSizeKey(options.ImageIntrinsicSize)) return false;
        if (!object.ReferenceEquals(ExternalDocument, options.ExternalDocument) || ExternalDocumentVersion != (options.ExternalDocument == null ? 0 : options.ExternalDocument.Version)) return false;
        if (!object.ReferenceEquals(ResourceProvider, options.ResourceProvider) || FontVersion != options.StyleOptions.FontVersion) return false;
        return true;
    }

    internal bool IsLive()
    {
        if (Document == null || Options == null) return false;
        return Matches(Document, Options);
    }

    static System.Collections.Generic.List<string> CopyStrings(System.Collections.Generic.List<string> source)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }
        return result;
    }

    static System.Collections.Generic.List<long> SheetVersions(System.Collections.Generic.List<SvgStyleSheet> source)
    {
        System.Collections.Generic.List<long> result = new System.Collections.Generic.List<long>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index].Version);
            index = index + 1;
        }
        return result;
    }

    static bool SameStrings(System.Collections.Generic.List<string>? left, System.Collections.Generic.List<string>? right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        int index = 0;
        while (index < left.Count)
        {
            if (!string.Equals(left[index], right[index], System.StringComparison.Ordinal)) return false;
            index = index + 1;
        }
        return true;
    }

    static bool SameSheets(System.Collections.Generic.List<SvgStyleSheet>? captured, System.Collections.Generic.List<long>? versions, System.Collections.Generic.List<SvgStyleSheet>? current)
    {
        if (captured == null || versions == null || current == null || captured.Count != current.Count || versions.Count != current.Count) return false;
        int index = 0;
        while (index < current.Count)
        {
            if (!object.ReferenceEquals(captured[index], current[index]) || current[index] == null || versions[index] != current[index].Version) return false;
            index = index + 1;
        }
        return true;
    }

    static string CreatePolicyKey(SvgResourcePolicy? policy)
    {
        if (policy == null) return "";
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append(policy.AllowCallerResolvedRootUri.ToString()).Append(':');
        result.Append(policy.AllowImportedStyleSheets.ToString()).Append(':');
        result.Append(policy.AllowRelativeImportUris.ToString()).Append(':');
        result.Append(policy.AllowDataImportUris.ToString()).Append(':');
        result.Append(policy.AllowFileImportUris.ToString()).Append(':');
        result.Append(policy.AllowCrossOriginImports.ToString()).Append(':');
        result.Append(policy.AllowSameDocumentUris.ToString()).Append(':');
        result.Append(policy.AllowRelativeUris.ToString()).Append(':');
        result.Append(policy.AllowDataUris.ToString()).Append(':');
        result.Append(policy.AllowFileUris.ToString()).Append(':');
        result.Append(policy.AllowRemoteFileUris.ToString()).Append(':');
        result.Append(policy.AllowHttpUris.ToString()).Append(':');
        result.Append(policy.AllowHttpsUris.ToString()).Append(':');
        result.Append(policy.AllowCrossOriginUris.ToString()).Append(':');
        result.Append(policy.AllowRedirects.ToString()).Append(':');
        result.Append(policy.AllowCrossOriginRedirects.ToString()).Append(':');
        result.Append(policy.AllowCustomSchemes.ToString()).Append(':');
        result.Append(policy.AllowUnknownMediaTypes.ToString());
        AddText(result, policy.FileRootUri);
        AddPolicyValues(result, policy.AllowedSchemes);
        AddPolicyValues(result, policy.AllowedMediaTypes);
        return result.ToString();
    }

    static void AddPolicyValues(System.Text.StringBuilder result, System.Collections.Generic.List<string> values)
    {
        if (values == null)
        {
            AddText(result, "");
            return;
        }
        System.Collections.Generic.List<string> ordered = new System.Collections.Generic.List<string>(values);
        ordered.Sort(System.StringComparer.OrdinalIgnoreCase);
        int index = 0;
        while (index < ordered.Count)
        {
            AddText(result, ordered[index]);
            index = index + 1;
        }
        AddText(result, ordered.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    static string CreateEnvironmentKey(SvgStyleEnvironment? environment)
    {
        if (environment == null) return "";
        return environment.MediaType + ":" + environment.ViewportWidth.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + environment.ViewportHeight.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + environment.ResolutionDpi.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + environment.ColorBits.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + environment.MonochromeBits.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + environment.ReducedMotion.ToString() + ":" + environment.RootFontSize.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    static string CreateNamespaceKey(SvgSelectorNamespaceBindings? namespaces)
    {
        if (namespaces == null) return "";
        System.Collections.Generic.List<string> keys = new System.Collections.Generic.List<string>();
        foreach (System.Collections.Generic.KeyValuePair<string, string> item in namespaces.NamedNamespaces) keys.Add(item.Key);
        keys.Sort(System.StringComparer.Ordinal);
        System.Text.StringBuilder value = new System.Text.StringBuilder();
        value.Append(namespaces.HasDefaultNamespace.ToString());
        AddText(value, namespaces.DefaultNamespace);
        int index = 0;
        while (index < keys.Count)
        {
            string key = keys[index];
            AddText(value, key);
            AddText(value, namespaces.NamedNamespaces[key]);
            index = index + 1;
        }
        return value.ToString();
    }

    static string CreateIntrinsicSizeKey(SvgIntrinsicSize? size)
    {
        if (size == null) return "";
        return size.HasWidth.ToString() + ":" + size.Width.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + size.HasHeight.ToString() + ":" + size.Height.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + size.HasAspectRatio.ToString() + ":" + size.AspectRatio.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    static void AddText(System.Text.StringBuilder value, string? text)
    {
        string current = text == null ? "" : text;
        value.Append(current.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        value.Append(':');
        value.Append(current);
    }

    static string CreateLimitsKey(SvgSecurityLimits? limits)
    {
        if (limits == null) return "";
        System.Text.StringBuilder value = new System.Text.StringBuilder();
        AppendInput(value, limits.Input);
        AppendTree(value, limits.Tree);
        AppendCss(value, limits.Css);
        AppendReferences(value, limits.References);
        AppendResources(value, limits.Resources);
        AppendGeometry(value, limits.Geometry);
        AppendFilters(value, limits.Filters);
        return value.ToString();
    }

    static void Add(System.Text.StringBuilder value, long number)
    {
        value.Append(number.ToString(System.Globalization.CultureInfo.InvariantCulture));
        value.Append(':');
    }

    static void AppendInput(System.Text.StringBuilder value, SvgInputLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxInputBytes); Add(value, limits.MaxInputCharacters); Add(value, limits.MaxGzipOutputBytes); Add(value, limits.MaxGzipExpansionRatio); Add(value, limits.MaxXmlTokenCharacters);
    }

    static void AppendTree(System.Text.StringBuilder value, SvgTreeLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxDepth); Add(value, limits.MaxNodes); Add(value, limits.MaxAttributesPerElement); Add(value, limits.MaxTotalAttributes); Add(value, limits.MaxTextCharacters); Add(value, limits.MaxNameCharacters);
    }

    static void AppendCss(System.Text.StringBuilder value, SvgCssLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxCharacters); Add(value, limits.MaxTokens); Add(value, limits.MaxRules); Add(value, limits.MaxDeclarations); Add(value, limits.MaxSelectorTokens); Add(value, limits.MaxBlockDepth); Add(value, limits.MaxImportDepth); Add(value, limits.MaxVariableExpansionTokens);
    }

    static void AppendReferences(System.Text.StringBuilder value, SvgReferenceLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxDepth); Add(value, limits.MaxCount); Add(value, limits.MaxUseInstances); Add(value, limits.MaxPaintServerDepth); Add(value, limits.MaxEffectDepth); Add(value, limits.MaxConditionalChecks);
    }

    static void AppendResources(System.Text.StringBuilder value, SvgResourceLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxCount); Add(value, limits.MaxBytesPerResource); Add(value, limits.MaxTotalBytes); Add(value, limits.MaxDataUrlBytes); Add(value, limits.MaxRedirects); Add(value, limits.MaxUriCharacters); Add(value, limits.MaxCallbackMilliseconds);
    }

    static void AppendGeometry(System.Text.StringBuilder value, SvgGeometryLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxPathSegments); Add(value, limits.MaxPoints); Add(value, limits.MaxFlattenedSegments); Add(value, limits.MaxSubdivisionDepth); Add(value, limits.MaxIntersections); Add(value, limits.MaxBooleanEdges); Add(value, limits.MaxHitTestNodes);
    }

    static void AppendFilters(System.Text.StringBuilder value, SvgFilterLimits? limits)
    {
        if (limits == null) return;
        Add(value, limits.MaxPrimitives); Add(value, limits.MaxAreaPixels); Add(value, limits.MaxKernelElements); Add(value, limits.MaxBlurStandardDeviation); Add(value, limits.MaxTurbulenceOctaves); Add(value, limits.MaxOffscreenSurfaces);
    }
}

internal class SvgResolvedBuildState
{
    internal SvgResolvedDocument Document;
    internal SvgResolveOptions Options;
    internal SvgDiagnosticBag Diagnostics;
    internal SvgStyleSheetCollection? StyleSheets;
    internal SvgComputedStyleTree? Styles;
    internal SvgStyleResolverOptions? StyleOptions;
    internal System.Collections.Generic.List<SvgStyleSheet> ActiveStyleSheets;
    internal SvgTypedAttributeAccessor? Attributes;
    internal SvgConditionalProcessingOptions? ConditionalOptions;
    internal SvgNestedDocumentContext? NestedResources;
    internal int NodeCount;
    internal bool LimitReached;
    internal bool Cancelled;

    internal SvgResolvedBuildState(SvgResolvedDocument document, SvgResolveOptions options, SvgDiagnosticBag diagnostics)
    {
        Document = document;
        Options = options;
        Diagnostics = diagnostics;
        StyleSheets = null;
        Styles = null;
        StyleOptions = null;
        ActiveStyleSheets = new System.Collections.Generic.List<SvgStyleSheet>();
        Attributes = null;
        ConditionalOptions = null;
        NestedResources = null;
        NodeCount = 0;
        LimitReached = false;
        Cancelled = false;
    }
}
