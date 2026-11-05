using System.Text;
using email;

namespace email.Tests;

public class EmailAddressFormatterTests
{
    [Test]
    public void FormatsBareMailboxDeterministicallyAndCanonicalizesDomainCase()
    {
        EmailAddress address = ParseMailbox("User.Name+tag@Example.COM");

        EmailAddressFormatResult first = new EmailAddressFormatter().FormatWithAddress(address);
        EmailAddressFormatResult second = address.Format(new EmailAddressFormatOptions());

        Assert.That(Text(first), Is.EqualTo("User.Name+tag@example.com"));
        Assert.That(second.GetBytes(), Is.EqualTo(first.GetBytes()));
        Assert.That(first.GetIssues().GetCount(), Is.Zero);
        Assert.That(first.IsSuccess(), Is.True);
    }

    [Test]
    public void UsesMinimalQuotedStringForLocalPartsThatAreNotDotAtoms()
    {
        EmailAddress spaced = ParseMailbox("\"local part\"@example.com");
        EmailAddress escaped = ParseMailbox("\"a\\\"b\\\\c\"@example.com");

        Assert.That(Text(new EmailAddressFormatter().FormatWithAddress(spaced)), Is.EqualTo("\"local part\"@example.com"));
        Assert.That(Text(new EmailAddressFormatter().FormatWithAddress(escaped)), Is.EqualTo("\"a\\\"b\\\\c\"@example.com"));
    }

    [Test]
    public void EmitsSimpleDisplayPhrasesAndQuotesSpecialDisplayNames()
    {
        EmailAddress simple = ParseMailbox("Mary Smith <mary@example.com>");
        EmailAddress dotted = ParseMailbox("John Q. Public <john@example.com>");
        EmailAddress special = ParseMailbox("\"Giant; \\\"Big\\\" Box\" <box@example.com>");

        Assert.That(Text(new EmailAddressFormatter().FormatWithAddress(simple)), Is.EqualTo("Mary Smith <mary@example.com>"));
        Assert.That(Text(new EmailAddressFormatter().FormatWithAddress(dotted)), Is.EqualTo("\"John Q. Public\" <john@example.com>"));
        Assert.That(Text(new EmailAddressFormatter().FormatWithAddress(special)), Is.EqualTo("\"Giant; \\\"Big\\\" Box\" <box@example.com>"));
    }

    [Test]
    public void QuotesLiteralTextThatCouldBeMisreadAsAnEncodedWord()
    {
        EmailAddress address = CreateAddress("=?utf-8?Q?not_encoded?=", "user", "example.com", false);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddress(address);

        Assert.That(Text(result), Is.EqualTo("\"=?utf-8?Q?not_encoded?=\" <user@example.com>"));
    }

