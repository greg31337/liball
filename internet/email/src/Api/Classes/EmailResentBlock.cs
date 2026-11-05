using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a resent block.
/// </summary>
public class EmailResentBlock : IEmailResentBlockContract
{
    List<EmailResentField> fields;
    /// <summary>
    /// Creates a new resent block.
    /// </summary>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailResentBlock(List<EmailResentField> fields)
    {
        if (fields == null)
        {
            throw new ArgumentNullException("fields");
        }

        this.fields = new List<EmailResentField>(fields);
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return fields.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailResentField GetAt(int index)
    {
        if (index < 0 || index >= fields.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return fields[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailResentField> GetAll()
    {
        return new List<EmailResentField>(fields);
    }

    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current first.</returns>
    public EmailResentField? GetFirst(EmailResentFieldKind kind)
    {
        ValidateKind(kind);
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index].GetKind() == kind)
            {
                return fields[index];
            }

            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current all.</returns>
    public List<EmailResentField> GetAllByKind(EmailResentFieldKind kind)
    {
        ValidateKind(kind);
        List<EmailResentField> matches = new List<EmailResentField>();
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index].GetKind() == kind)
            {
                matches.Add(fields[index]);
            }

            index++;
        }

        return matches;
    }

    static void ValidateKind(EmailResentFieldKind kind)
    {
        if (kind < EmailResentFieldKind.Date || kind > EmailResentFieldKind.Extension)
        {
            throw new ArgumentOutOfRangeException("kind");
        }
    }
}
