using System;
using System.IO;

namespace markdown;

/// <summary>
/// Provides chainable in-memory Markdown generation and document conversion.
/// </summary>
public class MarkdownGenerationBuilder : IMarkdownBuilder
{
    internal StringWriter Output;
    internal MarkdownWriter Writer;
    internal int MaximumOutputSize;

    /// <summary>Initializes an empty builder with LF line endings.</summary>
    public MarkdownGenerationBuilder()
    {
        Output = new StringWriter();
        Writer = new MarkdownWriter(Output);
        MaximumOutputSize = int.MaxValue;
    }

    internal void Configure(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownOptions options = pipeline.GetOptions();
        MaximumOutputSize = options.MaximumOutputSize;
        Writer.SetLineEnding(options.LineEnding);
        Writer.OutputSizeLimiter = new MarkdownOutputSizeLimiter(MaximumOutputSize);
    }

    /// <summary>Sets the line ending used by subsequent block operations.</summary><param name="lineEnding">The line ending.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder SetLineEnding(string lineEnding) { Writer.SetLineEnding(lineEnding); return this; }
    /// <summary>Appends escaped text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Text(string text) { Writer.Text(text); return this; }
    /// <summary>Appends raw Markdown.</summary><param name="markdown">The Markdown.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder RawMarkdown(string markdown) { Writer.WriteRaw(markdown); return this; }
    /// <summary>Appends one line ending.</summary><returns>This builder.</returns>
    public MarkdownGenerationBuilder LineEnding() { Writer.WriteLineEnding(); return this; }
    /// <summary>Appends a blank-line separator.</summary><returns>This builder.</returns>
    public MarkdownGenerationBuilder BlankLine() { Writer.BlankLine(); return this; }
    /// <summary>Appends an ATX heading.</summary><param name="level">The level.</param><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Heading(int level, string text) { Writer.Heading(level, text); return this; }
    /// <summary>Appends an escaped paragraph.</summary><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Paragraph(string text) { Writer.Paragraph(text); return this; }
    /// <summary>Appends a thematic break.</summary><returns>This builder.</returns>
    public MarkdownGenerationBuilder ThematicBreak() { Writer.ThematicBreak(); return this; }
    /// <summary>Appends a bullet list.</summary><param name="items">The items.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder BulletList(string[] items) { Writer.BulletList(items); return this; }
    /// <summary>Appends an ordered list.</summary><param name="items">The items.</param><param name="start">The first number.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder OrderedList(string[] items, int start) { Writer.OrderedList(items, start); return this; }
    /// <summary>Appends a GFM task list.</summary><param name="items">The tasks.</param><param name="checkedItems">The checked states.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder TaskList(string[] items, bool[] checkedItems) { Writer.TaskList(items, checkedItems); return this; }
    /// <summary>Appends an escaped block quote.</summary><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder BlockQuote(string text) { Writer.BlockQuote(text); return this; }
    /// <summary>Appends a fenced code block.</summary><param name="info">The information string.</param><param name="code">The code.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder CodeBlock(string info, string code) { Writer.CodeBlock(info, code); return this; }
    /// <summary>Appends an inline code span.</summary><param name="code">The code.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder CodeSpan(string code) { Writer.CodeSpan(code); return this; }
    /// <summary>Appends emphasized text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Emphasis(string text) { Writer.Emphasis(text); return this; }
    /// <summary>Appends strongly emphasized text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder StrongEmphasis(string text) { Writer.StrongEmphasis(text); return this; }
    /// <summary>Appends GFM strikethrough text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Strikethrough(string text) { Writer.Strikethrough(text); return this; }
    /// <summary>Appends an inline link.</summary><param name="text">The text.</param><param name="url">The destination.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Link(string text, string url) { Writer.Link(text, url); return this; }
    /// <summary>Appends an inline image.</summary><param name="alt">The alternative text.</param><param name="url">The destination.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Image(string alt, string url) { Writer.Image(alt, url); return this; }
    /// <summary>Appends a GFM pipe table.</summary><param name="rows">The rows.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Table(string[][] rows) { Writer.Table(rows); return this; }
    /// <summary>Appends an inline math span.</summary><param name="expression">The expression.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder MathSpan(string expression) { Writer.MathSpan(expression); return this; }
    /// <summary>Appends a math block.</summary><param name="expression">The expression.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder MathBlock(string expression) { Writer.MathBlock(expression); return this; }
    /// <summary>Appends an admonition.</summary><param name="type">The type.</param><param name="title">The title.</param><param name="markdownContent">The raw content.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder Admonition(string type, string title, string markdownContent) { Writer.Admonition(type, title, markdownContent); return this; }
    /// <summary>Appends a custom container.</summary><param name="name">The name.</param><param name="title">The title.</param><param name="markdownContent">The raw content.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder CustomContainer(string name, string title, string markdownContent) { Writer.CustomContainer(name, title, markdownContent); return this; }
    /// <summary>Appends a footnote definition.</summary><param name="label">The label.</param><param name="text">The text.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder FootnoteDefinition(string label, string text) { Writer.FootnoteDefinition(label, text); return this; }
    /// <summary>Appends a footnote reference.</summary><param name="label">The label.</param><returns>This builder.</returns>
    public MarkdownGenerationBuilder FootnoteReference(string label) { Writer.FootnoteReference(label); return this; }

    /// <summary>Clears generated output while retaining configuration.</summary><returns>This builder.</returns>
    public MarkdownGenerationBuilder Clear()
    {
        string lineEnding = Writer.LineEnding;
        Output.GetStringBuilder().Clear();
        Writer = MarkdownWriter.Create(Output, lineEnding, MaximumOutputSize);
        return this;
    }

    /// <summary>Returns the generated Markdown.</summary><returns>The generated Markdown.</returns>
    public string ToMarkdown() { return Output.ToString(); }
    /// <summary>Parses generated Markdown with CommonMark defaults.</summary><returns>The generated document.</returns>
    public MarkdownDocument ToDocument() { return MarkdownParser.Parse(ToMarkdown()); }
    /// <summary>Parses generated Markdown with a supplied pipeline.</summary><param name="pipeline">The pipeline.</param><returns>The generated document.</returns>
    public MarkdownDocument ToDocumentWithPipeline(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        return MarkdownParser.Parse(ToMarkdown(), pipeline);
    }
}
