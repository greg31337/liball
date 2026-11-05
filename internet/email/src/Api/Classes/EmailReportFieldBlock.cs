using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a report field block.
/// </summary>
public class EmailReportFieldBlock : IEmailReportFieldBlockContract
{
    EmailHeaders fields;
    byte[] rawBytes;
    byte[] separatorBytes;
    EmailSourcePosition sourcePosition;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new report field block.
    /// </summary>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "separatorBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReportFieldBlock(EmailHeaders fields, byte[] rawBytes, byte[] separatorBytes, EmailSourcePosition sourcePosition, EmailIssueList issues)
    {
        if (fields == null)
            throw new ArgumentNullException("fields");
        if (rawBytes == null)
            throw new ArgumentNullException("rawBytes");
        if (separatorBytes == null)
            throw new ArgumentNullException("separatorBytes");
        if (sourcePosition == null)
            throw new ArgumentNullException("sourcePosition");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.fields = fields.Clone();
        this.rawBytes = Copy(rawBytes);
        this.separatorBytes = Copy(separatorBytes);
        this.sourcePosition = sourcePosition;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the field count.
    /// </summary>
    /// <returns>The current field count.</returns>
    public int GetFieldCount()
    {
        return fields.GetCount();
    }

    /// <summary>
    /// Gets the field at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested field.</returns>
    public EmailHeader GetFieldAt(int index)
    {
        return fields.GetAt(index).Clone();
    }

    /// <summary>
    /// Gets the fields.
    /// </summary>
    /// <returns>The current fields.</returns>
    public EmailHeaders GetFields()
    {
        return fields.Clone();
    }

    /// <summary>
    /// Gets the fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current fields.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public List<EmailHeader> GetFieldsNamed(string name)
    {
        if (name == null)
            throw new ArgumentNullException("name");
        List<EmailHeader> values = fields.GetAllNamed(name);
        List<EmailHeader> copies = new List<EmailHeader>();
        int index = 0;
        while (index < values.Count)
        {
            copies.Add(values[index].Clone());
            index++;
        }

        return copies;
    }

    /// <summary>
    /// Gets the first field.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first field.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHeader? GetFirstField(string name)
    {
        if (name == null)
            throw new ArgumentNullException("name");
        EmailHeader? value = fields.GetFirst(name);
        return value == null ? null : value.Clone();
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the separator bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current separator bytes.</returns>
    public byte[] GetSeparatorBytes()
    {
        return Copy(separatorBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
