using System;
using System.Text;

namespace token;

/// <summary>
/// Contains the outcome of scanning compact JOSE token segments.
/// </summary>
public class JoseCompactScanResult
{
    /// <summary>
    /// Indicates whether scanning succeeded.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains segment boundaries, or <c>null</c> when scanning failed.
    /// </summary>
    public JoseCompactSegments? Segments;

    /// <summary>
    /// Initializes an unsuccessful scan result with no details.
    /// </summary>
    public JoseCompactScanResult()
    {
        Success = false;
        Error = null;
        Segments = null;
    }
}
