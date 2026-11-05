namespace svg;

/// <summary>Describes one normalized resource-policy decision request.</summary>
public class SvgResourcePolicyRequest
{
    /// <summary>The URI text authored by the document or supplied by the caller.</summary>
    public string AuthoredUri;
    /// <summary>The URI providing relative and origin context, or an empty string.</summary>
    public string BaseUri;
    /// <summary>The resolved target URI.</summary>
    public string ResolvedUri;
    /// <summary>The requested resource purpose.</summary>
    public SvgResourceKind Kind;
    /// <summary>The declared or detected media type, or an empty string before acquisition.</summary>
    public string MediaType;
    /// <summary>Whether this decision follows a redirect response.</summary>
    public bool IsRedirect;
    /// <summary>The one-based redirect count after applying this target.</summary>
    public int RedirectCount;
    /// <summary>The maximum permitted redirect count.</summary>
    public int MaxRedirects;
    /// <summary>The source span that requested the resource, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;

    /// <summary>Creates a resource-policy request.</summary>
    /// <param name="authoredUri">The authored URI text.</param>
    /// <param name="baseUri">The base URI or an empty string.</param>
    /// <param name="resolvedUri">The resolved target URI.</param>
    /// <param name="kind">The resource purpose.</param>
    public SvgResourcePolicyRequest(string authoredUri, string baseUri, string resolvedUri, SvgResourceKind kind)
    {
        if (authoredUri == null || baseUri == null || resolvedUri == null) throw new System.ArgumentNullException("authoredUri");
        AuthoredUri = authoredUri;
        BaseUri = baseUri;
        ResolvedUri = resolvedUri;
        Kind = kind;
        MediaType = "";
        IsRedirect = false;
        RedirectCount = 0;
        MaxRedirects = 5;
        Source = null;
    }
}

/// <summary>Contains an explicit resource-policy outcome and denial diagnostic.</summary>
public class SvgResourcePolicyDecision
{
    /// <summary>Whether the request was allowed.</summary>
    public bool Allowed;
    /// <summary>The stable denial reason.</summary>
    public SvgResourceDenialReason Reason;
    /// <summary>The denial diagnostic, or <c>null</c> for an allowed request.</summary>
    public SvgDiagnostic? Diagnostic;
    /// <summary>The normalized target URI.</summary>
    public string NormalizedUri;
    /// <summary>The lowercase target scheme, or an empty string.</summary>
    public string Scheme;
    /// <summary>The normalized target origin, or an empty string.</summary>
    public string Origin;
    /// <summary>Whether both HTTP origins were present and equal.</summary>
    public bool IsSameOrigin;
    /// <summary>Whether the target cache identity equals the base resource identity.</summary>
    public bool IsSameDocument;
    /// <summary>Whether a file target was inside the configured root.</summary>
    public bool IsWithinFileRoot;
    /// <summary>The redirect count inspected by the decision.</summary>
    public int RedirectCount;

    internal SvgResourcePolicyDecision()
    {
        Allowed = false;
        Reason = SvgResourceDenialReason.InvalidRequest;
        Diagnostic = null;
        NormalizedUri = "";
        Scheme = "";
        Origin = "";
        IsSameOrigin = false;
        IsSameDocument = false;
        IsWithinFileRoot = false;
        RedirectCount = 0;
    }
}

