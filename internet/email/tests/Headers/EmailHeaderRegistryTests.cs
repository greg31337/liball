using System.Text;
using email;

namespace email.Tests;

public class EmailHeaderRegistryTests
{
    [TestCase("Date", EmailHeaderValueKind.Date)]
    [TestCase("From", EmailHeaderValueKind.AddressList)]
    [TestCase("Sender", EmailHeaderValueKind.Mailbox)]
    [TestCase("Reply-To", EmailHeaderValueKind.AddressList)]
    [TestCase("To", EmailHeaderValueKind.AddressList)]
    [TestCase("Cc", EmailHeaderValueKind.AddressList)]
    [TestCase("Bcc", EmailHeaderValueKind.AddressList)]
    [TestCase("Message-ID", EmailHeaderValueKind.MessageId)]
    [TestCase("In-Reply-To", EmailHeaderValueKind.InReplyTo)]
    [TestCase("References", EmailHeaderValueKind.References)]
    [TestCase("Subject", EmailHeaderValueKind.Unstructured)]
    [TestCase("Comments", EmailHeaderValueKind.Unstructured)]
    [TestCase("Keywords", EmailHeaderValueKind.Keywords)]
    [TestCase("Received", EmailHeaderValueKind.Received)]
    [TestCase("Content-Type", EmailHeaderValueKind.MediaType)]
    [TestCase("Content-ID", EmailHeaderValueKind.ContentId)]
    [TestCase("Content-Description", EmailHeaderValueKind.Unstructured)]
    [TestCase("Content-Disposition", EmailHeaderValueKind.Disposition)]
    [TestCase("Content-Language", EmailHeaderValueKind.ContentLanguage)]
    [TestCase("Disposition-Notification-To", EmailHeaderValueKind.AddressList)]
    public void MapsCoreAndMimeFields(string name, EmailHeaderValueKind expected)
    {
        Assert.That(new EmailHeaderRegistry().GetValueKind(name), Is.EqualTo(expected));
        Assert.That(new EmailHeaderRegistry().Contains(name), Is.True);
    }

    [TestCase("Authentication-Results", EmailHeaderValueKind.AuthenticationResults)]
    [TestCase("Original-Authentication-Results", EmailHeaderValueKind.AuthenticationResults)]
    [TestCase("Received-SPF", EmailHeaderValueKind.ReceivedSpf)]
    [TestCase("DKIM-Signature", EmailHeaderValueKind.DkimSignature)]
    [TestCase("ARC-Message-Signature", EmailHeaderValueKind.ArcMessageSignature)]
    [TestCase("ARC-Seal", EmailHeaderValueKind.ArcSeal)]
    [TestCase("ARC-Authentication-Results", EmailHeaderValueKind.ArcAuthenticationResults)]
    [TestCase("Priority", EmailHeaderValueKind.Priority)]
    [TestCase("X-Priority", EmailHeaderValueKind.XPriority)]
    [TestCase("Importance", EmailHeaderValueKind.Importance)]
    [TestCase("Sensitivity", EmailHeaderValueKind.Sensitivity)]
    [TestCase("Auto-Submitted", EmailHeaderValueKind.AutoSubmitted)]
    [TestCase("List-Help", EmailHeaderValueKind.ListHelp)]
    [TestCase("List-Unsubscribe", EmailHeaderValueKind.ListUnsubscribe)]
    [TestCase("List-Subscribe", EmailHeaderValueKind.ListSubscribe)]
    [TestCase("List-Post", EmailHeaderValueKind.ListPost)]
    [TestCase("List-Owner", EmailHeaderValueKind.ListOwner)]
    [TestCase("List-Archive", EmailHeaderValueKind.ListArchive)]
    [TestCase("List-Unsubscribe-Post", EmailHeaderValueKind.ListUnsubscribePost)]
    [TestCase("Return-Path", EmailHeaderValueKind.ReturnPath)]
    [TestCase("MIME-Version", EmailHeaderValueKind.MimeVersion)]
    [TestCase("Content-Transfer-Encoding", EmailHeaderValueKind.ContentTransferEncoding)]
    [TestCase("Content-Location", EmailHeaderValueKind.ContentLocation)]
    [TestCase("Newsgroups", EmailHeaderValueKind.Newsgroups)]
    [TestCase("Followup-To", EmailHeaderValueKind.FollowupTo)]
    [TestCase("List-ID", EmailHeaderValueKind.ListId)]
    [TestCase("Delivered-To", EmailHeaderValueKind.Trace)]
    [TestCase("X-Original-To", EmailHeaderValueKind.Trace)]
    [TestCase("Envelope-To", EmailHeaderValueKind.Trace)]
    [TestCase("X-Envelope-To", EmailHeaderValueKind.Trace)]
    [TestCase("Errors-To", EmailHeaderValueKind.Trace)]
    [TestCase("Mail-Reply-To", EmailHeaderValueKind.Trace)]
    [TestCase("Mail-Followup-To", EmailHeaderValueKind.Trace)]
    [TestCase("X-Originating-IP", EmailHeaderValueKind.Trace)]
    public void MapsSecurityAutomationAndListFields(string name, EmailHeaderValueKind expected)
    {
        Assert.That(new EmailHeaderRegistry().GetValueKind(name), Is.EqualTo(expected));
    }

