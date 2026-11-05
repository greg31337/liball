namespace avif;

/// <summary>Defines uniform access to diagnostics from an operation result or stateful service.</summary>
public interface IAvifDiagnosticSource
{
    /// <summary>Returns an independent report for the current or completed operation.</summary>
    /// <returns>The ordered structured diagnostics.</returns>
    AvifDiagnosticReport GetDiagnostics();
}
