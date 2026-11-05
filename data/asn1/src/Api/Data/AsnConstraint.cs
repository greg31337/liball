namespace asn1;

/// <summary>
/// Describes an ASN.1 constraint classification.
/// </summary>
public class AsnConstraint
{
    /// <summary>
    /// Contains the constraint category.
    /// </summary>
    public AsnConstraintKind Kind;

    /// <summary>
    /// Contains constraint elements and set-operation operands.
    /// </summary>
    public AsnConstraintElement[] Elements = [];

    /// <summary>
    /// Indicates whether the constraint has an extension partition.
    /// </summary>
    public bool Extensible;

    /// <summary>
    /// Contains the retained body of a user-defined constraint.
    /// </summary>
    public string UserDefinedBody = "";

    /// <summary>
    /// Contains user-defined constraint parameters in source order.
    /// </summary>
    public string[] UserDefinedParameters = [];

    /// <summary>
    /// Indicates whether this constraint carries an exception identifier.
    /// </summary>
    public bool HasException;

    /// <summary>
    /// Contains the explicit or implied exception identifier type.
    /// </summary>
    public AsnType? ExceptionType;

    /// <summary>
    /// Contains the exception identifier value.
    /// </summary>
    public AsnValue? ExceptionValue;

    /// <summary>
    /// Contains the retained exception identifier text.
    /// </summary>
    public string ExceptionIdentifier = "";

    /// <summary>
    /// Indicates that application-supplied validation is required.
    /// </summary>
    public bool RequiresApplicationValidation;

    /// <summary>
    /// Contains the constraint source span when available.
    /// </summary>
    public AsnSourceSpan? Span;
}
