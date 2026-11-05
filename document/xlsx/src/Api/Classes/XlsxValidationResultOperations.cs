using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxValidationResult"/> data.
/// </summary>
public class XlsxValidationResultOperations : IXlsxValidationResultOperations
{
    /// <summary>
    /// Initializes a new xlsx validation result operations instance.
    /// </summary>
    public XlsxValidationResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.AddInfo through the AddInfo operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDiagnostic AddInfo(XlsxValidationResult data, string code, string message)
    {
        return data.AddInfo(code, message);
    }

    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.AddWarning through the AddWarning operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDiagnostic AddWarning(XlsxValidationResult data, string code, string message)
    {
        return data.AddWarning(code, message);
    }

    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.CountSeverity through the CountSeverity operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <param name="severity">The severity value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int CountSeverity(XlsxValidationResult data, XlsxDiagnosticSeverity severity)
    {
        return data.CountSeverity(severity);
    }

    /// <summary>
    /// Invokes <see cref="XlsxValidationResult"/>.HasErrors through the HasErrors operation.
    /// </summary>
    /// <param name="data">The xlsx validation result data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool HasErrors(XlsxValidationResult data)
    {
        return data.HasErrors();
    }
}
