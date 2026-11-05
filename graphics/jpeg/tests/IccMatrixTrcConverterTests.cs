namespace jpeg.Tests;

internal class IccMatrixTrcConverterTests
{
    [Test]
    public void Converter_ReadsRgbMatrixTrcProfileAndConvertsToXyz()
    {
        byte[] profile = CreateRgbMatrixTrcProfile();
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        global::jpeg.JpegIccMatrixTrcProfile matrix = converter.Parse(profile);
        global::jpeg.JpegXyzColor xyz = converter.ConvertRgbToXyz(matrix, 255, 128, 0);

        Assert.That(matrix.IsPresent, Is.True);
        Assert.That(matrix.IsRgb, Is.True);
        Assert.That(matrix.RedX, Is.EqualTo(1.0));
        Assert.That(matrix.GreenY, Is.EqualTo(1.0));
        Assert.That(matrix.BlueZ, Is.EqualTo(1.0));
        Assert.That(matrix.RedGamma, Is.EqualTo(1.0));
        Assert.That(matrix.GreenGamma, Is.EqualTo(1.0));
        Assert.That(matrix.BlueGamma, Is.EqualTo(1.0));
        Assert.That(xyz.X, Is.EqualTo(1.0).Within(0.000001));
        Assert.That(xyz.Y, Is.EqualTo(128.0 / 255.0).Within(0.000001));
        Assert.That(xyz.Z, Is.EqualTo(0.0).Within(0.000001));
    }

    [Test]
    public void Converter_ReadsGrayMatrixTrcProfileAndConvertsToXyz()
    {
        byte[] profile = CreateGrayMatrixTrcProfile();
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        global::jpeg.JpegIccMatrixTrcProfile matrix = converter.Parse(profile);
        global::jpeg.JpegXyzColor xyz = converter.ConvertGrayToXyz(matrix, 128);

        Assert.That(matrix.IsPresent, Is.True);
        Assert.That(matrix.IsGray, Is.True);
        Assert.That(matrix.WhiteX, Is.EqualTo(1.0));
        Assert.That(matrix.WhiteY, Is.EqualTo(1.0));
        Assert.That(matrix.WhiteZ, Is.EqualTo(1.0));
        Assert.That(matrix.GrayGamma, Is.EqualTo(1.0));
        Assert.That(xyz.X, Is.EqualTo(128.0 / 255.0).Within(0.000001));
        Assert.That(xyz.Y, Is.EqualTo(128.0 / 255.0).Within(0.000001));
        Assert.That(xyz.Z, Is.EqualTo(128.0 / 255.0).Within(0.000001));
    }

    [Test]
    public void Converter_ReadsCmykMatrixTrcProfileAndConvertsToXyz()
    {
        byte[] profile = CreateCmykMatrixTrcProfile();
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        global::jpeg.JpegIccMatrixTrcProfile matrix = converter.Parse(profile);
        global::jpeg.JpegXyzColor white = converter.ConvertCmykToXyz(matrix, 0, 0, 0, 0);
        global::jpeg.JpegXyzColor red = converter.ConvertCmykToXyz(matrix, 0, 255, 255, 0);
        global::jpeg.JpegXyzColor black = converter.ConvertCmykToXyz(matrix, 0, 0, 0, 255);

        Assert.That(matrix.IsPresent, Is.True);
        Assert.That(matrix.IsCmyk, Is.True);
        Assert.That(matrix.WhiteX, Is.EqualTo(0x0000F6D6L / 65536.0));
        Assert.That(matrix.WhiteY, Is.EqualTo(1.0));
        Assert.That(matrix.WhiteZ, Is.EqualTo(0x0000D342L / 65536.0));
        Assert.That(matrix.CyanGamma, Is.EqualTo(1.0));
        Assert.That(matrix.MagentaGamma, Is.EqualTo(1.0));
        Assert.That(matrix.YellowGamma, Is.EqualTo(1.0));
        Assert.That(matrix.BlackGamma, Is.EqualTo(1.0));
        Assert.That(white.X, Is.EqualTo(matrix.WhiteX).Within(0.000001));
        Assert.That(white.Y, Is.EqualTo(matrix.WhiteY).Within(0.000001));
        Assert.That(white.Z, Is.EqualTo(matrix.WhiteZ).Within(0.000001));
        Assert.That(red.X, Is.EqualTo(0.4360747 * matrix.WhiteX / 0.96422).Within(0.000001));
        Assert.That(red.Y, Is.EqualTo(0.2225045).Within(0.000001));
        Assert.That(red.Z, Is.EqualTo(0.0139322 * matrix.WhiteZ / 0.82521).Within(0.000001));
        Assert.That(black.X, Is.EqualTo(0.0).Within(0.000001));
        Assert.That(black.Y, Is.EqualTo(0.0).Within(0.000001));
        Assert.That(black.Z, Is.EqualTo(0.0).Within(0.000001));
    }

