using System.Buffers.Binary;
using System.Text;

namespace avif.Tests;

public class SingleImageAuthoringTests
{
    [Test]
    public void AuthorsColorAlphaAndIccWhilePreservingDecodedSamples()
    {
        byte[] iccBytes = BuildMinimalIccProfile();
        AvifIccProfile icc = AvifIccProfile.Create(iccBytes);
        AvifImage image = CreateColorAlphaImage();
        AvifColorInfo color = AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084,
            AvifMatrixCoefficients.Bt2020NonConstantLuminance,
            AvifRange.Limited);
        AvifResultCode colorCode = image.SetColorInfo(color);
        AvifResultCode iccCode = image.SetIccProfile(icc);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifImageItem? item = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        AvifItemProperty? nclx = FindColorProperty(item, "nclx");
        AvifItemProperty? profile = FindColorProperty(item, "prof");
        AvifDecodeResult decoded = Avif.Decode(finished.GetData());
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode readerParse = reader.Parse();
        AvifDecodeResult readerDecoded = reader.DecodeItem(itemId);

        Assert.Multiple(() =>
        {
            Assert.That(icc.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(colorCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(iccCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(nclx, Is.Not.Null);
            Assert.That(profile, Is.Not.Null);
            Assert.That((profile ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData().AsSpan(12).ToArray(),
                Is.EqualTo(iccBytes));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetAlphaMode(),
                Is.EqualTo(AvifAlphaMode.Straight));
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Bt2020));
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).TransferCharacteristics,
                Is.EqualTo(AvifTransferCharacteristics.Smpte2084));
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients,
                Is.EqualTo(AvifMatrixCoefficients.Bt2020NonConstantLuminance));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetRange(), Is.EqualTo(AvifRange.Limited));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetIccProfile(), Is.Not.Null);
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetIccProfile() ?? throw new InvalidOperationException("Invalid internal state.")).GetData(),
                Is.EqualTo(iccBytes));
            Assert.That(readerParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(readerDecoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((readerDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetIccProfile(), Is.Not.Null);
            Assert.That(((readerDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetIccProfile() ?? throw new InvalidOperationException("Invalid internal state.")).GetData(),
                Is.EqualTo(iccBytes));
        });
        AssertImagesEqual(image, decoded.GetImage());

        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (readerDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        image.Close();
        writer.Close();
    }

    [Test]
    public void TypedExifAndXmpAreCopiedAndLinkedToTheImage()
    {
        AvifExif exif = AvifExif.Create(BuildEmptyExif());
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(CreateXmpPacket()));
        byte[] expectedExif = exif.GetData();
        byte[] expectedXmp = xmp.GetData();
        AvifImage image = CreateMonochromeImage(31);
        AvifWriter writer = AvifWriter.Create();
        uint imageId = writer.AddImage(image);
        uint exifId = writer.AddExif(imageId, exif);
        uint xmpId = writer.AddXmp(imageId, xmp);
        exif.SetOrientation(6);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifImageItem? exifItem = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(exifId);
        AvifImageItem? xmpItem = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(xmpId);
        AvifReader reader = AvifReader.Create(finished.GetData());
        reader.Parse();

        Assert.Multiple(() =>
        {
            Assert.That(exifId, Is.EqualTo(2));
            Assert.That(xmpId, Is.EqualTo(3));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((exifItem ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("Exif"));
            Assert.That((xmpItem ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("mime"));
            Assert.That(xmpItem.GetContentType(),
                Is.EqualTo("application/rdf+xml"));
            Assert.That(exifItem.IsMetadataItem(), Is.True);
            Assert.That(xmpItem.IsMetadataItem(), Is.True);
            Assert.That((exifItem.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0),
                Is.EqualTo(imageId));
            Assert.That((xmpItem.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0),
                Is.EqualTo(imageId));
            Assert.That(reader.GetEncodedItem(exifId), Is.EqualTo(expectedExif));
            Assert.That(reader.GetEncodedItem(xmpId), Is.EqualTo(expectedXmp));
        });

        image.Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsTransformAndStaticHdrPropertiesInSemanticOrder()
    {
        AvifImage image = CreateMonochromeImage(47);
        AvifTransform transform = new AvifTransform();
        transform.SetCleanAperture(2, 1, 2, 1, -1, 1, -1, 1);
        transform.SetRotation(1);
        transform.SetMirror(0);
        transform.SetPixelAspectRatio(2, 2);
        AvifHdrMetadata hdr = new AvifHdrMetadata();
        AvifResultCode light = hdr.SetContentLightLevel(1000, 400);
        AvifResultCode mastering = hdr.SetMasteringDisplay(
            new int[] { 34000, 13250, 7500 },
            new int[] { 16000, 34500, 3000 },
            15635,
            16450,
            10000000,
            50);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        AvifResultCode transformCode = writer.SetTransform(itemId, transform);
        AvifResultCode hdrCode = writer.SetHdrMetadata(itemId, hdr);
        transform.ClearRotation();
        hdr.SetContentLightLevel(1, 1);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifImageItem? item = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        AvifTransform parsedTransform = (item ?? throw new InvalidOperationException("Invalid internal state.")).GetTransform();
        AvifHdrMetadata parsedHdr = item.GetHdrMetadata();
        AvifItemProperty? rotationProperty = FindProperty(item, "irot");

        Assert.Multiple(() =>
        {
            Assert.That(light, Is.EqualTo(AvifResultCode.Success));
            Assert.That(mastering, Is.EqualTo(AvifResultCode.Success));
            Assert.That(transformCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(hdrCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetPropertyTypes(item), Does.Contain("clap"));
            Assert.That(GetPropertyTypes(item), Does.Contain("irot"));
            Assert.That(GetPropertyTypes(item), Does.Contain("imir"));
            Assert.That(GetPropertyTypes(item), Does.Contain("pasp"));
            Assert.That(GetPropertyTypes(item).IndexOf("clap"),
                Is.LessThan(GetPropertyTypes(item).IndexOf("irot")));
            Assert.That(GetPropertyTypes(item).IndexOf("irot"),
                Is.LessThan(GetPropertyTypes(item).IndexOf("imir")));
            Assert.That(parsedTransform.GetRotationQuarterTurnsCounterClockwise(),
                Is.EqualTo(1));
            Assert.That(
                (rotationProperty ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetRotationDegrees(),
                Is.EqualTo(90));
            Assert.That(parsedTransform.GetMirrorAxis(), Is.Zero);
            Assert.That((parsedTransform.GetCleanAperture() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidthNumerator(),
                Is.EqualTo(2));
            Assert.That(parsedTransform.GetPixelAspectRatio().GetRatio(),
                Is.EqualTo(1));
            Assert.That((parsedHdr.GetContentLightLevel() ?? throw new InvalidOperationException("Invalid internal state."))
                .GetMaximumContentLightLevel(), Is.EqualTo(1000));
            Assert.That((parsedHdr.GetMasteringDisplay() ?? throw new InvalidOperationException("Invalid internal state.")).GetMaximumLuminance(),
                Is.EqualTo(10000000));
        });

        image.Close();
        writer.Close();
    }

    [Test]
    public void OpaquePropertiesAreDefensiveNonessentialAndStateBound()
    {
        byte[] payload = new byte[] { 9, 8, 7 };
        AvifOpaqueBox opaque = AvifOpaqueBox.Create("zzzz", payload, false);
        payload[0] = 0;
        AvifOpaqueBox essential = AvifOpaqueBox.Create(
            "yyyy", new byte[] { 1 }, true);
        AvifOpaqueBox known = AvifOpaqueBox.Create(
            "irot", new byte[] { 0 }, false);
        AvifImage image = CreateMonochromeImage(13);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        AvifResultCode added = writer.AddOpaqueProperty(itemId, opaque);
        AvifResultCode duplicate = writer.AddOpaqueProperty(itemId, opaque);
        AvifResultCode rejectedEssential =
            writer.AddOpaqueProperty(itemId, essential);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifItemProperty? parsed = FindProperty(
            (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId), "zzzz");
        AvifResultCode late = writer.AddOpaqueProperty(itemId, opaque);
        AvifHdrMetadata invalidHdr = new AvifHdrMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(opaque.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(known.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(added, Is.EqualTo(AvifResultCode.Success));
            Assert.That(duplicate, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(rejectedEssential, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.Not.Null);
            Assert.That((parsed ?? throw new InvalidOperationException("Invalid internal state.")).IsKnown(), Is.False);
            Assert.That(parsed.IsEssential(), Is.False);
            Assert.That(parsed.GetRawData().AsSpan(8).ToArray(),
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(late, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(invalidHdr.SetContentLightLevel(-1, 0),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidHdr.SetMasteringDisplay(
                    new int[] { 0, 0 },
                    new int[] { 0, 0 },
                    0,
                    0,
                    1,
                    1),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        image.Close();
        writer.Close();
    }

    static AvifImage CreateColorAlphaImage()
    {
        AvifImage image = AvifImage.Create(
            3,
            2,
            8,
            AvifChromaFormat.Yuv444,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.Straight,
            1,
            new AvifSecurityLimits());
        int kind = 0;
        while (kind <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? plane = image.GetPlane((AvifPlaneKind)kind);
            int row = 0;
            while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
            {
                Span<byte> span = plane.GetRowSpan(row);
                int index = 0;
                while (index < span.Length)
                {
                    span[index] = (byte)(kind * 40 + row * 7 + index);
                    index++;
                }
                row++;
            }
            kind++;
        }
        return image;
    }

    static AvifImage CreateMonochromeImage(byte value)
    {
        AvifImage image = AvifImage.Create(
            4,
            4,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertImagesEqual(AvifImage expected, AvifImage? actual)
    {
        int kind = 0;
        while (kind <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? expectedPlane = expected.GetPlane((AvifPlaneKind)kind);
            AvifPlane? actualPlane = (actual ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane((AvifPlaneKind)kind);
            Assert.That(actualPlane == null, Is.EqualTo(expectedPlane == null));
            if (expectedPlane != null)
            {
                int row = 0;
                while (row < expectedPlane.GetHeight())
                {
                    Assert.That((actualPlane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).ToArray(),
                        Is.EqualTo(expectedPlane.GetRowSpan(row).ToArray()));
                    row++;
                }
            }
            kind++;
        }
    }

    static AvifItemProperty? FindColorProperty(
        AvifImageItem? item,
        string colorType)
    {
        int index = 0;
        while (index < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(index);
            byte[] raw = (property ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();
            if (property.GetTypeName() == "colr" && raw.Length >= 12
                && Encoding.ASCII.GetString(raw, 8, 4) == colorType)
            {
                return property;
            }
            index++;
        }
        return null;
    }

    static AvifItemProperty? FindProperty(AvifImageItem? item, string type)
    {
        int index = 0;
        while (index < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            if ((item.GetProperty(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return item.GetProperty(index);
            }
            index++;
        }
        return null;
    }

    static List<string> GetPropertyTypes(AvifImageItem item)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            result.Add((item.GetProperty(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName());
            index++;
        }
        return result;
    }

    static byte[] BuildMinimalIccProfile()
    {
        byte[] data = new byte[132];
        BinaryPrimitives.WriteUInt32BigEndian(data, 132);
        data[8] = 4;
        data[9] = 64;
        WriteFourCc(data, 12, "mntr");
        WriteFourCc(data, 16, "RGB ");
        WriteFourCc(data, 20, "XYZ ");
        WriteFourCc(data, 36, "acsp");
        return data;
    }

    static byte[] BuildEmptyExif()
    {
        byte[] data = new byte[18];
        BinaryPrimitives.WriteUInt32BigEndian(data, 0);
        data[4] = 73;
        data[5] = 73;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6, 2), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 0);
        return data;
    }

    static string CreateXmpPacket()
    {
        return "<x:xmpmeta xmlns:x='adobe:ns:meta/' "
            + "xmlns:rdf='" + AvifXmp.RdfNamespace + "' "
            + "xmlns:dc='" + AvifXmp.DublinCoreNamespace + "'>"
            + "<rdf:RDF><rdf:Description rdf:about='' "
            + "dc:format='image/avif'/></rdf:RDF></x:xmpmeta>";
    }

    static void WriteFourCc(byte[] data, int offset, string value)
    {
        data[offset] = (byte)value[0];
        data[offset + 1] = (byte)value[1];
        data[offset + 2] = (byte)value[2];
        data[offset + 3] = (byte)value[3];
    }
}
