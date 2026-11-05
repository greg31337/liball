namespace tiff;

internal static class TiffValidationPolicy
{
    internal static TiffValidationMode GetMode(TiffReadOptions options)
    {
        if (options != null && !options.Strict)
        {
            return TiffValidationMode.Permissive;
        }

        return TiffValidationMode.Strict;
    }

    internal static void AddConformanceIssue(TiffDiagnostics diagnostics, TiffReadOptions options, TiffErrorCode code, string message, long offset, int imageIndex, int tagId)
    {
        TiffDiagnosticSeverity severity = TiffDiagnosticSeverity.Error;
        if (options != null && !options.Strict)
        {
            severity = TiffDiagnosticSeverity.Warning;
        }

        diagnostics.Add(TiffDiagnostic.Create(severity, code, message, offset, imageIndex, tagId));
    }

    internal static void AddFatalIssue(TiffDiagnostics diagnostics, TiffErrorCode code, string message, long offset, int imageIndex, int tagId)
    {
        diagnostics.Add(TiffDiagnostic.Create(TiffDiagnosticSeverity.Error, code, message, offset, imageIndex, tagId));
    }
}
