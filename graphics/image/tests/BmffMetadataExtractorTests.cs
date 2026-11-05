using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BmffMetadataExtractorTests
{
    [Test]
    public void AddFrames_ClassifiesDepthAuxiliaryAndUnrelatedPageItems()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        BmffMetadataState state = new BmffMetadataState();
        BmffItem primary = new BmffItem();
        primary.Id = 1;
        primary.Primary = true;
        primary.Type = "av01";
        primary.Width = 100;
        primary.Height = 80;
        BmffItem depth = new BmffItem();
        depth.Id = 2;
        depth.Auxiliary = true;
        depth.AuxiliaryType = "urn:example:depth";
        depth.Type = "av01";
        depth.Width = 100;
        depth.Height = 80;
        BmffItem page = new BmffItem();
        page.Id = 3;
        page.Type = "av01";
        page.Width = 50;
        page.Height = 40;
        state.PrimaryItemId = 1;
        state.Items.Add(primary);
        state.Items.Add(depth);
        state.Items.Add(page);
        BmffItemReference auxiliaryReference = new BmffItemReference();
        auxiliaryReference.Type = "auxl";
        auxiliaryReference.FromItemId = 2;
        auxiliaryReference.ToItemIds.Add(1);
        state.References.Add(auxiliaryReference);

        BmffMetadataExtractor.AddFrames(document, state, new ImageMetadataReaderOptions());

        Assert.That(document.Frames, Has.Count.EqualTo(3));
        Assert.That(document.Frames[1].Role, Is.EqualTo(ImageMetadataRole.AuxiliaryDepth));
        Assert.That(document.Frames[1].ParentFrameIndex, Is.Zero);
        Assert.That(document.Frames[2].Role, Is.EqualTo(ImageMetadataRole.Page));
    }

    [Test]
    public void Parse_ExtractsItemsPropertiesRelationshipsAndEmbeddedMetadata()
    {
        byte[] bytes = BuildCompleteAvif();
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.Items.Count").Value.IntegerValue, Is.EqualTo(6));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.Items.PrimaryItemId").Value.UnsignedIntegerValue, Is.EqualTo(1));
        List<MetadataTag> ids = new ImageMetadataQuery().Find(document, "Bmff.Item.Id");
        Assert.That(ids, Has.Count.EqualTo(6));
        Assert.That(ids[0].Value.UnsignedIntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Width").Value.IntegerValue, Is.EqualTo(640));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Height").Value.IntegerValue, Is.EqualTo(480));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Rotation Degrees Counter-Clockwise").Value.IntegerValue, Is.EqualTo(90));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Mirror Axis").DisplayValue, Is.EqualTo("Vertical"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Orientation").Value.IntegerValue, Is.EqualTo((int)ImageOrientation.MirrorHorizontalRotate270));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Clean Aperture Width").Value.Numerator, Is.EqualTo(620));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Color Type").Value.Text, Is.EqualTo("nclx"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Color Primaries").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/1/Full Range").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/2/Thumbnail").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.Item.Reference.thmb").Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedIntegerArray));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.Item.Reference.thmb").Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 1 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/3/Auxiliary").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "BMFF/Items/3/Auxiliary Type").Value.Text, Does.Contain("alpha"));
        Assert.That(document.Frames, Has.Count.EqualTo(3));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(10));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.YCbCr));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.MirrorHorizontalRotate270));
        Assert.That(document.Frames[0].Role, Is.EqualTo(ImageMetadataRole.Primary));
        Assert.That(document.Frames[0].ContainerItemId, Is.EqualTo(1));
        Assert.That(document.Frames[0].ParentFrameIndex, Is.EqualTo(-1));
        Assert.That(document.Frames[1].Width, Is.EqualTo(160));
        Assert.That(document.Frames[1].Role, Is.EqualTo(ImageMetadataRole.Thumbnail));
        Assert.That(document.Frames[1].ContainerItemId, Is.EqualTo(2));
        Assert.That(document.Frames[1].ParentFrameIndex, Is.Zero);
        Assert.That(document.Frames[2].HasAlpha, Is.True);
        Assert.That(document.Frames[2].Role, Is.EqualTo(ImageMetadataRole.AuxiliaryAlpha));
        Assert.That(document.Frames[2].ContainerItemId, Is.EqualTo(3));
        Assert.That(document.Frames[2].ParentFrameIndex, Is.Zero);
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(3));
        Assert.That(document.Summary.Orientation, Is.EqualTo(ImageOrientation.MirrorHorizontalRotate270));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Exif && profile.Data.Length == 8));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp && profile.Text.Contains("x:xmpmeta")));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Jumbf && profile.Data.Length == 4));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Thumbnail && profile.Data.SequenceEqual(new byte[] { 9, 8, 7 })));
        MetadataProfile exifProfile = document.Profiles.Find(profile => profile.Kind == MetadataProfileKind.Exif) ?? throw new InvalidOperationException("Expected EXIF profile was not found.");
        MetadataProfile xmpProfile = document.Profiles.Find(profile => profile.Kind == MetadataProfileKind.Xmp) ?? throw new InvalidOperationException("Expected XMP profile was not found.");
        MetadataProfile thumbnailProfile = document.Profiles.Find(profile => profile.Kind == MetadataProfileKind.Thumbnail) ?? throw new InvalidOperationException("Expected thumbnail profile was not found.");
        Assert.That(exifProfile.ContainerItemId, Is.EqualTo(4));
        Assert.That(exifProfile.SourceFrameIndex, Is.Zero);
        Assert.That(xmpProfile.ContainerItemId, Is.EqualTo(5));
        Assert.That(xmpProfile.SourceFrameIndex, Is.Zero);
        Assert.That(thumbnailProfile.ContainerItemId, Is.EqualTo(2));
        Assert.That(thumbnailProfile.SourceFrameIndex, Is.Zero);
        Assert.That(thumbnailProfile.Role, Is.EqualTo(ImageMetadataRole.Thumbnail));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.Exif.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(block => block.Name == "EXIF Item"));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(block => block.Name == "XMP Item"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_PreservesIccPropertyAndHonorsParseIccOption()
    {
        byte[] icc = IccProfileParserTests.CreateProfile(false);
        byte[] bytes = BuildSingleImageWithProperty(CreateBox("colr", Join(Encoding.ASCII.GetBytes("prof"), icc)));

        ImageMetadataDocument parsed = Parse(bytes, new ImageMetadataReaderOptions());
        Assert.That(parsed.Profiles, Has.Count.EqualTo(1));
        Assert.That(parsed.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(parsed.Profiles[0].Data, Is.EqualTo(icc));
        Assert.That(parsed.Summary.HasColorProfile, Is.True);
        Assert.That(parsed.Summary.ColorProfileName, Is.EqualTo("Studio Display"));
        Assert.That(parsed.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));

        ImageMetadataReaderOptions disabledOptions = new ImageMetadataReaderOptions();
        disabledOptions.ParseIcc = false;
        ImageMetadataDocument disabled = Parse(bytes, disabledOptions);
        Assert.That(disabled.Profiles, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(disabled, "Bmff.Item.ColorType").Value.Text, Is.EqualTo("prof"));
        Assert.That(disabled.Summary.HasColorProfile, Is.True);
    }

    [Test]
    public void Parse_NormalizesDisplayP3FromNclxPrimaries()
    {
        byte[] bytes = BuildSingleImageWithProperty(CreateNclx(12, 13, 0, true));

        ImageMetadataDocument document = Parse(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
    }

    [Test]
    public void Parse_DetectsTopLevelC2paJumbfAndCapturesRawRange()
    {
        byte[] ftyp = CreateFtyp();
        byte[] jumd = CreateBox("jumd", Encoding.ASCII.GetBytes("urn:c2pa:manifest"));
        byte[] jumb = CreateBox("jumb", jumd);

        ImageMetadataDocument document = Parse(Join(ftyp, jumb), new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.C2pa));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(ftyp.Length));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(jumb));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("jumb"));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
    }

    [Test]
    public void Parse_UsesFileAndIdatConstructionMethodsAndConcatenatesExtents()
    {
        byte[] first = Encoding.ASCII.GetBytes("<x:xmpmeta>");
        byte[] second = Encoding.ASCII.GetBytes("</x:xmpmeta>");
        byte[] ftyp = CreateFtyp();
        byte[] mdat = CreateBox("mdat", Join(first, second));
        int firstAbsolute = ftyp.Length + 8;
        int secondAbsolute = firstAbsolute + first.Length;
        byte[] infe = CreateInfe(5, "mime", "XMP", "application/rdf+xml");
        byte[] iinf = CreateIinf(new byte[][] { infe });
        byte[] iloc = CreateIlocVersionOne(new IlocItem[]
        {
            new IlocItem(5, 0, new uint[] { (uint)firstAbsolute, (uint)secondAbsolute }, new uint[] { (uint)first.Length, (uint)second.Length })
        });
        byte[] meta = CreateMeta(new byte[][] { CreatePitm(5), iinf, iloc });

        ImageMetadataDocument document = Parse(Join(ftyp, mdat, meta), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        MetadataProfile xmp = document.Profiles.Find(profile => profile.Kind == MetadataProfileKind.Xmp) ?? throw new InvalidOperationException("Expected XMP profile was not found.");
        Assert.That(xmp.Text, Is.EqualTo("<x:xmpmeta></x:xmpmeta>"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.Item.ExtentCount").Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ReportsMalformedLocationsAssociationsAndProperties()
    {
        byte[] infe = CreateInfe(1, "av01", "Primary", "");
        byte[] iinf = CreateIinf(new byte[][] { infe });
        byte[] badIloc = CreateBox("iloc", new byte[] { 1, 0, 0, 0, 148, 0, 0, 1 });
        byte[] badIspe = CreateBox("ispe", new byte[] { 0, 0, 0, 0, 0 });
        byte[] ipco = CreateBox("ipco", badIspe);
        byte[] badIpma = CreateBox("ipma", new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1 });
        byte[] iprp = CreateBox("iprp", Join(ipco, badIpma));
        byte[] meta = CreateMeta(new byte[][] { CreatePitm(1), iinf, badIloc, iprp });

        ImageMetadataDocument document = Parse(Join(CreateFtyp(), meta), new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "bmff.iloc.field-size.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "bmff.ispe.truncated", MetadataSeverity.Truncated);
        AssertDiagnostic(document, "bmff.ipma.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_RespectsFrameProfileXmlAndRawCaptureLimits()
    {
        byte[] bytes = BuildCompleteAvif();
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 1;
        options.MaxXmlBytes = 4;
        options.CaptureRawBlocks = false;

        ImageMetadataDocument document = Parse(bytes, options);

        AssertDiagnostic(document, "bmff.frame.count.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(document, "bmff.xmp.profile.limit", MetadataSeverity.LimitExceeded);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        MetadataProfile limitedXmp = document.Profiles.Find(profile => profile.Kind == MetadataProfileKind.Xmp) ?? throw new InvalidOperationException("Expected limited XMP profile was not found.");
        Assert.That(limitedXmp.Data, Is.Empty);
        Assert.That(limitedXmp.Length, Is.GreaterThan(4));
        Assert.That(document.RawBlocks, Is.Empty);
    }

    public static byte[] BuildCompleteAvif()
    {
        byte[] primary = new byte[] { 1, 2, 3, 4 };
        byte[] thumbnail = new byte[] { 9, 8, 7 };
        byte[] alpha = new byte[] { 5, 6 };
        byte[] exif = new byte[] { 0, 0, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0 };
        byte[] xmp = Encoding.UTF8.GetBytes("<x:xmpmeta></x:xmpmeta>");
        byte[] jumbf = new byte[] { 0, 0, 0, 4 };
        byte[][] data = new byte[][] { primary, thumbnail, alpha, exif, xmp, jumbf };
        List<byte> idatData = new List<byte>();
        uint[] offsets = new uint[data.Length];
        uint[] lengths = new uint[data.Length];
        int dataIndex = 0;
        while (dataIndex < data.Length)
        {
            offsets[dataIndex] = (uint)idatData.Count;
            lengths[dataIndex] = (uint)data[dataIndex].Length;
            AddBytes(idatData, data[dataIndex]);
            dataIndex = dataIndex + 1;
        }

        byte[][] entries = new byte[][]
        {
            CreateInfe(1, "av01", "Primary", ""),
            CreateInfe(2, "av01", "Thumbnail", ""),
            CreateInfe(3, "av01", "Alpha", ""),
            CreateInfe(4, "Exif", "Exif", ""),
            CreateInfe(5, "mime", "XMP", "application/rdf+xml"),
            CreateInfe(6, "jumb", "JUMBF", "")
        };
        IlocItem[] locations = new IlocItem[data.Length];
        int locationIndex = 0;
        while (locationIndex < data.Length)
        {
            locations[locationIndex] = new IlocItem((uint)(locationIndex + 1), 1, new uint[] { offsets[locationIndex] }, new uint[] { lengths[locationIndex] });
            locationIndex = locationIndex + 1;
        }

        List<byte> properties = new List<byte>();
        AddBytes(properties, CreateIspe(640, 480));
        AddBytes(properties, CreatePixi(new byte[] { 10, 10, 10 }));
        AddBytes(properties, CreateNclx(1, 13, 6, true));
        AddBytes(properties, CreateBox("irot", new byte[] { 1 }));
        AddBytes(properties, CreateBox("imir", new byte[] { 0 }));
        AddBytes(properties, CreateClap(620, 1, 460, 1, 0, 1, 0, 1));
        AddBytes(properties, CreateIspe(160, 120));
        AddBytes(properties, CreateIspe(640, 480));
        AddBytes(properties, CreateAuxC("urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
        byte[] ipco = CreateBox("ipco", properties.ToArray());
        byte[] ipma = CreateIpma(new IpmaItem[]
        {
            new IpmaItem(1, new int[] { 1, 2, 3, 4, 5, 6 }),
            new IpmaItem(2, new int[] { 7 }),
            new IpmaItem(3, new int[] { 8, 9 })
        });
        byte[] iprp = CreateBox("iprp", Join(ipco, ipma));
        byte[] iref = CreateIref(new byte[][]
        {
            CreateReference("thmb", 2, new uint[] { 1 }),
            CreateReference("auxl", 3, new uint[] { 1 }),
            CreateReference("cdsc", 4, new uint[] { 1 }),
            CreateReference("cdsc", 5, new uint[] { 1 })
        });
        byte[] meta = CreateMeta(new byte[][]
        {
            CreatePitm(1),
            CreateIinf(entries),
            CreateIlocVersionOne(locations),
            iprp,
            iref,
            CreateBox("idat", idatData.ToArray())
        });
        return Join(CreateFtyp(), meta);
    }

    public static byte[] BuildSingleImageWithProperty(byte[] additionalProperty)
    {
        byte[] data = new byte[] { 1 };
        byte[] infe = CreateInfe(1, "av01", "Primary", "");
        byte[] iinf = CreateIinf(new byte[][] { infe });
        byte[] iloc = CreateIlocVersionOne(new IlocItem[]
        {
            new IlocItem(1, 1, new uint[] { 0 }, new uint[] { 1 })
        });
        byte[] ipco = CreateBox("ipco", Join(CreateIspe(20, 10), additionalProperty));
        byte[] ipma = CreateIpma(new IpmaItem[] { new IpmaItem(1, new int[] { 1, 2 }) });
        byte[] iprp = CreateBox("iprp", Join(ipco, ipma));
        byte[] meta = CreateMeta(new byte[][] { CreatePitm(1), iinf, iloc, iprp, CreateBox("idat", data) });
        return Join(CreateFtyp(), meta);
    }

    public static byte[] CreateFtyp()
    {
        return CreateBox("ftyp", Join(Encoding.ASCII.GetBytes("avif"), new byte[] { 0, 0, 0, 0, 109, 105, 102, 49 }));
    }

    public static byte[] CreateMeta(byte[][] children)
    {
        List<byte> payload = new List<byte>();
        AddBytes(payload, new byte[] { 0, 0, 0, 0 });
        foreach (byte[] child in children)
        {
            AddBytes(payload, child);
        }

        return CreateBox("meta", payload.ToArray());
    }

    public static byte[] CreatePitm(uint itemId)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0 });
        AddUnsigned16(payload, itemId);
        return CreateBox("pitm", payload.ToArray());
    }

    public static byte[] CreateIinf(byte[][] entries)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0 });
        AddUnsigned16(payload, (uint)entries.Length);
        foreach (byte[] entry in entries)
        {
            AddBytes(payload, entry);
        }

        return CreateBox("iinf", payload.ToArray());
    }

    public static byte[] CreateInfe(uint itemId, string type, string name, string contentType)
    {
        List<byte> payload = new List<byte>(new byte[] { 2, 0, 0, 0 });
        AddUnsigned16(payload, itemId);
        AddUnsigned16(payload, 0);
        AddAscii(payload, type);
        AddAscii(payload, name);
        payload.Add(0);
        if (type == "mime")
        {
            AddAscii(payload, contentType);
            payload.Add(0);
        }

        return CreateBox("infe", payload.ToArray());
    }

    public static byte[] CreateIlocVersionOne(IlocItem[] items)
    {
        List<byte> payload = new List<byte>(new byte[] { 1, 0, 0, 0, 68, 64 });
        AddUnsigned16(payload, (uint)items.Length);
        foreach (IlocItem item in items)
        {
            AddUnsigned16(payload, item.Id);
            AddUnsigned16(payload, (uint)item.ConstructionMethod);
            AddUnsigned16(payload, 0);
            AddUnsigned32(payload, 0);
            AddUnsigned16(payload, (uint)item.Offsets.Length);
            int index = 0;
            while (index < item.Offsets.Length)
            {
                AddUnsigned32(payload, item.Offsets[index]);
                AddUnsigned32(payload, item.Lengths[index]);
                index = index + 1;
            }
        }

        return CreateBox("iloc", payload.ToArray());
    }

    public static byte[] CreateIspe(uint width, uint height)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0 });
        AddUnsigned32(payload, width);
        AddUnsigned32(payload, height);
        return CreateBox("ispe", payload.ToArray());
    }

    public static byte[] CreatePixi(byte[] bits)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0, (byte)bits.Length });
        AddBytes(payload, bits);
        return CreateBox("pixi", payload.ToArray());
    }

    public static byte[] CreateNclx(uint primaries, uint transfer, uint matrix, bool fullRange)
    {
        List<byte> payload = new List<byte>();
        AddAscii(payload, "nclx");
        AddUnsigned16(payload, primaries);
        AddUnsigned16(payload, transfer);
        AddUnsigned16(payload, matrix);
        payload.Add(fullRange ? (byte)128 : (byte)0);
        return CreateBox("colr", payload.ToArray());
    }

    public static byte[] CreateClap(uint widthNumerator, uint widthDenominator, uint heightNumerator, uint heightDenominator, int horizontalNumerator, uint horizontalDenominator, int verticalNumerator, uint verticalDenominator)
    {
        List<byte> payload = new List<byte>();
        AddUnsigned32(payload, widthNumerator);
        AddUnsigned32(payload, widthDenominator);
        AddUnsigned32(payload, heightNumerator);
        AddUnsigned32(payload, heightDenominator);
        AddUnsigned32(payload, IntegerBitPattern.ToUnsigned32(horizontalNumerator));
        AddUnsigned32(payload, horizontalDenominator);
        AddUnsigned32(payload, IntegerBitPattern.ToUnsigned32(verticalNumerator));
        AddUnsigned32(payload, verticalDenominator);
        return CreateBox("clap", payload.ToArray());
    }

    public static byte[] CreateAuxC(string auxiliaryType)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0 });
        AddAscii(payload, auxiliaryType);
        payload.Add(0);
        return CreateBox("auxC", payload.ToArray());
    }

    public static byte[] CreateIpma(IpmaItem[] items)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0 });
        AddUnsigned32(payload, (uint)items.Length);
        foreach (IpmaItem item in items)
        {
            AddUnsigned16(payload, item.Id);
            payload.Add((byte)item.PropertyIndexes.Length);
            foreach (int index in item.PropertyIndexes)
            {
                payload.Add((byte)index);
            }
        }

        return CreateBox("ipma", payload.ToArray());
    }

    public static byte[] CreateIref(byte[][] references)
    {
        List<byte> payload = new List<byte>(new byte[] { 0, 0, 0, 0 });
        foreach (byte[] reference in references)
        {
            AddBytes(payload, reference);
        }

        return CreateBox("iref", payload.ToArray());
    }

    public static byte[] CreateReference(string type, uint fromId, uint[] toIds)
    {
        List<byte> payload = new List<byte>();
        AddUnsigned16(payload, fromId);
        AddUnsigned16(payload, (uint)toIds.Length);
        foreach (uint toId in toIds)
        {
            AddUnsigned16(payload, toId);
        }

        return CreateBox(type, payload.ToArray());
    }

    public static byte[] CreateBox(string type, byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        AddUnsigned32(bytes, (uint)(8 + payload.Length));
        AddAscii(bytes, type);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] Join(byte[] first, byte[] second)
    {
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, first);
        AddBytes(bytes, second);
        return bytes.ToArray();
    }

    public static byte[] Join(byte[] first, byte[] second, byte[] third)
    {
        return Join(Join(first, second), third);
    }

    public static void AddUnsigned16(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        foreach (char value in text)
        {
            bytes.Add((byte)value);
        }
    }

    public static void AddBytes(List<byte> bytes, byte[] values)
    {
        foreach (byte value in values)
        {
            bytes.Add(value);
        }
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageMetadataReaderOptions options)
    {
        MemoryStream stream = new MemoryStream(bytes);
        return new ImageMetadataReader().ReadStreamWithOptions(stream, options);
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }

    public class IlocItem
    {
        public uint Id;
        public int ConstructionMethod;
        public uint[] Offsets;
        public uint[] Lengths;

        public IlocItem(uint id, int constructionMethod, uint[] offsets, uint[] lengths)
        {
            Id = id;
            ConstructionMethod = constructionMethod;
            Offsets = offsets;
            Lengths = lengths;
        }
    }

    public class IpmaItem
    {
        public uint Id;
        public int[] PropertyIndexes;

        public IpmaItem(uint id, int[] propertyIndexes)
        {
            Id = id;
            PropertyIndexes = propertyIndexes;
        }
    }
}
