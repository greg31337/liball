using System.Text;
using email;

namespace email.Tests;

public class EmailRedactorTests
{
    [Test]
    public void DefaultsAreOptInAndReturnAnIndependentExactClone()
    {
        EmailMessage original = ParseMessage("From: Alice <alice@example.test>\nSubject: secret\n\nbody\n");
        byte[] before = new EmailWriter().ToArrayWithMessageAndOptions(original, new EmailWriteOptionsOperations().CreateExactPreservation());

        EmailRedactionResult result = new EmailRedactor().RedactWithMessage(original);
        byte[] after = new EmailWriter().ToArrayWithMessageAndOptions(result.GetMessage()!, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.HasMessage(), Is.True);
        Assert.That(result.HasEntity(), Is.False);
        Assert.That(result.HasChanges(), Is.False);
        Assert.That(result.GetMessage(), Is.Not.SameAs(original));
        Assert.That(after, Is.EqualTo(before));
        Assert.That(original.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void AddressesIdsTraceAuthenticationSubjectsAndIpsAreRedactedConsistently()
    {
        EmailMessage original = ParseMessage(
            "From: Alice Person <alice@example.test>\r\n"
            + "To: Alice Person <alice@example.test>, Bob <bob@example.test>\r\n"
            + "Message-ID: <original@example.test>\r\n"
            + "References: <parent@example.test> <original@example.test>\r\n"
            + "Subject: acquisition secret\r\n"
            + "Received: from host.example ([192.0.2.9]) by mx.example; Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Authentication-Results: mx.example; dkim=pass header.d=example.test\r\n"
            + "X-Originating-IP: [IPv6:2001:db8::1] 198.51.100.7\r\n"
            + "Content-Type: text/plain\r\n"
            + "Content-ID: <content@example.test>\r\n\r\nbody");
        EmailAddress alice = new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes("alice@example.test"), EmailAddressParseMode.Strict).GetMailbox()!;
        EmailAddress bob = new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes("bob@example.test"), EmailAddressParseMode.Strict).GetMailbox()!;
        original.GetEnvelope()!.SetSender(alice);
        original.GetEnvelope()!.AddRecipient(alice);
        original.GetEnvelope()!.AddRecipient(bob);
        original.GetEnvelope()!.SetMboxFromLine(Encoding.ASCII.GetBytes("From alice@example.test Fri Nov 21 09:55:06 1997"));
        EmailRedactionOptions options = new EmailRedactionOptions();
        options.SetRedactAddresses(true);
        options.SetRedactMessageIds(true);
        options.SetRemoveReceivedHeaders(true);
        options.SetRemoveAuthenticationHeaders(true);
        options.SetRedactIpAddresses(true);
        options.SetRedactSubjects(true);

        EmailRedactionResult result = new EmailRedactor().RedactWithMessageAndOptions(original, options);
        EmailMessage redacted = result.GetMessage()!;
        string from = GetHeaderValue(redacted!.GetHeaders()!, "From");
        string to = GetHeaderValue(redacted.GetHeaders()!, "To");
        string messageId = GetHeaderValue(redacted.GetHeaders()!, "Message-ID");
        string references = GetHeaderValue(redacted.GetHeaders()!, "References");
        string contentId = GetHeaderValue(redacted.GetHeaders()!, "Content-ID");

        Assert.That(from, Is.EqualTo("user1@redacted.invalid"));
        Assert.That(to, Is.EqualTo("user1@redacted.invalid, user2@redacted.invalid"));
        Assert.That(redacted.GetEnvelope()!.GetSender()!.GetLocalPart(), Is.EqualTo("user1"));
        Assert.That(redacted.GetEnvelope()!.GetRecipientAt(0).GetLocalPart(), Is.EqualTo("user1"));
        Assert.That(redacted.GetEnvelope()!.HasMboxFromLine(), Is.False);
        Assert.That(messageId, Is.EqualTo("<id1@redacted.invalid>"));
        Assert.That(references, Is.EqualTo("<id2@redacted.invalid> <id1@redacted.invalid>"));
        Assert.That(contentId, Is.EqualTo("<id3@redacted.invalid>"));
        Assert.That(redacted.GetBody()!.GetContentId()!.GetIdLeft(), Is.EqualTo("id3"));
        Assert.That(redacted.GetHeaders()!.Contains("Received"), Is.False);
        Assert.That(redacted.GetHeaders()!.Contains("Authentication-Results"), Is.False);
        Assert.That(GetHeaderValue(redacted.GetHeaders()!, "Subject"), Is.EqualTo("[redacted]"));
        Assert.That(GetHeaderValue(redacted.GetHeaders()!, "X-Originating-IP"), Does.Not.Contain("2001:db8"));
        Assert.That(GetHeaderValue(redacted.GetHeaders()!, "X-Originating-IP"), Does.Not.Contain("198.51.100.7"));
        Assert.That(HasChange(result, EmailRedactionKind.Address), Is.True);
        Assert.That(HasChange(result, EmailRedactionKind.MessageId), Is.True);
        Assert.That(HasChange(result, EmailRedactionKind.Received), Is.True);
        Assert.That(HasChange(result, EmailRedactionKind.Authentication), Is.True);
        Assert.That(HasChange(result, EmailRedactionKind.IpAddress), Is.True);
        Assert.That(GetHeaderValue(original.GetHeaders()!, "From"), Does.Contain("alice@example.test"));
        Assert.That(original.GetHeaders()!.Contains("Received"), Is.True);
    }

