using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSignatureTagList"/>.
/// </summary>
public interface IEmailSignatureTagListContract
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    int GetCount();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailSignatureTag GetAt(int index);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    List<EmailSignatureTag> GetAll();
    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Contains(string name);
    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSignatureTag? GetFirst(string name);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current all.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    List<EmailSignatureTag> GetAllNamed(string name);
    /// <summary>
    /// Determines whether the duplicate is present or available.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the duplicate is available; otherwise, <c>false</c>.</returns>
    bool HasDuplicate(string name);
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
}
