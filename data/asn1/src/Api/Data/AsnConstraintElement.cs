namespace asn1;

/// <summary>
/// Contains one value, bound, or nested operand in a constraint expression.
/// </summary>
public class AsnConstraintElement
{
    /// <summary>
    /// Contains the element or set-operation category.
    /// </summary>
    public AsnConstraintKind Kind;

    /// <summary>
    /// Indicates whether LowerBound contains an integer lower bound.
    /// </summary>
    public bool HasLowerBound;

    /// <summary>
    /// Contains an arbitrary-size lower bound when HasLowerBound is true.
    /// </summary>
    public System.Numerics.BigInteger LowerBound;

    /// <summary>
    /// Indicates whether UpperBound contains an integer upper bound.
    /// </summary>
    public bool HasUpperBound;

    /// <summary>
    /// Contains an arbitrary-size upper bound when HasUpperBound is true.
    /// </summary>
    public System.Numerics.BigInteger UpperBound;

    /// <summary>
    /// Contains the lower endpoint value when present.
    /// </summary>
    public AsnValue? LowerValue;

    /// <summary>
    /// Contains the upper endpoint value when present.
    /// </summary>
    public AsnValue? UpperValue;

    /// <summary>
    /// Indicates whether an omitted lower endpoint denotes MIN.
    /// </summary>
    public bool LowerEndpointUnbounded;

    /// <summary>
    /// Indicates whether an omitted upper endpoint denotes MAX.
    /// </summary>
    public bool UpperEndpointUnbounded;

    /// <summary>
    /// Indicates whether the lower endpoint is included.
    /// </summary>
    public bool LowerEndpointIncluded = true;

    /// <summary>
    /// Indicates whether the upper endpoint is included.
    /// </summary>
    public bool UpperEndpointIncluded = true;

    /// <summary>
    /// Contains a single value operand when present.
    /// </summary>
    public AsnValue? Value;

    /// <summary>
    /// Contains a type operand when present.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Contains a component name when present.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains retained textual constraint information when present.
    /// </summary>
    public string Text = "";

    /// <summary>
    /// Contains nested constraint operands.
    /// </summary>
    public AsnConstraintElement[] Children = [];
}
