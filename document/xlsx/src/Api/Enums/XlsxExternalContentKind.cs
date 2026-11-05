using System;

namespace xlsx;

/// <summary>
/// Identifies a kind of external content referenced by a workbook.
/// </summary>
public enum XlsxExternalContentKind
{
    /// <summary>Identifies an external workbook link part.</summary>
    ExternalLink = 0,
    /// <summary>Identifies a workbook data connection.</summary>
    Connection = 1,
    /// <summary>Identifies a query table.</summary>
    QueryTable = 2,
    /// <summary>Identifies an explicit external package relationship.</summary>
    ExternalRelationship = 3,
    /// <summary>Identifies an external reference used by a formula.</summary>
    ExternalFormulaReference = 4,
    /// <summary>Identifies an external worksheet hyperlink.</summary>
    ExternalHyperlink = 5
}
