using System;
using System.Threading;

namespace html;

/// <summary>Classifies, finds, and resolves URL-bearing attributes in an HTML tree.</summary>
internal static class HtmlDomUrls
{
    /// <summary>Classifies the URL grammar associated with an element-and-attribute pair.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="attribute">The target HTML attribute.</param>
    /// <returns>The single-URL, URL-list, <c>srcset</c>, or refresh grammar; <see cref="HtmlUrlAttributeKind.None"/> when the attribute is not URL-bearing in that element context.</returns>
    public static HtmlUrlAttributeKind GetAttributeKind(HtmlNode element,
        HtmlAttribute attribute)
    {
        ValidateElementAndAttribute(element, attribute);
        if (element.GetKindStorage() != HtmlNodeKind.Element)
        {
            return HtmlUrlAttributeKind.None;
        }

        string attributeNamespace = attribute.GetNamespaceUri();
        string attributeName = attribute.GetLocalName();
        HtmlNamespace elementNamespace = element.GetNamespaceStorage();
        if (attributeNamespace
            == HtmlNamespaceData.GetUri(HtmlNamespace.XLink))
        {
            return elementNamespace != HtmlNamespace.Html
                    && attributeName == "href"
                ? HtmlUrlAttributeKind.Single
                : HtmlUrlAttributeKind.None;
        }

        if (attributeNamespace.Length != 0)
        {
            return HtmlUrlAttributeKind.None;
        }

        if (elementNamespace == HtmlNamespace.Svg
            || elementNamespace == HtmlNamespace.MathMl)
        {
            return attributeName == "href"
                ? HtmlUrlAttributeKind.Single
                : HtmlUrlAttributeKind.None;
        }

        if (elementNamespace != HtmlNamespace.Html)
        {
            return HtmlUrlAttributeKind.None;
        }

        return GetHtmlAttributeKind(element, attributeName);
    }

