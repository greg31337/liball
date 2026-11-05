namespace avif;

/// <summary>
/// Represents an AVIF operation failure for APIs that use exceptions instead of result objects.
/// </summary>
/// <remarks>
/// The result code is the stable machine-readable failure identity. Attached diagnostics provide
/// ordered context accumulated before the operation stopped.
/// </remarks>
public class AvifException : System.Exception
{
    /// <summary>The stable result code for the failure.</summary>
    public AvifResultCode ResultCode;

    /// <summary>A snapshot of the diagnostics attached when the exception was created.</summary>
    public AvifDiagnostic[] Diagnostics;

    /// <summary>
    /// Creates an AVIF exception without attached diagnostics.
    /// </summary>
    /// <param name="resultCode">The stable result code for the failure.</param>
    /// <param name="message">The caller-readable failure message. A null value becomes an empty string.</param>
    /// <returns>The initialized exception.</returns>
    internal static AvifException Create(AvifResultCode resultCode, string message)
    {
        return new AvifException(
            resultCode,
            message,
            System.Array.Empty<AvifDiagnostic>());
    }

    /// <summary>
    /// Creates an AVIF exception with a snapshot of attached diagnostics.
    /// </summary>
    /// <param name="resultCode">The stable result code for the failure.</param>
    /// <param name="message">The caller-readable failure message. A null value becomes an empty string.</param>
    /// <param name="diagnostics">The diagnostics to snapshot. A null value becomes an empty array.</param>
    public AvifException(
        AvifResultCode resultCode,
        string? message,
        AvifDiagnostic[]? diagnostics)
        : base(message == null ? string.Empty : message)
    {
        ResultCode = resultCode;
        if (diagnostics == null)
        {
            Diagnostics = System.Array.Empty<AvifDiagnostic>();
        }
        else
        {
            Diagnostics = new AvifDiagnostic[diagnostics.Length];
            System.Array.Copy(diagnostics, Diagnostics, diagnostics.Length);
        }
    }

    /// <summary>Creates an AVIF exception with attached diagnostics.</summary>
    /// <returns>The initialized exception.</returns>
    internal static AvifException Create(
        AvifResultCode resultCode,
        string? message,
        AvifDiagnostic[]? diagnostics)
    {
        return new AvifException(resultCode, message, diagnostics);
    }
}
