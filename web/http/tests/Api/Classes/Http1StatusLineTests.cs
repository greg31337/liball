using System.Text;

namespace http.Tests;

public class Http1StatusLineTests
{
    [Test]
    public void StatusLinePreservesVersionsKnownAndExtensionStatuses()
    {
        HttpResponse http10 = ResponseFrom("HTTP/1.0 200 OK");
        HttpResponse extension = ResponseFrom("HTTP/1.1 700 Extension Status");

        Assert.Multiple(() =>
        {
            Assert.That(http10.ProtocolVersion, Is.EqualTo(HttpProtocolVersion.Http10));
            Assert.That(http10.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(http10.HasReasonPhrase, Is.True);
            Assert.That(Encoding.ASCII.GetString(http10.ReasonPhrase), Is.EqualTo("OK"));
            Assert.That(extension.ProtocolVersion, Is.EqualTo(HttpProtocolVersion.Http11));
            Assert.That(extension.StatusCode, Is.EqualTo(700));
            Assert.That(Encoding.ASCII.GetString(extension.ReasonPhrase),
                Is.EqualTo("Extension Status"));
        });
    }

    [Test]
    public void EmptyReasonPhraseIsPreservedWhenRequiredSeparatorIsPresent()
    {
        HttpResponse response = ResponseFrom("HTTP/1.1 204 ");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCodes.NoContent));
            Assert.That(response.HasReasonPhrase, Is.True);
            Assert.That(response.ReasonPhrase, Is.Empty);
        });
    }

    [Test]
    public void ReasonPhrasePreservesTabsSpacesAndOpaqueOctetsExactly()
    {
        byte[] prefix = "HTTP/1.1 299 "u8.ToArray();
        byte[] reason = [0x09, 0x20, 0x4f, 0x4b, 0x80, 0xff, 0x20];
        byte[] suffix =
            "\r\nServer: origin\r\nContent-Length: 0\r\n\r\n"u8.ToArray();
        byte[] message = Join(prefix, reason, suffix);

        Http1ParseResult result = ParseBytes(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(message.Length));
            Assert.That(result.Events[0].Response!.ReasonPhrase, Is.EqualTo(reason));
            Assert.That(result.Events[0].Response!.ReasonPhrase, Is.Not.SameAs(reason));
            Assert.That(result.Events[0].Response!.Headers.Fields[0].Name,
                Is.EqualTo(HttpHeaderNames.Server));
        });
    }

    [Test]
    public void InformationalStatusProducesInformationalMessageMetadata()
    {
        Http1ParseResult result = ParseLine("HTTP/1.1 103 Early Hints");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[0].MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(result.Events[1].MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(result.Events[0].Response!.IsInformational, Is.True);
        });
    }

    [TestCase("HTTP/1.2 200 OK")]
    [TestCase("HTTP/2.0 200 OK")]
    [TestCase("http/1.1 200 OK")]
    [TestCase("HTTP/1 200 OK")]
    [TestCase(" HTTP/1.1 200 OK")]
    [TestCase("HTTP/1.1\t200 OK")]
    public void StatusVersionAndItsSeparatorAreStrict(string line)
    {
        Http1ParseResult result = ParseLine(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-status-version"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [TestCase("HTTP/1.1 20 OK")]
    [TestCase("HTTP/1.1 2000 OK")]
    [TestCase("HTTP/1.1 2O0 OK")]
    [TestCase("HTTP/1.1 099 Continue")]
    [TestCase("HTTP/1.1 000 Zero")]
    [TestCase("HTTP/1.1  200 OK")]
    public void StatusCodeMustBeExactlyThreeSupportedDigits(string line)
    {
        Http1ParseResult result = ParseLine(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-status-code"));
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [TestCase("HTTP/1.1 200")]
    [TestCase("HTTP/1.1 200\tOK")]
    public void SpaceAfterStatusCodeIsRequired(string line)
    {
        Http1ParseResult result = ParseLine(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http1-status-line-whitespace"));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [TestCase(0x00)]
    [TestCase(0x0b)]
    [TestCase(0x0c)]
    [TestCase(0x0d)]
    [TestCase(0x1f)]
    [TestCase(0x7f)]
    public void ReasonPhraseRejectsControlOctetsOtherThanTab(int invalid)
    {
        byte[] prefix = "HTTP/1.1 200 valid"u8.ToArray();
        byte[] suffix = "tail\r\n\r\n"u8.ToArray();
        byte[] message = Join(prefix, new[] { (byte)invalid }, suffix);

        Http1ParseResult result = ParseBytes(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-reason-phrase"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void StatusLineLimitIsIndependentFromFieldLimits()
    {
        string line = "HTTP/1.1 200 OK";
        Http1ParserOptions exactOptions = ResponseOptions();
        exactOptions.Limits.MaximumStartLineSize = Encoding.ASCII.GetByteCount(line);
        exactOptions.Limits.MaximumFieldLineSize = 0;
        exactOptions.Limits.MaximumFieldSectionSize = 0;
        exactOptions.Limits.MaximumFieldCount = 0;
        Http1Parser exactParser = new Http1Parser();
        exactParser.ResetHttp1Parser(exactOptions);
        Http1ParseResult exact = exactParser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(line + "\r\n\r\n"));

        Http1ParserOptions shortOptions = ResponseOptions();
        shortOptions.Limits.MaximumStartLineSize = Encoding.ASCII.GetByteCount(line) - 1;
        shortOptions.Limits.MaximumFieldLineSize = int.MaxValue;
        shortOptions.Limits.MaximumFieldSectionSize = int.MaxValue;
        Http1Parser shortParser = new Http1Parser();
        shortParser.ResetHttp1Parser(shortOptions);
        Http1ParseResult first = shortParser.ParseHttp1Bytes("HTTP/1.1"u8.ToArray());
        Http1ParseResult exceeded = shortParser.ParseHttp1Bytes(" 200 OKmore"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(exact.NeedsEndOfInput, Is.True);
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(exceeded.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(exceeded.Consumed,
                Is.EqualTo(shortOptions.Limits.MaximumStartLineSize - 8 + 1));
            Assert.That(exceeded.ErrorCode,
                Is.EqualTo("http1-start-line-limit-exceeded"));
        });
    }

    [Test]
    public void FragmentedStatusLineProducesNoEventUntilHeadCompletes()
    {
        byte[] message = "HTTP/1.1 304 Not Modified\r\n\r\n"u8.ToArray();
        Http1Parser parser = ResponseParser();
        Http1ParseResult result = new Http1ParseResult();
        int index = 0;
        while (index < message.Length)
        {
            result = parser.ParseHttp1Bytes(new[] { message[index] });
            Assert.That(result.Consumed, Is.EqualTo(1));
            if (index < message.Length - 1)
            {
                Assert.That(result.Events, Is.Empty);
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[0].Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.NotModified));
        });
    }

    static HttpResponse ResponseFrom(string line)
    {
        Http1ParseResult result = ParseLine(line);
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return result.Events[0].Response!;
    }

    static Http1ParseResult ParseLine(string line)
    {
        return ParseBytes(Encoding.ASCII.GetBytes(
            line + "\r\nContent-Length: 0\r\n\r\n"));
    }

    static Http1ParseResult ParseBytes(byte[] input)
    {
        return ResponseParser().ParseHttp1Bytes(input);
    }

    static Http1Parser ResponseParser()
    {
        Http1Parser result = new Http1Parser();
        result.ResetHttp1Parser(ResponseOptions());
        return result;
    }

    static Http1ParserOptions ResponseOptions()
    {
        Http1ParserOptions result = new Http1ParserOptions();
        result.MessageKind = HttpMessageKind.Response;
        return result;
    }

    static byte[] Join(byte[] first, byte[] second, byte[] third)
    {
        byte[] result = new byte[first.Length + second.Length + third.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        Array.Copy(third, 0, result, first.Length + second.Length, third.Length);
        return result;
    }
}
