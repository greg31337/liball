using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxExternalLinkPart"/> data.
/// </summary>
public class XlsxExternalLinkPartOperations : IXlsxExternalLinkPartOperations
{
    /// <summary>
    /// Initializes a new xlsx external link part operations instance.
    /// </summary>
    public XlsxExternalLinkPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxExternalLinkPart"/>.ResolveExternalBookRelationship through the ResolveExternalBookRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx external link part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    public void ResolveExternalBookRelationship(XlsxExternalLinkPart data, XlsxRelationshipsPart relationships)
    {
        data.ResolveExternalBookRelationship(relationships);
    }
}
