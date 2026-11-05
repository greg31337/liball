using System;
using System.IO;

namespace markdown;

/// <summary>
/// Provides a chainable in-memory Markdown generator.
/// </summary>
/// <remarks>
/// The builder delegates to <see cref="MarkdownWriter"/> and returns itself from mutating methods. Call <see cref="MarkdownBuilder.Clear"/> to reuse its buffer.
/// </remarks>
internal class MarkdownBuilder
{
    internal StringWriter Output;
    internal MarkdownWriter Writer;

    /// <summary>
    /// Initializes a new <see cref="MarkdownBuilder"/> instance.
    /// </summary>
    internal MarkdownBuilder()
    {
        Output = new StringWriter();
        Writer = new MarkdownWriter(Output);
    }

    /// <summary>
    /// Creates a builder with a configured line ending.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    internal static MarkdownBuilder Create(string lineEnding)
    {
        MarkdownBuilder builder = new MarkdownBuilder();
        builder.Writer.SetLineEnding(lineEnding);
        return builder;
    }

    /// <summary>
    /// Sets line ending.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownBuilder SetLineEnding(string lineEnding)
    {
        Writer.SetLineEnding(lineEnding);
        return this;
    }

    /// <summary>
    /// Appends Markdown text verbatim.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder Write(string text)
    {
        Writer.Write(text);
        return this;
    }

    /// <summary>
    /// Writes raw.
    /// </summary>
    /// <param name="markdown">The Markdown text to write without escaping.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder WriteRaw(string markdown)
    {
        Writer.WriteRaw(markdown);
        return this;
    }

    /// <summary>
    /// Writes one configured line ending.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder LineEnding()
    {
        Writer.WriteLineEnding();
        return this;
    }

    /// <summary>
    /// Writes two configured line endings.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder BlankLine()
    {
        Writer.BlankLine();
        return this;
    }

    /// <summary>
    /// Writes an ATX heading after escaping its text.
    /// </summary>
    /// <param name="level">The heading level from 1 through 6.</param>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder Heading(int level, string text)
    {
        Writer.Heading(level, text);
        return this;
    }

    /// <summary>
    /// Writes escaped paragraph text followed by a line ending.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder Paragraph(string text)
    {
        Writer.Paragraph(text);
        return this;
    }

    /// <summary>
    /// Writes a fenced code block using a fence long enough to contain the code safely.
    /// </summary>
    /// <param name="info">The optional code-fence information string.</param>
    /// <param name="code">The code block content.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder CodeBlock(string info, string code)
    {
        Writer.CodeBlock(info, code);
        return this;
    }

    /// <summary>
    /// Writes a Markdown link with escaped text and a safely delimited destination.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="url">The link or image destination.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder Link(string text, string url)
    {
        Writer.Link(text, url);
        return this;
    }

    /// <summary>
    /// Writes a Markdown image with escaped alternative text and a safely delimited destination.
    /// </summary>
    /// <param name="alt">The image alternative text.</param>
    /// <param name="url">The link or image destination.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder Image(string alt, string url)
    {
        Writer.Image(alt, url);
        return this;
    }

    /// <summary>
    /// Writes a pipe table whose first row is the header.
    /// </summary>
    /// <param name="rows">The table rows; the first row becomes the header.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal MarkdownBuilder Table(string[][] rows)
    {
        Writer.Table(rows);
        return this;
    }

    /// <summary>
    /// Clears all generated output while retaining the configured line ending.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownBuilder Clear()
    {
        Output.GetStringBuilder().Clear();
        return this;
    }

    /// <summary>
    /// Returns all Markdown generated so far without clearing the builder.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string ToMarkdown()
    {
        return Output.ToString();
    }

    /// <summary>
    /// Parses the generated Markdown with the CommonMark preset.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal MarkdownDocument ToDocument()
    {
        return MarkdownParser.Parse(ToMarkdown());
    }

    /// <summary>
    /// Parses the generated Markdown with the supplied pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal MarkdownDocument ToDocumentWithPipeline(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        return MarkdownParser.Parse(ToMarkdown(), pipeline);
    }
}
