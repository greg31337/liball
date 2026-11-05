using System;
using System.IO;

namespace markdown;

/// <summary>
/// Parses Markdown text or a text reader into a document using a pipeline.
/// </summary>
internal class MarkdownParser
{
    internal const string InputSizeDiagnosticCode = "MD_LIMIT_INPUT_SIZE";
    internal const string NestingDepthDiagnosticCode = "MD_LIMIT_NESTING_DEPTH";
    internal const string ReferenceDefinitionDiagnosticCode = "MD_LIMIT_REFERENCE_DEFINITIONS";

    /// <summary>
    /// Parses source text with the CommonMark preset.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument Parse(string source)
    {
        return Parse(source, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>
    /// Normalizes and parses source text, runs registered transforms, and returns the resulting document.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument Parse(string source, MarkdownPipeline pipeline)
    {
        MarkdownParseContext context = CreateContext(source, pipeline);
        Run(context);
        return context.Document;
    }

    /// <summary>
    /// Reads the remaining text from a reader, then parses and transforms it with the supplied pipeline.
    /// </summary>
    /// <param name="reader">The text reader to consume from its current position. The caller retains ownership of it.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument ParseReader(TextReader reader, MarkdownPipeline pipeline)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        return Parse(reader.ReadToEnd(), pipeline);
    }

    /// <summary>
    /// Scans and parses a character memory region, runs registered transforms, and returns the resulting document.
    /// </summary>
    /// <param name="source">The Markdown source memory.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument ParseMemory(ReadOnlyMemory<char> source, MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        string normalized = MarkdownInputNormalizer.NormalizeMemory(source);
        MarkdownParseContext context = CreateContextFromNormalizedSource(normalized, pipeline);
        Run(context);
        return context.Document;
    }

    internal static MarkdownParseContext CreateContext(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        string normalized = MarkdownInputNormalizer.Normalize(source);
        return CreateContextFromNormalizedSource(normalized, pipeline);
    }

    internal static MarkdownParseContext CreateContextFromNormalizedSource(string normalizedSource, MarkdownPipeline pipeline)
    {
        if (normalizedSource == null)
        {
            throw new ArgumentNullException("normalizedSource");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownIncludeProcessingResult includeResult = MarkdownIncludeProcessor.Process(normalizedSource, pipeline);
        string expandedSource = includeResult.GetSource();
        MarkdownDocument document = MarkdownDocument.Create(GetDocumentSpan(expandedSource));
        document.Source = expandedSource;
        document.SourceAttributions = new MarkdownSourceAttribution[includeResult.Attributions.Count];
        int index = 0;
        while (index < includeResult.Attributions.Count)
        {
            document.SourceAttributions[index] = includeResult.Attributions[index].Copy();
            index = index + 1;
        }

        index = 0;
        while (index < includeResult.Diagnostics.Count)
        {
            document.Diagnostics.Add(includeResult.Diagnostics[index]);
            index = index + 1;
        }

        return MarkdownParseContext.Create(expandedSource, document, pipeline);
    }

    internal static void Run(MarkdownParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        AddLimitDiagnostics(context);
        BlockParser.Parse(context);
        AddReferenceDefinitionLimitDiagnostics(context);
        InlineParser.Parse(context);
        if (context.Pipeline.GetFeatures().Abbreviations)
        {
            AbbreviationInlineProcessor.Apply(context.Document, context.Abbreviations);
        }

        if (context.Pipeline.GetFeatures().Typography)
        {
            TypographyProcessor.Apply(context.Document);
        }

        if (context.Pipeline.GetFeatures().HeadingIds)
        {
            HeadingIdGenerator.Apply(context.Document);
        }

        if (context.Pipeline.TracksTrivia())
        {
            MarkdownTriviaCapture.Apply(context.Document, context.Source);
        }

        MarkdownTransformPipeline.Apply(context);
        AddNestingDepthLimitDiagnostics(context);
    }

    internal static void AddLimitDiagnostics(MarkdownParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownOptions options = context.Pipeline.GetOptions();
        if (context.Source.Length > options.MaximumInputSize)
        {
            context.AddDiagnosticValues(
                MarkdownDiagnosticSeverity.Error,
                InputSizeDiagnosticCode,
                "Input size exceeds the configured maximum.",
                context.Document.Root.Span);
        }
    }

    internal static void AddReferenceDefinitionLimitDiagnostics(MarkdownParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownOptions options = context.Pipeline.GetOptions();
        if (context.LinkReferences.GetCount() > options.MaximumReferenceDefinitions)
        {
            context.AddDiagnosticValues(
                MarkdownDiagnosticSeverity.Error,
                ReferenceDefinitionDiagnosticCode,
                "Link reference definition count exceeds the configured maximum.",
                context.Document.Root.Span);
        }
    }

    internal static void AddNestingDepthLimitDiagnostics(MarkdownParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownOptions options = context.Pipeline.GetOptions();
        AddNestingDepthLimitDiagnostics(context, context.Document.Root, 0, options.MaximumNestingDepth);
    }

    internal static void AddNestingDepthLimitDiagnostics(
        MarkdownParseContext context,
        MarkdownNode node,
        int depth,
        int maximumDepth)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (depth > maximumDepth)
        {
            context.AddDiagnosticValues(
                MarkdownDiagnosticSeverity.Error,
                NestingDepthDiagnosticCode,
                "Document nesting depth exceeds the configured maximum.",
                node.Span);
            return;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            AddNestingDepthLimitDiagnostics(context, node.Children[index], depth + 1, maximumDepth);
            index = index + 1;
        }
    }

    internal static MarkdownSourceSpan GetDocumentSpan(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        MarkdownLine[] lines = MarkdownLineReader.ReadLines(source);
        if (lines.Length == 0)
        {
            return new MarkdownSourceSpan();
        }

        MarkdownLine lastLine = lines[lines.Length - 1];
        return MarkdownSourceSpan.Create(
            MarkdownSourcePosition.Create(0, 1, 1),
            lastLine.LineSpan.End);
    }
}
