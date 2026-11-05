using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxA1RangeReferenceConverter"/> operations.
/// </summary>
public class XlsxA1RangeReferenceConverterOperations : IXlsxA1RangeReferenceConverterOperations
{
    /// <summary>
    /// Initializes a new XlsxA1RangeReferenceConverterOperations instance.
    /// </summary>
    public XlsxA1RangeReferenceConverterOperations()
    {
    }

    /// <summary>
    /// Invokes the ParseString form of <see cref="XlsxA1RangeReferenceConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1RangeReferenceResult ParseString(string? text)
    {
        return XlsxA1RangeReferenceConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseSpan form of <see cref="XlsxA1RangeReferenceConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1RangeReferenceResult ParseSpan(System.ReadOnlySpan<char> text)
    {
        return XlsxA1RangeReferenceConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseLeading form of <see cref="XlsxA1RangeReferenceConverter"/>.ParseLeading.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1RangeReferenceResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        return XlsxA1RangeReferenceConverter.ParseLeading(text);
    }

    /// <summary>
    /// Invokes the Format form of <see cref="XlsxA1RangeReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1RangeReferenceResult Format(XlsxA1RangeReferenceValue range)
    {
        return XlsxA1RangeReferenceConverter.Format(range);
    }

    /// <summary>
    /// Invokes the FormatToSpan form of <see cref="XlsxA1RangeReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1RangeReferenceResult FormatToSpan(XlsxA1RangeReferenceValue range, System.Span<char> destination)
    {
        return XlsxA1RangeReferenceConverter.Format(range, destination);
    }

    /// <summary>
    /// Invokes the FormatString form of <see cref="XlsxA1RangeReferenceConverter"/>.FormatString.
    /// </summary>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string FormatString(XlsxA1RangeReferenceValue range)
    {
        return XlsxA1RangeReferenceConverter.FormatString(range);
    }
}
