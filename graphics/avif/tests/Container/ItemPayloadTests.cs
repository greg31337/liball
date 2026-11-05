using System.Buffers.Binary;

namespace avif.Tests;

public class ItemPayloadTests
{
    [Test]
    public void MultiExtentPayloadsReadAcrossBoundariesAndExtractLazily()
    {
        byte[] source = BuildDocument(
            new uint[] { 1 },
            new byte[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 },
            new TestLocation[]
            {
                new TestLocation(
                    1,
                    0,
                    new TestExtent[]
                    {
                        new TestExtent(0, 8, 2),
                        new TestExtent(0, 13, 3)
                    })
            },
            System.Array.Empty<TestReference>());

        MetaParseResult parsed = MetaBoxParser.Parse(source, CreateLimits());
        ItemPayloadResolveResult first = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(1);
        ItemPayloadResolveResult second = parsed.Meta.ResolveItemPayload(1);
        source[8] = 99;
        byte[] partialBytes = new byte[4];
        InputReadResult partial = (first.Payload ?? throw new InvalidOperationException("Invalid internal state.")).Read(source, 1, partialBytes);
        EncodedItemExtractionResult extracted = parsed.Meta.ExtractEncodedItem(source, 1);
        byte[] extractedBytes = (extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray();
        AvifResultCode closeCode = extracted.Data.Close();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.Payload, Is.SameAs(second.Payload));
            Assert.That(first.Payload.GetLength(), Is.EqualTo(5));
            Assert.That(first.Payload.GetSegmentCount(), Is.EqualTo(2));
            Assert.That(partial.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(partial.BytesRead, Is.EqualTo(4));
            Assert.That(partialBytes, Is.EqualTo(new byte[] { 11, 15, 16, 17 }));
            Assert.That(extracted.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((extracted.Item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(1));
            Assert.That(extractedBytes, Is.EqualTo(new byte[] { 99, 11, 15, 16, 17 }));
            Assert.That(closeCode, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ItemConstructionResolvesRecursivelyAndSharesPhysicalSegments()
    {
        byte[] source = BuildDocument(
            new uint[] { 1, 2, 3, 4 },
            new byte[] { 10, 11, 12, 13, 14, 15, 16, 17 },
            new TestLocation[]
            {
                new TestLocation(
                    1,
                    2,
                    new TestExtent[] { new TestExtent(1, 2, 4) }),
                new TestLocation(
                    2,
                    2,
                    new TestExtent[] { new TestExtent(1, 0, 0) }),
                new TestLocation(
                    3,
                    0,
                    new TestExtent[] { new TestExtent(0, 8, 8) }),
                new TestLocation(
                    4,
                    2,
                    new TestExtent[] { new TestExtent(1, 0, 0) })
            },
            new TestReference[]
            {
                new TestReference(1, new uint[] { 2 }),
                new TestReference(2, new uint[] { 3 }),
                new TestReference(4, new uint[] { 3 })
            });

        MetaParseResult parsed = MetaBoxParser.Parse(source, CreateLimits());
        ItemPayloadResolveResult root = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(1);
        ItemPayloadResolveResult middle = parsed.Meta.ResolveItemPayload(2);
        ItemPayloadResolveResult sourcePayload = parsed.Meta.ResolveItemPayload(3);
        ItemPayloadResolveResult shared = parsed.Meta.ResolveItemPayload(4);
        EncodedItemExtractionResult extracted = parsed.Meta.ExtractEncodedItem(source, 1);
        byte[] extractedBytes = (extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray();
        extracted.Data.Close();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(root.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((root.Payload ?? throw new InvalidOperationException("Invalid internal state.")).GetLength(), Is.EqualTo(4));
            Assert.That((root.Payload.GetSegment(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSourceOffset(), Is.EqualTo(10));
            Assert.That((middle.Payload ?? throw new InvalidOperationException("Invalid internal state.")).GetSegment(0), Is.SameAs((sourcePayload.Payload ?? throw new InvalidOperationException("Invalid internal state.")).GetSegment(0)));
            Assert.That((shared.Payload ?? throw new InvalidOperationException("Invalid internal state.")).GetSegment(0), Is.SameAs(sourcePayload.Payload.GetSegment(0)));
            Assert.That(
                parsed.Meta.GetItemPayloadResolver().GetCachedPayloadCount(),
                Is.EqualTo(4));
            Assert.That(extractedBytes, Is.EqualTo(new byte[] { 12, 13, 14, 15 }));
        });
    }

    [Test]
    public void ItemDataConstructionExtractsRelativeIdatExtents()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer, new uint[] { 1 });
        writer.BeginBox("idat");
        writer.Write(new byte[] { 1, 2, 3, 4, 5 });
        writer.EndBox();
        WriteItemLocations(writer, new TestLocation[]
        {
            new TestLocation(
                1,
                1,
                new TestExtent[] { new TestExtent(0, 1, 3) })
        });
        writer.EndBox();
        IsoBoxWriteResult written = writer.Finish();

        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        ItemPayloadResolveResult payload = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(1);
        EncodedItemExtractionResult extracted = parsed.Meta.ExtractEncodedItem(
            written.Data,
            1);
        byte[] extractedBytes = (extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray();
        extracted.Data.Close();

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(payload.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((payload.Payload ?? throw new InvalidOperationException("Invalid internal state.")).GetSegmentCount(), Is.EqualTo(1));
            Assert.That(extractedBytes, Is.EqualTo(new byte[] { 2, 3, 4 }));
        });
    }

    [Test]
    public void InvalidConstructionRangesAndUnavailableItemsReturnStableErrors()
    {
        byte[] invalidRangeData = BuildDocument(
            new uint[] { 1, 2 },
            new byte[] { 1, 2, 3, 4 },
            new TestLocation[]
            {
                new TestLocation(
                    1,
                    2,
                    new TestExtent[] { new TestExtent(1, 3, 2) }),
                new TestLocation(
                    2,
                    0,
                    new TestExtent[] { new TestExtent(0, 8, 4) })
            },
            new TestReference[]
            {
                new TestReference(1, new uint[] { 2 })
            });
        MetaParseResult invalidRange = MetaBoxParser.Parse(
            invalidRangeData,
            CreateLimits());
        ItemPayloadResolveResult invalidPayload = (invalidRange.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(1);

        byte[] missingData = BuildDocument(
            new uint[] { 1 },
            System.Array.Empty<byte>(),
            System.Array.Empty<TestLocation>(),
            System.Array.Empty<TestReference>());
        MetaParseResult missing = MetaBoxParser.Parse(missingData, CreateLimits());

        ParsedItem externalItem = CreateDirectItem(3, 0, false);
        ParsedItemPayloadResolver externalResolver = new ParsedItemPayloadResolver(
            new List<ParsedItem> { externalItem },
            1024,
            16);
        ParsedItem protectedItem = CreateDirectItem(4, 1, true);
        ParsedItemPayloadResolver protectedResolver = new ParsedItemPayloadResolver(
            new List<ParsedItem> { protectedItem },
            1024,
            16);

        Assert.Multiple(() =>
        {
            Assert.That(invalidRange.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(invalidPayload.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(
                (missing.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(1).Code,
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(
                externalResolver.Resolve(3).Code,
                Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(
                protectedResolver.Resolve(4).Code,
                Is.EqualTo(AvifResultCode.UnsupportedProtection));
            Assert.That(
                missing.Meta.ResolveItemPayload(99).Code,
                Is.EqualTo(AvifResultCode.NotFound));
        });
    }

    [Test]
    public void ExtractionHonorsTemporaryLimitsAndReadsFailAtomicallyOnTruncation()
    {
        byte[] source = BuildDocument(
            new uint[] { 1 },
            new byte[] { 10, 11, 12, 13, 14 },
            new TestLocation[]
            {
                new TestLocation(
                    1,
                    0,
                    new TestExtent[] { new TestExtent(0, 8, 5) })
            },
            System.Array.Empty<TestReference>());
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxTemporaryBytes = 4;
        MetaParseResult parsed = MetaBoxParser.Parse(source, limits);
        ItemPayloadResolveResult payload = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ResolveItemPayload(1);
        EncodedItemExtractionResult limited = parsed.Meta.ExtractEncodedItem(source, 1);
        byte[] destination = new byte[] { 90, 91, 92, 93, 94 };
        InputReadResult truncated = (payload.Payload ?? throw new InvalidOperationException("Invalid internal state.")).ReadExact(
            source.AsSpan(0, 12),
            0,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(payload.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limited.Data, Is.Null);
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncated.BytesRead, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 90, 91, 92, 93, 94 }));
        });
    }

    private static ParsedItem CreateDirectItem(
        uint itemId,
        ushort protectionIndex,
        bool resolved)
    {
        ParsedItem item = new ParsedItem(
            itemId,
            3,
            false,
            protectionIndex,
            "av01",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
        ParsedItemExtent extent = new ParsedItemExtent(
            0,
            0,
            4,
            resolved,
            0,
            resolved ? 4 : 0);
        ParsedItemLocation location = new ParsedItemLocation(
            itemId,
            0,
            resolved ? (ushort)0 : (ushort)1,
            0,
            true,
            4,
            new List<ParsedItemExtent> { extent });
        if (!resolved)
        {
            ParsedDataReference reference = new ParsedDataReference(
                1,
                ParsedDataReferenceKind.Url,
                "url ",
                false,
                string.Empty,
                "https://example.test/item.bin");
            location = location.WithDataReference(
                reference,
                true,
                4,
                new List<ParsedItemExtent> { extent });
        }

        return item.WithLocation(location);
    }

    private static byte[] BuildDocument(
        uint[] itemIds,
        byte[] mediaData,
        TestLocation[] locations,
        TestReference[] references)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        if (mediaData.Length > 0)
        {
            writer.BeginBox("mdat");
            writer.Write(mediaData);
            writer.EndBox();
        }

        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer, itemIds);
        if (locations.Length > 0)
        {
            WriteItemLocations(writer, locations);
        }

        if (references.Length > 0)
        {
            WriteReferences(writer, references);
        }

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

    private static void WriteItemLocations(
        IsoBoxWriter writer,
        TestLocation[] locations)
    {
        writer.BeginFullBox("iloc", 1, 0);
        writer.Write(new byte[] { 68, 4 });
        WriteUInt16(writer, (ushort)locations.Length);
        int locationIndex = 0;
        while (locationIndex < locations.Length)
        {
            TestLocation location = locations[locationIndex];
            WriteUInt16(writer, (ushort)location.ItemId);
            WriteUInt16(writer, (ushort)location.ConstructionMethod);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, (ushort)location.Extents.Length);
            int extentIndex = 0;
            while (extentIndex < location.Extents.Length)
            {
                TestExtent extent = location.Extents[extentIndex];
                WriteUInt32(writer, (uint)extent.Index);
                WriteUInt32(writer, (uint)extent.Offset);
                WriteUInt32(writer, (uint)extent.Length);
                extentIndex++;
            }

            locationIndex++;
        }

        writer.EndBox();
    }

    private static void WriteReferences(
        IsoBoxWriter writer,
        TestReference[] references)
    {
        writer.BeginFullBox("iref", 0, 0);
        int index = 0;
        while (index < references.Length)
        {
            TestReference reference = references[index];
            writer.BeginBox("iloc");
            WriteUInt16(writer, (ushort)reference.FromItemId);
            WriteUInt16(writer, (ushort)reference.TargetItemIds.Length);
            int targetIndex = 0;
            while (targetIndex < reference.TargetItemIds.Length)
            {
                WriteUInt16(writer, (ushort)reference.TargetItemIds[targetIndex]);
                targetIndex++;
            }

            writer.EndBox();
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
        limits.MaxBoxCount = 256;
        limits.MaxItemCount = 64;
        limits.MaxReferenceCount = 128;
        limits.MaxTemporaryBytes = 65536;
        return limits;
    }

    private class TestExtent
    {
        internal ulong Index;
        internal ulong Offset;
        internal ulong Length;

        internal TestExtent(ulong index, ulong offset, ulong length)
        {
            Index = index;
            Offset = offset;
            Length = length;
        }
    }

    private class TestLocation
    {
        internal uint ItemId;
        internal int ConstructionMethod;
        internal TestExtent[] Extents;

        internal TestLocation(
            uint itemId,
            int constructionMethod,
            TestExtent[] extents)
        {
            ItemId = itemId;
            ConstructionMethod = constructionMethod;
            Extents = extents;
        }
    }

    private class TestReference
    {
        internal uint FromItemId;
        internal uint[] TargetItemIds;

        internal TestReference(uint fromItemId, uint[] targetItemIds)
        {
            FromItemId = fromItemId;
            TargetItemIds = targetItemIds;
        }
    }
}
