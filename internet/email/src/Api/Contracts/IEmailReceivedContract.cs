using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReceived"/>.
/// </summary>
public interface IEmailReceivedContract
{
    /// <summary>
    /// Gets the clause count.
    /// </summary>
    /// <returns>The current clause count.</returns>
    int GetClauseCount();
    /// <summary>
    /// Gets the clause.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current clause.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailReceivedClause GetClause(int index);
    /// <summary>
    /// Gets the clauses.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current clauses.</returns>
    List<EmailReceivedClause> GetClauses();
    /// <summary>
    /// Gets the clauses.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current clauses.</returns>
    List<EmailReceivedClause> GetClausesByKind(EmailReceivedClauseKind kind);
    /// <summary>
    /// Gets the first clause.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current first clause.</returns>
    EmailReceivedClause? GetFirstClause(EmailReceivedClauseKind kind);
    /// <summary>
    /// Gets the from.
    /// </summary>
    /// <returns>The current from.</returns>
    EmailReceivedClause? GetFrom();
    /// <summary>
    /// Gets the by.
    /// </summary>
    /// <returns>The current by.</returns>
    EmailReceivedClause? GetBy();
    /// <summary>
    /// Gets the via.
    /// </summary>
    /// <returns>The current via.</returns>
    EmailReceivedClause? GetVia();
    /// <summary>
    /// Gets the with.
    /// </summary>
    /// <returns>The current with.</returns>
    EmailReceivedClause? GetWith();
    /// <summary>
    /// Gets the ID.
    /// </summary>
    /// <returns>The current ID.</returns>
    EmailReceivedClause? GetId();
    /// <summary>
    /// Gets the for.
    /// </summary>
    /// <returns>The current for.</returns>
    EmailReceivedClause? GetFor();
    /// <summary>
    /// Determines whether the date delimiter is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date delimiter is available; otherwise, <c>false</c>.</returns>
    bool HasDateDelimiter();
    /// <summary>
    /// Determines whether the date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date is available; otherwise, <c>false</c>.</returns>
    bool HasDate();
    /// <summary>
    /// Gets the date.
    /// </summary>
    /// <returns>The current date.</returns>
    EmailDateTime? GetDate();
    /// <summary>
    /// Gets the trace bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current trace bytes.</returns>
    byte[] GetTraceBytes();
    /// <summary>
    /// Gets the date bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current date bytes.</returns>
    byte[] GetDateBytes();
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
