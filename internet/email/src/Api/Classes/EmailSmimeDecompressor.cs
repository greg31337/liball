using System;
using System.Formats.Asn1;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace email;
/// <summary>
/// Provides operations for working with S/MIME decompressor.
/// </summary>
public class EmailSmimeDecompressor : IEmailSmimeDecompressorContract
{
    internal static EmailSmimeDecompressionResult DecompressImplementationWithCms(ReadOnlySpan<byte> cms)
    {
        return DecompressImplementationWithCmsAndOptions(cms, new EmailSmimeDecompressionOptions());
    }

    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    public EmailSmimeDecompressionResult DecompressWithCms(ReadOnlySpan<byte> cms)
    {
        return EmailSmimeDecompressor.DecompressImplementationWithCms(cms);
    }

    internal static EmailSmimeDecompressionResult DecompressImplementationWithCmsAndOptions(ReadOnlySpan<byte> cms, EmailSmimeDecompressionOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        return DecompressBytes(cms.ToArray(), options.Clone());
    }

    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeDecompressionResult DecompressWithCmsAndOptions(ReadOnlySpan<byte> cms, EmailSmimeDecompressionOptions options)
    {
        return EmailSmimeDecompressor.DecompressImplementationWithCmsAndOptions(cms, options);
    }

    internal static EmailSmimeDecompressionResult DecompressImplementationWithEntity(MimeEntity entity)
    {
        return DecompressImplementationWithEntityAndOptions(entity, new EmailSmimeDecompressionOptions());
    }

    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    public EmailSmimeDecompressionResult DecompressWithEntity(MimeEntity entity)
    {
        return EmailSmimeDecompressor.DecompressImplementationWithEntity(entity);
    }

    internal static EmailSmimeDecompressionResult DecompressImplementationWithEntityAndOptions(MimeEntity entity, EmailSmimeDecompressionOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailSmimeDecompressionOptions snapshot = options.Clone();
        using (MemoryStream decoded = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(entity, decoded, snapshot.GetLimits().GetMaxInputBytes());
            return DecompressBytes(decoded.ToArray(), snapshot);
        }
    }

    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decompression result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeDecompressionResult DecompressWithEntityAndOptions(MimeEntity entity, EmailSmimeDecompressionOptions options)
    {
        return EmailSmimeDecompressor.DecompressImplementationWithEntityAndOptions(entity, options);
    }

    static EmailSmimeDecompressionResult DecompressBytes(byte[] cms, EmailSmimeDecompressionOptions options)
    {
        EmailCryptographicLimits limits = options.GetLimits();
        if (cms.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsInputBytes", limits.GetMaxInputBytes(), cms.LongLength);
        if (limits.GetMaxOperationCount() < 1)
            throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), 1);
        EmailCmsReaderOptions readerOptions = new EmailCmsReaderOptions();
        readerOptions.SetLimits(limits);
        EmailCmsReadResult parsed = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(cms, readerOptions);
        EmailIssueList issues = parsed.GetIssues();
        if (!parsed.IsSuccess() || (parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() != EmailCmsContentKind.CompressedData)
            return Failure(EmailSmimeDecompressionStatus.Malformed, EmailIssueCode.SmimeCompressedDataMalformed, "The input is not a complete CMS CompressedData value.", cms, new byte[0], string.Empty, string.Empty, issues);
        EmailSmimeCompressedDataLayout? layout = ParseLayout((parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentNode(), limits);
        if (layout == null)
            return Failure(EmailSmimeDecompressionStatus.Malformed, EmailIssueCode.SmimeCompressedDataMalformed, "The CMS CompressedData fields are malformed.", cms, new byte[0], string.Empty, string.Empty, issues);
        if (!string.Equals(layout.GetAlgorithmObjectIdentifier(), EmailSmimeCompressorConstants.ZlibCompressionObjectIdentifier, StringComparison.Ordinal))
        {
            issues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.SmimeCompressionAlgorithmUnsupported, "The CMS compression algorithm is unsupported.", "The original CMS value and compressed content were retained exactly."));
            return new EmailSmimeDecompressionResult(EmailSmimeDecompressionStatus.UnsupportedAlgorithm, null, layout.GetCompressedContent(), cms, layout.GetAlgorithmObjectIdentifier(), layout.GetContentTypeObjectIdentifier(), issues);
        }

