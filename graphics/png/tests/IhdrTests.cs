using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class IhdrTests
{
    [Test]
    public void PngIhdr_UsesInternalFieldsOnly()
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.That(typeof(PngIhdr).IsNotPublic, Is.True);
        Assert.That(typeof(PngIhdr).GetProperties(flags), Is.Empty);
        Assert.That(typeof(PngIhdr).GetEvents(flags), Is.Empty);
        Assert.That(typeof(PngIhdr).GetMethods(flags), Is.Empty);
        Assert.That(typeof(PngIhdr).GetFields(flags), Has.Length.EqualTo(7));
    }

    [Test]
    public void Parse_ReadsIhdrPayloadFields()
    {
        byte[] data = CreateIhdrData(800, 600, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolorAlpha);

        PngIhdr ihdr = PngIhdrParser.Parse(data);

        Assert.That(ihdr.Width, Is.EqualTo(800));
        Assert.That(ihdr.Height, Is.EqualTo(600));
        Assert.That(ihdr.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(ihdr.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(ihdr.CompressionMethod, Is.EqualTo(PngCompressionMethod.Deflate));
        Assert.That(ihdr.FilterMethod, Is.EqualTo(PngFilterMethod.Adaptive));
        Assert.That(ihdr.InterlaceMethod, Is.EqualTo(PngInterlaceMethod.None));
    }

    [Test]
    public void Parse_ReadsPayloadAtOffset()
    {
        byte[] ihdrData = CreateIhdrData(3, 4, PngConstants.BitDepth16, PngConstants.ColorTypeGrayscaleAlpha);
        byte[] data = new byte[ihdrData.Length + 3];
        data[0] = 0xAA;
        data[1] = 0xBB;
        Array.Copy(ihdrData, 0, data, 2, ihdrData.Length);

        PngIhdr ihdr = PngIhdrParser.Parse(data, 2);

        Assert.That(ihdr.Width, Is.EqualTo(3));
        Assert.That(ihdr.Height, Is.EqualTo(4));
        Assert.That(ihdr.BitDepth, Is.EqualTo(PngBitDepth.Sixteen));
        Assert.That(ihdr.ColorType, Is.EqualTo(PngColorType.GrayscaleAlpha));
    }

    [Test]
    public void Parse_ReadsIhdrChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIHDR;
        chunk.Data = CreateIhdrData(9, 10, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor);

        PngIhdr ihdr = PngIhdrParser.Parse(chunk);

        Assert.That(ihdr.Width, Is.EqualTo(9));
        Assert.That(ihdr.Height, Is.EqualTo(10));
        Assert.That(ihdr.ColorType, Is.EqualTo(PngColorType.Truecolor));
    }

    [Test]
    public void WriteData_EmitsIhdrPayloadBytes()
    {
        PngIhdr ihdr = CreateIhdr(1024, 768, PngBitDepth.Sixteen, PngColorType.Truecolor);

        byte[] data = PngIhdrWriter.WriteData(ihdr);

        Assert.That(data, Has.Length.EqualTo(13));
        Assert.That(PngBinaryReader.ReadUInt32(data, 0), Is.EqualTo(1024));
        Assert.That(PngBinaryReader.ReadUInt32(data, 4), Is.EqualTo(768));
        Assert.That(data[8], Is.EqualTo(PngConstants.BitDepth16));
        Assert.That(data[9], Is.EqualTo(PngConstants.ColorTypeTruecolor));
        Assert.That(data[10], Is.EqualTo(PngConstants.CompressionMethodDeflate));
        Assert.That(data[11], Is.EqualTo(PngConstants.FilterMethodAdaptive));
        Assert.That(data[12], Is.EqualTo(PngConstants.InterlaceMethodNone));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngIhdr ihdr = CreateIhdr(5, 6, PngBitDepth.Eight, PngColorType.IndexedColor);
        byte[] data = new byte[16];
        data[0] = 0xAA;

        PngIhdrWriter.WriteData(data, 2, ihdr);

        Assert.That(data[0], Is.EqualTo(0xAA));
        Assert.That(PngBinaryReader.ReadUInt32(data, 2), Is.EqualTo(5));
        Assert.That(PngBinaryReader.ReadUInt32(data, 6), Is.EqualTo(6));
        Assert.That(data[10], Is.EqualTo(PngConstants.BitDepth8));
        Assert.That(data[11], Is.EqualTo(PngConstants.ColorTypeIndexedColor));
    }

    [Test]
    public void WriteChunk_CreatesIhdrChunkModel()
    {
        PngIhdr ihdr = CreateIhdr(11, 12, PngBitDepth.Eight, PngColorType.TruecolorAlpha);

        PngChunk chunk = PngIhdrWriter.WriteChunk(ihdr);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(chunk.Length, Is.EqualTo(13));
        Assert.That(chunk.Data, Has.Length.EqualTo(13));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.True);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkReaderAndParser()
    {
        PngIhdr ihdr = CreateIhdr(31, 17, PngBitDepth.Four, PngColorType.Grayscale);
        PngChunk chunk = PngIhdrWriter.WriteChunk(ihdr);
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);

        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngIhdr readIhdr = PngIhdrParser.Parse(readChunk);

        Assert.That(readChunk.Type, Is.EqualTo(PngConstants.ChunkIHDR));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkIHDR, readChunk.Data)));
        Assert.That(readIhdr.Width, Is.EqualTo(31));
        Assert.That(readIhdr.Height, Is.EqualTo(17));
        Assert.That(readIhdr.BitDepth, Is.EqualTo(PngBitDepth.Four));
        Assert.That(readIhdr.ColorType, Is.EqualTo(PngColorType.Grayscale));
    }

    [Test]
    public void Parse_RejectsInvalidInputs()
    {
        PngChunk wrongType = new PngChunk();
        wrongType.Type = PngConstants.ChunkIEND;
        wrongType.Data = new byte[13];
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[13];
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkIHDR;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngIhdrParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrParser.Parse(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngIhdrParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngIhdrParser.Parse(nullData));
        Assert.Throws<InvalidDataException>(() => PngIhdrParser.Parse(new byte[12]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrParser.Parse(new byte[13], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrParser.Parse(new byte[13], 1));
        Assert.Throws<InvalidDataException>(() => PngIhdrParser.Parse(wrongType));
    }

    [Test]
    public void Parse_RejectsDimensionsBeyondModelRange()
    {
        byte[] data = CreateIhdrData(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor);
        PngBinaryWriter.WriteUInt32(data, 0, (uint)int.MaxValue + 1u);

        Assert.Throws<OverflowException>(() => PngIhdrParser.Parse(data));
    }

    [Test]
    public void WriteData_RejectsInvalidInputs()
    {
        PngIhdr valid = CreateIhdr(1, 1, PngBitDepth.Eight, PngColorType.Truecolor);
        PngIhdr negativeWidth = CreateIhdr(-1, 1, PngBitDepth.Eight, PngColorType.Truecolor);
        PngIhdr negativeHeight = CreateIhdr(1, -1, PngBitDepth.Eight, PngColorType.Truecolor);
        PngIhdr invalidBitDepth = CreateIhdr(1, 1, (PngBitDepth)300, PngColorType.Truecolor);
        PngIhdr invalidColorType = CreateIhdr(1, 1, PngBitDepth.Eight, (PngColorType)300);
        PngIhdr invalidCompression = CreateIhdr(1, 1, PngBitDepth.Eight, PngColorType.Truecolor);
        PngIhdr invalidFilter = CreateIhdr(1, 1, PngBitDepth.Eight, PngColorType.Truecolor);
        PngIhdr invalidInterlace = CreateIhdr(1, 1, PngBitDepth.Eight, PngColorType.Truecolor);
        invalidCompression.CompressionMethod = (PngCompressionMethod)300;
        invalidFilter.FilterMethod = (PngFilterMethod)300;
        invalidInterlace.InterlaceMethod = (PngInterlaceMethod)300;

        Assert.Throws<ArgumentNullException>(() => PngIhdrWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrWriter.WriteData(null!, 0, valid));
        Assert.Throws<ArgumentNullException>(() => PngIhdrWriter.WriteData(new byte[13], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(new byte[13], -1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(new byte[13], 1, valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(negativeWidth));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(negativeHeight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(invalidBitDepth));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(invalidColorType));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(invalidCompression));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(invalidFilter));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngIhdrWriter.WriteData(invalidInterlace));
    }

    static PngIhdr CreateIhdr(int width, int height, PngBitDepth bitDepth, PngColorType colorType)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = bitDepth;
        ihdr.ColorType = colorType;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
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
}
