namespace ttf;

internal class SfntValidationResult
{
    FontOutlineKind outlineKind;
    FontDiagnostic[] diagnostics;
    bool valid;
    FontChecksumValidationState checksumValidationState;

    internal SfntValidationResult(
        FontOutlineKind outlineKind,
        FontDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            throw new System.ArgumentNullException("diagnostics");
        }

        this.outlineKind = outlineKind;
        this.diagnostics = diagnostics;
        valid = true;
        checksumValidationState = FontChecksumValidationState.NotApplicable;
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetSeverity() >= FontDiagnosticSeverity.Error)
            {
                valid = false;
            }

            index += 1;
        }
    }

    internal FontOutlineKind GetOutlineKind()
    {
        return outlineKind;
    }

    internal bool IsValid()
    {
        return valid;
    }

    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }

    internal FontChecksumValidationState GetChecksumValidationState()
    {
        return checksumValidationState;
    }

    internal void SetChecksumValidationState(FontChecksumValidationState state)
    {
        checksumValidationState = state;
    }
}
