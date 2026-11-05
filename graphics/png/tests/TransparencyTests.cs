using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TransparencyTests
{
    [Test]
    public void PngIndexedTransparencyModel_UsesPublicFieldsOnly()
    {
        Assert.That(typeof(PngIndexedTransparency).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngIndexedTransparency).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngIndexedTransparency).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngIndexedTransparency).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(1));

        PngIndexedTransparency transparency = new PngIndexedTransparency();

        Assert.That(transparency.Alpha, Is.Not.Null);
        Assert.That(transparency.Alpha, Is.Empty);
    }

    [Test]
    public void PngGrayscaleTransparencyModel_UsesPublicFieldsOnly()
    {
        Assert.That(typeof(PngGrayscaleTransparency).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngGrayscaleTransparency).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngGrayscaleTransparency).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngGrayscaleTransparency).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(1));

        PngGrayscaleTransparency transparency = new PngGrayscaleTransparency();

        Assert.That(transparency.Gray, Is.EqualTo(0));
    }

    [Test]
    public void PngTruecolorTransparencyModel_UsesPublicFieldsOnly()
    {
        Assert.That(typeof(PngTruecolorTransparency).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngTruecolorTransparency).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngTruecolorTransparency).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngTruecolorTransparency).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(3));

        PngTruecolorTransparency transparency = new PngTruecolorTransparency();

        Assert.That(transparency.Red, Is.EqualTo(0));
        Assert.That(transparency.Green, Is.EqualTo(0));
        Assert.That(transparency.Blue, Is.EqualTo(0));
    }

    [Test]
    public void ParseIndexed_ReadsAlphaBytes()
    {
        byte[] data = new byte[] { 0, 64, 128, 255 };

        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(data);

        Assert.That(transparency.Alpha, Is.EqualTo(new byte[] { 0, 64, 128, 255 }));
    }

    [Test]
    public void ParseIndexed_ReadsPayloadAtOffsetAndLength()
    {
        byte[] data = new byte[] { 9, 8, 1, 2, 3, 7 };

        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(data, 2, 3);

        Assert.That(transparency.Alpha, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void ParseIndexed_ReadsTransparencyChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTransparency;
        chunk.Data = new byte[] { 10, 20, 30 };

        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(chunk);

        Assert.That(transparency.Alpha, Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void ParseIndexed_ReturnsIndependentAlphaCopy()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(data);
        data[1] = 200;

        Assert.That(transparency.Alpha, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void WriteIndexedData_EmitsAlphaBytes()
    {
        PngIndexedTransparency transparency = CreateTransparency(255, 128, 0);

        byte[] data = PngTransparencyWriter.WriteIndexedData(transparency);

        Assert.That(data, Is.EqualTo(new byte[] { 255, 128, 0 }));
    }

    [Test]
    public void WriteIndexedData_WritesAtOffset()
    {
        PngIndexedTransparency transparency = CreateTransparency(3, 4, 5);
        byte[] data = new byte[] { 1, 1, 1, 1, 1 };

        PngTransparencyWriter.WriteIndexedData(data, 1, transparency);

        Assert.That(data, Is.EqualTo(new byte[] { 1, 3, 4, 5, 1 }));
    }

    [Test]
    public void WriteIndexedData_ReturnsIndependentAlphaCopy()
    {
        PngIndexedTransparency transparency = CreateTransparency(1, 2, 3);

        byte[] data = PngTransparencyWriter.WriteIndexedData(transparency);
        transparency.Alpha[1] = 200;

        Assert.That(data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void WriteIndexedChunk_CreatesTransparencyChunkModel()
    {
        PngIndexedTransparency transparency = CreateTransparency(0, 128, 255);

        PngChunk chunk = PngTransparencyWriter.WriteIndexedChunk(transparency);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkTransparency));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 0, 128, 255 }));
        Assert.That(chunk.Length, Is.EqualTo(3));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteIndexedChunk_RoundTripsThroughRawChunkReaderAndParser()
    {
        PngIndexedTransparency transparency = CreateTransparency(0, 17, 255, 128);

        PngChunk chunk = PngTransparencyWriter.WriteIndexedChunk(transparency);
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngIndexedTransparency readTransparency = PngTransparencyParser.ParseIndexed(readChunk);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkTransparency));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkTransparency, readChunk.Data)));
        Assert.That(readTransparency.Alpha, Is.EqualTo(new byte[] { 0, 17, 255, 128 }));
    }

    [Test]
    public void ValidateIndexed_AcceptsAlphaCountWithinPalette()
    {
        PngIndexedTransparency transparency = CreateTransparency(0, 128, 255);
        PngPalette palette = CreatePalette(3);
        PngIhdr ihdr = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Eight);
        PngChunk chunk = PngTransparencyWriter.WriteIndexedChunk(transparency);

        PngValidationResult modelResult = PngTransparencyValidator.ValidateIndexed(transparency, palette);
        PngValidationResult chunkResult = PngTransparencyValidator.ValidateIndexed(chunk, ihdr, palette);

        Assert.That(modelResult.IsValid, Is.True);
        Assert.That(modelResult.Errors, Is.Empty);
        Assert.That(chunkResult.IsValid, Is.True);
        Assert.That(chunkResult.Errors, Is.Empty);
        Assert.That(chunkResult.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(chunkResult.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(PngTransparencyValidator.IsValidIndexed(transparency), Is.True);
        Assert.That(PngTransparencyValidator.IsValidIndexed(transparency, palette), Is.True);
    }

    [Test]
    public void ValidateIndexed_RejectsAlphaCountOutsidePngRange()
    {
        PngIndexedTransparency empty = CreateTransparency();
        PngIndexedTransparency tooMany = CreateTransparency(257);

        PngValidationResult emptyResult = PngTransparencyValidator.ValidateIndexed(empty);
        PngValidationResult tooManyResult = PngTransparencyValidator.ValidateIndexed(tooMany);

        Assert.That(emptyResult.IsValid, Is.False);
        Assert.That(emptyResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_ALPHA_LENGTH_INVALID"));
        Assert.That(tooManyResult.IsValid, Is.False);
        Assert.That(tooManyResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_ALPHA_LENGTH_INVALID"));
        Assert.That(PngTransparencyValidator.IsValidIndexed(empty), Is.False);
    }

    [Test]
    public void ValidateIndexed_RejectsAlphaCountAbovePaletteEntryCount()
    {
        PngIndexedTransparency transparency = CreateTransparency(1, 2, 3);
        PngPalette palette = CreatePalette(2);

        PngValidationResult result = PngTransparencyValidator.ValidateIndexed(transparency, palette);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_ALPHA_EXCEEDS_PALETTE"));
    }

    [Test]
    public void ValidateIndexed_ChunkRejectsWrongTypeAndPayloadLengthRules()
    {
        PngChunk wrongType = CreateChunk(PngConstants.ChunkIHDR, new byte[] { 1, 2, 3 }, 11);
        PngChunk empty = CreateChunk(PngConstants.ChunkTransparency, new byte[0], 22);
        PngChunk tooLong = CreateChunk(PngConstants.ChunkTransparency, new byte[257], 33);

        PngValidationResult wrongTypeResult = PngTransparencyValidator.ValidateIndexed(wrongType);
        PngValidationResult emptyResult = PngTransparencyValidator.ValidateIndexed(empty);
        PngValidationResult tooLongResult = PngTransparencyValidator.ValidateIndexed(tooLong);

        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(wrongTypeResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_CHUNK_TYPE_INVALID"));
        Assert.That(wrongTypeResult.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(wrongTypeResult.Errors[0].Offset, Is.EqualTo(11));
        Assert.That(emptyResult.IsValid, Is.False);
        Assert.That(emptyResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_ALPHA_LENGTH_INVALID"));
        Assert.That(emptyResult.Errors[0].Offset, Is.EqualTo(22));
        Assert.That(tooLongResult.IsValid, Is.False);
        Assert.That(tooLongResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_ALPHA_LENGTH_INVALID"));
        Assert.That(tooLongResult.Errors[0].Offset, Is.EqualTo(33));
    }

    [Test]
    public void ValidateIndexed_ChunkAppliesIhdrAndPaletteContextRules()
    {
        PngIhdr truecolor = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);
        PngIhdr indexed = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Eight);
        PngPalette palette = CreatePalette(2);
        PngChunk chunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 1, 2, 3 }, 44);

        PngValidationResult truecolorResult = PngTransparencyValidator.ValidateIndexed(chunk, truecolor, palette);
        PngValidationResult indexedResult = PngTransparencyValidator.ValidateIndexed(chunk, indexed, palette);

        Assert.That(truecolorResult.IsValid, Is.False);
        Assert.That(truecolorResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_COLOR_REQUIRED"));
        Assert.That(truecolorResult.Errors[0].Offset, Is.EqualTo(44));
        Assert.That(indexedResult.IsValid, Is.False);
        Assert.That(indexedResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_INDEXED_ALPHA_EXCEEDS_PALETTE"));
        Assert.That(indexedResult.Errors[0].Offset, Is.EqualTo(44));
    }

    [Test]
    public void ParseIndexed_RejectsInvalidInputs()
    {
        PngChunk wrongType = new PngChunk();
        wrongType.Type = PngConstants.ChunkIHDR;
        wrongType.Data = new byte[] { 1, 2, 3 };
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 1, 2, 3 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTransparency;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseIndexed((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseIndexed(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseIndexed((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseIndexed(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseIndexed(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseIndexed(new byte[3], -1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseIndexed(new byte[3], 4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseIndexed(new byte[3], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseIndexed(new byte[3], 1, 3));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseIndexed(wrongType));
    }

    [Test]
    public void WriteIndexedData_RejectsInvalidInputs()
    {
        PngIndexedTransparency valid = CreateTransparency(1, 2, 3);
        PngIndexedTransparency nullAlpha = new PngIndexedTransparency();
        nullAlpha.Alpha = null!;

        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteIndexedData(null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteIndexedData(null!, 0, valid));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteIndexedData(new byte[3], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteIndexedData(nullAlpha));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteIndexedChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyWriter.WriteIndexedData(new byte[3], -1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyWriter.WriteIndexedData(new byte[3], 1, valid));
    }

    [Test]
    public void ValidateIndexed_RejectsNullInputs()
    {
        PngIndexedTransparency transparency = CreateTransparency(1);
        PngIndexedTransparency nullAlpha = new PngIndexedTransparency();
        nullAlpha.Alpha = null!;
        PngPalette palette = CreatePalette(1);
        PngPalette nullEntries = new PngPalette();
        nullEntries.Entries = null!;
        PngIhdr ihdr = CreateIhdr(PngColorType.IndexedColor, PngBitDepth.Eight);
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 1 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTransparency;
        nullData.Data = null!;
        PngChunk chunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 1 }, 0);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed((PngIndexedTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(nullAlpha));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(transparency, (PngPalette)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(transparency, nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(nullData));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(chunk, (PngPalette)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(chunk, nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(chunk, null!, palette));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(chunk, ihdr, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateIndexed(chunk, ihdr, nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.IsValidIndexed((PngIndexedTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.IsValidIndexed(transparency, null!));
    }

    [Test]
    public void ParseGrayscale_ReadsBigEndianGraySample()
    {
        byte[] data = new byte[] { 18, 52 };

        PngGrayscaleTransparency transparency = PngTransparencyParser.ParseGrayscale(data);

        Assert.That(transparency.Gray, Is.EqualTo(0x1234));
    }

    [Test]
    public void ParseGrayscale_ReadsPayloadAtOffsetAndLength()
    {
        byte[] data = new byte[] { 9, 8, 171, 205, 7 };

        PngGrayscaleTransparency transparency = PngTransparencyParser.ParseGrayscale(data, 2, 2);

        Assert.That(transparency.Gray, Is.EqualTo(0xABCD));
    }

    [Test]
    public void ParseGrayscale_ReadsTransparencyChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTransparency;
        chunk.Data = new byte[] { 0, 200 };

        PngGrayscaleTransparency transparency = PngTransparencyParser.ParseGrayscale(chunk);

        Assert.That(transparency.Gray, Is.EqualTo(200));
    }

    [Test]
    public void WriteGrayscaleData_EmitsBigEndianGraySample()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(0x1234);

        byte[] data = PngTransparencyWriter.WriteGrayscaleData(transparency);

        Assert.That(data, Is.EqualTo(new byte[] { 18, 52 }));
    }

    [Test]
    public void WriteGrayscaleData_WritesAtOffset()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(0xABCD);
        byte[] data = new byte[] { 1, 1, 1, 1 };

        PngTransparencyWriter.WriteGrayscaleData(data, 1, transparency);

        Assert.That(data, Is.EqualTo(new byte[] { 1, 171, 205, 1 }));
    }

    [Test]
    public void WriteGrayscaleChunk_CreatesTransparencyChunkModel()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(255);

        PngChunk chunk = PngTransparencyWriter.WriteGrayscaleChunk(transparency);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkTransparency));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 0, 255 }));
        Assert.That(chunk.Length, Is.EqualTo(2));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteGrayscaleChunk_RoundTripsThroughRawChunkReaderAndParser()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(0x8001);

        PngChunk chunk = PngTransparencyWriter.WriteGrayscaleChunk(transparency);
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngGrayscaleTransparency readTransparency = PngTransparencyParser.ParseGrayscale(readChunk);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkTransparency));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkTransparency, readChunk.Data)));
        Assert.That(readTransparency.Gray, Is.EqualTo(0x8001));
    }

    [Test]
    public void ValidateGrayscale_AcceptsSampleWithinBitDepth()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(15);
        PngIhdr ihdr = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Four);
        PngChunk chunk = PngTransparencyWriter.WriteGrayscaleChunk(transparency);

        PngValidationResult modelResult = PngTransparencyValidator.ValidateGrayscale(transparency, ihdr);
        PngValidationResult chunkResult = PngTransparencyValidator.ValidateGrayscale(chunk, ihdr);

        Assert.That(modelResult.IsValid, Is.True);
        Assert.That(modelResult.Errors, Is.Empty);
        Assert.That(chunkResult.IsValid, Is.True);
        Assert.That(chunkResult.Errors, Is.Empty);
        Assert.That(chunkResult.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(chunkResult.Info.BitDepth, Is.EqualTo(PngBitDepth.Four));
        Assert.That(PngTransparencyValidator.IsValidGrayscale(transparency), Is.True);
        Assert.That(PngTransparencyValidator.IsValidGrayscale(transparency, ihdr), Is.True);
    }

    [Test]
    public void ValidateGrayscale_RejectsSampleAboveBitDepth()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(16);
        PngIhdr ihdr = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Four);

        PngValidationResult result = PngTransparencyValidator.ValidateGrayscale(transparency, ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_TRNS_GRAYSCALE_SAMPLE_OUT_OF_RANGE"));
    }

    [Test]
    public void ValidateGrayscale_ChunkRejectsWrongTypeAndPayloadLengthRules()
    {
        PngChunk wrongType = CreateChunk(PngConstants.ChunkIHDR, new byte[] { 0, 1 }, 11);
        PngChunk tooShort = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 1 }, 22);
        PngChunk tooLong = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 1, 2 }, 33);

        PngValidationResult wrongTypeResult = PngTransparencyValidator.ValidateGrayscale(wrongType);
        PngValidationResult tooShortResult = PngTransparencyValidator.ValidateGrayscale(tooShort);
        PngValidationResult tooLongResult = PngTransparencyValidator.ValidateGrayscale(tooLong);

        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(wrongTypeResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_CHUNK_TYPE_INVALID"));
        Assert.That(wrongTypeResult.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(wrongTypeResult.Errors[0].Offset, Is.EqualTo(11));
        Assert.That(tooShortResult.IsValid, Is.False);
        Assert.That(tooShortResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_GRAYSCALE_LENGTH_INVALID"));
        Assert.That(tooShortResult.Errors[0].Offset, Is.EqualTo(22));
        Assert.That(tooLongResult.IsValid, Is.False);
        Assert.That(tooLongResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_GRAYSCALE_LENGTH_INVALID"));
        Assert.That(tooLongResult.Errors[0].Offset, Is.EqualTo(33));
    }

    [Test]
    public void ValidateGrayscale_ChunkAppliesIhdrContextRules()
    {
        PngIhdr truecolor = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);
        PngIhdr grayscale = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Two);
        PngChunk chunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 4 }, 44);

        PngValidationResult truecolorResult = PngTransparencyValidator.ValidateGrayscale(chunk, truecolor);
        PngValidationResult grayscaleResult = PngTransparencyValidator.ValidateGrayscale(chunk, grayscale);

        Assert.That(truecolorResult.IsValid, Is.False);
        Assert.That(truecolorResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_GRAYSCALE_COLOR_REQUIRED"));
        Assert.That(truecolorResult.Errors[0].Offset, Is.EqualTo(44));
        Assert.That(grayscaleResult.IsValid, Is.False);
        Assert.That(grayscaleResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_GRAYSCALE_SAMPLE_OUT_OF_RANGE"));
        Assert.That(grayscaleResult.Errors[0].Offset, Is.EqualTo(44));
    }

    [Test]
    public void ParseGrayscale_RejectsInvalidInputs()
    {
        PngChunk wrongType = new PngChunk();
        wrongType.Type = PngConstants.ChunkIHDR;
        wrongType.Data = new byte[] { 0, 1 };
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 0, 1 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTransparency;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseGrayscale((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseGrayscale(null!, 0, 2));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseGrayscale((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseGrayscale(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseGrayscale(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseGrayscale(new byte[2], -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseGrayscale(new byte[2], 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseGrayscale(new byte[2], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseGrayscale(new byte[2], 1, 2));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseGrayscale(new byte[] { 1 }));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseGrayscale(new byte[] { 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseGrayscale(wrongType));
    }

    [Test]
    public void WriteGrayscaleData_RejectsInvalidInputs()
    {
        PngGrayscaleTransparency valid = CreateGrayscaleTransparency(1);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteGrayscaleData(null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteGrayscaleData(null!, 0, valid));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteGrayscaleData(new byte[2], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteGrayscaleChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyWriter.WriteGrayscaleData(new byte[2], -1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyWriter.WriteGrayscaleData(new byte[2], 1, valid));
    }

    [Test]
    public void ValidateGrayscale_RejectsNullInputs()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(1);
        PngIhdr ihdr = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Eight);
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 0, 1 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTransparency;
        nullData.Data = null!;
        PngChunk chunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 1 }, 0);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateGrayscale((PngGrayscaleTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateGrayscale(transparency, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateGrayscale((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateGrayscale(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateGrayscale(nullData));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateGrayscale(chunk, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.IsValidGrayscale((PngGrayscaleTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.IsValidGrayscale(transparency, null!));
    }

    [Test]
    public void ParseTruecolor_ReadsBigEndianRgbSamples()
    {
        byte[] data = new byte[] { 18, 52, 171, 205, 128, 1 };

        PngTruecolorTransparency transparency = PngTransparencyParser.ParseTruecolor(data);

        Assert.That(transparency.Red, Is.EqualTo(0x1234));
        Assert.That(transparency.Green, Is.EqualTo(0xABCD));
        Assert.That(transparency.Blue, Is.EqualTo(0x8001));
    }

    [Test]
    public void ParseTruecolor_ReadsPayloadAtOffsetAndLength()
    {
        byte[] data = new byte[] { 9, 8, 0, 1, 0, 2, 0, 3, 7 };

        PngTruecolorTransparency transparency = PngTransparencyParser.ParseTruecolor(data, 2, 6);

        Assert.That(transparency.Red, Is.EqualTo(1));
        Assert.That(transparency.Green, Is.EqualTo(2));
        Assert.That(transparency.Blue, Is.EqualTo(3));
    }

    [Test]
    public void ParseTruecolor_ReadsTransparencyChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTransparency;
        chunk.Data = new byte[] { 0, 10, 0, 20, 0, 30 };

        PngTruecolorTransparency transparency = PngTransparencyParser.ParseTruecolor(chunk);

        Assert.That(transparency.Red, Is.EqualTo(10));
        Assert.That(transparency.Green, Is.EqualTo(20));
        Assert.That(transparency.Blue, Is.EqualTo(30));
    }

    [Test]
    public void WriteTruecolorData_EmitsBigEndianRgbSamples()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(0x1234, 0xABCD, 0x8001);

        byte[] data = PngTransparencyWriter.WriteTruecolorData(transparency);

        Assert.That(data, Is.EqualTo(new byte[] { 18, 52, 171, 205, 128, 1 }));
    }

    [Test]
    public void WriteTruecolorData_WritesAtOffset()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(1, 2, 3);
        byte[] data = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };

        PngTransparencyWriter.WriteTruecolorData(data, 1, transparency);

        Assert.That(data, Is.EqualTo(new byte[] { 9, 0, 1, 0, 2, 0, 3, 9 }));
    }

    [Test]
    public void WriteTruecolorChunk_CreatesTransparencyChunkModel()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(10, 20, 30);

        PngChunk chunk = PngTransparencyWriter.WriteTruecolorChunk(transparency);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkTransparency));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 0, 10, 0, 20, 0, 30 }));
        Assert.That(chunk.Length, Is.EqualTo(6));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteTruecolorChunk_RoundTripsThroughRawChunkReaderAndParser()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(0x1234, 0x4567, 0x89AB);

        PngChunk chunk = PngTransparencyWriter.WriteTruecolorChunk(transparency);
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngTruecolorTransparency readTransparency = PngTransparencyParser.ParseTruecolor(readChunk);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkTransparency));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkTransparency, readChunk.Data)));
        Assert.That(readTransparency.Red, Is.EqualTo(0x1234));
        Assert.That(readTransparency.Green, Is.EqualTo(0x4567));
        Assert.That(readTransparency.Blue, Is.EqualTo(0x89AB));
    }

    [Test]
    public void ValidateTruecolor_AcceptsSamplesWithinBitDepth()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(255, 128, 0);
        PngIhdr ihdr = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);
        PngChunk chunk = PngTransparencyWriter.WriteTruecolorChunk(transparency);

        PngValidationResult modelResult = PngTransparencyValidator.ValidateTruecolor(transparency, ihdr);
        PngValidationResult chunkResult = PngTransparencyValidator.ValidateTruecolor(chunk, ihdr);

        Assert.That(modelResult.IsValid, Is.True);
        Assert.That(modelResult.Errors, Is.Empty);
        Assert.That(chunkResult.IsValid, Is.True);
        Assert.That(chunkResult.Errors, Is.Empty);
        Assert.That(chunkResult.Info.ColorType, Is.EqualTo(PngColorType.Truecolor));
        Assert.That(chunkResult.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(PngTransparencyValidator.IsValidTruecolor(transparency), Is.True);
        Assert.That(PngTransparencyValidator.IsValidTruecolor(transparency, ihdr), Is.True);
    }

    [Test]
    public void ValidateTruecolor_RejectsSampleAboveBitDepth()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(255, 256, 0);
        PngIhdr ihdr = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);

        PngValidationResult result = PngTransparencyValidator.ValidateTruecolor(transparency, ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_TRNS_TRUECOLOR_SAMPLE_OUT_OF_RANGE"));
    }

    [Test]
    public void ValidateTruecolor_ChunkRejectsWrongTypeAndPayloadLengthRules()
    {
        PngChunk wrongType = CreateChunk(PngConstants.ChunkIHDR, new byte[] { 0, 1, 0, 2, 0, 3 }, 11);
        PngChunk tooShort = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 1, 0, 2, 0 }, 22);
        PngChunk tooLong = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 1, 0, 2, 0, 3, 0 }, 33);

        PngValidationResult wrongTypeResult = PngTransparencyValidator.ValidateTruecolor(wrongType);
        PngValidationResult tooShortResult = PngTransparencyValidator.ValidateTruecolor(tooShort);
        PngValidationResult tooLongResult = PngTransparencyValidator.ValidateTruecolor(tooLong);

        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(wrongTypeResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_CHUNK_TYPE_INVALID"));
        Assert.That(wrongTypeResult.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(wrongTypeResult.Errors[0].Offset, Is.EqualTo(11));
        Assert.That(tooShortResult.IsValid, Is.False);
        Assert.That(tooShortResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_TRUECOLOR_LENGTH_INVALID"));
        Assert.That(tooShortResult.Errors[0].Offset, Is.EqualTo(22));
        Assert.That(tooLongResult.IsValid, Is.False);
        Assert.That(tooLongResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_TRUECOLOR_LENGTH_INVALID"));
        Assert.That(tooLongResult.Errors[0].Offset, Is.EqualTo(33));
    }

    [Test]
    public void ValidateTruecolor_ChunkAppliesIhdrContextRules()
    {
        PngIhdr grayscale = CreateIhdr(PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr truecolor = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);
        PngChunk chunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 1, 0, 2, 1, 0 }, 44);

        PngValidationResult grayscaleResult = PngTransparencyValidator.ValidateTruecolor(chunk, grayscale);
        PngValidationResult truecolorResult = PngTransparencyValidator.ValidateTruecolor(chunk, truecolor);

        Assert.That(grayscaleResult.IsValid, Is.False);
        Assert.That(grayscaleResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_TRUECOLOR_COLOR_REQUIRED"));
        Assert.That(grayscaleResult.Errors[0].Offset, Is.EqualTo(44));
        Assert.That(truecolorResult.IsValid, Is.False);
        Assert.That(truecolorResult.Errors[0].Code, Is.EqualTo("PNG_TRNS_TRUECOLOR_SAMPLE_OUT_OF_RANGE"));
        Assert.That(truecolorResult.Errors[0].Offset, Is.EqualTo(44));
    }

    [Test]
    public void ParseTruecolor_RejectsInvalidInputs()
    {
        PngChunk wrongType = new PngChunk();
        wrongType.Type = PngConstants.ChunkIHDR;
        wrongType.Data = new byte[] { 0, 1, 0, 2, 0, 3 };
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 0, 1, 0, 2, 0, 3 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTransparency;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseTruecolor((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseTruecolor(null!, 0, 6));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseTruecolor((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseTruecolor(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyParser.ParseTruecolor(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseTruecolor(new byte[6], -1, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseTruecolor(new byte[6], 7, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseTruecolor(new byte[6], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyParser.ParseTruecolor(new byte[6], 1, 6));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseTruecolor(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseTruecolor(new byte[] { 1, 2, 3, 4, 5, 6, 7 }));
        Assert.Throws<InvalidDataException>(() => PngTransparencyParser.ParseTruecolor(wrongType));
    }

    [Test]
    public void WriteTruecolorData_RejectsInvalidInputs()
    {
        PngTruecolorTransparency valid = CreateTruecolorTransparency(1, 2, 3);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteTruecolorData(null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteTruecolorData(null!, 0, valid));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteTruecolorData(new byte[6], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyWriter.WriteTruecolorChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyWriter.WriteTruecolorData(new byte[6], -1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyWriter.WriteTruecolorData(new byte[6], 1, valid));
    }

    [Test]
    public void ValidateTruecolor_RejectsNullInputs()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(1, 2, 3);
        PngIhdr ihdr = CreateIhdr(PngColorType.Truecolor, PngBitDepth.Eight);
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 0, 1, 0, 2, 0, 3 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkTransparency;
        nullData.Data = null!;
        PngChunk chunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 1, 0, 2, 0, 3 }, 0);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateTruecolor((PngTruecolorTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateTruecolor(transparency, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateTruecolor((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateTruecolor(nullType));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateTruecolor(nullData));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.ValidateTruecolor(chunk, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.IsValidTruecolor((PngTruecolorTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyValidator.IsValidTruecolor(transparency, null!));
    }

    static PngIndexedTransparency CreateTransparency(params int[] values)
    {
        PngIndexedTransparency transparency = new PngIndexedTransparency();
        transparency.Alpha = CreateAlpha(values);
        return transparency;
    }

    static PngIndexedTransparency CreateTransparency(int length)
    {
        PngIndexedTransparency transparency = new PngIndexedTransparency();
        transparency.Alpha = new byte[length];
        int index = 0;
        while (index < length)
        {
            transparency.Alpha[index] = (byte)index;
            index += 1;
        }

        return transparency;
    }

    static byte[] CreateAlpha(params int[] values)
    {
        byte[] alpha = new byte[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            alpha[index] = (byte)values[index];
            index += 1;
        }

        return alpha;
    }

    static PngGrayscaleTransparency CreateGrayscaleTransparency(int gray)
    {
        PngGrayscaleTransparency transparency = new PngGrayscaleTransparency();
        transparency.Gray = (ushort)gray;
        return transparency;
    }

    static PngTruecolorTransparency CreateTruecolorTransparency(int red, int green, int blue)
    {
        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        transparency.Red = (ushort)red;
        transparency.Green = (ushort)green;
        transparency.Blue = (ushort)blue;
        return transparency;
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
}
