using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePublicApiCoverageCompletionTests
{
    [Test]
    public void PreviouslyUncalledInterfaceOperations_HaveBehavioralCoverage()
    {
        IUnicodeIdentifierProfileBuilder xidBuilder = new UnicodeIdentifierProfileBuilder("XID");
        IUnicodeIdentifierProfileBuilder idBuilder = new UnicodeIdentifierProfileBuilder("ID");
        Assert.That(xidBuilder.SetUseXidProperties(true), Is.SameAs(xidBuilder));
        Assert.That(idBuilder.SetUseXidProperties(false), Is.SameAs(idBuilder));

        UnicodeIdentifierProfile xidProfile = xidBuilder.Build();
        UnicodeIdentifierProfile idProfile = idBuilder.Build();
        IUnicodeIdentifiers identifiers = new UnicodeIdentifiers();
        UnicodeRangeSet idStartRanges = identifiers.GetIdStartRangeSet();

        Assert.That(xidProfile.AllowsStart(0x037A), Is.False);
        Assert.That(idProfile.AllowsStart(0x037A), Is.True);
        Assert.That(idStartRanges.Contains(0x0041), Is.True);
        Assert.That(idStartRanges.Contains(0x0030), Is.False);
        Assert.That(idStartRanges.Contains(0x037A), Is.True);
    }

    [Test]
    public void PublicBoundaryClassifiers_CoverInvalidAndAlternateBranches()
    {
        IUnicodeScalar scalar = new UnicodeScalar();
        IUnicodeHangul hangul = new UnicodeHangul();
        IUnicodePdfText pdfText = new UnicodePdfText();

        Assert.That(scalar.IsBmpScalar(-1), Is.False);
        Assert.That(scalar.IsBmpScalar(0xD800), Is.False);
        Assert.That(scalar.IsBmpScalar(0x110000), Is.False);
        Assert.That(scalar.IsSupplementaryScalar(-1), Is.False);
        Assert.That(scalar.IsSupplementaryScalar(0xDFFF), Is.False);
        Assert.That(scalar.IsSupplementaryScalar(0x110000), Is.False);
        Assert.That(hangul.IsLvtSyllable(0x0041), Is.False);
        Assert.That(hangul.ComposeJamoSequence(0x0041, 0x1161, 0x11A8), Is.EqualTo(-1));
        Assert.That(hangul.ComposeJamoSequence(0x1100, 0x1161, 0x0041), Is.EqualTo(-1));
        Assert.That(pdfText.GetTextWidthClass(0x1F1E6), Is.EqualTo(UnicodePdfTextWidth.Wide));
    }

    [Test]
    public void DataConstructors_ExposeFieldsAndRejectEveryInvalidLengthBranch()
    {
        UnicodeBufferResult success = new UnicodeBufferResult(true, 2, 2);
        UnicodeBufferResult shortWrite = new UnicodeBufferResult(false, 2, 1);

        Assert.That(success.Success, Is.True);
        Assert.That(success.RequiredLength, Is.EqualTo(2));
        Assert.That(success.WrittenLength, Is.EqualTo(2));
        Assert.That(shortWrite.Success, Is.False);
        Assert.That(shortWrite.RequiredLength, Is.EqualTo(2));
        Assert.That(shortWrite.WrittenLength, Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeBufferResult(false, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeBufferResult(false, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeBufferResult(false, 1, 2));
        Assert.Throws<ArgumentException>(() => new UnicodeBufferResult(true, 2, 1));

        UnicodePdfLayoutCluster cluster = new UnicodePdfLayoutCluster(
            1,
            3,
            UnicodeScript.Latin,
            UnicodePdfBidiDirection.LeftToRight,
            2);
        Assert.That(cluster.StartUtf16Index, Is.EqualTo(1));
        Assert.That(cluster.EndUtf16Index, Is.EqualTo(3));
        Assert.That(cluster.Utf16Length, Is.EqualTo(2));
        Assert.That(cluster.Script, Is.EqualTo(UnicodeScript.Latin));
        Assert.That(cluster.Direction, Is.EqualTo(UnicodePdfBidiDirection.LeftToRight));
        Assert.That(cluster.DisplayWidth, Is.EqualTo(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfLayoutCluster(
            -1,
            0,
            UnicodeScript.Latin,
            UnicodePdfBidiDirection.LeftToRight,
            1));
        Assert.Throws<ArgumentException>(() => new UnicodePdfLayoutCluster(
            2,
            1,
            UnicodeScript.Latin,
            UnicodePdfBidiDirection.LeftToRight,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfLayoutCluster(
            0,
            1,
            UnicodeScript.Latin,
            UnicodePdfBidiDirection.LeftToRight,
            -1));

        UnicodeRationalValue rational = new UnicodeRationalValue(true, -1, 2);
        Assert.That(rational.HasValue, Is.True);
        Assert.That(rational.Numerator, Is.EqualTo(-1));
        Assert.That(rational.Denominator, Is.EqualTo(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRationalValue(false, 0, 0));
    }

    [Test]
    public void PdfScalarLayoutConstructor_ValidatesEveryScalarAndOffsetInput()
    {
        UnicodePdfScalarLayout layout = CreateScalarLayout(0x0028, 2, 1, 0x0029, 0x0029);

        Assert.That(layout.CodePoint, Is.EqualTo(0x0028));
        Assert.That(layout.Utf16Index, Is.EqualTo(2));
        Assert.That(layout.Utf16Length, Is.EqualTo(1));
        Assert.That(layout.Script, Is.EqualTo(UnicodeScript.Common));
        Assert.That(layout.Direction, Is.EqualTo(UnicodePdfBidiDirection.RightToLeft));
        Assert.That(layout.PairedBracketType, Is.EqualTo(UnicodeBidiPairedBracketType.Open));
        Assert.That(layout.PairedBracketCodePoint, Is.EqualTo(0x0029));
        Assert.That(layout.IsBidiMirrored, Is.True);
        Assert.That(layout.MirrorCodePoint, Is.EqualTo(0x0029));
        Assert.That(layout.Width, Is.EqualTo(UnicodePdfTextWidth.Narrow));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateScalarLayout(0xD800, 0, 1, 0x0029, 0x0029));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateScalarLayout(0x0028, -1, 1, 0x0029, 0x0029));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateScalarLayout(0x0028, 0, 0, 0x0029, 0x0029));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateScalarLayout(0x0028, 0, 3, 0x0029, 0x0029));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateScalarLayout(0x0028, 0, 1, 0xD800, 0x0029));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateScalarLayout(0x0028, 0, 1, 0x0029, 0x110000));
    }

    [Test]
    public void PdfSubsetDataConstructors_ValidateItemsAndCopyCallerArrays()
    {
        int[] scalars = new int[] { 0x0041 };
        UnicodePdfClusterSubsetPlan cluster = new UnicodePdfClusterSubsetPlan(
            0,
            1,
            UnicodeScript.Latin,
            scalars,
            true);
        scalars[0] = 0x005A;
        Assert.That(cluster.Scalars, Is.EqualTo(new int[] { 0x0041 }));
        Assert.Throws<ArgumentException>(() => new UnicodePdfClusterSubsetPlan(
            2,
            1,
            UnicodeScript.Latin,
            new int[] { 0x0041 },
            true));

        UnicodePdfScriptSubsetPlan scriptPlan = new UnicodePdfScriptSubsetPlan(
            UnicodeScript.Latin,
            new int[] { 0x0041 },
            new int[] { 0x0041 },
            new int[] { });
        UnicodePdfScriptSubsetPlan[] scriptPlans = new UnicodePdfScriptSubsetPlan[] { scriptPlan };
        UnicodePdfClusterSubsetPlan[] clusters = new UnicodePdfClusterSubsetPlan[] { cluster };
        UnicodePdfFontSubsetPlan plan = new UnicodePdfFontSubsetPlan(
            new int[] { 0x0041 },
            new int[] { 0x0041 },
            new int[] { },
            scriptPlans,
            clusters);
        scriptPlans[0] = new UnicodePdfScriptSubsetPlan(
            UnicodeScript.Greek,
            new int[] { 0x0370 },
            new int[] { 0x0370 },
            new int[] { });
        clusters[0] = new UnicodePdfClusterSubsetPlan(
            1,
            2,
            UnicodeScript.Greek,
            new int[] { 0x0370 },
            true);

        Assert.That(plan.ScriptPlans[0], Is.SameAs(scriptPlan));
        Assert.That(plan.Clusters[0], Is.SameAs(cluster));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfFontSubsetPlan(
            new int[] { },
            new int[] { },
            new int[] { },
            new UnicodePdfScriptSubsetPlan[] { null! },
            new UnicodePdfClusterSubsetPlan[] { }));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfFontSubsetPlan(
            new int[] { },
            new int[] { },
            new int[] { },
            new UnicodePdfScriptSubsetPlan[] { },
            new UnicodePdfClusterSubsetPlan[] { null! }));
    }

    [Test]
    public void HalfOpenRangeRecords_RejectBeforeStartAndEndPositions()
    {
        UnicodeSegmentRecord segment = new UnicodeSegmentRecord(2, 4);
        UnicodePdfScriptRun scriptRun = new UnicodePdfScriptRun(2, 4, UnicodeScript.Latin);
        UnicodePdfBidiRun bidiRun = new UnicodePdfBidiRun(
            2,
            4,
            UnicodePdfBidiDirection.LeftToRight);

        Assert.That(segment.Contains(1), Is.False);
        Assert.That(segment.Contains(2), Is.True);
        Assert.That(segment.Contains(4), Is.False);
        Assert.That(scriptRun.Contains(1), Is.False);
        Assert.That(scriptRun.Contains(2), Is.True);
        Assert.That(scriptRun.Contains(4), Is.False);
        Assert.That(bidiRun.Contains(1), Is.False);
        Assert.That(bidiRun.Contains(2), Is.True);
        Assert.That(bidiRun.Contains(4), Is.False);
    }

    [Test]
    public void InputEnums_ExerciseEveryDefinedPublicBehaviorChoice()
    {
        IUnicodeByteEncoding byteEncoding = new UnicodeByteEncoding();
        foreach (UnicodeByteEncodingKind kind in Enum.GetValues<UnicodeByteEncodingKind>())
        {
            foreach (UnicodeMalformedInputPolicy policy in Enum.GetValues<UnicodeMalformedInputPolicy>())
            {
                byte[] encoded = byteEncoding.EncodeWithPolicy("A", kind, policy);
                Assert.That(byteEncoding.DecodeWithPolicy(encoded, kind, policy), Is.EqualTo("A"));
            }
        }

        IUnicodeNormalization normalization = new UnicodeNormalization();
        foreach (UnicodeNormalizationForm form in Enum.GetValues<UnicodeNormalizationForm>())
        {
            string normalized = normalization.Normalize("A\u0300", form);
            Assert.That(normalization.IsNormalized(normalized, form), Is.True);
        }

        IUnicodeCase casing = new UnicodeCase();
        foreach (UnicodeCompareKeyMode mode in Enum.GetValues<UnicodeCompareKeyMode>())
        {
            Assert.That(casing.CompareKey("A", mode), Is.EqualTo("a"));
        }

        foreach (UnicodeIdentifierNormalizationPolicy policy in Enum.GetValues<UnicodeIdentifierNormalizationPolicy>())
        {
            UnicodeIdentifierProfile profile = new UnicodeIdentifierProfileBuilder("Normalization")
                .SetNormalizationPolicy(policy)
                .Build();
            Assert.That(profile.Normalize("A"), Is.EqualTo("A"));
        }

        foreach (UnicodeIdentifierCasePolicy policy in Enum.GetValues<UnicodeIdentifierCasePolicy>())
        {
            UnicodeIdentifierProfile profile = new UnicodeIdentifierProfileBuilder("Case")
                .SetCasePolicy(policy)
                .Build();
            Assert.That(profile.GetCompareKey("A"), Is.Not.Empty);
        }

        IUnicodePdfText pdfText = new UnicodePdfText();
        IUnicodePdfDiagnostics pdfDiagnostics = new UnicodePdfDiagnostics();
        foreach (UnicodePdfTextContext context in Enum.GetValues<UnicodePdfTextContext>())
        {
            Assert.That(pdfText.PrepareAccessibilityText("A", context), Is.EqualTo("A"));
        }

        foreach (UnicodePdfConformance conformance in Enum.GetValues<UnicodePdfConformance>())
        {
            UnicodePdfDiagnosticResult result = pdfDiagnostics.CheckPdfConformance(
                "A",
                UnicodePdfTextContext.ActualText,
                conformance,
                false,
                null);
            Assert.That(result.IsValid, Is.True);
        }

        IUnicodeProperties properties = new UnicodeProperties();
        foreach (UnicodeBinaryProperty property in Enum.GetValues<UnicodeBinaryProperty>())
        {
            properties.HasBinaryProperty(0x0041, property);
        }
    }

    static UnicodePdfScalarLayout CreateScalarLayout(
        int codePoint,
        int utf16Index,
        int utf16Length,
        int pairedBracketCodePoint,
        int mirrorCodePoint)
    {
        return new UnicodePdfScalarLayout(
            codePoint,
            utf16Index,
            utf16Length,
            UnicodeScript.Common,
            UnicodePdfBidiDirection.RightToLeft,
            UnicodeBidiPairedBracketType.Open,
            pairedBracketCodePoint,
            true,
            mirrorCodePoint,
            UnicodePdfTextWidth.Narrow);
    }
}
