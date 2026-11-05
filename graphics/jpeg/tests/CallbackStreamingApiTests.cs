using System.Collections.Generic;

namespace jpeg.Tests;

internal class CallbackStreamingApiTests
{
    [Test]
    public void Reader_PublishesRowsInOrderThroughOneBoundedReusableBuffer()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        TrackingDecodedRows callback = new TrackingDecodedRows();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegImage image = reader.ReadRowsWithOptionsAndCancellation(
            data,
            new global::jpeg.JpegReadOptions(),
            callback,
            default(System.Threading.CancellationToken));

        Assert.That(callback.RowIndexes, Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(callback.SampleCounts, Is.All.EqualTo(8));
        Assert.That(callback.BufferLengths, Is.All.EqualTo(8));
        Assert.That(callback.ReusedOneBuffer, Is.True);
        Assert.That(callback.FirstSamples, Is.All.EqualTo(138));
        Assert.That(image.Pixels[0], Is.EqualTo(138));
        Assert.That(image.Pixels[63], Is.EqualTo(138));
    }

    [Test]
    public void Reader_ChecksCancellationAfterEveryRowCallback()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        CancellingDecodedRows callback = new CancellingDecodedRows(cancellation);
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        Assert.Throws<System.OperationCanceledException>(
            delegate
            {
                reader.ReadRowsWithOptionsAndCancellation(
                    data,
                    new global::jpeg.JpegReadOptions(),
                    callback,
                    cancellation.Token);
            });
        Assert.That(callback.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void Writer_RequestsRowsThroughOneBoundedReusableBufferAndEncodesThem()
    {
        FillingEncodeRows callback = new FillingEncodeRows(138);
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();

        byte[] data = writer.WriteRowsWithOptionsAndCancellation(
            8,
            8,
            global::jpeg.JpegPixelFormat.L8,
            global::jpeg.JpegColorSpace.Grayscale,
            new global::jpeg.JpegWriteOptions(),
            callback,
            default(System.Threading.CancellationToken));
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);

        Assert.That(callback.RowIndexes, Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(callback.ReusedOneBuffer, Is.True);
        Assert.That(callback.BufferLength, Is.EqualTo(8));
        Assert.That(decoded.Pixels, Is.All.EqualTo(138));
    }

    [Test]
    public void Writer_ChecksCancellationAndRejectsReplacingItsReusableBuffer()
    {
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        CancellingEncodeRows cancellingCallback = new CancellingEncodeRows(cancellation);
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();

        Assert.Throws<System.OperationCanceledException>(
            delegate
            {
                writer.WriteRowsWithOptionsAndCancellation(
                    8,
                    8,
                    global::jpeg.JpegPixelFormat.L8,
                    global::jpeg.JpegColorSpace.Grayscale,
                    new global::jpeg.JpegWriteOptions(),
                    cancellingCallback,
                    cancellation.Token);
            });
        Assert.That(cancellingCallback.CallCount, Is.EqualTo(1));

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteRowsWithOptionsAndCancellation(
                    8,
                    8,
                    global::jpeg.JpegPixelFormat.L8,
                    global::jpeg.JpegColorSpace.Grayscale,
                    new global::jpeg.JpegWriteOptions(),
                    new ReplacingEncodeRowBuffer(),
                    default(System.Threading.CancellationToken));
            });
    }

    [Test]
    public void Reader_PublishesProgressivePreviewAfterEachRealScan()
    {
        byte[] data = CreateProgressiveGrayscaleJpeg(143);
        TrackingProgressivePreviews callback = new TrackingProgressivePreviews();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegImage image = reader.ReadProgressivePreviewsWithOptionsAndCancellation(
            data,
            new global::jpeg.JpegReadOptions(),
            callback,
            default(System.Threading.CancellationToken));

        Assert.That(callback.ScanIndexes, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(callback.ScanCounts, Is.All.EqualTo(2));
        Assert.That(callback.CompletedValues, Is.EqualTo(new bool[] { false, true }));
        Assert.That(callback.ReusedOneUpdate, Is.True);
        Assert.That(callback.FirstSamples, Is.All.EqualTo(143));
        Assert.That(image.Pixels, Is.All.EqualTo(143));
    }

    [Test]
    public void Reader_ChecksCancellationAfterProgressivePreviewCallback()
    {
        byte[] data = CreateProgressiveGrayscaleJpeg(143);
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        CancellingProgressivePreview callback = new CancellingProgressivePreview(cancellation);
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        Assert.Throws<System.OperationCanceledException>(
            delegate
            {
                reader.ReadProgressivePreviewsWithOptionsAndCancellation(
                    data,
                    new global::jpeg.JpegReadOptions(),
                    callback,
                    cancellation.Token);
            });
        Assert.That(callback.CallCount, Is.EqualTo(1));
    }

    internal static byte[] CreateProgressiveGrayscaleJpeg(int value)
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, value);
        List<global::jpeg.JpegProgressiveScanData> scans = new global::jpeg.JpegProgressiveSof2Encoder().Encode(frame, tables, source);
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        new global::jpeg.JpegQuantizationTableWriter().WriteMarkerSegment(writer, tables.QuantizationTables);
        new global::jpeg.JpegHuffmanTableWriter().WriteMarkerSegment(writer, tables.HuffmanTables);
        new global::jpeg.JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);

        int scanIndex = 0;
        while (scanIndex < scans.Count)
        {
            new global::jpeg.JpegScanHeaderWriter().WriteMarkerSegment(writer, scans[scanIndex].Scan);
            writer.Writer.WriteBytes(scans[scanIndex].EntropyData);
            scanIndex = scanIndex + 1;
        }

        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}

