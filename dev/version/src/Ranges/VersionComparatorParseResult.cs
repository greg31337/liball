namespace version;

internal class VersionComparatorParseResult
{
    internal bool success;
    internal string sourceText;
    internal RangeSyntax syntax;
    internal VersionComparator? comparator;
    internal VersionDiagnostic[] diagnostics;

    internal VersionComparatorParseResult(
        bool successValue,
        string sourceTextValue,
        RangeSyntax syntaxValue,
        VersionComparator? comparatorValue,
        VersionDiagnostic[] diagnosticsValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceTextValue);
        System.ArgumentNullException.ThrowIfNull(diagnosticsValue);

        success = successValue;
        sourceText = sourceTextValue;
        syntax = syntaxValue;
        comparator = comparatorValue;
        diagnostics = CopyDiagnostics(diagnosticsValue);
    }

    internal static VersionComparatorParseResult CreateSuccess(string sourceText, RangeSyntax syntax, VersionComparator comparator)
    {
        System.ArgumentNullException.ThrowIfNull(comparator);

        return new VersionComparatorParseResult(true, sourceText, syntax, comparator, new VersionDiagnostic[0]);
    }

    internal static VersionComparatorParseResult CreateFailure(string sourceText, RangeSyntax syntax, VersionDiagnostic diagnostic)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostic);

        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = diagnostic;
        return new VersionComparatorParseResult(false, sourceText, syntax, null, diagnostics);
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

    internal RangeSyntax GetRangeSyntax()
    {
        return syntax;
    }

    internal bool HasComparator()
    {
        return comparator != null;
    }

    internal VersionComparator GetComparator()
    {
        if (comparator == null)
        {
            throw new System.InvalidOperationException("Parse result does not contain a comparator.");
        }

        return comparator;
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

    internal VersionDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
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
