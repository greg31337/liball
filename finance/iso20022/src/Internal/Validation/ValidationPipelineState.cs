namespace iso20022;

internal class ValidationPipelineState
{
    internal ValidationRequest Request;
    internal ValidationPolicy Policy;
    internal ValidationReport Report;
    internal ValidationStage Stage;
    internal bool Continue = true;

    internal ValidationPipelineState(
        ValidationRequest request,
        ValidationPolicy policy,
        ValidationReport report)
    {
        Request = request;
        Policy = policy;
        Report = report;
    }

    internal void AddDiagnostic(ValidationDiagnostic diagnostic)
    {
        if (!Continue)
        {
            return;
        }

        if (Report.Diagnostics.Count >= Policy.Limits.MaxDiagnosticCount)
        {
            ReachDiagnosticLimit();
            return;
        }

        diagnostic.Stage = Stage;
        if (Policy.RedactOffendingValues &&
            diagnostic.OffendingValue != null &&
            diagnostic.OffendingValue.Length > 0)
        {
            diagnostic.OffendingValue = string.Empty;
            diagnostic.OffendingValueRedacted = true;
        }

        Report.Diagnostics.Add(diagnostic);
        bool makesInvalid = false;
        if (diagnostic.Severity == ValidationSeverity.Information)
        {
            Report.InformationCount++;
        }
        else if (diagnostic.Severity == ValidationSeverity.Warning)
        {
            Report.WarningCount++;
            makesInvalid = Policy.TreatWarningsAsErrors;
        }
        else
        {
            Report.ErrorCount++;
            makesInvalid = true;
        }

        if (makesInvalid)
        {
            Report.Valid = false;
        }

        if (diagnostic.Kind == DiagnosticKind.ResourceLimit)
        {
            Report.Success = false;
            Report.ErrorCode = diagnostic.Code;
            Report.ErrorMessage = diagnostic.Message;
            Report.StoppedEarly = true;
            Continue = false;
        }
        else if (makesInvalid && Policy.FailFast)
        {
            Report.StoppedEarly = true;
            Continue = false;
        }
    }

    internal void ReachDiagnosticLimit()
    {
        Report.Success = false;
        Report.Valid = false;
        Report.DiagnosticLimitReached = true;
        Report.StoppedEarly = true;
        Report.ErrorCode = "validation.diagnostic_limit";
        Report.ErrorMessage =
            "Validation stopped at the configured diagnostic-count limit.";
        Continue = false;
    }
}
