using System;
using System.Collections.Generic;

namespace cbor;

internal class CddlDiagnosticCollector
{
    internal string? PrimarySourceName;
    internal int MaxDiagnostics;
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal bool HasErrors;

    internal CddlDiagnosticCollector(
        string? primarySourceName,
        int maxDiagnostics)
    {
        PrimarySourceName = primarySourceName;
        MaxDiagnostics = maxDiagnostics;
    }

    internal void AddDiagnostics(List<CddlDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            AddDiagnostic(diagnostics[index]);
            index = index + 1;
        }
    }

    internal void AddDiagnostic(CddlDiagnostic diagnostic)
    {
        if (diagnostic.Severity == CddlDiagnosticSeverity.Error)
        {
            HasErrors = true;
        }
        Diagnostics.Add(diagnostic);
    }

    internal void MarkError()
    {
        HasErrors = true;
    }

    internal CddlDiagnostic[] CreateOrderedArray()
    {
        List<CddlDiagnostic> ordered = new List<CddlDiagnostic>();
        int index = 0;
        while (index < Diagnostics.Count)
        {
            InsertOrdered(ordered, Diagnostics[index]);
            index = index + 1;
        }
        if (MaxDiagnostics <= 0)
        {
            return [];
        }
        int count = ordered.Count;
        if (count > MaxDiagnostics)
        {
            count = MaxDiagnostics;
        }
        CddlDiagnostic[] result = new CddlDiagnostic[count];
        index = 0;
        while (index < count)
        {
            result[index] = CloneDiagnostic(ordered[index]);
            index = index + 1;
        }
        return result;
    }

    internal void InsertOrdered(
        List<CddlDiagnostic> ordered,
        CddlDiagnostic diagnostic)
    {
        int index = 0;
        while (index < ordered.Count
            && CompareDiagnostics(ordered[index], diagnostic) <= 0)
        {
            index = index + 1;
        }
        ordered.Insert(index, diagnostic);
    }

    internal int CompareDiagnostics(
        CddlDiagnostic left,
        CddlDiagnostic right)
    {
        int source = CompareSourceNames(
            left.Span.SourceName,
            right.Span.SourceName
        );
        if (source != 0)
        {
            return source;
        }
        if (left.Span.Start < right.Span.Start)
        {
            return -1;
        }
        if (left.Span.Start > right.Span.Start)
        {
            return 1;
        }
        return 0;
    }

    internal int CompareSourceNames(string? left, string? right)
    {
        if (left == right)
        {
            return 0;
        }
        if (left == PrimarySourceName)
        {
            return -1;
        }
        if (right == PrimarySourceName)
        {
            return 1;
        }
        if (left == CddlPreludeLoader.PreludeSourceName)
        {
            return 1;
        }
        if (right == CddlPreludeLoader.PreludeSourceName)
        {
            return -1;
        }
        return string.Compare(left, right, StringComparison.Ordinal);
    }

    internal CddlDiagnostic CloneDiagnostic(CddlDiagnostic source)
    {
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = source.Severity;
        diagnostic.Code = source.Code;
        diagnostic.Message = source.Message;
        diagnostic.RuleName = source.RuleName;
        diagnostic.Span = CloneSpan(source.Span);
        return diagnostic;
    }

    internal CddlSourceSpan CloneSpan(CddlSourceSpan source)
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = source.SourceName;
        span.Start = source.Start;
        span.Length = source.Length;
        span.StartLine = source.StartLine;
        span.StartColumn = source.StartColumn;
        span.EndLine = source.EndLine;
        span.EndColumn = source.EndColumn;
        return span;
    }
}
