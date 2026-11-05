using System;

namespace markdown;

internal class MarkdownParseContext
{
    internal string Source;
    internal MarkdownDocument Document;
    internal MarkdownDiagnosticCollection Diagnostics;
    internal MarkdownPipeline Pipeline;
    internal MarkdownLinkReferenceMap LinkReferences;
    internal MarkdownFootnoteDefinitionMap Footnotes;
    internal MarkdownAbbreviationMap Abbreviations;

    internal MarkdownParseContext()
    {
        Source = "";
        Document = new MarkdownDocument();
        Diagnostics = Document.Diagnostics;
        Pipeline = MarkdownPipeline.CommonMarkPreset();
        LinkReferences = new MarkdownLinkReferenceMap();
        Footnotes = new MarkdownFootnoteDefinitionMap();
        Abbreviations = new MarkdownAbbreviationMap();
    }

    internal static MarkdownParseContext Create(string source)
    {
        return Create(source, new MarkdownDocument(), MarkdownPipeline.CommonMarkPreset());
    }

    internal static MarkdownParseContext Create(string source, MarkdownDocument document)
    {
        return Create(source, document, MarkdownPipeline.CommonMarkPreset());
    }

    internal static MarkdownParseContext Create(string source, MarkdownPipeline pipeline)
    {
        return Create(source, new MarkdownDocument(), pipeline);
    }

    internal static MarkdownParseContext Create(string source, MarkdownDocument document, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownParseContext context = new MarkdownParseContext();
        context.Source = MarkdownInputNormalizer.Normalize(source);
        context.Document = document;
        context.Diagnostics = document.Diagnostics;
        context.Pipeline = pipeline.Copy();
        return context;
    }

    internal void AddDiagnostic(MarkdownDiagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
    }

    internal void AddDiagnosticValues(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span)
    {
        Diagnostics.AddDiagnostic(severity, code, message, span);
    }

    internal MarkdownDiagnostic[] GetDiagnostics()
    {
        return Diagnostics.GetDiagnostics();
    }

    internal bool HasDiagnostics()
    {
        return Diagnostics.HasDiagnostics();
    }

    internal bool HasErrors()
    {
        return Diagnostics.HasErrors();
    }
}
