namespace svg.Tests;

public class SvgPathRasterizerTests
{
    [Test]
    public void PaintsTransformedFillWithComponentOpacity()
    {
        SvgPixelBuffer surface = Surface(6, 5);
        SvgPathRasterOptions options = new SvgPathRasterOptions();
        options.FillColor = new SvgPremultipliedRgba8(200, 100, 40, 200);
        options.FillOpacity = 0.5;
        options.RasterOptions.Transform = new SvgMatrix(1, 0, 0, 1, 2, 1);

        SvgPathRasterResult result = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(Parse("M0 0 H2 V2 H0 Z"), surface, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 1);
            Assert.That(result.FillResult, Is.Not.Null);
            Assert.That(result.StrokeResult, Is.Null);
            Assert.That(result.FillResult.Bounds.X, Is.EqualTo(2));
            Assert.That(result.FillResult.Bounds.Y, Is.EqualTo(1));
            AssertPixel(surface, 2, 1, 100, 50, 20, 100);
            AssertPixel(surface, 1, 1, 0, 0, 0, 0);
        });
    }

    [Test]
    public void PaintOrderControlsTheColorWhereFillAndStrokeOverlap()
    {
        SvgPathData rectangle = Parse("M2 2 H6 V6 H2 Z");
        SvgPathRasterOptions fillThenStroke = Colors();
        fillThenStroke.StrokeStyle.Width = 2;
        fillThenStroke.ShapeRendering = SvgShapeRendering.CrispEdges;
        SvgPathRasterOptions strokeThenFill = Colors();
        strokeThenFill.StrokeStyle.Width = 2;
        strokeThenFill.ShapeRendering = SvgShapeRendering.CrispEdges;
        strokeThenFill.PaintOrder.Clear();
        strokeThenFill.PaintOrder.Add(SvgPaintOrderComponent.Stroke);
        strokeThenFill.PaintOrder.Add(SvgPaintOrderComponent.Fill);
        strokeThenFill.PaintOrder.Add(SvgPaintOrderComponent.Markers);
        SvgPixelBuffer first = Surface(8, 8);
        SvgPixelBuffer second = Surface(8, 8);

        SvgPathRasterResult firstResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(rectangle, first, fillThenStroke);
        SvgPathRasterResult secondResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(rectangle, second, strokeThenFill);

        Assert.Multiple(() =>
        {
            AssertSuccess(firstResult, 2);
            AssertSuccess(secondResult, 2);
            AssertPixel(first, 2, 2, 0, 0, 255, 255);
            AssertPixel(second, 2, 2, 255, 0, 0, 255);
            AssertPixel(first, 4, 4, 255, 0, 0, 255);
            AssertPixel(second, 4, 4, 255, 0, 0, 255);
        });
    }

    [Test]
    public void OrdinaryAndNonScalingStrokeUseDifferentTransformSpaces()
    {
        SvgPathData line = Parse("M1 2 H4");
        SvgPathRasterOptions ordinary = StrokeOnly();
        ordinary.StrokeStyle.Width = 2;
        ordinary.ShapeRendering = SvgShapeRendering.CrispEdges;
        ordinary.RasterOptions.Transform = new SvgMatrix(2, 0, 0, 3, 0, 0);
        SvgPathRasterOptions nonScaling = StrokeOnly();
        nonScaling.StrokeStyle.Width = 2;
        nonScaling.ShapeRendering = SvgShapeRendering.CrispEdges;
        nonScaling.RasterOptions.Transform = new SvgMatrix(2, 0, 0, 3, 0, 0);
        nonScaling.VectorEffect = SvgStrokeVectorEffect.NonScalingStroke;
        SvgPixelBuffer ordinarySurface = Surface(10, 10);
        SvgPixelBuffer nonScalingSurface = Surface(10, 10);

        SvgPathRasterResult ordinaryResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(line, ordinarySurface, ordinary);
        SvgPathRasterResult nonScalingResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(line, nonScalingSurface, nonScaling);

        Assert.Multiple(() =>
        {
            AssertSuccess(ordinaryResult, 1);
            AssertSuccess(nonScalingResult, 1);
            AssertPixel(ordinarySurface, 4, 3, 0, 0, 255, 255);
            AssertPixel(nonScalingSurface, 4, 3, 0, 0, 0, 0);
            AssertPixel(ordinarySurface, 4, 5, 0, 0, 255, 255);
            AssertPixel(nonScalingSurface, 4, 5, 0, 0, 255, 255);
            Assert.That(ordinaryResult.StrokeResult.Bounds.Height, Is.EqualTo(6));
            Assert.That(nonScalingResult.StrokeResult.Bounds.Height, Is.EqualTo(2));
        });
    }

    [Test]
    public void DashedStrokeOutlinesRetainPaintedGaps()
    {
        SvgPathRasterOptions options = StrokeOnly();
        options.StrokeStyle.Width = 2;
        options.StrokeStyle.DashArray.Add(2);
        options.StrokeStyle.DashArray.Add(2);
        options.ShapeRendering = SvgShapeRendering.CrispEdges;
        SvgPixelBuffer surface = Surface(8, 4);

        SvgPathRasterResult result = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(Parse("M0 2 H8"), surface, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 1);
            AssertPixel(surface, 0, 1, 0, 0, 255, 255);
            AssertPixel(surface, 1, 2, 0, 0, 255, 255);
            AssertPixel(surface, 2, 1, 0, 0, 0, 0);
            AssertPixel(surface, 3, 2, 0, 0, 0, 0);
            AssertPixel(surface, 4, 1, 0, 0, 255, 255);
        });
    }

    [Test]
    public void ShapeRenderingHintSelectsDeterministicCoverageProfile()
    {
        SvgPathData fractional = Parse("M0.25 0.25 H1.25 V1.25 H0.25 Z");
        SvgPathRasterOptions automatic = new SvgPathRasterOptions();
        automatic.FillColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        SvgPathRasterOptions crisp = new SvgPathRasterOptions();
        crisp.FillColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        crisp.ShapeRendering = SvgShapeRendering.CrispEdges;
        SvgPathRasterOptions precise = new SvgPathRasterOptions();
        precise.FillColor = new SvgPremultipliedRgba8(255, 255, 255, 255);
        precise.ShapeRendering = SvgShapeRendering.GeometricPrecision;
        SvgPixelBuffer automaticSurface = Surface(2, 2);
        SvgPixelBuffer crispSurface = Surface(2, 2);
        SvgPixelBuffer preciseSurface = Surface(2, 2);

        SvgPathRasterResult automaticResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(fractional, automaticSurface, automatic);
        SvgPathRasterResult crispResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(fractional, crispSurface, crisp);
        SvgPathRasterResult preciseResult = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(fractional, preciseSurface, precise);

        Assert.Multiple(() =>
        {
            AssertSuccess(automaticResult, 1);
            AssertSuccess(crispResult, 1);
            AssertSuccess(preciseResult, 1);
            Assert.That(automaticSurface.Read(0, 0).Pixel.Alpha, Is.EqualTo(143));
            Assert.That(crispSurface.Read(0, 0).Pixel.Alpha, Is.EqualTo(255));
            Assert.That(preciseSurface.Read(0, 0).Pixel.Alpha, Is.EqualTo(143));
            Assert.That(crispResult.WorkUnits, Is.EqualTo(automaticResult.WorkUnits / 16));
            Assert.That(preciseResult.WorkUnits, Is.EqualTo(automaticResult.WorkUnits * 4));
        });
    }

    [Test]
    public void ScenePathsRetainComputedStrokeHintOpacityOrderAndTransform()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><path d='M0 1 H3' transform='translate(2 2)' fill='none' stroke='blue' stroke-width='2' stroke-opacity='.5' vector-effect='non-scaling-stroke' shape-rendering='crispEdges' paint-order='stroke fill markers'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode node = built.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgPixelBuffer surface = Surface(8, 6);

        SvgPathRasterResult result = new SvgPathRasterizer().DrawWithNodeAndSurfaceAndFillColorAndStrokeColorAndRasterOptions(node, surface, null, new SvgPremultipliedRgba8(0, 0, 255, 255), new SvgRasterOptions());

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(node.Path.StrokeStyle.Width, Is.EqualTo(2).Within(0.000001));
            Assert.That(node.Path.VectorEffect, Is.EqualTo(SvgStrokeVectorEffect.NonScalingStroke));
            Assert.That(node.Path.ShapeRendering, Is.EqualTo(SvgShapeRendering.CrispEdges));
            Assert.That(node.PaintOrder, Is.EqualTo(new[] { SvgPaintOrderComponent.Stroke, SvgPaintOrderComponent.Fill, SvgPaintOrderComponent.Markers }));
            AssertSuccess(result, 1);
            AssertPixel(surface, 3, 2, 0, 0, 128, 128);
            AssertPixel(surface, 3, 1, 0, 0, 0, 0);
        });
    }

    [Test]
    public void InvalidOptionsAndStrokeLimitsReturnStableFailures()
    {
        SvgPixelBuffer surface = Surface(8, 4);
        SvgPathData path = Parse("M0 2 H8");
        SvgPathRasterOptions invalidOrder = StrokeOnly();
        invalidOrder.PaintOrder[1] = SvgPaintOrderComponent.Fill;
        SvgPathRasterOptions invalidOpacity = StrokeOnly();
        invalidOpacity.StrokeOpacity = double.NaN;
        SvgPathRasterOptions invalidColor = StrokeOnly();
        invalidColor.StrokeColor.Red = 255;
        invalidColor.StrokeColor.Alpha = 1;
        SvgPathRasterOptions outlineLimit = StrokeOnly();
        outlineLimit.StrokeStyle.MaximumOutputCommands = 1;
        SvgPathRasterOptions workLimit = StrokeOnly();
        workLimit.RasterOptions.MaximumWork = 1;
        SvgPathData invalidPath = Parse("M0 2 H8");
        invalidPath.Commands[1].Values[0] = double.NaN;

        SvgPathRasterResult order = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(path, surface, invalidOrder);
        SvgPathRasterResult opacity = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(path, surface, invalidOpacity);
        SvgPathRasterResult color = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(path, surface, invalidColor);
        SvgPathRasterResult outline = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(path, surface, outlineLimit);
        SvgPathRasterResult work = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(path, surface, workLimit);
        SvgPathRasterResult invalid = new SvgPathRasterizer().DrawWithPathAndSurfaceAndOptions(invalidPath, surface, StrokeOnly());

        Assert.Multiple(() =>
        {
            AssertFailure(order, SvgPathRasterError.InvalidArgument);
            AssertFailure(opacity, SvgPathRasterError.InvalidArgument);
            AssertFailure(color, SvgPathRasterError.InvalidArgument);
            AssertFailure(outline, SvgPathRasterError.StrokeFailed);
            Assert.That(outline.StrokeError, Is.EqualTo(SvgStrokeError.OutputLimitExceeded));
            AssertFailure(work, SvgPathRasterError.StrokeFailed);
            Assert.That(work.StrokeResult.Error, Is.EqualTo(SvgRasterError.WorkLimitExceeded));
            AssertFailure(invalid, SvgPathRasterError.InvalidPath);
            Assert.That(surface.Data, Is.All.Zero);
        });
    }

    static SvgPathRasterOptions Colors()
    {
        SvgPathRasterOptions options = new SvgPathRasterOptions();
        options.FillColor = new SvgPremultipliedRgba8(255, 0, 0, 255);
        options.StrokeColor = new SvgPremultipliedRgba8(0, 0, 255, 255);
        return options;
    }

    static SvgPathRasterOptions StrokeOnly()
    {
        SvgPathRasterOptions options = new SvgPathRasterOptions();
        options.StrokeColor = new SvgPremultipliedRgba8(0, 0, 255, 255);
        return options;
    }

    static SvgPixelBuffer Surface(int width, int height)
    {
        return SvgPixelBuffer.Create(width, height, SvgPixelColorSpace.Srgb).Buffer;
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 256, 256);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void AssertPixel(SvgPixelBuffer surface, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult read = surface.Read(x, y);
        Assert.That(read.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(read.Pixel.Red, Is.EqualTo(red));
            Assert.That(read.Pixel.Green, Is.EqualTo(green));
            Assert.That(read.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(read.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static void AssertSuccess(SvgPathRasterResult result, int operations)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathRasterError.None));
        Assert.That(result.StrokeError, Is.EqualTo(SvgStrokeError.None));
        Assert.That(result.OperationsCompleted, Is.EqualTo(operations));
    }

    static void AssertFailure(SvgPathRasterResult result, SvgPathRasterError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
