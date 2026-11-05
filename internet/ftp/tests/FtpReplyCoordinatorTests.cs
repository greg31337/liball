namespace ftp.Tests;

public class FtpReplyCoordinatorTests
{
    [Test]
    public void Preliminary_reply_keeps_data_command_pending_until_positive_completion()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        FtpCommand command = CreateCommand("RETR file.bin");
        FtpReply preliminary = CreateReply(150, "Opening data connection");
        FtpReply completion = CreateReply(226, "Transfer complete");
        coordinator.BeginDataCommand(command);

        FtpReplyCoordinationResult started = coordinator.ReceiveControlReply(preliminary);
        FtpReplyCoordinationResult finished = coordinator.ReceiveControlReply(completion);

        Assert.Multiple(() =>
        {
            Assert.That(started.DataCommandPending, Is.True);
            Assert.That(started.CompletedDataCommand, Is.Null);
            Assert.That(finished.DataCommandPending, Is.False);
            Assert.That(finished.CompletedDataCommand, Is.Not.Null);
            Assert.That(finished.CompletedDataCommand!.Command, Is.SameAs(command));
            Assert.That(finished.CompletedDataCommand.PreliminaryReply, Is.SameAs(preliminary));
            Assert.That(finished.CompletedDataCommand.FinalReply, Is.SameAs(completion));
            Assert.That(finished.CompletedDataCommand.Replies, Is.EqualTo(new[] { preliminary, completion }));
            Assert.That(finished.CompletedDataCommand.Succeeded, Is.True);
            Assert.That(coordinator.HasPendingDataCommand(), Is.False);
        });
    }

    [Test]
    public void Data_command_can_complete_or_fail_without_a_preliminary_reply()
    {
        FtpReplyCoordinator completedCoordinator = new FtpReplyCoordinator();
        completedCoordinator.BeginDataCommand(CreateCommand("NLST"));
        FtpReplyCoordinationResult completed = completedCoordinator.ReceiveControlReply(
            CreateReply(250, "Requested action okay"));

        FtpReplyCoordinator failedCoordinator = new FtpReplyCoordinator();
        failedCoordinator.BeginDataCommand(CreateCommand("STOR file.bin"));
        FtpReply failure = CreateReply(425, "Cannot open data connection");
        FtpReplyCoordinationResult failed = failedCoordinator.ReceiveControlReply(failure);

        Assert.Multiple(() =>
        {
            Assert.That(completed.CompletedDataCommand!.Succeeded, Is.True);
            Assert.That(completed.CompletedDataCommand.PreliminaryReply, Is.Null);
            Assert.That(failed.CompletedDataCommand!.Succeeded, Is.False);
            Assert.That(failed.CompletedDataCommand.FinalReply, Is.SameAs(failure));
            Assert.That(failed.DataCommandPending, Is.False);
        });
    }

    [Test]
    public void Service_unavailable_is_asynchronous_and_closes_an_idle_coordinator()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        FtpReply closure = CreateReply(421, "Service closing control connection");

        FtpReplyCoordinationResult result = coordinator.ReceiveControlReply(closure);

        Assert.Multiple(() =>
        {
            Assert.That(result.AsynchronousReply, Is.SameAs(closure));
            Assert.That(result.ConnectionClosed, Is.True);
            Assert.That(result.CompletedDataCommand, Is.Null);
            Assert.That(result.CompletedAbortCommand, Is.Null);
            Assert.That(coordinator.IsConnectionClosed(), Is.True);
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.ReceiveControlReply(CreateReply(200, "Late reply")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginDataCommand(CreateCommand("LIST")));
        });
    }

    [Test]
    public void Service_unavailable_completes_a_pending_data_command_and_marks_connection_closed()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        FtpCommand command = CreateCommand("MLSD /incoming");
        FtpReply preliminary = CreateReply(125, "Data connection already open");
        FtpReply closure = CreateReply(421, "Server shutting down");
        coordinator.BeginDataCommand(command);
        coordinator.ReceiveControlReply(preliminary);

        FtpReplyCoordinationResult result = coordinator.ReceiveControlReply(closure);

        Assert.Multiple(() =>
        {
            Assert.That(result.AsynchronousReply, Is.SameAs(closure));
            Assert.That(result.ConnectionClosed, Is.True);
            Assert.That(result.DataCommandPending, Is.False);
            Assert.That(result.CompletedDataCommand, Is.Not.Null);
            Assert.That(result.CompletedDataCommand!.Replies, Is.EqualTo(new[] { preliminary, closure }));
            Assert.That(result.CompletedDataCommand.FinalReply, Is.SameAs(closure));
            Assert.That(result.CompletedDataCommand.Succeeded, Is.False);
            Assert.That(result.CompletedDataCommand.ConnectionClosed, Is.True);
        });
    }

    [Test]
    public void Abort_426_then_226_completes_transfer_failure_before_abort_success()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        FtpCommand dataCommand = CreateCommand("RETR large.bin");
        FtpCommand abortCommand = CreateCommand("ABOR");
        FtpReply preliminary = CreateReply(150, "Opening data connection");
        FtpReply aborted = CreateReply(426, "Transfer aborted");
        FtpReply abortComplete = CreateReply(226, "Abort successful");
        coordinator.BeginDataCommand(dataCommand);
        coordinator.ReceiveControlReply(preliminary);
        coordinator.BeginAbortCommand(abortCommand);

        FtpReplyCoordinationResult first = coordinator.ReceiveControlReply(aborted);
        FtpReplyCoordinationResult second = coordinator.ReceiveControlReply(abortComplete);

        Assert.Multiple(() =>
        {
            Assert.That(first.CompletedDataCommand, Is.Not.Null);
            Assert.That(first.CompletedDataCommand!.Command, Is.SameAs(dataCommand));
            Assert.That(first.CompletedDataCommand.Replies, Is.EqualTo(new[] { preliminary, aborted }));
            Assert.That(first.CompletedDataCommand.Succeeded, Is.False);
            Assert.That(first.AbortCommandPending, Is.True);
            Assert.That(second.CompletedAbortCommand, Is.Not.Null);
            Assert.That(second.CompletedAbortCommand!.Command, Is.SameAs(abortCommand));
            Assert.That(second.CompletedAbortCommand.Replies, Is.EqualTo(new[] { aborted, abortComplete }));
            Assert.That(second.CompletedAbortCommand.FinalReply, Is.SameAs(abortComplete));
            Assert.That(second.CompletedAbortCommand.Succeeded, Is.True);
            Assert.That(second.AbortCommandPending, Is.False);
        });
    }

    [Test]
    public void Abort_can_complete_with_225_when_no_transfer_is_active()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        FtpCommand abortCommand = CreateCommand("ABOR");
        FtpReply noTransfer = CreateReply(225, "No transfer in progress");
        coordinator.BeginAbortCommand(abortCommand);

        FtpReplyCoordinationResult result = coordinator.ReceiveControlReply(noTransfer);

        Assert.Multiple(() =>
        {
            Assert.That(result.CompletedAbortCommand, Is.Not.Null);
            Assert.That(result.CompletedAbortCommand!.Succeeded, Is.True);
            Assert.That(result.CompletedAbortCommand.Replies, Is.EqualTo(new[] { noTransfer }));
            Assert.That(result.CompletedDataCommand, Is.Null);
            Assert.That(result.AbortCommandPending, Is.False);
        });
    }

    [Test]
    public void Single_226_abort_reply_marks_a_pending_transfer_as_aborted()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        FtpCommand dataCommand = CreateCommand("STOR partial.bin");
        coordinator.BeginDataCommand(dataCommand);
        FtpReply preliminary = CreateReply(150, "Opening data connection");
        coordinator.ReceiveControlReply(preliminary);
        coordinator.BeginAbortCommand(CreateCommand("ABOR"));
        FtpReply completion = CreateReply(226, "Abort command successful");

        FtpReplyCoordinationResult result = coordinator.ReceiveControlReply(completion);

        Assert.Multiple(() =>
        {
            Assert.That(result.CompletedDataCommand, Is.Not.Null);
            Assert.That(result.CompletedDataCommand!.Succeeded, Is.False);
            Assert.That(result.CompletedDataCommand.FailureMessage, Is.EqualTo("The FTP data command was aborted."));
            Assert.That(result.CompletedDataCommand.PreliminaryReply, Is.SameAs(preliminary));
            Assert.That(result.CompletedAbortCommand!.Succeeded, Is.True);
            Assert.That(result.DataCommandPending, Is.False);
            Assert.That(result.AbortCommandPending, Is.False);
        });
    }

    [Test]
    public void Failed_abort_leaves_the_data_command_pending_for_its_own_terminal_reply()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        coordinator.BeginDataCommand(CreateCommand("APPE log.txt"));
        coordinator.ReceiveControlReply(CreateReply(150, "Opening data connection"));
        coordinator.BeginAbortCommand(CreateCommand("ABOR"));
        FtpReply rejection = CreateReply(500, "ABOR not understood");

        FtpReplyCoordinationResult rejected = coordinator.ReceiveControlReply(rejection);
        FtpReplyCoordinationResult completed = coordinator.ReceiveControlReply(
            CreateReply(226, "Transfer complete"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.CompletedAbortCommand!.Succeeded, Is.False);
            Assert.That(rejected.DataCommandPending, Is.True);
            Assert.That(rejected.AbortCommandPending, Is.False);
            Assert.That(completed.CompletedDataCommand!.Succeeded, Is.True);
        });
    }

    [Test]
    public void Service_unavailable_during_abort_completes_both_pending_commands()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        coordinator.BeginDataCommand(CreateCommand("LIST"));
        coordinator.ReceiveControlReply(CreateReply(150, "Opening data connection"));
        coordinator.BeginAbortCommand(CreateCommand("ABOR"));
        FtpReply closure = CreateReply(421, "Server shutting down");

        FtpReplyCoordinationResult result = coordinator.ReceiveControlReply(closure);

        Assert.Multiple(() =>
        {
            Assert.That(result.ConnectionClosed, Is.True);
            Assert.That(result.CompletedDataCommand!.ConnectionClosed, Is.True);
            Assert.That(result.CompletedAbortCommand!.ConnectionClosed, Is.True);
            Assert.That(result.CompletedAbortCommand.FinalReply, Is.SameAs(closure));
            Assert.That(result.DataCommandPending, Is.False);
            Assert.That(result.AbortCommandPending, Is.False);
        });
    }

    [Test]
    public void Coordinator_rejects_invalid_commands_and_reply_ordering()
    {
        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginDataCommand(CreateCommand("NOOP")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginAbortCommand(CreateCommand("QUIT")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.ReceiveControlReply(CreateReply(200, "No pending command")));
        });

        coordinator.BeginDataCommand(CreateCommand("LIST"));
        coordinator.ReceiveControlReply(CreateReply(150, "Opening data connection"));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.BeginDataCommand(CreateCommand("NLST")));
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.ReceiveControlReply(CreateReply(125, "Another preliminary reply")));
        });

        FtpReplyCoordinator abortCoordinator = new FtpReplyCoordinator();
        abortCoordinator.BeginAbortCommand(CreateCommand("ABOR"));
        Assert.Throws<FtpProtocolException>(() =>
            abortCoordinator.ReceiveControlReply(CreateReply(200, "Unexpected positive reply")));

        FtpReply noTransfer = CreateReply(225, "No transfer in progress");
        FtpReplyCoordinationResult recovered = abortCoordinator.ReceiveControlReply(noTransfer);
        Assert.That(recovered.CompletedAbortCommand!.Replies, Is.EqualTo(new[] { noTransfer }));
    }

    private static FtpCommand CreateCommand(string text)
    {
        return FtpCommandParser.ParseDecodedCommand(text);
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpReplyClassifier.ApplyClassification(reply);
        return reply;
    }
}
