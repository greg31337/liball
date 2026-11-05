namespace pop3.Tests;

public class Pop3AuthenticationDataTests
{
    [Test]
    public void AuthenticationInputsHaveSafeIndependentDefaults()
    {
        Pop3Credentials firstCredentials = new Pop3Credentials();
        Pop3Credentials secondCredentials = new Pop3Credentials();
        Pop3ApopRequest apop = new Pop3ApopRequest();
        Pop3AuthenticationRequest firstRequest = new Pop3AuthenticationRequest();
        Pop3AuthenticationRequest secondRequest = new Pop3AuthenticationRequest();

        Assert.That(firstCredentials.Username, Is.Empty);
        Assert.That(firstCredentials.Password, Is.Empty);
        Assert.That(firstCredentials.AuthorizationIdentity, Is.Null);
        Assert.That(secondCredentials.Username, Is.Empty);
        Assert.That(apop.Username, Is.Empty);
        Assert.That(apop.Timestamp, Is.Empty);
        Assert.That(apop.Digest, Is.Empty);
        Assert.That(firstRequest.Method, Is.EqualTo(Pop3AuthenticationMethod.Automatic));
        Assert.That(firstRequest.Credentials, Is.Not.Null);
        Assert.That(firstRequest.Credentials, Is.Not.SameAs(secondRequest.Credentials));
        Assert.That(firstRequest.Apop, Is.Null);
        Assert.That(firstRequest.SaslClient, Is.Null);
        Assert.That(firstRequest.SaslClients, Is.Empty);
        Assert.That(firstRequest.SaslClients, Is.Not.SameAs(secondRequest.SaslClients));
        Assert.That(firstRequest.AllowUnprotectedCredentials, Is.False);
        Assert.That(firstRequest.AllowApop, Is.False);
    }

    [Test]
    public void AuthenticationInputsAndResultsRoundTrip()
    {
        Pop3Credentials credentials = new Pop3Credentials();
        Pop3ApopRequest apop = new Pop3ApopRequest();
        Pop3AuthenticationRequest request = new Pop3AuthenticationRequest();
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();

        credentials.Username = "alice";
        credentials.Password = "secret";
        credentials.AuthorizationIdentity = "mail-reader";
        apop.Username = "alice";
        apop.Timestamp = "<1.2@example.test>";
        apop.Digest = "0123456789abcdef0123456789abcdef";
        request.Method = Pop3AuthenticationMethod.Apop;
        request.Credentials = credentials;
        request.Apop = apop;
        TestSaslClient saslClient = new TestSaslClient();
        request.SaslClient = saslClient;
        request.SaslClients = new IPop3SaslClient[] { saslClient };
        request.AllowUnprotectedCredentials = true;
        request.AllowApop = true;
        result.Status = Pop3AuthenticationStatus.LoginDelay;
        result.AuthenticatedIdentity = "alice";
        result.MaildropKey = "maildrop-1";
        result.RetryAfterSeconds = 60;
        result.FailureText = "retry later";

        Assert.That(credentials.Username, Is.EqualTo("alice"));
        Assert.That(credentials.Password, Is.EqualTo("secret"));
        Assert.That(request.Method, Is.EqualTo(Pop3AuthenticationMethod.Apop));
        Assert.That(request.Credentials, Is.SameAs(credentials));
        Assert.That(request.Credentials.AuthorizationIdentity, Is.EqualTo("mail-reader"));
        Assert.That(request.Apop, Is.SameAs(apop));
        Assert.That(request.Apop!.Username, Is.EqualTo("alice"));
        Assert.That(request.Apop.Timestamp, Is.EqualTo("<1.2@example.test>"));
        Assert.That(request.Apop!.Digest, Is.EqualTo("0123456789abcdef0123456789abcdef"));
        Assert.That(request.SaslClient, Is.SameAs(saslClient));
        Assert.That(request.SaslClients, Has.Length.EqualTo(1));
        Assert.That(request.AllowUnprotectedCredentials, Is.True);
        Assert.That(request.AllowApop, Is.True);
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.LoginDelay));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(result.MaildropKey, Is.EqualTo("maildrop-1"));
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(60));
        Assert.That(result.FailureText, Is.EqualTo("retry later"));
    }

    [Test]
    public void AuthenticationResultHasSafeDefaults()
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.Null);
        Assert.That(result.MaildropKey, Is.Null);
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(-1));
        Assert.That(result.FailureText, Is.Null);
    }

    [Test]
    public void SaslStepDefaultsAndPayloadRoundTrip()
    {
        Pop3SaslStep first = new Pop3SaslStep();
        Pop3SaslStep second = new Pop3SaslStep();

        Assert.That(first.Status, Is.EqualTo(Pop3SaslStepStatus.Continue));
        Assert.That(first.Data, Is.Empty);
        Assert.That(first.Data, Is.Not.SameAs(second.Data));
        Assert.That(first.HasData, Is.False);
        Assert.That(first.AuthenticatedIdentity, Is.Null);

        first.Status = Pop3SaslStepStatus.Success;
        first.Data = new byte[] { 1, 2, 3 };
        first.HasData = true;
        first.AuthenticatedIdentity = "alice";

        Assert.That(first.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(first.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(first.HasData, Is.True);
        Assert.That(first.AuthenticatedIdentity, Is.EqualTo("alice"));
    }

    private class TestSaslClient : IPop3SaslClient
    {
        public string GetMechanism()
        {
            return "TEST";
        }

        public Pop3SaslStep BeginAuthentication()
        {
            return new Pop3SaslStep();
        }

        public Pop3SaslStep ContinueAuthentication(byte[] challenge)
        {
            return new Pop3SaslStep();
        }

        public void CancelAuthentication()
        {
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            return stream;
        }
    }
}
