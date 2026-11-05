using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ExifProfileTests
{
    [Test]
    public void Parse_ReadsLittleEndianTiffProfileUsingOffset()
    {
        byte[] payload = CreateLittleEndianProfile();
        byte[] data = Wrap(payload);

        PngExifProfile profile = PngExifProfileParser.Parse(data, 1, payload.Length);

        Assert.That(profile.Profile, Is.EqualTo(payload));
    }

    [Test]
    public void Parse_ReadsBigEndianTiffProfile()
    {
        byte[] payload = CreateBigEndianProfile();

        PngExifProfile profile = PngExifProfileParser.Parse(payload);

        Assert.That(profile.Profile, Is.EqualTo(payload));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkExif, CreateLittleEndianProfile());

        PngExifProfile profile = PngExifProfileParser.Parse(chunk);

        Assert.That(profile.Profile, Is.EqualTo(CreateLittleEndianProfile()));
    }

    [Test]
    public void Parse_CopiesPayload()
    {
        byte[] payload = CreateLittleEndianProfile();

        PngExifProfile profile = PngExifProfileParser.Parse(payload);
        payload[4] = 99;

        Assert.That(profile.Profile, Is.EqualTo(CreateLittleEndianProfile()));
    }

    [Test]
    public void WriteData_WritesProfilePayload()
    {
        byte[] data = PngExifProfileWriter.WriteData(CreateProfile());

        Assert.That(data, Is.EqualTo(CreateLittleEndianProfile()));
    }

    [Test]
    public void WriteData_CopiesPayload()
    {
        PngExifProfile profile = CreateProfile();

        byte[] data = PngExifProfileWriter.WriteData(profile);
        profile.Profile[4] = 99;

        Assert.That(data, Is.EqualTo(CreateLittleEndianProfile()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] payload = CreateLittleEndianProfile();
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngExifProfileWriter.WriteData(data, 1, CreateProfile());

        Assert.That(data, Is.EqualTo(Wrap(payload)));
    }

    [Test]
    public void WriteChunk_CreatesKnownExifChunk()
    {
        PngChunk chunk = PngExifProfileWriter.WriteChunk(CreateProfile());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkExif));
        Assert.That(chunk.Data, Is.EqualTo(CreateLittleEndianProfile()));
        Assert.That(chunk.Length, Is.EqualTo(CreateLittleEndianProfile().Length));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.True);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngExifProfileWriter.WriteChunk(CreateProfile());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngExifProfile profile = PngExifProfileParser.Parse(readChunk);

        Assert.That(profile.Profile, Is.EqualTo(CreateLittleEndianProfile()));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkExif, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreateLittleEndianProfile();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkExif;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreateLittleEndianProfile());

        Assert.Throws<ArgumentNullException>(() => PngExifProfileParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileParser.Parse(null!, 0, 4));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileParser.Parse(new byte[4], -1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileParser.Parse(new byte[4], 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileParser.Parse(new byte[4], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileParser.Parse(new byte[4], 1, 4));
        Assert.Throws<InvalidDataException>(() => PngExifProfileParser.Parse(new byte[3]));
        Assert.Throws<InvalidDataException>(() => PngExifProfileParser.Parse(new byte[] { 0x45, 0x78, 0x69, 0x66, 0, 0, 0x49, 0x49, 0x2A, 0 }));
        Assert.Throws<InvalidDataException>(() => PngExifProfileParser.Parse(new byte[] { 0x49, 0x49, 0x00, 0x2A }));
        Assert.Throws<InvalidDataException>(() => PngExifProfileParser.Parse(new byte[] { 0x4D, 0x4D, 0x2A, 0x00 }));
        Assert.Throws<InvalidDataException>(() => PngExifProfileParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngExifProfile profile = CreateProfile();
        PngExifProfile nullProfile = CreateProfile();
        nullProfile.Profile = null!;
        PngExifProfile shortProfile = CreateProfile(new byte[3]);
        PngExifProfile app1Profile = CreateProfile(new byte[] { 0x45, 0x78, 0x69, 0x66, 0, 0, 0x49, 0x49, 0x2A, 0 });
        PngExifProfile invalidProfile = CreateProfile(new byte[] { 0x49, 0x49, 0x00, 0x2A });

        Assert.Throws<ArgumentNullException>(() => PngExifProfileWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileWriter.WriteData(null!, 0, profile));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileWriter.WriteData(new byte[CreateLittleEndianProfile().Length], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileWriter.WriteChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngExifProfileWriter.WriteData(nullProfile));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileWriter.WriteData(shortProfile));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileWriter.WriteData(app1Profile));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileWriter.WriteData(invalidProfile));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileWriter.WriteData(new byte[CreateLittleEndianProfile().Length], -1, profile));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngExifProfileWriter.WriteData(new byte[CreateLittleEndianProfile().Length], 1, profile));
    }

    static PngExifProfile CreateProfile()
    {
        return CreateProfile(CreateLittleEndianProfile());
    }

    static PngExifProfile CreateProfile(byte[] data)
    {
        PngExifProfile profile = new PngExifProfile();
        profile.Profile = data;
        return profile;
    }

    static byte[] CreateLittleEndianProfile()
    {
        return new byte[] { 0x49, 0x49, 0x2A, 0x00, 8, 0, 0, 0 };
    }

    static byte[] CreateBigEndianProfile()
    {
        return new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0, 0, 0, 8 };
    }

    static byte[] Wrap(byte[] payload)
    {
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;
        CopyBytes(payload, 0, data, 1, payload.Length);
        return data;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = true;
        return chunk;
    }

    static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
