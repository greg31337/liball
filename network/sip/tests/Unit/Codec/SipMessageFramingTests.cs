namespace sip.Tests;

public class SipMessageFramingTests
{
    [Test]
    public void StreamWaitsForTheExactBinaryBodyLength()
    {
        byte[] headers = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "Content-Type: application/octet-stream\r\nContent-Length: 5\r\n\r\n");
        byte[] body = [0, 13, 10, 255, 65];
        byte[] completeBytes = headers.Concat(body).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        for (int bodyCount = 0; bodyCount < body.Length; bodyCount = bodyCount + 1)
        {
            SipParseResult pending = codec.ParseStreamMessage(
                completeBytes.Take(headers.Length + bodyCount).ToArray(), FramingOptions());
            Assert.That(pending.Status, Is.EqualTo(SipParseStatus.Incomplete), bodyCount.ToString());
            Assert.That(pending.Consumed, Is.Zero, bodyCount.ToString());
            Assert.That(pending.RequiredBytes, Is.EqualTo(completeBytes.Length), bodyCount.ToString());
        }

        SipParseResult complete = codec.ParseStreamMessage(completeBytes, FramingOptions());
        completeBytes[headers.Length] = 99;

        Assert.That(complete.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(complete.Consumed, Is.EqualTo(headers.Length + body.Length));
        Assert.That(complete.RequiredBytes, Is.EqualTo(headers.Length + body.Length));
        Assert.That(complete.Message!.Body, Is.EqualTo(body));
        Assert.That(complete.Message.OriginalBytes[headers.Length], Is.Zero);
    }

