using websocket;

namespace websocket.Tests;

public class CloseCodeClassifierTests
{
    [TestCase(int.MinValue)]
    [TestCase(0)]
    [TestCase(999)]
    [TestCase(5000)]
    [TestCase(65535)]
    [TestCase(int.MaxValue)]
    public void ValuesOutsideTheWireRangeAreRejected(int code)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.OutOfRange));
            Assert.That(result.HasKnownCode, Is.False);
        });
    }

    [TestCase(1004, true)]
    [TestCase(1016, false)]
    [TestCase(2000, false)]
    [TestCase(2999, false)]
    public void ReservedValuesAreRejected(int code, bool hasKnownCode)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.Reserved));
            Assert.That(result.HasKnownCode, Is.EqualTo(hasKnownCode));
        });
    }

    [TestCase(1005, WebSocketCloseCode.NoStatusReceived)]
    [TestCase(1006, WebSocketCloseCode.AbnormalClosure)]
    [TestCase(1015, WebSocketCloseCode.TlsHandshake)]
    public void PseudoCodesAreKnownButForbiddenOnTheWire(int code, WebSocketCloseCode knownCode)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.ForbiddenPseudoCode));
            Assert.That(result.HasKnownCode, Is.True);
            Assert.That(result.KnownCode, Is.EqualTo(knownCode));
        });
    }

    [TestCase(1000)]
    [TestCase(1001)]
    [TestCase(1002)]
    [TestCase(1003)]
    [TestCase(1007)]
    [TestCase(1008)]
    [TestCase(1009)]
    [TestCase(1010)]
    [TestCase(1011)]
    [TestCase(1012)]
    [TestCase(1013)]
    [TestCase(1014)]
    public void RegisteredProtocolCodesAreValidAndKnown(int code)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.RegisteredProtocol));
            Assert.That(result.HasKnownCode, Is.True);
            Assert.That(result.KnownCode, Is.EqualTo((WebSocketCloseCode)code));
        });
    }

    [TestCase(3000, WebSocketCloseCode.Unauthorized)]
    [TestCase(3003, WebSocketCloseCode.Forbidden)]
    [TestCase(3008, WebSocketCloseCode.Timeout)]
    public void RegisteredApplicationCodesAreValidAndKnown(int code, WebSocketCloseCode knownCode)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.RegisteredApplication));
            Assert.That(result.HasKnownCode, Is.True);
            Assert.That(result.KnownCode, Is.EqualTo(knownCode));
        });
    }

    [TestCase(3001)]
    [TestCase(3002)]
    [TestCase(3004)]
    [TestCase(3007)]
    [TestCase(3009)]
    [TestCase(3999)]
    public void OtherApplicationRangeCodesRemainValid(int code)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.Application));
            Assert.That(result.HasKnownCode, Is.False);
        });
    }

    [TestCase(4000)]
    [TestCase(4500)]
    [TestCase(4999)]
    public void PrivateUseRangeCodesAreValid(int code)
    {
        var result = CloseCodeClassifier.Classify(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(CloseCodeKind.PrivateUse));
            Assert.That(result.HasKnownCode, Is.False);
        });
    }
}
