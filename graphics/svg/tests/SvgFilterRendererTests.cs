namespace svg.Tests;

public class SvgFilterRendererTests
{
    [Test]
    public void RendersFloodTileAndOffsetAcrossSafelyReusedSurfaces()
    {
        SvgSceneResource filter = BuildFilter(
            "<feFlood x='0' y='0' width='2' height='1' flood-color='red' flood-opacity='.5' result='paint'/>" +
            "<feTile in='paint' x='0' y='0' width='4' height='1' result='tiles'/>" +
            "<feOffset in='tiles' dx='1' dy='0' x='0' y='0' width='4' height='1'/>",
            4,
            1);

        SvgFilterRenderResult result = new SvgFilterRenderer().Render(filter, new SvgFilterRenderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PrimitivesEvaluated, Is.EqualTo(3));
            Assert.That(result.Plan.ReusedSurfaceCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(filter.Filter.Primitives[2].OffsetX, Is.EqualTo(1));
            AssertPixel(result.Output, 0, 0, 0, 0, 0, 0);
            AssertPixel(result.Output, 1, 0, 128, 0, 0, 128);
            AssertPixel(result.Output, 2, 0, 128, 0, 0, 128);
            AssertPixel(result.Output, 3, 0, 128, 0, 0, 128);
        });
    }

    [Test]
    public void AppliesBlendModesAndMergeOrderInThePrimitiveColorSpace()
    {
        SvgSceneResource filter = BuildFilter(
            "<feFlood flood-color='red' flood-opacity='.5' result='red'/>" +
            "<feFlood flood-color='blue' flood-opacity='.5' result='blue'/>" +
            "<feBlend in='red' in2='blue' mode='multiply' result='mixed'/>" +
            "<feMerge><feMergeNode in='mixed'/></feMerge>",
            1,
            1);

        SvgFilterRenderOptions options = new SvgFilterRenderOptions();
        options.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;
        SvgFilterRenderResult result = new SvgFilterRenderer().Render(filter, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Plan.Steps[2].ColorSpace, Is.EqualTo(SvgPixelColorSpace.Srgb));
            AssertPixel(result.Output, 0, 0, 64, 0, 64, 192, 1);
        });
    }

    [Test]
    public void EvaluatesPorterDuffAndArithmeticCompositeChannels()
    {
        SvgSceneResource filter = BuildFilter(
            "<feFlood flood-color='white' flood-opacity='.5' result='mask'/>" +
            "<feComposite in='SourceGraphic' in2='mask' operator='in' result='inside'/>" +
            "<feComposite in='inside' in2='mask' operator='arithmetic' k1='0' k2='.5' k3='.25' k4='0'/>",
            1,
            1);
        SvgPixelBuffer source = Surface(1, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(100, 50, 0, 200));
        SvgFilterRenderOptions options = new SvgFilterRenderOptions();
        options.GraphOptions.SourceGraphic = source;
        options.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;

        SvgFilterRenderResult result = new SvgFilterRenderer().Render(filter, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Plan.Steps[1].Inputs[0].StandardInput, Is.EqualTo(SvgFilterStandardInput.SourceGraphic));
            AssertPixel(result.Output, 0, 0, 57, 45, 32, 82, 1);
        });
    }

    [Test]
    public void ReportsWorkLimitsAndUnsupportedLaterPrimitivesWithoutOutput()
    {
        SvgSceneResource boundedFilter = BuildFilter("<feFlood flood-color='green'/>", 2, 1);
        SvgFilterRenderOptions boundedOptions = new SvgFilterRenderOptions();
        boundedOptions.MaximumPixels = 1;

        SvgFilterRenderResult bounded = new SvgFilterRenderer().Render(boundedFilter, boundedOptions);
        SvgSceneResource unsupportedFilter = BuildFilter("<feFlood/>", 1, 1);
        unsupportedFilter.Filter.Primitives[0].ElementKind = SvgElementKind.Unknown;
        SvgFilterRenderResult unsupported = new SvgFilterRenderer().Render(unsupportedFilter, new SvgFilterRenderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
            Assert.That(bounded.Output, Is.Null);
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Error, Is.EqualTo(SvgFilterRenderError.UnsupportedPrimitive));
            Assert.That(unsupported.Output, Is.Null);
        });
    }

    [Test]
    public void RendersDeterministicTurbulenceWithSeedFrequencyAndOctaves()
    {
        SvgFilterRenderResult first = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='.125' numOctaves='2' seed='7.9'/>", 3, 2), new SvgFilterRenderOptions());
        SvgFilterRenderResult repeated = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='.125 .125' numOctaves='2' seed='7'/>", 3, 2), new SvgFilterRenderOptions());
        SvgFilterRenderResult otherSeed = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='.125' numOctaves='2' seed='8'/>", 3, 2), new SvgFilterRenderOptions());
        SvgFilterRenderResult oneOctave = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='.125' numOctaves='1' seed='7'/>", 3, 2), new SvgFilterRenderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(repeated.Success, Is.True);
            Assert.That(first.PixelsProcessed, Is.EqualTo(48));
            Assert.That(BuffersEqual(first.Output, repeated.Output), Is.True);
            Assert.That(BuffersEqual(first.Output, otherSeed.Output), Is.False);
            Assert.That(BuffersEqual(first.Output, oneOctave.Output), Is.False);
            AssertPixel(first.Output, 0, 0, 0, 3, 3, 23);
            AssertPixel(first.Output, 1, 0, 4, 20, 41, 94);
            AssertPixel(first.Output, 2, 0, 8, 22, 77, 131);
            AssertPixel(first.Output, 0, 1, 1, 5, 3, 29);
            AssertPixel(first.Output, 1, 1, 3, 18, 26, 87);
            AssertPixel(first.Output, 2, 1, 1, 28, 45, 118);
        });
    }

    [Test]
    public void MapsZeroFrequencyForTurbulenceAndFractalNoise()
    {
        SvgFilterRenderResult turbulence = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='0' type='turbulence'/>", 1, 1), new SvgFilterRenderOptions());
        SvgFilterRenderResult fractal = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='0' type='fractalNoise'/>", 1, 1), new SvgFilterRenderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(turbulence.Success, Is.True);
            AssertPixel(turbulence.Output, 0, 0, 0, 0, 0, 0);
            Assert.That(fractal.Success, Is.True);
            AssertPixel(fractal.Output, 0, 0, 64, 64, 64, 128);
        });
    }

    [Test]
    public void StitchesTurbulenceAtPrimitiveSubregionEdges()
    {
        SvgFilterRenderOptions stitchedOptions = new SvgFilterRenderOptions();
        stitchedOptions.GraphOptions.Transform = new SvgMatrix(1, 0, 0, 1, 0.5, 0.5);
        SvgSceneResource stitchedFilter = BuildFilter("<feTurbulence baseFrequency='.13 .17' numOctaves='3' seed='12' stitchTiles='stitch'/>", 8, 8);
        SvgFilterRenderResult stitched = new SvgFilterRenderer().Render(stitchedFilter, stitchedOptions);
        SvgFilterRenderOptions unstitchedOptions = new SvgFilterRenderOptions();
        unstitchedOptions.GraphOptions.Transform = new SvgMatrix(1, 0, 0, 1, 0.5, 0.5);
        SvgSceneResource unstitchedFilter = BuildFilter("<feTurbulence baseFrequency='.13 .17' numOctaves='3' seed='12' stitchTiles='noStitch'/>", 8, 8);
        SvgFilterRenderResult unstitched = new SvgFilterRenderer().Render(unstitchedFilter, unstitchedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(stitched.Success, Is.True);
            Assert.That(PixelsEqual(stitched.Output, 0, 0, 8, 0), Is.True);
            Assert.That(PixelsEqual(stitched.Output, 0, 0, 0, 8), Is.True);
            Assert.That(PixelsEqual(stitched.Output, 0, 0, 8, 8), Is.True);
            Assert.That(unstitched.Success, Is.True);
            Assert.That(PixelsEqual(unstitched.Output, 0, 0, 8, 8), Is.False);
        });
    }

    [Test]
    public void EnforcesTurbulenceOctaveAndWorkLimits()
    {
        SvgSceneResource octaveFilter = BuildFilter("<feTurbulence baseFrequency='.1' numOctaves='3'/>", 1, 1);
        SvgFilterRenderOptions octaveOptions = new SvgFilterRenderOptions();
        octaveOptions.GraphOptions.FilterLimits.MaxTurbulenceOctaves = 2;
        SvgFilterRenderResult octaveLimited = new SvgFilterRenderer().Render(octaveFilter, octaveOptions);
        SvgFilterRenderOptions workOptions = new SvgFilterRenderOptions();
        workOptions.MaximumPixels = 7;
        SvgFilterRenderResult workLimited = new SvgFilterRenderer().Render(BuildFilter("<feTurbulence baseFrequency='.1' numOctaves='2'/>", 1, 1), workOptions);

        Assert.Multiple(() =>
        {
            Assert.That(octaveLimited.Success, Is.False);
            Assert.That(octaveLimited.Error, Is.EqualTo(SvgFilterRenderError.InvalidArgument));
            Assert.That(workLimited.Success, Is.False);
            Assert.That(workLimited.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
        });
    }

    [Test]
    public void RendersDiffuseAndSpecularDistantLighting()
    {
        SvgPixelBuffer bump = Surface(3, 3, SvgPixelColorSpace.Srgb);
        int y = 0;
        while (y < 3)
        {
            int x = 0;
            while (x < 3)
            {
                bump.Write(x, y, new SvgPremultipliedRgba8(0, 0, 0, 255));
                x = x + 1;
            }
            y = y + 1;
        }
        SvgSceneResource diffuseFilter = BuildFilter("<feDiffuseLighting diffuseConstant='.5' lighting-color='red'><feDistantLight elevation='90'/></feDiffuseLighting>", 3, 3);
        SvgSceneResource specularFilter = BuildFilter("<feSpecularLighting specularConstant='.5' specularExponent='32'><feDistantLight elevation='90'/></feSpecularLighting>", 3, 3);

        SvgFilterRenderResult diffuse = RenderWithSource(diffuseFilter, bump);
        SvgFilterRenderResult specular = RenderWithSource(specularFilter, bump);

        Assert.Multiple(() =>
        {
            Assert.That(diffuse.Success, Is.True);
            AssertPixel(diffuse.Output, 0, 0, 128, 0, 0, 255);
            AssertPixel(diffuse.Output, 1, 1, 128, 0, 0, 255);
            AssertPixel(diffuse.Output, 2, 2, 128, 0, 0, 255);
            Assert.That(specular.Success, Is.True);
            AssertPixel(specular.Output, 1, 1, 128, 128, 128, 128);
        });
    }

    [Test]
    public void AppliesPointAndConeLimitedSpotLightsInPrimitiveCoordinates()
    {
        SvgPixelBuffer bump = Surface(3, 3, SvgPixelColorSpace.Srgb);
        SvgSceneResource pointFilter = BuildFilter("<feDiffuseLighting><fePointLight x='1.5' y='1.5' z='1'/></feDiffuseLighting>", 3, 3);
        SvgSceneResource spotFilter = BuildFilter("<feDiffuseLighting><feSpotLight x='1.5' y='1.5' z='2' pointsAtX='1.5' pointsAtY='1.5' pointsAtZ='0' limitingConeAngle='20'/></feDiffuseLighting>", 3, 3);

        SvgFilterRenderResult point = RenderWithSource(pointFilter, bump);
        SvgFilterRenderResult spot = RenderWithSource(spotFilter, bump);

        Assert.Multiple(() =>
        {
            Assert.That(point.Success, Is.True);
            AssertPixel(point.Output, 1, 1, 255, 255, 255, 255);
            AssertPixel(point.Output, 0, 0, 147, 147, 147, 255, 1);
            Assert.That(spot.Success, Is.True);
            AssertPixel(spot.Output, 1, 1, 255, 255, 255, 255);
            AssertPixel(spot.Output, 0, 0, 0, 0, 0, 255);
        });
    }

    [Test]
    public void UsesSurfaceNormalsKernelUnitsAndSpecularExponents()
    {
        SvgPixelBuffer bump = Surface(5, 3, SvgPixelColorSpace.Srgb);
        int y = 0;
        while (y < 3)
        {
            bump.Write(0, y, new SvgPremultipliedRgba8(0, 0, 0, 0));
            bump.Write(1, y, new SvgPremultipliedRgba8(0, 0, 0, 32));
            bump.Write(2, y, new SvgPremultipliedRgba8(0, 0, 0, 255));
            bump.Write(3, y, new SvgPremultipliedRgba8(0, 0, 0, 96));
            bump.Write(4, y, new SvgPremultipliedRgba8(0, 0, 0, 0));
            y = y + 1;
        }
        SvgSceneResource unitFilter = BuildFilter("<feDiffuseLighting surfaceScale='4'><feDistantLight azimuth='180' elevation='45'/></feDiffuseLighting>", 5, 3);
        SvgSceneResource wideFilter = BuildFilter("<feDiffuseLighting surfaceScale='4' kernelUnitLength='2 1'><feDistantLight azimuth='180' elevation='45'/></feDiffuseLighting>", 5, 3);
        SvgPixelBuffer flat = Surface(1, 1, SvgPixelColorSpace.Srgb);
        SvgSceneResource broadSpecular = BuildFilter("<feSpecularLighting specularExponent='1'><feDistantLight elevation='45'/></feSpecularLighting>", 1, 1);
        SvgSceneResource sharpSpecular = BuildFilter("<feSpecularLighting specularExponent='16'><feDistantLight elevation='45'/></feSpecularLighting>", 1, 1);

        SvgFilterRenderResult unit = RenderWithSource(unitFilter, bump);
        SvgFilterRenderResult wide = RenderWithSource(wideFilter, bump);
        SvgFilterRenderResult broad = RenderWithSource(broadSpecular, flat);
        SvgFilterRenderResult sharp = RenderWithSource(sharpSpecular, flat);

        Assert.Multiple(() =>
        {
            Assert.That(unit.Success, Is.True);
            Assert.That(wide.Success, Is.True);
            Assert.That(BuffersEqual(unit.Output, wide.Output), Is.False);
            Assert.That(broad.Success, Is.True);
            Assert.That(sharp.Success, Is.True);
            Assert.That(broad.Output.Read(0, 0).Pixel.Alpha, Is.GreaterThan(sharp.Output.Read(0, 0).Pixel.Alpha));
        });
    }

    [Test]
    public void ConvertsLightingColorInThePrimitiveColorSpace()
    {
        SvgPixelBuffer bump = Surface(1, 1, SvgPixelColorSpace.Srgb);
        SvgSceneResource srgbFilter = BuildFilter("<feDiffuseLighting lighting-color='#808080'><feDistantLight elevation='90'/></feDiffuseLighting>", 1, 1);
        SvgSceneResource linearFilter = BuildFilter("<feDiffuseLighting style='color-interpolation-filters:linearRGB;lighting-color:#808080'><feDistantLight elevation='90'/></feDiffuseLighting>", 1, 1);

        SvgFilterRenderResult srgb = RenderWithSource(srgbFilter, bump);
        SvgFilterRenderResult linear = RenderWithSource(linearFilter, bump);

        Assert.Multiple(() =>
        {
            Assert.That(srgb.Success, Is.True);
            AssertPixel(srgb.Output, 0, 0, 128, 128, 128, 255);
            Assert.That(linear.Success, Is.True);
            Assert.That(linear.Output.ColorSpace, Is.EqualTo(SvgPixelColorSpace.SrgbLinear));
            AssertPixel(linear.Output, 0, 0, 55, 55, 55, 255, 1);
        });
    }

    [Test]
    public void RejectsInvalidLightingAndBoundsItsSurfaceWork()
    {
        SvgPixelBuffer bump = Surface(1, 1, SvgPixelColorSpace.Srgb);
        SvgFilterRenderResult missingLight = RenderWithSource(BuildFilter("<feDiffuseLighting/>", 1, 1), bump);
        SvgFilterRenderResult invalidExponent = RenderWithSource(BuildFilter("<feSpecularLighting specularExponent='129'><feDistantLight/></feSpecularLighting>", 1, 1), bump);
        SvgFilterRenderOptions limitedOptions = new SvgFilterRenderOptions();
        limitedOptions.GraphOptions.SourceGraphic = bump;
        limitedOptions.MaximumPixels = 8;
        SvgFilterRenderResult limited = new SvgFilterRenderer().Render(BuildFilter("<feDiffuseLighting><feDistantLight elevation='90'/></feDiffuseLighting>", 1, 1), limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(missingLight.Success, Is.False);
            Assert.That(missingLight.Error, Is.EqualTo(SvgFilterRenderError.InvalidArgument));
            Assert.That(invalidExponent.Success, Is.False);
            Assert.That(invalidExponent.Error, Is.EqualTo(SvgFilterRenderError.InvalidArgument));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
        });
    }

    [Test]
    public void AppliesFullColorMatricesToStraightChannelsBeforePremultiplying()
    {
        SvgSceneResource filter = BuildFilter(
            "<feColorMatrix type='matrix' values='0 0 1 0 0  0 1 0 0 0  1 0 0 0 0  0 0 0 .5 0'/>",
            1,
            1);
        SvgPixelBuffer source = Surface(1, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(100, 50, 25, 200));
        SvgFilterRenderOptions options = new SvgFilterRenderOptions();
        options.GraphOptions.SourceGraphic = source;
        options.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;

        SvgFilterRenderResult result = new SvgFilterRenderer().Render(filter, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(filter.Filter.Primitives[0].Attributes.Single(item => item.Name == SvgAttributeNames.Values).TypedValue.ValueKind, Is.EqualTo(SvgTypedAttributeKind.NumberList));
            AssertPixel(result.Output, 0, 0, 13, 25, 50, 100, 1);
        });
    }

    [Test]
    public void SupportsColorMatrixConvenienceModesAndIdentityDefault()
    {
        SvgPixelBuffer source = Surface(1, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(255, 0, 0, 255));

        SvgFilterRenderResult identity = RenderWithSource(BuildFilter("<feColorMatrix/>", 1, 1), source);
        SvgFilterRenderResult saturated = RenderWithSource(BuildFilter("<feColorMatrix type='saturate' values='0'/>", 1, 1), source);
        SvgFilterRenderResult rotated = RenderWithSource(BuildFilter("<feColorMatrix type='hueRotate' values='0'/>", 1, 1), source);
        SvgFilterRenderResult luminance = RenderWithSource(BuildFilter("<feColorMatrix type='luminanceToAlpha'/>", 1, 1), source);

        Assert.Multiple(() =>
        {
            Assert.That(identity.Success, Is.True);
            AssertPixel(identity.Output, 0, 0, 255, 0, 0, 255);
            AssertPixel(saturated.Output, 0, 0, 54, 54, 54, 255, 1);
            AssertPixel(rotated.Output, 0, 0, 255, 0, 0, 255, 1);
            AssertPixel(luminance.Output, 0, 0, 0, 0, 0, 54, 1);
        });
    }

    [Test]
    public void EvaluatesEveryComponentTransferFunctionAndClampsChannels()
    {
        SvgSceneResource filter = BuildFilter(
            "<feComponentTransfer>" +
            "<feFuncR type='table' tableValues='0 1'/>" +
            "<feFuncG type='discrete' tableValues='0 2'/>" +
            "<feFuncB type='linear' slope='.5' intercept='.1'/>" +
            "<feFuncA type='gamma' amplitude='.5' exponent='1' offset='.25'/>" +
            "</feComponentTransfer>",
            1,
            1);
        SvgPixelBuffer source = Surface(1, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(64, 128, 192, 255));

        SvgFilterRenderResult result = RenderWithSource(filter, source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(filter.Filter.Primitives[0].Children, Has.Count.EqualTo(4));
            AssertPixel(result.Output, 0, 0, 48, 191, 91, 191, 1);
        });
    }

    [Test]
    public void UsesIdentityForMissingOrEmptyTransferFunctionsAndBoundsTables()
    {
        SvgPixelBuffer source = Surface(1, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(20, 40, 60, 100));
        SvgFilterRenderResult missing = RenderWithSource(BuildFilter("<feComponentTransfer/>", 1, 1), source);
        SvgFilterRenderResult empty = RenderWithSource(BuildFilter("<feComponentTransfer><feFuncR type='table'/></feComponentTransfer>", 1, 1), source);
        SvgSceneResource oversizedFilter = BuildFilter("<feComponentTransfer><feFuncR type='table' tableValues='0 .25 .5 .75 1'/></feComponentTransfer>", 1, 1);
        SvgFilterRenderOptions limited = new SvgFilterRenderOptions();
        limited.GraphOptions.SourceGraphic = source;
        limited.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;
        limited.MaximumTableValues = 4;

        SvgFilterRenderResult oversized = new SvgFilterRenderer().Render(oversizedFilter, limited);
        SvgFilterRenderResult invalidMatrix = RenderWithSource(BuildFilter("<feColorMatrix values='1 0'/>", 1, 1), source);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            AssertPixel(missing.Output, 0, 0, 20, 40, 60, 100, 1);
            Assert.That(empty.Success, Is.True);
            AssertPixel(empty.Output, 0, 0, 20, 40, 60, 100, 1);
            Assert.That(oversized.Success, Is.False);
            Assert.That(oversized.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
            Assert.That(oversized.Output, Is.Null);
            Assert.That(invalidMatrix.Success, Is.False);
            Assert.That(invalidMatrix.Error, Is.EqualTo(SvgFilterRenderError.InvalidArgument));
            Assert.That(invalidMatrix.Output, Is.Null);
        });
    }

    [Test]
    public void AppliesSeparableGaussianBlurAndPreservesZeroDeviation()
    {
        SvgPixelBuffer source = Surface(5, 1, SvgPixelColorSpace.Srgb);
        source.Write(2, 0, new SvgPremultipliedRgba8(255, 255, 255, 255));

        SvgFilterRenderResult blurred = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='1 0'/>", 5, 1), source);
        SvgFilterRenderResult identity = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='0'/>", 5, 1), source);

        Assert.Multiple(() =>
        {
            Assert.That(blurred.Success, Is.True);
            AssertPixel(blurred.Output, 0, 0, 14, 14, 14, 14, 1);
            AssertPixel(blurred.Output, 1, 0, 62, 62, 62, 62, 1);
            AssertPixel(blurred.Output, 2, 0, 102, 102, 102, 102, 1);
            AssertPixel(blurred.Output, 3, 0, 62, 62, 62, 62, 1);
            AssertPixel(blurred.Output, 4, 0, 14, 14, 14, 14, 1);
            Assert.That(identity.Success, Is.True);
            AssertPixel(identity.Output, 2, 0, 255, 255, 255, 255);
            AssertPixel(identity.Output, 1, 0, 0, 0, 0, 0);
        });
    }

    [Test]
    public void AppliesGaussianBlurEdgeModesAndDefaultsToTransparentEdges()
    {
        SvgPixelBuffer source = Surface(3, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(255, 0, 0, 255));

        SvgFilterRenderResult defaultMode = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='1 0'/>", 3, 1), source);
        SvgFilterRenderResult none = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='1 0' edgeMode='none'/>", 3, 1), source);
        SvgFilterRenderResult duplicate = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='1 0' edgeMode='duplicate'/>", 3, 1), source);
        SvgFilterRenderResult wrap = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='1 0' edgeMode='wrap'/>", 3, 1), source);

        Assert.Multiple(() =>
        {
            Assert.That(defaultMode.Success, Is.True);
            Assert.That(none.Success, Is.True);
            Assert.That(duplicate.Success, Is.True);
            Assert.That(wrap.Success, Is.True);
            AssertPixel(defaultMode.Output, 0, 0, 102, 0, 0, 102, 1);
            AssertPixel(none.Output, 0, 0, 102, 0, 0, 102, 1);
            AssertPixel(duplicate.Output, 0, 0, 179, 0, 0, 179, 1);
            AssertPixel(wrap.Output, 0, 0, 104, 0, 0, 104, 1);
        });
    }

    [Test]
    public void ExpandsDropShadowAndCompositesTheSourceOverIt()
    {
        SvgPixelBuffer source = Surface(5, 1, SvgPixelColorSpace.Srgb);
        source.Write(1, 0, new SvgPremultipliedRgba8(255, 0, 0, 255));
        SvgSceneResource filter = BuildFilter("<feDropShadow dx='2' dy='0' stdDeviation='0' flood-color='blue' flood-opacity='.5'/>", 5, 1);

        SvgFilterRenderResult result = RenderWithSource(filter, source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PrimitivesEvaluated, Is.EqualTo(1));
            AssertPixel(result.Output, 1, 0, 255, 0, 0, 255);
            AssertPixel(result.Output, 3, 0, 0, 0, 128, 128);
            AssertPixel(result.Output, 0, 0, 0, 0, 0, 0);
        });
    }

    [Test]
    public void BoundsFilterKernelsAndRejectsUnknownEdgeModes()
    {
        SvgPixelBuffer source = Surface(1, 1, SvgPixelColorSpace.Srgb);
        SvgSceneResource blurFilter = BuildFilter("<feGaussianBlur stdDeviation='1'/>", 1, 1);
        SvgFilterRenderOptions limited = new SvgFilterRenderOptions();
        limited.GraphOptions.SourceGraphic = source;
        limited.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;
        limited.GraphOptions.FilterLimits.MaxKernelElements = 5;

        SvgFilterRenderResult bounded = new SvgFilterRenderer().Render(blurFilter, limited);
        SvgSceneResource convolutionFilter = BuildFilter("<feConvolveMatrix order='3' kernelMatrix='1 1 1 1 1 1 1 1 1'/>", 1, 1);
        SvgFilterRenderResult boundedConvolution = new SvgFilterRenderer().Render(convolutionFilter, limited);
        SvgSceneResource morphologyFilter = BuildFilter("<feMorphology radius='2'/>", 1, 1);
        SvgFilterRenderResult boundedMorphology = new SvgFilterRenderer().Render(morphologyFilter, limited);
        SvgFilterRenderResult invalid = RenderWithSource(BuildFilter("<feGaussianBlur stdDeviation='1' edgeMode='reflect'/>", 1, 1), source);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
            Assert.That(bounded.Output, Is.Null);
            Assert.That(boundedConvolution.Success, Is.False);
            Assert.That(boundedConvolution.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
            Assert.That(boundedMorphology.Success, Is.False);
            Assert.That(boundedMorphology.Error, Is.EqualTo(SvgFilterRenderError.WorkLimitExceeded));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgFilterRenderError.InvalidArgument));
            Assert.That(invalid.Output, Is.Null);
        });
    }

    [Test]
    public void ConvolvesWithReversedKernelsEdgeDuplicationAndOptionalAlphaPreservation()
    {
        SvgPixelBuffer source = Surface(3, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(10, 0, 0, 255));
        source.Write(1, 0, new SvgPremultipliedRgba8(20, 0, 0, 255));
        source.Write(2, 0, new SvgPremultipliedRgba8(30, 0, 0, 255));
        SvgFilterRenderResult shifted = RenderWithSource(BuildFilter("<feConvolveMatrix order='3 1' kernelMatrix='1 0 0' divisor='1' targetX='1' targetY='0' edgeMode='duplicate' preserveAlpha='true'/>", 3, 1), source);
        SvgFilterRenderResult none = RenderWithSource(BuildFilter("<feConvolveMatrix order='3 1' kernelMatrix='1 1 1' divisor='1' targetX='1' targetY='0' edgeMode='none'/>", 3, 1), source);
        SvgFilterRenderResult duplicate = RenderWithSource(BuildFilter("<feConvolveMatrix order='3 1' kernelMatrix='1 1 1' divisor='1' targetX='1' targetY='0' edgeMode='duplicate'/>", 3, 1), source);
        SvgFilterRenderResult wrap = RenderWithSource(BuildFilter("<feConvolveMatrix order='3 1' kernelMatrix='1 1 1' divisor='1' targetX='1' targetY='0' edgeMode='wrap'/>", 3, 1), source);

        SvgPixelBuffer translucent = Surface(1, 1, SvgPixelColorSpace.Srgb);
        translucent.Write(0, 0, new SvgPremultipliedRgba8(100, 50, 0, 128));
        SvgFilterRenderResult convolvedAlpha = RenderWithSource(BuildFilter("<feConvolveMatrix order='1' kernelMatrix='1' divisor='2'/>", 1, 1), translucent);
        SvgFilterRenderResult preservedAlpha = RenderWithSource(BuildFilter("<feConvolveMatrix order='1' kernelMatrix='1' divisor='2' preserveAlpha='true'/>", 1, 1), translucent);

        Assert.Multiple(() =>
        {
            Assert.That(shifted.Success, Is.True);
            AssertPixel(shifted.Output, 0, 0, 20, 0, 0, 255);
            AssertPixel(shifted.Output, 1, 0, 30, 0, 0, 255);
            AssertPixel(shifted.Output, 2, 0, 30, 0, 0, 255);
            AssertPixel(none.Output, 0, 0, 30, 0, 0, 255);
            AssertPixel(duplicate.Output, 0, 0, 40, 0, 0, 255);
            AssertPixel(wrap.Output, 0, 0, 60, 0, 0, 255);
            Assert.That(convolvedAlpha.Success, Is.True);
            AssertPixel(convolvedAlpha.Output, 0, 0, 50, 25, 0, 64);
            Assert.That(preservedAlpha.Success, Is.True);
            AssertPixel(preservedAlpha.Output, 0, 0, 50, 25, 0, 128, 1);
        });
    }

    [Test]
    public void AppliesConvolutionKernelUnitsDefaultDivisorAndBias()
    {
        SvgPixelBuffer source = Surface(5, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(10, 0, 0, 255));
        source.Write(1, 0, new SvgPremultipliedRgba8(20, 0, 0, 255));
        source.Write(2, 0, new SvgPremultipliedRgba8(30, 0, 0, 255));
        source.Write(3, 0, new SvgPremultipliedRgba8(40, 0, 0, 255));
        source.Write(4, 0, new SvgPremultipliedRgba8(50, 0, 0, 255));
        SvgFilterRenderResult spaced = RenderWithSource(BuildFilter("<feConvolveMatrix order='3 1' kernelMatrix='1 0 0' targetX='1' targetY='0' kernelUnitLength='2' preserveAlpha='true'/>", 5, 1), source);

        SvgPixelBuffer translucent = Surface(1, 1, SvgPixelColorSpace.Srgb);
        translucent.Write(0, 0, new SvgPremultipliedRgba8(0, 0, 0, 128));
        SvgFilterRenderResult biased = RenderWithSource(BuildFilter("<feConvolveMatrix order='1' kernelMatrix='0' bias='.5'/>", 1, 1), translucent);

        Assert.Multiple(() =>
        {
            Assert.That(spaced.Success, Is.True);
            AssertPixel(spaced.Output, 1, 0, 40, 0, 0, 255);
            Assert.That(biased.Success, Is.True);
            AssertPixel(biased.Output, 0, 0, 64, 64, 64, 128, 1);
        });
    }

    [Test]
    public void AppliesBoundedMorphologyErosionAndDilation()
    {
        SvgPixelBuffer impulse = Surface(5, 3, SvgPixelColorSpace.Srgb);
        impulse.Write(2, 1, new SvgPremultipliedRgba8(255, 0, 0, 255));
        SvgFilterRenderResult dilated = RenderWithSource(BuildFilter("<feMorphology operator='dilate' radius='1'/>", 5, 3), impulse);

        SvgPixelBuffer solid = Surface(5, 3, SvgPixelColorSpace.Srgb);
        solid.ClearWithPixel(new SvgPremultipliedRgba8(0, 255, 0, 255));
        SvgFilterRenderResult eroded = RenderWithSource(BuildFilter("<feMorphology operator='erode' radius='1'/>", 5, 3), solid);
        SvgFilterRenderResult disabled = RenderWithSource(BuildFilter("<feMorphology operator='dilate' radius='1 0'/>", 5, 3), impulse);

        Assert.Multiple(() =>
        {
            Assert.That(dilated.Success, Is.True);
            AssertPixel(dilated.Output, 0, 1, 0, 0, 0, 0);
            AssertPixel(dilated.Output, 1, 0, 255, 0, 0, 255);
            AssertPixel(dilated.Output, 2, 1, 255, 0, 0, 255);
            AssertPixel(dilated.Output, 3, 2, 255, 0, 0, 255);
            Assert.That(eroded.Success, Is.True);
            AssertPixel(eroded.Output, 0, 1, 0, 0, 0, 0);
            AssertPixel(eroded.Output, 1, 1, 0, 255, 0, 255);
            AssertPixel(eroded.Output, 3, 1, 0, 255, 0, 255);
            AssertPixel(eroded.Output, 4, 1, 0, 0, 0, 0);
            AssertPixel(eroded.Output, 2, 0, 0, 0, 0, 0);
            Assert.That(disabled.Success, Is.True);
            AssertPixel(disabled.Output, 2, 1, 255, 0, 0, 255);
            AssertPixel(disabled.Output, 1, 1, 0, 0, 0, 0);
        });
    }

    [Test]
    public void DisplacesTheFirstInputUsingStraightChannelsFromTheSecondInput()
    {
        SvgPixelBuffer source = Surface(3, 1, SvgPixelColorSpace.Srgb);
        source.Write(0, 0, new SvgPremultipliedRgba8(255, 0, 0, 255));
        source.Write(1, 0, new SvgPremultipliedRgba8(0, 255, 0, 255));
        source.Write(2, 0, new SvgPremultipliedRgba8(0, 0, 255, 255));
        SvgPixelBuffer map = Surface(3, 1, SvgPixelColorSpace.Srgb);
        map.ClearWithPixel(new SvgPremultipliedRgba8(128, 64, 0, 128));
        SvgSceneResource filter = BuildFilter("<feDisplacementMap in='SourceGraphic' in2='BackgroundImage' scale='2' xChannelSelector='R' yChannelSelector='G'/>", 3, 1);
        SvgFilterRenderOptions options = new SvgFilterRenderOptions();
        options.GraphOptions.SourceGraphic = source;
        options.GraphOptions.BackgroundImage = map;
        options.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;

        SvgFilterRenderResult result = new SvgFilterRenderer().Render(filter, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            AssertPixel(result.Output, 0, 0, 0, 255, 0, 255, 1);
            AssertPixel(result.Output, 1, 0, 0, 0, 255, 255, 1);
            AssertPixel(result.Output, 2, 0, 0, 0, 0, 0, 1);
        });
    }

    [Test]
    public void MapsDecodedFeImagePixelsAndLeavesUnavailableResourcesTransparent()
    {
        SvgSceneResource imageFilter = BuildFilter("<feImage preserveAspectRatio='none'/>", 4, 1);
        SvgSceneResource image = new SvgSceneResource("filter-image", SvgSceneResourceKind.Image);
        image.Image.DecodedPixels = Surface(2, 1, SvgPixelColorSpace.Srgb);
        image.Image.DecodedContentAvailable = true;
        image.Image.DecodedPixels.Write(0, 0, new SvgPremultipliedRgba8(255, 0, 0, 255));
        image.Image.DecodedPixels.Write(1, 0, new SvgPremultipliedRgba8(0, 0, 255, 255));
        imageFilter.Filter.Primitives[0].ImageResource = image;
        SvgSceneResource meetFilter = BuildFilter("<feImage/>", 4, 4);
        meetFilter.Filter.Primitives[0].ImageResource = image;
        SvgSceneResource missingFilter = BuildFilter("<feImage/>", 1, 1);
        SvgSceneResource blockedFilter = BuildFilter("<feImage href='https://example.test/image.png'/>", 1, 1);

        SvgFilterRenderResult mapped = new SvgFilterRenderer().Render(imageFilter, new SvgFilterRenderOptions());
        SvgFilterRenderResult met = new SvgFilterRenderer().Render(meetFilter, new SvgFilterRenderOptions());
        SvgFilterRenderResult missing = new SvgFilterRenderer().Render(missingFilter, new SvgFilterRenderOptions());
        SvgFilterRenderResult blocked = new SvgFilterRenderer().Render(blockedFilter, new SvgFilterRenderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True);
            AssertPixel(mapped.Output, 0, 0, 255, 0, 0, 255);
            AssertPixel(mapped.Output, 1, 0, 191, 0, 64, 255, 1);
            AssertPixel(mapped.Output, 2, 0, 64, 0, 191, 255, 1);
            AssertPixel(mapped.Output, 3, 0, 0, 0, 255, 255);
            Assert.That(met.Success, Is.True);
            AssertPixel(met.Output, 0, 0, 0, 0, 0, 0);
            AssertPixel(met.Output, 0, 1, 255, 0, 0, 255);
            AssertPixel(met.Output, 3, 2, 0, 0, 255, 255);
            AssertPixel(met.Output, 3, 3, 0, 0, 0, 0);
            Assert.That(missing.Success, Is.True);
            AssertPixel(missing.Output, 0, 0, 0, 0, 0, 0);
            Assert.That(blocked.Success, Is.True);
            Assert.That(blockedFilter.Filter.Primitives[0].ImageResource, Is.Null);
            AssertPixel(blocked.Output, 0, 0, 0, 0, 0, 0);
        });
    }

    [Test]
    public void RetainsPolicyResolvedFeImageResourcesForDeferredDecoding()
    {
        string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZK8sAAAAASUVORK5CYII=";
        SvgSceneResource filter = BuildFilter("<feImage href='data:image/png;base64," + png + "'/>", 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(filter.Filter.Primitives[0].ImageResource, Is.Not.Null);
            Assert.That(filter.Filter.Primitives[0].ImageResource.Image.Uri, Does.StartWith("data:image/png"));
            Assert.That(filter.Filter.Primitives[0].ImageResource.Image.Bytes, Is.Not.Empty);
            Assert.That(filter.Filter.Primitives[0].ImageResource.Image.IntrinsicSize.Width, Is.EqualTo(1));
        });
    }

    static SvgSceneResource BuildFilter(string primitives, int width, int height)
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='" + width + "' height='" + height + "'><defs>" +
            "<filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='" + width + "' height='" + height + "' style='color-interpolation-filters:sRGB'>" +
            primitives + "</filter></defs><rect width='" + width + "' height='" + height + "' filter='url(#f)'/></svg>";
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(resolved.Success, Is.True);
        SvgSceneBuildResult built = resolved.BuildScene();
        Assert.That(built.Success, Is.True);
        SvgSceneNode target = built.Scene.FindNode(loaded.Document.DocumentElement.Children[1], "");
        Assert.That(target.Filter, Is.Not.Null);
        return target.Filter;
    }

    static SvgPixelBuffer Surface(int width, int height, SvgPixelColorSpace colorSpace)
    {
        SvgPixelBufferResult result = SvgPixelBuffer.Create(width, height, colorSpace);
        Assert.That(result.Success, Is.True);
        return result.Buffer;
    }

    static SvgFilterRenderResult RenderWithSource(SvgSceneResource filter, SvgPixelBuffer source)
    {
        SvgFilterRenderOptions options = new SvgFilterRenderOptions();
        options.GraphOptions.SourceGraphic = source;
        options.GraphOptions.DefaultColorSpace = SvgPixelColorSpace.Srgb;
        return new SvgFilterRenderer().Render(filter, options);
    }

    static bool BuffersEqual(SvgPixelBuffer first, SvgPixelBuffer second)
    {
        if (first == null || second == null || first.Width != second.Width || first.Height != second.Height) return false;
        int y = 0;
        while (y < first.Height)
        {
            int x = 0;
            while (x < first.Width)
            {
                if (!PixelsEqual(first, x, y, second, x, y)) return false;
                x = x + 1;
            }
            y = y + 1;
        }
        return true;
    }

    static bool PixelsEqual(SvgPixelBuffer buffer, int firstX, int firstY, int secondX, int secondY)
    {
        return PixelsEqual(buffer, firstX, firstY, buffer, secondX, secondY);
    }

    static bool PixelsEqual(SvgPixelBuffer first, int firstX, int firstY, SvgPixelBuffer second, int secondX, int secondY)
    {
        SvgPixelReadResult firstPixel = first.Read(firstX, firstY);
        SvgPixelReadResult secondPixel = second.Read(secondX, secondY);
        Assert.That(firstPixel.Success, Is.True);
        Assert.That(secondPixel.Success, Is.True);
        return firstPixel.Pixel.Red == secondPixel.Pixel.Red && firstPixel.Pixel.Green == secondPixel.Pixel.Green && firstPixel.Pixel.Blue == secondPixel.Pixel.Blue && firstPixel.Pixel.Alpha == secondPixel.Pixel.Alpha;
    }

    static void AssertPixel(SvgPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        AssertPixel(buffer, x, y, red, green, blue, alpha, 0);
    }

    static void AssertPixel(SvgPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha, int tolerance)
    {
        SvgPixelReadResult result = buffer.Read(x, y);
        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That((int)result.Pixel.Red, Is.EqualTo((int)red).Within(tolerance));
            Assert.That((int)result.Pixel.Green, Is.EqualTo((int)green).Within(tolerance));
            Assert.That((int)result.Pixel.Blue, Is.EqualTo((int)blue).Within(tolerance));
            Assert.That((int)result.Pixel.Alpha, Is.EqualTo((int)alpha).Within(tolerance));
        });
    }
}