    [TestCase("Resent-Date")]
    [TestCase("Resent-From")]
    [TestCase("Resent-Sender")]
    [TestCase("Resent-To")]
    [TestCase("Resent-Cc")]
    [TestCase("Resent-Bcc")]
    [TestCase("Resent-Message-ID")]
    [TestCase("Resent-Reply-To")]
    public void MapsSupportedResentFieldsToTheResentFacade(string name)
    {
        Assert.That(new EmailHeaderRegistry().GetValueKind(name), Is.EqualTo(EmailHeaderValueKind.ResentField));
    }

    [Test]
    public void LookupIsAsciiCaseInsensitiveAndRejectsUnicodeNearMatches()
    {
        Assert.That(new EmailHeaderRegistry().GetValueKind("cOnTeNt-TyPe"), Is.EqualTo(EmailHeaderValueKind.MediaType));
        Assert.That(new EmailHeaderRegistry().Contains("list-UNSUBSCRIBE-post"), Is.True);
        Assert.That(new EmailHeaderRegistry().GetValueKind("SUBJECT"), Is.EqualTo(EmailHeaderValueKind.Unstructured));
        Assert.That(new EmailHeaderRegistry().GetValueKind("Subjſct"), Is.EqualTo(EmailHeaderValueKind.Unknown));
    }

    [TestCase("X-Custom")]
    [TestCase("")]
    [TestCase("Resent-X-Trace")]
    public void UnsupportedOrExtensionFieldsRemainUnknown(string name)
    {
        Assert.That(new EmailHeaderRegistry().Contains(name), Is.False);
        Assert.That(new EmailHeaderRegistry().GetValueKind(name), Is.EqualTo(EmailHeaderValueKind.Unknown));
    }

    [Test]
    public void RegistryEnumerationIsCompleteUniqueAndDefensive()
    {
        List<string> names = new EmailHeaderRegistry().GetNames();
        HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.That(names.Count, Is.EqualTo(new EmailHeaderRegistry().GetCount()));
        for (int index = 0; index < names.Count; index++)
        {
            Assert.That(new EmailHeaderRegistry().GetNameAt(index), Is.EqualTo(names[index]));
            Assert.That(new EmailHeaderRegistry().GetValueKindAt(index), Is.Not.EqualTo(EmailHeaderValueKind.Unknown));
            Assert.That(new EmailHeaderRegistry().GetValueKind(names[index]), Is.EqualTo(new EmailHeaderRegistry().GetValueKindAt(index)));
            Assert.That(unique.Add(names[index]), Is.True);
        }

        names.Clear();
        Assert.That(new EmailHeaderRegistry().GetCount(), Is.EqualTo(62));
        Assert.That(new EmailHeaderRegistry().GetNames(), Has.Count.EqualTo(62));
    }

    [Test]
    public void RegistryKindCanDriveLazyStructuredHeaderParsing()
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-Type", Encoding.ASCII.GetBytes(" text/plain; charset=utf-8"), new EmailHeaderRegistry().GetValueKind("content-type"));

        Assert.That(header.GetStructuredValue(), Is.TypeOf<EmailMediaType>());
        Assert.That(((EmailMediaType)header.GetStructuredValue()).IsType("text", "plain"), Is.True);
    }

    [Test]
    public void InvalidArgumentsAndIndexesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderRegistry().Contains(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderRegistry().GetValueKind(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHeaderRegistry().GetNameAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHeaderRegistry().GetValueKindAt(new EmailHeaderRegistry().GetCount()));
    }
}
