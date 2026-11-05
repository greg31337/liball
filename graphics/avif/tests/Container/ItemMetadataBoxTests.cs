using System.Buffers.Binary;
using System.Text;

namespace avif.Tests;

public class ItemMetadataBoxTests
{
    [Test]
    public void ParserBuildsImmutableModelFromVersionTwoAndThreeEntries()
    {
        byte[] source = BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "AVIF handler", 0);
            WritePrimaryItem(writer, 1, 70000);
            WriteItemInfo(writer, 1, 3, entries =>
            {
                WriteItemInfoEntry(entries, 3, 0, 70000, 0, "av01", "Main", "", "", "", true, false);
                WriteItemInfoEntry(entries, 2, 1, 2, 0, "mime", "XMP", "application/rdf+xml", "gzip", "", true, false);
                WriteItemInfoEntry(entries, 3, 0, 80000, 5, "uri ", "Link", "", "", "urn:example:item", true, false);
            });
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItem? primary = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem();
        ParsedItem? mime = result.Meta.FindItem(2);
        ParsedItem? uri = result.Meta.FindItem(80000);

        source.AsSpan().Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.GetHandlerType(), Is.EqualTo("pict"));
            Assert.That(result.Meta.GetHandlerName(), Is.EqualTo("AVIF handler"));
            Assert.That(result.Meta.HasPrimaryItem(), Is.True);
            Assert.That(result.Meta.GetPrimaryItemId(), Is.EqualTo(70000));
            Assert.That(result.Meta.GetItemCount(), Is.EqualTo(3));
            Assert.That((primary ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(70000));
            Assert.That(primary.GetItemInfoVersion(), Is.EqualTo(3));
            Assert.That(primary.GetItemType(), Is.EqualTo("av01"));
            Assert.That(primary.GetName(), Is.EqualTo("Main"));
            Assert.That((mime ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(mime.GetContentType(), Is.EqualTo("application/rdf+xml"));
            Assert.That(mime.GetContentEncoding(), Is.EqualTo("gzip"));
            Assert.That((uri ?? throw new InvalidOperationException("Invalid internal state.")).GetProtectionIndex(), Is.EqualTo(5));
            Assert.That(uri.GetItemUriType(), Is.EqualTo("urn:example:item"));
            Assert.That(result.Meta.GetItem(-1), Is.Null);
            Assert.That(result.Meta.FindItem(999), Is.Null);
        });
    }

    [Test]
    public void VersionZeroItemInfoTraversesEntriesAndFreeBoxes()
    {
        byte[] source = BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WritePrimaryItem(writer, 0, 1);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                entries.BeginBox("free");
                entries.EndBox();
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "av01", "", "", "", "", true, false);
            });
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        IsoBox meta = (result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes[0];
        IsoBox itemInfo = FindChild(meta, "iinf");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(itemInfo.IsContainer, Is.True);
            Assert.That(itemInfo.HasFullBoxFields, Is.True);
            Assert.That(itemInfo.Version, Is.Zero);
            Assert.That(itemInfo.Children.Count, Is.EqualTo(2));
            Assert.That(itemInfo.Children[0].Type, Is.EqualTo("free"));
            Assert.That(itemInfo.Children[1].Type, Is.EqualTo("infe"));
            Assert.That(((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemInfoVersion(), Is.EqualTo(2));
            Assert.That(result.Boxes.Resources.ItemCount.Current, Is.EqualTo(1));
        });
    }

    [Test]
    public void ParserRejectsUnsupportedBoxVersions()
    {
        MetaParseResult meta = MetaBoxParser.Parse(
            BuildMeta(1, writer => WriteHandler(writer, 0, 0, "pict", "", 0)),
            CreateLimits());
        MetaParseResult handler = MetaBoxParser.Parse(
            BuildMeta(0, writer => WriteHandler(writer, 1, 0, "pict", "", 0)),
            CreateLimits());
        MetaParseResult primary = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WritePrimaryItem(writer, 2, 1);
        }), CreateLimits());
        MetaParseResult itemInfo = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 2, 0, entries => { });
        }), CreateLimits());
        MetaParseResult entry = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                WriteItemInfoEntry(entries, 1, 0, 1, 0, "av01", "", "", "", "", true, false);
            });
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(meta.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(handler.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(primary.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(itemInfo.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(entry.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
        });
    }

    [Test]
    public void ItemInfoRejectsCountMismatchDuplicateIdsAndUnexpectedChildren()
    {
        MetaParseResult countMismatch = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 2, entries =>
            {
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "av01", "", "", "", "", true, false);
            });
        }), CreateLimits());
        MetaParseResult duplicate = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 2, entries =>
            {
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "av01", "", "", "", "", true, false);
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "Exif", "", "", "", "", true, false);
            });
        }), CreateLimits());
        MetaParseResult unexpected = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                entries.BeginBox("abcd");
                entries.EndBox();
            });
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(countMismatch.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(unexpected.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(((duplicate.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iinf"));
        });
    }

    [Test]
    public void ItemCountIsBoundedBeforeEntriesAreMaterialized()
    {
        byte[] source = BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 2, entries =>
            {
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "av01", "", "", "", "", true, false);
                WriteItemInfoEntry(entries, 2, 0, 2, 0, "av01", "", "", "", "", true, false);
            });
        });
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxItemCount = 1;

        MetaParseResult result = MetaBoxParser.Parse(source, limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That((result.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Resources.ItemCount.Current, Is.Zero);
            Assert.That(
                (result.Boxes.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
            Assert.That((result.Boxes.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/iinf"));
        });
    }

    [Test]
    public void EntryParserRejectsMalformedStringsTrailingDataAndReservedFlags()
    {
        MetaParseResult missingTerminator = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "mime", "XMP", "application/xml", "gzip", "", false, false);
            });
        }), CreateLimits());
        MetaParseResult trailingData = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "av01", "", "", "", "", true, true);
            });
        }), CreateLimits());
        MetaParseResult reservedFlags = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                WriteItemInfoEntry(entries, 2, 2, 1, 0, "av01", "", "", "", "", true, false);
            });
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missingTerminator.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(trailingData.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(reservedFlags.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void HandlerValidatesReservedFieldsAndAllowsAnOmittedName()
    {
        MetaParseResult noName = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandlerWithoutName(writer, 0, 0, "pict", 0);
            Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
        }), CreateLimits());
        MetaParseResult reserved = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 1);
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(noName.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((noName.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetHandlerName(), Is.Empty);
            Assert.That(reserved.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void PrimaryItemMustIdentifyADeclaredNonzeroItem()
    {
        MetaParseResult missing = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WritePrimaryItem(writer, 0, 2);
            WriteItemInfo(writer, 0, 1, entries =>
            {
                WriteItemInfoEntry(entries, 2, 0, 1, 0, "av01", "", "", "", "", true, false);
            });
        }), CreateLimits());
        MetaParseResult zero = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WritePrimaryItem(writer, 0, 0);
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((missing.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/pitm"));
            Assert.That(zero.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void MetaRequiresOneHandlerAndRejectsDuplicateSingletonChildren()
    {
        MetaParseResult missingHandler = MetaBoxParser.Parse(
            BuildMeta(0, writer => { }),
            CreateLimits());
        MetaParseResult duplicateHandler = MetaBoxParser.Parse(BuildMeta(0, writer =>
        {
            WriteHandler(writer, 0, 0, "pict", "", 0);
            WriteHandler(writer, 0, 0, "pict", "", 0);
        }), CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(missingHandler.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(((missingHandler.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta"));
            Assert.That(duplicateHandler.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(((duplicateHandler.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/hdlr"));
        });
    }

    private static byte[] BuildMeta(int version, Action<IsoBoxWriter> writeChildren)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 4096);
        Assert.That(writer.BeginFullBox("meta", version, 0), Is.EqualTo(AvifResultCode.Success));
        writeChildren(writer);
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteHandler(
        IsoBoxWriter writer,
        int version,
        int flags,
        string handlerType,
        string name,
        uint reservedValue)
    {
        WriteHandlerWithoutName(writer, version, flags, handlerType, reservedValue);
        byte[] encodedName = Encoding.UTF8.GetBytes(name);
        Assert.That(writer.Write(encodedName), Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.Write(new byte[] { 0 }), Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
    }

    private static void WriteHandlerWithoutName(
        IsoBoxWriter writer,
        int version,
        int flags,
        string handlerType,
        uint reservedValue)
    {
        Assert.That(writer.BeginFullBox("hdlr", version, flags), Is.EqualTo(AvifResultCode.Success));
        byte[] fields = new byte[20];
        WriteFourCc(fields.AsSpan(4, 4), handlerType);
        BinaryPrimitives.WriteUInt32BigEndian(fields.AsSpan(8, 4), reservedValue);
        Assert.That(writer.Write(fields), Is.EqualTo(AvifResultCode.Success));
    }

    private static void WritePrimaryItem(IsoBoxWriter writer, int version, uint itemId)
    {
        Assert.That(writer.BeginFullBox("pitm", version, 0), Is.EqualTo(AvifResultCode.Success));
        if (version == 0)
        {
            byte[] value = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(value, (ushort)itemId);
            writer.Write(value);
        }
        else
        {
            byte[] value = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(value, itemId);
            writer.Write(value);
        }

        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
    }

    private static void WriteItemInfo(
        IsoBoxWriter writer,
        int version,
        uint entryCount,
        Action<IsoBoxWriter> writeEntries)
    {
        Assert.That(writer.BeginFullBox("iinf", version, 0), Is.EqualTo(AvifResultCode.Success));
        if (version == 0)
        {
            byte[] value = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(value, (ushort)entryCount);
            writer.Write(value);
        }
        else
        {
            byte[] value = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(value, entryCount);
            writer.Write(value);
        }

        writeEntries(writer);
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
    }

    private static void WriteItemInfoEntry(
        IsoBoxWriter writer,
        int version,
        int flags,
        uint itemId,
        ushort protectionIndex,
        string itemType,
        string name,
        string contentType,
        string contentEncoding,
        string itemUriType,
        bool terminateLastString,
        bool addTrailingData)
    {
        Assert.That(writer.BeginFullBox("infe", version, flags), Is.EqualTo(AvifResultCode.Success));
        List<byte> payload = new List<byte>();
        if (version == 2 || version == 1)
        {
            AddUInt16(payload, (ushort)itemId);
        }
        else
        {
            AddUInt32(payload, itemId);
        }

        AddUInt16(payload, protectionIndex);
        AddFourCc(payload, itemType);
        AddString(payload, name, true);
        if (itemType == "mime")
        {
            AddString(payload, contentType, true);
            AddString(payload, contentEncoding, terminateLastString);
        }
        else if (itemType == "uri ")
        {
            AddString(payload, itemUriType, terminateLastString);
        }

        if (addTrailingData)
        {
            payload.Add(127);
        }

        writer.Write(payload.ToArray());
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
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

    private static void AddFourCc(List<byte> destination, string value)
    {
        byte[] bytes = new byte[4];
        WriteFourCc(bytes, value);
        destination.AddRange(bytes);
    }

    private static void AddString(List<byte> destination, string value, bool terminate)
    {
        destination.AddRange(Encoding.UTF8.GetBytes(value));
        if (terminate)
        {
            destination.Add(0);
        }
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
        limits.MaxSourceBytes = 8192;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 128;
        limits.MaxItemCount = 32;
        return limits;
    }
}
