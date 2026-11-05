namespace ftp.Tests;

public class FtpReplyExtractorTests
{
    [Test]
    public void Greeting_extraction_distinguishes_delayed_and_ready_service()
    {
        FtpGreetingReply delayed = FtpReplyExtractor.ExtractGreeting(CreateReply(120, "Ready in 2 minutes"));
        FtpGreetingReply ready = FtpReplyExtractor.ExtractGreeting(CreateReply(220, "Ready"));

        Assert.Multiple(() =>
        {
            Assert.That(delayed.Delayed, Is.True);
            Assert.That(delayed.Ready, Is.False);
            Assert.That(delayed.Message, Is.EqualTo("Ready in 2 minutes"));
            Assert.That(ready.Ready, Is.True);
            Assert.That(ready.Delayed, Is.False);
            Assert.That(ready.Message, Is.EqualTo("Ready"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractGreeting(CreateReply(230, "Logged in")));
        });
    }

    [Test]
    public void Size_extraction_accepts_nonnegative_int64_and_rejects_other_payloads()
    {
        long maximum = FtpReplyExtractor.ExtractSize(
            CreateReply(FtpReplyCodes.FileStatus, "9223372036854775807"));

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo(long.MaxValue));
            Assert.That(FtpReplyExtractor.ExtractSize(CreateReply(213, "0")), Is.Zero);
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractSize(CreateReply(213, " 12")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractSize(CreateReply(213, "9223372036854775808")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractSize(CreateReply(200, "12")));
        });
    }

    [Test]
    public void Modification_time_extraction_preserves_utc_fraction_and_raw_value()
    {
        FtpTimestamp timestamp = FtpReplyExtractor.ExtractModificationTime(
            CreateReply(213, "20240229123456.123456789"));

        Assert.Multiple(() =>
        {
            Assert.That(timestamp.HasValue, Is.True);
            Assert.That(timestamp.ValueUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(timestamp.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567)));
            Assert.That(timestamp.FractionalSeconds, Is.EqualTo("123456789"));
            Assert.That(timestamp.RawValue, Is.EqualTo("20240229123456.123456789"));
        });
    }

    [Test]
    public void Modification_time_extraction_rejects_invalid_calendar_and_wire_forms()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractModificationTime(CreateReply(213, "20230229123456")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractModificationTime(CreateReply(213, "20240101246000")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractModificationTime(CreateReply(213, "20240101120000.")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractModificationTime(CreateReply(213, "20240101120000Z")));
        });
    }

    [Test]
    public void System_type_extraction_requires_the_SYST_reply_code()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpReplyExtractor.ExtractSystemType(CreateReply(215, "UNIX Type: L8")),
                Is.EqualTo("UNIX Type: L8"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractSystemType(CreateReply(200, "UNIX")));
        });
    }

    [Test]
    public void Passive_extraction_decodes_ipv4_address_and_unsigned_port_bytes()
    {
        FtpPassiveReply passive = FtpReplyExtractor.ExtractPassiveEndpoint(
            CreateReply(227, "Entering Passive Mode (192,0,2,10,195,80)"));

        Assert.Multiple(() =>
        {
            Assert.That(passive.Extended, Is.False);
            Assert.That(passive.Endpoint.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(passive.Endpoint.Port, Is.EqualTo(50000));
            Assert.That(passive.Endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractPassiveEndpoint(
                CreateReply(227, "Entering Passive Mode (192,0,2,10,256,1)")));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractPassiveEndpoint(
                CreateReply(227, "Entering Passive Mode (192,0,2,10,1)")));
        });
    }

    [Test]
    public void Extended_passive_extraction_supports_arbitrary_printable_delimiters()
    {
        FtpPassiveReply standard = FtpReplyExtractor.ExtractExtendedPassiveEndpoint(
            CreateReply(229, "Entering Extended Passive Mode (|||6446|)"));
        FtpPassiveReply alternate = FtpReplyExtractor.ExtractExtendedPassiveEndpoint(
            CreateReply(229, "Extended passive (!!!49152!)"));

        Assert.Multiple(() =>
        {
            Assert.That(standard.Extended, Is.True);
            Assert.That(standard.Endpoint.Host, Is.Empty);
            Assert.That(standard.Endpoint.Port, Is.EqualTo(6446));
            Assert.That(standard.Endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.Unspecified));
            Assert.That(alternate.Endpoint.Port, Is.EqualTo(49152));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractExtendedPassiveEndpoint(
                CreateReply(229, "Entering Extended Passive Mode (|||0|)")));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractExtendedPassiveEndpoint(
                CreateReply(229, "Entering Extended Passive Mode (||6446|)")));
        });
    }

    [Test]
    public void Created_path_extraction_unescapes_doubled_quotes_and_ignores_suffix()
    {
        string path = FtpReplyExtractor.ExtractCreatedPath(
            CreateReply(257, "\"/reports/\"\"final\"\"\" directory created"));

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/reports/\"final\""));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractCreatedPath(CreateReply(257, "not quoted")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractCreatedPath(CreateReply(257, "\"unterminated")));
        });
    }

    [Test]
    public void Pending_action_extraction_types_REST_RNFR_and_restart_markers()
    {
        FtpPendingReply restart = FtpReplyExtractor.ExtractRestartReadiness(
            CreateReply(350, "Restarting at 1024"));
        FtpPendingReply rename = FtpReplyExtractor.ExtractRenameReadiness(
            CreateReply(350, "File exists, ready for destination name"));

        Assert.Multiple(() =>
        {
            Assert.That(restart.Operation, Is.EqualTo("REST"));
            Assert.That(restart.Message, Is.EqualTo("Restarting at 1024"));
            Assert.That(rename.Operation, Is.EqualTo("RNFR"));
            Assert.That(FtpReplyExtractor.ExtractRestartMarker(CreateReply(110, "MARK 12")),
                Is.EqualTo("MARK 12"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractRenameReadiness(CreateReply(250, "Renamed")));
        });
    }

    [Test]
    public void Network_protocol_rejection_extracts_the_supported_protocol_list()
    {
        FtpNetworkProtocolReply rejection = FtpReplyExtractor.ExtractNetworkProtocolRejection(
            CreateReply(522, "Network protocol not supported, use (1,2)"));

        Assert.Multiple(() =>
        {
            Assert.That(rejection.SupportedProtocols, Is.EqualTo(new[] { 1, 2 }));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractNetworkProtocolRejection(
                CreateReply(522, "Network protocol not supported, use ()")));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractNetworkProtocolRejection(
                CreateReply(522, "Network protocol not supported, use (1, 2)")));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractNetworkProtocolRejection(
                CreateReply(500, "Network protocol not supported, use (1)")));
        });
    }

    [Test]
    public void Transfer_completion_extracts_preliminary_and_successful_terminal_replies()
    {
        FtpCommand command = FtpCommandParser.ParseDecodedCommand("RETR file.bin");
        FtpReply preliminary = CreateReply(150, "Opening binary connection");
        FtpReply completion = CreateReply(226, "Transfer complete");
        FtpCommandResult result = FtpReplyExtractor.ExtractTransferCompletion(
            command, new[] { preliminary, completion });

        Assert.Multiple(() =>
        {
            Assert.That(result.Command, Is.SameAs(command));
            Assert.That(result.PreliminaryReply, Is.SameAs(preliminary));
            Assert.That(result.FinalReply, Is.SameAs(completion));
            Assert.That(result.Replies, Is.EqualTo(new[] { preliminary, completion }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ConnectionClosed, Is.False);
        });
    }

    [Test]
    public void Transfer_completion_retains_terminal_failures_without_throwing()
    {
        FtpCommand command = FtpCommandParser.ParseDecodedCommand("STOR file.bin");
        FtpReply preliminary = CreateReply(125, "Data connection already open");
        FtpReply failure = CreateReply(426, "Transfer aborted");
        FtpCommandResult result = FtpReplyExtractor.ExtractTransferCompletion(
            command, new[] { preliminary, failure });
        FtpCommandResult closed = FtpReplyExtractor.ExtractTransferCompletion(
            command, new[] { CreateReply(421, "Service unavailable") });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FinalReply, Is.SameAs(failure));
            Assert.That(closed.Succeeded, Is.False);
            Assert.That(closed.ConnectionClosed, Is.True);
        });
    }

    [Test]
    public void Transfer_completion_rejects_missing_misordered_and_extra_replies()
    {
        FtpCommand command = FtpCommandParser.ParseDecodedCommand("LIST");

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpReplyExtractor.ExtractTransferCompletion(command, new FtpReply[0]));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractTransferCompletion(
                command, new[] { CreateReply(150, "Opening") }));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractTransferCompletion(
                command, new[] { CreateReply(226, "Done"), CreateReply(250, "Okay") }));
            Assert.Throws<FtpProtocolException>(() => FtpReplyExtractor.ExtractTransferCompletion(
                command, new[] { CreateReply(150, "Opening"), CreateReply(350, "Pending") }));
        });
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
