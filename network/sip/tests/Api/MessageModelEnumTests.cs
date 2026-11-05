namespace sip.Tests;

public class MessageModelEnumTests
{
    [TestCase(SipMessageKind.Unknown, 0)]
    [TestCase(SipMessageKind.Request, 1)]
    [TestCase(SipMessageKind.Response, 2)]
    public void MessageKindHasStableValue(SipMessageKind value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(SipMethod.Unknown, 0)]
    [TestCase(SipMethod.Ack, 1)]
    [TestCase(SipMethod.Bye, 2)]
    [TestCase(SipMethod.Cancel, 3)]
    [TestCase(SipMethod.Info, 4)]
    [TestCase(SipMethod.Invite, 5)]
    [TestCase(SipMethod.Message, 6)]
    [TestCase(SipMethod.Notify, 7)]
    [TestCase(SipMethod.Options, 8)]
    [TestCase(SipMethod.Prack, 9)]
    [TestCase(SipMethod.Publish, 10)]
    [TestCase(SipMethod.Refer, 11)]
    [TestCase(SipMethod.Register, 12)]
    [TestCase(SipMethod.Subscribe, 13)]
    [TestCase(SipMethod.Update, 14)]
    public void MethodHasStableValue(SipMethod value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(SipStatusClass.Unknown, 0)]
    [TestCase(SipStatusClass.Provisional, 1)]
    [TestCase(SipStatusClass.Successful, 2)]
    [TestCase(SipStatusClass.Redirection, 3)]
    [TestCase(SipStatusClass.ClientError, 4)]
    [TestCase(SipStatusClass.ServerError, 5)]
    [TestCase(SipStatusClass.GlobalFailure, 6)]
    public void StatusClassHasStableHundredsDigit(SipStatusClass value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(SipUriScheme.Unknown, 0)]
    [TestCase(SipUriScheme.Sip, 1)]
    [TestCase(SipUriScheme.Sips, 2)]
    [TestCase(SipUriScheme.Tel, 3)]
    [TestCase(SipUriScheme.Absolute, 4)]
    public void UriSchemeHasStableValue(SipUriScheme value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(SipParseStatus.None, 0)]
    [TestCase(SipParseStatus.Complete, 1)]
    [TestCase(SipParseStatus.Incomplete, 2)]
    [TestCase(SipParseStatus.Error, 3)]
    public void ParseStatusHasStableValue(SipParseStatus value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(SipValidationSeverity.None, 0)]
    [TestCase(SipValidationSeverity.Warning, 1)]
    [TestCase(SipValidationSeverity.Error, 2)]
    public void ValidationSeverityHasStableValue(SipValidationSeverity value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(SipBodyDisposition.Unknown, 0)]
    [TestCase(SipBodyDisposition.Session, 1)]
    [TestCase(SipBodyDisposition.Render, 2)]
    [TestCase(SipBodyDisposition.Icon, 3)]
    [TestCase(SipBodyDisposition.Alert, 4)]
    [TestCase(SipBodyDisposition.Signal, 5)]
    [TestCase(SipBodyDisposition.ByReference, 6)]
    [TestCase(SipBodyDisposition.RecordingSession, 7)]
    public void BodyDispositionHasStableValue(SipBodyDisposition value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }
}
