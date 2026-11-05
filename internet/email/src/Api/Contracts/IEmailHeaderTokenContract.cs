using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderToken"/>.
/// </summary>
public interface IEmailHeaderTokenContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailHeaderTokenKind GetKind();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current value bytes.</returns>
    byte[] GetValueBytes();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    int GetByteCount();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Determines whether the current value or state is obsolete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is obsolete; otherwise, <c>false</c>.</returns>
    bool IsObsolete();
    /// <summary>
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    bool IsMalformed();
    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    bool IsInternationalized();
    /// <summary>
    /// Gets the quoted pair count.
    /// </summary>
    /// <returns>The current quoted pair count.</returns>
    int GetQuotedPairCount();
    /// <summary>
    /// Gets the maximum comment depth.
    /// </summary>
    /// <returns>The current maximum comment depth.</returns>
    int GetMaximumCommentDepth();
}
