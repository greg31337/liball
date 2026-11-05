using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PlteValidationTests
{
    [Test]
    public void ValidatePresence_RequiresPlteForIndexedColor()
    {
        PngIhdr indexed = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Eight);

        PngValidationResult missingResult = PngPlteValidator.ValidatePresence(false, indexed);
        PngValidationResult presentResult = PngPlteValidator.ValidatePresence(true, indexed);

        Assert.That(missingResult.IsValid, Is.False);
        Assert.That(missingResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_REQUIRED"));
        Assert.That(presentResult.IsValid, Is.True);
    }

    [Test]
    public void ValidatePresence_DisallowsPlteForGrayscaleColorTypes()
    {
        PngIhdr grayscale = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr grayscaleAlpha = CreateIhdr(PngColorType.GrayscaleAlpha, PngBitDepth.Eight);
        PngIhdr truecolor = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);
        PngIhdr truecolorAlpha = CreateIhdr(PngColorType.TruecolorAlpha, PngBitDepth.Eight);

        PngValidationResult grayscaleResult = PngPlteValidator.ValidatePresence(true, grayscale);
        PngValidationResult grayscaleAlphaResult = PngPlteValidator.ValidatePresence(true, grayscaleAlpha);
        PngValidationResult truecolorResult = PngPlteValidator.ValidatePresence(true, truecolor);
        PngValidationResult truecolorAlphaResult = PngPlteValidator.ValidatePresence(true, truecolorAlpha);

        Assert.That(grayscaleResult.IsValid, Is.False);
        Assert.That(grayscaleResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_DISALLOWED"));
        Assert.That(grayscaleAlphaResult.IsValid, Is.False);
        Assert.That(grayscaleAlphaResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_DISALLOWED"));
        Assert.That(truecolorResult.IsValid, Is.True);
        Assert.That(truecolorAlphaResult.IsValid, Is.True);
    }

    [Test]
    public void Validate_AcceptsPaletteWithinIndexedBitDepthLimit()
    {
        PngIhdr ihdr = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Two);
        PngPalette palette = CreatePalette(4);

        PngValidationResult result = PngPlteValidator.Validate(palette, ihdr);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(result.Info.BitDepth, Is.EqualTo(PngBitDepth.Two));
        Assert.That(PngPlteValidator.IsValid(palette, ihdr), Is.True);
    }

    [Test]
    public void Validate_RejectsIndexedPaletteAboveBitDepthLimit()
    {
        PngIhdr ihdr = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Two);
        PngPalette palette = CreatePalette(5);

        PngValidationResult result = PngPlteValidator.Validate(palette, ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_PLTE_ENTRY_COUNT_EXCEEDS_BIT_DEPTH"));
    }

    [Test]
    public void Validate_RejectsEntryCountOutsidePngRange()
    {
        PngPalette empty = CreatePalette(0);
        PngPalette tooMany = CreatePalette(257);

        PngValidationResult emptyResult = PngPlteValidator.Validate(empty);
        PngValidationResult tooManyResult = PngPlteValidator.Validate(tooMany);

        Assert.That(emptyResult.IsValid, Is.False);
        Assert.That(emptyResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_ENTRY_COUNT_INVALID"));
        Assert.That(tooManyResult.IsValid, Is.False);
        Assert.That(tooManyResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_ENTRY_COUNT_INVALID"));
        Assert.That(PngPlteValidator.IsValid(empty), Is.False);
    }

    [Test]
    public void Validate_RejectsNullPaletteEntries()
    {
        PngPalette palette = CreatePalette(1);
        palette.Entries[0] = null!;

        PngValidationResult result = PngPlteValidator.Validate(palette);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_PLTE_ENTRY_NULL"));
    }

    [Test]
    public void Validate_ChunkRejectsWrongTypeAndPayloadLengthRules()
    {
        PngChunk wrongType = CreateChunk(PngConstants.ChunkIHDR, new byte[] { 1, 2, 3 }, 11);
        PngChunk empty = CreateChunk(PngConstants.ChunkPLTE, new byte[0], 22);
        PngChunk notMultiple = CreateChunk(PngConstants.ChunkPLTE, new byte[] { 1, 2 }, 33);
        PngChunk tooLong = CreateChunk(PngConstants.ChunkPLTE, new byte[771], 44);

        PngValidationResult wrongTypeResult = PngPlteValidator.Validate(wrongType);
        PngValidationResult emptyResult = PngPlteValidator.Validate(empty);
        PngValidationResult notMultipleResult = PngPlteValidator.Validate(notMultiple);
        PngValidationResult tooLongResult = PngPlteValidator.Validate(tooLong);

        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(wrongTypeResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_CHUNK_TYPE_INVALID"));
        Assert.That(wrongTypeResult.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(wrongTypeResult.Errors[0].Offset, Is.EqualTo(11));
        Assert.That(emptyResult.IsValid, Is.False);
        Assert.That(HasError(emptyResult, "PNG_PLTE_PAYLOAD_LENGTH_INVALID"), Is.True);
        Assert.That(HasError(emptyResult, "PNG_PLTE_ENTRY_COUNT_INVALID"), Is.True);
        Assert.That(notMultipleResult.IsValid, Is.False);
        Assert.That(notMultipleResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_PAYLOAD_LENGTH_INVALID"));
        Assert.That(notMultipleResult.Errors[0].Offset, Is.EqualTo(33));
        Assert.That(tooLongResult.IsValid, Is.False);
        Assert.That(HasError(tooLongResult, "PNG_PLTE_PAYLOAD_LENGTH_INVALID"), Is.True);
        Assert.That(HasError(tooLongResult, "PNG_PLTE_ENTRY_COUNT_INVALID"), Is.True);
    }

    [Test]
    public void Validate_ChunkAppliesIhdrContextRules()
    {
        PngIhdr grayscale = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr indexed = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.One);
        PngChunk chunk = CreateChunk(PngConstants.ChunkPLTE, CreatePayload(3), 55);

        PngValidationResult grayscaleResult = PngPlteValidator.Validate(chunk, grayscale);
        PngValidationResult indexedResult = PngPlteValidator.Validate(chunk, indexed);

        Assert.That(grayscaleResult.IsValid, Is.False);
        Assert.That(grayscaleResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_DISALLOWED"));
        Assert.That(grayscaleResult.Errors[0].Offset, Is.EqualTo(55));
        Assert.That(indexedResult.IsValid, Is.False);
        Assert.That(indexedResult.Errors[0].Code, Is.EqualTo("PNG_PLTE_ENTRY_COUNT_EXCEEDS_BIT_DEPTH"));
        Assert.That(indexedResult.Errors[0].Offset, Is.EqualTo(55));
    }

    [Test]
    public void Validate_RejectsNullInputs()
    {
        PngPalette palette = CreatePalette(1);
        PngPalette nullEntries = new PngPalette();
        nullEntries.Entries = null!;
        PngIhdr ihdr = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Eight);
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[3];
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkPLTE;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate((PngPalette)null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate(nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate(palette, null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate(nullType));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate(nullData));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.Validate(CreateChunk(PngConstants.ChunkPLTE, new byte[3], 0), null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.ValidatePresence(false, null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.IsValid((PngPalette)null!));
        Assert.Throws<ArgumentNullException>(() => PngPlteValidator.IsValid(palette, null!));
    }

    static PngIhdr CreateIhdr(PngColorType colorType, PngBitDepth bitDepth)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = 1;
        ihdr.Height = 1;
        ihdr.ColorType = colorType;
        ihdr.BitDepth = bitDepth;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static PngPalette CreatePalette(int entryCount)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[entryCount];
        int index = 0;
        while (index < entryCount)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)index;
            entry.Green = (byte)(255 - index);
            entry.Blue = 128;
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static PngChunk CreateChunk(string chunkType, byte[] data, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        chunk.Offset = offset;
        chunk.Length = (uint)data.Length;
        return chunk;
    }

    static byte[] CreatePayload(int entryCount)
    {
        return PngPlteWriter.WriteData(CreatePalette(entryCount));
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
}
