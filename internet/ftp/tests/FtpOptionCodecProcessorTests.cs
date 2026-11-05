namespace ftp.Tests;

public class FtpOptionCodecProcessorTests
{
    [Test]
    public void Command_codec_parses_and_generates_normalized_OPTS_requests()
    {
        FtpOptionRequest mlst = FtpOptionCodec.ParseOptionCommand(
            FtpCommandParser.ParseDecodedCommand("OPTS mlst Type;Size;"));
        FtpOptionRequest absent = FtpOptionCodec.ParseOptionCommand(
            FtpCommandParser.ParseDecodedCommand("OPTS UTF8"));
        FtpOptionRequest empty = FtpOptionCodec.ParseOptionCommand(
            FtpCommandParser.ParseDecodedCommand("OPTS X-FOO "));
        FtpCommand generated = FtpOptionCodec.GenerateOptionCommand(
            CreateRequest("mlst", "type;modify;"));

        Assert.Multiple(() =>
        {
            Assert.That(mlst.CommandName, Is.EqualTo("MLST"));
            Assert.That(mlst.Value, Is.EqualTo("Type;Size;"));
            Assert.That(absent.Value, Is.Null);
            Assert.That(empty.Value, Is.Empty);
            Assert.That(generated.Kind, Is.EqualTo(FtpCommandKind.Opts));
            Assert.That(generated.Verb, Is.EqualTo("OPTS"));
            Assert.That(generated.Argument, Is.EqualTo("MLST type;modify;"));
            Assert.That(generated.RawText, Is.EqualTo("OPTS MLST type;modify;"));
        });
    }

