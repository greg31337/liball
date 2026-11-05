using System.Text;
using email;

namespace email.Tests;

public class EmailModelApiCompletionTests
{
    [Test]
    public void ExplicitMessageDirtyTransitionsAndGuardDisableAreObservable()
    {
        EmailMessage exact = ParseMessage("Subject: one\r\n\r\nbody");
        Assert.That(exact.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        exact.MarkExactDirty();
        Assert.That(exact.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));

        EmailMessage semantic = ParseMessage("Subject: one\r\n\r\nbody");
        semantic.MarkSemanticDirty();
        Assert.That(semantic.GetDirtyState(), Is.EqualTo(EmailDirtyState.SemanticOnly));

        EmailMessage guarded = ParseMessage("DKIM-Signature: v=1; a=rsa-sha256; d=example.test; s=test; h=subject; bh=AA==; b=AA==\r\nSubject: one\r\n\r\nbody");
        guarded.EnableSignatureMutationGuard();
        Assert.That(guarded.IsSignatureMutationGuardEnabled(), Is.True);
        Assert.Throws<EmailSignatureMutationException>(() => guarded.MarkExactDirty());
        guarded.DisableSignatureMutationGuard();
        Assert.That(guarded.IsSignatureMutationGuardEnabled(), Is.False);
        Assert.DoesNotThrow(() => guarded.MarkExactDirty());
    }

    [Test]
    public void ExplicitEntityDirtyStatesIssueClearingAndMaterializationAreObservable()
    {
        MimeEntity exact = ParseEntity("Content-Type: text/plain\r\n\r\nbody");
        exact.MarkExactDirty();
        Assert.That(exact.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));

        MimeEntity semantic = ParseEntity("Content-Type: text/plain\r\n\r\nbody");
        semantic.MarkSemanticDirty();
        Assert.That(semantic.GetDirtyState(), Is.EqualTo(EmailDirtyState.SemanticOnly));

        semantic.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.ValidationFailed, "test"));
        Assert.That(semantic.GetOwnIssues().GetCount(), Is.EqualTo(1));
        semantic.ClearOwnIssues();
        Assert.That(semantic.GetOwnIssues().GetCount(), Is.Zero);

        MimeEntity generated = new EmailEntityFactory().CreateTextWithValue("body");
        EmailContent content = generated.MaterializeContent(4);
        EmailContent decoded = generated.MaterializeDecodedContent(4);
        Assert.That(content, Is.SameAs(decoded));
        Assert.That(decoded.Materialize(4), Is.EqualTo(Bytes("body")));
    }

    [Test]
    public void EntityGuardCanBeDisabledBeforeExplicitMutation()
    {
        MimeEntity entity = ParseEntity("Content-Type: application/pkcs7-mime; smime-type=signed-data\r\nContent-Transfer-Encoding: base64\r\n\r\nAA==\r\n");
        entity.EnableSignatureMutationGuard();
        Assert.That(entity.IsSignatureMutationGuardEnabled(), Is.True);
        Assert.Throws<EmailSignatureMutationException>(() => entity.MarkSemanticDirty());
        entity.DisableSignatureMutationGuard();
        Assert.That(entity.IsSignatureMutationGuardEnabled(), Is.False);
        Assert.DoesNotThrow(() => entity.MarkSemanticDirty());
    }

    static EmailMessage ParseMessage(string value)
    {
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static MimeEntity ParseEntity(string value)
    {
        return new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving()).GetEntity()!;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
