using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxRelationshipsPart"/> data.
/// </summary>
public class XlsxRelationshipsPartOperations : IXlsxRelationshipsPartOperations
{
    /// <summary>
    /// Initializes a new xlsx relationships part operations instance.
    /// </summary>
    public XlsxRelationshipsPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxRelationshipsPart"/>.GetRelationship through the GetRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx relationships part data to operate on.</param>
    /// <param name="id">The id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxRelationship? GetRelationship(XlsxRelationshipsPart data, string? id)
    {
        return data.GetRelationship(id);
    }

    /// <summary>
    /// Invokes <see cref="XlsxRelationshipsPart"/>.GetFirstByType through the GetFirstByType operation.
    /// </summary>
    /// <param name="data">The xlsx relationships part data to operate on.</param>
    /// <param name="relationshipType">The relationship type value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxRelationship? GetFirstByType(XlsxRelationshipsPart data, string relationshipType)
    {
        return data.GetFirstByType(relationshipType);
    }
}
