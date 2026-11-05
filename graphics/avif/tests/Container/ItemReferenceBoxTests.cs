using System.Buffers.Binary;

namespace avif.Tests;

public class ItemReferenceBoxTests
{
    [Test]
    public void VersionZeroParsesTypedRelationshipsInOrder()
    {
        byte[] source = BuildDocument(new uint[] { 1, 2, 3, 4, 5, 6 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "auxl", 0, 1, new uint[] { 2 });
            WriteReference(writer, "thmb", 0, 3, new uint[] { 2 });
            WriteReference(writer, "dimg", 0, 4, new uint[] { 2, 3 });
            WriteReference(writer, "prem", 0, 5, new uint[] { 1 });
            WriteReference(writer, "cdsc", 0, 6, new uint[] { 2 });
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemReference? derived = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReference(2);
        ParsedItem? target = result.Meta.FindItem(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.GetReferenceCount(), Is.EqualTo(5));
            Assert.That((result.Meta.GetReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedItemReferenceKind.Auxiliary));
            Assert.That((result.Meta.GetReference(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedItemReferenceKind.Thumbnail));
            Assert.That((derived ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedItemReferenceKind.DerivedImage));
            Assert.That((result.Meta.GetReference(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedItemReferenceKind.Premultiplied));
            Assert.That((result.Meta.GetReference(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedItemReferenceKind.ContentDescription));
            Assert.That(derived.GetFromItemId(), Is.EqualTo(4));
            Assert.That(derived.GetTargetCount(), Is.EqualTo(2));
            Assert.That(derived.GetTargetItemId(0), Is.EqualTo(2));
            Assert.That(derived.GetTargetItemId(1), Is.EqualTo(3));
            Assert.That((result.Meta.FindItem(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReference(0), Is.SameAs(derived));
            Assert.That((target ?? throw new InvalidOperationException("Invalid internal state.")).GetIncomingReferenceCount(), Is.EqualTo(4));
            Assert.That((target.GetIncomingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("auxl"));
            Assert.That((target.GetIncomingReference(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("thmb"));
            Assert.That((target.GetIncomingReference(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("dimg"));
            Assert.That((target.GetIncomingReference(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("cdsc"));
        });
    }

    [Test]
    public void VersionOneSupportsLargeIdsAndPreservesUnknownTypes()
    {
        byte[] source = BuildDocument(new uint[] { 70000, 70001 }, writer =>
        {
            writer.BeginFullBox("iref", 1, 0);
            WriteReference(writer, "meta", 1, 70000, new uint[] { 70001 });
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemReference? reference = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReference(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedItemReferenceKind.Unknown));
            Assert.That(reference.GetBoxType(), Is.EqualTo("meta"));
            Assert.That(reference.GetTypeValue(), Is.EqualTo(IsoBoxType.GetValue("meta")));
            Assert.That(reference.GetFromItemId(), Is.EqualTo(70000));
            Assert.That(reference.GetTargetItemId(0), Is.EqualTo(70001));
            Assert.That((result.Meta.FindItem(70001) ?? throw new InvalidOperationException("Invalid internal state.")).GetIncomingReference(0), Is.SameAs(reference));
        });
    }

    [Test]
    public void ParserRejectsUnsupportedVersionsAndNonzeroFlags()
    {
        MetaParseResult version = ParseCustomIref(2, 0, writer => { });
        MetaParseResult flags = ParseCustomIref(0, 1, writer => { });

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void ItemIdsCountsAndTargetsMustDescribeValidRelationships()
    {
        MetaParseResult zeroSource = ParseSingleReference(0, 0, new uint[] { 1 });
        MetaParseResult zeroCount = ParseSingleReference(0, 1, System.Array.Empty<uint>());
        MetaParseResult zeroTarget = ParseSingleReference(0, 1, new uint[] { 0 });
        MetaParseResult self = ParseSingleReference(0, 1, new uint[] { 1 });
        MetaParseResult repeatedTarget = ParseSingleReference(0, 1, new uint[] { 2, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(zeroSource.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroCount.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroTarget.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(self.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(repeatedTarget.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void EachSourceAndReferenceTypeUsesOneChildBox()
    {
        byte[] source = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "dimg", 0, 1, new uint[] { 2 });
            WriteReference(writer, "dimg", 0, 1, new uint[] { 3 });
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidItem));
    }

    [Test]
    public void SourcesAndTargetsMustIdentifyDeclaredItems()
    {
        byte[] missingSourceData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "auxl", 0, 3, new uint[] { 2 });
            writer.EndBox();
        });
        byte[] missingTargetData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "auxl", 0, 1, new uint[] { 3 });
            writer.EndBox();
        });

        MetaParseResult missingSource = MetaBoxParser.Parse(missingSourceData, CreateLimits());
        MetaParseResult missingTarget = MetaBoxParser.Parse(missingTargetData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missingSource.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(missingTarget.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((missingSource.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iref"));
            Assert.That(((missingTarget.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iref"));
        });
    }

    [Test]
    public void RelationshipPayloadMustBeCompleteAndExact()
    {
        MetaParseResult truncated = ParseCustomIref(0, 0, writer =>
        {
            writer.BeginBox("auxl");
            writer.Write(new byte[] { 0, 1, 0, 1 });
            writer.EndBox();
        });
        MetaParseResult trailing = ParseCustomIref(0, 0, writer =>
        {
            writer.BeginBox("auxl");
            writer.Write(new byte[] { 0, 1, 0, 1, 0, 2, 255 });
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void ReferenceLimitAppliesAcrossRelationshipBoxes()
    {
        byte[] source = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "auxl", 0, 1, new uint[] { 2 });
            WriteReference(writer, "thmb", 0, 3, new uint[] { 2 });
            writer.EndBox();
        });
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxReferenceCount = 1;

        MetaParseResult result = MetaBoxParser.Parse(source, limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void FreeAndSkipChildrenAreIgnoredButDuplicateIrefBoxesFail()
    {
        byte[] ignoredData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            writer.BeginBox("free");
            writer.EndBox();
            WriteReference(writer, "auxl", 0, 1, new uint[] { 2 });
            writer.BeginBox("skip");
            writer.EndBox();
            writer.EndBox();
        });
        byte[] duplicateData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginFullBox("iref", 0, 0);
            writer.EndBox();
            writer.BeginFullBox("iref", 0, 0);
            writer.EndBox();
        });

        MetaParseResult ignored = MetaBoxParser.Parse(ignoredData, CreateLimits());
        MetaParseResult duplicate = MetaBoxParser.Parse(duplicateData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(ignored.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((ignored.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount(), Is.EqualTo(1));
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    private static MetaParseResult ParseSingleReference(
        int version,
        uint fromItemId,
        uint[] toItemIds)
    {
        byte[] source = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginFullBox("iref", version, 0);
            WriteReference(writer, "auxl", version, fromItemId, toItemIds);
            writer.EndBox();
        });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static MetaParseResult ParseCustomIref(
        int version,
        int flags,
        Action<IsoBoxWriter> writeReferences)
    {
        byte[] source = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginFullBox("iref", version, flags);
            writeReferences(writer);
            writer.EndBox();
        });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static byte[] BuildDocument(
        uint[] itemIds,
        Action<IsoBoxWriter> writeChildren)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer, itemIds);
        writeChildren(writer);
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteHandler(IsoBoxWriter writer)
    {
        writer.BeginFullBox("hdlr", 0, 0);
        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        writer.Write(fields);
        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer, uint[] itemIds)
    {
        writer.BeginFullBox("iinf", 1, 0);
        byte[] count = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(count, (uint)itemIds.Length);
        writer.Write(count);
        int index = 0;
        while (index < itemIds.Length)
        {
            writer.BeginFullBox("infe", 3, 0);
            byte[] entry = new byte[11];
            BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), itemIds[index]);
            WriteFourCc(entry.AsSpan(6, 4), "av01");
            writer.Write(entry);
            writer.EndBox();
            index++;
        }

        writer.EndBox();
    }

    private static void WriteReference(
        IsoBoxWriter writer,
        string type,
        int version,
        uint fromItemId,
        uint[] toItemIds)
    {
        writer.BeginBox(type);
        if (version == 0)
        {
            WriteUInt16(writer, (ushort)fromItemId);
        }
        else
        {
            WriteUInt32(writer, fromItemId);
        }

        WriteUInt16(writer, (ushort)toItemIds.Length);
        int index = 0;
        while (index < toItemIds.Length)
        {
            if (version == 0)
            {
                WriteUInt16(writer, (ushort)toItemIds[index]);
            }
            else
            {
                WriteUInt32(writer, toItemIds[index]);
            }

            index++;
        }

        writer.EndBox();
    }

    private static void WriteUInt16(IsoBoxWriter writer, ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 131072;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 64;
        limits.MaxPropertyCount = 256;
        limits.MaxReferenceCount = 256;
        limits.MaxTemporaryBytes = 65536;
        return limits;
    }
}
