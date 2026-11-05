using System;
using System.IO;

namespace markdown;

/// <summary>
/// Generates stable Markdown directly into a caller-owned text writer.
/// </summary>
public class MarkdownGenerationWriter : IMarkdownWriter
{
    internal MarkdownWriter Writer;

    /// <summary>
    /// Initializes a generator with LF line endings and no practical output-size limit.
    /// </summary>
    /// <param name="writer">The destination writer, which remains owned by the caller.</param>
    public MarkdownGenerationWriter(TextWriter writer)
    {
        Writer = new MarkdownWriter(writer);
    }

    internal void Configure(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownOptions options = pipeline.GetOptions();
        Writer.SetLineEnding(options.LineEnding);
        Writer.OutputSizeLimiter = new MarkdownOutputSizeLimiter(options.MaximumOutputSize);
    }

    /// <summary>Sets the line ending used by subsequent block operations.</summary>
    /// <param name="lineEnding">Use <c>\n</c> or <c>\r\n</c>.</param>
    public void SetLineEnding(string lineEnding) { Writer.SetLineEnding(lineEnding); }
    /// <summary>Writes text after escaping Markdown syntax.</summary><param name="text">The text.</param>
    public void WriteText(string text) { Writer.Text(text); }
    /// <summary>Writes caller-supplied Markdown without escaping.</summary><param name="markdown">The Markdown.</param>
    public void WriteRawMarkdown(string markdown) { Writer.WriteRaw(markdown); }
    /// <summary>Writes one configured line ending.</summary>
    public void WriteLineEnding() { Writer.WriteLineEnding(); }
    /// <summary>Writes a blank-line separator.</summary>
    public void WriteBlankLine() { Writer.BlankLine(); }
    /// <summary>Writes an ATX heading.</summary><param name="level">The level.</param><param name="text">The text.</param>
    public void WriteHeading(int level, string text) { Writer.Heading(level, text); }
    /// <summary>Writes an escaped paragraph.</summary><param name="text">The text.</param>
    public void WriteParagraph(string text) { Writer.Paragraph(text); }
    /// <summary>Writes a thematic break.</summary>
    public void WriteThematicBreak() { Writer.ThematicBreak(); }
    /// <summary>Writes a bullet list.</summary><param name="items">The items.</param>
    public void WriteBulletList(string[] items) { Writer.BulletList(items); }
    /// <summary>Writes an ordered list.</summary><param name="items">The items.</param><param name="start">The first number.</param>
    public void WriteOrderedList(string[] items, int start) { Writer.OrderedList(items, start); }
    /// <summary>Writes a GFM task list.</summary><param name="items">The tasks.</param><param name="checkedItems">The checked states.</param>
    public void WriteTaskList(string[] items, bool[] checkedItems) { Writer.TaskList(items, checkedItems); }
    /// <summary>Writes an escaped block quote.</summary><param name="text">The text.</param>
    public void WriteBlockQuote(string text) { Writer.BlockQuote(text); }
    /// <summary>Writes a fenced code block.</summary><param name="info">The information string.</param><param name="code">The code.</param>
    public void WriteCodeBlock(string info, string code) { Writer.CodeBlock(info, code); }
    /// <summary>Writes an inline code span.</summary><param name="code">The code.</param>
    public void WriteCodeSpan(string code) { Writer.CodeSpan(code); }
    /// <summary>Writes emphasized text.</summary><param name="text">The text.</param>
    public void WriteEmphasis(string text) { Writer.Emphasis(text); }
    /// <summary>Writes strongly emphasized text.</summary><param name="text">The text.</param>
    public void WriteStrongEmphasis(string text) { Writer.StrongEmphasis(text); }
    /// <summary>Writes GFM strikethrough text.</summary><param name="text">The text.</param>
    public void WriteStrikethrough(string text) { Writer.Strikethrough(text); }
    /// <summary>Writes an inline link.</summary><param name="text">The text.</param><param name="url">The destination.</param>
    public void WriteLink(string text, string url) { Writer.Link(text, url); }
    /// <summary>Writes an inline image.</summary><param name="alt">The alternative text.</param><param name="url">The destination.</param>
    public void WriteImage(string alt, string url) { Writer.Image(alt, url); }
    /// <summary>Writes a GFM pipe table.</summary><param name="rows">The rows.</param>
    public void WriteTable(string[][] rows) { Writer.Table(rows); }
    /// <summary>Writes an inline math span.</summary><param name="expression">The expression.</param>
    public void WriteMathSpan(string expression) { Writer.MathSpan(expression); }
    /// <summary>Writes a math block.</summary><param name="expression">The expression.</param>
    public void WriteMathBlock(string expression) { Writer.MathBlock(expression); }
    /// <summary>Writes an admonition.</summary><param name="type">The type.</param><param name="title">The title.</param><param name="markdownContent">The raw content.</param>
    public void WriteAdmonition(string type, string title, string markdownContent) { Writer.Admonition(type, title, markdownContent); }
    /// <summary>Writes a custom container.</summary><param name="name">The name.</param><param name="title">The title.</param><param name="markdownContent">The raw content.</param>
    public void WriteCustomContainer(string name, string title, string markdownContent) { Writer.CustomContainer(name, title, markdownContent); }
    /// <summary>Writes a footnote definition.</summary><param name="label">The label.</param><param name="text">The text.</param>
    public void WriteFootnoteDefinition(string label, string text) { Writer.FootnoteDefinition(label, text); }
    /// <summary>Writes a footnote reference.</summary><param name="label">The label.</param>
    public void WriteFootnoteReference(string label) { Writer.FootnoteReference(label); }
    /// <summary>Returns text escaped for stable Markdown output.</summary><param name="text">The text.</param><returns>The escaped text.</returns>
    public string EscapeText(string text)
    {
        return MarkdownRenderer.EscapeParagraphLineStarts(MarkdownRenderer.EscapeText(text));
    }
}
