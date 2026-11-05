namespace ftp;

internal class FtpClientLoginSequence
{
    internal FtpClientOptions Options;
    internal FtpSessionStateMachine Session;
    internal FtpControlConnection Connection;
    internal FtpControlCommandExchange Exchange;

    internal FtpClientLoginSequence(
        FtpClientOptions options,
        FtpSessionStateMachine session,
        FtpControlConnection connection)
    {
        Options = options;
        Session = session;
        Connection = connection;
        Exchange = new FtpControlCommandExchange(options);
    }

    internal void RouteVirtualHost()
    {
        RouteVirtualHostSelection(Options.VirtualHost);
    }

    internal void RouteVirtualHostSelection(string? virtualHost)
    {
        if (virtualHost == null ||
            string.Equals(
                Session.State.SelectedHost,
                virtualHost,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        FtpCommand command = CreateCommand(
            FtpCommandKind.Host, FtpCommandNames.Host, virtualHost);
        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
        FtpReply reply = Exchange.ExecuteCommand(Connection, command);
        Session.ApplyCommandReply(command, reply);
        if (reply.Code == FtpReplyCodes.ServiceReady)
        {
            return;
        }
        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            Connection.Close();
            throw new FtpConnectionException(
                $"The FTP server closed the connection while selecting HOST {virtualHost}.",
                null);
        }
        if (IsNegativeReply(reply.Code))
        {
            throw new FtpCommandException(
                $"The FTP server rejected HOST {virtualHost} with reply {reply.Code}: {reply.Text}",
                null);
        }

        throw new FtpProtocolException(
            "HOST must receive reply 220 or a negative completion reply.", null);
    }

    internal FtpAuthenticationResult Authenticate(FtpCredentials credentials)
    {
        FtpConfigurationValidator.ValidateCredentialOptions(credentials);
        if (string.IsNullOrWhiteSpace(credentials.UserName))
        {
            throw new FtpConfigurationException(
                "FTP authentication requires a non-empty user name.", null);
        }
        if (HasCredentials(credentials) && !Connection.ControlProtected &&
            !Options.AllowPlaintextCredentials)
        {
            throw new FtpConfigurationException(
                "Plaintext credentials require explicit opt-in.", null);
        }

        FtpCommand userCommand = CreateCommand(
            FtpCommandKind.User, FtpCommandNames.User, credentials.UserName);
        FtpSequenceValidator.RequireAllowedCommand(userCommand, Session.State);
        FtpReply userReply = Exchange.ExecuteCommand(Connection, userCommand);
        Session.ApplyCommandReply(userCommand, userReply);
        if (IsAuthenticationCompleteReply(userReply.Code))
        {
            return CreateSuccess(credentials);
        }
        if (userReply.Code == FtpReplyCodes.UserNameOkayNeedPassword ||
            userReply.Code == FtpReplyCodes.UserNameOkayNeedChallengePassword)
        {
            if (credentials.Password == null)
            {
                return CreateAdditionalInformation(userReply.Text);
            }
            return AuthenticatePassword(credentials);
        }
        if (userReply.Code == FtpReplyCodes.NeedAccountForLogin)
        {
            if (credentials.Account == null)
            {
                return CreateAdditionalInformation(userReply.Text);
            }
            return AuthenticateAccount(credentials);
        }

        return HandleAuthenticationFailure("USER", userReply);
    }

    internal FtpAuthenticationResult AuthenticatePassword(
        FtpCredentials credentials)
    {
        FtpCommand passwordCommand = CreateCommand(
            FtpCommandKind.Pass, FtpCommandNames.Pass, credentials.Password);
        FtpSequenceValidator.RequireAllowedCommand(passwordCommand, Session.State);
        FtpReply passwordReply = Exchange.ExecuteCommand(
            Connection, passwordCommand);
        Session.ApplyCommandReply(passwordCommand, passwordReply);
        if (IsAuthenticationCompleteReply(passwordReply.Code) ||
            passwordReply.Code == FtpReplyCodes.CommandSuperfluous)
        {
            return CreateSuccess(credentials);
        }
        if (passwordReply.Code == FtpReplyCodes.NeedAccountForLogin)
        {
            if (credentials.Account == null)
            {
                return CreateAdditionalInformation(passwordReply.Text);
            }
            return AuthenticateAccount(credentials);
        }

        return HandleAuthenticationFailure("PASS", passwordReply);
    }

    internal FtpAuthenticationResult AuthenticateAccount(
        FtpCredentials credentials)
    {
        FtpCommand accountCommand = CreateCommand(
            FtpCommandKind.Acct, FtpCommandNames.Acct, credentials.Account);
        FtpSequenceValidator.RequireAllowedCommand(accountCommand, Session.State);
        FtpReply accountReply = Exchange.ExecuteCommand(Connection, accountCommand);
        Session.ApplyCommandReply(accountCommand, accountReply);
        if (IsAuthenticationCompleteReply(accountReply.Code) ||
            accountReply.Code == FtpReplyCodes.CommandSuperfluous)
        {
            return CreateSuccess(credentials);
        }

        return HandleAuthenticationFailure("ACCT", accountReply);
    }

    internal FtpAuthenticationResult HandleAuthenticationFailure(
        string operation,
        FtpReply reply)
    {
        if (!IsNegativeReply(reply.Code))
        {
            throw new FtpProtocolException(
                $"The FTP {operation} exchange received an invalid intermediate or completion reply.",
                null);
        }

        FtpAuthenticationResult result = new FtpAuthenticationResult();
        result.Status = FtpAuthenticationStatus.Failure;
        result.FailureMessage =
            $"FTP {operation} failed with reply {reply.Code}: {reply.Text}";
        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            Connection.Close();
        }
        return result;
    }

    internal FtpAuthenticationResult CreateSuccess(FtpCredentials credentials)
    {
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = credentials.UserName;
        principal.Authenticated = true;
        principal.Anonymous = credentials.Anonymous;
        principal.Account = credentials.Account;
        FtpAuthenticationResult result = new FtpAuthenticationResult();
        result.Status = FtpAuthenticationStatus.Success;
        result.Principal = principal;
        return result;
    }

    internal FtpAuthenticationResult CreateAdditionalInformation(string challenge)
    {
        FtpAuthenticationResult result = new FtpAuthenticationResult();
        result.Status = FtpAuthenticationStatus.AdditionalInformationRequired;
        result.Challenge = challenge;
        return result;
    }

    internal FtpCommand CreateCommand(
        FtpCommandKind kind,
        string verb,
        string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = verb;
        command.Argument = argument;
        command.RawText = argument == null ? verb : verb + " " + argument;
        return command;
    }

    internal bool HasCredentials(FtpCredentials credentials)
    {
        return credentials.Anonymous ||
            !string.IsNullOrEmpty(credentials.UserName) ||
            credentials.Password != null || credentials.Account != null;
    }

    internal bool IsAuthenticationCompleteReply(int code)
    {
        return code == FtpReplyCodes.UserLoggedIn ||
            code == FtpReplyCodes.UserLoggedInBySecurityExchange;
    }

    internal bool IsNegativeReply(int code)
    {
        return code >= 400 && code <= 599;
    }
}
