using System;
using System.Collections.Generic;
using System.IO;

namespace markdown;

/// <summary>
/// Collects headings, links, images, text, and structural counts from Markdown.
/// </summary>
internal class MarkdownAnalyzer
{
    /// <summary>
    /// Analyzes an already parsed document without modifying it.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting <see cref="MarkdownDocumentAnalysis"/> value.</returns>
    internal static MarkdownDocumentAnalysis Analyze(MarkdownDocument document)
    {
        return AnalyzeConfigured(
            document,
            MarkdownPipeline.CommonMarkPreset(),
            new MarkdownAnalysisOptions());
    }

    internal static MarkdownDocumentAnalysis AnalyzeConfigured(
        MarkdownDocument document,
        MarkdownPipeline pipeline,
        MarkdownAnalysisOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        MarkdownAnalysisOptions configured = options.CopyValidated();
        MarkdownDocumentAnalysis analysis = new MarkdownDocumentAnalysis();
        analysis.PlainText = PlainTextRenderer.Render(document, pipeline);
        analysis.CharacterCount = analysis.PlainText.Length;
        analysis.WordCount = CountWords(analysis.PlainText);
        List<string> usedIds = new List<string>();

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            AnalyzeNode(analysis, context.Node);
            if (context.Node.Kind == MarkdownNodeKind.Heading)
            {
                MarkdownHeadingInfo heading = analysis.Headings[analysis.Headings.Count - 1];
                string baseId = heading.Id;
                if (baseId.Length == 0)
                {
                    baseId = HeadingIdGenerator.GenerateHeadingId(heading.Text);
                }

                heading.Id = MakeUniqueSlug(usedIds, baseId);
            }

            return MarkdownTreeWalkResult.Continue;
        };

        MarkdownTreeWalker.Walk(document, visitor);
        BuildHeadingOutline(analysis);
        if (configured.GenerateTableOfContents)
        {
            analysis.TableOfContents = GenerateTableOfContents(
                analysis,
                configured,
                pipeline.GetOptions().LineEnding);
        }

