namespace sip.Tests;

public class PublicModelEnumUsageTests
{
    [Test]
    public void PublicMessageModelsRetainEnumValues()
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sips;
        uri.SchemeToken = "sips";

        SipContentDisposition disposition = new SipContentDisposition();
        disposition.Disposition = SipBodyDisposition.Session;
        disposition.DispositionToken = "session";

        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Invite;
        message.MethodToken = SipMethods.Invite;
        message.StatusClass = SipStatusClass.Successful;
        message.RequestUri = uri;
        message.ContentDisposition = disposition;

        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Warning;

        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Complete;
        result.Message = message;
        result.ValidationIssues = [issue];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(result.Message, Is.SameAs(message));
            Assert.That(message.Kind, Is.EqualTo(SipMessageKind.Request));
            Assert.That(message.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(message.StatusClass, Is.EqualTo(SipStatusClass.Successful));
            Assert.That(message.RequestUri, Is.SameAs(uri));
            Assert.That(uri.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(message.ContentDisposition, Is.SameAs(disposition));
            Assert.That(disposition.Disposition, Is.EqualTo(SipBodyDisposition.Session));
            Assert.That(result.ValidationIssues, Is.EqualTo(new SipValidationIssue[] { issue }));
            Assert.That(issue.Severity, Is.EqualTo(SipValidationSeverity.Warning));
        });
    }

    [Test]
    public void PublicMessageModelsUseSafeSentinelDefaults()
    {
        SipMessage message = new SipMessage();
        SipParseResult result = new SipParseResult();
        SipValidationIssue issue = new SipValidationIssue();

        Assert.Multiple(() =>
        {
            Assert.That(message.Kind, Is.EqualTo(SipMessageKind.Unknown));
            Assert.That(message.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(message.StatusClass, Is.EqualTo(SipStatusClass.Unknown));
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.None));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
            Assert.That(issue.Severity, Is.EqualTo(SipValidationSeverity.None));
            Assert.That(issue.ByteOffset, Is.EqualTo(-1));
        });
    }
}
