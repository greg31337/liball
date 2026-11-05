using System.Buffers.Binary;

namespace avif.Tests;

public class EntityGroupBoxTests
{
    [Test]
    public void AlternateAndStereoGroupsResolveItemsInEntityOrder()
    {
        byte[] source = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "altr", 0, 0, 10, new uint[] { 900, 2, 1 }, System.Array.Empty<byte>());
            WriteGroup(writer, "ster", 0, 0, 11, new uint[] { 1, 3 }, System.Array.Empty<byte>());
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedEntityGroup? alternate = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindEntityGroup(10);
        ParsedEntityGroup? stereo = result.Meta.FindEntityGroup(11);
        ParsedItem? left = result.Meta.FindItem(1);
        ParsedItem? alternateItem = result.Meta.FindItem(2);
        ParsedItem? right = result.Meta.FindItem(3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Meta.GetEntityGroupCount(), Is.EqualTo(2));
            Assert.That(result.Meta.GetEntityGroup(0), Is.SameAs(alternate));
            Assert.That((alternate ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedEntityGroupKind.Alternate));
            Assert.That(alternate.GetGroupingType(), Is.EqualTo("altr"));
            Assert.That(alternate.GetEntityCount(), Is.EqualTo(3));
            Assert.That(alternate.GetEntityId(0), Is.EqualTo(900));
            Assert.That(alternate.GetEntityItem(0), Is.Null);
            Assert.That(alternate.SelectFirstAvailableItem(), Is.SameAs(alternateItem));
            Assert.That((alternateItem ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityGroup(0), Is.SameAs(alternate));
            Assert.That((stereo ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(ParsedEntityGroupKind.Stereo));
            Assert.That(stereo.GetLeftItem(), Is.SameAs(left));
            Assert.That(stereo.GetRightItem(), Is.SameAs(right));
            Assert.That((left ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityGroupCount(), Is.EqualTo(2));
            Assert.That(left.GetEntityGroup(0), Is.SameAs(alternate));
            Assert.That(left.GetEntityGroup(1), Is.SameAs(stereo));
            Assert.That((right ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityGroup(0), Is.SameAs(stereo));
        });
    }

    [Test]
    public void UnknownGroupTypeIsOpaqueAndPreservedIndependently()
    {
        byte[] source = BuildDocument(new uint[] { 1 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "meta", 7, 0x010203, 20, new uint[] { 1 }, new byte[] { 9, 8, 7 });
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedEntityGroup? group = (result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityGroup(0);
        byte[] expected = (group ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();
        byte[] callerCopy = group.GetRawData();
        callerCopy.AsSpan().Clear();
        source.AsSpan().Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(group.GetKind(), Is.EqualTo(ParsedEntityGroupKind.Unknown));
            Assert.That(group.GetGroupingType(), Is.EqualTo("meta"));
            Assert.That(group.GetTypeValue(), Is.EqualTo(IsoBoxType.GetValue("meta")));
            Assert.That(group.GetVersion(), Is.EqualTo(7));
            Assert.That(group.GetFlags(), Is.EqualTo(0x010203));
            Assert.That(group.HasPreservedData(), Is.True);
            Assert.That(group.GetRawData(), Is.EqualTo(expected));
            Assert.That(group.GetEntityItem(0), Is.SameAs(result.Meta.FindItem(1)));
            Assert.That(expected.AsSpan(4, 4).ToArray(), Is.EqualTo(new byte[] { 109, 101, 116, 97 }));
        });
    }

    [Test]
    public void KnownGroupsRequireVersionZeroZeroFlagsExactPayloadAndStereoArity()
    {
        MetaParseResult alternateVersion = ParseSingleGroup(
            "altr", 1, 0, 10, new uint[] { 1, 2 }, System.Array.Empty<byte>());
        MetaParseResult stereoFlags = ParseSingleGroup(
            "ster", 0, 1, 10, new uint[] { 1, 2 }, System.Array.Empty<byte>());
        MetaParseResult singleAlternate = ParseSingleGroup(
            "altr", 0, 0, 10, new uint[] { 1 }, System.Array.Empty<byte>());
        MetaParseResult stereoArity = ParseSingleGroup(
            "ster", 0, 0, 10, new uint[] { 1, 2, 3 }, System.Array.Empty<byte>());
        MetaParseResult trailing = ParseSingleGroup(
            "altr", 0, 0, 10, new uint[] { 1, 2 }, new byte[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(alternateVersion.Code, Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(stereoFlags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(singleAlternate.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((((singleAlternate.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityGroup(0) ?? throw new InvalidOperationException("Invalid internal state.")).SelectFirstAvailableItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(1));
            Assert.That(stereoArity.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    [Test]
    public void GroupAndEntityIdentifiersMustBeUniqueAndDisjoint()
    {
        MetaParseResult duplicateEntity = ParseSingleGroup(
            "altr", 0, 0, 10, new uint[] { 1, 1 }, System.Array.Empty<byte>());
        MetaParseResult zeroEntity = ParseSingleGroup(
            "altr", 0, 0, 10, new uint[] { 0, 1 }, System.Array.Empty<byte>());
        MetaParseResult entityIsGroup = ParseSingleGroup(
            "altr", 0, 0, 10, new uint[] { 10, 1 }, System.Array.Empty<byte>());
        byte[] duplicateGroupData = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "altr", 0, 0, 10, new uint[] { 1, 2 }, System.Array.Empty<byte>());
            WriteGroup(writer, "ster", 0, 0, 10, new uint[] { 2, 3 }, System.Array.Empty<byte>());
            writer.EndBox();
        });
        MetaParseResult duplicateGroup = MetaBoxParser.Parse(duplicateGroupData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(duplicateEntity.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroEntity.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(entityIsGroup.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(duplicateGroup.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void AlternateMembershipIsExclusiveAndStereoRequiresDeclaredItems()
    {
        byte[] repeatedAlternateData = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "altr", 0, 0, 10, new uint[] { 1, 2 }, System.Array.Empty<byte>());
            WriteGroup(writer, "altr", 0, 0, 11, new uint[] { 2, 3 }, System.Array.Empty<byte>());
            writer.EndBox();
        });
        byte[] missingStereoData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "ster", 0, 0, 10, new uint[] { 1, 99 }, System.Array.Empty<byte>());
            writer.EndBox();
        });
        byte[] conflictingItemData = BuildDocument(new uint[] { 1, 10 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "altr", 0, 0, 10, new uint[] { 1, 2 }, System.Array.Empty<byte>());
            writer.EndBox();
        });

        MetaParseResult repeatedAlternate = MetaBoxParser.Parse(repeatedAlternateData, CreateLimits());
        MetaParseResult missingStereo = MetaBoxParser.Parse(missingStereoData, CreateLimits());
        MetaParseResult conflictingItem = MetaBoxParser.Parse(conflictingItemData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(repeatedAlternate.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(missingStereo.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(conflictingItem.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(((missingStereo.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/meta/grpl"));
        });
    }

    [Test]
    public void PayloadMustContainTheDeclaredEntityIdentifiers()
    {
        byte[] source = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginBox("grpl");
            writer.BeginFullBox("altr", 0, 0);
            WriteUInt32(writer, 10);
            WriteUInt32(writer, 2);
            WriteUInt32(writer, 1);
            writer.EndBox();
            writer.EndBox();
        });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());

        Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
    }

    [Test]
    public void ReferenceAndPreservationLimitsApplyToGroups()
    {
        byte[] entityData = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "altr", 0, 0, 10, new uint[] { 1, 2, 3 }, System.Array.Empty<byte>());
            writer.EndBox();
        });
        AvifSecurityLimits entityLimits = CreateLimits();
        entityLimits.MaxReferenceCount = 2;

        byte[] preservationData = BuildDocument(new uint[] { 1 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, "zzzz", 0, 0, 10, new uint[] { 1 }, new byte[32]);
            writer.EndBox();
        });
        AvifSecurityLimits preservationLimits = CreateLimits();
        preservationLimits.MaxTemporaryBytes = 8;

        MetaParseResult entities = MetaBoxParser.Parse(entityData, entityLimits);
        MetaParseResult preservation = MetaBoxParser.Parse(preservationData, preservationLimits);

        Assert.Multiple(() =>
        {
            Assert.That(entities.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(preservation.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(((entities.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Resource));
            Assert.That(((preservation.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void FreeAndSkipChildrenAreIgnoredButDuplicateGroupListsFail()
    {
        byte[] ignoredData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginBox("grpl");
            writer.BeginBox("free");
            writer.EndBox();
            WriteGroup(writer, "altr", 0, 0, 10, new uint[] { 1, 2 }, System.Array.Empty<byte>());
            writer.BeginBox("skip");
            writer.EndBox();
            writer.EndBox();
        });
        byte[] duplicateData = BuildDocument(new uint[] { 1, 2 }, writer =>
        {
            writer.BeginBox("grpl");
            writer.EndBox();
            writer.BeginBox("grpl");
            writer.EndBox();
        });

        MetaParseResult ignored = MetaBoxParser.Parse(ignoredData, CreateLimits());
        MetaParseResult duplicate = MetaBoxParser.Parse(duplicateData, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(ignored.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((ignored.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityGroupCount(), Is.EqualTo(1));
            Assert.That(duplicate.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
        });
    }

    private static MetaParseResult ParseSingleGroup(
        string type,
        int version,
        int flags,
        uint groupId,
        uint[] entityIds,
        byte[] trailingData)
    {
        byte[] source = BuildDocument(new uint[] { 1, 2, 3 }, writer =>
        {
            writer.BeginBox("grpl");
            WriteGroup(writer, type, version, flags, groupId, entityIds, trailingData);
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

    private static void WriteGroup(
        IsoBoxWriter writer,
        string type,
        int version,
        int flags,
        uint groupId,
        uint[] entityIds,
        byte[] trailingData)
    {
        writer.BeginFullBox(type, version, flags);
        WriteUInt32(writer, groupId);
        WriteUInt32(writer, (uint)entityIds.Length);
        int index = 0;
        while (index < entityIds.Length)
        {
            WriteUInt32(writer, entityIds[index]);
            index++;
        }

        writer.Write(trailingData);
        writer.EndBox();
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
