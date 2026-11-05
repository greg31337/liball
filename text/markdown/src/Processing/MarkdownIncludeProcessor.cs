using System;
using System.Collections.Generic;
using System.Text;

namespace markdown;

internal class MarkdownIncludeDirective
{
    internal string Name;

    internal MarkdownIncludeDirective()
    {
        Name = "";
    }
}

internal class MarkdownIncludeProcessingResult
{
    internal StringBuilder Builder;
    internal List<MarkdownSourceAttribution> Attributions;
    internal List<MarkdownDiagnostic> Diagnostics;
    internal int ExpandedOffset;
    internal int ExpandedLine;
    internal int ExpandedColumn;

    internal MarkdownIncludeProcessingResult()
    {
        Builder = new StringBuilder();
        Attributions = new List<MarkdownSourceAttribution>();
        Diagnostics = new List<MarkdownDiagnostic>();
        ExpandedOffset = 0;
        ExpandedLine = 1;
        ExpandedColumn = 1;
    }

    internal string GetSource()
    {
        return Builder.ToString();
    }
}

internal class MarkdownIncludeProcessor
{
    internal const string ResolverMissingDiagnosticCode = "MD_INCLUDE_RESOLVER_MISSING";
    internal const string NotFoundDiagnosticCode = "MD_INCLUDE_NOT_FOUND";
    internal const string CycleDiagnosticCode = "MD_INCLUDE_CYCLE";
    internal const string DepthDiagnosticCode = "MD_INCLUDE_DEPTH";
    internal const string InvalidResultDiagnosticCode = "MD_INCLUDE_INVALID_RESULT";

    internal static MarkdownIncludeProcessingResult Process(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownIncludeProcessingResult result = new MarkdownIncludeProcessingResult();
        MarkdownOptions options = pipeline.GetOptions();
        if (!options.ProcessIncludes)
        {
            result.Builder.Append(source);
            return result;
        }

        List<string> activeSources = new List<string>();
        if (options.IncludeRootName.Length > 0)
        {
            activeSources.Add(options.IncludeRootName);
        }

        ProcessSource(
            source,
            options.IncludeRootName,
            0,
            options.MaximumIncludeDepth,
            options.IncludeResolver,
            activeSources,
            result);
        return result;
    }

