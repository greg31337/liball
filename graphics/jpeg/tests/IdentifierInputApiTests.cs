namespace jpeg.Tests;

internal class IdentifierInputApiTests
{
    [Test]
    public void IdentifierContract_IdentifiesReadOnlySpansWithDefaultsAndOptions()
    {
        byte[] data = JpegIdentifyApiTests.CreateJfifBaselineJpeg();
        global::jpeg.IJpegIdentifier identifier = new global::jpeg.JpegIdentifierService();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.SkipMetadata = true;

        global::jpeg.JpegInfo defaultInfo = identifier.IdentifySpan(data.AsSpan());
        global::jpeg.JpegInfo optionsInfo = identifier.IdentifySpanWithOptions(data.AsSpan(), options);

        Assert.That(defaultInfo.Width, Is.EqualTo(32));
        Assert.That(defaultInfo.Metadata.HasJfif, Is.True);
        Assert.That(optionsInfo.Width, Is.EqualTo(32));
        Assert.That(optionsInfo.Metadata.HasJfif, Is.False);
        Assert.That(optionsInfo.MetadataBytes, Is.EqualTo(0));
    }

    [Test]
    public void IdentifierContract_IdentifiesFilesWithDefaultsAndOptions()
    {
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, JpegIdentifyApiTests.CreateJfifBaselineJpeg());
        try
        {
            global::jpeg.IJpegIdentifier identifier = new global::jpeg.JpegIdentifierService();
            global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
            options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;

            global::jpeg.JpegInfo defaultInfo = identifier.IdentifyFile(path);
            global::jpeg.JpegInfo optionsInfo = identifier.IdentifyFileWithOptions(path, options);

            Assert.That(defaultInfo.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.ClassicJpeg));
            Assert.That(defaultInfo.Metadata.HasJfif, Is.True);
            Assert.That(optionsInfo.Width, Is.EqualTo(32));
            Assert.That(optionsInfo.Metadata.HasJfif, Is.False);
            Assert.That(optionsInfo.Metadata.RawMarkers, Is.Empty);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Test]
    public void IdentifierContract_CancellationVariantsRejectPreCanceledOperations()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, data);
        try
        {
            global::jpeg.IJpegIdentifier identifier = new global::jpeg.JpegIdentifierService();
            global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
            System.Threading.CancellationToken token = new System.Threading.CancellationToken(true);

            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifyWithCancellation(data, token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifyWithOptionsAndCancellation(data, options, token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifySpanWithCancellation(data.AsSpan(), token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifySpanWithOptionsAndCancellation(data.AsSpan(), options, token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifyStreamWithCancellation(new System.IO.MemoryStream(data), token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifyStreamWithOptionsAndCancellation(new System.IO.MemoryStream(data), options, token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifyFileWithCancellation(path, token); });
            Assert.Throws<System.OperationCanceledException>(
                delegate { identifier.IdentifyFileWithOptionsAndCancellation(path, options, token); });
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Test]
    public void IdentifierContract_CancellableNonSeekableStreamStartsAtCurrentPositionAndStaysOpen()
    {
        byte[] jpegData = IdentifierTests.CreateMinimalBaselineJpeg();
        byte[] prefixedData = new byte[jpegData.Length + 2];
        prefixedData[0] = 17;
        prefixedData[1] = 23;
        System.Array.Copy(jpegData, 0, prefixedData, 2, jpegData.Length);
        NonSeekableReadStream stream = new NonSeekableReadStream(prefixedData);
        Assert.That(stream.ReadByte(), Is.EqualTo(17));
        Assert.That(stream.ReadByte(), Is.EqualTo(23));
        global::jpeg.IJpegIdentifier identifier = new global::jpeg.JpegIdentifierService();

        global::jpeg.JpegInfo info = identifier.IdentifyStreamWithOptionsAndCancellation(
            stream,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));

        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.CanSeek, Is.False);
    }

    [Test]
    public void IdentifierContract_CancellableVariantsCompleteWhenTokenAllowsWork()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, data);
        try
        {
            global::jpeg.IJpegIdentifier identifier = new global::jpeg.JpegIdentifierService();
            global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
            System.Threading.CancellationToken token = default(System.Threading.CancellationToken);

            Assert.That(identifier.IdentifyWithCancellation(data, token).Width, Is.EqualTo(32));
            Assert.That(identifier.IdentifyWithOptionsAndCancellation(data, options, token).Height, Is.EqualTo(16));
            Assert.That(identifier.IdentifySpanWithCancellation(data.AsSpan(), token).Width, Is.EqualTo(32));
            Assert.That(identifier.IdentifySpanWithOptionsAndCancellation(data.AsSpan(), options, token).Height, Is.EqualTo(16));
            Assert.That(identifier.IdentifyStreamWithCancellation(new System.IO.MemoryStream(data), token).Width, Is.EqualTo(32));
            Assert.That(identifier.IdentifyStreamWithOptionsAndCancellation(new System.IO.MemoryStream(data), options, token).Height, Is.EqualTo(16));
            Assert.That(identifier.IdentifyFileWithCancellation(path, token).Width, Is.EqualTo(32));
            Assert.That(identifier.IdentifyFileWithOptionsAndCancellation(path, options, token).Height, Is.EqualTo(16));
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
