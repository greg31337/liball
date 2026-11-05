using System;
using System.Net;

namespace ftp;

internal class FtpThirdPartyTransferCoordinator
{
    internal IPAddress TargetControlPeerAddress;
    internal FtpAddressFamily TargetControlFamily;
    internal bool SourceUseExtendedActive;
    internal bool SourceAllowActiveFallback;
    internal bool SourceRejectPrivilegedActivePorts;
    internal bool TargetUseExtendedPassive;
    internal bool TargetAllowPassiveFallback;
    internal bool TargetIgnorePassiveAdvertisedAddress;
    internal bool TargetRejectPassiveAddressMismatch;
    internal FtpReplyCoordinator SourceReplies;
    internal FtpReplyCoordinator TargetReplies;
    internal FtpThirdPartyTransferState State;

    internal FtpThirdPartyTransferCoordinator(
        IPAddress? targetControlPeerAddress,
        FtpDataConnectionOptions? sourceDataOptions,
        FtpDataConnectionOptions? targetDataOptions,
        FtpDataProtectionLevel sourceProtection,
        FtpDataProtectionLevel targetProtection)
    {
        ValidateConfiguration(
            targetControlPeerAddress,
            sourceDataOptions,
            targetDataOptions,
            sourceProtection,
            targetProtection);
        if (targetControlPeerAddress == null ||
            sourceDataOptions == null || targetDataOptions == null)
        {
            throw new FtpConfigurationException(
                "Third-party transfer setup requires both session policies and the target control peer.", null);
        }

        TargetControlPeerAddress = FtpPeerPolicy.NormalizeAddress(targetControlPeerAddress);
        TargetControlFamily = FtpPeerPolicy.GetAddressFamily(TargetControlPeerAddress);
        SourceUseExtendedActive = sourceDataOptions.UseExtendedActive;
        SourceAllowActiveFallback = sourceDataOptions.AllowActiveFallback;
        SourceRejectPrivilegedActivePorts = sourceDataOptions.RejectPrivilegedActivePorts;
        TargetUseExtendedPassive = targetDataOptions.UseExtendedPassive;
        TargetAllowPassiveFallback = targetDataOptions.AllowPassiveFallback;
        TargetIgnorePassiveAdvertisedAddress = targetDataOptions.IgnorePassiveAdvertisedAddress;
        TargetRejectPassiveAddressMismatch = targetDataOptions.RejectPassiveAddressMismatch;
        SourceReplies = new FtpReplyCoordinator();
        TargetReplies = new FtpReplyCoordinator();
        State = new FtpThirdPartyTransferState();
    }

    internal FtpCommand BeginTargetExtendedPassiveSetup(FtpAddressFamily requestedFamily)
    {
        RequireFreshPassiveSetup();
        if (!TargetUseExtendedPassive)
        {
            throw new FtpConfigurationException(
                "Extended passive mode is disabled for the target session.", null);
        }
        if (requestedFamily != FtpAddressFamily.Unspecified &&
            requestedFamily != TargetControlFamily)
        {
            throw new FtpConfigurationException(
                "The EPSV network protocol must match the target control peer.", null);
        }

        string? argument = null;
        if (requestedFamily != FtpAddressFamily.Unspecified)
        {
            FtpExtendedPassiveRequest request = new FtpExtendedPassiveRequest();
            request.HasProtocol = true;
            request.Protocol = FtpEndpointCodec.FormatNetworkProtocol(requestedFamily, "EPSV");
            request.AddressFamily = requestedFamily;
            argument = FtpEndpointCodec.FormatExtendedPassiveArgument(request);
        }

        FtpCommand command = CreateCommand(FtpCommandKind.Epsv, FtpCommandNames.Epsv, argument);
        SetPendingPassiveCommand(command);
        return command;
    }

