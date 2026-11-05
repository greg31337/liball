namespace uri;

internal static class TemplateGrammar
{
    internal static bool IsLiteralScalar(int scalarValue)
    {
        if (scalarValue == 33 ||
            (scalarValue >= 35 && scalarValue <= 36) ||
            scalarValue == 38 ||
            scalarValue == 39 ||
            (scalarValue >= 40 && scalarValue <= 59) ||
            scalarValue == 61 ||
            (scalarValue >= 63 && scalarValue <= 91) ||
            scalarValue == 93 ||
            scalarValue == 95 ||
            (scalarValue >= 97 && scalarValue <= 122) ||
            scalarValue == 126)
        {
            return true;
        }

        return IriGrammar.IsUcsCharacter(scalarValue) ||
            IriGrammar.IsPrivateCharacter(scalarValue);
    }
}
