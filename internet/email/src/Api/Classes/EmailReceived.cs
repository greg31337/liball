using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a received.
/// </summary>
public class EmailReceived : IEmailReceivedContract
{
    List<EmailReceivedClause> clauses;
    EmailDateTime? date;
    bool dateDelimiterPresent;
    byte[] traceBytes;
    byte[] dateBytes;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new received.
    /// </summary>
    /// <param name = "clauses">The clauses used by the operation.</param>
    /// <param name = "date">The date used by the operation.</param>
    /// <param name = "dateDelimiterPresent">Whether to enable the date delimiter present behavior.</param>
    /// <param name = "traceBytes">The source bytes or data to process.</param>
    /// <param name = "dateBytes">The source bytes or data to process.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReceived(List<EmailReceivedClause> clauses, EmailDateTime? date, bool dateDelimiterPresent, byte[] traceBytes, byte[] dateBytes, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (clauses == null)
        {
            throw new ArgumentNullException("clauses");
        }

        if (traceBytes == null)
        {
            throw new ArgumentNullException("traceBytes");
        }

        if (dateBytes == null)
        {
            throw new ArgumentNullException("dateBytes");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.clauses = new List<EmailReceivedClause>(clauses);
        this.date = date;
        this.dateDelimiterPresent = dateDelimiterPresent;
        this.traceBytes = Copy(traceBytes);
        this.dateBytes = Copy(dateBytes);
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the clause count.
    /// </summary>
    /// <returns>The current clause count.</returns>
    public int GetClauseCount()
    {
        return clauses.Count;
    }

    /// <summary>
    /// Gets the clause.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current clause.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailReceivedClause GetClause(int index)
    {
        if (index < 0 || index >= clauses.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return clauses[index];
    }

    /// <summary>
    /// Gets the clauses.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current clauses.</returns>
    public List<EmailReceivedClause> GetClauses()
    {
        return new List<EmailReceivedClause>(clauses);
    }

    /// <summary>
    /// Gets the clauses.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current clauses.</returns>
    public List<EmailReceivedClause> GetClausesByKind(EmailReceivedClauseKind kind)
    {
        ValidateKind(kind);
        List<EmailReceivedClause> matches = new List<EmailReceivedClause>();
        int index = 0;
        while (index < clauses.Count)
        {
            if (clauses[index].GetKind() == kind)
            {
                matches.Add(clauses[index]);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Gets the first clause.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current first clause.</returns>
    public EmailReceivedClause? GetFirstClause(EmailReceivedClauseKind kind)
    {
        ValidateKind(kind);
        int index = 0;
        while (index < clauses.Count)
        {
            if (clauses[index].GetKind() == kind)
            {
                return clauses[index];
            }

            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the from.
    /// </summary>
    /// <returns>The current from.</returns>
    public EmailReceivedClause? GetFrom()
    {
        return GetFirstClause(EmailReceivedClauseKind.From);
    }

    /// <summary>
    /// Gets the by.
    /// </summary>
    /// <returns>The current by.</returns>
    public EmailReceivedClause? GetBy()
    {
        return GetFirstClause(EmailReceivedClauseKind.By);
    }

    /// <summary>
    /// Gets the via.
    /// </summary>
    /// <returns>The current via.</returns>
    public EmailReceivedClause? GetVia()
    {
        return GetFirstClause(EmailReceivedClauseKind.Via);
    }

    /// <summary>
    /// Gets the with.
    /// </summary>
    /// <returns>The current with.</returns>
    public EmailReceivedClause? GetWith()
    {
        return GetFirstClause(EmailReceivedClauseKind.With);
    }

    /// <summary>
    /// Gets the ID.
    /// </summary>
    /// <returns>The current ID.</returns>
    public EmailReceivedClause? GetId()
    {
        return GetFirstClause(EmailReceivedClauseKind.Id);
    }

    /// <summary>
    /// Gets the for.
    /// </summary>
    /// <returns>The current for.</returns>
    public EmailReceivedClause? GetFor()
    {
        return GetFirstClause(EmailReceivedClauseKind.For);
    }

    /// <summary>
    /// Determines whether the date delimiter is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date delimiter is available; otherwise, <c>false</c>.</returns>
    public bool HasDateDelimiter()
    {
        return dateDelimiterPresent;
    }

    /// <summary>
    /// Determines whether the date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date is available; otherwise, <c>false</c>.</returns>
    public bool HasDate()
    {
        return date != null;
    }

    /// <summary>
    /// Gets the date.
    /// </summary>
    /// <returns>The current date.</returns>
    public EmailDateTime? GetDate()
    {
        return date;
    }

    /// <summary>
    /// Gets the trace bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current trace bytes.</returns>
    public byte[] GetTraceBytes()
    {
        return Copy(traceBytes);
    }

    /// <summary>
    /// Gets the date bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current date bytes.</returns>
    public byte[] GetDateBytes()
    {
        return Copy(dateBytes);
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
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    static void ValidateKind(EmailReceivedClauseKind kind)
    {
        if (kind < EmailReceivedClauseKind.From || kind > EmailReceivedClauseKind.Extension)
        {
            throw new ArgumentOutOfRangeException("kind");
        }
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
