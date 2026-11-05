using System;
using System.Threading;

namespace html;

/// <summary>Converts HTML trees or source text into Markdown with configurable element rules.</summary>
internal static class HtmlMarkdownConverter
{
    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <returns>The converted Markdown, completion status, and diagnostics.</returns>
    public static HtmlMarkdownResult Convert(string html)
    {
        return Convert(html, HtmlMarkdownOptions.CreateCommonMark(),
            CancellationToken.None);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The converted Markdown, completion status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlMarkdownResult Convert(string html,
        IHtmlMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ConvertCompiled(html, options.Compile(), cancellationToken);
    }

    internal static HtmlMarkdownResult ConvertCompiled(string html,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        ValidateOptions(options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlParseOptions parseOptions = HtmlParseOptions.CreateDefault();
        parseOptions.SetLimits(options.GetLimits());
        parseOptions.SetBaseUrl(options.GetBaseUrl());
        HtmlParseResult parsed = Html.Parse(html, parseOptions,
            cancellationToken);
        HtmlDocument document = parsed.GetDocument();
        HtmlNode? node = document == null ? null : document.GetRoot();
        return ConvertCore(node, options, cancellationToken,
            parsed.GetDiagnostics(), parsed.IsSuccess(),
            parsed.WasTruncated() || !parsed.IsComplete(), null);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <returns>The converted Markdown, completion status, and diagnostics.</returns>
    public static HtmlMarkdownResult Convert(HtmlDocument document)
    {
        return Convert(document, HtmlMarkdownOptions.CreateCommonMark(),
            CancellationToken.None);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The converted Markdown, completion status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlMarkdownResult Convert(HtmlDocument document,
        IHtmlMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ConvertCompiled(document, options.Compile(),
            cancellationToken);
    }

    internal static HtmlMarkdownResult ConvertCompiled(HtmlDocument document,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        ValidateOptions(options);
        return ConvertCore(document.GetRoot(), options, cancellationToken,
            document.GetDiagnostics(), true, false, null);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <returns>The converted Markdown, completion status, and diagnostics.</returns>
    public static HtmlMarkdownResult Convert(HtmlNode node)
    {
        return Convert(node, HtmlMarkdownOptions.CreateCommonMark(),
            CancellationToken.None);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The converted Markdown, completion status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlMarkdownResult Convert(HtmlNode node,
        IHtmlMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ConvertCompiled(node, options.Compile(), cancellationToken);
    }

    internal static HtmlMarkdownResult ConvertCompiled(HtmlNode node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ValidateOptions(options);
        return ConvertCore(node, options, cancellationToken,
            new HtmlDiagnostic[0], true, false, null);
    }

    internal static HtmlMarkdownResult Convert(HtmlNode? node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlBudget budget)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ValidateOptions(options);
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        return ConvertCore(node, options, cancellationToken,
            new HtmlDiagnostic[0], true, false, budget);
    }

    internal static HtmlMarkdownResult ConvertCore(HtmlNode? node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken,
        HtmlDiagnostic[] diagnostics, bool success, bool truncated,
        HtmlBudget? budget)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HtmlMarkdownOperationContext? context =
            new HtmlMarkdownOperationContext(options, cancellationToken,
                budget);
        context.AddDiagnostics(diagnostics);
        if (truncated)
        {
            context.MarkTruncated();
        }

        string markdown = "";
        bool treeValid = node == null || context.TryMeasureTree(node);
        if (node != null && treeValid)
        {
            System.Threading.Tasks.Task<string> writeTask =
                System.Threading.Tasks.Task.Run(() =>
                {
                    HtmlMarkdownWriter writer =
                        HtmlMarkdownWriter.Create(options, cancellationToken);
                    writer.WriteNode(node);
                    return writer.Complete();
                });
            while (!writeTask.IsCompleted)
            {
                Thread.Yield();
            }

            if (writeTask.IsCompletedSuccessfully)
            {
                markdown = writeTask.Result;
            }
            else
            {
                AggregateException? aggregate = writeTask.Exception;
                if (aggregate == null)
                {
                    throw new InvalidOperationException(
                        "A failed Markdown write task must contain an exception.");
                }

                Exception exception = aggregate.GetBaseException();
                if (exception.GetType().Name ==
                    "InvalidOperationException"
                    && exception.Data.Contains(
                        "HtmlMarkdownUnknownElement"))
                {
                    HtmlNode? unknownNode = (HtmlNode?)exception.Data[
                        "HtmlMarkdownUnknownElement"];
                    if (unknownNode == null)
                    {
                        throw new InvalidOperationException(
                            "An unknown-element failure must identify its node.");
                    }

                    context.MarkUnknownElement(exception, unknownNode);
                }
                else if (exception.GetType().Name ==
                    "HtmlSerializationLimitException")
                {
                    context.MarkTruncated();
                }
                else
                {
                    throw exception;
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        markdown = context.ApplyOutputLimit(markdown,
            options.GetTruncationMarker());
        return context.CreateResult(markdown, success);
    }

    internal static void ValidateOptions(HtmlCompiledMarkdownOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }
}
