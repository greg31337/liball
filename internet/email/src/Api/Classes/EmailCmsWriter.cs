using System;
using System.Collections.Generic;
using System.Formats.Asn1;

namespace email;
/// <summary>
/// Writes CMS in its wire or storage format.
/// </summary>
public class EmailCmsWriter : IEmailCmsWriterContract
{
    internal static EmailCmsWriteResult WriteImplementationWithContentInfo(EmailCmsContentInfo contentInfo)
    {
        return WriteImplementationWithContentInfoAndOptions(contentInfo, new EmailCmsWriterOptions());
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "contentInfo">The content info used by the operation.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    public EmailCmsWriteResult WriteWithContentInfo(EmailCmsContentInfo contentInfo)
    {
        return EmailCmsWriter.WriteImplementationWithContentInfo(contentInfo);
    }

    internal static EmailCmsWriteResult WriteImplementationWithContentInfoAndOptions(EmailCmsContentInfo contentInfo, EmailCmsWriterOptions options)
    {
        if (contentInfo == null)
            throw new ArgumentNullException("contentInfo");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCmsWriterOptions snapshot = options.Clone();
        EmailIssueList issues = new EmailIssueList();
        EmailCmsDerEncoder encoder = new EmailCmsDerEncoder(snapshot.GetLimits(), issues);
        byte[]? content = encoder.Encode(contentInfo.GetContentNode());
        if (content == null)
            return new EmailCmsWriteResult(new byte[0], issues, contentInfo.WasBerEncoded());
        return WriteContentInfo(contentInfo.GetContentType(), content, contentInfo.WasBerEncoded(), snapshot.GetLimits(), issues);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "contentInfo">The content info used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCmsWriteResult WriteWithContentInfoAndOptions(EmailCmsContentInfo contentInfo, EmailCmsWriterOptions options)
    {
        return EmailCmsWriter.WriteImplementationWithContentInfoAndOptions(contentInfo, options);
    }

    internal static EmailCmsWriteResult WriteDataImplementationWithData(ReadOnlySpan<byte> data)
    {
        return WriteDataImplementationWithDataAndOptions(data, new EmailCmsWriterOptions());
    }

    /// <summary>
    /// Writes the supplied data to the requested destination.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    public EmailCmsWriteResult WriteDataWithData(ReadOnlySpan<byte> data)
    {
        return EmailCmsWriter.WriteDataImplementationWithData(data);
    }

    internal static EmailCmsWriteResult WriteDataImplementationWithDataAndOptions(ReadOnlySpan<byte> data, EmailCmsWriterOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCmsWriterOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (data.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsDataBytes", limits.GetMaxInputBytes(), data.Length);
        if (limits.GetMaxAsn1Depth() < 3)
            throw new EmailLimitException("MaxAsn1Depth", limits.GetMaxAsn1Depth(), 3);
        if (limits.GetMaxAsn1ElementCount() < 4)
            throw new EmailLimitException("MaxAsn1ElementCount", limits.GetMaxAsn1ElementCount(), 4);
        AsnWriter inner = new AsnWriter(AsnEncodingRules.DER);
        inner.WriteOctetString(data);
        return WriteContentInfo(EmailCmsReaderConstants.DataObjectIdentifier, inner.Encode(), false, limits, new EmailIssueList());
    }

    /// <summary>
    /// Writes the supplied data to the requested destination.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The CMS write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailCmsWriteResult WriteDataWithDataAndOptions(ReadOnlySpan<byte> data, EmailCmsWriterOptions options)
    {
        return EmailCmsWriter.WriteDataImplementationWithDataAndOptions(data, options);
    }

    static EmailCmsWriteResult WriteContentInfo(string contentType, byte[] encodedContent, bool normalizedFromBer, EmailCryptographicLimits limits, EmailIssueList issues)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteObjectIdentifier(contentType);
        Asn1Tag explicitTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        writer.PushSequence(explicitTag);
        writer.WriteEncodedValue(encodedContent);
        writer.PopSequence(explicitTag);
        writer.PopSequence();
        byte[] bytes = writer.Encode();
        if (bytes.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsOutputBytes", limits.GetMaxInputBytes(), bytes.LongLength);
        return new EmailCmsWriteResult(bytes, issues, normalizedFromBer);
    }
}
