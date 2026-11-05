namespace jpeg.Tests;

internal class ConcurrencyAndCancellationWorkflowTests
{
    [Test]
    public void IndependentServiceInstances_RunSimultaneouslyWithoutCrossTalkOrInputMutation()
    {
        byte[] source = JpegLosslessTransformerTests.CreateMetadataCoefficientJpeg();
        System.Threading.ManualResetEventSlim start = new System.Threading.ManualResetEventSlim(false);
        System.Threading.Tasks.Task<int>[] operations = new System.Threading.Tasks.Task<int>[48];
        int index = 0;
        while (index < operations.Length)
        {
            int operationIndex = index;
            operations[index] = System.Threading.Tasks.Task.Run(
                () => RunIndependentServiceOperation(start, source, operationIndex));
            index = index + 1;
        }

        start.Set();
        System.Threading.Tasks.Task.WaitAll(operations);

        index = 0;
        while (index < operations.Length)
        {
            Assert.That(operations[index].Result, Is.EqualTo(1), "operation " + index);
            index = index + 1;
        }
    }

    [Test]
    public void PreCanceledOperations_DoNotTouchCallerBuffersOrStreams()
    {
        byte[] source = JpegLosslessTransformerTests.CreateMetadataCoefficientJpeg();
        byte[] expectedSource = (byte[])source.Clone();
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(16, 16, 138);
        byte[] expectedPixels = (byte[])image.Pixels.Clone();
        byte[] destination = new byte[source.Length * 2];
        System.Array.Fill(destination, (byte)73);
        byte[] expectedDestination = (byte[])destination.Clone();
        System.IO.MemoryStream writerOutput = CreatePrefixedOutput();
        System.IO.MemoryStream transformInput = new System.IO.MemoryStream(source);
        System.IO.MemoryStream transformOutput = CreatePrefixedOutput();
        System.IO.MemoryStream inspectionInput = new System.IO.MemoryStream(source);
        System.Threading.CancellationToken canceled = new System.Threading.CancellationToken(true);

        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegIdentifierService().IdentifyWithCancellation(source, canceled);
        });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegReader().ReadWithCancellation(source, canceled);
        });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegWriter().WriteToSpanWithCancellation(image, destination, canceled);
        });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegWriter().WriteToStreamWithCancellation(image, writerOutput, canceled);
        });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegInspectionService().InspectMarkersStreamWithCancellation(inspectionInput, canceled);
        });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegTransformer().Rotate90StreamWithOptionsAndCancellation(
                transformInput,
                transformOutput,
                new global::jpeg.JpegTransformOptions(),
                canceled);
        });

        Assert.That(source, Is.EqualTo(expectedSource));
        Assert.That(image.Pixels, Is.EqualTo(expectedPixels));
        Assert.That(destination, Is.EqualTo(expectedDestination));
        Assert.That(writerOutput.ToArray(), Is.EqualTo(new byte[] { 17, 19, 23 }));
        Assert.That(transformOutput.ToArray(), Is.EqualTo(new byte[] { 17, 19, 23 }));
        Assert.That(inspectionInput.Position, Is.EqualTo(0));
        Assert.That(transformInput.Position, Is.EqualTo(0));
    }

    [TestCase("identifier")]
    [TestCase("reader")]
    [TestCase("inspector")]
    [TestCase("transformer")]
    public void ActiveStreamReads_StopAtTheFirstCancellationCheckpoint(string service)
    {
        byte[] source = JpegLosslessTransformerTests.CreateMetadataCoefficientJpeg();
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        CancelAfterFirstReadStream input = new CancelAfterFirstReadStream(source, cancellation);
        System.IO.MemoryStream output = CreatePrefixedOutput();

        Assert.Throws<System.OperationCanceledException>(delegate
        {
            RunCancelableStreamService(service, input, output, cancellation.Token);
        });

        Assert.That(input.ReadCalls, Is.EqualTo(1));
        Assert.That(input.BytesRead, Is.EqualTo(1));
        Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 17, 19, 23 }));
    }

    [Test]
    public void CallbackCancellation_StopsAfterOneCallbackWithoutMutatingEncodedInput()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        byte[] expectedSource = (byte[])source.Clone();
        System.Threading.CancellationTokenSource readCancellation = new System.Threading.CancellationTokenSource();
        CancellingDecodedRows readCallback = new CancellingDecodedRows(readCancellation);
        System.Threading.CancellationTokenSource writeCancellation = new System.Threading.CancellationTokenSource();
        CancellingEncodeRows writeCallback = new CancellingEncodeRows(writeCancellation);

        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegReader().ReadRowsWithOptionsAndCancellation(
                source,
                new global::jpeg.JpegReadOptions(),
                readCallback,
                readCancellation.Token);
        });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegWriter().WriteRowsWithOptionsAndCancellation(
                64,
                64,
                global::jpeg.JpegPixelFormat.L8,
                global::jpeg.JpegColorSpace.Grayscale,
                new global::jpeg.JpegWriteOptions(),
                writeCallback,
                writeCancellation.Token);
        });

        Assert.That(readCallback.CallCount, Is.EqualTo(1));
        Assert.That(writeCallback.CallCount, Is.EqualTo(1));
        Assert.That(source, Is.EqualTo(expectedSource));
    }

    public static int RunIndependentServiceOperation(
        System.Threading.ManualResetEventSlim start,
        byte[] sharedSource,
        int operationIndex)
    {
        start.Wait();
        byte[] source = (byte[])sharedSource.Clone();
        byte[] expected = (byte[])source.Clone();
        int service = operationIndex % 6;
        bool valid = false;
        if (service == 0)
        {
            valid = new global::jpeg.JpegIdentifierService().Identify(source).Width == 16;
        }
        else if (service == 1)
        {
            valid = new global::jpeg.JpegReader().Read(source).Width == 16;
        }
        else if (service == 2)
        {
            byte value = (byte)(96 + operationIndex);
            global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(8, 8, value);
            byte[] pixels = (byte[])image.Pixels.Clone();
            byte[] encoded = new global::jpeg.JpegWriter().Write(image);
            valid = global::jpeg.Jpeg.Read(encoded).Width == 8 && image.Pixels.SequenceEqual(pixels);
        }
        else if (service == 3)
        {
            valid = new global::jpeg.JpegInspectionService().ReadCoefficients(source).Width == 16;
        }
        else if (service == 4)
        {
            byte[] stripped = new global::jpeg.JpegMetadataEditor().StripMetadata(source);
            valid = global::jpeg.Jpeg.Identify(stripped).CommentCount == 0;
        }
        else
        {
            byte[] rotated = new global::jpeg.JpegTransformer().Rotate180(source);
            valid = global::jpeg.Jpeg.Identify(rotated).Width == 16;
        }

        if (!valid || !source.SequenceEqual(expected) || !sharedSource.SequenceEqual(expected))
        {
            return 0;
        }

        return 1;
    }

    public static void RunCancelableStreamService(
        string service,
        System.IO.Stream input,
        System.IO.Stream output,
        System.Threading.CancellationToken cancellationToken)
    {
        if (service == "identifier")
        {
            new global::jpeg.JpegIdentifierService().IdentifyStreamWithCancellation(input, cancellationToken);
            return;
        }
        if (service == "reader")
        {
            new global::jpeg.JpegReader().ReadStreamWithCancellation(input, cancellationToken);
            return;
        }
        if (service == "inspector")
        {
            new global::jpeg.JpegInspectionService().InspectMarkersStreamWithCancellation(input, cancellationToken);
            return;
        }
        if (service == "transformer")
        {
            new global::jpeg.JpegTransformer().Rotate90StreamWithOptionsAndCancellation(
                input,
                output,
                new global::jpeg.JpegTransformOptions(),
                cancellationToken);
            return;
        }

        throw new System.ArgumentOutOfRangeException(nameof(service));
    }

    public static System.IO.MemoryStream CreatePrefixedOutput()
    {
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        output.Write(new byte[] { 17, 19, 23 }, 0, 3);
        return output;
    }
}

internal class CancelAfterFirstReadStream : System.IO.Stream
{
    internal byte[] Data;
    internal System.Threading.CancellationTokenSource Cancellation;
    internal int Offset;
    internal int ReadCalls;
    internal int BytesRead;

    internal CancelAfterFirstReadStream(byte[] data, System.Threading.CancellationTokenSource cancellation)
    {
        Data = data;
        Cancellation = cancellation;
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { return Data.Length; }
    }

    public override long Position
    {
        get { return Offset; }
        set { throw new System.NotSupportedException(); }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCalls = ReadCalls + 1;
        if (Offset >= Data.Length)
        {
            return 0;
        }

        buffer[offset] = Data[Offset];
        Offset = Offset + 1;
        BytesRead = BytesRead + 1;
        Cancellation.Cancel();
        return 1;
    }

    public override long Seek(long offset, System.IO.SeekOrigin origin)
    {
        throw new System.NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new System.NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotSupportedException();
    }
}
