namespace asn1;

/// <summary>
/// Describes an encoding instruction classification.
/// </summary>
public class AsnEncodingInstruction
{
    /// <summary>
    /// Contains the identifying keyword or property name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the instruction category.
    /// </summary>
    public AsnInstructionKind Kind;

    /// <summary>
    /// Indicates whether the instruction is a negating instruction.
    /// </summary>
    public bool Negated;

    /// <summary>
    /// Contains instruction arguments in source order.
    /// </summary>
    public string[] Arguments = [];

    /// <summary>
    /// Contains target type or component paths.
    /// </summary>
    public string[] Targets = [];

    /// <summary>
    /// Contains the instruction source span when available.
    /// </summary>
    public AsnSourceSpan? Span;
}
