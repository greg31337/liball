using System.Buffers.Binary;
using System.Text;

namespace avif.Tests;

public class ItemDataLocationTests
{
    [Test]
    public void ItemAndMediaDataBoxesExposeLazyBoundedPayloadViews()
    {
        byte[] source = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer =>
            {
                writer.BeginBox("free");
                writer.Write(new byte[] { 90 });
                writer.EndBox();
            },
            writer =>
            {
                writer.BeginBox("idat");
                writer.Write(new byte[] { 1, 2, 3, 4 });
                writer.EndBox();
                writer.BeginBox("skip");
                writer.EndBox();
            },
            writer =>
            {
                writer.BeginBox("mdat");
                writer.Write(new byte[] { 5, 6, 7 });
                writer.EndBox();
                writer.BeginBox("free");
                writer.EndBox();
            });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedDataBox? itemData = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDataBox();
        ParsedDataBox? mediaData = result.Meta.GetMediaDataBox(0);
        source[(itemData ?? throw new InvalidOperationException("Invalid internal state.")).GetPayloadOffset() + 1] = 9;
        byte[] itemBytes = new byte[3];
        byte[] partialBytes = new byte[3];
        InputReadResult itemRead = itemData.ReadExact(source, 1, itemBytes);
        InputReadResult partialRead = (mediaData ?? throw new InvalidOperationException("Invalid internal state.")).Read(source, 2, partialBytes);
        InputReadResult truncated = mediaData.ReadExact(source, 2, new byte[2]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.HasItemDataBox(), Is.True);
            Assert.That(itemData.GetBoxType(), Is.EqualTo("idat"));
            Assert.That(itemData.GetPayloadLength(), Is.EqualTo(4));
            Assert.That(itemRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(itemBytes, Is.EqualTo(new byte[] { 9, 3, 4 }));
            Assert.That(result.Meta.GetMediaDataBoxCount(), Is.EqualTo(1));
            Assert.That(mediaData.GetBoxType(), Is.EqualTo("mdat"));
            Assert.That(partialRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(partialRead.BytesRead, Is.EqualTo(1));
            Assert.That(partialBytes[0], Is.EqualTo(7));
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(result.Meta.GetMediaDataBox(1), Is.Null);
        });
    }

    [Test]
    public void SelfContainedDataReferenceResolvesAndReadsAbsoluteFileOffsets()
    {
        byte[] source = BuildDocument(
            new uint[] { 1 },
            writer =>
            {
                writer.BeginBox("mdat");
                writer.Write(new byte[] { 10, 11, 12, 13 });
                writer.EndBox();
            },
            writer => { },
            writer =>
            {
                WriteItemLocation(
                    writer,
                    new TestLocation[]
                    {
                        new TestLocation(1, 0, 1, 0, 8, 4)
                    });
                WriteDataInformation(writer, 0, 0, 1, entries =>
                {
                    entries.BeginBox("free");
                    entries.EndBox();
                    WriteUrl(entries, 0, 1, string.Empty, true);
                    entries.BeginBox("skip");
                    entries.EndBox();
                });
            },
            writer => { });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemLocation? location = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();
        ParsedItemExtent? extent = (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0);
        ParsedDataReference? reference = location.GetDataReference();
        byte[] payload = new byte[4];
        InputReadResult read = (extent ?? throw new InvalidOperationException("Invalid internal state.")).ReadExact(source, 0, payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.GetDataReferenceCount(), Is.EqualTo(1));
            Assert.That(reference, Is.SameAs(result.Meta.GetDataReference(0)));
            Assert.That((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetIndex(), Is.EqualTo(1));
            Assert.That(reference.GetKind(), Is.EqualTo(ParsedDataReferenceKind.Url));
            Assert.That(reference.IsSelfContained(), Is.True);
            Assert.That(reference.GetLocation(), Is.Empty);
            Assert.That(extent.HasResolvedRange(), Is.True);
            Assert.That(extent.GetResolvedOffset(), Is.EqualTo(8));
            Assert.That(read.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(payload, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
        });
    }

    [Test]
    public void ExternalUrlAndUrnReferencesRemainDescribedButUnresolved()
    {
        byte[] source = BuildDocument(
            new uint[] { 1, 2 },
            writer => { },
            writer => { },
            writer =>
            {
                WriteDataInformation(writer, 0, 0, 2, entries =>
                {
                    WriteUrl(entries, 0, 0, "https://example.test/items.bin", true);
                    WriteUrn(entries, 0, 0, "urn:example:item-data", string.Empty, true);
                });
                WriteItemLocation(
                    writer,
                    new TestLocation[]
                    {
                        new TestLocation(1, 0, 1, 1000, 4, 3),
                        new TestLocation(2, 0, 2, 2000, 5, 0)
                    });
            },
            writer => { });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemLocation? urlLocation = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();
        ParsedItemLocation? urnLocation = (result.Meta.FindItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();
        ParsedDataReference? url = result.Meta.GetDataReference(0);
        ParsedDataReference? urn = result.Meta.GetDataReference(1);
        InputReadResult unavailable = ((urlLocation ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).Read(
            source,
            0,
            new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((url ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedDataReferenceKind.Url));
            Assert.That(url.IsSelfContained(), Is.False);
            Assert.That(url.GetLocation(), Is.EqualTo("https://example.test/items.bin"));
            Assert.That((urn ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedDataReferenceKind.Urn));
            Assert.That(urn.GetName(), Is.EqualTo("urn:example:item-data"));
            Assert.That(urn.GetLocation(), Is.Empty);
            Assert.That(urlLocation.GetDataReference(), Is.SameAs(url));
            Assert.That((urlLocation.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).HasResolvedRange(), Is.False);
            Assert.That(urlLocation.HasKnownTotalLength(), Is.True);
            Assert.That(urlLocation.GetTotalLength(), Is.EqualTo(3));
            Assert.That((urnLocation ?? throw new InvalidOperationException("Invalid internal state.")).HasKnownTotalLength(), Is.False);
            Assert.That(unavailable.Code, Is.EqualTo(AvifResultCode.InvalidState));
        });
    }

    [Test]
    public void DataReferenceIndicesMustResolveThroughDref()
    {
        byte[] missingData = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer => WriteItemLocation(
                writer,
                new TestLocation[] { new TestLocation(1, 0, 1, 0, 0, 1) }),
            writer => { });
        byte[] outOfRangeData = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer =>
            {
                WriteDataInformation(writer, 0, 0, 1, entries =>
                {
                    WriteUrl(entries, 0, 0, "file.bin", true);
                });
                WriteItemLocation(
                    writer,
                    new TestLocation[] { new TestLocation(1, 0, 2, 0, 0, 1) });
            },
            writer => { });

        MetaParseResult missing = MetaBoxParser.Parse(missingData, CreateLimits());
        MetaParseResult outOfRange = MetaBoxParser.Parse(outOfRangeData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(outOfRange.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((missing.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iloc"));
            Assert.That(((outOfRange.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iloc"));
        });
    }

    [Test]
    public void DrefRequiresAValidCountVersionAndFlags()
    {
        MetaParseResult zero = ParseDataInformation(0, 0, 0, writer => { });
        MetaParseResult mismatch = ParseDataInformation(0, 0, 2, writer =>
        {
            WriteUrl(writer, 0, 0, "file.bin", true);
        });
        MetaParseResult version = ParseDataInformation(1, 0, 1, writer =>
        {
            WriteUrl(writer, 0, 0, "file.bin", true);
        });
        MetaParseResult flags = ParseDataInformation(0, 1, 1, writer =>
        {
            WriteUrl(writer, 0, 0, "file.bin", true);
        });

        Assert.Multiple(() =>
        {
            Assert.That(zero.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(mismatch.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void DataEntriesEnforceFullBoxAndStringSyntax()
    {
        MetaParseResult urlVersion = ParseDataInformation(0, 0, 1, writer =>
        {
            WriteUrl(writer, 1, 0, "file.bin", true);
        });
        MetaParseResult invalidFlags = ParseDataInformation(0, 0, 1, writer =>
        {
            WriteUrl(writer, 0, 2, "file.bin", true);
        });
        MetaParseResult selfContainedPayload = ParseDataInformation(0, 0, 1, writer =>
        {
            WriteUrl(writer, 0, 1, "forbidden", true);
        });
        MetaParseResult unterminatedUrl = ParseDataInformation(0, 0, 1, writer =>
        {
            WriteUrl(writer, 0, 0, "file.bin", false);
        });
        MetaParseResult emptyUrn = ParseDataInformation(0, 0, 1, writer =>
        {
            WriteUrn(writer, 0, 0, string.Empty, string.Empty, true);
        });
        MetaParseResult urnSelfContained = ParseDataInformation(0, 0, 1, writer =>
        {
            WriteUrn(writer, 0, 1, "urn:test", string.Empty, true);
        });

        Assert.Multiple(() =>
        {
            Assert.That(urlVersion.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(invalidFlags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(selfContainedPayload.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(unterminatedUrl.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(emptyUrn.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(urnSelfContained.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void DinfRequiresExactlyOneDrefAndIgnoresAdditionalBoxes()
    {
        MetaParseResult missing = ParseDinf(writer => { });
        MetaParseResult duplicate = ParseDinf(writer =>
        {
            WriteDref(writer, 0, 0, 1, entries =>
            {
                WriteUrl(entries, 0, 0, "a.bin", true);
            });
            WriteDref(writer, 0, 0, 1, entries =>
            {
                WriteUrl(entries, 0, 0, "b.bin", true);
            });
        });
        MetaParseResult additional = ParseDataInformation(0, 0, 1, writer =>
        {
            writer.BeginBox("zzzz");
            writer.EndBox();
            WriteUrl(writer, 0, 0, "file.bin", true);
        });
        byte[] duplicateDinfData = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer =>
            {
                WriteDataInformation(writer, 0, 0, 1, entries =>
                {
                    WriteUrl(entries, 0, 0, "a.bin", true);
                });
                WriteDataInformation(writer, 0, 0, 1, entries =>
                {
                    WriteUrl(entries, 0, 0, "b.bin", true);
                });
            },
            writer => { });
        MetaParseResult duplicateDinf = MetaBoxParser.Parse(
            duplicateDinfData,
            CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(additional.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(duplicateDinf.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void DuplicateIdatAndExcessDataReferencesAreRejected()
    {
        byte[] duplicateData = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer =>
            {
                writer.BeginBox("idat");
                writer.EndBox();
                writer.BeginBox("idat");
                writer.EndBox();
            },
            writer => { });
        byte[] limitedData = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer => WriteDataInformation(writer, 0, 0, 2, entries =>
            {
                WriteUrl(entries, 0, 0, "a.bin", true);
                WriteUrl(entries, 0, 0, "b.bin", true);
            }),
            writer => { });
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxReferenceCount = 1;

        MetaParseResult duplicate = MetaBoxParser.Parse(duplicateData, CreateLimits());
        MetaParseResult limited = MetaBoxParser.Parse(limitedData, limits);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(((limited.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    private static MetaParseResult ParseDataInformation(
        int version,
        int flags,
        uint entryCount,
        Action<IsoBoxWriter> writeEntries)
    {
        byte[] source = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer => WriteDataInformation(
                writer,
                version,
                flags,
                entryCount,
                writeEntries),
            writer => { });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static MetaParseResult ParseDinf(Action<IsoBoxWriter> writeChildren)
    {
        byte[] source = BuildDocument(
            new uint[] { 1 },
            writer => { },
            writer => { },
            writer =>
            {
                writer.BeginBox("dinf");
                writeChildren(writer);
                writer.EndBox();
            },
            writer => { });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static byte[] BuildDocument(
        uint[] itemIds,
        Action<IsoBoxWriter> writeBeforeMeta,
        Action<IsoBoxWriter> writeBetweenHeaderAndItems,
        Action<IsoBoxWriter> writeMetaChildren,
        Action<IsoBoxWriter> writeAfterMeta)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        writeBeforeMeta(writer);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        writeBetweenHeaderAndItems(writer);
        WriteItemInfo(writer, itemIds);
        writeMetaChildren(writer);
        writer.EndBox();
        writeAfterMeta(writer);
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
        WriteUInt32(writer, (uint)itemIds.Length);
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

    private static void WriteItemLocation(
        IsoBoxWriter writer,
        TestLocation[] locations)
    {
        writer.BeginFullBox("iloc", 1, 0);
        writer.Write(new byte[] { 68, 64 });
        WriteUInt16(writer, (ushort)locations.Length);
        int index = 0;
        while (index < locations.Length)
        {
            TestLocation location = locations[index];
            WriteUInt16(writer, (ushort)location.ItemId);
            WriteUInt16(writer, (ushort)location.ConstructionMethod);
            WriteUInt16(writer, location.DataReferenceIndex);
            WriteUInt32(writer, location.BaseOffset);
            WriteUInt16(writer, 1);
            WriteUInt32(writer, location.ExtentOffset);
            WriteUInt32(writer, location.ExtentLength);
            index++;
        }

        writer.EndBox();
    }

    private static void WriteDataInformation(
        IsoBoxWriter writer,
        int version,
        int flags,
        uint entryCount,
        Action<IsoBoxWriter> writeEntries)
    {
        writer.BeginBox("dinf");
        writer.BeginBox("free");
        writer.EndBox();
        WriteDref(writer, version, flags, entryCount, writeEntries);
        writer.BeginBox("skip");
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteDref(
        IsoBoxWriter writer,
        int version,
        int flags,
        uint entryCount,
        Action<IsoBoxWriter> writeEntries)
    {
        writer.BeginFullBox("dref", version, flags);
        WriteUInt32(writer, entryCount);
        writeEntries(writer);
        writer.EndBox();
    }

    private static void WriteUrl(
        IsoBoxWriter writer,
        int version,
        int flags,
        string location,
        bool terminate)
    {
        writer.BeginFullBox("url ", version, flags);
        if (location.Length > 0)
        {
            writer.Write(Encoding.UTF8.GetBytes(location));
        }

        if (terminate && flags == 0)
        {
            writer.Write(new byte[] { 0 });
        }
        else if (terminate && flags != 0 && location.Length > 0)
        {
            writer.Write(new byte[] { 0 });
        }

        writer.EndBox();
    }

    private static void WriteUrn(
        IsoBoxWriter writer,
        int version,
        int flags,
        string name,
        string location,
        bool terminate)
    {
        writer.BeginFullBox("urn ", version, flags);
        writer.Write(Encoding.UTF8.GetBytes(name));
        if (terminate)
        {
            writer.Write(new byte[] { 0 });
        }

        writer.Write(Encoding.UTF8.GetBytes(location));
        if (terminate)
        {
            writer.Write(new byte[] { 0 });
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
        limits.MaxReferenceCount = 256;
        limits.MaxTemporaryBytes = 65536;
        return limits;
    }

    private class TestLocation
    {
        public uint ItemId;
        public int ConstructionMethod;
        public ushort DataReferenceIndex;
        public uint BaseOffset;
        public uint ExtentOffset;
        public uint ExtentLength;

        public TestLocation(
            uint itemId,
            int constructionMethod,
            ushort dataReferenceIndex,
            uint baseOffset,
            uint extentOffset,
            uint extentLength)
        {
            ItemId = itemId;
            ConstructionMethod = constructionMethod;
            DataReferenceIndex = dataReferenceIndex;
            BaseOffset = baseOffset;
            ExtentOffset = extentOffset;
            ExtentLength = extentLength;
        }
    }
}
