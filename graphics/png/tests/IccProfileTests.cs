using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class IccProfileTests
{
    [Test]
    public void Parse_ReadsNameAndDecompressesProfileUsingOffset()
    {
        byte[] profileData = CreateProfileBytes();
        byte[] compressed = PngZlibCompressor.Compress(profileData);
        byte[] payload = CreateIccpData("Display", compressed);
        byte[] wrapped = Wrap(payload);

        PngIccProfile profile = PngIccProfileParser.Parse(wrapped, 1, payload.Length);

        Assert.That(profile.Name, Is.EqualTo("Display"));
        Assert.That(profile.CompressionMethod, Is.EqualTo(0));
        Assert.That(profile.Profile, Is.EqualTo(profileData));
        Assert.That(profile.CompressedProfile, Is.EqualTo(compressed));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        byte[] profileData = CreateProfileBytes();
        byte[] payload = CreateIccpData("Profile", PngZlibCompressor.Compress(profileData));
        PngChunk chunk = CreateChunk(PngConstants.ChunkIccProfile, payload);

        PngIccProfile profile = PngIccProfileParser.Parse(chunk);

        Assert.That(profile.Name, Is.EqualTo("Profile"));
        Assert.That(profile.Profile, Is.EqualTo(profileData));
    }

    [Test]
    public void WriteData_CompressesProfilePayload()
    {
        PngIccProfile profile = CreateProfile();

        byte[] data = PngIccProfileWriter.WriteData(profile);
        PngIccProfile parsed = PngIccProfileParser.Parse(data);

        Assert.That(data[0], Is.EqualTo((byte)'D'));
        Assert.That(data[7], Is.EqualTo(0));
        Assert.That(data[8], Is.EqualTo(0));
        Assert.That(parsed.Name, Is.EqualTo("Display"));
        Assert.That(parsed.Profile, Is.EqualTo(profile.Profile));
        Assert.That(parsed.CompressedProfile, Is.Not.Empty);
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngIccProfile profile = CreateProfile();
        byte[] payload = PngIccProfileWriter.WriteData(profile);
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngIccProfileWriter.WriteData(data, 1, profile);

        Assert.That(data[0], Is.EqualTo(99));
        Assert.That(data[data.Length - 1], Is.EqualTo(88));
        Assert.That(PngIccProfileParser.Parse(data, 1, payload.Length).Profile, Is.EqualTo(profile.Profile));
    }

    [Test]
    public void WriteChunk_CreatesKnownIccpChunk()
    {
        PngChunk chunk = PngIccProfileWriter.WriteChunk(CreateProfile());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkIccProfile));
        Assert.That(chunk.Data, Is.Not.Empty);
        Assert.That(chunk.Length, Is.EqualTo(chunk.Data.Length));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngIccProfileWriter.WriteChunk(CreateProfile());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngIccProfile profile = PngIccProfileParser.Parse(readChunk);

        Assert.That(profile.Name, Is.EqualTo("Display"));
        Assert.That(profile.Profile, Is.EqualTo(CreateProfileBytes()));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkIccProfile, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        byte[] compressed = PngZlibCompressor.Compress(CreateProfileBytes());
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreateIccpData("A", compressed);
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkIccProfile;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreateIccpData("A", compressed));

        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse(new byte[0], (PngSecurityLimits)null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse(new byte[0], 0, 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileParser.Parse(new byte[4], -1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileParser.Parse(new byte[4], 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileParser.Parse(new byte[4], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileParser.Parse(new byte[4], 1, 4));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(new byte[] { 65, 66, 67 }));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(CreateIccpData("", compressed)));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(CreateLongNameData()));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(new byte[] { 65, 0 }));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(new byte[] { 65, 0, 1, 1 }));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(new byte[] { 65, 0, 0 }));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(new byte[] { 65, 0, 0, 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => PngIccProfileParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngIccProfile profile = CreateProfile();
        PngIccProfile nullName = CreateProfile();
        nullName.Name = null!;
        PngIccProfile nullProfile = CreateProfile();
        nullProfile.Profile = null!;
        PngIccProfile badMethod = CreateProfile();
        badMethod.CompressionMethod = 1;
        PngIccProfile emptyName = CreateProfile();
        emptyName.Name = "";
        PngIccProfile longName = CreateProfile();
        longName.Name = CreateLongName();
        PngIccProfile nullCharacterName = CreateProfile();
        nullCharacterName.Name = "A\0B";
        PngIccProfile nonLatin1Name = CreateProfile();
        nonLatin1Name.Name = "A\u0100";

        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteData((PngIccProfile)null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteData(null!, 0, profile));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteData(new byte[1], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteData(nullName));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteData(nullProfile));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteData(profile, null!));
        Assert.Throws<ArgumentNullException>(() => PngIccProfileWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(badMethod));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(emptyName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(longName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(nullCharacterName));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(nonLatin1Name));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(new byte[4], -1, profile));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIccProfileWriter.WriteData(new byte[4], 1, profile));
    }

    static PngIccProfile CreateProfile()
    {
        PngIccProfile profile = new PngIccProfile();
        profile.Name = "Display";
        profile.CompressionMethod = 0;
        profile.Profile = CreateProfileBytes();
        return profile;
    }

    static byte[] CreateProfileBytes()
    {
        return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
    }

    static byte[] CreateIccpData(string name, byte[] compressed)
    {
        byte[] data = new byte[name.Length + 2 + compressed.Length];
        int index = 0;
        while (index < name.Length)
        {
            data[index] = (byte)name[index];
            index += 1;
        }

        data[name.Length] = 0;
        data[name.Length + 1] = 0;
        CopyBytes(compressed, 0, data, name.Length + 2, compressed.Length);
        return data;
    }

    static byte[] CreateLongNameData()
    {
        byte[] compressed = PngZlibCompressor.Compress(CreateProfileBytes());
        byte[] data = new byte[82 + compressed.Length];
        int index = 0;
        while (index < 80)
        {
            data[index] = 65;
            index += 1;
        }

        data[80] = 0;
        data[81] = 0;
        CopyBytes(compressed, 0, data, 82, compressed.Length);
        return data;
    }

    static string CreateLongName()
    {
        return new string('A', 80);
    }

    static byte[] Wrap(byte[] payload)
    {
        byte[] wrapped = new byte[payload.Length + 2];
        wrapped[0] = 99;
        wrapped[wrapped.Length - 1] = 88;
        CopyBytes(payload, 0, wrapped, 1, payload.Length);
        return wrapped;
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
