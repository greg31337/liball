namespace uri;

/// <summary>
/// Stores one literal or expression part of a URI Template.
/// </summary>
public class UriTemplatePart
{
    /// <summary>Whether this part is literal text or an expression.</summary>
    public UriTemplatePartKind Kind;

    /// <summary>The literal source text; ignored for expression parts.</summary>
    public string Literal;

    /// <summary>The expression operator; ignored for literal parts.</summary>
    public UriTemplateOperator Operator;

    /// <summary>The ordered variable specifications; empty for literal parts.</summary>
    public UriTemplateVariable[] Variables;

    /// <summary>The range occupied by this part in the original template.</summary>
    public TextRange Range;

    /// <summary>Initializes an empty literal part at the start of the template.</summary>
    public UriTemplatePart()
    {
        Kind = UriTemplatePartKind.Literal;
        Literal = "";
        Operator = UriTemplateOperator.Simple;
        Variables = new UriTemplateVariable[0];
        Range = new TextRange(0, 0);
    }
}
