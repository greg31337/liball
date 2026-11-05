using System.Text;
using email;

namespace email.Tests;

public class EmailStructuredHeaderApiCompletionTests
{
    [Test]
    public void AddressSnapshotsExposeRoutesKindsRawInputAndGroupFlags()
    {
        byte[] routeInput = Bytes("Route <@old.example,@relay.example:user@example.com>");
        EmailAddress route = new EmailAddressParser().ParseMailbox(routeInput, EmailAddressParseMode.Tolerant).GetMailbox()!;
        byte[] listInput = Bytes("first@example.com, Team: second@example.com;");
        EmailAddressList list = new EmailAddressParser().ParseListWithInputAndMode(listInput, EmailAddressParseMode.Strict).GetAddressList();
        EmailAddressGroup group = list.GetAt(1).GetGroup()!;

        Assert.That(route.GetSourceRouteCount(), Is.EqualTo(2));
        Assert.That(route.GetSourceRouteAt(0), Is.EqualTo("old.example"));
        Assert.That(route.GetSourceRouteAt(1), Is.EqualTo("relay.example"));
        Assert.Throws<ArgumentOutOfRangeException>(() => route.GetSourceRouteAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => route.GetSourceRouteAt(2));
        Assert.That(list.GetRawBytes(), Is.EqualTo(listInput));
        Assert.That(list.GetAt(0).GetKind(), Is.EqualTo(EmailAddressListEntryKind.Mailbox));
        Assert.That(list.GetAt(1).GetKind(), Is.EqualTo(EmailAddressListEntryKind.Group));
        Assert.That(group.IsInternationalized(), Is.False);
        Assert.That(group.UsesObsoleteSyntax(), Is.False);

        EmailAddressFormatResult formatted = new EmailAddressFormatter().FormatWithAddress(route);
        Assert.That(formatted.GetByteCount(), Is.EqualTo(formatted.GetBytes().Length));
    }

    [Test]
    public void StructuredParseResultsReportCompleteSourceConsumption()
    {
        byte[] languageInput = Bytes("en, fr");
        EmailContentLanguageParseResult language = new EmailContentLanguageParser().ParseWithInputAndMode(languageInput, EmailStructuredValueParseMode.Strict);
        byte[] dateInput = Bytes("1 Jan 2024 00:00 +0000");
        EmailDateParseResult date = new EmailDateParser().ParseWithInputAndMode(dateInput, EmailDateParseMode.Strict);
        byte[] dispositionInput = Bytes("attachment; filename=x.txt");
        EmailDispositionParseResult disposition = new EmailDispositionParser().ParseWithInputAndMode(dispositionInput, EmailStructuredValueParseMode.Strict);
        byte[] mediaInput = Bytes("text/plain; charset=utf-8");
        EmailMediaTypeParseResult media = new EmailMediaTypeParser().ParseWithInputAndMode(mediaInput, EmailStructuredValueParseMode.Strict);
        byte[] parameterInput = Bytes("charset=utf-8; format=flowed");
        EmailParameterParseResult parameters = new EmailParameterParser().ParseWithInputAndMode(parameterInput, EmailStructuredValueParseMode.Strict);
        byte[] receivedInput = Bytes("from origin.example by mx.example; 1 Jan 2024 00:00 +0000");
        EmailReceivedParseResult received = new EmailReceivedParser().ParseWithInputAndMode(receivedInput, EmailReceivedParseMode.Strict);

        AssertComplete(language.GetConsumedByteCount(), language.GetSourceByteCount(), language.IsComplete(), language.HasFatalError(), languageInput.Length);
        AssertComplete(date.GetConsumedByteCount(), date.GetSourceByteCount(), date.IsComplete(), date.HasFatalError(), dateInput.Length);
        AssertComplete(disposition.GetConsumedByteCount(), disposition.GetSourceByteCount(), disposition.IsComplete(), disposition.HasFatalError(), dispositionInput.Length);
        AssertComplete(media.GetConsumedByteCount(), media.GetSourceByteCount(), media.IsComplete(), media.HasFatalError(), mediaInput.Length);
        AssertComplete(parameters.GetConsumedByteCount(), parameters.GetSourceByteCount(), parameters.IsComplete(), parameters.HasFatalError(), parameterInput.Length);
        AssertComplete(received.GetConsumedByteCount(), received.GetSourceByteCount(), received.IsComplete(), received.HasFatalError(), receivedInput.Length);
        Assert.That(received.GetValue()!.GetClauses(), Has.Count.EqualTo(received.GetValue()!.GetClauseCount()));

        EmailDateFormatResult formattedDate = new EmailDateFormatter().FormatWithValue(date.GetValue()!);
        Assert.That(formattedDate.GetByteCount(), Is.EqualTo(formattedDate.GetBytes().Length));
    }

