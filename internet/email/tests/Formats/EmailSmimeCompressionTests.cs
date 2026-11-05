using System.Formats.Asn1;
using System.IO.Compression;
using System.Text;
using email;

namespace email.Tests;

public class EmailSmimeCompressionTests
{
    [Test]
    public void CompressesToCmsZlibAndDecompressesExactContent()
    {
        byte[] content = Bytes(string.Concat(Enumerable.Repeat("Content-Type: text/plain\r\n\r\ncompressible content\r\n", 50)));
        EmailSmimeCompressionOptions options = new EmailSmimeCompressionOptions();
        options.SetCompressionLevel(CompressionLevel.SmallestSize);

        EmailSmimeCompressionResult compressed = new EmailSmimeCompressor().CompressWithContentAndOptions(content, options);
        EmailCmsReadResult parsed = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(compressed.GetCmsBytes());
        EmailSmimeDecompressionResult decompressed = new EmailSmimeDecompressor().DecompressWithCms(compressed.GetCmsBytes());

        Assert.Multiple(() =>
        {
            Assert.That(compressed.IsSuccess(), Is.True);
            Assert.That(compressed.GetIssues().GetCount(), Is.Zero);
            Assert.That(compressed.GetUncompressedBytes(), Is.EqualTo(content.Length));
            Assert.That(compressed.GetCompressedBytes(), Is.LessThan(content.Length));
            Assert.That(parsed.IsSuccess(), Is.True);
            Assert.That(parsed.GetContentInfo()!.GetContentKind(), Is.EqualTo(EmailCmsContentKind.CompressedData));
            Assert.That(decompressed.IsSuccess(), Is.True);
            Assert.That(decompressed.GetStatus(), Is.EqualTo(EmailSmimeDecompressionStatus.Success));
            Assert.That(decompressed.GetContent(), Is.EqualTo(content));
            Assert.That(decompressed.GetAlgorithmObjectIdentifier(), Is.EqualTo(EmailSmimeCompressorConstants.ZlibCompressionObjectIdentifier));
            Assert.That(decompressed.GetContentTypeObjectIdentifier(), Is.EqualTo(EmailCmsReaderConstants.DataObjectIdentifier));
            Assert.That(decompressed.GetCompressedContent().Length, Is.EqualTo(compressed.GetCompressedBytes()));
        });

        byte[] returnedCms = compressed.GetCmsBytes();
        returnedCms[0] = 0;
        byte[] returnedContent = decompressed.GetContent();
        returnedContent[0] = 0;
        Assert.Multiple(() =>
        {
            Assert.That(compressed.GetCmsBytes()[0], Is.EqualTo(0x30));
            Assert.That(decompressed.GetContent(), Is.EqualTo(content));
        });
    }