    [Test]
    public void BodiesAndAttachmentNamesAndContentUseSeparatePolicies()
    {
        EmailMessage original = ParseMessage(
            "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nsecret body\r\n"
            + "--m\r\nContent-Type: application/pdf; name=\"quarterly-secret.pdf\"\r\n"
            + "Content-Disposition: attachment; filename=\"quarterly-secret.pdf\"\r\n"
            + "Content-Transfer-Encoding: base64\r\n\r\n"
            + "c2VjcmV0IGF0dGFjaG1lbnQ=\r\n"
            + "--m--\r\n");
        EmailRedactionOptions options = new EmailRedactionOptions();
        options.SetRedactBodies(true);
        options.SetRedactAttachmentNames(true);
        options.SetRedactAttachmentContent(true);

        EmailRedactionResult result = new EmailRedactor().RedactWithMessageAndOptions(original, options);
        MimeEntity body = result.GetMessage()!.GetBody()!.GetChildAt(0);
        MimeEntity attachment = result.GetMessage()!.GetBody()!.GetChildAt(1);

        Assert.That(Encoding.UTF8.GetString(body.GetDecodedContent()!.Materialize(100)), Is.EqualTo("[redacted body]\r\n"));
        Assert.That(body.GetTransferEncodingName(), Is.EqualTo("7bit"));
        Assert.That(new EmailFilename().Decode(attachment), Is.EqualTo("attachment-1"));
        Assert.That(attachment.HasDecodedContent(), Is.True);
        Assert.That(attachment.GetDecodedContent()!.Materialize(100), Is.Empty);
        Assert.That(HasChange(result, EmailRedactionKind.Body), Is.True);
        Assert.That(HasChange(result, EmailRedactionKind.AttachmentName), Is.True);
        Assert.That(HasChange(result, EmailRedactionKind.AttachmentContent), Is.True);
        Assert.That(Encoding.ASCII.GetString(original.GetBody()!.GetChildAt(0).GetContent()!.Materialize(100)), Does.Contain("secret body"));
        Assert.That(new EmailFilename().Decode(original.GetBody()!.GetChildAt(1)), Is.EqualTo("quarterly-secret.pdf"));
    }

    [Test]
    public void PoliciesRemainIndependent()
    {
        EmailMessage original = ParseMessage(
            "From: alice@example.test\r\n"
            + "Message-ID: <secret@example.test>\r\n"
            + "Received: from 192.0.2.1 by mx.example; Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Subject: private\r\n\r\nbody");
        EmailRedactionOptions options = new EmailRedactionOptions();
        options.SetRedactSubjects(true);

        EmailRedactionResult result = new EmailRedactor().RedactWithMessageAndOptions(original, options);

        Assert.That(GetHeaderValue(result.GetMessage()!.GetHeaders()!, "Subject"), Is.EqualTo("[redacted]"));
        Assert.That(GetHeaderValue(result.GetMessage()!.GetHeaders()!, "From"), Is.EqualTo("alice@example.test"));
        Assert.That(GetHeaderValue(result.GetMessage()!.GetHeaders()!, "Message-ID"), Is.EqualTo("<secret@example.test>"));
        Assert.That(result.GetMessage()!.GetHeaders()!.Contains("Received"), Is.True);
        Assert.That(result.GetChangeCount(), Is.EqualTo(1));
    }

    [Test]
    public void OptionsResultsArgumentsAndTraversalLimitsAreDefensive()
    {
        EmailRedactionOptions options = new EmailRedactionOptionsOperations().CreateAll();
        options.SetAttachmentContentReplacement(new byte[] { 1, 2, 3 });
        EmailRedactionOptions clone = options.Clone();
        byte[] returnedBytes = clone.GetAttachmentContentReplacement();
        returnedBytes[0] = 9;
        EmailRedactionResult empty = new EmailRedactor().RedactWithEntityAndOptions(new EmailEntityFactory().CreateTextWithValue("value"), new EmailRedactionOptions());
        List<EmailRedactionChange> returned = empty.GetChanges();
        returned.Add(new EmailRedactionChange(EmailRedactionKind.Body, null!, "body", 1));

        Assert.That(clone.GetAttachmentContentReplacement()[0], Is.EqualTo(1));
        Assert.That(empty.GetChangeCount(), Is.Zero);
        Assert.Throws<ArgumentNullException>(() => new EmailRedactor().RedactWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailRedactor().RedactWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailRedactor().RedactWithMessageAndOptions(new EmailMessageOperations().CreateWith(), null!));
        Assert.Throws<ArgumentException>(() => options.SetSubjectReplacement("bad\r\nInjected: value"));
        Assert.Throws<ArgumentNullException>(() => options.SetAttachmentContentReplacement(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.GetChangeAt(0));

        MimeEntity deep = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("child") });
        EmailRedactionOptions limited = new EmailRedactionOptions();
        limited.SetMaximumDepth(0);
        Assert.That(Assert.Throws<EmailLimitException>(() => new EmailRedactor().RedactWithEntityAndOptions(deep, limited)).GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static string GetHeaderValue(EmailHeaders headers, string name)
    {
        return Encoding.ASCII.GetString(headers.GetFirst(name)!.GetUnfoldedValue()).Trim();
    }

    static bool HasChange(EmailRedactionResult result, EmailRedactionKind kind)
    {
        for (int index = 0; index < result.GetChangeCount(); index++)
        {
            if (result.GetChangeAt(index).GetKind() == kind)
            {
                return true;
            }
        }

        return false;
    }
}
