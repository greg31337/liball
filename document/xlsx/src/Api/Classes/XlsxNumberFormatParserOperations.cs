using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxNumberFormatParser"/> operations.
/// </summary>
public class XlsxNumberFormatParserOperations : IXlsxNumberFormatParserOperations
{
    /// <summary>
    /// Initializes a new XlsxNumberFormatParserOperations instance.
    /// </summary>
    public XlsxNumberFormatParserOperations()
    {
    }

    /// <summary>
    /// Invokes the ParseBuiltIn form of <see cref="XlsxNumberFormatParser"/>.ParseBuiltIn.
    /// </summary>
    /// <param name="builtInId">The builtInId value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxNumberFormatParseResult ParseBuiltIn(uint builtInId)
    {
        return XlsxNumberFormatParser.ParseBuiltIn(builtInId);
    }

    /// <summary>
    /// Invokes the ParseCustom form of <see cref="XlsxNumberFormatParser"/>.ParseCustom.
    /// </summary>
    /// <param name="formatCode">The formatCode value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxNumberFormatParseResult ParseCustom(string formatCode)
    {
        return XlsxNumberFormatParser.ParseCustom(formatCode);
    }
}
