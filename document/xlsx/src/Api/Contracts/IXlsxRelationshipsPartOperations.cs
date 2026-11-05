using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxRelationshipsPart"/> data.
/// </summary>
public interface IXlsxRelationshipsPartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxRelationshipsPart"/>.GetRelationship through the GetRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx relationships part data to operate on.</param>
    /// <param name="id">The id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxRelationship? GetRelationship(XlsxRelationshipsPart data, string? id);
    /// <summary>
    /// Invokes <see cref="XlsxRelationshipsPart"/>.GetFirstByType through the GetFirstByType operation.
    /// </summary>
    /// <param name="data">The xlsx relationships part data to operate on.</param>
    /// <param name="relationshipType">The relationship type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxRelationship? GetFirstByType(XlsxRelationshipsPart data, string relationshipType);
}
