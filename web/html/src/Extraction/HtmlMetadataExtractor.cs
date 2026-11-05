using System;
using System.Threading;

namespace html;

/// <summary>Extracts normalized document, link, social, and Dublin Core metadata from a parsed document.</summary>
internal static class HtmlMetadataExtractor
{
    /// <summary>Extracts and resolves bounded metadata candidates from the parsed document.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <returns>The extracted and precedence-resolved document metadata.</returns>
    public static HtmlMetadata Extract(HtmlDocument document)
    {
        return ExtractWithOptions(document, new HtmlMetadataOptions(),
            CancellationToken.None);
    }

    internal static HtmlMetadata ExtractWithOptions(HtmlDocument document,
        IHtmlMetadataOptions options, CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlMetadataOptions snapshot = options.Copy();
        if (snapshot == null)
        {
            throw new ArgumentException(
                "Metadata options must produce a copy.", "options");
        }

        snapshot.Validate();
        return ExtractCore(document, snapshot, cancellationToken, null);
    }

    internal static HtmlMetadata Extract(HtmlDocument document,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return Extract(document, limits, cancellationToken, null);
    }

    internal static HtmlMetadata Extract(HtmlDocument document,
        HtmlLimits limits, CancellationToken cancellationToken,
        HtmlBudget? budget)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        HtmlMetadataOptions options = new HtmlMetadataOptions();
        options.SetLimits(limits);
        return ExtractCore(document, options, cancellationToken, budget);
    }

    internal static HtmlMetadata ExtractCore(HtmlDocument document,
        HtmlMetadataOptions options, CancellationToken cancellationToken,
        HtmlBudget? budget)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        HtmlMetadataExtractionContext context =
            new HtmlMetadataExtractionContext(document, options,
                cancellationToken, budget);
        context.Extract();
        return context.CreateMetadata();
    }
}

internal class HtmlMetadataExtractionContext
{
    internal const int PrimaryPrecedence = 10;
    internal const int SecondaryPrecedence = 20;
    internal const int TertiaryPrecedence = 30;
    internal const int FallbackPrecedence = 100;

    internal HtmlDocument document;
    internal HtmlMetadataOptions options;
    internal HtmlLimits limits;
    internal CancellationToken cancellationToken;
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal HtmlMetadataCandidateCollector candidates;
    internal HtmlUrlResult? effectiveBaseUrl;
    internal bool baseElementSeen;
    internal bool limitDiagnosticAdded;
    internal int documentOrder;

    internal HtmlMetadataExtractionContext(HtmlDocument documentValue,
        HtmlMetadataOptions optionsValue,
        CancellationToken cancellationTokenValue,
        HtmlBudget? budgetValue)
    {
        this.document = documentValue;
        this.options = optionsValue.Copy();
        this.limits = this.options.GetLimits();
        this.cancellationToken = cancellationTokenValue;
        this.budget = budgetValue == null
            ? new HtmlBudget(this.limits, cancellationTokenValue)
            : budgetValue;
        this.diagnostics = new HtmlDiagnosticCollector(this.budget);
        this.candidates = HtmlMetadataCandidateCollector.Create(this.limits);
        this.effectiveBaseUrl = null;
        this.baseElementSeen = false;
        this.limitDiagnosticAdded = false;
        this.documentOrder = 0;
    }

    internal void Extract()
    {
        if (this.NeedsEffectiveBaseUrl())
        {
            this.InitializeEffectiveBaseUrl();
        }

        this.AddDocumentBaseUrl();
        HtmlNode root = this.document.GetRoot();
        HtmlNode? current = root;
        int depth = 0;
        while (current != null)
        {
            if (!this.TryVisit(depth))
            {
                break;
            }

            if (current.GetKind() == HtmlNodeKind.Element
                && current.GetNamespaceStorage() == HtmlNamespace.Html)
            {
                this.ExtractElement(current);
            }

            this.documentOrder = this.documentOrder + 1;
            HtmlNode? child = current.GetFirstChild();
            if (child != null)
            {
                current = child;
                depth = depth + 1;
                continue;
            }

            while (current != root && current.GetNextSibling() == null)
            {
                HtmlNode? parent = current.GetParent();
                if (parent == null)
                {
                    throw new InvalidOperationException(
                        "Metadata traversal lost its document root.");
                }

                current = parent;
                depth = depth - 1;
            }

            if (current == root)
            {
                current = null;
            }
            else
            {
                current = current.GetNextSibling();
            }
        }
    }

