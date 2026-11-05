namespace languagecodes;

internal class CodeIndexSeedRow
{
    internal string Value;
    internal int EntityIndex;
    internal LanguageCodeForm Form;
    internal Iso639Set Set;

    internal CodeIndexSeedRow(string value, int entityIndex, LanguageCodeForm form, Iso639Set set)
    {
        Value = value;
        EntityIndex = entityIndex;
        Form = form;
        Set = set;
    }
}
