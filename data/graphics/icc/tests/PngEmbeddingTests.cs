using System.IO.Compression;
using icc;

namespace icc.Tests;

public class PngEmbeddingTests
{
    [Test]
    public void InsertsExtractsReplacesAndRemovesProfileWithoutChangingOtherChunks()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreatePng(Chunk("tEXt",
            System.Text.Encoding.ASCII.GetBytes("key\0value")));
        IccProfile first = CreateGrayProfile(2024);
        IccProfile second = CreateGrayProfile(2025);
        IccWriteOptions options = new IccWriteOptions();
        byte[] firstBytes = new IccProfileWriter().WriteProfileBytes(first,
            options);
        byte[] secondBytes = new IccProfileWriter().WriteProfileBytes(second,
            options);

        byte[] inserted = writer.InsertProfileBytes(original, first,
            IccEmbeddedFormat.Png, options);
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(inserted,
            new IccEmbeddingOptions());
        IccProfile extracted = reader.ExtractProfilesBytes(inserted,
            new IccReadOptions()).Single();
        byte[] replaced = writer.ReplaceProfileBytes(inserted, 0, second,
            IccEmbeddedFormat.Png, options);
        IccProfile replacement = reader.ExtractProfilesBytes(replaced,
            new IccReadOptions()).Single();
        byte[] removed = writer.RemoveProfileBytes(replaced, 0,
            IccEmbeddedFormat.Png);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Result.Message, Does.Contain("ICC Profile"));
            Assert.That(result.Options.Format, Is.EqualTo(IccEmbeddedFormat.Png));
            Assert.That(result.Profiles, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Format, Is.EqualTo(IccEmbeddedFormat.Png));
            Assert.That(result.Profiles[0].Index, Is.Zero);
            Assert.That(result.Profiles[0].Parts, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Location.Offset,
                Is.EqualTo(result.Profiles[0].Parts[0].Offset));
            Assert.That(result.Profiles[0].Profile.Location.Offset, Is.EqualTo(-1));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(firstBytes));
            Assert.That(replacement.OriginalBytes, Is.EqualTo(secondBytes));
            Assert.That(removed, Is.EqualTo(original));
            Assert.That(writer.ProbeEmbeddingOperation(IccEmbeddedFormat.Png,
                IccEmbeddedOperation.Replace).Status,
                Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void AcceptsFrameworkDynamicZlibAndLatinOneProfileNames()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] profileBytes = EncodeProfile(CreateGrayProfile(2026));
        byte[] name = new byte[] { 0x50, 0x72, 0xf3, 0x66 };
        byte[] carrier = CreatePng(IccpChunk(name, profileBytes, 0, true));

        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(carrier,
            new IccEmbeddingOptions());
        IccProfile extracted = reader.ExtractProfilesMemory(carrier,
            new IccReadOptions()).Single();
        byte[] replaced = writer.ReplaceProfileBytes(carrier, 0,
            CreateGrayProfile(2031), IccEmbeddedFormat.Png,
            new IccWriteOptions());
        IccEmbeddedProfileResult replacementResult =
            reader.EnumerateProfilesBytes(replaced, new IccEmbeddingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Result.Message, Does.Contain("Próf"));
            Assert.That(result.Profiles.Single().Profile.OriginalBytes,
                Is.EqualTo(profileBytes));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(profileBytes));
            Assert.That(replacementResult.Result.Message, Does.Contain("Próf"));
        });
    }

    [Test]
    public void StreamMutationRoundTripsAndHonorsCarrierLimits()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        byte[] original = CreatePng();
        IccProfile profile = CreateGrayProfile(2027);
        using MemoryStream source = new MemoryStream(original);
        using MemoryStream destination = new MemoryStream();
        writer.InsertProfileStream(source, destination, profile,
            IccEmbeddedFormat.Png, new IccWriteOptions());
        byte[] inserted = destination.ToArray();
        using MemoryStream removeSource = new MemoryStream(inserted);
        using MemoryStream removeDestination = new MemoryStream();
        writer.RemoveProfileStream(removeSource, removeDestination, 0,
            IccEmbeddedFormat.Png);
        IccWriteOptions limited = new IccWriteOptions();
        limited.ResourceLimits.MaximumCarrierBytes = original.Length;

        Assert.Multiple(() =>
        {
            Assert.That(reader.ExtractProfilesStream(new MemoryStream(inserted),
                new IccReadOptions()), Has.Length.EqualTo(1));
            Assert.That(removeDestination.ToArray(), Is.EqualTo(original));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                original, profile, IccEmbeddedFormat.Png, limited));
        });
    }

    [Test]
    public void RejectsInvalidProfileNamesCompressionCrcAndChunkOrder()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profileBytes = EncodeProfile(CreateGrayProfile(2028));
        byte[][] invalidNames = new byte[][]
        {
            new byte[0],
            System.Text.Encoding.Latin1.GetBytes(" leading"),
            System.Text.Encoding.Latin1.GetBytes("trailing "),
            System.Text.Encoding.Latin1.GetBytes("two  spaces"),
            new byte[] { 0x50, 0x80 },
            Enumerable.Repeat((byte)0x50, 80).ToArray()
        };

        foreach (byte[] name in invalidNames)
        {
            IccEmbeddedProfileResult invalidName = reader.EnumerateProfilesBytes(
                CreatePng(IccpChunk(name, profileBytes, 0, false)),
                new IccEmbeddingOptions());
            Assert.That(invalidName.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidName.Result.Message, Does.Contain("name"));
        }

        byte[] badMethod = CreatePng(IccpChunk(
            System.Text.Encoding.ASCII.GetBytes("Profile"), profileBytes, 1,
            false));
        byte[] damagedChunk = IccpChunk(
            System.Text.Encoding.ASCII.GetBytes("Profile"), profileBytes, 0,
            false);
        damagedChunk[^1] ^= 1;
        byte[] badCrc = CreatePng(damagedChunk);
        byte[] iccp = IccpChunk(System.Text.Encoding.ASCII.GetBytes("Profile"),
            profileBytes, 0, false);
        byte[] late = Join(PngSignature(), Ihdr(), Idat(), iccp, Iend());
        byte[] duplicate = CreatePng(iccp, iccp);
        byte[] splitData = Join(PngSignature(), Ihdr(), Idat(),
            Chunk("tEXt", new byte[] { 1 }), Idat(), Iend());

        Assert.Multiple(() =>
        {
            AssertInvalid(reader, badMethod, "compression method");
            AssertInvalid(reader, badCrc, "CRC-32");
            AssertInvalid(reader, late, "precede");
            AssertInvalid(reader, duplicate, "more than one");
            AssertInvalid(reader, splitData, "consecutive");
            Assert.Throws<IccEmbeddingException>(() => reader.ExtractProfilesBytes(
                late, new IccReadOptions()));
        });
    }

    [Test]
    public void BoundsDecompressionAndRejectsInvalidZlibStreams()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profileBytes = EncodeProfile(CreateGrayProfile(2029));
        byte[] valid = CreatePng(IccpChunk(
            System.Text.Encoding.ASCII.GetBytes("Profile"), profileBytes, 0,
            true));
        IccEmbeddingOptions limited = new IccEmbeddingOptions();
        limited.ResourceLimits.MaximumDecompressedBytes = profileBytes.Length - 1;
        IccEmbeddedProfileResult limitResult = reader.EnumerateProfilesBytes(
            valid, limited);
        byte[] invalidData = Join(
            System.Text.Encoding.ASCII.GetBytes("Profile"),
            new byte[] { 0, 0, 1, 2, 3, 4 });
        byte[] invalid = CreatePng(Chunk("iCCP", invalidData));
        IccEmbeddedProfileResult invalidResult = reader.EnumerateProfilesBytes(
            invalid, new IccEmbeddingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(limitResult.Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(invalidResult.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidResult.Result.Message, Does.Contain("zlib"));
        });
    }

    [Test]
    public void ReportsColorSignalingConflictsAndRefusesAmbiguousMutation()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfile profile = CreateGrayProfile(2030);
        byte[] profileBytes = EncodeProfile(profile);
        byte[] iccp = IccpChunk(System.Text.Encoding.ASCII.GetBytes("Profile"),
            profileBytes, 0, true);
        byte[] srgb = Chunk("sRGB", new byte[] { 0 });
        byte[] cicp = Chunk("cICP", new byte[] { 1, 13, 0, 1 });
        byte[] srgbConflict = CreatePng(srgb, iccp);
        byte[] cicpConflict = CreatePng(srgb, iccp, cicp);
        IccEmbeddedProfileResult srgbResult = reader.EnumerateProfilesBytes(
            srgbConflict, new IccEmbeddingOptions());
        IccEmbeddedProfileResult cicpResult = reader.EnumerateProfilesBytes(
            cicpConflict, new IccEmbeddingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(srgbResult.Result.Status,
                Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(srgbResult.Profiles, Has.Length.EqualTo(1));
            Assert.That(cicpResult.Result.Status,
                Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(cicpResult.Result.Message, Does.Contain("higher precedence"));
            Assert.That(reader.ExtractProfilesBytes(cicpConflict,
                new IccReadOptions()).Single().OriginalBytes,
                Is.EqualTo(profileBytes));
            Assert.Throws<IccEmbeddingException>(() => writer.ReplaceProfileBytes(
                srgbConflict, 0, profile, IccEmbeddedFormat.Png,
                new IccWriteOptions()));
            Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
                CreatePng(cicp), profile, IccEmbeddedFormat.Png,
                new IccWriteOptions()));
        });
    }

    [Test]
    public void RejectsProfilesThatDoNotMatchThePngColorType()
    {
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        IccProfile xyz = new IccProfileBuilder().CreateXyzIdentityProfile(options);

        Assert.Throws<IccEmbeddingException>(() => writer.InsertProfileBytes(
            CreatePng(), xyz, IccEmbeddedFormat.Png, new IccWriteOptions()));
    }

    [Test]
    public void DoesNotRawScanProfileBytesInsideAnUnrelatedPngChunk()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profileBytes = EncodeProfile(CreateGrayProfile(2032));
        byte[] carrier = CreatePng(Chunk("prIv", profileBytes));

        IccEmbeddedProfileResult result = reader.EnumerateProfilesMemory(
            carrier, new IccEmbeddingOptions());
        IccEmbeddingException error = Assert.Throws<IccEmbeddingException>(
            () => reader.ExtractProfilesMemory(carrier,
                new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Result.Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(result.Profiles, Is.Empty);
            Assert.That(result.Result.Message, Does.Contain("iCCP"));
            Assert.That(error.Message, Does.Contain("iCCP"));
        });
    }

    private static void AssertInvalid(IIccEmbeddedProfileReader reader,
        byte[] carrier, string messagePart)
    {
        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(carrier,
            new IccEmbeddingOptions());
        Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(result.Result.Message, Does.Contain(messagePart));
    }

    private static IccProfile CreateGrayProfile(int year)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.Display;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = year;
        options.GrayDefinition = new IccGrayProfileDefinition();
        options.GrayDefinition.WhitePoint.X =
            IccStandardColorimetry.D65ChromaticityX;
        options.GrayDefinition.WhitePoint.Y =
            IccStandardColorimetry.D65ChromaticityY;
        options.GrayDefinition.GrayCurve = new IccCurve
        {
            Kind = IccCurveKind.Gamma,
            Gamma = 2.2
        };
        return new IccProfileBuilder().CreateGrayProfile(options);
    }

    private static byte[] EncodeProfile(IccProfile profile)
    {
        return new IccProfileWriter().WriteProfileBytes(profile,
            new IccWriteOptions());
    }

    private static byte[] CreatePng(params byte[][] chunks)
    {
        List<byte[]> parts = new List<byte[]> { PngSignature(), Ihdr() };
        parts.AddRange(chunks);
        parts.Add(Idat());
        parts.Add(Iend());
        return Join(parts.ToArray());
    }

    private static byte[] PngSignature()
    {
        return new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
    }

    private static byte[] Ihdr()
    {
        byte[] data = new byte[13];
        WriteUInt32(data, 0, 1);
        WriteUInt32(data, 4, 1);
        data[8] = 8;
        data[9] = 0;
        return Chunk("IHDR", data);
    }

    private static byte[] Idat()
    {
        return Chunk("IDAT", new byte[] { 0 });
    }

    private static byte[] Iend()
    {
        return Chunk("IEND", new byte[0]);
    }

    private static byte[] IccpChunk(byte[] name, byte[] profileBytes,
        byte method, bool dynamicCompression)
    {
        byte[] compressed = dynamicCompression
            ? Compress(profileBytes) : StoredZlib(profileBytes);
        byte[] data = new byte[name.Length + compressed.Length + 2];
        System.Array.Copy(name, data, name.Length);
        data[name.Length] = 0;
        data[name.Length + 1] = method;
        System.Array.Copy(compressed, 0, data, name.Length + 2,
            compressed.Length);
        return Chunk("iCCP", data);
    }

    private static byte[] Compress(byte[] bytes)
    {
        using MemoryStream output = new MemoryStream();
        using (ZLibStream zlib = new ZLibStream(output,
            CompressionLevel.SmallestSize, true))
        {
            zlib.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    private static byte[] StoredZlib(byte[] bytes)
    {
        List<byte> result = new List<byte> { 0x78, 0x01 };
        int offset = 0;
        while (offset < bytes.Length)
        {
            int length = Math.Min(65535, bytes.Length - offset);
            bool final = offset + length == bytes.Length;
            result.Add(final ? (byte)1 : (byte)0);
            result.Add((byte)length);
            result.Add((byte)(length >> 8));
            int complement = length ^ 0xffff;
            result.Add((byte)complement);
            result.Add((byte)(complement >> 8));
            for (int index = 0; index < length; index++)
            {
                result.Add(bytes[offset + index]);
            }
            offset += length;
        }
        uint adler = ComputeAdler32(bytes);
        result.Add((byte)(adler >> 24));
        result.Add((byte)(adler >> 16));
        result.Add((byte)(adler >> 8));
        result.Add((byte)adler);
        return result.ToArray();
    }

    private static uint ComputeAdler32(byte[] bytes)
    {
        uint first = 1;
        uint second = 0;
        foreach (byte value in bytes)
        {
            first = (first + value) % 65521;
            second = (second + first) % 65521;
        }
        return (second << 16) | first;
    }

    private static byte[] Chunk(string type, byte[] data)
    {
        byte[] result = new byte[data.Length + 12];
        WriteUInt32(result, 0, (uint)data.Length);
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        System.Array.Copy(typeBytes, 0, result, 4, 4);
        System.Array.Copy(data, 0, result, 8, data.Length);
        WriteUInt32(result, data.Length + 8,
            ComputeCrc(result.AsSpan(4, data.Length + 4)));
        return result;
    }

    private static uint ComputeCrc(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xffffffff;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) == 0
                    ? crc >> 1 : (crc >> 1) ^ 0xedb88320;
            }
        }
        return crc ^ 0xffffffff;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static byte[] Join(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            System.Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }
        return result;
    }
}
