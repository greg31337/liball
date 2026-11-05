namespace unicode.Tools.UnicodeDataGenerator;

public static class UnicodeDataFiles
{
    public const string UnicodeVersion = "17.0.0";
    public const string UcdBaseUrl = "https://www.unicode.org/Public/17.0.0/ucd/";
    public const string EmojiBaseUrl = "https://www.unicode.org/Public/17.0.0/emoji/";
    public const string SecurityBaseUrl = "https://www.unicode.org/Public/17.0.0/security/";

    public static string[] GetRequiredFileNames()
    {
        return new string[]
        {
            "UnicodeData.txt",
            "NameAliases.txt",
            "PropertyAliases.txt",
            "PropertyValueAliases.txt",
            "NamedSequences.txt",
            "PropList.txt",
            "DerivedCoreProperties.txt",
            "Scripts.txt",
            "ScriptExtensions.txt",
            "Blocks.txt",
            "DerivedAge.txt",
            "EastAsianWidth.txt",
            "BidiMirroring.txt",
            "BidiBrackets.txt",
            "extracted/DerivedBidiClass.txt",
            "ArabicShaping.txt",
            "LineBreak.txt",
            "HangulSyllableType.txt",
            "auxiliary/GraphemeBreakProperty.txt",
            "auxiliary/GraphemeBreakTest.txt",
            "auxiliary/WordBreakProperty.txt",
            "auxiliary/WordBreakTest.txt",
            "auxiliary/SentenceBreakProperty.txt",
            "auxiliary/SentenceBreakTest.txt",
            "auxiliary/LineBreakTest.txt",
            "SpecialCasing.txt",
            "CaseFolding.txt",
            "CompositionExclusions.txt",
            "DerivedNormalizationProps.txt",
            "NormalizationTest.txt",
            "emoji/emoji-data.txt",
            "emoji/emoji-variation-sequences.txt",
            "emoji/emoji-sequences.txt",
            "emoji/emoji-zwj-sequences.txt",
            "security/confusables.txt",
            "security/IdentifierStatus.txt",
            "security/IdentifierType.txt",
        };
    }

    public static string GetDownloadUrl(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (fileName == "emoji/emoji-sequences.txt")
        {
            return EmojiBaseUrl + "emoji-sequences.txt";
        }

        if (fileName == "emoji/emoji-zwj-sequences.txt")
        {
            return EmojiBaseUrl + "emoji-zwj-sequences.txt";
        }

        if (fileName.StartsWith("security/", StringComparison.Ordinal))
        {
            return SecurityBaseUrl + fileName.Substring("security/".Length);
        }

        return UcdBaseUrl + fileName;
    }
}
