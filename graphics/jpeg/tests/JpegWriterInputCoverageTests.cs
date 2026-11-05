namespace jpeg.Tests;

internal class JpegWriterInputCoverageTests
{
    [TestCase(global::jpeg.JpegProcess.BaselineDct)]
    [TestCase(global::jpeg.JpegProcess.ProgressiveDct)]
    public void Writer_EncodesEightBitPlanarComponentInput(global::jpeg.JpegProcess process)
    {
        global::jpeg.JpegImage source = CreatePlanarYcbcrImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = process;
        options.Quality = 100;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.RawComponentOutput = true;
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data, readOptions);

        Assert.That(info.Process, Is.EqualTo(process));
        Assert.That(decoded.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Planar));
        Assert.That(decoded.PlanarBuffer.Components.Count, Is.EqualTo(3));
        Assert.That(decoded.PlanarBuffer.Components[0].Samples[0], Is.InRange(89, 91));
        Assert.That(decoded.PlanarBuffer.Components[1].Samples[0], Is.InRange(119, 121));
        Assert.That(decoded.PlanarBuffer.Components[2].Samples[0], Is.InRange(139, 141));
    }

    [Test]
    public void Writer_UsesStrideForInterleavedRgbComponentInput()
    {
        int width = 8;
        int height = 16;
        int stride = 27;
        byte[] samples = new byte[stride * height];
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int index = y * stride + x * 3;
                if (y < 8)
                {
                    samples[index] = 255;
                }
                else
                {
                    samples[index + 2] = 255;
                }

                x = x + 1;
            }

            samples[y * stride + 24] = 17;
            samples[y * stride + 25] = 231;
            samples[y * stride + 26] = 49;
            y = y + 1;
        }

        global::jpeg.JpegImage source = new global::jpeg.JpegImageFactory().CreatePackedImageFromByteSamples(
            samples.AsSpan(),
            width,
            height,
            3,
            stride,
            global::jpeg.JpegPixelFormat.Rgb24,
            global::jpeg.JpegColorSpace.RGB);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Quality = 100;
        options.Subsampling = global::jpeg.JpegSubsampling.Ratio444;
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.Rgb24;

        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(
            new global::jpeg.JpegWriter().WriteWithOptions(source, options),
            readOptions);
        int lowerPixel = 8 * decoded.RowStride;

        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(decoded.Pixels[0], Is.GreaterThan(240));
        Assert.That(decoded.Pixels[1], Is.LessThan(15));
        Assert.That(decoded.Pixels[2], Is.LessThan(15));
        Assert.That(decoded.Pixels[lowerPixel], Is.LessThan(15));
        Assert.That(decoded.Pixels[lowerPixel + 1], Is.LessThan(15));
        Assert.That(decoded.Pixels[lowerPixel + 2], Is.GreaterThan(240));
    }

    [TestCase(global::jpeg.JpegColorSpace.CMYK, 0)]
    [TestCase(global::jpeg.JpegColorSpace.YCCK, 2)]
    public void Writer_EncodesRawFourComponentColorSpaces(global::jpeg.JpegColorSpace colorSpace, int adobeTransform)
    {
        int width = 8;
        int height = 8;
        byte[] samples = new byte[width * height * 4];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = 40;
            samples[index + 1] = 80;
            samples[index + 2] = 120;
            samples[index + 3] = 160;
            index = index + 4;
        }

        global::jpeg.JpegImage source = new global::jpeg.JpegImageFactory().CreatePackedImageFromByteSamples(
            samples.AsSpan(),
            width,
            height,
            4,
            width * 4,
            global::jpeg.JpegPixelFormat.RawComponents,
            colorSpace);
        source.Metadata.HasAdobe = true;
        source.Metadata.Adobe.IsPresent = true;
        source.Metadata.Adobe.Version = 100;
        source.Metadata.Adobe.Transform = adobeTransform;
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Quality = 100;
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.RawComponentOutput = true;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data, readOptions);

        Assert.That(info.ColorSpace, Is.EqualTo(colorSpace));
        Assert.That(info.ComponentCount, Is.EqualTo(4));
        Assert.That(decoded.PlanarBuffer.Components[0].Samples[0], Is.InRange(39, 41));
        Assert.That(decoded.PlanarBuffer.Components[1].Samples[0], Is.InRange(79, 81));
        Assert.That(decoded.PlanarBuffer.Components[2].Samples[0], Is.InRange(119, 121));
        Assert.That(decoded.PlanarBuffer.Components[3].Samples[0], Is.InRange(159, 161));
    }

    [Test]
    public void Writer_AppliesSelectedChromaDownsamplingFilter()
    {
        global::jpeg.JpegImage source = CreateAlternatingChromaImage();
        global::jpeg.JpegWriteOptions boxOptions = new global::jpeg.JpegWriteOptions();
        boxOptions.Quality = 100;
        boxOptions.Subsampling = global::jpeg.JpegSubsampling.Ratio422;
        boxOptions.ChromaDownsamplingFilter = global::jpeg.JpegChromaDownsamplingFilter.Box;
        global::jpeg.JpegWriteOptions centerOptions = new global::jpeg.JpegWriteOptions();
        centerOptions.Quality = 100;
        centerOptions.Subsampling = global::jpeg.JpegSubsampling.Ratio422;
        centerOptions.ChromaDownsamplingFilter = global::jpeg.JpegChromaDownsamplingFilter.Center;
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.RawComponentOutput = true;
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();

        global::jpeg.JpegImage box = global::jpeg.Jpeg.Read(writer.WriteWithOptions(source, boxOptions), readOptions);
        global::jpeg.JpegImage center = global::jpeg.Jpeg.Read(writer.WriteWithOptions(source, centerOptions), readOptions);
        int boxChroma = box.PlanarBuffer.Components[1].Samples[0];
        int centerChroma = center.PlanarBuffer.Components[1].Samples[0];

        Assert.That(boxChroma, Is.InRange(99, 101));
        Assert.That(centerChroma, Is.InRange(199, 201));
    }

    [Test]
    public void Writer_AppliesIndependentLuminanceAndChrominanceQuality()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Quality = 50;
        options.LuminanceQuality = 100;
        options.ChrominanceQuality = 10;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(CreateAlternatingChromaImage(), options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.QuantizationTables.Count, Is.EqualTo(2));
        Assert.That(info.QuantizationTables[0].Values[0], Is.EqualTo(1));
        Assert.That(info.QuantizationTables[1].Values[0], Is.GreaterThan(50));
        Assert.That(info.QuantizationTables[0].Values, Is.Not.EqualTo(info.QuantizationTables[1].Values));
    }

    internal static global::jpeg.JpegImage CreatePlanarYcbcrImage()
    {
        int width = 8;
        int height = 8;
        int stride = 10;
        global::jpeg.JpegPlanarImageBuffer layout = new global::jpeg.JpegPlanarImageBuffer();
        layout.Width = width;
        layout.Height = height;
        layout.Precision = 8;
        layout.ComponentCount = 3;
        layout.ColorSpace = global::jpeg.JpegColorSpace.YCbCr;
        layout.Subsampling = global::jpeg.JpegSubsampling.Ratio444;
        int componentIndex = 0;
        while (componentIndex < 3)
        {
            global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
            component.ComponentIndex = componentIndex;
            component.Identifier = componentIndex + 1;
            component.Precision = 8;
            component.HorizontalSamplingFactor = 1;
            component.VerticalSamplingFactor = 1;
            component.Width = width;
            component.Height = height;
            component.PaddedWidth = width;
            component.PaddedHeight = height;
            component.Stride = stride;
            layout.Components.Add(component);
            componentIndex = componentIndex + 1;
        }

        byte[] samples = new byte[stride * height * 3];
        FillPlane(samples, 0, stride, width, height, 90);
        FillPlane(samples, stride * height, stride, width, height, 120);
        FillPlane(samples, stride * height * 2, stride, width, height, 140);
        return new global::jpeg.JpegImageFactory().CreatePlanarImageFromByteSamples(samples.AsSpan(), layout);
    }

    internal static void FillPlane(byte[] samples, int offset, int stride, int width, int height, int value)
    {
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < stride)
            {
                samples[offset + y * stride + x] = 251;
                if (x < width)
                {
                    samples[offset + y * stride + x] = (byte)value;
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal static global::jpeg.JpegImage CreateAlternatingChromaImage()
    {
        int width = 16;
        int height = 8;
        global::jpeg.JpegImage image = JpegEncoderComponentOptionsTests.CreateYcbcrImage(width, height, 128, 0, 128);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                image.Pixels[(y * width + x) * 3 + 1] = (byte)((x % 2) * 200);
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }
}
