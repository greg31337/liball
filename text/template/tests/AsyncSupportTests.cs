using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class AsyncSupportTests
{
    [Test]
    public void MemoryTemplateLoader_LoadAsync_ReturnsTemplateCopiesAndHonorsCancellation()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        loader.SetTemplate("card", "Hello {{ name }}");

        TemplateSource source = loader.LoadWithoutCancellationAsync("card").GetAwaiter().GetResult()!;
        source.Text = "changed";
        TemplateSource second = loader.LoadRequiredAsync("card").GetAwaiter().GetResult();

        Assert.That(second.Text, Is.EqualTo("Hello {{ name }}"));

        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<TemplateSource?> canceled = loader.LoadAsync("card", cancellation.Token);
        Assert.That(canceled.IsCanceled, Is.True);
    }

    [Test]
    public void FileTemplateLoader_LoadAsync_LoadsFilesAndHonorsCancellation()
    {
        string root = Path.Combine(Path.GetTempPath(), "template-async-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "card.tpl"), "Card {{ name }}");

        FileTemplateLoader loader = new FileTemplateLoader(root);
        TemplateSource source = loader.LoadRequiredAsync("card.tpl").GetAwaiter().GetResult();

        Assert.That(source.Text, Is.EqualTo("Card {{ name }}"));
        Assert.That(source.Name, Is.EqualTo("card.tpl"));
        Assert.That(source.Timestamp, Is.EqualTo(source.Version));

        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<TemplateSource?> canceled = loader.LoadAsync("card.tpl", cancellation.Token);
        Assert.That(canceled.IsCanceled, Is.True);

        Directory.Delete(root, true);
    }

    [Test]
    public void TemplateRegistry_LoadTemplateAsync_UsesRegisteredLoaders()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("memory", "Memory {{ name }}");

        TemplateSource source = registry.LoadTemplateAsync("memory").GetAwaiter().GetResult()!;

        Assert.That(source.Text, Is.EqualTo("Memory {{ name }}"));
    }

    [Test]
    public void TemplateRenderer_RenderAsync_RendersStringsDocumentsAndWriters()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateParseResult parseResult = TemplateParser.ParseNative("Doc {{ name }}");
        StringWriter writer = new StringWriter();

        string textResult = renderer.RenderToStringAsync("Hello {{ name }}", CreateRoot()).GetAwaiter().GetResult();
        string documentResult = renderer.RenderDocumentToStringAsync(parseResult.Document, CreateRoot()).GetAwaiter().GetResult();
        renderer.RenderToWriterAsync("Writer {{ name }}", CreateRoot(), writer).GetAwaiter().GetResult();

        Assert.That(textResult, Is.EqualTo("Hello Ada"));
        Assert.That(documentResult, Is.EqualTo("Doc Ada"));
        Assert.That(writer.ToString(), Is.EqualTo("Writer Ada"));
    }

    [Test]
    public void TemplateRenderer_RenderAsync_HonorsPreCanceledTokens()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<string> stringTask = renderer.RenderToStringWithCancellationAsync("Hello {{ name }}", CreateRoot(), cancellation.Token);
        Task writerTask = renderer.RenderToWriterWithCancellationAsync("Hello {{ name }}", CreateRoot(), new StringWriter(), cancellation.Token);

        Assert.That(stringTask.IsCanceled, Is.True);
        Assert.That(writerTask.IsCanceled, Is.True);
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Ada");
        return TemplateValue.FromMap(root);
    }
}
