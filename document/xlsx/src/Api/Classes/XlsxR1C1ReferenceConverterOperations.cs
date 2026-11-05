using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxR1C1ReferenceConverter"/> operations.
/// </summary>
public class XlsxR1C1ReferenceConverterOperations : IXlsxR1C1ReferenceConverterOperations
{
    /// <summary>
    /// Initializes a new XlsxR1C1ReferenceConverterOperations instance.
    /// </summary>
    public XlsxR1C1ReferenceConverterOperations()
    {
    }

    /// <summary>
    /// Invokes the ParseString form of <see cref="XlsxR1C1ReferenceConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxR1C1ReferenceResult ParseString(string text)
    {
        return XlsxR1C1ReferenceConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseSpan form of <see cref="XlsxR1C1ReferenceConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxR1C1ReferenceResult ParseSpan(System.ReadOnlySpan<char> text)
    {
        return XlsxR1C1ReferenceConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseLeading form of <see cref="XlsxR1C1ReferenceConverter"/>.ParseLeading.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxR1C1ReferenceResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        return XlsxR1C1ReferenceConverter.ParseLeading(text);
    }

    /// <summary>
    /// Invokes the Format form of <see cref="XlsxR1C1ReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxR1C1ReferenceResult Format(XlsxR1C1ReferenceValue reference)
    {
        return XlsxR1C1ReferenceConverter.Format(reference);
    }

    /// <summary>
    /// Invokes the FormatToSpan form of <see cref="XlsxR1C1ReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxR1C1ReferenceResult FormatToSpan(XlsxR1C1ReferenceValue reference, System.Span<char> destination)
    {
        return XlsxR1C1ReferenceConverter.Format(reference, destination);
    }

    /// <summary>
    /// Invokes the FormatString form of <see cref="XlsxR1C1ReferenceConverter"/>.FormatString.
    /// </summary>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string FormatString(XlsxR1C1ReferenceValue reference)
    {
        return XlsxR1C1ReferenceConverter.FormatString(reference);
    }

    /// <summary>
    /// Invokes the WriteUnsignedInteger form of <see cref="XlsxR1C1ReferenceConverter"/>.WriteUnsignedInteger.
    /// </summary>
    /// <param name="value">The value value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <param name="start">The start value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int WriteUnsignedInteger(int value, System.Span<char> destination, int start)
    {
        return XlsxR1C1ReferenceConverter.WriteUnsignedInteger(value, destination, start);
    }
}
