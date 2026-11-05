namespace email;
/// <summary>
/// Specifies the supported authentication result values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailAuthenticationResultKind
{
    /// <summary>
    /// No authentication result is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the pass choice or state.
    /// </summary>
    Pass = 1,
    /// <summary>
    /// Represents the fail choice or state.
    /// </summary>
    Fail = 2,
    /// <summary>
    /// Represents the soft fail choice or state.
    /// </summary>
    SoftFail = 3,
    /// <summary>
    /// Represents the policy choice or state.
    /// </summary>
    Policy = 4,
    /// <summary>
    /// Represents the neutral choice or state.
    /// </summary>
    Neutral = 5,
    /// <summary>
    /// Represents the temp error choice or state.
    /// </summary>
    TempError = 6,
    /// <summary>
    /// Represents the perm error choice or state.
    /// </summary>
    PermError = 7,
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 8
}
