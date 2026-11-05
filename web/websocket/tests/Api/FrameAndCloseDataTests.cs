using websocket;

namespace websocket.Tests;

public class FrameAndCloseDataTests
{
    [Test]
    public void FrameHeaderCarriesAllWireFields()
    {
        var empty = new WebSocketFrameHeader();
        var header = new WebSocketFrameHeader();
        header.Final = true;
        header.Rsv1 = true;
        header.Rsv2 = false;
        header.Rsv3 = true;
        header.Opcode = WebSocketOpcode.Binary;
        header.Masked = true;
        header.MaskingKey = 0x11223344U;
        header.PayloadLength = 65536;

        Assert.Multiple(() =>
        {
            Assert.That(empty.Final, Is.False);
            Assert.That(empty.Rsv1, Is.False);
            Assert.That(empty.Rsv2, Is.False);
            Assert.That(empty.Rsv3, Is.False);
            Assert.That(empty.Opcode, Is.EqualTo(WebSocketOpcode.Continuation));
            Assert.That(empty.Masked, Is.False);
            Assert.That(empty.MaskingKey, Is.Zero);
            Assert.That(empty.PayloadLength, Is.Zero);
            Assert.That(header.Final, Is.True);
            Assert.That(header.Rsv1, Is.True);
            Assert.That(header.Rsv2, Is.False);
            Assert.That(header.Rsv3, Is.True);
            Assert.That(header.Opcode, Is.EqualTo(WebSocketOpcode.Binary));
            Assert.That(header.Masked, Is.True);
            Assert.That(header.MaskingKey, Is.EqualTo(0x11223344U));
            Assert.That(header.PayloadLength, Is.EqualTo(65536));
        });
    }

