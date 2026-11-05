using System.Text;
using email;

namespace email.Tests;

public class EmailMessageIdFormatterTests
{
    [Test]
    public void FormatsMessageAndContentIdentifiersCanonically()
    {
        EmailMessageId identifier = Parse(" <part.1@Example.COM> ");

        EmailMessageIdFormatResult message = new EmailMessageIdFormatter().FormatWithIdentifier(identifier);
        EmailMessageIdFormatResult content = new EmailMessageIdFormatter().FormatContentIdWithIdentifier(identifier);

        Assert.That(Text(message), Is.EqualTo("<part.1@Example.COM>"));
        Assert.That(content.GetBytes(), Is.EqualTo(message.GetBytes()));
        Assert.That(message.GetByteCount(), Is.EqualTo(message.GetBytes().Length));
        Assert.That(message.GetIssues().GetCount(), Is.Zero);
        Assert.That(message.IsSuccess(), Is.True);
    }

    [Test]
    public void FormatsDomainLiteralWithoutLosingItsDelimiters()
    {
        EmailMessageId identifier = Parse("<part@[IPv6:2001:db8::1]>");

        Assert.That(Text(identifier.Format()), Is.EqualTo("<part@[IPv6:2001:db8::1]>"));
    }

    [Test]
    public void CanonicalizesRecoverableBareAndObsoleteInternalSyntax()
    {
        EmailMessageId bare = Parse("bare@example.com");
        EmailMessageId obsolete = Parse("<left (comment) . part @ example . com>");

        Assert.That(Text(bare.Format()), Is.EqualTo("<bare@example.com>"));
        Assert.That(Text(obsolete.Format()), Is.EqualTo("<left.part@example.com>"));
    }

    [Test]
    public void FormatsListsWithSingleSpacesAndNoObsoletePhraseText()
    {
        EmailMessageIdList identifiers = new EmailMessageIdParser().ParseReferencesWithInputAndMode(Bytes("old text <one@example> and <two@example>"), EmailMessageIdParseMode.Tolerant).GetIdentifiers();

        EmailMessageIdFormatResult result = new EmailMessageIdFormatter().FormatListWithIdentifiers(identifiers);

        Assert.That(Text(result), Is.EqualTo("<one@example> <two@example>"));
        Assert.That(Text(result), Does.Not.Contain("old text"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(identifiers.GetAll(), Has.Count.EqualTo(2));
        Assert.That(identifiers.UsesObsoleteSyntax(), Is.False);
    }

    [Test]
    public void FoldsListsOnlyBetweenCompleteIdentifiers()
    {
        EmailMessageIdList identifiers = new EmailMessageIdParser().ParseReferencesWithInputAndMode(Bytes("<first.long.identifier@example.com> <second.long.identifier@example.net>"), EmailMessageIdParseMode.Strict).GetIdentifiers();
        EmailMessageIdFormatOptions options = new EmailMessageIdFormatOptions();
        options.SetFirstLineColumn(20);
        options.SetMaximumLineLength(50);

        EmailMessageIdFormatResult result = identifiers.FormatWithOptions(options);

        Assert.That(Text(result), Is.EqualTo("<first.long.identifier@example.com>\r\n <second.long.identifier@example.net>"));
        Assert.That(result.GetFoldCount(), Is.EqualTo(1));
        Assert.That(result.GetFinalLineColumn(), Is.EqualTo(37));
    }

    [Test]
    public void RejectsObsoleteQuotedValueThatCannotBecomeDotAtomText()
    {
        EmailMessageId identifier = Parse("<\"quoted value\"@example.com>");

        EmailMessageIdFormatResult result = new EmailMessageIdFormatter().FormatWithIdentifier(identifier);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdFormattingInvalid), Is.True);
        Assert.That(result.HasFatalError(), Is.True);
    }

    [Test]
    public void RejectsUnsafeConstructedComponents()
    {
        EmailMessageId identifier = new EmailMessageId("left\r\nBcc", "example.com", false, new byte[0], new EmailSourcePosition(0, 1, 1), false, false);

        EmailMessageIdFormatResult result = identifier.Format();

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdFormattingInvalid), Is.True);
    }

    [Test]
    public void AppliesOutputLimitWithoutReturningPartialBytes()
    {
        EmailMessageIdFormatOptions options = new EmailMessageIdFormatOptions();
        options.SetMaximumOutputBytes(5);

        EmailMessageIdFormatResult result = new EmailMessageIdFormatter().FormatWithIdentifierAndOptions(Parse("<id@example>"), options);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdOutputLimitExceeded), Is.True);
        Assert.That(result.HasFatalError(), Is.True);
    }

    [Test]
    public void RejectsEmptyIdentifierList()
    {
        EmailMessageIdList identifiers = new EmailMessageIdList(new List<EmailMessageId>(), new byte[0]);

        EmailMessageIdFormatResult result = identifiers.Format();

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdFormattingInvalid), Is.True);
    }

    [Test]
    public void FormatResultsDefensivelySnapshotBytesAndIssues()
    {
        EmailMessageIdFormatResult result = new EmailMessageIdFormatter().FormatWithIdentifier(Parse("<id@example>"));
        byte[] bytes = result.GetBytes();
        bytes[1] = (byte)'X';
        EmailIssueList issues = result.GetIssues();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.MessageIdMalformed, "external"));

        Assert.That(Text(result), Is.EqualTo("<id@example>"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(Parse("<id@example>").FormatWithOptions(new EmailMessageIdFormatOptions()).IsSuccess(), Is.True);
    }

    [Test]
    public void FormatOptionsValidateAndCloneIndependently()
    {
        EmailMessageIdFormatOptions options = new EmailMessageIdFormatOptions();
        options.SetFirstLineColumn(12);
        options.SetMaximumLineLength(60);
        options.SetMaximumOutputBytes(200);
        EmailMessageIdFormatOptions clone = options.Clone();
        clone.SetMaximumOutputBytes(300);

        Assert.That(options.GetMaximumOutputBytes(), Is.EqualTo(200));
        Assert.That(clone.GetMaximumOutputBytes(), Is.EqualTo(300));
        Assert.That(clone.GetFirstLineColumn(), Is.EqualTo(12));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFirstLineColumn(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(19));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumOutputBytes(0));
    }

    static EmailMessageId Parse(string value)
    {
        return new EmailMessageIdParser().ParseMessageIdWithInputAndMode(Bytes(value), EmailMessageIdParseMode.Tolerant).GetIdentifier()!;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(EmailMessageIdFormatResult result)
    {
        return Encoding.ASCII.GetString(result.GetBytes());
    }
}
