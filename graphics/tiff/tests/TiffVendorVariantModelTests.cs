using System.Text;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffVendorVariantModelTests
{
    [Test]
    public void ReadVendorMetadata_ExposesTypedDngIdentityFieldsAndLosslessValues()
    {
        byte[] privateData = new byte[] { 9, 8, 7, 6, 5 };
        byte[] uniqueId = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] originalRaw = new byte[] { 21, 22, 23, 24, 25, 26 };
        byte[] localized = Encoding.UTF8.GetBytes("Cámara");
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 1),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 1),
            TiffTagValue.CreateUnsignedArray(TiffTagId.DngVersion, TiffDataType.Byte, new ulong[] { 1, 7, 1, 0 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.DngBackwardVersion, TiffDataType.Byte, new ulong[] { 1, 4, 0, 0 }),
            TiffTagValue.CreateAscii(TiffTagId.UniqueCameraModel, "Example Raw Camera"),
            TiffTagValue.CreateUnsignedArray(TiffTagId.LocalizedCameraModel, TiffDataType.Byte, ToUnsigned(localized)),
            TiffTagValue.CreateRationalArray(TiffTagId.BlackLevel, TiffDataType.Rational, new TiffRational[] { TiffRational.Create(64, 1), TiffRational.Create(65, 1) }),
            TiffTagValue.CreateRaw(TiffTagId.DngPrivateData, TiffDataType.Byte, privateData.Length, privateData),
            TiffTagValue.CreateRaw(TiffTagId.RawDataUniqueId, TiffDataType.Byte, uniqueId.Length, uniqueId),
            TiffTagValue.CreateRaw(TiffTagId.OriginalRawFileData, TiffDataType.Undefined, originalRaw.Length, originalRaw)
        }));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 42 });
        writer.Flush();
        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());

        TiffVendorMetadataReadResult result = file.GetImage(0).ReadVendorMetadata();
        TiffDngField blackLevel = FindDngField(result.Metadata.Dng, TiffTagId.BlackLevel);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.Dng.HasDng, Is.True);
        Assert.That(result.Metadata.Dng.Version, Is.EqualTo(new int[] { 1, 7, 1, 0 }));
        Assert.That(result.Metadata.Dng.BackwardVersion, Is.EqualTo(new int[] { 1, 4, 0, 0 }));
        Assert.That(result.Metadata.Dng.UniqueCameraModel, Is.EqualTo("Example Raw Camera"));
        Assert.That(result.Metadata.Dng.LocalizedCameraModel, Is.EqualTo("Cámara"));
        Assert.That(blackLevel.Name, Is.EqualTo("BlackLevel"));
        Assert.That(blackLevel.Value.RationalValues[0].Numerator, Is.EqualTo(64));
        Assert.That(blackLevel.Value.RationalValues[1].Numerator, Is.EqualTo(65));
        Assert.That(result.Metadata.Dng.PrivateData, Is.EqualTo(privateData));
        Assert.That(result.Metadata.Dng.RawDataUniqueId, Is.EqualTo(uniqueId));
        Assert.That(result.Metadata.Dng.OriginalRawFileData, Is.EqualTo(originalRaw));
        Assert.That(FindDngField(result.Metadata.Dng, TiffTagId.DngPrivateData).EncodedBytes, Is.EqualTo(privateData));
        Assert.That(result.Metadata.PrivatePayloads.Length, Is.EqualTo(2));

        file.Close();
        writer.Close();
    }

    [TestCase("Aperio Image Library v12|AppMag = 40", "Aperio", "AppMag", "40")]
    [TestCase("Hamamatsu NDPI|FocalPlane = 2", "Hamamatsu", "FocalPlane", "2")]
    [TestCase("Leica SCN|Objective = 20", "Leica", "Objective", "20")]
    [TestCase("Philips PIM_DP_SCANNED_IMAGES|Barcode = ABC", "Philips", "Barcode", "ABC")]
    [TestCase("Ventana BIF|ScanMode = Brightfield", "Roche/Ventana", "ScanMode", "Brightfield")]
    [TestCase("Carl Zeiss LSM|DimensionZ = 12", "Zeiss", "DimensionZ", "12")]
    [TestCase("Micro-Manager Metadata|Frames = 5", "Micro-Manager", "Frames", "5")]
    [TestCase("Astro-TIFF|OBJECT = M42", "Astro-TIFF", "OBJECT", "M42")]
    public void ReadVendorMetadata_ExposesNamedVariantTextAndFields(string description, string vendor, string key, string value)
    {
        TiffDirectory directory = CreateAsciiDirectory(description, out byte[] fileBytes);

        TiffVendorMetadataReadResult result = TiffApi.Codec.ReadVendorMetadata(directory, fileBytes);
        TiffVendorVariantMetadata variant = SelectVariant(result.Metadata, vendor);

        Assert.That(result.Success, Is.True);
        Assert.That(variant.HasMetadata, Is.True);
        Assert.That(variant.Vendor, Is.EqualTo(vendor));
        Assert.That(variant.Texts.Length, Is.EqualTo(1));
        Assert.That(variant.Texts[0].Text, Is.EqualTo(description));
        Assert.That(variant.Texts[0].Source, Is.EqualTo("ImageDescription"));
        Assert.That(FindVendorField(variant, key).Value, Is.EqualTo(value));
    }

    [Test]
    public void ReadVendorMetadata_PreservesKnownAndUnknownPrivatePayloads()
    {
        byte[] fileBytes = new byte[256];
        byte[] description = WriteBytes(fileBytes, 64, TiffAscii.EncodeString("Carl Zeiss LSM|DimensionZ = 4"));
        byte[] unknownPayload = WriteBytes(fileBytes, 192, new byte[] { 31, 32, 33, 34, 35, 36 });
        TiffIfdEntry lsmEntry = CreateInlineEntry(TiffTagId.CzLsmInfo, TiffDataType.Undefined, new byte[] { 1, 2, 3, 4 });
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateExternalEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, description.Length, 64),
            lsmEntry,
            CreateExternalEntry(65000, TiffDataType.Undefined, unknownPayload.Length, 192)
        });

        TiffVendorMetadataReadResult result = TiffApi.Codec.ReadVendorMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.PrivatePayloads.Length, Is.EqualTo(2));
        Assert.That(FindPayload(result.Metadata.PrivatePayloads, TiffTagId.CzLsmInfo).Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(FindPayload(result.Metadata.PrivatePayloads, 65000).Bytes, Is.EqualTo(unknownPayload));
        Assert.That(result.Metadata.Zeiss.PrivatePayloads.Length, Is.EqualTo(2));
    }

    static TiffDngField FindDngField(TiffDngMetadata metadata, int tagId)
    {
        int index = 0;
        while (index < metadata.Fields.Length)
        {
            if (metadata.Fields[index].TagId == tagId)
            {
                return metadata.Fields[index];
            }

            index = index + 1;
        }

        return new TiffDngField();
    }

    static TiffVendorField FindVendorField(TiffVendorVariantMetadata metadata, string key)
    {
        int index = 0;
        while (index < metadata.Fields.Length)
        {
            if (metadata.Fields[index].Key == key)
            {
                return metadata.Fields[index];
            }

            index = index + 1;
        }

        return new TiffVendorField();
    }

    static TiffVendorPrivatePayload FindPayload(TiffVendorPrivatePayload[] payloads, int tagId)
    {
        int index = 0;
        while (index < payloads.Length)
        {
            if (payloads[index].TagId == tagId)
            {
                return payloads[index];
            }

            index = index + 1;
        }

        return new TiffVendorPrivatePayload();
    }

    static TiffVendorVariantMetadata SelectVariant(TiffVendorMetadata metadata, string vendor)
    {
        if (vendor == "Aperio")
        {
            return metadata.Aperio;
        }

        if (vendor == "Hamamatsu")
        {
            return metadata.Hamamatsu;
        }

        if (vendor == "Leica")
        {
            return metadata.Leica;
        }

        if (vendor == "Philips")
        {
            return metadata.Philips;
        }

        if (vendor == "Roche/Ventana")
        {
            return metadata.RocheVentana;
        }

        if (vendor == "Zeiss")
        {
            return metadata.Zeiss;
        }

        if (vendor == "Micro-Manager")
        {
            return metadata.MicroManager;
        }

        return metadata.AstroTiff;
    }

    static TiffDirectory CreateAsciiDirectory(string text, out byte[] fileBytes)
    {
        fileBytes = new byte[256];
        byte[] bytes = WriteBytes(fileBytes, 64, TiffAscii.EncodeString(text));
        return TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateExternalEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, bytes.Length, 64) });
    }

    static byte[] WriteBytes(byte[] destination, int offset, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            destination[offset + index] = bytes[index];
            index = index + 1;
        }

        return bytes;
    }

    static TiffIfdEntry CreateExternalEntry(int tagId, TiffDataType dataType, int count, ulong offset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = offset;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)offset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }

    static TiffIfdEntry CreateInlineEntry(int tagId, TiffDataType dataType, byte[] bytes)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = bytes.Length;
        entry.RawCount = (ulong)bytes.Length;
        entry.ValueOffsetBytes = bytes;
        entry.ValueByteCount = bytes.Length;
        entry.ValueSlotByteCount = 4;
        return entry;
    }

    static ulong[] ToUnsigned(byte[] bytes)
    {
        ulong[] values = new ulong[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            values[index] = bytes[index];
            index = index + 1;
        }

        return values;
    }
}
