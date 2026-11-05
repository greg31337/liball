using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWorkbookRelationshipResolutionResult"/> data.
/// </summary>
public interface IXlsxWorkbookRelationshipResolutionResultOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookRelationshipResolutionResult"/>.GetRelationship through the GetRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx workbook relationship resolution result data to operate on.</param>
    /// <param name="id">The id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookResolvedRelationship? GetRelationship(XlsxWorkbookRelationshipResolutionResult data, string? id);
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookRelationshipResolutionResult"/>.GetFirstByKind through the GetFirstByKind operation.
    /// </summary>
    /// <param name="data">The xlsx workbook relationship resolution result data to operate on.</param>
    /// <param name="kind">The kind value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxWorkbookResolvedRelationship? GetFirstByKind(XlsxWorkbookRelationshipResolutionResult data, XlsxWorkbookRelationshipKind kind);
}
