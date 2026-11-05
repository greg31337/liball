using System.Buffers.Binary;
using System.Globalization;

namespace avif.Tests;

public class AvifDocumentTests
{
    [Test]
    public void ParsedDocumentExposesBoxesItemsExtentsPropertiesReferencesGroupsAndFindings()
    {
        byte[] source = BuildDocument();
        AvifDocument document = AvifDocument.Parse(source, CreateOptions());
        AvifFileInfo? file = document.GetFileInfo();
        AvifBox? meta = document.GetTopLevelBox(2);
        AvifImageItem? coded = document.FindItem(1);
        AvifImageItem? grid = document.FindItem(2);
        AvifItemProperty? configuration = (coded ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0);
        AvifItemProperty? opaque = coded.GetProperty(3);
        AvifItemReference? reference = document.GetReference(0);
        AvifEntityGroup? group = document.GetGroup(0);
        AvifDiagnostic? finding = document.GetFinding(0);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((file ?? throw new InvalidOperationException("Invalid internal state.")).GetMajorBrand(), Is.EqualTo("avif"));
            Assert.That(file.GetMinorVersion(), Is.Zero);
            Assert.That(file.GetCompatibleBrandCount(), Is.EqualTo(1));
            Assert.That(file.GetCompatibleBrand(0), Is.EqualTo("miaf"));
            Assert.That(file.GetCompatibleBrand(1), Is.Empty);
            Assert.That(file.HasBrand("miaf"), Is.True);
            Assert.That(file.HasBrand("mif1"), Is.False);
            Assert.That(file.GetHandlerType(), Is.EqualTo("pict"));
            Assert.That(file.GetPrimaryItemId(), Is.EqualTo(2));

            Assert.That(document.GetTopLevelBoxCount(), Is.EqualTo(3));
            Assert.That(document.GetBoxCount(), Is.EqualTo(20));
            Assert.That((meta ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("meta"));
            Assert.That(meta.GetPath(), Is.EqualTo("/meta"));
            Assert.That(meta.GetHeaderSize(), Is.EqualTo(8));
            Assert.That(meta.UsesLargeSize(), Is.False);
            Assert.That(meta.ExtendsToEnd(), Is.False);
            Assert.That(meta.IsFullBox(), Is.True);
            Assert.That(meta.GetVersion(), Is.Zero);
            Assert.That(meta.GetChildCount(), Is.EqualTo(7));
            Assert.That((meta.GetChild(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("hdlr"));
            Assert.That(meta.GetChild(-1), Is.Null);
            Assert.That(document.GetBox(-1), Is.Null);

            Assert.That(document.GetItemCount(), Is.EqualTo(2));
            Assert.That(coded.IsCodedImage(), Is.True);
            Assert.That(coded.IsPrimary(), Is.False);
            Assert.That(coded.IsSelfContained(), Is.True);
            Assert.That(coded.HasLocation(), Is.True);
            Assert.That(coded.HasKnownPayloadLength(), Is.True);
            Assert.That(coded.GetExtentCount(), Is.EqualTo(1));
            Assert.That((coded.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsResolved(), Is.True);
            Assert.That((coded.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(), Is.EqualTo(28));
            Assert.That((coded.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedLength(), Is.EqualTo(4));
            Assert.That((grid ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary(), Is.True);
            Assert.That(grid.IsDerivedImage(), Is.True);
            Assert.That((grid.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0), Is.EqualTo(1));
            Assert.That((coded.GetIncomingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetFromItemId(), Is.EqualTo(2));

            Assert.That((configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("av1C"));
            Assert.That(configuration.IsEssential(), Is.True);
            Assert.That(configuration.IsKnown(), Is.True);
            Assert.That(configuration.GetAv1Profile(), Is.Zero);
            Assert.That(configuration.GetAv1LevelIndex(), Is.EqualTo(4));
            Assert.That(configuration.GetAv1Tier(), Is.Zero);
            Assert.That(configuration.GetBitDepth(), Is.EqualTo(8));
            Assert.That((coded.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetImageWidth(), Is.EqualTo(64));
            Assert.That((coded.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetImageHeight(), Is.EqualTo(32));
            Assert.That((coded.GetProperty(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelChannelBitDepth(2), Is.EqualTo(8));
            Assert.That((opaque ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("zzzz"));
            Assert.That(opaque.IsKnown(), Is.False);
            Assert.That(opaque.GetSummary(), Is.EqualTo("opaque"));

            Assert.That((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(AvifItemReferenceKind.DerivedImage));
            Assert.That(reference.GetTypeName(), Is.EqualTo("dimg"));
            Assert.That(reference.GetFromItemId(), Is.EqualTo(2));
            Assert.That(reference.GetTargetItemId(0), Is.EqualTo(1));
            Assert.That((group ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(AvifEntityGroupKind.Alternate));
            Assert.That(group.GetGroupId(), Is.EqualTo(10));
            Assert.That(group.GetEntityId(1), Is.EqualTo(2));
            Assert.That(group.HasRawData(), Is.False);
            Assert.That(coded.GetGroupId(0), Is.EqualTo(10));

            Assert.That(document.GetFindingCount(), Is.EqualTo(1));
            Assert.That((finding ?? throw new InvalidOperationException("Invalid internal state.")).Severity, Is.EqualTo(AvifDiagnosticSeverity.Warning));
            Assert.That(finding.Message, Does.Contain("mif1"));
        });
    }

    [Test]
    public void PartialDocumentRetainsLargeAndExtendsToEndBoxHeaders()
    {
        byte[] source = new byte[28];
        BinaryPrimitives.WriteUInt32BigEndian(source.AsSpan(0, 4), 1);
        WriteFourCc(source.AsSpan(4, 4), "free");
        BinaryPrimitives.WriteUInt64BigEndian(source.AsSpan(8, 8), 16);
        WriteFourCc(source.AsSpan(20, 4), "skip");
        source[24] = 1;
        source[25] = 2;
        source[26] = 3;
        source[27] = 4;

        AvifDocument document = AvifDocument.Parse(source);
        AvifBox? large = document.GetTopLevelBox(0);
        AvifBox? trailing = document.GetTopLevelBox(1);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetTopLevelBoxCount(), Is.EqualTo(2));
            Assert.That((large ?? throw new InvalidOperationException("Invalid internal state.")).GetHeaderSize(), Is.EqualTo(16));
            Assert.That(large.UsesLargeSize(), Is.True);
            Assert.That(large.ExtendsToEnd(), Is.False);
            Assert.That((trailing ?? throw new InvalidOperationException("Invalid internal state.")).GetHeaderSize(), Is.EqualTo(8));
            Assert.That(trailing.UsesLargeSize(), Is.False);
            Assert.That(trailing.ExtendsToEnd(), Is.True);
            Assert.That(trailing.GetSize(), Is.EqualTo(12));
        });
    }

    [Test]
    public void ModelOwnsRetainedBytesAndReturnsDefensiveCopies()
    {
        byte[] source = BuildDocument();
        AvifDocument document = AvifDocument.Parse(source, CreateOptions());
        AvifItemProperty? opaque = (document.FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(3);
        byte[] expected = (opaque ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();
        byte[] callerCopy = opaque.GetRawData();
        AvifDiagnostic? callerFinding = document.GetFinding(0);
        string findingDump = document.DumpFindings();

        source.AsSpan().Clear();
        callerCopy.AsSpan().Clear();
        (callerFinding ?? throw new InvalidOperationException("Invalid internal state.")).Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(opaque.GetRawData(), Is.EqualTo(expected));
            Assert.That(opaque.GetRawData(), Is.Not.EqualTo(callerCopy));
            Assert.That((document.GetFinding(0) ?? throw new InvalidOperationException("Invalid internal state.")).Message, Does.Contain("mif1"));
            Assert.That(document.DumpFindings(), Is.EqualTo(findingDump));
            Assert.That(document.FindItem(99), Is.Null);
            Assert.That(document.GetGroup(99), Is.Null);
        });
    }

    [Test]
    public void SectionDumpsAreDeterministicAndContainTheCompleteModel()
    {
        AvifDocument document = AvifDocument.Parse(BuildDocument(), CreateOptions());
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        string first = document.Dump();
        string second;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            second = document.Dump();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.StartWith("result Success\nfile major=avif"));
            Assert.That(document.DumpBoxes(), Does.Contain("box depth=1 path=/meta/iprp"));
            Assert.That(document.DumpItems(), Does.Contain("item id=2 type=grid primary=true"));
            Assert.That(document.DumpExtents(), Does.Contain(
                "extent item=1 index=0 offset=28 length=4 resolved=true sourceOffset=28 sourceLength=4"));
            Assert.That(document.DumpProperties(), Does.Contain(
                "property item=1 index=1 type=av1C essential=true known=true"));
            Assert.That(document.DumpReferences(), Is.EqualTo(
                "references 1\nreference type=dimg kind=DerivedImage from=2 targets=1\n"));
            Assert.That(document.DumpGroups(), Is.EqualTo(
                "groups 1\ngroup id=10 type=altr kind=Alternate version=0 flags=0 entities=1,2\n"));
            Assert.That(document.DumpFindings(), Does.Contain(
                "finding severity=Warning code=NonConformingData category=Validation"));
        });
    }

    [Test]
    public void InvalidInputStillProducesAStablePartialDocumentAndFindings()
    {
        byte[] complete = BuildDocument();
        byte[] truncated = complete[..^1];
        AvifDocument partial = AvifDocument.Parse(truncated, CreateOptions());
        AvifDocument? nullSource = AvifDocument.Parse((byte[]?)null);

        Assert.Multiple(() =>
        {
            Assert.That(partial.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(partial.GetTopLevelBoxCount(), Is.GreaterThan(0));
            Assert.That(partial.GetFindingCount(), Is.EqualTo(1));
            Assert.That((partial.GetFinding(0) ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(partial.Dump(), Does.StartWith("result TruncatedData"));
            Assert.That(nullSource.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(nullSource.GetFileInfo(), Is.Null);
            Assert.That((nullSource.GetFinding(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category, Is.EqualTo(AvifDiagnosticCategory.Api));
        });
    }

    private static byte[] BuildDocument()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        FileTypeBox.Write(writer, "avif", 0, new string[] { "miaf" });
        writer.BeginBox("mdat");
        writer.Write(new byte[] { 0, 0, 0, 0 });
        writer.EndBox();
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        writer.BeginFullBox("pitm", 0, 0);
        WriteUInt16(writer, 2);
        writer.EndBox();
        WriteLocations(writer);
        WriteItemInfo(writer);
        WriteReferences(writer);
        WriteGroups(writer);
        WriteProperties(writer);
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

    private static void WriteLocations(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iloc", 1, 0);
        writer.Write(new byte[] { 68, 0 });
        WriteUInt16(writer, 2);
        int itemIndex = 0;
        while (itemIndex < 2)
        {
            WriteUInt16(writer, (ushort)(itemIndex + 1));
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 1);
            WriteUInt32(writer, 28);
            WriteUInt32(writer, 4);
            itemIndex++;
        }

        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, 2);
        WriteItemInfoEntry(writer, 1, "av01", "coded");
        WriteItemInfoEntry(writer, 2, "grid", "grid");
        writer.EndBox();
    }

    private static void WriteItemInfoEntry(
        IsoBoxWriter writer,
        uint itemId,
        string itemType,
        string name)
    {
        writer.BeginFullBox("infe", 3, 0);
        byte[] fields = new byte[10];
        BinaryPrimitives.WriteUInt32BigEndian(fields.AsSpan(0, 4), itemId);
        WriteFourCc(fields.AsSpan(6, 4), itemType);
        writer.Write(fields);
        writer.Write(System.Text.Encoding.UTF8.GetBytes(name));
        writer.Write(new byte[] { 0 });
        writer.EndBox();
    }

    private static void WriteReferences(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iref", 0, 0);
        writer.BeginBox("dimg");
        WriteUInt16(writer, 2);
        WriteUInt16(writer, 1);
        WriteUInt16(writer, 1);
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteGroups(IsoBoxWriter writer)
    {
        writer.BeginBox("grpl");
        writer.BeginFullBox("altr", 0, 0);
        WriteUInt32(writer, 10);
        WriteUInt32(writer, 2);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 2);
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteProperties(IsoBoxWriter writer)
    {
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        writer.BeginBox("av1C");
        writer.Write(new byte[] { 0x81, 4, 0x0c, 0 });
        writer.EndBox();
        writer.BeginFullBox("ispe", 0, 0);
        WriteUInt32(writer, 64);
        WriteUInt32(writer, 32);
        writer.EndBox();
        writer.BeginFullBox("pixi", 0, 0);
        writer.Write(new byte[] { 3, 8, 8, 8 });
        writer.EndBox();
        writer.BeginBox("zzzz");
        writer.Write(new byte[] { 9, 8, 7 });
        writer.EndBox();
        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, 2);
        WriteUInt16(writer, 1);
        writer.Write(new byte[] { 4, 0x81, 2, 3, 4 });
        WriteUInt16(writer, 2);
        writer.Write(new byte[] { 1, 2 });
        writer.EndBox();
        writer.EndBox();
    }

    private static AvifValidationOptions CreateOptions()
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.ValidateEncodedPayload = false;
        options.Limits.MaxSourceBytes = 131072;
        options.Limits.MaxBoxDepth = 16;
        options.Limits.MaxBoxCount = 512;
        options.Limits.MaxItemCount = 64;
        options.Limits.MaxPropertyCount = 256;
        options.Limits.MaxReferenceCount = 256;
        options.Limits.MaxTemporaryBytes = 65536;
        return options;
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
}
