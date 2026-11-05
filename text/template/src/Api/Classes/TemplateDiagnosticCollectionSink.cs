using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Collects reported diagnostics in a thread-safe ordered list.
/// </summary>
public class TemplateDiagnosticCollectionSink : ITemplateDiagnosticSink
{
    internal List<TemplateDiagnostic> Diagnostics;
    internal object SyncRoot;

    /// <summary>
    /// Initializes an empty diagnostic collection sink.
    /// </summary>
    public TemplateDiagnosticCollectionSink()
    {
        Diagnostics = new List<TemplateDiagnostic>();
        SyncRoot = new object();
    }

    /// <summary>
    /// Reports one diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to collect.</param>
    public void Report(TemplateDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        lock (SyncRoot)
        {
            Diagnostics.Add(diagnostic);
        }
    }

    /// <summary>
    /// Returns a defensive copy of reported diagnostics in report order.
    /// </summary>
    /// <returns>The collected diagnostics.</returns>
    public List<TemplateDiagnostic> GetDiagnostics()
    {
        lock (SyncRoot)
        {
            return new List<TemplateDiagnostic>(Diagnostics);
        }
    }

    /// <summary>
    /// Removes all reported diagnostics.
    /// </summary>
    public void Clear()
    {
        lock (SyncRoot)
        {
            Diagnostics.Clear();
        }
    }
}
