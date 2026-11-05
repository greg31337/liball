using System;

namespace ftp;

internal class FtpSessionStateMachine
{
    internal FtpSequenceState State;

    internal FtpSessionStateMachine()
    {
        State = new FtpSequenceState();
    }

    internal void OpenConnection()
    {
        if (State.SessionState != FtpSessionState.Disconnected &&
            State.SessionState != FtpSessionState.Closed)
        {
            throw new FtpProtocolException(
                "An FTP session can be opened only when disconnected or closed.", null);
        }

        ResetFreshSession();
        State.SessionState = FtpSessionState.Connected;
    }

    internal void ApplyCommandReply(FtpCommand? command, FtpReply? reply)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (State.SessionState == FtpSessionState.Disconnected ||
            State.SessionState == FtpSessionState.Closed)
        {
            throw new FtpProtocolException(
                "An FTP command reply cannot be applied without an open session.", null);
        }

        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            CloseConnection();
            return;
        }

        ClearConsumedOperationState(command.Kind);

        if (command.Kind == FtpCommandKind.Host)
        {
            ApplyHostReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Lang)
        {
            ApplyLanguageReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Auth)
        {
            ApplyAuthorizationReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Adat)
        {
            ApplySecurityDataReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.User)
        {
            ApplyUserReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Pass)
        {
            ApplyPasswordReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Acct)
        {
            ApplyAccountReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Rein)
        {
            if (reply.Code == FtpReplyCodes.ServiceReady)
            {
                ResetFreshSession();
                State.SessionState = FtpSessionState.Connected;
            }
            return;
        }
        if (command.Kind == FtpCommandKind.Pbsz)
        {
            ApplyProtectionBufferReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Prot)
        {
            ApplyProtectionReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Ccc)
        {
            return;
        }
        if (command.Kind == FtpCommandKind.Type)
        {
            ApplyRepresentationReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Stru)
        {
            ApplyStructureReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Mode)
        {
            ApplyModeReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Cwd ||
            command.Kind == FtpCommandKind.Xcwd)
        {
            ApplyWorkingDirectoryReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Cdup ||
            command.Kind == FtpCommandKind.Xcup)
        {
            ApplyParentDirectoryReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Pwd ||
            command.Kind == FtpCommandKind.Xpwd)
        {
            ApplyPrintWorkingDirectoryReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Rnfr)
        {
            ApplyRenameFromReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Rnto)
        {
            ClearRenameState();
            return;
        }
        if (command.Kind == FtpCommandKind.Rest)
        {
            ApplyRestartReply(command, reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Port || command.Kind == FtpCommandKind.Eprt ||
            command.Kind == FtpCommandKind.Pasv || command.Kind == FtpCommandKind.Epsv)
        {
            ApplyDataSetupReply(command, reply);
            return;
        }
        if (FtpReplyCoordinator.IsDataCommand(command.Kind))
        {
            ApplyDataCommandReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Abor)
        {
            ApplyAbortReply(reply);
            return;
        }
        if (command.Kind == FtpCommandKind.Quit &&
            reply.Code == FtpReplyCodes.ServiceClosingControlConnection)
        {
            State.QuitPending = true;
            State.SessionState = FtpSessionState.Closing;
        }
    }

    internal void CompleteSecurityExchange(bool controlProtected)
    {
        if (!State.SecurityExchangeInProgress)
        {
            throw new FtpProtocolException(
                "No FTP security exchange is awaiting completion.", null);
        }

        State.SecurityExchangeInProgress = false;
        State.SecurityExchangeComplete = true;
        State.ControlProtected = controlProtected;
        State.SessionState = ResolveUnauthenticatedState();
    }

    internal void CompleteImplicitTls()
    {
        if (State.SessionState != FtpSessionState.Connected ||
            State.SecurityExchangeInProgress || State.SecurityExchangeComplete)
        {
            throw new FtpProtocolException(
                "Implicit FTP TLS can complete only on a fresh connected session.", null);
        }

        State.SecurityExchangeComplete = true;
        State.SecurityExchangeIsTls = true;
        State.ControlProtected = true;
    }

    internal void CompleteClearCommandChannel(FtpCommand? command, FtpReply? reply)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (command.Kind != FtpCommandKind.Ccc ||
            reply.Code != FtpReplyCodes.CommandOkay ||
            !State.SecurityExchangeComplete || !State.SecurityExchangeIsTls ||
            !State.ControlProtected || State.ClearCommandChannelCompleted)
        {
            throw new FtpProtocolException(
                "The FTP control channel can be cleared only after an accepted protected TLS CCC exchange.",
                null);
        }

        State.ControlProtected = false;
        State.ClearCommandChannelCompleted = true;
    }

    internal void ApplyTransferReply(FtpReply? reply)
    {
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (!State.TransferInProgress)
        {
            throw new FtpProtocolException(
                "No FTP transfer is awaiting a terminal reply.", null);
        }

        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            CloseConnection();
            return;
        }
        if (reply.Code == FtpReplyCodes.ClosingDataConnection ||
            reply.Code == FtpReplyCodes.RequestedFileActionOkay || IsFailureReply(reply.Code))
        {
            CompleteTransferState();
        }
    }

    internal void CloseConnection()
    {
        ResetFreshSession();
        State.SessionState = FtpSessionState.Closed;
    }

    internal void ApplyHostReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code != FtpReplyCodes.ServiceReady)
        {
            return;
        }

        State.SelectedHost = command.Argument;
        State.Language = null;
        ResetAuthenticationState();
        ResetTransferParameters();
        ClearOperationState();
        State.SessionState = FtpSessionState.HostSelected;
    }

    internal void ApplyLanguageReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.CommandOkay)
        {
            State.Language = command.Argument == null ? null :
                FtpArgumentCodec.ParseLanguageTag(command.Argument);
        }
    }

    internal void ApplyAuthorizationReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code != FtpReplyCodes.SecurityExchangeComplete &&
            reply.Code != FtpReplyCodes.SecurityMechanismOkay)
        {
            return;
        }

        ResetAuthenticationState();
        ResetTransferParameters();
        ClearOperationState();
        State.SecurityExchangeComplete = false;
        State.SecurityExchangeIsTls = IsTlsMechanism(command.Argument);
        State.ControlProtected = false;
        State.ClearCommandChannelCompleted = false;
        State.SecurityExchangeInProgress = true;
        State.SessionState = FtpSessionState.Authenticating;

        if (reply.Code == FtpReplyCodes.SecurityExchangeComplete &&
            !IsTlsMechanism(command.Argument))
        {
            CompleteSecurityExchange(false);
        }
    }

    internal void ApplySecurityDataReply(FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.SecurityDataAcceptedNeedMore)
        {
            State.SecurityExchangeInProgress = true;
            State.SessionState = FtpSessionState.Authenticating;
            return;
        }
        if (reply.Code == FtpReplyCodes.SecurityExchangeCompleteWithData)
        {
            CompleteSecurityExchange(false);
            return;
        }
        if (reply.Code == FtpReplyCodes.UserLoggedInBySecurityExchange)
        {
            State.SecurityExchangeInProgress = false;
            State.SecurityExchangeComplete = true;
            State.SessionState = FtpSessionState.Authenticated;
        }
    }

    internal void ApplyUserReply(FtpCommand command, FtpReply reply)
    {
        ResetAuthenticationState();
        ClearOperationState();
        State.UserName = command.Argument;
        if (reply.Code == FtpReplyCodes.UserLoggedIn ||
            reply.Code == FtpReplyCodes.UserLoggedInBySecurityExchange)
        {
            CompleteAuthentication();
            return;
        }
        if (reply.Code == FtpReplyCodes.UserNameOkayNeedPassword ||
            reply.Code == FtpReplyCodes.UserNameOkayNeedChallengePassword)
        {
            State.UserNameSupplied = true;
            State.SessionState = FtpSessionState.Authenticating;
            return;
        }
        if (reply.Code == FtpReplyCodes.NeedAccountForLogin)
        {
            State.UserNameSupplied = true;
            State.AccountRequired = true;
            State.SessionState = FtpSessionState.Authenticating;
            return;
        }

        State.UserName = null;
        State.SessionState = ResolveUnauthenticatedState();
    }

    internal void ApplyPasswordReply(FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.UserLoggedIn ||
            reply.Code == FtpReplyCodes.UserLoggedInBySecurityExchange ||
            reply.Code == FtpReplyCodes.CommandSuperfluous)
        {
            CompleteAuthentication();
            return;
        }
        if (reply.Code == FtpReplyCodes.NeedAccountForLogin)
        {
            State.AccountRequired = true;
            State.SessionState = FtpSessionState.Authenticating;
        }
    }

    internal void ApplyAccountReply(FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.UserLoggedIn ||
            reply.Code == FtpReplyCodes.UserLoggedInBySecurityExchange ||
            reply.Code == FtpReplyCodes.CommandSuperfluous)
        {
            CompleteAuthentication();
        }
    }

    internal void ApplyProtectionReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code != FtpReplyCodes.CommandOkay || command.Argument == null ||
            !State.SecurityExchangeComplete || !State.ProtectionBufferSizeSet ||
            State.ClearCommandChannelCompleted ||
            (State.SecurityExchangeIsTls && !State.ControlProtected))
        {
            return;
        }

        if (FtpProtectionNegotiation.InspectProtectionLevel(
            command.Argument, State.SecurityExchangeIsTls) != 0)
        {
            return;
        }

        if (FtpProtectionNegotiation.IsPrivateLevel(command.Argument))
        {
            State.DataProtectionLevel = FtpDataProtectionLevel.Private;
        }
        else if (FtpProtectionNegotiation.IsClearLevel(command.Argument))
        {
            State.DataProtectionLevel = FtpDataProtectionLevel.Clear;
        }
    }

    internal void ApplyProtectionBufferReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.CommandOkay &&
            State.SecurityExchangeComplete && !State.ClearCommandChannelCompleted &&
            FtpProtectionNegotiation.InspectBufferSize(
                command.Argument, State.SecurityExchangeIsTls) == 0)
        {
            State.ProtectionBufferSizeSet = true;
        }
    }

    internal void ApplyRenameFromReply(FtpCommand command, FtpReply reply)
    {
        ClearRenameState();
        if (reply.Code == FtpReplyCodes.RequestedFileActionPending)
        {
            State.RenameSourceReady = true;
            State.RenameSource = command.Argument;
        }
    }

    internal void ApplyRestartReply(FtpCommand command, FtpReply reply)
    {
        ClearRestartState();
        if (reply.Code == FtpReplyCodes.RequestedFileActionPending)
        {
            if (State.TransferMode == FtpTransferMode.Stream ||
                State.TransferMode == FtpTransferMode.Deflate)
            {
                State.RestartMarker =
                    FtpArgumentCodec.ParseRestartMarker(command.Argument);
            }
            State.RestartMarkerText =
                FtpArgumentCodec.ValidateRestartMarker(command.Argument);
            State.HasRestartMarker = true;
        }
    }

    internal void ApplyDataSetupReply(FtpCommand command, FtpReply reply)
    {
        if (command.Kind == FtpCommandKind.Epsv && command.Argument != null &&
            string.Equals(command.Argument, "ALL", StringComparison.OrdinalIgnoreCase) &&
            reply.Code == FtpReplyCodes.CommandOkay)
        {
            ClearDataConnectionState();
            State.ExtendedPassiveOnly = true;
            return;
        }

        if (command.Kind == FtpCommandKind.Port && reply.Code == FtpReplyCodes.CommandOkay)
        {
            SetDataEndpoint(FtpEndpointCodec.ParsePortArgument(command.Argument), false, false);
        }
        else if (command.Kind == FtpCommandKind.Eprt && reply.Code == FtpReplyCodes.CommandOkay)
        {
            SetDataEndpoint(FtpEndpointCodec.ParseExtendedPortArgument(command.Argument), false, true);
        }
        else if (command.Kind == FtpCommandKind.Pasv &&
            reply.Code == FtpReplyCodes.EnteringPassiveMode)
        {
            SetDataEndpoint(
                FtpReplyExtractor.ExtractPassiveEndpoint(reply).Endpoint, true, false);
        }
        else if (command.Kind == FtpCommandKind.Epsv &&
            reply.Code == FtpReplyCodes.EnteringExtendedPassiveMode)
        {
            SetDataEndpoint(
                FtpReplyExtractor.ExtractExtendedPassiveEndpoint(reply).Endpoint, true, true);
        }
    }

    internal void ApplyDataCommandReply(FtpReply reply)
    {
        if (FtpReplyCoordinator.IsTransferPreliminaryReply(reply.Code))
        {
            State.TransferInProgress = true;
            State.SessionState = FtpSessionState.Transferring;
            return;
        }
        if (reply.Code == FtpReplyCodes.ClosingDataConnection ||
            reply.Code == FtpReplyCodes.RequestedFileActionOkay || IsFailureReply(reply.Code))
        {
            CompleteTransferState();
        }
    }

    internal void ApplyAbortReply(FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.ClosingDataConnection ||
            (reply.Code == FtpReplyCodes.DataConnectionOpenNoTransfer &&
             State.TransferInProgress))
        {
            CompleteTransferState();
        }
        else if (reply.Code == FtpReplyCodes.DataConnectionOpenNoTransfer)
        {
            ClearDataConnectionState();
            ClearRestartState();
        }
    }

    internal void CompleteAuthentication()
    {
        State.UserNameSupplied = false;
        State.AccountRequired = false;
        State.SessionState = FtpSessionState.Authenticated;
    }

    internal void ApplyRepresentationReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code != FtpReplyCodes.CommandOkay)
        {
            return;
        }

        FtpTransferOptions options =
            FtpTransferParameterCodec.ParseRepresentation(command.Argument);
        State.TransferType = options.Type;
        State.TextFormat = options.TextFormat;
        State.HasLocalByteSize = options.HasLocalByteSize;
        State.LocalByteSize = options.LocalByteSize;
    }

    internal void ApplyStructureReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.CommandOkay)
        {
            State.FileStructure =
                FtpTransferParameterCodec.ParseStructure(command.Argument);
        }
    }

    internal void ApplyModeReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.CommandOkay)
        {
            State.TransferMode =
                FtpTransferParameterCodec.ParseMode(command.Argument);
        }
    }

    internal void ApplyWorkingDirectoryReply(FtpCommand command, FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.RequestedFileActionOkay &&
            command.Argument != null)
        {
            State.CurrentDirectory = FtpPathCodec.ResolveWorkingPath(
                State.CurrentDirectory, command.Argument);
        }
    }

    internal void ApplyParentDirectoryReply(FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.CommandOkay ||
            reply.Code == FtpReplyCodes.RequestedFileActionOkay)
        {
            State.CurrentDirectory = FtpPathCodec.ResolveWorkingPath(
                State.CurrentDirectory, "..");
        }
    }

    internal void ApplyPrintWorkingDirectoryReply(FtpReply reply)
    {
        if (reply.Code == FtpReplyCodes.PathCreated)
        {
            State.CurrentDirectory = FtpReplyExtractor.ExtractCreatedPath(reply);
        }
    }

    internal void CompleteTransferState()
    {
        FtpSessionState priorState = State.SessionState;
        State.TransferInProgress = false;
        ClearDataConnectionState();
        ClearRestartState();
        if (State.QuitPending)
        {
            State.SessionState = FtpSessionState.Closing;
        }
        else if (priorState == FtpSessionState.Authenticated ||
            priorState == FtpSessionState.Transferring)
        {
            State.SessionState = FtpSessionState.Authenticated;
        }
    }

    internal void SetDataEndpoint(FtpEndpoint endpoint, bool passive, bool extended)
    {
        FtpEndpoint copy = new FtpEndpoint();
        copy.Host = endpoint.Host;
        copy.Port = endpoint.Port;
        copy.AddressFamily = endpoint.AddressFamily;
        State.DataEndpoint = copy;
        State.DataConnectionConfigured = true;
        State.PassiveDataConnection = passive;
        State.ExtendedDataConnection = extended;
    }

    internal void ClearConsumedOperationState(FtpCommandKind commandKind)
    {
        if (State.RenameSourceReady && commandKind != FtpCommandKind.Rnto)
        {
            ClearRenameState();
        }
        if (State.HasRestartMarker && commandKind != FtpCommandKind.Rest)
        {
            ClearRestartState();
        }
    }

    internal void ResetFreshSession()
    {
        State.SelectedHost = null;
        State.Language = null;
        ResetAuthenticationState();
        State.SecurityExchangeInProgress = false;
        State.SecurityExchangeComplete = false;
        State.SecurityExchangeIsTls = false;
        State.ControlProtected = false;
        State.ClearCommandChannelCompleted = false;
        ResetTransferParameters();
        ClearOperationState();
        State.QuitPending = false;
    }

    internal void ResetAuthenticationState()
    {
        State.UserName = null;
        State.Principal = null;
        State.UserNameSupplied = false;
        State.AccountRequired = false;
    }

    internal void ResetTransferParameters()
    {
        State.TransferType = FtpTransferType.Ascii;
        State.TextFormat = FtpTextFormat.NonPrint;
        State.FileStructure = FtpFileStructure.File;
        State.TransferMode = FtpTransferMode.Stream;
        State.HasLocalByteSize = false;
        State.LocalByteSize = 0;
        State.CurrentDirectory = "/";
        State.ProtectionBufferSizeSet = false;
        State.DataProtectionLevel = FtpDataProtectionLevel.Clear;
        State.ExtendedPassiveOnly = false;
    }

    internal void ClearOperationState()
    {
        ClearRenameState();
        ClearRestartState();
        ClearDataConnectionState();
        State.TransferInProgress = false;
    }

    internal void ClearRenameState()
    {
        State.RenameSourceReady = false;
        State.RenameSource = null;
    }

    internal void ClearRestartState()
    {
        State.HasRestartMarker = false;
        State.RestartMarker = 0;
        State.RestartMarkerText = null;
    }

    internal void ClearDataConnectionState()
    {
        State.DataConnectionConfigured = false;
        State.PassiveDataConnection = false;
        State.ExtendedDataConnection = false;
        State.DataEndpoint = null;
    }

    internal FtpSessionState ResolveUnauthenticatedState()
    {
        return State.SelectedHost == null ?
            FtpSessionState.Connected : FtpSessionState.HostSelected;
    }

    internal bool IsTlsMechanism(string? mechanism)
    {
        return string.Equals(mechanism, "TLS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mechanism, "TLS-C", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsFailureReply(int code)
    {
        return code >= 400 && code <= 599;
    }
}
