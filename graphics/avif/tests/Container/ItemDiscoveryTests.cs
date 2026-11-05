using System.Buffers.Binary;
using System.Text;

namespace avif.Tests;

public class ItemDiscoveryTests
{
    [Test]
    public void DiscoveryClassifiesOverlappingItemRolesAndMasterLinks()
    {
        TestItem[] items = new TestItem[]
        {
            new TestItem(1, "av01", false),
            new TestItem(2, "av01", true),
            new TestItem(3, "grid", false),
            new TestItem(4, "mime", false),
            new TestItem(5, "Exif", false),
            new TestItem(6, "uri ", false),
            new TestItem(7, "av01", true),
            new TestItem(8, "av01", false),
            new TestItem(9, "av01", false),
            new TestItem(10, "av01", false),
            new TestItem(11, "zzzz", false)
        };
        byte[] source = BuildDocument(items, 1, writer =>
        {
            WriteAuxiliaryProperties(writer, new AuxiliaryAssociation[]
            {
                new AuxiliaryAssociation(
                    7,
                    "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"),
                new AuxiliaryAssociation(
                    8,
                    "urn:mpeg:mpegB:cicp:systems:auxiliary:depth"),
                new AuxiliaryAssociation(9, "urn:example:confidence")
            });
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "auxl", 7, new uint[] { 1 });
            WriteReference(writer, "auxl", 8, new uint[] { 1 });
            WriteReference(writer, "auxl", 9, new uint[] { 1 });
            WriteReference(writer, "thmb", 10, new uint[] { 1 });
            WriteReference(writer, "dimg", 3, new uint[] { 1, 2 });
            WriteReference(writer, "cdsc", 11, new uint[] { 1 });
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemDiscovery discovery = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDiscovery();
        ParsedItemRole? primary = discovery.FindRole(1);
        ParsedItemRole? alpha = discovery.FindRole(7);
        ParsedItemRole? thumbnail = discovery.FindRole(10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(discovery.GetRoleCount(), Is.EqualTo(11));
            Assert.That(discovery.HasPrimaryItem(), Is.True);
            Assert.That((discovery.GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(1));
            Assert.That(discovery.GetHiddenItemCount(), Is.EqualTo(2));
            Assert.That((discovery.GetHiddenItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(2));
            Assert.That((discovery.GetHiddenItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(7));
            Assert.That(discovery.GetThumbnailItemCount(), Is.EqualTo(1));
            Assert.That((discovery.GetThumbnailItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(10));
            Assert.That(discovery.GetCodedItemCount(), Is.EqualTo(6));
            Assert.That(discovery.GetDerivedItemCount(), Is.EqualTo(1));
            Assert.That((discovery.GetDerivedItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(3));
            Assert.That(discovery.GetMetadataItemCount(), Is.EqualTo(4));
            Assert.That((discovery.GetMetadataItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(4));
            Assert.That((discovery.GetMetadataItem(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(11));
            Assert.That(discovery.GetAuxiliaryItemCount(), Is.EqualTo(3));
            Assert.That(discovery.GetAlphaItemCount(), Is.EqualTo(1));
            Assert.That((discovery.GetAlphaItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(7));
            Assert.That(discovery.GetDepthItemCount(), Is.EqualTo(1));
            Assert.That((discovery.GetDepthItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(8));
            Assert.That(discovery.GetArbitraryAuxiliaryItemCount(), Is.EqualTo(1));
            Assert.That((discovery.GetArbitraryAuxiliaryItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(9));
            Assert.That((alpha ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary(), Is.False);
            Assert.That(alpha.IsHidden(), Is.True);
            Assert.That(alpha.IsCodedImage(), Is.True);
            Assert.That(alpha.IsAuxiliaryImage(), Is.True);
            Assert.That(alpha.IsAlphaImage(), Is.True);
            Assert.That(alpha.GetAuxiliaryKind(), Is.EqualTo(ParsedAuxiliaryItemKind.Alpha));
            Assert.That(
                alpha.GetAuxiliaryType(),
                Is.EqualTo("urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
            Assert.That(alpha.GetAuxiliaryMasterCount(), Is.EqualTo(1));
            Assert.That((alpha.GetAuxiliaryMaster(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(1));
            Assert.That((primary ?? throw new InvalidOperationException("Invalid internal state.")).GetAssociatedAuxiliaryItemCount(), Is.EqualTo(3));
            Assert.That((primary.GetAssociatedAuxiliaryItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(7));
            Assert.That((primary.GetAssociatedAuxiliaryItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(9));
            Assert.That(primary.GetAssociatedThumbnailItemCount(), Is.EqualTo(1));
            Assert.That((primary.GetAssociatedThumbnailItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(10));
            Assert.That((thumbnail ?? throw new InvalidOperationException("Invalid internal state.")).IsThumbnail(), Is.True);
            Assert.That(thumbnail.GetThumbnailMasterCount(), Is.EqualTo(1));
            Assert.That((thumbnail.GetThumbnailMaster(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(1));
        });
    }

    [Test]
    public void DerivedTypesAndReferenceEvidenceAreDiscoveredWithoutPrimaryItem()
    {
        TestItem[] items = new TestItem[]
        {
            new TestItem(1, "grid", false),
            new TestItem(2, "tmap", false),
            new TestItem(3, "sato", false),
            new TestItem(4, "av01", false),
            new TestItem(5, "av01", false),
            new TestItem(6, "zzzz", false),
            new TestItem(7, "av01", false)
        };
        byte[] source = BuildDocument(items, 0, writer =>
        {
            WriteAuxiliaryProperties(writer, new AuxiliaryAssociation[]
            {
                new AuxiliaryAssociation(
                    4,
                    "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"),
                new AuxiliaryAssociation(7, "urn:mpeg:hevc:2015:auxid:1")
            });
            writer.BeginFullBox("iref", 0, 0);
            WriteReference(writer, "auxl", 5, new uint[] { 4 });
            WriteReference(writer, "auxl", 7, new uint[] { 4 });
            WriteReference(writer, "dimg", 6, new uint[] { 4 });
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemDiscovery discovery = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDiscovery();
        ParsedItemRole? typedOnly = discovery.FindRole(4);
        ParsedItemRole? untypedAuxiliary = discovery.FindRole(5);
        ParsedItemRole? referencedDerived = discovery.FindRole(6);
        ParsedItemRole? legacyAlpha = discovery.FindRole(7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(discovery.HasPrimaryItem(), Is.False);
            Assert.That(discovery.GetPrimaryItem(), Is.Null);
            Assert.That(discovery.GetDerivedItemCount(), Is.EqualTo(4));
            Assert.That((discovery.GetDerivedItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("grid"));
            Assert.That((discovery.GetDerivedItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("tmap"));
            Assert.That((discovery.GetDerivedItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("sato"));
            Assert.That((discovery.GetDerivedItem(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(6));
            Assert.That((typedOnly ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliaryImage(), Is.False);
            Assert.That(typedOnly.IsAlphaImage(), Is.False);
            Assert.That(
                typedOnly.GetAuxiliaryType(),
                Is.EqualTo("urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
            Assert.That((untypedAuxiliary ?? throw new InvalidOperationException("Invalid internal state.")).IsArbitraryAuxiliaryImage(), Is.True);
            Assert.That(untypedAuxiliary.GetAuxiliaryType(), Is.Empty);
            Assert.That((untypedAuxiliary.GetAuxiliaryMaster(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(4));
            Assert.That((referencedDerived ?? throw new InvalidOperationException("Invalid internal state.")).IsDerivedImage(), Is.True);
            Assert.That(discovery.GetAlphaItemCount(), Is.EqualTo(1));
            Assert.That((legacyAlpha ?? throw new InvalidOperationException("Invalid internal state.")).IsAlphaImage(), Is.True);
            Assert.That(legacyAlpha.GetAuxiliaryKind(), Is.EqualTo(ParsedAuxiliaryItemKind.Alpha));
        });
    }

    [Test]
    public void DiscoveryLookupsAreBoundedAndPreserveItemOrder()
    {
        TestItem[] items = new TestItem[]
        {
            new TestItem(30, "av01", true),
            new TestItem(10, "mime", false),
            new TestItem(20, "grid", true)
        };
        byte[] source = BuildDocument(items, 30, writer => { });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedItemDiscovery discovery = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemDiscovery();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((discovery.GetRole(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetItem().GetItemId(), Is.EqualTo(30));
            Assert.That((discovery.GetRole(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetItem().GetItemId(), Is.EqualTo(10));
            Assert.That((discovery.GetRole(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetItem().GetItemId(), Is.EqualTo(20));
            Assert.That((discovery.FindRole(30) ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary(), Is.True);
            Assert.That(discovery.FindRole(999), Is.Null);
            Assert.That(discovery.GetRole(-1), Is.Null);
            Assert.That(discovery.GetRole(3), Is.Null);
            Assert.That(discovery.GetHiddenItem(-1), Is.Null);
            Assert.That(discovery.GetHiddenItem(2), Is.Null);
            Assert.That(discovery.GetMetadataItem(1), Is.Null);
            Assert.That((discovery.FindRole(30) ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryMaster(0), Is.Null);
            Assert.That((discovery.FindRole(30) ?? throw new InvalidOperationException("Invalid internal state.")).GetThumbnailMaster(0), Is.Null);
            Assert.That((discovery.FindRole(30) ?? throw new InvalidOperationException("Invalid internal state.")).GetAssociatedAuxiliaryItem(0), Is.Null);
            Assert.That((discovery.FindRole(30) ?? throw new InvalidOperationException("Invalid internal state.")).GetAssociatedThumbnailItem(0), Is.Null);
        });
    }

    private static byte[] BuildDocument(
        TestItem[] items,
        uint primaryItemId,
        Action<IsoBoxWriter> writeChildren)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 131072);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        if (primaryItemId != 0)
        {
            writer.BeginFullBox("pitm", 1, 0);
            WriteUInt32(writer, primaryItemId);
            writer.EndBox();
        }

        WriteItemInfo(writer, items);
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

    private static void WriteItemInfo(IsoBoxWriter writer, TestItem[] items)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, (uint)items.Length);
        int index = 0;
        while (index < items.Length)
        {
            TestItem item = items[index];
            writer.BeginFullBox("infe", 3, item.Hidden ? 1 : 0);
            WriteUInt32(writer, item.Id);
            WriteUInt16(writer, 0);
            WriteFourCc(writer, item.Type);
            writer.Write(new byte[] { 0 });
            if (item.Type == "mime")
            {
                writer.Write(Encoding.UTF8.GetBytes("application/rdf+xml"));
                writer.Write(new byte[] { 0, 0 });
            }
            else if (item.Type == "uri ")
            {
                writer.Write(Encoding.UTF8.GetBytes("urn:example:metadata"));
                writer.Write(new byte[] { 0 });
            }

            writer.EndBox();
            index++;
        }

        writer.EndBox();
    }

    private static void WriteAuxiliaryProperties(
        IsoBoxWriter writer,
        AuxiliaryAssociation[] associations)
    {
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        int index = 0;
        while (index < associations.Length)
        {
            writer.BeginFullBox("auxC", 0, 0);
            writer.Write(Encoding.UTF8.GetBytes(associations[index].Type));
            writer.Write(new byte[] { 0 });
            writer.EndBox();
            index++;
        }

        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, (uint)associations.Length);
        index = 0;
        while (index < associations.Length)
        {
            WriteUInt16(writer, (ushort)associations[index].ItemId);
            writer.Write(new byte[] { 1, (byte)(index + 1) });
            index++;
        }

        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteReference(
        IsoBoxWriter writer,
        string type,
        uint fromItemId,
        uint[] targetItemIds)
    {
        writer.BeginBox(type);
        WriteUInt16(writer, (ushort)fromItemId);
        WriteUInt16(writer, (ushort)targetItemIds.Length);
        int index = 0;
        while (index < targetItemIds.Length)
        {
            WriteUInt16(writer, (ushort)targetItemIds[index]);
            index++;
        }

        writer.EndBox();
    }

    private static void WriteFourCc(IsoBoxWriter writer, string value)
    {
        byte[] bytes = new byte[4];
        WriteFourCc(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
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

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 262144;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 64;
        limits.MaxPropertyCount = 256;
        limits.MaxReferenceCount = 256;
        limits.MaxMetadataBytes = 65536;
        limits.MaxIccBytes = 32768;
        limits.MaxTemporaryBytes = 131072;
        return limits;
    }

    private class TestItem
    {
        internal uint Id;
        internal string Type;
        internal bool Hidden;

        internal TestItem(uint id, string type, bool hidden)
        {
            Id = id;
            Type = type;
            Hidden = hidden;
        }
    }

    private class AuxiliaryAssociation
    {
        internal uint ItemId;
        internal string Type;

        internal AuxiliaryAssociation(uint itemId, string type)
        {
            ItemId = itemId;
            Type = type;
        }
    }
}
