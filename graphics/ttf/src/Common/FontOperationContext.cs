namespace ttf;

internal static class FontOperationContext
{
    internal static void Checkpoint(FontOperationOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();
    }

    internal static FontLimits CreateEffectiveLimits(
        FontLimits retained,
        FontOperationOptions options)
    {
        if (retained == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        FontLimits selected = options.GetLimits();
        return FontLimits.Create(
            Minimum(retained.GetMaxInputBytes(), selected.GetMaxInputBytes()),
            Minimum(retained.GetMaxOutputBytes(), selected.GetMaxOutputBytes()),
            Minimum(retained.GetMaxFaces(), selected.GetMaxFaces()),
            Minimum(retained.GetMaxTablesPerFace(), selected.GetMaxTablesPerFace()),
            Minimum(retained.GetMaxGlyphsPerFace(), selected.GetMaxGlyphsPerFace()),
            Minimum(retained.GetMaxPointsPerGlyph(), selected.GetMaxPointsPerGlyph()),
            Minimum(retained.GetMaxContoursPerGlyph(), selected.GetMaxContoursPerGlyph()),
            Minimum(retained.GetMaxComponentDepth(), selected.GetMaxComponentDepth()),
            Minimum(retained.GetMaxCffSubroutineDepth(), selected.GetMaxCffSubroutineDepth()),
            Minimum(retained.GetMaxLayoutNesting(), selected.GetMaxLayoutNesting()),
            Minimum(retained.GetMaxLookupsPerTable(), selected.GetMaxLookupsPerTable()),
            Minimum(retained.GetMaxLayoutOperations(), selected.GetMaxLayoutOperations()),
            Minimum(retained.GetMaxCoverageEntries(), selected.GetMaxCoverageEntries()),
            Minimum(retained.GetMaxStringBytes(), selected.GetMaxStringBytes()),
            Minimum(retained.GetMaxDecompressedBytes(), selected.GetMaxDecompressedBytes()),
            Minimum(retained.GetMaxDecompressionRatio(), selected.GetMaxDecompressionRatio()),
            Minimum(retained.GetMaxVariationRegions(), selected.GetMaxVariationRegions()),
            Minimum(retained.GetMaxPaintDepth(), selected.GetMaxPaintDepth()),
            Minimum(retained.GetMaxDiagnostics(), selected.GetMaxDiagnostics()));
    }

    internal static void ValidateOutputLength(
        int length,
        FontLimits limits)
    {
        if (length < 0 || length > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.CreateDiagnostic(
                "Operation output exceeds the configured byte limit.",
                FontDiagnosticCode.LimitExceeded);
        }
    }

    internal static void RejectDiagnosticsInStrictMode(
        FontDiagnostic[] diagnostics,
        FontOperationOptions options,
        string message)
    {
        if (diagnostics == null || options == null || message == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (options.GetMode() != FontLoadMode.Strict)
        {
            return;
        }

        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetSeverity() >=
                FontDiagnosticSeverity.Warning)
            {
                throw FontFormatException.CreateDiagnostic(
                    message,
                    diagnostics[index].GetCode());
            }

            index += 1;
        }
    }

    static int Minimum(int left, int right)
    {
        return left < right ? left : right;
    }
}
