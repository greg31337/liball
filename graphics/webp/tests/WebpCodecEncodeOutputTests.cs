using webp;

namespace webp.Tests;

[TestFixture]
public class WebpCodecEncodeOutputTests
{
    [Test]
    public void EncodeStreamWritesAtCurrentPositionAndLeavesCallerStreamOpen()
    {
        WebpCodec codec = new WebpCodec();
        IWebpEncoder encoder = codec;
        WebpPixelBuffer buffer = NewBuffer(2, 1, 10, 20, 30, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        byte[] expected = encoder.Encode(buffer, options);
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xAA);

        encoder.EncodeStream(buffer, stream, options);

        byte[] written = stream.ToArray();
        Assert.That(written[0], Is.EqualTo(0xAA));
        Assert.That(CopyBytes(written, 1, written.Length - 1), Is.EqualTo(expected));
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
        Assert.That(stream.CanWrite, Is.True);
    }

    [Test]
    public void EncodeImageStreamWritesAnimationAndLeavesCallerStreamOpen()
    {
        WebpCodec codec = new WebpCodec();
        IWebpEncoder encoder = codec;
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xBB);

        encoder.EncodeImageStream(NewAnimation(), stream, options);

        byte[] written = stream.ToArray();
        byte[] encoded = CopyBytes(written, 1, written.Length - 1);
        WebpDecodeResult decoded = codec.Decode(encoded, new WebpDecodeOptions());
        Assert.That(written[0], Is.EqualTo(0xBB));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.HasAnimation, Is.True);
        Assert.That(decoded.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
        Assert.That(stream.CanWrite, Is.True);
    }

    [Test]
    public void EncodeFileAndEncodeImageFileReplaceExistingContentsAndUseOptions()
    {
        WebpCodec codec = new WebpCodec();
        IWebpEncoder encoder = codec;
        string stillPath = NewTempPath();
        string animationPath = NewTempPath();
        File.WriteAllBytes(stillPath, new byte[512]);
        File.WriteAllBytes(animationPath, new byte[512]);
        WebpEncodeOptions lossyOptions = new WebpEncodeOptions();
        lossyOptions.Mode = WebpEncodeMode.Lossy;
        WebpEncodeOptions animationOptions = new WebpEncodeOptions();
        animationOptions.Mode = WebpEncodeMode.Lossless;

        try
        {
            encoder.EncodeFile(NewBuffer(1, 1, 1, 2, 3, 255), stillPath, lossyOptions);
            encoder.EncodeImageFile(NewAnimation(), animationPath, animationOptions);

            WebpDecodeResult still = codec.DecodeFile(stillPath, new WebpDecodeOptions());
            WebpDecodeResult animation = codec.DecodeFile(animationPath, new WebpDecodeOptions());
            Assert.That(still.IsValid, Is.True);
            Assert.That(still.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
            Assert.That(animation.IsValid, Is.True);
            Assert.That(animation.Image.HasAnimation, Is.True);
            Assert.That(animation.Image.Frames, Has.Count.EqualTo(2));
            Assert.That(new FileInfo(stillPath).Length, Is.LessThan(512));
            Assert.That(new FileInfo(animationPath).Length, Is.LessThan(512));
        }
        finally
        {
            File.Delete(stillPath);
            File.Delete(animationPath);
        }
    }

    [Test]
    public void StreamAndFileEncodersEnforceOutputLimitsBeforeWriting()
    {
        WebpCodec codec = new WebpCodec();
        IWebpEncoder encoder = codec;
        WebpPixelBuffer buffer = NewBuffer(1, 1, 1, 2, 3, 255);
        WebpImage image = NewAnimation();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        options.Limits.MaxRiffSize = 1;
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xCC);
        string path = NewTempPath();
        byte[] originalFile = new byte[] { 9, 8, 7 };
        File.WriteAllBytes(path, originalFile);

        try
        {
            Assert.Throws<ArgumentException>(() => encoder.EncodeStream(buffer, stream, options));
            Assert.Throws<ArgumentException>(() => encoder.EncodeImageFile(image, path, options));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xCC }));
            Assert.That(stream.CanWrite, Is.True);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(originalFile));
        }
        finally
        {
            File.Delete(path);
        }
    }

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "webp-output-" + Guid.NewGuid().ToString("N") + ".webp");
    }

    static byte[] CopyBytes(byte[] source, int offset, int length)
    {
        byte[] output = new byte[length];
        Array.Copy(source, offset, output, 0, length);
        return output;
    }

    static WebpImage NewAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 3;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Frames.Add(NewFrame(0, 25, NewBuffer(1, 1, 255, 0, 0, 255)));
        image.Frames.Add(NewFrame(2, 40, NewBuffer(1, 1, 0, 0, 255, 255)));
        return image;
    }

    static WebpFrame NewFrame(int x, int duration, WebpPixelBuffer pixels)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = 0;
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.Pixels = pixels;
        return frame;
    }

    static WebpPixelBuffer NewBuffer(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int offset = 0;
        while (offset < buffer.Rgba.Length)
        {
            buffer.Rgba[offset] = red;
            buffer.Rgba[offset + 1] = green;
            buffer.Rgba[offset + 2] = blue;
            buffer.Rgba[offset + 3] = alpha;
            offset = offset + 4;
        }

        return buffer;
    }
}
