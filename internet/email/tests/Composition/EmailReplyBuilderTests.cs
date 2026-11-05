using System.Text;
using email;

namespace email.Tests;

public class EmailReplyBuilderTests
{
    [Test]
    public void ReplyUsesReplyToNormalizesSubjectThreadsAndQuotesPlainText()
    {
        EmailMessage original = ParseMessage(
            "From: Alice <alice@example.test>\r\n"
            + "Reply-To: Support <support@example.test>\r\n"
            + "To: Me <me@example.test>\r\n"
            + "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Message-ID: <parent@example.test>\r\n"
            + "References: <root@example.test> <previous@example.test>\r\n"
            + "Subject: Re[2]: RE: =?UTF-8?Q?Caf=C3=A9?=\r\n"
            + "Content-Type: text/plain; charset=utf-8\r\n"
            + "Content-Transfer-Encoding: quoted-printable\r\n\r\n"
            + "First line\r\n\r\nLast line");
        EmailReplyOptions options = RelaxedOptions();
        options.SetReplyText("Thanks");

        EmailComposer composer = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("Me <me@example.test>"), options);
        EmailMessage reply = composer.Build();

        Assert.That(Mailboxes(reply, "To"), Is.EqualTo(new[] { "support@example.test" }));
        Assert.That(Mailboxes(reply, "Cc"), Is.Empty);
        Assert.That(reply.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Re: Café"));
        Assert.That(HeaderText(reply, "In-Reply-To"), Is.EqualTo("<parent@example.test>"));
        Assert.That(HeaderText(reply, "References"), Is.EqualTo("<root@example.test> <previous@example.test> <parent@example.test>"));
        Assert.That(BodyText(reply), Is.EqualTo("Thanks\r\n\r\nOn Fri, 21 Nov 1997 09:55:06 -0600, Alice wrote:\r\n> First line\r\n>\r\n> Last line"));
        Assert.That(composer.Validate().IsValid(), Is.True);
    }

