using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a multipart report.
/// </summary>
public class EmailMultipartReport : IEmailMultipartReportContract
{
    MimeEntity root;
    string? reportType;
    MimeEntity? humanReadablePart;
    MimeEntity? machineReadablePart;
    MimeEntity? originalContentPart;
    List<MimeEntity> additionalParts;
    bool typeConsistent;
    /// <summary>
    /// Creates a new multipart report.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "reportType">The report type used by the operation.</param>
    /// <param name = "humanReadablePart">The human readable part used by the operation.</param>
    /// <param name = "machineReadablePart">The machine readable part used by the operation.</param>
    /// <param name = "originalContentPart">The original content part used by the operation.</param>
    /// <param name = "additionalParts">The additional parts used by the operation.</param>
    /// <param name = "typeConsistent">Whether to enable the type consistent behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMultipartReport(MimeEntity root, string? reportType, MimeEntity? humanReadablePart, MimeEntity? machineReadablePart, MimeEntity? originalContentPart, List<MimeEntity> additionalParts, bool typeConsistent)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        if (!(root.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "report"))
            throw new ArgumentException("The report root must have media type multipart/report.", "root");
        if (reportType != null && reportType.Length == 0)
            throw new ArgumentException("A report type cannot be empty when supplied.", "reportType");
        if (additionalParts == null)
            throw new ArgumentNullException("additionalParts");
        int index = 0;
        while (index < additionalParts.Count)
        {
            if (additionalParts[index] == null)
                throw new ArgumentException("Additional report parts cannot contain null entries.", "additionalParts");
            index++;
        }

        this.root = root;
        this.reportType = reportType;
        this.humanReadablePart = humanReadablePart;
        this.machineReadablePart = machineReadablePart;
        this.originalContentPart = originalContentPart;
        this.additionalParts = new List<MimeEntity>(additionalParts);
        this.typeConsistent = typeConsistent;
    }

    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <returns>The current root.</returns>
    public MimeEntity GetRoot()
    {
        return root;
    }

    /// <summary>
    /// Determines whether the report type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the report type is available; otherwise, <c>false</c>.</returns>
    public bool HasReportType()
    {
        return reportType != null;
    }

    /// <summary>
    /// Gets the report type.
    /// </summary>
    /// <returns>The current report type.</returns>
    public string? GetReportType()
    {
        return reportType;
    }

    /// <summary>
    /// Determines whether the human readable part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the human readable part is available; otherwise, <c>false</c>.</returns>
    public bool HasHumanReadablePart()
    {
        return humanReadablePart != null;
    }

    /// <summary>
    /// Gets the human readable part.
    /// </summary>
    /// <returns>The current human readable part.</returns>
    public MimeEntity? GetHumanReadablePart()
    {
        return humanReadablePart;
    }

    /// <summary>
    /// Determines whether the machine readable part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the machine readable part is available; otherwise, <c>false</c>.</returns>
    public bool HasMachineReadablePart()
    {
        return machineReadablePart != null;
    }

    /// <summary>
    /// Gets the machine readable part.
    /// </summary>
    /// <returns>The current machine readable part.</returns>
    public MimeEntity? GetMachineReadablePart()
    {
        return machineReadablePart;
    }

    /// <summary>
    /// Determines whether the original content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original content part is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalContentPart()
    {
        return originalContentPart != null;
    }

    /// <summary>
    /// Gets the original content part.
    /// </summary>
    /// <returns>The current original content part.</returns>
    public MimeEntity? GetOriginalContentPart()
    {
        return originalContentPart;
    }

    /// <summary>
    /// Gets the additional part count.
    /// </summary>
    /// <returns>The current additional part count.</returns>
    public int GetAdditionalPartCount()
    {
        return additionalParts.Count;
    }

    /// <summary>
    /// Gets the additional part at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested additional part.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public MimeEntity GetAdditionalPartAt(int index)
    {
        if (index < 0 || index >= additionalParts.Count)
            throw new ArgumentOutOfRangeException("index");
        return additionalParts[index];
    }

    /// <summary>
    /// Gets the additional parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current additional parts.</returns>
    public List<MimeEntity> GetAdditionalParts()
    {
        return new List<MimeEntity>(additionalParts);
    }

    /// <summary>
    /// Determines whether the current value or state is type consistent.
    /// </summary>
    /// <returns><c>true</c> when the value or state is type consistent; otherwise, <c>false</c>.</returns>
    public bool IsTypeConsistent()
    {
        return typeConsistent;
    }

    /// <summary>
    /// Determines whether the current value or state is delivery status.
    /// </summary>
    /// <returns><c>true</c> when the value or state is delivery status; otherwise, <c>false</c>.</returns>
    public bool IsDeliveryStatus()
    {
        return string.Equals(reportType, "delivery-status", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is disposition notification.
    /// </summary>
    /// <returns><c>true</c> when the value or state is disposition notification; otherwise, <c>false</c>.</returns>
    public bool IsDispositionNotification()
    {
        return string.Equals(reportType, "disposition-notification", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is feedback report.
    /// </summary>
    /// <returns><c>true</c> when the value or state is feedback report; otherwise, <c>false</c>.</returns>
    public bool IsFeedbackReport()
    {
        return string.Equals(reportType, "feedback-report", StringComparison.OrdinalIgnoreCase);
    }
}
