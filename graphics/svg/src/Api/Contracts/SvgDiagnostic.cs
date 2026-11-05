namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDiagnosticCodeNames"/>.</summary>
public interface ISvgDiagnosticCodeNames
{
    /// <summary>Returns stable external text for a diagnostic code.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The stable <c>SVG</c>-prefixed code text.</returns>
    string Get(SvgDiagnosticCode code);
}