    [Test]
    public void ReplyAllExcludesAuthorAliasesAndDeduplicatesToAndCc()
    {
        EmailMessage original = ParseMessage(
            "From: Alice <alice@example.test>\r\n"
            + "Reply-To: Team <team@example.test>\r\n"
            + "To: Me <me@example.test>, Bob <bob@example.test>, Alias <alias@example.test>\r\n"
            + "Cc: Carol <carol@example.test>, BOB <BOB@example.test>, Me <ME@example.test>\r\n"
            + "Subject: Topic\r\n\r\nbody");
        EmailReplyOptions options = RelaxedOptions();
        options.SetQuoteOriginal(false);
        options.SetReplyText("reply all");
        options.AddIdentity(Address("alias@example.test"));

        EmailMessage reply = new EmailReplyBuilder().CreateReplyAllWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), options).Build();

        Assert.That(Mailboxes(reply, "To"), Is.EqualTo(new[] { "team@example.test", "bob@example.test" }));
        Assert.That(Mailboxes(reply, "Cc"), Is.EqualTo(new[] { "carol@example.test" }));
        Assert.That(BodyText(reply), Is.EqualTo("reply all"));
    }

    [Test]
    public void ReplyFallsBackToFromAndRejectsMessagesWithoutAnotherRecipient()
    {
        EmailReplyOptions options = RelaxedOptions();
        options.SetQuoteOriginal(false);
        EmailMessage fromOnly = ParseMessage("From: Alice <alice@example.test>\r\nSubject: hi\r\n\r\nbody");

        EmailMessage reply = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(fromOnly, Address("me@example.test"), options).Build();
        Assert.That(Mailboxes(reply, "To"), Is.EqualTo(new[] { "alice@example.test" }));

        EmailMessage selfOnly = ParseMessage("From: Me <me@example.test>\r\nSubject: note\r\n\r\nbody");
        Assert.Throws<InvalidOperationException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(selfOnly, Address("ME@example.test"), options));
        Assert.Throws<ArgumentNullException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(null!, Address("me@example.test"), options));
        Assert.Throws<ArgumentNullException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(fromOnly, null!, options));
        Assert.Throws<ArgumentNullException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(fromOnly, Address("me@example.test"), (EmailReplyOptions)null!));
    }

    [Test]
    public void SubjectNormalizationCollapsesReplyCountersAndUnsafeWhitespace()
    {
        Assert.That(new EmailReplyBuilder().NormalizeSubject("  RE: Re[12]: Topic  "), Is.EqualTo("Re: Topic"));
        Assert.That(new EmailReplyBuilder().NormalizeSubject("Topic"), Is.EqualTo("Re: Topic"));
        Assert.That(new EmailReplyBuilder().NormalizeSubject("Re:"), Is.EqualTo("Re:"));
        Assert.That(new EmailReplyBuilder().NormalizeSubject("Re:\r\nInjected"), Is.EqualTo("Re: Injected"));
        Assert.Throws<ArgumentNullException>(() => new EmailReplyBuilder().NormalizeSubject(null));
    }

    [Test]
    public void ReferenceLimitKeepsRootAndNewestIdentifiersWithParentLast()
    {
        EmailMessage original = ParseMessage(
            "From: alice@example.test\r\n"
            + "Message-ID: <parent@example.test>\r\n"
            + "References: <root@example.test> <one@example.test> <two@example.test> <three@example.test>\r\n"
            + "Subject: topic\r\n\r\nbody");
        EmailReplyOptions options = RelaxedOptions();
        options.SetQuoteOriginal(false);
        options.SetMaximumReferenceCount(3);

        EmailMessage reply = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), options).Build();

        Assert.That(HeaderText(reply, "In-Reply-To"), Is.EqualTo("<parent@example.test>"));
        Assert.That(HeaderText(reply, "References"), Is.EqualTo("<root@example.test> <three@example.test> <parent@example.test>"));
    }

    [Test]
    public void HtmlReplyQuotesSanitizedHtmlAndCreatesPlainAlternative()
    {
        EmailMessage original = ParseMessage(
            "From: Alice <alice@example.test>\r\n"
            + "Subject: HTML\r\n"
            + "Content-Type: multipart/alternative; boundary=a\r\n\r\n"
            + "--a\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nPlain original\r\n"
            + "--a\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<script>bad()</script><p>Hello <b>world</b></p><img src=\"https://tracker.test/pixel\">\r\n"
            + "--a--\r\n");
        EmailReplyOptions options = RelaxedOptions();
        options.SetReplyText("Plain reply");
        options.SetReplyHtml("<p>HTML reply</p>");

        EmailMessage reply = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), options).Build();
        MimeEntity body = reply.GetBody()!;
        MimeEntity plain = new EmailBodySelector().GetPlainTextWithMessage(reply)!;
        MimeEntity html = new EmailBodySelector().GetHtmlWithMessage(reply)!;
        string htmlText = new EmailTextProcessor().DecodeWithEntity(html!).GetText();

        Assert.That(body!.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(new EmailTextProcessor().DecodeWithEntity(plain!).GetText(), Does.Contain("> Plain original"));
        Assert.That(htmlText, Does.Contain("<blockquote type=\"cite\">") );
        Assert.That(htmlText, Does.Contain("Hello <b>world</b>"));
        Assert.That(htmlText, Does.Not.Contain("<script"));
        Assert.That(htmlText, Does.Not.Contain("https://tracker.test"));
    }

    [Test]
    public void QuoteLimitAndDisabledQuotingAreEnforced()
    {
        EmailMessage original = ParseMessage("From: alice@example.test\r\nSubject: test\r\n\r\nfour");
        EmailReplyOptions limited = RelaxedOptions();
        limited.SetMaximumQuotedCharacters(3);
        Assert.Throws<EmailLimitException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), limited));

        EmailReplyOptions disabled = RelaxedOptions();
        disabled.SetMaximumQuotedCharacters(1);
        disabled.SetQuoteOriginal(false);
        disabled.SetReplyText("unquoted");
        Assert.That(BodyText(new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), disabled).Build()), Is.EqualTo("unquoted"));
    }

    [Test]
    public void AttachmentPoliciesDecodeRegularAttachmentsAndOptionallyKeepInlineResources()
    {
        EmailMessage original = AttachmentMessage();
        EmailReplyOptions none = RelaxedOptions();
        none.SetQuoteOriginal(false);
        EmailMessage without = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), none).Build();
        Assert.That(new EmailAttachmentFinder().FindWithMessage(without), Is.Empty);
        Assert.That(new EmailAttachmentFinder().GetInlineResourcesWithMessage(without), Is.Empty);

        EmailReplyOptions regular = RelaxedOptions();
        regular.SetQuoteOriginal(false);
        regular.SetAttachmentPolicy(EmailReplyAttachmentPolicy.Attachments);
        EmailMessage withAttachment = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), regular).Build();
        List<EmailAttachmentView> attachments = new EmailAttachmentFinder().FindWithMessage(withAttachment);
        Assert.That(attachments.Count, Is.EqualTo(1));
        Assert.That(attachments[0].GetFilename(), Is.EqualTo("note.txt"));
        Assert.That(Encoding.ASCII.GetString(DecodedBytes(attachments[0].GetEntity())), Is.EqualTo("A B"));
        Assert.That(new EmailAttachmentFinder().GetInlineResourcesWithMessage(withAttachment), Is.Empty);

        EmailReplyOptions all = RelaxedOptions();
        all.SetQuoteOriginal(false);
        all.SetAttachmentPolicy(EmailReplyAttachmentPolicy.AttachmentsAndInlineResources);
        EmailMessage withAll = new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), all).Build();
        List<EmailAttachmentView> resources = new EmailAttachmentFinder().GetInlineResourcesWithMessage(withAll);
        Assert.That(new EmailAttachmentFinder().FindWithMessage(withAll).Count, Is.EqualTo(1));
        Assert.That(resources.Count, Is.EqualTo(1));
        Assert.That(resources[0].GetFilename(), Is.EqualTo("logo.png"));
        Assert.That(resources[0].GetContentId()!.GetIdLeft(), Is.EqualTo("logo"));
    }

    [Test]
    public void AttachmentCountAndDecodedByteLimitsFailBeforeReturningAComposer()
    {
        EmailMessage original = AttachmentMessage();
        EmailReplyOptions countLimited = RelaxedOptions();
        countLimited.SetQuoteOriginal(false);
        countLimited.SetAttachmentPolicy(EmailReplyAttachmentPolicy.AttachmentsAndInlineResources);
        countLimited.SetMaximumAttachmentCount(1);
        Assert.Throws<EmailLimitException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), countLimited));

        EmailReplyOptions byteLimited = RelaxedOptions();
        byteLimited.SetQuoteOriginal(false);
        byteLimited.SetAttachmentPolicy(EmailReplyAttachmentPolicy.Attachments);
        byteLimited.SetMaximumAttachmentBytes(2);
        Assert.Throws<EmailLimitException>(() => new EmailReplyBuilder().CreateReplyWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), byteLimited));
    }

    [Test]
    public void OptionsCloneNestedComposerSettingsAndValidateInputs()
    {
        EmailReplyOptions options = new EmailReplyOptions();
        EmailComposerOptions composerOptions = new EmailComposerOptions();
        composerOptions.SetAllowSmtpUtf8(true);
        options.SetComposerOptions(composerOptions);
        options.SetReplyText("reply");
        options.SetReplyHtml("html");
        options.AddIdentity(Address("alias@example.test"));
        EmailReplyOptions clone = options.Clone();
        composerOptions.SetAllowSmtpUtf8(false);
        options.ClearReplyHtml();
        options.ClearIdentities();

        Assert.That(clone.GetComposerOptions().GetAllowSmtpUtf8(), Is.True);
        Assert.That(clone.GetReplyHtml(), Is.EqualTo("html"));
        Assert.That(clone.GetIdentityCount(), Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetAttachmentPolicy((EmailReplyAttachmentPolicy)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumReferenceCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumQuotedCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAttachmentCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAttachmentBytes(0));
        Assert.Throws<ArgumentNullException>(() => options.SetReplyText(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetReplyHtml(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetComposerOptions(null!));
        Assert.Throws<ArgumentNullException>(() => options.AddIdentity(null!));
    }

    static EmailMessage AttachmentMessage()
    {
        return ParseMessage(
            "From: alice@example.test\r\n"
            + "Subject: files\r\n"
            + "MIME-Version: 1.0\r\n"
            + "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: multipart/related; boundary=r\r\n\r\n"
            + "--r\r\nContent-Type: text/html; charset=us-ascii\r\n\r\n<img src=\"cid:logo@example.test\">\r\n"
            + "--r\r\nContent-Type: image/png\r\nContent-ID: <logo@example.test>\r\nContent-Disposition: inline; filename=logo.png\r\n\r\nPNG\r\n"
            + "--r--\r\n"
            + "--m\r\nContent-Type: text/plain\r\nContent-Transfer-Encoding: quoted-printable\r\nContent-Disposition: attachment; filename=note.txt\r\n\r\nA=20B\r\n"
            + "--m--\r\n");
    }

    static EmailReplyOptions RelaxedOptions()
    {
        EmailComposerOptions composerOptions = new EmailComposerOptions();
        composerOptions.SetRequireDate(false);
        composerOptions.SetRequireMessageId(false);
        EmailReplyOptions options = new EmailReplyOptions();
        options.SetComposerOptions(composerOptions);
        return options;
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static EmailAddress Address(string source)
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(source), EmailAddressParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMailbox()!;
    }

    static List<string> Mailboxes(EmailMessage message, string name)
    {
        List<string> values = new List<string>();
        EmailHeader header = message.GetHeaders()!.GetFirst(name)!;
        if (header == null)
        {
            return values;
        }

        EmailAddressList list = new EmailAddressParser().ParseListWithInputAndMode(header.GetUnfoldedValue(), EmailAddressParseMode.Strict).GetAddressList();
        int index = 0;
        while (index < list.GetCount())
        {
            EmailAddress mailbox = list.GetAt(index).GetMailbox()!;
            values.Add(mailbox!.GetLocalPart().ToLowerInvariant() + "@" + mailbox.GetDomain().ToLowerInvariant());
            index++;
        }

        return values;
    }

    static string? HeaderText(EmailMessage message, string name)
    {
        EmailHeader header = message.GetHeaders()!.GetFirst(name)!;
        return header == null ? null : Encoding.ASCII.GetString(header.GetUnfoldedValue()).Trim()!;
    }

    static string BodyText(EmailMessage message)
    {
        return new EmailTextProcessor().DecodeWithEntity(new EmailBodySelector().GetPlainTextWithMessage(message)!).GetText();
    }

    static byte[] DecodedBytes(MimeEntity entity)
    {
        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutput(entity, output);
            return output.ToArray();
        }
    }
}