    [Test]
    public void HandlesEmptyContentAndPermittedNullAlgorithmParameters()
    {
        EmailSmimeCompressionResult empty = new EmailSmimeCompressor().CompressWithContent(new byte[0]);
        EmailSmimeDecompressionResult emptyResult = new EmailSmimeDecompressor().DecompressWithCms(empty.GetCmsBytes());
        byte[] content = Bytes("zlib with NULL parameters");
        byte[] compressedContent = CompressedContent(new EmailSmimeCompressor().CompressWithContent(content).GetCmsBytes());
        byte[] cmsWithNull = WrapCompressed(EmailSmimeCompressorConstants.ZlibCompressionObjectIdentifier, compressedContent, AlgorithmParameters.Null, 0);

        EmailSmimeDecompressionResult withNull = new EmailSmimeDecompressor().DecompressWithCms(cmsWithNull);

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsSuccess(), Is.True);
            Assert.That(emptyResult.IsSuccess(), Is.True);
            Assert.That(emptyResult.GetContent(), Is.Empty);
            Assert.That(withNull.IsSuccess(), Is.True);
            Assert.That(withNull.GetContent(), Is.EqualTo(content));
        });
    }

    [Test]
    public void DecompressesBase64MimeEntity()
    {
        byte[] content = Bytes("MIME compressed content");
        EmailSmimeCompressionResult compressed = new EmailSmimeCompressor().CompressWithContent(content);
        string source = "Content-Type: application/pkcs7-mime; smime-type=compressed-data\r\nContent-Transfer-Encoding: base64\r\n\r\n" + Convert.ToBase64String(compressed.GetCmsBytes());
        EmailEntityParseResult entity = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());

        EmailSmimeDecompressionResult result = new EmailSmimeDecompressor().DecompressWithEntity(entity.GetEntity()!);
        EmailSmimeDecompressionResult withOptions = new EmailSmimeDecompressor().DecompressWithEntityAndOptions(entity.GetEntity()!, new EmailSmimeDecompressionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetContent(), Is.EqualTo(content));
            Assert.That(withOptions.GetContent(), Is.EqualTo(content));
        });
    }

    [Test]
    public void EnforcesCompressionInputAndDecompressedOutputLimits()
    {
        EmailCryptographicLimits compressionLimits = new EmailCryptographicLimits();
        compressionLimits.SetMaxInputBytes(3);
        EmailSmimeCompressionOptions compressionOptions = new EmailSmimeCompressionOptions();
        compressionOptions.SetLimits(compressionLimits);

        byte[] expandingContent = new byte[10000];
        EmailSmimeCompressionResult compressed = new EmailSmimeCompressor().CompressWithContent(expandingContent);
        EmailCryptographicLimits decompressionLimits = new EmailCryptographicLimits();
        decompressionLimits.SetMaxDecompressedBytes(100);
        EmailSmimeDecompressionOptions decompressionOptions = new EmailSmimeDecompressionOptions();
        decompressionOptions.SetLimits(decompressionLimits);
        decompressionOptions.SetReadBufferBytes(17);

        EmailLimitException input = Assert.Throws<EmailLimitException>(() => new EmailSmimeCompressor().CompressWithContentAndOptions(Bytes("four"), compressionOptions));
        EmailLimitException output = Assert.Throws<EmailLimitException>(() => new EmailSmimeDecompressor().DecompressWithCmsAndOptions(compressed.GetCmsBytes(), decompressionOptions));

        Assert.Multiple(() =>
        {
            Assert.That(input.GetLimitName(), Is.EqualTo("MaxSmimeContentBytes"));
            Assert.That(output.GetLimitName(), Is.EqualTo("MaxDecompressedBytes"));
            Assert.That(output.GetObserved(), Is.GreaterThan(100));
        });
    }

    [Test]
    public void PreservesUnsupportedAlgorithmCmsAndCompressedBytesLosslessly()
    {
        byte[] opaque = [0x00, 0xFF, 0x10, 0x80, 0x42];
        byte[] cms = WrapCompressed("1.2.3.4.5.6", opaque, AlgorithmParameters.Integer, 0);

        EmailSmimeDecompressionResult result = new EmailSmimeDecompressor().DecompressWithCms(cms);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetStatus(), Is.EqualTo(EmailSmimeDecompressionStatus.UnsupportedAlgorithm));
            Assert.That(result.IsUnsupportedAlgorithm(), Is.True);
            Assert.That(result.IsLosslesslyPreserved(), Is.True);
            Assert.That(result.HasContent(), Is.False);
            Assert.That(result.GetOriginalCmsBytes(), Is.EqualTo(cms));
            Assert.That(result.GetCompressedContent(), Is.EqualTo(opaque));
            Assert.That(result.GetAlgorithmObjectIdentifier(), Is.EqualTo("1.2.3.4.5.6"));
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.SmimeCompressionAlgorithmUnsupported), Is.True);
        });

        byte[] returnedCms = result.GetOriginalCmsBytes();
        byte[] returnedCompressed = result.GetCompressedContent();
        returnedCms[0] = 0;
        returnedCompressed[0] = 1;
        Assert.Multiple(() =>
        {
            Assert.That(result.GetOriginalCmsBytes(), Is.EqualTo(cms));
            Assert.That(result.GetCompressedContent(), Is.EqualTo(opaque));
        });
    }

    [Test]
    public void RejectsMalformedAndInvalidCompressedDataWithoutContent()
    {
        byte[] wrongVersion = WrapCompressed(EmailSmimeCompressorConstants.ZlibCompressionObjectIdentifier, [1, 2, 3], AlgorithmParameters.Absent, 1);
        byte[] invalidZlib = WrapCompressed(EmailSmimeCompressorConstants.ZlibCompressionObjectIdentifier, [1, 2, 3], AlgorithmParameters.Absent, 0);
        byte[] wrongKind = new EmailCmsWriter().WriteDataWithData(Bytes("not compressed-data")).GetBytes();

        EmailSmimeDecompressionResult version = new EmailSmimeDecompressor().DecompressWithCms(wrongVersion);
        EmailSmimeDecompressionResult invalid = new EmailSmimeDecompressor().DecompressWithCms(invalidZlib);
        EmailSmimeDecompressionResult kind = new EmailSmimeDecompressor().DecompressWithCms(wrongKind);

        Assert.Multiple(() =>
        {
            Assert.That(version.GetStatus(), Is.EqualTo(EmailSmimeDecompressionStatus.Malformed));
            Assert.That(version.HasContent(), Is.False);
            Assert.That(version.GetIssues().ContainsCode(EmailIssueCode.SmimeCompressedDataMalformed), Is.True);
            Assert.That(invalid.GetStatus(), Is.EqualTo(EmailSmimeDecompressionStatus.DecompressionFailed));
            Assert.That(invalid.HasContent(), Is.False);
            Assert.That(invalid.GetIssues().ContainsCode(EmailIssueCode.SmimeDecompressionFailed), Is.True);
            Assert.That(kind.GetStatus(), Is.EqualTo(EmailSmimeDecompressionStatus.Malformed));
            Assert.That(kind.HasContent(), Is.False);
        });
    }

    static byte[] CompressedContent(byte[] cms)
    {
        EmailCmsAsn1Node compressedData = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(cms).GetContentInfo()!.GetContentNode();
        return compressedData.GetChildAt(2).GetChildAt(1).GetChildAt(0).GetContentBytes();
    }

    static byte[] WrapCompressed(string algorithmOid, byte[] compressed, AlgorithmParameters parameters, int version)
    {
        AsnWriter compressedData = new AsnWriter(AsnEncodingRules.DER);
        compressedData.PushSequence();
        compressedData.WriteInteger(version);
        compressedData.PushSequence();
        compressedData.WriteObjectIdentifier(algorithmOid);
        if (parameters == AlgorithmParameters.Null) compressedData.WriteNull();
        if (parameters == AlgorithmParameters.Integer) compressedData.WriteInteger(7);
        compressedData.PopSequence();
        compressedData.PushSequence();
        compressedData.WriteObjectIdentifier(EmailCmsReaderConstants.DataObjectIdentifier);
        Asn1Tag encapsulatedTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        compressedData.PushSequence(encapsulatedTag);
        compressedData.WriteOctetString(compressed);
        compressedData.PopSequence(encapsulatedTag);
        compressedData.PopSequence();
        compressedData.PopSequence();

        AsnWriter contentInfo = new AsnWriter(AsnEncodingRules.DER);
        contentInfo.PushSequence();
        contentInfo.WriteObjectIdentifier(EmailCmsReaderConstants.CompressedDataObjectIdentifier);
        Asn1Tag contentTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        contentInfo.PushSequence(contentTag);
        contentInfo.WriteEncodedValue(compressedData.Encode());
        contentInfo.PopSequence(contentTag);
        contentInfo.PopSequence();
        return contentInfo.Encode();
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }

    enum AlgorithmParameters
    {
        Absent,
        Null,
        Integer
    }
}
