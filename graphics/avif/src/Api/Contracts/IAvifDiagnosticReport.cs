namespace avif;

/// <summary>Defines read-only access to an ordered structured diagnostic collection.</summary>
public interface IAvifDiagnosticReport
{
    /// <summary>Returns the number of diagnostics.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();

    /// <summary>Returns one independent diagnostic description.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic, or null for an invalid index.</returns>
    AvifDiagnostic? GetDiagnostic(int index);
}
