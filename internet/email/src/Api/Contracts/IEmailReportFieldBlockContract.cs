using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportFieldBlock"/>.
/// </summary>
public interface IEmailReportFieldBlockContract
{
    /// <summary>
    /// Gets the field count.
    /// </summary>
    /// <returns>The current field count.</returns>
    int GetFieldCount();
    /// <summary>
    /// Gets the field at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested field.</returns>
    EmailHeader GetFieldAt(int index);
    /// <summary>
    /// Gets the fields.
    /// </summary>
    /// <returns>The current fields.</returns>
    EmailHeaders GetFields();
    /// <summary>
    /// Gets the fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current fields.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    List<EmailHeader> GetFieldsNamed(string name);
    /// <summary>
    /// Gets the first field.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first field.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHeader? GetFirstField(string name);
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the separator bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current separator bytes.</returns>
    byte[] GetSeparatorBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
