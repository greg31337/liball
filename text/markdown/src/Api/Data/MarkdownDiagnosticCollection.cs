using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Stores diagnostics in the order in which processing produced them.
/// </summary>
public class MarkdownDiagnosticCollection
{
    /// <summary>
    /// Stores diagnostics in insertion order.
    /// </summary>
    public List<MarkdownDiagnostic> Items;

    /// <summary>
    /// Initializes a new <see cref="MarkdownDiagnosticCollection"/> instance.
    /// </summary>
    public MarkdownDiagnosticCollection()
    {
        Items = new List<MarkdownDiagnostic>();
    }

    /// <summary>
    /// Returns count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetCount()
    {
        return Items.Count;
    }

    /// <summary>
    /// Reports whether at least one diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return Items.Count > 0;
    }

    /// <summary>
    /// Returns a detached copy of the diagnostic at the requested position.
    /// </summary>
    /// <param name="index">The zero-based position in the collection.</param>
    /// <returns>The resulting <see cref="MarkdownDiagnostic"/> value.</returns>
    internal MarkdownDiagnostic Get(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return Items[index].Copy();
    }

    /// <summary>
    /// Returns diagnostics.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownDiagnostic[] GetDiagnostics()
    {
        MarkdownDiagnostic[] diagnostics = new MarkdownDiagnostic[Items.Count];
        int index = 0;

        while (index < Items.Count)
        {
            diagnostics[index] = Items[index].Copy();
            index = index + 1;
        }

        return diagnostics;
    }

    /// <summary>
    /// Appends a detached copy of a diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic value.</param>
    internal void Add(MarkdownDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        Items.Add(diagnostic.Copy());
    }

    /// <summary>
    /// Creates and appends a diagnostic from its individual values.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal void AddDiagnostic(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span)
    {
        Add(MarkdownDiagnostic.Create(severity, code, message, span));
    }

    internal void AddDiagnosticWithSourceName(
        MarkdownDiagnosticSeverity severity,
        string code,
        string message,
        MarkdownSourceSpan span,
        string sourceName)
    {
        Add(MarkdownDiagnostic.CreateWithSourceName(severity, code, message, span, sourceName));
    }

    /// <summary>
    /// Adds info.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal void AddInfo(string code, string message, MarkdownSourceSpan span)
    {
        AddDiagnostic(MarkdownDiagnosticSeverity.Info, code, message, span);
    }

    /// <summary>
    /// Adds warning.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal void AddWarning(string code, string message, MarkdownSourceSpan span)
    {
        AddDiagnostic(MarkdownDiagnosticSeverity.Warning, code, message, span);
    }

    /// <summary>
    /// Adds error.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal void AddError(string code, string message, MarkdownSourceSpan span)
    {
        AddDiagnostic(MarkdownDiagnosticSeverity.Error, code, message, span);
    }

    /// <summary>
    /// Counts by severity.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int CountBySeverity(MarkdownDiagnosticSeverity severity)
    {
        int count = 0;
        int index = 0;

        while (index < Items.Count)
        {
            if (Items[index].Severity == severity)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Reports whether at least one error diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return CountBySeverity(MarkdownDiagnosticSeverity.Error) > 0;
    }

    /// <summary>
    /// Reports whether at least one warning diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWarnings()
    {
        return CountBySeverity(MarkdownDiagnosticSeverity.Warning) > 0;
    }

    /// <summary>
    /// Removes every diagnostic.
    /// </summary>
    internal void Clear()
    {
        Items.Clear();
    }
}
