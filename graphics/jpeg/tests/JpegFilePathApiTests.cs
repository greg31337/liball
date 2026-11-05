namespace jpeg.Tests;

internal class JpegFilePathApiTests
{
    [Test]
    public void IdentifyFile_AndReadFile_ReadJpegBytesFromPath()
    {
        string path = CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, JpegReadApiTests.CreateReadableGrayscaleJpeg(2));

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.IdentifyFile(path);
        global::jpeg.JpegImage image = global::jpeg.Jpeg.ReadFile(path);

        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.Pixels[0], Is.EqualTo(138));

        System.IO.File.Delete(path);
    }

    [Test]
    public void WriteFile_WritesJpegImageToPathWithOptions()
    {
        string path = CreateTempJpegPath();
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.RestartInterval = 1;

        global::jpeg.Jpeg.WriteFile(path, image, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.IdentifyFile(path);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.ReadFile(path);

        Assert.That(System.IO.File.Exists(path), Is.True);
        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoded.Width, Is.EqualTo(16));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[8], Is.EqualTo(143));

        System.IO.File.Delete(path);
    }

    [Test]
    public void FileApis_PassReadOptionsToUnderlyingRead()
    {
        string path = CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, JpegReadApiTests.CreateReadableGrayscaleJpeg(2));
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxEntropyBytes = 0;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.Jpeg.ReadFile(path, options);
            });

        System.IO.File.Delete(path);
    }

    [Test]
    public void FileApis_RejectInvalidPaths()
    {
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.IdentifyFile(null!);
            });
        Assert.Throws<System.ArgumentException>(
            delegate
            {
                global::jpeg.Jpeg.ReadFile("");
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.WriteFile(null!, JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128));
            });
    }

    [Test]
    public void WriteFile_RejectsNullOptions()
    {
        string path = CreateTempJpegPath();
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.WriteFile(path, image, null!);
            });
    }

    public static string CreateTempJpegPath()
    {
        return System.IO.Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "jpeg-file-api-" + System.Guid.NewGuid().ToString("N") + ".jpg");
    }
}
