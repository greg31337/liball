using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownDiagnosticsOwnerTests
{
    [Test]
    public void Document_OwnsDiagnosticCollection()
    {
        MarkdownDocument document = new MarkdownDocument();

        document.AddDiagnosticValues(MarkdownDiagnosticSeverity.Error, "E001", "error", new MarkdownSourceSpan());

        Assert.That(document.HasDiagnostics(), Is.True);
        Assert.That(document.HasErrors(), Is.True);
        Assert.That(document.Diagnostics.GetCount(), Is.EqualTo(1));
        Assert.That(document.GetDiagnostics()[0].Code, Is.EqualTo("E001"));
    }

    [Test]
    public void Document_AddDiagnosticCopiesInput()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownDiagnostic diagnostic = MarkdownDiagnostic.Warning(
            "W001",
            "warning",
            MarkdownSourceSpan.Create(1, 1, 2, 4, 1, 5));

        document.AddDiagnostic(diagnostic);
        diagnostic.Span.Start.Offset = 99;

        Assert.That(document.GetDiagnostics()[0].Span.Start.Offset, Is.EqualTo(1));
    }

    [Test]
    public void ParseContext_DefaultsToEmptySourceAndDocumentDiagnostics()
    {
        MarkdownParseContext context = new MarkdownParseContext();

        context.AddDiagnosticValues(MarkdownDiagnosticSeverity.Warning, "W001", "warning", new MarkdownSourceSpan());

        Assert.That(context.Source, Is.EqualTo(""));
        Assert.That(context.Pipeline.UsesCommonMark(), Is.True);
        Assert.That(context.Document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(context.Diagnostics, Is.SameAs(context.Document.Diagnostics));
        Assert.That(context.LinkReferences, Is.Not.Null);
        Assert.That(context.LinkReferences.IsEmpty(), Is.True);
        Assert.That(context.HasDiagnostics(), Is.True);
        Assert.That(context.Document.Diagnostics.GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void ParseContext_NormalizesSourceAndSharesCustomDocumentDiagnostics()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownParseContext context = MarkdownParseContext.Create("a\0b", document);

        context.AddDiagnostic(MarkdownDiagnostic.Error("E001", "error", new MarkdownSourceSpan()));

        Assert.That(context.Source[1], Is.EqualTo(MarkdownInputNormalizer.NullReplacementCharacter));
        Assert.That(context.Document, Is.SameAs(document));
        Assert.That(context.Diagnostics, Is.SameAs(document.Diagnostics));
        Assert.That(document.HasErrors(), Is.True);
        Assert.That(context.GetDiagnostics()[0].Code, Is.EqualTo("E001"));
    }

    [Test]
    public void ParseContext_CanBeCreatedWithPipeline()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.GfmPreset()
            .AllowUnsafeLinks()
            .Build();

        MarkdownParseContext context = MarkdownParseContext.Create("text", pipeline);

        Assert.That(context.Pipeline.UsesGfm(), Is.True);
        Assert.That(context.Pipeline.AllowsUnsafeLinks(), Is.True);
    }

    [Test]
    public void ParseContext_RejectsInvalidArguments()
    {
        Assert.That(() => MarkdownParseContext.Create(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParseContext.Create(null!, new MarkdownDocument()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParseContext.Create("", (MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParseContext.Create(null!, MarkdownPipeline.CommonMarkPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParseContext.Create("", (MarkdownPipeline)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParseContext.Create("", new MarkdownDocument(), null!), Throws.TypeOf<ArgumentNullException>());
    }
}
