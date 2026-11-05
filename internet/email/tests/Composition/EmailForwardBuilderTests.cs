using System.Text;
using email;

namespace email.Tests;

public class EmailForwardBuilderTests
{
    [Test]
    public void InlineForwardUsesSafeSummaryHeadersNormalizedSubjectAndDecodedBody()
    {
        EmailMessage original = ParseMessage(
            "Return-Path: <bounce@example.test>\r\n"
            + "Received: from private.example.test by mx.example.test\r\n"
            + "From: Alice <alice@example.test>\r\n"
            + "To: Bob <bob@example.test>\r\n"
            + "Cc: Carol <carol@example.test>\r\n"
            + "Bcc: Hidden <hidden@example.test>\r\n"
            + "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Message-ID: <message@example.test>\r\n"
            + "Authentication-Results: mx.example.test; spf=pass\r\n"
            + "X-Internal: secret\r\n"
            + "Subject: Fwd[3]: FW: Plan\r\n"
            + "Content-Type: text/plain; charset=us-ascii\r\n"
            + "Content-Transfer-Encoding: quoted-printable\r\n\r\n"
            + "A=20B");
        EmailForwardOptions options = RelaxedOptions();
        options.SetForwardText("FYI");

        EmailComposer composer = new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), options);
        EmailMessage forward = composer.Build();
        string text = BodyText(forward);

        Assert.That(forward.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Fwd: Plan"));
        Assert.That(forward.GetHeaders()!.Contains("To"), Is.False);
        Assert.That(text, Does.StartWith("FYI\r\n\r\n---------- Forwarded message ----------\r\n"));
        Assert.That(text, Does.Contain("From: Alice <alice@example.test>"));
        Assert.That(text, Does.Contain("To: Bob <bob@example.test>"));
        Assert.That(text, Does.Contain("Cc: Carol <carol@example.test>"));
        Assert.That(text, Does.Contain("Message-ID: <message@example.test>"));
        Assert.That(text, Does.EndWith("\r\n\r\nA B"));
        Assert.That(text, Does.Not.Contain("Bcc:"));
        Assert.That(text, Does.Not.Contain("Received:"));
        Assert.That(text, Does.Not.Contain("Authentication-Results:"));
        Assert.That(text, Does.Not.Contain("X-Internal:"));
        Assert.That(composer.Validate().IsValid(), Is.True);
    }

    [Test]
    public void InlineHeaderAndBodyPoliciesControlDisclosureIndependently()
    {
        EmailMessage original = ParseMessage(
            "From: alice@example.test\r\n"
            + "To: bob@example.test\r\n"
            + "Bcc: hidden@example.test\r\n"
            + "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Subject: topic\r\n"
            + "X-Custom: retained only explicitly\r\n\r\n"
            + "original body");

        EmailForwardOptions minimal = RelaxedOptions();
        minimal.SetHeaderPolicy(EmailForwardHeaderPolicy.Minimal);
        minimal.SetBodyPolicy(EmailForwardBodyPolicy.HeadersOnly);
        string minimalText = BodyText(new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), minimal).Build());
        Assert.That(minimalText, Does.Contain("From: alice@example.test"));
        Assert.That(minimalText, Does.Contain("Date:"));
        Assert.That(minimalText, Does.Contain("Subject: topic"));
        Assert.That(minimalText, Does.Not.Contain("To:"));
        Assert.That(minimalText, Does.Not.Contain("original body"));

        EmailForwardOptions omitted = RelaxedOptions();
        omitted.SetForwardText("intro only");
        omitted.SetBodyPolicy(EmailForwardBodyPolicy.OmitOriginal);
        Assert.That(BodyText(new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), omitted).Build()), Is.EqualTo("intro only"));

        EmailForwardOptions preserved = RelaxedOptions();
        preserved.SetHeaderPolicy(EmailForwardHeaderPolicy.Preserve);
        preserved.SetBodyPolicy(EmailForwardBodyPolicy.HeadersOnly);
        string preservedText = BodyText(new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), preserved).Build());
        Assert.That(preservedText, Does.Contain("Bcc: hidden@example.test"));
        Assert.That(preservedText, Does.Contain("X-Custom: retained only explicitly"));
    }

    [Test]
    public void InlineHtmlIsSanitizedAndAttachmentPolicyCarriesDecodedFilesAndResources()
    {
        EmailMessage original = AttachmentMessage();
        EmailForwardOptions options = RelaxedOptions();
        options.SetForwardText("plain intro");
        options.SetForwardHtml("<p>HTML intro</p>");
        options.SetAttachmentPolicy(EmailForwardAttachmentPolicy.AttachmentsAndInlineResources);

        EmailMessage forward = new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), options).Build();
        MimeEntity html = new EmailBodySelector().GetHtmlWithMessage(forward)!;
        string htmlText = new EmailTextProcessor().DecodeWithEntity(html!).GetText();
        List<EmailAttachmentView> attachments = new EmailAttachmentFinder().FindWithMessage(forward);
        List<EmailAttachmentView> resources = new EmailAttachmentFinder().GetInlineResourcesWithMessage(forward);

        Assert.That(htmlText, Does.Contain("HTML intro"));
        Assert.That(htmlText, Does.Contain("forwarded-body"));
        Assert.That(htmlText, Does.Contain("Hello <b>world</b>"));
        Assert.That(htmlText, Does.Not.Contain("<script"));
        Assert.That(htmlText, Does.Not.Contain("https://tracker.test"));
        Assert.That(attachments.Count, Is.EqualTo(1));
        Assert.That(attachments[0].GetFilename(), Is.EqualTo("note.txt"));
        Assert.That(Encoding.ASCII.GetString(DecodedBytes(attachments[0].GetEntity())), Is.EqualTo("A B"));
        Assert.That(resources.Count, Is.EqualTo(1));
        Assert.That(resources[0].GetFilename(), Is.EqualTo("logo.png"));

        EmailForwardOptions noAttachments = RelaxedOptions();
        EmailMessage without = new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), noAttachments).Build();
        Assert.That(new EmailAttachmentFinder().FindWithMessage(without), Is.Empty);
        Assert.That(new EmailAttachmentFinder().GetInlineResourcesWithMessage(without), Is.Empty);
    }

    [Test]
    public void InlineLimitsBoundHeadersBodiesAndAttachments()
    {
        EmailMessage simple = ParseMessage("From: alice@example.test\r\nSubject: topic\r\n\r\nfour");
        EmailForwardOptions bodyLimited = RelaxedOptions();
        bodyLimited.SetMaximumBodyCharacters(3);
        Assert.Throws<EmailLimitException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(simple, Address("me@example.test"), bodyLimited));

        EmailForwardOptions headerLimited = RelaxedOptions();
        headerLimited.SetBodyPolicy(EmailForwardBodyPolicy.HeadersOnly);
        headerLimited.SetMaximumHeaderCount(1);
        Assert.Throws<EmailLimitException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(simple, Address("me@example.test"), headerLimited));

        EmailMessage attachments = AttachmentMessage();
        EmailForwardOptions countLimited = RelaxedOptions();
        countLimited.SetForwardHtml("html");
        countLimited.SetAttachmentPolicy(EmailForwardAttachmentPolicy.AttachmentsAndInlineResources);
        countLimited.SetMaximumAttachmentCount(1);
        Assert.Throws<EmailLimitException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(attachments, Address("me@example.test"), countLimited));

        EmailForwardOptions byteLimited = RelaxedOptions();
        byteLimited.SetAttachmentPolicy(EmailForwardAttachmentPolicy.Attachments);
        byteLimited.SetMaximumAttachmentBytes(2);
        Assert.Throws<EmailLimitException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(attachments, Address("me@example.test"), byteLimited));
    }

    [Test]
    public void AttachedForwardBuildsNamedMessageRfc822WithSafeClonedHeaders()
    {
        EmailMessage original = AttachmentMessage();
        original.GetHeaders()!.Insert(0, new EmailHeaderOperations().CreateWithNameAndRawValue("Return-Path", Encoding.ASCII.GetBytes("<bounce@example.test>")));
        original.GetHeaders()!.Insert(1, new EmailHeaderOperations().CreateWithNameAndRawValue("Received", Encoding.ASCII.GetBytes("from private.example.test")));
        original.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Bcc", Encoding.ASCII.GetBytes("hidden@example.test"), EmailHeaderValueKind.AddressList));
        original.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Authentication-Results", Encoding.ASCII.GetBytes("mx.example.test; spf=pass")));
        original.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("X-Keep", Encoding.ASCII.GetBytes("custom")));
        EmailForwardOptions options = RelaxedOptions();
        options.SetForwardText("See attached");
        options.SetAttachedFilename("../forwarded.eml");

        EmailMessage forward = new EmailForwardBuilder().CreateAttachedMessageWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), options).Build();
        MimeEntity root = forward.GetBody()!;
        MimeEntity embedded = root!.GetChildAt(1);
        EmailMessage nested = embedded.GetNestedMessage()!;

        Assert.That(root.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(Boundary(root), Is.Not.Empty);
        Assert.That(BodyEntityText(root.GetChildAt(0)), Is.EqualTo("See attached"));
        Assert.That(embedded.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(embedded.GetMediaType()!.IsType("message", "rfc822"), Is.True);
        Assert.That(embedded.GetDisposition()!.IsAttachment(), Is.True);
        Assert.That(new EmailFilename().Decode(embedded), Is.EqualTo("forwarded.eml"));
        Assert.That(nested!.GetHeaders()!.Contains("Return-Path"), Is.False);
        Assert.That(nested.GetHeaders()!.Contains("Received"), Is.False);
        Assert.That(nested.GetHeaders()!.Contains("Bcc"), Is.False);
        Assert.That(nested.GetHeaders()!.Contains("Authentication-Results"), Is.False);
        Assert.That(nested.GetHeaders()!.Contains("X-Keep"), Is.True);
        Assert.That(nested.GetBody(), Is.Not.SameAs(original.GetBody()));
        Assert.That(new EmailAttachmentFinder().FindWithMessage(nested).Count, Is.EqualTo(1));
        Assert.That(nested.GetEnvelope()!.GetRecipientCount(), Is.Zero);
        Assert.That(original.GetHeaders()!.Contains("Bcc"), Is.True);

        string serialized = Encoding.ASCII.GetString(new EmailWriter().ToArrayWithMessageAndOptions(forward, new EmailWriteOptionsOperations().CreateCanonicalInternet()));
        Assert.That(serialized, Does.Contain("Content-Type: message/rfc822"));
    }

    [Test]
    public void AttachedHeaderPoliciesSupportMinimalAndExplicitPreservation()
    {
        EmailMessage original = ParseMessage(
            "Return-Path: <bounce@example.test>\r\n"
            + "From: alice@example.test\r\n"
            + "To: bob@example.test\r\n"
            + "Bcc: hidden@example.test\r\n"
            + "Subject: topic\r\n"
            + "X-Custom: custom\r\n\r\nbody");
        EmailForwardOptions minimal = RelaxedOptions();
        minimal.SetHeaderPolicy(EmailForwardHeaderPolicy.Minimal);
        EmailHeaders minimalHeaders = EmbeddedHeaders(new EmailForwardBuilder().CreateAttachedMessageWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), minimal).Build());
        Assert.That(minimalHeaders.Contains("From"), Is.True);
        Assert.That(minimalHeaders.Contains("To"), Is.True);
        Assert.That(minimalHeaders.Contains("Subject"), Is.True);
        Assert.That(minimalHeaders.Contains("Return-Path"), Is.False);
        Assert.That(minimalHeaders.Contains("X-Custom"), Is.False);

        EmailForwardOptions preserve = RelaxedOptions();
        preserve.SetHeaderPolicy(EmailForwardHeaderPolicy.Preserve);
        EmailHeaders preservedHeaders = EmbeddedHeaders(new EmailForwardBuilder().CreateAttachedMessageWithOriginalAndAuthorAndOptions(original, Address("me@example.test"), preserve).Build());
        Assert.That(preservedHeaders.Contains("Return-Path"), Is.True);
        Assert.That(preservedHeaders.Contains("Bcc"), Is.True);
        Assert.That(preservedHeaders.Contains("X-Custom"), Is.True);
    }

    [Test]
    public void SubjectNormalizationCollapsesForwardPrefixes()
    {
        Assert.That(new EmailForwardBuilder().NormalizeSubject(" FWD: Fw[9]: Topic "), Is.EqualTo("Fwd: Topic"));
        Assert.That(new EmailForwardBuilder().NormalizeSubject("Re: Topic"), Is.EqualTo("Fwd: Re: Topic"));
        Assert.That(new EmailForwardBuilder().NormalizeSubject("Fwd:"), Is.EqualTo("Fwd:"));
        Assert.That(new EmailForwardBuilder().NormalizeSubject("Fw:\r\nInjected"), Is.EqualTo("Fwd: Injected"));
        Assert.Throws<ArgumentNullException>(() => new EmailForwardBuilder().NormalizeSubject(null));
    }

    [Test]
    public void OptionsCloneNestedSettingsAndValidateInputs()
    {
        EmailForwardOptions options = new EmailForwardOptions();
        EmailComposerOptions composerOptions = new EmailComposerOptions();
        composerOptions.SetAllowSmtpUtf8(true);
        options.SetComposerOptions(composerOptions);
        options.SetForwardText("text");
        options.SetForwardHtml("html");
        options.SetAttachedFilename("../message.eml");
        EmailForwardOptions clone = options.Clone();
        composerOptions.SetAllowSmtpUtf8(false);
        options.ClearForwardHtml();

        Assert.That(clone.GetComposerOptions().GetAllowSmtpUtf8(), Is.True);
        Assert.That(clone.GetForwardHtml(), Is.EqualTo("html"));
        Assert.That(clone.GetAttachedFilename(), Is.EqualTo("message.eml"));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetHeaderPolicy((EmailForwardHeaderPolicy)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetBodyPolicy((EmailForwardBodyPolicy)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetAttachmentPolicy((EmailForwardAttachmentPolicy)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumHeaderCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumHeaderCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumBodyCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAttachmentCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAttachmentBytes(0));
        Assert.Throws<ArgumentNullException>(() => options.SetForwardText(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetForwardHtml(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetAttachedFilename(null!));
        Assert.Throws<ArgumentException>(() => options.SetAttachedFilename(""));
        Assert.Throws<ArgumentNullException>(() => options.SetComposerOptions(null!));
    }

    [Test]
    public void BuildersRejectNullInputs()
    {
        EmailMessage original = ParseMessage("From: alice@example.test\r\n\r\nbody");
        EmailAddress author = Address("me@example.test");
        EmailForwardOptions options = RelaxedOptions();
        Assert.Throws<ArgumentNullException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(null!, author, options));
        Assert.Throws<ArgumentNullException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, null!, options));
        Assert.Throws<ArgumentNullException>(() => new EmailForwardBuilder().CreateInlineWithOriginalAndAuthorAndOptions(original, author, (EmailForwardOptions)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailForwardBuilder().CreateAttachedMessageWithOriginalAndAuthorAndOptions(null!, author, options));
    }

    static EmailMessage AttachmentMessage()
    {
        return ParseMessage(
            "From: alice@example.test\r\n"
            + "To: bob@example.test\r\n"
            + "Subject: files\r\n"
            + "MIME-Version: 1.0\r\n"
            + "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: multipart/related; boundary=r\r\n\r\n"
            + "--r\r\nContent-Type: text/html; charset=us-ascii\r\n\r\n<script>bad()</script><p>Hello <b>world</b></p><img src=\"cid:logo@example.test\"><img src=\"https://tracker.test/pixel\">\r\n"
            + "--r\r\nContent-Type: image/png\r\nContent-ID: <logo@example.test>\r\nContent-Disposition: inline; filename=logo.png\r\n\r\nPNG\r\n"
            + "--r--\r\n"
            + "--m\r\nContent-Type: text/plain\r\nContent-Transfer-Encoding: quoted-printable\r\nContent-Disposition: attachment; filename=note.txt\r\n\r\nA=20B\r\n"
            + "--m--\r\n");
    }

    static EmailForwardOptions RelaxedOptions()
    {
        EmailComposerOptions composerOptions = new EmailComposerOptions();
        composerOptions.SetRequireDate(false);
        composerOptions.SetRequireMessageId(false);
        composerOptions.SetRequireRecipient(false);
        EmailForwardOptions options = new EmailForwardOptions();
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

    static string BodyText(EmailMessage message)
    {
        return new EmailTextProcessor().DecodeWithEntity(new EmailBodySelector().GetPlainTextWithMessage(message)!).GetText();
    }

    static string BodyEntityText(MimeEntity entity)
    {
        return new EmailTextProcessor().DecodeWithEntity(entity).GetText();
    }

    static byte[] DecodedBytes(MimeEntity entity)
    {
        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutput(entity, output);
            return output.ToArray();
        }
    }

    static string? Boundary(MimeEntity entity)
    {
        EmailRfc2231DecodedParameter boundary = entity.GetMediaType()!.GetParameters().GetDecoded("boundary")!;
        return boundary == null ? null : Encoding.ASCII.GetString(boundary.GetDecodedBytes())!;
    }

    static EmailHeaders EmbeddedHeaders(EmailMessage forward)
    {
        return forward.GetBody()!.GetChildAt(1).GetNestedMessage()!.GetHeaders()!;
    }
}
