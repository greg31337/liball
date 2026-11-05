using System.Text;
using email;

namespace email.Tests;

public class EmailMetadataParserTests
{
    [Test]
    public void ParsesKeywordPhrasesAndPreservesEveryMember()
    {
        const string input = "alpha, \"two words\" (note), three words";

        EmailMetadataParseResult result = new EmailMetadataParser().ParseKeywordsWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailKeywordList value = result.GetKeywords()!;
        Assert.That(value!.GetCount(), Is.EqualTo(3));
        Assert.That(value.GetAt(0).GetValue(), Is.EqualTo("alpha"));
        Assert.That(value.GetAt(1).GetValue(), Is.EqualTo("two words"));
        Assert.That(Ascii(value.GetAt(1).GetRawBytes()), Is.EqualTo(" \"two words\" (note)"));
        Assert.That(value.GetAt(2).GetValue(), Is.EqualTo("three words"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void ReportsEmptyAndMalformedKeywordMembersByMode()
    {
        const string input = "alpha,,bad.value,";

        EmailMetadataParseResult tolerant = new EmailMetadataParser().ParseKeywordsWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Tolerant);
        EmailMetadataParseResult strict = new EmailMetadataParser().ParseKeywordsWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetKeywords()!.GetCount(), Is.EqualTo(2));
        Assert.That(tolerant.GetKeywords()!.GetAt(1).IsMalformed(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.KeywordMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetKeywords()!.GetCount(), Is.EqualTo(2));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [TestCase("non-urgent", EmailPriorityKind.NonUrgent)]
    [TestCase("Normal", EmailPriorityKind.Normal)]
    [TestCase("URGENT", EmailPriorityKind.Urgent)]
    public void ParsesPriorityTokensCaseInsensitively(string input, EmailPriorityKind expected)
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParsePriorityWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetPriority()!.GetKind(), Is.EqualTo(expected));
        Assert.That(result.GetPriority()!.GetOriginalValue(), Is.EqualTo(input));
        Assert.That(result.GetPriority()!.IsXPriority(), Is.False);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [TestCase("1 (Highest)", 1, EmailPriorityKind.Urgent)]
    [TestCase("2", 2, EmailPriorityKind.Urgent)]
    [TestCase("3 (Normal)", 3, EmailPriorityKind.Normal)]
    [TestCase("4", 4, EmailPriorityKind.NonUrgent)]
    [TestCase("5 (Lowest)", 5, EmailPriorityKind.NonUrgent)]
    public void ParsesConventionalXPriorityValues(string input, int numeric, EmailPriorityKind expected)
    {
        EmailPriority value = new EmailMetadataParser().ParseXPriorityWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict).GetPriority()!;

        Assert.That(value!.IsXPriority(), Is.True);
        Assert.That(value.GetNumericValue(), Is.EqualTo(numeric));
        Assert.That(value.GetKind(), Is.EqualTo(expected));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [TestCase("low", EmailImportanceKind.Low)]
    [TestCase("NORMAL", EmailImportanceKind.Normal)]
    [TestCase("High", EmailImportanceKind.High)]
    public void ParsesImportanceValues(string input, EmailImportanceKind expected)
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseImportanceWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetImportance()!.GetKind(), Is.EqualTo(expected));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [TestCase("Normal", EmailSensitivityKind.Normal)]
    [TestCase("Personal", EmailSensitivityKind.Personal)]
    [TestCase("Private", EmailSensitivityKind.Private)]
    [TestCase("Company-Confidential", EmailSensitivityKind.CompanyConfidential)]
    public void ParsesSensitivityValues(string input, EmailSensitivityKind expected)
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseSensitivityWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetSensitivity()!.GetKind(), Is.EqualTo(expected));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RetainsUnknownClassificationValuesWithModeSpecificIssues()
    {
        EmailMetadataParseResult tolerant = new EmailMetadataParser().ParseImportanceWithInputAndMode(Bytes("critical"), EmailStructuredValueParseMode.Tolerant);
        EmailMetadataParseResult strict = new EmailMetadataParser().ParseImportanceWithInputAndMode(Bytes("critical"), EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetImportance()!.GetKind(), Is.EqualTo(EmailImportanceKind.Unknown));
        Assert.That(tolerant.GetImportance()!.GetOriginalValue(), Is.EqualTo("critical"));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.MetadataValueMalformed), Is.True);
    }

    [Test]
    public void ParsesAutoSubmittedDispositionAndOrderedParameters()
    {
        const string input = "auto-generated; owner=jobs; reason=\"nightly run\"";

        EmailMetadataParseResult result = new EmailMetadataParser().ParseAutoSubmittedWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailAutoSubmitted value = result.GetAutoSubmitted()!;
        Assert.That(value!.GetKind(), Is.EqualTo(EmailAutoSubmittedKind.AutoGenerated));
        Assert.That(value.GetOriginalToken(), Is.EqualTo("auto-generated"));
        Assert.That(value.GetParameters().GetCount(), Is.EqualTo(2));
        Assert.That(value.GetParameters().GetAt(0).GetName(), Is.EqualTo("owner"));
        Assert.That(Ascii(value.GetParameters().GetAt(0).GetValueBytes()), Is.EqualTo("jobs"));
        Assert.That(Ascii(value.GetParameters().GetAt(1).GetValueBytes()), Is.EqualTo("nightly run"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void AcceptsAndPreservesAutoSubmittedExtensionTokens()
    {
        EmailAutoSubmitted value = new EmailMetadataParser().ParseAutoSubmittedWithInputAndMode(Bytes("x-vacation; owner=bot"), EmailStructuredValueParseMode.Strict).GetAutoSubmitted()!;

        Assert.That(value!.GetKind(), Is.EqualTo(EmailAutoSubmittedKind.Extension));
        Assert.That(value.GetNormalizedToken(), Is.EqualTo("x-vacation"));
        Assert.That(value.GetParameters().GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void RequiresSemicolonsBeforeAutoSubmittedParameters()
    {
        EmailMetadataParseResult tolerant = new EmailMetadataParser().ParseAutoSubmittedWithInputAndMode(Bytes("auto-replied owner=bot"), EmailStructuredValueParseMode.Tolerant);
        EmailMetadataParseResult strict = new EmailMetadataParser().ParseAutoSubmittedWithInputAndMode(Bytes("auto-replied owner=bot"), EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetAutoSubmitted()!.GetParameters().GetCount(), Is.EqualTo(1));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.AutoSubmittedMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ParsesListCommandsIncludingCommaInsideUri()
    {
        const string input = "(preferred) <https://example.test/unsub/a,b>, <mailto:list@example.test?subject=unsubscribe>";

        EmailMetadataParseResult result = new EmailMetadataParser().ParseListManagementWithInputAndKindAndMode(Bytes(input), EmailListManagementKind.Unsubscribe, EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailListManagementValue value = result.GetListManagement()!;
        Assert.That(value!.GetKind(), Is.EqualTo(EmailListManagementKind.Unsubscribe));
        Assert.That(value.GetCommandCount(), Is.EqualTo(2));
        Assert.That(value.GetCommand(0).GetUri(), Is.EqualTo("https://example.test/unsub/a,b"));
        Assert.That(Ascii(value.GetCommand(0).GetRawBytes()), Is.EqualTo("(preferred) <https://example.test/unsub/a,b>"));
        Assert.That(value.GetCommand(1).GetUri(), Is.EqualTo("mailto:list@example.test?subject=unsubscribe"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void ParsesListPostNoAsPostingProhibition()
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseListManagementWithInputAndKindAndMode(Bytes("NO (announcements only)"), EmailListManagementKind.Post, EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetListManagement()!.IsPostingProhibited(), Is.True);
        Assert.That(result.GetListManagement()!.GetCommandCount(), Is.Zero);
    }

    [Test]
    public void ReportsRepeatedListPostNoMarkers()
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseListManagementWithInputAndKindAndMode(Bytes("NO, NO"), EmailListManagementKind.Post, EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetListManagement()!.IsPostingProhibited(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ListManagementMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
    }

    [Test]
    public void RetainsMalformedListValueWithoutCreatingUnsafeCommand()
    {
        EmailMetadataParseResult result = new EmailMetadataParser().ParseListManagementWithInputAndKindAndMode(Bytes("https://example.test/unsubscribe"), EmailListManagementKind.Unsubscribe, EmailStructuredValueParseMode.Tolerant);

        Assert.That(result.GetListManagement()!.GetCommandCount(), Is.Zero);
        Assert.That(Ascii(result.GetListManagement()!.GetRawBytes()), Is.EqualTo("https://example.test/unsubscribe"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ListManagementMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RecognizesOnlyTheRfcOneClickPostArgument()
    {
        EmailMetadataParseResult valid = new EmailMetadataParser().ParseListUnsubscribePostWithInputAndMode(Bytes("List-Unsubscribe=One-Click"), EmailStructuredValueParseMode.Strict);
        EmailMetadataParseResult invalid = new EmailMetadataParser().ParseListUnsubscribePostWithInputAndMode(Bytes("List-Unsubscribe=Confirm"), EmailStructuredValueParseMode.Tolerant);

        Assert.That(valid.GetListUnsubscribePost()!.IsOneClick(), Is.True);
        Assert.That(valid.IsSuccess(), Is.True);
        Assert.That(invalid.GetListUnsubscribePost()!.IsOneClick(), Is.False);
        Assert.That(invalid.GetIssues().ContainsCode(EmailIssueCode.ListUnsubscribePostMalformed), Is.True);
        Assert.That(Ascii(invalid.GetListUnsubscribePost()!.GetRawBytes()), Is.EqualTo("List-Unsubscribe=Confirm"));
    }

    [Test]
    public void AppliesConfiguredItemAndInputLimits()
    {
        EmailMetadataParserOptions itemOptions = new EmailMetadataParserOptions();
        itemOptions.SetMaximumItems(1);
        EmailMetadataParseResult itemResult = new EmailMetadataParser().ParseKeywordsWithInputAndOptionsAndInitialPosition(Bytes("one, two"), itemOptions, new EmailSourcePosition(0, 1, 1));
        EmailMetadataParserOptions byteOptions = new EmailMetadataParserOptions();
        byteOptions.SetMaximumInputBytes(3);
        EmailMetadataParseResult byteResult = new EmailMetadataParser().ParseImportanceWithInputAndOptionsAndInitialPosition(Bytes("normal"), byteOptions, new EmailSourcePosition(0, 1, 1));

        Assert.That(itemResult.HasFatalError(), Is.True);
        Assert.That(itemResult.GetKeywords()!.GetCount(), Is.EqualTo(1));
        Assert.That(itemResult.IsComplete(), Is.False);
        Assert.That(byteResult.HasValue(), Is.False);
        Assert.That(byteResult.HasFatalError(), Is.True);
        Assert.That(byteResult.GetIssues().ContainsCode(EmailIssueCode.MetadataValueLimitExceeded), Is.True);
    }

    [Test]
    public void MetadataModelsDefensivelyCopyRawBytes()
    {
        EmailImportance value = new EmailMetadataParser().ParseImportanceWithInputAndMode(Bytes("High"), EmailStructuredValueParseMode.Strict).GetImportance()!;
        byte[] raw = value!.GetRawBytes();
        raw[0] = (byte)'X';

        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo("High"));
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
