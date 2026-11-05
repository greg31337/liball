using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;

/// <summary>Converts HTML trees or source text into readable plain text.</summary>
internal static class HtmlTextConverter
{
    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <returns>The converted plain text, completion status, and diagnostics.</returns>
    public static HtmlTextResult Convert(string html)
    {
        return Convert(html, HtmlTextOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The converted plain text, completion status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlTextResult Convert(string html,
        IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ConvertCompiled(html, options.Compile(), cancellationToken);
    }

    internal static HtmlTextResult ConvertCompiled(string html,
        HtmlCompiledTextOptions options,
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
    /// <returns>The converted plain text, completion status, and diagnostics.</returns>
    public static HtmlTextResult Convert(HtmlDocument document)
    {
        return Convert(document, HtmlTextOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The converted plain text, completion status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlTextResult Convert(HtmlDocument document,
        IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ConvertCompiled(document, options.Compile(),
            cancellationToken);
    }

    internal static HtmlTextResult ConvertCompiled(HtmlDocument document,
        HtmlCompiledTextOptions options,
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
    /// <returns>The converted plain text, completion status, and diagnostics.</returns>
    public static HtmlTextResult Convert(HtmlNode node)
    {
        return Convert(node, HtmlTextOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Converts the supplied HTML into the configured output format.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The converted plain text, completion status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlTextResult Convert(HtmlNode node,
        IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return ConvertCompiled(node, options.Compile(), cancellationToken);
    }

    internal static HtmlTextResult ConvertCompiled(HtmlNode node,
        HtmlCompiledTextOptions options,
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

    internal static HtmlTextResult Convert(HtmlNode? node,
        HtmlCompiledTextOptions options,
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

    internal static HtmlTextResult ConvertCore(HtmlNode? node,
        HtmlCompiledTextOptions options,
        CancellationToken cancellationToken,
        HtmlDiagnostic[] diagnostics, bool success, bool truncated,
        HtmlBudget? budget)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HtmlTextOperationContext? context = new HtmlTextOperationContext(
            options, cancellationToken, budget);
        context.AddDiagnostics(diagnostics);
        if (truncated)
        {
            context.MarkTruncated();
        }

        string text = "";
        if (node != null)
        {
            HtmlTextWriter writer = new HtmlTextWriter(options, context,
                cancellationToken);
            writer.WriteNode(node);
            text = writer.Complete();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (context.WasTruncated())
        {
            text = AppendTruncationMarker(text,
                options.GetTruncationMarker(),
                options.GetLimits().GetMaximumOutputCharacters());
        }

        return context.CreateResult(text, success);
    }

    internal static string AppendTruncationMarker(string text, string marker,
        int maximumCharacters)
    {
        if (marker.Length == 0)
        {
            return text;
        }

        if (marker.Length >= maximumCharacters)
        {
            return GetPrefix(marker, maximumCharacters);
        }

        int textCharacters = maximumCharacters - marker.Length;
        string prefix = text.Length <= textCharacters
            ? text : GetPrefix(text, textCharacters);
        return prefix + marker;
    }

    internal static string GetPrefix(string value, int maximumCharacters)
    {
        if (value.Length <= maximumCharacters)
        {
            return value;
        }

        StringBuilder builder = new StringBuilder(maximumCharacters);
        TextElementEnumerator elements =
            StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (builder.Length + element.Length > maximumCharacters)
            {
                break;
            }

            builder.Append(element);
        }

        return builder.ToString();
    }

    internal static void ValidateOptions(HtmlCompiledTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }
}
