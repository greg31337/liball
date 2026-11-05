using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines public package digital signature operations.
/// </summary>
public interface IXlsxPackageSignatureOperations
{
    /// <summary>
    /// Enumerates typed signatures and signed package part references.
    /// </summary>
    /// <param name="workbook">The workbook package model to inspect.</param>
    /// <returns>The signatures and enumeration diagnostics.</returns>
    XlsxSignatureEnumerationResult EnumerateSignatures(XlsxWorkbook workbook);
    /// <summary>
    /// Validates signature values, part references, certificates, and optionally certificate trust.
    /// </summary>
    /// <param name="workbook">The workbook package model to validate.</param>
    /// <param name="evaluateTrust">Whether to build certificate chains against the current trust store.</param>
    /// <returns>The independent validation outcomes.</returns>
    XlsxSignatureValidationResult ValidateSignatures(XlsxWorkbook workbook, bool evaluateTrust);
    /// <summary>
    /// Removes every package signature and its OPC signature infrastructure.
    /// </summary>
    /// <param name="workbook">The workbook package model to change.</param>
    /// <returns>Diagnostics produced by the removal operation.</returns>
    XlsxValidationResult RemoveSignatures(XlsxWorkbook workbook);
    /// <summary>
    /// Authors a supported RSA and SHA-2 OPC signature over selected package parts.
    /// </summary>
    /// <param name="workbook">The workbook package model to sign.</param>
    /// <param name="options">The certificate, algorithm, identifiers, and parts to sign.</param>
    /// <returns>The authored signature and diagnostics.</returns>
    XlsxSignatureAuthoringResult AuthorSignature(XlsxWorkbook workbook, XlsxSignatureAuthoringOptions options);
}
