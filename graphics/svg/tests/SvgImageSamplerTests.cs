namespace svg.Tests;

public class SvgImageSamplerTests
{
    [Test]
    public void ImageRenderingHintSelectsNearestAndExplicitBilinearOverridesIt()
    {
        SvgPixelBuffer pixels = Surface(2, 1);
        Write(pixels, 0, 0, 255, 0, 0, 255);
        Write(pixels, 1, 0, 0, 0, 255, 255);
        SvgSceneNode image = Image(pixels, SvgRect.Create(0, 0, 4, 2), new SvgMatrix(2, 0, 0, 2, 0, 0));
        image.Image.Rendering = SvgImageRendering.Pixelated;
        SvgImageSampleOptions smooth = new SvgImageSampleOptions();
        smooth.SamplingMode = SvgImageSamplingMode.Bilinear;

        SvgImageSampleResult left = new SvgImageSampler().SampleWithNodeAndPoint(image, Point(0.9, 1));
        SvgImageSampleResult right = new SvgImageSampler().SampleWithNodeAndPoint(image, Point(3.1, 1));
        SvgImageSampleResult middle = new SvgImageSampler().SampleWithNodeAndPointAndOptions(image, Point(2, 1), smooth);

        AssertSample(left, true, 255, 0, 0, 255);
        AssertSample(right, true, 0, 0, 255, 255);
        AssertSample(middle, true, 128, 0, 128, 255);
    }

    [Test]
    public void BilinearFilteringInterpolatesPremultipliedAlphaBeforeOpacity()
    {
        SvgPixelBuffer pixels = Surface(2, 2);
        Write(pixels, 0, 0, 255, 0, 0, 255);
        Write(pixels, 1, 0, 0, 255, 0, 255);
        Write(pixels, 0, 1, 0, 0, 255, 255);
        Write(pixels, 1, 1, 0, 0, 0, 0);
        SvgSceneNode image = Image(pixels, SvgRect.Create(0, 0, 2, 2), Identity());
        SvgImageSampleOptions bilinear = new SvgImageSampleOptions();
        bilinear.SamplingMode = SvgImageSamplingMode.Bilinear;
        bilinear.Opacity = 0.5;
        SvgImageSampleOptions nearest = new SvgImageSampleOptions();
        nearest.SamplingMode = SvgImageSamplingMode.Nearest;

        SvgImageSampleResult blended = new SvgImageSampler().SampleWithNodeAndPointAndOptions(image, Point(1, 1), bilinear);
        SvgImageSampleResult transparent = new SvgImageSampler().SampleWithNodeAndPointAndOptions(image, Point(1, 1), nearest);

        AssertSample(blended, true, 32, 32, 32, 96);
        AssertSample(transparent, true, 0, 0, 0, 0);
    }

    [Test]
    public void PreserveAspectRatioMeetLetterboxesAndSliceCropsSource()
    {
        SvgPixelBuffer pixels = Surface(2, 1);
        Write(pixels, 0, 0, 255, 0, 0, 255);
        Write(pixels, 1, 0, 0, 0, 255, 255);
        SvgSceneNode meet = Image(pixels, SvgRect.Create(0, 0, 4, 4), new SvgMatrix(2, 0, 0, 2, 0, 1));
        SvgSceneNode slice = Image(pixels, SvgRect.Create(0, 0, 4, 4), new SvgMatrix(4, 0, 0, 4, -2, 0));
        slice.Image.AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Slice, false);
        SvgImageSampleOptions nearest = new SvgImageSampleOptions();
        nearest.SamplingMode = SvgImageSamplingMode.Nearest;

        SvgImageSampleResult letterbox = new SvgImageSampler().SampleWithNodeAndPointAndOptions(meet, Point(0.5, 0.5), nearest);
        SvgImageSampleResult meetPixel = new SvgImageSampler().SampleWithNodeAndPointAndOptions(meet, Point(0.5, 1.5), nearest);
        SvgImageSampleResult sliceLeft = new SvgImageSampler().SampleWithNodeAndPointAndOptions(slice, Point(0.5, 2), nearest);
        SvgImageSampleResult sliceRight = new SvgImageSampler().SampleWithNodeAndPointAndOptions(slice, Point(3.5, 2), nearest);

