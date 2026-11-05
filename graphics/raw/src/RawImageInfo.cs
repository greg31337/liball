namespace raw;

internal static class RawModelValidation
{
    internal static void Add(System.Collections.Generic.List<RawValidationMessage> messages, RawDiagnosticCode code, string context, string message)
    {
        messages.Add(new RawValidationMessage(RawValidationCategory.Semantic, code, RawSeverity.Error, RawFormat.Unknown, -1, context, message, false));
    }

    internal static void AddReport(System.Collections.Generic.List<RawValidationMessage> messages, RawValidationReport report)
    {
        if (report == null || report.Messages == null)
        {
            return;
        }
        int index = 0;
        while (index < report.Messages.Length)
        {
            if (report.Messages[index] != null)
            {
                messages.Add(report.Messages[index]);
            }
            index++;
        }
    }

    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal static bool ValidImageGeometry(int width, int height, RawLimits? limits)
    {
        return limits != null && width > 0 && height > 0 && width <= limits.MaxWidth && height <= limits.MaxHeight && (long)width * height <= limits.MaxPixelCount;
    }
}
