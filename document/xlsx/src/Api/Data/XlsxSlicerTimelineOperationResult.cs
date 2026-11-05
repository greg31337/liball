using System;

namespace xlsx;

/// <summary>
/// Contains slicer or timeline authoring output and diagnostics.
/// </summary>
public class XlsxSlicerTimelineOperationResult
{
    /// <summary>Indicates whether the operation completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains operation diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores an authored slicer cache.</summary>
    public XlsxSlicerCachePart? SlicerCache;
    /// <summary>Stores an authored slicers part.</summary>
    public XlsxSlicersPart? Slicers;
    /// <summary>Stores an authored timeline cache.</summary>
    public XlsxSlicerTimelinePart? TimelineCache;
    /// <summary>Stores an authored timelines part.</summary>
    public XlsxSlicerTimelinePart? Timelines;
    /// <summary>Stores created relationships.</summary>
    public XlsxRelationshipsPart Relationships;

    /// <summary>Initializes an empty slicer or timeline operation result.</summary>
    public XlsxSlicerTimelineOperationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        SlicerCache = null;
        Slicers = null;
        TimelineCache = null;
        Timelines = null;
        Relationships = new XlsxRelationshipsPart();
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
        return diagnostic;
    }
}
