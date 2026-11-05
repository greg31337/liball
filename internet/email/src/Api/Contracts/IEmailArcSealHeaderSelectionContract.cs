using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSealHeaderSelection"/>.
/// </summary>
public interface IEmailArcSealHeaderSelectionContract
{
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailSignatureHeaderKind GetKind();
    /// <summary>
    /// Gets the source index.
    /// </summary>
    /// <returns>The current source index.</returns>
    int GetSourceIndex();
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailHeader GetHeader();
    /// <summary>
    /// Determines whether the operation or source value was signature value blanked.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasSignatureValueBlanked();
}
