using System;

namespace markdown;

/// <summary>
/// Supplies an inline parser rule with controlled access to the current source position and output container.
/// </summary>
/// <remarks>
/// Instances are created by the parser. A rule that returns <c>true</c> must advance at least one character.
/// </remarks>
public class MarkdownInlineParseContext : IMarkdownInlineParseContext
{
    internal MarkdownParseContext ParseContext;
    internal MarkdownNode Container;
    internal string Source;
    internal int Position;
    internal MarkdownDelimiterStack Delimiters;
    internal MarkdownBracketStack Brackets;
    internal bool InLink;

    internal MarkdownInlineParseContext()
    {
        ParseContext = new MarkdownParseContext();
        Container = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        Source = "";
        Position = 0;
        Delimiters = new MarkdownDelimiterStack();
        Brackets = new MarkdownBracketStack();
        InLink = false;
    }

    internal static MarkdownInlineParseContext Create(MarkdownParseContext parseContext, MarkdownNode container)
    {
        if (parseContext == null)
        {
            throw new ArgumentNullException("parseContext");
        }

        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        return Create(parseContext, container, container.Literal);
    }

    internal static MarkdownInlineParseContext Create(MarkdownParseContext parseContext, MarkdownNode container, string source)
    {
        if (parseContext == null)
        {
            throw new ArgumentNullException("parseContext");
        }

        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        MarkdownInlineParseContext context = new MarkdownInlineParseContext();
        context.ParseContext = parseContext;
        context.Container = container;
        context.Source = source;
        context.InLink = IsInLinkContainer(container);
        return context;
    }

    /// <summary>
    /// Reports whether a source character is available at the current position.
    /// </summary>
    /// <returns><c>true</c> when a current character is available; otherwise, <c>false</c>.</returns>
    public bool HasCurrentCharacter()
    {
        return Position >= 0 && Position < Source.Length;
    }

    /// <summary>
    /// Returns the current source character, or the null character after the end of the source.
    /// </summary>
    /// <returns>The current source character.</returns>
    public char GetCurrentCharacter()
    {
        if (!HasCurrentCharacter())
        {
            return '\0';
        }

        return Source[Position];
    }

    /// <summary>
    /// Advances to the next source character.
    /// </summary>
    /// <returns><c>true</c> when another current character remains; otherwise, <c>false</c>.</returns>
    public bool Advance()
    {
        if (Position < Source.Length)
        {
            Position = Position + 1;
        }

        return HasCurrentCharacter();
    }

    /// <summary>
    /// Advances by a nonnegative number of source characters.
    /// </summary>
    /// <param name="count">The number of characters to consume.</param>
    /// <returns><c>true</c> when another current character remains; otherwise, <c>false</c>.</returns>
    public bool AdvanceBy(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        int index = 0;
        while (index < count)
        {
            Advance();
            index = index + 1;
        }

        return HasCurrentCharacter();
    }

    /// <summary>
    /// Returns the source text from the current position to the end of the inline container.
    /// </summary>
    /// <returns>The remaining source text.</returns>
    public string GetRemainingText()
    {
        if (!HasCurrentCharacter())
        {
            return "";
        }

        return Source.Substring(Position);
    }

    /// <summary>
    /// Appends an inline node to the active container.
    /// </summary>
    /// <param name="node">The inline node to append.</param>
    public void AppendInline(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (!MarkdownNodeKindFacts.IsInlineKind(node.Kind))
        {
            throw new ArgumentException("Node kind is not an inline kind.");
        }

        Container.AppendChild(node);
    }

    /// <summary>
    /// Returns the normalized source text for the active inline container.
    /// </summary>
    /// <returns>The inline source text.</returns>
    public string GetSource()
    {
        return Source;
    }

    /// <summary>
    /// Returns the zero-based current source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public int GetPosition()
    {
        return Position;
    }

    /// <summary>
    /// Returns the mutable node that receives parsed inline children.
    /// </summary>
    /// <returns>The active inline container.</returns>
    public MarkdownNode GetContainer()
    {
        return Container;
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

    internal static bool IsInLinkContainer(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownNode? current = node;
        while (current != null)
        {
            if (current.Kind == MarkdownNodeKind.Link)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}
