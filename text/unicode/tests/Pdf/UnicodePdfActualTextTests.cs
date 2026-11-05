using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfActualTextTests
{
    [Test]
    public void PrepareActualText_NormalizesToNfc()
    {
        Assert.That(new UnicodePdfText().PrepareActualText("A\u0300"), Is.EqualTo("\u00C0"));
        Assert.That(new UnicodePdfText().PrepareActualText("plain"), Is.EqualTo("plain"));
    }

    [Test]
    public void PrepareActualText_ReplacesInvalidUtf16BeforeNormalization()
    {
        string prepared = new UnicodePdfText().PrepareActualText("\uD800A");

        Assert.That(prepared, Is.EqualTo("\uFFFDA"));
        Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars(prepared), Is.True);
    }

    [Test]
    public void PrepareActualText_PreservesSupplementaryScalars()
    {
        string text = "A\U0001F600";

        Assert.That(new UnicodePdfText().PrepareActualText(text), Is.EqualTo(text));
        Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars(text), Is.True);
    }

    [Test]
    public void ContainsOnlyValidActualTextScalars_DetectsInvalidUtf16Input()
    {
        Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars("A"), Is.True);
        Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars("\uFFFD"), Is.True);
        Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars("\uD800"), Is.False);
        Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars("\uDC00"), Is.False);
    }

    [Test]
    public void IsActualTextPrepared_RequiresValidScalarsAndNfcText()
    {
        Assert.That(new UnicodePdfText().IsActualTextPrepared(""), Is.True);
        Assert.That(new UnicodePdfText().IsActualTextPrepared("\u00C0"), Is.True);
        Assert.That(new UnicodePdfText().IsActualTextPrepared("A\u0300"), Is.False);
        Assert.That(new UnicodePdfText().IsActualTextPrepared("\uD800"), Is.False);
    }

    [Test]
    public void PrepareActualText_OutputCanBeEncodedAsPdfTextString()
    {
        string prepared = new UnicodePdfText().PrepareActualText("A\u0300");
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString(prepared);

        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo(prepared));
    }

    [Test]
    public void ActualTextHelpers_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().PrepareActualText(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().ContainsOnlyValidActualTextScalars(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().IsActualTextPrepared(null!));
    }
}
