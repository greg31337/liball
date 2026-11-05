using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ftp;

internal class FtpClientDataConnectionStrategy
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;
    internal FtpControlConnection ControlConnection;
    internal FtpDataConnectionCoordinator Coordinator;
    internal FtpDataConnectionMode SelectedMode;
    internal FtpCommand[] SetupCommands;
    internal FtpActiveListener? ActiveListener;
    internal bool Prepared;
    internal bool TransferStarted;

    internal FtpClientDataConnectionStrategy(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for data setup.", null);
        }

        ClientState = clientState;
        Options = clientState.Options;
        ControlConnection = clientState.Connection;
        Coordinator = new FtpDataConnectionCoordinator();
        SelectedMode = FtpDataConnectionMode.Automatic;
        SetupCommands = new FtpCommand[0];
        ActiveListener = null;
        Prepared = false;
        TransferStarted = false;
    }

    internal FtpDataConnectionCoordinator PrepareDataConnection()
    {
        if (TransferStarted)
        {
            throw new FtpProtocolException(
                "FTP data setup cannot be retried after a transfer has started.", null);
        }
        if (Prepared)
        {
            throw new FtpProtocolException(
                "The FTP data strategy has already prepared one data connection.", null);
        }

        ClientState.RequireOpenSession("PrepareDataConnection");
        ClientState.Session.ClearDataConnectionState();
        FtpFaultIsolationResult preparation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                PrepareDataConnectionCore();
                return null;
            });
        if (!preparation.Succeeded)
        {
            CleanupFailedPreparation(preparation.Failure);
        }

        Prepared = true;
        return Coordinator;
    }

    internal void PrepareDataConnectionCore()
    {
        FtpDataConnectionMode configuredMode = Options.DataConnection.Mode;
        SelectedMode = configuredMode == FtpDataConnectionMode.Active ?
            FtpDataConnectionMode.Active : FtpDataConnectionMode.Passive;
        if (SelectedMode == FtpDataConnectionMode.Passive)
        {
            FtpReply? passiveFailure = PreparePassiveConnection();
            if (passiveFailure == null)
            {
                return;
            }

            bool automaticFallback = configuredMode == FtpDataConnectionMode.Automatic &&
                !ClientState.Session.State.ExtendedPassiveOnly &&
                !passiveFailure.Classification.Malformed;
            if (!automaticFallback)
            {
                ThrowSetupFailure(passiveFailure, "passive");
            }

            SelectedMode = FtpDataConnectionMode.Active;
        }

        FtpReply? activeFailure = PrepareActiveConnection();
        if (activeFailure != null)
        {
            CloseActiveListener();
            ThrowSetupFailure(activeFailure, "active");
        }

    }

    internal FtpReply BeginTransfer()
    {
        if (!Prepared)
        {
            throw new FtpProtocolException(
                "FTP data setup must complete before a transfer starts.", null);
        }
        if (TransferStarted)
        {
            throw new FtpProtocolException(
                "The FTP data strategy cannot replay a transfer command.", null);
        }

        TransferStarted = true;
        return Coordinator.BeginTransfer();
    }

    internal void AbortDataConnection()
    {
        FtpFaultIsolationResult coordinatorCleanup =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                Coordinator.AbortTransfer();
                return null;
            });
        FtpFaultIsolationResult listenerCleanup =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                CloseActiveListener();
                return null;
            });
        Prepared = false;
        TransferStarted = false;
        ClientState.Session.ClearDataConnectionState();
        if (!coordinatorCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The FTP data connection could not be aborted.",
                coordinatorCleanup.Failure);
        }
        if (!listenerCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The FTP active listener could not be closed.",
                listenerCleanup.Failure);
        }
    }

    internal FtpReply? PreparePassiveConnection()
    {
        IPAddress peerAddress = ParseControlAddress(
            ControlConnection.RemoteEndpoint, "remote");
        FtpDataConnectionOptions dataOptions =
            FtpClientOptionsSnapshot.CopyDataConnection(Options.DataConnection);
        FtpAddressFamily family = FtpPeerPolicy.GetAddressFamily(peerAddress);
        bool featureKnownUnsupported = ClientState.Features.Negotiated &&
            !ClientState.FeatureState.ExtendedPassive;
        if (featureKnownUnsupported && family == FtpAddressFamily.IPv4 &&
            !dataOptions.SendExtendedPassiveAll)
        {
            dataOptions.UseExtendedPassive = false;
        }

        FtpPassiveConnector connector = new FtpPassiveConnector(
            peerAddress, dataOptions);
        FtpCommand? allCommand = connector.CreateExtendedPassiveAllCommand();
        if (allCommand != null)
        {
            FtpReply allReply = ExecuteSetupCommand(allCommand);
            connector.ApplyExtendedPassiveAllReply(allReply);
            if (allReply.Code == FtpReplyCodes.ServiceUnavailable)
            {
                return allReply;
            }
        }

        FtpCommand command;
        if (connector.UseExtendedPassive)
        {
            command = connector.CreateExtendedPassiveCommand(
                FtpAddressFamily.Unspecified);
        }
        else
        {
            command = connector.CreatePassiveCommand();
        }

        FtpReply reply = ExecuteSetupCommand(command);
        if (command.Kind == FtpCommandKind.Epsv &&
            reply.Code == FtpReplyCodes.EnteringExtendedPassiveMode)
        {
            FtpPassiveConnection passive = connector.OpenExtendedPassiveConnection(
                reply,
                FtpAddressFamily.Unspecified,
                Options.Timeouts.DataConnectionTimeoutMilliseconds,
                Options.Timeouts.CancellationToken);
            Coordinator.SetPreopenedConnection(passive.Connection);
            passive.Connection = null;
            return null;
        }
        if (command.Kind == FtpCommandKind.Pasv &&
            reply.Code == FtpReplyCodes.EnteringPassiveMode)
        {
            FtpPassiveConnection passive = connector.OpenPassiveConnection(
                reply,
                Options.Timeouts.DataConnectionTimeoutMilliseconds,
                Options.Timeouts.CancellationToken);
            Coordinator.SetPreopenedConnection(passive.Connection);
            passive.Connection = null;
            return null;
        }

        bool canFallback = command.Kind == FtpCommandKind.Epsv &&
            connector.AllowPassiveFallback &&
            connector.ControlPeerFamily == FtpAddressFamily.IPv4 &&
            IsExtendedUnsupportedReply(reply);
        if (!canFallback)
        {
            return reply;
        }

        FtpCommand fallback = connector.CreatePassiveFallbackCommand(reply);
        FtpReply fallbackReply = ExecuteSetupCommand(fallback);
        if (fallbackReply.Code != FtpReplyCodes.EnteringPassiveMode)
        {
            return fallbackReply;
        }

        FtpPassiveConnection fallbackConnection = connector.OpenPassiveConnection(
            fallbackReply,
            Options.Timeouts.DataConnectionTimeoutMilliseconds,
            Options.Timeouts.CancellationToken);
        Coordinator.SetPreopenedConnection(fallbackConnection.Connection);
        fallbackConnection.Connection = null;
        return null;
    }

    internal FtpReply? PrepareActiveConnection()
    {
        IPAddress peerAddress = ParseControlAddress(
            ControlConnection.RemoteEndpoint, "remote");
        IPAddress localAddress = ParseControlAddress(
            ControlConnection.LocalEndpoint, "local");
        FtpDataConnectionOptions dataOptions =
            FtpClientOptionsSnapshot.CopyDataConnection(Options.DataConnection);
        FtpAddressFamily family = FtpPeerPolicy.GetAddressFamily(localAddress);
        bool featureKnownUnsupported = ClientState.Features.Negotiated &&
            !ClientState.FeatureState.ExtendedActive;
        if (featureKnownUnsupported && family == FtpAddressFamily.IPv4)
        {
            dataOptions.UseExtendedActive = false;
        }

        FtpActiveConnector connector = new FtpActiveConnector(
            peerAddress, localAddress, dataOptions);
        FtpActiveListener listener = connector.OpenListener();
        ActiveListener = listener;
        FtpCommand command = connector.UseExtendedActive ?
            connector.CreateExtendedActiveCommand(listener) :
            connector.CreateActiveCommand(listener);
        FtpReply reply = ExecuteSetupCommand(command);
        if (command.Kind == FtpCommandKind.Eprt &&
            reply.Code != FtpReplyCodes.CommandOkay &&
            connector.AllowActiveFallback &&
            connector.AddressFamily == FtpAddressFamily.IPv4 &&
            IsExtendedUnsupportedReply(reply))
        {
            FtpCommand fallback = connector.CreateActiveFallbackCommand(
                listener, reply);
            reply = ExecuteSetupCommand(fallback);
        }
        if (reply.Code != FtpReplyCodes.CommandOkay)
        {
            return reply;
        }

        Coordinator.SetOnDemandConnection(
            (timeoutMilliseconds, cancellationToken) =>
            {
                return connector.AcceptConnection(
                    listener, timeoutMilliseconds, cancellationToken);
            },
            () =>
            {
                CloseActiveListenerValue(listener);
            });
        return null;
    }

    internal FtpReply ExecuteSetupCommand(FtpCommand command)
    {
        if (TransferStarted)
        {
            throw new FtpProtocolException(
                "FTP data fallback is prohibited after a transfer has started.", null);
        }

        RecordSetupCommand(command);
        FtpCommandResult result =
            new FtpClientCommandExecutor(ClientState).ExecutePreparedCommand(command);
        if (result.FinalReply == null)
        {
            throw new FtpProtocolException(
                "An FTP data setup command requires a terminal reply.", null);
        }
        return result.FinalReply;
    }

    internal void RecordSetupCommand(FtpCommand command)
    {
        FtpCommand[] commands = new FtpCommand[SetupCommands.Length + 1];
        Array.Copy(SetupCommands, commands, SetupCommands.Length);
        commands[commands.Length - 1] = command;
        SetupCommands = commands;
    }

    internal void CloseActiveListener()
    {
        FtpActiveListener? listener = ActiveListener;
        ActiveListener = null;
        if (listener != null)
        {
            CloseActiveListenerValue(listener);
        }
    }

    internal void CleanupFailedPreparation(Exception? preparationFailure)
    {
        FtpFaultIsolationResult coordinatorCleanup =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                Coordinator.AbortTransfer();
                return null;
            });
        FtpFaultIsolationResult listenerCleanup =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                CloseActiveListener();
                return null;
            });
        ClientState.Session.ClearDataConnectionState();
        if (preparationFailure != null)
        {
            throw preparationFailure;
        }
        if (!coordinatorCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "A failed FTP data setup could not release its connection.",
                coordinatorCleanup.Failure);
        }
        if (!listenerCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "A failed FTP data setup could not release its listener.",
                listenerCleanup.Failure);
        }
        throw new FtpConnectionException(
            "The FTP data setup did not reach a terminal result.", null);
    }

    internal static void CloseActiveListenerValue(FtpActiveListener listener)
    {
        Socket? socket = listener.Listener;
        listener.Listener = null;
        listener.Open = false;
        new FtpResourceLifetime(socket).DisposeResource();
    }

    internal static bool IsExtendedUnsupportedReply(FtpReply reply)
    {
        return reply.Code == FtpReplyCodes.SyntaxError ||
            reply.Code == FtpReplyCodes.SyntaxErrorInParameters ||
            reply.Code == FtpReplyCodes.CommandNotImplemented ||
            reply.Code == FtpReplyCodes.CommandParameterNotImplemented ||
            reply.Code == FtpReplyCodes.NetworkProtocolNotSupported;
    }

    internal static IPAddress ParseControlAddress(
        FtpEndpoint endpoint,
        string endpointName)
    {
        if (endpoint.AddressFamily != FtpAddressFamily.IPv4 &&
            endpoint.AddressFamily != FtpAddressFamily.IPv6)
        {
            throw new FtpConnectionException(
                $"The FTP control {endpointName} endpoint is not numeric.", null);
        }
        return FtpActiveConnector.ParseNumericAddress(
            endpoint.Host, endpoint.AddressFamily, "control " + endpointName);
    }

    internal static void ThrowSetupFailure(FtpReply reply, string mode)
    {
        throw new FtpConnectionException(
            $"The FTP {mode} data setup failed with reply {reply.Code}.", null);
    }
}
