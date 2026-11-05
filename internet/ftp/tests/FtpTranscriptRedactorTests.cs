namespace ftp.Tests;

public class FtpTranscriptRedactorTests
{
    [TestCase(FtpCommandKind.Pass, "PASS")]
    [TestCase(FtpCommandKind.Acct, "ACCT")]
    [TestCase(FtpCommandKind.Adat, "ADAT")]
    [TestCase(FtpCommandKind.Mic, "MIC")]
    [TestCase(FtpCommandKind.Conf, "CONF")]
    [TestCase(FtpCommandKind.Enc, "ENC")]
    public void Secret_command_and_reply_payloads_are_removed_from_transcript_events(
        FtpCommandKind kind, string verb)
    {
        const string secret = "secret-certificate-token-material";
        FtpCommand command = CreateCommand(kind, verb, secret);
        FtpReply reply = CreateReply(secret);

        FtpEvent commandEvent = FtpTranscriptRedactor.CreateCommandEvent(command);
        FtpEvent replyEvent = FtpTranscriptRedactor.CreateReplyEvent(command, reply);

        Assert.Multiple(() =>
        {
            Assert.That(commandEvent.Kind, Is.EqualTo(FtpEventKind.Command));
            Assert.That(commandEvent.Command!.Sensitive, Is.True);
            Assert.That(commandEvent.Command.Argument,
                Is.EqualTo(FtpTranscriptRedactor.RedactedText));
            Assert.That(commandEvent.Command.RawText, Does.Not.Contain(secret));
            Assert.That(commandEvent.Message, Does.Not.Contain(secret));
            Assert.That(replyEvent.Kind, Is.EqualTo(FtpEventKind.Reply));
            Assert.That(replyEvent.Command!.Argument,
                Is.EqualTo(FtpTranscriptRedactor.RedactedText));
            Assert.That(replyEvent.Reply!.Code, Is.EqualTo(230));
            Assert.That(replyEvent.Reply.Text,
                Is.EqualTo(FtpTranscriptRedactor.RedactedText));
            Assert.That(replyEvent.Reply.RawText, Does.Not.Contain(secret));
            Assert.That(replyEvent.Reply.Lines, Has.Length.EqualTo(2));
            Assert.That(replyEvent.Reply.Lines.All(line =>
                !line.Text.Contains(secret) && !line.RawText.Contains(secret)), Is.True);
            Assert.That(replyEvent.Message, Does.Not.Contain(secret));
            Assert.That(replyEvent.HasReplyCategory, Is.True);
            Assert.That(replyEvent.ReplyCategory,
                Is.EqualTo(FtpReplyCategory.PositiveCompletion));
            Assert.That(replyEvent.Timestamp.HasValue, Is.True);
            Assert.That(replyEvent.Redacted, Is.True);
        });
    }

    [Test]
    public void Explicit_sensitive_marker_redacts_unknown_extension_payloads()
    {
        FtpCommand command = CreateCommand(
            FtpCommandKind.Unknown, "XSECRET", "private-extension-value");
        command.Sensitive = true;

        FtpCommand safe = FtpTranscriptRedactor.RedactCommand(command);

        Assert.Multiple(() =>
        {
            Assert.That(safe.Sensitive, Is.True);
            Assert.That(safe.Argument, Is.EqualTo(FtpTranscriptRedactor.RedactedText));
            Assert.That(safe.RawText, Does.Not.Contain("private-extension-value"));
        });
    }

    [Test]
    public void Non_secret_transcripts_are_deep_copied_without_losing_protocol_facts()
    {
        FtpCommand command = CreateCommand(FtpCommandKind.Feat, "FEAT", null);
        FtpReply reply = CreateReply("UTF8");

        FtpCommand safeCommand = FtpTranscriptRedactor.RedactCommand(command);
        FtpReply safeReply = FtpTranscriptRedactor.RedactReply(command, reply);
        reply.Text = "mutated";
        reply.Lines[0].Text = "mutated";
        reply.Classification.Category = FtpReplyCategory.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(safeCommand, Is.Not.SameAs(command));
            Assert.That(safeCommand.Sensitive, Is.False);
            Assert.That(safeCommand.RawText, Is.EqualTo("FEAT"));
            Assert.That(safeReply, Is.Not.SameAs(reply));
            Assert.That(safeReply.Text, Is.EqualTo("UTF8"));
            Assert.That(safeReply.Lines, Is.Not.SameAs(reply.Lines));
            Assert.That(safeReply.Lines[0], Is.Not.SameAs(reply.Lines[0]));
            Assert.That(safeReply.Lines[0].Text, Is.EqualTo("UTF8"));
            Assert.That(safeReply.Classification,
                Is.Not.SameAs(reply.Classification));
            Assert.That(safeReply.Classification.Category,
                Is.EqualTo(FtpReplyCategory.PositiveCompletion));
        });
    }

    [Test]
    public void Session_reuse_diagnostics_expose_only_boolean_evidence()
    {
        FtpTlsDiagnostics diagnostics = new FtpTlsDiagnostics();
        FtpTlsSessionReuseEvidence evidence = new FtpTlsSessionReuseEvidence();
        evidence.Available = true;
        evidence.Reused = true;
        evidence.MatchesControlSession = true;

        FtpTlsDiagnosticsFactory.ApplySessionReuse(diagnostics, evidence);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.SessionReuseEvidenceAvailable, Is.True);
            Assert.That(diagnostics.SessionReused, Is.True);
            Assert.That(diagnostics.SessionMatchesControl, Is.True);
            Assert.That(diagnostics.RemoteCertificateSha256Fingerprint, Is.Null);
        });
    }

    private static FtpCommand CreateCommand(
        FtpCommandKind kind, string verb, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = verb;
        command.Argument = argument;
        command.RawText = argument == null ? verb : verb + " " + argument;
        return command;
    }

    private static FtpReply CreateReply(string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = 230;
        reply.Text = text;
        reply.RawText = "230-" + text + "\n230 " + text;
        reply.Multiline = true;
        FtpReplyLine first = new FtpReplyLine();
        first.HasCode = true;
        first.Code = 230;
        first.Text = text;
        first.RawText = "230-" + text;
        FtpReplyLine second = new FtpReplyLine();
        second.HasCode = true;
        second.Code = 230;
        second.Text = text;
        second.RawText = "230 " + text;
        second.TerminatesMultiline = true;
        reply.Lines = new[] { first, second };
        reply.Classification = FtpReplyClassifier.ClassifyCode(reply.Code);
        return reply;
    }
}
