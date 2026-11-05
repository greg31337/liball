namespace asn1;

/// <summary>
/// Identifies an executable encoding-plan step.
/// </summary>
public enum AsnPlanStepKind
{
    /// <summary>
    /// Represents unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Represents primitive.
    /// </summary>
    Primitive = 1,

    /// <summary>
    /// Represents sequence.
    /// </summary>
    Sequence = 2,

    /// <summary>
    /// Represents choice.
    /// </summary>
    Choice = 3,

    /// <summary>
    /// Represents repetition.
    /// </summary>
    Repetition = 4,

    /// <summary>
    /// Represents optional.
    /// </summary>
    Optional = 5,

    /// <summary>
    /// Represents tag.
    /// </summary>
    Tag = 6,

    /// <summary>
    /// Represents length.
    /// </summary>
    Length = 7,

    /// <summary>
    /// Represents alignment.
    /// </summary>
    Alignment = 8,

    /// <summary>
    /// Represents padding.
    /// </summary>
    Padding = 9,

    /// <summary>
    /// Represents condition.
    /// </summary>
    Condition = 10,

    /// <summary>
    /// Represents determinant.
    /// </summary>
    Determinant = 11,

    /// <summary>
    /// Represents mapping.
    /// </summary>
    Mapping = 12,

    /// <summary>
    /// Represents replacement.
    /// </summary>
    Replacement = 13,

    /// <summary>
    /// Represents transformation.
    /// </summary>
    Transformation = 14,

    /// <summary>
    /// Represents open type.
    /// </summary>
    OpenType = 15,

    /// <summary>
    /// Represents standard encoding.
    /// </summary>
    StandardEncoding = 16
}