    [Test]
    public void MetadataCollectionsAndSourceValuesReturnDefensiveSnapshots()
    {
        EmailMetadataParser parser = new EmailMetadataParser();
        byte[] keywordInput = Bytes("alpha, beta");
        EmailMetadataParseResult keywordResult = parser.ParseKeywordsWithInputAndMode(keywordInput, EmailStructuredValueParseMode.Strict);
        EmailKeywordList keywords = keywordResult.GetKeywords()!;
        List<EmailKeyword> keywordSnapshot = keywords.GetAll();
        keywordSnapshot.Clear();

        byte[] sensitivityInput = Bytes("Company-Confidential");
        EmailMetadataParseResult sensitivityResult = parser.ParseSensitivityWithInputAndMode(sensitivityInput, EmailStructuredValueParseMode.Strict);
        EmailSensitivity sensitivity = sensitivityResult.GetSensitivity()!;

        byte[] listInput = Bytes("<https://example.test/help>, <mailto:help@example.test>");
        EmailMetadataParseResult listResult = parser.ParseListManagementWithInputAndKindAndMode(listInput, EmailListManagementKind.Help, EmailStructuredValueParseMode.Strict);
        EmailListManagementValue list = listResult.GetListManagement()!;
        List<EmailListCommand> commandSnapshot = list.GetCommands();
        byte[] firstCommandRaw = list.GetCommand(0).GetRawUriBytes();
        commandSnapshot.Clear();

        Assert.That(keywords.GetCount(), Is.EqualTo(2));
        Assert.That(keywordResult.GetConsumedByteCount(), Is.EqualTo(keywordInput.Length));
        Assert.That(keywordResult.GetSourceByteCount(), Is.EqualTo(keywordInput.Length));
        Assert.That(sensitivity.GetOriginalValue(), Is.EqualTo("Company-Confidential"));
        Assert.That(sensitivity.GetRawBytes(), Is.EqualTo(sensitivityInput));
        Assert.That(list.GetCommandCount(), Is.EqualTo(2));
        Assert.That(Encoding.ASCII.GetString(firstCommandRaw), Is.EqualTo("https://example.test/help"));
        Assert.That(list.GetSourcePosition().GetByteOffset(), Is.Zero);
        Assert.That(listResult.GetConsumedByteCount(), Is.EqualTo(listInput.Length));
        Assert.That(listResult.GetSourceByteCount(), Is.EqualTo(listInput.Length));

        EmailResentField resent = parser.ParseResentFieldWithFieldNameAndInputAndMode("Resent-Message-ID", Bytes("<one@example.test>"), EmailStructuredValueParseMode.Strict).GetResentField()!;
        EmailResentBlock block = new EmailResentBlock(new List<EmailResentField> { resent });
        List<EmailResentField> fields = block.GetAll();
        fields.Clear();
        Assert.That(block.GetCount(), Is.EqualTo(1));
    }

    static void AssertComplete(int consumed, int source, bool complete, bool fatal, int expected)
    {
        Assert.That(consumed, Is.EqualTo(expected));
        Assert.That(source, Is.EqualTo(expected));
        Assert.That(complete, Is.True);
        Assert.That(fatal, Is.False);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
