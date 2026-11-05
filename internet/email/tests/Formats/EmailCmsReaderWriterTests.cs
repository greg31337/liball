using System.Formats.Asn1;
using System.Text;
using email;

namespace email.Tests;

public class EmailCmsReaderWriterTests
{
    [Test]
    public void WritesAndReadsDerDataContentInfo()
    {
        byte[] payload = Bytes("CMS payload");

        EmailCmsWriteResult written = new EmailCmsWriter().WriteDataWithData(payload);
        EmailCmsReadResult read = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(written.GetBytes());
        EmailCmsContentInfo content = read.GetContentInfo()!;

        Assert.Multiple(() =>
        {
            Assert.That(written.IsSuccess(), Is.True);
            Assert.That(written.GetIssues().GetCount(), Is.Zero);
            Assert.That(read.IsSuccess(), Is.True);
            Assert.That(read.IsComplete(), Is.True);
            Assert.That(content!.GetContentType(), Is.EqualTo(EmailCmsReaderConstants.DataObjectIdentifier));
            Assert.That(content.GetContentKind(), Is.EqualTo(EmailCmsContentKind.Data));
            Assert.That(content.GetEncodingKind(), Is.EqualTo(EmailCmsEncodingKind.Der));
            Assert.That(content.WasBerEncoded(), Is.False);
            Assert.That(content.GetRootNode().HasTag(EmailCmsAsn1TagClass.Universal, 16), Is.True);
            Assert.That(content.HasDataContent(), Is.True);
            Assert.That(content.GetDataContent(), Is.EqualTo(payload));
            Assert.That(content.GetOriginalBytes(), Is.EqualTo(written.GetBytes()));
            Assert.That(read.GetAsn1ElementCount(), Is.EqualTo(4));
            Assert.That(read.GetMaximumAsn1Depth(), Is.EqualTo(3));
        });

        EmailCmsWriterOptions writerOptions = new EmailCmsWriterOptions();
        Assert.That(new EmailCmsWriter().WriteWithContentInfoAndOptions(content, writerOptions).GetBytes(), Is.EqualTo(written.GetBytes()));

        byte[] returned = content!.GetDataContent()!;
        returned![0] = 0;
        Assert.That(content.GetDataContent(), Is.EqualTo(payload));
        byte[] encoded = content.GetContentNode().GetEncodedBytes();
        encoded[0] = 0;
        Assert.That(content.GetContentNode().GetEncodedBytes()[0], Is.EqualTo(4));
    }