    [Test]
    public void Converter_AppliesSingleGammaCurve()
    {
        byte[] profile = CreateRgbMatrixTrcProfile();
        int rTrcOffset = FindTagDataOffset(profile, "rTRC");
        WriteCurveTag(profile, rTrcOffset, 512);
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        global::jpeg.JpegIccMatrixTrcProfile matrix = converter.Parse(profile);
        global::jpeg.JpegXyzColor xyz = converter.ConvertRgbToXyz(matrix, 128, 0, 0);

        Assert.That(matrix.RedGamma, Is.EqualTo(2.0));
        Assert.That(xyz.X, Is.EqualTo(System.Math.Pow(128.0 / 255.0, 2.0)).Within(0.000001));
        Assert.That(xyz.Y, Is.EqualTo(0.0).Within(0.000001));
        Assert.That(xyz.Z, Is.EqualTo(0.0).Within(0.000001));
    }

    [Test]
    public void Converter_AppliesCmykGammaCurve()
    {
        byte[] profile = CreateCmykMatrixTrcProfile();
        int kTrcOffset = FindTagDataOffset(profile, "kTRC");
        WriteCurveTag(profile, kTrcOffset, 512);
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        global::jpeg.JpegIccMatrixTrcProfile matrix = converter.Parse(profile);
        global::jpeg.JpegXyzColor xyz = converter.ConvertCmykToXyz(matrix, 0, 0, 0, 128);
        double ink = System.Math.Pow(128.0 / 255.0, 2.0);

        Assert.That(matrix.BlackGamma, Is.EqualTo(2.0));
        Assert.That(xyz.X, Is.EqualTo(matrix.WhiteX * (1.0 - ink)).Within(0.000001));
        Assert.That(xyz.Y, Is.EqualTo(matrix.WhiteY * (1.0 - ink)).Within(0.000001));
        Assert.That(xyz.Z, Is.EqualTo(matrix.WhiteZ * (1.0 - ink)).Within(0.000001));
    }

