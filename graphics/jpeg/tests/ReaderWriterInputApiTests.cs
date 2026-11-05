namespace jpeg.Tests;

internal class ReaderWriterInputApiTests
{
    [Test]
    public void ReaderContract_DecodesSpansWithDefaultsAndOptions()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.OutputPixelFormat = global::jpeg.JpegPixelFormat.L8;

        global::jpeg.JpegImage defaultImage = reader.ReadSpan(data.AsSpan());
        global::jpeg.JpegImage optionsImage = reader.ReadSpanWithOptions(data.AsSpan(), options);

        Assert.That(defaultImage.Width, Is.EqualTo(8));
        Assert.That(defaultImage.Height, Is.EqualTo(8));
        Assert.That(defaultImage.Pixels[0], Is.EqualTo(138));
        Assert.That(optionsImage.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(optionsImage.Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void WriterContract_EncodesIntoSpansAndValidatesCapacityBeforeCopying()
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
        byte[] expected = writer.Write(image);
        byte[] destination = new byte[expected.Length + 7];
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.RestartInterval = 1;
        byte[] optionsExpected = writer.WriteWithOptions(image, options);
        byte[] optionsDestination = new byte[optionsExpected.Length];

        int written = writer.WriteToSpan(image, destination.AsSpan());
        int optionsWritten = writer.WriteToSpanWithOptions(image, optionsDestination.AsSpan(), options);

        Assert.That(written, Is.EqualTo(expected.Length));
        Assert.That(destination.AsSpan(0, written).ToArray(), Is.EqualTo(expected));
        Assert.That(optionsWritten, Is.EqualTo(optionsExpected.Length));
        Assert.That(optionsDestination, Is.EqualTo(optionsExpected));
        Assert.That(global::jpeg.Jpeg.Identify(optionsDestination).RestartMarkerCount, Is.EqualTo(1));

        byte[] tooSmall = new byte[expected.Length - 1];
        System.Array.Fill(tooSmall, (byte)91);
        Assert.Throws<System.ArgumentException>(
            delegate { writer.WriteToSpan(image, tooSmall.AsSpan()); });
        Assert.That(tooSmall, Is.All.EqualTo(91));
    }

    [Test]
    public void ReaderWriterContracts_ReadAndWriteFilesWithDefaultsAndOptions()
    {
        string defaultPath = JpegFilePathApiTests.CreateTempJpegPath();
        string optionsPath = JpegFilePathApiTests.CreateTempJpegPath();
        try
        {
            global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
            global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();
            global::jpeg.JpegImage image = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
            global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
            writeOptions.RestartInterval = 1;
            global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
            readOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.L8;

            writer.WriteFile(image, defaultPath);
            writer.WriteFileWithOptions(image, optionsPath, writeOptions);
            global::jpeg.JpegImage defaultImage = reader.ReadFile(defaultPath);
            global::jpeg.JpegImage optionsImage = reader.ReadFileWithOptions(optionsPath, readOptions);

            Assert.That(defaultImage.Width, Is.EqualTo(16));
            Assert.That(defaultImage.Pixels[0], Is.EqualTo(138));
            Assert.That(optionsImage.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
            Assert.That(optionsImage.Pixels[8], Is.EqualTo(143));
            Assert.That(global::jpeg.Jpeg.IdentifyFile(optionsPath).RestartMarkerCount, Is.EqualTo(1));
        }
        finally
        {
            System.IO.File.Delete(defaultPath);
            System.IO.File.Delete(optionsPath);
        }
    }

    [Test]
    public void ReaderContract_ReadsNonSeekableStreamFromCurrentPositionAndLeavesItOpen()
    {
        byte[] jpegData = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        byte[] prefixedData = new byte[jpegData.Length + 2];
        prefixedData[0] = 41;
        prefixedData[1] = 43;
        System.Array.Copy(jpegData, 0, prefixedData, 2, jpegData.Length);
        NonSeekableReadStream stream = new NonSeekableReadStream(prefixedData);
        Assert.That(stream.ReadByte(), Is.EqualTo(41));
        Assert.That(stream.ReadByte(), Is.EqualTo(43));
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegImage image = reader.ReadStreamWithOptionsAndCancellation(
            stream,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Pixels[0], Is.EqualTo(138));
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.CanSeek, Is.False);
    }

    [Test]
    public void WriterContract_WritesNonSeekableStreamAtCurrentPositionAndLeavesItOpen()
    {
        NonSeekableWriteStream stream = new NonSeekableWriteStream(new byte[] { 47, 53 });
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);

        writer.WriteToStreamWithOptionsAndCancellation(
            source,
            stream,
            new global::jpeg.JpegWriteOptions(),
            default(System.Threading.CancellationToken));
        stream.WriteByte(59);
        byte[] result = stream.ToArray();
        byte[] jpegData = new byte[result.Length - 3];
        System.Array.Copy(result, 2, jpegData, 0, jpegData.Length);

        Assert.That(result[0], Is.EqualTo(47));
        Assert.That(result[1], Is.EqualTo(53));
        Assert.That(result[result.Length - 1], Is.EqualTo(59));
        Assert.That(global::jpeg.Jpeg.Read(jpegData).Pixels[0], Is.EqualTo(138));
        Assert.That(stream.CanWrite, Is.True);
        Assert.That(stream.CanSeek, Is.False);
    }

