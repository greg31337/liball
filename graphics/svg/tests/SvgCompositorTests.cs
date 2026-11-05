namespace svg.Tests;

public class SvgCompositorTests
{
    [TestCase(SvgCompositeOperator.Clear, 0, 0, 0, 0)]
    [TestCase(SvgCompositeOperator.Copy, 128, 0, 0, 128)]
    [TestCase(SvgCompositeOperator.Destination, 0, 0, 64, 64)]
    [TestCase(SvgCompositeOperator.SourceOver, 128, 0, 32, 160)]
    [TestCase(SvgCompositeOperator.DestinationOver, 96, 0, 64, 160)]
    [TestCase(SvgCompositeOperator.SourceIn, 32, 0, 0, 32)]
    [TestCase(SvgCompositeOperator.DestinationIn, 0, 0, 32, 32)]
    [TestCase(SvgCompositeOperator.SourceOut, 96, 0, 0, 96)]
    [TestCase(SvgCompositeOperator.DestinationOut, 0, 0, 32, 32)]
    [TestCase(SvgCompositeOperator.SourceAtop, 32, 0, 32, 64)]
    [TestCase(SvgCompositeOperator.DestinationAtop, 96, 0, 32, 128)]
    [TestCase(SvgCompositeOperator.Xor, 96, 0, 32, 128)]
    [TestCase(SvgCompositeOperator.Lighter, 128, 0, 64, 192)]
    public void ImplementsEveryPorterDuffOperator(SvgCompositeOperator operation, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelBuffer source = Surface(1, 1);
        SvgPixelBuffer destination = Surface(1, 1);
        Write(source, 0, 0, 128, 0, 0, 128);
        Write(destination, 0, 0, 0, 0, 64, 64);
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.Operator = operation;

        SvgCompositeResult result = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(source, destination, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgCompositeError.None));
            Assert.That(result.PixelsProcessed, Is.EqualTo(1));
            Assert.That(result.UsedSnapshot, Is.False);
        });
        AssertPixel(destination, 0, 0, red, green, blue, alpha);
    }

    [Test]
    public void AppliesSourceOpacityBeforeSourceOver()
    {
        SvgPixelBuffer source = Surface(1, 1);
        SvgPixelBuffer destination = Surface(1, 1);
        Write(source, 0, 0, 128, 0, 0, 128);
        Write(destination, 0, 0, 0, 0, 64, 64);
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.Opacity = 0.5;

        SvgCompositeResult result = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(source, destination, options);

        Assert.That(result.Success, Is.True);
        AssertPixel(destination, 0, 0, 64, 0, 48, 112);
    }

    [Test]
    public void ClipsPlacementAndPreflightsWorkBeforeWriting()
    {
        SvgPixelBuffer source = Surface(3, 1);
        Write(source, 0, 0, 255, 0, 0, 255);
        Write(source, 1, 0, 0, 255, 0, 255);
        Write(source, 2, 0, 0, 0, 255, 255);
        SvgPixelBuffer destination = Surface(3, 1);
        SvgCompositeOptions clipped = new SvgCompositeOptions();
        clipped.Operator = SvgCompositeOperator.Copy;
        clipped.DestinationX = -1;
        clipped.Clip = new SvgPixelRect(1, 0, 1, 1);
        clipped.MaximumPixels = 1;

        SvgCompositeResult success = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(source, destination, clipped);
        SvgPixelBuffer limitedDestination = Surface(3, 1);
        SvgCompositeOptions limited = new SvgCompositeOptions();
        limited.DestinationX = -1;
        limited.MaximumPixels = 1;
        SvgCompositeResult failure = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(source, limitedDestination, limited);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Bounds.X, Is.EqualTo(1));
            Assert.That(success.Bounds.Width, Is.EqualTo(1));
            Assert.That(success.PixelsProcessed, Is.EqualTo(1));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Error, Is.EqualTo(SvgCompositeError.WorkLimitExceeded));
            Assert.That(failure.PixelsProcessed, Is.EqualTo(2));
        });
        AssertPixel(destination, 0, 0, 0, 0, 0, 0);
        AssertPixel(destination, 1, 0, 0, 0, 255, 255);
        AssertPixel(destination, 2, 0, 0, 0, 0, 0);
        AssertPixel(limitedDestination, 0, 0, 0, 0, 0, 0);
    }

    [Test]
    public void SnapshotsSharedStorageForOverlapSafety()
    {
        SvgPixelBuffer buffer = Surface(3, 1);
        Write(buffer, 0, 0, 255, 0, 0, 255);
        Write(buffer, 1, 0, 0, 255, 0, 255);
        Write(buffer, 2, 0, 0, 0, 255, 255);
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.Operator = SvgCompositeOperator.Copy;
        options.SourceRectangle = new SvgPixelRect(0, 0, 2, 1);
        options.DestinationX = 1;

        SvgCompositeResult result = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(buffer, buffer, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedSnapshot, Is.True);
        });
        AssertPixel(buffer, 0, 0, 255, 0, 0, 255);
        AssertPixel(buffer, 1, 0, 255, 0, 0, 255);
        AssertPixel(buffer, 2, 0, 0, 255, 0, 255);
    }

    [Test]
    public void SharedDisjointAndFullyClippedOperationsAvoidSnapshots()
    {
        SvgPixelBuffer buffer = Surface(3, 1);
        Write(buffer, 0, 0, 255, 0, 0, 255);
        Write(buffer, 2, 0, 0, 0, 255, 255);
        SvgCompositeOptions disjoint = new SvgCompositeOptions();
        disjoint.Operator = SvgCompositeOperator.Copy;
        disjoint.SourceRectangle = new SvgPixelRect(0, 0, 1, 1);
        disjoint.DestinationX = 2;
        SvgCompositeOptions outside = new SvgCompositeOptions();
        outside.DestinationX = 100;

        SvgCompositeResult copied = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(buffer, buffer, disjoint);
        SvgCompositeResult clipped = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(buffer, buffer, outside);

        Assert.Multiple(() =>
        {
            Assert.That(copied.Success, Is.True);
            Assert.That(copied.UsedSnapshot, Is.False);
            Assert.That(clipped.Success, Is.True);
            Assert.That(clipped.Bounds.X, Is.EqualTo(3));
            Assert.That(clipped.Bounds.IsEmpty, Is.True);
            Assert.That(clipped.PixelsProcessed, Is.Zero);
            Assert.That(clipped.UsedSnapshot, Is.False);
        });
        AssertPixel(buffer, 2, 0, 255, 0, 0, 255);
    }

    [Test]
    public void OpacityGroupAllocatesWithinLimitsAndCompositesOnce()
    {
        SvgPixelRect bounds = new SvgPixelRect(1, 0, 1, 1);
        SvgOpacityGroupResult created = new SvgCompositor().CreateOpacityGroupWithBoundsAndColorSpaceAndOpacityAndLimits(bounds, SvgPixelColorSpace.Srgb, 0.5, new SvgRasterLimits());
        Write(created.Group.Surface, 0, 0, 255, 0, 0, 255);
        SvgPixelBuffer destination = Surface(3, 1);
        destination.ClearWithPixel(new SvgPremultipliedRgba8(0, 0, 255, 255));

        SvgCompositeResult composited = created.Group.CompositeTo(destination);
        SvgRasterLimits limits = new SvgRasterLimits();
        limits.MaxWidth = 1;
        SvgOpacityGroupResult rejected = new SvgCompositor().CreateOpacityGroupWithBoundsAndColorSpaceAndOpacityAndLimits(new SvgPixelRect(0, 0, 2, 1), SvgPixelColorSpace.Srgb, 1, limits);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Group.Surface.OwnsData, Is.True);
            Assert.That(composited.Success, Is.True);
            Assert.That(composited.Bounds.X, Is.EqualTo(1));
            Assert.That(composited.PixelsProcessed, Is.EqualTo(1));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Error, Is.EqualTo(SvgCompositeError.AllocationLimitExceeded));
        });
        AssertPixel(destination, 0, 0, 0, 0, 255, 255);
        AssertPixel(destination, 1, 0, 128, 0, 127, 255);
        AssertPixel(destination, 2, 0, 0, 0, 255, 255);
    }

    [Test]
    public void CreatesOpacityGroupFromPlannedLayerBounds()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g opacity='.25'><rect width='2' height='1'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildResult built = new SvgSceneBuilder().BuildWithDocument(resolved);
        SvgSceneNode groupNode = built.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgSceneLayer layer = built.Scene.LayerPlan.Find(groupNode);

        SvgOpacityGroupResult result = new SvgCompositor().CreateOpacityGroupWithLayerAndColorSpaceAndLimits(layer, SvgPixelColorSpace.Srgb, new SvgRasterLimits());

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(layer.CreatesCompositingLayer, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Group.Bounds.X, Is.EqualTo(0));
            Assert.That(result.Group.Bounds.Y, Is.EqualTo(0));
            Assert.That(result.Group.Bounds.Width, Is.EqualTo(2));
            Assert.That(result.Group.Bounds.Height, Is.EqualTo(1));
            Assert.That(result.Group.Opacity, Is.EqualTo(0.25));
        });
    }

    [Test]
    public void ConvertsColorSpacesAndRejectsInvalidPremultipliedPixels()
    {
        SvgPixelBuffer source = Surface(1, 1);
        Write(source, 0, 0, 128, 0, 0, 255);
        SvgPixelBuffer linear = SvgPixelBuffer.Create(1, 1, SvgPixelColorSpace.SrgbLinear).Buffer;
        SvgCompositeResult converted = new SvgCompositor().CompositeWithSourceAndDestination(source, linear);
        SvgPixelBuffer invalid = Surface(1, 1);
        invalid.Data[0] = 1;
        SvgCompositeResult malformed = new SvgCompositor().CompositeWithSourceAndDestination(invalid, Surface(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Error, Is.EqualTo(SvgCompositeError.InvalidPremultipliedPixel));
        });
        AssertPixel(linear, 0, 0, 55, 0, 0, 255);
    }

    static SvgPixelBuffer Surface(int width, int height)
    {
        return SvgPixelBuffer.Create(width, height, SvgPixelColorSpace.Srgb).Buffer;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void Write(SvgPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(buffer.Write(x, y, new SvgPremultipliedRgba8(red, green, blue, alpha)).Success, Is.True);
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
