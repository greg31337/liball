using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported protected header mismatch values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailProtectedHeaderMismatchKind
{
    /// <summary>
    /// Represents the missing outer header choice or state.
    /// </summary>
    MissingOuterHeader = 0,
    /// <summary>
    /// Represents the unexpected outer header choice or state.
    /// </summary>
    UnexpectedOuterHeader = 1,
    /// <summary>
    /// Represents the outer value mismatch choice or state.
    /// </summary>
    OuterValueMismatch = 2,
    /// <summary>
    /// Represents the from address mismatch choice or state.
    /// </summary>
    FromAddressMismatch = 3
}