    internal HtmlMetadata CreateMetadata()
    {
        if (this.candidates.GetDroppedCount() > 0)
        {
            this.AddLimitDiagnostic(
                "The metadata candidate retention limit was exceeded.");
        }

        HtmlMetadataCandidateResolution resolution =
            HtmlMetadataResolver.Resolve(this.candidates.GetCandidates());
        return new HtmlMetadata(resolution.GetCandidates(),
            this.candidates.GetTotalCount(),
            this.candidates.GetDroppedCount(),
            resolution.GetDuplicateCount(),
            this.candidates.WereRawValuesTruncated(),
            this.diagnostics.GetDiagnostics(),
            this.diagnostics.GetTotalCount(),
            this.diagnostics.GetDroppedCount());
    }

    internal void ExtractElement(HtmlNode element)
    {
        string name = element.GetLocalName();
        switch (name)
        {
            case "html":
                this.ExtractRootAttributes(element);
                break;
            case "body":
                this.ExtractBodyAttributes(element);
                break;
            case "title":
                this.AddText(HtmlMetadataField.Title,
                    element.GetTextContent(), HtmlMetadataSource.Document,
                    FallbackPrecedence);
                break;
            case "base":
                this.ExtractBase(element);
                break;
            case "link":
                this.ExtractLink(element);
                break;
            case "meta":
                this.ExtractMeta(element);
                break;
            case "script":
                this.ExtractJsonLd(element);
                break;
        }
    }

    internal void ExtractRootAttributes(HtmlNode element)
    {
        string? language = element.GetAttribute("lang");
        if (language == null)
        {
            language = element.GetAttributeInNamespace(
                HtmlNamespaceData.GetUri(HtmlNamespace.Xml), "lang");
        }

        if (language != null)
        {
            this.AddText(HtmlMetadataField.Language, language,
                HtmlMetadataSource.Document, PrimaryPrecedence);
        }

        string? direction = NormalizeDirection(element.GetAttribute("dir"));
        if (direction != null && this.Includes(
            HtmlMetadataField.Direction, HtmlMetadataSource.Document))
        {
            this.candidates.Add(HtmlMetadataField.Direction, direction,
                element.GetAttribute("dir"), HtmlMetadataSource.Document,
                PrimaryPrecedence, this.documentOrder);
        }
    }

    internal void ExtractBodyAttributes(HtmlNode element)
    {
        string? direction = NormalizeDirection(element.GetAttribute("dir"));
        if (direction != null && this.Includes(
            HtmlMetadataField.Direction, HtmlMetadataSource.Document))
        {
            this.candidates.Add(HtmlMetadataField.Direction, direction,
                element.GetAttribute("dir"), HtmlMetadataSource.Document,
                SecondaryPrecedence, this.documentOrder);
        }
    }

    internal void ExtractBase(HtmlNode element)
    {
        if (this.baseElementSeen || element.GetAttribute("href") == null)
        {
            return;
        }

        this.baseElementSeen = true;
        this.AddElementUrl(HtmlMetadataField.BaseUrl, element, "href",
            HtmlMetadataSource.Link, PrimaryPrecedence);
    }

    internal void ExtractLink(HtmlNode element)
    {
        string[] relations = element.GetSpaceSeparatedTokens("rel");
        if (relations.Length == 0)
        {
            return;
        }

        if (ContainsToken(relations, "canonical"))
        {
            this.AddElementUrl(HtmlMetadataField.CanonicalUrl, element,
                "href", HtmlMetadataSource.Link, PrimaryPrecedence);
        }

        if (ContainsToken(relations, "alternate")
            && !ContainsToken(relations, "stylesheet"))
        {
            this.AddElementUrl(HtmlMetadataField.AlternateUrl, element,
                "href", HtmlMetadataSource.Link, PrimaryPrecedence);
        }

        int iconPrecedence = GetIconPrecedence(relations);
        if (iconPrecedence >= 0)
        {
            this.AddElementUrl(HtmlMetadataField.FaviconUrl, element,
                "href", HtmlMetadataSource.Link, iconPrecedence);
        }

        if (ContainsToken(relations, "image_src"))
        {
            this.AddElementUrl(HtmlMetadataField.ImageUrl, element, "href",
                HtmlMetadataSource.Link, TertiaryPrecedence);
        }
    }

