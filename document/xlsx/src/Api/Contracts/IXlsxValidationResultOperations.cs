using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxValidationResult"/> data.
/// </summary>
public interface IXlsxValidationResultOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.AddInfo through the AddInfo operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxDiagnostic AddInfo(XlsxValidationResult data, string code, string message);
    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.AddWarning through the AddWarning operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxDiagnostic AddWarning(XlsxValidationResult data, string code, string message);
    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.CountSeverity through the CountSeverity operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <param name="severity">The severity value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    int CountSeverity(XlsxValidationResult data, XlsxDiagnosticSeverity severity);
    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.HasErrors through the HasErrors operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    bool HasErrors(XlsxValidationResult data);
}
