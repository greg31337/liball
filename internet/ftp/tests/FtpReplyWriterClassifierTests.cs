using System.Text;

namespace ftp.Tests;

public class FtpReplyWriterClassifierTests
{
    [Test]
    public void Classifier_maps_each_first_digit_and_failure_flags()
    {
        FtpReplyClassification preliminary = FtpReplyClassifier.ClassifyCode(150);
        FtpReplyClassification completion = FtpReplyClassifier.ClassifyCode(220);
        FtpReplyClassification intermediate = FtpReplyClassifier.ClassifyCode(331);
        FtpReplyClassification transient = FtpReplyClassifier.ClassifyCode(421);
        FtpReplyClassification authentication = FtpReplyClassifier.ClassifyCode(530);

        Assert.Multiple(() =>
        {
            Assert.That(preliminary.Category, Is.EqualTo(FtpReplyCategory.PositivePreliminary));
            Assert.That(completion.Category, Is.EqualTo(FtpReplyCategory.PositiveCompletion));
            Assert.That(completion.Succeeded, Is.True);
            Assert.That(intermediate.Category, Is.EqualTo(FtpReplyCategory.PositiveIntermediate));
            Assert.That(transient.Category, Is.EqualTo(FtpReplyCategory.TransientNegativeCompletion));
            Assert.That(transient.TransientFailure, Is.True);
            Assert.That(authentication.Category, Is.EqualTo(FtpReplyCategory.PermanentNegativeCompletion));
            Assert.That(authentication.PermanentFailure, Is.True);
            Assert.That(authentication.AuthenticationFailure, Is.True);
        });
    }

