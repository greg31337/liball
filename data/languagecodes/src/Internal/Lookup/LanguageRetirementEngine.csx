namespace languagecodes;

internal static class LanguageRetirementEngine
{
    internal static LanguageRetirement? GetRetirement(string? code)
    {
        return GetRetirementEntry(code, RetirementIndex.Data);
    }

    internal static RetirementChainRow? ResolveReplacementChain(string? code)
    {
        return ResolveReplacementChainEntry(code, RetirementIndex.Data);
    }

    internal static LanguageRetirement? GetRetirementEntry(
        string? code,
        RetirementIndexData data)
    {
        if (code == null || !data.RecordsByCode.ContainsKey(code))
        {
            return null;
        }

        return PublicResultFactory.CreateLanguageRetirement(
            data.RecordsByCode[code]);
    }

    internal static RetirementChainRow? ResolveReplacementChainEntry(
        string? code,
        RetirementIndexData data)
    {
        if (code == null || !data.ChainsByCode.ContainsKey(code))
        {
            return null;
        }

        return RetirementIndexBuilder.CopyRetirementChain(
            data.ChainsByCode[code]);
    }
}