    internal static void ProcessSource(
        string source,
        string sourceName,
        int depth,
        int maximumDepth,
        IMarkdownIncludeResolver? resolver,
        List<string> activeSources,
        MarkdownIncludeProcessingResult result)
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines(source);
        int index = 0;
        while (index < lines.Length)
        {
            MarkdownLine line = lines[index];
            MarkdownIncludeDirective? directive = ParseDirective(line.Text);
            if (directive == null)
            {
                AppendAttributedText(line.Text + line.LineEnding, sourceName, line.LineSpan, depth, result);
                index = index + 1;
                continue;
            }

            int childDepth = depth + 1;
            if (childDepth > maximumDepth)
            {
                AppendFailure(
                    line,
                    sourceName,
                    depth,
                    DepthDiagnosticCode,
                    "Include nesting depth exceeds the configured maximum.",
                    result);
                index = index + 1;
                continue;
            }

            if (resolver == null)
            {
                AppendFailure(
                    line,
                    sourceName,
                    depth,
                    ResolverMissingDiagnosticCode,
                    "Include processing is enabled without a resolver.",
                    result);
                index = index + 1;
                continue;
            }

            MarkdownIncludeContext context = MarkdownIncludeContext.Create(directive.Name, sourceName, childDepth);
            MarkdownIncludeResult resolution = resolver.Resolve(context);
            if (resolution == null || !resolution.Found)
            {
                AppendFailure(
                    line,
                    sourceName,
                    depth,
                    NotFoundDiagnosticCode,
                    "Include source was not found: " + directive.Name,
                    result);
                index = index + 1;
                continue;
            }

            if (resolution.SourceName == null
                || resolution.SourceName.Length == 0
                || resolution.Content == null)
            {
                AppendFailure(
                    line,
                    sourceName,
                    depth,
                    InvalidResultDiagnosticCode,
                    "Include resolver returned an invalid found result.",
                    result);
                index = index + 1;
                continue;
            }

            if (ContainsSource(activeSources, resolution.SourceName))
            {
                AppendFailure(
                    line,
                    sourceName,
                    depth,
                    CycleDiagnosticCode,
                    "Include cycle detected for source: " + resolution.SourceName,
                    result);
                index = index + 1;
                continue;
            }

            int expandedStart = result.Builder.Length;
            activeSources.Add(resolution.SourceName);
            ProcessSource(
                MarkdownInputNormalizer.Normalize(resolution.Content),
                resolution.SourceName,
                childDepth,
                maximumDepth,
                resolver,
                activeSources,
                result);
            activeSources.RemoveAt(activeSources.Count - 1);

            if (line.LineEnding.Length > 0
                && (result.Builder.Length == expandedStart || !EndsWithLineEnding(result.Builder)))
            {
                AppendAttributedText(line.LineEnding, sourceName, line.LineSpan, depth, result);
            }

            index = index + 1;
        }
    }

    internal static MarkdownIncludeDirective? ParseDirective(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length && index < 4 && text[index] == ' ')
        {
            index = index + 1;
        }

        if (index > 3 || text.Length - index < 11)
        {
            return null;
        }

        if (text.Substring(index, 8) != "!INCLUDE")
        {
            return null;
        }

        index = index + 8;
        if (index >= text.Length || (text[index] != ' ' && text[index] != '\t'))
        {
            return null;
        }

        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        if (index >= text.Length || (text[index] != '"' && text[index] != '\''))
        {
            return null;
        }

        char quote = text[index];
        int nameStart = index + 1;
        int closingIndex = text.IndexOf(quote, nameStart);
        if (closingIndex <= nameStart)
        {
            return null;
        }

        index = closingIndex + 1;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        if (index != text.Length)
        {
            return null;
        }

        MarkdownIncludeDirective directive = new MarkdownIncludeDirective();
        directive.Name = text.Substring(nameStart, closingIndex - nameStart);
        return directive;
    }

    internal static bool ContainsSource(List<string> activeSources, string sourceName)
    {
        int index = 0;
        while (index < activeSources.Count)
        {
            if (activeSources[index] == sourceName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool EndsWithLineEnding(StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return false;
        }

        char last = builder[builder.Length - 1];
        return last == '\n' || last == '\r';
    }

    internal static void AppendFailure(
        MarkdownLine line,
        string sourceName,
        int depth,
        string code,
        string message,
        MarkdownIncludeProcessingResult result)
    {
        MarkdownSourcePosition start = MarkdownSourcePosition.Create(
            result.ExpandedOffset,
            result.ExpandedLine,
            result.ExpandedColumn);
        AppendAttributedText(line.Text + line.LineEnding, sourceName, line.LineSpan, depth, result);
        MarkdownSourcePosition end = MarkdownSourcePosition.Create(
            result.ExpandedOffset,
            result.ExpandedLine,
            result.ExpandedColumn);
        result.Diagnostics.Add(MarkdownDiagnostic.CreateWithSourceName(
            MarkdownDiagnosticSeverity.Error,
            code,
            message,
            MarkdownSourceSpan.Create(start, end),
            sourceName));
    }

    internal static void AppendAttributedText(
        string text,
        string sourceName,
        MarkdownSourceSpan originalSpan,
        int depth,
        MarkdownIncludeProcessingResult result)
    {
        if (text.Length == 0)
        {
            return;
        }

        MarkdownSourcePosition start = MarkdownSourcePosition.Create(
            result.ExpandedOffset,
            result.ExpandedLine,
            result.ExpandedColumn);
        result.Builder.Append(text);
        AdvanceExpandedPosition(text, result);
        MarkdownSourcePosition end = MarkdownSourcePosition.Create(
            result.ExpandedOffset,
            result.ExpandedLine,
            result.ExpandedColumn);
        result.Attributions.Add(MarkdownSourceAttribution.Create(
            sourceName,
            MarkdownSourceSpan.Create(start, end),
            originalSpan,
            depth));
    }

    internal static void AdvanceExpandedPosition(string text, MarkdownIncludeProcessingResult result)
    {
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            result.ExpandedOffset = result.ExpandedOffset + 1;
            if (current == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index = index + 1;
                    result.ExpandedOffset = result.ExpandedOffset + 1;
                }

                result.ExpandedLine = result.ExpandedLine + 1;
                result.ExpandedColumn = 1;
            }
            else if (current == '\n')
            {
                result.ExpandedLine = result.ExpandedLine + 1;
                result.ExpandedColumn = 1;
            }
            else
            {
                result.ExpandedColumn = result.ExpandedColumn + 1;
            }

            index = index + 1;
        }
    }
}
