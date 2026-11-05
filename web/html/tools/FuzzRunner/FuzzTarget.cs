namespace UniversalHtml.FuzzRunner;

internal enum FuzzTarget
{
    Html = 0,
    Selector = 1,
    Css = 2,
    Url = 3
}

internal sealed class GeneratedCase
{
    public GeneratedCase(FuzzTarget targetValue, int caseIndexValue,
        ulong caseSeedValue, string inputValue)
    {
        Target = targetValue;
        CaseIndex = caseIndexValue;
        CaseSeed = caseSeedValue;
        Input = inputValue;
    }

    public FuzzTarget Target { get; }
    public int CaseIndex { get; }
    public ulong CaseSeed { get; }
    public string Input { get; }
}
