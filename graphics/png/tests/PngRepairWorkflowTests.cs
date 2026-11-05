using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngRepairWorkflowTests
{
    [Test]
    public void RepairBytes_PerformsEverySelectedRecoverableRepairAndReportsActions()
    {
        byte[] png = Join(
            CreatePng(
                CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData()),
                CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 1 }),
                CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 2 }),
                CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
                CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
                CreateChunkBytes(PngConstants.ChunkTime, new byte[] { 1 }),
                CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
                CreateChunkBytes(PngConstants.ChunkIEND, new byte[0])),
            new byte[] { 9, 8, 7 });
        PngRepairOptions options = CreateAllRepairOptions();
        IPngRepairer repairer = new PngRepairService();

        PngRepairResult result = repairer.RepairBytes(png, options);

        Assert.That(result.Report.Changed, Is.True);
        Assert.That(result.Report.BeforeValidation.IsValid, Is.False);
        Assert.That(result.Report.AfterValidation.IsValid, Is.True);
        Assert.That(result.Report.HasUnrepairedCriticalErrors, Is.False);
        Assert.That(HasAction(result, "PNG_REPAIR_TRAILING_DATA_REMOVED"), Is.True);
        Assert.That(HasAction(result, "PNG_REPAIR_MALFORMED_ANCILLARY_REMOVED"), Is.True);
        Assert.That(HasAction(result, "PNG_REPAIR_DUPLICATE_ANCILLARY_REMOVED"), Is.True);
        Assert.That(HasAction(result, "PNG_REPAIR_ANCILLARY_MOVED"), Is.True);
        Assert.That(HasAction(result, "PNG_REPAIR_ANCILLARY_CRC_RECALCULATED"), Is.True);
        Assert.That(CountChunks(result.Chunks, PngConstants.ChunkGamma), Is.EqualTo(1));
        Assert.That(CountChunks(result.Chunks, PngConstants.ChunkTime), Is.EqualTo(0));
    }

    [Test]
    public void RepairBytes_DoesNotPerformRepairsThatWereNotSelected()
    {
        byte[] png = Join(CreateValidPngWithBadTextCrc(), new byte[] { 4, 5 });
        PngRepairOptions options = new PngRepairOptions();
        IPngRepairer repairer = new PngRepairService();

        PngRepairResult result = repairer.RepairBytes(png, options);

        Assert.That(result.Data, Is.EqualTo(png));
        Assert.That(result.Report.Changed, Is.False);
        Assert.That(result.Report.Actions, Is.Empty);
        Assert.That(result.Report.AfterValidation.IsValid, Is.False);
    }

    [Test]
    public void RepairBytes_PreservesCriticalImageDataCorruptionAndReportsIt()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 }, 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngRepairOptions options = new PngRepairOptions();
        options.RepairAncillaryCrc = true;
        IPngRepairer repairer = new PngRepairService();

        PngRepairResult result = repairer.RepairBytes(png, options);

        Assert.That(HasAction(result, "PNG_REPAIR_ANCILLARY_CRC_RECALCULATED"), Is.True);
        Assert.That(result.Report.AfterValidation.IsValid, Is.False);
        Assert.That(result.Report.HasUnrepairedCriticalErrors, Is.True);
        Assert.That(HasError(result.Report.AfterValidation, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkIDAT), Is.True);
        Assert.That(HasError(result.Report.AfterValidation, "PNG_IDAT_ZLIB_INVALID", PngConstants.ChunkIDAT), Is.True);
    }

    [Test]
    public void RepairBytes_DoesNotRemoveMalformedApngImageDataChunks()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 }),
            CreateChunkBytes(PngConstants.ChunkFrameData, new byte[] { 0 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngRepairOptions options = new PngRepairOptions();
        options.RemoveMalformedAncillaryChunks = true;
        IPngRepairer repairer = new PngRepairService();

        PngRepairResult result = repairer.RepairBytes(png, options);

        Assert.That(CountChunks(result.Chunks, PngConstants.ChunkFrameData), Is.EqualTo(1));
        Assert.That(result.Report.Changed, Is.False);
        Assert.That(result.Report.HasUnrepairedCriticalErrors, Is.True);
    }

    [Test]
    public void RepairService_SupportsOffsetStreamAndChunkInputs()
    {
        byte[] png = Join(CreateValidPng(), new byte[] { 1, 2 });
        byte[] wrapped = Join(new byte[] { 7, 6 }, png);
        PngRepairOptions trailingOptions = new PngRepairOptions();
        trailingOptions.RemoveTrailingData = true;
        PngRepairOptions chunkOptions = new PngRepairOptions();
        chunkOptions.RemoveDuplicateAncillaryChunks = true;
        IPngRepairer repairer = new PngRepairService();
        PngChunk[] duplicateChunks = Png.ReadChunks(CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 1 }),
            CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 2 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]))).Chunks;

        PngRepairResult offsetResult = repairer.RepairBytesAtOffset(wrapped, 2, trailingOptions);
        PngRepairResult streamResult = repairer.RepairStream(new MemoryStream(png), trailingOptions);
        PngRepairResult chunkResult = repairer.RepairChunks(duplicateChunks, chunkOptions);

        Assert.That(offsetResult.Report.AfterValidation.IsValid, Is.True);
        Assert.That(streamResult.Report.AfterValidation.IsValid, Is.True);
        Assert.That(CountChunks(chunkResult.Chunks, PngConstants.ChunkGamma), Is.EqualTo(1));
        Assert.That(chunkResult.Report.AfterValidation.IsValid, Is.True);
    }

    [Test]
    public void RepairService_RejectsInvalidInputs()
    {
        IPngRepairer repairer = new PngRepairService();
        PngRepairOptions nullLimits = new PngRepairOptions();
        nullLimits.Limits = null!;

        Assert.Throws<ArgumentNullException>(() => repairer.RepairBytes(null!, new PngRepairOptions()));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairBytes(CreateValidPng(), null!));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairBytes(CreateValidPng(), nullLimits));
        Assert.Throws<ArgumentOutOfRangeException>(() => repairer.RepairBytesAtOffset(CreateValidPng(), -1, new PngRepairOptions()));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairStream(null!, new PngRepairOptions()));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairStream(new MemoryStream(CreateValidPng()), null!));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairChunks(null!, new PngRepairOptions()));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairChunks(Png.ReadChunks(CreateValidPng()).Chunks, null!));
    }

    static PngRepairOptions CreateAllRepairOptions()
    {
        PngRepairOptions options = new PngRepairOptions();
        options.RemoveTrailingData = true;
        options.RemoveMalformedAncillaryChunks = true;
        options.RemoveDuplicateAncillaryChunks = true;
        options.RepairAncillaryOrdering = true;
        options.RepairAncillaryCrc = true;
        return options;
    }

    static byte[] CreateValidPng()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    static byte[] CreateValidPngWithBadTextCrc()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    static byte[] CreateIhdrData()
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, 1);
        PngBinaryWriter.WriteUInt32(data, 4, 1);
        data[8] = PngConstants.BitDepth8;
        data[9] = PngConstants.ColorTypeTruecolorAlpha;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = PngConstants.InterlaceMethodNone;
        return data;
    }

    static byte[] CreateValidIdatData()
    {
        return PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 });
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] data)
    {
        return CreateChunkBytesWithCrc(chunkType, data, PngCrc32.CalculateChunkCrc(chunkType, data));
    }

    static byte[] CreateChunkBytesWithCrc(string chunkType, byte[] data, uint crc)
    {
        byte[] result = new byte[data.Length + 12];
        PngBinaryWriter.WriteUInt32(result, 0, (uint)data.Length);
        result[4] = (byte)chunkType[0];
        result[5] = (byte)chunkType[1];
        result[6] = (byte)chunkType[2];
        result[7] = (byte)chunkType[3];
        data.CopyTo(result, 8);
        PngBinaryWriter.WriteUInt32(result, 8 + data.Length, crc);
        return result;
    }

    static byte[] CreatePng(params byte[][] chunks)
    {
        byte[][] arrays = new byte[chunks.Length + 1][];
        arrays[0] = CreateSignature();
        int index = 0;
        while (index < chunks.Length)
        {
            arrays[index + 1] = chunks[index];
            index += 1;
        }

        return Join(arrays);
    }

    static byte[] CreateSignature()
    {
        return new byte[]
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
    }

    static byte[] Join(params byte[][] arrays)
    {
        int length = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            length += arrays[index].Length;
            index += 1;
        }

        byte[] result = new byte[length];
        int offset = 0;
        index = 0;
        while (index < arrays.Length)
        {
            arrays[index].CopyTo(result, offset);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }

    static bool HasAction(PngRepairResult result, string code)
    {
        int index = 0;
        while (index < result.Report.Actions.Length)
        {
            if (result.Report.Actions[index].Code == code)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static bool HasError(PngValidationResult result, string code, string chunkType)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code && result.Errors[index].ChunkType == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static int CountChunks(PngChunk[] chunks, string chunkType)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }
}
