namespace svg.Tests;

public class SvgBlendModeTests
{
    [TestCase(SvgBlendMode.Multiply, 38, 45, 61)]
    [TestCase(SvgBlendMode.Screen, 204, 198, 194)]
    [TestCase(SvgBlendMode.Overlay, 76, 141, 122)]
    [TestCase(SvgBlendMode.Darken, 51, 64, 102)]
    [TestCase(SvgBlendMode.Lighten, 191, 179, 153)]
    [TestCase(SvgBlendMode.ColorDodge, 203, 239, 255)]
    [TestCase(SvgBlendMode.ColorBurn, 0, 0, 0)]
    [TestCase(SvgBlendMode.HardLight, 153, 90, 133)]
    [TestCase(SvgBlendMode.SoftLight, 82, 152, 114)]
    [TestCase(SvgBlendMode.Difference, 140, 115, 51)]
    [TestCase(SvgBlendMode.Exclusion, 166, 153, 133)]
    [TestCase(SvgBlendMode.PlusDarker, 0, 0, 0)]
    [TestCase(SvgBlendMode.PlusLighter, 242, 243, 255)]
    public void ImplementsSeparableAndAdditiveModes(SvgBlendMode mode, byte red, byte green, byte blue)
    {
        SvgPixelBuffer destination = Pixel(51, 179, 102, 255);
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.BlendMode = mode;

        SvgCompositeResult result = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(191, 64, 153, 255), destination, options);

        Assert.That(result.Success, Is.True);
        AssertPixel(destination, red, green, blue, 255);
    }

    [TestCase(SvgBlendMode.Hue, 212, 84, 174)]
    [TestCase(SvgBlendMode.Saturation, 52, 179, 102)]
    [TestCase(SvgBlendMode.Color, 211, 84, 173)]
    [TestCase(SvgBlendMode.Luminosity, 31, 159, 82)]
    public void ImplementsNonSeparableModes(SvgBlendMode mode, byte red, byte green, byte blue)
    {
        SvgPixelBuffer destination = Pixel(51, 179, 102, 255);
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.BlendMode = mode;

        SvgCompositeResult result = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(191, 64, 153, 255), destination, options);

        Assert.That(result.Success, Is.True);
        AssertPixel(destination, red, green, blue, 255);
    }

    [Test]
    public void BlendsPremultipliedPartiallyTransparentPixels()
    {
        SvgPixelBuffer destination = Pixel(0, 0, 128, 128);
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.BlendMode = SvgBlendMode.Multiply;

        SvgCompositeResult result = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(128, 0, 0, 128), destination, options);

        Assert.That(result.Success, Is.True);
        AssertPixel(destination, 64, 0, 64, 192);
    }

    [Test]
    public void SelectsSrgbOrLinearSrgbBlendArithmetic()
    {
        SvgPixelBuffer srgbDestination = Pixel(128, 128, 128, 255);
        SvgPixelBuffer linearDestination = Pixel(128, 128, 128, 255);
        SvgCompositeOptions srgb = new SvgCompositeOptions();
        srgb.BlendMode = SvgBlendMode.Screen;
        srgb.BlendColorSpace = SvgBlendColorSpace.Srgb;
        SvgCompositeOptions linear = new SvgCompositeOptions();
        linear.BlendMode = SvgBlendMode.Screen;
        linear.BlendColorSpace = SvgBlendColorSpace.SrgbLinear;

        SvgCompositeResult srgbResult = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(128, 128, 128, 255), srgbDestination, srgb);
        SvgCompositeResult linearResult = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(128, 128, 128, 255), linearDestination, linear);

        Assert.Multiple(() =>
        {
            Assert.That(srgbResult.Success, Is.True);
            Assert.That(linearResult.Success, Is.True);
        });
        AssertPixel(srgbDestination, 192, 192, 192, 255);
        AssertPixel(linearDestination, 167, 167, 167, 255);
    }

    [Test]
    public void IsolationSuppressesBackdropColorBlending()
    {
        SvgPixelBuffer blended = Pixel(128, 128, 128, 255);
        SvgPixelBuffer isolated = Pixel(128, 128, 128, 255);
        SvgCompositeOptions blendOptions = new SvgCompositeOptions();
        blendOptions.BlendMode = SvgBlendMode.Multiply;
        SvgCompositeOptions isolatedOptions = new SvgCompositeOptions();
        isolatedOptions.BlendMode = SvgBlendMode.Multiply;
        isolatedOptions.Isolated = true;

        new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(255, 0, 0, 255), blended, blendOptions);
        new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(255, 0, 0, 255), isolated, isolatedOptions);

        AssertPixel(blended, 128, 0, 0, 255);
        AssertPixel(isolated, 255, 0, 0, 255);
    }

    [Test]
    public void SceneLayerCarriesBlendIsolationAndInterpolationToItsGroup()
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg xmlns='http://www.w3.org/2000/svg'><g style='mix-blend-mode:screen;isolation:isolate;color-interpolation:linearRGB'><rect width='2' height='1'/></g></svg>", new SvgLoadOptions());
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(loaded.Document);
        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode node = built.Scene.FindNode(loaded.Document.DocumentElement.Children[0], "");
        SvgSceneLayer layer = built.Scene.LayerPlan.Find(node);

        SvgOpacityGroupResult result = new SvgCompositor().CreateOpacityGroupWithLayerAndColorSpaceAndLimits(layer, SvgPixelColorSpace.Srgb, new SvgRasterLimits());

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Success, Is.True);
            Assert.That(built.Success, Is.True);
            Assert.That(node.ColorInterpolation, Is.EqualTo(SvgColorSpace.SrgbLinear));
            Assert.That(layer.ColorInterpolation, Is.EqualTo(SvgColorSpace.SrgbLinear));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Group.BlendMode, Is.EqualTo(SvgBlendMode.Screen));
            Assert.That(result.Group.BlendColorSpace, Is.EqualTo(SvgBlendColorSpace.SrgbLinear));
            Assert.That(result.Group.Isolated, Is.True);
        });
    }

    [Test]
    public void RejectsInvalidModesAndBlendModesOnOtherOperators()
    {
        SvgCompositeOptions invalid = new SvgCompositeOptions();
        invalid.BlendMode = (SvgBlendMode)100;
        SvgCompositeOptions incompatible = new SvgCompositeOptions();
        incompatible.Operator = SvgCompositeOperator.Copy;
        incompatible.BlendMode = SvgBlendMode.Multiply;

        SvgCompositeResult invalidResult = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(0, 0, 0, 0), Pixel(0, 0, 0, 0), invalid);
        SvgCompositeResult incompatibleResult = new SvgCompositor().CompositeWithSourceAndDestinationAndOptions(Pixel(0, 0, 0, 0), Pixel(0, 0, 0, 0), incompatible);

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Error, Is.EqualTo(SvgCompositeError.InvalidArgument));
            Assert.That(incompatibleResult.Error, Is.EqualTo(SvgCompositeError.InvalidArgument));
        });
    }

    static SvgPixelBuffer Pixel(byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelBuffer buffer = SvgPixelBuffer.Create(1, 1, SvgPixelColorSpace.Srgb).Buffer;
        Assert.That(buffer.Write(0, 0, new SvgPremultipliedRgba8(red, green, blue, alpha)).Success, Is.True);
        return buffer;
    }

    static void AssertPixel(SvgPixelBuffer buffer, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult result = buffer.Read(0, 0);
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
