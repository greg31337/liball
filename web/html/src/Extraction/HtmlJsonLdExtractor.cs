using System;
using System.Text.Json;
using System.Threading;

namespace html;

internal static class HtmlJsonLdExtractor
{
    internal static void Extract(HtmlNode script, HtmlUrlResult? effectiveBaseUrl,
        HtmlMetadataCandidateCollector candidates,
        HtmlDiagnosticCollector diagnostics, HtmlBudget budget,
        HtmlLimits limits, CancellationToken cancellationToken,
        int documentOrder, HtmlMetadataOptions metadataOptions)
    {
        if (!IsJsonLdScript(script))
        {
            return;
        }

        string json = script.GetTextContent();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        budget.CheckCancellation();
        int maximumInputCharacters = Math.Min(
            limits.GetMaximumInputCharacters(),
            limits.GetMaximumTokenCharacters());
        if (json.Length > maximumInputCharacters)
        {
            AddDiagnostic(diagnostics,
                HtmlDiagnosticCode.ResourceLimitExceeded,
                "A JSON-LD metadata block exceeded the input character "
                    + "limit.", script.GetSourceSpan());
            return;
        }

        JsonDocumentOptions options = new JsonDocumentOptions();
        options.AllowTrailingCommas = false;
        options.CommentHandling = JsonCommentHandling.Disallow;
        options.MaxDepth = limits.GetMaximumJsonDepth();
        System.Threading.Tasks.Task<JsonDocument> parseTask =
            System.Threading.Tasks.Task.Run(() =>
                JsonDocument.Parse(json, options));
        while (!parseTask.IsCompleted)
        {
            Thread.Yield();
        }

        if (!parseTask.IsCompletedSuccessfully)
        {
            AggregateException? aggregate = parseTask.Exception;
            if (aggregate == null)
            {
                throw new InvalidOperationException(
                    "A failed JSON-LD task must contain an exception.");
            }

            Exception exception = aggregate.GetBaseException();
            if (!IsExceptionType(exception, "JsonException"))
            {
                throw exception;
            }

            AddDiagnostic(diagnostics,
                HtmlDiagnosticCode.MetadataInvalidJsonLd,
                "A JSON-LD metadata block was ignored because it is "
                    + "invalid or exceeds the configured depth limit.",
                script.GetSourceSpan());
            return;
        }

        using (JsonDocument document = parseTask.Result)
        {
            budget.CheckCancellation();
            HtmlJsonLdExtractionContext context =
                new HtmlJsonLdExtractionContext(effectiveBaseUrl,
                    candidates, diagnostics, budget, limits,
                    cancellationToken, documentOrder,
                    script.GetSourceSpan(), metadataOptions);
            context.Scan(document.RootElement);
        }
    }

    internal static bool IsJsonLdScript(HtmlNode script)
    {
        string? type = script.GetAttribute("type");
        if (type == null)
        {
            return false;
        }

        int separator = type.IndexOf(';');
        string mediaType = separator < 0 ? type.Trim()
            : type.Substring(0, separator).Trim();
        return EqualsAsciiIgnoreCase(mediaType, "application/ld+json");
    }

