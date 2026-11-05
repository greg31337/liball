using System.Buffers.Binary;

namespace avif.Tests;

public class IsoBoxTests
{
    [Test]
    public void WriterAndReaderSupportNormalLargeUuidAndToEndHeaders()
    {
        byte[] userType = new byte[16];
        int userIndex = 0;
        while (userIndex < userType.Length)
        {
            userType[userIndex] = (byte)userIndex;
            userIndex++;
        }

        IsoBoxWriter writer = new IsoBoxWriter(0, 128);
        writer.BeginBox("free");
        writer.Write(new byte[] { 1, 2, 3 });
        writer.EndBox();
        writer.BeginLargeUuidBox(userType);
        writer.Write(new byte[] { 4, 5 });
        writer.EndBox();
        writer.BeginToEndBox("skip");
        writer.Write(new byte[] { 6 });
        writer.EndBox();
        IsoBoxWriteResult written = writer.Finish();

        IsoBoxParseResult parsed = IsoBoxReader.Parse(written.Data, CreateLimits(), true);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Boxes.Count, Is.EqualTo(3));
            Assert.That(parsed.Boxes[0].Type, Is.EqualTo("free"));
            Assert.That(parsed.Boxes[0].Size, Is.EqualTo(11));
            Assert.That(parsed.Boxes[0].HeaderSize, Is.EqualTo(8));
            Assert.That(parsed.Boxes[0].UsesLargeSize, Is.False);
            Assert.That(parsed.Boxes[1].Type, Is.EqualTo("uuid"));
            Assert.That(parsed.Boxes[1].Size, Is.EqualTo(34));
            Assert.That(parsed.Boxes[1].HeaderSize, Is.EqualTo(32));
            Assert.That(parsed.Boxes[1].UsesLargeSize, Is.True);
            Assert.That(parsed.Boxes[1].IsUuid, Is.True);
            Assert.That(parsed.Boxes[1].UserType, Is.EqualTo(userType));
            Assert.That(parsed.Boxes[2].Type, Is.EqualTo("skip"));
            Assert.That(parsed.Boxes[2].ExtendsToEnd, Is.True);
            Assert.That(parsed.Boxes[2].EndOffset, Is.EqualTo(written.Data.Length));
        });
    }

    [Test]
    public void FullBoxContainerRecursionRetainsFieldsPathsAndOpaquePayload()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 128);
        writer.BeginFullBox("meta", 2, 1193046);
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        writer.BeginBox("zzzz");
        writer.Write(new byte[] { 9, 8, 7 });
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        IsoBoxWriteResult written = writer.Finish();

        IsoBoxParseResult parsed = IsoBoxReader.Parse(written.Data, CreateLimits(), true);
        IsoBox meta = parsed.Boxes[0];
        IsoBox iprp = meta.Children[0];
        IsoBox ipco = iprp.Children[0];
        IsoBox opaque = ipco.Children[0];
        IsoFullBox fields = IsoFullBox.Parse(written.Data, meta);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(meta.IsContainer, Is.True);
            Assert.That(meta.HasFullBoxFields, Is.True);
            Assert.That(meta.Version, Is.EqualTo(2));
            Assert.That(meta.Flags, Is.EqualTo(1193046));
            Assert.That(fields.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(fields.PayloadOffset, Is.EqualTo(meta.PayloadOffset + 4));
            Assert.That(iprp.Type, Is.EqualTo("iprp"));
            Assert.That(ipco.Type, Is.EqualTo("ipco"));
            Assert.That(opaque.Type, Is.EqualTo("zzzz"));
            Assert.That(opaque.IsUnknown, Is.True);
            Assert.That(opaque.RawData.Length, Is.EqualTo(11));
            Assert.That(opaque.RawData[8..], Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(parsed.Resources.BoxCount.Current, Is.EqualTo(4));
            Assert.That(parsed.Resources.BoxDepth.Current, Is.Zero);
        });
    }

    [Test]
    public void UnknownBoxesCanBeSkippedOrPreservedAndRoundTripExactly()
    {
        IsoBoxWriter sourceWriter = new IsoBoxWriter(0, 64);
        sourceWriter.BeginBox("abcd");
        sourceWriter.Write(new byte[] { 0, 1, 2, 3, 4 });
        sourceWriter.EndBox();
        IsoBoxWriteResult source = sourceWriter.Finish();

        IsoBoxParseResult skipped = IsoBoxReader.Parse(source.Data, CreateLimits(), false);
        IsoBoxParseResult preserved = IsoBoxReader.Parse(source.Data, CreateLimits(), true);
        IsoBoxWriter copyWriter = new IsoBoxWriter(0, 64);
        AvifResultCode copyCode = copyWriter.WritePreservedBox(preserved.Boxes[0]);
        IsoBoxWriteResult copy = copyWriter.Finish();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(skipped.Boxes[0].RawData, Is.Empty);
            Assert.That(skipped.Boxes[0].PayloadOffset, Is.EqualTo(8));
            Assert.That(skipped.Boxes[0].PayloadSize, Is.EqualTo(5));
            Assert.That(preserved.Boxes[0].RawData, Is.EqualTo(source.Data));
            Assert.That(copyCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(copy.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(copy.Data, Is.EqualTo(source.Data));
        });
    }

    [Test]
    public void GroupsListRecursesWithoutFullBoxPrefixAndPreservationIsBounded()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 64);
        writer.BeginBox("grpl");
        writer.BeginBox("zzzz");
        writer.Write(new byte[] { 1, 2, 3 });
        writer.EndBox();
        writer.EndBox();
        byte[] source = writer.Finish().Data;

        IsoBoxParseResult skipped = IsoBoxReader.Parse(source, CreateLimits(), false);
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxTemporaryBytes = 10;
        IsoBoxParseResult bounded = IsoBoxReader.Parse(source, limits, true);

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(skipped.Boxes[0].Type, Is.EqualTo("grpl"));
            Assert.That(skipped.Boxes[0].HasFullBoxFields, Is.False);
            Assert.That(skipped.Boxes[0].Children.Count, Is.EqualTo(1));
            Assert.That(skipped.Boxes[0].Children[0].Type, Is.EqualTo("zzzz"));
            Assert.That(bounded.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(bounded.Resources.TemporaryBytes.Current, Is.Zero);
            Assert.That((bounded.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/grpl/zzzz"));
        });
    }

    [Test]
    public void ParserDistinguishesSmallHeadersTruncationAndParentOverrun()
    {
        byte[] tooSmall = CreateHeader(4, "free");
        byte[] truncated = CreateHeader(20, "free");
        byte[] nestedOverrun = new byte[24];
        CreateHeader(16, "moov").CopyTo(nestedOverrun, 0);
        CreateHeader(16, "free").CopyTo(nestedOverrun, 8);

        IsoBoxParseResult smallResult = IsoBoxReader.Parse(tooSmall, CreateLimits(), false);
        IsoBoxParseResult truncatedResult = IsoBoxReader.Parse(truncated, CreateLimits(), false);
        IsoBoxParseResult nestedResult = IsoBoxReader.Parse(nestedOverrun, CreateLimits(), false);

        Assert.Multiple(() =>
        {
            Assert.That(smallResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That((smallResult.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/free"));
            Assert.That(smallResult.Resources.BoxDepth.Current, Is.Zero);
            Assert.That(truncatedResult.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That((truncatedResult.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).ByteOffset, Is.Zero);
            Assert.That(nestedResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That((nestedResult.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/moov/free"));
            Assert.That(nestedResult.Resources.BoxDepth.Current, Is.Zero);
        });
    }

    [Test]
    public void DiagnosticPathsEscapeSeparatorBytesInOpaqueBoxTypes()
    {
        byte[] invalid = CreateHeader(4, "a/bc");

        IsoBoxParseResult result = IsoBoxReader.Parse(invalid, CreateLimits(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That((result.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/a%2Fbc"));
            Assert.That(result.Resources.BoxDepth.Current, Is.Zero);
        });
    }

    [Test]
    public void ParserRejectsTruncatedUuidLargeSizeOverflowAndShortFullBox()
    {
        byte[] uuid = CreateHeader(24, "uuid");
        byte[] huge = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(huge.AsSpan(0, 4), 1);
        new byte[] { 102, 114, 101, 101 }.CopyTo(huge, 4);
        BinaryPrimitives.WriteUInt64BigEndian(huge.AsSpan(8, 8), ulong.MaxValue);
        byte[] shortMeta = CreateHeader(8, "meta");

        IsoBoxParseResult uuidResult = IsoBoxReader.Parse(uuid, CreateLimits(), false);
        IsoBoxParseResult hugeResult = IsoBoxReader.Parse(huge, CreateLimits(), false);
        IsoBoxParseResult metaResult = IsoBoxReader.Parse(shortMeta, CreateLimits(), false);

        Assert.Multiple(() =>
        {
            Assert.That(uuidResult.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(hugeResult.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That((hugeResult.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Resource));
            Assert.That(metaResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That((metaResult.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta"));
        });
    }

    [Test]
    public void ParserEnforcesSourceBoxCountAndNestingLimits()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 64);
        writer.BeginBox("moov");
        writer.BeginBox("trak");
        writer.BeginBox("free");
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        byte[] source = writer.Finish().Data;

        AvifSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaxBoxDepth = 2;
        IsoBoxParseResult depth = IsoBoxReader.Parse(source, depthLimits, false);

        AvifSecurityLimits countLimits = CreateLimits();
        countLimits.MaxBoxCount = 2;
        IsoBoxParseResult count = IsoBoxReader.Parse(source, countLimits, false);

        AvifSecurityLimits sourceLimits = CreateLimits();
        sourceLimits.MaxSourceBytes = source.Length - 1;
        IsoBoxParseResult bytes = IsoBoxReader.Parse(source, sourceLimits, false);

        Assert.Multiple(() =>
        {
            Assert.That(depth.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(depth.Resources.BoxCount.Current, Is.EqualTo(2));
            Assert.That(depth.Resources.BoxDepth.Current, Is.Zero);
            Assert.That((depth.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/moov/trak"));
            Assert.That(count.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(count.Resources.BoxCount.Current, Is.EqualTo(2));
            Assert.That(bytes.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(bytes.Resources.SourceBytes.Current, Is.Zero);
        });
    }

    [Test]
    public void ToEndChildSealsItsParentContentButAllowsFollowingTopLevelBox()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 64);
        writer.BeginBox("moov");
        writer.BeginToEndBox("free");
        writer.Write(new byte[] { 1 });
        writer.EndBox();
        AvifResultCode parentEnd = writer.EndBox();
        AvifResultCode nextBegin = writer.BeginBox("skip");
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        IsoBoxParseResult parsed = IsoBoxReader.Parse(result.Data, CreateLimits(), false);

        Assert.Multiple(() =>
        {
            Assert.That(parentEnd, Is.EqualTo(AvifResultCode.Success));
            Assert.That(nextBegin, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Boxes.Count, Is.EqualTo(2));
            Assert.That(parsed.Boxes[0].Children[0].ExtendsToEnd, Is.True);
            Assert.That(parsed.Boxes[1].Type, Is.EqualTo("skip"));
        });
    }

    [Test]
    public void WriterRejectsInvalidFieldsUnclosedBoxesAndWritesPastToEndRoot()
    {
        IsoBoxWriter invalidFields = new IsoBoxWriter(0, 64);
        AvifResultCode fieldsCode = invalidFields.BeginFullBox("meta", 256, 0);

        IsoBoxWriter unclosed = new IsoBoxWriter(0, 64);
        unclosed.BeginBox("free");
        IsoBoxWriteResult unfinished = unclosed.Finish();
        unclosed.EndBox();
        IsoBoxWriteResult completed = unclosed.Finish();

        IsoBoxWriter sealedWriter = new IsoBoxWriter(0, 64);
        sealedWriter.BeginToEndBox("free");
        sealedWriter.EndBox();
        AvifResultCode sealedCode = sealedWriter.BeginBox("skip");

        IsoBoxWriter boundedWriter = new IsoBoxWriter(0, 11);
        AvifResultCode boundedCode = boundedWriter.BeginFullBox("meta", 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(fieldsCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidFields.Output.Length, Is.Zero);
            Assert.That(unfinished.Code, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(completed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(sealedCode, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(boundedCode, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(boundedWriter.Output.Length, Is.Zero);
        });
    }

    private static byte[] CreateHeader(uint size, string type)
    {
        byte[] header = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), size);
        header[4] = (byte)type[0];
        header[5] = (byte)type[1];
        header[6] = (byte)type[2];
        header[7] = (byte)type[3];
        return header;
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 1024;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 64;
        return limits;
    }
}