    internal void ExtractMeta(HtmlNode element)
    {
        string? content = element.GetAttribute("content");
        if (content == null)
        {
            return;
        }

        string? property = element.GetAttribute("property");
        string? name = element.GetAttribute("name");
        string? httpEquiv = element.GetAttribute("http-equiv");
        if (!string.IsNullOrWhiteSpace(property))
        {
            this.ExtractMetaValue(property, content,
                HtmlMetadataSource.MetaProperty);
        }

        if (!string.IsNullOrWhiteSpace(name)
            && !EqualsAsciiIgnoreCase(name, property))
        {
            this.ExtractMetaValue(name, content,
                HtmlMetadataSource.MetaName);
        }

        if (!string.IsNullOrWhiteSpace(httpEquiv)
            && !EqualsAsciiIgnoreCase(httpEquiv, property)
            && !EqualsAsciiIgnoreCase(httpEquiv, name))
        {
            this.ExtractHttpEquiv(httpEquiv, content);
        }
    }

    internal void ExtractJsonLd(HtmlNode element)
    {
        if (!this.options.GetIncludeSource(HtmlMetadataSource.JsonLd))
        {
            return;
        }

        HtmlJsonLdExtractor.Extract(element, this.effectiveBaseUrl,
            this.candidates, this.diagnostics, this.budget, this.limits,
            this.cancellationToken, this.documentOrder, this.options);
    }

