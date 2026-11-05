namespace asn1;

/// <summary>
/// Describes the classified parts of an ASN.1 assignment.
/// </summary>
public class AsnAssignment
{
    /// <summary>
    /// Contains the assigned reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the assignment category.
    /// </summary>
    public AsnAssignmentKind Kind;

    /// <summary>
    /// Contains the assigned type when present.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Contains the assigned value when present.
    /// </summary>
    public AsnValue? Value;

    /// <summary>
    /// Contains formal parameters associated with the assignment.
    /// </summary>
    public AsnParameter[] Parameters = [];

    /// <summary>
    /// Contains object-class fields associated with the assignment.
    /// </summary>
    public AsnObjectClassField[] ObjectFields = [];

    /// <summary>
    /// Contains an assigned object class when present.
    /// </summary>
    public AsnObjectClass? ObjectClass;

    /// <summary>
    /// Contains an assigned information object when present.
    /// </summary>
    public AsnInformationObject? Object;

    /// <summary>
    /// Contains an assigned information object set when present.
    /// </summary>
    public AsnInformationObjectSet? ObjectSet;

    /// <summary>
    /// Contains formal actual-parameter substitutions.
    /// </summary>
    public AsnActualParameter[] ActualParameters = [];

    /// <summary>
    /// Contains the assignment source span when available.
    /// </summary>
    public AsnSourceSpan? Span;
}