    [Test]
    public void ReadsIndefiniteBerAndNormalizesConstructedDataToDer()
    {
        byte[] ber =
        [
            0x30, 0x80,
            0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x01,
            0xA0, 0x80,
            0x24, 0x80,
            0x04, 0x03, 0x61, 0x62, 0x63,
            0x04, 0x02, 0x64, 0x65,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        ];

        EmailCmsReadResult read = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(ber);
        EmailCmsReaderOptions derOnlyOptions = ReaderOptions();
        derOnlyOptions.SetAllowBer(false);
        EmailCmsReadResult rejected = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(ber, derOnlyOptions);
        EmailCmsWriteResult normalized = new EmailCmsWriter().WriteWithContentInfo(read.GetContentInfo()!);
        EmailCmsReadResult reparsed = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(normalized.GetBytes());

        Assert.Multiple(() =>
        {
            Assert.That(read.IsSuccess(), Is.True);
            Assert.That(read.GetContentInfo()!.GetRootNode().HasIndefiniteLength(), Is.True);
            Assert.That(read.GetContentInfo()!.GetRootNode().GetChildren().Count, Is.EqualTo(2));
            Assert.That(read.GetContentInfo()!.GetEncodingKind(), Is.EqualTo(EmailCmsEncodingKind.Ber));
            Assert.That(read.GetContentInfo()!.GetDataContent(), Is.EqualTo(Bytes("abcde")));
            Assert.That(read.GetIssues().ContainsCode(EmailIssueCode.CmsBerEncodingAccepted), Is.True);
            Assert.That(rejected.IsSuccess(), Is.False);
            Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.CmsContentInfoMalformed), Is.True);
            Assert.That(read.GetAsn1ElementCount(), Is.EqualTo(6));
            Assert.That(read.GetMaximumAsn1Depth(), Is.EqualTo(4));
            Assert.That(normalized.IsSuccess(), Is.True);
            Assert.That(normalized.WasNormalizedFromBer(), Is.True);
            Assert.That(normalized.GetBytes(), Does.Not.Contain((byte)0x80));
            Assert.That(reparsed.IsSuccess(), Is.True);
            Assert.That(reparsed.GetContentInfo()!.GetEncodingKind(), Is.EqualTo(EmailCmsEncodingKind.Der));
            Assert.That(reparsed.GetContentInfo()!.GetDataContent(), Is.EqualTo(Bytes("abcde")));
            Assert.That(reparsed.GetContentInfo()!.GetContentNode().IsConstructed(), Is.False);
        });
    }

    [Test]
    public void RecognizesCoreCmsKindsAndPreservesUnknownContent()
    {
        byte[] signedData = Wrap(EmailCmsReaderConstants.SignedDataObjectIdentifier, Sequence(Integer(1)));
        EmailCmsReadResult signed = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(signedData);
        byte[] unknownBytes = Wrap("1.2.3.4.5", Utf8("opaque"));
        EmailCmsReadResult unknown = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(unknownBytes);
        EmailCmsWriteResult unknownWritten = new EmailCmsWriter().WriteWithContentInfo(unknown.GetContentInfo()!);

        Assert.Multiple(() =>
        {
            Assert.That(signed.IsSuccess(), Is.True);
            Assert.That(signed.GetContentInfo()!.GetContentKind(), Is.EqualTo(EmailCmsContentKind.SignedData));
            Assert.That(signed.GetContentInfo()!.GetContentNode().HasTag(EmailCmsAsn1TagClass.Universal, 16), Is.True);
            Assert.That(unknown.IsSuccess(), Is.True);
            Assert.That(unknown.GetContentInfo()!.GetContentKind(), Is.EqualTo(EmailCmsContentKind.Unknown));
            Assert.That(unknown.GetContentInfo()!.GetEncodedContent(), Is.EqualTo(Utf8("opaque")));
            Assert.That(unknownWritten.IsSuccess(), Is.True);
            Assert.That(unknownWritten.GetBytes(), Is.EqualTo(unknownBytes));
        });
    }

    [Test]
    public void RejectsTruncationInvalidOidTrailingValuesAndWrongKnownShape()
    {
        EmailCmsReadResult empty = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(Array.Empty<byte>());
        EmailCmsReadResult truncated = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(new byte[] { 0x30, 0x03, 0x06, 0x01 });
        byte[] data = new EmailCmsWriter().WriteDataWithData(Bytes("x")).GetBytes();
        EmailCmsReadResult trailing = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(data.Concat(data).ToArray());
        byte[] invalidOid = [0x30, 0x07, 0x06, 0x01, 0x80, 0xA0, 0x02, 0x05, 0x00];
        EmailCmsReadResult oid = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(invalidOid);
        EmailCmsReadResult wrongShape = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(Wrap(EmailCmsReaderConstants.EnvelopedDataObjectIdentifier, Integer(1)));

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsSuccess(), Is.False);
            Assert.That(empty.GetIssues().ContainsCode(EmailIssueCode.CmsContentInfoMalformed), Is.True);
            Assert.That(truncated.IsSuccess(), Is.False);
            Assert.That(truncated.GetIssues().ContainsCode(EmailIssueCode.CmsContentInfoMalformed), Is.True);
            Assert.That(trailing.IsSuccess(), Is.False);
            Assert.That(trailing.GetAsn1ElementCount(), Is.EqualTo(8));
            Assert.That(oid.IsSuccess(), Is.False);
            Assert.That(oid.GetIssues().ContainsCode(EmailIssueCode.CmsObjectIdentifierMalformed), Is.True);
            Assert.That(wrongShape.HasContentInfo(), Is.True);
            Assert.That(wrongShape.IsSuccess(), Is.False);
            Assert.That(wrongShape.GetIssues().ContainsCode(EmailIssueCode.CmsContentMalformed), Is.True);
        });
    }

    [Test]
    public void EnforcesInputDepthElementDataAndOutputLimits()
    {
        byte[] der = new EmailCmsWriter().WriteDataWithData(Bytes("payload")).GetBytes();
        EmailCmsReaderOptions inputOptions = ReaderOptions();
        EmailCryptographicLimits inputLimits = inputOptions.GetLimits();
        inputLimits.SetMaxInputBytes(der.Length - 1);
        inputOptions.SetLimits(inputLimits);
        AssertLimit("MaxCmsInputBytes", () => new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(der, inputOptions));

        byte[] ber =
        [
            0x30, 0x80,
            0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x01,
            0xA0, 0x80, 0x24, 0x80, 0x04, 0x01, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];
        EmailCmsReaderOptions depthOptions = ReaderOptions();
        EmailCryptographicLimits depthLimits = depthOptions.GetLimits();
        depthLimits.SetMaxAsn1Depth(3);
        depthOptions.SetLimits(depthLimits);
        AssertLimit("MaxAsn1Depth", () => new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(ber, depthOptions));

        EmailCmsReaderOptions elementOptions = ReaderOptions();
        EmailCryptographicLimits elementLimits = elementOptions.GetLimits();
        elementLimits.SetMaxAsn1ElementCount(4);
        elementOptions.SetLimits(elementLimits);
        AssertLimit("MaxAsn1ElementCount", () => new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(ber, elementOptions));

        EmailCmsWriterOptions outputOptions = new EmailCmsWriterOptions();
        EmailCryptographicLimits outputLimits = outputOptions.GetLimits();
        outputLimits.SetMaxInputBytes(5);
        outputOptions.SetLimits(outputLimits);
        AssertLimit("MaxCmsOutputBytes", () => new EmailCmsWriter().WriteDataWithDataAndOptions(Bytes("abc"), outputOptions));

        EmailCmsWriterOptions outputDepthOptions = new EmailCmsWriterOptions();
        EmailCryptographicLimits outputDepthLimits = outputDepthOptions.GetLimits();
        outputDepthLimits.SetMaxAsn1Depth(2);
        outputDepthOptions.SetLimits(outputDepthLimits);
        AssertLimit("MaxAsn1Depth", () => new EmailCmsWriter().WriteDataWithDataAndOptions(Bytes("x"), outputDepthOptions));

        EmailCmsWriterOptions outputElementOptions = new EmailCmsWriterOptions();
        EmailCryptographicLimits outputElementLimits = outputElementOptions.GetLimits();
        outputElementLimits.SetMaxAsn1ElementCount(3);
        outputElementOptions.SetLimits(outputElementLimits);
        AssertLimit("MaxAsn1ElementCount", () => new EmailCmsWriter().WriteDataWithDataAndOptions(Bytes("x"), outputElementOptions));
    }

    [Test]
    public void SupportsBoundedStreamsAndOptionSnapshots()
    {
        byte[] der = new EmailCmsWriter().WriteDataWithData(Bytes("streamed")).GetBytes();
        EmailCmsReaderOptions options = ReaderOptions();
        options.SetReadBufferBytes(2);
        EmailCmsReaderOptions clone = options.Clone();
        options.SetAllowBer(false);
        options.SetReadBufferBytes(9);

        using MemoryStream stream = new MemoryStream(der);
        EmailCmsReadResult result = new EmailCmsReader().ParseWithInputAndOptionsUsingStreamAndEmailCmsReaderOptions(stream, clone);
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("application", "pkcs7-mime"), new EmailContentOperations().CreateMemoryCopy(der, false), new List<MimeEntity>(), null);
        EmailCmsReadResult entityResult = new EmailCmsReader().ParseWithEntityAndOptions(entity, clone);
        EmailCmsReadResult defaultEntityResult = new EmailCmsReader().ParseWithEntity(entity);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetContentInfo()!.GetDataContent(), Is.EqualTo(Bytes("streamed")));
            Assert.That(entityResult.IsSuccess(), Is.True);
            Assert.That(entityResult.GetContentInfo()!.GetDataContent(), Is.EqualTo(Bytes("streamed")));
            Assert.That(defaultEntityResult.IsSuccess(), Is.True);
            Assert.That(clone.GetAllowBer(), Is.True);
            Assert.That(clone.GetReadBufferBytes(), Is.EqualTo(2));
        });

        Assert.Throws<ArgumentNullException>(() => new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(default(ReadOnlySpan<byte>), null!));
        Assert.Throws<ArgumentNullException>(() => new EmailCmsReader().ParseWithInputUsingStream((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailCmsWriter().WriteWithContentInfo((EmailCmsContentInfo)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailCmsWriter().WriteDataWithDataAndOptions(Bytes("x"), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetReadBufferBytes(0));
        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
    }

    [Test]
    public void DerWritingSortsSetMembersDeterministically()
    {
        byte[] source =
        [
            0x30, 0x11,
            0x06, 0x03, 0x2A, 0x03, 0x04,
            0xA0, 0x0A,
            0x30, 0x08,
            0x31, 0x06,
            0x02, 0x01, 0x02,
            0x02, 0x01, 0x01
        ];
        EmailCmsReadResult read = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(source);

        EmailCmsWriteResult written = new EmailCmsWriter().WriteWithContentInfo(read.GetContentInfo()!);
        EmailCmsReadResult normalized = new EmailCmsReader().ParseWithInputUsingReadOnlySpan(written.GetBytes());
        EmailCmsAsn1Node set = normalized.GetContentInfo()!.GetContentNode().GetChildAt(0);

        Assert.Multiple(() =>
        {
            Assert.That(written.IsSuccess(), Is.True);
            Assert.That(set.GetChildCount(), Is.EqualTo(2));
            Assert.That(set.GetChildAt(0).GetContentBytes(), Is.EqualTo(new byte[] { 0x01 }));
            Assert.That(set.GetChildAt(1).GetContentBytes(), Is.EqualTo(new byte[] { 0x02 }));
        });
    }

    [Test]
    public void PublicAsn1NodeFactoryCopiesCallerOwnedBuffersAndChildren()
    {
        byte[] encoded = [0x04, 0x01, 0x41];
        byte[] content = [0x41];
        EmailCmsAsn1Node child = new EmailCmsAsn1NodeOperations().CreateWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndEncodedBytesAndContentBytesAndChildren(
            EmailCmsAsn1TagClass.Universal, 4, false, false, encoded, content, new List<EmailCmsAsn1Node>());
        List<EmailCmsAsn1Node> children = new List<EmailCmsAsn1Node> { child };
        EmailCmsAsn1Node parent = new EmailCmsAsn1NodeOperations().CreateWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndEncodedBytesAndContentBytesAndChildren(
            EmailCmsAsn1TagClass.Universal, 16, true, true, [0x30, 0x80, 0x00, 0x00], [0x04, 0x01, 0x41], children);

        encoded[2] = 0;
        content[0] = 0;
        children.Clear();

        Assert.That(child.GetEncodedBytes(), Is.EqualTo(new byte[] { 0x04, 0x01, 0x41 }));
        Assert.That(child.GetContentBytes(), Is.EqualTo(new byte[] { 0x41 }));
        Assert.That(parent.HasIndefiniteLength(), Is.True);
        Assert.That(parent.GetChildren(), Is.EqualTo(new[] { child }));
    }

    static EmailCmsReaderOptions ReaderOptions()
    {
        return new EmailCmsReaderOptions();
    }

    static byte[] Wrap(string oid, byte[] content)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteObjectIdentifier(oid);
        Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        writer.PushSequence(tag);
        writer.WriteEncodedValue(content);
        writer.PopSequence(tag);
        writer.PopSequence();
        return writer.Encode();
    }

    static EmailMediaType Media(string type, string subtype)
    {
        EmailParameterList parameters = new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
        return new EmailMediaType(type, subtype, parameters, Bytes(type + "/" + subtype), new EmailSourcePosition(0, 1, 1), false);
    }

    static byte[] Sequence(params byte[][] values)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        foreach (byte[] value in values) writer.WriteEncodedValue(value);
        writer.PopSequence();
        return writer.Encode();
    }

    static byte[] Integer(int value)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteInteger(value);
        return writer.Encode();
    }

    static byte[] Utf8(string value)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteCharacterString(UniversalTagNumber.UTF8String, value);
        return writer.Encode();
    }

    static void AssertLimit(string name, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(name));
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
