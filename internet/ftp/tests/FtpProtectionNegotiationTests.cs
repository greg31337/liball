namespace ftp.Tests;

public class FtpProtectionNegotiationTests
{
    [Test]
    public void TLS_PBSZ_requires_a_completed_exchange_and_a_numeric_zero()
    {
        FtpSequenceState state = new FtpSequenceState();
        state.SessionState = FtpSessionState.Connected;
        state.SecurityExchangeIsTls = true;
        FtpSequenceValidationResult early = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "0"), state);
        state.SecurityExchangeComplete = true;

        FtpSequenceValidationResult zero = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "0"), state);
        FtpSequenceValidationResult paddedZero = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "000"), state);
        FtpSequenceValidationResult nonzero = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "1"), state);
        FtpSequenceValidationResult malformed = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "zero"), state);
        FtpSequenceValidationResult overflow = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "4294967296"), state);

        Assert.Multiple(() =>
        {
            Assert.That(early.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(zero.Allowed, Is.True);
            Assert.That(paddedZero.Allowed, Is.True);
            Assert.That(nonzero.ReplyCode,
                Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
            Assert.That(malformed.ReplyCode,
                Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
            Assert.That(overflow.ReplyCode,
                Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
        });
    }

    [Test]
    public void TLS_PROT_requires_PBSZ_and_accepts_only_clear_or_private_levels()
    {
        FtpSequenceState state = new FtpSequenceState();
        state.SessionState = FtpSessionState.Connected;
        state.SecurityExchangeComplete = true;
        state.SecurityExchangeIsTls = true;
        state.ControlProtected = true;
        FtpSequenceValidationResult early = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "P"), state);
        state.ProtectionBufferSizeSet = true;

        FtpSequenceValidationResult clear = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "C"), state);
        FtpSequenceValidationResult privateLevel = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "p"), state);
        FtpSequenceValidationResult safe = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "S"), state);
        FtpSequenceValidationResult confidential = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "E"), state);
        FtpSequenceValidationResult unknown = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "X"), state);
        FtpSequenceValidationResult longCode = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "PP"), state);
        state.ControlProtected = false;
        FtpSequenceValidationResult unprotected = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "C"), state);
        state.ClearCommandChannelCompleted = true;
        FtpSequenceValidationResult afterClear = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "C"), state);

        Assert.Multiple(() =>
        {
            Assert.That(early.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(clear.Allowed, Is.True);
            Assert.That(privateLevel.Allowed, Is.True);
            Assert.That(safe.ReplyCode,
                Is.EqualTo(FtpReplyCodes.ProtectionLevelNotSupported));
            Assert.That(confidential.ReplyCode,
                Is.EqualTo(FtpReplyCodes.ProtectionLevelNotSupported));
            Assert.That(unknown.ReplyCode,
                Is.EqualTo(FtpReplyCodes.CommandParameterNotImplemented));
            Assert.That(longCode.ReplyCode,
                Is.EqualTo(FtpReplyCodes.CommandParameterNotImplemented));
            Assert.That(unprotected.ReplyCode,
                Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(afterClear.ReplyCode,
                Is.EqualTo(FtpReplyCodes.BadCommandSequence));
        });
    }

    [Test]
    public void Accepted_PBSZ_and_PROT_replies_change_state_only_in_valid_order()
    {
        FtpSessionStateMachine machine = CreateTlsMachine();

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "P"), CreateReply(200));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pbsz, "1"), CreateReply(200));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.False);
            Assert.That(machine.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Clear));
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pbsz, "0"), CreateReply(534));
        Assert.That(machine.State.ProtectionBufferSizeSet, Is.False);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pbsz, "000"), CreateReply(200));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "P"), CreateReply(200));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.True);
            Assert.That(machine.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "C"), CreateReply(534));
        Assert.That(machine.State.DataProtectionLevel,
            Is.EqualTo(FtpDataProtectionLevel.Private));

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "c"), CreateReply(200));
        Assert.That(machine.State.DataProtectionLevel,
            Is.EqualTo(FtpDataProtectionLevel.Clear));
    }

    [Test]
    public void Rejected_AUTH_retains_PROT_but_accepted_AUTH_and_REIN_reset_it()
    {
        FtpSessionStateMachine machine = CreateTlsMachine();
        SelectPrivateProtection(machine);

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "TLS"), CreateReply(534));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SecurityExchangeComplete, Is.True);
            Assert.That(machine.State.SecurityExchangeIsTls, Is.True);
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.True);
            Assert.That(machine.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
        });

        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "TLS-C"), CreateReply(234));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SecurityExchangeComplete, Is.False);
            Assert.That(machine.State.SecurityExchangeInProgress, Is.True);
            Assert.That(machine.State.SecurityExchangeIsTls, Is.True);
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.False);
            Assert.That(machine.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Clear));
        });

        machine.CompleteSecurityExchange(true);
        SelectPrivateProtection(machine);
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Rein, null), CreateReply(220));

        Assert.Multiple(() =>
        {
            Assert.That(machine.State.SecurityExchangeComplete, Is.False);
            Assert.That(machine.State.SecurityExchangeIsTls, Is.False);
            Assert.That(machine.State.ControlProtected, Is.False);
            Assert.That(machine.State.ProtectionBufferSizeSet, Is.False);
            Assert.That(machine.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Clear));
            Assert.That(machine.State.SessionState, Is.EqualTo(FtpSessionState.Connected));
        });
    }

    [Test]
    public void Non_TLS_security_uses_the_RFC_2228_buffer_range_and_levels()
    {
        FtpSequenceState state = new FtpSequenceState();
        state.SessionState = FtpSessionState.Connected;
        state.SecurityExchangeComplete = true;

        FtpSequenceValidationResult minimum = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "1"), state);
        FtpSequenceValidationResult maximum = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "4294967295"), state);
        FtpSequenceValidationResult zero = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "0"), state);
        state.ProtectionBufferSizeSet = true;
        FtpSequenceValidationResult safe = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "S"), state);
        FtpSequenceValidationResult confidential = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "E"), state);

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Allowed, Is.True);
            Assert.That(maximum.Allowed, Is.True);
            Assert.That(zero.ReplyCode,
                Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
            Assert.That(safe.Allowed, Is.True);
            Assert.That(confidential.Allowed, Is.True);
        });
    }

    private static FtpSessionStateMachine CreateTlsMachine()
    {
        FtpSessionStateMachine machine = new FtpSessionStateMachine();
        machine.OpenConnection();
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "TLS"), CreateReply(234));
        machine.CompleteSecurityExchange(true);
        return machine;
    }

    private static void SelectPrivateProtection(FtpSessionStateMachine machine)
    {
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pbsz, "0"), CreateReply(200));
        machine.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "P"), CreateReply(200));
    }

    private static FtpCommand CreateCommand(FtpCommandKind kind, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = FtpCommandCatalog.ResolveVerb(kind);
        command.Argument = argument;
        return command;
    }

    private static FtpReply CreateReply(int code)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