        if (!layout.HasPermittedZlibParameters())
            return Failure(EmailSmimeDecompressionStatus.Malformed, EmailIssueCode.SmimeCompressedDataMalformed, "The zlib AlgorithmIdentifier parameters must be absent or NULL.", cms, layout.GetCompressedContent(), layout.GetAlgorithmObjectIdentifier(), layout.GetContentTypeObjectIdentifier(), issues);
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() => Inflate(layout.GetCompressedContent(), options.GetReadBufferBytes(), limits.GetMaxDecompressedBytes()));
        if (captured.IsSuccess())
            return new EmailSmimeDecompressionResult(EmailSmimeDecompressionStatus.Success, (byte[]? )captured.GetValue(), layout.GetCompressedContent(), cms, layout.GetAlgorithmObjectIdentifier(), layout.GetContentTypeObjectIdentifier(), issues);
        if (captured.GetException() is InvalidDataException)
            return Failure(EmailSmimeDecompressionStatus.DecompressionFailed, EmailIssueCode.SmimeDecompressionFailed, "The zlib-compressed CMS content is invalid or truncated.", cms, layout.GetCompressedContent(), layout.GetAlgorithmObjectIdentifier(), layout.GetContentTypeObjectIdentifier(), issues);
        if (captured.GetException() is IOException)
            return Failure(EmailSmimeDecompressionStatus.DecompressionFailed, EmailIssueCode.SmimeDecompressionFailed, "The zlib-compressed CMS content could not be read.", cms, layout.GetCompressedContent(), layout.GetAlgorithmObjectIdentifier(), layout.GetContentTypeObjectIdentifier(), issues);
        throw (captured.GetException() ?? throw new InvalidOperationException());
    }

    static EmailSmimeCompressedDataLayout? ParseLayout(EmailCmsAsn1Node node, EmailCryptographicLimits limits)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() != 3)
            return null;
        if (!IsZeroInteger(node.GetChildAt(0)))
            return null;
        EmailCmsAsn1Node algorithm = node.GetChildAt(1);
        if (!algorithm.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !algorithm.IsConstructed() || algorithm.GetChildCount() < 1 || algorithm.GetChildCount() > 2)
            return null;
        string? algorithmOid = ReadObjectIdentifier(algorithm.GetChildAt(0));
        if (algorithmOid == null)
            return null;
        bool permittedZlibParameters = algorithm.GetChildCount() == 1 || IsNull(algorithm.GetChildAt(1));
        EmailCmsAsn1Node encapsulated = node.GetChildAt(2);
        if (!encapsulated.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !encapsulated.IsConstructed() || encapsulated.GetChildCount() != 2)
            return null;
        string? contentTypeOid = ReadObjectIdentifier(encapsulated.GetChildAt(0));
        if (contentTypeOid == null)
            return null;
        EmailCmsAsn1Node wrapper = encapsulated.GetChildAt(1);
        if (!wrapper.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0) || !wrapper.IsConstructed() || wrapper.GetChildCount() != 1)
            return null;
        EmailCmsOctetCollector collector = new EmailCmsOctetCollector(limits.GetMaxInputBytes());
        collector.Add(wrapper.GetChildAt(0));
        if (!collector.IsValid())
            return null;
        return new EmailSmimeCompressedDataLayout(algorithmOid, contentTypeOid, collector.GetBytes(), permittedZlibParameters);
    }

    static byte[] Inflate(byte[] compressed, int readBufferBytes, long maximumBytes)
    {
        using (MemoryStream input = new MemoryStream(compressed, false))
        using (ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress))
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[readBufferBytes];
            while (true)
            {
                int count = zlib.Read(buffer, 0, buffer.Length);
                if (count == 0)
                    break;
                long observed = output.Length + count;
                if (observed > maximumBytes)
                    throw new EmailLimitException("MaxDecompressedBytes", maximumBytes, observed);
                output.Write(buffer, 0, count);
            }

            return output.ToArray();
        }
    }

    static bool IsZeroInteger(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 2) || node.IsConstructed())
            return false;
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            AsnReader reader = new AsnReader(node.GetEncodedBytes(), AsnEncodingRules.BER);
            BigInteger value = reader.ReadInteger();
            return value.IsZero && !reader.HasData;
        });
        return captured.IsSuccess() && (bool)(captured.GetValue() ?? throw new InvalidOperationException());
    }

    static bool IsNull(EmailCmsAsn1Node node)
    {
        return node.HasTag(EmailCmsAsn1TagClass.Universal, 5) && !node.IsConstructed() && node.GetContentBytes().Length == 0;
    }

    static string? ReadObjectIdentifier(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 6) || node.IsConstructed())
            return null;
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            AsnReader reader = new AsnReader(node.GetEncodedBytes(), AsnEncodingRules.BER);
            string value = reader.ReadObjectIdentifier();
            return reader.HasData ? null : value;
        });
        return captured.IsSuccess() ? (string? )captured.GetValue() : null;
    }

    static EmailSmimeDecompressionResult Failure(EmailSmimeDecompressionStatus status, EmailIssueCode code, string description, byte[] cms, byte[] compressed, string algorithmOid, string contentTypeOid, EmailIssueList issues)
    {
        issues.Add(Issue(EmailIssueSeverity.Error, code, description, "No decompressed content was released."));
        return new EmailSmimeDecompressionResult(status, null, compressed, cms, algorithmOid, contentTypeOid, issues);
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, null, recovery);
    }
}
