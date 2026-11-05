using System.IO;

namespace markdown;

/// <summary>
/// Defines streaming operations for generating stable Markdown into a caller-owned writer.
/// </summary>
public interface IMarkdownWriter
{
    /// <summary>Sets the line ending used by subsequent block operations.</summary>
    /// <param name="lineEnding">Use <c>\n</c> or <c>\r\n</c>.</param>
    void SetLineEnding(string lineEnding);
    /// <summary>Writes text after escaping Markdown syntax.</summary>
    /// <param name="text">The text to escape and write.</param>
    void WriteText(string text);
    /// <summary>Writes caller-supplied Markdown without escaping.</summary>
    /// <param name="markdown">The raw Markdown.</param>
    void WriteRawMarkdown(string markdown);
    /// <summary>Writes one configured line ending.</summary>
    void WriteLineEnding();
    /// <summary>Writes a blank-line separator.</summary>
    void WriteBlankLine();
    /// <summary>Writes an ATX heading.</summary>
    /// <param name="level">The heading level from one through six.</param>
    /// <param name="text">The heading text.</param>
    void WriteHeading(int level, string text);
    /// <summary>Writes an escaped paragraph.</summary>
    /// <param name="text">The paragraph text.</param>
    void WriteParagraph(string text);
    /// <summary>Writes a thematic break.</summary>
    void WriteThematicBreak();
    /// <summary>Writes a bullet list.</summary>
    /// <param name="items">The list item text values.</param>
    void WriteBulletList(string[] items);
    /// <summary>Writes an ordered list.</summary>
    /// <param name="items">The list item text values.</param>
    /// <param name="start">The first list number.</param>
    void WriteOrderedList(string[] items, int start);
    /// <summary>Writes a GFM task list.</summary>
    /// <param name="items">The task text values.</param>
    /// <param name="checkedItems">The matching checked states.</param>
    void WriteTaskList(string[] items, bool[] checkedItems);
    /// <summary>Writes an escaped block quote.</summary>
    /// <param name="text">The quoted text.</param>
    void WriteBlockQuote(string text);
    /// <summary>Writes a fenced code block.</summary>
    /// <param name="info">The optional information string.</param>
    /// <param name="code">The code content.</param>
    void WriteCodeBlock(string info, string code);
    /// <summary>Writes an inline code span with a safe delimiter.</summary>
    /// <param name="code">The code content.</param>
    void WriteCodeSpan(string code);
    /// <summary>Writes escaped emphasized text.</summary>
    /// <param name="text">The text to emphasize.</param>
    void WriteEmphasis(string text);
    /// <summary>Writes escaped strongly emphasized text.</summary>
    /// <param name="text">The text to emphasize.</param>
    void WriteStrongEmphasis(string text);
    /// <summary>Writes escaped GFM strikethrough text.</summary>
    /// <param name="text">The text to strike through.</param>
    void WriteStrikethrough(string text);
    /// <summary>Writes an inline link.</summary>
    /// <param name="text">The link text.</param>
    /// <param name="url">The destination.</param>
    void WriteLink(string text, string url);
    /// <summary>Writes an inline image.</summary>
    /// <param name="alt">The alternative text.</param>
    /// <param name="url">The destination.</param>
    void WriteImage(string alt, string url);
    /// <summary>Writes a GFM pipe table whose first row is the header.</summary>
    /// <param name="rows">The rectangular table rows.</param>
    void WriteTable(string[][] rows);
    /// <summary>Writes an inline math span.</summary>
    /// <param name="expression">The math expression.</param>
    void WriteMathSpan(string expression);
    /// <summary>Writes a math block.</summary>
    /// <param name="expression">The math expression.</param>
    void WriteMathBlock(string expression);
    /// <summary>Writes an admonition containing raw Markdown blocks.</summary>
    /// <param name="type">The admonition type.</param>
    /// <param name="title">The optional title.</param>
    /// <param name="markdownContent">The raw Markdown block content.</param>
    void WriteAdmonition(string type, string title, string markdownContent);
    /// <summary>Writes a named custom container containing raw Markdown blocks.</summary>
    /// <param name="name">The registered container name.</param>
    /// <param name="title">The optional title.</param>
    /// <param name="markdownContent">The raw Markdown block content.</param>
    void WriteCustomContainer(string name, string title, string markdownContent);
    /// <summary>Writes an escaped footnote definition.</summary>
    /// <param name="label">The footnote label.</param>
    /// <param name="text">The footnote text.</param>
    void WriteFootnoteDefinition(string label, string text);
    /// <summary>Writes a footnote reference.</summary>
    /// <param name="label">The footnote label.</param>
    void WriteFootnoteReference(string label);
    /// <summary>Returns text escaped for stable Markdown output.</summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>The escaped Markdown text.</returns>
    string EscapeText(string text);
}