    [Test]
    public void Converter_RejectsUnsupportedMatrixTrcProfiles()
    {
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateRgbMatrixTrcProfile();
                IccProfileParserTests.WriteSignature(profile, 16, "CMYK");
                converter.Parse(profile);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.Parse(IccProfileParserTests.CreateIccProfileWithOneTag());
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateRgbMatrixTrcProfile();
                int rTrcOffset = FindTagDataOffset(profile, "rTRC");
                global::jpeg.JpegByteWriter.WriteUInt32(profile, rTrcOffset + 8, 2);
                converter.Parse(profile);
            });
    }

    [Test]
    public void Converter_RejectsUnsupportedCmykMatrixTrcProfiles()
    {
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateCmykMatrixTrcProfile();
                IccProfileParserTests.WriteSignature(profile, 132, "A2B0");
                converter.Parse(profile);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                byte[] profile = CreateCmykMatrixTrcProfile();
                global::jpeg.JpegIccMatrixTrcProfile matrix = converter.Parse(profile);
                converter.ConvertCmykToXyz(matrix, 0, 0, 0, 256);
            });
    }

    [Test]
    public void Converter_UsesNullForMissingOptionalTags()
    {
        byte[] profile = CreateRgbMatrixTrcProfile();
        global::jpeg.JpegIccProfile icc = new global::jpeg.JpegIccProfileParser().Parse(profile);
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        global::jpeg.JpegIccTag? missing = converter.FindTagOrNull(icc, "missing");

        Assert.That(missing, Is.Null);
        Assert.That(converter.IsXyzTag(profile, missing), Is.False);
        Assert.That(converter.IsCurveTagSupported(profile, missing), Is.False);
    }

    public static byte[] CreateRgbMatrixTrcProfile()
    {
        int tagCount = 7;
        int dataOffset = 132 + tagCount * 12;
        byte[] profile = new byte[dataOffset + 122];
        WriteBasicHeader(profile, "RGB ");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 128, tagCount);
        int offset = dataOffset;
        WriteTagEntry(profile, 0, "rXYZ", offset, 20);
        WriteXyzTag(profile, offset, 0x00010000L, 0, 0);
        offset = offset + 20;
        WriteTagEntry(profile, 1, "gXYZ", offset, 20);
        WriteXyzTag(profile, offset, 0, 0x00010000L, 0);
        offset = offset + 20;
        WriteTagEntry(profile, 2, "bXYZ", offset, 20);
        WriteXyzTag(profile, offset, 0, 0, 0x00010000L);
        offset = offset + 20;
        WriteTagEntry(profile, 3, "wtpt", offset, 20);
        WriteXyzTag(profile, offset, 0x00010000L, 0x00010000L, 0x00010000L);
        offset = offset + 20;
        WriteTagEntry(profile, 4, "rTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        offset = offset + 14;
        WriteTagEntry(profile, 5, "gTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        offset = offset + 14;
        WriteTagEntry(profile, 6, "bTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        return profile;
    }

    public static byte[] CreateGrayMatrixTrcProfile()
    {
        int tagCount = 2;
        int dataOffset = 132 + tagCount * 12;
        byte[] profile = new byte[dataOffset + 34];
        WriteBasicHeader(profile, "GRAY");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 128, tagCount);
        int offset = dataOffset;
        WriteTagEntry(profile, 0, "wtpt", offset, 20);
        WriteXyzTag(profile, offset, 0x00010000L, 0x00010000L, 0x00010000L);
        offset = offset + 20;
        WriteTagEntry(profile, 1, "kTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        return profile;
    }

    public static byte[] CreateCmykMatrixTrcProfile()
    {
        int tagCount = 5;
        int dataOffset = 132 + tagCount * 12;
        byte[] profile = new byte[dataOffset + 76];
        WriteBasicHeader(profile, "CMYK");
        IccProfileParserTests.WriteSignature(profile, 12, "prtr");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 128, tagCount);
        int offset = dataOffset;
        WriteTagEntry(profile, 0, "wtpt", offset, 20);
        WriteXyzTag(profile, offset, 0x0000F6D6L, 0x00010000L, 0x0000D342L);
        offset = offset + 20;
        WriteTagEntry(profile, 1, "cTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        offset = offset + 14;
        WriteTagEntry(profile, 2, "mTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        offset = offset + 14;
        WriteTagEntry(profile, 3, "yTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        offset = offset + 14;
        WriteTagEntry(profile, 4, "kTRC", offset, 14);
        WriteCurveTag(profile, offset, 256);
        return profile;
    }

    public static void WriteBasicHeader(byte[] profile, string colorSpace)
    {
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 0, profile.Length);
        IccProfileParserTests.WriteSignature(profile, 4, "TEST");
        profile[8] = 4;
        profile[9] = 0x30;
        IccProfileParserTests.WriteSignature(profile, 12, "mntr");
        IccProfileParserTests.WriteSignature(profile, 16, colorSpace);
        IccProfileParserTests.WriteSignature(profile, 20, "XYZ ");
        IccProfileParserTests.WriteSignature(profile, 36, "acsp");
    }

    public static void WriteTagEntry(byte[] profile, int index, string signature, int offset, int size)
    {
        int entryOffset = 132 + index * 12;
        IccProfileParserTests.WriteSignature(profile, entryOffset, signature);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, entryOffset + 4, offset);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, entryOffset + 8, size);
    }

    public static void WriteXyzTag(byte[] profile, int offset, long x, long y, long z)
    {
        IccProfileParserTests.WriteSignature(profile, offset, "XYZ ");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, offset + 8, x);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, offset + 12, y);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, offset + 16, z);
    }

    public static void WriteCurveTag(byte[] profile, int offset, int gamma)
    {
        IccProfileParserTests.WriteSignature(profile, offset, "curv");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, offset + 8, 1);
        global::jpeg.JpegByteWriter.WriteUInt16(profile, offset + 12, gamma);
    }

    public static int FindTagDataOffset(byte[] profile, string signature)
    {
        long tagCount = global::jpeg.JpegByteReader.ReadUInt32(profile, 128);
        int index = 0;
        while (index < tagCount)
        {
            int entryOffset = 132 + index * 12;
            string tagSignature = System.Text.Encoding.ASCII.GetString(profile, entryOffset, 4);
            if (tagSignature == signature)
            {
                return (int)global::jpeg.JpegByteReader.ReadUInt32(profile, entryOffset + 4);
            }

            index = index + 1;
        }

        return -1;
    }
}
