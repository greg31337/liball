using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a package path operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxPackagePathResult
{
    /// <summary>
    /// Stores the input value used by this model.
    /// </summary>
    public string? Input;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string Path;
    /// <summary>
    /// Stores the fragment value used by this model.
    /// </summary>
    public string Fragment;
    /// <summary>
    /// Stores the external target value used by this model.
    /// </summary>
    public string ExternalTarget;
    /// <summary>
    /// Indicates whether external.
    /// </summary>
    public bool IsExternal;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Initializes a new package path result with its default state.
    /// </summary>
    public XlsxPackagePathResult()
    {
        Input = "";
        Path = "";
        Fragment = "";
        ExternalTarget = "";
        IsExternal = false;
        Validation = new XlsxValidationResult();
    }

    /// <summary>
    /// Performs the is valid operation using the current model state.
    /// </summary>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsValid()
    {
        return Validation.IsValid;
    }
}
