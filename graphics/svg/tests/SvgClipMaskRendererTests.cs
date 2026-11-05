namespace svg.Tests;

public class SvgClipMaskRendererTests
{
    [Test]
    public void IntersectsRectangleAndResourceClipStackEntries()
    {
        SvgPixelBuffer surface = Surface(6, 1, 255, 0, 0, 255);
        SvgClipMaskOptions options = new SvgClipMaskOptions();
        options.Clips.Add(SvgClipStackEntry.CreateResource(Clip("clip", "M0 0 H4 V1 H0 Z"), Identity()));
        options.Clips.Add(new SvgClipStackEntry(new SvgPixelRect(1, 0, 4, 1)));

        SvgClipMaskResult result = new SvgClipMaskRenderer().Apply(surface, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ClipsApplied, Is.EqualTo(2));
            Assert.That(result.MaskApplied, Is.False);
            Assert.That(result.OffscreenSurfaces, Is.GreaterThanOrEqualTo(3));
        });
        AssertAlpha(surface, 0, 0);
        AssertAlpha(surface, 1, 255);
        AssertAlpha(surface, 2, 255);
        AssertAlpha(surface, 3, 255);
        AssertAlpha(surface, 4, 0);
        AssertAlpha(surface, 5, 0);
    }

    [Test]
    public void IntersectsAClipAttachedInsideClipContent()
    {
        SvgSceneResource inner = Clip("inner", "M1 0 H3 V1 H1 Z");
        SvgSceneResource outer = Clip("outer", "M0 0 H4 V1 H0 Z");
        outer.Clip.Content.Children[0].Clip = inner;
        SvgPixelBuffer surface = Surface(4, 1, 0, 255, 0, 255);
        SvgClipMaskOptions options = new SvgClipMaskOptions();
        options.Clips.Add(SvgClipStackEntry.CreateResource(outer, Identity()));

        SvgClipMaskResult result = new SvgClipMaskRenderer().Apply(surface, options);

        Assert.That(result.Success, Is.True);
        AssertAlpha(surface, 0, 0);
        AssertAlpha(surface, 1, 255);
        AssertAlpha(surface, 2, 255);
        AssertAlpha(surface, 3, 0);
    }

    [Test]
    public void SceneBuilderRetainsAClipAppliedToAClipPathElement()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<clipPath id='inner'><rect x='1' width='2' height='1'/></clipPath>" +
            "<clipPath id='outer' clip-path='url(#inner)'><rect width='4' height='1'/></clipPath>" +
            "</defs><rect width='4' height='1' clip-path='url(#outer)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode target = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgPixelBuffer surface = Surface(4, 1, 255, 0, 0, 255);
        SvgClipMaskOptions options = new SvgClipMaskOptions();
        options.Clips.Add(SvgClipStackEntry.CreateResource(target.Clip, Identity()));

        SvgClipMaskResult result = new SvgClipMaskRenderer().Apply(surface, options);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(target.Clip.Clip.Content.Clip, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        });
        AssertAlpha(surface, 0, 0);
        AssertAlpha(surface, 1, 255);
        AssertAlpha(surface, 2, 255);
        AssertAlpha(surface, 3, 0);
    }

    [Test]
    public void AlphaAndLuminanceMasksUseDifferentChannels()
    {
        SvgPixelBuffer alphaSurface = Surface(1, 1, 255, 255, 255, 255);
        SvgPixelBuffer luminanceSurface = Surface(1, 1, 255, 255, 255, 255);
        SvgSceneResource alpha = Mask("alpha", SvgMaskType.Alpha, 0, 0, 1, 1, Solid("red", 1, 0, 0, 0.5));
        SvgSceneResource luminance = Mask("luminance", SvgMaskType.Luminance, 0, 0, 1, 1, Solid("red-2", 1, 0, 0, 1));
        SvgClipMaskOptions alphaOptions = new SvgClipMaskOptions();
        alphaOptions.Mask = alpha;
        SvgClipMaskOptions luminanceOptions = new SvgClipMaskOptions();
        luminanceOptions.Mask = luminance;

        SvgClipMaskResult alphaResult = new SvgClipMaskRenderer().Apply(alphaSurface, alphaOptions);
        SvgClipMaskResult luminanceResult = new SvgClipMaskRenderer().Apply(luminanceSurface, luminanceOptions);

        Assert.Multiple(() =>
        {
            Assert.That(alphaResult.Success, Is.True);
            Assert.That(alphaResult.MaskApplied, Is.True);
            Assert.That(luminanceResult.Success, Is.True);
        });
        AssertPixel(alphaSurface, 0, 128, 128, 128, 128);
        AssertPixel(luminanceSurface, 0, 54, 54, 54, 54);
    }

    [Test]
    public void LuminanceUsesTheSelectedSrgbOrLinearSrgbSpace()
    {
        SvgSceneResource paint = Solid("gray", 0.5, 0.5, 0.5, 1);
        SvgSceneResource srgb = Mask("srgb", SvgMaskType.Luminance, 0, 0, 1, 1, paint);
        SvgSceneResource linear = Mask("linear", SvgMaskType.Luminance, 0, 0, 1, 1, paint);
        linear.Mask.ColorInterpolation = SvgColorSpace.SrgbLinear;
        SvgPixelBuffer srgbSurface = Surface(1, 1, 255, 255, 255, 255);
        SvgPixelBuffer linearSurface = Surface(1, 1, 255, 255, 255, 255);
        SvgClipMaskOptions srgbOptions = new SvgClipMaskOptions();
        srgbOptions.Mask = srgb;
        SvgClipMaskOptions linearOptions = new SvgClipMaskOptions();
        linearOptions.Mask = linear;

        new SvgClipMaskRenderer().Apply(srgbSurface, srgbOptions);
        new SvgClipMaskRenderer().Apply(linearSurface, linearOptions);

        AssertAlpha(srgbSurface, 0, 128);
        AssertAlpha(linearSurface, 0, 55);
    }

    [Test]
    public void LinearLuminanceConvertsSrgbImageMaskContent()
    {
        SvgPixelBuffer imagePixels = Surface(1, 1, 128, 128, 128, 255);
        SvgSceneResource imageResource = new SvgSceneResource("mask-image", SvgSceneResourceKind.Image);
        imageResource.Image.DecodedPixels = imagePixels;
        imageResource.Image.DecodedContentAvailable = true;
        imageResource.Image.IntrinsicSize = SvgIntrinsicSize.Create(1, 1);
        SvgSceneNode image = new SvgSceneNode(SvgSceneNodeKind.Image);
        image.Image.Resource = imageResource;
        image.Image.Bounds = SvgRect.Create(0, 0, 1, 1, false);
        image.Image.ContentTransform = Identity();
        SvgSceneResource mask = new SvgSceneResource("image-mask", SvgSceneResourceKind.Mask);
        mask.Mask.MaskType = SvgMaskType.Luminance;
        mask.Mask.ColorInterpolation = SvgColorSpace.SrgbLinear;
        mask.Mask.Units = SvgPaintServerUnits.UserSpaceOnUse;
        mask.Mask.Region = SvgRect.Create(0, 0, 1, 1, false);
        mask.Mask.Content = new SvgSceneNode(SvgSceneNodeKind.Group);
        Assert.That(mask.Mask.Content.AddChild(image), Is.True);
        SvgPixelBuffer surface = Surface(1, 1, 255, 255, 255, 255);
        SvgClipMaskOptions options = new SvgClipMaskOptions();
        options.Mask = mask;

        SvgClipMaskResult result = new SvgClipMaskRenderer().Apply(surface, options);

        Assert.That(result.Success, Is.True);
        AssertAlpha(surface, 0, 55);
    }

    [Test]
    public void MaskTransformRegionAndOperationSubregionBoundTheEffect()
    {
        SvgPixelBuffer surface = Surface(5, 1, 0, 0, 255, 255);
        SvgSceneResource mask = Mask("translated", SvgMaskType.Alpha, 0, 0, 2, 1, Solid("white", 1, 1, 1, 1));
        SvgClipMaskOptions options = new SvgClipMaskOptions();
        options.Mask = mask;
        options.MaskTransform = new SvgMatrix(1, 0, 0, 1, 2, 0);
        options.Subregion = new SvgPixelRect(1, 0, 3, 1);

        SvgClipMaskResult result = new SvgClipMaskRenderer().Apply(surface, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bounds.X, Is.EqualTo(1));
            Assert.That(result.Bounds.Width, Is.EqualTo(3));
        });
        AssertAlpha(surface, 0, 255);
        AssertAlpha(surface, 1, 0);
        AssertAlpha(surface, 2, 255);
        AssertAlpha(surface, 3, 255);
        AssertAlpha(surface, 4, 255);
    }

    [Test]
    public void EmptyClipAndMaskCompletelyClearTheAffectedPixels()
    {
        SvgSceneResource emptyClip = new SvgSceneResource("empty-clip", SvgSceneResourceKind.Clip);
        emptyClip.Clip.Content = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgPixelBuffer clipped = Surface(2, 1, 255, 0, 0, 255);
        SvgClipMaskOptions clipOptions = new SvgClipMaskOptions();
        clipOptions.Clips.Add(SvgClipStackEntry.CreateResource(emptyClip, Identity()));
        SvgSceneResource emptyMask = Mask("empty-mask", SvgMaskType.Alpha, 0, 0, 0, 1, Solid("white-empty", 1, 1, 1, 1));
        SvgPixelBuffer masked = Surface(2, 1, 0, 255, 0, 255);
        SvgClipMaskOptions maskOptions = new SvgClipMaskOptions();
        maskOptions.Mask = emptyMask;

        SvgClipMaskResult clipResult = new SvgClipMaskRenderer().Apply(clipped, clipOptions);
        SvgClipMaskResult maskResult = new SvgClipMaskRenderer().Apply(masked, maskOptions);

        Assert.Multiple(() =>
        {
            Assert.That(clipResult.Success, Is.True);
            Assert.That(maskResult.Success, Is.True);
        });
        AssertAlpha(clipped, 0, 0);
        AssertAlpha(clipped, 1, 0);
        AssertAlpha(masked, 0, 0);
        AssertAlpha(masked, 1, 0);
    }

    [Test]
    public void SceneResourcesRetainObjectBoxTransformsClipRuleAndMaskInterpolation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<clipPath id='c' clipPathUnits='objectBoundingBox'><path clip-rule='evenodd' d='M0 0 H.5 V1 H0 Z'/></clipPath>" +
            "<mask id='m' maskUnits='objectBoundingBox' maskContentUnits='objectBoundingBox' color-interpolation='linearRGB'><rect width='.5' height='1' fill='white'/></mask>" +
            "</defs><rect x='2' width='4' height='1' clip-path='url(#c)' mask='url(#m)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode target = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneNode clipPath = target.Clip.Clip.Content.Children[0];
        SvgPixelBuffer surface = Surface(6, 1, 255, 0, 0, 255);
        SvgClipMaskOptions options = new SvgClipMaskOptions();
        options.Clips.Add(SvgClipStackEntry.CreateResource(target.Clip, Identity()));
        options.Subregion = new SvgPixelRect(2, 0, 4, 1);
        SvgPixelBuffer maskSurface = Surface(6, 1, 0, 255, 0, 255);
        SvgClipMaskOptions maskOptions = new SvgClipMaskOptions();
        maskOptions.Mask = target.Mask;
        maskOptions.Subregion = new SvgPixelRect(2, 0, 4, 1);

        SvgClipMaskResult result = new SvgClipMaskRenderer().Apply(surface, options);
        SvgClipMaskResult maskResult = new SvgClipMaskRenderer().Apply(maskSurface, maskOptions);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(target.Clip.Clip.Units, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(target.Clip.Clip.Transform.A, Is.EqualTo(4));
            Assert.That(target.Clip.Clip.Transform.E, Is.EqualTo(2));
            Assert.That(clipPath.Path.FillRule, Is.EqualTo(SvgFillRule.EvenOdd));
            Assert.That(target.Mask.Mask.ContentUnits, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(target.Mask.Mask.ContentTransform.A, Is.EqualTo(4));
            Assert.That(target.Mask.Mask.ColorInterpolation, Is.EqualTo(SvgColorSpace.SrgbLinear));
            Assert.That(result.Success, Is.True);
            Assert.That(maskResult.Success, Is.True);
        });
        AssertAlpha(surface, 2, 255);
        AssertAlpha(surface, 3, 255);
        AssertAlpha(surface, 4, 0);
        AssertAlpha(surface, 5, 0);
        AssertAlpha(maskSurface, 2, 255);
        AssertAlpha(maskSurface, 3, 255);
        AssertAlpha(maskSurface, 4, 0);
        AssertAlpha(maskSurface, 5, 0);
    }

    [Test]
    public void AllocationWorkDepthAndRecursionFailuresLeaveTargetUnchanged()
    {
        SvgSceneResource clip = Clip("limited", "M0 0 H3 V1 H0 Z");
        SvgPixelBuffer allocationSurface = Surface(3, 1, 255, 0, 0, 255);
        SvgClipMaskOptions allocation = new SvgClipMaskOptions();
        allocation.Clips.Add(SvgClipStackEntry.CreateResource(clip, Identity()));
        allocation.RasterLimits.MaxWidth = 2;
        SvgPixelBuffer workSurface = Surface(3, 1, 255, 0, 0, 255);
        SvgClipMaskOptions work = new SvgClipMaskOptions();
        work.Clips.Add(SvgClipStackEntry.CreateResource(clip, Identity()));
        work.MaximumPixels = 2;
        SvgPixelBuffer depthSurface = Surface(3, 1, 255, 0, 0, 255);
        SvgClipMaskOptions depth = new SvgClipMaskOptions();
        depth.MaximumDepth = 1;
        depth.Clips.Add(SvgClipStackEntry.CreateResource(clip, Identity()));
        depth.Clips.Add(new SvgClipStackEntry(new SvgPixelRect(0, 0, 3, 1)));
        SvgSceneResource recursive = Clip("recursive", "M0 0 H3 V1 H0 Z");
        recursive.Clip.Content.Children[0].Clip = recursive;
        SvgPixelBuffer recursionSurface = Surface(3, 1, 255, 0, 0, 255);
        SvgClipMaskOptions recursion = new SvgClipMaskOptions();
        recursion.Clips.Add(SvgClipStackEntry.CreateResource(recursive, Identity()));

        SvgClipMaskResult allocationResult = new SvgClipMaskRenderer().Apply(allocationSurface, allocation);
        SvgClipMaskResult workResult = new SvgClipMaskRenderer().Apply(workSurface, work);
        SvgClipMaskResult depthResult = new SvgClipMaskRenderer().Apply(depthSurface, depth);
        SvgClipMaskResult recursionResult = new SvgClipMaskRenderer().Apply(recursionSurface, recursion);

        Assert.Multiple(() =>
        {
            Assert.That(allocationResult.Error, Is.EqualTo(SvgClipMaskError.AllocationLimitExceeded));
            Assert.That(workResult.Error, Is.EqualTo(SvgClipMaskError.WorkLimitExceeded));
            Assert.That(depthResult.Error, Is.EqualTo(SvgClipMaskError.DepthLimitExceeded));
            Assert.That(recursionResult.Error, Is.EqualTo(SvgClipMaskError.RecursionDetected));
        });
        AssertAlpha(allocationSurface, 0, 255);
        AssertAlpha(workSurface, 0, 255);
        AssertAlpha(depthSurface, 0, 255);
        AssertAlpha(recursionSurface, 0, 255);
    }

    static SvgSceneResource Clip(string key, string pathData)
    {
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.Clip);
        resource.Clip.Content = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgSceneNode path = new SvgSceneNode(SvgSceneNodeKind.Path);
        path.Path.Path = Parse(pathData);
        Assert.That(resource.Clip.Content.AddChild(path), Is.True);
        return resource;
    }

    static SvgSceneResource Mask(string key, SvgMaskType type, double x, double y, double width, double height, SvgSceneResource fill)
    {
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.Mask);
        resource.Mask.MaskType = type;
        resource.Mask.Units = SvgPaintServerUnits.UserSpaceOnUse;
        resource.Mask.Region = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
        resource.Mask.Content = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgSceneNode path = new SvgSceneNode(SvgSceneNodeKind.Path);
        path.Path.Path = Parse("M" + x + " " + y + " H" + (x + width) + " V" + (y + height) + " H" + x + " Z");
        path.Fill = fill;
        Assert.That(resource.Mask.Content.AddChild(path), Is.True);
        return resource;
    }

    static SvgSceneResource Solid(string key, double red, double green, double blue, double alpha)
    {
        SvgSceneResource resource = new SvgSceneResource(key, SvgSceneResourceKind.SolidPaint);
        resource.SolidPaint.Color = new SvgColor(SvgColorSpace.Srgb, red, green, blue, 1);
        resource.SolidPaint.Opacity = alpha;
        return resource;
    }

    static SvgPixelBuffer Surface(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelBuffer surface = SvgPixelBuffer.Create(width, height, SvgPixelColorSpace.Srgb).Buffer;
        Assert.That(surface.ClearWithPixel(new SvgPremultipliedRgba8(red, green, blue, alpha)).Success, Is.True);
        return surface;
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 256, 256);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void AssertAlpha(SvgPixelBuffer surface, int x, byte alpha)
    {
        SvgPixelReadResult result = surface.Read(x, 0);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Pixel.Alpha, Is.EqualTo(alpha), "pixel " + x);
    }

    static void AssertPixel(SvgPixelBuffer surface, int x, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult result = surface.Read(x, 0);
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
