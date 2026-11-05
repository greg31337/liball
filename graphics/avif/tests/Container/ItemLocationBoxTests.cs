using System.Buffers.Binary;

namespace avif.Tests;

public class ItemLocationBoxTests
{
    [Test]
    public void VersionZeroResolvesMultipleFileExtents()
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                0,
                4,
                4,
                4,
                0,
                new TestLocation[]
                {
                    new TestLocation(
                        1,
                        0,
                        0,
                        4,
                        new TestExtent[]
                        {
                            new TestExtent(0, 0, 4),
                            new TestExtent(0, 4, 4)
                        })
                },
                false);
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemLocation? location = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((location ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod(), Is.Zero);
            Assert.That(location.GetBaseOffset(), Is.EqualTo(4));
            Assert.That(location.GetExtentCount(), Is.EqualTo(2));
            Assert.That(location.HasKnownTotalLength(), Is.True);
            Assert.That(location.GetTotalLength(), Is.EqualTo(8));
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).HasResolvedRange(), Is.True);
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(), Is.EqualTo(4));
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedLength(), Is.EqualTo(4));
            Assert.That((location.GetExtent(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(), Is.EqualTo(8));
        });
    }

    [Test]
    public void VersionOneResolvesIdatOffsetsAndImplicitLength()
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                4,
                4,
                0,
                4,
                new TestLocation[]
                {
                    new TestLocation(
                        1,
                        1,
                        0,
                        0,
                        new TestExtent[]
                        {
                            new TestExtent(0, 1, 3),
                            new TestExtent(0, 10, 0)
                        })
                },
                false);
            WriteIdat(writer, 20);
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        IsoBox idat = FindChild((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes[0], "idat");
        ParsedItemLocation? location = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((location ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod(), Is.EqualTo(1));
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(), Is.EqualTo(idat.PayloadOffset + 1));
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedLength(), Is.EqualTo(3));
            Assert.That((location.GetExtent(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(), Is.EqualTo(idat.PayloadOffset + 10));
            Assert.That((location.GetExtent(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedLength(), Is.EqualTo(10));
            Assert.That(location.GetTotalLength(), Is.EqualTo(13));
        });
    }

    [Test]
    public void VersionTwoPreservesEightByteItemReferenceCoordinates()
    {
        byte[] source = BuildDocument(new uint[] { 70000 }, writer =>
        {
            WriteItemLocation(
                writer,
                2,
                8,
                8,
                8,
                8,
                new TestLocation[]
                {
                    new TestLocation(
                        70000,
                        2,
                        0,
                        10,
                        new TestExtent[] { new TestExtent(2, 20, 30) })
                },
                false);
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemLocation? location = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(70000) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();
        ParsedItemExtent? extent = (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(location.GetItemId(), Is.EqualTo(70000));
            Assert.That(location.GetConstructionMethod(), Is.EqualTo(2));
            Assert.That(location.GetBaseOffset(), Is.EqualTo(10));
            Assert.That(location.GetTotalLength(), Is.EqualTo(30));
            Assert.That((extent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex(), Is.EqualTo(2));
            Assert.That(extent.GetExtentOffset(), Is.EqualTo(20));
            Assert.That(extent.GetExtentLength(), Is.EqualTo(30));
            Assert.That(extent.HasResolvedRange(), Is.False);
        });
    }

    [Test]
    public void ZeroWidthFieldsUseItemReferenceIndexOneAndUnknownLength()
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                0,
                0,
                0,
                0,
                new TestLocation[]
                {
                    new TestLocation(
                        1,
                        2,
                        0,
                        0,
                        new TestExtent[] { new TestExtent(0, 0, 0) })
                },
                false);
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemLocation? location = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex(), Is.EqualTo(1));
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentOffset(), Is.Zero);
            Assert.That((location.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentLength(), Is.Zero);
            Assert.That(location.HasKnownTotalLength(), Is.False);
        });
    }

    [Test]
    public void ParserRejectsIllegalFieldWidthsAndVersionZeroIndexSize()
    {
        MetaParseResult illegalWidth = MetaBoxParser.Parse(
            BuildDocument(System.Array.Empty<uint>(), writer =>
            {
                WriteItemLocation(
                    writer,
                    0,
                    1,
                    0,
                    0,
                    0,
                    System.Array.Empty<TestLocation>(),
                    false);
            }),
            CreateLimits());
        MetaParseResult reservedIndex = MetaBoxParser.Parse(
            BuildDocument(System.Array.Empty<uint>(), writer =>
            {
                WriteItemLocation(
                    writer,
                    0,
                    0,
                    0,
                    0,
                    4,
                    System.Array.Empty<TestLocation>(),
                    false);
            }),
            CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(illegalWidth.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(reservedIndex.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void ParserValidatesConstructionMethodsDataReferencesAndIndices()
    {
        MetaParseResult unsupportedMethod = ParseSingleLocation(
            new TestLocation(1, 3, 0, 0, new TestExtent[] { new TestExtent(0, 0, 1) }),
            4,
            false);
        MetaParseResult reservedMethodBits = ParseSingleLocation(
            new TestLocation(1, 4097, 0, 0, new TestExtent[] { new TestExtent(0, 0, 1) }),
            4,
            true);
        MetaParseResult idatExternal = ParseSingleLocation(
            new TestLocation(1, 1, 1, 0, new TestExtent[] { new TestExtent(0, 0, 1) }),
            4,
            true);
        MetaParseResult missingIdat = ParseSingleLocation(
            new TestLocation(1, 1, 0, 0, new TestExtent[] { new TestExtent(0, 0, 1) }),
            4,
            false);
        MetaParseResult fileIndex = ParseSingleLocation(
            new TestLocation(1, 0, 0, 0, new TestExtent[] { new TestExtent(1, 0, 1) }),
            4,
            false);
        MetaParseResult zeroItemIndex = ParseSingleLocation(
            new TestLocation(1, 2, 0, 0, new TestExtent[] { new TestExtent(0, 0, 1) }),
            4,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(unsupportedMethod.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(reservedMethodBits.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(idatExternal.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(missingIdat.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(fileIndex.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroItemIndex.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void ParserRejectsArithmeticOverflowAndOutOfBoundsFileRanges()
    {
        MetaParseResult overflow = ParseSingleLocation(
            new TestLocation(
                1,
                2,
                0,
                ulong.MaxValue,
                new TestExtent[] { new TestExtent(1, 1, 1) }),
            8,
            false);
        MetaParseResult outOfBounds = ParseSingleLocation(
            new TestLocation(
                1,
                0,
                0,
                10000,
                new TestExtent[] { new TestExtent(0, 0, 1) }),
            8,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(overflow.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(outOfBounds.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void OverlapChecksRejectSameSourceRangesButAllowDifferentItemReferences()
    {
        MetaParseResult fileOverlap = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                4,
                4,
                0,
                4,
                new TestLocation[]
                {
                    new TestLocation(
                        1,
                        0,
                        0,
                        0,
                        new TestExtent[]
                        {
                            new TestExtent(0, 10, 10),
                            new TestExtent(0, 15, 2)
                        })
                },
                false);
        }), CreateLimits());
        MetaParseResult separateReferences = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                4,
                4,
                0,
                4,
                new TestLocation[]
                {
                    new TestLocation(
                        1,
                        2,
                        0,
                        0,
                        new TestExtent[]
                        {
                            new TestExtent(1, 0, 10),
                            new TestExtent(2, 0, 10)
                        })
                },
                false);
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(fileOverlap.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(separateReferences.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ItemAndExtentCountsHonorConfiguredLimits()
    {
        byte[] itemSource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                0,
                0,
                0,
                0,
                new TestLocation[]
                {
                    new TestLocation(1, 0, 0, 0, System.Array.Empty<TestExtent>()),
                    new TestLocation(2, 0, 0, 0, System.Array.Empty<TestExtent>())
                },
                false);
        });
        AvifSecurityLimits itemLimits = CreateLimits();
        itemLimits.MaxItemCount = 1;
        MetaParseResult itemResult = MetaBoxParser.Parse(itemSource, itemLimits);

        byte[] extentSource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                4,
                4,
                0,
                0,
                new TestLocation[]
                {
                    new TestLocation(
                        1,
                        0,
                        0,
                        0,
                        new TestExtent[]
                        {
                            new TestExtent(0, 0, 1),
                            new TestExtent(0, 1, 1)
                        })
                },
                false);
        });
        AvifSecurityLimits extentLimits = CreateLimits();
        extentLimits.MaxReferenceCount = 1;
        MetaParseResult extentResult = MetaBoxParser.Parse(extentSource, extentLimits);

        Assert.Multiple(() =>
        {
            Assert.That(itemResult.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(extentResult.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(
                ((extentResult.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void ParserRejectsDuplicateUndeclaredAndTrailingLocationData()
    {
        MetaParseResult duplicate = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                0,
                0,
                0,
                0,
                new TestLocation[]
                {
                    new TestLocation(1, 0, 0, 0, System.Array.Empty<TestExtent>()),
                    new TestLocation(1, 0, 0, 0, System.Array.Empty<TestExtent>())
                },
                false);
        }), CreateLimits());
        MetaParseResult undeclared = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                0,
                0,
                0,
                0,
                new TestLocation[]
                {
                    new TestLocation(2, 0, 0, 0, System.Array.Empty<TestExtent>())
                },
                false);
        }), CreateLimits());
        MetaParseResult trailing = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                0,
                0,
                0,
                0,
                new TestLocation[]
                {
                    new TestLocation(1, 0, 0, 0, System.Array.Empty<TestExtent>())
                },
                true);
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(undeclared.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(((undeclared.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iloc"));
        });
    }

    private static MetaParseResult ParseSingleLocation(
        TestLocation location,
        int fieldSize,
        bool includeIdat)
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WriteItemLocation(
                writer,
                1,
                fieldSize,
                fieldSize,
                fieldSize,
                fieldSize,
                new TestLocation[] { location },
                false);
            if (includeIdat)
            {
                WriteIdat(writer, 8);
            }
        });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static byte[] BuildDocument(
        uint[] itemIds,
        Action<IsoBoxWriter> writeChildren)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 16384);
        Assert.That(writer.BeginFullBox("meta", 0, 0), Is.EqualTo(AvifResultCode.Success));
        WriteHandler(writer);
        WriteItemInfo(writer, itemIds);
        writeChildren(writer);
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
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

    private static void WriteItemLocation(
        IsoBoxWriter writer,
        int version,
        int offsetSize,
        int lengthSize,
        int baseOffsetSize,
        int indexSize,
        TestLocation[] locations,
        bool addTrailingByte)
    {
        writer.BeginFullBox("iloc", version, 0);
        List<byte> payload = new List<byte>();
        payload.Add((byte)(offsetSize << 4 | lengthSize));
        payload.Add((byte)(baseOffsetSize << 4 | indexSize));
        if (version < 2)
        {
            AddUInt16(payload, (ushort)locations.Length);
        }
        else
        {
            AddUInt32(payload, (uint)locations.Length);
        }

        int locationIndex = 0;
        while (locationIndex < locations.Length)
        {
            TestLocation location = locations[locationIndex];
            if (version < 2)
            {
                AddUInt16(payload, (ushort)location.ItemId);
            }
            else
            {
                AddUInt32(payload, location.ItemId);
            }

            if (version == 1 || version == 2)
            {
                AddUInt16(payload, location.MethodField);
            }

            AddUInt16(payload, location.DataReferenceIndex);
            AddSized(payload, baseOffsetSize, location.BaseOffset);
            AddUInt16(payload, (ushort)location.Extents.Length);
            int extentIndex = 0;
            while (extentIndex < location.Extents.Length)
            {
                TestExtent extent = location.Extents[extentIndex];
                if ((version == 1 || version == 2) && indexSize > 0)
                {
                    AddSized(payload, indexSize, extent.Index);
                }

                AddSized(payload, offsetSize, extent.Offset);
                AddSized(payload, lengthSize, extent.Length);
                extentIndex++;
            }

            locationIndex++;
        }

        if (addTrailingByte)
        {
            payload.Add(255);
        }

        writer.Write(payload.ToArray());
        writer.EndBox();
    }

    private static void WriteIdat(IsoBoxWriter writer, int payloadSize)
    {
        writer.BeginBox("idat");
        writer.Write(new byte[payloadSize]);
        writer.EndBox();
    }

    private static void AddSized(List<byte> destination, int size, ulong value)
    {
        if (size == 0)
        {
            return;
        }

        if (size == 4)
        {
            AddUInt32(destination, (uint)value);
        }
        else if (size == 8)
        {
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            destination.AddRange(bytes);
        }
    }

    private static void AddUInt16(List<byte> destination, ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        destination.AddRange(bytes);
    }

    private static void AddUInt32(List<byte> destination, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        destination.AddRange(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    private static IsoBox FindChild(IsoBox parent, string type)
    {
        return parent.Children.Single(child => child.Type == type);
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 32768;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 128;
        limits.MaxItemCount = 32;
        limits.MaxReferenceCount = 128;
        return limits;
    }

    private class TestLocation
    {
        public uint ItemId;
        public ushort MethodField;
        public ushort DataReferenceIndex;
        public ulong BaseOffset;
        public TestExtent[] Extents;

        public TestLocation(
            uint itemId,
            ushort methodField,
            ushort dataReferenceIndex,
            ulong baseOffset,
            TestExtent[] extents)
        {
            ItemId = itemId;
            MethodField = methodField;
            DataReferenceIndex = dataReferenceIndex;
            BaseOffset = baseOffset;
            Extents = extents;
        }
    }

    private class TestExtent
    {
        public ulong Index;
        public ulong Offset;
        public ulong Length;

        public TestExtent(ulong index, ulong offset, ulong length)
        {
            Index = index;
            Offset = offset;
            Length = length;
        }
    }
}
