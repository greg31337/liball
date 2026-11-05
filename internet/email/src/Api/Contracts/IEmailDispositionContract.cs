using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDisposition"/>.
/// </summary>
public interface IEmailDispositionContract
{
    /// <summary>
    /// Gets the disposition type.
    /// </summary>
    /// <returns>The current disposition type.</returns>
    string GetDispositionType();
    /// <summary>
    /// Gets the normalized disposition type.
    /// </summary>
    /// <returns>The current normalized disposition type.</returns>
    string GetNormalizedDispositionType();
    /// <summary>
    /// Determines whether the current value or state is inline.
    /// </summary>
    /// <returns><c>true</c> when the value or state is inline; otherwise, <c>false</c>.</returns>
    bool IsInline();
    /// <summary>
    /// Determines whether the current value or state is attachment.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment; otherwise, <c>false</c>.</returns>
    bool IsAttachment();
    /// <summary>
    /// Determines whether the current value or state is extension.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extension; otherwise, <c>false</c>.</returns>
    bool IsExtension();
    /// <summary>
    /// Gets the effective disposition type.
    /// </summary>
    /// <returns>The current effective disposition type.</returns>
    string GetEffectiveDispositionType();
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
