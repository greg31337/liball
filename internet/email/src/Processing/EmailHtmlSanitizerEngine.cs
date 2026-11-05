using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailHtmlSanitizerEngine
{
    static readonly List<string> allowedElementNames = new List<string> { "a", "abbr", "b", "blockquote", "br", "caption", "cite", "code", "col", "colgroup", "dd", "del", "details", "div", "dl", "dt", "em", "figcaption", "figure", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "img", "ins", "kbd", "li", "mark", "ol", "p", "pre", "q", "s", "samp", "small", "span", "strong", "sub", "summary", "sup", "table", "tbody", "td", "tfoot", "th", "thead", "time", "tr", "u", "ul", "var", "wbr" };
    static readonly List<string> suppressedElementNames = new List<string> { "script", "style", "iframe", "frame", "frameset", "object", "embed", "applet", "template", "svg", "math", "head" };
    static readonly List<string> voidElementNames = new List<string> { "br", "col", "hr", "img", "wbr" };
    EmailHtmlTokenizationResult tokenization;
    EmailHtmlSanitizeOptions options;
    EmailHtmlLimits limits;
    EmailTextDecodeResult? decoded;
    EmailIssueList issues;
    StringBuilder output;
    List<EmailHtmlMutation> mutations;
    List<string> openElements;
    List<string> suppressedElements;
    int blockedRemoteResourceCount;
    int blockedUnsafeUrlCount;
    int preservedCidResourceCount;
    int dataUriCount;
    long dataUriBytes;

    EmailHtmlSanitizerEngine(EmailHtmlTokenizationResult tokenization, EmailHtmlSanitizeOptions options, EmailHtmlLimits limits, EmailTextDecodeResult? decoded, EmailIssueList issues)
    {
        this.tokenization = tokenization;
        this.options = options;
        this.limits = limits;
        this.decoded = decoded;
        this.issues = issues;
        output = new StringBuilder();
        mutations = new List<EmailHtmlMutation>();
        openElements = new List<string>();
        suppressedElements = new List<string>();
        blockedRemoteResourceCount = 0;
        blockedUnsafeUrlCount = 0;
        preservedCidResourceCount = 0;
        dataUriCount = 0;
        dataUriBytes = 0;
    }

    internal static EmailHtmlSanitizeResult Sanitize(string html, EmailHtmlSanitizeOptions options, EmailTextDecodeResult? decoded, EmailIssueList initialIssues)
    {
        EmailHtmlLimits limits = options.GetLimits();
        EmailHtmlLimits tokenizerLimits = limits.Clone();
        if (html.Length > tokenizerLimits.GetMaxOutputCharacters()) tokenizerLimits.SetMaxOutputCharacters(html.Length);
        EmailHtmlTokenizationResult tokenization = new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits(html, tokenizerLimits);
        EmailIssueList issues = new EmailIssueList();
        issues.AddRange(initialIssues);
        issues.AddRange(tokenization.GetIssues());
        EmailHtmlSanitizerEngine sanitizer = new EmailHtmlSanitizerEngine(tokenization, options, limits, decoded, issues);
        return sanitizer.Run();
    }

    EmailHtmlSanitizeResult Run()
    {
        int index = 0;
        while (index < tokenization.GetTokenCount())
        {
            Handle(tokenization.GetToken(index));
            index++;
        }

        while (openElements.Count > 0)
        {
            string name = openElements[openElements.Count - 1];
            openElements.RemoveAt(openElements.Count - 1);
            Append("</" + name + ">");
            AddMutation(EmailHtmlMutationKind.RebalancedMarkup, name, "", "", "</" + name + ">", tokenization.GetSourceCharacterCount(), "An unclosed allowed element was closed at the end of the sanitized fragment.");
        }

        if (mutations.Count > 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.HtmlSanitized, "The HTML was modified by the conservative sanitization policy."));
        if (blockedRemoteResourceCount > 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HtmlRemoteContentBlocked, blockedRemoteResourceCount + " remote HTML resource reference(s) were blocked."));
        if (blockedUnsafeUrlCount > 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HtmlUnsafeUrlBlocked, blockedUnsafeUrlCount + " unsafe HTML URL reference(s) were blocked."));
        return new EmailHtmlSanitizeResult(output.ToString(), mutations, issues, tokenization, blockedRemoteResourceCount, blockedUnsafeUrlCount, preservedCidResourceCount, decoded);
    }

    void Handle(EmailHtmlToken token)
    {
        if (suppressedElements.Count > 0)
        {
            HandleSuppressed(token);
            return;
        }

        if (token.GetKind() == EmailHtmlTokenKind.Text)
        {
            AppendEscapedText(token.GetData());
            return;
        }

        if (token.GetKind() == EmailHtmlTokenKind.Comment)
        {
            HandleComment(token);
            return;
        }

        if (token.GetKind() == EmailHtmlTokenKind.Doctype)
        {
            AddMutation(EmailHtmlMutationKind.RemovedElement, "!doctype", "", token.GetData(), "", token.GetSourceOffset(), "Document type declarations are not retained in sanitized fragments.");
            return;
        }

        if (token.GetKind() == EmailHtmlTokenKind.StartTag)
        {
            HandleStartTag(token);
            return;
        }

        if (token.GetKind() == EmailHtmlTokenKind.EndTag) HandleEndTag(token);
    }

    void HandleSuppressed(EmailHtmlToken token)
    {
        if (token.GetKind() == EmailHtmlTokenKind.StartTag && !token.IsSelfClosing() && !IsVoidElement(token.GetName()))
        {
            suppressedElements.Add(token.GetName());
            return;
        }

        if (token.GetKind() != EmailHtmlTokenKind.EndTag) return;
        int index = suppressedElements.Count - 1;
        while (index >= 0 && !string.Equals(suppressedElements[index], token.GetName(), StringComparison.Ordinal)) index--;
        if (index >= 0) suppressedElements.RemoveRange(index, suppressedElements.Count - index);
    }

    void HandleComment(EmailHtmlToken token)
    {
        if (!options.GetPreserveComments())
        {
            AddMutation(EmailHtmlMutationKind.RemovedComment, "", "", token.GetData(), "", token.GetSourceOffset(), "HTML comments are removed by default.");
            return;
        }

        string value = MakeSafeComment(token.GetData());
        Append("<!--" + value + "-->");
        if (!string.Equals(value, token.GetData(), StringComparison.Ordinal)) AddMutation(EmailHtmlMutationKind.RewrittenAttribute, "", "comment", token.GetData(), value, token.GetSourceOffset(), "A comment delimiter sequence was rewritten to keep the serialized comment inert.");
    }

    void HandleStartTag(EmailHtmlToken token)
    {
        string name = token.GetName();
        if (IsSuppressedElement(name))
        {
            AddMutation(EmailHtmlMutationKind.RemovedElement, name, "", name, "", token.GetSourceOffset(), "An active or embedded-content element and its descendants were removed.");
            if (!token.IsSelfClosing() && !IsVoidElement(name)) suppressedElements.Add(name);
            return;
        }

        if (!IsAllowedElement(name))
        {
            AddMutation(EmailHtmlMutationKind.RemovedElement, name, "", name, "", token.GetSourceOffset(), "An element outside the conservative element allowlist was removed while retaining its descendants.");
            return;
        }

        Append("<" + name);
        List<string> retainedAttributes = new List<string>();
        int index = 0;
        while (index < token.GetAttributeCount())
        {
            HandleAttribute(token, token.GetAttribute(index), retainedAttributes);
            index++;
        }

        Append(">");
        if (IsVoidElement(name)) return;
        if (token.IsSelfClosing())
        {
            Append("</" + name + ">");
            AddMutation(EmailHtmlMutationKind.RebalancedMarkup, name, "", "/>", "></" + name + ">", token.GetSourceOffset(), "A self-closing non-void element was serialized with an explicit closing tag.");
            return;
        }

        openElements.Add(name);
    }

    void HandleEndTag(EmailHtmlToken token)
    {
        string name = token.GetName();
        if (!IsAllowedElement(name) || IsVoidElement(name))
        {
            AddMutation(EmailHtmlMutationKind.RemovedElement, name, "", name, "", token.GetSourceOffset(), "A disallowed or inapplicable closing tag was removed.");
            return;
        }

        int index = openElements.Count - 1;
        while (index >= 0 && !string.Equals(openElements[index], name, StringComparison.Ordinal)) index--;
        if (index < 0)
        {
            AddMutation(EmailHtmlMutationKind.RebalancedMarkup, name, "", "</" + name + ">", "", token.GetSourceOffset(), "An unmatched closing tag was removed.");
            return;
        }

        while (openElements.Count - 1 >= index)
        {
            string selected = openElements[openElements.Count - 1];
            openElements.RemoveAt(openElements.Count - 1);
            Append("</" + selected + ">");
            if (!string.Equals(selected, name, StringComparison.Ordinal)) AddMutation(EmailHtmlMutationKind.RebalancedMarkup, selected, "", "", "</" + selected + ">", token.GetSourceOffset(), "An allowed element was closed early to produce balanced markup.");
        }
    }

    void HandleAttribute(EmailHtmlToken token, EmailHtmlAttribute attribute, List<string> retainedAttributes)
    {
        string tagName = token.GetName();
        string name = attribute.GetName();
        string value = attribute.GetValue();
        if (Contains(retainedAttributes, name))
        {
            AddMutation(EmailHtmlMutationKind.RemovedAttribute, tagName, name, value, "", attribute.GetSourceOffset(), "A duplicate attribute was removed.");
            return;
        }

        if (StartsWith(name, "on") || string.Equals(name, "style", StringComparison.Ordinal) || !IsAllowedAttribute(tagName, name))
        {
            AddMutation(EmailHtmlMutationKind.RemovedAttribute, tagName, name, value, "", attribute.GetSourceOffset(), "An attribute outside the conservative attribute allowlist was removed.");
            return;
        }

        if (IsNumericAttribute(name) && !IsUnsignedNumber(value))
        {
            AddMutation(EmailHtmlMutationKind.RemovedAttribute, tagName, name, value, "", attribute.GetSourceOffset(), "A numeric HTML attribute with a non-numeric value was removed.");
            return;
        }

        if (IsUrlAttribute(tagName, name))
        {
            string? safeValue = SanitizeUrl(tagName, name, value, attribute.GetSourceOffset());
            if (safeValue == null) return;
            value = safeValue;
        }

        retainedAttributes.Add(name);
        Append(" " + name);
        if (attribute.HasValue() && !IsBooleanAttribute(name)) Append("=\"" + EscapeAttribute(value) + "\"");
    }

    string? SanitizeUrl(string tagName, string attributeName, string original, int sourceOffset)
    {
        if (original.Length > limits.GetMaxUrlCharacters()) throw new EmailLimitException("MaxHtmlUrlCharacters", limits.GetMaxUrlCharacters(), original.Length);
        string value = original.Trim();
        if (value.Length == 0 || ContainsUrlWhitespaceOrControl(value))
        {
            BlockUnsafeUrl(tagName, attributeName, original, sourceOffset, "An empty URL or a URL containing whitespace or control characters was blocked.");
            return null;
        }

        string? scheme = GetScheme(value);
        if (scheme == null)
        {
            if (HasSchemeDelimiter(value))
            {
                BlockUnsafeUrl(tagName, attributeName, original, sourceOffset, "A URL with a malformed scheme was blocked.");
                return null;
            }

            if (IsResourceUrl(tagName, attributeName) && !options.GetAllowRemoteResources())
            {
                BlockRemoteUrl(tagName, attributeName, original, sourceOffset);
                return null;
            }

            return RewriteUrlIfNeeded(tagName, attributeName, original, value, sourceOffset);
        }

        string lowerScheme = scheme.ToLowerInvariant();
        string normalized = lowerScheme + value.Substring(scheme.Length);
        if (string.Equals(lowerScheme, "cid", StringComparison.Ordinal))
        {
            if (!options.GetAllowCidResources() || !IsValidCidUrl(normalized))
            {
                BlockUnsafeUrl(tagName, attributeName, original, sourceOffset, "A disabled or malformed CID resource reference was blocked.");
                return null;
            }

            preservedCidResourceCount++;
            return RewriteUrlIfNeeded(tagName, attributeName, original, normalized, sourceOffset);
        }

        if (string.Equals(lowerScheme, "data", StringComparison.Ordinal))
        {
            if (!IsResourceUrl(tagName, attributeName) || !options.GetAllowSafeDataImages() || !IsSafeRasterDataUrl(normalized))
            {
                BlockUnsafeUrl(tagName, attributeName, original, sourceOffset, "A data URL outside the opt-in raster-image policy was blocked.");
                return null;
            }

            RecordDataUrl(normalized);
            return RewriteUrlIfNeeded(tagName, attributeName, original, normalized, sourceOffset);
        }

        if (string.Equals(lowerScheme, "http", StringComparison.Ordinal) || string.Equals(lowerScheme, "https", StringComparison.Ordinal))
        {
            if (IsResourceUrl(tagName, attributeName) && !options.GetAllowRemoteResources())
            {
                BlockRemoteUrl(tagName, attributeName, original, sourceOffset);
                return null;
            }

            return RewriteUrlIfNeeded(tagName, attributeName, original, normalized, sourceOffset);
        }

        if (!IsResourceUrl(tagName, attributeName) && (string.Equals(lowerScheme, "mailto", StringComparison.Ordinal) || string.Equals(lowerScheme, "tel", StringComparison.Ordinal))) return RewriteUrlIfNeeded(tagName, attributeName, original, normalized, sourceOffset);
        BlockUnsafeUrl(tagName, attributeName, original, sourceOffset, "A URL using a scheme outside the conservative scheme allowlist was blocked.");
        return null;
    }

    string RewriteUrlIfNeeded(string tagName, string attributeName, string original, string replacement, int sourceOffset)
    {
        if (!string.Equals(original, replacement, StringComparison.Ordinal)) AddMutation(EmailHtmlMutationKind.RewrittenAttribute, tagName, attributeName, original, replacement, sourceOffset, "A retained URL was trimmed or its scheme was normalized.");
        return replacement;
    }

    void BlockRemoteUrl(string tagName, string attributeName, string value, int sourceOffset)
    {
        blockedRemoteResourceCount++;
        AddMutation(EmailHtmlMutationKind.BlockedRemoteResource, tagName, attributeName, value, "", sourceOffset, "A fetch-capable remote resource reference was blocked by default.");
    }

    void BlockUnsafeUrl(string tagName, string attributeName, string value, int sourceOffset, string description)
    {
        blockedUnsafeUrlCount++;
        AddMutation(EmailHtmlMutationKind.BlockedUnsafeUrl, tagName, attributeName, value, "", sourceOffset, description);
    }

    void RecordDataUrl(string value)
    {
        dataUriCount++;
        if (dataUriCount > limits.GetMaxDataUriCount()) throw new EmailLimitException("MaxHtmlDataUriCount", limits.GetMaxDataUriCount(), dataUriCount);
        dataUriBytes += Encoding.UTF8.GetByteCount(value);
        if (dataUriBytes > limits.GetMaxDataUriBytes()) throw new EmailLimitException("MaxHtmlDataUriBytes", limits.GetMaxDataUriBytes(), dataUriBytes);
    }

    void AddMutation(EmailHtmlMutationKind kind, string tagName, string attributeName, string originalValue, string replacementValue, int sourceOffset, string description)
    {
        if (mutations.Count >= options.GetMaximumMutationCount()) throw new EmailLimitException("MaxHtmlMutationCount", options.GetMaximumMutationCount(), (long)mutations.Count + 1);
        mutations.Add(new EmailHtmlMutation(kind, tagName, attributeName, originalValue, replacementValue, sourceOffset, description));
    }

    void Append(string value)
    {
        long observed = (long)output.Length + value.Length;
        if (observed > limits.GetMaxOutputCharacters()) throw new EmailLimitException("MaxHtmlOutputCharacters", limits.GetMaxOutputCharacters(), observed);
        output.Append(value);
    }

    void AppendEscapedText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char selected = value[index];
            if (selected == '&') Append("&amp;");
            else if (selected == '<') Append("&lt;");
            else if (selected == '>') Append("&gt;");
            else Append(selected.ToString());
            index++;
        }
    }

    static string EscapeAttribute(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char selected = value[index];
            if (selected == '&') result.Append("&amp;");
            else if (selected == '"') result.Append("&quot;");
            else if (selected == '<') result.Append("&lt;");
            else if (selected == '>') result.Append("&gt;");
            else result.Append(selected);
            index++;
        }

        return result.ToString();
    }

    static string MakeSafeComment(string value)
    {
        string result = value.Replace("--", "- -");
        if (result.EndsWith("-", StringComparison.Ordinal)) result += " ";
        return result;
    }

    static bool IsAllowedElement(string name)
    {
        return Contains(allowedElementNames, name);
    }

    static bool IsSuppressedElement(string name)
    {
        return Contains(suppressedElementNames, name);
    }

    static bool IsVoidElement(string name)
    {
        return Contains(voidElementNames, name);
    }

    static bool IsAllowedAttribute(string tagName, string name)
    {
        if (IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixthAndSeventh(name, "title", "lang", "dir", "class", "role", "aria-label", "aria-hidden")) return true;
        if (string.Equals(tagName, "a", StringComparison.Ordinal) && IsOneOfWithValueAndFirstAndSecond(name, "href", "hreflang")) return true;
        if (string.Equals(tagName, "img", StringComparison.Ordinal) && IsOneOfWithValueAndFirstAndSecondAndThirdAndFourth(name, "src", "alt", "width", "height")) return true;
        if (IsOneOfWithValueAndFirstAndSecond(tagName, "blockquote", "q") && string.Equals(name, "cite", StringComparison.Ordinal)) return true;
        if (string.Equals(tagName, "ol", StringComparison.Ordinal) && IsOneOfWithValueAndFirstAndSecondAndThird(name, "start", "reversed", "type")) return true;
        if (string.Equals(tagName, "li", StringComparison.Ordinal) && string.Equals(name, "value", StringComparison.Ordinal)) return true;
        if (IsOneOfWithValueAndFirstAndSecond(tagName, "td", "th") && IsOneOfWithValueAndFirstAndSecondAndThirdAndFourth(name, "colspan", "rowspan", "headers", "scope")) return true;
        if (string.Equals(tagName, "details", StringComparison.Ordinal) && string.Equals(name, "open", StringComparison.Ordinal)) return true;
        if (string.Equals(tagName, "time", StringComparison.Ordinal) && string.Equals(name, "datetime", StringComparison.Ordinal)) return true;
        return false;
    }

    static bool IsUrlAttribute(string tagName, string name)
    {
        return string.Equals(name, "href", StringComparison.Ordinal) || string.Equals(name, "src", StringComparison.Ordinal) || string.Equals(name, "cite", StringComparison.Ordinal);
    }

    static bool IsResourceUrl(string tagName, string name)
    {
        return string.Equals(tagName, "img", StringComparison.Ordinal) && string.Equals(name, "src", StringComparison.Ordinal);
    }

    static bool IsNumericAttribute(string name)
    {
        return IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixth(name, "width", "height", "start", "value", "colspan", "rowspan");
    }

    static bool IsBooleanAttribute(string name)
    {
        return IsOneOfWithValueAndFirstAndSecond(name, "reversed", "open");
    }

    static bool IsUnsignedNumber(string value)
    {
        if (value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            index++;
        }

        return true;
    }

    static bool ContainsUrlWhitespaceOrControl(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char selected = value[index];
            if (selected <= 32 || selected == 127) return true;
            index++;
        }

        return false;
    }

    static string? GetScheme(string value)
    {
        int colon = value.IndexOf(':');
        int boundary = FirstUrlBoundary(value);
        if (colon < 0 || boundary >= 0 && colon > boundary) return null;
        if (colon == 0 || !IsAsciiLetter(value[0])) return null;
        int index = 1;
        while (index < colon)
        {
            char selected = value[index];
            if (!IsAsciiLetter(selected) && !IsAsciiDigit(selected) && selected != '+' && selected != '-' && selected != '.') return null;
            index++;
        }

        return value.Substring(0, colon);
    }

    static bool HasSchemeDelimiter(string value)
    {
        int colon = value.IndexOf(':');
        int boundary = FirstUrlBoundary(value);
        return colon >= 0 && (boundary < 0 || colon < boundary);
    }

    static int FirstUrlBoundary(string value)
    {
        int result = -1;
        int slash = value.IndexOf('/');
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (slash >= 0) result = slash;
        if (query >= 0 && (result < 0 || query < result)) result = query;
        if (fragment >= 0 && (result < 0 || fragment < result)) result = fragment;
        return result;
    }

    static bool IsValidCidUrl(string value)
    {
        if (value.Length <= 4) return false;
        string identifier = value.Substring(4);
        if (identifier.Length == 0) return false;
        int index = 0;
        while (index < identifier.Length)
        {
            char selected = identifier[index];
            if (selected <= 32 || selected == 127 || selected == '<' || selected == '>' || selected == '"') return false;
            index++;
        }

        return true;
    }

    static bool IsSafeRasterDataUrl(string value)
    {
        int comma = value.IndexOf(',');
        if (comma < 0) return false;
        string metadata = value.Substring(5, comma - 5).ToLowerInvariant();
        return HasDataMediaType(metadata, "image/png") || HasDataMediaType(metadata, "image/jpeg") || HasDataMediaType(metadata, "image/gif") || HasDataMediaType(metadata, "image/webp") || HasDataMediaType(metadata, "image/avif");
    }

    static bool HasDataMediaType(string metadata, string mediaType)
    {
        if (string.Equals(metadata, mediaType, StringComparison.Ordinal)) return true;
        return metadata.StartsWith(mediaType + ";", StringComparison.Ordinal);
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
    }

    static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool StartsWith(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.Ordinal);
    }

    static bool Contains(List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], expected, StringComparison.Ordinal)) return true;
            index++;
        }

        return false;
    }

    static bool IsOneOfWithValueAndFirstAndSecond(string value, string first, string second)
    {
        return string.Equals(value, first, StringComparison.Ordinal) || string.Equals(value, second, StringComparison.Ordinal);
    }

    static bool IsOneOfWithValueAndFirstAndSecondAndThird(string value, string first, string second, string third)
    {
        return IsOneOfWithValueAndFirstAndSecond(value, first, second) || string.Equals(value, third, StringComparison.Ordinal);
    }

    static bool IsOneOfWithValueAndFirstAndSecondAndThirdAndFourth(string value, string first, string second, string third, string fourth)
    {
        return IsOneOfWithValueAndFirstAndSecondAndThird(value, first, second, third) || string.Equals(value, fourth, StringComparison.Ordinal);
    }

    static bool IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifth(string value, string first, string second, string third, string fourth, string fifth)
    {
        return IsOneOfWithValueAndFirstAndSecondAndThirdAndFourth(value, first, second, third, fourth) || string.Equals(value, fifth, StringComparison.Ordinal);
    }

    static bool IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixth(string value, string first, string second, string third, string fourth, string fifth, string sixth)
    {
        return IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifth(value, first, second, third, fourth, fifth) || string.Equals(value, sixth, StringComparison.Ordinal);
    }

    static bool IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixthAndSeventh(string value, string first, string second, string third, string fourth, string fifth, string sixth, string seventh)
    {
        return IsOneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixth(value, first, second, third, fourth, fifth, sixth) || string.Equals(value, seventh, StringComparison.Ordinal);
    }
}
