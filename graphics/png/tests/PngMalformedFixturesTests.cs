using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngMalformedFixturesTests
{
    [Test]
    public void Fixtures_AreDeterministic()
    {
        byte[] invalidSignature = PngMalformedFixtures.CreateInvalidSignature();
        byte[] missingIend = PngMalformedFixtures.CreateMissingIend();
        byte[] badCriticalCrc = PngMalformedFixtures.CreateBadCriticalCrc();
        byte[] badAncillaryCrc = PngMalformedFixtures.CreateBadAncillaryCrc();
        byte[] ancillaryAfterIdat = PngMalformedFixtures.CreateAncillaryAfterIdat();
        byte[] plteAfterIdat = PngMalformedFixtures.CreatePlteAfterIdat();
        byte[] truncatedChunkData = PngMalformedFixtures.CreateTruncatedChunkData();

        Assert.That(PngMalformedFixtures.CreateInvalidSignature(), Is.EqualTo(invalidSignature));
        Assert.That(PngMalformedFixtures.CreateMissingIend(), Is.EqualTo(missingIend));
        Assert.That(PngMalformedFixtures.CreateBadCriticalCrc(), Is.EqualTo(badCriticalCrc));
        Assert.That(PngMalformedFixtures.CreateBadAncillaryCrc(), Is.EqualTo(badAncillaryCrc));
        Assert.That(PngMalformedFixtures.CreateAncillaryAfterIdat(), Is.EqualTo(ancillaryAfterIdat));
        Assert.That(PngMalformedFixtures.CreatePlteAfterIdat(), Is.EqualTo(plteAfterIdat));
        Assert.That(PngMalformedFixtures.CreateTruncatedChunkData(), Is.EqualTo(truncatedChunkData));
    }

    [Test]
    public void InvalidSignatureFixture_ReportsSignatureMismatch()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateInvalidSignature());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_SIGNATURE_MISMATCH", ""), Is.True);
    }

    [Test]
    public void TruncatedSignatureFixture_ReportsShortSignature()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateTruncatedSignature());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_SIGNATURE_TOO_SHORT", ""), Is.True);
    }

    [Test]
    public void MissingIhdrFixture_ReportsMissingIhdr()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateMissingIhdr());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IHDR_NOT_FIRST", PngConstants.ChunkIDAT), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_MISSING", ""), Is.True);
    }

    [Test]
    public void MissingIendFixture_ReportsMissingIend()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateMissingIend());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IEND_MISSING", ""), Is.True);
    }

    [Test]
    public void BadCriticalCrcFixture_ReportsCriticalCrcMismatch()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateBadCriticalCrc());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkIDAT), Is.True);
    }

    [Test]
    public void BadAncillaryCrcFixture_ReportsAncillaryCrcMismatch()
    {
        PngValidationResult strict = PngValidator.ValidateStrict(PngMalformedFixtures.CreateBadAncillaryCrc());
        PngValidationResult tolerant = PngValidator.ValidateTolerantMetadata(PngMalformedFixtures.CreateBadAncillaryCrc());

        Assert.That(strict.IsValid, Is.False);
        Assert.That(HasError(strict, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkText), Is.True);
        Assert.That(tolerant.IsValid, Is.True);
        Assert.That(HasWarning(tolerant, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkText), Is.True);
    }

    [Test]
    public void AncillaryAfterIdatFixture_ReportsAncillaryOrderingError()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateAncillaryAfterIdat());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_ANCILLARY_AFTER_IDAT", PngConstants.ChunkPhysicalPixels), Is.True);
    }

    [Test]
    public void PlteAfterIdatFixture_ReportsStructuralOrderingError()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreatePlteAfterIdat());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_PLTE_AFTER_IDAT", PngConstants.ChunkPLTE), Is.True);
    }

    [Test]
    public void TruncatedChunkDataFixture_CausesChunkReaderToThrow()
    {
        byte[] png = PngMalformedFixtures.CreateTruncatedChunkData();

        Assert.Throws<EndOfStreamException>(() => Png.ReadChunks(png));
    }

    [Test]
    public void ValidMinimalFixture_IsStrictlyValid()
    {
        PngValidationResult result = PngValidator.ValidateStrict(PngMalformedFixtures.CreateValidMinimalPng());

        Assert.That(result.IsValid, Is.True);
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

    static bool HasWarning(PngValidationResult result, string code, string chunkType)
    {
        int index = 0;
        while (index < result.Warnings.Length)
        {
            if (result.Warnings[index].Code == code && result.Warnings[index].ChunkType == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
