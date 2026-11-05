namespace asn1;

/// <summary>
/// Contains reflection metadata for one normalized constraint.
/// </summary>
public class AsnConstraintDescriptor
{
    /// <summary>
    /// Contains the stable constraint identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the normalized constraint category.
    /// </summary>
    public AsnConstraintKind Kind;

    /// <summary>
    /// Contains the source constraint when retained.
    /// </summary>
    public AsnConstraint? Constraint;

    /// <summary>
    /// Contains the effective constraint domain.
    /// </summary>
    public AsnTypeKind DomainKind;

    /// <summary>
    /// Contains the normalized root expression when present.
    /// </summary>
    public AsnConstraintElement? Root;

    /// <summary>
    /// Contains the normalized extension expression when present.
    /// </summary>
    public AsnConstraintElement? Extension;

    /// <summary>
    /// Contains applied constraints in evaluation order.
    /// </summary>
    public AsnConstraintDescriptor[] Applications = [];

    /// <summary>
    /// Contains normalized root intervals.
    /// </summary>
    public AsnIntervalDescriptor[] RootIntervals = [];

    /// <summary>
    /// Contains normalized extension intervals.
    /// </summary>
    public AsnIntervalDescriptor[] ExtensionIntervals = [];

    /// <summary>
    /// Indicates whether an extension partition is present.
    /// </summary>
    public bool Extensible;

    /// <summary>
    /// Indicates whether the extension partition is open.
    /// </summary>
    public bool OpenExtension;

    /// <summary>
    /// Contains the constraint source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the constraint.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
