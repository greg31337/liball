using email;

namespace email.Tests;

public class EmailHtmlEntityDecoderTests
{
    [Test]
    public void DecodesNamedDecimalHexadecimalAndSupplementaryReferences()
    {
        EmailHtmlEntityDecodeResult result = new EmailHtmlEntityDecoder().DecodeResultWithValue(
            "&lt;p&gt;Tom &amp; Jerry&nbsp;&euro; &#169; &#x1F600;");

        Assert.That(result.GetText(), Is.EqualTo("<p>Tom & Jerry\u00a0€ © 😀"));
        Assert.That(result.GetDecodedReferenceCount(), Is.EqualTo(7));
        Assert.That(result.GetMalformedReferenceCount(), Is.Zero);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void InvalidReferencesArePreservedOrReplacedAndReported()
    {
        EmailHtmlEntityDecodeResult result = new EmailHtmlEntityDecoder().DecodeResultWithValue(
            "&bogus; &#x110000; &#0; &#xD800; &#128; &#65 tail\0");

        Assert.That(result.GetText(), Is.EqualTo("&bogus; � � � € A tail�"));
        Assert.That(result.GetDecodedReferenceCount(), Is.EqualTo(5));
        Assert.That(result.GetMalformedReferenceCount(), Is.EqualTo(7));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlEntityMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void OrdinaryAmpersandsAndUnterminatedNamesRemainLiteral()
    {
        Assert.That(new EmailHtmlEntityDecoder().DecodeWithValue("AT&T &amp &unknown text"), Is.EqualTo("AT&T &amp &unknown text"));
    }

    [Test]
    public void OutputAndArgumentLimitsAreEnforced()
    {
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailHtmlEntityDecoder().DecodeWithValueAndMaximumOutputCharacters("abcd", 3));
        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxHtmlOutputCharacters"));
        Assert.That(limit.GetLimit(), Is.EqualTo(3));
        Assert.That(limit.GetObserved(), Is.EqualTo(4));

        EmailHtmlLimits input = new EmailHtmlLimits();
        input.SetMaxInputCharacters(3);
        EmailLimitException inputLimit = Assert.Throws<EmailLimitException>(() => new EmailHtmlEntityDecoder().DecodeWithValueAndLimits("four", input));
        Assert.That(inputLimit.GetLimitName(), Is.EqualTo("MaxHtmlInputCharacters"));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlEntityDecoder().DecodeWithValue(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHtmlEntityDecoder().DecodeWithValueAndMaximumOutputCharacters("", 0));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlEntityDecoder().DecodeWithValueAndLimits("", (EmailHtmlLimits)null!));
    }
}
