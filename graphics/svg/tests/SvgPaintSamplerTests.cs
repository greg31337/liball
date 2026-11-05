namespace svg.Tests;

public class SvgPaintSamplerTests
{
    [Test]
    public void SolidPaintUsesEffectiveAndAdditionalOpacityInRequestedEncoding()
    {
        SvgSceneResource solid = new SvgSceneResource("solid", SvgSceneResourceKind.SolidPaint);
        solid.SolidPaint.Color = new SvgColor(SvgColorSpace.Srgb, 1, 0.5, 0, 0.75);
        solid.SolidPaint.Opacity = 0.25;
        SvgPaintSampleOptions options = new SvgPaintSampleOptions();
        options.Opacity = 0.5;
        SvgPaintSampleOptions linearOptions = new SvgPaintSampleOptions();
        linearOptions.Opacity = 0.5;
        linearOptions.OutputColorSpace = SvgPixelColorSpace.SrgbLinear;

        SvgPaintSampleResult result = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(solid, Point(100, -20), options);
        SvgPaintSampleResult linearResult = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(solid, Point(100, -20), linearOptions);

        Assert.Multiple(() =>
        {
            AssertSample(result, true, 32, 16, 0, 32);
            AssertSample(linearResult, true, 32, 7, 0, 32);
            Assert.That(result.StopCount, Is.Zero);
            Assert.That(result.LowerStopIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void LinearGradientsInterpolatePremultipliedStopsInSelectedColorSpace()
    {
        SvgSceneResource srgb = Linear(0, 0, 10, 0);
        srgb.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        srgb.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));
        SvgSceneResource linear = Linear(0, 0, 10, 0);
        linear.GradientPaint.InterpolationSpace = SvgColorSpace.SrgbLinear;
        linear.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        linear.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));
        SvgSceneResource transparent = Linear(0, 0, 10, 0);
        transparent.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 0));
        transparent.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));

        SvgPaintSampleResult srgbMiddle = new SvgPaintSampler().SampleWithResourceAndPoint(srgb, Point(5, 0));
        SvgPaintSampleResult linearMiddle = new SvgPaintSampler().SampleWithResourceAndPoint(linear, Point(5, 0));
        SvgPaintSampleResult alphaMiddle = new SvgPaintSampler().SampleWithResourceAndPoint(transparent, Point(5, 0));

        Assert.Multiple(() =>
        {
            AssertSample(srgbMiddle, true, 128, 0, 128, 255);
            AssertSample(linearMiddle, true, 188, 0, 188, 255);
            AssertSample(alphaMiddle, true, 0, 0, 128, 128);
            Assert.That(srgbMiddle.Offset, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(srgbMiddle.LowerStopIndex, Is.Zero);
            Assert.That(srgbMiddle.UpperStopIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void TransformAndSpreadMethodsMapOffsetsDeterministically()
    {
        SvgSceneResource gradient = Linear(0, 0, 10, 0);
        gradient.GradientPaint.Transform = new SvgMatrix(2, 0, 0, 1, 10, 0);
        gradient.GradientPaint.Stops.Add(Stop(0, 0, 0, 0, 1));
        gradient.GradientPaint.Stops.Add(Stop(1, 1, 1, 1, 1));

        gradient.GradientPaint.SpreadMethod = SvgGradientSpreadMethod.Repeat;
        SvgPaintSampleResult repeated = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(35, 0));
        gradient.GradientPaint.SpreadMethod = SvgGradientSpreadMethod.Reflect;
        SvgPaintSampleResult reflected = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(35, 0));
        gradient.GradientPaint.SpreadMethod = SvgGradientSpreadMethod.Pad;
        SvgPaintSampleResult padded = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(0, 0));

        Assert.Multiple(() =>
        {
            AssertSample(repeated, true, 64, 64, 64, 255);
            Assert.That(repeated.Offset, Is.EqualTo(0.25).Within(1e-12));
            AssertSample(reflected, true, 191, 191, 191, 255);
            Assert.That(reflected.Offset, Is.EqualTo(0.75).Within(1e-12));
            AssertSample(padded, true, 0, 0, 0, 255);
            Assert.That(padded.Offset, Is.Zero);
        });
    }

    [Test]
    public void SamplingNormalizesMutableOffsetsAndHonorsDuplicateHardStops()
    {
        SvgSceneResource gradient = Linear(0, 0, 1, 0);
        gradient.GradientPaint.Stops.Add(Stop(0.8, 1, 0, 0, 1));
        gradient.GradientPaint.Stops.Add(Stop(0.2, 0, 1, 0, 1));
        gradient.GradientPaint.Stops.Add(Stop(1.5, 0, 0, 1, 1));
        SvgPaintSampleOptions limited = new SvgPaintSampleOptions();
        limited.MaximumStops = 2;

        SvgPaintSampleResult hardStop = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(0.8, 0));
        SvgPaintSampleResult after = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(0.9, 0));
        SvgPaintSampleResult overLimit = new SvgPaintSampler().SampleWithResourceAndPointAndOptions(gradient, Point(0.9, 0), limited);

        Assert.Multiple(() =>
        {
            AssertSample(hardStop, true, 0, 255, 0, 255);
            Assert.That(hardStop.LowerStopIndex, Is.EqualTo(1));
            Assert.That(hardStop.UpperStopIndex, Is.EqualTo(1));
            Assert.That(hardStop.StopCount, Is.EqualTo(3));
            AssertSample(after, true, 0, 128, 128, 255);
            AssertFailure(overLimit, SvgPaintSampleError.StopLimitExceeded);
            Assert.That(gradient.GradientPaint.Stops[1].Offset, Is.EqualTo(0.2));
            Assert.That(gradient.GradientPaint.Stops[2].Offset, Is.EqualTo(1.5));
        });
    }

    [Test]
    public void RadialFocalRadiusSamplesTheTwoCircleRamp()
    {
        SvgSceneResource gradient = Radial(0, 0, 6, 0, 0, 2);
        gradient.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        gradient.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));

        SvgPaintSampleResult insideFocalCircle = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(0, 0));
        SvgPaintSampleResult middle = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(4, 0));
        SvgPaintSampleResult terminal = new SvgPaintSampler().SampleWithResourceAndPoint(gradient, Point(6, 0));

        Assert.Multiple(() =>
        {
            AssertSample(insideFocalCircle, true, 255, 0, 0, 255);
            Assert.That(insideFocalCircle.Offset, Is.Zero);
            AssertSample(middle, true, 128, 0, 128, 255);
            Assert.That(middle.Offset, Is.EqualTo(0.5).Within(1e-12));
            AssertSample(terminal, true, 0, 0, 255, 255);
            Assert.That(terminal.Offset, Is.EqualTo(1).Within(1e-12));
        });
    }

    [Test]
    public void RadialConesLeaveExteriorTransparentAndAverageTangentRepeats()
    {
        SvgSceneResource cone = Radial(0, 0, 1, 2, 0, 0);
        cone.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        cone.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));
        SvgSceneResource tangent = Radial(0, 0, 1, 1, 0, 0);
        tangent.GradientPaint.SpreadMethod = SvgGradientSpreadMethod.Repeat;
        tangent.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        tangent.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));

        SvgPaintSampleResult exterior = new SvgPaintSampler().SampleWithResourceAndPoint(cone, Point(2, 1));
        SvgPaintSampleResult average = new SvgPaintSampler().SampleWithResourceAndPoint(tangent, Point(2, 0));

        Assert.Multiple(() =>
        {
            AssertSample(exterior, false, 0, 0, 0, 0);
            AssertSample(average, true, 128, 0, 128, 255);
            Assert.That(average.LowerStopIndex, Is.Zero);
            Assert.That(average.UpperStopIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void DegenerateAndUnsupportedPaintsHaveStableOutcomes()
    {
        SvgSceneResource noStops = Linear(0, 0, 10, 0);
        SvgSceneResource oneStop = Linear(0, 0, 10, 0);
        oneStop.GradientPaint.Stops.Add(Stop(0.4, 0, 1, 0, 0.5));
        SvgSceneResource collapsed = Linear(2, 3, 2, 3);
        collapsed.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        collapsed.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));
        SvgSceneResource overlapping = Radial(0, 0, 2, 0, 0, 2);
        overlapping.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        overlapping.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));
        SvgSceneResource zeroRadius = Radial(0, 0, 0, 0, 0, 0);
        zeroRadius.GradientPaint.Stops.Add(Stop(0, 1, 0, 0, 1));
        zeroRadius.GradientPaint.Stops.Add(Stop(1, 0, 0, 1, 1));
        SvgSceneResource singular = Linear(0, 0, 1, 0);
        singular.GradientPaint.Transform = new SvgMatrix(1, 0, 0, 0, 0, 0);
        singular.GradientPaint.Stops.Add(Stop(0, 0, 0, 0, 1));
        singular.GradientPaint.Stops.Add(Stop(1, 1, 1, 1, 1));
        SvgSceneResource pattern = new SvgSceneResource("pattern", SvgSceneResourceKind.PatternPaint);

        Assert.Multiple(() =>
        {
            AssertSample(new SvgPaintSampler().SampleWithResourceAndPoint(noStops, Point(0, 0)), false, 0, 0, 0, 0);
            AssertSample(new SvgPaintSampler().SampleWithResourceAndPoint(oneStop, Point(100, 100)), true, 0, 128, 0, 128);
            AssertSample(new SvgPaintSampler().SampleWithResourceAndPoint(collapsed, Point(-50, 20)), true, 0, 0, 255, 255);
            AssertSample(new SvgPaintSampler().SampleWithResourceAndPoint(overlapping, Point(0, 0)), false, 0, 0, 0, 0);
            AssertSample(new SvgPaintSampler().SampleWithResourceAndPoint(zeroRadius, Point(8, 8)), true, 0, 0, 255, 255);
            AssertFailure(new SvgPaintSampler().SampleWithResourceAndPoint(singular, Point(0, 0)), SvgPaintSampleError.SingularTransform);
            AssertFailure(new SvgPaintSampler().SampleWithResourceAndPoint(pattern, Point(0, 0)), SvgPaintSampleError.InvalidArgument);
        });
    }

    [Test]
    public void SceneGradientRetainsComputedInterpolationSpaceForSampling()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='10' color-interpolation='linearRGB'><stop stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><rect width='10' height='2' fill='url(#g)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgSceneNode rectangle = scene.FindNode(document.DocumentElement.Children[1], "");

        SvgPaintSampleResult result = new SvgPaintSampler().SampleWithResourceAndPoint(rectangle.Fill, Point(5, 0));

        Assert.Multiple(() =>
        {
            Assert.That(rectangle.Fill.Kind, Is.EqualTo(SvgSceneResourceKind.GradientPaint));
            Assert.That(rectangle.Fill.GradientPaint.InterpolationSpace, Is.EqualTo(SvgColorSpace.SrgbLinear));
            AssertSample(result, true, 188, 0, 188, 255);
        });
    }

    static SvgSceneResource Linear(double x1, double y1, double x2, double y2)
    {
        SvgSceneResource resource = new SvgSceneResource("linear-" + x1 + "-" + y1 + "-" + x2 + "-" + y2, SvgSceneResourceKind.GradientPaint);
        resource.GradientPaint.Kind = SvgPaintServerKind.LinearGradient;
        resource.GradientPaint.StartOrCenter = Point(x1, y1);
        resource.GradientPaint.EndOrFocus = Point(x2, y2);
        return resource;
    }

    static SvgSceneResource Radial(double cx, double cy, double radius, double fx, double fy, double focalRadius)
    {
        SvgSceneResource resource = new SvgSceneResource("radial-" + cx + "-" + cy + "-" + radius + "-" + fx + "-" + fy + "-" + focalRadius, SvgSceneResourceKind.GradientPaint);
        resource.GradientPaint.Kind = SvgPaintServerKind.RadialGradient;
        resource.GradientPaint.StartOrCenter = Point(cx, cy);
        resource.GradientPaint.EndOrFocus = Point(fx, fy);
        resource.GradientPaint.Radius = radius;
        resource.GradientPaint.FocalRadius = focalRadius;
        return resource;
    }

    static SvgSceneGradientStopData Stop(double offset, double red, double green, double blue, double alpha)
    {
        SvgSceneGradientStopData stop = new SvgSceneGradientStopData();
        stop.Offset = offset;
        stop.Color = new SvgColor(SvgColorSpace.Srgb, red, green, blue, alpha);
        stop.Alpha = alpha;
        return stop;
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

    static void AssertSample(SvgPaintSampleResult result, bool hasPaint, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPaintSampleError.None));
        Assert.That(result.HasPaint, Is.EqualTo(hasPaint));
        Assert.Multiple(() =>
        {
            Assert.That(result.Pixel.Red, Is.EqualTo(red));
            Assert.That(result.Pixel.Green, Is.EqualTo(green));
            Assert.That(result.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(result.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static void AssertFailure(SvgPaintSampleResult result, SvgPaintSampleError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.HasPaint, Is.False);
        Assert.That(result.Pixel.Alpha, Is.Zero);
    }
}
