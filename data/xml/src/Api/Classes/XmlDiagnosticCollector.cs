using System.Collections.Generic;

namespace xml;

/// <summary>Provides bounded XML diagnostic accumulation.</summary>
public class XmlDiagnosticCollector : IXmlErrorReporter, IXmlDiagnosticCollector
{
    internal const int DefaultMaximumDiagnosticCount = 1024;
    internal List<XmlDiagnostic> Diagnostics;
    internal int MaximumDiagnosticCount;
    internal XmlDiagnostic? LastDiagnostic;

    /// <summary>Creates an XML diagnostic collector.</summary>
    public XmlDiagnosticCollector()
    {
        Diagnostics = new List<XmlDiagnostic>();
        MaximumDiagnosticCount = DefaultMaximumDiagnosticCount;
        LastDiagnostic = null;
    }

    /// <summary>Records an XML diagnostic.</summary>
    public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
    {
        Guard.NotNull(diagnostic, "diagnostic");
        if (Diagnostics.Count >= MaximumDiagnosticCount)
        {
            throw CreateXmlDiagnosticCollectionLimit(diagnostic);
        }

        Diagnostics.Add(diagnostic);
        LastDiagnostic = diagnostic;
    }

    /// <summary>Sets the maximum retained diagnostic count and clears retained diagnostics.</summary>
    public void ConfigureXmlDiagnosticCollection(int maximumCount)
    {
        Guard.Positive(maximumCount, "maximumCount");
        Diagnostics.Clear();
        MaximumDiagnosticCount = maximumCount;
        LastDiagnostic = null;
    }

    /// <summary>Returns the retained diagnostics in arrival order.</summary>
    public XmlDiagnostic[] SnapshotCollectedXmlDiagnostics()
    {
        return Diagnostics.ToArray();
    }

    /// <summary>Returns the retained diagnostic count.</summary>
    public int CountCollectedXmlDiagnostics()
    {
        return Diagnostics.Count;
    }

    /// <summary>Clears retained diagnostics while preserving the configured maximum.</summary>
    public void ClearCollectedXmlDiagnostics()
    {
        Diagnostics.Clear();
        LastDiagnostic = null;
    }

    /// <summary>Clears retained diagnostics and restores the default maximum.</summary>
    public void ResetXmlDiagnosticCollection()
    {
        Diagnostics.Clear();
        MaximumDiagnosticCount = DefaultMaximumDiagnosticCount;
        LastDiagnostic = null;
    }

    internal XmlLimitException CreateXmlDiagnosticCollectionLimit(XmlDiagnostic rejectedDiagnostic)
    {
        string message = "The XML diagnostic collector limit of " + MaximumDiagnosticCount + " diagnostics was exceeded.";
        XmlDiagnostic limit = new XmlDiagnostic();
        limit.Code = XmlErrorCodes.LimitExceeded;
        limit.Category = XmlErrorCategory.Limit;
        limit.Severity = XmlDiagnosticSeverity.Fatal;
        limit.Message = message;
        limit.Range = rejectedDiagnostic.Range;
        SourceLocation? location = rejectedDiagnostic.Range == null ? null : rejectedDiagnostic.Range.Start;
        return new XmlLimitException(message, limit, location);
    }
}
