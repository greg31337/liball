using System.Collections.Generic;

namespace sdp;

internal class SdpDiagnosticLimiter
{
    internal List<SdpDiagnostic> Diagnostics;
    internal int MaximumDiagnosticCount;
    internal bool DiagnosticLimitReached;

    internal SdpDiagnosticLimiter(List<SdpDiagnostic> diagnostics, int maximumDiagnosticCount)
    {
        Diagnostics = diagnostics;
        MaximumDiagnosticCount = maximumDiagnosticCount > 0 ? maximumDiagnosticCount : 1;
        DiagnosticLimitReached = false;
    }

    internal bool AddDiagnostic(SdpDiagnostic diagnostic)
    {
        if (DiagnosticLimitReached)
        {
            if (diagnostic.Severity == SdpValidationSeverity.Fatal && Diagnostics[MaximumDiagnosticCount - 1].Code == SdpDiagnosticCodes.DiagnosticLimitReached)
            {
                Diagnostics[MaximumDiagnosticCount - 1] = diagnostic;
            }
            return false;
        }
        if (Diagnostics.Count < MaximumDiagnosticCount)
        {
            Diagnostics.Add(diagnostic);
            return true;
        }
        if (diagnostic.Severity == SdpValidationSeverity.Fatal)
        {
            Diagnostics[MaximumDiagnosticCount - 1] = diagnostic;
        }
        else if (Diagnostics[MaximumDiagnosticCount - 1].Severity != SdpValidationSeverity.Fatal)
        {
            Diagnostics[MaximumDiagnosticCount - 1] = SdpDiagnosticBuilder.CreateDiagnosticLimitMarker(diagnostic);
        }
        DiagnosticLimitReached = true;
        return false;
    }
}
