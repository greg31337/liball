namespace asn1;

internal class TimeSettingsParseResult
{
    internal TimePropertySettings? Settings;
    internal string ErrorMessage = "";
    internal int ErrorOffset;
    internal int DiagnosticCode = ConstraintDiagnosticCodes.InvalidTimeSettings;
    internal bool Success;
}
