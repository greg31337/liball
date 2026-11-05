using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCustomCodecWorkflowTests
{
    [Test]
    public void ReadAndWriteOptions_UseCustomCodecAcrossNormalRasterWorkflows()
    {
        TestDirectionalCodec codec = new TestDirectionalCodec(true, true);
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.CompressionCodec = codec;
        TiffWriter writer = new TiffFactory().CreateWriter(writeOptions);
        TiffWriterImageConfiguration configuration = CreateConfiguration();
        int imageIndex = writer.AddConfiguredImage(configuration);
        writer.WriteEncodedStrip(imageIndex, 0, new byte[] { 1, 2, 3 });
        writer.WriteEncodedStrip(imageIndex, 1, new byte[] { 4, 5, 6 });

        TiffDiagnostics update = writer.WriteScanline(imageIndex, 0, new byte[] { 10, 11, 12 });
        TiffDiagnostics flush = writer.Flush();

        Assert.That(update.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(0));
        Assert.That(codec.EncodeCallCount, Is.EqualTo(3));
        Assert.That(writer.GetOptions().CompressionCodec, Is.SameAs(codec));

        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.CompressionCodec = codec;
        TiffFile file = new TiffFactory().OpenReadBytes(writer.GetFileBytes(), readOptions);
        TiffImage image = file.GetImage(0);
        byte[] raster = new byte[6];
        byte[] scanline = new byte[3];
        byte[] region = new byte[4];
        TiffRegionReadOptions regionOptions = new TiffRegionReadOptions();
        regionOptions.Region.Column = 1;
        regionOptions.Region.Width = 2;
        regionOptions.Region.Length = 2;

        TiffDiagnostics rasterRead = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics scanlineRead = image.ReadScanlineToBuffer(1, scanline);
        TiffDiagnostics regionRead = image.ReadRegionToBuffer(regionOptions, region);
        TiffRgba8ReadResult rgba = image.ReadRgba8(new TiffRgbaReadOptions());

        Assert.That(file.Success, Is.True);
        Assert.That(rasterRead.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(new byte[] { 10, 11, 12, 4, 5, 6 }));
        Assert.That(scanlineRead.Count(), Is.EqualTo(0));
        Assert.That(scanline, Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(regionRead.Count(), Is.EqualTo(0));
        Assert.That(region, Is.EqualTo(new byte[] { 11, 12, 5, 6 }));
        Assert.That(rgba.Success, Is.True);
        Assert.That(rgba.Bytes, Is.EqualTo(new byte[]
        {
            10, 10, 10, 255,
            11, 11, 11, 255,
            12, 12, 12, 255,
            4, 4, 4, 255,
            5, 5, 5, 255,
            6, 6, 6, 255
        }));
        Assert.That(codec.DecodeCallCount, Is.GreaterThan(1));
    }

    [Test]
    public void CodecService_ReportsDecodeEncodeAndRawPassThroughSeparately()
    {
        ITiffCodecService service = new TiffCodecService();
        TestDirectionalCodec decodeOnly = new TestDirectionalCodec(true, false);
        TestDirectionalCodec encodeOnly = new TestDirectionalCodec(false, true);
        TestDirectionalCodec bidirectional = new TestDirectionalCodec(true, true);

        Assert.That(service.CanDecodeCompressionWithCodec(TiffCompression.Jpeg, decodeOnly), Is.True);
        Assert.That(service.CanEncodeCompressionWithCodec(TiffCompression.Jpeg, decodeOnly), Is.False);
        Assert.That(service.CanPassThroughRawCompressionWithCodec(TiffCompression.Jpeg, decodeOnly), Is.True);
        Assert.That(service.IsCompressionSupportedWithCodec(TiffCompression.Jpeg, decodeOnly), Is.False);
        Assert.That(service.CanDecodeCompressionWithCodec(TiffCompression.Jpeg, encodeOnly), Is.False);
        Assert.That(service.CanEncodeCompressionWithCodec(TiffCompression.Jpeg, encodeOnly), Is.True);
        Assert.That(service.CanPassThroughRawCompressionWithCodec(TiffCompression.Jpeg, encodeOnly), Is.True);
        Assert.That(service.CanDecodeCompressionWithCodec(TiffCompression.Jpeg, bidirectional), Is.True);
        Assert.That(service.CanEncodeCompressionWithCodec(TiffCompression.Jpeg, bidirectional), Is.True);
        Assert.That(service.CanPassThroughRawCompressionWithCodec(TiffCompression.Jpeg, bidirectional), Is.False);
        Assert.That(service.IsCompressionSupportedWithCodec(TiffCompression.Jpeg, bidirectional), Is.True);
    }

    [Test]
    public void DirectionalOptions_AcceptEncodeOnlyWriterAndDecodeOnlyReader()
    {
        TestDirectionalCodec encoder = new TestDirectionalCodec(false, true);
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.CompressionCodec = encoder;
        TiffWriter writer = new TiffFactory().CreateWriter(writeOptions);
        int imageIndex = writer.AddConfiguredImage(CreateConfiguration());
        byte[] expected = new byte[] { 1, 2, 3, 4, 5, 6 };

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, expected);
        TiffDiagnostics flush = writer.Flush();

        TestDirectionalCodec decoder = new TestDirectionalCodec(true, false);
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.CompressionCodec = decoder;
        TiffImage image = new TiffFactory().OpenReadBytes(writer.GetFileBytes(), readOptions).GetImage(0);
        byte[] actual = new byte[expected.Length];
        TiffDiagnostics read = image.ReadSourceRasterToBuffer(actual);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(encoder.EncodeCallCount, Is.EqualTo(2));
        Assert.That(encoder.DecodeCallCount, Is.EqualTo(0));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(decoder.DecodeCallCount, Is.EqualTo(2));
        Assert.That(decoder.EncodeCallCount, Is.EqualTo(0));
    }

    [Test]
    public void TiledWorkflow_UsesCustomCodecForTileEncodingAndRegionDecoding()
    {
        TestDirectionalCodec encoder = new TestDirectionalCodec(false, true);
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.CompressionCodec = encoder;
        TiffWriter writer = new TiffFactory().CreateWriter(writeOptions);
        TiffWriterImageConfiguration configuration = CreateConfiguration();
        configuration.Width = 2;
        configuration.Length = 2;
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.TileWidth = 2;
        configuration.Layout.TileLength = 2;
        configuration.Layout.TileDepth = 1;
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, new byte[] { 1, 2, 3, 4 });
        TiffDiagnostics flush = writer.Flush();

        TestDirectionalCodec decoder = new TestDirectionalCodec(true, false);
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.CompressionCodec = decoder;
        TiffImage image = new TiffFactory().OpenReadBytes(writer.GetFileBytes(), readOptions).GetImage(0);
        TiffRegionReadOptions regionOptions = new TiffRegionReadOptions();
        regionOptions.Region.Column = 1;
        regionOptions.Region.Width = 1;
        regionOptions.Region.Length = 2;
        byte[] region = new byte[2];
        TiffDiagnostics read = image.ReadRegionToBuffer(regionOptions, region);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(encoder.EncodeCallCount, Is.EqualTo(1));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(region, Is.EqualTo(new byte[] { 2, 4 }));
        Assert.That(decoder.DecodeCallCount, Is.EqualTo(2));
    }

    [Test]
    public void CustomCodec_TakesPrecedenceOverUncompressedDirectReadAndBuiltInEncoding()
    {
        TestDirectionalCodec codec = new TestDirectionalCodec(true, true);
        codec.Compression = TiffCompression.None;
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.CompressionCodec = codec;
        TiffWriter writer = new TiffFactory().CreateWriter(writeOptions);
        TiffWriterImageConfiguration configuration = CreateConfiguration();
        configuration.Compression = TiffCompression.None;
        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, new byte[] { 1, 2, 3, 4, 5, 6 });
        TiffDiagnostics flush = writer.Flush();
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.CompressionCodec = codec;
        TiffImage image = new TiffFactory().OpenReadBytes(writer.GetFileBytes(), readOptions).GetImage(0);
        byte[] scanline = new byte[3];

        TiffDiagnostics read = image.ReadScanlineToBuffer(0, scanline);

        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(read.Count(), Is.EqualTo(0));
        Assert.That(scanline, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(codec.EncodeCallCount, Is.EqualTo(2));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(1));
    }

    static TiffWriterImageConfiguration CreateConfiguration()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 3;
        configuration.Length = 2;
        configuration.Compression = TiffCompression.Jpeg;
        configuration.Layout.Kind = TiffWriterLayoutKind.Strips;
        configuration.Layout.RowsPerStrip = 1;
        return configuration;
    }

    class TestDirectionalCodec : ITiffCompressionCodec
    {
        public TiffCompression Compression;
        public bool DecodeSupported;
        public bool EncodeSupported;
        public int DecodeCallCount;
        public int EncodeCallCount;

        public TestDirectionalCodec(bool decodeSupported, bool encodeSupported)
        {
            Compression = TiffCompression.Jpeg;
            DecodeSupported = decodeSupported;
            EncodeSupported = encodeSupported;
            DecodeCallCount = 0;
            EncodeCallCount = 0;
        }

        public bool CanDecode(TiffCompression compression)
        {
            return DecodeSupported && compression == Compression;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return EncodeSupported && compression == Compression;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            DecodeCallCount = DecodeCallCount + 1;
            return TiffCodecResult.Create(Transform(source), new TiffDiagnostics());
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            EncodeCallCount = EncodeCallCount + 1;
            return TiffCodecResult.Create(Transform(source), new TiffDiagnostics());
        }

        public byte[] Transform(ReadOnlySpan<byte> source)
        {
            byte[] destination = new byte[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                destination[index] = (byte)(source[index] ^ 165);
                index = index + 1;
            }

            return destination;
        }
    }
}
