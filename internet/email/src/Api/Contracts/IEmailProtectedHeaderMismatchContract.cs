using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProtectedHeaderMismatch"/>.
/// </summary>
public interface IEmailProtectedHeaderMismatchContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailProtectedHeaderMismatchKind GetKind();
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    string GetFieldName();
    /// <summary>
    /// Determines whether the expected value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expected value is available; otherwise, <c>false</c>.</returns>
    bool HasExpectedValue();
    /// <summary>
    /// Gets the expected value.
    /// </summary>
    /// <returns>The current expected value.</returns>
    string? GetExpectedValue();
    /// <summary>
    /// Determines whether the actual value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the actual value is available; otherwise, <c>false</c>.</returns>
    bool HasActualValue();
    /// <summary>
    /// Gets the actual value.
    /// </summary>
    /// <returns>The current actual value.</returns>
    string? GetActualValue();
}
