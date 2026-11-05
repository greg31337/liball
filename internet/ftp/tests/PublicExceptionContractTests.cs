namespace ftp.Tests;

public class PublicExceptionContractTests
{
    [Test]
    public void Every_custom_exception_is_directly_throwable_and_catchable_as_System_Exception()
    {
        Exception inner = new InvalidOperationException("underlying failure");
        Exception[] failures =
        {
            new FtpAuthenticationException("authentication", inner),
            new FtpAuthorizationException("authorization", inner),
            new FtpCommandException("command", inner),
            new FtpConfigurationException("configuration", inner),
            new FtpConnectionException("connection", inner),
            new FtpProtocolException("protocol", inner),
            new FtpTlsException("tls", inner),
            new FtpTransferException("transfer", inner)
        };
        Type[] publicExceptionTypes = typeof(FtpClient).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "ftp" &&
                type.IsSubclassOf(typeof(Exception)))
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(publicExceptionTypes,
            Is.EqualTo(failures
                .Select(failure => failure.GetType())
                .OrderBy(type => type.Name)
                .ToArray()));

        foreach (Exception failure in failures)
        {
            Exception? caught = null;
            try
            {
                throw failure;
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            Assert.Multiple(() =>
            {
                Assert.That(caught, Is.SameAs(failure), failure.GetType().Name);
                Assert.That(caught!.InnerException, Is.SameAs(inner),
                    failure.GetType().Name);
                Assert.That(caught.Message, Is.Not.Empty,
                    failure.GetType().Name);
            });
        }
    }

    [Test]
    public void Intended_failure_paths_produce_every_specific_custom_exception()
    {
        FtpCommandResult authenticationResult = CreateCommandFailure(
            FtpReplyCodes.NotLoggedIn, "login denied");
        authenticationResult.FinalReply!.Classification.AuthenticationFailure =
            true;
        Exception authentication = FtpFailureMapper.MapCommandFailure(
            authenticationResult, null);
        Exception authorization = FtpFailureMapper.CreateAuthorizationFailure(
            "authorization denied", null);
        FtpCommandResult commandResult = CreateCommandFailure(
            FtpReplyCodes.RequestedActionNotTaken, "command denied");
        Exception command = FtpFailureMapper.MapCommandFailure(
            commandResult, null);

        FtpConfigurationException configuration =
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpClient(null).Connect(null!))!;
        FtpConnectionException connection =
            Assert.Throws<FtpConnectionException>(() =>
                new FtpClient(null).Reconnect())!;
        FtpProtocolException protocol =
            Assert.Throws<FtpProtocolException>(() =>
                new FtpCommandCodec().ParseCommand(
                    "NOOP\r\nDELE victim"))!;
        FtpTlsException tls = Assert.Throws<FtpTlsException>(() =>
            FtpTlsSessionReusePolicy.RequireSatisfied(
                true, true, new FtpTlsSessionReuseEvidence()))!;
        FtpTransferException transfer =
            Assert.Throws<FtpTransferException>(() =>
                new RestartMarkerCodec().ParseMarker(
                    "F2:1:2:3:0:0"))!;

        Assert.Multiple(() =>
        {
            Assert.That(authentication,
                Is.TypeOf<FtpAuthenticationException>());
            Assert.That(authentication.Message, Does.Contain("login denied"));
            Assert.That(authorization,
                Is.TypeOf<FtpAuthorizationException>());
            Assert.That(authorization.Message,
                Is.EqualTo("authorization denied"));
            Assert.That(command, Is.TypeOf<FtpCommandException>());
            Assert.That(command.Message, Does.Contain("command denied"));
            Assert.That(configuration.Message, Does.Contain("required"));
            Assert.That(connection.Message, Does.Contain("Reconnect"));
            Assert.That(protocol.Message, Does.Contain("control"));
            Assert.That(tls.Message, Does.Contain("reuse"));
            Assert.That(transfer.Message, Does.Contain("F1"));
        });
    }

    private static FtpCommandResult CreateCommandFailure(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpCommandResult result = new FtpCommandResult();
        result.Command.Verb = FtpCommandNames.Noop;
        result.FinalReply = reply;
        return result;
    }
}
