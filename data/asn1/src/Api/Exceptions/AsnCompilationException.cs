namespace asn1;

/// <summary>
/// Reports an unrecoverable compilation operation failure.
/// </summary>
public class AsnCompilationException : System.Exception
{
    /// <summary>
    /// Contains the stable machine-readable error code.
    /// </summary>
    public string Code;

    /// <summary>
    /// Contains diagnostics associated with the operation failure.
    /// </summary>
    public AsnDiagnostic[] Diagnostics;

    /// <summary>
    /// Initializes the exception with the shared error payload.
    /// </summary>
    /// <param name="message">The human-readable message, or null to use an empty message.</param>
    /// <param name="code">The stable error code, or null to use an empty code.</param>
    /// <param name="diagnostics">The associated diagnostics, or null to use an empty array.</param>
    public AsnCompilationException(string? message, string? code, AsnDiagnostic[]? diagnostics)
        : base(message == null ? "" : message)
    {
        this.Code = code == null ? "" : code;
        this.Diagnostics = diagnostics == null ? [] : diagnostics;
    }
}

