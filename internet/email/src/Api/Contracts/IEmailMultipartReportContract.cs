using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMultipartReport"/>.
/// </summary>
public interface IEmailMultipartReportContract
{
    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <returns>The current root.</returns>
    MimeEntity GetRoot();
    /// <summary>
    /// Determines whether the report type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the report type is available; otherwise, <c>false</c>.</returns>
    bool HasReportType();
    /// <summary>
    /// Gets the report type.
    /// </summary>
    /// <returns>The current report type.</returns>
    string? GetReportType();
    /// <summary>
    /// Determines whether the human readable part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the human readable part is available; otherwise, <c>false</c>.</returns>
    bool HasHumanReadablePart();
    /// <summary>
    /// Gets the human readable part.
    /// </summary>
    /// <returns>The current human readable part.</returns>
    MimeEntity? GetHumanReadablePart();
    /// <summary>
    /// Determines whether the machine readable part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the machine readable part is available; otherwise, <c>false</c>.</returns>
    bool HasMachineReadablePart();
    /// <summary>
    /// Gets the machine readable part.
    /// </summary>
    /// <returns>The current machine readable part.</returns>
    MimeEntity? GetMachineReadablePart();
    /// <summary>
    /// Determines whether the original content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original content part is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalContentPart();
    /// <summary>
    /// Gets the original content part.
    /// </summary>
    /// <returns>The current original content part.</returns>
    MimeEntity? GetOriginalContentPart();
    /// <summary>
    /// Gets the additional part count.
    /// </summary>
    /// <returns>The current additional part count.</returns>
    int GetAdditionalPartCount();
    /// <summary>
    /// Gets the additional part at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested additional part.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    MimeEntity GetAdditionalPartAt(int index);
    /// <summary>
    /// Gets the additional parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current additional parts.</returns>
    List<MimeEntity> GetAdditionalParts();
    /// <summary>
    /// Determines whether the current value or state is type consistent.
    /// </summary>
    /// <returns><c>true</c> when the value or state is type consistent; otherwise, <c>false</c>.</returns>
    bool IsTypeConsistent();
    /// <summary>
    /// Determines whether the current value or state is delivery status.
    /// </summary>
    /// <returns><c>true</c> when the value or state is delivery status; otherwise, <c>false</c>.</returns>
    bool IsDeliveryStatus();
    /// <summary>
    /// Determines whether the current value or state is disposition notification.
    /// </summary>
    /// <returns><c>true</c> when the value or state is disposition notification; otherwise, <c>false</c>.</returns>
    bool IsDispositionNotification();
    /// <summary>
    /// Determines whether the current value or state is feedback report.
    /// </summary>
    /// <returns><c>true</c> when the value or state is feedback report; otherwise, <c>false</c>.</returns>
    bool IsFeedbackReport();
}