internal class TrackingDecodedRows : global::jpeg.IJpegDecodedRowCallback
{
    internal List<int> RowIndexes = new List<int>();
    internal List<int> SampleCounts = new List<int>();
    internal List<int> BufferLengths = new List<int>();
    internal List<int> FirstSamples = new List<int>();
    internal byte[] FirstBuffer = new byte[0];
    internal bool ReusedOneBuffer = true;

    public void OnDecodedRow(global::jpeg.JpegRowBuffer row)
    {
        if (RowIndexes.Count == 0)
        {
            FirstBuffer = row.Samples;
        }
        else if (!object.ReferenceEquals(FirstBuffer, row.Samples))
        {
            ReusedOneBuffer = false;
        }

        RowIndexes.Add(row.RowIndex);
        SampleCounts.Add(row.SampleCount);
        BufferLengths.Add(row.Samples.Length);
        FirstSamples.Add(row.Samples[0]);
    }
}

internal class CancellingDecodedRows : global::jpeg.IJpegDecodedRowCallback
{
    internal System.Threading.CancellationTokenSource Cancellation;
    internal int CallCount;

    internal CancellingDecodedRows(System.Threading.CancellationTokenSource cancellation)
    {
        Cancellation = cancellation;
    }

    public void OnDecodedRow(global::jpeg.JpegRowBuffer row)
    {
        CallCount = CallCount + 1;
        Cancellation.Cancel();
    }
}

internal class FillingEncodeRows : global::jpeg.IJpegEncodeRowCallback
{
    internal int Value;
    internal List<int> RowIndexes = new List<int>();
    internal byte[] FirstBuffer = new byte[0];
    internal bool ReusedOneBuffer = true;
    internal int BufferLength;

    internal FillingEncodeRows(int value)
    {
        Value = value;
    }

    public void FillEncodeRow(global::jpeg.JpegRowBuffer row)
    {
        if (RowIndexes.Count == 0)
        {
            FirstBuffer = row.Samples;
            BufferLength = row.Samples.Length;
        }
        else if (!object.ReferenceEquals(FirstBuffer, row.Samples))
        {
            ReusedOneBuffer = false;
        }

        RowIndexes.Add(row.RowIndex);
        int index = 0;
        while (index < row.SampleCount)
        {
            row.Samples[index] = (byte)Value;
            index = index + 1;
        }
    }
}

internal class CancellingEncodeRows : global::jpeg.IJpegEncodeRowCallback
{
    internal System.Threading.CancellationTokenSource Cancellation;
    internal int CallCount;

    internal CancellingEncodeRows(System.Threading.CancellationTokenSource cancellation)
    {
        Cancellation = cancellation;
    }

    public void FillEncodeRow(global::jpeg.JpegRowBuffer row)
    {
        CallCount = CallCount + 1;
        Cancellation.Cancel();
    }
}

internal class ReplacingEncodeRowBuffer : global::jpeg.IJpegEncodeRowCallback
{
    public void FillEncodeRow(global::jpeg.JpegRowBuffer row)
    {
        row.Samples = new byte[row.SampleCount];
    }
}

internal class TrackingProgressivePreviews : global::jpeg.IJpegProgressivePreviewCallback
{
    internal List<int> ScanIndexes = new List<int>();
    internal List<int> ScanCounts = new List<int>();
    internal List<bool> CompletedValues = new List<bool>();
    internal List<int> FirstSamples = new List<int>();
    internal global::jpeg.JpegProgressivePreview FirstUpdate = new global::jpeg.JpegProgressivePreview();
    internal bool ReusedOneUpdate = true;

    public void OnProgressivePreview(global::jpeg.JpegProgressivePreview preview)
    {
        if (ScanIndexes.Count == 0)
        {
            FirstUpdate = preview;
        }
        else if (!object.ReferenceEquals(FirstUpdate, preview))
        {
            ReusedOneUpdate = false;
        }

        ScanIndexes.Add(preview.ScanIndex);
        ScanCounts.Add(preview.ScanCount);
        CompletedValues.Add(preview.Completed);
        FirstSamples.Add(preview.Image.Pixels[0]);
    }
}

internal class CancellingProgressivePreview : global::jpeg.IJpegProgressivePreviewCallback
{
    internal System.Threading.CancellationTokenSource Cancellation;
    internal int CallCount;

    internal CancellingProgressivePreview(System.Threading.CancellationTokenSource cancellation)
    {
        Cancellation = cancellation;
    }

    public void OnProgressivePreview(global::jpeg.JpegProgressivePreview preview)
    {
        CallCount = CallCount + 1;
        Cancellation.Cancel();
    }
}
