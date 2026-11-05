using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;

internal class EmailHtmlDataUriExtractor
{
    string html;
    EmailHtmlDataUriOptions options;
    EmailHtmlLimits limits;
    EmailHtmlTokenizationResult tokenization;
    EmailTextDecodeResult? decoded;
    EmailIssueList issues;
    List<string> allowedMediaTypes;
    List<EmailHtmlSourceReplacement> replacements;
    List<MimeEntity> relatedParts;
    List<EmailHtmlDataUriExtraction> extractions;
    Dictionary<string, MimeEntity> partsByContentId;
    int rejectedDataUriCount;
    long extractedByteCount;

    EmailHtmlDataUriExtractor(string html, EmailHtmlDataUriOptions options, EmailHtmlLimits limits, EmailHtmlTokenizationResult tokenization, EmailTextDecodeResult? decoded, EmailIssueList issues)
    {
        this.html = html;
        this.options = options;
        this.limits = limits;
        this.tokenization = tokenization;
        this.decoded = decoded;
        this.issues = issues;
        allowedMediaTypes = options.GetAllowedMediaTypes();
        replacements = new List<EmailHtmlSourceReplacement>();
        relatedParts = new List<MimeEntity>();
        extractions = new List<EmailHtmlDataUriExtraction>();
        partsByContentId = new Dictionary<string, MimeEntity>(StringComparer.Ordinal);
        rejectedDataUriCount = 0;
        extractedByteCount = 0;
    }

    internal static EmailHtmlDataUriResult Extract(string html, EmailHtmlDataUriOptions options, EmailTextDecodeResult? decoded, EmailIssueList initialIssues)
    {
        EmailHtmlLimits limits = options.GetLimits();
        EmailHtmlLimits tokenizerLimits = limits.Clone();
        if (html.Length > tokenizerLimits.GetMaxOutputCharacters()) tokenizerLimits.SetMaxOutputCharacters(html.Length);
        EmailHtmlTokenizationResult tokenization = new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits(html, tokenizerLimits);
        EmailIssueList issues = new EmailIssueList();
        issues.AddRange(initialIssues);
        issues.AddRange(tokenization.GetIssues());
        EmailHtmlDataUriExtractor extractor = new EmailHtmlDataUriExtractor(html, options, limits, tokenization, decoded, issues);
        return extractor.Run();
    }

