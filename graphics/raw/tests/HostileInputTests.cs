using raw;
using System.Text;
using System.Xml;

namespace raw.Tests;

public class HostileInputTests
{
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    [Test]
    public void BigTiffOffsetOverflowIsRejectedBeforeRandomAccess()
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, true);
        builder.AddIfd();
        builder.SetMalformedFirstIfdOffset(ulong.MaxValue);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => RawCodec.OpenMetadata(builder.Build(), null, new RawOpenOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
            Assert.That(exception.Offset, Is.EqualTo(-1));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CyclesAreRejectedWhileRepeatedIfdPointersBecomeBoundedAliases(bool bigTiff)
    {
        TiffFieldType pointerType = bigTiff ? TiffFieldType.Ifd8 : TiffFieldType.Ifd;
        SyntheticTiffBuilder cyclicBuilder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, bigTiff);
        SyntheticTiffIfd cyclicRoot = cyclicBuilder.AddIfd();
        SyntheticTiffIfd cyclicChild = cyclicBuilder.AddIfd();
        cyclicBuilder.AddPointer(cyclicRoot, 330, pointerType, cyclicChild);
        cyclicBuilder.AddPointer(cyclicChild, 34665, pointerType, cyclicRoot);

        RawFormatException? cycle = Assert.Throws<RawFormatException>(() => Parse(cyclicBuilder.Build(), new RawLimits()));

        SyntheticTiffBuilder aliasBuilder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, bigTiff);
        SyntheticTiffIfd aliasRoot = aliasBuilder.AddIfd();
        SyntheticTiffIfd aliasTarget = aliasBuilder.AddIfd();
        aliasBuilder.AddPointer(aliasRoot, 34853, pointerType, aliasTarget);
        aliasBuilder.AddPointer(aliasRoot, 0xc001, pointerType, aliasTarget);
        TiffIfdGraph graph = Parse(aliasBuilder.Build(), new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That((cycle ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(cycle.Message, Does.Contain("cycle"));
            Assert.That(graph.Ifds, Has.Length.EqualTo(2));
            Assert.That(graph.Links, Has.Length.EqualTo(2));
            Assert.That(graph.Links.Count(value => value.Alias), Is.EqualTo(1));
            Assert.That(graph.Links.Single(value => value.Alias).TargetPath, Is.EqualTo("IFD0/GPS"));
        });
    }

    [Test]
    public void PayloadAliasesAndNestingAreBoundedButCrossingOverlapsAreRejected()
    {
        RawPayloadBlock outer = new RawPayloadBlock(RawPayloadKind.MakerNote, "MakerNote", 16, 32, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock nested = new RawPayloadBlock(RawPayloadKind.Authenticity, "MakerNote/Decision", 24, 8, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock alias = new RawPayloadBlock(RawPayloadKind.Metadata, "Alias", 16, 32, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock crossing = new RawPayloadBlock(RawPayloadKind.Opaque, "Crossing", 40, 16, -1, -1, -1, RawCompression.None, false, true);

        RawValidationReport accepted = new RawPayloadMap(64, new RawPayloadBlock[] { outer, nested, alias }).Validate();
        RawValidationReport rejected = new RawPayloadMap(64, new RawPayloadBlock[] { outer, crossing }).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.IsValid(), Is.True);
            Assert.That(rejected.IsValid(), Is.False);
            Assert.That(((rejected.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(value => value.Code == RawDiagnosticCode.InvalidContainer && value.Context == "PayloadMap.Overlap")), Is.True);
        });
    }

    [Test]
    public void CompressedExpansionAndNonSeekableBufferingRespectAllocationLimits()
    {
        byte[] expanded = Enumerable.Repeat((byte)0x5a, 4096).ToArray();
        byte[] compressed = DeflateCodec.Encode(expanded, true, new RawLimits(), CancellationToken.None);
        RawLimits decodeLimits = new RawLimits();
        decodeLimits.MaxDecodedBytes = 128;
        decodeLimits.MaxAllocationBytes = 128;
        RawLimitException? decompression = Assert.Throws<RawLimitException>(() => DeflateCodec.Decode(compressed, expanded.Length, true, decodeLimits, CancellationToken.None));

        byte[] tiff = MinimalTiff();
        HostileReadStream stream = new HostileReadStream(tiff, null, 4);
        RawLimits bufferingLimits = new RawLimits();
        bufferingLimits.MaxAllocationBytes = tiff.Length - 1;
        BoundedInput input = BoundedInput.CreateStream(stream, 0, tiff.Length, bufferingLimits);
        RawLimitException? buffering = Assert.Throws<RawLimitException>(() => TiffParser.Parse(input));

        Assert.Multiple(() =>
        {
            Assert.That((decompression ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((buffering ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That(stream.ReadCount, Is.Zero);
        });
    }

    [Test]
    public void MalformedXmlAndExternalEntitiesAreRejectedWithoutResolution()
    {
        string entity = "<!DOCTYPE x:xmpmeta [<!ENTITY leak SYSTEM 'file:///definitely-not-readable/host-secret'>]><x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description>&leak;</rdf:Description></rdf:RDF></x:xmpmeta>";
        string malformed = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'></x:xmpmeta>";

        RawFormatException? declaration = Assert.Throws<RawFormatException>(() => RawXmpPacket.Parse(Encoding.UTF8.GetBytes(entity), new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That((declaration ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That(declaration.Message, Does.Contain("prohibited"));
            Assert.That(declaration.Message, Does.Not.Contain("host-secret"));
            Assert.Throws<XmlException>(() => RawXmpPacket.Parse(Encoding.UTF8.GetBytes(malformed), new RawLimits()));
        });
    }

    [Test]
    public void CombinedMetadataLimitRejectsIndividuallyBoundedTagValues()
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 0xf100, TiffFieldType.Undefined, 48, Enumerable.Repeat((byte)1, 48).ToArray());
        builder.AddField(root, 0xf101, TiffFieldType.Undefined, 48, Enumerable.Repeat((byte)2, 48).ToArray());
        RawLimits limits = new RawLimits();
        limits.MaxTagValueBytes = 64;
        limits.MaxMetadataBytes = 80;
        limits.MaxXmpBytes = 80;
        RawOpenOptions options = new RawOpenOptions();
        options.Limits = limits;

        RawLimitException? exception = Assert.Throws<RawLimitException>(() => RawCodec.OpenMetadata(builder.Build(), null, options));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
    }

    [Test]
    public void EveryTruncatedPrefixOfMetadataTiffFailsWithAMachineReadableError()
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about=''/></rdf:RDF></x:xmpmeta>";
        byte[] packet = Encoding.UTF8.GetBytes(xml);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        builder.AddField(root, 700, TiffFieldType.Byte, (ulong)packet.Length, packet);
        byte[] complete = builder.Build();

        RawDocument document = RawCodec.OpenMetadata(complete, null, new RawOpenOptions());
        Assert.That(document.Metadata.XmpPackets, Has.Length.EqualTo(1));

        for (int length = 0; length < complete.Length; length++)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            RawFormatException? exception = Assert.Throws<RawFormatException>(() => RawCodec.OpenMetadata(truncated, null, new RawOpenOptions()), "prefix length " + length);
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.Not.EqualTo(RawDiagnosticCode.None), "prefix length " + length);
        }
    }

    [Test]
    public void StreamCopyStopsAtTheFirstCancellationBoundary()
    {
        byte[] tiff = MinimalTiff();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        HostileReadStream stream = new HostileReadStream(tiff, cancellation, 4);
        RawOpenOptions options = new RawOpenOptions();
        options.CancellationToken = cancellation.Token;

        Assert.Throws<OperationCanceledException>(() => RawCodec.OpenMetadata(stream, 0, tiff.Length, null, options));

        Assert.Multiple(() =>
        {
            Assert.That(stream.ReadCount, Is.EqualTo(1));
            Assert.That(stream.BytesRead, Is.EqualTo(4));
        });
    }

    static TiffIfdGraph Parse(byte[] bytes, RawLimits limits)
    {
        return TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, limits));
    }

    static byte[] MinimalTiff()
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        builder.AddIfd();
        return builder.Build();
    }
}

internal class HostileReadStream : Stream
{
    internal byte[] Bytes;
    internal CancellationTokenSource? Cancellation;
    internal int MaximumRead;
    internal int BytesRead;
    internal int ReadCount;

    internal HostileReadStream(byte[] bytes, CancellationTokenSource? cancellation, int maximumRead)
    {
        Bytes = bytes;
        Cancellation = cancellation;
        MaximumRead = maximumRead;
        BytesRead = 0;
        ReadCount = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => BytesRead;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int remaining = Bytes.Length - BytesRead;
        if (remaining == 0)
        {
            return 0;
        }
        int read = Math.Min(count, Math.Min(MaximumRead, remaining));
        Array.Copy(Bytes, BytesRead, buffer, offset, read);
        BytesRead += read;
        ReadCount++;
        if (Cancellation != null)
        {
            Cancellation.Cancel();
        }
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
