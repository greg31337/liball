namespace ftp;

internal static class FtpFailureMapper
{
    internal static System.Exception MapCommandFailure(FtpCommandResult result, System.Exception? innerException)
    {
        string message = GetCommandFailureMessage(result);
        if (result.ConnectionClosed)
        {
            return new FtpConnectionException(message, innerException);
        }

        FtpReply? reply = result.FinalReply;
        if (reply == null)
        {
            return new FtpProtocolException(message, innerException);
        }

        if (reply.Classification.Malformed)
        {
            return new FtpProtocolException(message, innerException);
        }

        if (reply.Classification.AuthenticationFailure)
        {
            return new FtpAuthenticationException(message, innerException);
        }

        return new FtpCommandException(message, innerException);
    }

    internal static System.Exception CreateProtocolFailure(string message, System.Exception? innerException)
    {
        return new FtpProtocolException(message, innerException);
    }

    internal static System.Exception CreateConnectionFailure(string message, System.Exception? innerException)
    {
        return new FtpConnectionException(message, innerException);
    }

    internal static System.Exception CreateAuthenticationFailure(string message, System.Exception? innerException)
    {
        return new FtpAuthenticationException(message, innerException);
    }

    internal static System.Exception CreateAuthorizationFailure(string message, System.Exception? innerException)
    {
        return new FtpAuthorizationException(message, innerException);
    }

    internal static System.Exception CreateTransferFailure(string message, System.Exception? innerException)
    {
        return new FtpTransferException(message, innerException);
    }

    internal static System.Exception CreateTlsFailure(string message, System.Exception? innerException)
    {
        return new FtpTlsException(message, innerException);
    }

    internal static System.Exception CreateConfigurationFailure(string message, System.Exception? innerException)
    {
        return new FtpConfigurationException(message, innerException);
    }

    internal static string GetCommandFailureMessage(FtpCommandResult result)
    {
        if (result.FailureMessage != null)
        {
            return result.FailureMessage;
        }

        if (result.FinalReply != null)
        {
            return $"FTP command {result.Command.Verb} failed with reply {result.FinalReply.Code}: {result.FinalReply.Text}";
        }

        return $"FTP command {result.Command.Verb} failed without a terminal reply.";
    }
}