    internal void ExtractMetaValue(string identifierValue, string content,
        HtmlMetadataSource defaultSource)
    {
        string identifier = AsciiLower(identifierValue.Trim());
        HtmlMetadataSource source = GetMetadataSource(identifier,
            defaultSource);
        switch (identifier)
        {
            case "og:title":
                this.AddText(HtmlMetadataField.Title, content, source,
                    PrimaryPrecedence);
                break;
            case "twitter:title":
                this.AddText(HtmlMetadataField.Title, content, source,
                    SecondaryPrecedence);
                break;
            case "dc.title":
            case "dc:title":
            case "dcterms.title":
            case "dcterms:title":
                this.AddText(HtmlMetadataField.Title, content, source,
                    TertiaryPrecedence);
                break;
            case "title":
                this.AddText(HtmlMetadataField.Title, content, source, 40);
                break;
            case "og:description":
                this.AddText(HtmlMetadataField.Description, content, source,
                    PrimaryPrecedence);
                break;
            case "twitter:description":
                this.AddText(HtmlMetadataField.Description, content, source,
                    SecondaryPrecedence);
                break;
            case "description":
                this.AddText(HtmlMetadataField.Description, content, source,
                    TertiaryPrecedence);
                break;
            case "dc.description":
            case "dc:description":
            case "dcterms.description":
            case "dcterms:description":
                this.AddText(HtmlMetadataField.Description, content, source,
                    40);
                break;
            case "og:site_name":
                this.AddText(HtmlMetadataField.SiteName, content, source,
                    PrimaryPrecedence);
                break;
            case "twitter:site":
                this.AddText(HtmlMetadataField.SiteName, content, source,
                    SecondaryPrecedence);
                break;
            case "application-name":
                this.AddText(HtmlMetadataField.SiteName, content, source,
                    TertiaryPrecedence);
                break;
            case "dc.publisher":
            case "dc:publisher":
            case "dcterms.publisher":
            case "dcterms:publisher":
                this.AddText(HtmlMetadataField.SiteName, content, source,
                    TertiaryPrecedence);
                break;
            case "author":
            case "byline":
            case "byl":
            case "citation_author":
            case "parsely-author":
            case "sailthru.author":
                this.AddText(HtmlMetadataField.Author, content, source,
                    SecondaryPrecedence);
                break;
            case "article:author":
                this.AddText(HtmlMetadataField.Author, content, source,
                    PrimaryPrecedence);
                break;
            case "twitter:creator":
                this.AddText(HtmlMetadataField.Author, content, source,
                    TertiaryPrecedence);
                break;
            case "dc.creator":
            case "dc:creator":
            case "dc.author":
            case "dcterms.creator":
            case "dcterms:creator":
                this.AddText(HtmlMetadataField.Author, content, source,
                    SecondaryPrecedence);
                break;
            case "language":
            case "dc.language":
            case "dc:language":
            case "dcterms.language":
            case "dcterms:language":
                this.AddText(HtmlMetadataField.Language, content, source,
                    TertiaryPrecedence);
                break;
            case "og:locale":
                this.AddText(HtmlMetadataField.Language, content, source,
                    SecondaryPrecedence);
                break;
            case "direction":
            case "dir":
                this.AddDirection(content, source, TertiaryPrecedence);
                break;
            case "article:published_time":
                this.AddText(HtmlMetadataField.PublishedTime, content, source,
                    PrimaryPrecedence);
                break;
            case "date":
            case "pubdate":
            case "publish-date":
            case "datepublished":
            case "citation_publication_date":
            case "dc.date":
            case "dc:date":
            case "dcterms.date":
            case "dcterms:date":
            case "dcterms.created":
            case "dcterms:created":
            case "dcterms.issued":
            case "dcterms:issued":
                this.AddText(HtmlMetadataField.PublishedTime, content, source,
                    TertiaryPrecedence);
                break;
            case "article:modified_time":
                this.AddText(HtmlMetadataField.ModifiedTime, content, source,
                    PrimaryPrecedence);
                break;
            case "last-modified":
            case "date-modified":
            case "datemodified":
            case "dcterms.modified":
            case "dcterms:modified":
                this.AddText(HtmlMetadataField.ModifiedTime, content, source,
                    TertiaryPrecedence);
                break;
            case "og:url":
                this.AddContentUrl(HtmlMetadataField.CanonicalUrl, content,
                    source, SecondaryPrecedence);
                break;
            case "twitter:url":
                this.AddContentUrl(HtmlMetadataField.CanonicalUrl, content,
                    source, TertiaryPrecedence);
                break;
            case "dc.identifier":
            case "dc:identifier":
            case "dcterms.identifier":
            case "dcterms:identifier":
                this.AddContentUrl(HtmlMetadataField.CanonicalUrl, content,
                    source, 40);
                break;
            case "og:image:secure_url":
                this.AddContentUrl(HtmlMetadataField.ImageUrl, content,
                    source, PrimaryPrecedence - 1);
                break;
            case "og:image":
            case "og:image:url":
                this.AddContentUrl(HtmlMetadataField.ImageUrl, content,
                    source, PrimaryPrecedence);
                break;
            case "twitter:image":
            case "twitter:image:src":
                this.AddContentUrl(HtmlMetadataField.ImageUrl, content,
                    source, SecondaryPrecedence);
                break;
            case "image":
            case "thumbnail":
            case "thumbnailurl":
            case "parsely-image-url":
                this.AddContentUrl(HtmlMetadataField.ImageUrl, content,
                    source, TertiaryPrecedence);
                break;
        }
    }

    internal void ExtractHttpEquiv(string identifierValue, string content)
    {
        string identifier = AsciiLower(identifierValue.Trim());
        switch (identifier)
        {
            case "content-language":
                string language = GetFirstLanguage(content);
                this.AddText(HtmlMetadataField.Language, language,
                    HtmlMetadataSource.HttpEquiv, TertiaryPrecedence);
                break;
            case "last-modified":
                this.AddText(HtmlMetadataField.ModifiedTime, content,
                    HtmlMetadataSource.HttpEquiv, SecondaryPrecedence);
                break;
        }
    }

    internal void AddDocumentBaseUrl()
    {
        if (!this.Includes(HtmlMetadataField.BaseUrl,
            HtmlMetadataSource.Document))
        {
            return;
        }

        string rawValue = this.document.GetBaseUrl();
        if (rawValue.Length == 0)
        {
            rawValue = this.document.GetUrl();
        }

        if (rawValue.Length == 0)
        {
            return;
        }

        HtmlUrlResult result = HtmlUrls.Normalize(rawValue, this.limits,
            this.cancellationToken);
        if (result.IsSuccess())
        {
            this.candidates.Add(HtmlMetadataField.BaseUrl, result.GetUrl(),
                rawValue, HtmlMetadataSource.Document, FallbackPrecedence, 0);
        }
    }

