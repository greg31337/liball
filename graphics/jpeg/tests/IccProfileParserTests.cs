namespace jpeg.Tests;

internal class IccProfileParserTests
{
    [Test]
    public void Parser_ReadsIccHeaderAndTagTable()
    {
        byte[] profile = CreateIccProfileWithOneTag();
        global::jpeg.JpegIccProfileParser parser = new global::jpeg.JpegIccProfileParser();

        global::jpeg.JpegIccProfile icc = parser.Parse(profile);

        Assert.That(icc.IsPresent, Is.True);
        Assert.That(icc.ProfileSize, Is.EqualTo(profile.Length));
        Assert.That(icc.CmmType, Is.EqualTo("TEST"));
        Assert.That(icc.VersionMajor, Is.EqualTo(4));
        Assert.That(icc.VersionMinor, Is.EqualTo(3));
        Assert.That(icc.VersionBugFix, Is.EqualTo(2));
        Assert.That(icc.ProfileClass, Is.EqualTo("mntr"));
        Assert.That(icc.ColorSpace, Is.EqualTo("RGB "));
        Assert.That(icc.Pcs, Is.EqualTo("XYZ "));
        Assert.That(icc.Year, Is.EqualTo(2024));
        Assert.That(icc.Month, Is.EqualTo(8));
        Assert.That(icc.Day, Is.EqualTo(5));
        Assert.That(icc.Signature, Is.EqualTo("acsp"));
        Assert.That(icc.Platform, Is.EqualTo("APPL"));
        Assert.That(icc.Flags, Is.EqualTo(1));
        Assert.That(icc.DeviceManufacturer, Is.EqualTo("MAKE"));
        Assert.That(icc.DeviceModel, Is.EqualTo("MODL"));
        Assert.That(icc.RenderingIntent, Is.EqualTo(1));
        Assert.That(icc.IlluminantX, Is.EqualTo(1.0));
        Assert.That(icc.IlluminantY, Is.EqualTo(1.0));
        Assert.That(icc.IlluminantZ, Is.EqualTo(1.0));
        Assert.That(icc.Creator, Is.EqualTo("CRTR"));
        Assert.That(icc.ProfileId[0], Is.EqualTo(1));
        Assert.That(icc.ProfileId[15], Is.EqualTo(16));
        Assert.That(icc.TagCount, Is.EqualTo(1));
        Assert.That(icc.Tags.Count, Is.EqualTo(1));
        Assert.That(icc.Tags[0].Signature, Is.EqualTo("desc"));
        Assert.That(icc.Tags[0].Offset, Is.EqualTo(144));
        Assert.That(icc.Tags[0].Size, Is.EqualTo(16));
    }

    [Test]
    public void Parser_ReadsNegativeS15Fixed16Values()
    {
        byte[] data = new byte[4];
        global::jpeg.JpegByteWriter.WriteUInt32(data, 0, 0xFFFF0000L);
        global::jpeg.JpegIccProfileParser parser = new global::jpeg.JpegIccProfileParser();

        double value = parser.ReadS15Fixed16(data, 0);

        Assert.That(value, Is.EqualTo(-1.0));
    }

    [Test]
    public void Parser_RejectsMalformedIccProfiles()
    {
        global::jpeg.JpegIccProfileParser parser = new global::jpeg.JpegIccProfileParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[131]);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateIccProfileWithOneTag();
                global::jpeg.JpegByteWriter.WriteUInt32(profile, 0, profile.Length - 1);
                parser.Parse(profile);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateIccProfileWithOneTag();
                WriteSignature(profile, 36, "bad!");
                parser.Parse(profile);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateIccProfileWithOneTag();
                global::jpeg.JpegByteWriter.WriteUInt32(profile, 128, 3);
                parser.Parse(profile);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] profile = CreateIccProfileWithOneTag();
                global::jpeg.JpegByteWriter.WriteUInt32(profile, 136, profile.Length - 4);
                global::jpeg.JpegByteWriter.WriteUInt32(profile, 140, 8);
                parser.Parse(profile);
            });
    }

    public static byte[] CreateIccProfileWithOneTag()
    {
        byte[] profile = new byte[160];
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 0, profile.Length);
        WriteSignature(profile, 4, "TEST");
        profile[8] = 4;
        profile[9] = 0x32;
        WriteSignature(profile, 12, "mntr");
        WriteSignature(profile, 16, "RGB ");
        WriteSignature(profile, 20, "XYZ ");
        global::jpeg.JpegByteWriter.WriteUInt16(profile, 24, 2024);
        global::jpeg.JpegByteWriter.WriteUInt16(profile, 26, 8);
        global::jpeg.JpegByteWriter.WriteUInt16(profile, 28, 5);
        global::jpeg.JpegByteWriter.WriteUInt16(profile, 30, 12);
        global::jpeg.JpegByteWriter.WriteUInt16(profile, 32, 34);
        global::jpeg.JpegByteWriter.WriteUInt16(profile, 34, 56);
        WriteSignature(profile, 36, "acsp");
        WriteSignature(profile, 40, "APPL");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 44, 1);
        WriteSignature(profile, 48, "MAKE");
        WriteSignature(profile, 52, "MODL");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 64, 1);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 68, 0x00010000L);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 72, 0x00010000L);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 76, 0x00010000L);
        WriteSignature(profile, 80, "CRTR");
        int profileIdIndex = 0;
        while (profileIdIndex < 16)
        {
            profile[84 + profileIdIndex] = (byte)(profileIdIndex + 1);
            profileIdIndex = profileIdIndex + 1;
        }

        global::jpeg.JpegByteWriter.WriteUInt32(profile, 128, 1);
        WriteSignature(profile, 132, "desc");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 136, 144);
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 140, 16);
        return profile;
    }

    public static void WriteSignature(byte[] data, int offset, string signature)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(signature);
        System.Array.Copy(bytes, 0, data, offset, 4);
    }
}
