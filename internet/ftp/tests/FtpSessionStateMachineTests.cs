namespace ftp.Tests;

public class FtpSessionStateMachineTests
{
    [Test]
    public void Opening_connection_establishes_all_protocol_defaults()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();

        machine.OpenConnection();

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Connected));
            Assert.That(machine.State.SelectedHost, Is.Null);
            Assert.That(machine.State.UserName, Is.Null);
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.DataConnectionConfigured, Is.False);
            Assert.That(machine.State.TransferInProgress, Is.False);
            Assert.That(machine.State.TransferType, Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(machine.State.TextFormat, Is.EqualTo(FtpTextFormat.NonPrint));
            Assert.That(machine.State.FileStructure, Is.EqualTo(FtpFileStructure.File));
            Assert.That(machine.State.TransferMode, Is.EqualTo(FtpTransferMode.Stream));
            Assert.That(machine.State.CurrentDirectory, Is.EqualTo("/"));
            Assert.Throws<FtpProtocolException>(() => machine.OpenConnection());
        });
    }

    [Test]
    public void HOST_selects_the_latest_host_and_resets_language_login_and_transfer_state()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(CreateCommand(FtpCommandKind.Lang, "fr"), CreateReply(200, "French"));
        machine.State.UserName = "old-user";
        machine.State.UserNameSupplied = true;
        machine.State.TransferType = FtpTransferType.Image;
        machine.State.CurrentDirectory = "/old";
        machine.State.HasRestartMarker = true;
        machine.State.RestartMarker = 20;

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Host, "Files.Example"), CreateReply(220, "Host ready"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.HostSelected));
            Assert.That(machine.State.SelectedHost, Is.EqualTo("Files.Example"));
            Assert.That(machine.State.Language, Is.Null);
            Assert.That(machine.State.UserName, Is.Null);
            Assert.That(machine.State.UserNameSupplied, Is.False);
            Assert.That(machine.State.TransferType, Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(machine.State.CurrentDirectory, Is.EqualTo("/"));
            Assert.That(machine.State.HasRestartMarker, Is.False);
        });

        machine.ApplyCommandReply(CreateCommand(FtpCommandKind.Lang, "EN-us"), CreateReply(200, "English"));
        Assert.That(machine.State.Language, Is.EqualTo("en-us"));
    }

    [Test]
    public void AUTH_TLS_resets_session_parameters_before_handshake_completion()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.State.SelectedHost = "ftp.example";
        machine.State.TransferType = FtpTransferType.LocalByte;
        machine.State.HasLocalByteSize = true;
        machine.State.LocalByteSize = 16;
        machine.State.CurrentDirectory = "/private";
        machine.State.RenameSourceReady = true;
        machine.State.RenameSource = "old";
        machine.State.DataConnectionConfigured = true;

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "TLS"), CreateReply(234, "Begin TLS"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Authenticating));
            Assert.That(machine.State.SecurityExchangeInProgress, Is.True);
            Assert.That(machine.State.SecurityExchangeComplete, Is.False);
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.SelectedHost, Is.EqualTo("ftp.example"));
            Assert.That(machine.State.UserName, Is.Null);
            Assert.That(machine.State.TransferType, Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(machine.State.HasLocalByteSize, Is.False);
            Assert.That(machine.State.CurrentDirectory, Is.EqualTo("/"));
            Assert.That(machine.State.RenameSourceReady, Is.False);
            Assert.That(machine.State.DataConnectionConfigured, Is.False);
        });

        machine.CompleteSecurityExchange(true);
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.HostSelected));
            Assert.That(machine.State.SecurityExchangeComplete, Is.True);
            Assert.That(machine.State.ControlProtected, Is.True);
        });
    }

    [Test]
    public void AUTH_ADAT_exchange_retains_host_and_completes_only_on_terminal_security_reply()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Host, "ftp.example"), CreateReply(220, "Selected"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "GSSAPI"), CreateReply(334, "Continue"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Adat, "first"), CreateReply(335, "More"));

        Assert.That(machine.State.SecurityExchangeInProgress, Is.True);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Adat, "second"), CreateReply(235, "Complete"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.HostSelected));
            Assert.That(machine.State.SelectedHost, Is.EqualTo("ftp.example"));
            Assert.That(machine.State.SecurityExchangeInProgress, Is.False);
            Assert.That(machine.State.SecurityExchangeComplete, Is.True);
            Assert.That(machine.State.ControlProtected, Is.False);
        });
    }

    [Test]
    public void USER_PASS_and_ACCT_follow_intermediate_reply_state()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Host, "ftp.example"), CreateReply(220, "Selected"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.User, "alice"), CreateReply(331, "Password"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Authenticating));
            Assert.That(machine.State.UserName, Is.EqualTo("alice"));
            Assert.That(machine.State.UserNameSupplied, Is.True);
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pass, "secret"), CreateReply(332, "Account"));
        Assert.That(machine.State.AccountRequired, Is.True);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Acct, "billing"), CreateReply(230, "Logged in"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(machine.State.UserName, Is.EqualTo("alice"));
            Assert.That(machine.State.UserNameSupplied, Is.False);
            Assert.That(machine.State.AccountRequired, Is.False);
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.User, "unknown"), CreateReply(530, "Denied"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.HostSelected));
            Assert.That(machine.State.UserName, Is.Null);
        });
    }

    [Test]
    public void PBSZ_PROT_and_CCC_retain_data_protection_but_prevent_renegotiation_after_clear()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "TLS"), CreateReply(234, "Begin TLS"));
        machine.CompleteSecurityExchange(true);
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pbsz, "0"), CreateReply(200, "PBSZ set"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "P"), CreateReply(200, "Private"));
        FtpCommand clearCommand = CreateCommand(FtpCommandKind.Ccc, null);
        FtpReply clearReply = CreateReply(200, "Clear");
        machine.ApplyCommandReply(clearCommand, clearReply);

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.ControlProtected, Is.True);
            Assert.That(machine.State.ClearCommandChannelCompleted, Is.False);
        });

        machine.CompleteClearCommandChannel(
            clearCommand, clearReply);

        FtpSequenceValidationResult pbsz = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "0"), machine.State);
        FtpSequenceValidationResult prot = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "C"), machine.State);

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.True);
            Assert.That(machine.State.DataProtectionLevel, Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.SecurityExchangeComplete, Is.True);
            Assert.That(machine.State.ClearCommandChannelCompleted, Is.True);
            Assert.That(pbsz.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(prot.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
        });
    }

    [Test]
    public void REIN_restores_post_connect_state_including_HOST_TLS_and_transfer_defaults()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.State.SelectedHost = "ftp.example";
        machine.State.Language = "fr";
        machine.State.SecurityExchangeComplete = true;
        machine.State.ControlProtected = true;
        machine.State.ProtectionBufferSizeSet = true;
        machine.State.DataProtectionLevel = FtpDataProtectionLevel.Private;
        machine.State.TransferType = FtpTransferType.Image;
        machine.State.CurrentDirectory = "/work";
        machine.State.ExtendedPassiveOnly = true;

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Rein, null), CreateReply(220, "Ready for new user"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Connected));
            Assert.That(machine.State.SelectedHost, Is.Null);
            Assert.That(machine.State.Language, Is.Null);
            Assert.That(machine.State.UserName, Is.Null);
            Assert.That(machine.State.SecurityExchangeComplete, Is.False);
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.False);
            Assert.That(machine.State.DataProtectionLevel, Is.EqualTo(FtpDataProtectionLevel.Clear));
            Assert.That(machine.State.TransferType, Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(machine.State.CurrentDirectory, Is.EqualTo("/"));
            Assert.That(machine.State.ExtendedPassiveOnly, Is.False);
        });
    }

    [Test]
    public void Rename_and_restart_state_are_one_command_transactions()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Rnfr, "old.txt"), CreateReply(350, "Ready"));
        Assert.That(machine.State.RenameSource, Is.EqualTo("old.txt"));

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Noop, null), CreateReply(200, "Okay"));
        Assert.That(machine.State.RenameSourceReady, Is.False);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Rnfr, "old.txt"), CreateReply(350, "Ready"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Rnto, "new.txt"), CreateReply(250, "Renamed"));
        Assert.That(machine.State.RenameSource, Is.Null);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Rest, "4096"), CreateReply(350, "Saved"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.HasRestartMarker, Is.True);
            Assert.That(machine.State.RestartMarker, Is.EqualTo(4096));
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Retr, "file.bin"), CreateReply(150, "Opening"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.HasRestartMarker, Is.False);
            Assert.That(machine.State.TransferInProgress, Is.True);
        });
    }

    [Test]
    public void Data_setup_is_replaced_and_cleared_after_transfer_or_EPSV_ALL()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pasv, null),
            CreateReply(227, "Entering Passive Mode (192,0,2,10,8,1)"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.DataConnectionConfigured, Is.True);
            Assert.That(machine.State.PassiveDataConnection, Is.True);
            Assert.That(machine.State.DataEndpoint!.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(machine.State.DataEndpoint.Port, Is.EqualTo(2049));
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Eprt, "|1|198.51.100.7|3000|"),
            CreateReply(200, "Active"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.PassiveDataConnection, Is.False);
            Assert.That(machine.State.ExtendedDataConnection, Is.True);
            Assert.That(machine.State.DataEndpoint!.Host, Is.EqualTo("198.51.100.7"));
            Assert.That(machine.State.DataEndpoint.Port, Is.EqualTo(3000));
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Retr, "file.bin"), CreateReply(150, "Opening"));
        machine.ApplyTransferReply(CreateReply(226, "Complete"));
        Assert.That(machine.State.DataConnectionConfigured, Is.False);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Epsv, "ALL"), CreateReply(200, "EPSV only"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.ExtendedPassiveOnly, Is.True);
            Assert.That(machine.State.DataEndpoint, Is.Null);
        });
    }

    [Test]
    public void ABOR_clears_transfer_state_only_on_its_terminal_completion()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Port, "192,0,2,1,8,1"), CreateReply(200, "Active"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Stor, "file.bin"), CreateReply(150, "Opening"));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Abor, null), CreateReply(426, "Aborted"));

        Assert.That(machine.State.TransferInProgress, Is.True);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Abor, null), CreateReply(226, "Abort complete"));
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.TransferInProgress, Is.False);
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(machine.State.DataConnectionConfigured, Is.False);
        });
    }

    [Test]
    public void QUIT_and_421_move_to_closing_or_closed_and_clear_sensitive_state()
    {
        FtpSessionStateMachine machine = CreateAuthenticatedMachine();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Quit, null), CreateReply(221, "Goodbye"));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Closing));
            Assert.That(machine.State.QuitPending, Is.True);
        });

        machine.CloseConnection();
        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Closed));
            Assert.That(machine.State.QuitPending, Is.False);
            Assert.That(machine.State.UserName, Is.Null);
        });

        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Noop, null), CreateReply(421, "Closing"));
        Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Closed));
    }

    [Test]
    public void State_machine_rejects_events_without_their_required_pending_state()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => machine.ApplyCommandReply(
                CreateCommand(FtpCommandKind.Noop, null), CreateReply(200, "Okay")));
            Assert.Throws<FtpProtocolException>(() => machine.CompleteSecurityExchange(true));
            Assert.Throws<FtpProtocolException>(() => machine.ApplyTransferReply(
                CreateReply(226, "Complete")));
            Assert.Throws<ArgumentNullException>(() => machine.ApplyCommandReply(
                null, CreateReply(200, "Okay")));
            Assert.Throws<ArgumentNullException>(() => machine.ApplyTransferReply(null));
        });
    }

    private static FtpSessionStateMachine CreateAuthenticatedMachine()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.User, "alice"), CreateReply(230, "Logged in"));
        return machine;
    }

    private static FtpCommand CreateCommand(FtpCommandKind kind, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = FtpCommandCatalog.ResolveVerb(kind);
        command.Argument = argument;
        return command;
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        return reply;
    }
}
