using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents a typed pivot-cache records part.
/// </summary>
public class XlsxPivotCacheRecordsPart
{
    /// <summary>Indicates whether parsing or validation completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains validation diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the normalized package part path.</summary>
    public string PartPath;
    /// <summary>Stores preserved source XML.</summary>
    public string RawXml;
    /// <summary>Contains typed cache records.</summary>
    public List<XlsxPivotCacheRecord> Records;

    /// <summary>Initializes an empty pivot-cache records part.</summary>
    public XlsxPivotCacheRecordsPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/pivotCache/pivotCacheRecords1.xml";
        RawXml = "";
        Records = new List<XlsxPivotCacheRecord>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
