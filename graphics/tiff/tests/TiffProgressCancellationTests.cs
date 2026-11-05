using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffProgressCancellationTests
{
    [Test]
    public void OpenAndDecode_ReportStartAndCompletion()
    {
        byte[] fileBytes = CreateFileBytes(1);
        ProgressRecorder openProgress = new ProgressRecorder();
        TiffMemorySource source = new TiffMemorySource(fileBytes);

        TiffFile file = new TiffFactory().OpenReadSourceWithCancellation(source, new TiffReadOptions(), openProgress, CancellationToken.None);

        Assert.That(file.GetSuccess(), Is.True);
        AssertProgressCompleted(openProgress, TiffProgressOperation.Open, 1);

        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.None;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 2, new TiffLimits());
        ProgressRecorder decodeProgress = new ProgressRecorder();
        TiffCodecResult decoded = new TiffCodecService().DecodeWithCancellation(context, new byte[] { 7, 8 }, null, decodeProgress, CancellationToken.None);

        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 7, 8 }));
        AssertProgressCompleted(decodeProgress, TiffProgressOperation.Decode, 1);
        file.Close();
    }

    [Test]
    public void OpenWithCancellation_RejectsPreCanceledTokenBeforeUsingSource()
    {
        TiffMemorySource source = new TiffMemorySource(CreateFileBytes(1));
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(() => new TiffFactory().OpenReadSourceWithCancellation(source, new TiffReadOptions(), null, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
        Assert.That(source.GetIsOpen(), Is.True);
    }

    [Test]
    public void Validation_CancelsBetweenDirectories()
    {
        TiffFile file = new TiffFactory().OpenReadBytes(CreateFileBytes(2), new TiffReadOptions());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        ProgressRecorder progress = ProgressRecorder.CreateCanceling(TiffProgressOperation.Validation, 1, cancellation);

        Assert.That(() => new TiffValidationService().ValidateFileWithCancellation(file, progress, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
        Assert.That(progress.Values.Count, Is.EqualTo(2));
        Assert.That(progress.Values[0].CompletedUnits, Is.EqualTo(0));
        Assert.That(progress.Values[0].TotalUnits, Is.EqualTo(2));
        Assert.That(progress.Values[1].CompletedUnits, Is.EqualTo(1));
        file.Close();
    }

    [Test]
    public void RegionRead_CancelsBetweenRowsAndLeavesUnreadRowsUntouched()
    {
        TiffFile file = new TiffFactory().OpenReadBytes(CreateFileBytes(1), new TiffReadOptions());
        TiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = new TiffRegionReadOptions();
        options.Region.Width = 2;
        options.Region.Length = 4;
        byte[] destination = new byte[8];
        CancellationTokenSource cancellation = new CancellationTokenSource();
        ProgressRecorder progress = ProgressRecorder.CreateCanceling(TiffProgressOperation.RegionRead, 2, cancellation);

        Assert.That(() => image.ReadRegionToBufferWithCancellation(options, destination, progress, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 0, 0, 0, 0 }));
        Assert.That(progress.Values[progress.Values.Count - 1].CompletedUnits, Is.EqualTo(2));
        Assert.That(progress.Values[progress.Values.Count - 1].TotalUnits, Is.EqualTo(4));
        file.Close();
    }

    [Test]
    public void RawCopy_CancelsBetweenSegments()
    {
        TiffFile file = new TiffFactory().OpenReadBytes(CreateFileBytes(1), new TiffReadOptions());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        ProgressRecorder progress = ProgressRecorder.CreateCanceling(TiffProgressOperation.Copy, 2, cancellation);

        Assert.That(() => new TiffCopyService().CopyRawImageDataWithCancellation(file.GetImage(0), progress, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
        Assert.That(progress.Values[0].CompletedUnits, Is.EqualTo(0));
        Assert.That(progress.Values[0].TotalUnits, Is.EqualTo(4));
        Assert.That(progress.Values[progress.Values.Count - 1].CompletedUnits, Is.EqualTo(2));
        file.Close();
    }

    [Test]
    public void SourceRasterWrite_CancelsBetweenSegmentsAndWriterRemainsUsable()
    {
        TiffWriter writer = CreateWriter();
        int imageIndex = writer.AddConfiguredImage(CreateConfiguration());
        byte[] source = CreateRasterBytes();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        ProgressRecorder progress = ProgressRecorder.CreateCanceling(TiffProgressOperation.Write, 2, cancellation);

        Assert.That(() => writer.WriteSourceRasterWithCancellation(imageIndex, source, progress, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
        Assert.That(progress.Values[0].TotalUnits, Is.EqualTo(4));
        Assert.That(progress.Values[progress.Values.Count - 1].CompletedUnits, Is.EqualTo(2));

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();

        Assert.That(write.HasErrors(), Is.False);
        Assert.That(flush.HasErrors(), Is.False);
        Assert.That(writer.GetFileByteCount(), Is.GreaterThan(0));
        writer.Close();
    }

    [Test]
    public void RegionCopyAndWrite_ReportMonotonicCompletion()
    {
        byte[] fileBytes = CreateFileBytes(1);
        TiffFile file = new TiffFactory().OpenReadBytes(fileBytes, new TiffReadOptions());
        TiffRegionReadOptions regionOptions = new TiffRegionReadOptions();
        regionOptions.Region.Width = 2;
        regionOptions.Region.Length = 4;
        ProgressRecorder regionProgress = new ProgressRecorder();

        TiffDiagnostics region = file.GetImage(0).ReadRegionToBufferWithCancellation(regionOptions, new byte[8], regionProgress, CancellationToken.None);
        ProgressRecorder copyProgress = new ProgressRecorder();
        TiffRawImageDataCopyResult copy = new TiffCopyService().CopyRawImageDataWithCancellation(file.GetImage(0), copyProgress, CancellationToken.None);
        TiffWriter writer = CreateWriter();
        int imageIndex = writer.AddConfiguredImage(CreateConfiguration());
        ProgressRecorder writeProgress = new ProgressRecorder();
        TiffDiagnostics write = writer.WriteSourceRasterWithCancellation(imageIndex, CreateRasterBytes(), writeProgress, CancellationToken.None);

        Assert.That(region.HasErrors(), Is.False);
        Assert.That(copy.Success, Is.True);
        Assert.That(write.HasErrors(), Is.False);
        AssertProgressCompleted(regionProgress, TiffProgressOperation.RegionRead, 4);
        AssertProgressCompleted(copyProgress, TiffProgressOperation.Copy, 4);
        AssertProgressCompleted(writeProgress, TiffProgressOperation.Write, 4);
        AssertMonotonic(regionProgress.Values);
        AssertMonotonic(copyProgress.Values);
        AssertMonotonic(writeProgress.Values);
        writer.Close();
        file.Close();
    }

    static void AssertProgressCompleted(ProgressRecorder recorder, TiffProgressOperation operation, long totalUnits)
    {
        Assert.That(recorder.Values.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(recorder.Values[0].Operation, Is.EqualTo(operation));
        Assert.That(recorder.Values[0].CompletedUnits, Is.EqualTo(0));
        Assert.That(recorder.Values[0].TotalUnits, Is.EqualTo(totalUnits));
        TiffProgress last = recorder.Values[recorder.Values.Count - 1];
        Assert.That(last.Operation, Is.EqualTo(operation));
        Assert.That(last.CompletedUnits, Is.EqualTo(totalUnits));
        Assert.That(last.TotalUnits, Is.EqualTo(totalUnits));
    }

    static void AssertMonotonic(List<TiffProgress> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            Assert.That(values[index].CompletedUnits, Is.GreaterThan(values[index - 1].CompletedUnits));
            Assert.That(values[index].TotalUnits, Is.EqualTo(values[0].TotalUnits));
            index = index + 1;
        }
    }

    static byte[] CreateFileBytes(int imageCount)
    {
        TiffWriter writer = CreateWriter();
        int index = 0;
        while (index < imageCount)
        {
            int imageIndex = writer.AddConfiguredImage(CreateConfiguration());
            TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, CreateRasterBytes());
            Assert.That(write.HasErrors(), Is.False);
            index = index + 1;
        }

        TiffDiagnostics flush = writer.Flush();
        Assert.That(flush.HasErrors(), Is.False);
        byte[] bytes = writer.GetFileBytes();
        writer.Close();
        return bytes;
    }

    static TiffWriter CreateWriter()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.ValidateBeforeWrite = true;
        return new TiffFactory().CreateWriter(options);
    }

    static TiffWriterImageConfiguration CreateConfiguration()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 4;
        configuration.Compression = TiffCompression.None;
        configuration.Layout.Kind = TiffWriterLayoutKind.Strips;
        configuration.Layout.RowsPerStrip = 1;
        return configuration;
    }

    static byte[] CreateRasterBytes()
    {
        return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
    }

    class ProgressRecorder : ITiffProgress
    {
        public List<TiffProgress> Values;
        public TiffProgressOperation CancelOperation;
        public long CancelAt;
        public CancellationTokenSource? Cancellation;

        public ProgressRecorder()
        {
            Values = new List<TiffProgress>();
            CancelOperation = TiffProgressOperation.Open;
            CancelAt = -1;
            Cancellation = null;
        }

        public static ProgressRecorder CreateCanceling(TiffProgressOperation operation, long cancelAt, CancellationTokenSource cancellation)
        {
            ProgressRecorder value = new ProgressRecorder();
            value.CancelOperation = operation;
            value.CancelAt = cancelAt;
            value.Cancellation = cancellation;
            return value;
        }

        public void Report(TiffProgress progress)
        {
            Values.Add(progress);
            if (Cancellation != null && progress.Operation == CancelOperation && progress.CompletedUnits == CancelAt)
            {
                Cancellation.Cancel();
            }
        }
    }
}
