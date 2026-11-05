namespace ftp.Tests;

public class FtpCommandStateMatrixTests
{
    [Test]
    public void Every_catalog_command_has_the_expected_pre_login_sequence_result()
    {
        FtpSequenceState state = CreateState(FtpSessionState.Connected);

        AssertBeforeLoginMatrix(state, "connected before login");
    }

    [Test]
    public void Every_catalog_command_has_the_expected_authenticated_sequence_result()
    {
        FtpSequenceState state = CreateState(FtpSessionState.Authenticated);
        EnableCompletedTlsSecurity(state);
        state.RenameSourceReady = true;

        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSequenceValidationResult result =
                FtpSequenceValidator.InspectCommand(
                    CreateCommand(definition), state);
            bool rejected = definition.Kind == FtpCommandKind.Host ||
                definition.Kind == FtpCommandKind.Pass ||
                definition.Kind == FtpCommandKind.Acct ||
                definition.Kind == FtpCommandKind.Adat;

            AssertResult(
                result,
                !rejected,
                rejected ? FtpReplyCodes.BadCommandSequence : 0,
                "authenticated",
                definition);
        }
    }

    [Test]
    public void Rename_and_restart_pending_state_is_validated_and_consumed_by_every_next_command()
    {
        FtpSequenceState renameValidation =
            CreateState(FtpSessionState.Authenticated);
        FtpCommandDefinition rnto = RequireDefinition(FtpCommandKind.Rnto);
        FtpSequenceValidationResult earlyRename =
            FtpSequenceValidator.InspectCommand(
                CreateCommand(rnto), renameValidation);
        renameValidation.RenameSourceReady = true;
        FtpSequenceValidationResult readyRename =
            FtpSequenceValidator.InspectCommand(
                CreateCommand(rnto), renameValidation);

        AssertResult(
            earlyRename,
            false,
            FtpReplyCodes.BadCommandSequence,
            "rename without RNFR",
            rnto);
        AssertResult(readyRename, true, 0, "rename after RNFR", rnto);

        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSessionStateMachine machine = CreateAuthenticatedMachine();
            machine.State.RenameSourceReady = true;
            machine.State.RenameSource = "old.txt";
            machine.State.HasRestartMarker = true;
            machine.State.RestartMarker = 4096;
            machine.State.RestartMarkerText = "4096";

            machine.ApplyCommandReply(
                CreateCommand(definition),
                CreateReply(FtpReplyCodes.CommandNotImplemented,
                    "Rejected for matrix test"));

            Assert.Multiple(() =>
            {
                Assert.That(machine.State.RenameSourceReady, Is.False,
                    definition.Kind.ToString());
                Assert.That(machine.State.RenameSource, Is.Null,
                    definition.Kind.ToString());
                Assert.That(machine.State.HasRestartMarker, Is.False,
                    definition.Kind.ToString());
                Assert.That(machine.State.RestartMarkerText, Is.Null,
                    definition.Kind.ToString());
            });
        }

        FtpSessionStateMachine successful = CreateAuthenticatedMachine();
        successful.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Rnfr)),
            CreateReply(FtpReplyCodes.RequestedFileActionPending,
                "Rename destination required"));
        successful.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Rnto)),
            CreateReply(FtpReplyCodes.RequestedFileActionOkay, "Renamed"));
        successful.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Rest)),
            CreateReply(FtpReplyCodes.RequestedFileActionPending,
                "Restart accepted"));
        successful.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Retr)),
            CreateReply(
                FtpReplyCodes.FileStatusOkayOpeningDataConnection,
                "Opening data"));

        Assert.Multiple(() =>
        {
            Assert.That(successful.State.RenameSourceReady, Is.False);
            Assert.That(successful.State.HasRestartMarker, Is.False);
            Assert.That(successful.State.TransferInProgress, Is.True);
            Assert.That(successful.State.SessionState,
                Is.EqualTo(FtpSessionState.Transferring));
        });
    }

    [Test]
    public void Every_catalog_command_obeys_its_during_transfer_availability_flag()
    {
        FtpSequenceState state = CreateState(FtpSessionState.Transferring);
        state.TransferInProgress = true;
        EnableCompletedTlsSecurity(state);
        state.RenameSourceReady = true;

        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSequenceValidationResult result =
                FtpSequenceValidator.InspectCommand(
                    CreateCommand(definition), state);

            AssertResult(
                result,
                definition.AvailableDuringTransfer,
                definition.AvailableDuringTransfer ? 0 :
                    FtpReplyCodes.BadCommandSequence,
                "transfer in progress",
                definition);
        }

        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Stor)),
            CreateReply(
                FtpReplyCodes.FileStatusOkayOpeningDataConnection,
                "Opening data"));
        machine.ApplyTransferReply(
            CreateReply(FtpReplyCodes.ClosingDataConnection, "Complete"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.TransferInProgress, Is.False);
            Assert.That(machine.State.SessionState,
                Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(machine.State.DataConnectionConfigured, Is.False);
            Assert.That(machine.State.HasRestartMarker, Is.False);
        });
    }

    [Test]
    public void AUTH_and_CCC_define_complete_catalog_sequence_boundaries()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        FtpCommand auth =
            CreateCommand(RequireDefinition(FtpCommandKind.Auth));
        machine.ApplyCommandReply(
            auth,
            CreateReply(FtpReplyCodes.SecurityMechanismOkay, "Begin TLS"));

        Assert.That(machine.State.SecurityExchangeInProgress, Is.True);
        AssertSecurityExchangeMatrix(machine.State);

        machine.CompleteSecurityExchange(true);
        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.User)),
            CreateReply(FtpReplyCodes.UserLoggedIn, "Logged in"));
        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Pbsz)),
            CreateReply(FtpReplyCodes.CommandOkay, "PBSZ set"));
        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Prot)),
            CreateReply(FtpReplyCodes.CommandOkay, "Private data"));
        FtpCommand ccc =
            CreateCommand(RequireDefinition(FtpCommandKind.Ccc));
        FtpReply cccReply =
            CreateReply(FtpReplyCodes.CommandOkay, "Clear control");
        machine.ApplyCommandReply(ccc, cccReply);
        machine.CompleteClearCommandChannel(ccc, cccReply);
        machine.State.RenameSourceReady = true;

        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSequenceValidationResult result =
                FtpSequenceValidator.InspectCommand(
                    CreateCommand(definition), machine.State);
            bool badSequence = definition.Kind == FtpCommandKind.Host ||
                definition.Kind == FtpCommandKind.Pass ||
                definition.Kind == FtpCommandKind.Acct ||
                definition.Kind == FtpCommandKind.Adat ||
                definition.Kind == FtpCommandKind.Pbsz ||
                definition.Kind == FtpCommandKind.Prot;
            bool protectionDenied = definition.Kind == FtpCommandKind.Ccc;

            AssertResult(
                result,
                !badSequence && !protectionDenied,
                badSequence ? FtpReplyCodes.BadCommandSequence :
                    protectionDenied ?
                        FtpReplyCodes.CommandProtectionDenied : 0,
                "after CCC",
                definition);
        }

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SecurityExchangeComplete, Is.True);
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.ClearCommandChannelCompleted, Is.True);
            Assert.That(machine.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
        });
    }

    [Test]
    public void REIN_success_restores_the_complete_pre_login_command_matrix()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.State.SelectedHost = "ftp.example.test";
        machine.State.Language = "fr";
        EnableCompletedTlsSecurity(machine.State);
        machine.State.ClearCommandChannelCompleted = true;
        machine.State.RenameSourceReady = true;
        machine.State.HasRestartMarker = true;
        machine.State.DataConnectionConfigured = true;
        machine.State.ExtendedPassiveOnly = true;
        machine.State.TransferType = FtpTransferType.Image;
        machine.State.CurrentDirectory = "/work";

        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Rein)),
            CreateReply(FtpReplyCodes.ServiceReady, "Ready"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState,
                Is.EqualTo(FtpSessionState.Connected));
            Assert.That(machine.State.SelectedHost, Is.Null);
            Assert.That(machine.State.Language, Is.Null);
            Assert.That(machine.State.SecurityExchangeComplete, Is.False);
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.ClearCommandChannelCompleted, Is.False);
            Assert.That(machine.State.RenameSourceReady, Is.False);
            Assert.That(machine.State.HasRestartMarker, Is.False);
            Assert.That(machine.State.DataConnectionConfigured, Is.False);
            Assert.That(machine.State.ExtendedPassiveOnly, Is.False);
            Assert.That(machine.State.TransferType,
                Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(machine.State.CurrentDirectory, Is.EqualTo("/"));
        });
        AssertBeforeLoginMatrix(machine.State, "after REIN");
    }

    [Test]
    public void QUIT_rejects_the_complete_catalog_while_closing_and_after_close()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.State.TransferInProgress = true;
        machine.State.SessionState = FtpSessionState.Transferring;
        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.Quit)),
            CreateReply(FtpReplyCodes.ServiceClosingControlConnection,
                "Goodbye"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState,
                Is.EqualTo(FtpSessionState.Closing));
            Assert.That(machine.State.QuitPending, Is.True);
            Assert.That(machine.State.TransferInProgress, Is.True);
        });
        AssertUnavailableMatrix(machine.State, "QUIT pending");

        machine.ApplyTransferReply(
            CreateReply(FtpReplyCodes.ClosingDataConnection,
                "Transfer complete"));
        Assert.That(machine.State.SessionState,
            Is.EqualTo(FtpSessionState.Closing));
        machine.CloseConnection();

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState,
                Is.EqualTo(FtpSessionState.Closed));
            Assert.That(machine.State.QuitPending, Is.False);
            Assert.That(machine.State.TransferInProgress, Is.False);
        });
        AssertUnavailableMatrix(machine.State, "connection closed");
    }

    private static void AssertBeforeLoginMatrix(
        FtpSequenceState state,
        string phase)
    {
        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSequenceValidationResult result =
                FtpSequenceValidator.InspectCommand(
                    CreateCommand(definition), state);
            bool badSequence = definition.Kind == FtpCommandKind.Pass ||
                definition.Kind == FtpCommandKind.Acct ||
                definition.Kind == FtpCommandKind.Adat ||
                definition.Kind == FtpCommandKind.Pbsz ||
                definition.Kind == FtpCommandKind.Prot ||
                definition.Kind == FtpCommandKind.Ccc ||
                definition.Kind == FtpCommandKind.Mic ||
                definition.Kind == FtpCommandKind.Conf ||
                definition.Kind == FtpCommandKind.Enc;
            bool authenticationRequired =
                definition.RequiresAuthentication && !badSequence;

            AssertResult(
                result,
                !badSequence && !authenticationRequired,
                badSequence ? FtpReplyCodes.BadCommandSequence :
                    authenticationRequired ? FtpReplyCodes.NotLoggedIn : 0,
                phase,
                definition);
        }
    }

    private static void AssertSecurityExchangeMatrix(FtpSequenceState state)
    {
        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSequenceValidationResult result =
                FtpSequenceValidator.InspectCommand(
                    CreateCommand(definition), state);
            bool badSequence = definition.Kind == FtpCommandKind.Pass ||
                definition.Kind == FtpCommandKind.Acct ||
                definition.Kind == FtpCommandKind.Auth ||
                definition.Kind == FtpCommandKind.Pbsz ||
                definition.Kind == FtpCommandKind.Prot ||
                definition.Kind == FtpCommandKind.Ccc ||
                definition.Kind == FtpCommandKind.Mic ||
                definition.Kind == FtpCommandKind.Conf ||
                definition.Kind == FtpCommandKind.Enc;
            bool authenticationRequired =
                definition.RequiresAuthentication && !badSequence;
            bool allowed = definition.Kind == FtpCommandKind.Adat ||
                (!badSequence && !authenticationRequired);

            AssertResult(
                result,
                allowed,
                allowed ? 0 : badSequence ?
                    FtpReplyCodes.BadCommandSequence :
                    FtpReplyCodes.NotLoggedIn,
                "AUTH exchange",
                definition);
        }
    }

    private static void AssertUnavailableMatrix(
        FtpSequenceState state,
        string phase)
    {
        foreach (FtpCommandDefinition definition in
            FtpCommandCatalog.GetDefinitions())
        {
            FtpSequenceValidationResult result =
                FtpSequenceValidator.InspectCommand(
                    CreateCommand(definition), state);
            AssertResult(
                result,
                false,
                FtpReplyCodes.ServiceUnavailable,
                phase,
                definition);
        }
    }

    private static void AssertResult(
        FtpSequenceValidationResult result,
        bool allowed,
        int replyCode,
        string phase,
        FtpCommandDefinition definition)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Allowed, Is.EqualTo(allowed),
                phase + ": " + definition.Kind);
            Assert.That(result.ReplyCode, Is.EqualTo(replyCode),
                phase + ": " + definition.Kind);
            Assert.That(result.Definition.Kind, Is.EqualTo(definition.Kind),
                phase + ": " + definition.Kind);
        });
    }

    private static FtpSequenceState CreateState(FtpSessionState sessionState)
    {
        FtpSequenceState state = new FtpSequenceState();
        state.SessionState = sessionState;
        return state;
    }

    private static void EnableCompletedTlsSecurity(FtpSequenceState state)
    {
        state.SecurityExchangeComplete = true;
        state.SecurityExchangeIsTls = true;
        state.ControlProtected = true;
        state.ProtectionBufferSizeSet = true;
        state.DataProtectionLevel = FtpDataProtectionLevel.Private;
    }

    private static FtpSessionStateMachine CreateAuthenticatedMachine()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(RequireDefinition(FtpCommandKind.User)),
            CreateReply(FtpReplyCodes.UserLoggedIn, "Logged in"));
        return machine;
    }

    private static FtpCommandDefinition RequireDefinition(
        FtpCommandKind kind)
    {
        FtpCommandDefinition? definition =
            FtpCommandCatalog.FindByKind(kind);
        Assert.That(definition, Is.Not.Null, kind.ToString());
        return definition!;
    }

    private static FtpCommand CreateCommand(
        FtpCommandDefinition definition)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = definition.Kind;
        command.Verb = definition.Kind == FtpCommandKind.Unknown ?
            "X-VENDOR" : definition.Verb;
        command.Argument = BuildValidArgument(definition);
        return command;
    }

    private static string? BuildValidArgument(
        FtpCommandDefinition definition)
    {
        if (!definition.ArgumentRequired)
        {
            return null;
        }
        if (definition.Kind == FtpCommandKind.Rest ||
            definition.Kind == FtpCommandKind.Pbsz)
        {
            return "0";
        }
        if (definition.Kind == FtpCommandKind.Prot)
        {
            return "P";
        }
        if (definition.Kind == FtpCommandKind.Port)
        {
            return "192,0,2,1,8,1";
        }
        if (definition.Kind == FtpCommandKind.Eprt)
        {
            return "|1|192.0.2.1|2049|";
        }
        if (definition.Kind == FtpCommandKind.Type)
        {
            return "I";
        }
        if (definition.Kind == FtpCommandKind.Stru)
        {
            return "F";
        }
        if (definition.Kind == FtpCommandKind.Mode)
        {
            return "S";
        }
        if (definition.Kind == FtpCommandKind.Lang)
        {
            return "en";
        }
        if (definition.Kind == FtpCommandKind.Auth)
        {
            return "TLS";
        }
        return "value";
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        return reply;
    }
}
