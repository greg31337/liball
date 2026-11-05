using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxDiagnostic"/> data.
/// </summary>
public class XlsxDiagnosticOperations : IXlsxDiagnosticOperations
{
    /// <summary>
    /// Initializes a new xlsx diagnostic operations instance.
    /// </summary>
    public XlsxDiagnosticOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxDiagnostic"/>.SetLocation through the SetLocation operation.
    /// </summary>
    /// <param name="data">The xlsx diagnostic data to operate on.</param>
    /// <param name="partPath">The part path value used by the operation.</param>
    /// <param name="elementName">The element name value used by the operation.</param>
    /// <param name="attributeName">The attribute name value used by the operation.</param>
    public void SetLocation(XlsxDiagnostic data, string partPath, string elementName, string attributeName)
    {
        data.SetLocation(partPath, elementName, attributeName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxDiagnostic"/>.SetCellLocation through the SetCellLocation operation.
    /// </summary>
    /// <param name="data">The xlsx diagnostic data to operate on.</param>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    public void SetCellLocation(XlsxDiagnostic data, int row, int column)
    {
        data.SetCellLocation(row, column);
    }

    /// <summary>
    /// Invokes <see cref="XlsxDiagnostic"/>.SetRelationshipId through the SetRelationshipId operation.
    /// </summary>
    /// <param name="data">The xlsx diagnostic data to operate on.</param>
    /// <param name="relationshipId">The relationship id value used by the operation.</param>
    public void SetRelationshipId(XlsxDiagnostic data, string? relationshipId)
    {
        data.SetRelationshipId(relationshipId);
    }
}
