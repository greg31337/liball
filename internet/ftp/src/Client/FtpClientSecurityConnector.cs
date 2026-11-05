using System;

namespace ftp;

internal class FtpClientSecurityConnector
{
    internal FtpClientOptions Options;
    internal IFtpCertificateValidator? CertificateValidator;
    internal FtpSessionStateMachine Session;
    internal FtpControlConnection? PendingConnection;
    internal FtpClientAuthTlsCoordinator? AuthorizationCoordinator;

    internal FtpClientSecurityConnector(
        FtpClientOptions options,
        IFtpCertificateValidator? certificateValidator,
        FtpSessionStateMachine session)
    {
        Options = options;
        CertificateValidator = certificateValidator;
        Session = session;
        PendingConnection = null;
        AuthorizationCoordinator = null;
    }

    internal FtpControlConnection OpenConnection()
    {
        FtpFaultIsolationResult openResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return OpenConnectionCore();
        });
        if (openResult.Succeeded)
        {
            FtpControlConnection? connection =
                openResult.Value as FtpControlConnection;
            if (connection != null)
            {
                PendingConnection = null;
                return connection;
            }
        }

        FtpControlConnection? pendingConnection = PendingConnection;
        PendingConnection = null;
        if (pendingConnection != null)
        {
            pendingConnection.Close();
        }
        FtpSessionState sessionState = Session.State.SessionState;
        if (sessionState != FtpSessionState.Disconnected &&
            sessionState != FtpSessionState.Closed)
        {
            Session.CloseConnection();
        }
        if (openResult.Failure != null)
        {
            throw openResult.Failure;
        }

        throw new FtpConnectionException(
            "The FTP security connection did not reach a terminal result.", null);
    }

    internal FtpControlConnection OpenConnectionCore()
    {
        if (Options.SecurityMode == FtpSecurityMode.Plain)
        {
            return OpenPlainConnection();
        }
        if (Options.SecurityMode == FtpSecurityMode.ImplicitTls)
        {
            return OpenImplicitConnection();
        }

        return OpenExplicitConnection();
    }

    internal FtpControlConnection OpenPlainConnection()
    {
        FtpControlConnector connector = new FtpControlConnector(Options);
        FtpControlConnection connection = connector.OpenConnection();
        PendingConnection = connection;
        Session.OpenConnection();
        return connection;
    }

    internal FtpControlConnection OpenImplicitConnection()
    {
        FtpControlConnector connector = new FtpControlConnector(Options);
        FtpControlConnection connection = connector.OpenTransportConnection();
        PendingConnection = connection;
        AuthenticateTls(connection, true, null, Options.Tls);
        connector.ReadGreeting(connection);
        Session.OpenConnection();
        Session.CompleteImplicitTls();
        return connection;
    }

    internal FtpControlConnection OpenExplicitConnection()
    {
        FtpControlConnector connector = new FtpControlConnector(Options);
        FtpControlConnection connection = connector.OpenConnection();
        PendingConnection = connection;
        Session.OpenConnection();
        ExecuteCompatibilityCommands(
            connection,
            Options.Quirks.CommandsBeforeAuthorization,
            "before AUTH TLS");
        FtpClientAuthTlsCoordinator authorization =
            new FtpClientAuthTlsCoordinator(
                Options.SecurityMode,
                Options.AllowPlaintextCredentials,
                Session);
        AuthorizationCoordinator = authorization;
        FtpCommand command = authorization.BeginAuthorization("TLS");
        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(Options);
        FtpReply reply = exchange.ExecuteCommand(connection, command);
        authorization.ApplyAuthorizationReply(reply);
        if (authorization.State.PlaintextFallback)
        {
            connection.PlaintextFallback = true;
            ExecuteCompatibilityCommands(
                connection,
                Options.Quirks.CommandsAfterAuthorization,
                "after AUTH TLS");
            return connection;
        }
        if (!authorization.State.HandshakePending)
        {
            throw new FtpTlsException(
                $"The FTP server rejected AUTH TLS with reply {reply.Code}: {reply.Text}",
                null);
        }

        AuthenticateTls(connection, false, reply, Options.Tls);
        authorization.CompleteHandshake();
        ExecuteCompatibilityCommands(
            connection,
            Options.Quirks.CommandsAfterAuthorization,
            "after AUTH TLS");
        return connection;
    }

    internal void ExecuteCompatibilityCommands(
        FtpControlConnection connection,
        FtpCommand[] commands,
        string stage)
    {
        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(Options);
        int index = 0;
        while (index < commands.Length)
        {
            FtpCommand command = commands[index];
            FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
            FtpReply reply = exchange.ExecuteCommand(connection, command);
            FtpReplyClassification classification =
                FtpReplyClassifier.ClassifyCode(reply.Code);
            if (classification.Category == FtpReplyCategory.PositivePreliminary)
            {
                throw new FtpProtocolException(
                    $"An FTP compatibility command {stage} cannot continue after a preliminary reply.",
                    null);
            }
            Session.ApplyCommandReply(command, reply);
            if (!classification.Succeeded)
            {
                throw new FtpCommandException(
                    $"FTP compatibility command {command.Verb} {stage} failed with reply {reply.Code}: {reply.Text}",
                    null);
            }
            index++;
        }
    }

    internal void AuthenticateTls(
        FtpControlConnection connection,
        bool implicitTls,
        FtpReply? authorizationReply,
        FtpTlsOptions tlsOptions)
    {
        if (connection.TransportStream == null)
        {
            throw new FtpConnectionException(
                "A connected control transport is required for FTP TLS.", null);
        }

        FtpTlsClientStreamRequest request = new FtpTlsClientStreamRequest();
        request.TransportStream = connection.TransportStream;
        request.Options = tlsOptions;
        request.TargetHost = Options.Endpoint.Host;
        request.HandshakeTimeoutMilliseconds =
            Options.Timeouts.TlsHandshakeTimeoutMilliseconds;
        request.CancellationToken = Options.Timeouts.CancellationToken;
        request.LeaveTransportOpen = true;
        request.RemoteEndpoint = Options.Endpoint;
        request.CertificateValidator = CertificateValidator;
        FtpTlsClientStream tlsClient = new FtpTlsClientStream(request);
        FtpTlsStreamResult result = implicitTls ?
            tlsClient.AuthenticateImplicitClient() :
            tlsClient.UpgradeExplicitClient(authorizationReply);
        if (result.Stream == null || !result.Authenticated ||
            !result.Encrypted || !result.Signed)
        {
            tlsClient.CloseClientTls();
            throw new FtpTlsException(
                "The FTP TLS handshake did not produce a protected control stream.",
                null);
        }

        connection.TlsClient = tlsClient;
        connection.TlsResult = result;
        connection.Stream = result.Stream;
        connection.ControlProtected = true;
    }
}