    internal FtpCommand BeginTargetPassiveSetup()
    {
        RequireFreshPassiveSetup();
        RequireIpv4TargetControlPeer();
        if (TargetUseExtendedPassive)
        {
            throw new FtpConfigurationException(
                "Direct PASV setup requires extended passive mode to be disabled.", null);
        }

        FtpCommand command = CreateCommand(FtpCommandKind.Pasv, FtpCommandNames.Pasv, null);
        SetPendingPassiveCommand(command);
        return command;
    }

    internal FtpCommand BeginTargetPassiveFallback(FtpReply? extendedPassiveReply)
    {
        if (!State.TargetPassiveSetupPending)
        {
            throw new FtpProtocolException(
                "No target EPSV command awaits fallback.", null);
        }
        RequirePendingCommand(State.TargetPassiveCommand, FtpCommandKind.Epsv, "EPSV");
        RequireIpv4TargetControlPeer();
        if (!TargetAllowPassiveFallback)
        {
            throw new FtpProtocolException(
                "PASV fallback is not enabled for the target session.", null);
        }

        FtpReply failureReply = RequireUnsupportedReply(extendedPassiveReply, "EPSV");
        FtpCommand failedCommand = RequireCommand(State.TargetPassiveCommand, "EPSV");
        State.TargetCommandResults = AppendResult(
            State.TargetCommandResults,
            CreateSetupResult(failedCommand, failureReply, false));
        FtpCommand command = CreateCommand(FtpCommandKind.Pasv, FtpCommandNames.Pasv, null);
        SetPendingPassiveCommand(command);
        return command;
    }

    internal void ApplyTargetPassiveSetupReply(FtpReply? reply)
    {
        FtpCommand command = RequireCommand(State.TargetPassiveCommand, "passive setup");
        if (!State.TargetPassiveSetupPending)
        {
            throw new FtpProtocolException(
                "No target passive setup command awaits a reply.", null);
        }

        FtpReply checkedReply;
        FtpEndpoint endpoint = new FtpEndpoint();
        if (command.Kind == FtpCommandKind.Epsv)
        {
            FtpPassiveReply passive = FtpEndpointCodec.ParseExtendedPassiveReply(reply);
            checkedReply = passive.Reply;
            endpoint.Host = TargetControlPeerAddress.ToString();
            endpoint.Port = passive.Endpoint.Port;
            endpoint.AddressFamily = TargetControlFamily;
        }
        else if (command.Kind == FtpCommandKind.Pasv)
        {
            FtpPassiveReply passive = FtpEndpointCodec.ParsePassiveReply(reply);
            checkedReply = passive.Reply;
            IPAddress selectedAddress = SelectTargetPassiveAddress(passive.Endpoint);
            endpoint.Host = selectedAddress.ToString();
            endpoint.Port = passive.Endpoint.Port;
            endpoint.AddressFamily = FtpAddressFamily.IPv4;
        }
        else
        {
            throw new FtpProtocolException(
                "The pending target command is not PASV or EPSV.", null);
        }

        State.TargetPassiveSetupPending = false;
        State.TargetPassiveConfigured = true;
        State.TargetPassiveReply = checkedReply;
        State.PassiveEndpoint = endpoint;
        State.TargetCommandResults = AppendResult(
            State.TargetCommandResults,
            CreateSetupResult(command, checkedReply, true));
    }

    internal FtpCommand BeginSourceExtendedActiveSetup()
    {
        FtpEndpoint endpoint = RequirePassiveEndpoint();
        RequireFreshActiveSetup();
        if (!SourceUseExtendedActive)
        {
            throw new FtpConfigurationException(
                "Extended active mode is disabled for the source session.", null);
        }

        ValidateRelayedEndpoint(endpoint);
        FtpCommand command = CreateCommand(
            FtpCommandKind.Eprt,
            FtpCommandNames.Eprt,
            FtpEndpointCodec.FormatExtendedPortDefault(endpoint));
        SetPendingActiveCommand(command);
        return command;
    }

