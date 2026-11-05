using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChunkDuplicateValidatorTests
{
    [Test]
    public void Validate_AllowsRepeatableChunkTypes()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIDAT, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkText, 24),
            CreateChunk(PngConstants.ChunkText, 36),
            CreateChunk(PngConstants.ChunkSuggestedPalette, 48),
            CreateChunk(PngConstants.ChunkSuggestedPalette, 60),
            CreateChunk(PngConstants.ChunkFrameControl, 72),
            CreateChunk(PngConstants.ChunkFrameControl, 84),
            CreateChunk(PngConstants.ChunkFrameData, 96),
            CreateChunk(PngConstants.ChunkFrameData, 108)
        };

        PngValidationResult result = PngChunkDuplicateValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_ReportsDuplicateSingletonChunks()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkIHDR, 12),
            CreateChunk(PngConstants.ChunkPLTE, 24),
            CreateChunk(PngConstants.ChunkPLTE, 36),
            CreateChunk(PngConstants.ChunkGamma, 48),
            CreateChunk(PngConstants.ChunkGamma, 60),
            CreateChunk(PngConstants.ChunkPhysicalPixels, 72),
            CreateChunk(PngConstants.ChunkPhysicalPixels, 84),
            CreateChunk(PngConstants.ChunkIEND, 96),
            CreateChunk(PngConstants.ChunkIEND, 108)
        };

        PngValidationResult result = PngChunkDuplicateValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(CountErrors(result, "PNG_CHUNK_DUPLICATE"), Is.EqualTo(5));
        Assert.That(HasDuplicateError(result, PngConstants.ChunkIHDR, 12), Is.True);
        Assert.That(HasDuplicateError(result, PngConstants.ChunkPLTE, 36), Is.True);
        Assert.That(HasDuplicateError(result, PngConstants.ChunkGamma, 60), Is.True);
        Assert.That(HasDuplicateError(result, PngConstants.ChunkPhysicalPixels, 84), Is.True);
        Assert.That(HasDuplicateError(result, PngConstants.ChunkIEND, 108), Is.True);
    }

    [Test]
    public void Validate_ReportsEveryDuplicateAfterFirstOccurrence()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, 0),
            CreateChunk(PngConstants.ChunkGamma, 12),
            CreateChunk(PngConstants.ChunkGamma, 24)
        };

        PngValidationResult result = PngChunkDuplicateValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(2));
        Assert.That(HasDuplicateError(result, PngConstants.ChunkGamma, 12), Is.True);
        Assert.That(HasDuplicateError(result, PngConstants.ChunkGamma, 24), Is.True);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_DUPLICATE"));
        Assert.That(result.Errors[0].Message, Is.EqualTo("PNG chunk type must not appear more than once."));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [Test]
    public void Validate_AcceptsChunkListResult()
    {
        PngChunkListResult chunkList = new PngChunkListResult();
        chunkList.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkIEND, 24)
        };

        PngValidationResult result = PngChunkDuplicateValidator.Validate(chunkList);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void MustAppearAtMostOnce_ClassifiesSingletonAndRepeatableChunks()
    {
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkIHDR), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkPLTE), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkIEND), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkTransparency), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkChromaticities), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkGamma), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkIccProfile), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkSignificantBits), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkStandardRgb), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkCodingIndependentCodePoints), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkMasteringDisplayColorVolume), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkContentLightLevelInformation), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkBackgroundColor), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkPaletteHistogram), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkPhysicalPixels), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkExif), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkTime), Is.True);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkAnimationControl), Is.True);

        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkIDAT), Is.False);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkText), Is.False);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkCompressedText), Is.False);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkInternationalText), Is.False);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkSuggestedPalette), Is.False);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkFrameControl), Is.False);
        Assert.That(PngChunkDuplicateValidator.MustAppearAtMostOnce(PngConstants.ChunkFrameData), Is.False);
    }

    [Test]
    public void Validate_RejectsInvalidInputs()
    {
        PngChunk nullType = CreateChunk(PngConstants.ChunkIHDR, 0);
        nullType.Type = null!;
        PngChunkListResult nullChunks = new PngChunkListResult();
        nullChunks.Chunks = null!;

        Assert.Throws<ArgumentNullException>(() => PngChunkDuplicateValidator.Validate((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkDuplicateValidator.Validate((PngChunkListResult)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkDuplicateValidator.Validate(nullChunks));
        Assert.Throws<ArgumentNullException>(() => PngChunkDuplicateValidator.Validate(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngChunkDuplicateValidator.Validate(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngChunkDuplicateValidator.MustAppearAtMostOnce(null!));
    }

    static PngChunk CreateChunk(string chunkType, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Offset = offset;
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

    static bool HasDuplicateError(PngValidationResult result, string chunkType, long offset)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].ChunkType == chunkType && result.Errors[index].Offset == offset)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
