namespace odf;

internal class XmlValidationResult
{
    internal bool IsValid;
    internal bool StoppedEarly;
    internal int ValidatedPartCount;
    internal OdfDiagnostic[] Diagnostics = [];
}