    [Test]
    public void Command_codec_rejects_missing_invalid_and_non_OPTS_targets()
    {
        string oversizedValue = new string('x', FtpDefaults.MaximumControlLineBytes);
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpOptionCodec.ParseOptionCommand(
                FtpCommandParser.ParseDecodedCommand("OPTS")));
            Assert.Throws<FtpProtocolException>(() => FtpOptionCodec.ParseOptionCommand(
                FtpCommandParser.ParseDecodedCommand("NOOP")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpOptionCodec.GenerateOptionCommand(CreateRequest("1BAD", "value")));
            Assert.Throws<FtpProtocolException>(() =>
                FtpOptionCodec.GenerateOptionCommand(CreateRequest("X-FOO", oversizedValue)));
        });
    }

    [Test]
    public void Processor_initializes_and_applies_MLST_facts_in_server_supported_order()
    {
        FtpOptionProcessor processor = CreateProcessor();

        FtpOptionApplicationResult result = processor.ApplyOptionRequest(
            CreateRequest("mlst", "modify;frogs;type;"));

        Assert.Multiple(() =>
        {
            Assert.That(processor.State.MlstFacts, Is.EqualTo(new[] { "type", "modify" }));
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Reply!.Code, Is.EqualTo(200));
            Assert.That(result.Reply.Text, Is.EqualTo("MLST OPTS type;modify;"));
            Assert.That(result.Request.CommandName, Is.EqualTo("MLST"));
        });
    }

    [Test]
    public void OPTS_MLST_without_options_selects_pathnames_only()
    {
        FtpOptionProcessor processor = CreateProcessor();
        Assert.That(processor.State.MlstFacts, Is.EqualTo(new[] { "type", "modify" }));

        FtpOptionApplicationResult result = processor.ApplyOptionRequest(
            CreateRequest("MLST", null));

        Assert.Multiple(() =>
        {
            Assert.That(processor.State.MlstFacts, Is.Empty);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Reply!.Text, Is.EqualTo("MLST OPTS"));
        });
    }

    [Test]
    public void Invalid_MLST_options_return_501_without_changing_prior_state()
    {
        FtpOptionProcessor processor = CreateProcessor();
        processor.ApplyOptionRequest(CreateRequest("MLST", "size;"));

        FtpOptionApplicationResult empty = processor.ApplyOptionRequest(CreateRequest("MLST", ""));
        FtpOptionApplicationResult missingTerminator = processor.ApplyOptionRequest(
            CreateRequest("MLST", "type"));
        FtpOptionApplicationResult asterisk = processor.ApplyOptionRequest(
            CreateRequest("MLST", "type*;"));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Accepted, Is.False);
            Assert.That(empty.Reply!.Code, Is.EqualTo(501));
            Assert.That(missingTerminator.Reply!.Code, Is.EqualTo(501));
            Assert.That(asterisk.Reply!.Code, Is.EqualTo(501));
            Assert.That(processor.State.MlstFacts, Is.EqualTo(new[] { "size" }));
        });
    }

    [Test]
    public void UTF8_ON_and_OFF_update_session_state_while_invalid_values_are_rejected()
    {
        FtpOptionProcessor processor = CreateProcessor();
        FtpOptionApplicationResult enabled = processor.ApplyOptionRequest(
            CreateRequest("utf8", "on"));

        Assert.Multiple(() =>
        {
            Assert.That(enabled.Accepted, Is.True);
            Assert.That(enabled.Reply!.Text, Is.EqualTo("UTF8 ON"));
            Assert.That(processor.State.Utf8Enabled, Is.True);
            Assert.That(processor.State.Utf8Configured, Is.True);
        });

        FtpOptionApplicationResult invalid = processor.ApplyOptionRequest(
            CreateRequest("UTF8", "AUTO"));
        Assert.Multiple(() =>
        {
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Reply!.Code, Is.EqualTo(501));
            Assert.That(processor.State.Utf8Enabled, Is.True);
        });

        FtpOptionApplicationResult disabled = processor.ApplyOptionRequest(
            CreateRequest("UTF8", "OFF"));
        Assert.That(disabled.Accepted, Is.True);
        Assert.That(processor.State.Utf8Enabled, Is.False);
    }

    [Test]
    public void Unavailable_standard_options_are_rejected_without_state_changes()
    {
        FtpOptionProcessor processor = new FtpOptionProcessor(null, false, false, new string[0]);

        FtpOptionApplicationResult mlst = processor.ApplyOptionRequest(
            CreateRequest("MLST", "type;"));
        FtpOptionApplicationResult utf8 = processor.ApplyOptionRequest(
            CreateRequest("UTF8", "ON"));

        Assert.Multiple(() =>
        {
            Assert.That(mlst.Reply!.Code, Is.EqualTo(501));
            Assert.That(utf8.Reply!.Code, Is.EqualTo(501));
            Assert.That(processor.State.MlstFacts, Is.Empty);
            Assert.That(processor.State.Utf8Enabled, Is.False);
            Assert.That(processor.State.Utf8Configured, Is.False);
        });
    }

    [Test]
    public void Configured_extension_options_are_snapshotted_and_replaced_per_target()
    {
        FtpOptionProcessor processor = CreateProcessor();
        FtpOptionRequest firstRequest = CreateRequest("x-mode", "FAST");
        FtpOptionApplicationResult first = processor.ApplyOptionRequest(firstRequest);
        firstRequest.Value = "mutated";
        FtpOptionApplicationResult second = processor.ApplyOptionRequest(
            CreateRequest("X-MODE", "SAFE"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Handled, Is.True);
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.ExtensionOwned, Is.True);
            Assert.That(second.Accepted, Is.True);
            Assert.That(processor.State.ExtensionOptions, Has.Length.EqualTo(1));
            Assert.That(processor.State.ExtensionOptions[0].CommandName, Is.EqualTo("X-MODE"));
            Assert.That(processor.State.ExtensionOptions[0].Value, Is.EqualTo("SAFE"));
        });
    }

    [Test]
    public void Unknown_option_targets_remain_unhandled_for_the_extension_chain()
    {
        FtpOptionProcessor processor = CreateProcessor();

        FtpOptionApplicationResult unknown = processor.ApplyOptionRequest(
            CreateRequest("X-UNKNOWN", "value"));
        FtpOptionApplicationResult feat = processor.ApplyOptionRequest(
            CreateRequest("FEAT", null));
        FtpOptionApplicationResult opts = processor.ApplyOptionRequest(
            CreateRequest("OPTS", null));

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Handled, Is.False);
            Assert.That(unknown.ExtensionOwned, Is.True);
            Assert.That(unknown.Reply, Is.Null);
            Assert.That(feat.Handled, Is.True);
            Assert.That(feat.Reply!.Code, Is.EqualTo(501));
            Assert.That(opts.Reply!.Code, Is.EqualTo(501));
        });
    }

    [Test]
    public void Accepted_MLST_reply_updates_client_state_and_negative_reply_preserves_it()
    {
        FtpOptionProcessor processor = CreateProcessor();
        FtpOptionRequest request = CreateRequest("MLST", "size;type;" );
        FtpReply acceptedReply = CreateReply(200, "mlst opts size;");

        FtpOptionApplicationResult accepted = processor.AcceptOptionReply(request, acceptedReply);
        FtpOptionApplicationResult rejected = processor.AcceptOptionReply(
            CreateRequest("MLST", "type;"), CreateReply(501, "Invalid options"));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(processor.State.MlstFacts, Is.EqualTo(new[] { "size" }));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reply!.Code, Is.EqualTo(501));
            Assert.That(processor.State.MlstFacts, Is.EqualTo(new[] { "size" }));
        });
    }

    [Test]
    public void Client_rejects_malformed_positive_MLST_option_replies()
    {
        FtpOptionProcessor processor = CreateProcessor();
        FtpOptionRequest request = CreateRequest("MLST", "type;");

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => processor.AcceptOptionReply(
                request, CreateReply(200, "Options okay")));
            Assert.Throws<FtpProtocolException>(() => processor.AcceptOptionReply(
                request, CreateReply(200, "MLST OPTS type")));
            Assert.Throws<FtpProtocolException>(() => processor.AcceptOptionReply(
                request, CreateReply(250, "MLST OPTS type;")));
        });
    }

    [Test]
    public void Option_state_is_owned_independently_by_each_session_processor()
    {
        FtpOptionProcessor first = CreateProcessor();
        FtpOptionProcessor second = CreateProcessor();
        first.ApplyOptionRequest(CreateRequest("MLST", "size;"));
        first.ApplyOptionRequest(CreateRequest("UTF8", "ON"));
        first.ApplyOptionRequest(CreateRequest("X-MODE", "FAST"));

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.Not.SameAs(second.State));
            Assert.That(first.State.MlstFacts, Is.EqualTo(new[] { "size" }));
            Assert.That(second.State.MlstFacts, Is.EqualTo(new[] { "type", "modify" }));
            Assert.That(second.State.Utf8Enabled, Is.False);
            Assert.That(second.State.ExtensionOptions, Is.Empty);
        });
    }

    private static FtpOptionProcessor CreateProcessor()
    {
        FtpMlstFeatureInfo info = new FtpMlstFeatureInfo();
        info.Advertised = true;
        info.Facts = new[] { "type", "size", "modify" };
        info.DefaultFacts = new[] { "modify", "type" };
        return new FtpOptionProcessor(info, true, false, new[] { "X-MODE" });
    }

    private static FtpOptionRequest CreateRequest(string commandName, string? value)
    {
        FtpOptionRequest request = new FtpOptionRequest();
        request.CommandName = commandName;
        request.Value = value;
        return request;
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