    EmailHtmlDataUriResult Run()
    {
        int index = 0;
        while (index < tokenization.GetTokenCount())
        {
            EmailHtmlToken token = tokenization.GetToken(index);
            if (token.GetKind() == EmailHtmlTokenKind.StartTag) HandleStartTag(token);
            else if (token.GetKind() == EmailHtmlTokenKind.Text && token.IsRawText() && string.Equals(GetContainingRawElement(index), "style", StringComparison.Ordinal)) HandleStyleText(token);
            index++;
        }

        string rewritten = ApplyReplacements();
        if (extractions.Count > 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.HtmlDataUriExtracted, extractions.Count + " HTML data URI reference(s) were extracted into " + relatedParts.Count + " related MIME part(s)."));
        if (rejectedDataUriCount > 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HtmlDataUriRejected, rejectedDataUriCount + " malformed or disallowed HTML data URI reference(s) were left unchanged."));
        return new EmailHtmlDataUriResult(rewritten, relatedParts, extractions, issues, tokenization, rejectedDataUriCount, extractedByteCount, decoded);
    }

    string GetContainingRawElement(int tokenIndex)
    {
        if (tokenIndex < 1) return "";
        EmailHtmlToken previous = tokenization.GetToken(tokenIndex - 1);
        return previous.GetKind() == EmailHtmlTokenKind.StartTag ? previous.GetName() : "";
    }

    void HandleStartTag(EmailHtmlToken token)
    {
        int index = 0;
        while (index < token.GetAttributeCount())
        {
            EmailHtmlAttribute attribute = token.GetAttribute(index);
            if (attribute.HasValue())
            {
                EmailHtmlDataUriRewrite? rewrite = null;
                if (IsUrlAttribute(attribute.GetName())) rewrite = RewriteSingleUrl(attribute.GetValue(), attribute.GetSourceOffset());
                else if (string.Equals(attribute.GetName(), "srcset", StringComparison.Ordinal)) rewrite = RewriteSrcSet(attribute.GetValue(), attribute.GetSourceOffset());
                else if (string.Equals(attribute.GetName(), "style", StringComparison.Ordinal)) rewrite = RewriteCss(attribute.GetValue(), attribute.GetSourceOffset());
                if (rewrite != null && rewrite.WasChanged())
                {
                    string replacement = attribute.GetName() + "=\"" + EscapeAttribute(rewrite.GetValue()) + "\"";
                    replacements.Add(new EmailHtmlSourceReplacement(attribute.GetSourceOffset(), attribute.GetSourceLength(), replacement));
                }
            }

            index++;
        }
    }

    void HandleStyleText(EmailHtmlToken token)
    {
        EmailHtmlDataUriRewrite rewrite = RewriteCss(token.GetData(), token.GetSourceOffset());
        if (rewrite.WasChanged()) replacements.Add(new EmailHtmlSourceReplacement(token.GetSourceOffset(), token.GetSourceLength(), rewrite.GetValue()));
    }

    EmailHtmlDataUriRewrite RewriteSingleUrl(string value, int sourceOffset)
    {
        string selected = value.Trim();
        if (!selected.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return new EmailHtmlDataUriRewrite(value, false);
        string? replacement = ExtractUri(selected, sourceOffset);
        return replacement == null ? new EmailHtmlDataUriRewrite(value, false) : new EmailHtmlDataUriRewrite(replacement, true);
    }

    EmailHtmlDataUriRewrite RewriteSrcSet(string value, int sourceOffset)
    {
        StringBuilder output = new StringBuilder();
        bool changed = false;
        int index = 0;
        while (index < value.Length)
        {
            int start = index;
            while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] == ',')) index++;
            output.Append(value.Substring(start, index - start));
            if (index >= value.Length) break;
            start = index;
            bool dataUri = StartsWithIgnoreCase(value, index, "data:");
            if (dataUri)
            {
                while (index < value.Length && !char.IsWhiteSpace(value[index])) index++;
                string candidate = value.Substring(start, index - start);
                string? replacement = ExtractUri(candidate, sourceOffset + start);
                if (replacement == null) output.Append(candidate);
                else
                {
                    output.Append(replacement);
                    changed = true;
                }
            }
            else
            {
                while (index < value.Length && !char.IsWhiteSpace(value[index]) && value[index] != ',') index++;
                output.Append(value.Substring(start, index - start));
            }
        }

        return new EmailHtmlDataUriRewrite(output.ToString(), changed);
    }

    EmailHtmlDataUriRewrite RewriteCss(string value, int sourceOffset)
    {
        StringBuilder output = new StringBuilder();
        bool changed = false;
        int cursor = 0;
        while (cursor < value.Length)
        {
            int function = IndexOfIgnoreCase(value, "url(", cursor);
            if (function < 0)
            {
                output.Append(value.Substring(cursor));
                break;
            }

            if (function > 0 && IsCssNameCharacter(value[function - 1]))
            {
                output.Append(value.Substring(cursor, function + 4 - cursor));
                cursor = function + 4;
                continue;
            }

            int content = function + 4;
            while (content < value.Length && char.IsWhiteSpace(value[content])) content++;
            char quote = content < value.Length && (value[content] == '"' || value[content] == '\'') ? value[content] : '\0';
            int uriStart = quote == '\0' ? content : content + 1;
            int end = uriStart;
            if (quote == '\0')
            {
                while (end < value.Length && value[end] != ')') end++;
            }
            else
            {
                while (end < value.Length && value[end] != quote) end++;
            }

            if (end >= value.Length)
            {
                output.Append(value.Substring(cursor));
                break;
            }

            int uriEnd = end;
            if (quote == '\0') while (uriEnd > uriStart && char.IsWhiteSpace(value[uriEnd - 1])) uriEnd--;
            output.Append(value.Substring(cursor, uriStart - cursor));
            string candidate = value.Substring(uriStart, uriEnd - uriStart);
            string? replacement = candidate.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? ExtractUri(candidate, sourceOffset + uriStart) : null;
            if (replacement == null) output.Append(candidate);
            else
            {
                output.Append(replacement);
                changed = true;
            }

            output.Append(value.Substring(uriEnd, end - uriEnd));
            if (quote != '\0')
            {
                output.Append(quote);
                end++;
                while (end < value.Length && char.IsWhiteSpace(value[end]))
                {
                    output.Append(value[end]);
                    end++;
                }
            }

            if (end < value.Length && value[end] == ')')
            {
                output.Append(')');
                end++;
            }

            cursor = end;
        }

        return new EmailHtmlDataUriRewrite(output.ToString(), changed);
    }

    string? ExtractUri(string uri, int sourceOffset)
    {
        if (uri.Length > limits.GetMaxUrlCharacters()) throw new EmailLimitException("MaxHtmlUrlCharacters", limits.GetMaxUrlCharacters(), uri.Length);
        EmailHtmlParsedDataUri parsed = ParseDataUri(uri);
        if (!parsed.IsValid())
        {
            rejectedDataUriCount++;
            return null;
        }

        if (extractions.Count >= limits.GetMaxDataUriCount()) throw new EmailLimitException("MaxHtmlDataUriCount", limits.GetMaxDataUriCount(), (long)extractions.Count + 1);
        string contentId = CreateContentId(parsed.GetMediaType(), parsed.GetBytes());
        if (contentId.Length + 4 > limits.GetMaxUrlCharacters()) throw new EmailLimitException("MaxHtmlUrlCharacters", limits.GetMaxUrlCharacters(), contentId.Length + 4);
        MimeEntity part;
        bool reused = partsByContentId.ContainsKey(contentId);
        if (reused)
        {
            part = partsByContentId[contentId];
        }
        else
        {
            long observed = extractedByteCount + parsed.GetBytes().Length;
            if (observed > limits.GetMaxDataUriBytes()) throw new EmailLimitException("MaxHtmlDataUriBytes", limits.GetMaxDataUriBytes(), observed);
            part = CreateRelatedPart(parsed.GetMediaType(), parsed.GetBytes(), contentId);
            partsByContentId.Add(contentId, part);
            relatedParts.Add(part);
            extractedByteCount = observed;
        }

        extractions.Add(new EmailHtmlDataUriExtraction(uri, contentId, parsed.GetMediaType() ?? throw new InvalidOperationException(), parsed.GetBytes().Length, sourceOffset, part, reused));
        return "cid:" + contentId;
    }

    EmailHtmlParsedDataUri ParseDataUri(string uri)
    {
        int comma = uri.IndexOf(',');
        if (comma < 5) return EmailHtmlParsedDataUri.Invalid();
        string header = uri.Substring(5, comma - 5);
        if (header.Length > options.GetMaximumMediaTypeCharacters()) throw new EmailLimitException("MaxDataUriMediaTypeCharacters", options.GetMaximumMediaTypeCharacters(), header.Length);
        bool base64 = header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);
        if (base64) header = header.Substring(0, header.Length - 7);
        if (header.Length == 0) header = "text/plain;charset=US-ASCII";
        EmailHtmlPercentDecodeResult decodedHeader = PercentDecode(header, options.GetMaximumMediaTypeCharacters());
        if (!decodedHeader.IsValid()) return EmailHtmlParsedDataUri.Invalid();
        byte[] headerBytes = decodedHeader.GetBytes();
        int headerIndex = 0;
        while (headerIndex < headerBytes.Length)
        {
            if (headerBytes[headerIndex] > 127) return EmailHtmlParsedDataUri.Invalid();
            headerIndex++;
        }

        EmailMediaTypeParseResult media = new EmailMediaTypeParser().ParseWithInputAndMode(headerBytes, EmailStructuredValueParseMode.Strict);
        if (!media.IsSuccess() || !media.IsComplete()) return EmailHtmlParsedDataUri.Invalid();
        string mediaTypeName = media.GetValue().GetNormalizedTopLevelType() + "/" + media.GetValue().GetNormalizedSubtype();
        if (!allowedMediaTypes.Contains(mediaTypeName)) return EmailHtmlParsedDataUri.Invalid();
        string payload = uri.Substring(comma + 1);
        byte[] bytes;
        if (base64)
        {
            EmailHtmlPercentDecodeResult decodedPayload = PercentDecode(payload, limits.GetMaxInputCharacters());
            if (!decodedPayload.IsValid() || !IsAsciiWithoutWhitespace(decodedPayload.GetBytes())) return EmailHtmlParsedDataUri.Invalid();
            EmailBase64DecodeResult decodedBase64 = new EmailBase64().Decode(decodedPayload.GetBytes(), EmailBase64DecodeMode.Strict, limits.GetMaxDataUriBytes());
            if (decodedBase64.HasFatalError()) throw new EmailLimitException("MaxHtmlDataUriBytes", limits.GetMaxDataUriBytes(), (long)limits.GetMaxDataUriBytes() + 1);
            if (!decodedBase64.IsSuccess()) return EmailHtmlParsedDataUri.Invalid();
            bytes = decodedBase64.GetDecodedBytes();
        }
        else
        {
            EmailHtmlPercentDecodeResult decodedPayload = PercentDecode(payload, limits.GetMaxDataUriBytes());
            if (!decodedPayload.IsValid()) return EmailHtmlParsedDataUri.Invalid();
            bytes = decodedPayload.GetBytes();
        }

        return new EmailHtmlParsedDataUri(media.GetValue(), bytes, true);
    }

    static EmailHtmlPercentDecodeResult PercentDecode(string value, long maximumBytes)
    {
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            if (bytes.Count >= maximumBytes) throw new EmailLimitException("MaxHtmlDataUriBytes", maximumBytes, (long)bytes.Count + 1);
            char selected = value[index];
            if (selected == '%')
            {
                if (index + 2 >= value.Length || !IsHex(value[index + 1]) || !IsHex(value[index + 2])) return EmailHtmlPercentDecodeResult.Invalid();
                bytes.Add((byte)(Hex(value[index + 1]) * 16 + Hex(value[index + 2])));
                index += 3;
            }
            else
            {
                if (selected > 127) return EmailHtmlPercentDecodeResult.Invalid();
                bytes.Add((byte)selected);
                index++;
            }
        }

        return new EmailHtmlPercentDecodeResult(bytes.ToArray(), true);
    }

    static string CreateContentId(EmailMediaType? mediaType, byte[] bytes)
    {
        byte[] type = new EmailValueWriter().WriteMediaTypeWithValue(mediaType).GetBytes();
        byte[] input = new byte[type.Length + bytes.Length + 1];
        type.CopyTo(input, 0);
        bytes.CopyTo(input, type.Length + 1);
        byte[] hash = SHA256.HashData(input);
        return "data-uri-" + ToHex(hash) + "@email.invalid";
    }

    static MimeEntity CreateRelatedPart(EmailMediaType? mediaType, byte[] bytes, string contentId)
    {
        int at = contentId.LastIndexOf('@');
        byte[] raw = Encoding.ASCII.GetBytes("<" + contentId + ">");
        EmailMessageId identifier = new EmailMessageId(contentId.Substring(0, at), contentId.Substring(at + 1), false, raw, new EmailSourcePosition(0, 1, 1), false, false);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), mediaType, null, "base64", identifier, null, null, null, null, new EmailContentOperations().CreateMemoryCopy(bytes, false), new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
    }

    string ApplyReplacements()
    {
        StringBuilder output = new StringBuilder();
        int cursor = 0;
        int index = 0;
        while (index < replacements.Count)
        {
            EmailHtmlSourceReplacement replacement = replacements[index];
            if (replacement.GetStart() < cursor) throw new InvalidOperationException("HTML data-URI replacements cannot overlap.");
            AppendBounded(output, html.Substring(cursor, replacement.GetStart() - cursor));
            AppendBounded(output, replacement.GetValue());
            cursor = replacement.GetStart() + replacement.GetLength();
            index++;
        }

        AppendBounded(output, html.Substring(cursor));
        return output.ToString();
    }

    void AppendBounded(StringBuilder output, string value)
    {
        long observed = (long)output.Length + value.Length;
        if (observed > limits.GetMaxOutputCharacters()) throw new EmailLimitException("MaxHtmlOutputCharacters", limits.GetMaxOutputCharacters(), observed);
        output.Append(value);
    }

    static string EscapeAttribute(string value)
    {
        return value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
    }

    static bool IsUrlAttribute(string name)
    {
        return string.Equals(name, "src", StringComparison.Ordinal) || string.Equals(name, "href", StringComparison.Ordinal) || string.Equals(name, "poster", StringComparison.Ordinal) || string.Equals(name, "background", StringComparison.Ordinal) || string.Equals(name, "xlink:href", StringComparison.Ordinal);
    }

    static bool IsAsciiWithoutWhitespace(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte selected = value[index];
            if (selected > 127 || selected == 32 || selected == 9 || selected == 13 || selected == 10 || selected == 11 || selected == 12) return false;
            index++;
        }

        return true;
    }

    static int IndexOfIgnoreCase(string value, string expected, int start)
    {
        int index = start;
        while (index + expected.Length <= value.Length)
        {
            if (string.Equals(value.Substring(index, expected.Length), expected, StringComparison.OrdinalIgnoreCase)) return index;
            index++;
        }

        return -1;
    }

    static bool StartsWithIgnoreCase(string value, int start, string expected)
    {
        return start + expected.Length <= value.Length && string.Equals(value.Substring(start, expected.Length), expected, StringComparison.OrdinalIgnoreCase);
    }

    static bool IsCssNameCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '-' || value == '_';
    }

    static bool IsHex(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    static int Hex(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        return value - 'a' + 10;
    }

    static string ToHex(byte[] value)
    {
        char[] characters = new char[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            int current = value[index];
            characters[index * 2] = HexCharacter(current >> 4);
            characters[index * 2 + 1] = HexCharacter(current & 15);
            index++;
        }

        return new string(characters);
    }

    static char HexCharacter(int value)
    {
        return (char)(value < 10 ? 48 + value : 87 + value);
    }
}
internal class EmailHtmlSourceReplacement
{
    int start;
    int length;
    string value;

