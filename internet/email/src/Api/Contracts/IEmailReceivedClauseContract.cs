using System;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReceivedClause"/>.
/// </summary>
public interface IEmailReceivedClauseContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailReceivedClauseKind GetKind();
    /// <summary>
    /// Determines whether the current value or state is extension.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extension; otherwise, <c>false</c>.</returns>
    bool IsExtension();
    /// <summary>
    /// Gets the original name.
    /// </summary>
    /// <returns>The current original name.</returns>
    string GetOriginalName();
    /// <summary>
    /// Gets the normalized name.
    /// </summary>
    /// <returns>The current normalized name.</returns>
    string GetNormalizedName();
    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    string GetValue();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the raw value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw value bytes.</returns>
    byte[] GetRawValueBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    bool IsMalformed();
}