/// <summary>
/// Defines chainable in-memory Markdown generation and document conversion operations.
/// </summary>
public interface IMarkdownBuilder
{
    /// <summary>Sets the line ending used by subsequent block operations.</summary>
    /// <param name="lineEnding">Use <c>\n</c> or <c>\r\n</c>.</param>
    /// <returns>This builder.</returns>
    MarkdownGenerationBuilder SetLineEnding(string lineEnding);
    /// <summary>Appends escaped text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Text(string text);
    /// <summary>Appends raw Markdown.</summary><param name="markdown">The raw Markdown.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder RawMarkdown(string markdown);
    /// <summary>Appends one line ending.</summary><returns>This builder.</returns>
    MarkdownGenerationBuilder LineEnding();
    /// <summary>Appends a blank-line separator.</summary><returns>This builder.</returns>
    MarkdownGenerationBuilder BlankLine();
    /// <summary>Appends an ATX heading.</summary><param name="level">The level.</param><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Heading(int level, string text);
    /// <summary>Appends an escaped paragraph.</summary><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Paragraph(string text);
    /// <summary>Appends a thematic break.</summary><returns>This builder.</returns>
    MarkdownGenerationBuilder ThematicBreak();
    /// <summary>Appends a bullet list.</summary><param name="items">The items.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder BulletList(string[] items);
    /// <summary>Appends an ordered list.</summary><param name="items">The items.</param><param name="start">The first number.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder OrderedList(string[] items, int start);
    /// <summary>Appends a GFM task list.</summary><param name="items">The tasks.</param><param name="checkedItems">The checked states.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder TaskList(string[] items, bool[] checkedItems);
    /// <summary>Appends an escaped block quote.</summary><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder BlockQuote(string text);
    /// <summary>Appends a fenced code block.</summary><param name="info">The information string.</param><param name="code">The code.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder CodeBlock(string info, string code);
    /// <summary>Appends an inline code span.</summary><param name="code">The code.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder CodeSpan(string code);
    /// <summary>Appends emphasized text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Emphasis(string text);
    /// <summary>Appends strongly emphasized text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder StrongEmphasis(string text);
    /// <summary>Appends GFM strikethrough text.</summary><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Strikethrough(string text);
    /// <summary>Appends an inline link.</summary><param name="text">The text.</param><param name="url">The destination.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Link(string text, string url);
    /// <summary>Appends an inline image.</summary><param name="alt">The alternative text.</param><param name="url">The destination.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Image(string alt, string url);
    /// <summary>Appends a GFM table.</summary><param name="rows">The rows.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Table(string[][] rows);
    /// <summary>Appends an inline math span.</summary><param name="expression">The expression.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder MathSpan(string expression);
    /// <summary>Appends a math block.</summary><param name="expression">The expression.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder MathBlock(string expression);
    /// <summary>Appends an admonition.</summary><param name="type">The type.</param><param name="title">The title.</param><param name="markdownContent">The raw content.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder Admonition(string type, string title, string markdownContent);
    /// <summary>Appends a custom container.</summary><param name="name">The name.</param><param name="title">The title.</param><param name="markdownContent">The raw content.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder CustomContainer(string name, string title, string markdownContent);
    /// <summary>Appends a footnote definition.</summary><param name="label">The label.</param><param name="text">The text.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder FootnoteDefinition(string label, string text);
    /// <summary>Appends a footnote reference.</summary><param name="label">The label.</param><returns>This builder.</returns>
    MarkdownGenerationBuilder FootnoteReference(string label);
    /// <summary>Clears generated output while retaining configuration.</summary><returns>This builder.</returns>
    MarkdownGenerationBuilder Clear();
    /// <summary>Returns the generated Markdown.</summary><returns>The generated Markdown.</returns>
    string ToMarkdown();
    /// <summary>Parses generated Markdown with CommonMark defaults.</summary><returns>The generated document.</returns>
    MarkdownDocument ToDocument();
    /// <summary>Parses generated Markdown with a supplied pipeline.</summary><param name="pipeline">The pipeline.</param><returns>The generated document.</returns>
    MarkdownDocument ToDocumentWithPipeline(MarkdownPipeline pipeline);
}
