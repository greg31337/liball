using System;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicCopyRewriteWorkflowTests
{
    [Test]
    public void RewriteImageMetadata_PathWorkflowPreservesCompressedStripsWithoutDecoding()
    {
        string sourcePath = CreateTempPath();
        string destinationPath = CreateTempPath();
        try
        {
            File.WriteAllBytes(sourcePath, CreateCompressedSource(false, TiffCompression.PackBits));
            CountingDecoder decoder = new CountingDecoder(TiffCompression.PackBits);
            TiffReadOptions readOptions = new TiffReadOptions();
            readOptions.CompressionCodec = decoder;
            TiffFile sourceFile = TiffApi.Factory.OpenReadPath(sourcePath, readOptions);
            TiffImage sourceImage = sourceFile.GetImage(0);
            byte[][] expectedStrips = ReadRawStrips(sourceImage);
            TiffWriter destination = TiffApi.Factory.CreatePathWriter(destinationPath, new TiffWriteOptions());
            TiffMetadataRewriteOptions rewriteOptions = CreateRewriteOptions("path rewrite");

            TiffMetadataRewriteResult rewrite = TiffApi.Copy.RewriteImageMetadata(sourceImage, destination, rewriteOptions);
            destination.Close();
            sourceFile.Close();

            TiffFile rewrittenFile = TiffApi.Factory.OpenReadPath(destinationPath, new TiffReadOptions());
            TiffImage rewrittenImage = rewrittenFile.GetImage(0);

            Assert.That(rewrite.Success, Is.True);
            Assert.That(rewrite.RawImageDataPassedThrough, Is.True);
            Assert.That(decoder.DecodeCallCount, Is.EqualTo(0));
            Assert.That(ReadRawStrips(rewrittenImage), Is.EqualTo(expectedStrips));
            Assert.That(ReadDescription(rewrittenImage), Is.EqualTo("path rewrite"));
            Assert.That(destination.IsOpen, Is.False);
            Assert.That(sourceFile.IsOpen, Is.False);
            Assert.That(File.Exists(destinationPath), Is.True);

            rewrittenFile.Close();
        }
        finally
        {
            DeleteIfExists(sourcePath);
            DeleteIfExists(destinationPath);
        }
    }

    [Test]
    public void RewriteImageMetadata_StreamWorkflowPreservesCompressedTilesAndOwnership()
    {
        byte[] sourceBytes = CreateCompressedSource(true, TiffCompression.Deflate);
        CountingDecoder decoder = new CountingDecoder(TiffCompression.Deflate);
        MemoryStream borrowedSourceStream = new MemoryStream(sourceBytes);
        MemoryStream borrowedDestinationStream = new MemoryStream();
        TiffReadOptions borrowedReadOptions = new TiffReadOptions();
        borrowedReadOptions.LeaveOpen = true;
        borrowedReadOptions.CompressionCodec = decoder;
        TiffWriteOptions borrowedWriteOptions = new TiffWriteOptions();
        borrowedWriteOptions.LeaveOpen = true;
        TiffFile borrowedSource = TiffApi.Factory.OpenReadStream(borrowedSourceStream, borrowedReadOptions);
        TiffImage borrowedImage = borrowedSource.GetImage(0);
        byte[][] expectedTiles = ReadRawTiles(borrowedImage);
        TiffWriter borrowedDestination = TiffApi.Factory.CreateStreamWriter(borrowedDestinationStream, borrowedWriteOptions);

        TiffMetadataRewriteResult borrowedRewrite = TiffApi.Copy.RewriteImageMetadata(borrowedImage, borrowedDestination, CreateRewriteOptions("stream rewrite"));
        borrowedDestination.Close();
        borrowedSource.Close();

        TiffFile rewrittenFile = TiffApi.Factory.OpenReadBytes(borrowedDestinationStream.ToArray(), new TiffReadOptions());
        TiffImage rewrittenImage = rewrittenFile.GetImage(0);

        Assert.That(borrowedRewrite.Success, Is.True);
        Assert.That(borrowedRewrite.RawImageDataPassedThrough, Is.True);
        Assert.That(decoder.DecodeCallCount, Is.EqualTo(0));
        Assert.That(ReadRawTiles(rewrittenImage), Is.EqualTo(expectedTiles));
        Assert.That(ReadDescription(rewrittenImage), Is.EqualTo("stream rewrite"));
        Assert.That(borrowedSourceStream.CanRead, Is.True);
        Assert.That(borrowedDestinationStream.CanWrite, Is.True);

        rewrittenFile.Close();

        MemoryStream ownedSourceStream = new MemoryStream(sourceBytes);
        MemoryStream ownedDestinationStream = new MemoryStream();
        TiffFile ownedSource = TiffApi.Factory.OpenReadStream(ownedSourceStream, new TiffReadOptions());
        TiffWriter ownedDestination = TiffApi.Factory.CreateStreamWriter(ownedDestinationStream, new TiffWriteOptions());

        TiffMetadataRewriteResult ownedRewrite = TiffApi.Copy.RewriteImageMetadata(ownedSource.GetImage(0), ownedDestination, CreateRewriteOptions("owned rewrite"));
        ownedDestination.Close();
        ownedSource.Close();

        Assert.That(ownedRewrite.Success, Is.True);
        Assert.That(ownedSourceStream.CanRead, Is.False);
        Assert.That(ownedDestinationStream.CanWrite, Is.False);
    }

    [Test]
    public void CopyRawImageDataToDefinition_RejectsLayoutAndCodecChangesWithLocatedDiagnostics()
    {
        byte[] sourceBytes = CreateCompressedSource(false, TiffCompression.PackBits);
        TiffFile sourceFile = TiffApi.Factory.OpenReadBytes(sourceBytes, new TiffReadOptions());
        TiffImage sourceImage = sourceFile.GetImage(0);
        TiffRawImageDataCopyResult layoutBaseline = TiffApi.Copy.CopyRawImageData(sourceImage);
        TiffRawImageDataCopyResult codecBaseline = TiffApi.Copy.CopyRawImageData(sourceImage);
        TiffApi.Data.SetImageTag(layoutBaseline.Definition, TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 4));
        TiffApi.Data.SetImageTag(codecBaseline.Definition, TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.Lzw));

        TiffRawImageDataCopyResult layoutResult = TiffApi.Copy.CopyRawImageDataToDefinition(sourceImage, layoutBaseline.Definition);
        TiffRawImageDataCopyResult codecResult = TiffApi.Copy.CopyRawImageDataToDefinition(sourceImage, codecBaseline.Definition);

        Assert.That(layoutResult.Success, Is.False);
        AssertLocatedIncompatibility(layoutResult.Diagnostics, TiffTagId.RowsPerStrip);
        Assert.That(codecResult.Success, Is.False);
        AssertLocatedIncompatibility(codecResult.Diagnostics, TiffTagId.Compression);

        sourceFile.Close();
    }

    static byte[] CreateCompressedSource(bool tiled, TiffCompression compression)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 4;
        configuration.Length = 4;
        configuration.Compression = compression;
        if (tiled)
        {
            configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
            configuration.Layout.TileWidth = 2;
            configuration.Layout.TileLength = 2;
        }
        else
        {
            configuration.Layout.Kind = TiffWriterLayoutKind.Strips;
            configuration.Layout.RowsPerStrip = 2;
        }

        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffDiagnostics metadata = writer.WriteTag(imageIndex, TiffApi.Data.CreateAsciiTag(TiffTagId.ImageDescription, "original"));
        TiffDiagnostics raster = writer.WriteSourceRaster(imageIndex, new byte[]
        {
            1, 1, 1, 1,
            2, 3, 3, 2,
            4, 4, 4, 4,
            5, 6, 6, 5
        });
        TiffDiagnostics flush = writer.Flush();

        Assert.That(imageIndex, Is.EqualTo(0));
        Assert.That(metadata.HasErrors(), Is.False);
        Assert.That(raster.HasErrors(), Is.False);
        Assert.That(flush.HasErrors(), Is.False);
        return writer.GetFileBytes();
    }

    static TiffMetadataRewriteOptions CreateRewriteOptions(string description)
    {
        TiffMetadataRewriteOptions options = new TiffMetadataRewriteOptions();
        options.Updates = new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(TiffTagId.ImageDescription, description)
        };
        return options;
    }

    static byte[][] ReadRawStrips(TiffImage image)
    {
        byte[][] strips = new byte[(int)image.GetStripCount()][];
        int index = 0;
        while (index < strips.Length)
        {
            strips[index] = image.ReadRawStrip(index).Bytes;
            index = index + 1;
        }

        return strips;
    }

    static byte[][] ReadRawTiles(TiffImage image)
    {
        byte[][] tiles = new byte[(int)image.GetTileCount()][];
        int index = 0;
        while (index < tiles.Length)
        {
            tiles[index] = image.ReadRawTile(index).Bytes;
            index = index + 1;
        }

        return tiles;
    }

    static string ReadDescription(TiffImage image)
    {
        TiffMetadataCopyResult metadata = TiffApi.Copy.CopyMetadata(image);
        int index = 0;
        while (index < metadata.Values.Length)
        {
            if (metadata.Values[index].TagId == TiffTagId.ImageDescription)
            {
                return metadata.Values[index].Text;
            }

            index = index + 1;
        }

        return "";
    }

    static void AssertLocatedIncompatibility(TiffDiagnostics diagnostics, int tagId)
    {
        Assert.That(diagnostics.Count(), Is.EqualTo(1));
        TiffDiagnostic diagnostic = diagnostics.Get(0);
        Assert.That(diagnostic.Code, Is.EqualTo(TiffErrorCode.InvalidTagValue));
        Assert.That(diagnostic.ImageIndex, Is.EqualTo(0));
        Assert.That(diagnostic.TagId, Is.EqualTo(tagId));
        Assert.That(diagnostic.Message, Does.Contain("not compatible"));
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        DeleteIfExists(path);
        return path;
    }

    static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    class CountingDecoder : ITiffCompressionCodec
    {
        public TiffCompression Compression;
        public int DecodeCallCount;

        public CountingDecoder(TiffCompression compression)
        {
            Compression = compression;
            DecodeCallCount = 0;
        }

        public bool CanDecode(TiffCompression compression)
        {
            return compression == Compression;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return false;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            DecodeCallCount = DecodeCallCount + 1;
            TiffCodecResult result = new TiffCodecResult();
            result.Success = true;
            result.Bytes = new byte[(int)context.ExpectedDecodedByteCount];
            return result;
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            return new TiffCodecResult();
        }
    }
}
