using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSentenceSegmentationTests
{
    [Test]
    public void GetSentenceBreaks_ReturnsStartAndEndForEmptyAndPlainText()
    {
        Assert.That(new UnicodeSegments().GetSentenceBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("Hello world"), Is.EqualTo(new int[] { 0, 11 }));
    }

    [Test]
    public void GetSentenceBreaks_BreaksAfterATermAndTrailingSpaces()
    {
        Assert.That(new UnicodeSegments().GetSentenceBreaks("Hi. There"), Is.EqualTo(new int[] { 0, 4, 9 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("Hi.There"), Is.EqualTo(new int[] { 0, 8 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("A. B. C"), Is.EqualTo(new int[] { 0, 3, 6, 7 }));
    }

    [Test]
    public void GetSentenceBreaks_DoesNotBreakDecimalNumbers()
    {
        Assert.That(new UnicodeSegments().GetSentenceBreaks("3.14"), Is.EqualTo(new int[] { 0, 4 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("3. A"), Is.EqualTo(new int[] { 0, 3, 4 }));
    }

    [Test]
    public void GetSentenceBreaks_HandlesCrLfAndControls()
    {
        Assert.That(new UnicodeSegments().GetSentenceBreaks("\r\nA"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("A\rB"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("A\u0001B"), Is.EqualTo(new int[] { 0, 3 }));
    }

    [Test]
    public void GetSentenceBreaks_HandlesAbbreviationsClosersAndContinuations()
    {
        Assert.That(new UnicodeSegments().GetSentenceBreaks("etc. (Next)"), Is.EqualTo(new int[] { 0, 5, 11 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("What?! Next"), Is.EqualTo(new int[] { 0, 7, 11 }));
        Assert.That(new UnicodeSegments().GetSentenceBreaks("A. lower"), Is.EqualTo(new int[] { 0, 8 }));
    }

    [Test]
    public void GetSentenceBreakClass_UsesBoundaryTablesAndFallbacks()
    {
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x000D), Is.EqualTo(UnicodeSentenceBreakClass.Cr));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x000A), Is.EqualTo(UnicodeSentenceBreakClass.Lf));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x002E), Is.EqualTo(UnicodeSentenceBreakClass.ATerm));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x0020), Is.EqualTo(UnicodeSentenceBreakClass.Sp));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x0041), Is.EqualTo(UnicodeSentenceBreakClass.Upper));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x0061), Is.EqualTo(UnicodeSentenceBreakClass.Lower));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x0030), Is.EqualTo(UnicodeSentenceBreakClass.Numeric));
        Assert.That(new UnicodeSegments().GetSentenceBreakClass(0x002C), Is.EqualTo(UnicodeSentenceBreakClass.SContinue));
    }

    [Test]
    public void SentenceHelpers_ExposeRepresentativeRulePredicates()
    {
        UnicodeSentenceBreakClass[] classes = new UnicodeSentenceBreakClass[]
        {
            UnicodeSentenceBreakClass.Upper,
            UnicodeSentenceBreakClass.ATerm,
            UnicodeSentenceBreakClass.Sp,
        };

        Assert.That(UnicodeSegments.SentenceBreakClassFromName("ATerm"), Is.EqualTo(UnicodeSentenceBreakClass.ATerm));
        Assert.That(UnicodeSegments.IsSentenceBreakSeparator(UnicodeSentenceBreakClass.Sep), Is.True);
        Assert.That(UnicodeSegments.IsSentenceIgnored(UnicodeSentenceBreakClass.Extend), Is.True);
        Assert.That(UnicodeSegments.IsSentenceClose(UnicodeSentenceBreakClass.Close), Is.True);
        Assert.That(UnicodeSegments.FindPreviousSignificantSentenceIndex(classes, 2), Is.EqualTo(1));
        Assert.That(UnicodeSegments.IsAfterSentenceTerminal(classes, 2), Is.True);
    }

    [Test]
    public void GetSentenceBreaks_RejectsNullText()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetSentenceBreaks(null!));
    }
}
