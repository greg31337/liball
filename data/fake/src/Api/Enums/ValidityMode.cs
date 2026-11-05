namespace fake;

/// <summary>
/// Reports the result of one independently evaluated validity dimension.
/// </summary>
public enum ValidityMode
{
    /// <summary>
    /// The dimension could not be evaluated because prerequisite syntax was absent.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The dimension does not apply to this value contract.
    /// </summary>
    NotApplicable = 1,

    /// <summary>
    /// The dimension was evaluated and is valid.
    /// </summary>
    Valid = 2,

    /// <summary>
    /// The dimension was evaluated and is invalid.
    /// </summary>
    Invalid = 3
}
