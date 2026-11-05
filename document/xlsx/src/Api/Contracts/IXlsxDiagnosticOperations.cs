using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxDiagnostic"/> data.
/// </summary>
public interface IXlsxDiagnosticOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxDiagnostic"/>.SetLocation through the SetLocation operation.
    /// </summary>
    /// <param name="data">The xlsx diagnostic data to operate on.</param>
    /// <param name="partPath">The part path value used by the operation.</param>
    /// <param name="elementName">The element name value used by the operation.</param>
    /// <param name="attributeName">The attribute name value used by the operation.</param>
    void SetLocation(XlsxDiagnostic data, string partPath, string elementName, string attributeName);
    /// <summary>
    /// Invokes <see cref="XlsxDiagnostic"/>.SetCellLocation through the SetCellLocation operation.
    /// </summary>
    /// <param name="data">The xlsx diagnostic data to operate on.</param>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    void SetCellLocation(XlsxDiagnostic data, int row, int column);
    /// <summary>
    /// Invokes <see cref="XlsxDiagnostic"/>.SetRelationshipId through the SetRelationshipId operation.
    /// </summary>
    /// <param name="data">The xlsx diagnostic data to operate on.</param>
    /// <param name="relationshipId">The relationship id value used by the operation.</param>
    void SetRelationshipId(XlsxDiagnostic data, string? relationshipId);
}