    internal FtpCommand BeginSourceActiveSetup()
    {
        FtpEndpoint endpoint = RequirePassiveEndpoint();
        RequireFreshActiveSetup();
        if (SourceUseExtendedActive)
        {
            throw new FtpConfigurationException(
                "Direct PORT setup requires extended active mode to be disabled.", null);
        }
        if (endpoint.AddressFamily != FtpAddressFamily.IPv4)
        {
            throw new FtpProtocolException(
                "PORT can relay only an IPv4 passive endpoint.", null);
        }

        ValidateRelayedEndpoint(endpoint);
        FtpCommand command = CreateCommand(
            FtpCommandKind.Port,
            FtpCommandNames.Port,
            FtpEndpointCodec.FormatPortArgument(endpoint));
        SetPendingActiveCommand(command);
        return command;
    }

    internal FtpCommand BeginSourceActiveFallback(FtpReply? extendedActiveReply)
    {
        FtpEndpoint endpoint = RequirePassiveEndpoint();
        if (!State.SourceActiveSetupPending)
        {
            throw new FtpProtocolException(
                "No source EPRT command awaits fallback.", null);
        }
        RequirePendingCommand(State.SourceActiveCommand, FtpCommandKind.Eprt, "EPRT");
        if (!SourceAllowActiveFallback)
        {
            throw new FtpProtocolException(
                "PORT fallback is not enabled for the source session.", null);
        }
        if (endpoint.AddressFamily != FtpAddressFamily.IPv4)
        {
            throw new FtpProtocolException(
                "PORT fallback is available only for an IPv4 passive endpoint.", null);
        }

        FtpReply failureReply = RequireUnsupportedReply(extendedActiveReply, "EPRT");
        FtpCommand failedCommand = RequireCommand(State.SourceActiveCommand, "EPRT");
        State.SourceCommandResults = AppendResult(
            State.SourceCommandResults,
            CreateSetupResult(failedCommand, failureReply, false));
        ValidateRelayedEndpoint(endpoint);
        FtpCommand command = CreateCommand(
            FtpCommandKind.Port,
            FtpCommandNames.Port,
            FtpEndpointCodec.FormatPortArgument(endpoint));
        SetPendingActiveCommand(command);
        return command;
    }

