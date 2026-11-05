using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxUnknownPartStore"/> data.
/// </summary>
public class XlsxUnknownPartStoreOperations : IXlsxUnknownPartStoreOperations
{
    /// <summary>
    /// Initializes a new xlsx unknown part store operations instance.
    /// </summary>
    public XlsxUnknownPartStoreOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.AddPart through the AddPart operation.
    /// </summary>
    /// <param name="model">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <param name="data">The data value used by the operation.</param>
    public void AddPart(XlsxUnknownPartStore model, string path, string? contentType, byte[] data)
    {
        model.AddPart(path, contentType, data);
    }

    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.GetPart through the GetPart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxUnknownPart? GetPart(XlsxUnknownPartStore data, string path)
    {
        return data.GetPart(path);
    }

    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.ContainsPart through the ContainsPart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool ContainsPart(XlsxUnknownPartStore data, string path)
    {
        return data.ContainsPart(path);
    }

    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.RemovePart through the RemovePart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool RemovePart(XlsxUnknownPartStore data, string path)
    {
        return data.RemovePart(path);
    }

    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.AddContentTypesToPart through the AddContentTypesToPart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="contentTypes">The content types value used by the operation.</param>
    public void AddContentTypesToPart(XlsxUnknownPartStore data, XlsxContentTypesPart? contentTypes)
    {
        data.AddContentTypesToPart(contentTypes);
    }
}
