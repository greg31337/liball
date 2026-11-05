using fix;
using NUnit.Framework;

namespace fix.Tests;

public class BasicSessionAuthenticatorTests
{
    [Test]
    public void ConstructorRequiresSettingsAndCopiesExpectedValues()
    {
        SessionSettings settings = Settings();
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(settings, null);
        AuthenticationRequest original = Request();
        settings.Id.SenderCompID = "CHANGED";
        settings.Username = "changed";
        settings.Password = "changed";

        AuthenticationResult result = authenticator.AuthenticateSession(original);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new BasicSessionAuthenticator(null!, null));
            AssertSuccess(result);
            Assert.That(authenticator.ExpectedId.SenderCompID, Is.EqualTo("BUY"));
            Assert.That(authenticator.ExpectedUsername, Is.EqualTo("operator"));
            Assert.That(authenticator.ExpectedPassword, Is.EqualTo("secret"));
        });
    }

    [Test]
    public void MissingConfiguredIdentityIsCopiedAsAnEmptyIdentity()
    {
        SessionSettings settings = Settings();
        settings.Id = null!;
        settings.Username = "";
        settings.Password = "";
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(settings, null);
        AuthenticationRequest request = Request();
        request.Id = new SessionId();

        AuthenticationResult result = authenticator.AuthenticateSession(request);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(authenticator.ExpectedId, Is.Not.Null);
            Assert.That(authenticator.ExpectedId.BeginString, Is.Empty);
        });
    }

    [TestCase("BeginString", "begin-string-mismatch")]
    [TestCase("SenderCompID", "sender-comp-id-mismatch")]
    [TestCase("SenderSubID", "sender-sub-id-mismatch")]
    [TestCase("SenderLocationID", "sender-location-id-mismatch")]
    [TestCase("TargetCompID", "target-comp-id-mismatch")]
    [TestCase("TargetSubID", "target-sub-id-mismatch")]
    [TestCase("TargetLocationID", "target-location-id-mismatch")]
    [TestCase("Qualifier", "session-qualifier-mismatch")]
    public void EverySessionIdentityPartMustMatchExactly(string member, string expectedCode)
    {
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(Settings(), null);
        AuthenticationRequest request = Request();
        ChangeIdentityMember(request.Id, member, "WRONG");

        AuthenticationResult result = authenticator.AuthenticateSession(request);

        AssertFailure(result, expectedCode, true);
    }

    [Test]
    public void ConfiguredCredentialsAreRequiredAndSuccessfulResultIsStable()
    {
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(Settings(), null);
        AuthenticationRequest valid = Request();
        AuthenticationRequest wrongUsername = Request();
        wrongUsername.Username = "intruder";
        AuthenticationRequest wrongPassword = Request();
        wrongPassword.Password = "guess";

        AuthenticationResult accepted = authenticator.AuthenticateSession(valid);
        AuthenticationResult usernameRejected = authenticator.AuthenticateSession(wrongUsername);
        AuthenticationResult passwordRejected = authenticator.AuthenticateSession(wrongPassword);

        Assert.Multiple(() =>
        {
            AssertSuccess(accepted);
            AssertFailure(usernameRejected, "invalid-username", true);
            AssertFailure(passwordRejected, "invalid-password", true);
            Assert.That(BasicSessionAuthenticator.ConstantTimeMatches("pässword", "pässword"), Is.True);
            Assert.That(BasicSessionAuthenticator.ConstantTimeMatches("pässword", "password"), Is.False);
        });
    }

    [Test]
    public void MissingConfiguredCredentialsReturnStableAuthenticationFailures()
    {
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(Settings(), null);
        AuthenticationRequest missingUsername = Request();
        missingUsername.Username = null!;
        AuthenticationRequest missingPassword = Request();
        missingPassword.Password = null!;

        AuthenticationResult usernameRejected = authenticator.AuthenticateSession(missingUsername);
        AuthenticationResult passwordRejected = authenticator.AuthenticateSession(missingPassword);

        Assert.Multiple(() =>
        {
            AssertFailure(usernameRejected, "invalid-username", true);
            AssertFailure(passwordRejected, "invalid-password", true);
        });
    }

    [Test]
    public void UnconfiguredCredentialsAndRawDataDoNotRequireASecret()
    {
        SessionSettings settings = Settings();
        settings.Username = "";
        settings.Password = "";
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(settings, null);
        AuthenticationRequest request = Request();
        request.Username = "optional-user";
        request.Password = "optional-password";
        request.RawData = [0, 1, 255];

        AuthenticationResult result = authenticator.AuthenticateSession(request);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(request.RawData, Is.EqualTo(new byte[] { 0, 1, 255 }));
        });
    }

    [Test]
    public void CustomAuthenticatorReceivesIntactSensitiveAndLogonContext()
    {
        CapturingAuthenticator custom = new CapturingAuthenticator();
        custom.Result = new AuthenticationResult
        {
            Authenticated = false,
            Disconnect = false,
            Code = "application-denied",
            Text = "The application denied this peer."
        };
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(Settings(), custom);
        AuthenticationRequest request = Request();

        AuthenticationResult result = authenticator.AuthenticateSession(request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(custom.Result));
            Assert.That(custom.Request, Is.SameAs(request));
            Assert.That(custom.Request!.RawData, Is.EqualTo(new byte[] { 0, 1, 255 }));
            Assert.That(custom.Request.RemoteAddress, Is.EqualTo("192.0.2.10"));
            Assert.That(custom.Request.LogonMessage, Is.SameAs(request.LogonMessage));
            Assert.That(result.Code, Is.EqualTo("application-denied"));
            Assert.That(result.Disconnect, Is.False);
        });
    }

    [Test]
    public void BasicFailurePreventsCustomDecision()
    {
        CapturingAuthenticator custom = new CapturingAuthenticator();
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(Settings(), custom);
        AuthenticationRequest request = Request();
        request.Password = "wrong";

        AuthenticationResult result = authenticator.AuthenticateSession(request);

        Assert.Multiple(() =>
        {
            AssertFailure(result, "invalid-password", true);
            Assert.That(custom.CallCount, Is.Zero);
            Assert.That(custom.Request, Is.Null);
        });
    }

    [Test]
    public void MissingRequestIdentityAndCustomResultAreRejected()
    {
        BasicSessionAuthenticator authenticator = new BasicSessionAuthenticator(Settings(), null);
        AuthenticationRequest missingId = Request();
        missingId.Id = null!;
        CapturingAuthenticator custom = new CapturingAuthenticator { Result = null! };
        BasicSessionAuthenticator customAuthenticator = new BasicSessionAuthenticator(Settings(), custom);

        AuthenticationResult missingRequest = authenticator.AuthenticateSession(null!);
        AuthenticationResult missingIdentity = authenticator.AuthenticateSession(missingId);
        AuthenticationResult missingCustomResult = customAuthenticator.AuthenticateSession(Request());

        Assert.Multiple(() =>
        {
            AssertFailure(missingRequest, "missing-authentication-request", true);
            AssertFailure(missingIdentity, "missing-session-id", true);
            AssertFailure(missingCustomResult, "missing-custom-authentication-result", true);
        });
    }

    private static SessionSettings Settings()
    {
        SessionSettings settings = new SessionSettings();
        settings.Id = Identity();
        settings.Username = "operator";
        settings.Password = "secret";
        return settings;
    }

    private static AuthenticationRequest Request()
    {
        AuthenticationRequest request = new AuthenticationRequest();
        request.Id = Identity();
        request.Username = "operator";
        request.Password = "secret";
        request.RawData = [0, 1, 255];
        request.RemoteAddress = "192.0.2.10";
        request.LogonMessage = new FixMessage { MsgType = FixAdminMessageTypes.Logon };
        return request;
    }

    private static SessionId Identity()
    {
        return new SessionId
        {
            BeginString = "FIXT.1.1",
            SenderCompID = "BUY",
            SenderSubID = "ORDER",
            SenderLocationID = "NY",
            TargetCompID = "SELL",
            TargetSubID = "MATCH",
            TargetLocationID = "LDN",
            Qualifier = "PRIMARY"
        };
    }

    private static void ChangeIdentityMember(SessionId id, string member, string value)
    {
        switch (member)
        {
            case "BeginString": id.BeginString = value; break;
            case "SenderCompID": id.SenderCompID = value; break;
            case "SenderSubID": id.SenderSubID = value; break;
            case "SenderLocationID": id.SenderLocationID = value; break;
            case "TargetCompID": id.TargetCompID = value; break;
            case "TargetSubID": id.TargetSubID = value; break;
            case "TargetLocationID": id.TargetLocationID = value; break;
            case "Qualifier": id.Qualifier = value; break;
        }
    }

    private static void AssertSuccess(AuthenticationResult result)
    {
        Assert.That(result.Authenticated, Is.True);
        Assert.That(result.Disconnect, Is.False);
        Assert.That(result.Code, Is.EqualTo("authenticated"));
        Assert.That(result.Text, Is.Not.Empty);
    }

    private static void AssertFailure(AuthenticationResult result, string code, bool disconnect)
    {
        Assert.That(result.Authenticated, Is.False);
        Assert.That(result.Disconnect, Is.EqualTo(disconnect));
        Assert.That(result.Code, Is.EqualTo(code));
        Assert.That(result.Text, Is.Not.Empty);
    }

    private class CapturingAuthenticator : ISessionAuthenticator
    {
        public AuthenticationRequest? Request;
        public AuthenticationResult Result = new AuthenticationResult { Authenticated = true, Code = "custom-accepted" };
        public int CallCount;

        public AuthenticationResult AuthenticateSession(AuthenticationRequest request)
        {
            Request = request;
            CallCount++;
            return Result;
        }
    }
}
