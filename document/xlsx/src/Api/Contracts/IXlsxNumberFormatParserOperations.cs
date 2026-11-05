using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines instance-based public access to <see cref="XlsxNumberFormatParser"/> operations.
/// </summary>
public interface IXlsxNumberFormatParserOperations
{
    /// <summary>
    /// Invokes the ParseBuiltIn form of <see cref="XlsxNumberFormatParser"/>.ParseBuiltIn.
    /// </summary>
    /// <param name="builtInId">The builtInId value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxNumberFormatParseResult ParseBuiltIn(uint builtInId);
    /// <summary>
    /// Invokes the ParseCustom form of <see cref="XlsxNumberFormatParser"/>.ParseCustom.
    /// </summary>
    /// <param name="formatCode">The formatCode value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxNumberFormatParseResult ParseCustom(string formatCode);
}
