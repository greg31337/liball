using System;
using System.Buffers;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMemoryPolicyTests
{
    [Test]
    public void MemoryInput_FailsBeforeCopyWhenPolicyAllocationLimitIsExceeded()
    {
        byte[] bytes = CreateSingleStripFile(2, 2, new byte[] { 1, 2, 3, 4 });
        TiffReadOptions options = new TiffReadOptions();
        options.MemoryPolicy.MaximumAllocationBytes = bytes.Length - 1;

        TiffFile file = TiffApi.Factory.OpenReadBytes(bytes, options);

        Assert.That(file.GetSuccess(), Is.False);
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void BufferedSource_PreflightsCompleteAllocationBeforeReading()
    {
        byte[] bytes = CreateSingleStripFile(2, 2, new byte[] { 1, 2, 3, 4 });
        TrackingSource source = new TrackingSource(bytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;
        options.MemoryPolicy.SourceLoadingMode = TiffSourceLoadingMode.Buffered;
        options.MemoryPolicy.MaximumAllocationBytes = bytes.Length - 1;

        TiffFile file = TiffApi.Factory.OpenReadSource(source, options);

        Assert.That(file.GetSuccess(), Is.False);
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
        Assert.That(source.ReadCallCount, Is.EqualTo(0));
        Assert.That(source.GetIsOpen(), Is.True);
    }

    [Test]
    public void SourceLoadingMode_SelectsStreamingOrCompleteBuffering()
    {
        byte[] fileBytes = CreateSingleStripFile(2, 2, new byte[] { 1, 2, 3, 4 });
        byte[] paddedBytes = new byte[fileBytes.Length + 4096];
        fileBytes.CopyTo(paddedBytes, 0);
        TrackingSource streamingSource = new TrackingSource(paddedBytes);
        TiffReadOptions streamingOptions = new TiffReadOptions();
        streamingOptions.LeaveOpen = true;
        streamingOptions.PreserveTrailingData = false;
        streamingOptions.MemoryPolicy.SourceLoadingMode = TiffSourceLoadingMode.Streaming;

        TiffFile streamingFile = TiffApi.Factory.OpenReadSource(streamingSource, streamingOptions);

        Assert.That(streamingFile.GetSuccess(), Is.True);
        Assert.That(streamingFile.GetSource(), Is.SameAs(streamingSource));
        Assert.That(streamingSource.TotalRequested, Is.LessThan(paddedBytes.Length));

        TrackingSource bufferedSource = new TrackingSource(paddedBytes);
        TiffReadOptions bufferedOptions = new TiffReadOptions();
        bufferedOptions.LeaveOpen = true;
        bufferedOptions.PreserveTrailingData = false;
        bufferedOptions.MemoryPolicy.SourceLoadingMode = TiffSourceLoadingMode.Buffered;

        TiffFile bufferedFile = TiffApi.Factory.OpenReadSource(bufferedSource, bufferedOptions);

        Assert.That(bufferedFile.GetSuccess(), Is.True);
        Assert.That(bufferedFile.GetSource(), Is.Not.SameAs(bufferedSource));
        Assert.That(bufferedSource.TotalRequested, Is.EqualTo(paddedBytes.Length));
        Assert.That(bufferedSource.GetIsOpen(), Is.True);
    }

    [Test]
    public void MaximumDecodedBytes_RejectsCallerBufferReadBeforePayloadAccess()
    {
        byte[] bytes = CreateSingleStripFile(2, 2, new byte[] { 1, 2, 3, 4 });
        TrackingSource source = new TrackingSource(bytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;
        options.MemoryPolicy.MaximumDecodedBytes = 3;
        TiffImage image = TiffApi.Factory.OpenReadSource(source, options).GetImage(0);
        int readsBeforeRaster = source.ReadCallCount;

        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(new byte[4]);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
        Assert.That(source.ReadCallCount, Is.EqualTo(readsBeforeRaster));
    }

    [Test]
    public void LazyThresholdAndChoppedStripTarget_AreAppliedByNormalImageOperations()
    {
        TiffReadOptions lazyOptions = new TiffReadOptions();
        lazyOptions.MemoryPolicy.LazyTagValueByteThreshold = 10;
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.DataType = TiffDataType.Byte;
        entry.Count = 11;
        entry.RawCount = 11;
        entry.ValueByteCount = 11;
        entry.ValueSlotByteCount = 4;

        Assert.That(lazyOptions.ShouldLoadTagValueLazily(entry), Is.True);

        byte[] bytes = CreateSingleStripFile(2, 4, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        TiffReadOptions chopOptions = new TiffReadOptions();
        chopOptions.MemoryPolicy.ChoppedStripByteCount = 4;
        TiffStripLayoutResult layout = TiffApi.Factory.OpenReadBytes(bytes, chopOptions).GetImage(0).GetStripLayout();

        Assert.That(layout.Success, Is.True);
        Assert.That(layout.Layout.IsChopped, Is.True);
        Assert.That(layout.Layout.SourceStripCount, Is.EqualTo(1));
        Assert.That(layout.Layout.StripCount, Is.EqualTo(2));
        Assert.That(layout.Layout.Segments[1].FirstRow, Is.EqualTo(2));
    }

    [Test]
    public void PooledSegmentBuffers_AreRentedAndReturnedDuringDecode()
    {
        byte[] bytes = CreateSingleStripFile(2, 2, new byte[] { 1, 2, 3, 4 });
        CountingPool pool = new CountingPool();
        PassThroughCodec codec = new PassThroughCodec();
        TiffReadOptions options = new TiffReadOptions();
        options.CompressionCodec = codec;
        options.MemoryPolicy.UsePooledSegmentBuffers = true;
        options.MemoryPolicy.SegmentBufferPool = pool;
        TiffImage image = TiffApi.Factory.OpenReadBytes(bytes, options).GetImage(0);
        byte[] scanline = new byte[2];

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(0, scanline);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(scanline, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(1));
        Assert.That(pool.RentCallCount, Is.EqualTo(1));
        Assert.That(pool.ReturnCallCount, Is.EqualTo(1));
    }

    static byte[] CreateSingleStripFile(int width, int length, byte[] pixels)
    {
        TiffWriter writer = TiffWriter.Create();
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, (ulong)width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, (ulong)length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, (ulong)length)
        };
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(tags));
        writer.WriteRawStrip(imageIndex, 0, pixels);
        writer.Flush();
        return writer.GetFileBytes();
    }

    class TrackingSource : ITiffRandomAccessSource
    {
        public byte[] Bytes;
        public bool IsOpen;
        public int ReadCallCount;
        public long TotalRequested;

        public TrackingSource(byte[] bytes)
        {
            Bytes = bytes;
            IsOpen = true;
            ReadCallCount = 0;
            TotalRequested = 0;
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
            ReadCallCount = ReadCallCount + 1;
            TotalRequested = TotalRequested + destination.Length;
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
    }

    class CountingPool : ArrayPool<byte>
    {
        public int RentCallCount;
        public int ReturnCallCount;

        public CountingPool()
        {
            RentCallCount = 0;
            ReturnCallCount = 0;
        }

        public override byte[] Rent(int minimumLength)
        {
            RentCallCount = RentCallCount + 1;
            return new byte[minimumLength];
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            ReturnCallCount = ReturnCallCount + 1;
        }
    }

    class PassThroughCodec : ITiffCompressionCodec
    {
        public int DecodeCallCount;

        public PassThroughCodec()
        {
            DecodeCallCount = 0;
        }

        public bool CanDecode(TiffCompression compression)
        {
            return compression == TiffCompression.None;
        }

        public bool CanEncode(TiffCompression compression)
        {
            return false;
        }

        public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            DecodeCallCount = DecodeCallCount + 1;
            return TiffCodecResult.Create(source.ToArray(), new TiffDiagnostics());
        }

        public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnsupportedCompression, "Encoding is not supported by this test codec.");
            return TiffCodecResult.Create(new byte[0], diagnostics);
        }
    }
}
