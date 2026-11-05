using System.Text;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffVendorMetadataTests
{
    [Test]
    public void ReadVendorMetadata_RecognizesAperioFromImageDescription()
    {
        TiffDirectory directory = CreateAsciiDirectory(TiffTagId.ImageDescription, "Aperio Image Library v12|AppMag = 20|MPP = 0.25", out byte[] fileBytes);

        TiffVendorMetadataReadResult result = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasVendorMetadata, Is.True);
        Assert.That(result.Metadata.PrimaryVendor, Is.EqualTo("Aperio"));
        Assert.That(result.Metadata.PrimaryFormat, Is.EqualTo("SVS"));
        Assert.That(result.Metadata.Matches[0].Source, Is.EqualTo("ImageDescription"));
        Assert.That(result.Metadata.Matches[0].IsSlide, Is.True);
    }

    [TestCase("Hamamatsu NDPI metadata block", "Hamamatsu", "NDPI", true, false)]
    [TestCase("Leica SCN scanner metadata", "Leica", "SCN", true, false)]
    [TestCase("Philips PIM_DP_SCANNED_IMAGES", "Philips", "Philips TIFF", true, false)]
    [TestCase("Roche Ventana BIF slide", "Roche/Ventana", "BIF", true, false)]
    [TestCase("Carl Zeiss LSM 710", "Zeiss", "LSM", false, true)]
    [TestCase("Micro-Manager Metadata", "Micro-Manager", "Micro-Manager", false, true)]
    [TestCase("SIMPLE  = T / FITS-compatible Astro-TIFF", "Astro-TIFF", "Astro-TIFF", false, true)]
    public void ReadVendorMetadata_RecognizesTextPatternsFromSoftware(string text, string vendor, string format, bool isSlide, bool isScientific)
    {
        TiffDirectory directory = CreateAsciiDirectory(TiffTagId.Software, text, out byte[] fileBytes);

        TiffVendorMetadataReadResult result = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.MatchCount(), Is.EqualTo(1));
        Assert.That(result.Metadata.Matches[0].Vendor, Is.EqualTo(vendor));
        Assert.That(result.Metadata.Matches[0].Format, Is.EqualTo(format));
        Assert.That(result.Metadata.Matches[0].Source, Is.EqualTo("Software"));
        Assert.That(result.Metadata.Matches[0].IsSlide, Is.EqualTo(isSlide));
        Assert.That(result.Metadata.Matches[0].IsScientific, Is.EqualTo(isScientific));
    }

    [Test]
    public void ReadVendorMetadata_RecognizesPhilipsFromXmp()
    {
        string xmp = "<x:xmpmeta><rdf:Description>Philips digital pathology TIFF</rdf:Description></x:xmpmeta>";
        TiffDirectory directory = CreateRawDirectory(TiffTagId.Xmp, TiffDataType.Byte, Encoding.UTF8.GetBytes(xmp), out byte[] fileBytes);

        TiffVendorMetadataReadResult result = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.PrimaryVendor, Is.EqualTo("Philips"));
        Assert.That(result.Metadata.Matches[0].Source, Is.EqualTo("XMP"));
    }

    [Test]
    public void ReadVendorMetadata_RecognizesDngAndZeissKnownTags()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.DngVersion, TiffDataType.Byte, 4, 0),
            CreateEntry(TiffTagId.CzLsmInfo, TiffDataType.Undefined, 8, 0)
        });

        TiffVendorMetadataReadResult result = TiffCodec.ReadVendorMetadata(directory, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.MatchCount(), Is.EqualTo(2));
        Assert.That(result.Metadata.Matches[0].Vendor, Is.EqualTo("Zeiss"));
        Assert.That(result.Metadata.Matches[0].Evidence, Is.EqualTo("CZ_LSMInfo"));
        Assert.That(result.Metadata.Matches[1].Vendor, Is.EqualTo("Adobe"));
        Assert.That(result.Metadata.Matches[1].Format, Is.EqualTo("DNG"));
        Assert.That(result.Metadata.Matches[1].Evidence, Is.EqualTo("DNGVersion"));
    }

    [Test]
    public void ReadVendorMetadata_ReturnsEmptyWhenNoVendorEvidenceExists()
    {
        TiffDirectory directory = CreateAsciiDirectory(TiffTagId.Software, "plain encoder", out byte[] fileBytes);

        TiffVendorMetadataReadResult result = TiffCodec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasVendorMetadata, Is.False);
        Assert.That(result.Metadata.MatchCount(), Is.EqualTo(0));
    }

    [Test]
    public void DefaultRegistry_ContainsVendorRecognitionTags()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        Assert.That(registry.Contains(TiffTagId.CzLsmInfo), Is.True);
        Assert.That(registry.Contains(TiffTagId.DngVersion), Is.True);
        Assert.That(registry.Contains(TiffTagId.UniqueCameraModel), Is.True);
    }

    static TiffDirectory CreateAsciiDirectory(int tagId, string text, out byte[] fileBytes)
    {
        byte[] bytes = TiffAscii.EncodeString(text);
        return CreateRawDirectory(tagId, TiffDataType.Ascii, bytes, out fileBytes);
    }

    static TiffDirectory CreateRawDirectory(int tagId, TiffDataType dataType, byte[] valueBytes, out byte[] fileBytes)
    {
        int offset = 64;
        fileBytes = new byte[offset + valueBytes.Length + 8];
        int index = 0;
        while (index < valueBytes.Length)
        {
            fileBytes[offset + index] = valueBytes[index];
            index = index + 1;
        }

        return TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(tagId, dataType, valueBytes.Length, (ulong)offset) });
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
