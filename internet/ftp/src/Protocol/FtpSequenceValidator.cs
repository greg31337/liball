namespace ftp;

internal static class FtpSequenceValidator
{
    internal static FtpSequenceValidationResult InspectCommand(
        FtpCommand? command, FtpSequenceState? state)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }
        if (state == null)
        {
            throw new System.ArgumentNullException("state");
        }

        ValidateSessionState(state.SessionState);
        FtpCommandDefinition? definition = ResolveDefinition(command);
        if (definition == null)
        {
            return RejectCommand(
                new FtpCommandDefinition(), FtpReplyCodes.SyntaxError,
                "The FTP command kind or verb is not recognized by the command catalog.");
        }

        if (definition.ArgumentRequired && command.Argument == null)
        {
            return RejectCommand(
                definition, FtpReplyCodes.SyntaxErrorInParameters,
                "The FTP command requires an argument.");
        }
        if (!definition.ArgumentAllowed && command.Argument != null)
        {
            return RejectCommand(
                definition, FtpReplyCodes.SyntaxErrorInParameters,
                "The FTP command does not accept an argument.");
        }

        if (definition.Kind == FtpCommandKind.Rest &&
            (state.TransferMode == FtpTransferMode.Stream ||
             state.TransferMode == FtpTransferMode.Deflate))
        {
            FtpArgumentCodec.ParseRestartMarker(command.Argument);
        }

        if (state.SessionState == FtpSessionState.Disconnected ||
            state.SessionState == FtpSessionState.Closing ||
            state.SessionState == FtpSessionState.Closed)
        {
            return RejectCommand(
                definition, FtpReplyCodes.ServiceUnavailable,
                "The FTP control connection is not accepting commands.");
        }

        if (state.SessionState == FtpSessionState.Transferring &&
            !definition.AvailableDuringTransfer)
        {
            return RejectCommand(
                definition, FtpReplyCodes.BadCommandSequence,
                "The FTP command cannot be issued while a transfer is in progress.");
        }

        if (state.ExtendedPassiveOnly &&
            (definition.Kind == FtpCommandKind.Port ||
             definition.Kind == FtpCommandKind.Pasv ||
             definition.Kind == FtpCommandKind.Eprt ||
             definition.Kind == FtpCommandKind.Lprt ||
             definition.Kind == FtpCommandKind.Lpsv))
        {
            return RejectCommand(
                definition, FtpReplyCodes.BadCommandSequence,
                "EPSV ALL permits only EPSV data-connection setup commands.");
        }

        if (definition.Kind == FtpCommandKind.Host && IsLoggedInState(state.SessionState))
        {
            return RejectCommand(
                definition, FtpReplyCodes.BadCommandSequence,
                "HOST must be issued before the user is authenticated.");
        }

        if (definition.Kind == FtpCommandKind.Pass)
        {
            if (state.SessionState != FtpSessionState.Authenticating ||
                !state.UserNameSupplied || state.SecurityExchangeInProgress)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "PASS must follow USER when a password is requested.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Acct)
        {
            if (state.SessionState != FtpSessionState.Authenticating || !state.AccountRequired)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "ACCT must follow a reply that requests account information.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Adat)
        {
            if (state.SessionState != FtpSessionState.Authenticating ||
                !state.SecurityExchangeInProgress)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "ADAT must follow AUTH while a security-data exchange is in progress.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Auth)
        {
            if (state.SecurityExchangeInProgress)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "AUTH cannot interrupt a security-data exchange.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Pbsz)
        {
            if (!state.SecurityExchangeComplete || state.ClearCommandChannelCompleted)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "PBSZ requires an active security association before CCC.");
            }
            int bufferReplyCode = FtpProtectionNegotiation.InspectBufferSize(
                command.Argument, state.SecurityExchangeIsTls);
            if (bufferReplyCode != 0)
            {
                return RejectCommand(
                    definition, bufferReplyCode,
                    state.SecurityExchangeIsTls ?
                        "FTP over TLS requires a PBSZ value of zero." :
                        "PBSZ requires a decimal value from 1 through 4294967295.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Prot)
        {
            if (!state.SecurityExchangeComplete || !state.ProtectionBufferSizeSet ||
                state.ClearCommandChannelCompleted ||
                (state.SecurityExchangeIsTls && !state.ControlProtected))
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "PROT requires a protected control channel and a completed PBSZ negotiation.");
            }
            int protectionReplyCode = FtpProtectionNegotiation.InspectProtectionLevel(
                command.Argument, state.SecurityExchangeIsTls);
            if (protectionReplyCode != 0)
            {
                return RejectCommand(
                    definition, protectionReplyCode,
                    protectionReplyCode == FtpReplyCodes.ProtectionLevelNotSupported ?
                        "FTP over TLS supports only PROT C and PROT P." :
                        "The PROT argument must be one recognized protection-level code.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Ccc)
        {
            if (!state.SecurityExchangeComplete)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "CCC requires a completed security exchange.");
            }
            if (!state.ControlProtected)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.CommandProtectionDenied,
                    "CCC itself must be protected.");
            }
            return AllowCommand(definition);
        }

        if (definition.Kind == FtpCommandKind.Mic ||
            definition.Kind == FtpCommandKind.Conf ||
            definition.Kind == FtpCommandKind.Enc)
        {
            if (!state.SecurityExchangeComplete)
            {
                return RejectCommand(
                    definition, FtpReplyCodes.BadCommandSequence,
                    "Protected commands require a completed security exchange.");
            }
            return AllowCommand(definition);
        }

        if (definition.RequiresAuthentication && !IsLoggedInState(state.SessionState))
        {
            return RejectCommand(
                definition, FtpReplyCodes.NotLoggedIn,
                "The FTP command requires an authenticated session.");
        }

        if (definition.Kind == FtpCommandKind.Rnto && !state.RenameSourceReady)
        {
            return RejectCommand(
                definition, FtpReplyCodes.BadCommandSequence,
                "RNTO must follow a successful RNFR command.");
        }

        return AllowCommand(definition);
    }

    internal static void RequireAllowedCommand(FtpCommand? command, FtpSequenceState? state)
    {
        FtpSequenceValidationResult result = InspectCommand(command, state);
        if (!result.Allowed)
        {
            throw new FtpProtocolException(
                result.ReplyCode.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " " + result.Message, null);
        }
    }

    internal static FtpCommandDefinition? ResolveDefinition(FtpCommand command)
    {
        if (command.Kind != FtpCommandKind.Unknown)
        {
            return FtpCommandCatalog.FindByKind(command.Kind);
        }

        FtpCommandDefinition? recognized = FtpCommandCatalog.FindByVerb(command.Verb);
        if (recognized != null)
        {
            return recognized;
        }
        if (!IsValidExtensionVerb(command.Verb))
        {
            return null;
        }
        return FtpCommandCatalog.FindByKind(FtpCommandKind.Unknown);
    }

    internal static bool IsValidExtensionVerb(string? verb)
    {
        if (string.IsNullOrEmpty(verb))
        {
            return false;
        }

        int index = 0;
        while (index < verb.Length)
        {
            char character = verb[index];
            bool letter = (character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z');
            bool digit = character >= '0' && character <= '9';
            if ((!letter && (index == 0 || (!digit && character != '-'))) || character > 127)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool IsLoggedInState(FtpSessionState state)
    {
        return state == FtpSessionState.Authenticated || state == FtpSessionState.Transferring;
    }

    internal static FtpSequenceValidationResult AllowCommand(FtpCommandDefinition definition)
    {
        FtpSequenceValidationResult result = new FtpSequenceValidationResult();
        result.Allowed = true;
        result.ReplyCode = 0;
        result.Message = "";
        result.Definition = definition;
        return result;
    }

    internal static FtpSequenceValidationResult RejectCommand(
        FtpCommandDefinition definition, int replyCode, string message)
    {
        FtpSequenceValidationResult result = new FtpSequenceValidationResult();
        result.ReplyCode = replyCode;
        result.Message = message;
        result.Definition = definition;
        return result;
    }

    internal static void ValidateSessionState(FtpSessionState state)
    {
        if (state < FtpSessionState.Disconnected || state > FtpSessionState.Closed)
        {
            throw new FtpConfigurationException("The FTP session state is invalid.", null);
        }
    }
}
