namespace sip.Tests;

public class SipMessageParserComprehensiveTests
{
    [Test]
    public void StreamRemainsIncompleteAtEveryByteBoundary()
    {
        byte[] headers = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "X-Test: value\r\nContent-Type: application/octet-stream\r\n"
            + "Content-Length: 5\r\n\r\n");
        byte[] body = [0, 13, 10, 255, 65];
        byte[] message = headers.Concat(body).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        for (int count = 0; count < message.Length; count = count + 1)
        {
            SipParseResult result = codec.ParseStreamMessage(message.Take(count).ToArray(), Options());

            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Incomplete), "prefix " + count);
            Assert.That(result.Consumed, Is.Zero, "prefix " + count);
            Assert.That(result.Message, Is.Null, "prefix " + count);
            Assert.That(result.ErrorOffset, Is.EqualTo(-1), "prefix " + count);
            Assert.That(result.RequiredBytes, Is.EqualTo(count >= headers.Length ? message.Length : -1),
                "prefix " + count);
        }

        SipParseResult complete = codec.ParseStreamMessage(message, Options());

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(complete.Consumed, Is.EqualTo(message.Length));
            Assert.That(complete.RequiredBytes, Is.EqualTo(message.Length));
            Assert.That(complete.Message!.Body, Is.EqualTo(body));
        });
    }

    [Test]
    public void ParsesThreePipelinedMessagesWithBinaryAndMessageLikeBodies()
    {
        byte[] first = Message("OPTIONS sip:one@example.com SIP/2.0", new byte[0]);
        byte[] body = Bytes("SIP/2.0 200 OK\r\n\0").Concat(new byte[] { 255 }).ToArray();
        byte[] second = Message("MESSAGE sip:two@example.com SIP/2.0", body);
        byte[] third = Message("SIP/2.0 486 Busy Here", Bytes("tail"));
        byte[] pipeline = first.Concat(second).Concat(third).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult firstResult = codec.ParseStreamMessage(pipeline, Options());
        SipParseResult secondResult = codec.ParseStreamMessage(
            pipeline.Skip(firstResult.Consumed).ToArray(), Options());
        SipParseResult thirdResult = codec.ParseStreamMessage(
            pipeline.Skip(firstResult.Consumed + secondResult.Consumed).ToArray(), Options());

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Consumed, Is.EqualTo(first.Length));
            Assert.That(secondResult.Consumed, Is.EqualTo(second.Length));
            Assert.That(secondResult.Message!.Body, Is.EqualTo(body));
            Assert.That(thirdResult.Consumed, Is.EqualTo(third.Length));
            Assert.That(thirdResult.Message!.StatusCode, Is.EqualTo(486));
            Assert.That(thirdResult.Message.Body, Is.EqualTo(Bytes("tail")));
        });
    }

    [Test]
    public void ExactConfiguredMessageLimitsAreInclusive()
    {
        string start = "MESSAGE sip:bob@example.com SIP/2.0\r\n";
        string firstHeader = "X-Test: value\r\n";
        string lengthHeader = "Content-Length: 3\r\n";
        string headerSection = firstHeader + lengthHeader + "\r\n";
        byte[] source = Bytes(start + headerSection + "abc");
        SipParseOptions exact = Options();
        exact.MaxStartLineBytes = Bytes(start).Length;
        exact.MaxHeaderLineBytes = Bytes(lengthHeader).Length;
        exact.MaxHeaderBytes = Bytes(headerSection).Length;
        exact.MaxHeaderCount = 2;
        exact.MaxBodyBytes = 3;
        exact.MaxMessageBytes = source.Length;

        SipParseResult result = new SipMessageCodec().ParseStreamMessage(source, exact);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Consumed, Is.EqualTo(source.Length));
            Assert.That(result.Message!.Body, Is.EqualTo(Bytes("abc")));
        });
    }

    [Test]
    public void OneBelowEveryConfiguredMessageLimitFails()
    {
        string start = "MESSAGE sip:bob@example.com SIP/2.0\r\n";
        string firstHeader = "X-Test: value\r\n";
        string lengthHeader = "Content-Length: 3\r\n";
        string headerSection = firstHeader + lengthHeader + "\r\n";
        byte[] source = Bytes(start + headerSection + "abc");
        SipParseOptions startLimit = Options();
        startLimit.MaxStartLineBytes = Bytes(start).Length - 1;
        SipParseOptions lineLimit = Options();
        lineLimit.MaxHeaderLineBytes = Bytes(lengthHeader).Length - 1;
        SipParseOptions headerLimit = Options();
        headerLimit.MaxHeaderBytes = Bytes(headerSection).Length - 1;
        SipParseOptions countLimit = Options();
        countLimit.MaxHeaderCount = 1;
        SipParseOptions bodyLimit = Options();
        bodyLimit.MaxBodyBytes = 2;
        SipParseOptions messageLimit = Options();
        messageLimit.MaxMessageBytes = source.Length - 1;

        SipParseResult[] results =
        [
            new SipMessageCodec().ParseStreamMessage(source, startLimit),
            new SipMessageCodec().ParseStreamMessage(source, lineLimit),
            new SipMessageCodec().ParseStreamMessage(source, headerLimit),
            new SipMessageCodec().ParseStreamMessage(source, countLimit),
            new SipMessageCodec().ParseStreamMessage(source, bodyLimit),
            new SipMessageCodec().ParseStreamMessage(source, messageLimit)
        ];

        Assert.That(results.Select(value => value.Status), Is.All.EqualTo(SipParseStatus.Error));
        Assert.That(results.Select(value => value.ErrorOffset), Is.All.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void PartialInputFailsAsSoonAsEachConfiguredLimitIsReached()
    {
        byte[] partialStart = Bytes("OPTIONS sip:service@example.com");
        string completeStart = "OPTIONS sip:service@example.com SIP/2.0\r\n";
        byte[] partialHeader = Bytes("X-Test: unterminated");
        byte[] headerInput = Bytes(completeStart).Concat(partialHeader).ToArray();
        SipParseOptions startLimit = Options();
        startLimit.MaxStartLineBytes = partialStart.Length;
        SipParseOptions startMessageLimit = Options();
        startMessageLimit.MaxMessageBytes = partialStart.Length;
        SipParseOptions lineLimit = Options();
        lineLimit.MaxHeaderLineBytes = partialHeader.Length;
        SipParseOptions headerLimit = Options();
        headerLimit.MaxHeaderBytes = partialHeader.Length;
        SipParseOptions headerMessageLimit = Options();
        headerMessageLimit.MaxMessageBytes = headerInput.Length;

        SipParseResult limitedStart = new SipMessageCodec().ParseStreamMessage(partialStart, startLimit);
        SipParseResult limitedStartMessage = new SipMessageCodec().ParseStreamMessage(
            partialStart, startMessageLimit);
        SipParseResult limitedLine = new SipMessageCodec().ParseStreamMessage(headerInput, lineLimit);
        SipParseResult limitedHeaders = new SipMessageCodec().ParseStreamMessage(headerInput, headerLimit);
        SipParseResult limitedHeaderMessage = new SipMessageCodec().ParseStreamMessage(
            headerInput, headerMessageLimit);

        Assert.Multiple(() =>
        {
            Assert.That(limitedStart.ErrorOffset, Is.EqualTo(partialStart.Length));
            Assert.That(limitedStartMessage.ErrorOffset, Is.EqualTo(partialStart.Length));
            Assert.That(limitedLine.ErrorOffset, Is.EqualTo(headerInput.Length));
            Assert.That(limitedHeaders.ErrorOffset, Is.EqualTo(headerInput.Length));
            Assert.That(limitedHeaderMessage.ErrorOffset, Is.EqualTo(headerInput.Length));
            Assert.That(new[]
            {
                limitedStart, limitedStartMessage, limitedLine, limitedHeaders, limitedHeaderMessage
            }.Select(value => value.Status), Is.All.EqualTo(SipParseStatus.Error));
        });
    }

    [Test]
    public void RejectsNegativeValueForEveryParserLimit()
    {
        SipParseOptions message = Options();
        message.MaxMessageBytes = -1;
        SipParseOptions body = Options();
        body.MaxBodyBytes = -1;
        SipParseOptions headers = Options();
        headers.MaxHeaderBytes = -1;
        SipParseOptions start = Options();
        start.MaxStartLineBytes = -1;
        SipParseOptions line = Options();
        line.MaxHeaderLineBytes = -1;
        SipParseOptions count = Options();
        count.MaxHeaderCount = -1;
        SipParseOptions uri = Options();
        uri.MaxUriBytes = -1;
        SipParseOptions parameters = Options();
        parameters.MaxParameterCount = -1;
        SipParseOptions list = Options();
        list.MaxListValueCount = -1;
        SipParseOptions[] options = [message, body, headers, start, line, count, uri, parameters, list];

        SipParseResult[] results = options.Select(value =>
            new SipMessageCodec().ParseStreamMessage(new byte[0], value)).ToArray();

        Assert.That(results.Select(value => value.Status), Is.All.EqualTo(SipParseStatus.Error));
        Assert.That(results.Select(value => value.ErrorOffset), Is.All.EqualTo(0));
    }

    [TestCase("Content-Length: 3, 4")]
    [TestCase("Content-Length: +3")]
    [TestCase("Content-Length: 3 4")]
    [TestCase("Content-Length: 3\r\nContent-Length : 4")]
    [TestCase("Content-Length: 3\r\nL:\t4")]
    [TestCase("Content-Length: 3\r\n\t0")]
    public void RejectsContentLengthSmugglingForms(string fields)
    {
        byte[] source = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\n" + fields + "\r\n\r\nabc");

        SipParseResult result = new SipMessageCodec().ParseStreamMessage(source, Options());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), fields);
        Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0), fields);
    }

    [Test]
    public void EqualLengthVariantsCannotCreateAmbiguousFraming()
    {
        string headers = "MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "Content-Length : 003\r\nL:\t3\r\n\r\n";
        byte[] body = [0, 255, 65];
        byte[] trailing = Bytes("NEXT");
        byte[] source = Bytes(headers).Concat(body).Concat(trailing).ToArray();

        SipParseResult result = new SipMessageCodec().ParseStreamMessage(source, Options());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Consumed, Is.EqualTo(Bytes(headers).Length + body.Length));
            Assert.That(result.Message!.Body, Is.EqualTo(body));
            Assert.That(source.Length - result.Consumed, Is.EqualTo(trailing.Length));
        });
    }

    [Test]
    public void TransferEncodingHeaderNeverOverridesContentLengthFraming()
    {
        string headers = "MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "Transfer-Encoding: chunked\r\nContent-Length: 3\r\n\r\n";
        byte[] source = Bytes(headers + "abc5\r\nextra\r\n");

        SipParseResult result = new SipMessageCodec().ParseStreamMessage(source, Options());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Message!.Body, Is.EqualTo(Bytes("abc")));
            Assert.That(result.Consumed, Is.EqualTo(Bytes(headers + "abc").Length));
        });
    }

    [Test]
    public void DatagramAndStreamApplyDistinctMissingLengthRules()
    {
        byte[] headers = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\n\r\n");
        byte[] body = [0, 13, 10, 255];
        byte[] source = headers.Concat(body).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult datagram = codec.ParseDatagramMessage(source, Options());
        SipParseResult stream = codec.ParseStreamMessage(source, Options());

        Assert.Multiple(() =>
        {
            Assert.That(datagram.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(datagram.Consumed, Is.EqualTo(source.Length));
            Assert.That(datagram.Message!.Body, Is.EqualTo(body));
            Assert.That(stream.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(stream.ErrorOffset, Is.EqualTo(headers.Length));
        });
    }

    [TestCase("ACK", SipMethod.Ack)]
    [TestCase("BYE", SipMethod.Bye)]
    [TestCase("CANCEL", SipMethod.Cancel)]
    [TestCase("INFO", SipMethod.Info)]
    [TestCase("INVITE", SipMethod.Invite)]
    [TestCase("MESSAGE", SipMethod.Message)]
    [TestCase("NOTIFY", SipMethod.Notify)]
    [TestCase("OPTIONS", SipMethod.Options)]
    [TestCase("PRACK", SipMethod.Prack)]
    [TestCase("PUBLISH", SipMethod.Publish)]
    [TestCase("REFER", SipMethod.Refer)]
    [TestCase("REGISTER", SipMethod.Register)]
    [TestCase("SUBSCRIBE", SipMethod.Subscribe)]
    [TestCase("UPDATE", SipMethod.Update)]
    [TestCase("X-METHOD", SipMethod.Unknown)]
    public void MapsEveryRequestMethod(string method, SipMethod expected)
    {
        SipParseResult result = new SipMessageCodec().ParseStreamMessage(
            Bytes(method + " sip:service@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n"), Options());

        Assert.That(result.Message!.Method, Is.EqualTo(expected));
    }

    [TestCase(100, SipStatusClass.Provisional)]
    [TestCase(200, SipStatusClass.Successful)]
    [TestCase(300, SipStatusClass.Redirection)]
    [TestCase(400, SipStatusClass.ClientError)]
    [TestCase(500, SipStatusClass.ServerError)]
    [TestCase(600, SipStatusClass.GlobalFailure)]
    [TestCase(700, SipStatusClass.Unknown)]
    public void MapsEveryResponseStatusClass(int status, SipStatusClass expected)
    {
        SipParseResult result = new SipMessageCodec().ParseStreamMessage(
            Bytes("SIP/2.0 " + status + " Result\r\nContent-Length: 0\r\n\r\n"), Options());

        Assert.That(result.Message!.StatusClass, Is.EqualTo(expected));
    }

    [Test]
    public void EveryFailureOffsetMatchesConsumptionAndValidationIssue()
    {
        string start = "OPTIONS sip:service@example.com SIP/2.0\r\n";
        string conflict = start + "Content-Length: 0\r\nl: 1\r\n\r\n";
        string[] malformed =
        [
            "OPTIONS sip:service@example.com SIP/2.0\n",
            "OPTIONS sip:service@example.com SIP/2.0\rX",
            start + " continuation\r\n\r\n",
            start + "Missing-Colon\r\n\r\n",
            start + "Content-Length: invalid\r\n\r\n",
            conflict,
            "\r\n" + start + "Content-Length: invalid\r\n\r\n"
        ];

        foreach (string text in malformed)
        {
            SipParseResult result = new SipMessageCodec().ParseStreamMessage(Bytes(text), Options());

            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), text);
            Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0), text);
            Assert.That(result.Consumed, Is.EqualTo(result.ErrorOffset), text);
            Assert.That(result.ValidationIssues[0].ByteOffset, Is.EqualTo(result.ErrorOffset), text);
        }
    }

    static byte[] Message(string startLine, byte[] body)
    {
        return Bytes(startLine + "\r\nContent-Length: " + body.Length + "\r\n\r\n")
            .Concat(body).ToArray();
    }

    static SipParseOptions Options()
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        return options;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
