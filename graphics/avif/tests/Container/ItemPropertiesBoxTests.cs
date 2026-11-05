using System.Buffers.Binary;

namespace avif.Tests;

public class ItemPropertiesBoxTests
{
    [Test]
    public void ParserPreservesDeduplicatesAndOrdersOpaqueProperties()
    {
        byte[] userType = new byte[16];
        int userIndex = 0;
        while (userIndex < userType.Length)
        {
            userType[userIndex] = (byte)(userIndex + 1);
            userIndex++;
        }

        byte[] source = BuildDocument(new uint[] { 1, 70000 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1, 2, 3 });
                WriteProperty(properties, "zzzz", new byte[] { 1, 2, 3 });
                properties.BeginBox("free");
                properties.EndBox();
                WriteProperty(properties, "abcd", new byte[] { 4, 5 });
                WriteProperty(properties, "meta", new byte[] { 9, 8, 7 });
                WriteUuidProperty(properties, userType, new byte[] { 6, 7 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            1,
                            new TestAssociation[]
                            {
                                new TestAssociation(2, true),
                                new TestAssociation(3, false),
                                new TestAssociation(1, true),
                                new TestAssociation(4, false),
                                new TestAssociation(5, true)
                            })
                    },
                    false);
                WriteAssociations(
                    associations,
                    1,
                    1,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            70000,
                            new TestAssociation[] { new TestAssociation(4, true) })
                    },
                    false);
            });
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItem? item = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        ParsedItemProperty? first = result.Meta.GetProperty(0);
        ParsedItemProperty? duplicate = result.Meta.GetProperty(1);
        ParsedItemProperty? opaqueContainerType = result.Meta.GetProperty(3);
        ParsedItemProperty? uuid = result.Meta.GetProperty(4);

        source.AsSpan().Clear();
        byte[] firstRaw = (first ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();
        byte[] uuidRaw = (uuid ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.GetPropertyCount(), Is.EqualTo(5));
            Assert.That(object.ReferenceEquals(first, duplicate), Is.True);
            Assert.That((duplicate ?? throw new InvalidOperationException("Invalid internal state.")).GetCanonicalIndex(), Is.EqualTo(1));
            Assert.That(first.GetBoxType(), Is.EqualTo("zzzz"));
            Assert.That(firstRaw[8..], Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That((opaqueContainerType ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("meta"));
            Assert.That(opaqueContainerType.GetRawData()[8..], Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(uuid.IsUuid(), Is.True);
            Assert.That(uuid.GetUserType(), Is.EqualTo(userType));
            Assert.That(uuidRaw[^2..], Is.EqualTo(new byte[] { 6, 7 }));
            Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(4));
            Assert.That((item.GetPropertyAssociation(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyIndex(), Is.EqualTo(2));
            Assert.That((item.GetPropertyAssociation(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.True);
            Assert.That((item.GetPropertyAssociation(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyIndex(), Is.EqualTo(3));
            Assert.That((item.GetPropertyAssociation(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyIndex(), Is.EqualTo(4));
            Assert.That((item.GetPropertyAssociation(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyIndex(), Is.EqualTo(5));
            Assert.That(((result.Meta.FindItem(70000) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyAssociation(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.True);
        });
    }

    [Test]
    public void VersionOneAssociationsSupportFifteenBitPropertyIndices()
    {
        byte[] source = BuildDocument(new uint[] { 70000 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                int index = 0;
                while (index < 130)
                {
                    byte[] payload = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(payload, index);
                    WriteProperty(properties, "zzzz", payload);
                    index++;
                }
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    1,
                    1,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            70000,
                            new TestAssociation[] { new TestAssociation(130, true) })
                    },
                    false);
            });
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedPropertyAssociation? association = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(70000) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetPropertyAssociation(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.GetPropertyCount(), Is.EqualTo(130));
            Assert.That((association ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyIndex(), Is.EqualTo(130));
            Assert.That(association.IsEssential(), Is.True);
            Assert.That(association.GetProperty().GetCanonicalIndex(), Is.EqualTo(130));
        });
    }

    [Test]
    public void ZeroIndexIsIgnoredUnlessEssentialAndConflictingDuplicatesFail()
    {
        MetaParseResult ignoredZero = ParsePropertyDocument(
            new byte[][] { new byte[] { 1 } },
            new TestAssociation[]
            {
                new TestAssociation(0, false),
                new TestAssociation(1, false)
            });
        MetaParseResult essentialZero = ParsePropertyDocument(
            new byte[][] { new byte[] { 1 } },
            new TestAssociation[] { new TestAssociation(0, true) });

        byte[] conflictSource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            1,
                            new TestAssociation[]
                            {
                                new TestAssociation(1, false),
                                new TestAssociation(2, true)
                            })
                    },
                    false);
            });
        });
        MetaParseResult conflict = MetaBoxParser.Parse(conflictSource, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(ignoredZero.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((ignoredZero.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(1));
            Assert.That(essentialZero.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(conflict.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void AssociationParserRejectsInvalidIndicesAndTrailingData()
    {
        MetaParseResult invalidIndex = ParsePropertyDocument(
            new byte[][] { new byte[] { 1 } },
            new TestAssociation[] { new TestAssociation(2, false) });
        byte[] trailingSource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            1,
                            new TestAssociation[] { new TestAssociation(1, false) })
                    },
                    true);
            });
        });
        MetaParseResult trailing = MetaBoxParser.Parse(trailingSource, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(invalidIndex.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void ItemIdsMustBeIncreasingAndUniqueAcrossAssociationBoxes()
    {
        byte[] unorderedSource = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(2, System.Array.Empty<TestAssociation>()),
                        new TestAssociationEntry(1, System.Array.Empty<TestAssociation>())
                    },
                    false);
            });
        });
        MetaParseResult unordered = MetaBoxParser.Parse(unorderedSource, CreateLimits());

        byte[] duplicateSource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(1, System.Array.Empty<TestAssociation>())
                    },
                    false);
                WriteAssociations(
                    associations,
                    1,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(1, System.Array.Empty<TestAssociation>())
                    },
                    false);
            });
        });
        MetaParseResult duplicate = MetaBoxParser.Parse(duplicateSource, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(unordered.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void AssociationBoxesRequireUniqueVersionAndFlagPairs()
    {
        byte[] source = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(1, System.Array.Empty<TestAssociation>())
                    },
                    false);
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(2, System.Array.Empty<TestAssociation>())
                    },
                    false);
            });
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
    }

    [Test]
    public void PropertyContainerMustPrecedeAssociationsAndRejectUnexpectedChildren()
    {
        MetaParseResult reversed = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            writer.BeginBox("iprp");
            WriteAssociations(
                writer,
                0,
                0,
                System.Array.Empty<TestAssociationEntry>(),
                false);
            writer.BeginBox("ipco");
            writer.EndBox();
            writer.EndBox();
        }), CreateLimits());
        MetaParseResult duplicateContainer = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            writer.BeginBox("iprp");
            writer.BeginBox("ipco");
            writer.EndBox();
            writer.BeginBox("ipco");
            writer.EndBox();
            writer.EndBox();
        }), CreateLimits());
        MetaParseResult unexpected = MetaBoxParser.Parse(BuildDocument(new uint[] { 1 }, writer =>
        {
            writer.BeginBox("iprp");
            writer.BeginBox("ipco");
            writer.EndBox();
            writer.BeginBox("abcd");
            writer.EndBox();
            writer.EndBox();
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(reversed.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(duplicateContainer.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(unexpected.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void AssociationParserRejectsUnsupportedVersionsAndFlags()
    {
        MetaParseResult version = ParseCustomAssociation(2, 0);
        MetaParseResult flags = ParseCustomAssociation(0, 2);

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void PropertyAssociationAndPreservationLimitsAreEnforced()
    {
        byte[] propertySource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "aaaa", new byte[] { 1 });
                WriteProperty(properties, "bbbb", new byte[] { 2 });
            }, associations => { });
        });
        AvifSecurityLimits propertyLimits = CreateLimits();
        propertyLimits.MaxPropertyCount = 1;
        MetaParseResult propertyResult = MetaBoxParser.Parse(propertySource, propertyLimits);

        AvifSecurityLimits byteLimits = CreateLimits();
        byteLimits.MaxTemporaryBytes = 8;
        MetaParseResult byteResult = MetaBoxParser.Parse(propertySource, byteLimits);

        byte[] associationSource = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "aaaa", new byte[] { 1 });
                WriteProperty(properties, "bbbb", new byte[] { 2 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            1,
                            new TestAssociation[]
                            {
                                new TestAssociation(1, false),
                                new TestAssociation(2, false)
                            })
                    },
                    false);
            });
        });
        AvifSecurityLimits associationLimits = CreateLimits();
        associationLimits.MaxReferenceCount = 1;
        MetaParseResult associationResult = MetaBoxParser.Parse(
            associationSource,
            associationLimits);

        Assert.Multiple(() =>
        {
            Assert.That(propertyResult.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(byteResult.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(associationResult.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(
                ((associationResult.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void AssociationsCannotIdentifyUndeclaredItems()
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(
                            2,
                            new TestAssociation[] { new TestAssociation(1, false) })
                    },
                    false);
            });
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iprp"));
        });
    }

    private static MetaParseResult ParsePropertyDocument(
        byte[][] propertyPayloads,
        TestAssociation[] associations)
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                int index = 0;
                while (index < propertyPayloads.Length)
                {
                    WriteProperty(properties, "zzzz", propertyPayloads[index]);
                    index++;
                }
            }, associationWriter =>
            {
                WriteAssociations(
                    associationWriter,
                    0,
                    0,
                    new TestAssociationEntry[]
                    {
                        new TestAssociationEntry(1, associations)
                    },
                    false);
            });
        });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static MetaParseResult ParseCustomAssociation(int version, int flags)
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            WritePropertySet(writer, properties =>
            {
                WriteProperty(properties, "zzzz", new byte[] { 1 });
            }, associations =>
            {
                WriteAssociations(
                    associations,
                    version,
                    flags,
                    System.Array.Empty<TestAssociationEntry>(),
                    false);
            });
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

    private static void WritePropertySet(
        IsoBoxWriter writer,
        Action<IsoBoxWriter> writeProperties,
        Action<IsoBoxWriter> writeAssociations)
    {
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        writeProperties(writer);
        writer.EndBox();
        writeAssociations(writer);
        writer.EndBox();
    }

    private static void WriteProperty(IsoBoxWriter writer, string type, byte[] payload)
    {
        writer.BeginBox(type);
        writer.Write(payload);
        writer.EndBox();
    }

    private static void WriteUuidProperty(
        IsoBoxWriter writer,
        byte[] userType,
        byte[] payload)
    {
        writer.BeginUuidBox(userType);
        writer.Write(payload);
        writer.EndBox();
    }

    private static void WriteAssociations(
        IsoBoxWriter writer,
        int version,
        int flags,
        TestAssociationEntry[] entries,
        bool trailingByte)
    {
        writer.BeginFullBox("ipma", version, flags);
        List<byte> payload = new List<byte>();
        AddUInt32(payload, (uint)entries.Length);
        int entryIndex = 0;
        while (entryIndex < entries.Length)
        {
            TestAssociationEntry entry = entries[entryIndex];
            if (version == 0)
            {
                AddUInt16(payload, (ushort)entry.ItemId);
            }
            else
            {
                AddUInt32(payload, entry.ItemId);
            }

            payload.Add((byte)entry.Associations.Length);
            int associationIndex = 0;
            while (associationIndex < entry.Associations.Length)
            {
                TestAssociation association = entry.Associations[associationIndex];
                if (flags == 1)
                {
                    ushort value = (ushort)association.PropertyIndex;
                    if (association.Essential)
                    {
                        value |= 32768;
                    }

                    AddUInt16(payload, value);
                }
                else
                {
                    byte value = (byte)association.PropertyIndex;
                    if (association.Essential)
                    {
                        value |= 128;
                    }

                    payload.Add(value);
                }

                associationIndex++;
            }

            entryIndex++;
        }

        if (trailingByte)
        {
            payload.Add(255);
        }

        writer.Write(payload.ToArray());
        writer.EndBox();
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

    private class TestAssociationEntry
    {
        public uint ItemId;
        public TestAssociation[] Associations;

        public TestAssociationEntry(uint itemId, TestAssociation[] associations)
        {
            ItemId = itemId;
            Associations = associations;
        }
    }

    private class TestAssociation
    {
        public int PropertyIndex;
        public bool Essential;

        public TestAssociation(int propertyIndex, bool essential)
        {
            PropertyIndex = propertyIndex;
            Essential = essential;
        }
    }
}
