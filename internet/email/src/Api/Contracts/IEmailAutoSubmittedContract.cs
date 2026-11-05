using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAutoSubmitted"/>.
/// </summary>
public interface IEmailAutoSubmittedContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailAutoSubmittedKind GetKind();
    /// <summary>
    /// Gets the original token.
    /// </summary>
    /// <returns>The current original token.</returns>
    string GetOriginalToken();
    /// <summary>
    /// Gets the normalized token.
    /// </summary>
    /// <returns>The current normalized token.</returns>
    string GetNormalizedToken();
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    EmailParameterList GetParameters();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
}
