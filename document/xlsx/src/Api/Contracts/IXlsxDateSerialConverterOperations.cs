using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines instance-based public access to <see cref="XlsxDateSerialConverter"/> operations.
/// </summary>
public interface IXlsxDateSerialConverterOperations
{
    /// <summary>
    /// Invokes the FromSerial form of <see cref="XlsxDateSerialConverter"/>.FromSerial.
    /// </summary>
    /// <param name="serialValue">The serialValue value used by the operation.</param>
    /// <param name="use1904DateSystem">The use1904DateSystem value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxDateSerialConversionResult FromSerial(double serialValue, bool use1904DateSystem);
    /// <summary>
    /// Invokes the ToSerial form of <see cref="XlsxDateSerialConverter"/>.ToSerial.
    /// </summary>
    /// <param name="dateTimeValue">The dateTimeValue value used by the operation.</param>
    /// <param name="use1904DateSystem">The use1904DateSystem value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxDateSerialConversionResult ToSerial(System.DateTime dateTimeValue, bool use1904DateSystem);
}
