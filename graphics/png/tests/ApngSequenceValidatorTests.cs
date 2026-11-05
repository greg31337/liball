using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngSequenceValidatorTests
{
    [Test]
    public void Validate_AcceptsConsecutiveFrameControlAndFrameDataSequence()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateFrameData(1, 48),
            CreateFrameData(2, 72),
            CreateFrameControl(3, 96)
        };

        PngValidationResult result = PngApngSequenceValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_IgnoresNonSequenceChunks()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0, new byte[13]),
            CreateFrameControl(0, 12),
            CreateChunk(PngConstants.ChunkIDAT, 48, new byte[] { 1, 2 }),
            CreateFrameData(1, 72),
            CreateChunk(PngConstants.ChunkIEND, 96, new byte[0])
        };

        PngValidationResult result = PngApngSequenceValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_ReportsFirstSequenceNumberThatDoesNotStartAtZero()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateFrameControl(1, 12),
            CreateFrameData(1, 48)
        };

        PngValidationResult result = PngApngSequenceValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(CountErrors(result, "PNG_APNG_SEQUENCE_NUMBER_INVALID"), Is.EqualTo(1));
        Assert.That(HasError(result, "PNG_APNG_SEQUENCE_NUMBER_INVALID", PngConstants.ChunkFrameControl, 12), Is.True);
    }

    [Test]
    public void Validate_ReportsGapsAndDuplicateSequenceNumbers()
    {
        PngChunk[] gap = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateFrameData(2, 48)
        };
        PngChunk[] duplicate = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateFrameData(0, 48)
        };

        PngValidationResult gapResult = PngApngSequenceValidator.Validate(gap);
        PngValidationResult duplicateResult = PngApngSequenceValidator.Validate(duplicate);

        Assert.That(gapResult.IsValid, Is.False);
        Assert.That(HasError(gapResult, "PNG_APNG_SEQUENCE_NUMBER_INVALID", PngConstants.ChunkFrameData, 48), Is.True);
        Assert.That(duplicateResult.IsValid, Is.False);
        Assert.That(HasError(duplicateResult, "PNG_APNG_SEQUENCE_NUMBER_INVALID", PngConstants.ChunkFrameData, 48), Is.True);
    }

    [Test]
    public void Validate_UsesSingleGlobalSequenceAcrossFrameControlAndFrameDataChunks()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateFrameData(1, 48),
            CreateFrameControl(2, 84),
            CreateFrameData(3, 120)
        };
        PngChunk[] outOfOrder = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateFrameData(2, 48),
            CreateFrameControl(1, 84)
        };

        PngValidationResult validResult = PngApngSequenceValidator.Validate(chunks);
        PngValidationResult invalidResult = PngApngSequenceValidator.Validate(outOfOrder);

        Assert.That(validResult.IsValid, Is.True);
        Assert.That(validResult.Errors, Is.Empty);
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(CountErrors(invalidResult, "PNG_APNG_SEQUENCE_NUMBER_INVALID"), Is.EqualTo(2));
        Assert.That(HasError(invalidResult, "PNG_APNG_SEQUENCE_NUMBER_INVALID", PngConstants.ChunkFrameData, 48), Is.True);
        Assert.That(HasError(invalidResult, "PNG_APNG_SEQUENCE_NUMBER_INVALID", PngConstants.ChunkFrameControl, 84), Is.True);
    }

    [Test]
    public void Validate_AcceptsFrameDataPayloadWithOnlySequenceNumber()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateChunk(PngConstants.ChunkFrameData, 48, CreateSequencePayload(1))
        };

        PngValidationResult result = PngApngSequenceValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_ReportsMalformedSequenceChunkPayloads()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkFrameControl, 12, new byte[25]),
            CreateChunk(PngConstants.ChunkFrameData, 48, new byte[3]),
            CreateChunk(PngConstants.ChunkFrameControl, 84, new byte[27])
        };

        PngValidationResult result = PngApngSequenceValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(CountErrors(result, "PNG_APNG_SEQUENCE_CHUNK_LENGTH_INVALID"), Is.EqualTo(3));
        Assert.That(HasError(result, "PNG_APNG_SEQUENCE_CHUNK_LENGTH_INVALID", PngConstants.ChunkFrameControl, 12), Is.True);
        Assert.That(HasError(result, "PNG_APNG_SEQUENCE_CHUNK_LENGTH_INVALID", PngConstants.ChunkFrameData, 48), Is.True);
        Assert.That(HasError(result, "PNG_APNG_SEQUENCE_CHUNK_LENGTH_INVALID", PngConstants.ChunkFrameControl, 84), Is.True);
    }

    [Test]
    public void Validate_AcceptsChunkListResult()
    {
        PngChunkListResult chunkList = new PngChunkListResult();
        chunkList.Chunks = new PngChunk[]
        {
            CreateFrameControl(0, 12),
            CreateFrameData(1, 48)
        };

        PngValidationResult result = PngApngSequenceValidator.Validate(chunkList);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void IsSequenceChunk_ClassifiesApngSequenceChunks()
    {
        Assert.That(PngApngSequenceValidator.IsSequenceChunk(PngConstants.ChunkFrameControl), Is.True);
        Assert.That(PngApngSequenceValidator.IsSequenceChunk(PngConstants.ChunkFrameData), Is.True);
        Assert.That(PngApngSequenceValidator.IsSequenceChunk(PngConstants.ChunkAnimationControl), Is.False);
        Assert.That(PngApngSequenceValidator.IsSequenceChunk(PngConstants.ChunkIDAT), Is.False);
    }

    [Test]
    public void Validate_RejectsInvalidInputs()
    {
        PngChunk nullType = CreateFrameControl(0, 12);
        nullType.Type = null!;
        PngChunk nullData = CreateFrameControl(0, 12);
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngApngSequenceValidator.Validate((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngApngSequenceValidator.Validate((PngChunkListResult)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngSequenceValidator.Validate(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngApngSequenceValidator.Validate(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngApngSequenceValidator.Validate(new PngChunk[] { nullData }));
        Assert.Throws<ArgumentNullException>(() => PngApngSequenceValidator.IsSequenceChunk(null!));
    }

    static PngChunk CreateFrameControl(uint sequenceNumber, long offset)
    {
        byte[] data = PngFrameControlWriter.WriteData(CreateFrameControlModel(sequenceNumber));
        return CreateChunk(PngConstants.ChunkFrameControl, offset, data);
    }

    static PngChunk CreateFrameData(uint sequenceNumber, long offset)
    {
        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = sequenceNumber;
        frameData.Data = new byte[] { 1, 2, 3 };
        return CreateChunk(PngConstants.ChunkFrameData, offset, PngFrameDataWriter.WriteData(frameData));
    }

    static byte[] CreateSequencePayload(uint sequenceNumber)
    {
        byte[] data = new byte[PngFrameDataParser.SequenceNumberLength];
        PngBinaryWriter.WriteUInt32(data, 0, sequenceNumber);
        return data;
    }

    static PngFrameControl CreateFrameControlModel(uint sequenceNumber)
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = sequenceNumber;
        control.Width = 1;
        control.Height = 1;
        control.XOffset = 0;
        control.YOffset = 0;
        control.DelayNumerator = 1;
        control.DelayDenominator = 24;
        control.DisposeOperation = PngApngDisposeOperation.None;
        control.BlendOperation = PngApngBlendOperation.Source;
        return control;
    }

    static PngChunk CreateChunk(string chunkType, long offset, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Offset = offset;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        return chunk;
    }

    static int CountErrors(PngValidationResult result, string code)
    {
        int count = 0;
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static bool HasError(PngValidationResult result, string code, string chunkType, long offset)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code && result.Errors[index].ChunkType == chunkType && result.Errors[index].Offset == offset)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
