namespace jpeg.Tests;

internal class ExifOrientationTransformerTests
{
    [Test]
    public void Transformer_AppliesAllExifOrientationMappings()
    {
        global::jpeg.JpegExifOrientationTransformer transformer = new global::jpeg.JpegExifOrientationTransformer();

        AssertOrientation(transformer, 1, 3, 2, new byte[] { 1, 2, 3, 4, 5, 6 });
        AssertOrientation(transformer, 2, 3, 2, new byte[] { 3, 2, 1, 6, 5, 4 });
        AssertOrientation(transformer, 3, 3, 2, new byte[] { 6, 5, 4, 3, 2, 1 });
        AssertOrientation(transformer, 4, 3, 2, new byte[] { 4, 5, 6, 1, 2, 3 });
        AssertOrientation(transformer, 5, 2, 3, new byte[] { 1, 4, 2, 5, 3, 6 });
        AssertOrientation(transformer, 6, 2, 3, new byte[] { 4, 1, 5, 2, 6, 3 });
        AssertOrientation(transformer, 7, 2, 3, new byte[] { 6, 3, 5, 2, 4, 1 });
        AssertOrientation(transformer, 8, 2, 3, new byte[] { 3, 6, 2, 5, 1, 4 });
    }

    [Test]
    public void Transformer_UsesExifMetadataOrientation()
    {
        global::jpeg.JpegImage image = CreateL8Image();
        image.Metadata.ExifMetadata.HasOrientation = true;
        image.Metadata.ExifMetadata.Orientation = 6;
        global::jpeg.JpegExifOrientationTransformer transformer = new global::jpeg.JpegExifOrientationTransformer();

        global::jpeg.JpegImage transformed = transformer.Apply(image);

        Assert.That(transformed.Width, Is.EqualTo(2));
        Assert.That(transformed.Height, Is.EqualTo(3));
        Assert.That(transformed.Pixels, Is.EqualTo(new byte[] { 4, 1, 5, 2, 6, 3 }));
        Assert.That(image.Pixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(transformed.Metadata, Is.SameAs(image.Metadata));
    }

    [Test]
    public void Transformer_PreservesMultiBytePixels()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 1;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        image.Pixels = new byte[] { 10, 20, 30, 40, 50, 60 };
        global::jpeg.JpegExifOrientationTransformer transformer = new global::jpeg.JpegExifOrientationTransformer();

        global::jpeg.JpegImage transformed = transformer.ApplyOrientation(image, 2);

        Assert.That(transformed.Width, Is.EqualTo(2));
        Assert.That(transformed.Height, Is.EqualTo(1));
        Assert.That(transformed.Pixels, Is.EqualTo(new byte[] { 40, 50, 60, 10, 20, 30 }));
    }

    [Test]
    public void Transformer_RejectsInvalidOrientationInputs()
    {
        global::jpeg.JpegExifOrientationTransformer transformer = new global::jpeg.JpegExifOrientationTransformer();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                transformer.Apply(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                transformer.ApplyOrientation(CreateL8Image(), 9);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegImage image = CreateL8Image();
                image.Pixels = new byte[] { 1, 2 };
                transformer.ApplyOrientation(image, 1);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegImage image = CreateL8Image();
                image.PixelFormat = global::jpeg.JpegPixelFormat.Unknown;
                transformer.ApplyOrientation(image, 1);
            });
    }

    public static void AssertOrientation(global::jpeg.JpegExifOrientationTransformer transformer, int orientation, int width, int height, byte[] expected)
    {
        global::jpeg.JpegImage image = CreateL8Image();

        global::jpeg.JpegImage transformed = transformer.ApplyOrientation(image, orientation);

        Assert.That(transformed.Width, Is.EqualTo(width));
        Assert.That(transformed.Height, Is.EqualTo(height));
        Assert.That(transformed.Pixels, Is.EqualTo(expected));
    }

    public static global::jpeg.JpegImage CreateL8Image()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 3;
        image.Height = 2;
        image.Precision = 8;
        image.ComponentCount = 1;
        image.PixelFormat = global::jpeg.JpegPixelFormat.L8;
        image.ColorSpace = global::jpeg.JpegColorSpace.Grayscale;
        image.Pixels = new byte[] { 1, 2, 3, 4, 5, 6 };
        return image;
    }
}
