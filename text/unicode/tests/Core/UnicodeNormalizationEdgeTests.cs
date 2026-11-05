using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationEdgeTests
{
    [Test]
    public void QuickCheckSpans_StopAtNoMaybeAndOrderingBoundaries()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();

        Assert.That(normalization.SpanQuickCheckYes("", UnicodeNormalizationForm.Nfc), Is.EqualTo(0));
        Assert.That(normalization.SpanQuickCheckYes("AB\u00B2", UnicodeNormalizationForm.Nfkc), Is.EqualTo(2));
        Assert.That(normalization.SpanQuickCheckYes("\U0001F600A\u00C0", UnicodeNormalizationForm.Nfd), Is.EqualTo(3));
        Assert.That(normalization.SpanQuickCheckYes("A\u0315\u0327", UnicodeNormalizationForm.Nfd), Is.EqualTo(2));
        Assert.That(normalization.SpanQuickCheckYes("A\u0300", UnicodeNormalizationForm.Nfc), Is.EqualTo(1));
    }

    [Test]
    public void AppendAndConcat_RepairNormalizationAcrossBoundaries()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();

        Assert.That(normalization.Append("\u00C5", "\u0301", UnicodeNormalizationForm.Nfc), Is.EqualTo("\u01FA"));
        Assert.That(normalization.Append("A\u0315", "\u0327", UnicodeNormalizationForm.Nfd), Is.EqualTo("A\u0327\u0315"));
        Assert.That(normalization.Append("\u212B", "\u0301", UnicodeNormalizationForm.Nfkc), Is.EqualTo("\u01FA"));
        Assert.That(normalization.Append("\u00B2\u0315", "\u0327", UnicodeNormalizationForm.Nfkd), Is.EqualTo("2\u0327\u0315"));
        Assert.That(normalization.Append("\uD83D", "\uDE00", UnicodeNormalizationForm.Nfc), Is.EqualTo("\U0001F600"));
        Assert.That(
            normalization.Concat(new string[] { "\uD83D", "\uDE00", "U\u0308", "\u0300" }, UnicodeNormalizationForm.Nfc),
            Is.EqualTo("\U0001F600\u01DB"));
    }

    [Test]
    public void StreamSafe_HandlesMultipleLongNonStarterRuns()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string text = new string('\u0301', 95);
        string streamSafe = normalization.ToStreamSafe(text);
        string[] runs = streamSafe.Split('\u034F');

        Assert.That(runs, Has.Length.EqualTo(4));
        Assert.That(runs[0], Has.Length.EqualTo(30));
        Assert.That(runs[1], Has.Length.EqualTo(30));
        Assert.That(runs[2], Has.Length.EqualTo(30));
        Assert.That(runs[3], Has.Length.EqualTo(5));
        Assert.That(normalization.IsStreamSafe(streamSafe), Is.True);
        Assert.That(normalization.ToStreamSafe(streamSafe), Is.EqualTo(streamSafe));
    }

    [Test]
    public void MalformedUtf16_IsReplacedConsistentlyByNormalizationOperations()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string malformed = "A\uD800B\uDC00C";
        string replaced = "A\uFFFDB\uFFFDC";
        UnicodeNormalizationForm[] forms = new UnicodeNormalizationForm[]
        {
            UnicodeNormalizationForm.Nfc,
            UnicodeNormalizationForm.Nfd,
            UnicodeNormalizationForm.Nfkc,
            UnicodeNormalizationForm.Nfkd,
        };

        int index = 0;
        while (index < forms.Length)
        {
            Assert.That(normalization.Normalize(malformed, forms[index]), Is.EqualTo(replaced));
            Assert.That(normalization.QuickCheck(malformed, forms[index]), Is.EqualTo(UnicodeQuickCheckResult.No));
            Assert.That(normalization.IsNormalized(malformed, forms[index]), Is.False);
            Assert.That(normalization.Normalize(replaced, forms[index]), Is.EqualTo(replaced));
            index = index + 1;
        }

        Assert.That(normalization.SpanQuickCheckYes(malformed, UnicodeNormalizationForm.Nfc), Is.EqualTo(1));
        Assert.That(normalization.IsFcd(malformed), Is.False);
        Assert.That(normalization.ToFcd(malformed), Is.EqualTo(replaced));
        Assert.That(normalization.IsFcc(malformed), Is.False);
        Assert.That(normalization.ToFcc(malformed), Is.EqualTo(replaced));
        Assert.That(normalization.IsStreamSafe(malformed), Is.False);
        Assert.That(normalization.ToStreamSafe(malformed), Is.EqualTo(replaced));
    }

    [Test]
    public void NormalizationForms_AreIdempotentForComplexAndMalformedInputs()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string[] inputs = new string[]
        {
            "\u01DB\u0327\u00B2\u1100\u1161\u11A8",
            "\u212B\u0315\u0327\U0001D15E",
            "\uD800A\uDC00",
            "",
        };
        UnicodeNormalizationForm[] forms = new UnicodeNormalizationForm[]
        {
            UnicodeNormalizationForm.Nfc,
            UnicodeNormalizationForm.Nfd,
            UnicodeNormalizationForm.Nfkc,
            UnicodeNormalizationForm.Nfkd,
        };

        int inputIndex = 0;
        while (inputIndex < inputs.Length)
        {
            int formIndex = 0;
            while (formIndex < forms.Length)
            {
                string once = normalization.Normalize(inputs[inputIndex], forms[formIndex]);
                Assert.That(normalization.Normalize(once, forms[formIndex]), Is.EqualTo(once));
                Assert.That(normalization.IsNormalized(once, forms[formIndex]), Is.True);
                formIndex = formIndex + 1;
            }

            inputIndex = inputIndex + 1;
        }
    }

    [Test]
    public void CanonicallyEquivalentInputs_HaveIdenticalCanonicalForms()
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string[] left = new string[]
        {
            "\u212B",
            "\u0958",
            "\uAC01",
        };
        string[] right = new string[]
        {
            "A\u030A",
            "\u0915\u093C",
            "\u1100\u1161\u11A8",
        };

        int index = 0;
        while (index < left.Length)
        {
            Assert.That(normalization.ToNfc(left[index]), Is.EqualTo(normalization.ToNfc(right[index])));
            Assert.That(normalization.ToNfd(left[index]), Is.EqualTo(normalization.ToNfd(right[index])));
            index = index + 1;
        }

        Assert.That(normalization.ToNfkc("\uFB03"), Is.EqualTo(normalization.ToNfkc("ffi")));
        Assert.That(normalization.ToNfkd("\uFB03"), Is.EqualTo(normalization.ToNfkd("ffi")));
    }
}
