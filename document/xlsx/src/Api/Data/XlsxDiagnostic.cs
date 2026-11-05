using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Describes a validation diagnostic with a stable code, human-readable message, severity, and optional package or cell location.
/// </summary>
public class XlsxDiagnostic
{
    /// <summary>
    /// Stores the severity value used by this model.
    /// </summary>
    public XlsxDiagnosticSeverity Severity;
    /// <summary>
    /// Stores the code value used by this model.
    /// </summary>
    public string Code;
    /// <summary>
    /// Stores the message value used by this model.
    /// </summary>
    public string Message;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the element name value used by this model.
    /// </summary>
    public string ElementName;
    /// <summary>
    /// Stores the attribute name value used by this model.
    /// </summary>
    public string AttributeName;
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public int Row;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public int Column;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string? RelationshipId;
    /// <summary>
    /// Initializes a new diagnostic with its required severity, code, and message.
    /// </summary>
    /// <param name = "severity">The severity value used by the operation.</param>
    /// <param name = "code">The diagnostic code, which should remain stable across releases.</param>
    /// <param name = "message">The human-readable diagnostic message.</param>
    public XlsxDiagnostic(XlsxDiagnosticSeverity severity, string code, string message)
    {
        Severity = severity;
        Code = code;
        Message = message;
        PartPath = "";
        ElementName = "";
        AttributeName = "";
        Row = 0;
        Column = 0;
        RelationshipId = "";
    }

    /// <summary>
    /// Sets location and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "elementName">The element name value used by the operation.</param>
    /// <param name = "attributeName">The attribute name value used by the operation.</param>
    internal void SetLocation(string partPath, string elementName, string attributeName)
    {
        PartPath = partPath;
        ElementName = elementName;
        AttributeName = attributeName;
    }

    /// <summary>
    /// Sets cell location and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "row">The row value used by the operation.</param>
    /// <param name = "column">The column value used by the operation.</param>
    internal void SetCellLocation(int row, int column)
    {
        Row = row;
        Column = column;
    }

    /// <summary>
    /// Sets relationship id and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "relationshipId">The relationship id value used by the operation.</param>
    internal void SetRelationshipId(string? relationshipId)
    {
        RelationshipId = relationshipId;
    }
}