        CalculateReadingTime(analysis, configured);
        return analysis;
    }

    /// <summary>
    /// Parses and analyzes source text with the CommonMark preset.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting <see cref="MarkdownDocumentAnalysis"/> value.</returns>
    internal static MarkdownDocumentAnalysis Analyze(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        return AnalyzeConfigured(
            MarkdownParser.Parse(source),
            MarkdownPipeline.CommonMarkPreset(),
            new MarkdownAnalysisOptions());
    }

    /// <summary>
    /// Parses source text with the supplied pipeline and analyzes the resulting document.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocumentAnalysis"/> value.</returns>
    internal static MarkdownDocumentAnalysis Analyze(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        return AnalyzeConfigured(
            MarkdownParser.Parse(source, pipeline),
            pipeline,
            new MarkdownAnalysisOptions());
    }

    internal static MarkdownDocumentAnalysis AnalyzeConfiguredSource(
        string source,
        MarkdownPipeline pipeline,
        MarkdownAnalysisOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return AnalyzeConfigured(MarkdownParser.Parse(source, pipeline), pipeline, options);
    }

    internal static void AnalyzeNode(MarkdownDocumentAnalysis analysis, MarkdownNode node)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        analysis.NodeCount = analysis.NodeCount + 1;
        if (MarkdownNodeKindFacts.IsDocumentKind(node.Kind))
        {
            analysis.DocumentCount = analysis.DocumentCount + 1;
        }

        if (MarkdownNodeKindFacts.IsBlockKind(node.Kind))
        {
            analysis.BlockCount = analysis.BlockCount + 1;
        }

        if (MarkdownNodeKindFacts.IsInlineKind(node.Kind))
        {
            analysis.InlineCount = analysis.InlineCount + 1;
        }

        if (MarkdownNodeKindFacts.IsExtensionKind(node.Kind))
        {
            analysis.ExtensionCount = analysis.ExtensionCount + 1;
        }

        if (node.Kind == MarkdownNodeKind.Text)
        {
            analysis.TextNodeCount = analysis.TextNodeCount + 1;
        }

        if (node.Kind == MarkdownNodeKind.Heading)
        {
            AddHeading(analysis, node);
        }
        else if (node.Kind == MarkdownNodeKind.Link)
        {
            AddLink(analysis, node);
        }
        else if (node.Kind == MarkdownNodeKind.Image)
        {
            AddImage(analysis, node);
        }
        else if (node.Kind == MarkdownNodeKind.LinkReferenceDefinition)
        {
            AddLinkReferenceDefinition(analysis, node);
        }
        else if (node.Kind == MarkdownNodeKind.FrontMatter)
        {
            AddFrontMatter(analysis, node);
        }
    }

    internal static void AddHeading(MarkdownDocumentAnalysis analysis, MarkdownNode node)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind != MarkdownNodeKind.Heading)
        {
            throw new ArgumentException("Node kind must be a heading.");
        }

        MarkdownHeadingInfo heading = new MarkdownHeadingInfo(
            node,
            HtmlRenderer.GetHeadingLevel(node),
            HtmlRenderer.GetPlainText(node),
            node.GetAttributeValue("id"));
        analysis.Headings.Add(heading);
    }

    internal static void AddLink(MarkdownDocumentAnalysis analysis, MarkdownNode node)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind != MarkdownNodeKind.Link)
        {
            throw new ArgumentException("Node kind must be a link.");
        }

        MarkdownLinkInfo link = new MarkdownLinkInfo(
            node,
            node.GetAttributeValue("destination"),
            node.GetAttributeValue("title"),
            HtmlRenderer.GetPlainText(node));
        analysis.Links.Add(link);
    }

    internal static void AddImage(MarkdownDocumentAnalysis analysis, MarkdownNode node)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind != MarkdownNodeKind.Image)
        {
            throw new ArgumentException("Node kind must be an image.");
        }

        MarkdownImageInfo image = new MarkdownImageInfo(
            node,
            node.GetAttributeValue("destination"),
            node.GetAttributeValue("title"),
            HtmlRenderer.GetPlainText(node));
        analysis.Images.Add(image);
    }

    internal static void AddLinkReferenceDefinition(
        MarkdownDocumentAnalysis analysis,
        MarkdownNode node)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind != MarkdownNodeKind.LinkReferenceDefinition)
        {
            throw new ArgumentException("Node kind must be a link-reference definition.");
        }

        MarkdownLinkReferenceInfo item = new MarkdownLinkReferenceInfo();
        item.Node = node;
        item.Label = node.GetAttributeValue("label");
        item.NormalizedLabel = node.GetAttributeValue("normalizedLabel");
        item.Destination = node.GetAttributeValue("destination");
        item.Title = node.GetAttributeValue("title");
        analysis.LinkReferenceDefinitions.Add(item);
    }

    internal static void AddFrontMatter(MarkdownDocumentAnalysis analysis, MarkdownNode node)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind != MarkdownNodeKind.FrontMatter)
        {
            throw new ArgumentException("Node kind must be front matter.");
        }

        MarkdownFrontMatterInfo item = new MarkdownFrontMatterInfo();
        item.Node = node;
        item.Format = node.GetAttributeValue("format");
        item.Delimiter = node.GetAttributeValue("delimiter");
        item.Content = node.Literal;
        analysis.FrontMatter.Add(item);
    }

    internal static string MakeUniqueSlug(List<string> usedIds, string baseId)
    {
        if (usedIds == null)
        {
            throw new ArgumentNullException("usedIds");
        }

        if (baseId == null)
        {
            throw new ArgumentNullException("baseId");
        }

        string root = baseId;
        if (root.Length == 0)
        {
            root = "section";
        }

        string candidate = root;
        int suffix = 1;
        while (ContainsText(usedIds, candidate))
        {
            candidate = root
                + "-"
                + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }

        usedIds.Add(candidate);
        return candidate;
    }

    internal static bool ContainsText(List<string> values, string value)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void BuildHeadingOutline(MarkdownDocumentAnalysis analysis)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        int index = 0;
        while (index < analysis.Headings.Count)
        {
            MarkdownHeadingOutlineItem item = new MarkdownHeadingOutlineItem();
            item.Heading = analysis.Headings[index].Copy();
            int parentIndex = index - 1;
            while (parentIndex >= 0
                && analysis.Headings[parentIndex].Level >= item.Heading.Level)
            {
                parentIndex = parentIndex - 1;
            }

            item.ParentIndex = parentIndex;
            if (parentIndex >= 0)
            {
                item.Depth = analysis.HeadingOutline[parentIndex].Depth + 1;
            }

            analysis.HeadingOutline.Add(item);
            index = index + 1;
        }
    }

    internal static string GenerateTableOfContents(
        MarkdownDocumentAnalysis analysis,
        MarkdownAnalysisOptions options,
        string lineEnding)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (lineEnding == null)
        {
            throw new ArgumentNullException("lineEnding");
        }

        StringWriter writer = new StringWriter();
        List<int> includedLevels = new List<int>();
        int index = 0;
        while (index < analysis.Headings.Count)
        {
            MarkdownHeadingInfo heading = analysis.Headings[index];
            if (heading.Level >= options.TableOfContentsMinimumLevel
                && heading.Level <= options.TableOfContentsMaximumLevel)
            {
                while (includedLevels.Count > 0
                    && includedLevels[includedLevels.Count - 1] >= heading.Level)
                {
                    includedLevels.RemoveAt(includedLevels.Count - 1);
                }

                int depth = includedLevels.Count;
                writer.Write(new string(' ', depth * 2));
                writer.Write("- [");
                writer.Write(MarkdownRenderer.EscapeText(NormalizeTableOfContentsText(heading.Text)));
                writer.Write("](");
                writer.Write(GetTableOfContentsDestination(heading.Id));
                writer.Write(")");
                writer.Write(lineEnding);
                includedLevels.Add(heading.Level);
            }

            index = index + 1;
        }

        return writer.ToString();
    }

    internal static string NormalizeTableOfContentsText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return MarkdownRenderer.NormalizeLineEndings(text).Replace("\n", " ");
    }

    internal static string GetTableOfContentsDestination(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        string destination = "#" + id;
        if (MarkdownRenderer.NeedsAngleDestination(destination))
        {
            return "<" + MarkdownRenderer.EscapeAngleDestination(destination) + ">";
        }

        return MarkdownRenderer.EscapeBareDestination(destination);
    }

    internal static void CalculateReadingTime(
        MarkdownDocumentAnalysis analysis,
        MarkdownAnalysisOptions options)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException("analysis");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        long seconds = 0;
        if (analysis.WordCount > 0)
        {
            seconds = ((long)analysis.WordCount * 60 + options.WordsPerMinute - 1)
                / options.WordsPerMinute;
        }

        if (seconds < options.MinimumReadingTimeSeconds)
        {
            seconds = options.MinimumReadingTimeSeconds;
        }

        analysis.ReadingTimeSeconds = ClampToInteger(seconds);
        analysis.ReadingTimeMinutes = ClampToInteger((seconds + 59) / 60);
    }

    internal static int ClampToInteger(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)value;
    }

    internal static int CountWords(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int count = 0;
        bool inWord = false;
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsLetterOrDigit(text[index]))
            {
                if (!inWord)
                {
                    count = count + 1;
                    inWord = true;
                }
            }
            else
            {
                inWord = false;
            }

            index = index + 1;
        }

        return count;
    }
}
