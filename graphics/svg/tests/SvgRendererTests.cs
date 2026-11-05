namespace svg.Tests;

public class SvgRendererTests
{
    [Test]
    public void RendersShapesAtRequestedViewportScaleBackgroundTimeAndPng()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='4' height='3'><rect x='1' y='1' width='2' height='1' fill='red'/></svg>");
        SvgRenderOptions options = Options(4, 3);
        options.Scale = 2;
        options.Background = new SvgPremultipliedRgba8(0, 0, 255, 255);
        options.AnimationTime = 12.5;
        options.EncodePng = true;
        options.SamplesPerAxis = 1;

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);
        SvgPngDecodeResult png = new SvgPngCodec().DecodeWithBytes(result.PngBytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, ResultMessage(result));
            Assert.That(result.Error, Is.EqualTo(SvgRenderError.None));
            Assert.That(result.Pixels.Width, Is.EqualTo(8));
            Assert.That(result.Pixels.Height, Is.EqualTo(6));
            Assert.That(result.AnimationTime, Is.EqualTo(12.5));
            Assert.That(result.Commands.Success, Is.True);
            Assert.That(result.NodesVisited, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.PngBytes, Is.Not.Empty);
            Assert.That(png.Success, Is.True);
            AssertPixel(result.Pixels, 0, 0, 0, 0, 255, 255);
            AssertPixel(result.Pixels, 2, 2, 255, 0, 0, 255);
            AssertPixel(result.Pixels, 5, 3, 255, 0, 0, 255);
            AssertPixel(result.Pixels, 6, 2, 0, 0, 255, 255);
            AssertSamePixels(result.Pixels, png.Pixels);
        });
    }

    [Test]
    public void RendersGradientPaintIntoDeterministicGoldenPixels()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='4' height='1'><defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='4'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><rect width='4' height='1' fill='url(#g)'/></svg>";
        SvgRenderOptions options = Options(4, 1);
        options.SamplesPerAxis = 1;

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(Load(source), options);

        Assert.That(result.Success, Is.True, ResultMessage(result));
        AssertPixel(result.Pixels, 0, 0, 223, 0, 32, 255);
        AssertPixel(result.Pixels, 1, 0, 159, 0, 96, 255);
        AssertPixel(result.Pixels, 2, 0, 96, 0, 159, 255);
        AssertPixel(result.Pixels, 3, 0, 32, 0, 223, 255);
    }

    [Test]
    public void RendersFilterClipAndGroupOpacityEffects()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='4' height='2'><defs>" +
            "<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='4' height='2'><feFlood flood-color='lime'/></filter>" +
            "<clipPath id='c' clipPathUnits='userSpaceOnUse'><rect width='2' height='2'/></clipPath>" +
            "</defs><g opacity='.5' clip-path='url(#c)' filter='url(#f)'><rect width='4' height='2' fill='red'/></g></svg>";
        SvgRenderOptions options = Options(4, 2);
        options.SamplesPerAxis = 1;

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(Load(source), options);

        Assert.That(result.Success, Is.True, ResultMessage(result));
        AssertPixel(result.Pixels, 0, 0, 0, 128, 0, 128);
        AssertPixel(result.Pixels, 1, 1, 0, 128, 0, 128);
        AssertPixel(result.Pixels, 2, 0, 0, 0, 0, 0);
        AssertPixel(result.Pixels, 3, 1, 0, 0, 0, 0);
    }

    [Test]
    public void RendersTextUsingResolvedOriginFontSizeAndDeterministicFallback()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='5' height='5'><text x='1' y='4' font-size='4' fill='white'>A</text></svg>");
        SvgRenderOptions options = Options(5, 5);
        options.SamplesPerAxis = 1;

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);
        SvgSceneNode text = result.Scene.Nodes.Single(node => node.Kind == SvgSceneNodeKind.Text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, ResultMessage(result));
            Assert.That(text.Text.Text, Is.EqualTo("A"));
            Assert.That(text.Text.X, Is.EqualTo(1));
            Assert.That(text.Text.Y, Is.EqualTo(4));
            Assert.That(text.Text.FontSize, Is.EqualTo(4));
            AssertPixel(result.Pixels, 0, 1, 0, 0, 0, 0);
            AssertPixel(result.Pixels, 1, 0, 255, 255, 255, 255);
            AssertPixel(result.Pixels, 2, 3, 255, 255, 255, 255);
            AssertPixel(result.Pixels, 3, 2, 0, 0, 0, 0);
        });
    }

    [Test]
    public void RendersDecodedPngImagesThroughSceneMapping()
    {
        SvgPixelBuffer image = Buffer(1, 1);
        image.Write(0, 0, new SvgPremultipliedRgba8(200, 100, 50, 255));
        string data = Convert.ToBase64String(new SvgPngCodec().EncodeWithPixels(image).Bytes);
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='2' height='2'><image width='2' height='2' image-rendering='pixelated' href='data:image/png;base64," + data + "'/></svg>");
        SvgRenderOptions options = Options(2, 2);

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);

        Assert.That(result.Success, Is.True, ResultMessage(result));
        AssertPixel(result.Pixels, 0, 0, 200, 100, 50, 255);
        AssertPixel(result.Pixels, 1, 0, 200, 100, 50, 255);
        AssertPixel(result.Pixels, 0, 1, 200, 100, 50, 255);
        AssertPixel(result.Pixels, 1, 1, 200, 100, 50, 255);
    }

    [Test]
    public void RendersWholeFragmentSourceNodeAndExactInstanceTargets()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='2'><defs><rect id='art' width='2' height='2' fill='lime'/></defs><rect id='blue' width='2' height='2' fill='blue'/><rect id='red' x='2' width='2' height='2' fill='red'/><use href='#art' x='5'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgNode blueSource = document.DocumentElement.Children[1];
        SvgNode redSource = document.DocumentElement.Children[2];
        SvgNode artSource = document.DocumentElement.Children[0].Children[0];
        SvgSceneNode instance = scene.Nodes.Single(node => node.SourceNode == artSource && node.InstancePath.Length != 0 && node.Kind == SvgSceneNodeKind.Path);
        SvgRenderOptions options = Options(8, 2);
        options.SamplesPerAxis = 1;

        SvgRenderResult whole = new SvgRenderer().RenderWithSvgResolvedDocumentAndSvgRenderOptions(resolved, options);
        SvgRenderResult fragment = new SvgRenderer().RenderFragmentWithSvgResolvedDocumentAndStringAndSvgRenderOptions(resolved, "#red", options);
        SvgRenderResult documentFragment = new SvgRenderer().RenderFragmentWithSvgDocumentAndStringAndSvgRenderOptions(document, "#red", options);
        SvgRenderResult nodeResult = new SvgRenderer().RenderNodeWithSvgResolvedDocumentAndSvgNodeAndSvgRenderOptions(resolved, blueSource, options);
        SvgRenderResult instanceResult = new SvgRenderer().RenderInstanceWithSvgResolvedDocumentAndStringAndSvgRenderOptions(resolved, instance.InstancePath, options);
        SvgRenderOptions croppedOptions = Options(2, 2);
        croppedOptions.ViewportX = 2;
        croppedOptions.SamplesPerAxis = 1;
        SvgRenderResult cropped = new SvgRenderer().RenderWithSvgResolvedDocumentAndSvgRenderOptions(resolved, croppedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(whole.Success, Is.True, ResultMessage(whole));
            Assert.That(fragment.Success, Is.True, ResultMessage(fragment));
            Assert.That(documentFragment.Success, Is.True, ResultMessage(documentFragment));
            Assert.That(nodeResult.Success, Is.True, ResultMessage(nodeResult));
            Assert.That(instanceResult.Success, Is.True, ResultMessage(instanceResult));
            Assert.That(cropped.Success, Is.True, ResultMessage(cropped));
            AssertPixel(whole.Pixels, 0, 0, 0, 0, 255, 255);
            AssertPixel(whole.Pixels, 2, 0, 255, 0, 0, 255);
            AssertPixel(whole.Pixels, 5, 0, 0, 255, 0, 255);
            AssertPixel(fragment.Pixels, 0, 0, 0, 0, 0, 0);
            AssertPixel(fragment.Pixels, 2, 0, 255, 0, 0, 255);
            AssertPixel(documentFragment.Pixels, 2, 0, 255, 0, 0, 255);
            AssertPixel(nodeResult.Pixels, 0, 0, 0, 0, 255, 255);
            AssertPixel(nodeResult.Pixels, 2, 0, 0, 0, 0, 0);
            AssertPixel(instanceResult.Pixels, 5, 0, 0, 255, 0, 255);
            AssertPixel(instanceResult.Pixels, 0, 0, 0, 0, 0, 0);
            AssertPixel(cropped.Pixels, 0, 0, 255, 0, 0, 255);
            AssertPixel(cropped.Pixels, 1, 1, 255, 0, 0, 255);
            Assert.That(fragment.Scene.FindNode(blueSource, ""), Is.Null);
            Assert.That(nodeResult.Scene.FindNode(redSource, ""), Is.Null);
        });
    }

    [Test]
    public void ReportsSecureAllocationWorkCancellationTargetStalenessAndCallbackFailures()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='4' height='2'><rect id='r' width='4' height='2'/><text y='2'>A</text></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgRenderOptions allocation = Options(4, 2);
        allocation.Limits.Raster.MaxWidth = 2;
        SvgRenderOptions work = Options(4, 2);
        work.MaximumPixels = 1;
        SvgRenderOptions cancelled = Options(4, 2);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        cancelled.CancellationToken = cancellation.Token;
        SvgRenderOptions callback = Options(4, 2);
        callback.TextPathCallback = node => throw new InvalidOperationException("failed");

        SvgRenderResult allocationResult = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, allocation);
        SvgRenderResult workResult = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, work);
        SvgRenderResult cancelledResult = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, cancelled);
        SvgRenderResult missingResult = new SvgRenderer().RenderFragmentWithSvgResolvedDocumentAndStringAndSvgRenderOptions(resolved, "#missing", Options(4, 2));
        SvgRenderResult callbackResult = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, callback);
        new SvgNodeApi().SetTextContent(document.DocumentElement.Children[0], "changed");
        SvgRenderResult staleResult = new SvgRenderer().RenderWithSvgResolvedDocumentAndSvgRenderOptions(resolved, Options(4, 2));

        Assert.Multiple(() =>
        {
            Assert.That(allocationResult.Error, Is.EqualTo(SvgRenderError.AllocationLimitExceeded));
            Assert.That(workResult.Error, Is.EqualTo(SvgRenderError.WorkLimitExceeded));
            Assert.That(cancelledResult.Error, Is.EqualTo(SvgRenderError.Cancelled));
            Assert.That(missingResult.Error, Is.EqualTo(SvgRenderError.TargetNotFound));
            Assert.That(callbackResult.Error, Is.EqualTo(SvgRenderError.RasterizationFailed));
            Assert.That(staleResult.Error, Is.EqualTo(SvgRenderError.StaleInput));
            Assert.That(allocationResult.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(callbackResult.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.RenderingFailed), Is.True);
        });
    }

    static SvgRenderOptions Options(double width, double height)
    {
        SvgRenderOptions result = new SvgRenderOptions();
        result.ViewportWidth = width;
        result.ViewportHeight = height;
        return result;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }

    static SvgPixelBuffer Buffer(int width, int height)
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create(width, height, SvgPixelColorSpace.Srgb);
        Assert.That(created.Success, Is.True);
        return created.Buffer;
    }

    static string ResultMessage(SvgRenderResult result)
    {
        return result.Error + ": " + string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }

    static void AssertPixel(SvgPixelBuffer pixels, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult read = pixels.Read(x, y);
        Assert.That(read.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(read.Pixel.Red, Is.EqualTo(red), $"red at {x},{y}");
            Assert.That(read.Pixel.Green, Is.EqualTo(green), $"green at {x},{y}");
            Assert.That(read.Pixel.Blue, Is.EqualTo(blue), $"blue at {x},{y}");
            Assert.That(read.Pixel.Alpha, Is.EqualTo(alpha), $"alpha at {x},{y}");
        });
    }

    static void AssertSamePixels(SvgPixelBuffer expected, SvgPixelBuffer actual)
    {
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        int y = 0;
        while (y < expected.Height)
        {
            int x = 0;
            while (x < expected.Width)
            {
                SvgPremultipliedRgba8 left = expected.Read(x, y).Pixel;
                SvgPremultipliedRgba8 right = actual.Read(x, y).Pixel;
                Assert.That(new[] { right.Red, right.Green, right.Blue, right.Alpha }, Is.EqualTo(new[] { left.Red, left.Green, left.Blue, left.Alpha }), $"pixel {x},{y}");
                x++;
            }
            y++;
        }
    }
}
