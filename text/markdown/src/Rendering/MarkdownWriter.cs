using System;
using System.IO;

namespace markdown;

/// <summary>
/// Generates Markdown constructs directly into a text writer.
/// </summary>
/// <remarks>
/// Structural methods escape user text where needed and choose safe delimiters for destinations and code. <see cref="MarkdownWriter.Write"/> and <see cref="MarkdownWriter.WriteRaw"/> write text verbatim.
/// </remarks>
internal class MarkdownWriter
{
    internal TextWriter Writer;
    /// <summary>
    /// Stores the line ending value used by this instance.
    /// </summary>
    internal string LineEnding;
    internal MarkdownOutputSizeLimiter OutputSizeLimiter;

    /// <summary>
    /// Initializes a writer with LF line endings and no output-size limit.
    /// </summary>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    internal MarkdownWriter(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        Writer = writer;
        LineEnding = "\n";
        OutputSizeLimiter = new MarkdownOutputSizeLimiter(int.MaxValue);
    }

    /// <summary>
    /// Creates a writer with an output-size limit.
    /// </summary>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    /// <param name="maximumOutputSize">The maximum number of characters the writer may emit.</param>
    internal static MarkdownWriter Create(TextWriter writer, int maximumOutputSize)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        markdownWriter.OutputSizeLimiter = new MarkdownOutputSizeLimiter(maximumOutputSize);
        return markdownWriter;
    }

    /// <summary>
    /// Creates a writer with a configured line ending.
    /// </summary>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    internal static MarkdownWriter Create(TextWriter writer, string lineEnding)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        markdownWriter.SetLineEnding(lineEnding);
        return markdownWriter;
    }

    /// <summary>
    /// Creates a writer with a configured line ending and output-size limit.
    /// </summary>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    /// <param name="maximumOutputSize">The maximum number of characters the writer may emit.</param>
    internal static MarkdownWriter Create(TextWriter writer, string lineEnding, int maximumOutputSize)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        MarkdownWriter markdownWriter = Create(writer, maximumOutputSize);
        markdownWriter.SetLineEnding(lineEnding);
        return markdownWriter;
    }

    /// <summary>
    /// Sets line ending.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    internal void SetLineEnding(string lineEnding)
    {
        MarkdownOptions options = new MarkdownOptions();
        options.SetLineEnding(lineEnding);
        LineEnding = lineEnding;
    }

    /// <summary>
    /// Writes Markdown text verbatim while enforcing the output-size limit.
    /// </summary>
    /// <param name="text">The text to process.</param>
    internal void Write(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        OutputSizeLimiter.EnsureCanAdd(text);
        Writer.Write(text);
        OutputSizeLimiter.CommitCount(text.Length);
    }

    /// <summary>
    /// Writes raw.
    /// </summary>
    /// <param name="markdown">The Markdown text to write without escaping.</param>
    internal void WriteRaw(string markdown)
    {
        Write(markdown);
    }

    /// <summary>
    /// Writes line ending.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    internal void WriteSpecificLineEnding(string lineEnding)
    {
        if (lineEnding == null)
        {
            throw new ArgumentNullException("lineEnding");
        }

        Write(lineEnding);
    }

    /// <summary>
    /// Writes line ending.
    /// </summary>
    internal void WriteLineEnding()
    {
        Write(LineEnding);
    }

    /// <summary>
    /// Writes two configured line endings.
    /// </summary>
    internal void BlankLine()
    {
        WriteLineEnding();
    }

    internal void Text(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        WriteNormalizedText(
            MarkdownRenderer.EscapeParagraphLineStarts(MarkdownRenderer.EscapeText(text)));
    }

    /// <summary>
    /// Writes an ATX heading after escaping its text.
    /// </summary>
    /// <param name="level">The heading level from 1 through 6.</param>
    /// <param name="text">The text to process.</param>
    internal void Heading(int level, string text)
    {
        if (level < 1 || level > 6)
        {
            throw new ArgumentOutOfRangeException("level");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Write(new string('#', level));
        Write(" ");
        WriteNormalizedText(MarkdownRenderer.EscapeText(text));
        WriteLineEnding();
    }

    /// <summary>
    /// Writes escaped paragraph text followed by a line ending.
    /// </summary>
    /// <param name="text">The text to process.</param>
    internal void Paragraph(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        WriteNormalizedText(
            MarkdownRenderer.EscapeParagraphLineStarts(MarkdownRenderer.EscapeText(text)));
        WriteLineEnding();
    }

    internal void ThematicBreak()
    {
        Write("---");
        WriteLineEnding();
    }

    internal void BulletList(string[] items)
    {
        ValidateListItems(items);

        int index = 0;
        while (index < items.Length)
        {
            Write("- ");
            Write(MarkdownRenderer.EscapeText(items[index]));
            WriteLineEnding();
            index = index + 1;
        }
    }

    internal void OrderedList(string[] items, int start)
    {
        ValidateListItems(items);
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        int index = 0;
        while (index < items.Length)
        {
            string number = ((long)start + index).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            Write(number);
            Write(". ");
            Write(MarkdownRenderer.EscapeText(items[index]));
            WriteLineEnding();
            index = index + 1;
        }
    }

    internal void TaskList(string[] items, bool[] checkedItems)
    {
        ValidateListItems(items);
        if (checkedItems == null)
        {
            throw new ArgumentNullException("checkedItems");
        }

        if (items.Length != checkedItems.Length)
        {
            throw new ArgumentException("Task text and checked-state arrays must have the same length.");
        }

        int index = 0;
        while (index < items.Length)
        {
            if (checkedItems[index])
            {
                Write("- [x] ");
            }
            else
            {
                Write("- [ ] ");
            }

            Write(MarkdownRenderer.EscapeText(items[index]));
            WriteLineEnding();
            index = index + 1;
        }
    }

    internal void BlockQuote(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string escaped = MarkdownRenderer.EscapeParagraphLineStarts(
            MarkdownRenderer.EscapeText(text));
        WritePrefixedLines("> ", escaped);
    }

    /// <summary>
    /// Writes a fenced code block using a fence long enough to contain the code safely.
    /// </summary>
    /// <param name="info">The optional code-fence information string.</param>
    /// <param name="code">The code block content.</param>
    internal void CodeBlock(string info, string code)
    {
        if (info == null)
        {
            throw new ArgumentNullException("info");
        }

        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (MarkdownRenderer.ContainsCharacter(info, '`'))
        {
            throw new ArgumentException("Code block info text must not contain backticks.");
        }

        string fence = GetCodeFence(code);
        Write(fence);
        Write(info);
        WriteLineEnding();
        Write(code);
        if (!MarkdownRenderer.EndsWithLineEnding(code))
        {
            WriteLineEnding();
        }

        Write(fence);
        WriteLineEnding();
    }

    internal void CodeSpan(string code)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        string delimiter = MarkdownRenderer.GetCodeSpanDelimiter(code);
        bool padding = MarkdownRenderer.NeedsCodeSpanPadding(code);
        Write(delimiter);
        if (padding)
        {
            Write(" ");
        }

        Write(code);
        if (padding)
        {
            Write(" ");
        }

        Write(delimiter);
    }

    internal void Emphasis(string text)
    {
        WriteWrappedText("*", text);
    }

    internal void StrongEmphasis(string text)
    {
        WriteWrappedText("**", text);
    }

    internal void Strikethrough(string text)
    {
        WriteWrappedText("~~", text);
    }

    /// <summary>
    /// Writes a Markdown link with escaped text and a safely delimited destination.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="url">The link or image destination.</param>
    internal void Link(string text, string url)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        Write("[");
        Write(MarkdownRenderer.EscapeText(text));
        Write("](");
        WriteDestination(url);
        Write(")");
    }

    /// <summary>
    /// Writes a Markdown image with escaped alternative text and a safely delimited destination.
    /// </summary>
    /// <param name="alt">The image alternative text.</param>
    /// <param name="url">The link or image destination.</param>
    internal void Image(string alt, string url)
    {
        if (alt == null)
        {
            throw new ArgumentNullException("alt");
        }

        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        Write("![");
        Write(MarkdownRenderer.EscapeText(alt));
        Write("](");
        WriteDestination(url);
        Write(")");
    }

    /// <summary>
    /// Writes a pipe table whose first row is the header.
    /// </summary>
    /// <param name="rows">The table rows; the first row becomes the header.</param>
    internal void Table(string[][] rows)
    {
        if (rows == null)
        {
            throw new ArgumentNullException("rows");
        }

        if (rows.Length == 0)
        {
            throw new ArgumentException("Table must contain at least a header row.");
        }

        int columnCount = ValidateTableRows(rows);
        WriteTableRow(rows[0], columnCount);
        WriteTableDelimiterRow(columnCount);

        int index = 1;
        while (index < rows.Length)
        {
            WriteTableRow(rows[index], columnCount);
            index = index + 1;
        }
    }

    internal void MathSpan(string expression)
    {
        ValidateDelimitedContent(expression, "$", "expression");
        Write("$");
        Write(expression);
        Write("$");
    }

    internal void MathBlock(string expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        Write("$$");
        WriteLineEnding();
        WriteNormalizedText(expression);
        if (!MarkdownRenderer.EndsWithLineEnding(expression))
        {
            WriteLineEnding();
        }

        Write("$$");
        WriteLineEnding();
    }

    internal void Admonition(string type, string title, string markdownContent)
    {
        ValidateGenerationName(type, "type");
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (markdownContent == null)
        {
            throw new ArgumentNullException("markdownContent");
        }

        Write("!!! ");
        Write(type);
        if (title.Length > 0)
        {
            Write(" \"");
            Write(MarkdownRenderer.EscapeHeadingAttributeValue(title));
            Write("\"");
        }

        WriteLineEnding();
        WriteIndentedLines("    ", markdownContent);
    }

    internal void CustomContainer(string name, string title, string markdownContent)
    {
        ValidateGenerationName(name, "name");
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (markdownContent == null)
        {
            throw new ArgumentNullException("markdownContent");
        }

        Write("::: ");
        Write(name);
        if (title.Length > 0)
        {
            Write(" \"");
            Write(MarkdownRenderer.EscapeHeadingAttributeValue(title));
            Write("\"");
        }

        WriteLineEnding();
        WriteNormalizedText(markdownContent);
        if (!MarkdownRenderer.EndsWithLineEnding(markdownContent))
        {
            WriteLineEnding();
        }

        Write(":::");
        WriteLineEnding();
    }

    internal void FootnoteDefinition(string label, string text)
    {
        ValidateGenerationName(label, "label");
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string escaped = MarkdownRenderer.EscapeParagraphLineStarts(
            MarkdownRenderer.EscapeText(text));
        Write("[^");
        Write(label);
        Write("]: ");
        string normalized = MarkdownRenderer.NormalizeLineEndings(escaped);
        string[] lines = normalized.Split(new char[] { '\n' });
        int index = 0;
        while (index < lines.Length)
        {
            if (index > 0)
            {
                Write("    ");
            }

            Write(lines[index]);
            WriteLineEnding();
            index = index + 1;
        }
    }

    internal void FootnoteReference(string label)
    {
        ValidateGenerationName(label, "label");
        Write("[^");
        Write(label);
        Write("]");
    }

    internal static void ValidateListItems(string[] items)
    {
        if (items == null)
        {
            throw new ArgumentNullException("items");
        }

        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == null)
            {
                throw new ArgumentNullException("items");
            }

            index = index + 1;
        }
    }

    internal static void ValidateDelimitedContent(string text, string delimiter, string parameterName)
    {
        if (text == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (text.Contains(delimiter))
        {
            throw new ArgumentException("Content must not contain its Markdown delimiter.");
        }
    }

    internal static void ValidateGenerationName(string name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Name must not be empty.");
        }

        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            bool valid = (character >= 'a' && character <= 'z')
                || (character >= 'A' && character <= 'Z')
                || (character >= '0' && character <= '9')
                || character == '-'
                || character == '_';
            if (!valid)
            {
                throw new ArgumentException("Name contains unsupported Markdown delimiter characters.");
            }

            index = index + 1;
        }
    }

    internal void WriteWrappedText(string delimiter, string text)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Write(delimiter);
        WriteNormalizedText(MarkdownRenderer.EscapeText(text));
        Write(delimiter);
    }

    internal void WriteNormalizedText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string normalized = MarkdownRenderer.NormalizeLineEndings(text);
        int index = 0;
        while (index < normalized.Length)
        {
            if (normalized[index] == '\n')
            {
                WriteLineEnding();
            }
            else
            {
                Write(normalized[index].ToString());
            }

            index = index + 1;
        }
    }

    internal void WritePrefixedLines(string prefix, string text)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string normalized = MarkdownRenderer.NormalizeLineEndings(text);
        string[] lines = normalized.Split(new char[] { '\n' });
        int index = 0;
        while (index < lines.Length)
        {
            Write(prefix);
            Write(lines[index]);
            WriteLineEnding();
            index = index + 1;
        }
    }

    internal void WriteIndentedLines(string prefix, string markdownContent)
    {
        WritePrefixedLines(prefix, markdownContent);
    }

    internal static string GetCodeFence(string code)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        int length = MarkdownRenderer.GetLongestRun(code, '`') + 1;
        if (length < 3)
        {
            length = 3;
        }

        return new string('`', length);
    }

    internal void WriteDestination(string url)
    {
        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        if (MarkdownRenderer.NeedsAngleDestination(url))
        {
            Write("<");
            Write(MarkdownRenderer.EscapeAngleDestination(url));
            Write(">");
        }
        else
        {
            Write(MarkdownRenderer.EscapeBareDestination(url));
        }
    }

    internal int ValidateTableRows(string[][] rows)
    {
        if (rows == null)
        {
            throw new ArgumentNullException("rows");
        }

        if (rows.Length == 0)
        {
            throw new ArgumentException("Table must contain at least a header row.");
        }

        if (rows[0] == null)
        {
            throw new ArgumentNullException("rows");
        }

        if (rows[0].Length == 0)
        {
            throw new ArgumentException("Table must contain at least one column.");
        }

        int columnCount = rows[0].Length;
        int rowIndex = 0;
        while (rowIndex < rows.Length)
        {
            if (rows[rowIndex] == null)
            {
                throw new ArgumentNullException("rows");
            }

            if (rows[rowIndex].Length != columnCount)
            {
                throw new ArgumentException("All table rows must have the same column count.");
            }

            int columnIndex = 0;
            while (columnIndex < rows[rowIndex].Length)
            {
                if (rows[rowIndex][columnIndex] == null)
                {
                    throw new ArgumentNullException("rows");
                }

                columnIndex = columnIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }

        return columnCount;
    }

    internal void WriteTableRow(string[] row, int columnCount)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        if (columnCount < 1)
        {
            throw new ArgumentOutOfRangeException("columnCount");
        }

        if (row.Length != columnCount)
        {
            throw new ArgumentException("Row length must match the table column count.");
        }

        Write("|");
        int index = 0;
        while (index < columnCount)
        {
            if (row[index] == null)
            {
                throw new ArgumentNullException("row");
            }

            Write(" ");
            Write(EscapeTableCell(row[index]));
            Write(" |");
            index = index + 1;
        }

        WriteLineEnding();
    }

    internal void WriteTableDelimiterRow(int columnCount)
    {
        if (columnCount < 1)
        {
            throw new ArgumentOutOfRangeException("columnCount");
        }

        Write("|");
        int index = 0;
        while (index < columnCount)
        {
            Write(" --- |");
            index = index + 1;
        }

        WriteLineEnding();
    }

    internal static string EscapeTableCell(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return MarkdownRenderer.EscapeCharacters(MarkdownRenderer.EscapeText(text), "|");
    }
}
