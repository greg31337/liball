using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class IdentifierTests
{
    [Test]
    public void PngIdentifyResult_UsesPublicFieldsOnly()
    {
        Assert.That(typeof(PngIdentifyResult).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngIdentifyResult).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngIdentifyResult).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngIdentifyResult).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(9));
    }

    [Test]
    public void Identify_ArrayEnforcesAggregateIdatLimitWithoutAllocatingIdat()
    {
        byte[] ihdr = CreateIhdrData(32, 16, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolorAlpha);
        byte[] phys = new byte[] { 0, 0, 11, 19, 0, 0, 11, 19, 1 };
        byte[] actl = new byte[] { 0, 0, 0, 2, 0, 0, 0, 0 };
        byte[] idat = new byte[] { 120, 156, 3 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, ihdr),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, phys),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, idat),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxIdatBytes = 1;

        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(png, 0, options));
    }

    [Test]
    public void Identify_StreamStopsAtIendAndLeavesTrailingBytesUnread()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 2, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3, 4, 5 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]),
            new byte[] { 9, 8, 7 });
        MemoryStream stream = new MemoryStream(png);

        PngIdentifyResult result = PngIdentifier.Identify(stream);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IdatBytes, Is.EqualTo(5));
        Assert.That(stream.Position, Is.EqualTo(png.Length - 3));
    }

    [Test]
    public void Identify_TracksUnknownChunksWhenPreserved()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor)),
            CreateChunkBytes("vpAg", new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions preserve = new PngDecoderOptions();
        preserve.PreserveUnknownChunks = true;
        PngDecoderOptions drop = new PngDecoderOptions();
        drop.PreserveUnknownChunks = false;

        PngIdentifyResult preservedResult = PngIdentifier.Identify(png, 0, preserve);
        PngIdentifyResult dropResult = PngIdentifier.Identify(png, 0, drop);

        Assert.That(preservedResult.Metadata.UnknownChunks, Has.Length.EqualTo(1));
        Assert.That(preservedResult.Metadata.UnknownChunks[0].Type, Is.EqualTo("vpAg"));
        Assert.That(dropResult.Metadata.UnknownChunks, Is.Empty);
        Assert.That(HasChunk(dropResult.Metadata.Chunks, "vpAg"), Is.True);
    }

    [Test]
    public void Identify_InvalidSignatureReturnsFailureResult()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PngIdentifyResult result = PngIdentifier.Identify(data);

        Assert.That(result.IsPng, Is.False);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SIGNATURE_TOO_SHORT"));
    }

    [Test]
    public void Identify_ReportsMissingIhdrAndMissingIend()
    {
        byte[] missingIhdr = CreatePng(CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] missingIend = CreatePng(CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor)));

        PngIdentifyResult missingIhdrResult = PngIdentifier.Identify(missingIhdr);
        PngIdentifyResult missingIendResult = PngIdentifier.Identify(missingIend);

        Assert.That(missingIhdrResult.IsValid, Is.False);
        Assert.That(missingIhdrResult.Errors[0].Code, Is.EqualTo("PNG_IHDR_MISSING"));
        Assert.That(missingIendResult.IsValid, Is.True);
        Assert.That(missingIendResult.FoundIend, Is.False);
        Assert.That(missingIendResult.Warnings, Has.Length.EqualTo(1));
        Assert.That(missingIendResult.Warnings[0].Code, Is.EqualTo("PNG_IEND_MISSING"));
    }

    [Test]
    public void Identify_EnforcesMetadataAndTextByteLimits()
    {
        byte[] metadataPng = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor)),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] textPng = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor)),
            CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65, 66, 67 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions metadataOptions = new PngDecoderOptions();
        metadataOptions.Limits.MaxMetadataBytes = 2;
        PngDecoderOptions textOptions = new PngDecoderOptions();
        textOptions.Limits.MaxTextBytes = 2;

        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(metadataPng, 0, metadataOptions));
        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(textPng, 0, textOptions));
    }

    [Test]
    public void Identify_TracksAndEnforcesTextEntryCountLimit()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor)),
            CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 }),
            CreateChunkBytes(PngConstants.ChunkCompressedText, new byte[] { 66 }),
            CreateChunkBytes(PngConstants.ChunkInternationalText, new byte[] { 67 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxTextEntries = 2;
        PngDecoderOptions unlimited = new PngDecoderOptions();
        unlimited.Limits.MaxTextEntries = 0;

        PngIdentifyResult result = PngIdentifier.Identify(png, 0, unlimited);

        Assert.That(result.Metadata.TextBytes, Is.EqualTo(3));
        Assert.That(result.Metadata.TextEntryCount, Is.EqualTo(3));
        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(png, 0, options));
    }

    [Test]
    public void Identify_EnforcesAnimationFrameCountLimit()
    {
        byte[] actl = new byte[] { 0, 0, 0, 2, 0, 0, 0, 0 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolorAlpha)),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxAnimationFrames = 1;

        Assert.Throws<OverflowException>(() => PngIdentifier.Identify(png, 0, options));
    }

    [Test]
    public void Identify_ReportsZeroAnimationFrameCount()
    {
        byte[] actl = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolorAlpha)),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngIdentifyResult result = PngIdentifier.Identify(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_ACTL_FRAME_COUNT_INVALID"));
        Assert.That(result.Info.IsAnimated, Is.False);
    }

    [Test]
    public void Identify_RejectsInvalidInputs()
    {
        byte[] data = CreatePng(CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        Assert.Throws<ArgumentNullException>(() => PngIdentifier.Identify((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngIdentifier.Identify(data, 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngIdentifier.Identify((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngIdentifier.Identify(new MemoryStream(data), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIdentifier.Identify(data, -1));
    }

    static byte[] CreateIhdrData(int width, int height, int bitDepth, int colorType)
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, (uint)width);
        PngBinaryWriter.WriteUInt32(data, 4, (uint)height);
        data[8] = (byte)bitDepth;
        data[9] = (byte)colorType;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = PngConstants.InterlaceMethodNone;
        return data;
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = chunkData;
        return PngRawChunkWriter.WriteToBytes(chunk);
    }

    static byte[] CreatePng(params byte[][] chunks)
    {
        byte[] signature = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };

        byte[][] arrays = new byte[chunks.Length + 1][];
        arrays[0] = signature;
        int index = 0;
        while (index < chunks.Length)
        {
            arrays[index + 1] = chunks[index];
            index += 1;
        }

        return Join(arrays);
    }

    static byte[] Join(params byte[][] arrays)
    {
        int totalLength = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            totalLength += arrays[index].Length;
            index += 1;
        }

        byte[] result = new byte[totalLength];
        int offset = 0;
        index = 0;
        while (index < arrays.Length)
        {
            Array.Copy(arrays[index], 0, result, offset, arrays[index].Length);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }

    static bool HasChunk(PngChunk[] chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