    internal void ApplySourceActiveSetupReply(FtpReply? reply)
    {
        if (!State.SourceActiveSetupPending)
        {
            throw new FtpProtocolException(
                "No source active setup command awaits a reply.", null);
        }

        FtpCommand command = RequireCommand(State.SourceActiveCommand, "active setup");
        FtpReply checkedReply = FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.CommandOkay, command.Verb);
        State.SourceActiveSetupPending = false;
        State.SourceActiveConfigured = true;
        State.SourceActiveReply = checkedReply;
        State.SourceCommandResults = AppendResult(
            State.SourceCommandResults,
            CreateSetupResult(command, checkedReply, true));
    }

    internal void BeginPairedTransfer(FtpCommand? sourceCommand, FtpCommand? targetCommand)
    {
        if (!State.TargetPassiveConfigured || !State.SourceActiveConfigured)
        {
            throw new FtpProtocolException(
                "Both sides of the third-party data connection must be configured before transfer.", null);
        }
        if (State.TransferStarted)
        {
            throw new FtpProtocolException(
                "Only one transfer can consume a third-party data setup.", null);
        }

        FtpCommand checkedSource = RequireCommand(sourceCommand, "source transfer");
        FtpCommand checkedTarget = RequireCommand(targetCommand, "target transfer");
        if (checkedSource.Kind != FtpCommandKind.Retr)
        {
            throw new FtpProtocolException(
                "The source side of a third-party transfer must use RETR.", null);
        }
        if (checkedTarget.Kind != FtpCommandKind.Stor &&
            checkedTarget.Kind != FtpCommandKind.Appe &&
            checkedTarget.Kind != FtpCommandKind.Stou)
        {
            throw new FtpProtocolException(
                "The target side of a third-party transfer must use STOR, APPE, or STOU.", null);
        }

        SourceReplies.BeginDataCommand(checkedSource);
        TargetReplies.BeginDataCommand(checkedTarget);
        State.TransferStarted = true;
        State.SourceTransferPending = true;
        State.TargetTransferPending = true;
    }

    internal FtpThirdPartyTransferState ApplySourceTransferReply(FtpReply? reply)
    {
        RequirePendingTransfer(true);
        FtpReplyCoordinationResult result = SourceReplies.ReceiveControlReply(reply);
        State.SourceConnectionClosed = result.ConnectionClosed;
        if (result.CompletedDataCommand != null)
        {
            State.SourceTransferResult = result.CompletedDataCommand;
            State.SourceTransferPending = false;
            State.SourceCommandResults = AppendResult(
                State.SourceCommandResults, result.CompletedDataCommand);
        }
        UpdateCompletion();
        return GetCurrentState();
    }

    internal FtpThirdPartyTransferState ApplyTargetTransferReply(FtpReply? reply)
    {
        RequirePendingTransfer(false);
        FtpReplyCoordinationResult result = TargetReplies.ReceiveControlReply(reply);
        State.TargetConnectionClosed = result.ConnectionClosed;
        if (result.CompletedDataCommand != null)
        {
            State.TargetTransferResult = result.CompletedDataCommand;
            State.TargetTransferPending = false;
            State.TargetCommandResults = AppendResult(
                State.TargetCommandResults, result.CompletedDataCommand);
        }
        UpdateCompletion();
        return GetCurrentState();
    }

    internal FtpThirdPartyTransferState GetCurrentState()
    {
        FtpThirdPartyTransferState snapshot = new FtpThirdPartyTransferState();
        snapshot.TargetPassiveSetupPending = State.TargetPassiveSetupPending;
        snapshot.TargetPassiveConfigured = State.TargetPassiveConfigured;
        snapshot.SourceActiveSetupPending = State.SourceActiveSetupPending;
        snapshot.SourceActiveConfigured = State.SourceActiveConfigured;
        snapshot.TransferStarted = State.TransferStarted;
        snapshot.SourceTransferPending = State.SourceTransferPending;
        snapshot.TargetTransferPending = State.TargetTransferPending;
        snapshot.SourceConnectionClosed = State.SourceConnectionClosed;
        snapshot.TargetConnectionClosed = State.TargetConnectionClosed;
        snapshot.Complete = State.Complete;
        snapshot.Succeeded = State.Succeeded;
        snapshot.PassiveEndpoint = CopyEndpoint(State.PassiveEndpoint);
        snapshot.TargetPassiveCommand = State.TargetPassiveCommand;
        snapshot.SourceActiveCommand = State.SourceActiveCommand;
        snapshot.TargetPassiveReply = State.TargetPassiveReply;
        snapshot.SourceActiveReply = State.SourceActiveReply;
        snapshot.SourceCommandResults = CopyResults(State.SourceCommandResults);
        snapshot.TargetCommandResults = CopyResults(State.TargetCommandResults);
        snapshot.SourceTransferResult = State.SourceTransferResult;
        snapshot.TargetTransferResult = State.TargetTransferResult;
        return snapshot;
    }

    internal void ResetOperation()
    {
        SourceReplies = new FtpReplyCoordinator();
        TargetReplies = new FtpReplyCoordinator();
        State = new FtpThirdPartyTransferState();
    }

    internal void SetPendingPassiveCommand(FtpCommand command)
    {
        State.TargetPassiveCommand = command;
        State.TargetPassiveReply = null;
        State.TargetPassiveSetupPending = true;
        State.TargetPassiveConfigured = false;
        State.PassiveEndpoint = null;
    }

    internal void SetPendingActiveCommand(FtpCommand command)
    {
        State.SourceActiveCommand = command;
        State.SourceActiveReply = null;
        State.SourceActiveSetupPending = true;
        State.SourceActiveConfigured = false;
    }

    internal void UpdateCompletion()
    {
        if (!State.TransferStarted || State.SourceTransferPending || State.TargetTransferPending ||
            State.SourceTransferResult == null || State.TargetTransferResult == null)
        {
            return;
        }

        State.Complete = true;
        State.Succeeded = State.SourceTransferResult.Succeeded &&
            State.TargetTransferResult.Succeeded;
    }

    internal void RequireFreshPassiveSetup()
    {
        if (State.TargetPassiveSetupPending || State.TargetPassiveConfigured ||
            State.SourceActiveSetupPending || State.SourceActiveConfigured || State.TransferStarted)
        {
            throw new FtpProtocolException(
                "The current third-party data setup must be reset before another passive command.", null);
        }
    }

    internal void RequireFreshActiveSetup()
    {
        if (State.SourceActiveSetupPending || State.SourceActiveConfigured || State.TransferStarted)
        {
            throw new FtpProtocolException(
                "The current third-party data setup must be reset before another active command.", null);
        }
    }

    internal FtpEndpoint RequirePassiveEndpoint()
    {
        if (!State.TargetPassiveConfigured || State.PassiveEndpoint == null)
        {
            throw new FtpProtocolException(
                "A successful target PASV or EPSV reply is required before source active setup.", null);
        }
        return State.PassiveEndpoint;
    }

    internal void RequirePendingTransfer(bool source)
    {
        if (!State.TransferStarted)
        {
            throw new FtpProtocolException(
                "No paired third-party transfer has started.", null);
        }
        if ((source && !State.SourceTransferPending) ||
            (!source && !State.TargetTransferPending))
        {
            throw new FtpProtocolException(
                "The selected third-party transfer session has no pending command.", null);
        }
    }

    internal void RequireIpv4TargetControlPeer()
    {
        if (TargetControlFamily != FtpAddressFamily.IPv4)
        {
            throw new FtpProtocolException(
                "PASV is available only for an IPv4 target control session.", null);
        }
    }

    internal void ValidateRelayedEndpoint(FtpEndpoint endpoint)
    {
        if (SourceRejectPrivilegedActivePorts && endpoint.Port < 1024)
        {
            throw new FtpProtocolException(
                "The relayed third-party data endpoint uses a prohibited privileged port.", null);
        }
    }

    internal IPAddress SelectTargetPassiveAddress(FtpEndpoint advertisedEndpoint)
    {
        int[] values = FtpEndpointCodec.ParseIpv4Address(
            advertisedEndpoint.Host, "PASV");
        IPAddress advertisedAddress = new IPAddress(new byte[]
        {
            (byte)values[0],
            (byte)values[1],
            (byte)values[2],
            (byte)values[3]
        });
        if (TargetIgnorePassiveAdvertisedAddress)
        {
            return TargetControlPeerAddress;
        }
        if (FtpPeerPolicy.IsProhibitedPassiveTarget(advertisedAddress))
        {
            throw new FtpConnectionException(
                "The target PASV reply advertised a prohibited data endpoint.", null);
        }

        bool advertisedPrivate = FtpPeerPolicy.IsPrivateOrLocalAddress(advertisedAddress);
        bool controlPrivate = FtpPeerPolicy.IsPrivateOrLocalAddress(TargetControlPeerAddress);
        if (TargetRejectPassiveAddressMismatch &&
            advertisedPrivate != controlPrivate)
        {
            throw new FtpConnectionException(
                "The target PASV reply crosses the configured private/public address boundary.", null);
        }
        return advertisedAddress;
    }

    internal static void ValidateConfiguration(
        IPAddress? targetControlPeerAddress,
        FtpDataConnectionOptions? sourceDataOptions,
        FtpDataConnectionOptions? targetDataOptions,
        FtpDataProtectionLevel sourceProtection,
        FtpDataProtectionLevel targetProtection)
    {
        if (targetControlPeerAddress == null ||
            sourceDataOptions == null || targetDataOptions == null)
        {
            throw new FtpConfigurationException(
                "Third-party transfer setup requires both session policies and the target control peer.", null);
        }

        FtpConfigurationValidator.ValidateDataConnectionOptions(sourceDataOptions);
        FtpConfigurationValidator.ValidateDataConnectionOptions(targetDataOptions);
        FtpPeerPolicy.GetAddressFamily(targetControlPeerAddress);
        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(targetControlPeerAddress))
        {
            throw new FtpConfigurationException(
                "The target control peer cannot be unspecified or multicast.", null);
        }
        if (!sourceDataOptions.AllowForeignDataConnections ||
            !targetDataOptions.AllowForeignDataConnections)
        {
            throw new FtpConfigurationException(
                "Both FTP sessions must explicitly permit foreign data connections for FXP.", null);
        }
        if (sourceProtection != FtpDataProtectionLevel.Clear ||
            targetProtection != FtpDataProtectionLevel.Clear)
        {
            throw new FtpConfigurationException(
                "RFC 959 third-party transfers require clear data connections on both sessions.", null);
        }
    }

    internal static FtpReply RequireUnsupportedReply(FtpReply? reply, string commandName)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireReply(reply, commandName);
        int code = checkedReply.Code;
        bool unsupported = code == FtpReplyCodes.SyntaxError ||
            code == FtpReplyCodes.SyntaxErrorInParameters ||
            code == FtpReplyCodes.CommandNotImplemented ||
            code == FtpReplyCodes.CommandParameterNotImplemented ||
            code == FtpReplyCodes.NetworkProtocolNotSupported;
        if (!unsupported)
        {
            throw new FtpProtocolException(
                $"{commandName} fallback requires a standards-defined unsupported reply.", null);
        }
        return checkedReply;
    }

    internal static void RequirePendingCommand(
        FtpCommand? command,
        FtpCommandKind expectedKind,
        string commandName)
    {
        FtpCommand checkedCommand = RequireCommand(command, commandName);
        if (checkedCommand.Kind != expectedKind)
        {
            throw new FtpProtocolException(
                $"No pending {commandName} command can be replaced by fallback.", null);
        }
    }

    internal static FtpCommand RequireCommand(FtpCommand? command, string commandName)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (string.IsNullOrEmpty(command.Verb))
        {
            throw new FtpProtocolException(
                $"The FTP {commandName} command verb is unavailable.", null);
        }
        return command;
    }

    internal static FtpCommand CreateCommand(
        FtpCommandKind kind,
        string verb,
        string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = verb;
        command.Argument = argument;
        return command;
    }

    internal static FtpCommandResult CreateSetupResult(
        FtpCommand command,
        FtpReply reply,
        bool succeeded)
    {
        return FtpReplyCoordinator.CreateCommandResult(
            command, new[] { reply }, reply, succeeded, false, null);
    }

    internal static FtpCommandResult[] AppendResult(
        FtpCommandResult[] values,
        FtpCommandResult value)
    {
        FtpCommandResult[] appended = new FtpCommandResult[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            appended[index] = values[index];
            index++;
        }
        appended[values.Length] = value;
        return appended;
    }

    internal static FtpCommandResult[] CopyResults(FtpCommandResult[] values)
    {
        FtpCommandResult[] copy = new FtpCommandResult[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index++;
        }
        return copy;
    }

    internal static FtpEndpoint? CopyEndpoint(FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            return null;
        }

        FtpEndpoint copy = new FtpEndpoint();
        copy.Host = endpoint.Host;
        copy.Port = endpoint.Port;
        copy.AddressFamily = endpoint.AddressFamily;
        return copy;
    }
}
