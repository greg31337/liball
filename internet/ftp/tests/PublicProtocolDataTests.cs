namespace ftp.Tests;

public class PublicProtocolDataTests
{
    [Test]
    public void Command_defaults_to_unknown_without_an_argument()
    {
        FtpCommand command = new FtpCommand();

        Assert.Multiple(() =>
        {
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Unknown));
            Assert.That(command.Verb, Is.Empty);
            Assert.That(command.Argument, Is.Null);
            Assert.That(command.RawText, Is.Empty);
            Assert.That(command.Sensitive, Is.False);
        });

        command.Argument = "";
        Assert.That(command.Argument, Is.Empty);
    }

    [Test]
    public void Reply_line_distinguishes_missing_codes_from_zero()
    {
        FtpReplyLine line = new FtpReplyLine();

        Assert.Multiple(() =>
        {
            Assert.That(line.HasCode, Is.False);
            Assert.That(line.Code, Is.Zero);
            Assert.That(line.Text, Is.Empty);
            Assert.That(line.RawText, Is.Empty);
            Assert.That(line.TerminatesMultiline, Is.False);
        });
    }

    [Test]
    public void Reply_classification_does_not_imply_success_or_failure()
    {
        FtpReplyClassification classification = new FtpReplyClassification();

        Assert.Multiple(() =>
        {
            Assert.That(classification.Category, Is.EqualTo(FtpReplyCategory.Unknown));
            Assert.That(classification.Kind, Is.EqualTo(FtpReplyKind.Unknown));
            Assert.That(classification.Malformed, Is.False);
            Assert.That(classification.Succeeded, Is.False);
            Assert.That(classification.TransientFailure, Is.False);
            Assert.That(classification.PermanentFailure, Is.False);
            Assert.That(classification.AuthenticationFailure, Is.False);
        });
    }

    [Test]
    public void Reply_owns_empty_line_collection_and_classification()
    {
        FtpReply first = new FtpReply();
        FtpReply second = new FtpReply();

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.Zero);
            Assert.That(first.Text, Is.Empty);
            Assert.That(first.Lines, Is.Empty);
            Assert.That(first.Lines, Is.Not.SameAs(second.Lines));
            Assert.That(first.Classification, Is.Not.SameAs(second.Classification));
            Assert.That(first.Multiline, Is.False);
            Assert.That(first.RawText, Is.Empty);
        });
    }

    [Test]
    public void Command_result_starts_empty_and_non_successful()
    {
        FtpCommandResult first = new FtpCommandResult();
        FtpCommandResult second = new FtpCommandResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Command, Is.Not.SameAs(second.Command));
            Assert.That(first.Replies, Is.Empty);
            Assert.That(first.Replies, Is.Not.SameAs(second.Replies));
            Assert.That(first.PreliminaryReply, Is.Null);
            Assert.That(first.FinalReply, Is.Null);
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.ConnectionClosed, Is.False);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Feature_preserves_absent_parameters_and_unknown_support()
    {
        FtpFeature feature = new FtpFeature();

        Assert.Multiple(() =>
        {
            Assert.That(feature.Name, Is.Empty);
            Assert.That(feature.Parameters, Is.Null);
            Assert.That(feature.RawLine, Is.Empty);
            Assert.That(feature.Support, Is.EqualTo(FtpFeatureSupport.Unknown));
        });

        feature.Parameters = "";
        Assert.That(feature.Parameters, Is.Empty);
    }

    [Test]
    public void Feature_set_owns_empty_collection_and_is_not_negotiated()
    {
        FtpFeatureSet first = new FtpFeatureSet();
        FtpFeatureSet second = new FtpFeatureSet();

        Assert.Multiple(() =>
        {
            Assert.That(first.Features, Is.Empty);
            Assert.That(first.Features, Is.Not.SameAs(second.Features));
            Assert.That(first.SourceReply, Is.Null);
            Assert.That(first.Negotiated, Is.False);
        });
    }

    [Test]
    public void Option_request_distinguishes_absent_and_empty_values()
    {
        FtpOptionRequest option = new FtpOptionRequest();

        Assert.Multiple(() =>
        {
            Assert.That(option.CommandName, Is.Empty);
            Assert.That(option.Value, Is.Null);
        });

        option.Value = "";
        Assert.That(option.Value, Is.Empty);
    }
}

