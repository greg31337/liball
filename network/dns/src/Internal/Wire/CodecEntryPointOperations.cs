using System.Collections.Generic;

namespace dns;

internal static class CodecEntryPointOperations
{
    internal static string ReadDiagnosticMessage(
        List<DnsDiagnostic> diagnostics,
        string fallback)
    {
        if (diagnostics == null || diagnostics.Count == 0)
        {
            return fallback;
        }

        DnsDiagnostic diagnostic = diagnostics[0];
        string message = diagnostic.Reason;
        if (message.Length == 0)
        {
            message = fallback;
        }

        if (diagnostic.Field != null)
        {
            message = $"{message} Field: {diagnostic.Field}.";
        }

        if (diagnostic.HasOffset)
        {
            message = $"{message} Offset: {diagnostic.Offset}.";
        }

        if (diagnostic.HasSection)
        {
            message = $"{message} Section: {diagnostic.Section}.";
        }

        if (diagnostic.HasRecordIndex)
        {
            message = $"{message} Record index: {diagnostic.RecordIndex}.";
        }

        return message;
    }
}
