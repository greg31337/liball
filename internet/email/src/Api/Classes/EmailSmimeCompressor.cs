using System;
using System.Formats.Asn1;
using System.IO;
using System.IO.Compression;

namespace email;
/// <summary>
/// Provides operations for working with S/MIME compressor.
/// </summary>
public class EmailSmimeCompressor : IEmailSmimeCompressorContract
{
    /// <summary>
    /// The zlib compression object identifier value exposed by this API.
    /// </summary>
    internal const string ZlibCompressionObjectIdentifier = "1.2.840.113549.1.9.16.3.8";
    internal static EmailSmimeCompressionResult CompressImplementationWithContent(ReadOnlySpan<byte> content)
    {
        return CompressImplementationWithContentAndOptions(content, new EmailSmimeCompressionOptions());
    }

    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The S/MIME compression result produced by the operation.</returns>
    public EmailSmimeCompressionResult CompressWithContent(ReadOnlySpan<byte> content)
    {
        return EmailSmimeCompressor.CompressImplementationWithContent(content);
    }

    internal static EmailSmimeCompressionResult CompressImplementationWithContentAndOptions(ReadOnlySpan<byte> content, EmailSmimeCompressionOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailSmimeCompressionOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (content.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxSmimeContentBytes", limits.GetMaxInputBytes(), content.Length);
        if (limits.GetMaxOperationCount() < 1)
            throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), 1);
        EmailIssueList issues = new EmailIssueList();
        byte[] compressed;
        using (MemoryStream output = new MemoryStream())
        {
            using (ZLibStream zlib = new ZLibStream(output, snapshot.GetCompressionLevel(), true))
                zlib.Write(content);
            compressed = output.ToArray();
        }

        if (compressed.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCompressedContentBytes", limits.GetMaxInputBytes(), compressed.LongLength);
        byte[] cms = Encode(compressed);
        if (cms.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsOutputBytes", limits.GetMaxInputBytes(), cms.LongLength);
        EmailCmsReaderOptions readerOptions = new EmailCmsReaderOptions();
        readerOptions.SetLimits(limits);
        readerOptions.SetAllowBer(false);
        EmailCmsReadResult validation = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(cms, readerOptions);
        if (!validation.IsSuccess() || (validation.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() != EmailCmsContentKind.CompressedData)
        {
            issues.Add(Issue(EmailIssueCode.SmimeCompressionFailed, "The generated CMS CompressedData value failed DER validation.", "The invalid output was withheld."));
            return new EmailSmimeCompressionResult(new byte[0], issues, content.Length, compressed.Length);
        }

        return new EmailSmimeCompressionResult(cms, issues, content.Length, compressed.Length);
    }

    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME compression result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailSmimeCompressionResult CompressWithContentAndOptions(ReadOnlySpan<byte> content, EmailSmimeCompressionOptions options)
    {
        return EmailSmimeCompressor.CompressImplementationWithContentAndOptions(content, options);
    }

    static byte[] Encode(byte[] compressed)
    {
        AsnWriter value = new AsnWriter(AsnEncodingRules.DER);
        value.PushSequence();
        value.WriteInteger(0);
        value.PushSequence();
        value.WriteObjectIdentifier(ZlibCompressionObjectIdentifier);
        value.PopSequence();
        value.PushSequence();
        value.WriteObjectIdentifier(EmailCmsReaderConstants.DataObjectIdentifier);
        Asn1Tag contentTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        value.PushSequence(contentTag);
        value.WriteOctetString(compressed);
        value.PopSequence(contentTag);
        value.PopSequence();
        value.PopSequence();
        AsnWriter contentInfo = new AsnWriter(AsnEncodingRules.DER);
        contentInfo.PushSequence();
        contentInfo.WriteObjectIdentifier(EmailCmsReaderConstants.CompressedDataObjectIdentifier);
        Asn1Tag explicitTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        contentInfo.PushSequence(explicitTag);
        contentInfo.WriteEncodedValue(value.Encode());
        contentInfo.PopSequence(explicitTag);
        contentInfo.PopSequence();
        return contentInfo.Encode();
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, recovery);
    }
}
