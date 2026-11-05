using System.Text;

namespace pop3.Tests;

public class Pop3PlainSaslClientTests
{
    [Test]
    public void BuildsTheRfc4616MessageWithAuthorizationIdentity()
    {
        Pop3Credentials credentials = Credentials("alice", "secret", "mail-reader");
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(credentials, true, false);

        Pop3SaslStep step = client.BeginAuthentication();

        Assert.That(client.GetMechanism(), Is.EqualTo("PLAIN"));
        Assert.That(step.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(step.HasData, Is.True);
        Assert.That(step.Data, Is.EqualTo(Encoding.UTF8.GetBytes("mail-reader\0alice\0secret")));
        Assert.That(step.AuthenticatedIdentity, Is.EqualTo("alice"));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void OmittedAuthorizationIdentityProducesAnEmptyFirstField(string? authorizationIdentity)
    {
        Pop3Credentials credentials = Credentials("alice", "secret", authorizationIdentity);
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(credentials, true, false);

        Pop3SaslStep step = client.BeginAuthentication();

        Assert.That(step.Data, Is.EqualTo(Encoding.UTF8.GetBytes("\0alice\0secret")));
    }

    [Test]
    public void EveryCredentialFieldUsesStrictUtf8()
    {
        Pop3Credentials credentials = Credentials("álîçé😀", "päss🔑", "郵便");
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(credentials, true, false);

        Pop3SaslStep step = client.BeginAuthentication();

        Assert.That(step.Data, Is.EqualTo(Encoding.UTF8.GetBytes("郵便\0álîçé😀\0päss🔑")));
        Assert.That(step.AuthenticatedIdentity, Is.EqualTo("álîçé😀"));
    }

    [Test]
    public void ConstructorCopiesCredentialsBeforeTheyCanBeMutated()
    {
        Pop3Credentials credentials = Credentials("alice", "secret", "reader");
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(credentials, true, false);
        credentials.Username = "mallory";
        credentials.Password = "changed";
        credentials.AuthorizationIdentity = "changed-role";

        Pop3SaslStep step = client.BeginAuthentication();

        Assert.That(step.Data, Is.EqualTo(Encoding.UTF8.GetBytes("reader\0alice\0secret")));
        Assert.That(step.AuthenticatedIdentity, Is.EqualTo("alice"));
    }

    [Test]
    public void UnprotectedTransportRequiresExplicitPermission()
    {
        Pop3Credentials credentials = Credentials("alice", "secret", null);

        Assert.Throws<InvalidOperationException>(() =>
            new Pop3PlainSaslClient(credentials, false, false));
        Assert.DoesNotThrow(() => new Pop3PlainSaslClient(credentials, false, true));
        Assert.DoesNotThrow(() => new Pop3PlainSaslClient(credentials, true, false));
    }

    [TestCase("username")]
    [TestCase("password")]
    [TestCase("authorization")]
    public void NulIsRejectedInEveryCredentialField(string field)
    {
        Pop3Credentials credentials = Credentials("alice", "secret", "reader");
        if (field == "username")
        {
            credentials.Username = "ali\0ce";
        }
        else if (field == "password")
        {
            credentials.Password = "sec\0ret";
        }
        else
        {
            credentials.AuthorizationIdentity = "read\0er";
        }

        Assert.Throws<ArgumentException>(() =>
            new Pop3PlainSaslClient(credentials, true, false));
    }

    [Test]
    public void RequiredFieldsCannotBeEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Pop3PlainSaslClient(Credentials("", "secret", null), true, false));
        Assert.Throws<ArgumentException>(() =>
            new Pop3PlainSaslClient(Credentials("alice", "", null), true, false));
    }

    [TestCase("username")]
    [TestCase("password")]
    [TestCase("authorization")]
    public void InvalidUnicodeIsRejectedBeforeEncoding(string field)
    {
        Pop3Credentials credentials = Credentials("alice", "secret", "reader");
        if (field == "username")
        {
            credentials.Username = "ali\uD800ce";
        }
        else if (field == "password")
        {
            credentials.Password = "sec\uDC00ret";
        }
        else
        {
            credentials.AuthorizationIdentity = "reader\uD800";
        }

        Assert.Throws<ArgumentException>(() =>
            new Pop3PlainSaslClient(credentials, true, false));
    }

    [Test]
    public void NullCredentialsAndRuntimeNullRequiredFieldsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Pop3PlainSaslClient(null!, true, false));

        Pop3Credentials nullUsername = Credentials("alice", "secret", null);
        nullUsername.Username = null!;
        Assert.Throws<ArgumentException>(() =>
            new Pop3PlainSaslClient(nullUsername, true, false));

        Pop3Credentials nullPassword = Credentials("alice", "secret", null);
        nullPassword.Password = null!;
        Assert.Throws<ArgumentException>(() =>
            new Pop3PlainSaslClient(nullPassword, true, false));
    }

    [Test]
    public void CancellationClearsTheReturnedCredentialBufferAndPreventsReuse()
    {
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(
            Credentials("alice", "secret", null),
            true,
            false);
        Pop3SaslStep step = client.BeginAuthentication();

        client.CancelAuthentication();
        client.CancelAuthentication();

        Assert.That(step.Data, Is.All.Zero);
        Assert.Throws<InvalidOperationException>(() => client.BeginAuthentication());
    }

    [Test]
    public void SecurityLayerCompletionClearsSecretsAndReturnsTheSameStream()
    {
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(
            Credentials("alice", "secret", null),
            true,
            false);
        Pop3SaslStep step = client.BeginAuthentication();
        MemoryStream stream = new MemoryStream();

        Stream returned = client.ApplySecurityLayer(stream);

        Assert.That(returned, Is.SameAs(stream));
        Assert.That(step.Data, Is.All.Zero);
        Assert.Throws<ArgumentNullException>(() => client.ApplySecurityLayer(null!));
    }

    [Test]
    public void UnexpectedChallengeRejectsAndClearsTheExchange()
    {
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(
            Credentials("alice", "secret", null),
            true,
            false);
        Pop3SaslStep initial = client.BeginAuthentication();

        Pop3SaslStep rejected = client.ContinueAuthentication(new byte[] { 1, 2, 3 });

        Assert.That(rejected.Status, Is.EqualTo(Pop3SaslStepStatus.Rejected));
        Assert.That(rejected.HasData, Is.False);
        Assert.That(rejected.Data, Is.Empty);
        Assert.That(initial.Data, Is.All.Zero);
        Assert.Throws<ArgumentNullException>(() => client.ContinueAuthentication(null!));
    }

    [Test]
    public void CancellationBeforeBeginClearsStoredCredentialBytes()
    {
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(
            Credentials("alice", "secret", "reader"),
            true,
            false);

        client.CancelAuthentication();

        Assert.That(client.AuthorizationIdentityBytes, Is.Empty);
        Assert.That(client.AuthenticationIdentityBytes, Is.Empty);
        Assert.That(client.PasswordBytes, Is.Empty);
        Assert.Throws<InvalidOperationException>(() => client.BeginAuthentication());
    }

    private static Pop3Credentials Credentials(string username, string password, string? authorizationIdentity)
    {
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = username;
        credentials.Password = password;
        credentials.AuthorizationIdentity = authorizationIdentity;
        return credentials;
    }
}
