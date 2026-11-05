namespace jpeg.Tests;

internal class ChromaDownsamplerTests
{
    [Test]
    public void Downsampler_BoxFilterAveragesSourceRegions()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        byte[] source = CreateFourByFourSource();

        byte[] destination = downsampler.Downsample(source, 4, 4, 4, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);

        Assert.That(destination, Is.EqualTo(new byte[] { 35, 55, 115, 135 }));
    }

    [Test]
    public void Downsampler_CenterFilterSamplesSourceRegionCenters()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        byte[] source = CreateFourByFourSource();

        byte[] destination = downsampler.Downsample(source, 4, 4, 4, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Center);

        Assert.That(destination, Is.EqualTo(new byte[] { 60, 80, 140, 160 }));
    }

    [Test]
    public void Downsampler_HandlesOddSourceRegions()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        byte[] source = new byte[] { 0, 10, 20, 30, 40 };

        byte[] box = downsampler.Downsample(source, 5, 1, 5, 2, 1, global::jpeg.JpegChromaDownsamplingFilter.Box);
        byte[] center = downsampler.Downsample(source, 5, 1, 5, 2, 1, global::jpeg.JpegChromaDownsamplingFilter.Center);

        Assert.That(box, Is.EqualTo(new byte[] { 5, 30 }));
        Assert.That(center, Is.EqualTo(new byte[] { 10, 30 }));
    }

    [Test]
    public void Downsampler_DownsampleComponentPreservesMetadata()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        global::jpeg.JpegPlanarComponentBuffer source = CreateComponent();

        global::jpeg.JpegPlanarComponentBuffer destination = downsampler.DownsampleComponent(source, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);

        Assert.That(destination.ComponentIndex, Is.EqualTo(1));
        Assert.That(destination.Identifier, Is.EqualTo(2));
        Assert.That(destination.Width, Is.EqualTo(2));
        Assert.That(destination.Height, Is.EqualTo(2));
        Assert.That(destination.PaddedWidth, Is.EqualTo(2));
        Assert.That(destination.PaddedHeight, Is.EqualTo(2));
        Assert.That(destination.Stride, Is.EqualTo(2));
        Assert.That(destination.Samples, Is.EqualTo(new byte[] { 35, 55, 115, 135 }));
    }

    [Test]
    public void Downsampler_CopySupportsSourceAndDestinationStrides()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        byte[] source = new byte[]
        {
            10, 20, 30, 40, 99,
            50, 60, 70, 80, 98
        };
        byte[] destination = new byte[] { 0, 0, 77, 88 };

        downsampler.DownsampleTo(source, 4, 2, 5, 2, 1, destination, 4, global::jpeg.JpegChromaDownsamplingFilter.Box);

        Assert.That(destination, Is.EqualTo(new byte[] { 35, 55, 77, 88 }));
    }

    [Test]
    public void Downsampler_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        global::jpeg.JpegPlanarComponentBuffer source = new global::jpeg.JpegPlanarComponentBuffer();
        source.ComponentIndex = 1;
        source.Identifier = 2;
        source.Width = 4;
        source.Height = 0;
        source.PaddedWidth = 4;
        source.PaddedHeight = 0;
        source.Stride = 4;
        source.Samples = new byte[0];

        global::jpeg.JpegPlanarComponentBuffer destination = downsampler.DownsampleComponent(source, 2, 0, global::jpeg.JpegChromaDownsamplingFilter.Box);

        Assert.That(destination.Width, Is.EqualTo(2));
        Assert.That(destination.Height, Is.EqualTo(0));
        Assert.That(destination.Samples.Length, Is.EqualTo(0));
    }

    [Test]
    public void Downsampler_ExposesMappingHelpers()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        byte[] source = CreateFourByFourSource();

        Assert.That(downsampler.MapOutputStartToSource(1, 5, 2), Is.EqualTo(2));
        Assert.That(downsampler.MapOutputEndToSource(1, 5, 2), Is.EqualTo(5));
        Assert.That(downsampler.MapOutputCenterToSource(0, 4, 2), Is.EqualTo(1));
        Assert.That(downsampler.ReadDownsampledSample(source, 4, 4, 4, 1, 1, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Center), Is.EqualTo(160));
    }

    [Test]
    public void Downsampler_RejectsInvalidInputs()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                downsampler.Downsample(null!, 4, 4, 4, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                downsampler.Downsample(new byte[16], 4, 4, 3, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                downsampler.Downsample(new byte[16], 4, 4, 4, 5, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                downsampler.Downsample(new byte[4], 4, 4, 4, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                downsampler.DownsampleTo(new byte[16], 4, 4, 4, 2, 2, new byte[3], 2, global::jpeg.JpegChromaDownsamplingFilter.Box);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                downsampler.Downsample(new byte[16], 4, 4, 4, 2, 2, (global::jpeg.JpegChromaDownsamplingFilter)99);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                downsampler.ReadDownsampledSample(new byte[16], 4, 4, 4, -1, 0, 2, 2, global::jpeg.JpegChromaDownsamplingFilter.Box);
            });
    }

    public static byte[] CreateFourByFourSource()
    {
        return new byte[]
        {
            10, 20, 30, 40,
            50, 60, 70, 80,
            90, 100, 110, 120,
            130, 140, 150, 160
        };
    }

    public static global::jpeg.JpegPlanarComponentBuffer CreateComponent()
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.ComponentIndex = 1;
        component.Identifier = 2;
        component.Width = 4;
        component.Height = 4;
        component.PaddedWidth = 4;
        component.PaddedHeight = 4;
        component.Stride = 4;
        component.Samples = CreateFourByFourSource();
        return component;
    }
}