    /// <summary>Classifies a no-namespace attribute name in an element context.</summary>
    /// <param name="element">The target element.</param>
    /// <param name="attributeName">The no-namespace attribute name.</param>
    /// <returns>The URL grammar used by the named attribute.</returns>
    public static HtmlUrlAttributeKind GetNamedAttributeKind(HtmlNode element,
        string attributeName)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element)
        {
            return HtmlUrlAttributeKind.None;
        }

        HtmlNamespace elementNamespace = element.GetNamespaceStorage();
        if (elementNamespace == HtmlNamespace.Html)
        {
            return GetHtmlAttributeKind(element, attributeName);
        }

        return (elementNamespace == HtmlNamespace.Svg
                || elementNamespace == HtmlNamespace.MathMl)
                && attributeName == "href"
            ? HtmlUrlAttributeKind.Single : HtmlUrlAttributeKind.None;
    }

    /// <summary>Finds the first HTML <c>base</c> element with an <c>href</c> attribute in document order.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <returns>The base element. Returns null when the document contains no qualifying element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlNode? GetBaseElement(HtmlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return FindBaseElement(document,
            HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Resolves the document's first qualifying HTML <c>base</c> element against its fallback URL.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <returns>The normalized effective base URL and diagnostics; the fallback document URL is used when no qualifying base element exists.</returns>
    public static HtmlUrlResult GetEffectiveBaseUrl(HtmlDocument document)
    {
        return GetEffectiveBaseUrl(document, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Resolves the document's first qualifying HTML <c>base</c> element against its fallback URL within explicit limits.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The normalized effective base URL and diagnostics; the fallback document URL is used when no qualifying base element exists.</returns>
    public static HtmlUrlResult GetEffectiveBaseUrl(HtmlDocument document,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateDocumentAndLimits(document, limits);
        cancellationToken.ThrowIfCancellationRequested();
        string fallback = GetFallbackBaseValue(document);
        HtmlNode? baseElement = FindBaseElement(document,
            limits.GetMaximumVisits());
        if (baseElement == null)
        {
            return HtmlUrls.Normalize(fallback, limits, cancellationToken);
        }

        string? href = baseElement.GetAttribute("href");
        if (href == null)
        {
            return HtmlUrls.Normalize(fallback, limits, cancellationToken);
        }

        HtmlUrlResult candidate = HtmlUrls.Resolve(
            href, fallback, limits,
            cancellationToken);
        if (candidate.WasLimitExceeded())
        {
            return candidate;
        }

        if (candidate.IsSuccess()
            && candidate.GetScheme() != "data"
            && candidate.GetScheme() != "javascript")
        {
            return candidate;
        }

        return HtmlUrls.Normalize(fallback, limits, cancellationToken);
    }

    /// <summary>Resolves an element attribute against the document base URL.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="attribute">The target HTML attribute.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    public static HtmlUrlResult ResolveAttribute(HtmlNode element,
        HtmlAttribute attribute)
    {
        return ResolveAttribute(element, attribute,
            HtmlLimits.CreateDefault(), CancellationToken.None);
    }

    /// <summary>Resolves an element attribute against the document base URL.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="attribute">The target HTML attribute.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlResult ResolveAttribute(HtmlNode element,
        HtmlAttribute attribute, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateElementAndAttribute(element, attribute);
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        if (GetAttributeKind(element, attribute)
            != HtmlUrlAttributeKind.Single)
        {
            return CreateInvalidResult(
                "The attribute does not contain one URL.");
        }

        HtmlUrlSchemeResult attributeScheme = HtmlUrls.ExtractScheme(
            attribute.GetValue(), limits, cancellationToken);
        if (!attributeScheme.IsComplete() || attributeScheme.HasScheme())
        {
            return HtmlUrls.Resolve(attribute.GetValue(), "", limits,
                cancellationToken);
        }

        HtmlDocument document = element.GetOwnerDocumentStorage();
        HtmlUrlResult baseResult;
        if (IsHtmlElement(element, "base")
            && attribute.GetNamespaceUri().Length == 0
            && EqualsHtmlName(attribute.GetLocalName(), "href"))
        {
            baseResult = GetFallbackBaseUrl(document, limits,
                cancellationToken);
        }
        else
        {
            baseResult = GetEffectiveBaseUrl(document, limits,
                cancellationToken);
        }

        if (!baseResult.IsSuccess())
        {
            return baseResult;
        }

        return HtmlUrls.Resolve(attribute.GetValue(), baseResult.GetUrl(),
            limits, cancellationToken);
    }

    /// <summary>Resolves an element attribute against the document base URL.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    public static HtmlUrlResult ResolveAttribute(HtmlNode element,
        string attributeName)
    {
        return ResolveAttribute(element, attributeName,
            HtmlLimits.CreateDefault(), CancellationToken.None);
    }

    /// <summary>Resolves an element attribute against the document base URL.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlResult ResolveAttribute(HtmlNode element,
        string attributeName, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }

        HtmlAttribute? attribute = FindAttribute(element, attributeName);
        return attribute == null
            ? CreateInvalidResult("The URL attribute is absent.")
            : ResolveAttribute(element, attribute, limits,
                cancellationToken);
    }

    /// <summary>Resolves an element attribute against the document base URL.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name="localName">The namespace-local name.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    public static HtmlUrlResult ResolveAttribute(HtmlNode element,
        string namespaceUri, string localName)
    {
        return ResolveAttribute(element, namespaceUri, localName,
            HtmlLimits.CreateDefault(), CancellationToken.None);
    }

    /// <summary>Resolves an element attribute against the document base URL.</summary>
    /// <param name="element">The target HTML element.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name="localName">The namespace-local name.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlResult ResolveAttribute(HtmlNode element,
        string namespaceUri, string localName, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        HtmlAttribute? attribute = FindAttribute(element, namespaceUri,
            localName);
        return attribute == null
            ? CreateInvalidResult("The URL attribute is absent.")
            : ResolveAttribute(element, attribute, limits,
                cancellationToken);
    }

    /// <summary>Finds HTML, SVG, and MathML link elements with URL-bearing attributes in document order.</summary>
    /// <param name="root">The root node that bounds the operation.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public static HtmlNode[] FindLinks(HtmlNode root)
    {
        return FindLinks(root, HtmlTreeSearch.GetDefaultMaximumResults(),
            HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds HTML, SVG, and MathML link elements with URL-bearing attributes in document order.</summary>
    /// <param name="root">The root node that bounds the operation.</param>
    /// <param name="maximumResults">The maximum number of matching entries to return.</param>
    /// <param name="maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlNode[] FindLinks(HtmlNode root, int maximumResults,
        int maximumVisits)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        return root.FindAllWithLimits(node => IsLinkElement(node), maximumResults,
            maximumVisits);
    }

    /// <summary>Finds media elements that contain a single-URL or <c>srcset</c> attribute in document order.</summary>
    /// <param name="root">The root node that bounds the operation.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public static HtmlNode[] FindMedia(HtmlNode root)
    {
        return FindMedia(root, HtmlTreeSearch.GetDefaultMaximumResults(),
            HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds media elements that contain a single-URL or <c>srcset</c> attribute in document order.</summary>
    /// <param name="root">The root node that bounds the operation.</param>
    /// <param name="maximumResults">The maximum number of matching entries to return.</param>
    /// <param name="maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlNode[] FindMedia(HtmlNode root, int maximumResults,
        int maximumVisits)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        return root.FindAllWithLimits(node => IsMediaElement(node), maximumResults,
            maximumVisits);
    }

    internal static HtmlUrlAttributeKind GetHtmlAttributeKind(HtmlNode element,
        string attributeName)
    {
        string name = FoldHtmlName(attributeName);
        if (name == "itemid")
        {
            return HtmlUrlAttributeKind.Single;
        }

        if (name == "itemtype")
        {
            return HtmlUrlAttributeKind.SpaceSeparated;
        }

        if (name == "archive"
            && IsOneOf(element.GetLocalNameStorage(), "applet", "object",
                "", ""))
        {
            return HtmlUrlAttributeKind.SpaceSeparated;
        }

        string elementName = element.GetLocalNameStorage();
        if (name == "href")
        {
            return IsOneOf(elementName, "a", "area", "base", "link")
                ? HtmlUrlAttributeKind.Single
                : HtmlUrlAttributeKind.None;
        }

        if (name == "ping")
        {
            return IsOneOf(elementName, "a", "area", "", "")
                ? HtmlUrlAttributeKind.SpaceSeparated
                : HtmlUrlAttributeKind.None;
        }

        if (name == "srcset")
        {
            return IsOneOf(elementName, "img", "source", "", "")
                ? HtmlUrlAttributeKind.SourceSet
                : HtmlUrlAttributeKind.None;
        }

        if (name == "imagesrcset")
        {
            return elementName == "link"
                ? HtmlUrlAttributeKind.SourceSet
                : HtmlUrlAttributeKind.None;
        }

        if (name == "content" && elementName == "meta"
            && IsRefreshPragma(element))
        {
            return HtmlUrlAttributeKind.Refresh;
        }

        if (name == "background"
            && IsOneOf(elementName, "body", "table", "td", "th")
            || name == "background" && elementName == "tr")
        {
            return HtmlUrlAttributeKind.Single;
        }

        if (name == "action" && elementName == "form"
            || name == "formaction"
                && IsOneOf(elementName, "button", "input", "", "")
            || name == "cite"
                && IsOneOf(elementName, "blockquote", "del", "ins", "q")
            || name == "data" && elementName == "object"
            || name == "poster" && elementName == "video"
            || name == "manifest" && elementName == "html"
            || name == "profile" && elementName == "head"
            || name == "codebase"
                && IsOneOf(elementName, "applet", "object", "", "")
            || name == "classid" && elementName == "object"
            || name == "code" && elementName == "applet"
            || name == "longdesc"
                && IsOneOf(elementName, "iframe", "img", "", "")
            || name == "dynsrc" && elementName == "img"
            || name == "lowsrc" && elementName == "img"
            || name == "src" && IsHtmlSourceElement(elementName))
        {
            return HtmlUrlAttributeKind.Single;
        }

        return HtmlUrlAttributeKind.None;
    }

    internal static HtmlNode? FindBaseElement(HtmlDocument document,
        int maximumVisits)
    {
        return document.GetRootStorage().FindFirstWithLimit(node =>
            IsHtmlElement(node, "base") && node.HasAttribute("href"),
            maximumVisits);
    }

    internal static bool IsHtmlSourceElement(string name)
    {
        return name == "audio" || name == "embed" || name == "iframe"
            || name == "img" || name == "input" || name == "script"
            || name == "source" || name == "track" || name == "video";
    }

    internal static bool IsLinkElement(HtmlNode node)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string name = node.GetLocalNameStorage();
        if (namespaceKind == HtmlNamespace.Html)
        {
            return IsOneOf(name, "a", "area", "link", "")
                && FindAttribute(node, "href") != null;
        }

        return (namespaceKind == HtmlNamespace.Svg
                || namespaceKind == HtmlNamespace.MathMl)
            && name == "a" && HasSingleUrlAttribute(node);
    }

    internal static bool IsMediaElement(HtmlNode node)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string name = node.GetLocalNameStorage();
        if (namespaceKind == HtmlNamespace.Svg)
        {
            return name == "image" && HasSingleUrlAttribute(node);
        }

        if (namespaceKind != HtmlNamespace.Html
            || !IsOneOf(name, "audio", "embed", "iframe", "img")
                && !IsOneOf(name, "input", "object", "source", "track")
                && name != "video")
        {
            return false;
        }

        HtmlAttribute[] attributes = node.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlUrlAttributeKind kind = GetAttributeKind(node,
                attributes[index]);
            if (kind == HtmlUrlAttributeKind.Single
                || kind == HtmlUrlAttributeKind.SourceSet)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasSingleUrlAttribute(HtmlNode element)
    {
        HtmlAttribute[] attributes = element.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            if (GetAttributeKind(element, attributes[index])
                == HtmlUrlAttributeKind.Single)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static HtmlUrlResult GetFallbackBaseUrl(HtmlDocument document,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlUrls.Normalize(GetFallbackBaseValue(document), limits,
            cancellationToken);
    }

    internal static string GetFallbackBaseValue(HtmlDocument document)
    {
        string result = document.GetBaseUrlStorage();
        return result.Length == 0 ? document.GetUrlStorage() : result;
    }

    internal static HtmlAttribute? FindAttribute(HtmlNode element, string name)
    {
        HtmlAttribute[] attributes = element.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute current = attributes[index];
            bool matches = element.GetNamespaceStorage() == HtmlNamespace.Html
                ? EqualsHtmlName(current.GetName(), name)
                : current.GetName() == name;
            if (matches)
            {
                return current;
            }

            index = index + 1;
        }

        return null;
    }

    internal static HtmlAttribute? FindAttribute(HtmlNode element, string namespaceUri,
        string localName)
    {
        HtmlAttribute[] attributes = element.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute current = attributes[index];
            if (current.GetNamespaceUri() == namespaceUri
                && current.GetLocalName() == localName)
            {
                return current;
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool IsRefreshPragma(HtmlNode element)
    {
        string? value = element.GetAttribute("http-equiv");
        if (value == null)
        {
            return false;
        }

        int start = 0;
        while (start < value.Length && Ascii.IsHtmlWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && Ascii.IsHtmlWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return Ascii.EqualsIgnoreCase(value.AsSpan(start, end - start),
            "refresh".AsSpan());
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }

    internal static bool IsOneOf(string value, string first, string second,
        string third, string fourth)
    {
        return value == first || value == second || value == third
            || value == fourth;
    }

    internal static string FoldHtmlName(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static bool EqualsHtmlName(string left, string right)
    {
        return Ascii.EqualsIgnoreCase(left.AsSpan(), right.AsSpan());
    }

    internal static HtmlUrlResult CreateInvalidResult(string message)
    {
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.UrlInvalid, HtmlDiagnosticSeverity.Error,
            message, HtmlSourceSpan.CreateUnknown());
        return new HtmlUrlResult("", "", false, false, false, false,
            new[] { diagnostic });
    }

    internal static void ValidateElementAndAttribute(HtmlNode element,
        HtmlAttribute attribute)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }
    }

    internal static void ValidateDocumentAndLimits(HtmlDocument document,
        HtmlLimits limits)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }
    }
}