    internal void InitializeEffectiveBaseUrl()
    {
        System.Threading.Tasks.Task<HtmlUrlResult> baseUrlTask =
            System.Threading.Tasks.Task.Run(() =>
                HtmlDomUrls.GetEffectiveBaseUrl(this.document, this.limits,
                    this.cancellationToken));
        while (!baseUrlTask.IsCompleted)
        {
            Thread.Yield();
        }

        if (baseUrlTask.IsCompletedSuccessfully)
        {
            this.effectiveBaseUrl = baseUrlTask.Result;
            return;
        }

        AggregateException? aggregate = baseUrlTask.Exception;
        if (aggregate == null)
        {
            throw new InvalidOperationException(
                "A failed base-URL task must contain an exception.");
        }

        Exception exception = aggregate.GetBaseException();
        if (exception.GetType().FullName !=
            "System.InvalidOperationException")
        {
            throw exception;
        }

        this.AddLimitDiagnostic(
            "The metadata base URL search limit was exceeded.");
        string fallback = this.document.GetBaseUrl();
        if (fallback.Length == 0)
        {
            fallback = this.document.GetUrl();
        }

        this.effectiveBaseUrl = HtmlUrls.Normalize(fallback, this.limits,
            this.cancellationToken);
    }

    internal void AddText(HtmlMetadataField field, string rawValue,
        HtmlMetadataSource source, int precedence)
    {
        if (rawValue == null || !this.Includes(field, source))
        {
            return;
        }

        string value = HtmlTextContent.Normalize(rawValue);
        if (value.Length == 0)
        {
            return;
        }

        this.candidates.Add(field, value, rawValue, source, precedence,
            this.documentOrder);
    }

    internal void AddDirection(string rawValue, HtmlMetadataSource source,
        int precedence)
    {
        if (!this.Includes(HtmlMetadataField.Direction, source))
        {
            return;
        }

        string? value = NormalizeDirection(rawValue);
        if (value != null)
        {
            this.candidates.Add(HtmlMetadataField.Direction, value, rawValue,
                source, precedence, this.documentOrder);
        }
    }

