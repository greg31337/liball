using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCloudOptimizedGeoTiffHighLevelTests
{
    [Test]
    public void AddCloudOptimizedGeoTiff_WritesInternalOverviewsInRangeFriendlyOrder()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult configured = writer.AddCloudOptimizedGeoTiff(CreateConfiguration(32, 16, 8));

        Assert.That(configured.Success, Is.True);
        Assert.That(configured.BaseImageIndex, Is.EqualTo(0));
        Assert.That(configured.OverviewImageIndices, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(writer.WriteSourceRaster(configured.BaseImageIndex, CreateBytes(32 * 32)).Count(), Is.EqualTo(0));
        Assert.That(writer.WriteSubImageSourceRaster(configured.BaseImageIndex, configured.OverviewImageIndices[0], CreateBytes(16 * 16)).Count(), Is.EqualTo(0));
        Assert.That(writer.WriteSubImageSourceRaster(configured.BaseImageIndex, configured.OverviewImageIndices[1], CreateBytes(8 * 8)).Count(), Is.EqualTo(0));
        Assert.That(writer.Flush().Count(), Is.EqualTo(0));

        TiffCloudOptimizedGeoTiffValidationResult validation = TiffApi.Validation.ValidateCloudOptimizedBytes(writer.GetFileBytes(), new TiffReadOptions());

        Assert.That(validation.Success, Is.True);
        Assert.That(validation.HasRangeFriendlyLayout, Is.True);
        Assert.That(validation.ImageCount, Is.EqualTo(3));
        Assert.That(validation.FullResolutionImageCount, Is.EqualTo(1));
        Assert.That(validation.OverviewImageCount, Is.EqualTo(2));
        Assert.That(validation.InternalOverviewImageCount, Is.EqualTo(2));
        Assert.That(validation.FirstTileOffset, Is.GreaterThan(0));
        Assert.That(validation.LastTileEndOffset, Is.EqualTo((ulong)writer.GetFileByteCount()));

        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        TiffImage baseImage = file.GetImage(0);
        Assert.That(baseImage.GetPyramidLevelCount(), Is.EqualTo(3));
        Assert.That(baseImage.GetPyramidLevel(1).GetInfo().Width, Is.EqualTo(16));
        Assert.That(baseImage.GetPyramidLevel(2).GetInfo().Width, Is.EqualTo(8));
    }

    [Test]
    public void ValidateCloudOptimizedSource_ReadsMetadataRangesWithoutReadingTilePayloads()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult configured = writer.AddCloudOptimizedGeoTiff(CreateConfiguration(32, 16));
        writer.WriteSourceRaster(configured.BaseImageIndex, CreateBytes(32 * 32));
        writer.WriteSubImageSourceRaster(configured.BaseImageIndex, configured.OverviewImageIndices[0], CreateBytes(16 * 16));
        writer.Flush();
        TrackingSource source = new TrackingSource(writer.GetFileBytes());
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffCloudOptimizedGeoTiffValidationResult validation = TiffApi.Validation.ValidateCloudOptimizedSource(source, options);

        Assert.That(validation.Success, Is.True);
        Assert.That(source.GetIsOpen(), Is.True);
        Assert.That(source.TotalRequested, Is.LessThan(source.GetLength()));
        Assert.That(source.LargestRequest, Is.LessThan(256));
        Assert.That(source.HasReadAtOrAfter((long)validation.FirstTileOffset), Is.False);
    }

    [Test]
    public void AddCloudOptimizedGeoTiff_RejectsStrippedBaseBeforeAddingImages()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteConfiguration configuration = CreateConfiguration(32, 16);
        configuration.BaseImage.Layout.Kind = TiffWriterLayoutKind.Strips;
        configuration.BaseImage.Layout.RowsPerStrip = 16;

        TiffCloudOptimizedGeoTiffWriteResult result = writer.AddCloudOptimizedGeoTiff(configuration);

        Assert.That(result.Success, Is.False);
        Assert.That(result.BaseImageIndex, Is.EqualTo(-1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(writer.Flush().ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void BigTiffCloudOptimizedOutput_UsesAlignedLong8TileArrays()
    {
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.FileKind = TiffFileKind.BigTiff;
        TiffWriter writer = TiffApi.Factory.CreateWriter(writeOptions);
        TiffCloudOptimizedGeoTiffWriteResult configured = writer.AddCloudOptimizedGeoTiff(CreateConfiguration(16, 8));
        writer.WriteSourceRaster(configured.BaseImageIndex, CreateBytes(16 * 16));
        writer.WriteSubImageSourceRaster(configured.BaseImageIndex, configured.OverviewImageIndices[0], CreateBytes(8 * 8));
        writer.Flush();

        TiffFile file = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions());
        TiffIfdEntry offsets = file.GetImage(0).GetTag(TiffTagId.TileOffsets)!;
        TiffCloudOptimizedGeoTiffValidationResult validation = TiffApi.Validation.ValidateCloudOptimizedFile(file);

        Assert.That(validation.Success, Is.True);
        Assert.That(validation.Header.FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(offsets.DataType, Is.EqualTo(TiffDataType.Long8));
        Assert.That(file.GetDirectories()[0].Offset % 8, Is.EqualTo(0));
        Assert.That(offsets.HasExternalValue(), Is.True);
        Assert.That(offsets.ValueOffset % 8, Is.EqualTo(0));
    }

    static TiffCloudOptimizedGeoTiffWriteConfiguration CreateConfiguration(int baseSize, params int[] overviewSizes)
    {
        TiffCloudOptimizedGeoTiffWriteConfiguration configuration = new TiffCloudOptimizedGeoTiffWriteConfiguration();
        configuration.BaseImage = CreateImageConfiguration(baseSize);
        configuration.Overviews = new TiffWriterImageConfiguration[overviewSizes.Length];
        int index = 0;
        while (index < overviewSizes.Length)
        {
            configuration.Overviews[index] = CreateImageConfiguration(overviewSizes[index]);
            index = index + 1;
        }

        configuration.Model.HasPixelScale = true;
        configuration.Model.PixelScale = new double[] { 1, 1, 0 };
        configuration.Model.HasTiepoints = true;
        TiffGeoTiepoint tiepoint = new TiffGeoTiepoint();
        tiepoint.ModelX = 100;
        tiepoint.ModelY = 200;
        configuration.Model.Tiepoints = new TiffGeoTiepoint[] { tiepoint };
        configuration.KeyDirectory.HasDirectory = true;
        configuration.KeyDirectory.KeyDirectoryVersion = 1;
        configuration.KeyDirectory.KeyRevision = 1;
        configuration.KeyDirectory.MinorRevision = 0;
        configuration.KeyDirectory.NumberOfKeys = 0;
        return configuration;
    }

    static TiffWriterImageConfiguration CreateImageConfiguration(int size)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = size;
        configuration.Length = size;
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.TileWidth = 8;
        configuration.Layout.TileLength = 8;
        configuration.Layout.TileDepth = 1;
        return configuration;
    }

    static byte[] CreateBytes(int count)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < count)
        {
            bytes[index] = (byte)(index % 251);
            index = index + 1;
        }

        return bytes;
    }

    class TrackingSource : ITiffRandomAccessSource
    {
        public byte[] Bytes;
        public bool IsOpen;
        public long TotalRequested;
        public int LargestRequest;
        public long[] Offsets;

        public TrackingSource(byte[] bytes)
        {
            Bytes = bytes;
            IsOpen = true;
            TotalRequested = 0;
            LargestRequest = 0;
            Offsets = new long[0];
        }

        public bool GetIsOpen()
        {
            return IsOpen;
        }

        public long GetLength()
        {
            return Bytes.Length;
        }

        public int Read(long offset, Span<byte> destination)
        {
            TotalRequested = TotalRequested + destination.Length;
            if (destination.Length > LargestRequest)
            {
                LargestRequest = destination.Length;
            }

            long[] offsets = new long[Offsets.Length + 1];
            Offsets.CopyTo(offsets, 0);
            offsets[offsets.Length - 1] = offset;
            Offsets = offsets;
            int count = destination.Length;
            if (Bytes.Length - offset < count)
            {
                count = (int)(Bytes.Length - offset);
            }

            Bytes.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public bool HasReadAtOrAfter(long offset)
        {
            int index = 0;
            while (index < Offsets.Length)
            {
                if (Offsets[index] >= offset)
                {
                    return true;
                }

                index = index + 1;
            }

            return false;
        }
    }
}