    [Test]
    public void EncodesNonAsciiDisplayNamesAsUtf8EncodedWordsByDefault()
    {
        EmailAddress address = CreateAddress("Jöhn Doe", "john", "example.com", false);
        EmailAddressFormatOptions options = new EmailAddressFormatOptions();
        options.SetEncodedWordEncoding(EmailEncodedWordEncodingChoice.QuotedPrintable);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddressAndOptions(address, options);

        Assert.That(Text(result), Is.EqualTo("=?utf-8?Q?J=C3=B6hn_Doe?= <john@example.com>"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EmitsUnicodeDisplayNamesOnlyWhenInternationalizedOutputIsEnabled()
    {
        EmailAddress address = CreateAddress("Jöhn Doe", "john", "example.com", false);
        EmailAddressFormatOptions enabled = new EmailAddressFormatOptions();
        enabled.SetDisplayNameFormat(EmailAddressDisplayNameFormat.Unicode);
        enabled.SetAllowInternationalized(true);
        EmailAddressFormatOptions disabled = enabled.Clone();
        disabled.SetAllowInternationalized(false);

        EmailAddressFormatResult unicode = new EmailAddressFormatter().FormatWithAddressAndOptions(address, enabled);
        EmailAddressFormatResult rejected = new EmailAddressFormatter().FormatWithAddressAndOptions(address, disabled);

        Assert.That(Text(unicode), Is.EqualTo("Jöhn Doe <john@example.com>"));
        Assert.That(rejected.GetBytes(), Is.Empty);
        Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.AddressInternationalizationRequired), Is.True);
        Assert.That(rejected.HasFatalError(), Is.True);
    }

    [Test]
    public void ConvertsUnicodeDomainsToAsciiIdnaByDefault()
    {
        EmailAddress address = CreateAddress(null!, "user", "例え.テスト", false);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddress(address);

        Assert.That(Text(result), Is.EqualTo("user@xn--r8jz45g.xn--zckzah"));
        Assert.That(address.GetAsciiDomain(), Is.EqualTo("xn--r8jz45g.xn--zckzah"));
        Assert.That(new EmailAddressIdn().IsInternationalized(address.GetDomain()), Is.True);
    }

    [Test]
    public void ConvertsAsciiIdnaDomainsBackToUnicodeWhenRequested()
    {
        EmailAddress address = CreateAddress(null!, "user", "xn--r8jz45g.xn--zckzah", false);
        EmailAddressFormatOptions options = new EmailAddressFormatOptions();
        options.SetDomainFormat(EmailAddressDomainFormat.Unicode);
        options.SetAllowInternationalized(true);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddressAndOptions(address, options);

        Assert.That(Text(result), Is.EqualTo("user@例え.テスト"));
        Assert.That(address.GetUnicodeDomain(), Is.EqualTo("例え.テスト"));
    }

    [Test]
    public void RequiresExplicitInternationalizedOutputForNonAsciiLocalParts()
    {
        EmailAddress address = CreateAddress(null!, "müller", "example.com", false);
        EmailAddressFormatOptions enabled = new EmailAddressFormatOptions();
        enabled.SetAllowInternationalized(true);

        EmailAddressFormatResult rejected = new EmailAddressFormatter().FormatWithAddress(address);
        EmailAddressFormatResult formatted = new EmailAddressFormatter().FormatWithAddressAndOptions(address, enabled);

        Assert.That(rejected.GetBytes(), Is.Empty);
        Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.AddressInternationalizationRequired), Is.True);
        Assert.That(Text(formatted), Is.EqualTo("müller@example.com"));
    }

    [Test]
    public void FormatsDomainLiteralsWithoutApplyingIdna()
    {
        EmailAddress address = CreateAddress(null!, "user", "IPv6:2001:db8::1", true);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddress(address);

        Assert.That(Text(result), Is.EqualTo("user@[IPv6:2001:db8::1]"));
        Assert.That(address.GetAsciiDomain(), Is.EqualTo("IPv6:2001:db8::1"));
    }

    [Test]
    public void FormatsPopulatedAndEmptyGroupsCanonically()
    {
        EmailAddressParseResult populated = new EmailAddressParser().ParseGroup(Encoding.ASCII.GetBytes("Team:a@example.com,B <b@example.net>;"), EmailAddressParseMode.Tolerant);
        EmailAddressParseResult empty = new EmailAddressParser().ParseGroup(Encoding.ASCII.GetBytes("Undisclosed recipients:;"), EmailAddressParseMode.Tolerant);

        Assert.That(Text(populated.GetGroup()!.Format(new EmailAddressFormatOptions())), Is.EqualTo("Team: a@example.com, B <b@example.net>;"));
        Assert.That(Text(new EmailAddressFormatter().FormatGroupWithGroup(empty.GetGroup()!)), Is.EqualTo("Undisclosed recipients:;"));
    }

    [Test]
    public void FormatsMixedAddressListsInTheirOriginalEntryOrder()
    {
        EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndMode(Encoding.ASCII.GetBytes("a@example.com,Team:b@example.com;,C <c@example.net>"), EmailAddressParseMode.Tolerant);

        EmailAddressFormatResult result = parsed.GetAddressList().Format(new EmailAddressFormatOptions());

        Assert.That(Text(result), Is.EqualTo("a@example.com, Team: b@example.com;, C <c@example.net>"));
    }

    [Test]
    public void DoesNotRegenerateObsoleteSourceRoutes()
    {
        EmailAddress address = ParseMailbox("Route <@old.example,@relay.example:user@example.com>");

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddress(address);

        Assert.That(Text(result), Is.EqualTo("Route <user@example.com>"));
        Assert.That(Text(result), Does.Not.Contain("old.example"));
    }

    [TestCase("display")]
    [TestCase("local")]
    [TestCase("domain")]
    public void RejectsHeaderInjectionInEveryMailboxComponent(string component)
    {
        string display = component == "display" ? "Safe\r\nBcc: victim@example.com" : "Safe";
        string local = component == "local" ? "safe\r\nBcc: victim" : "local";
        string domain = component == "domain" ? "example.com\r\nBcc: victim@example.com" : "example.com";
        EmailAddress address = CreateAddress(display, local, domain, false);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddress(address);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressInjectionRejected), Is.True);
        Assert.That(result.HasFatalError(), Is.True);
    }

    [Test]
    public void RejectsHeaderInjectionInGroupNames()
    {
        EmailAddressGroup group = new EmailAddressGroup("Safe\nBcc", new List<EmailAddress>(), new byte[0], new EmailSourcePosition(0, 1, 1), false, false);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatGroupWithGroup(group);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressInjectionRejected), Is.True);
    }

    [Test]
    public void RejectsInvalidDomainsAndUnsafeDomainLiterals()
    {
        EmailAddress invalidDomain = CreateAddress(null!, "user", "bad_domain.example", false);
        EmailAddress invalidLiteral = CreateAddress(null!, "user", "bad]literal", true);

        EmailAddressFormatResult domain = new EmailAddressFormatter().FormatWithAddress(invalidDomain);
        EmailAddressFormatResult literal = new EmailAddressFormatter().FormatWithAddress(invalidLiteral);

        Assert.That(domain.GetIssues().ContainsCode(EmailIssueCode.AddressFormattingInvalid), Is.True);
        Assert.That(literal.GetIssues().ContainsCode(EmailIssueCode.AddressFormattingInvalid), Is.True);
        Assert.That(domain.GetBytes(), Is.Empty);
        Assert.That(literal.GetBytes(), Is.Empty);
    }

    [Test]
    public void RejectsInvalidUnicodeWithoutReplacementEncoding()
    {
        EmailAddress address = CreateAddress(null!, "bad\uD800", "example.com", false);
        EmailAddressFormatOptions options = new EmailAddressFormatOptions();
        options.SetAllowInternationalized(true);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddressAndOptions(address, options);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressFormattingInvalid), Is.True);
    }

    [Test]
    public void ReturnsNoPartialValueWhenTheOutputLimitIsExceeded()
    {
        EmailAddress address = ParseMailbox("Name <local@example.com>");
        EmailAddressFormatOptions options = new EmailAddressFormatOptions();
        options.SetMaximumOutputBytes(10);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddressAndOptions(address, options);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.AddressOutputLimitExceeded), Is.True);
        Assert.That(result.HasFatalError(), Is.True);
    }

    [Test]
    public void FoldsLongEncodedDisplayNamesUsingConfiguredHeaderColumns()
    {
        EmailAddress address = CreateAddress("長い表示名長い表示名長い表示名", "user", "example.com", false);
        EmailAddressFormatOptions options = new EmailAddressFormatOptions();
        options.SetEncodedWordEncoding(EmailEncodedWordEncodingChoice.Base64);
        options.SetFirstLineColumn(35);
        options.SetMaximumLineLength(45);

        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddressAndOptions(address, options);

        Assert.That(result.GetFoldCount(), Is.GreaterThan(0));
        Assert.That(Text(result), Does.Contain("\r\n "));
        Assert.That(result.GetFinalLineColumn(), Is.GreaterThan(0));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void FormatResultsDefensivelySnapshotTheirBytesAndIssues()
    {
        EmailAddressFormatResult result = new EmailAddressFormatter().FormatWithAddress(ParseMailbox("a@example.com"));
        byte[] bytes = result.GetBytes();
        bytes[0] = 120;
        EmailIssueList issues = result.GetIssues();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.AddressMalformed, "external"));

        Assert.That(Text(result), Is.EqualTo("a@example.com"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void IdnHelpersValidateUnsafeInput()
    {
        Assert.Throws<ArgumentException>(() => new EmailAddressIdn().ToAscii("bad domain.example"));
        Assert.Throws<ArgumentException>(() => new EmailAddressIdn().ToUnicode("bad@domain.example"));
        Assert.Throws<ArgumentNullException>(() => new EmailAddressIdn().IsInternationalized(null!));
    }

    [Test]
    public void OptionsValidateAndCloneIndependently()
    {
        EmailAddressFormatOptions options = new EmailAddressFormatOptions();
        options.SetDisplayNameFormat(EmailAddressDisplayNameFormat.Unicode);
        options.SetDomainFormat(EmailAddressDomainFormat.Unicode);
        options.SetEncodedWordEncoding(EmailEncodedWordEncodingChoice.Base64);
        options.SetAllowInternationalized(true);
        options.SetFirstLineColumn(12);
        options.SetMaximumLineLength(50);
        options.SetMaximumEncodedWords(4);
        options.SetMaximumOutputBytes(200);
        EmailAddressFormatOptions clone = options.Clone();
        clone.SetMaximumOutputBytes(300);

        Assert.That(options.GetMaximumOutputBytes(), Is.EqualTo(200));
        Assert.That(clone.GetMaximumOutputBytes(), Is.EqualTo(300));
        Assert.That(clone.GetAllowInternationalized(), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetDisplayNameFormat((EmailAddressDisplayNameFormat)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetDomainFormat((EmailAddressDomainFormat)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(19));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEncodedWords(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumOutputBytes(0));
    }

    static EmailAddress ParseMailbox(string value)
    {
        return new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(value), EmailAddressParseMode.Tolerant).GetMailbox()!;
    }

    static EmailAddress CreateAddress(string displayName, string localPart, string domain, bool domainLiteral)
    {
        return new EmailAddress(displayName, localPart, domain, new byte[0], Encoding.UTF8.GetBytes(localPart), Encoding.UTF8.GetBytes(domain), new EmailSourcePosition(0, 1, 1), domainLiteral, EmailAddressFormattingTestText.ContainsNonAscii(localPart) || EmailAddressFormattingTestText.ContainsNonAscii(domain), false, new List<string>());
    }

    static string Text(EmailAddressFormatResult result)
    {
        return Encoding.UTF8.GetString(result.GetBytes());
    }
}

static class EmailAddressFormattingTestText
{
    public static bool ContainsNonAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
