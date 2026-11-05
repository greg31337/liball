using System.Text;

namespace email.Tests;

public class EmailTnefReaderTests
{
    [Test]
    public void ParsesContainerAttributesChecksumsRangesAndAttachmentGroups()
    {
        byte[] unknownMessage = new byte[] { 9, 8, 7, 6 };
        byte[] rendering = new byte[] { 1, 0, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0 };
        byte[] unknownAttachment = Encoding.ASCII.GetBytes("opaque");
        byte[] source = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, Concat(UInt32(1252), UInt32(0))),
            Attribute(1, 0x00027777, unknownMessage),
            Attribute(2, EmailTnefReaderConstants.AttachmentRenderingDataTag, rendering),
            Attribute(2, 0x0006A001, unknownAttachment));

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsComplete(), Is.True);
        EmailTnefContainer container = result.GetContainer()!;
        Assert.Multiple(() =>
        {
            Assert.That(container!.GetHeader().GetSignature(), Is.EqualTo(EmailTnefReaderConstants.TnefSignature));
            Assert.That(container.GetHeader().GetLegacyKey(), Is.EqualTo(0x1234));
            Assert.That(container.GetHeader().GetSourceRange().EqualsRange(new EmailRange(0, 6)), Is.True);
            Assert.That(container.GetAttributeCount(), Is.EqualTo(5));
            Assert.That(container.GetAttachmentCount(), Is.EqualTo(1));
            Assert.That(container.GetVersion(), Is.EqualTo(0x00010000));
            Assert.That(container.GetCodePage(), Is.EqualTo(1252));
            Assert.That(container.HasSubCodePage(), Is.True);
            Assert.That(container.GetSubCodePage(), Is.EqualTo(0));
            Assert.That(container.GetSourceByteCount(), Is.EqualTo(source.Length));
        });

        EmailTnefAttribute version = container!.GetAttributeAt(0);
        EmailTnefAttribute unknown = container.GetAttributeAt(2);
        EmailTnefAttribute attachment = container.GetAttributeAt(4);
        Assert.Multiple(() =>
        {
            Assert.That(version.GetLevel(), Is.EqualTo(EmailTnefAttributeLevel.Message));
            Assert.That(version.GetAttributeType(), Is.EqualTo(EmailTnefAttributeType.Dword));
            Assert.That(version.GetAttributeId(), Is.EqualTo(0x9006));
            Assert.That(version.GetKind(), Is.EqualTo(EmailTnefAttributeKind.TnefVersion));
            Assert.That(version.IsChecksumValid(), Is.True);
            Assert.That(version.GetHeaderRange().GetLength(), Is.EqualTo(9));
            Assert.That(version.GetDataRange().GetLength(), Is.EqualTo(4));
            Assert.That(version.GetChecksumRange()!.GetLength(), Is.EqualTo(2));
            Assert.That(version.GetSourceRange().GetLength(), Is.EqualTo(15));
            Assert.That(unknown.IsKnown(), Is.False);
            Assert.That(unknown.GetTag(), Is.EqualTo(0x00027777));
            Assert.That(unknown.GetData(), Is.EqualTo(unknownMessage));
            Assert.That(unknown.HasAttachmentIndex(), Is.False);
            Assert.That(attachment.IsKnown(), Is.False);
            Assert.That(attachment.GetData(), Is.EqualTo(unknownAttachment));
            Assert.That(attachment.GetAttachmentIndex(), Is.EqualTo(0));
        });
    }

    [Test]
    public void PreservesUnknownDataAndSourceDefensively()
    {
        byte[] payload = new byte[] { 1, 2, 3 };
        byte[] source = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(437)),
            Attribute(1, 0x1234ABCD, payload));
        EmailTnefContainer container = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source).GetContainer()!;

        source[0] = 0;
        payload[0] = 99;
        byte[] firstRead = container!.GetAttributeAt(2).GetData();
        firstRead[1] = 99;
        byte[] sourceRead = container.GetSourceBytes();
        sourceRead[0] = 0;

        Assert.That(container.GetHeader().IsSignatureValid(), Is.True);
        Assert.That(container.GetAttributeAt(2).GetData(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(container.GetSourceBytes()[0], Is.EqualTo(0x78));
    }

    [Test]
    public void CodePageScalarsRetainSignedBitPatterns()
    {
        byte[] source = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, Concat(UInt32(uint.MaxValue), UInt32(uint.MaxValue))));

        EmailTnefContainer container = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source).GetContainer()!;

        Assert.That(container.GetCodePage(), Is.EqualTo(-1));
        Assert.That(container.GetSubCodePage(), Is.EqualTo(-1));
    }

    [Test]
    public void ReportsChecksumMismatchAndCanRetainItAsWarning()
    {
        byte[] source = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)));
        source[^1] ^= 0x40;

        EmailTnefParseResult strict = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);
        EmailTnefReaderOptions options = new EmailTnefReaderOptions();
        options.SetRequireValidChecksums(false);
        EmailTnefParseResult retaining = new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(strict.IsComplete(), Is.True);
            Assert.That(strict.IsSuccess(), Is.False);
            Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.TnefChecksumMismatch), Is.True);
            Assert.That(strict.GetContainer()!.GetAttributeAt(1).IsChecksumValid(), Is.False);
            Assert.That(retaining.IsSuccess(), Is.True);
            Assert.That(retaining.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        });
    }

    [Test]
    public void RetainsPartialAttributeWithoutMisreadingAChecksum()
    {
        List<byte> source = Header();
        source.Add(1);
        AddUInt32(source, 0x00027777);
        AddUInt32(source, 10);
        source.AddRange(new byte[] { 4, 5, 6 });

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source.ToArray());

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefAttributeMalformed), Is.True);
        EmailTnefAttribute attribute = result.GetContainer()!.GetAttributeAt(0);
        Assert.Multiple(() =>
        {
            Assert.That(attribute.IsComplete(), Is.False);
            Assert.That(attribute.GetDeclaredLength(), Is.EqualTo(10));
            Assert.That(attribute.GetRetainedLength(), Is.EqualTo(3));
            Assert.That(attribute.GetData(), Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(attribute.HasChecksum(), Is.False);
            Assert.That(attribute.HasChecksumRange(), Is.False);
        });
    }

    [Test]
    public void RejectsInvalidSignatureWithoutInterpretingAttributes()
    {
        byte[] source = BuildContainer(Attribute(1, 0x00027777, new byte[] { 1 }));
        source[0] = 0;

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.HasContainer(), Is.True);
        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.GetContainer()!.GetAttributeCount(), Is.EqualTo(0));
        Assert.That(result.GetContainer()!.GetSourceBytes(), Is.EqualTo(source));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefSignatureInvalid), Is.True);
    }

    [Test]
    public void DiagnosesRequiredPrefixAndAttachmentOrderingWhileRetainingAttributes()
    {
        byte[] source = BuildContainer(
            Attribute(2, 0x0006A001, new byte[] { 1 }),
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)));

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetContainer()!.GetAttributeCount(), Is.EqualTo(3));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefRequiredAttributeMissing), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefStructureMalformed), Is.True);
    }

    [Test]
    public void EnforcesInputAttributeSizeAndCountLimitsBeforeAllocation()
    {
        byte[] threeAttributes = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, 0x00027777, new byte[] { 1 }));
        EmailArchiveLimits countLimits = new EmailArchiveLimits();
        countLimits.SetMaxTnefAttributeCount(2);
        EmailTnefReaderOptions countOptions = new EmailTnefReaderOptions();
        countOptions.SetLimits(countLimits);

        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(threeAttributes, countOptions));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxTnefAttributeCount"));

        List<byte> malicious = Header();
        malicious.Add(1);
        AddUInt32(malicious, 0x00027777);
        AddUInt32(malicious, 100);
        EmailArchiveLimits attributeLimits = new EmailArchiveLimits();
        attributeLimits.SetMaxEntryBytes(32);
        EmailTnefReaderOptions attributeOptions = new EmailTnefReaderOptions();
        attributeOptions.SetLimits(attributeLimits);
        EmailLimitException attribute = Assert.Throws<EmailLimitException>(() => new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(malicious.ToArray(), attributeOptions));
        Assert.That(attribute.GetLimitName(), Is.EqualTo("MaxTnefAttributeBytes"));

        EmailArchiveLimits inputLimits = new EmailArchiveLimits();
        inputLimits.SetMaxEntryBytes(threeAttributes.Length - 1);
        EmailTnefReaderOptions inputOptions = new EmailTnefReaderOptions();
        inputOptions.SetLimits(inputLimits);
        EmailLimitException input = Assert.Throws<EmailLimitException>(() => new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(threeAttributes, inputOptions));
        Assert.That(input.GetLimitName(), Is.EqualTo("MaxTnefBytes"));
    }

    [Test]
    public void ReadsChunkedStreamsWithoutClosingThemAndSnapshotsOptions()
    {
        byte[] source = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)));
        TrackingChunkStream stream = new TrackingChunkStream(source, 3);
        EmailTnefReaderOptions options = new EmailTnefReaderOptions();
        options.SetReadBufferBytes(7);
        EmailArchiveLimits supplied = new EmailArchiveLimits();
        supplied.SetMaxTnefAttributeCount(2);
        options.SetLimits(supplied);
        supplied.SetMaxTnefAttributeCount(1);

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputAndOptionsUsingStreamAndEmailTnefReaderOptions(stream, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(stream.WasDisposed, Is.False);
        Assert.That(options.GetLimits().GetMaxTnefAttributeCount(), Is.EqualTo(2));
        EmailArchiveLimits returned = options.GetLimits();
        returned.SetMaxTnefAttributeCount(1);
        Assert.That(options.GetLimits().GetMaxTnefAttributeCount(), Is.EqualTo(2));
    }

    [Test]
    public void ParsesTnefMimeEntityContentAndRejectsOtherEntityKinds()
    {
        byte[] source = BuildContainer(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)));
        MimeEntity tnef = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Tnef, new EmailHeadersOperations().CreateWith(), Media("application", "ms-tnef"), new EmailContentOperations().CreateMemoryCopy(source, false), new List<MimeEntity>(), null);
        MimeEntity ordinary = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("application", "octet-stream"), new EmailContentOperations().CreateMemoryCopy(source, false), new List<MimeEntity>(), null);

        EmailTnefParseResult parsed = new EmailTnefReader().ParseWithEntity(tnef);
        EmailTnefParseResult rejected = new EmailTnefReader().ParseWithEntity(ordinary);

        Assert.That(parsed.IsSuccess(), Is.True);
        Assert.That(rejected.HasContainer(), Is.False);
        Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.TnefHeaderMalformed), Is.True);
    }

    static byte[] BuildContainer(params byte[][] attributes)
    {
        List<byte> result = Header();
        foreach (byte[] attribute in attributes) result.AddRange(attribute);
        return result.ToArray();
    }

    static EmailMediaType Media(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>()), Encoding.ASCII.GetBytes(type + "/" + subtype), new EmailSourcePosition(0, 1, 1), false);
    }

    static List<byte> Header()
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, EmailTnefReaderConstants.TnefSignature);
        result.Add(0x34);
        result.Add(0x12);
        return result;
    }

    static byte[] Attribute(int level, uint tag, byte[] data)
    {
        List<byte> result = new List<byte>();
        result.Add((byte)level);
        AddUInt32(result, tag);
        AddUInt32(result, (uint)data.Length);
        result.AddRange(data);
        int checksum = 0;
        foreach (byte value in data) checksum = (checksum + value) & 0xffff;
        result.Add((byte)checksum);
        result.Add((byte)(checksum >> 8));
        return result.ToArray();
    }

    static byte[] UInt32(uint value)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, value);
        return result.ToArray();
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        return first.Concat(second).ToArray();
    }

    static void AddUInt32(List<byte> output, uint value)
    {
        output.Add((byte)value);
        output.Add((byte)(value >> 8));
        output.Add((byte)(value >> 16));
        output.Add((byte)(value >> 24));
    }

    sealed class TrackingChunkStream : MemoryStream
    {
        readonly int maximumChunk;

        public TrackingChunkStream(byte[] value, int maximumChunk) : base(value)
        {
            this.maximumChunk = maximumChunk;
        }

        public bool WasDisposed { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, maximumChunk));
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
