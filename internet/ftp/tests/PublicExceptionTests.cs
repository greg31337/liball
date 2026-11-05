namespace ftp.Tests;

public class PublicExceptionTests
{
    [Test]
    public void Domain_failure_mappings_preserve_message_and_inner_exception()
    {
        Exception inner = new InvalidOperationException("socket failure");
        Exception[] failures =
        {
            FtpFailureMapper.CreateProtocolFailure("protocol", inner),
            FtpFailureMapper.CreateConnectionFailure("connection", inner),
            FtpFailureMapper.CreateAuthenticationFailure("authentication", inner),
            FtpFailureMapper.CreateAuthorizationFailure("authorization", inner),
            FtpFailureMapper.CreateTransferFailure("transfer", inner),
            FtpFailureMapper.CreateTlsFailure("tls", inner),
            FtpFailureMapper.CreateConfigurationFailure("configuration", inner),
        };

        Assert.Multiple(() =>
        {
            Assert.That(failures[0], Is.TypeOf<FtpProtocolException>());
            Assert.That(failures[1], Is.TypeOf<FtpConnectionException>());
            Assert.That(failures[2], Is.TypeOf<FtpAuthenticationException>());
            Assert.That(failures[3], Is.TypeOf<FtpAuthorizationException>());
            Assert.That(failures[4], Is.TypeOf<FtpTransferException>());
            Assert.That(failures[5], Is.TypeOf<FtpTlsException>());
            Assert.That(failures[6], Is.TypeOf<FtpConfigurationException>());
            Assert.That(failures.Select(failure => failure.Message), Is.EqualTo(new[]
            {
                "protocol",
                "connection",
                "authentication",
                "authorization",
                "transfer",
                "tls",
                "configuration",
            }));
            Assert.That(failures.All(failure => ReferenceEquals(failure.InnerException, inner)), Is.True);
        });
    }

    [Test]
    public void Closed_command_connection_maps_to_connection_failure()
    {
        FtpCommandResult result = CreateCommandFailure(FtpReplyCodes.ServiceUnavailable, "closing");
        result.ConnectionClosed = true;

        Exception failure = FtpFailureMapper.MapCommandFailure(result, null);

        Assert.That(failure, Is.TypeOf<FtpConnectionException>());
        Assert.That(failure.Message, Is.EqualTo("FTP command NOOP failed with reply 421: closing"));
    }

    [Test]
    public void Missing_or_malformed_command_reply_maps_to_protocol_failure()
    {
        FtpCommandResult missing = new FtpCommandResult();
        missing.Command.Verb = FtpCommandNames.Noop;
        FtpCommandResult malformed = CreateCommandFailure(999, "invalid reply");
        malformed.FinalReply!.Classification.Malformed = true;

        Exception missingFailure = FtpFailureMapper.MapCommandFailure(missing, null);
        Exception malformedFailure = FtpFailureMapper.MapCommandFailure(malformed, null);

        Assert.Multiple(() =>
        {
            Assert.That(missingFailure, Is.TypeOf<FtpProtocolException>());
            Assert.That(missingFailure.Message, Is.EqualTo("FTP command NOOP failed without a terminal reply."));
            Assert.That(malformedFailure, Is.TypeOf<FtpProtocolException>());
        });
    }

    [Test]
    public void Authentication_reply_maps_to_authentication_failure()
    {
        FtpCommandResult result = CreateCommandFailure(FtpReplyCodes.NotLoggedIn, "login denied");
        result.FinalReply!.Classification.AuthenticationFailure = true;

        Exception failure = FtpFailureMapper.MapCommandFailure(result, null);

        Assert.That(failure, Is.TypeOf<FtpAuthenticationException>());
    }

    [Test]
    public void Other_negative_reply_maps_to_command_failure_and_prefers_local_diagnostics()
    {
        FtpCommandResult result = CreateCommandFailure(FtpReplyCodes.RequestedActionNotTaken, "missing");
        result.FailureMessage = "local command failure";

        Exception failure = FtpFailureMapper.MapCommandFailure(result, null);

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.TypeOf<FtpCommandException>());
            Assert.That(failure.Message, Is.EqualTo("local command failure"));
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
