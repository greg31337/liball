namespace pop3.Tests;

public class Pop3ExceptionTests
{
    [Test]
    public void ConnectionExceptionRetainsTransportContext()
    {
        InvalidOperationException inner = new InvalidOperationException("socket failure");
        Pop3ConnectionException exception = new Pop3ConnectionException(
            "connection ended",
            "ReadGreeting",
            Pop3SessionState.Authorization,
            true,
            false,
            inner);

        Assert.That(exception.Message, Is.EqualTo("connection ended"));
        Assert.That(exception.Operation, Is.EqualTo("ReadGreeting"));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(exception.IsTimeout, Is.False);
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void ProtocolExceptionRetainsSafeDiagnosticContext()
    {
        FormatException inner = new FormatException("invalid decimal");
        Pop3ProtocolException exception = new Pop3ProtocolException(
            "malformed STAT response",
            Pop3SessionState.Transaction,
            "STAT message count",
            inner);

        Assert.That(exception.Message, Is.EqualTo("malformed STAT response"));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(exception.Context, Is.EqualTo("STAT message count"));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void CommandExceptionRetainsCommandAndCompleteResponse()
    {
        Pop3Command command = new Pop3Command();
        Pop3Response response = new Pop3Response();
        command.Keyword = Pop3Commands.Retr;
        command.Arguments = new[] { "7" };
        response.Kind = Pop3ResponseKind.Negative;
        response.ResponseCodeLevels = new[] { Pop3ResponseCodes.SystemTemporary };
        response.Text = "message unavailable";

        Pop3CommandException exception = new Pop3CommandException("RETR failed", command, response);

        Assert.That(exception.Message, Is.EqualTo("RETR failed"));
        Assert.That(exception.Command, Is.SameAs(command));
        Assert.That(exception.Response, Is.SameAs(response));
        Assert.That(exception.Response.Text, Is.EqualTo("message unavailable"));
    }

    [Test]
    public void AuthenticationExceptionRetainsClassifiedResultWithoutRequestSecrets()
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = Pop3AuthenticationStatus.LoginDelay;
        result.RetryAfterSeconds = 60;
        result.FailureText = "retry later";
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.ResponseCodeLevels = new[] { Pop3ResponseCodes.LoginDelay };
        response.Text = "[LOGIN-DELAY] retry later";

        Pop3AuthenticationException exception = new Pop3AuthenticationException(
            "authentication delayed",
            Pop3AuthenticationMethod.UserPassword,
            result,
            response);

        Assert.That(exception.Message, Is.EqualTo("authentication delayed"));
        Assert.That(exception.Method, Is.EqualTo(Pop3AuthenticationMethod.UserPassword));
        Assert.That(exception.Result, Is.SameAs(result));
        Assert.That(exception.Result.Status, Is.EqualTo(Pop3AuthenticationStatus.LoginDelay));
        Assert.That(exception.Result.RetryAfterSeconds, Is.EqualTo(60));
        Assert.That(exception.Response, Is.SameAs(response));
        Assert.That(exception.Response.ResponseCodeLevels, Is.EqualTo(new[] { Pop3ResponseCodes.LoginDelay }));
    }

    [Test]
    public void TlsExceptionRetainsPolicyAndUnderlyingFailure()
    {
        System.Security.Authentication.AuthenticationException inner = new System.Security.Authentication.AuthenticationException("certificate rejected");
        Pop3TlsException exception = new Pop3TlsException(
            "TLS handshake failed",
            Pop3SecurityMode.ImplicitTls,
            "AuthenticateAsClient",
            inner);

        Assert.That(exception.Message, Is.EqualTo("TLS handshake failed"));
        Assert.That(exception.SecurityMode, Is.EqualTo(Pop3SecurityMode.ImplicitTls));
        Assert.That(exception.Operation, Is.EqualTo("AuthenticateAsClient"));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void MailboxExceptionRetainsOperationAndMessageNumber()
    {
        IOException inner = new IOException("provider failure");
        Pop3MailboxException exception = new Pop3MailboxException("retrieval failed", "Retrieve", 7, inner);

        Assert.That(exception.Message, Is.EqualTo("retrieval failed"));
        Assert.That(exception.Operation, Is.EqualTo("Retrieve"));
        Assert.That(exception.MessageNumber, Is.EqualTo(7));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void StateExceptionRetainsRejectedOperationAndState()
    {
        Pop3StateException exception = new Pop3StateException(
            "STAT is unavailable before authentication",
            Pop3Commands.Stat,
            Pop3SessionState.Authorization);

        Assert.That(exception.Message, Is.EqualTo("STAT is unavailable before authentication"));
        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.InnerException, Is.Null);
    }
}
