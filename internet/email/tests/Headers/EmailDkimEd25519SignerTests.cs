using System.Text;
using email;

namespace email.Tests;

public class EmailDkimEd25519SignerTests
{
    [Test]
    public void ReportsTargetRuntimeEd25519SigningAsExplicitlyUnsupported()
    {
        EmailDkimSigningAttemptResult result = new EmailDkimSigner().GetEd25519Sha256Support();

        Assert.Multiple(() =>
        {
            Assert.That(new EmailDkimSigner().IsEd25519Sha256Supported(), Is.False);
            Assert.That(result.GetStatus(), Is.EqualTo(EmailDkimSigningAttemptStatus.UnsupportedAlgorithm));
            Assert.That(result.GetAlgorithmKind(), Is.EqualTo(EmailDkimAlgorithmKind.Ed25519Sha256));
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsUnsupportedAlgorithm(), Is.True);
            Assert.That(result.HasSigningResult(), Is.False);
            Assert.That(result.GetSigningResult(), Is.Null);
            Assert.That(result.GetReason(), Does.Contain(".NET 10"));
            Assert.That(result.GetReason(), Does.Contain("built-in Ed25519 signing primitive"));
            Assert.That(result.GetReason(), Does.Contain("does not add an external cryptography dependency"));
        });
    }

    [Test]
    public void SigningAttemptReturnsUnsupportedWithoutMutationOrKeyRetention()
    {
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: test\r\n");
        byte[] originalHeaders = JoinHeaderBytes(headers);
        byte[] body = Bytes("body\r\n");
        byte[] originalBody = (byte[])body.Clone();
        byte[] privateKey = new byte[32];
        int index = 0;
        while (index < privateKey.Length)
        {
            privateKey[index] = (byte)(index + 1);
            index++;
        }

        byte[] originalKey = (byte[])privateKey.Clone();
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();
        options.SetDomain("example.test");
        options.SetSelector("ed-key");
        options.SetTimestamp(1700000000);

        EmailDkimSigningAttemptResult result = new EmailDkimSigner().SignEd25519Sha256(headers, body, privateKey, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsUnsupportedAlgorithm(), Is.True);
            Assert.That(JoinHeaderBytes(headers), Is.EqualTo(originalHeaders));
            Assert.That(body, Is.EqualTo(originalBody));
            Assert.That(privateKey, Is.EqualTo(originalKey));
            Assert.That(headers.GetCountNamed("DKIM-Signature"), Is.Zero);
        });
    }

    [Test]
    public void SigningAttemptValidatesRequiredReferenceArguments()
    {
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\n");
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();

        Assert.Throws<ArgumentNullException>(() => new EmailDkimSigner().SignEd25519Sha256(null!, new byte[0], new byte[32], options));
        Assert.Throws<ArgumentNullException>(() => new EmailDkimSigner().SignEd25519Sha256(headers, new byte[0], null!, options));
        Assert.Throws<ArgumentNullException>(() => new EmailDkimSigner().SignEd25519Sha256(headers, new byte[0], new byte[32], null!));
    }

    static EmailHeaders ParseHeaders(string fields)
    {
        return new EmailHeaderBlockParser().ParseWithInput(Bytes(fields + "\r\n")).GetHeaders();
    }

    static byte[] JoinHeaderBytes(EmailHeaders headers)
    {
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < headers.GetCount())
        {
            bytes.AddRange(headers.GetAt(index).GetRawFieldBytes());
            index++;
        }

        return bytes.ToArray();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
