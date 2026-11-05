using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Collects diagnostics in reporting order and provides severity-based queries.</summary>
public class SqlDiagnosticBag
{
    /// <summary>The diagnostics in reporting order.</summary>
    public List<SqlDiagnostic> Diagnostics;

    /// <summary>Initializes a new instance of <see cref="SqlDiagnosticBag"/> from the supplied values.</summary>
    public SqlDiagnosticBag()
    {
        Diagnostics = new List<SqlDiagnostic>();
    }

    /// <summary>Returns the number of  values.</summary>
    /// <returns>The requested count.</returns>
    internal int GetCount()
    {
        return Diagnostics.Count;
    }

    /// <summary>Returns whether this value contains no elements or source characters.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Diagnostics.Count == 0;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting diagnostic.</returns>
    internal SqlDiagnostic GetAt(int index)
    {
        return Diagnostics[index];
    }

    /// <summary>Copies the diagnostics into a new array in reporting order.</summary>
    /// <returns>The resulting diagnostic.</returns>
    internal SqlDiagnostic[] ToArray()
    {
        return Diagnostics.ToArray();
    }

    /// <summary>Removes every diagnostic from the bag.</summary>
    internal void Clear()
    {
        Diagnostics.Clear();
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="diagnostic">The diagnostic to use.</param>
    /// <exception cref="ArgumentNullException">diagnostic is <c>null</c>.</exception>
    internal void Add(SqlDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        Diagnostics.Add(diagnostic);
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting diagnostic.</returns>
    internal SqlDiagnostic AddDiagnostic(SqlDiagnosticSeverity severity, string message, SqlTextSpan span)
    {
        SqlDiagnostic diagnostic = SqlDiagnostic.Create(severity, message, span);

        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <returns>The resulting diagnostic.</returns>
    internal SqlDiagnostic AddDiagnosticWithDetails(SqlDiagnosticSeverity severity, string? code, string message, SqlTextSpan span, SqlSourceText? sourceText)
    {
        SqlDiagnostic diagnostic = SqlDiagnostic.Create(severity, code, message, span, sourceText);

        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Appends every diagnostic from another bag in its existing order.</summary>
    /// <param name="diagnostics">The diagnostic collection produced during processing.</param>
    /// <exception cref="ArgumentNullException">diagnostics is <c>null</c>.</exception>
    internal void Merge(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        int count = diagnostics.GetCount();
        int index = 0;

        while (index < count)
        {
            Add(diagnostics.GetAt(index));
            index = index + 1;
        }
    }

    /// <summary>Returns whether errors is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return CountBySeverity(SqlDiagnosticSeverity.Error) > 0;
    }

    /// <summary>Counts diagnostics having the requested severity.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int CountBySeverity(SqlDiagnosticSeverity severity)
    {
        int count = 0;
        int index = 0;

        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].Severity == severity)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