    [Test]
    public void Classifier_maps_each_second_digit_functional_group()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpReplyClassifier.ClassifyCode(200).Kind, Is.EqualTo(FtpReplyKind.Syntax));
            Assert.That(FtpReplyClassifier.ClassifyCode(211).Kind, Is.EqualTo(FtpReplyKind.Information));
            Assert.That(FtpReplyClassifier.ClassifyCode(225).Kind, Is.EqualTo(FtpReplyKind.Connections));
            Assert.That(FtpReplyClassifier.ClassifyCode(332).Kind, Is.EqualTo(FtpReplyKind.AuthenticationAndAccounting));
            Assert.That(FtpReplyClassifier.ClassifyCode(242).Kind, Is.EqualTo(FtpReplyKind.Unspecified));
            Assert.That(FtpReplyClassifier.ClassifyCode(250).Kind, Is.EqualTo(FtpReplyKind.FileSystem));
        });
    }

    [Test]
    public void Invalid_or_unknown_categories_are_retained_as_malformed_classification()
    {
        FtpReplyParser parser = new FtpReplyParser(FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines, true);
        FtpReply parsed = parser.ParseReplyText("699 vendor\r\n");
        FtpReplyClassification shortCode = FtpReplyClassifier.ClassifyCode(99);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Code, Is.EqualTo(699));
            Assert.That(parsed.Classification.Category, Is.EqualTo(FtpReplyCategory.Unknown));
            Assert.That(parsed.Classification.Malformed, Is.True);
            Assert.That(shortCode.Category, Is.EqualTo(FtpReplyCategory.Unknown));
            Assert.That(shortCode.Kind, Is.EqualTo(FtpReplyKind.Unknown));
            Assert.That(shortCode.Malformed, Is.True);
        });
    }

    [Test]
    public void Writer_formats_canonical_single_line_and_blank_replies()
    {
        FtpReplyWriter writer = CreateWriter();
        FtpReply ready = new FtpReply();
        ready.Code = FtpReplyCodes.ServiceReady;
        ready.Text = "Ready";
        FtpReply blank = new FtpReply();
        blank.Code = FtpReplyCodes.CommandOkay;

        Assert.Multiple(() =>
        {
            Assert.That(writer.FormatReply(ready), Is.EqualTo("220 Ready\r\n"));
            Assert.That(writer.FormatReply(blank), Is.EqualTo("200 \r\n"));
        });
    }

    [Test]
    public void Writer_generates_matching_multiline_framing_and_preserves_body_lines()
    {
        FtpReplyWriter writer = CreateWriter();
        FtpReply reply = new FtpReply();
        reply.Code = FtpReplyCodes.SystemStatus;
        reply.Text = "End";
        reply.Multiline = true;
        FtpReplyLine opening = new FtpReplyLine();
        opening.HasCode = true;
        opening.Code = FtpReplyCodes.SystemStatus;
        opening.Text = "Features";
        FtpReplyLine feature = new FtpReplyLine();
        feature.Text = " UTF8";
        FtpReplyLine unrelated = new FtpReplyLine();
        unrelated.HasCode = true;
        unrelated.Code = FtpReplyCodes.SecurityExchangeComplete;
        unrelated.Text = "unrelated";
        FtpReplyLine terminal = new FtpReplyLine();
        terminal.HasCode = true;
        terminal.Code = FtpReplyCodes.SystemStatus;
        terminal.Text = "End";
        reply.Lines = new[] { opening, feature, unrelated, terminal };

        string formatted = writer.FormatReply(reply);

        Assert.That(formatted, Is.EqualTo("211-Features\r\n UTF8\r\n234 unrelated\r\n211 End\r\n"));
    }

    [Test]
    public void Writer_rejects_invalid_codes_controls_and_ambiguous_multiline_bodies()
    {
        FtpReplyWriter writer = CreateWriter();
        FtpReply invalidCode = new FtpReply();
        invalidCode.Code = 699;
        FtpReply injected = new FtpReply();
        injected.Code = 220;
        injected.Text = "Ready\r\n421 closing";
        FtpReply multiline = CreateSimpleMultilineReply();
        multiline.Lines[1].RawText = "211 premature";
        FtpReply tooShort = new FtpReply();
        tooShort.Code = 211;
        tooShort.Multiline = true;
        tooShort.Lines = new[] { new FtpReplyLine() };

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => writer.FormatReply(invalidCode));
            Assert.Throws<FtpProtocolException>(() => writer.FormatReply(injected));
            Assert.Throws<FtpProtocolException>(() => writer.FormatReply(multiline));
            Assert.Throws<FtpProtocolException>(() => writer.FormatReply(tooShort));
        });
    }

    [Test]
    public void Encoded_writer_applies_negotiated_ascii_or_utf8_without_substitution()
    {
        FtpReplyWriter writer = CreateWriter();
        FtpReply reply = new FtpReply();
        reply.Code = 220;
        reply.Text = "café";

        byte[] utf8 = writer.FormatEncodedReply(reply, Encoding.UTF8);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(utf8), Is.EqualTo("220 café\r\n"));
            Assert.Throws<FtpProtocolException>(() => writer.FormatEncodedReply(reply, Encoding.ASCII));
            Assert.Throws<FtpConfigurationException>(() => writer.FormatEncodedReply(reply, Encoding.Unicode));
        });
    }

    private static FtpReplyWriter CreateWriter()
    {
        return new FtpReplyWriter(FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);
    }

    private static FtpReply CreateSimpleMultilineReply()
    {
        FtpReply reply = new FtpReply();
        reply.Code = 211;
        reply.Text = "End";
        reply.Multiline = true;
        FtpReplyLine opening = new FtpReplyLine();
        opening.HasCode = true;
        opening.Code = 211;
        opening.Text = "Start";
        FtpReplyLine middle = new FtpReplyLine();
        middle.Text = "body";
        FtpReplyLine terminal = new FtpReplyLine();
        terminal.HasCode = true;
        terminal.Code = 211;
        terminal.Text = "End";
        reply.Lines = new[] { opening, middle, terminal };
        return reply;
    }
}
