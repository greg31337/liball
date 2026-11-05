using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxUnknownPartStore"/> data.
/// </summary>
public interface IXlsxUnknownPartStoreOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.AddPart through the AddPart operation.
    /// </summary>
    /// <param name="model">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <param name="contentType">The content type value used by the operation.</param>
    /// <param name="data">The data value used by the operation.</param>
    void AddPart(XlsxUnknownPartStore model, string path, string? contentType, byte[] data);
    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.GetPart through the GetPart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxUnknownPart? GetPart(XlsxUnknownPartStore data, string path);
    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.ContainsPart through the ContainsPart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    bool ContainsPart(XlsxUnknownPartStore data, string path);
    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.RemovePart through the RemovePart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    bool RemovePart(XlsxUnknownPartStore data, string path);
    /// <summary>
    /// Invokes <see cref="XlsxUnknownPartStore"/>.AddContentTypesToPart through the AddContentTypesToPart operation.
    /// </summary>
    /// <param name="data">The xlsx unknown part store data to operate on.</param>
    /// <param name="contentTypes">The content types value used by the operation.</param>
    void AddContentTypesToPart(XlsxUnknownPartStore data, XlsxContentTypesPart? contentTypes);
}
