namespace jpeg.Tests;

internal class FastBoxChromaUpsamplerTests
{
    [Test]
    public void FastBoxUpsampler_ReplicatesTwoByTwoSamples()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        byte[] source = new byte[] { 10, 20, 30, 50 };

        byte[] destination = upsampler.Upsample(source, 2, 2, 2, 4, 4);

        Assert.That(
            destination,
            Is.EqualTo(new byte[]
            {
                10, 10, 20, 20,
                10, 10, 20, 20,
                30, 30, 50, 50,
                30, 30, 50, 50
            }));
    }

    [Test]
    public void FastBoxUpsampler_UpsamplesHorizontalOnly()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        byte[] source = new byte[] { 10, 20 };

        byte[] destination = upsampler.Upsample(source, 2, 1, 2, 4, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 10, 10, 20, 20 }));
    }

    [Test]
    public void FastBoxUpsampler_UpsamplesToOddOutputSize()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        byte[] source = new byte[] { 10, 20, 30, 50 };

        byte[] destination = upsampler.Upsample(source, 2, 2, 2, 3, 3);

        Assert.That(destination, Is.EqualTo(new byte[] { 10, 10, 20, 10, 10, 20, 30, 30, 50 }));
    }

    [Test]
    public void FastBoxUpsampler_UpsampleComponentPreservesMetadata()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        global::jpeg.JpegPlanarComponentBuffer source = CreateComponent();

        global::jpeg.JpegPlanarComponentBuffer destination = upsampler.UpsampleComponent(source, 4, 4);

        Assert.That(destination.ComponentIndex, Is.EqualTo(1));
        Assert.That(destination.Identifier, Is.EqualTo(2));
        Assert.That(destination.Width, Is.EqualTo(4));
        Assert.That(destination.Height, Is.EqualTo(4));
        Assert.That(destination.PaddedWidth, Is.EqualTo(4));
        Assert.That(destination.PaddedHeight, Is.EqualTo(4));
        Assert.That(destination.Stride, Is.EqualTo(4));
        Assert.That(destination.Samples.Length, Is.EqualTo(16));
        Assert.That(destination.Samples[0], Is.EqualTo(10));
        Assert.That(destination.Samples[15], Is.EqualTo(50));
    }

    [Test]
    public void FastBoxUpsampler_CopySupportsDestinationStride()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        byte[] source = new byte[] { 10, 20 };
        byte[] destination = new byte[] { 0, 0, 0, 0, 77, 88 };

        upsampler.UpsampleTo(source, 2, 1, 2, 4, 1, destination, 6);

        Assert.That(destination, Is.EqualTo(new byte[] { 10, 10, 20, 20, 77, 88 }));
    }

    [Test]
    public void FastBoxUpsampler_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        global::jpeg.JpegPlanarComponentBuffer source = new global::jpeg.JpegPlanarComponentBuffer();
        source.ComponentIndex = 1;
        source.Identifier = 2;
        source.Width = 4;
        source.Height = 0;
        source.PaddedWidth = 4;
        source.PaddedHeight = 0;
        source.Stride = 4;
        source.Samples = new byte[0];

        global::jpeg.JpegPlanarComponentBuffer destination = upsampler.UpsampleComponent(source, 8, 0);

        Assert.That(destination.Width, Is.EqualTo(8));
        Assert.That(destination.Height, Is.EqualTo(0));
        Assert.That(destination.Samples.Length, Is.EqualTo(0));
    }

    [Test]
    public void FastBoxUpsampler_ExposesMappingHelper()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        byte[] source = new byte[] { 10, 20, 30, 50 };

        Assert.That(upsampler.MapOutputToSourceIndex(0, 2, 4), Is.EqualTo(0));
        Assert.That(upsampler.MapOutputToSourceIndex(1, 2, 4), Is.EqualTo(0));
        Assert.That(upsampler.MapOutputToSourceIndex(2, 2, 4), Is.EqualTo(1));
        Assert.That(upsampler.MapOutputToSourceIndex(2, 2, 3), Is.EqualTo(1));
        Assert.That(upsampler.ReadBoxSample(source, 2, 2, 2, 2, 2, 4, 4), Is.EqualTo(50));
    }

    [Test]
    public void FastBoxUpsampler_RejectsInvalidInputs()
    {
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                upsampler.Upsample(null!, 2, 2, 2, 4, 4);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                upsampler.Upsample(new byte[4], 2, 2, 1, 4, 4);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                upsampler.Upsample(new byte[4], 2, 2, 2, 1, 4);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                upsampler.Upsample(new byte[1], 2, 2, 2, 4, 4);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                upsampler.UpsampleTo(new byte[4], 2, 2, 2, 4, 4, new byte[15], 4);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                upsampler.ReadBoxSample(new byte[4], 2, 2, 2, -1, 0, 4, 4);
            });
    }

    public static global::jpeg.JpegPlanarComponentBuffer CreateComponent()
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.ComponentIndex = 1;
        component.Identifier = 2;
        component.Width = 2;
        component.Height = 2;
        component.PaddedWidth = 2;
        component.PaddedHeight = 2;
        component.Stride = 2;
        component.Samples = new byte[] { 10, 20, 30, 50 };
        return component;
    }
}