    internal static bool IsExceptionType(Exception exception,
        string typeName)
    {
        Type? type = exception.GetType();
        while (type != null)
        {
            if (type.Name == typeName)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    internal static bool EqualsAsciiIgnoreCase(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
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

    internal static void AddDiagnostic(HtmlDiagnosticCollector diagnostics,
        HtmlDiagnosticCode code, string message, HtmlSourceSpan span)
    {
        diagnostics.AddDetails(code, HtmlDiagnosticSeverity.Warning, message, span);
    }
}

internal class HtmlJsonLdObjectInspection
{
    internal bool success;
    internal int precedence;

    internal HtmlJsonLdObjectInspection(bool successValue,
        int precedenceValue)
    {
        this.success = successValue;
        this.precedence = precedenceValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal int GetPrecedence()
    {
        return this.precedence;
    }
}

internal class HtmlJsonLdExtractionContext
{
    internal const int ArticlePrecedence = 15;
    internal const int CreativeWorkPrecedence = 16;
    internal const int WebPagePrecedence = 18;
    internal const int UntypedPrecedence = 19;

    internal HtmlUrlResult? effectiveBaseUrl;
    internal HtmlMetadataCandidateCollector candidates;
    internal HtmlDiagnosticCollector diagnostics;
    internal HtmlBudget budget;
    internal HtmlLimits limits;
    internal CancellationToken cancellationToken;
    internal int documentOrder;
    internal HtmlSourceSpan sourceSpan;
    internal HtmlMetadataOptions metadataOptions;
    internal bool propertyLimitDiagnosticAdded;

    internal HtmlJsonLdExtractionContext(HtmlUrlResult? effectiveBaseUrlValue,
        HtmlMetadataCandidateCollector candidatesValue,
        HtmlDiagnosticCollector diagnosticsValue, HtmlBudget budgetValue,
        HtmlLimits limitsValue, CancellationToken cancellationTokenValue,
        int documentOrderValue, HtmlSourceSpan sourceSpanValue,
        HtmlMetadataOptions metadataOptionsValue)
    {
        this.effectiveBaseUrl = effectiveBaseUrlValue;
        this.candidates = candidatesValue;
        this.diagnostics = diagnosticsValue;
        this.budget = budgetValue;
        this.limits = limitsValue;
        this.cancellationToken = cancellationTokenValue;
        this.documentOrder = documentOrderValue;
        this.sourceSpan = sourceSpanValue;
        this.metadataOptions = metadataOptionsValue;
        this.propertyLimitDiagnosticAdded = false;
    }

    internal void Scan(JsonElement root)
    {
        this.ScanElement(root);
    }

    internal bool ScanElement(JsonElement element)
    {
        this.budget.CheckCancellation();
        if (element.ValueKind == JsonValueKind.Object)
        {
            return this.ScanObject(element);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (!this.ScanElement(item))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal bool ScanObject(JsonElement element)
    {
        HtmlJsonLdObjectInspection inspection = this.InspectObject(element);
        if (!inspection.IsSuccess())
        {
            return false;
        }

        int precedence = inspection.GetPrecedence();

        foreach (JsonProperty property in element.EnumerateObject())
        {
            string name = NormalizeSchemaTerm(property.Name);
            bool scanned = name == "@context"
                ? this.ScanIgnoredElement(property.Value)
                : this.ScanElement(property.Value);
            if (!scanned)
            {
                return false;
            }

            if (precedence >= 0)
            {
                this.ExtractProperty(name, property.Value, precedence);
            }
        }

        return true;
    }

    internal bool ScanIgnoredElement(JsonElement element)
    {
        this.budget.CheckCancellation();
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (!this.ScanIgnoredElement(item))
                {
                    return false;
                }
            }

            return true;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!this.budget.TryConsume(HtmlLimitKind.JsonProperties, 1))
            {
                this.AddPropertyLimitDiagnostic();
                return false;
            }

            if (!this.ScanIgnoredElement(property.Value))
            {
                return false;
            }
        }

        return true;
    }

    internal HtmlJsonLdObjectInspection InspectObject(JsonElement element)
    {
        bool hasType = false;
        int typePrecedence = -1;
        bool hasUntypedMetadata = false;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!this.budget.TryConsume(HtmlLimitKind.JsonProperties, 1))
            {
                this.AddPropertyLimitDiagnostic();
                return new HtmlJsonLdObjectInspection(false, -1);
            }

            string name = NormalizeSchemaTerm(property.Name);
            if (name == "@type")
            {
                hasType = true;
                int currentPrecedence =
                    GetTypePrecedence(property.Value);
                if (currentPrecedence >= 0
                    && (typePrecedence < 0
                        || currentPrecedence < typePrecedence))
                {
                    typePrecedence = currentPrecedence;
                }
            }
            else if (name == "headline" || name == "author"
                || name == "publisher" || name == "datePublished"
                || name == "dateModified")
            {
                hasUntypedMetadata = true;
            }
        }

        if (hasType)
        {
            return new HtmlJsonLdObjectInspection(true, typePrecedence);
        }

        return new HtmlJsonLdObjectInspection(true,
            hasUntypedMetadata ? UntypedPrecedence : -1);
    }

    internal void ExtractProperty(string name, JsonElement value, int precedence)
    {
        switch (name)
        {
            case "headline":
                if (this.IncludesField(HtmlMetadataField.Title))
                {
                    this.AddTextValues(HtmlMetadataField.Title, value,
                        precedence);
                }
                break;
            case "author":
                if (this.IncludesField(HtmlMetadataField.Author))
                {
                    this.AddNameValues(HtmlMetadataField.Author, value,
                        precedence);
                }
                break;
            case "publisher":
                if (this.IncludesField(HtmlMetadataField.SiteName))
                {
                    this.AddNameValues(HtmlMetadataField.SiteName, value,
                        precedence);
                }
                break;
            case "datePublished":
                if (this.IncludesField(HtmlMetadataField.PublishedTime))
                {
                    this.AddTextValues(HtmlMetadataField.PublishedTime,
                        value, precedence);
                }
                break;
            case "dateModified":
                if (this.IncludesField(HtmlMetadataField.ModifiedTime))
                {
                    this.AddTextValues(HtmlMetadataField.ModifiedTime,
                        value, precedence);
                }
                break;
            case "image":
                if (this.IncludesField(HtmlMetadataField.ImageUrl))
                {
                    this.AddImageValues(value, precedence);
                }
                break;
        }
    }

    internal bool IncludesField(HtmlMetadataField field)
    {
        return this.metadataOptions.GetIncludeField(field);
    }

    internal void AddTextValues(HtmlMetadataField field, JsonElement value,
        int precedence)
    {
        this.budget.CheckCancellation();
        if (value.ValueKind == JsonValueKind.String)
        {
            string? rawValue = value.GetString();
            string normalized = HtmlTextContent.Normalize(rawValue);
            if (normalized.Length > 0)
            {
                this.candidates.Add(field, normalized, rawValue,
                    HtmlMetadataSource.JsonLd, precedence,
                    this.documentOrder);
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                this.AddTextValues(field, item, precedence);
            }
        }
    }

    internal void AddNameValues(HtmlMetadataField field, JsonElement value,
        int precedence)
    {
        this.budget.CheckCancellation();
        if (value.ValueKind == JsonValueKind.String
            || value.ValueKind == JsonValueKind.Array)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                this.AddTextValues(field, value, precedence);
                return;
            }

            foreach (JsonElement item in value.EnumerateArray())
            {
                this.AddNameValues(field, item, precedence);
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (NormalizeSchemaTerm(property.Name) == "name")
            {
                this.AddTextValues(field, property.Value, precedence);
            }
        }
    }

