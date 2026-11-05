using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffScientificSlideMetadataRecognitionTests
{
    [Test]
    public void ScientificRecognition_CombinesOmeMicroManagerDngAndAstroEvidence()
    {
        byte[] fileBytes = new byte[512];
        string omeXml = "<?xml version=\"1.0\"?><OME Creator=\"Micro-Manager\"><Image ID=\"Image:0\" /></OME>";
        byte[] omeBytes = WriteAscii(fileBytes, 64, omeXml);
        byte[] softwareBytes = WriteAscii(fileBytes, 224, "PixInsight FITS Astro-TIFF export");
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, omeBytes.Length, 64),
            CreateEntry(TiffTagId.Software, TiffDataType.Ascii, softwareBytes.Length, 224),
            CreateEntry(TiffTagId.DngVersion, TiffDataType.Byte, 4, 0)
        });

        TiffOmeMetadataReadResult ome = TiffCodec.ReadOmeMetadata(directory, fileBytes);
        TiffVendorMetadataReadResult vendor = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(ome.Success, Is.True);
        Assert.That(ome.Metadata.HasOmeXml, Is.True);
        Assert.That(ome.Metadata.Xml, Does.Contain("<OME"));
        Assert.That(vendor.Success, Is.True);
        Assert.That(ContainsVendor(vendor.Metadata, "Micro-Manager", "Micro-Manager"), Is.True);
        Assert.That(ContainsVendor(vendor.Metadata, "Astro-TIFF", "Astro-TIFF"), Is.True);
        Assert.That(ContainsVendor(vendor.Metadata, "Adobe", "DNG"), Is.True);
        Assert.That(AllMatchesAreScientific(vendor.Metadata), Is.True);
    }

    [Test]
    public void ScientificRecognition_RecognizesImageJHyperstackAndZeissLsmTag()
    {
        byte[] fileBytes = new byte[256];
        string imageJ = "ImageJ=1.54f\nimages=12\nchannels=2\nslices=3\nframes=2\nhyperstack=true\nunit=micron\n";
        byte[] descriptionBytes = WriteAscii(fileBytes, 64, imageJ);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, descriptionBytes.Length, 64),
            CreateEntry(TiffTagId.CzLsmInfo, TiffDataType.Undefined, 8, 0)
        });

        TiffImageJMetadataReadResult imageJResult = TiffCodec.ReadImageJMetadata(directory, fileBytes);
        TiffVendorMetadataReadResult vendor = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(imageJResult.Success, Is.True);
        Assert.That(imageJResult.Metadata.HasImageJ, Is.True);
        Assert.That(imageJResult.Metadata.IsHyperstack, Is.True);
        Assert.That(imageJResult.Metadata.ImageCount, Is.EqualTo(12));
        Assert.That(imageJResult.Metadata.Channels, Is.EqualTo(2));
        Assert.That(ContainsVendor(vendor.Metadata, "Zeiss", "LSM"), Is.True);
        Assert.That(vendor.Metadata.Matches[0].IsScientific, Is.True);
    }

    [Test]
    public void SlideRecognition_CombinesAperioVendorPyramidAndTileLookup()
    {
        byte[] fileBytes = new byte[256];
        byte[] descriptionBytes = WriteAscii(fileBytes, 64, "Aperio Image Library v12|AppMag = 20|MPP = 0.25");
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, descriptionBytes.Length, 64),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4096),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2048)
        });
        TiffSubIfd[] subIfds = new TiffSubIfd[]
        {
            TiffSubIfd.Create(0, 8, 0, 1, 128, new TiffDirectory[]
            {
                CreateImageDirectory(1, 1024, 512, true)
            })
        };
        TiffImageInfo tileInfo = CreateTileInfo(4096, 2048, 1024, 512);

        TiffVendorMetadataReadResult vendor = TiffCodec.ReadVendorMetadata(parent, fileBytes);
        TiffPyramidDetectionResult pyramid = TiffCodec.DetectPyramid(parent, subIfds, fileBytes, TiffByteOrder.LittleEndian);
        TiffTileLookupResult tile = TiffCodec.LookupTile(tileInfo, CreateSequence(16, 1000, 100), CreateSequence(16, 50, 1), 4095, 2047, 0);

        Assert.That(vendor.Success, Is.True);
        Assert.That(vendor.Metadata.PrimaryVendor, Is.EqualTo("Aperio"));
        Assert.That(vendor.Metadata.Matches[0].IsSlide, Is.True);
        Assert.That(pyramid.Success, Is.True);
        Assert.That(pyramid.Pyramid.HasPyramid, Is.True);
        Assert.That(pyramid.Pyramid.Levels[0].Width, Is.EqualTo(1024));
        Assert.That(tile.Success, Is.True);
        Assert.That(tile.Lookup.TileIndex, Is.EqualTo(15));
        Assert.That(tile.Lookup.TileColumn, Is.EqualTo(3));
        Assert.That(tile.Lookup.TileRow, Is.EqualTo(3));
    }

    [TestCase("Hamamatsu NDPI focal plane metadata", "Hamamatsu", "NDPI")]
    [TestCase("Leica SCN macro image metadata", "Leica", "SCN")]
    [TestCase("Philips PIM_DP_SCANNED_IMAGES slide metadata", "Philips", "Philips TIFF")]
    [TestCase("Roche Ventana BIF barcode metadata", "Roche/Ventana", "BIF")]
    public void SlideRecognition_CoversMajorWholeSlideVendorEvidence(string softwareText, string vendor, string format)
    {
        byte[] fileBytes = new byte[256];
        byte[] softwareBytes = WriteAscii(fileBytes, 64, softwareText);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.Software, TiffDataType.Ascii, softwareBytes.Length, 64)
        });

        TiffVendorMetadataReadResult result = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.PrimaryVendor, Is.EqualTo(vendor));
        Assert.That(result.Metadata.PrimaryFormat, Is.EqualTo(format));
        Assert.That(result.Metadata.Matches[0].IsSlide, Is.True);
        Assert.That(result.Metadata.Matches[0].IsScientific, Is.False);
    }

    static bool ContainsVendor(TiffVendorMetadata metadata, string vendor, string format)
    {
        int index = 0;
        while (index < metadata.Matches.Length)
        {
            if (metadata.Matches[index].Vendor == vendor && metadata.Matches[index].Format == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool AllMatchesAreScientific(TiffVendorMetadata metadata)
    {
        int index = 0;
        while (index < metadata.Matches.Length)
        {
            if (!metadata.Matches[index].IsScientific)
            {
                return false;
            }

            index = index + 1;
        }

        return metadata.Matches.Length > 0;
    }

    static byte[] WriteAscii(byte[] fileBytes, int offset, string text)
    {
        byte[] bytes = TiffAscii.EncodeString(text);
        int index = 0;
        while (index < bytes.Length)
        {
            fileBytes[offset + index] = bytes[index];
            index = index + 1;
        }

        return bytes;
    }

    static TiffDirectory CreateImageDirectory(int imageIndex, uint width, uint length, bool reduced)
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[2];
        if (reduced)
        {
            entries = new TiffIfdEntry[3];
        }

        entries[0] = CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width);
        entries[1] = CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length);
        if (reduced)
        {
            entries[2] = CreateEntry(TiffTagId.NewSubfileType, TiffDataType.Long, 1, 1);
        }

        return TiffDirectory.Create(imageIndex, (ulong)(8 + (imageIndex * 32)), 0, entries);
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, ulong tileWidth, ulong tileLength)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.IsTiled = true;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
        return info;
    }

    static ulong[] CreateSequence(int count, ulong start, ulong step)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = start + ((ulong)index * step);
            index = index + 1;
        }

        return values;
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
