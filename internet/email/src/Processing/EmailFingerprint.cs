using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
internal class EmailFingerprintFilter
{
    internal static EmailMessage FilterMessage(EmailMessage? source, EmailCanonicalComparisonOptions options)
    {
        EmailHeaders headers = FilterHeaders((source ?? throw new InvalidOperationException()).GetHeaders(), options);
        MimeEntity? body = source.HasBody() ? FilterEntity(source.GetBody(), options) : null;
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, (source.GetEnvelope() ?? throw new InvalidOperationException()).Clone(), null, source.GetOwnIssues());
    }

    internal static MimeEntity FilterEntity(MimeEntity? source, EmailCanonicalComparisonOptions options)
    {
        List<MimeEntity> children = new List<MimeEntity>();
        int index = 0;
        while (index < (source ?? throw new InvalidOperationException()).GetChildCount())
        {
            children.Add(FilterEntity(source.GetChildAt(index), options));
            index++;
        }

        EmailMessage? nested = source.HasNestedMessage() ? FilterMessage(source.GetNestedMessage(), options) : null;
        string? transferEncoding = source.HasTransferEncoding() ? source.GetTransferEncodingName() : null;
        EmailContent? rawContent = source.HasRawContent() ? source.GetRawContent() : null;
        EmailContent? decodedContent = source.HasDecodedContent() ? source.GetDecodedContent() : null;
        EmailBodyIngestMetadata? ingestMetadata = source.HasBodyIngestMetadata() ? source.GetBodyIngestMetadata() : null;
        if (source.HasContent() && (!source.HasTransferEncoding() || source.HasKnownTransferEncoding()))
        {
            transferEncoding = null;
            ingestMetadata = null;
            if (source.HasDecodedContent())
            {
                rawContent = null;
            }
            else if (source.GetEffectiveTransferEncodingKind() >= EmailTransferEncodingKind.QuotedPrintable)
            {
                using (MemoryStream output = new MemoryStream())
                {
                    new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(source, output, options.GetMaximumOutputBytes());
                    decodedContent = new EmailContentOperations().CreateMemoryCopy(output.ToArray(), false);
                    rawContent = null;
                }
            }
        }

        EmailMediaType mediaType = NormalizeCharset(source.GetMediaType());
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(source.GetKind(), FilterHeaders(source.GetHeaders(), options), mediaType, source.HasDisposition() ? source.GetDisposition() : null, transferEncoding, source.HasContentId() ? source.GetContentId() : null, source.HasContentLocation() ? source.GetContentLocation() : null, source.HasContentLanguage() ? source.GetContentLanguage() : null, source.HasContentDescription() ? source.GetContentDescription() : null, source.HasPartialMessageMetadata() ? source.GetPartialMessageMetadata() : null, source.HasExternalBodyMetadata() ? source.GetExternalBodyMetadata() : null, ingestMetadata, rawContent, decodedContent, children, nested, source.HasPreamble() ? source.GetPreamble() : null, source.HasEpilogue() ? source.GetEpilogue() : null, source.HasSourceRange() ? source.GetSourceRange() : null, source.GetOwnIssues());
    }

    static EmailMediaType NormalizeCharset(EmailMediaType? mediaType)
    {
        EmailParameter? charset = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetFirst("charset");
        if (charset == null)
        {
            return mediaType;
        }

        string current = Encoding.ASCII.GetString(charset.GetValueBytes());
        EmailCharsetLookupResult lookup = new EmailCharsetRegistry().Lookup(current);
        if (!lookup.IsFound() || string.Equals(current, lookup.GetCanonicalName(), StringComparison.Ordinal))
        {
            return mediaType;
        }

        List<EmailParameter> physical = new List<EmailParameter>();
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        EmailParameterList original = mediaType.GetParameters();
        int index = 0;
        while (index < original.GetCount())
        {
            if (!string.Equals(original.GetAt(index).GetName(), "charset", StringComparison.OrdinalIgnoreCase))
            {
                physical.Add(original.GetAt(index));
            }

            index++;
        }

        index = 0;
        while (index < original.GetDecodedCount())
        {
            if (!string.Equals(original.GetDecodedAt(index).GetName(), "charset", StringComparison.OrdinalIgnoreCase))
            {
                decoded.Add(original.GetDecodedAt(index));
            }

            index++;
        }

        byte[] bytes = Encoding.ASCII.GetBytes((lookup.GetCanonicalName() ?? throw new InvalidOperationException()));
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        physical.Add(new EmailParameter("charset", bytes, bytes, false, new byte[0], position));
        decoded.Add(new EmailRfc2231DecodedParameter("charset", bytes, null, false, null, false, false, false, 1, new List<EmailRfc2231Parameter>()));
        return new EmailMediaType(mediaType.GetTopLevelType(), mediaType.GetSubtype(), new EmailParameterList(physical, decoded), new byte[0], mediaType.GetSourcePosition(), false);
    }

    static EmailHeaders FilterHeaders(EmailHeaders? source, EmailCanonicalComparisonOptions options)
    {
        List<EmailHeader> headers = new List<EmailHeader>();
        int index = 0;
        while (index < (source ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = source.GetAt(index);
            if (!IsIgnored(header.GetName(), options))
            {
                headers.Add(header.Clone());
            }

            index++;
        }

        if (options.GetIgnoreHeaderOrder())
        {
            SortHeaders(headers);
        }

        return new EmailHeadersOperations().CreateWithPreservedHeaders(headers);
    }

    static bool IsIgnored(string name, EmailCanonicalComparisonOptions options)
    {
        if (options.ContainsIgnoredHeaderName(name))
        {
            return true;
        }

        if (options.GetIgnoreReceivedHeaders() && string.Equals(name, "Received", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (options.GetIgnoreAuthenticationHeaders() && IsAuthenticationHeader(name))
        {
            return true;
        }

        if (options.GetIgnoreDateHeaders() && (string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Date", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (options.GetIgnoreMessageIdentifiers() && IsIdentifierHeader(name))
        {
            return true;
        }

        return options.GetIgnoreSubjectHeaders() && string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsIdentifierHeader(string name)
    {
        return EqualsAny(name, new string[] { "Message-ID", "Resent-Message-ID", "In-Reply-To", "References", "Content-ID" });
    }

    static bool IsAuthenticationHeader(string name)
    {
        return EqualsAny(name, new string[] { "Authentication-Results", "Original-Authentication-Results", "Received-SPF", "DKIM-Signature", "DomainKey-Signature", "ARC-Authentication-Results", "ARC-Message-Signature", "ARC-Seal" });
    }

    static bool EqualsAny(string value, string[] choices)
    {
        int index = 0;
        while (index < choices.Length)
        {
            if (string.Equals(value, choices[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void SortHeaders(List<EmailHeader> headers)
    {
        int index = 1;
        while (index < headers.Count)
        {
            EmailHeader current = headers[index];
            int insertion = index;
            while (insertion > 0 && CompareHeaders(headers[insertion - 1], current) > 0)
            {
                headers[insertion] = headers[insertion - 1];
                insertion--;
            }

            headers[insertion] = current;
            index++;
        }
    }

    static int CompareHeaders(EmailHeader left, EmailHeader right)
    {
        int nameComparison = string.Compare(left.GetNormalizedName(), right.GetNormalizedName(), StringComparison.Ordinal);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        string leftValue = Encoding.UTF8.GetString(left.GetUnfoldedValue()).Trim();
        string rightValue = Encoding.UTF8.GetString(right.GetUnfoldedValue()).Trim();
        return string.Compare(leftValue, rightValue, StringComparison.Ordinal);
    }
}
