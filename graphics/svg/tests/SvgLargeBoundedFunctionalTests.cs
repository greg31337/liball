namespace svg.Tests;

[Category("Functional")]
public class SvgLargeBoundedFunctionalTests
{
    [Test]
    public void DeepTreeLoadsTraversesWritesAndReloadsWithoutRecursiveDescent()
    {
        const int depth = 2048;
        string source = DeepDocument(depth);
        SvgLoadOptions loadOptions = LargeLoadOptions(depth + 4, depth * 3, source.Length);

        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, loadOptions);
        Assert.That(loaded.Success, Is.True, LoadMessage(loaded));
        List<SvgNode> descendants = new SvgNodeApi().GetDescendants(loaded.Document.DocumentElement);
        SvgWriteOptions writeOptions = new SvgWriteOptions();
        writeOptions.Mode = SvgWriteMode.Compact;
        writeOptions.MaximumDepth = depth + 4;
        writeOptions.MaximumNodes = depth + 4;
        writeOptions.MaximumCharacters = source.Length * 2L;
        SvgWriteResult written = new SvgWriter().WriteWithDocumentAndOptions(loaded.Document, writeOptions);
        SvgLoadResult reloaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(written.Text, loadOptions);

        Assert.Multiple(() =>
        {
            Assert.That(descendants, Has.Count.EqualTo(depth + 1));
            Assert.That(descendants[0].ElementKind, Is.EqualTo(SvgElementKind.G));
            Assert.That(descendants[^1].ElementKind, Is.EqualTo(SvgElementKind.Rect));
            Assert.That(written.Success, Is.True, WriteMessage(written));
            Assert.That(reloaded.Success, Is.True, LoadMessage(reloaded));
            Assert.That(new SvgNodeApi().GetDescendants(reloaded.Document.DocumentElement), Has.Count.EqualTo(depth + 1));
        });
    }

    [Test]
    public void LargeSiblingDocumentHonorsExactAndOneBelowResolvedNodeBudgets()
    {
        const int siblings = 4096;
        string source = SiblingDocument(siblings);
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, LargeLoadOptions(8, siblings + 8, source.Length));
        Assert.That(loaded.Success, Is.True, LoadMessage(loaded));
        SvgResolveOptions exactOptions = new SvgResolveOptions();
        exactOptions.Limits.Tree.MaxNodes = siblings + 1;
        SvgResolveOptions shortOptions = new SvgResolveOptions();
        shortOptions.Limits.Tree.MaxNodes = siblings;

        SvgResolvedDocument exact = new SvgDocumentResolver().ResolveWithOptions(loaded.Document, exactOptions);
        SvgResolvedDocument limited = new SvgDocumentResolver().ResolveWithOptions(loaded.Document, shortOptions);
        SvgResolvedDocument repeated = new SvgDocumentResolver().ResolveWithOptions(loaded.Document, shortOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True, string.Join(" | ", exact.Diagnostics.Select(item => item.Message)));
            Assert.That(exact.Nodes, Has.Count.EqualTo(siblings + 1));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Nodes, Has.Count.EqualTo(siblings));
            Assert.That(limited.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(limited.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(limited.Diagnostics[0].Message, Does.Contain("node limit"));
            Assert.That(repeated.Success, Is.False);
            Assert.That(repeated.Diagnostics.Single().Code, Is.EqualTo(limited.Diagnostics.Single().Code));
            Assert.That(repeated.Diagnostics.Single().Message, Is.EqualTo(limited.Diagnostics.Single().Message));
            Assert.That(repeated.Diagnostics.Single().Source.StartOffset, Is.EqualTo(limited.Diagnostics.Single().Source.StartOffset));
        });
    }

    [Test]
    public void LargeReferenceChainStopsAtConfiguredDepthWithBoundedDiagnostics()
    {
        const int references = 256;
        string source = ReferenceDocument(references);
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, LargeLoadOptions(8, references * 10, source.Length));
        Assert.That(loaded.Success, Is.True, LoadMessage(loaded));
        SvgResolveOptions options = new SvgResolveOptions();
        options.Limits.References.MaxDepth = 32;
        options.MaxDiagnostics = 8;

        SvgResolvedDocument first = new SvgDocumentResolver().ResolveWithOptions(loaded.Document, options);
        SvgResolvedDocument repeated = new SvgDocumentResolver().ResolveWithOptions(loaded.Document, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Diagnostics, Is.Not.Empty);
            Assert.That(first.Diagnostics, Has.Count.LessThanOrEqualTo(options.MaxDiagnostics));
            Assert.That(first.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(first.Diagnostics.Any(item => item.Message.Contains("MaxDepth", StringComparison.Ordinal)), Is.True);
            Assert.That(repeated.Diagnostics.Select(DiagnosticSignature), Is.EqualTo(first.Diagnostics.Select(DiagnosticSignature)));
        });
    }

    [Test]
    public void LargePathRenderStopsAtWorkLimitWithOneGracefulDiagnostic()
    {
        string source = PathDocument(2048);
        SvgDocument document = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, LargeLoadOptions(8, 16, source.Length)).Document;
        SvgRenderOptions options = new SvgRenderOptions();
        options.ViewportWidth = 64;
        options.ViewportHeight = 64;
        options.MaximumPixels = 5000;

        SvgRenderResult result = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgRenderError.WorkLimitExceeded));
            Assert.That(result.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.EqualTo(1));
            Assert.That(result.Diagnostics.Last().Message, Does.Contain("scan conversion"));
            Assert.That(result.PixelsProcessed, Is.LessThanOrEqualTo(options.MaximumPixels));
            Assert.That(result.PngBytes, Is.Empty);
        });
    }

    [Test]
    public void PreCancellationPropagatesThroughLargeLoadResolveRenderAndWriteOperations()
    {
        string source = SiblingDocument(1024);
        SvgLoadOptions normalOptions = LargeLoadOptions(8, 2048, source.Length);
        SvgDocument document = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, normalOptions).Document;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SvgLoadOptions loadOptions = LargeLoadOptions(8, 2048, source.Length);
        loadOptions.CancellationToken = cancellation.Token;
        SvgResolveOptions resolveOptions = new SvgResolveOptions();
        resolveOptions.CancellationToken = cancellation.Token;
        SvgRenderOptions renderOptions = new SvgRenderOptions();
        renderOptions.ViewportWidth = 16;
        renderOptions.ViewportHeight = 16;
        renderOptions.CancellationToken = cancellation.Token;
        SvgWriteOptions writeOptions = new SvgWriteOptions();
        writeOptions.CancellationToken = cancellation.Token;

        SvgLoadResult load = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, loadOptions);
        SvgResolvedDocument resolved = new SvgDocumentResolver().ResolveWithOptions(document, resolveOptions);
        SvgRenderResult rendered = new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, renderOptions);
        SvgWriteResult written = new SvgWriter().WriteWithDocumentAndOptions(document, writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(load.Success, Is.False);
            Assert.That(load.Document, Is.Null);
            AssertSingleCancellation(load.Diagnostics);
            Assert.That(resolved.Success, Is.False);
            Assert.That(resolved.Root, Is.Null);
            AssertSingleCancellation(resolved.Diagnostics);
            Assert.That(rendered.Success, Is.False);
            Assert.That(rendered.Error, Is.EqualTo(SvgRenderError.Cancelled));
            AssertSingleCancellation(rendered.Diagnostics);
            Assert.That(written.Success, Is.False);
            Assert.That(written.Error, Is.EqualTo(SvgWriteError.Cancelled));
            Assert.That(written.Text, Is.Empty);
            AssertSingleCancellation(written.Diagnostics);
        });
    }

    static SvgLoadOptions LargeLoadOptions(int maximumDepth, int maximumNodes, int characters)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.Limits.Tree.MaxDepth = maximumDepth;
        options.Limits.Tree.MaxNodes = maximumNodes;
        options.Limits.Input.MaxInputCharacters = characters + 16;
        options.Limits.Input.MaxXmlTokenCharacters = characters + 16;
        return options;
    }

    static string DeepDocument(int depth)
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder();
        source.Append("<svg xmlns='http://www.w3.org/2000/svg'>");
        int index = 0;
        while (index < depth)
        {
            source.Append("<g>");
            index++;
        }
        source.Append("<rect/>");
        while (index > 0)
        {
            source.Append("</g>");
            index--;
        }
        source.Append("</svg>");
        return source.ToString();
    }

    static string SiblingDocument(int count)
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder();
        source.Append("<svg xmlns='http://www.w3.org/2000/svg'>");
        int index = 0;
        while (index < count)
        {
            source.Append("<rect/>");
            index++;
        }
        source.Append("</svg>");
        return source.ToString();
    }

    static string ReferenceDocument(int count)
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder();
        source.Append("<svg xmlns='http://www.w3.org/2000/svg'><defs><g id='r0'><rect width='1' height='1'/></g>");
        int index = 1;
        while (index < count)
        {
            source.Append("<g id='r").Append(index).Append("'><use href='#r").Append(index - 1).Append("'/></g>");
            index++;
        }
        source.Append("</defs><use href='#r").Append(count - 1).Append("'/></svg>");
        return source.ToString();
    }

    static string PathDocument(int segments)
    {
        System.Text.StringBuilder source = new System.Text.StringBuilder();
        source.Append("<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64'><path fill='none' stroke='black' d='M0 0");
        int index = 0;
        while (index < segments)
        {
            source.Append('L').Append(index % 64).Append(' ').Append((index * 17) % 64);
            index++;
        }
        source.Append("'/></svg>");
        return source.ToString();
    }

    static void AssertSingleCancellation(List<SvgDiagnostic> diagnostics)
    {
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(SvgDiagnosticSeverity.Error));
    }

    static string DiagnosticSignature(SvgDiagnostic diagnostic)
    {
        long offset = diagnostic.Source == null ? -1 : diagnostic.Source.StartOffset;
        return diagnostic.Code + "|" + diagnostic.Severity + "|" + offset + "|" + diagnostic.Message;
    }

    static string LoadMessage(SvgLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }

    static string WriteMessage(SvgWriteResult result)
    {
        return result.Error + ": " + string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
