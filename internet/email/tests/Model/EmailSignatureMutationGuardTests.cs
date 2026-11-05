using System.Text;
using email;

namespace email.Tests;

public class EmailSignatureMutationGuardTests
{
    [Test]
    public void ProtectsMultipartSignedBytesAndLeavesClonesEditable()
    {
        MimeEntity signed = ParseSignedEntity();
        signed.EnableSignatureMutationGuard();
        MimeEntity content = signed.GetChildAt(0);
        MimeEntity clone = signed.Clone();
        EmailHeaders alias = new EmailHeadersOperations().CreateWith();
        alias.Add(content.GetHeaders()!.GetFirst("Content-Type")!);

        Assert.Multiple(() =>
        {
            Assert.That(signed.IsSignatureMutationGuardEnabled(), Is.True);
            Assert.That(content.IsSignatureMutationGuardEnabled(), Is.True);
            Assert.Throws<EmailSignatureMutationException>(() => content.GetHeaders()!.GetFirst("Content-Type")!.SetValue("text/html"));
            Assert.Throws<EmailSignatureMutationException>(() => alias.GetFirst("Content-Type")!.SetValue("text/html"));
            Assert.Throws<EmailSignatureMutationException>(() => content.ReplaceContent(new EmailContentOperations().CreateMemoryCopy(Bytes("changed"), false)));
            Assert.Throws<EmailSignatureMutationException>(() => signed.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("X-Changed", Bytes(" yes"))));
            Assert.That(clone.IsSignatureMutationGuardEnabled(), Is.False);
            Assert.DoesNotThrow(() => clone.GetChildAt(0).GetHeaders()!.GetFirst("Content-Type")!.SetValue("text/html"));
        });

        signed.DisableSignatureMutationGuard();
        Assert.DoesNotThrow(() => content.GetHeaders()!.GetFirst("Content-Type")!.SetValue("text/plain; charset=utf-8"));
    }

    [Test]
    public void ProtectsDkimMessageHeadersAndBodyButNotOuterMimeHeaders()
    {
        EmailMessage dkim = new EmailParser().ParseMessageWithInputUsingValueArray(Bytes(
            "DKIM-Signature: v=1; a=rsa-sha256; d=example.test; s=test; h=from:subject; bh=AA==; b=AA==\r\n" +
            "From: alice@example.test\r\nSubject: signed\r\n\r\nbody")).GetMessage()!;
        dkim!.EnableSignatureMutationGuard();

        EmailMessage mimeSigned = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWith(), ParseSignedEntity(), new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
        mimeSigned.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" outer"), EmailHeaderValueKind.Unstructured));
        mimeSigned.EnableSignatureMutationGuard();

        Assert.Multiple(() =>
        {
            Assert.That(dkim.IsSignatureMutationGuardEnabled(), Is.True);
            Assert.Throws<EmailSignatureMutationException>(() => dkim.GetHeaders()!.GetFirst("Subject")!.SetValue("changed"));
            Assert.Throws<EmailSignatureMutationException>(() => dkim.GetHeaders()!.RemoveFirst("DKIM-Signature"));
            Assert.Throws<EmailSignatureMutationException>(() => dkim.GetBody()!.ReplaceContent(new EmailContentOperations().CreateMemoryCopy(Bytes("changed"), false)));
            Assert.Throws<EmailSignatureMutationException>(() => dkim.SetBody(new EmailEntityFactory().CreateTextWithValue("replacement")));
            Assert.That(mimeSigned.IsSignatureMutationGuardEnabled(), Is.False);
            Assert.DoesNotThrow(() => mimeSigned.GetHeaders()!.GetFirst("Subject")!.SetValue("safe outer change"));
            Assert.Throws<EmailSignatureMutationException>(() => mimeSigned.GetBody()!.GetChildAt(0).ReplaceContent(new EmailContentOperations().CreateMemoryCopy(Bytes("changed"), false)));
        });
    }

    [Test]
    public void ProtectsOpaqueSmimeSignedData()
    {
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(
            "Content-Type: application/pkcs7-mime; smime-type=signed-data\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\nAA==\r\n"));
        MimeEntity opaque = parsed.GetEntity()!;
        opaque!.EnableSignatureMutationGuard();

        Assert.Multiple(() =>
        {
            Assert.That(opaque.IsSignatureMutationGuardEnabled(), Is.True);
            Assert.Throws<EmailSignatureMutationException>(() => opaque.ReplaceContent(new EmailContentOperations().CreateMemoryCopy(Bytes("changed"), false)));
            Assert.Throws<EmailSignatureMutationException>(() => opaque.SetMediaType(new EmailMediaTypeParser().ParseWithInputAndMode(Bytes("application/octet-stream"), EmailStructuredValueParseMode.Strict).GetValue()));
        });
    }

    static MimeEntity ParseSignedEntity()
    {
        string source =
            "Content-Type: multipart/signed; boundary=s; protocol=\"application/pgp-signature\"; micalg=pgp-sha256\r\n\r\n" +
            "--s\r\nContent-Type: text/plain\r\n\r\nsigned body\r\n" +
            "--s\r\nContent-Type: application/pgp-signature\r\n\r\nnot-a-real-signature\r\n" +
            "--s--\r\n";
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.IsComplete(), Is.True);
        Assert.That(parsed.GetEntity()!.GetKind(), Is.EqualTo(MimeEntityKind.Signed));
        return parsed.GetEntity()!;
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }
}
