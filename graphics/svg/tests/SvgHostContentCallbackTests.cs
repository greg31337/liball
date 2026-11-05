namespace svg.Tests;

public class SvgHostContentCallbackTests
{
    [Test]
    public void RepresentsEveryHostKindWithCallbacksDisabledByDefault()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'>" +
            "<image href='image.bin' width='1' height='1'/>" +
            "<foreignObject width='1' height='1'><p xmlns='http://www.w3.org/1999/xhtml'>x</p></foreignObject>" +
            "<audio href='audio.bin' width='1' height='1'/>" +
            "<video href='video.bin' width='1' height='1'/>" +
            "<iframe href='frame.html' width='1' height='1'/>" +
            "<canvas width='1' height='1'/>" +
            "</svg>";
        SvgDocument document = Load(source);
        SvgEmbeddedContentKind[] expected =
        [
            SvgEmbeddedContentKind.Image,
            SvgEmbeddedContentKind.ForeignObject,
            SvgEmbeddedContentKind.Audio,
            SvgEmbeddedContentKind.Video,
            SvgEmbeddedContentKind.Iframe,
            SvgEmbeddedContentKind.Canvas
        ];

        int index = 0;
        while (index < expected.Length)
        {
            SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNode(document.DocumentElement.Children[index]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, "kind " + expected[index]);
                Assert.That(result.Kind, Is.EqualTo(expected[index]));
                Assert.That(result.HostContent.Kind, Is.EqualTo(expected[index]));
                Assert.That(result.CallbackResult, Is.Null);
                Assert.That(result.ContentAvailable, Is.False);
                Assert.That(result.Renderable, Is.False);
            });
            index++;
        }

        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildResult scene = resolved.BuildScene();
        Assert.Multiple(() =>
        {
            Assert.That(scene.Success, Is.True);
            Assert.That(scene.Scene.Nodes.Count(node => node.Kind == SvgSceneNodeKind.Image), Is.EqualTo(1));
            Assert.That(scene.Scene.Nodes.Count(node => node.Kind == SvgSceneNodeKind.HostContent), Is.EqualTo(5));
            Assert.That(scene.Scene.Nodes.Where(node => node.Kind == SvgSceneNodeKind.HostContent).Select(node => node.HostContent.Kind), Is.EqualTo(expected.Skip(1)));
            Assert.That(new SvgResolveOptions().HostContentCallback, Is.Null);
        });
    }

    [Test]
    public void AppliesBoundedIntrinsicLayoutRasterAndOrderedDiagnostics()
    {
        SvgNode canvas = Load("<svg xmlns='http://www.w3.org/2000/svg'><canvas/></svg>").DocumentElement.Children[0];
        SvgPixelBuffer pixels = Buffer(2, 1, new SvgPremultipliedRgba8(10, 20, 30, 255));
        SvgHostContentCallbackRequest observed = null;
        SvgDiagnostic first = new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.None, "first", canvas.SourceSpan);
        SvgDiagnostic second = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "second", canvas.SourceSpan);
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.MaxHostRasterWidth = 2;
        options.MaxHostRasterHeight = 1;
        options.MaxHostRasterPixels = 2;
        options.HostContentCallback = request =>
        {
            observed = request;
            SvgHostContentCallbackResult callback = new SvgHostContentCallbackResult();
            callback.Success = true;
            callback.IntrinsicSize = SvgIntrinsicSize.Create(4, 2);
            callback.ContentBounds = SvgRect.Create(3, 4, 8, 6, false);
            callback.ContentTransform = new SvgMatrix(4, 0, 0, 6, 3, 4);
            callback.RasterPixels = pixels;
            callback.Diagnostics.Add(first);
            callback.Diagnostics.Add(second);
            return callback;
        };

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(canvas, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(observed.Kind, Is.EqualTo(SvgEmbeddedContentKind.Canvas));
            Assert.That(observed.Node, Is.SameAs(canvas));
            Assert.That(observed.NodeVersion, Is.EqualTo(canvas.Version));
            Assert.That(observed.MaxRasterPixels, Is.EqualTo(2));
            Assert.That(observed.Bounds.Width, Is.EqualTo(300));
            Assert.That(result.Width, Is.EqualTo(4));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.ContentBounds.X, Is.EqualTo(3));
            Assert.That(result.ContentTransform.A, Is.EqualTo(4));
            Assert.That(result.ContentAvailable, Is.True);
            Assert.That(result.Renderable, Is.True);
            Assert.That(result.CallbackResult.RasterPixels, Is.SameAs(pixels));
            Assert.That(result.HostContent.CallbackResult, Is.SameAs(result.CallbackResult));
            Assert.That(result.Diagnostics, Is.EqualTo(new[] { first, second }));
        });
    }

    [Test]
    public void RejectsCallbackRasterOutputThatExceedsTheConfiguredBound()
    {
        SvgNode canvas = Load("<svg xmlns='http://www.w3.org/2000/svg'><canvas width='1' height='1'/></svg>").DocumentElement.Children[0];
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.MaxHostRasterWidth = 1;
        options.HostContentCallback = request =>
        {
            SvgHostContentCallbackResult callback = new SvgHostContentCallbackResult();
            callback.Success = true;
            callback.RasterPixels = Buffer(2, 1, new SvgPremultipliedRgba8(0, 0, 0, 0));
            return callback;
        };

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(canvas, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.CallbackResult, Is.Null);
            Assert.That(result.ContentAvailable, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    [Test]
    public void CarriesHostRasterThroughResolutionSceneCommandsAndRendering()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='2' height='1'><canvas width='2' height='1'/></svg>");
        SvgPixelBuffer supplied = Buffer(1, 1, new SvgPremultipliedRgba8(200, 100, 50, 255));
        SvgDiagnostic callbackDiagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.None, "host raster", document.DocumentElement.Children[0].SourceSpan);
        SvgRenderOptions options = new SvgRenderOptions();
        options.ViewportWidth = 2;
        options.ViewportHeight = 1;
        options.SamplesPerAxis = 1;
        options.ResolveOptions.HostContentCallback = request =>
        {
            SvgHostContentCallbackResult callback = new SvgHostContentCallbackResult();
            callback.Success = true;
            callback.RasterPixels = supplied;
            callback.Diagnostics.Add(callbackDiagnostic);
            return callback;
        };

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);
        SvgSceneNode host = result.Scene.Nodes.Single(node => node.Kind == SvgSceneNodeKind.HostContent);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
            Assert.That(host.HostContent.Kind, Is.EqualTo(SvgEmbeddedContentKind.Canvas));
            Assert.That(host.HostContent.Record.Kind, Is.EqualTo(SvgEmbeddedContentKind.Canvas));
            Assert.That(host.HostContent.RasterPixels, Is.Not.SameAs(supplied));
            Assert.That(result.Commands.Commands.Any(command => command.Kind == SvgRenderCommandKind.DrawHostContent && command.Node == host), Is.True);
            Assert.That(result.Diagnostics.Any(item => item.Message == callbackDiagnostic.Message && item.Code == callbackDiagnostic.Code), Is.True);
            AssertPixel(result.Pixels, 0, 0, 200, 100, 50, 255);
            AssertPixel(result.Pixels, 1, 0, 200, 100, 50, 255);
        });
    }

    [Test]
    public void UsesCallbackDecodedPixelsForImageSceneAndRendering()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'><image href='asset.bin' width='1' height='1'/></svg>");
        SvgPixelBuffer supplied = Buffer(1, 1, new SvgPremultipliedRgba8(0, 180, 0, 255));
        SvgHostContentCallbackRequest observed = null;
        SvgRenderOptions options = new SvgRenderOptions();
        options.ViewportWidth = 1;
        options.ViewportHeight = 1;
        options.SamplesPerAxis = 1;
        options.ResolveOptions.HostContentCallback = request =>
        {
            observed = request;
            SvgHostContentCallbackResult callback = new SvgHostContentCallbackResult();
            callback.Success = true;
            callback.RasterPixels = supplied;
            return callback;
        };

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);
        SvgSceneNode image = result.Scene.Nodes.Single(node => node.Kind == SvgSceneNodeKind.Image);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
            Assert.That(observed.Kind, Is.EqualTo(SvgEmbeddedContentKind.Image));
            Assert.That(observed.Reference, Is.Not.Null);
            Assert.That(image.Image.Resource.Image.DecodedContentAvailable, Is.True);
            Assert.That(image.Image.Resource.Image.DecodedPixels, Is.Not.SameAs(supplied));
            Assert.That(image.Image.HostContent.CallbackResult.RasterPixels, Is.SameAs(supplied));
            AssertPixel(result.Pixels, 0, 0, 0, 180, 0, 255);
        });
    }

    [Test]
    public void SuppressedContentSkipsCallbacksWhileActiveFailureAndCancellationStayLocated()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><canvas width='1' height='1' style='display:none'/><canvas width='1' height='1' visibility='hidden'/><canvas width='0' height='1'/><canvas width='1' height='1'/></svg>");
        int suppressedCalls = 0;
        SvgEmbeddedContentResolveOptions displayOptions = CallbackOptions(request =>
        {
            suppressedCalls = suppressedCalls + 1;
            return new SvgHostContentCallbackResult();
        });
        displayOptions.ComputedStyle = new SvgStyleResolver().Resolve(document.DocumentElement.Children[0], new List<SvgStyleSheet>());
        SvgEmbeddedContentResolveOptions hiddenOptions = CallbackOptions(request =>
        {
            suppressedCalls = suppressedCalls + 1;
            return new SvgHostContentCallbackResult();
        });
        SvgEmbeddedContentResolveOptions zeroOptions = CallbackOptions(request =>
        {
            suppressedCalls = suppressedCalls + 1;
            return new SvgHostContentCallbackResult();
        });
        SvgEmbeddedContentResolveOptions conditionOptions = CallbackOptions(request =>
        {
            suppressedCalls = suppressedCalls + 1;
            return new SvgHostContentCallbackResult();
        });
        conditionOptions.ConditionsSatisfied = false;

        SvgEmbeddedContentResult display = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0], displayOptions);
        SvgEmbeddedContentResult hidden = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[1], hiddenOptions);
        SvgEmbeddedContentResult zero = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[2], zeroOptions);
        SvgEmbeddedContentResult condition = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], conditionOptions);

        int failureCalls = 0;
        SvgDiagnostic callbackWarning = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "host declined", document.DocumentElement.Children[3].SourceSpan);
        SvgEmbeddedContentResolveOptions failureOptions = CallbackOptions(request =>
        {
            failureCalls = failureCalls + 1;
            SvgHostContentCallbackResult declined = new SvgHostContentCallbackResult();
            declined.Diagnostics.Add(callbackWarning);
            return declined;
        });
        SvgEmbeddedContentResult failure = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], failureOptions);

        int cancelledCalls = 0;
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        SvgEmbeddedContentResolveOptions cancellationOptions = CallbackOptions(request =>
        {
            cancelledCalls = cancelledCalls + 1;
            return new SvgHostContentCallbackResult();
        });
        cancellationOptions.CancellationToken = cancellation.Token;
        SvgEmbeddedContentResult cancelled = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[3], cancellationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(suppressedCalls, Is.Zero);
            Assert.That(new[] { display, hidden, zero, condition }, Has.All.Matches<SvgEmbeddedContentResult>(item => item.Success && item.CallbackResult == null && !item.Renderable));
            Assert.That(failureCalls, Is.EqualTo(1));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Diagnostics.Select(item => item.Code), Is.EqualTo(new[] { SvgDiagnosticCode.CompatibilityRecovery, SvgDiagnosticCode.RenderingFailed }));
            Assert.That(failure.Diagnostics[0], Is.SameAs(callbackWarning));
            Assert.That(failure.Diagnostics[1].Source, Is.SameAs(document.DocumentElement.Children[3].SourceSpan));
            Assert.That(cancelledCalls, Is.Zero);
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
        });
    }

    [Test]
    public void UseInstancesCarryDistinctIdentityThroughCallbacksResolvedRecordsAndSceneNodes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><g id='host'><canvas width='2' height='1'/></g></defs><use href='#host' x='3'/><use href='#host' x='7'/></svg>");
        SvgNode canvasSource = document.DocumentElement.Children[0].Children[0].Children[0];
        List<SvgHostContentCallbackRequest> requests = new List<SvgHostContentCallbackRequest>();
        SvgResolveOptions options = new SvgResolveOptions();
        options.HostContentCallback = request =>
        {
            requests.Add(request);
            SvgHostContentCallbackResult callback = new SvgHostContentCallbackResult();
            callback.Success = true;
            return callback;
        };

        SvgResolvedDocument resolved = new SvgDocumentResolver().ResolveWithOptions(document, options);
        List<SvgResolvedNode> instances = resolved.Nodes.Where(node => node.EmbeddedContent != null && node.EmbeddedContent.CallbackResult != null).ToList();
        SvgSceneBuildResult built = resolved.BuildScene();
        List<SvgSceneNode> sceneInstances = built.Scene.Nodes.Where(node => node.Kind == SvgSceneNodeKind.HostContent).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.True, string.Join(" | ", resolved.Diagnostics.Select(item => item.Message)));
            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(requests.Select(request => request.Node), Has.All.SameAs(canvasSource));
            Assert.That(requests.Select(request => request.InstancePath), Is.EqualTo(new[] { "0/1/instance/0/0", "0/2/instance/0/0" }));
            Assert.That(instances.Select(node => node.InstancePath), Is.EqualTo(requests.Select(request => request.InstancePath)));
            Assert.That(instances.Select(node => node.EmbeddedContent.InstancePath), Is.EqualTo(requests.Select(request => request.InstancePath)));
            Assert.That(instances.Select(node => node.EmbeddedContent.HostContent.InstancePath), Is.EqualTo(requests.Select(request => request.InstancePath)));
            Assert.That(built.Success, Is.True);
            Assert.That(sceneInstances.Select(node => node.InstancePath), Is.EqualTo(requests.Select(request => request.InstancePath)));
            Assert.That(sceneInstances.Select(node => node.HostContent.Record.InstancePath), Is.EqualTo(requests.Select(request => request.InstancePath)));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static SvgPixelBuffer Buffer(int width, int height, SvgPremultipliedRgba8 pixel)
    {
        SvgPixelBuffer buffer = SvgPixelBuffer.Create(width, height, SvgPixelColorSpace.Srgb).Buffer;
        buffer.ClearWithPixel(pixel);
        return buffer;
    }

    static SvgEmbeddedContentResolveOptions CallbackOptions(Func<SvgHostContentCallbackRequest, SvgHostContentCallbackResult> callback)
    {
        SvgEmbeddedContentResolveOptions options = new SvgEmbeddedContentResolveOptions();
        options.HostContentCallback = callback;
        return options;
    }

    static void AssertPixel(SvgPixelBuffer pixels, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        SvgPremultipliedRgba8 pixel = pixels.Read(x, y).Pixel;
        Assert.That(new[] { pixel.Red, pixel.Green, pixel.Blue, pixel.Alpha }, Is.EqualTo(new[] { red, green, blue, alpha }));
    }
}