/// <summary>Defines policy for root and nested caller-mediated resources.</summary>
public class SvgResourcePolicy
{
    /// <summary>Whether an explicitly requested root URI may be sent to the caller callback.</summary>
    public bool AllowCallerResolvedRootUri;
    /// <summary>Whether CSS import resources may be sent to the caller callback.</summary>
    public bool AllowImportedStyleSheets;
    /// <summary>Whether a relative CSS import URI may be resolved against its containing stylesheet.</summary>
    public bool AllowRelativeImportUris;
    /// <summary>Whether a CSS import may use the data scheme.</summary>
    public bool AllowDataImportUris;
    /// <summary>Whether a CSS import may use the file scheme.</summary>
    public bool AllowFileImportUris;
    /// <summary>Whether an absolute CSS import may cross an available HTTP origin boundary.</summary>
    public bool AllowCrossOriginImports;
    /// <summary>Whether same-document resource references are allowed.</summary>
    public bool AllowSameDocumentUris;
    /// <summary>Whether non-stylesheet relative resource references are allowed.</summary>
    public bool AllowRelativeUris;
    /// <summary>Whether non-stylesheet data URLs are allowed.</summary>
    public bool AllowDataUris;
    /// <summary>Whether non-stylesheet file URLs are allowed.</summary>
    public bool AllowFileUris;
    /// <summary>Whether file URLs with non-local authorities are allowed.</summary>
    public bool AllowRemoteFileUris;
    /// <summary>Whether HTTP resource URLs are allowed.</summary>
    public bool AllowHttpUris;
    /// <summary>Whether HTTPS resource URLs are allowed.</summary>
    public bool AllowHttpsUris;
    /// <summary>Whether non-stylesheet HTTP resources may cross an origin boundary.</summary>
    public bool AllowCrossOriginUris;
    /// <summary>Whether redirect targets may be evaluated.</summary>
    public bool AllowRedirects;
    /// <summary>Whether redirect targets may cross an HTTP origin boundary.</summary>
    public bool AllowCrossOriginRedirects;
    /// <summary>Whether schemes other than the built-in schemes are allowed.</summary>
    public bool AllowCustomSchemes;
    /// <summary>Whether unrecognized media types are allowed.</summary>
    public bool AllowUnknownMediaTypes;
    /// <summary>An optional local or remote file URI root, or an absolute filesystem root.</summary>
    public string FileRootUri;
    /// <summary>Additional lowercase schemes allowed when custom schemes are otherwise disabled.</summary>
    public System.Collections.Generic.List<string> AllowedSchemes;
    /// <summary>Additional exact media types allowed for any resource kind.</summary>
    public System.Collections.Generic.List<string> AllowedMediaTypes;

    /// <summary>Creates secure caller-mediated resource defaults.</summary>
    public SvgResourcePolicy()
    {
        AllowCallerResolvedRootUri = true;
        AllowImportedStyleSheets = true;
        AllowRelativeImportUris = true;
        AllowDataImportUris = false;
        AllowFileImportUris = false;
        AllowCrossOriginImports = false;
        AllowSameDocumentUris = true;
        AllowRelativeUris = true;
        AllowDataUris = true;
        AllowFileUris = false;
        AllowRemoteFileUris = false;
        AllowHttpUris = true;
        AllowHttpsUris = true;
        AllowCrossOriginUris = false;
        AllowRedirects = true;
        AllowCrossOriginRedirects = false;
        AllowCustomSchemes = false;
        AllowUnknownMediaTypes = false;
        FileRootUri = "";
        AllowedSchemes = new System.Collections.Generic.List<string>();
        AllowedMediaTypes = new System.Collections.Generic.List<string>();
    }

