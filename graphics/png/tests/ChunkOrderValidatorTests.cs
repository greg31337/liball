using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChunkOrderValidatorTests
{
    [Test]
    public void Validate_AcceptsValidCriticalAndAncillaryOrder()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkGamma, 12),
            CreateChunk(PngConstants.ChunkPLTE, 24),
            CreateChunk(PngConstants.ChunkTransparency, 36),
            CreateChunk(PngConstants.ChunkIDAT, 48),
            CreateChunk(PngConstants.ChunkIDAT, 60),
            CreateChunk(PngConstants.ChunkText, 72),
            CreateChunk(PngConstants.ChunkIEND, 84)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_RejectsEmptyChunkList()
    {
        PngValidationResult result = PngChunkOrderValidator.Validate(new PngChunk[0]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_ORDER_EMPTY"));
        Assert.That(result.Errors[0].ChunkType, Is.EqualTo(""));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(0));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [Test]
    public void Validate_RejectsIhdrWhenMissingOrNotFirst()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkPLTE, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkIEND, 24)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IHDR_NOT_FIRST"), Is.True);
        Assert.That(HasError(result, "PNG_PLTE_BEFORE_IHDR"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_MISSING"), Is.True);
    }

    [Test]
    public void Validate_RejectsPlteAfterIdat()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkPLTE, 24),
            CreateChunk(PngConstants.ChunkIEND, 36)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_PLTE_AFTER_IDAT"), Is.True);
    }

    [Test]
    public void Validate_RejectsIdatBeforeIhdr()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIDAT, 0),
            CreateChunk(PngConstants.ChunkIEND, 12)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IHDR_NOT_FIRST"), Is.True);
        Assert.That(HasError(result, "PNG_IDAT_BEFORE_IHDR"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_MISSING"), Is.True);
    }

    [Test]
    public void Validate_RejectsNonConsecutiveIdatChunks()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkText, 24),
            CreateChunk(PngConstants.ChunkIDAT, 36),
            CreateChunk(PngConstants.ChunkIEND, 48)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IDAT_NOT_CONSECUTIVE"), Is.True);
    }

    [Test]
    public void Validate_RejectsChunksAfterIend()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkIEND, 24),
            CreateChunk(PngConstants.ChunkText, 36)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IEND_NOT_LAST"), Is.True);
        Assert.That(HasError(result, "PNG_CHUNK_AFTER_IEND"), Is.True);
    }

    [Test]
    public void Validate_RejectsAncillaryPlacementAfterPlteOrIdat()
    {
        PngChunk[] afterPlte = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkPLTE, 12),
            CreateChunk(PngConstants.ChunkGamma, 24),
            CreateChunk(PngConstants.ChunkIDAT, 36),
            CreateChunk(PngConstants.ChunkIEND, 48)
        };
        PngChunk[] afterIdat = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkIDAT, 12),
            CreateChunk(PngConstants.ChunkPhysicalPixels, 24),
            CreateChunk(PngConstants.ChunkIEND, 36)
        };

        PngValidationResult afterPlteResult = PngChunkOrderValidator.Validate(afterPlte);
        PngValidationResult afterIdatResult = PngChunkOrderValidator.Validate(afterIdat);

        Assert.That(afterPlteResult.IsValid, Is.False);
        Assert.That(HasError(afterPlteResult, "PNG_ANCILLARY_AFTER_PLTE"), Is.True);
        Assert.That(afterIdatResult.IsValid, Is.False);
        Assert.That(HasError(afterIdatResult, "PNG_ANCILLARY_AFTER_IDAT"), Is.True);
    }

    [Test]
    public void Validate_ReportsBothAncillaryPlacementErrorsWhenRequiredBeforePlteAndIdat()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0),
            CreateChunk(PngConstants.ChunkPLTE, 12),
            CreateChunk(PngConstants.ChunkIDAT, 24),
            CreateChunk(PngConstants.ChunkGamma, 36),
            CreateChunk(PngConstants.ChunkIEND, 48)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_ANCILLARY_AFTER_PLTE"), Is.True);
        Assert.That(HasError(result, "PNG_ANCILLARY_AFTER_IDAT"), Is.True);
        Assert.That(HasError(result, "PNG_ANCILLARY_AFTER_IDAT", PngConstants.ChunkGamma, 36), Is.True);
    }

    [Test]
    public void Validate_ReportsMissingIdatAndIend()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, 0)
        };

        PngValidationResult result = PngChunkOrderValidator.Validate(chunks);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IDAT_MISSING"), Is.True);
        Assert.That(HasError(result, "PNG_IEND_MISSING"), Is.True);
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

        PngValidationResult result = PngChunkOrderValidator.Validate(chunkList);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_RejectsInvalidInputs()
    {
        PngChunk nullType = CreateChunk(PngConstants.ChunkIHDR, 0);
        nullType.Type = null!;

        Assert.Throws<ArgumentNullException>(() => PngChunkOrderValidator.Validate((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkOrderValidator.Validate((PngChunkListResult)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkOrderValidator.Validate(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngChunkOrderValidator.Validate(new PngChunk[] { nullType }));
    }

    static PngChunk CreateChunk(string chunkType, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Offset = offset;
        return chunk;
    }

    static bool HasError(PngValidationResult result, string code)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static bool HasError(PngValidationResult result, string code, string chunkType, long offset)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            PngError error = result.Errors[index];
            if (error.Code == code && error.ChunkType == chunkType && error.Offset == offset && error.IsFatal)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
