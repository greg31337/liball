namespace jpeg.Tests;

internal class FancyChromaUpsamplerTests
{
    [Test]
    public void FancyUpsampler_UpsamplesTwoByTwoWithBilinearFiltering()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
        byte[] source = new byte[] { 10, 20, 30, 50 };

        byte[] destination = upsampler.Upsample(source, 2, 2, 2, 4, 4);

        Assert.That(
            destination,
            Is.EqualTo(new byte[]
            {
                10, 13, 18, 20,
                15, 19, 25, 28,
                25, 30, 38, 43,
                30, 35, 45, 50
            }));
    }

    [Test]
    public void FancyUpsampler_UpsamplesHorizontalOnly()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
        byte[] source = new byte[] { 10, 20 };

        byte[] destination = upsampler.Upsample(source, 2, 1, 2, 4, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 10, 13, 18, 20 }));
    }

    [Test]
    public void FancyUpsampler_UpsamplesToOddOutputSize()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
        byte[] source = new byte[] { 10, 20, 30, 50 };

        byte[] destination = upsampler.Upsample(source, 2, 2, 2, 3, 3);

        Assert.That(destination, Is.EqualTo(new byte[] { 10, 15, 20, 20, 28, 35, 30, 40, 50 }));
    }

    [Test]
    public void FancyUpsampler_UpsampleComponentPreservesMetadata()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
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
    public void FancyUpsampler_CopySupportsDestinationStride()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
        byte[] source = new byte[] { 10, 20 };
        byte[] destination = new byte[] { 0, 0, 0, 0, 77, 88 };

        upsampler.UpsampleTo(source, 2, 1, 2, 4, 1, destination, 6);

        Assert.That(destination, Is.EqualTo(new byte[] { 10, 13, 18, 20, 77, 88 }));
    }

    [Test]
    public void FancyUpsampler_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
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
    public void FancyUpsampler_ExposesMappingAndBlendingHelpers()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();
        byte[] source = new byte[] { 10, 20, 30, 50 };

        Assert.That(upsampler.MapOutputToSourceFixed(0, 2, 4), Is.EqualTo(0));
        Assert.That(upsampler.MapOutputToSourceFixed(1, 2, 4), Is.EqualTo(64));
        Assert.That(upsampler.MapOutputToSourceFixed(3, 2, 4), Is.EqualTo(256));
        Assert.That(upsampler.Blend(10, 20, 128), Is.EqualTo(15));
        Assert.That(upsampler.InterpolateSample(source, 2, 2, 2, 1, 1, 4, 4), Is.EqualTo(19));
    }

    [Test]
    public void FancyUpsampler_RejectsInvalidInputs()
    {
        global::jpeg.JpegFancyChromaUpsampler upsampler = new global::jpeg.JpegFancyChromaUpsampler();

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
                upsampler.InterpolateSample(new byte[4], 2, 2, 2, -1, 0, 4, 4);
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