    /// <summary>Evaluates one normalized resource request without performing I/O.</summary>
    /// <param name="request">The URI, resource kind, media type, redirect count, and source context.</param>
    /// <returns>An allowed decision or a reason-specific denial diagnostic.</returns>
    internal SvgResourcePolicyDecision Evaluate(SvgResourcePolicyRequest? request)
    {
        if (request == null || request.AuthoredUri == null || request.BaseUri == null || request.ResolvedUri == null || request.MediaType == null || (int)request.Kind < 0 || (int)request.Kind > 5 || request.RedirectCount < 0 || request.MaxRedirects < 0 || request.IsRedirect && request.RedirectCount == 0 || FileRootUri == null || AllowedSchemes == null || AllowedMediaTypes == null)
        {
            return Deny(SvgResourceDenialReason.InvalidRequest, request, null);
        }
        SvgUriResult? baseResult = request.BaseUri.Length == 0 ? null : new SvgUri().NormalizeWithValue(request.BaseUri);
        if (baseResult != null && !baseResult.Success) return Deny(SvgResourceDenialReason.InvalidRequest, request, null);
        SvgUriResult target = request.BaseUri.Length == 0 ? new SvgUri().NormalizeWithValue(request.ResolvedUri) : new SvgUri().ResolveWithBaseUriAndReference(request.BaseUri, request.ResolvedUri);
        if (!target.Success) return Deny(SvgResourceDenialReason.InvalidRequest, request, target);
        SvgResourcePolicyDecision context = Context(request, target);
        if (request.Kind == SvgResourceKind.RootDocument && !AllowCallerResolvedRootUri) return DenyWithContext(SvgResourceDenialReason.RootUriDenied, request, context);
        if (request.IsRedirect || request.RedirectCount > 0)
        {
            if (!AllowRedirects) return DenyWithContext(SvgResourceDenialReason.RedirectDenied, request, context);
            if (request.RedirectCount > request.MaxRedirects) return DenyWithContext(SvgResourceDenialReason.RedirectLimitExceeded, request, context);
            if (context.Origin.Length != 0 && !context.IsSameOrigin && !AllowCrossOriginRedirects) return DenyWithContext(SvgResourceDenialReason.CrossOriginRedirectDenied, request, context);
        }
        if (context.IsSameDocument)
        {
            if (!AllowSameDocumentUris) return DenyWithContext(SvgResourceDenialReason.SameDocumentDenied, request, context);
            return Allow(request, context);
        }
        if (request.Kind == SvgResourceKind.StyleSheet && !AllowImportedStyleSheets) return DenyWithContext(SvgResourceDenialReason.StyleSheetDenied, request, context);
        if (IsRelative(request.AuthoredUri))
        {
            bool relativeAllowed = request.Kind == SvgResourceKind.StyleSheet ? AllowRelativeImportUris : AllowRelativeUris;
            if (!relativeAllowed) return DenyWithContext(SvgResourceDenialReason.RelativeUriDenied, request, context);
        }
        SvgResourceDenialReason schemeReason = SchemeDenial(request, target, context);
        if (schemeReason != SvgResourceDenialReason.None) return DenyWithContext(schemeReason, request, context);
        if (context.Origin.Length != 0 && !context.IsSameOrigin && request.Kind != SvgResourceKind.RootDocument)
        {
            bool crossOriginAllowed = request.Kind == SvgResourceKind.StyleSheet ? AllowCrossOriginImports : AllowCrossOriginUris;
            if (!crossOriginAllowed) return DenyWithContext(SvgResourceDenialReason.CrossOriginDenied, request, context);
        }
        if (!MediaTypeAllowed(request.Kind, request.MediaType)) return DenyWithContext(SvgResourceDenialReason.MediaTypeDenied, request, context);
        return Allow(request, context);
    }

    internal SvgResourcePolicyDecision Context(SvgResourcePolicyRequest request, SvgUriResult target)
    {
        SvgResourcePolicyDecision result = new SvgResourcePolicyDecision();
        result.NormalizedUri = target.NormalizedIri;
        result.Scheme = Scheme(target.NormalizedIri);
        result.Origin = Origin(target.NormalizedIri);
        result.RedirectCount = request.RedirectCount;
        SvgUriResult? baseResult = request.BaseUri.Length == 0 ? null : new SvgUri().NormalizeWithValue(request.BaseUri);
        string baseOrigin = baseResult == null || !baseResult.Success ? "" : Origin(baseResult.NormalizedIri);
        result.IsSameOrigin = result.Origin.Length != 0 && baseOrigin.Length != 0 && string.Equals(result.Origin, baseOrigin, System.StringComparison.Ordinal);
        result.IsSameDocument = baseResult != null && baseResult.Success && string.Equals(target.CacheKey, baseResult.CacheKey, System.StringComparison.Ordinal);
        result.IsWithinFileRoot = target.IsFile && FileRootContains(target);
        return result;
    }