    internal void AddImageValues(JsonElement value, int precedence)
    {
        this.budget.CheckCancellation();
        if (value.ValueKind == JsonValueKind.String)
        {
            this.AddImageUrl(value.GetString(), precedence);
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                this.AddImageValues(item, precedence);
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            string name = NormalizeSchemaTerm(property.Name);
            if (name == "url" || name == "contentUrl"
                || name == "thumbnailUrl")
            {
                this.AddImageValues(property.Value, precedence);
            }
        }
    }

    internal void AddImageUrl(string? rawValue, int precedence)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
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
        if (result.IsSuccess())
        {
            this.candidates.Add(HtmlMetadataField.ImageUrl, result.GetUrl(),
                rawValue, HtmlMetadataSource.JsonLd, precedence,
                this.documentOrder);
            return;
        }

        HtmlDiagnostic[] found = result.GetDiagnostics();
        int index = 0;
        while (index < found.Length)
        {
            this.diagnostics.Add(found[index]);
            index = index + 1;
        }
    }

    internal void AddPropertyLimitDiagnostic()
    {
        if (this.propertyLimitDiagnosticAdded)
        {
            return;
        }

        this.propertyLimitDiagnosticAdded = true;
        this.diagnostics.AddDetails(HtmlDiagnosticCode.ResourceLimitExceeded,
            HtmlDiagnosticSeverity.Warning,
            "A JSON-LD metadata block exceeded the property limit.",
            this.sourceSpan);
    }

    internal static int GetTypePrecedence(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return GetTypePrecedence(value.GetString());
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            int result = -1;
            foreach (JsonElement item in value.EnumerateArray())
            {
                int current = GetTypePrecedence(item);
                if (current >= 0 && (result < 0 || current < result))
                {
                    result = current;
                }
            }

            return result;
        }

        return -1;
    }

    internal static int GetTypePrecedence(string? value)
    {
        if (value == null)
        {
            return -1;
        }

        string type = NormalizeSchemaTerm(value);
        if (type.EndsWith("Article", StringComparison.Ordinal)
            || type.EndsWith("Posting", StringComparison.Ordinal))
        {
            return ArticlePrecedence;
        }

        if (type == "CreativeWork" || type == "Report")
        {
            return CreativeWorkPrecedence;
        }

        if (type == "WebPage")
        {
            return WebPagePrecedence;
        }

        return -1;
    }

    internal static string NormalizeSchemaTerm(string value)
    {
        const string HttpPrefix = "http://schema.org/";
        const string HttpsPrefix = "https://schema.org/";
        const string SchemaPrefix = "schema:";
        if (value.StartsWith(HttpsPrefix, StringComparison.Ordinal))
        {
            return value.Substring(HttpsPrefix.Length);
        }

        if (value.StartsWith(HttpPrefix, StringComparison.Ordinal))
        {
            return value.Substring(HttpPrefix.Length);
        }

        if (value.StartsWith(SchemaPrefix, StringComparison.Ordinal))
        {
            return value.Substring(SchemaPrefix.Length);
        }

        return value;
    }
}