    internal EmailHtmlSourceReplacement(int start, int length, string value)
    {
        if (start < 0) throw new ArgumentOutOfRangeException("start");
        if (length < 1) throw new ArgumentOutOfRangeException("length");
        if (value == null) throw new ArgumentNullException("value");
        this.start = start;
        this.length = length;
        this.value = value;
    }

    internal int GetStart() { return start; }
    internal int GetLength() { return length; }
    internal string GetValue() { return value; }
}
internal class EmailHtmlDataUriRewrite
{
    string value;
    bool changed;

    internal EmailHtmlDataUriRewrite(string value, bool changed)
    {
        this.value = value;
        this.changed = changed;
    }

    internal string GetValue() { return value; }
    internal bool WasChanged() { return changed; }
}
internal class EmailHtmlParsedDataUri
{
    EmailMediaType? mediaType;
    byte[] bytes;
    bool valid;

    internal EmailHtmlParsedDataUri(EmailMediaType? mediaType, byte[] bytes, bool valid)
    {
        this.mediaType = mediaType;
        this.bytes = bytes;
        this.valid = valid;
    }

    internal static EmailHtmlParsedDataUri Invalid() { return new EmailHtmlParsedDataUri(null, new byte[0], false); }
    internal EmailMediaType? GetMediaType() { return mediaType; }
    internal byte[] GetBytes() { return bytes; }
    internal bool IsValid() { return valid; }
}
internal class EmailHtmlPercentDecodeResult
{
    byte[] bytes;
    bool valid;

    internal EmailHtmlPercentDecodeResult(byte[] bytes, bool valid)
    {
        this.bytes = bytes;
        this.valid = valid;
    }

    internal static EmailHtmlPercentDecodeResult Invalid() { return new EmailHtmlPercentDecodeResult(new byte[0], false); }
    internal byte[] GetBytes() { return bytes; }
    internal bool IsValid() { return valid; }
}
