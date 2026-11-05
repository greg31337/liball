namespace pop3;

/// <summary>Serves one caller-supplied POP3 connection at a time.</summary>
public class Pop3Server : IPop3Server
{
    /// <summary>Serves one accepted connection until QUIT, extension closure, or failure.</summary>
    /// <param name="connection">The accepted connection.</param>
    /// <param name="options">The listener-neutral server configuration.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking server session.</param>
    public void ServeConnection(
        Pop3ServerConnection connection,
        Pop3ServerOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ServerEngine engine = new Pop3ServerEngine();
        using (Pop3ServerFailureGuard guard =
            new Pop3ServerFailureGuard(engine))
        {
            engine.StartSession(connection, options, cancellationToken);
            bool complete = false;
            while (!complete)
            {
                Pop3SessionState state =
                    engine.GetStateMachine().GetCurrentState();
                if (state != Pop3SessionState.Authorization &&
                    state != Pop3SessionState.Transaction)
                {
                    complete = true;
                }
                else
                {
                    Pop3Command command = engine.ReadNextCommand(
                        cancellationToken);
                    DispatchCommand(engine, command, cancellationToken);
                }
            }
            guard.CompleteOperation();
        }
    }

    void DispatchCommand(
        Pop3ServerEngine engine,
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        string keyword = command.Keyword;
        if (keyword == Pop3Commands.User)
        {
            engine.HandleUserCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Pass)
        {
            CompleteAuthenticationAttempt(
                engine,
                engine.HandlePasswordCommand(command, cancellationToken),
                cancellationToken);
        }
        else if (keyword == Pop3Commands.Apop)
        {
            CompleteAuthenticationAttempt(
                engine,
                engine.HandleApopCommand(command, cancellationToken),
                cancellationToken);
        }
        else if (keyword == Pop3Commands.Auth)
        {
            CompleteAuthenticationAttempt(
                engine,
                engine.HandleAuthCommand(command, cancellationToken),
                cancellationToken);
        }
        else if (keyword == Pop3Commands.Stat)
        {
            engine.HandleStatCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.List)
        {
            engine.HandleListCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Retr)
        {
            engine.HandleRetrCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Dele)
        {
            engine.HandleDeleCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Noop)
        {
            engine.HandleNoopCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Rset)
        {
            engine.HandleRsetCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Quit)
        {
            engine.HandleQuitCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Top)
        {
            engine.HandleTopCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Uidl)
        {
            engine.HandleUidlCommand(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Capa)
        {
            engine.WriteCapabilitiesResponse(cancellationToken);
        }
        else if (keyword == Pop3Commands.Stls)
        {
            engine.UpgradeTransportToTls(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Utf8)
        {
            engine.HandleUtf8Command(command, cancellationToken);
        }
        else if (keyword == Pop3Commands.Lang)
        {
            engine.HandleLangCommand(command, cancellationToken);
        }
        else
        {
            engine.HandleExtensionCommand(command, cancellationToken);
        }
    }

    void CompleteAuthenticationAttempt(
        Pop3ServerEngine engine,
        Pop3AuthenticationResult result,
        System.Threading.CancellationToken cancellationToken)
    {
        if (result.Status == Pop3AuthenticationStatus.Success)
        {
            engine.CompleteAuthentication(result, cancellationToken);
        }
    }
}
