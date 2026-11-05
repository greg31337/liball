namespace odf;

internal class CompatibilityProfilePolicy
{
    internal string Identifier = "";
    internal bool IsValid;
    internal bool IsActive;
    internal bool PreserveAllExtensions;
    internal bool PreserveLibreOfficeExtensions;
    internal bool PreserveOpenOfficeExtensions;
    internal bool PreserveMicrosoftOfficeExtensions;
    internal OdfConformanceClass ConformanceClass;
}
