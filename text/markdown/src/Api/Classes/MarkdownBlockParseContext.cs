using System;

namespace markdown;

/// <summary>
/// Supplies a block parser rule with controlled access to the current source line and output document.
/// </summary>
/// <remarks>
/// Instances are created by the parser. A rule that returns <c>true</c> must advance at least one line.
/// </remarks>
public class MarkdownBlockParseContext : IMarkdownBlockParseContext
{
    internal MarkdownParseContext ParseContext;
    internal MarkdownLine[] Lines;
    internal int LineIndex;
    internal MarkdownContainerStack OpenContainers;
    internal int BlankLineCount;
    internal bool LastLineWasBlank;
    internal MarkdownLine? LastBlankLine;

    internal MarkdownBlockParseContext()
    {
        ParseContext = new MarkdownParseContext();
        Lines = new MarkdownLine[0];
        LineIndex = 0;
        OpenContainers = new MarkdownContainerStack();
        BlankLineCount = 0;
        LastLineWasBlank = false;
        LastBlankLine = null;
    }

    internal static MarkdownBlockParseContext Create(MarkdownParseContext parseContext)
    {
        if (parseContext == null)
        {
            throw new ArgumentNullException("parseContext");
        }

        MarkdownBlockParseContext context = new MarkdownBlockParseContext();
        context.ParseContext = parseContext;
        context.Lines = MarkdownLineReader.ReadLines(parseContext.Source);
        return context;
    }

    internal static MarkdownBlockParseContext Create(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        return Create(MarkdownParser.CreateContext(source, pipeline));
    }

    /// <summary>
    /// Reports whether a source line is available at the current position.
    /// </summary>
    /// <returns><c>true</c> when a current line is available; otherwise, <c>false</c>.</returns>
    public bool HasCurrentLine()
    {
        return LineIndex >= 0 && LineIndex < Lines.Length;
    }

    internal MarkdownLine? GetCurrentLine()
    {
        if (!HasCurrentLine())
        {
            return null;
        }

        return Lines[LineIndex];
    }

    /// <summary>
    /// Returns the total number of normalized source lines.
    /// </summary>
    /// <returns>The source line count.</returns>
    public int GetLineCount()
    {
        return Lines.Length;
    }

    /// <summary>
    /// Advances to the next source line.
    /// </summary>
    /// <returns><c>true</c> when another current line remains; otherwise, <c>false</c>.</returns>
    public bool AdvanceLine()
    {
        if (LineIndex < Lines.Length)
        {
            LineIndex = LineIndex + 1;
        }

        return HasCurrentLine();
    }

    /// <summary>
    /// Returns the zero-based index of the current source line.
    /// </summary>
    /// <returns>The current line index.</returns>
    public int GetLineIndex()
    {
        return LineIndex;
    }

    /// <summary>
    /// Returns the current normalized source line text, or an empty string after the final line.
    /// </summary>
    /// <returns>The current line text.</returns>
    public string GetCurrentLineText()
    {
        MarkdownLine? line = GetCurrentLine();
        if (line == null)
        {
            return "";
        }

        return line.Text;
    }

    /// <summary>
    /// Returns a detached source span for the current line.
    /// </summary>
    /// <returns>The current line span, or the document root span after the final line.</returns>
    public MarkdownSourceSpan GetCurrentLineSpan()
    {
        MarkdownLine? line = GetCurrentLine();
        if (line == null)
        {
            return ParseContext.Document.Root.Span.Copy();
        }

        return line.LineSpan.Copy();
    }

    /// <summary>
    /// Returns the document being built by the active parser.
    /// </summary>
    /// <returns>The mutable output document.</returns>
    public MarkdownDocument GetDocument()
    {
        return ParseContext.Document;
    }

    /// <summary>
    /// Returns a detached snapshot of the active pipeline.
    /// </summary>
    /// <returns>The active pipeline settings.</returns>
    public MarkdownPipeline GetPipeline()
    {
        return ParseContext.Pipeline.Copy();
    }

    /// <summary>
    /// Appends a block node to the document root.
    /// </summary>
    /// <param name="node">The block node to append.</param>
    public void AppendBlock(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (!MarkdownNodeKindFacts.IsBlockKind(node.Kind))
        {
            throw new ArgumentException("Node kind is not a block kind.");
        }

        ParseContext.Document.Root.AppendChild(node);
    }

    /// <summary>
    /// Adds a diagnostic to the document being parsed.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The source span associated with the diagnostic.</param>
    public void AddDiagnostic(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span)
    {
        ParseContext.AddDiagnosticValues(severity, code, message, span);
    }

    internal void ResetLines()
    {
        LineIndex = 0;
        LastLineWasBlank = false;
    }

    internal void NoteBlankLine(MarkdownLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        BlankLineCount = BlankLineCount + 1;
        LastLineWasBlank = true;
        LastBlankLine = line;
    }

    internal void NoteContentLine()
    {
        LastLineWasBlank = false;
    }

    internal void PushContainer(MarkdownNode node, int indent, int markerColumn, string marker)
    {
        OpenContainers.PushNode(node, indent, markerColumn, marker);
    }

    internal MarkdownContainerState? PopContainer()
    {
        return OpenContainers.Pop();
    }

    internal MarkdownContainerState? GetCurrentContainer()
    {
        return OpenContainers.Peek();
    }

    internal int GetOpenContainerCount()
    {
        return OpenContainers.GetCount();
    }
}
