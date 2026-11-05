namespace version;

internal class RubyGemsVersionParseResult
{
    internal bool success;
    internal string sourceText;
    internal RubyGemsVersion? version;
    internal VersionDiagnostic[] diagnostics;

    internal RubyGemsVersionParseResult(bool successValue, string sourceTextValue, RubyGemsVersion? versionValue, VersionDiagnostic[] diagnosticsValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceTextValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticsValue);

        success = successValue;
        sourceText = sourceTextValue;
        version = versionValue;
        diagnostics = CopyDiagnostics(diagnosticsValue);
    }

    internal static RubyGemsVersionParseResult CreateSuccess(string sourceText, RubyGemsVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return new RubyGemsVersionParseResult(true, sourceText, version, new VersionDiagnostic[0]);
    }

    internal static RubyGemsVersionParseResult CreateFailure(string sourceText, VersionDiagnostic diagnostic)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostic);

        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = diagnostic;
        return new RubyGemsVersionParseResult(false, sourceText, null, diagnostics);
    }

    internal bool IsSuccess()
    {
        return success;
    }

    internal bool IsFailure()
    {
        return !success;
    }

    internal string GetSourceText()
    {
        return sourceText;
    }

    internal bool HasVersion()
    {
        return version != null;
    }

    internal RubyGemsVersion GetVersion()
    {
        if (version == null)
        {
            throw new System.InvalidOperationException("Parse result does not contain a RubyGems version.");
        }

        return version;
    }

    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal VersionDiagnostic GetDiagnostic(int diagnosticIndex)
    {
        if (diagnosticIndex < 0 || diagnosticIndex >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("diagnosticIndex");
        }

        return diagnostics[diagnosticIndex];
    }

    internal static VersionDiagnostic[] CopyDiagnostics(VersionDiagnostic[] diagnosticValues)
    {
        VersionDiagnostic[] copy = new VersionDiagnostic[diagnosticValues.Length];
        int index = 0;

        while (index < diagnosticValues.Length)
        {
            if (diagnosticValues[index] == null)
            {
                throw new System.ArgumentException("Diagnostics cannot contain null values.");
            }

            copy[index] = diagnosticValues[index];
            index = index + 1;
        }

        return copy;
    }
}
