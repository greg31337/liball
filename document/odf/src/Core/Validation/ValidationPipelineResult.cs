namespace odf;

internal class ValidationPipelineResult
{
    internal bool IsValid;
    internal bool StoppedEarly;
    internal bool PackageValidated;
    internal bool VersionValidated;
    internal bool CompatibilityValidated;
    internal bool XmlValidated;
    internal bool SemanticsValidated;
    internal int RecoveredDiagnosticCount;
    internal string Profile = "";
    internal VersionAnalysisResult? VersionAnalysis;
    internal OdfDiagnostic[] Diagnostics = [];
}
