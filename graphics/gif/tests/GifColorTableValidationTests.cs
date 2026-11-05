using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifColorTableValidationTests
{
    [Test]
    public void Validator_AcceptsPowerOfTwoColorCountsFromTwoThroughTwoHundredFiftySix()
    {
        GifColorTableValidator validator = new GifColorTableValidator();

        Assert.That(validator.IsValidColorCount(2), Is.True);
        Assert.That(validator.IsValidColorCount(4), Is.True);
        Assert.That(validator.IsValidColorCount(8), Is.True);
        Assert.That(validator.IsValidColorCount(16), Is.True);
        Assert.That(validator.IsValidColorCount(32), Is.True);
        Assert.That(validator.IsValidColorCount(64), Is.True);
        Assert.That(validator.IsValidColorCount(128), Is.True);
        Assert.That(validator.IsValidColorCount(256), Is.True);
    }

    [Test]
    public void Validator_RejectsNonPowerOfTwoOrOutOfRangeColorCounts()
    {
        GifColorTableValidator validator = new GifColorTableValidator();

        Assert.That(validator.IsValidColorCount(0), Is.False);
        Assert.That(validator.IsValidColorCount(1), Is.False);
        Assert.That(validator.IsValidColorCount(3), Is.False);
        Assert.That(validator.IsValidColorCount(255), Is.False);
        Assert.That(validator.IsValidColorCount(257), Is.False);
    }

    [Test]
    public void Validator_RejectsPaletteWithNonPowerOfTwoCount()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(0, 0, 0), new GifColor(0, 0, 0), new GifColor(0, 0, 0) });
        GifColorTableValidator validator = new GifColorTableValidator();

        Assert.That(validator.IsValidPalette(palette), Is.False);
    }

    [Test]
    public void Reader_RejectsNonPowerOfTwoColorCountBeforeReading()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        GifColorTableReader reader = new GifColorTableReader();

        GifColorTableReadResult result = reader.Read(byteReader, 3);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
    }

    [Test]
    public void Writer_RejectsNonPowerOfTwoPaletteCount()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), new GifColor(4, 5, 6), new GifColor(7, 8, 9) });
        GifColorTableWriter writer = new GifColorTableWriter();

        GifColorTableWriteResult result = writer.Create(palette);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(result.EncodedData, Is.Empty);
    }

    [Test]
    public void Validator_CreatesConsistentInvalidSizeDiagnostic()
    {
        GifColorTableValidator validator = new GifColorTableValidator();

        GifErrorDiagnostic diagnostic = validator.CreateInvalidColorTableSizeError(42);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(diagnostic.Position.ByteOffset, Is.EqualTo(42));
        Assert.That(diagnostic.Position.BlockName, Is.EqualTo("ColorTable"));
    }
}
