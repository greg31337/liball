namespace asn1;

internal class CompilationDiagnosticSet
{
    internal bool TreatWarningsAsErrors;
    internal DiagnosticBag Comparer = new DiagnosticBag(
        false,
        new AsnResourceLimits());
    internal System.Collections.Generic.List<AsnDiagnostic> Items =
        new System.Collections.Generic.List<AsnDiagnostic>();

    internal void AddDiagnostics(AsnDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            AsnDiagnostic clone = this.Comparer.CloneDiagnostic(
                diagnostics[index]);
            if (this.TreatWarningsAsErrors
                && clone.Severity == AsnDiagnosticSeverity.Warning)
            {
                clone.Severity = AsnDiagnosticSeverity.Error;
            }

            if (!this.ContainsDiagnostic(clone))
            {
                this.Items.Add(clone);
            }

            index = index + 1;
        }
    }

    internal void AddDiagnostic(AsnDiagnostic diagnostic)
    {
        this.AddDiagnostics([diagnostic]);
    }

    internal bool ContainsDiagnostic(AsnDiagnostic diagnostic)
    {
        int index = 0;
        while (index < this.Items.Count)
        {
            AsnDiagnostic candidate = this.Items[index];
            if (candidate.Severity == diagnostic.Severity
                && candidate.Category == diagnostic.Category
                && candidate.Code == diagnostic.Code
                && candidate.Message == diagnostic.Message
                && this.Comparer.SpansEqual(candidate.Span, diagnostic.Span))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasErrors()
    {
        int index = 0;
        while (index < this.Items.Count)
        {
            if (this.Items[index].Severity == AsnDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnDiagnostic[] GetDiagnostics()
    {
        AsnDiagnostic[] result = new AsnDiagnostic[this.Items.Count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.Comparer.CloneDiagnostic(this.Items[index]);
            index = index + 1;
        }

        int insertionIndex = 1;
        while (insertionIndex < result.Length)
        {
            AsnDiagnostic current = result[insertionIndex];
            int position = insertionIndex;
            while (position > 0
                && this.Comparer.CompareDiagnostics(
                    result[position - 1],
                    current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            insertionIndex = insertionIndex + 1;
        }

        return result;
    }
}
