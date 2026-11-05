using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents an error raised at a stable library boundary when an operation cannot return a normal result object.
/// </summary>
public class XlsxException : System.Exception
{
    /// <summary>
    /// Initializes a new exception with a stable message and optional underlying exception.
    /// </summary>
    /// <param name = "message">The human-readable diagnostic message.</param>
    /// <param name = "innerException">The optional underlying exception that caused this exception.</param>
    public XlsxException(string message, System.Exception? innerException) : base(message, innerException)
    {
    }
}
