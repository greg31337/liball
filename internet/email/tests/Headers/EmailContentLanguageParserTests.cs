using System.Text;
using email;

namespace email.Tests;

public class EmailContentLanguageParserTests
{
    [Test]
    public void ParsesOrderedLanguageListWithCommentsAndFolding()
    {
        const string input = "en-GB, fr (dictionary),\r\n x-klingon";

        EmailContentLanguageParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        EmailContentLanguage value = result.GetValue();
        Assert.That(value.GetCount(), Is.EqualTo(3));
        Assert.That(value.GetAt(0), Is.EqualTo("en-GB"));
        Assert.That(value.GetAt(1), Is.EqualTo("fr"));
        Assert.That(value.GetAt(2), Is.EqualTo("x-klingon"));
        Assert.That(value.GetNormalizedAt(0), Is.EqualTo("en-gb"));
        Assert.That(Text(value.GetRawBytes()), Is.EqualTo(input));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void PreservesDuplicateLanguageTagsAndOriginalCase()
    {
        EmailContentLanguage value = Parse("EN, en, Fr", EmailStructuredValueParseMode.Strict).GetValue();

        Assert.That(value.GetAll(), Is.EqualTo(new[] { "EN", "en", "Fr" }));
    }

    [Test]
    public void EmptyMembersAreRecoveredWithModeDependentSeverity()
    {
        EmailContentLanguageParseResult tolerant = Parse(", en,, fr,", EmailStructuredValueParseMode.Tolerant);
        EmailContentLanguageParseResult strict = Parse(", en,, fr,", EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetValue().GetAll(), Is.EqualTo(new[] { "en", "fr" }));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ContentLanguageMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetValue().GetCount(), Is.EqualTo(2));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void RejectsMalformedLanguageAndRetainsLaterValidMember()
    {
        EmailContentLanguageParseResult result = Parse("bad_tag, fr", EmailStructuredValueParseMode.Tolerant);

        Assert.That(result.GetValue().GetAll(), Is.EqualTo(new[] { "fr" }));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ContentLanguageMalformed), Is.True);
    }

    [TestCase("123, fr")]
    [TestCase("languageislong, fr")]
    [TestCase("en-segment99, fr")]
    public void EnforcesLanguageSubtagSyntax(string input)
    {
        EmailContentLanguageParseResult result = Parse(input, EmailStructuredValueParseMode.Tolerant);

        Assert.That(result.GetValue().GetAll(), Is.EqualTo(new[] { "fr" }));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ContentLanguageMalformed), Is.True);
    }

    [Test]
    public void AppliesLanguageItemLimitAtExactSourcePosition()
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMaximumParameters(1);

        EmailContentLanguageParseResult result = new EmailContentLanguageParser().ParseWithInputAndOptionsAndInitialPosition(Encoding.ASCII.GetBytes("en, fr"), options, new EmailSourcePosition(20, 2, 3));

        Assert.That(result.GetValue().GetAll(), Is.EqualTo(new[] { "en" }));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(6));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.StructuredParameterLimitExceeded), Is.True);
    }

    static EmailContentLanguageParseResult Parse(string value, EmailStructuredValueParseMode mode)
    {
        return new EmailContentLanguageParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), mode);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