    [Test]
    public void ReaderWriterContracts_CancellableVariantsCompleteWhenTokenAllowsWork()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        System.Threading.CancellationToken token = default(System.Threading.CancellationToken);
        byte[] data = writer.WriteWithCancellation(source, token);
        byte[] destination = new byte[data.Length];

        Assert.That(writer.WriteWithOptionsAndCancellation(source, writeOptions, token), Is.EqualTo(data));
        Assert.That(writer.WriteToSpanWithCancellation(source, destination.AsSpan(), token), Is.EqualTo(data.Length));
        Assert.That(writer.WriteToSpanWithOptionsAndCancellation(source, destination.AsSpan(), writeOptions, token), Is.EqualTo(data.Length));
        Assert.That(reader.ReadWithCancellation(data, token).Pixels[0], Is.EqualTo(138));
        Assert.That(reader.ReadWithOptionsAndCancellation(data, readOptions, token).Pixels[0], Is.EqualTo(138));
        Assert.That(reader.ReadSpanWithCancellation(data.AsSpan(), token).Pixels[0], Is.EqualTo(138));
        Assert.That(reader.ReadSpanWithOptionsAndCancellation(data.AsSpan(), readOptions, token).Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void ReaderWriterContracts_PreCanceledVariantsDoNotModifyDestinations()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        byte[] data = global::jpeg.Jpeg.Write(source);
        global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        System.Threading.CancellationToken token = new System.Threading.CancellationToken(true);
        byte[] destination = new byte[data.Length];
        System.Array.Fill(destination, (byte)67);
        System.IO.MemoryStream output = new System.IO.MemoryStream();

        Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadWithCancellation(data, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadWithOptionsAndCancellation(data, readOptions, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadSpanWithCancellation(data.AsSpan(), token); });
        Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadSpanWithOptionsAndCancellation(data.AsSpan(), readOptions, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadStreamWithCancellation(new System.IO.MemoryStream(data), token); });
        Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadStreamWithOptionsAndCancellation(new System.IO.MemoryStream(data), readOptions, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteWithCancellation(source, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteWithOptionsAndCancellation(source, writeOptions, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteToSpanWithCancellation(source, destination.AsSpan(), token); });
        Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteToSpanWithOptionsAndCancellation(source, destination.AsSpan(), writeOptions, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteToStreamWithCancellation(source, output, token); });
        Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteToStreamWithOptionsAndCancellation(source, output, writeOptions, token); });

        Assert.That(destination, Is.All.EqualTo(67));
        Assert.That(output.Length, Is.EqualTo(0));
    }

    [Test]
    public void ReaderWriterContracts_CancellableFileVariantsRespectCancellation()
    {
        string sourcePath = JpegFilePathApiTests.CreateTempJpegPath();
        string destinationPath = JpegFilePathApiTests.CreateTempJpegPath();
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        System.IO.File.WriteAllBytes(sourcePath, global::jpeg.Jpeg.Write(source));
        try
        {
            global::jpeg.IJpegWriter writer = new global::jpeg.JpegWriter();
            global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();
            System.Threading.CancellationToken canceled = new System.Threading.CancellationToken(true);

            Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadFileWithCancellation(sourcePath, canceled); });
            Assert.Throws<System.OperationCanceledException>(delegate { reader.ReadFileWithOptionsAndCancellation(sourcePath, new global::jpeg.JpegReadOptions(), canceled); });
            Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteFileWithCancellation(source, destinationPath, canceled); });
            Assert.Throws<System.OperationCanceledException>(delegate { writer.WriteFileWithOptionsAndCancellation(source, destinationPath, new global::jpeg.JpegWriteOptions(), canceled); });
            Assert.That(System.IO.File.Exists(destinationPath), Is.False);

            writer.WriteFileWithOptionsAndCancellation(
                source,
                destinationPath,
                new global::jpeg.JpegWriteOptions(),
                default(System.Threading.CancellationToken));
            Assert.That(reader.ReadFileWithOptionsAndCancellation(
                destinationPath,
                new global::jpeg.JpegReadOptions(),
                default(System.Threading.CancellationToken)).Pixels[0], Is.EqualTo(138));
        }
        finally
        {
            System.IO.File.Delete(sourcePath);
            System.IO.File.Delete(destinationPath);
        }
    }
}

internal class NonSeekableWriteStream : System.IO.Stream
{
    internal System.IO.MemoryStream Destination;

    internal NonSeekableWriteStream(byte[] prefix)
    {
        Destination = new System.IO.MemoryStream();
        Destination.Write(prefix, 0, prefix.Length);
    }

    public override bool CanRead
    {
        get { return false; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return true; }
    }

    public override long Length
    {
        get { return Destination.Length; }
    }

    public override long Position
    {
        get { throw new System.NotSupportedException(); }
        set { throw new System.NotSupportedException(); }
    }

    public override void Flush()
    {
        Destination.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotSupportedException();
    }

    public override long Seek(long offset, System.IO.SeekOrigin origin)
    {
        throw new System.NotSupportedException();
    }

    public override void SetLength(long value)
    {
        Destination.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Destination.Write(buffer, offset, count);
    }

    internal byte[] ToArray()
    {
        return Destination.ToArray();
    }
}
