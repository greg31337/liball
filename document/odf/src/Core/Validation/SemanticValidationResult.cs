namespace odf;

internal class SemanticValidationResult
{
    internal bool IsValid;
    internal bool StoppedEarly;
    internal int ValidatedNodeCount;
    internal OdfDiagnostic[] Diagnostics = [];
}