    internal void AddElementUrl(HtmlMetadataField field, HtmlNode element,
        string attributeName, HtmlMetadataSource source, int precedence)
    {
        if (!this.Includes(field, source))
        {
            return;
        }

        string? rawValue = element.GetAttribute(attributeName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        if (element.GetLocalName() != "base")
        {
            this.AddContentUrl(field, rawValue, source, precedence);
            return;
        }

        string fallback = this.document.GetBaseUrl();
        if (fallback.Length == 0)
        {
            fallback = this.document.GetUrl();
        }

        HtmlUrlResult result = fallback.Length == 0
            ? HtmlUrls.Normalize(rawValue, this.limits,
                this.cancellationToken)
            : HtmlUrls.Resolve(rawValue, fallback, this.limits,
                this.cancellationToken);
        if (result.IsSuccess()
            && (result.GetScheme() == "data"
                || result.GetScheme() == "javascript"))
        {
            return;
        }

        this.AddResolvedUrl(field, rawValue, source, precedence, result);
    }

    internal void AddContentUrl(HtmlMetadataField field, string rawValue,
        HtmlMetadataSource source, int precedence)
    {
        if (string.IsNullOrWhiteSpace(rawValue)
            || !this.Includes(field, source))
        {
            return;
        }

        HtmlUrlResult? effectiveBaseUrl = this.effectiveBaseUrl;
        HtmlUrlResult result = effectiveBaseUrl != null
            && effectiveBaseUrl.IsSuccess()
            ? HtmlUrls.Resolve(rawValue, effectiveBaseUrl.GetUrl(),
                this.limits, this.cancellationToken)
            : HtmlUrls.Normalize(rawValue, this.limits,
                this.cancellationToken);
        this.AddResolvedUrl(field, rawValue, source, precedence, result);
    }

    internal void AddResolvedUrl(HtmlMetadataField field, string rawValue,
        HtmlMetadataSource source, int precedence, HtmlUrlResult result)
    {
        if (result.IsSuccess())
        {
            this.candidates.Add(field, result.GetUrl(), rawValue, source,
                precedence, this.documentOrder);
            return;
        }

        this.AddDiagnostics(result.GetDiagnostics());
    }

    internal bool Includes(HtmlMetadataField field,
        HtmlMetadataSource source)
    {
        return this.options.GetIncludeField(field)
            && this.options.GetIncludeSource(source);
    }

    internal bool NeedsEffectiveBaseUrl()
    {
        bool hasUrlField = this.options.GetIncludeField(
            HtmlMetadataField.CanonicalUrl)
            || this.options.GetIncludeField(HtmlMetadataField.AlternateUrl)
            || this.options.GetIncludeField(HtmlMetadataField.FaviconUrl)
            || this.options.GetIncludeField(HtmlMetadataField.ImageUrl);
        if (!hasUrlField)
        {
            return false;
        }

        return this.options.GetIncludeSource(HtmlMetadataSource.Link)
            || this.options.GetIncludeSource(HtmlMetadataSource.MetaName)
            || this.options.GetIncludeSource(
                HtmlMetadataSource.MetaProperty)
            || this.options.GetIncludeSource(HtmlMetadataSource.OpenGraph)
            || this.options.GetIncludeSource(HtmlMetadataSource.Twitter)
            || this.options.GetIncludeSource(HtmlMetadataSource.DublinCore)
            || this.options.GetIncludeSource(HtmlMetadataSource.JsonLd);
    }

    internal void AddDiagnostics(HtmlDiagnostic[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            this.diagnostics.Add(values[index]);
            index = index + 1;
        }
    }

    internal bool TryVisit(int depth)
    {
        if (!this.budget.Allows(HtmlLimitKind.Depth, depth)
            || !this.budget.TryConsume(HtmlLimitKind.Visits, 1))
        {
            this.AddLimitDiagnostic(
                "The metadata extraction traversal limit was exceeded.");
            return false;
        }

        return true;
    }

    internal void AddLimitDiagnostic(string message)
    {
        if (this.limitDiagnosticAdded)
        {
            return;
        }

        this.limitDiagnosticAdded = true;
        this.diagnostics.AddDetails(HtmlDiagnosticCode.ResourceLimitExceeded,
            HtmlDiagnosticSeverity.Warning, message,
            HtmlSourceSpan.CreateUnknown());
    }

    internal static HtmlMetadataSource GetMetadataSource(string identifier,
        HtmlMetadataSource defaultSource)
    {
        if (identifier.StartsWith("og:", StringComparison.Ordinal)
            || identifier.StartsWith("article:", StringComparison.Ordinal))
        {
            return HtmlMetadataSource.OpenGraph;
        }

        if (identifier.StartsWith("twitter:", StringComparison.Ordinal))
        {
            return HtmlMetadataSource.Twitter;
        }

        if (identifier.StartsWith("dc.", StringComparison.Ordinal)
            || identifier.StartsWith("dc:", StringComparison.Ordinal)
            || identifier.StartsWith("dcterms.", StringComparison.Ordinal)
            || identifier.StartsWith("dcterms:", StringComparison.Ordinal))
        {
            return HtmlMetadataSource.DublinCore;
        }

        return defaultSource;
    }

    internal static string? NormalizeDirection(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string normalized = AsciiLower(value.Trim());
        return normalized == "ltr" || normalized == "rtl"
            || normalized == "auto" ? normalized : null;
    }

    internal static string GetFirstLanguage(string value)
    {
        int comma = value.IndexOf(',');
        return comma < 0 ? value : value.Substring(0, comma);
    }

    internal static int GetIconPrecedence(string[] relations)
    {
        if (ContainsToken(relations, "icon"))
        {
            return PrimaryPrecedence;
        }

        if (ContainsToken(relations, "apple-touch-icon")
            || ContainsToken(relations, "apple-touch-icon-precomposed"))
        {
            return SecondaryPrecedence;
        }

        if (ContainsToken(relations, "mask-icon"))
        {
            return TertiaryPrecedence;
        }

        return -1;
    }

    internal static bool ContainsToken(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (EqualsAsciiIgnoreCase(values[index], expected))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool EqualsAsciiIgnoreCase(string left, string? right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return left == right;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (Ascii.ToLower(left[index]) != Ascii.ToLower(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string AsciiLower(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Ascii.ToLower(result[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
