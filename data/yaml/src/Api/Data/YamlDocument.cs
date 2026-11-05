using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Represents one mutable YAML document, including directives, anchors, comments, and diagnostics.
/// </summary>
public class YamlDocument
{
    /// <summary>
    /// The directives declared before the document content, in source order.
    /// </summary>
    public List<YamlDirective> Directives;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;
    /// <summary>
    /// Comments attached before this item, without changing their order.
    /// </summary>
    public List<string> LeadingComments;
    /// <summary>
    /// Blank-line markers preserved before this item.
    /// </summary>
    public List<string> LeadingBlankLines;
    /// <summary>
    /// Ordered comment and blank-line metadata attached to this document.
    /// </summary>
    public List<YamlTrivia> Trivia;
    /// <summary>
    /// The anchor table scoped to this document.
    /// </summary>
    public YamlAnchorTable Anchors;
    /// <summary>
    /// The document's root node, or null when the document has no content.
    /// </summary>
    public YamlNode? Root;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a new YAML document with empty or default state.
    /// </summary>
    public YamlDocument()
    {
        Directives = new List<YamlDirective>();
        Diagnostics = new List<YamlDiagnostic>();
        LeadingComments = new List<string>();
        LeadingBlankLines = new List<string>();
        Trivia = new List<YamlTrivia>();
        Anchors = new YamlAnchorTable();
        Root = null;
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Replaces this instance's stored values with the supplied values.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    internal void SetRoot(YamlNode? node)
    {
        Root = node;
    }

    /// <summary>
    /// Determines whether this document contains a root node.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasRoot()
    {
        return Root != null;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="directive">The directive to append.</param>
    internal void AddDirective(YamlDirective? directive)
    {
        if (directive == null)
        {
            return;
        }

        Directives.Add(directive);
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to append.</param>
    internal void AddDiagnostic(YamlDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="comment">The comment text, with or without a leading number sign.</param>
    internal void AddLeadingComment(string? comment)
    {
        string text = comment == null ? "" : comment;
        LeadingComments.Add(text);
        Trivia.Add(YamlTrivia.Create(YamlTriviaKind.Comment, YamlTriviaPlacement.Before, text, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="blankLine">The preserved blank-line marker.</param>
    internal void AddLeadingBlankLine(string? blankLine)
    {
        string text = blankLine == null ? "" : blankLine;
        LeadingBlankLines.Add(text);
        Trivia.Add(YamlTrivia.Create(YamlTriviaKind.BlankLine, YamlTriviaPlacement.Before, text, YamlSourceSpan.Empty()));
    }

    internal void AddTrivia(YamlTrivia? trivia)
    {
        if (trivia == null)
        {
            return;
        }

        YamlTrivia copy = trivia.Copy();
        Trivia.Add(copy);
        if (copy.Kind == YamlTriviaKind.BlankLine)
        {
            LeadingBlankLines.Add(copy.Text);
        }
        else if (copy.Kind == YamlTriviaKind.Comment)
        {
            LeadingComments.Add(copy.Text);
        }
    }

    internal bool RemoveTrivia(int index)
    {
        if (index < 0 || index >= Trivia.Count)
        {
            return false;
        }

        YamlTrivia trivia = Trivia[index];
        Trivia.RemoveAt(index);
        if (trivia.Kind == YamlTriviaKind.BlankLine)
        {
            YamlTrivia.RemoveFirstText(LeadingBlankLines, trivia.Text);
        }
        else if (trivia.Kind == YamlTriviaKind.Comment)
        {
            YamlTrivia.RemoveFirstText(LeadingComments, trivia.Text);
        }

        return true;
    }
}
