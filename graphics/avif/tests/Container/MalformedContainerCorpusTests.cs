using System.Buffers.Binary;

namespace avif.Tests;

public class MalformedContainerCorpusTests
{
    [Test]
    public void EveryTruncatedPrefixOfCanonicalOutputFailsSafely()
    {
        byte[] source = CreateCanonicalContainer(false);
        AvifValidationOptions options = CreateOptions();
        int length = 0;
        while (length < source.Length)
        {
            byte[] prefix = source.AsSpan(0, length).ToArray();
            AvifContainerValidationResult result = AvifContainerValidator.Validate(
                prefix,
                options);
            Assert.That(
                result.Code,
                Is.Not.EqualTo(AvifResultCode.Success),
                "prefix length " + length);
            Assert.That(
                result.Diagnostics.Diagnostics,
                Is.Not.Empty,
                "prefix length " + length);
            length++;
        }
    }

    [Test]
    public void ConflictingAndOverflowingBoxSizesHaveStableFailureCodes()
    {
        byte[] canonical = CreateCanonicalContainer(false);
        byte[] undersized = (byte[])canonical.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(undersized.AsSpan(0, 4), 4);

        byte[] truncated = (byte[])canonical.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(
            truncated.AsSpan(0, 4),
            (uint)truncated.Length + 1);

        byte[] parentOverrun = (byte[])canonical.Clone();
        IsoBoxParseResult parsed = IsoBoxReader.Parse(
            parentOverrun,
            CreateOptions().Limits,
            false);
        IsoBox meta = parsed.Boxes.Single(box => box.Type == "meta");
        IsoBox? handler = MetaBoxParser.FindChild(meta, "hdlr");
        BinaryPrimitives.WriteUInt32BigEndian(
            parentOverrun.AsSpan((int)(handler ?? throw new InvalidOperationException("Invalid internal state.")).Offset, 4),
            (uint)(meta.EndOffset - handler.Offset + 1));

        byte[] overflowingLargeSize = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(overflowingLargeSize.AsSpan(0, 4), 1);
        WriteFourCc(overflowingLargeSize.AsSpan(4, 4), "free");
        BinaryPrimitives.WriteUInt64BigEndian(
            overflowingLargeSize.AsSpan(8, 8),
            ulong.MaxValue);

        AvifResultCode undersizedCode = IsoBoxReader.Parse(
            undersized,
            CreateOptions().Limits,
            false).Code;
        AvifResultCode truncatedCode = IsoBoxReader.Parse(
            truncated,
            CreateOptions().Limits,
            false).Code;
        AvifResultCode overrunCode = IsoBoxReader.Parse(
            parentOverrun,
            CreateOptions().Limits,
            false).Code;
        AvifResultCode overflowCode = IsoBoxReader.Parse(
            overflowingLargeSize,
            CreateOptions().Limits,
            false).Code;

        Assert.Multiple(() =>
        {
            Assert.That(undersizedCode, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(truncatedCode, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(overrunCode, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(overflowCode, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void OverlappingExtentsAndCyclicDependenciesAreRejectedAtTheirBoxes()
    {
        MetaParseResult overlap = MetaBoxParser.Parse(
            BuildOverlappingExtentDocument(),
            CreateOptions().Limits);
        MetaParseResult cycle = MetaBoxParser.Parse(
            BuildCyclicDependencyDocument(),
            CreateOptions().Limits);

        Assert.Multiple(() =>
        {
            Assert.That(overlap.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((overlap.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iloc"));
            Assert.That(cycle.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((cycle.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iref"));
        });
    }

    [TestCase("hdlr")]
    [TestCase("pitm")]
    [TestCase("iloc")]
    [TestCase("iinf")]
    [TestCase("iref")]
    [TestCase("iprp")]
    [TestCase("grpl")]
    [TestCase("idat")]
    public void DuplicateSingletonMetaChildrenAreRejected(string type)
    {
        MetaParseResult result = MetaBoxParser.Parse(
            BuildDuplicateSingletonDocument(type),
            CreateOptions().Limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(result.Meta, Is.Null);
            Assert.That(((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath,
                Is.EqualTo("/meta/" + type));
        });
    }

    [Test]
    public void DeclaredCountBombsFailBeforeEntryParsingOrAllocation()
    {
        byte[][] corpus = new byte[][]
        {
            BuildCountBomb("iinf"),
            BuildCountBomb("iloc"),
            BuildCountBomb("ipma"),
            BuildCountBomb("iref")
        };

        int index = 0;
        while (index < corpus.Length)
        {
            MetaParseResult result = MetaBoxParser.Parse(
                corpus[index],
                CreateOptions().Limits);
            Assert.That(
                result.Code,
                Is.EqualTo(AvifResultCode.LimitExceeded),
                "corpus index " + index);
            Assert.That(
                ((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource),
                "corpus index " + index);
            index++;
        }
    }

    [Test]
    public void IntegerOverflowInItemRangesFailsBeforeRangeResolution()
    {
        MetaParseResult result = MetaBoxParser.Parse(
            BuildOverflowingItemRangeDocument(),
            CreateOptions().Limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(result.Meta, Is.Null);
            Assert.That(((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath,
                Is.EqualTo("/meta/iloc"));
            Assert.That((result.Boxes.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void RedundantAv1ConfigurationAndPixelInformationMustAgree()
    {
        byte[] source = CreateCanonicalContainer(false);
        IsoBoxParseResult boxes = IsoBoxReader.Parse(
            source,
            CreateOptions().Limits,
            false);
        IsoBox meta = boxes.Boxes.Single(box => box.Type == "meta");
        IsoBox? properties = MetaBoxParser.FindChild(meta, "iprp");
        IsoBox propertyContainer = (properties ?? throw new InvalidOperationException("Invalid internal state.")).Children.Single(box => box.Type == "ipco");
        IsoBox pixels = propertyContainer.Children.Single(box => box.Type == "pixi");
        source[(int)pixels.EndOffset - 1] = 10;

        MetaParseResult result = MetaBoxParser.Parse(
            source,
            CreateOptions().Limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(result.Meta, Is.Null);
            Assert.That(((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath,
                Is.EqualTo("/meta/iprp"));
        });
    }

    [Test]
    public void RecursionAndAllocationBombsStopAtConfiguredLimits()
    {
        AvifSecurityLimits depthLimits = CreateOptions().Limits;
        depthLimits.MaxBoxDepth = 4;
        IsoBoxParseResult depth = IsoBoxReader.Parse(
            BuildNestedBoxBomb(12),
            depthLimits,
            false);

        byte[] dimensionsSource = CreateCanonicalContainer(false);
        IsoBoxParseResult dimensionsBoxes = IsoBoxReader.Parse(
            dimensionsSource,
            CreateOptions().Limits,
            false);
        IsoBox dimensionsMeta = dimensionsBoxes.Boxes.Single(box => box.Type == "meta");
        IsoBox? dimensionsProperties = MetaBoxParser.FindChild(dimensionsMeta, "iprp");
        IsoBox dimensionsContainer = (dimensionsProperties ?? throw new InvalidOperationException("Invalid internal state.")).Children.Single(
            box => box.Type == "ipco");
        IsoBox extents = dimensionsContainer.Children.Single(box => box.Type == "ispe");
        BinaryPrimitives.WriteUInt32BigEndian(
            dimensionsSource.AsSpan((int)extents.PayloadOffset + 4, 4),
            uint.MaxValue);
        MetaParseResult dimensions = MetaBoxParser.Parse(
            dimensionsSource,
            CreateOptions().Limits);

        Assert.Multiple(() =>
        {
            Assert.That(depth.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(depth.Resources.BoxDepth.Current, Is.Zero);
            Assert.That((depth.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
            Assert.That(dimensions.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(dimensions.Meta, Is.Null);
            Assert.That(((dimensions.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath,
                Is.EqualTo("/meta/iprp"));
            Assert.That((dimensions.Boxes.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void UnknownEssentialPropertyRemainsFatalAfterByteLevelMutation()
    {
        byte[] source = CreateCanonicalContainer(true);
        IsoBoxParseResult boxes = IsoBoxReader.Parse(
            source,
            CreateOptions().Limits,
            false);
        IsoBox meta = boxes.Boxes.Single(box => box.Type == "meta");
        IsoBox? properties = MetaBoxParser.FindChild(meta, "iprp");
        IsoBox associations = (properties ?? throw new InvalidOperationException("Invalid internal state.")).Children.Single(box => box.Type == "ipma");
        IsoFullBox fullBox = IsoFullBox.Parse(source, associations);
        int unknownAssociationOffset = (int)fullBox.PayloadOffset + 10;
        source[unknownAssociationOffset] |= 128;

        AvifContainerValidationResult result = AvifContainerValidator.Validate(
            source,
            CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Code,
                Is.EqualTo(AvifResultCode.UnsupportedEssentialProperty));
            Assert.That((((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyAssociation(3) ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(),
                Is.True);
            Assert.That(result.Diagnostics.Diagnostics.Any(diagnostic =>
                diagnostic.Message.Contains(
                    "unknown essential",
                    StringComparison.OrdinalIgnoreCase)),
                Is.True);
        });
    }

    private static byte[] BuildOverlappingExtentDocument()
    {
        return BuildMeta(writer =>
        {
            WriteHandler(writer);
            WriteItemInfo(writer, new uint[] { 1 });
            writer.BeginFullBox("iloc", 1, 0);
            writer.Write(new byte[] { 68, 0 });
            WriteUInt16(writer, 1);
            WriteUInt16(writer, 1);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 2);
            WriteUInt32(writer, 0);
            WriteUInt32(writer, 4);
            WriteUInt32(writer, 2);
            WriteUInt32(writer, 2);
            writer.EndBox();
        });
    }

    private static byte[] BuildCyclicDependencyDocument()
    {
        return BuildMeta(writer =>
        {
            WriteHandler(writer);
            WriteItemInfo(writer, new uint[] { 1, 2 });
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, 1, 2);
            WriteReference(writer, 2, 1);
            writer.EndBox();
        });
    }

    private static byte[] BuildOverflowingItemRangeDocument()
    {
        return BuildMeta(writer =>
        {
            WriteHandler(writer);
            WriteItemInfo(writer, new uint[] { 1 });
            writer.BeginFullBox("iloc", 1, 0);
            writer.Write(new byte[] { 136, 128 });
            WriteUInt16(writer, 1);
            WriteUInt16(writer, 1);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt64(writer, ulong.MaxValue);
            WriteUInt16(writer, 1);
            WriteUInt64(writer, 1);
            WriteUInt64(writer, 1);
            writer.EndBox();
        });
    }

    private static byte[] BuildNestedBoxBomb(int depth)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 131072);
        int index = 0;
        while (index < depth)
        {
            writer.BeginBox("moov");
            index++;
        }

        while (index > 0)
        {
            writer.EndBox();
            index--;
        }

        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static byte[] BuildDuplicateSingletonDocument(string type)
    {
        return BuildMeta(writer =>
        {
            if (type != "hdlr")
            {
                WriteHandler(writer);
            }

            WriteSingleton(writer, type);
            WriteSingleton(writer, type);
        });
    }

    private static byte[] BuildCountBomb(string type)
    {
        return BuildMeta(writer =>
        {
            WriteHandler(writer);
            if (type == "iinf")
            {
                writer.BeginFullBox("iinf", 1, 0);
                WriteUInt32(writer, uint.MaxValue);
                writer.EndBox();
            }
            else if (type == "iloc")
            {
                writer.BeginFullBox("iloc", 2, 0);
                writer.Write(new byte[] { 0, 0 });
                WriteUInt32(writer, uint.MaxValue);
                writer.EndBox();
            }
            else if (type == "ipma")
            {
                writer.BeginBox("iprp");
                writer.BeginBox("ipco");
                writer.EndBox();
                writer.BeginFullBox("ipma", 1, 0);
                WriteUInt32(writer, uint.MaxValue);
                writer.EndBox();
                writer.EndBox();
            }
            else
            {
                writer.BeginFullBox("iref", 0, 0);
                writer.BeginBox("dimg");
                WriteUInt16(writer, 1);
                WriteUInt16(writer, ushort.MaxValue);
                writer.EndBox();
                writer.EndBox();
            }
        });
    }

    private static void WriteSingleton(IsoBoxWriter writer, string type)
    {
        if (type == "hdlr")
        {
            WriteHandler(writer);
        }
        else if (type == "pitm")
        {
            writer.BeginFullBox("pitm", 0, 0);
            WriteUInt16(writer, 1);
            writer.EndBox();
        }
        else if (type == "iloc")
        {
            writer.BeginFullBox("iloc", 1, 0);
            writer.Write(new byte[] { 0, 0 });
            WriteUInt16(writer, 0);
            writer.EndBox();
        }
        else if (type == "iinf")
        {
            writer.BeginFullBox("iinf", 1, 0);
            WriteUInt32(writer, 0);
            writer.EndBox();
        }
        else if (type == "iref")
        {
            writer.BeginFullBox("iref", 0, 0);
            writer.EndBox();
        }
        else if (type == "iprp")
        {
            writer.BeginBox("iprp");
            writer.BeginBox("ipco");
            writer.EndBox();
            writer.EndBox();
        }
        else if (type == "grpl")
        {
            writer.BeginBox("grpl");
            writer.EndBox();
        }
        else
        {
            writer.BeginBox("idat");
            writer.EndBox();
        }
    }

    private static byte[] BuildMeta(Action<IsoBoxWriter> writeChildren)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 131072);
        writer.BeginFullBox("meta", 0, 0);
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
        WriteUInt32(writer, (uint)itemIds.Length);
        int index = 0;
        while (index < itemIds.Length)
        {
            writer.BeginFullBox("infe", 3, 0);
            WriteUInt32(writer, itemIds[index]);
            WriteUInt16(writer, 0);
            WriteFourCc(writer, "av01");
            writer.Write(new byte[] { 0 });
            writer.EndBox();
            index++;
        }

        writer.EndBox();
    }

    private static void WriteReference(IsoBoxWriter writer, int from, int to)
    {
        writer.BeginBox("dimg");
        WriteUInt16(writer, from);
        WriteUInt16(writer, 1);
        WriteUInt16(writer, to);
        writer.EndBox();
    }

    private static byte[] CreateCanonicalContainer(bool includeUnknownProperty)
    {
        CanonicalItemContainerOptions writerOptions = new CanonicalItemContainerOptions();
        writerOptions.MaximumOutputBytes = 65536;
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            1,
            "av01",
            "image",
            false,
            new byte[] { 1 });
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("av1C", new byte[] { 129, 13, 12, 0 }),
            true));

        byte[] extents = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(extents.AsSpan(4, 4), 32);
        BinaryPrimitives.WriteUInt32BigEndian(extents.AsSpan(8, 4), 24);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("ispe", extents),
            false));
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("pixi", new byte[] { 0, 0, 0, 0, 3, 8, 8, 8 }),
            false));
        if (includeUnknownProperty)
        {
            item.Properties.Add(new CanonicalItemPropertyAssociation(
                BuildRawProperty("zzzz", new byte[] { 1, 2, 3 }),
                false));
        }

        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            1,
            new List<CanonicalItemPayload> { item },
            writerOptions);
        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
        return written.Data;
    }

    private static byte[] BuildRawProperty(string type, byte[] payload)
    {
        byte[] raw = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(0, 4), (uint)raw.Length);
        WriteFourCc(raw.AsSpan(4, 4), type);
        payload.CopyTo(raw, 8);
        return raw;
    }

    private static AvifValidationOptions CreateOptions()
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.ValidateEncodedPayload = false;
        options.StopAfterFirstError = true;
        options.Limits.MaxSourceBytes = 262144;
        options.Limits.MaxBoxDepth = 16;
        options.Limits.MaxBoxCount = 512;
        options.Limits.MaxItemCount = 64;
        options.Limits.MaxPropertyCount = 256;
        options.Limits.MaxReferenceCount = 256;
        options.Limits.MaxTemporaryBytes = 131072;
        return options;
    }

    private static void WriteUInt16(IsoBoxWriter writer, int value)
    {
        byte[] data = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(data, (ushort)value);
        writer.Write(data);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, value);
        writer.Write(data);
    }

    private static void WriteUInt64(IsoBoxWriter writer, ulong value)
    {
        byte[] data = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(data, value);
        writer.Write(data);
    }

    private static void WriteFourCc(IsoBoxWriter writer, string type)
    {
        byte[] data = new byte[4];
        WriteFourCc(data, type);
        writer.Write(data);
    }

    private static void WriteFourCc(Span<byte> destination, string type)
    {
        destination[0] = (byte)type[0];
        destination[1] = (byte)type[1];
        destination[2] = (byte)type[2];
        destination[3] = (byte)type[3];
    }
}
