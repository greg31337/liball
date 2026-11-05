using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;

namespace email;
/// <summary>
/// Reads and interprets CMS.
/// </summary>
public class EmailCmsReader : IEmailCmsReaderContract
{
    /// <summary>
    /// The data object identifier value exposed by this API.
    /// </summary>
    internal const string DataObjectIdentifier = "1.2.840.113549.1.7.1";
    /// <summary>
    /// The signed data object identifier value exposed by this API.
    /// </summary>
    internal const string SignedDataObjectIdentifier = "1.2.840.113549.1.7.2";
    /// <summary>
    /// The enveloped data object identifier value exposed by this API.
    /// </summary>
    internal const string EnvelopedDataObjectIdentifier = "1.2.840.113549.1.7.3";
    /// <summary>
    /// The signed and enveloped data object identifier value exposed by this API.
    /// </summary>
    internal const string SignedAndEnvelopedDataObjectIdentifier = "1.2.840.113549.1.7.4";
    /// <summary>
    /// The digested data object identifier value exposed by this API.
    /// </summary>
    internal const string DigestedDataObjectIdentifier = "1.2.840.113549.1.7.5";
    /// <summary>
    /// The encrypted data object identifier value exposed by this API.
    /// </summary>
    internal const string EncryptedDataObjectIdentifier = "1.2.840.113549.1.7.6";
    /// <summary>
    /// The authenticated data object identifier value exposed by this API.
    /// </summary>
    internal const string AuthenticatedDataObjectIdentifier = "1.2.840.113549.1.9.16.1.2";
    /// <summary>
    /// The compressed data object identifier value exposed by this API.
    /// </summary>
    internal const string CompressedDataObjectIdentifier = "1.2.840.113549.1.9.16.1.9";
    /// <summary>
    /// The auth enveloped data object identifier value exposed by this API.
    /// </summary>
    internal const string AuthEnvelopedDataObjectIdentifier = "1.2.840.113549.1.9.16.1.23";
    internal static EmailCmsReadResult ParseImplementationWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input)
    {
        return ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(input, new EmailCmsReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCmsReadResult ParseWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input)
    {
        return EmailCmsReader.ParseImplementationWithInputUsingReadOnlySpan(input);
    }

    internal static EmailCmsReadResult ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(ReadOnlySpan<byte> input, EmailCmsReaderOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCmsReaderOptions snapshot = options.Clone();
        byte[] bytes = input.ToArray();
        ValidateInputLength(bytes.LongLength, snapshot.GetLimits());
        return ParseBytes(bytes, snapshot);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCmsReadResult ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(ReadOnlySpan<byte> input, EmailCmsReaderOptions options)
    {
        return EmailCmsReader.ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(input, options);
    }

    internal static EmailCmsReadResult ParseImplementationWithInputUsingStream(Stream input)
    {
        return ParseImplementationWithInputAndOptionsUsingStreamAndEmailCmsReaderOptions(input, new EmailCmsReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCmsReadResult ParseWithInputUsingStream(Stream input)
    {
        return EmailCmsReader.ParseImplementationWithInputUsingStream(input);
    }

    internal static EmailCmsReadResult ParseImplementationWithInputAndOptionsUsingStreamAndEmailCmsReaderOptions(Stream input, EmailCmsReaderOptions options)
    {
        if (input == null)
            throw new ArgumentNullException("input");
        if (!input.CanRead)
            throw new ArgumentException("The CMS input stream must be readable.", "input");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCmsReaderOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (input.CanSeek)
        {
            long remaining = input.Length - input.Position;
            if (remaining < 0)
                throw new ArgumentException("The stream position exceeds its length.", "input");
            ValidateInputLength(remaining, limits);
        }

        byte[] buffer = new byte[snapshot.GetReadBufferBytes()];
        using (MemoryStream output = new MemoryStream())
        {
            while (true)
            {
                int count = input.Read(buffer, 0, buffer.Length);
                if (count == 0)
                    break;
                long observed = output.Length + count;
                if (observed > limits.GetMaxInputBytes())
                    throw new EmailLimitException("MaxCmsInputBytes", limits.GetMaxInputBytes(), observed);
                output.Write(buffer, 0, count);
            }

            return ParseBytes(output.ToArray(), snapshot);
        }
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailCmsReadResult ParseWithInputAndOptionsUsingStreamAndEmailCmsReaderOptions(Stream input, EmailCmsReaderOptions options)
    {
        return EmailCmsReader.ParseImplementationWithInputAndOptionsUsingStreamAndEmailCmsReaderOptions(input, options);
    }

    internal static EmailCmsReadResult ParseImplementationWithEntity(MimeEntity entity)
    {
        return ParseImplementationWithEntityAndOptions(entity, new EmailCmsReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCmsReadResult ParseWithEntity(MimeEntity entity)
    {
        return EmailCmsReader.ParseImplementationWithEntity(entity);
    }

    internal static EmailCmsReadResult ParseImplementationWithEntityAndOptions(MimeEntity entity, EmailCmsReaderOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailIssueList issues = new EmailIssueList();
        if (!entity.HasContent())
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsContentInfoMalformed, "The MIME entity has no CMS content.", 0, "No CMS ContentInfo value was parsed."));
            return new EmailCmsReadResult(null, issues, false, 0, 0);
        }

        EmailCmsReaderOptions snapshot = options.Clone();
        EmailContent? content = entity.HasDecodedContent() ? entity.GetDecodedContent() : entity.GetContent();
        byte[] bytes = (content ?? throw new InvalidOperationException()).Materialize(snapshot.GetLimits().GetMaxInputBytes());
        return ParseBytes(bytes, snapshot);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCmsReadResult ParseWithEntityAndOptions(MimeEntity entity, EmailCmsReaderOptions options)
    {
        return EmailCmsReader.ParseImplementationWithEntityAndOptions(entity, options);
    }

    static EmailCmsReadResult ParseBytes(byte[] bytes, EmailCmsReaderOptions options)
    {
        EmailIssueList issues = new EmailIssueList();
        if (bytes.Length == 0)
        {
            issues.Add(Issue(EmailIssueSeverity.Fatal, EmailIssueCode.CmsContentInfoMalformed, "The CMS input is empty.", 0, "No CMS ContentInfo value was parsed."));
            return new EmailCmsReadResult(null, issues, false, 0, 0);
        }

        EmailCryptographicLimits limits = options.GetLimits();
        EmailCmsScanResult scan = EmailCmsAsn1Scanner.Scan(bytes, AsnEncodingRules.DER, limits);
        EmailCmsEncodingKind encoding = EmailCmsEncodingKind.Der;
        if (!scan.IsValid() && options.GetAllowBer())
        {
            scan = EmailCmsAsn1Scanner.Scan(bytes, AsnEncodingRules.BER, limits);
            encoding = EmailCmsEncodingKind.Ber;
        }

        if (!scan.IsValid())
        {
            issues.Add(Issue(EmailIssueSeverity.Fatal, EmailIssueCode.CmsContentInfoMalformed, "The CMS input is not a complete structurally valid DER or permitted BER value.", scan.GetErrorOffset(), "The input was rejected without attempting cryptographic processing."));
            return new EmailCmsReadResult(null, issues, false, scan.GetElementCount(), scan.GetMaximumDepth());
        }

        if (encoding == EmailCmsEncodingKind.Ber)
        {
            issues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.CmsBerEncodingAccepted, "The CMS input uses BER features that are not valid DER.", 0, "The original BER bytes were retained and can be normalized by the CMS DER writer."));
        }

        if (scan.GetRootCount() != 1)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsContentInfoMalformed, "CMS input must contain exactly one top-level ContentInfo value.", 0, "Every ASN.1 element was bounded and inspected, but no ContentInfo result was produced."));
            return new EmailCmsReadResult(null, issues, false, scan.GetElementCount(), scan.GetMaximumDepth());
        }

        EmailCmsAsn1Node root = scan.GetRootAt(0);
        if (!root.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !root.IsConstructed() || root.GetChildCount() != 2)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsContentInfoMalformed, "CMS ContentInfo must be a two-field SEQUENCE.", 0, "The bounded ASN.1 tree was retained, but no ContentInfo result was produced."));
            return new EmailCmsReadResult(null, issues, false, scan.GetElementCount(), scan.GetMaximumDepth());
        }

        EmailCmsAsn1Node oidNode = root.GetChildAt(0);
        if (!IsValidObjectIdentifierNode(oidNode))
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsObjectIdentifierMalformed, "CMS ContentInfo begins with an invalid content type object identifier.", 0, "The encoded object identifier was retained without decoding it."));
            return new EmailCmsReadResult(null, issues, false, scan.GetElementCount(), scan.GetMaximumDepth());
        }

        AsnEncodingRules rules = encoding == EmailCmsEncodingKind.Der ? AsnEncodingRules.DER : AsnEncodingRules.BER;
        AsnReader oidReader = new AsnReader(oidNode.GetEncodedBytes(), rules, new AsnReaderOptions());
        string contentType = oidReader.ReadObjectIdentifier();
        EmailCmsAsn1Node explicitContent = root.GetChildAt(1);
        if (!explicitContent.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0) || !explicitContent.IsConstructed() || explicitContent.GetChildCount() != 1)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsContentMalformed, "CMS ContentInfo content must be one value in an explicit context-specific tag zero.", 0, "The malformed content wrapper was retained without interpreting its payload."));
            return new EmailCmsReadResult(null, issues, false, scan.GetElementCount(), scan.GetMaximumDepth());
        }

        EmailCmsAsn1Node contentNode = explicitContent.GetChildAt(0);
        EmailCmsContentKind kind = GetContentKind(contentType);
        byte[]? data = null;
        bool contentValid = true;
        if (kind == EmailCmsContentKind.Data)
        {
            EmailCmsOctetCollector collector = new EmailCmsOctetCollector(limits.GetMaxInputBytes());
            collector.Add(contentNode);
            if (!collector.IsValid())
            {
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsContentMalformed, "CMS data content is not a primitive or constructed OCTET STRING.", 0, "The encoded content was retained without exposing data octets."));
                contentValid = false;
            }
            else
            {
                data = collector.GetBytes();
            }
        }
        else if (kind != EmailCmsContentKind.Unknown && (!contentNode.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !contentNode.IsConstructed()))
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CmsContentMalformed, "The recognized CMS content type does not contain its required SEQUENCE value.", 0, "The encoded content was retained without interpreting its fields."));
            contentValid = false;
        }

        EmailCmsContentInfo contentInfo = new EmailCmsContentInfo(contentType, kind, encoding, root, contentNode, data, bytes);
        return new EmailCmsReadResult(contentInfo, issues, contentValid, scan.GetElementCount(), scan.GetMaximumDepth());
    }

    static EmailCmsContentKind GetContentKind(string oid)
    {
        if (string.Equals(oid, DataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.Data;
        if (string.Equals(oid, SignedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.SignedData;
        if (string.Equals(oid, EnvelopedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.EnvelopedData;
        if (string.Equals(oid, SignedAndEnvelopedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.SignedAndEnvelopedData;
        if (string.Equals(oid, DigestedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.DigestedData;
        if (string.Equals(oid, EncryptedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.EncryptedData;
        if (string.Equals(oid, AuthenticatedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.AuthenticatedData;
        if (string.Equals(oid, CompressedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.CompressedData;
        if (string.Equals(oid, AuthEnvelopedDataObjectIdentifier, StringComparison.Ordinal))
            return EmailCmsContentKind.AuthEnvelopedData;
        return EmailCmsContentKind.Unknown;
    }

    static bool IsValidObjectIdentifierNode(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 6) || node.IsConstructed())
            return false;
        byte[] content = node.GetContentBytes();
        if (content.Length == 0)
            return false;
        int index = 0;
        while (index < content.Length)
        {
            if (content[index] == 128)
                return false;
            while ((content[index] & 128) != 0)
            {
                index++;
                if (index >= content.Length)
                    return false;
            }

            index++;
        }

        return true;
    }

    static void ValidateInputLength(long length, EmailCryptographicLimits limits)
    {
        if (length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsInputBytes", limits.GetMaxInputBytes(), length);
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, long offset, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, new EmailSourcePosition(offset, 1, 1), null, null, recovery);
    }
}
