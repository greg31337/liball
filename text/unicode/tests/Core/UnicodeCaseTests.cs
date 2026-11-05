using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCaseTests
{
    [Test]
    public void ToUpper_UsesFullUnconditionalMappingsThenSimpleMappings()
    {
        Assert.That(new UnicodeCase().ToUpper("aA\u00DF\U0001F600"), Is.EqualTo("AASS\U0001F600"));
        Assert.That(new UnicodeCase().ToUpperScalar(0x0061), Is.EqualTo(0x0041));
        Assert.That(new UnicodeCase().ToUpperScalar(0x0041), Is.EqualTo(0x0041));
    }

    [Test]
    public void ToLower_UsesFullUnconditionalMappingsThenSimpleMappings()
    {
        Assert.That(new UnicodeCase().ToLower("Aa\u00DF\u0130\U0001F600"), Is.EqualTo("aa\u00DFi\u0307\U0001F600"));
        Assert.That(new UnicodeCase().ToLowerScalar(0x0041), Is.EqualTo(0x0061));
        Assert.That(new UnicodeCase().ToLowerScalar(0x0061), Is.EqualTo(0x0061));
    }

    [Test]
    public void ToTitle_UsesFullUnconditionalMappingsThenSimpleMappings()
    {
        Assert.That(new UnicodeCase().ToTitle("aA\u00DF\U0001F600"), Is.EqualTo("AASs\U0001F600"));
        Assert.That(new UnicodeCase().ToTitleScalar(0x0061), Is.EqualTo(0x0041));
        Assert.That(new UnicodeCase().ToTitleScalar(0x0041), Is.EqualTo(0x0041));
    }

    [Test]
    public void ToTitleWords_TitlecasesFirstCasedCharacterAndLowercasesTheRemainder()
    {
        IUnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(
            unicodeCase.ToTitleWords("hELLO can't \u01F3UNGLE \u00DFTRA\u1E9Ee"),
            Is.EqualTo("Hello Can't \u01F2ungle Sstra\u00DFe"));
        Assert.That(unicodeCase.ToTitleWords("\u039F\u03A3 \u039F\u03A3\u0391"), Is.EqualTo("\u039F\u03C2 \u039F\u03C3\u03B1"));
        Assert.That(unicodeCase.ToTitleWords(""), Is.EqualTo(""));
    }

    [Test]
    public void ToTitleGraphemes_TitlecasesEachExtendedGraphemeCluster()
    {
        IUnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(
            unicodeCase.ToTitleGraphemes("hELLO a\u0301BC \u00DFx"),
            Is.EqualTo("HELLO A\u0301BC SsX"));
    }

    [Test]
    public void TitlecaseLocaleMethods_ApplyLocaleRulesWithoutAmbiguousOverloads()
    {
        IUnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(unicodeCase.ToTitleLocale("i", "tr"), Is.EqualTo("\u0130"));
        Assert.That(unicodeCase.ToTitleLocale("i\u0307", "lt-LT"), Is.EqualTo("I"));
        Assert.That(unicodeCase.ToTitleWordsLocale("istanbul IZMIR", "tr-TR"), Is.EqualTo("\u0130stanbul Izm\u0131r"));
        Assert.That(unicodeCase.ToTitleGraphemesLocale("iyi", "az_Latn"), Is.EqualTo("\u0130Y\u0130"));
    }

    [Test]
    public void Fold_UsesCommonAndFullRowsOnly()
    {
        Assert.That(new UnicodeCase().Fold("A\u00DF\u0130I"), Is.EqualTo("assi\u0307i"));
        Assert.That(new UnicodeCase().FoldScalar(0x0041), Is.EqualTo(0x0061));
        Assert.That(new UnicodeCase().FoldScalar(0x00DF), Is.EqualTo(0x00DF));
        Assert.That(new UnicodeCase().FoldScalar(0x0049), Is.EqualTo(0x0069));
        Assert.That(UnicodeCase.IsSimpleFoldStatus("C"), Is.True);
        Assert.That(UnicodeCase.IsSimpleFoldStatus("S"), Is.True);
        Assert.That(UnicodeCase.IsSimpleFoldStatus("F"), Is.False);
        Assert.That(UnicodeCase.IsSimpleFoldStatus("T"), Is.False);
        Assert.That(UnicodeCase.IsFullFoldStatus("C"), Is.True);
        Assert.That(UnicodeCase.IsFullFoldStatus("F"), Is.True);
        Assert.That(UnicodeCase.IsFullFoldStatus("S"), Is.False);
        Assert.That(UnicodeCase.IsFullFoldStatus("T"), Is.False);
    }

    [Test]
    public void FoldTurkic_PrefersTurkicRowsAndFallsBackToDefaultFolding()
    {
        IUnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(unicodeCase.Fold("I\u0130"), Is.EqualTo("ii\u0307"));
        Assert.That(unicodeCase.FoldTurkic("I\u0130\u1E9E"), Is.EqualTo("\u0131iss"));
        Assert.That(unicodeCase.FoldScalarTurkic(0x0049), Is.EqualTo(0x0131));
        Assert.That(unicodeCase.FoldScalarTurkic(0x0130), Is.EqualTo(0x0069));
        Assert.That(unicodeCase.FoldScalarTurkic(0x0041), Is.EqualTo(0x0061));
    }

    [Test]
    public void CompareKey_ReturnsOrdinalNfcAndNfkcFoldKeys()
    {
        Assert.That(
            new UnicodeCase().CompareKey("A\u00DF\u0130I", UnicodeCompareKeyMode.OrdinalFold),
            Is.EqualTo("assi\u0307i"));
        Assert.That(
            new UnicodeCase().CompareKey("U\u0308\u0300", UnicodeCompareKeyMode.NfcFold),
            Is.EqualTo("\u01DC"));
        Assert.That(
            new UnicodeCase().CompareKey("\u00B2A\u00DF", UnicodeCompareKeyMode.NfkcFold),
            Is.EqualTo("2ass"));
        Assert.That(
            new UnicodeCase().ToNfkcCaseFold("\u00BDA"),
            Is.EqualTo("1\u20442a"));
    }

    [Test]
    public void CompareKey_NfcModeMakesCanonicallyEquivalentFoldKeysEqual()
    {
        UnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(
            unicodeCase.CompareKey("\u00C5", UnicodeCompareKeyMode.OrdinalFold),
            Is.Not.EqualTo(unicodeCase.CompareKey("A\u030A", UnicodeCompareKeyMode.OrdinalFold)));
        Assert.That(
            unicodeCase.CompareKey("\u00C5", UnicodeCompareKeyMode.NfcFold),
            Is.EqualTo(unicodeCase.CompareKey("A\u030A", UnicodeCompareKeyMode.NfcFold)));
    }

    [Test]
    public void NfkcCaseFold_UsesStableUnicodeMappingsAndRemovesDefaultIgnorables()
    {
        UnicodeCase unicodeCase = new UnicodeCase();
        string input = "A\u030A\u00AD\u200D\uFE0F\u034F\u0132\U0001D400";
        string expected = "\u00E5ija";

        Assert.That(unicodeCase.ToNfkcCaseFold(input), Is.EqualTo(expected));
        Assert.That(unicodeCase.ToNfkcCaseFold(expected), Is.EqualTo(expected));
        Assert.That(unicodeCase.CompareKey(input, UnicodeCompareKeyMode.NfkcFold), Is.EqualTo(expected));
    }

    [Test]
    public void NfkcCaseFold_UsesSharedRangeMappingsAndPreservesUnmappedScalars()
    {
        UnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(unicodeCase.ToNfkcCaseFold("\u0132\u0133"), Is.EqualTo("ijij"));
        Assert.That(unicodeCase.ToNfkcCaseFold("\U0001F600"), Is.EqualTo("\U0001F600"));
        Assert.That(unicodeCase.ToNfkcCaseFold("\u00AD\u200B\uFE0F"), Is.EqualTo(""));
    }

    [Test]
    public void EqualsFolded_ComparesOrdinalFoldKeys()
    {
        Assert.That(new UnicodeCase().EqualsFolded("A\u00DF", "ass"), Is.True);
        Assert.That(new UnicodeCase().EqualsFolded("A", "B"), Is.False);
    }

    [Test]
    public void ToLower_AppliesFinalSigmaContext()
    {
        Assert.That(new UnicodeCase().ToLower("A\u03A3"), Is.EqualTo("a\u03C2"));
        Assert.That(new UnicodeCase().ToLower("A\u0301\u03A3"), Is.EqualTo("a\u0301\u03C2"));
        Assert.That(new UnicodeCase().ToLower("A\u03A3\u0301"), Is.EqualTo("a\u03C2\u0301"));
        Assert.That(new UnicodeCase().ToLower("A\u03A3A"), Is.EqualTo("a\u03C3a"));
        Assert.That(new UnicodeCase().ToLower("\u03A3A"), Is.EqualTo("\u03C3a"));
        Assert.That(new UnicodeCase().ToLower("A\u03A3\u0301A"), Is.EqualTo("a\u03C3\u0301a"));
    }

    [Test]
    public void CaseContexts_UseCompleteDerivedPropertiesAndCombiningClassBarriers()
    {
        UnicodeCase unicodeCase = new UnicodeCase();

        Assert.That(unicodeCase.ToLower("A\u2019\u03A3"), Is.EqualTo("a\u2019\u03C2"));
        Assert.That(unicodeCase.ToLowerLocale("I\u200D\u0301", "lt"), Is.EqualTo("i\u200D\u0301"));
        Assert.That(unicodeCase.ToLowerLocale("I\u0301\u0307", "tr"), Is.EqualTo("\u0131\u0301\u0307"));
        Assert.That(unicodeCase.ToUpperLocale("\u0249\u0307", "lt"), Is.EqualTo("\u0248"));
        Assert.That(UnicodeCase.IsCaseIgnorable(0x2019), Is.True);
        Assert.That(UnicodeCase.IsCased(0x1F130), Is.True);
        Assert.That(UnicodeCase.IsSoftDotted(0x0249), Is.True);
    }

    [Test]
    public void ContextSensitiveCaseMapping_DefersLocaleGatedConditions()
    {
        Assert.That(new UnicodeCase().ToLower("I\u0307"), Is.EqualTo("i\u0307"));
        Assert.That(UnicodeCase.IsLocaleCondition("lt"), Is.True);
        Assert.That(UnicodeCase.IsLocaleCondition("tr"), Is.True);
        Assert.That(UnicodeCase.IsLocaleCondition("Final_Sigma"), Is.False);
    }

    [Test]
    public void ToLowerLocale_AppliesTurkishAndAzeriSpecialCasing()
    {
        Assert.That(new UnicodeCase().ToLowerLocale("I", "tr"), Is.EqualTo("\u0131"));
        Assert.That(new UnicodeCase().ToLowerLocale("I", "az-Latn"), Is.EqualTo("\u0131"));
        Assert.That(new UnicodeCase().ToLowerLocale("\u0130", "tr"), Is.EqualTo("i"));
        Assert.That(new UnicodeCase().ToLowerLocale("\u0130", "az"), Is.EqualTo("i"));
        Assert.That(new UnicodeCase().ToLowerLocale("I\u0307", "tr"), Is.EqualTo("i"));
        Assert.That(new UnicodeCase().ToLowerLocale("I\u0307", "az"), Is.EqualTo("i"));
    }

    [Test]
    public void ToUpperLocale_AppliesTurkishAndAzeriSpecialCasing()
    {
        Assert.That(new UnicodeCase().ToUpper("i"), Is.EqualTo("I"));
        Assert.That(new UnicodeCase().ToUpperLocale("i", "tr"), Is.EqualTo("\u0130"));
        Assert.That(new UnicodeCase().ToUpperLocale("i", "az"), Is.EqualTo("\u0130"));
        Assert.That(new UnicodeCase().ToUpperLocale("i", "TR-tr"), Is.EqualTo("\u0130"));
    }

    [Test]
    public void ToLowerLocale_AppliesLithuanianMoreAboveSpecialCasing()
    {
        Assert.That(new UnicodeCase().ToLower("I\u0301"), Is.EqualTo("i\u0301"));
        Assert.That(new UnicodeCase().ToLowerLocale("I\u0301", "lt"), Is.EqualTo("i\u0307\u0301"));
        Assert.That(new UnicodeCase().ToLowerLocale("I\u0300", "lt-LT"), Is.EqualTo("i\u0307\u0300"));
    }

    [Test]
    public void LocaleHelpers_NormalizeAndMatchPrimaryLanguageSubtags()
    {
        UnicodeScalarRecord[] scalars = new UnicodeScalarReader().ReadAll("I\u0301");
        int[] conditionIndexes = new int[] { 0, 1 };
        string[] conditionNames = new string[] { "lt", "More_Above" };

        Assert.That(UnicodeCase.NormalizeLocale(" TR-tr "), Is.EqualTo("tr"));
        Assert.That(UnicodeCase.ConditionsMatch(0, scalars, 0, 2, conditionIndexes, conditionNames, "lt"), Is.True);
        Assert.That(UnicodeCase.ConditionsMatch(0, scalars, 0, 2, conditionIndexes, conditionNames, ""), Is.False);
    }

    [Test]
    public void CaseContextHelpers_EvaluateRepresentativeContexts()
    {
        UnicodeScalarRecord[] finalSigma = new UnicodeScalarReader().ReadAll("A\u0301\u03A3");
        UnicodeScalarRecord[] notFinalSigma = new UnicodeScalarReader().ReadAll("A\u03A3A");
        UnicodeScalarRecord[] softDotted = new UnicodeScalarReader().ReadAll("i\u0327\u0307");
        UnicodeScalarRecord[] blockedSoftDotted = new UnicodeScalarReader().ReadAll("i\u0301\u0307");
        UnicodeScalarRecord[] moreAbove = new UnicodeScalarReader().ReadAll("I\u0301");
        UnicodeScalarRecord[] beforeDot = new UnicodeScalarReader().ReadAll("I\u0307");

        Assert.That(UnicodeCase.IsFinalSigmaContext(2, finalSigma), Is.True);
        Assert.That(UnicodeCase.IsFinalSigmaContext(1, notFinalSigma), Is.False);
        Assert.That(UnicodeCase.IsAfterSoftDottedContext(2, softDotted), Is.True);
        Assert.That(UnicodeCase.IsAfterSoftDottedContext(2, blockedSoftDotted), Is.False);
        Assert.That(UnicodeCase.IsMoreAboveContext(0, moreAbove), Is.True);
        Assert.That(UnicodeCase.IsBeforeDotContext(0, beforeDot), Is.True);
        Assert.That(UnicodeCase.IsAfterIContext(1, beforeDot), Is.True);
    }

    [Test]
    public void CaseMapping_ReplacesInvalidUtf16WithReplacementCharacter()
    {
        Assert.That(new UnicodeCase().ToUpper("\uD800a"), Is.EqualTo("\uFFFDA"));
        Assert.That(new UnicodeCase().Fold("\uD800A"), Is.EqualTo("\uFFFDa"));
    }

    [Test]
    public void CaseMapping_RejectsNullText()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToUpper(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToLower(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitle(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleLocale(null!, "tr"));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleLocale("i", null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleWords(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleWordsLocale(null!, "tr"));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleWordsLocale("i", null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleGraphemes(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleGraphemesLocale(null!, "tr"));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToTitleGraphemesLocale("i", null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().Fold(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().FoldTurkic(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToUpperLocale(null!, "tr"));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToUpperLocale("i", null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToLowerLocale(null!, "tr"));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToLowerLocale("I", null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().ToNfkcCaseFold(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().CompareKey(null!, UnicodeCompareKeyMode.OrdinalFold));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().EqualsFolded(null!, "a"));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeCase().EqualsFolded("a", null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeCase().CompareKey("a", (UnicodeCompareKeyMode)99));
    }
}
