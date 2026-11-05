using websocket;

namespace websocket.Tests;

public class CloseCodesTests
{
    [Test]
    public void IntegerConstantsMatchKnownCloseCodeEnums()
    {
        var constants = new[]
        {
            WebSocketCloseCodes.NormalClosure,
            WebSocketCloseCodes.GoingAway,
            WebSocketCloseCodes.ProtocolError,
            WebSocketCloseCodes.UnsupportedData,
            WebSocketCloseCodes.Reserved,
            WebSocketCloseCodes.NoStatusReceived,
            WebSocketCloseCodes.AbnormalClosure,
            WebSocketCloseCodes.InvalidFramePayloadData,
            WebSocketCloseCodes.PolicyViolation,
            WebSocketCloseCodes.MessageTooBig,
            WebSocketCloseCodes.MandatoryExtension,
            WebSocketCloseCodes.InternalError,
            WebSocketCloseCodes.ServiceRestart,
            WebSocketCloseCodes.TryAgainLater,
            WebSocketCloseCodes.BadGateway,
            WebSocketCloseCodes.TlsHandshake,
            WebSocketCloseCodes.Unauthorized,
            WebSocketCloseCodes.Forbidden,
            WebSocketCloseCodes.Timeout
        };
        var enums = Enum.GetValues<WebSocketCloseCode>().Select(value => (int)value);

        Assert.That(constants, Is.EqualTo(enums));
    }

    [Test]
    public void ApplicationAndPrivateUseRangesMatchIana()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebSocketCloseCodes.RegisteredApplicationRangeMinimum, Is.EqualTo(3000));
            Assert.That(WebSocketCloseCodes.RegisteredApplicationRangeMaximum, Is.EqualTo(3999));
            Assert.That(WebSocketCloseCodes.PrivateUseRangeMinimum, Is.EqualTo(4000));
            Assert.That(WebSocketCloseCodes.PrivateUseRangeMaximum, Is.EqualTo(4999));
        });
    }
}
