namespace raw;

internal static class RawImageBufferValidation
{
    internal static long RequiredSamples(int width, int height, int channelCount, int rowStrideSamples)
    {
        long rowSamples = (long)width * channelCount;
        if (width <= 0 || height <= 0 || channelCount <= 0 || rowSamples > int.MaxValue || rowStrideSamples < rowSamples)
        {
            return -1;
        }
        long preceding = (long)(height - 1) * rowStrideSamples;
        if (preceding > long.MaxValue - rowSamples)
        {
            return -1;
        }
        return preceding + rowSamples;
    }

    internal static void ValidateDiagnostics(System.Collections.Generic.List<RawValidationMessage> messages, RawDiagnostic[] diagnostics, string context)
    {
        if (diagnostics == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, context, "The diagnostic collection cannot be null.");
            return;
        }
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index] == null)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, context, "Every diagnostic entry must be populated.");
                return;
            }
            index++;
        }
    }
}