    internal SvgResourceDenialReason SchemeDenial(SvgResourcePolicyRequest request, SvgUriResult target, SvgResourcePolicyDecision context)
    {
        string scheme = context.Scheme;
        if (scheme.Length == 0) return SvgResourceDenialReason.None;
        if (scheme == "data")
        {
            bool allowed = request.Kind == SvgResourceKind.StyleSheet ? AllowDataImportUris : AllowDataUris;
            return allowed ? SvgResourceDenialReason.None : SvgResourceDenialReason.DataUriDenied;
        }
        if (scheme == "file")
        {
            bool allowed = request.Kind == SvgResourceKind.StyleSheet ? AllowFileImportUris : AllowFileUris;
            if (!allowed) return SvgResourceDenialReason.FileUriDenied;
            if (!target.IsLocalFile && !AllowRemoteFileUris) return SvgResourceDenialReason.RemoteFileDenied;
            if (FileRootUri.Length != 0 && !context.IsWithinFileRoot) return SvgResourceDenialReason.FileRootDenied;
            return SvgResourceDenialReason.None;
        }
        if (scheme == "http") return AllowHttpUris ? SvgResourceDenialReason.None : SvgResourceDenialReason.SchemeDenied;
        if (scheme == "https") return AllowHttpsUris ? SvgResourceDenialReason.None : SvgResourceDenialReason.SchemeDenied;
        if (request.Kind == SvgResourceKind.RootDocument && AllowCallerResolvedRootUri) return SvgResourceDenialReason.None;
        if (AllowCustomSchemes || ContainsIgnoreCase(AllowedSchemes, scheme)) return SvgResourceDenialReason.None;
        return SvgResourceDenialReason.SchemeDenied;
    }

    internal bool FileRootContains(SvgUriResult target)
    {
        if (FileRootUri.Length == 0) return true;
        SvgUriResult? root = NormalizeFileRoot(FileRootUri);
        if (root == null || !root.Success || !root.IsFile) return false;
        string rootValue = RemoveQuery(root.CacheKey);
        string targetValue = RemoveQuery(target.CacheKey);
        if (string.Equals(rootValue, targetValue, System.StringComparison.Ordinal)) return true;
        if (!rootValue.EndsWith("/", System.StringComparison.Ordinal)) rootValue = rootValue + "/";
        return targetValue.StartsWith(rootValue, System.StringComparison.Ordinal);
    }

