using System.Collections.Generic;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class IccProfileParserTests
{
    [Test]
    public void Parse_HeaderProducesTypedProfileFields()
    {
        byte[] bytes = CreateProfile(false);
        ImageMetadataDocument document = Parse(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Unsigned(document, "Icc.Header.ProfileSize"), Is.EqualTo((ulong)bytes.Length));
        Assert.That(Text(document, "Icc.Header.PreferredCmm"), Is.EqualTo("TEST"));
        Assert.That(Text(document, "Icc.Header.Version"), Is.EqualTo("4.3.0"));
        Assert.That(Text(document, "Icc.Header.DeviceClass"), Is.EqualTo("mntr"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.Header.DeviceClass").DisplayValue, Is.EqualTo("Display device"));
        Assert.That(Text(document, "Icc.Header.ColorSpace"), Is.EqualTo("RGB "));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.Header.ColorSpace").DisplayValue, Is.EqualTo("RGB"));
        Assert.That(Text(document, "Icc.Header.ConnectionSpace"), Is.EqualTo("XYZ "));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.Header.CreationDate").ValueType, Is.EqualTo(MetadataValueKind.DateText));
        Assert.That(Text(document, "Icc.Header.CreationDate"), Is.EqualTo("2026-08-12T10:11:12"));
        Assert.That(Unsigned(document, "Icc.Header.RenderingIntent"), Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.Header.RenderingIntent").DisplayValue, Is.EqualTo("Media-relative colorimetric"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.Header.IlluminantX").Value.Numerator, Is.EqualTo(63190));
        Assert.That(Text(document, "Icc.Header.ProfileId"), Is.EqualTo("0102030405060708090a0b0c0d0e0f10"));
        Assert.That(document.Directories[0].Path, Is.EqualTo("Test/ICC/Header"));
        Assert.That(document.Directories[0].Offset, Is.EqualTo(25));
    }

    [Test]
    public void Parse_TagTableDecodesDescriptionCopyrightAndWhitePoint()
    {
        byte[] bytes = CreateProfile(false);
        ImageMetadataDocument document = Parse(bytes);
        new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(Unsigned(document, "Icc.TagTable.Count"), Is.EqualTo(3));
        Assert.That(Text(document, "Icc.Tag.desc.Type"), Is.EqualTo("desc"));
        Assert.That(Text(document, "Icc.desc.Description"), Is.EqualTo("Studio Display"));
        Assert.That(Text(document, "Icc.cprt.Copyright"), Is.EqualTo("Copyright 2026"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.wtpt.X").Value.Numerator, Is.EqualTo(63190));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.wtpt.Y").Value.Numerator, Is.EqualTo(65536));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.wtpt.Z").Value.Numerator, Is.EqualTo(54061));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("Studio Display"));
        Assert.That(document.Summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(bytes));
        Assert.That(document.Directories.Single(directory => directory.Group == "TagTable").Children, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_MultilingualDescriptionSelectsEnglishAndRetainsLocales()
    {
        byte[] bytes = CreateProfile(true);
        ImageMetadataDocument document = Parse(bytes);

        Assert.That(Text(document, "Icc.desc.Description"), Is.EqualTo("English profile"));
        Assert.That(Text(document, "Icc.desc.Description.fr-FR"), Is.EqualTo("Profil francais"));
        Assert.That(Text(document, "Icc.desc.Description.en-US"), Is.EqualTo("English profile"));
        Assert.That(Text(document, "Icc.cprt.Copyright"), Is.EqualTo("Copyright 2026"));
    }

    [Test]
    public void Parse_InvalidSignatureAndTagRangeProduceDiagnostics()
    {
        byte[] invalidSignature = CreateProfile(false);
        invalidSignature[36] = (byte)'x';
        ImageMetadataDocument signatureDocument = Parse(invalidSignature);
        Assert.That(signatureDocument.Directories, Is.Empty);
        Assert.That(signatureDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "icc.signature.invalid"));

        byte[] invalidRange = CreateProfile(false);
        WriteUnsigned32(invalidRange, 136, (uint)(invalidRange.Length + 20));
        ImageMetadataDocument rangeDocument = Parse(invalidRange);
        Assert.That(rangeDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "icc.tag.range.invalid"));
    }

    [Test]
    public void Parse_TruncatedProfileAndTagLimitAreReported()
    {
        byte[] truncated = CreateProfile(false);
        WriteUnsigned32(truncated, 0, (uint)(truncated.Length + 100));
        ImageMetadataDocument truncatedDocument = Parse(truncated);
        Assert.That(truncatedDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "icc.profile.truncated" && item.Severity == MetadataSeverity.Truncated));

        byte[] bytes = CreateProfile(false);
        ImageMetadataDocument limitedDocument = new ImageMetadataDocument();
        MetadataProfile profile = Profile(bytes);
        limitedDocument.AddProfile(profile);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxTagCount = 3;
        IccProfileParser.ParseProfile(limitedDocument, profile, options, "Test/ICC", "ICC");
        Assert.That(limitedDocument.Directories.Sum(directory => directory.Tags.Count), Is.EqualTo(3));
        Assert.That(limitedDocument.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "icc.tag.limit"));
    }

    [Test]
    public void JpegIntegration_ParsesReassembledProfileAndUsesDecodedName()
    {
        byte[] bytes = CreateProfile(false);
        int split = bytes.Length / 2;
        byte[] first = new byte[split];
        byte[] second = new byte[bytes.Length - split];
        System.Array.Copy(bytes, 0, first, 0, first.Length);
        System.Array.Copy(bytes, split, second, 0, second.Length);
        byte[] firstPayload = JpegIccExtractorTests.CreateIccPayload(1, 2, first);
        byte[] secondPayload = JpegIccExtractorTests.CreateIccPayload(2, 2, second);

        ImageMetadataDocument document = JpegIccExtractorTests.ParseJpeg(new byte[][] { secondPayload, firstPayload }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles[0].Data, Is.EqualTo(bytes));
        Assert.That(Text(document, "Icc.desc.Description"), Is.EqualTo("Studio Display"));
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("Studio Display"));
        Assert.That(document.Directories, Has.Some.Matches<MetadataDirectory>(directory => directory.Path == "JPEG/APP2/ICC/Header"));
    }

    public static ImageMetadataDocument Parse(byte[] bytes)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataProfile profile = Profile(bytes);
        document.AddProfile(profile);
        IccProfileParser.ParseProfile(document, profile, new ImageMetadataReaderOptions(), "Test/ICC", "ICC");
        return document;
    }

    public static MetadataProfile Profile(byte[] bytes)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Icc;
        profile.Family = MetadataFamily.Icc;
        profile.Key = "Icc.Test";
        profile.Name = "ICC Profile";
        profile.Data = bytes;
        profile.Offset = 25;
        profile.Length = bytes.Length;
        return profile;
    }

    public static byte[] CreateProfile(bool multilingualDescription)
    {
        byte[] description = multilingualDescription
            ? Mluc("fr", "FR", "Profil francais", "en", "US", "English profile", 0)
            : Description("Studio Display");
        byte[] copyright = multilingualDescription ? TextTag("Copyright 2026") : Mluc("fr", "FR", "Droit 2026", "en", "US", "Copyright 2026", 0);
        byte[] whitePoint = Xyz(63190, 65536, 54061);
        int tableLength = 4 + 3 * 12;
        int descriptionOffset = 128 + tableLength;
        int copyrightOffset = Align4(descriptionOffset + description.Length);
        int whitePointOffset = Align4(copyrightOffset + copyright.Length);
        int totalLength = whitePointOffset + whitePoint.Length;
        byte[] profile = new byte[totalLength];
        WriteUnsigned32(profile, 0, (uint)totalLength);
        WriteAscii(profile, 4, "TEST");
        profile[8] = 4;
        profile[9] = 48;
        WriteAscii(profile, 12, "mntr");
        WriteAscii(profile, 16, "RGB ");
        WriteAscii(profile, 20, "XYZ ");
        WriteUnsigned16(profile, 24, 2026);
        WriteUnsigned16(profile, 26, 8);
        WriteUnsigned16(profile, 28, 12);
        WriteUnsigned16(profile, 30, 10);
        WriteUnsigned16(profile, 32, 11);
        WriteUnsigned16(profile, 34, 12);
        WriteAscii(profile, 36, "acsp");
        WriteAscii(profile, 40, "APPL");
        WriteUnsigned32(profile, 44, 1);
        WriteAscii(profile, 48, "ACME");
        WriteAscii(profile, 52, "MODL");
        WriteUnsigned32(profile, 60, 1);
        WriteUnsigned32(profile, 64, 1);
        WriteSigned32(profile, 68, 63190);
        WriteSigned32(profile, 72, 65536);
        WriteSigned32(profile, 76, 54061);
        WriteAscii(profile, 80, "MAKE");
        int idIndex = 0;
        while (idIndex < 16)
        {
            profile[84 + idIndex] = (byte)(idIndex + 1);
            idIndex++;
        }

        WriteUnsigned32(profile, 128, 3);
        WriteEntry(profile, 132, "desc", descriptionOffset, description.Length);
        WriteEntry(profile, 144, "cprt", copyrightOffset, copyright.Length);
        WriteEntry(profile, 156, "wtpt", whitePointOffset, whitePoint.Length);
        if (multilingualDescription)
        {
            FixMlucOffsets(description, descriptionOffset);
        }
        else
        {
            FixMlucOffsets(copyright, copyrightOffset);
        }

        System.Array.Copy(description, 0, profile, descriptionOffset, description.Length);
        System.Array.Copy(copyright, 0, profile, copyrightOffset, copyright.Length);
        System.Array.Copy(whitePoint, 0, profile, whitePointOffset, whitePoint.Length);
        return profile;
    }

    public static byte[] Description(string text)
    {
        byte[] value = Encoding.ASCII.GetBytes(text + "\0");
        byte[] data = new byte[12 + value.Length];
        WriteAscii(data, 0, "desc");
        WriteUnsigned32(data, 8, (uint)value.Length);
        System.Array.Copy(value, 0, data, 12, value.Length);
        return data;
    }

    public static byte[] TextTag(string text)
    {
        byte[] value = Encoding.ASCII.GetBytes(text + "\0");
        byte[] data = new byte[8 + value.Length];
        WriteAscii(data, 0, "text");
        System.Array.Copy(value, 0, data, 8, value.Length);
        return data;
    }

    public static byte[] Mluc(string language1, string country1, string text1, string language2, string country2, string text2, int profileOffset)
    {
        byte[] first = Encoding.BigEndianUnicode.GetBytes(text1);
        byte[] second = Encoding.BigEndianUnicode.GetBytes(text2);
        byte[] data = new byte[40 + first.Length + second.Length];
        WriteAscii(data, 0, "mluc");
        WriteUnsigned32(data, 8, 2);
        WriteUnsigned32(data, 12, 12);
        WriteAscii(data, 16, language1 + country1);
        WriteUnsigned32(data, 20, (uint)first.Length);
        WriteUnsigned32(data, 24, (uint)(profileOffset + 40));
        WriteAscii(data, 28, language2 + country2);
        WriteUnsigned32(data, 32, (uint)second.Length);
        WriteUnsigned32(data, 36, (uint)(profileOffset + 40 + first.Length));
        System.Array.Copy(first, 0, data, 40, first.Length);
        System.Array.Copy(second, 0, data, 40 + first.Length, second.Length);
        return data;
    }

    public static void FixMlucOffsets(byte[] data, int profileOffset)
    {
        uint firstLength = ReadUnsigned32(data, 20);
        WriteUnsigned32(data, 24, (uint)(profileOffset + 40));
        WriteUnsigned32(data, 36, (uint)(profileOffset + 40 + firstLength));
    }

    public static byte[] Xyz(int x, int y, int z)
    {
        byte[] data = new byte[20];
        WriteAscii(data, 0, "XYZ ");
        WriteSigned32(data, 8, x);
        WriteSigned32(data, 12, y);
        WriteSigned32(data, 16, z);
        return data;
    }

    public static void WriteEntry(byte[] data, int offset, string signature, int valueOffset, int size)
    {
        WriteAscii(data, offset, signature);
        WriteUnsigned32(data, offset + 4, (uint)valueOffset);
        WriteUnsigned32(data, offset + 8, (uint)size);
    }

    public static void WriteAscii(byte[] data, int offset, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        System.Array.Copy(bytes, 0, data, offset, bytes.Length);
    }

    public static void WriteUnsigned16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    public static void WriteUnsigned32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    public static uint ReadUnsigned32(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    }

    public static void WriteSigned32(byte[] data, int offset, int value)
    {
        WriteUnsigned32(data, offset, IntegerBitPattern.ToUnsigned32(value));
    }

    public static int Align4(int value)
    {
        return (value + 3) & ~3;
    }

    public static string Text(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.Text;
    }

    public static ulong Unsigned(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key).Value.UnsignedIntegerValue;
    }
}