        AssertSample(letterbox, false, 0, 0, 0, 0);
        AssertSample(meetPixel, true, 255, 0, 0, 255);
        AssertSample(sliceLeft, true, 255, 0, 0, 255);
        AssertSample(sliceRight, true, 0, 0, 255, 255);
    }

    [Test]
    public void SceneBuilderRetainsImageRenderingAndIntrinsicMapping()
    {
        string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZK8sAAAAASUVORK5CYII=";
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image width='4' height='2' preserveAspectRatio='none' image-rendering='crisp-edges' href='data:image/png;base64," + png + "'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = new SvgSceneBuilder().BuildWithDocument(resolved);
        SvgSceneNode image = built.Scene.FindNode(document.DocumentElement.Children[0], "");

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(image.Image.Rendering, Is.EqualTo(SvgImageRendering.CrispEdges));
            Assert.That(image.Image.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.None));
            Assert.That(image.Image.ContentTransform.A, Is.EqualTo(4));
            Assert.That(image.Image.ContentTransform.D, Is.EqualTo(2));
            Assert.That(image.Image.Resource.Image.IntrinsicSize.Width, Is.EqualTo(1));
        });
    }

    [Test]
    public void DrawAppliesNodeTransformClipAndAlphaWithoutTouchingOtherPixels()
    {
        SvgPixelBuffer source = Surface(2, 1);
        Write(source, 0, 0, 255, 0, 0, 255);
        Write(source, 1, 0, 0, 0, 255, 255);
        SvgSceneNode image = Image(source, SvgRect.Create(0, 0, 2, 1), Identity());
        image.AbsoluteTransform = new SvgMatrix(2, 0, 0, 2, 1, 0);
        image.Opacity = 0.5;
        SvgPixelBuffer destination = Surface(6, 2);
        destination.ClearWithPixel(new SvgPremultipliedRgba8(0, 255, 0, 255));
        SvgImageDrawOptions options = new SvgImageDrawOptions();
        options.SamplingMode = SvgImageSamplingMode.Nearest;
        options.Opacity = 0.5;
        options.Clip = new SvgPixelRect(2, 0, 2, 1);
        options.MaximumPixels = 2;

        SvgImageDrawResult result = new SvgImageRasterizer().DrawWithNodeAndDestinationAndOptions(image, destination, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bounds.X, Is.EqualTo(2));
            Assert.That(result.Bounds.Width, Is.EqualTo(2));
            Assert.That(result.PixelsTested, Is.EqualTo(2));
            Assert.That(result.PixelsWritten, Is.EqualTo(2));
        });
        AssertPixel(destination, 1, 0, 0, 255, 0, 255);
        AssertPixel(destination, 2, 0, 64, 0, 0, 64);
        AssertPixel(destination, 3, 0, 0, 0, 64, 64);
        AssertPixel(destination, 4, 0, 0, 255, 0, 255);
    }

    [Test]
    public void ClippedOversizedSourceUsesOnlyBoundedDestinationWork()
    {
        SvgPixelBuffer source = Surface(100, 1);
        source.ClearWithPixel(new SvgPremultipliedRgba8(255, 0, 0, 255));
        SvgSceneNode image = Image(source, SvgRect.Create(0, 0, 10000, 1), new SvgMatrix(100, 0, 0, 1, 0, 0));
        SvgPixelBuffer destination = Surface(4, 1);
        SvgImageDrawOptions clipped = new SvgImageDrawOptions();
        clipped.SamplingMode = SvgImageSamplingMode.Nearest;
        clipped.Clip = new SvgPixelRect(2, 0, 1, 1);
        clipped.MaximumPixels = 1;
        SvgImageDrawOptions limited = new SvgImageDrawOptions();
        limited.MaximumPixels = 1;

        SvgImageDrawResult success = new SvgImageRasterizer().DrawWithNodeAndDestinationAndOptions(image, destination, clipped);
        SvgImageDrawResult failure = new SvgImageRasterizer().DrawWithNodeAndDestinationAndOptions(image, destination, limited);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.PixelsTested, Is.EqualTo(1));
            Assert.That(success.PixelsWritten, Is.EqualTo(1));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Error, Is.EqualTo(SvgImageSampleError.WorkLimitExceeded));
            Assert.That(failure.PixelsTested, Is.EqualTo(4));
        });
        AssertPixel(destination, 0, 0, 0, 0, 0, 0);
        AssertPixel(destination, 2, 0, 255, 0, 0, 255);
        AssertPixel(destination, 3, 0, 0, 0, 0, 0);
    }

    [Test]
    public void ReportsMissingPixelsSingularMappingsAndColorMismatch()
    {
        SvgSceneNode missing = new SvgSceneNode(SvgSceneNodeKind.Image);
        missing.Image.Bounds = SvgRect.Create(0, 0, 1, 1);
        missing.Image.Resource = new SvgSceneResource("missing", SvgSceneResourceKind.Image);
        SvgPixelBuffer linear = SvgPixelBuffer.Create(1, 1, SvgPixelColorSpace.SrgbLinear).Buffer;
        SvgSceneNode mismatch = Image(linear, SvgRect.Create(0, 0, 1, 1), Identity());
        SvgSceneNode singular = Image(Surface(1, 1), SvgRect.Create(0, 0, 1, 1), Identity());
        singular.Image.ContentTransform.A = 0;

        SvgImageSampleResult noPixels = new SvgImageSampler().SampleWithNodeAndPoint(missing, Point(0.5, 0.5));
        SvgImageSampleResult badTransform = new SvgImageSampler().SampleWithNodeAndPoint(singular, Point(0.5, 0.5));
        SvgImageDrawResult badSpace = new SvgImageRasterizer().DrawWithNodeAndDestination(mismatch, Surface(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(noPixels.Success, Is.False);
            Assert.That(noPixels.Error, Is.EqualTo(SvgImageSampleError.MissingDecodedPixels));
            Assert.That(badTransform.Success, Is.False);
            Assert.That(badTransform.Error, Is.EqualTo(SvgImageSampleError.SingularTransform));
            Assert.That(badSpace.Success, Is.False);
            Assert.That(badSpace.Error, Is.EqualTo(SvgImageSampleError.ColorSpaceMismatch));
        });
    }

    static SvgSceneNode Image(SvgPixelBuffer pixels, SvgRect bounds, SvgMatrix contentTransform)
    {
        SvgSceneResource resource = new SvgSceneResource("image-" + pixels.Width + "-" + pixels.Height + "-" + bounds.Width, SvgSceneResourceKind.Image);
        resource.Image.DecodedContentAvailable = true;
        resource.Image.DecodedPixels = pixels;
        resource.Image.IntrinsicSize = SvgIntrinsicSize.Create(pixels.Width, pixels.Height);
        SvgSceneNode node = new SvgSceneNode(SvgSceneNodeKind.Image);
        node.Image.Resource = resource;
        node.Image.Bounds = bounds;
        node.Image.ContentTransform = contentTransform;
        return node;
    }

    static SvgPixelBuffer Surface(int width, int height)
    {
        return SvgPixelBuffer.Create(width, height, SvgPixelColorSpace.Srgb).Buffer;
    }

    static void Write(SvgPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(buffer.Write(x, y, new SvgPremultipliedRgba8(red, green, blue, alpha)).Success, Is.True);
    }

    static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }

    static SvgPoint Point(double x, double y)
    {
        return new SvgPoint(x, y);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void AssertSample(SvgImageSampleResult result, bool hasPixel, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgImageSampleError.None));
        Assert.That(result.HasPixel, Is.EqualTo(hasPixel));
        Assert.Multiple(() =>
        {
            Assert.That(result.Pixel.Red, Is.EqualTo(red));
            Assert.That(result.Pixel.Green, Is.EqualTo(green));
            Assert.That(result.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(result.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static void AssertPixel(SvgPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult result = buffer.Read(x, y);
        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Pixel.Red, Is.EqualTo(red));
            Assert.That(result.Pixel.Green, Is.EqualTo(green));
            Assert.That(result.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(result.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }
}