    [Test]
    public void FrameReadResultUsesPayloadRangeAndPresenceFlags()
    {
        var payload = new byte[] { 99, 1, 2, 3, 88 };
        var result = new WebSocketFrameReadResult();
        var second = new WebSocketFrameReadResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.HasHeader, Is.False);
            Assert.That(result.Payload, Is.Empty);
            Assert.That(result.PayloadOffset, Is.Zero);
            Assert.That(result.PayloadCount, Is.Zero);
            Assert.That(result.EndOfFrame, Is.False);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Header, Is.Not.SameAs(second.Header));
            Assert.That(result.Error, Is.Not.SameAs(second.Error));
        });

        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = 5;
        result.HasHeader = true;
        result.Header.Opcode = WebSocketOpcode.Text;
        result.Payload = payload;
        result.PayloadOffset = 1;
        result.PayloadCount = 3;
        result.EndOfFrame = true;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Protocol;

        Assert.Multiple(() =>
        {
            Assert.That(result.ConsumedCount, Is.EqualTo(5));
            Assert.That(result.HasHeader, Is.True);
            Assert.That(result.Header.Opcode, Is.EqualTo(WebSocketOpcode.Text));
            Assert.That(result.Payload, Is.SameAs(payload));
            Assert.That(result.Payload.Skip(result.PayloadOffset).Take(result.PayloadCount), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.EndOfFrame, Is.True);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Protocol));
        });
    }

    [Test]
    public void FrameWriteRequestCarriesCallerRangeAndMaskingContinuation()
    {
        var payload = new byte[] { 0, 10, 20, 30, 0 };
        var source = new DataTestMaskKeySource();
        var request = new WebSocketFrameWriteRequest();
        var second = new WebSocketFrameWriteRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Header, Is.Not.SameAs(second.Header));
            Assert.That(request.Payload, Is.Empty);
            Assert.That(request.PayloadOffset, Is.Zero);
            Assert.That(request.PayloadCount, Is.Zero);
            Assert.That(request.HasMaskKeySource, Is.False);
            Assert.That(request.MaskKeySource, Is.Null);
            Assert.That(request.MaskingOffset, Is.Zero);
        });

        request.Header.Final = true;
        request.Header.Opcode = WebSocketOpcode.Binary;
        request.Payload = payload;
        request.PayloadOffset = 1;
        request.PayloadCount = 3;
        request.HasMaskKeySource = true;
        request.MaskKeySource = source;
        request.MaskingOffset = 7;

        Assert.Multiple(() =>
        {
            Assert.That(request.Header.Final, Is.True);
            Assert.That(request.Header.Opcode, Is.EqualTo(WebSocketOpcode.Binary));
            Assert.That(request.Payload, Is.SameAs(payload));
            Assert.That(request.Payload.Skip(request.PayloadOffset).Take(request.PayloadCount), Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(request.HasMaskKeySource, Is.True);
            Assert.That(request.MaskKeySource, Is.SameAs(source));
            Assert.That(request.MaskingOffset, Is.EqualTo(7));
        });
    }

    [Test]
    public void CloseInfoDistinguishesEmptyPayloadFromKnownCodeAndReason()
    {
        var close = new WebSocketCloseInfo();

        Assert.Multiple(() =>
        {
            Assert.That(close.HasCode, Is.False);
            Assert.That(close.Code, Is.Zero);
            Assert.That(close.HasKnownCode, Is.False);
            Assert.That((int)close.KnownCode, Is.Zero);
            Assert.That(close.HasReason, Is.False);
            Assert.That(close.Reason, Is.Empty);
            Assert.That(close.InitiatedLocally, Is.False);
            Assert.That(close.Sent, Is.False);
            Assert.That(close.Received, Is.False);
            Assert.That(close.Clean, Is.False);
        });

        close.HasCode = true;
        close.Code = 1000;
        close.HasKnownCode = true;
        close.KnownCode = WebSocketCloseCode.NormalClosure;
        close.HasReason = true;
        close.Reason = "done";
        close.InitiatedLocally = true;
        close.Sent = true;
        close.Received = true;
        close.Clean = true;

        Assert.Multiple(() =>
        {
            Assert.That(close.HasCode, Is.True);
            Assert.That(close.Code, Is.EqualTo(1000));
            Assert.That(close.HasKnownCode, Is.True);
            Assert.That(close.KnownCode, Is.EqualTo(WebSocketCloseCode.NormalClosure));
            Assert.That(close.HasReason, Is.True);
            Assert.That(close.Reason, Is.EqualTo("done"));
            Assert.That(close.InitiatedLocally, Is.True);
            Assert.That(close.Sent, Is.True);
            Assert.That(close.Received, Is.True);
            Assert.That(close.Clean, Is.True);
        });
    }

    [Test]
    public void CloseReadResultSeparatesValidityFromFailureMetadata()
    {
        var valid = new WebSocketCloseReadResult();
        var second = new WebSocketCloseReadResult();
        valid.Valid = true;
        valid.Close.HasCode = true;
        valid.Close.Code = 1001;
        var invalid = new WebSocketCloseReadResult();
        invalid.Valid = false;
        invalid.HasError = true;
        invalid.Error.Kind = WebSocketErrorKind.Protocol;
        invalid.Error.Identifier = "one-byte-close";

        Assert.Multiple(() =>
        {
            Assert.That(second.Valid, Is.False);
            Assert.That(second.HasError, Is.False);
            Assert.That(valid.Close, Is.Not.SameAs(second.Close));
            Assert.That(valid.Error, Is.Not.SameAs(second.Error));
            Assert.That(valid.Valid, Is.True);
            Assert.That(valid.Close.Code, Is.EqualTo(1001));
            Assert.That(valid.HasError, Is.False);
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.HasError, Is.True);
            Assert.That(invalid.Error.Kind, Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(invalid.Error.Identifier, Is.EqualTo("one-byte-close"));
        });
    }
}

internal class DataTestMaskKeySource : IWebSocketMaskKeySource
{
    public uint CreateMaskingKey()
    {
        return 0x11223344U;
    }
}
