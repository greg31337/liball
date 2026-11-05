namespace markdown;

/// <summary>
/// Supplies a caller URI policy with the node, rewritten destination, built-in classification, and destination role.
/// </summary>
public class MarkdownUriPolicyContext
{
    /// <summary>The link or image node being rendered.</summary>
    public MarkdownNode Node;
    /// <summary>The destination after the configured rewrite hook.</summary>
    public string Destination;
    /// <summary>The safe-default classification of the rewritten destination.</summary>
    public MarkdownUriClassification Classification;
    /// <summary>Whether the destination belongs to an image rather than a link.</summary>
    public bool IsImage;

    /// <summary>Initializes an empty URI policy context.</summary>
    public MarkdownUriPolicyContext()
    {
        Node = new MarkdownNode();
        Destination = "";
        Classification = new MarkdownUriClassification();
        IsImage = false;
    }
}

/// <summary>
/// Describes the exception-less evaluation of a link or image destination.
/// </summary>
public class MarkdownUriEvaluation
{
    /// <summary>Whether a non-null input destination was supplied.</summary>
    public bool InputProvided;
    /// <summary>Whether the evaluated destination may be emitted.</summary>
    public bool Allowed;
    /// <summary>The destination after the configured rewrite hook.</summary>
    public string Destination;
    /// <summary>Whether the destination text changed during rewriting.</summary>
    public bool Rewritten;
    /// <summary>The safe-default classification of the rewritten destination.</summary>
    public MarkdownUriClassification Classification;

    /// <summary>Initializes a rejected empty evaluation.</summary>
    public MarkdownUriEvaluation()
    {
        InputProvided = false;
        Allowed = false;
        Destination = "";
        Rewritten = false;
        Classification = new MarkdownUriClassification();
    }
}

/// <summary>
/// Supplies a caller raw-HTML policy with the node, literal, built-in decision, and pipeline settings.
/// </summary>
public class MarkdownRawHtmlPolicyContext
{
    /// <summary>The raw HTML block or inline node being rendered.</summary>
    public MarkdownNode Node;
    /// <summary>The raw HTML literal.</summary>
    public string Literal;
    /// <summary>The decision selected by built-in safe rendering.</summary>
    public MarkdownRawHtmlDecision DefaultDecision;
    /// <summary>A detached copy of the effective rendering options.</summary>
    public MarkdownOptions Options;

    /// <summary>Initializes an empty raw-HTML policy context.</summary>
    public MarkdownRawHtmlPolicyContext()
    {
        Node = new MarkdownNode();
        Literal = "";
        DefaultDecision = MarkdownRawHtmlDecision.Escape;
        Options = new MarkdownOptions();
    }
}

/// <summary>
/// Describes the exception-less evaluation of a raw HTML literal.
/// </summary>
public class MarkdownRawHtmlEvaluation
{
    /// <summary>Whether a non-null input literal was supplied.</summary>
    public bool InputProvided;
    /// <summary>The selected rendering decision.</summary>
    public MarkdownRawHtmlDecision Decision;
    /// <summary>Whether the literal contains at least one GFM-disallowed raw HTML tag.</summary>
    public bool ContainsDisallowedTag;

    /// <summary>Initializes a suppressed empty evaluation.</summary>
    public MarkdownRawHtmlEvaluation()
    {
        InputProvided = false;
        Decision = MarkdownRawHtmlDecision.Suppress;
        ContainsDisallowedTag = false;
    }
}