    [Test]
    public void StreamConsumesOnlyTheFirstOfConcatenatedMessages()
    {
        byte[] first = Bytes("OPTIONS sip:one@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
        byte[] second = Bytes("SIP/2.0 200 OK\r\nContent-Length: 3\r\n\r\nack");
        byte[] pipeline = first.Concat(second).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult firstResult = codec.ParseStreamMessage(pipeline, FramingOptions());
        SipParseResult secondResult = codec.ParseStreamMessage(
            pipeline.Skip(firstResult.Consumed).ToArray(), FramingOptions());

        Assert.That(firstResult.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(firstResult.Consumed, Is.EqualTo(first.Length));
        Assert.That(firstResult.Message!.OriginalBytes, Has.Length.EqualTo(first.Length));
        Assert.That(secondResult.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(secondResult.Consumed, Is.EqualTo(second.Length));
        Assert.That(secondResult.Message!.StatusCode, Is.EqualTo(200));
        Assert.That(secondResult.Message.Body, Is.EqualTo(Bytes("ack")));
    }

    [Test]
    public void EqualDuplicateLengthsAreUnambiguousButConflictsFail()
    {
        string equal = "MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "Content-Length: 3\r\nl: 3\r\n\r\nabc";
        string conflicting = "MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "Content-Length: 3\r\nl: 4\r\n\r\nabc";
        int conflictOffset = conflicting.IndexOf("l: 4", StringComparison.Ordinal) + 3;
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult accepted = codec.ParseStreamMessage(Bytes(equal), FramingOptions());
        SipParseResult rejected = codec.ParseStreamMessage(Bytes(conflicting), FramingOptions());

        Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(accepted.Message!.Body, Is.EqualTo(Bytes("abc")));
        Assert.That(rejected.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(rejected.ErrorOffset, Is.EqualTo(conflictOffset));
        Assert.That(rejected.ErrorMessage, Does.Contain("Conflicting"));
    }

    [Test]
    public void StreamRequiresContentLengthEvenForAnEmptyBody()
    {
        byte[] source = Bytes("OPTIONS sip:service@example.com SIP/2.0\r\nX-Trace: one\r\n\r\n");
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseStreamMessage(source, FramingOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset, Is.EqualTo(source.Length));
        Assert.That(result.ErrorMessage, Does.Contain("requires a Content-Length"));
    }

    [Test]
    public void DatagramWithoutContentLengthUsesThePacketRemainderAsBody()
    {
        byte[] headers = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\nContent-Type: application/octet-stream\r\n\r\n");
        byte[] body = [1, 0, 255, 13, 10];
        byte[] source = headers.Concat(body).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseDatagramMessage(source, FramingOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Consumed, Is.EqualTo(source.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(source.Length));
        Assert.That(result.Message!.Body, Is.EqualTo(body));
    }

    [Test]
    public void DatagramReportsAndExcludesBytesAfterDeclaredBody()
    {
        byte[] message = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\nContent-Length: 3\r\n\r\nabc");
        byte[] trailing = Bytes("TRAILING-NOISE");
        byte[] datagram = message.Concat(trailing).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseDatagramMessage(datagram, FramingOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Consumed, Is.EqualTo(message.Length));
        Assert.That(datagram.Length - result.Consumed, Is.EqualTo(trailing.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(message.Length));
        Assert.That(result.Message!.Body, Is.EqualTo(Bytes("abc")));
        Assert.That(result.Message.OriginalBytes, Has.Length.EqualTo(message.Length));
    }

    [Test]
    public void TruncatedDatagramIsAnErrorWithTheDeclaredRequiredSize()
    {
        byte[] source = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\nContent-Length: 5\r\n\r\nabc");
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseDatagramMessage(source, FramingOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset, Is.EqualTo(source.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(source.Length + 2));
        Assert.That(result.ErrorMessage, Does.Contain("datagram ends"));
    }

    [Test]
    public void DeclaredAndImplicitBodiesHonorBodyAndMessageLimits()
    {
        string declaredText = "MESSAGE sip:bob@example.com SIP/2.0\r\nContent-Length: 5\r\n\r\nabcde";
        int declaredValueOffset = declaredText.IndexOf("5", StringComparison.Ordinal);
        byte[] implicitHeaders = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\n\r\n");
        byte[] implicitDatagram = implicitHeaders.Concat(Bytes("abcde")).ToArray();
        SipMessageCodec codec = new SipMessageCodec();
        SipParseOptions bodyLimit = new SipParseOptions();
        bodyLimit.MaxBodyBytes = 4;
        bodyLimit.Strict = false;
        SipParseOptions messageLimit = new SipParseOptions();
        messageLimit.MaxMessageBytes = Bytes(declaredText).Length - 1;
        messageLimit.Strict = false;

        SipParseResult declaredBody = codec.ParseStreamMessage(Bytes(declaredText), bodyLimit);
        SipParseResult implicitBody = codec.ParseDatagramMessage(implicitDatagram, bodyLimit);
        SipParseResult wholeMessage = codec.ParseStreamMessage(Bytes(declaredText), messageLimit);

        Assert.That(declaredBody.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(declaredBody.ErrorOffset, Is.EqualTo(declaredValueOffset));
        Assert.That(implicitBody.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(implicitBody.ErrorOffset, Is.EqualTo(implicitHeaders.Length + bodyLimit.MaxBodyBytes));
        Assert.That(wholeMessage.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(wholeMessage.ErrorOffset, Is.EqualTo(messageLimit.MaxMessageBytes));
    }

    [Test]
    public void StreamIgnoresLeadingCrlfAndReportsItsConsumption()
    {
        byte[] keepalive = Bytes("\r\n\r\n");
        byte[] message = Bytes("OPTIONS sip:service@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
        byte[] source = keepalive.Concat(message).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseStreamMessage(source, FramingOptions());
        SipParseResult onlyKeepalive = codec.ParseStreamMessage(keepalive, FramingOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Consumed, Is.EqualTo(source.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(source.Length));
        Assert.That(result.Message!.OriginalBytes, Is.EqualTo(message));
        Assert.That(onlyKeepalive.Status, Is.EqualTo(SipParseStatus.Incomplete));
        Assert.That(onlyKeepalive.Consumed, Is.EqualTo(keepalive.Length));
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static SipParseOptions FramingOptions()
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        return options;
    }
}
