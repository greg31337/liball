using System;
using System.Text;

namespace token;

/// <summary>
/// Contains the outcome of reading a PEM block.
/// </summary>
public class PemReadResult
{
    /// <summary>
    /// Indicates whether the PEM block was read successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the decoded PEM block, or <c>null</c> when reading failed.
    /// </summary>
    public PemBlock? Block;

    /// <summary>
    /// Initializes an unsuccessful PEM read result with no details.
    /// </summary>
    public PemReadResult()
    {
        Success = false;
        Error = null;
        Block = null;
    }
}
