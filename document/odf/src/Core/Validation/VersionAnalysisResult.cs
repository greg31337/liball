namespace odf;

internal class VersionAnalysisResult
{
    internal bool IsValid;
    internal bool IsSupported;
    internal bool IsUpgrade;
    internal bool IsDowngrade;
    internal bool CanConvertWithoutLoss;
    internal bool PreservesFutureContent;
    internal bool VersionWasInferred;
    internal OdfVersion SourceVersion;
    internal OdfVersion TargetVersion;
    internal VersionMigrationStep[] Steps = [];
    internal OdfDiagnostic[] Diagnostics = [];
}
