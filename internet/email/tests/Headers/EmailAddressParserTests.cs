using System.Text;
using email;

namespace email.Tests;

public class EmailAddressParserTests
{
    [Test]
    public void ParsesBareDotAtomMailboxAndPreservesItsComponents()
    {
        EmailAddressParseResult result = ParseMailbox("user.name+tag@example.com");

        Assert.That(result.HasMailbox(), Is.True);
        EmailAddress address = result.GetMailbox()!;
        Assert.That(address!.HasDisplayName(), Is.False);
        Assert.That(address.GetLocalPart(), Is.EqualTo("user.name+tag"));
        Assert.That(address.GetDomain(), Is.EqualTo("example.com"));
        Assert.That(Text(address.GetRawLocalPartBytes()), Is.EqualTo("user.name+tag"));
        Assert.That(Text(address.GetRawDomainBytes()), Is.EqualTo("example.com"));
        Assert.That(Text(address.GetRawBytes()), Is.EqualTo("user.name+tag@example.com"));
        Assert.That(address.IsDomainLiteral(), Is.False);
        Assert.That(address.UsesObsoleteSyntax(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void ParsesQuotedDisplayNameAndIgnoresItsComments()
    {
        EmailAddressParseResult result = ParseMailbox("\"Doe, John\" (friend) <john@example.com>");

        Assert.That(result.HasMailbox(), Is.True);
        EmailAddress address = result.GetMailbox()!;
        Assert.That(address!.GetDisplayName(), Is.EqualTo("Doe, John"));
        Assert.That(address.GetLocalPart(), Is.EqualTo("john"));
        Assert.That(Text(address.GetRawBytes()), Is.EqualTo("\"Doe, John\" (friend) <john@example.com>"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void RecoversObsoletePeriodTokensInCommonDisplayNames()
    {
        EmailAddressParseResult result = ParseMailbox("John Q. Public <john@example.com>");

        Assert.That(result.GetMailbox()!.GetDisplayName(), Is.EqualTo("John Q. Public"));
        Assert.That(result.GetMailbox()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressObsoleteSyntax), Is.True);
    }

    [Test]
    public void DecodesEncodedWordsInDisplayNamesUsingTheirDeclaredCharset()
    {
        EmailAddressParseResult result = ParseMailbox("=?UTF-8?Q?J=C3=B6hn_Doe?= <john@example.com>");

        Assert.That(result.GetMailbox()!.GetDisplayName(), Is.EqualTo("Jöhn Doe"));
        Assert.That(result.GetMailbox()!.IsInternationalized(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ParsesQuotedLocalPartAndDomainLiteral()
    {
        EmailAddressParseResult result = ParseMailbox("\"local part\"@[IPv6:2001:db8::1]");

        EmailAddress address = result.GetMailbox()!;
        Assert.That(address!.GetLocalPart(), Is.EqualTo("local part"));
        Assert.That(address.GetDomain(), Is.EqualTo("IPv6:2001:db8::1"));
        Assert.That(address.IsDomainLiteral(), Is.True);
        Assert.That(Text(address.GetRawLocalPartBytes()), Is.EqualTo("\"local part\""));
        Assert.That(Text(address.GetRawDomainBytes()), Is.EqualTo("[IPv6:2001:db8::1]"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ParsesMixedMailboxAndGroupEntriesInOrder()
    {
        EmailAddressParseResult result = ParseList("first@example.com, Team: one@example.com, Two <two@example.net>;, last@example.org");

        EmailAddressList list = result.GetAddressList();
        Assert.That(list.GetCount(), Is.EqualTo(3));
        Assert.That(list.GetAt(0).IsMailbox(), Is.True);
        Assert.That(list.GetAt(1).IsGroup(), Is.True);
        Assert.That(list.GetAt(2).IsMailbox(), Is.True);
        Assert.That(list.GetAt(1).GetGroup()!.GetName(), Is.EqualTo("Team"));
        Assert.That(list.GetAt(1).GetGroup()!.GetAddressCount(), Is.EqualTo(2));
        Assert.That(list.GetAt(1).GetGroup()!.GetAddress(1).GetDisplayName(), Is.EqualTo("Two"));
        Assert.That(list.GetMailboxCount(), Is.EqualTo(4));
        Assert.That(list.GetGroupCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void AcceptsAnEmptyNamedGroup()
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseGroup(Encoding.ASCII.GetBytes("Undisclosed recipients:;"), EmailAddressParseMode.Strict);

        Assert.That(result.HasGroup(), Is.True);
        Assert.That(result.GetGroup()!.GetName(), Is.EqualTo("Undisclosed recipients"));
        Assert.That(result.GetGroup()!.GetAddressCount(), Is.Zero);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RecoversObsoleteSourceRoutesWithModeDependentSeverity()
    {
        string input = "Route <@old.example,@relay.example:user@example.com>";

        EmailAddressParseResult tolerant = ParseMailbox(input);
        EmailAddressParseResult strict = new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(input), EmailAddressParseMode.Strict);

        EmailAddress address = tolerant.GetMailbox()!;
        Assert.That(address!.GetLocalPart(), Is.EqualTo("user"));
        Assert.That(address.GetDomain(), Is.EqualTo("example.com"));
        Assert.That(address.HasSourceRoute(), Is.True);
        Assert.That(address.GetSourceRoute(), Is.EqualTo(new[] { "old.example", "relay.example" }));
        Assert.That(address.UsesObsoleteSyntax(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.AddressObsoleteSyntax), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.HasMailbox(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
    }

    [Test]
    public void RecoversCommentsAroundObsoleteLocalAndDomainDots()
    {
        EmailAddressParseResult result = ParseMailbox("first (left) . (right) last@exam (left) . (right) ple.com");

        EmailAddress address = result.GetMailbox()!;
        Assert.That(address!.GetLocalPart(), Is.EqualTo("first.last"));
        Assert.That(address.GetDomain(), Is.EqualTo("exam.ple.com"));
        Assert.That(address.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressObsoleteSyntax), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RecoversObsoleteMixedQuotedAndAtomLocalPartWords()
    {
        EmailAddressParseResult result = ParseMailbox("\"first\".last@example.com");

        Assert.That(result.GetMailbox()!.GetLocalPart(), Is.EqualTo("first.last"));
        Assert.That(result.GetMailbox()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressObsoleteSyntax), Is.True);
    }

    [Test]
    public void AcceptsCfwsAroundTheAtSignWithoutMarkingItObsolete()
    {
        EmailAddressParseResult result = ParseMailbox("local (comment) @ (comment) example.com");

        Assert.That(result.GetMailbox()!.GetLocalPart(), Is.EqualTo("local"));
        Assert.That(result.GetMailbox()!.GetDomain(), Is.EqualTo("example.com"));
        Assert.That(result.GetMailbox()!.UsesObsoleteSyntax(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ParsesInternationalizedDisplayLocalAndDomainParts()
    {
        string input = "Jöhn Dœ <müller@例え.テスト>";

        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(input), EmailAddressParseMode.Strict);

        EmailAddress address = result.GetMailbox()!;
        Assert.That(address!.GetDisplayName(), Is.EqualTo("Jöhn Dœ"));
        Assert.That(address.GetLocalPart(), Is.EqualTo("müller"));
        Assert.That(address.GetDomain(), Is.EqualTo("例え.テスト"));
        Assert.That(address.IsInternationalized(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RejectsInternationalizedInputWhenTheOptionIsDisabled()
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetAllowInternationalized(false);
        options.SetMode(EmailAddressParseMode.Strict);

        EmailAddressParseResult result = new EmailAddressParser().ParseListWithInputAndOptions(Encoding.UTF8.GetBytes("müller@example.com"), options);

        Assert.That(result.GetAddressList().GetCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
    }

    [Test]
    public void MalformedUtf8IsRetainedAsAnIssueAndNotACompletedMailbox()
    {
        byte[] input = new byte[] { 97, 195, 64, 101, 120, 97, 109, 112, 108, 101, 46, 99, 111, 109 };

        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(input, EmailAddressParseMode.Tolerant);

        Assert.That(result.HasMailbox(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderLexicalMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
    }

    [Test]
    public void RecoversObsoleteNullListMembers()
    {
        EmailAddressParseResult result = ParseList(", first@example.com,,second@example.com,");

        Assert.That(result.GetAddressList().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressObsoleteSyntax), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void StrictModeRetainsMalformedRecoveryButReportsFailure()
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes("Name <local@example.com"), EmailAddressParseMode.Strict);

        Assert.That(result.HasMailbox(), Is.True);
        Assert.That(result.GetMailbox()!.GetDisplayName(), Is.EqualTo("Name"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
    }

    [Test]
    public void RejectsLeadingTrailingAndRepeatedDots()
    {
        EmailAddressParseResult leading = ParseMailbox(".local@example.com");
        EmailAddressParseResult repeated = ParseMailbox("local@example..com");
        EmailAddressParseResult trailing = ParseMailbox("local.@example.com");

        Assert.That(leading.HasMailbox(), Is.False);
        Assert.That(repeated.HasMailbox(), Is.False);
        Assert.That(trailing.HasMailbox(), Is.False);
        Assert.That(leading.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
        Assert.That(repeated.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
        Assert.That(trailing.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
    }

    [Test]
    public void TracksAnInitialSourcePositionAcrossFoldingWhiteSpace()
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        EmailSourcePosition initial = new EmailSourcePosition(50, 7, 4);

        EmailAddressParseResult result = new EmailAddressParser().ParseListWithInputAndOptionsAndInitialPosition(Encoding.ASCII.GetBytes("Name\r\n <local@example.com>"), options, initial);

        EmailSourcePosition position = result.GetMailbox()!.GetSourcePosition();
        Assert.That(position.GetByteOffset(), Is.EqualTo(50));
        Assert.That(position.GetLineNumber(), Is.EqualTo(7));
        Assert.That(position.GetColumnNumber(), Is.EqualTo(4));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EnforcesTheAddressCountLimitWithoutAddingTheExcessMailbox()
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetMaximumAddresses(2);

        EmailAddressParseResult result = new EmailAddressParser().ParseListWithInputAndOptions(Encoding.ASCII.GetBytes("a@example.com,b@example.com,c@example.com"), options);

        Assert.That(result.GetAddressList().GetCount(), Is.EqualTo(2));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressLimitExceeded), Is.True);
    }

    [Test]
    public void RejectsInputBeyondTheConfiguredByteLimitBeforeScanning()
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetMaximumInputBytes(5);

        EmailAddressParseResult result = new EmailAddressParser().ParseListWithInputAndOptions(Encoding.ASCII.GetBytes("a@example.com"), options);

        Assert.That(result.GetAddressList().GetCount(), Is.Zero);
        Assert.That(result.GetConsumedByteCount(), Is.Zero);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressLimitExceeded), Is.True);
    }

    [Test]
    public void AddressCollectionsAndRawBytesAreDefensiveSnapshots()
    {
        byte[] input = Encoding.ASCII.GetBytes("Team:a@example.com;");
        EmailAddressParseResult result = new EmailAddressParser().ParseListWithInputAndMode(input, EmailAddressParseMode.Tolerant);
        input[0] = 88;
        List<EmailAddressListEntry> entries = result.GetAddressList().GetAll();
        entries.Clear();
        List<EmailAddress> members = result.GetGroup()!.GetAddresses();
        members.Clear();
        byte[] raw = result.GetGroup()!.GetRawBytes();
        raw[0] = 89;

        Assert.That(result.GetAddressList().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetGroup()!.GetAddressCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetGroup()!.GetRawBytes()), Is.EqualTo("Team:a@example.com;"));
    }

    [Test]
    public void ConvenienceParsersRequireExactlyTheRequestedEntryKind()
    {
        EmailAddressParseResult mailbox = new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes("Group:;"), EmailAddressParseMode.Strict);
        EmailAddressParseResult group = new EmailAddressParser().ParseGroup(Encoding.ASCII.GetBytes("a@example.com"), EmailAddressParseMode.Strict);

        Assert.That(mailbox.HasGroup(), Is.True);
        Assert.That(mailbox.IsSuccess(), Is.False);
        Assert.That(group.HasMailbox(), Is.True);
        Assert.That(group.IsSuccess(), Is.False);
        Assert.That(mailbox.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
        Assert.That(group.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
    }

    [Test]
    public void OptionsValidateAndCloneIndependently()
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetMode(EmailAddressParseMode.Strict);
        options.SetMaximumAddresses(4);
        options.SetMaximumInputBytes(100);
        options.SetAllowInternationalized(false);
        EmailAddressParserOptions clone = options.Clone();
        clone.SetMaximumAddresses(5);

        Assert.That(options.GetMaximumAddresses(), Is.EqualTo(4));
        Assert.That(clone.GetMaximumAddresses(), Is.EqualTo(5));
        Assert.That(clone.GetAllowInternationalized(), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAddresses(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumInputBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailAddressParseMode)99));
    }

    [Test]
    public void EmptyAddressListIsDiagnosed()
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseListWithInputAndMode(Array.Empty<byte>(), EmailAddressParseMode.Strict);

        Assert.That(result.GetAddressList().GetCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
    }

    static EmailAddressParseResult ParseMailbox(string input)
    {
        return new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(input), EmailAddressParseMode.Tolerant);
    }

    static EmailAddressParseResult ParseList(string input)
    {
        return new EmailAddressParser().ParseListWithInputAndMode(Encoding.ASCII.GetBytes(input), EmailAddressParseMode.Tolerant);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