    internal static SvgUriResult? NormalizeFileRoot(string value)
    {
        string normalized = value.Replace('\\', '/');
        if (normalized.StartsWith("file:", System.StringComparison.OrdinalIgnoreCase)) return new SvgUri().NormalizeWithValue(normalized);
        if (normalized.StartsWith("/", System.StringComparison.Ordinal)) return new SvgUri().NormalizeWithValue("file://" + normalized);
        if (normalized.Length >= 3 && IsAsciiLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/') return new SvgUri().NormalizeWithValue("file:///" + normalized);
        return null;
    }

    internal bool MediaTypeAllowed(SvgResourceKind kind, string mediaType)
    {
        if (mediaType.Length == 0) return true;
        SvgMediaTypeParseResult parsed = new SvgMediaTypeParser().Parse(mediaType, 64, 8192);
        if (!parsed.Success) return false;
        string value = (parsed.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Type + "/" + parsed.MediaType.Subtype;
        if (ContainsIgnoreCase(AllowedMediaTypes, value)) return true;
        bool kindMatch = false;
        if (kind == SvgResourceKind.RootDocument || kind == SvgResourceKind.Document) kindMatch = IsDocumentMediaType(value);
        else if (kind == SvgResourceKind.StyleSheet) kindMatch = string.Equals(value, SvgMimeTypes.Css, System.StringComparison.OrdinalIgnoreCase);
        else if (kind == SvgResourceKind.Image) kindMatch = IsImageMediaType(value);
        else if (kind == SvgResourceKind.Font) kindMatch = IsFontMediaType(value);
        else if (kind == SvgResourceKind.Other) kindMatch = string.Equals(value, SvgMimeTypes.Binary, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Gzip, System.StringComparison.OrdinalIgnoreCase);
        if (kindMatch) return true;
        if (IsKnownMediaType(value)) return false;
        return AllowUnknownMediaTypes;
    }

    internal static bool IsDocumentMediaType(string value)
    {
        return string.Equals(value, SvgMimeTypes.Svg, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Xml, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.XmlText, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsImageMediaType(string value)
    {
        return string.Equals(value, SvgMimeTypes.Svg, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Png, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Jpeg, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Gif, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.WebP, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Avif, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsFontMediaType(string value)
    {
        return string.Equals(value, SvgMimeTypes.TrueType, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.OpenType, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Woff, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Woff2, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsKnownMediaType(string value)
    {
        return IsDocumentMediaType(value) || IsImageMediaType(value) || IsFontMediaType(value) || string.Equals(value, SvgMimeTypes.Css, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Gzip, System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, SvgMimeTypes.Binary, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static SvgResourcePolicyDecision Allow(SvgResourcePolicyRequest request, SvgResourcePolicyDecision context)
    {
        context.Allowed = true;
        context.Reason = SvgResourceDenialReason.None;
        context.Diagnostic = null;
        context.RedirectCount = request.RedirectCount;
        return context;
    }

    internal static SvgResourcePolicyDecision Deny(SvgResourceDenialReason reason, SvgResourcePolicyRequest? request, SvgUriResult? target)
    {
        SvgResourcePolicyDecision context = new SvgResourcePolicyDecision();
        if (target != null && target.Success)
        {
            context.NormalizedUri = target.NormalizedIri;
            context.Scheme = Scheme(target.NormalizedIri);
            context.Origin = Origin(target.NormalizedIri);
        }
        return DenyWithContext(reason, request, context);
    }

    internal static SvgResourcePolicyDecision DenyWithContext(SvgResourceDenialReason reason, SvgResourcePolicyRequest? request, SvgResourcePolicyDecision context)
    {
        context.Allowed = false;
        context.Reason = reason;
        context.RedirectCount = request == null ? 0 : request.RedirectCount;
        SvgSourceSpan? source = request == null ? null : request.Source;
        context.Diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, reason == SvgResourceDenialReason.InvalidRequest ? SvgDiagnosticCode.InvalidArgument : SvgDiagnosticCode.ResourceDenied, DenialMessage(reason), source);
        return context;
    }

    internal static string DenialMessage(SvgResourceDenialReason reason)
    {
        switch (reason)
        {
            case SvgResourceDenialReason.InvalidRequest:
                return "The resource policy request or URI is invalid.";
            case SvgResourceDenialReason.RootUriDenied:
                return "The resource policy denied caller-resolved root URI loading.";
            case SvgResourceDenialReason.SameDocumentDenied:
                return "The resource policy denied a same-document reference.";
            case SvgResourceDenialReason.StyleSheetDenied:
                return "The resource policy denied an imported stylesheet.";
            case SvgResourceDenialReason.RelativeUriDenied:
                return "The resource policy denied a relative resource URI.";
            case SvgResourceDenialReason.SchemeDenied:
                return "The resource policy denied the target URI scheme.";
            case SvgResourceDenialReason.DataUriDenied:
                return "The resource policy denied a data URL.";
            case SvgResourceDenialReason.FileUriDenied:
                return "The resource policy denied a file URL.";
            case SvgResourceDenialReason.RemoteFileDenied:
                return "The resource policy denied a remote file authority.";
            case SvgResourceDenialReason.FileRootDenied:
                return "The resource policy denied a file URL outside the configured root.";
            case SvgResourceDenialReason.CrossOriginDenied:
                return "The resource policy denied a cross-origin target.";
            case SvgResourceDenialReason.MediaTypeDenied:
                return "The resource policy denied the resource media type.";
            case SvgResourceDenialReason.RedirectDenied:
                return "The resource policy denied redirects.";
            case SvgResourceDenialReason.RedirectLimitExceeded:
                return "The resource policy redirect limit was exceeded.";
            case SvgResourceDenialReason.CrossOriginRedirectDenied:
                return "The resource policy denied a cross-origin redirect.";
            default:
                return "The resource policy denied the request.";
        }
    }

    internal static bool IsRelative(string value)
    {
        SvgUriResult result = new SvgUri().NormalizeWithValue(value);
        return result.Success && !result.IsAbsolute && !result.NormalizedIri.StartsWith("//", System.StringComparison.Ordinal);
    }

    internal static string Scheme(string value)
    {
        int colon = value.IndexOf(':');
        if (colon < 1) return "";
        int slash = value.IndexOf('/');
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (slash >= 0 && slash < colon || query >= 0 && query < colon || fragment >= 0 && fragment < colon) return "";
        return value.Substring(0, colon).ToLowerInvariant();
    }

    internal static string Origin(string value)
    {
        string scheme = Scheme(value);
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

    internal static string RemoveQuery(string value)
    {
        int query = value.IndexOf('?');
        return query < 0 ? value : value.Substring(0, query);
    }

    internal static bool ContainsIgnoreCase(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] != null && string.Equals(values[index], value, System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }
}
