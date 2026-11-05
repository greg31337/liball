using System.Text;

namespace http.Tests;

public class HttpProtocolRandomizedMutationTests
{
    const int Iterations = 64;

    [Test]
    [CancelAfter(5000)]
    public void SeededFieldGrammarsCanonicalizeAndRejectMutations()
    {
        Random random = new Random(14901);
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();

        for (int index = 0; index < Iterations; index++)
        {
            string suffix = RandomToken(random, 3, 12);
            string mediaText = "application/" + suffix +
                "; profile=\"profile " + index + "\"";
            HttpMediaType media = codec.ParseMediaType(Bytes(mediaText));
            byte[] canonicalMedia = codec.SerializeMediaType(media);
            Assert.That(
                codec.SerializeMediaType(codec.ParseMediaType(canonicalMedia)),
                Is.EqualTo(canonicalMedia));

            int quality = random.Next(0, 1001);
            string weightedText = suffix + ";q=" + QualityText(quality);
            List<HttpWeightedValue> weighted = codec.ParseWeightedValues(
                Bytes(weightedText));
            byte[] canonicalWeighted = codec.SerializeWeightedValues(weighted);
            Assert.That(
                codec.SerializeWeightedValues(
                    codec.ParseWeightedValues(canonicalWeighted)),
                Is.EqualTo(canonicalWeighted));

            string languageText = "en-" + RandomLetters(random, 2, 5) +
                ";q=" + QualityText(quality);
            List<HttpLanguageRange> languages = codec.ParseLanguageRanges(
                Bytes(languageText));
            byte[] canonicalLanguages = codec.SerializeLanguageRanges(languages);
            Assert.That(
                codec.SerializeLanguageRanges(
                    codec.ParseLanguageRanges(canonicalLanguages)),
                Is.EqualTo(canonicalLanguages));

            int urgency = random.Next(0, 8);
            string priorityText = random.Next(0, 2) == 0 ?
                "u=" + urgency : "u=" + urgency + ", i";
            HttpPriority priority = codec.ParsePriority(Bytes(priorityText));
            byte[] canonicalPriority = codec.SerializePriority(priority);
            Assert.That(
                codec.SerializePriority(codec.ParsePriority(canonicalPriority)),
                Is.EqualTo(canonicalPriority));

            Assert.Throws<HttpParseException>(() =>
                codec.ParseMediaType(Bytes("application " + suffix)));
            Assert.Throws<HttpParseException>(() =>
                codec.ParseWeightedValues(Bytes(suffix + ";q=1.001")));
            Assert.Throws<HttpParseException>(() =>
                codec.ParseLanguageRanges(Bytes("en-*-" + suffix)));
            Assert.Throws<HttpParseException>(() =>
                codec.ParsePriority(Bytes("u=")));
        }
    }

    [Test]
    [CancelAfter(5000)]
    public void SeededHttp1ChunkBoundariesRoundTripAndRecoverAfterMutations()
    {
        Random random = new Random(14902);

        for (int index = 0; index < Iterations; index++)
        {
            byte[] content = RandomBytes(random, random.Next(1, 97));
            HttpRequest request = Http1Request("/chunks/" + index);
            request.Content = content;
            request.Trailers.Fields.Add(Field("X-Seed", index.ToString()));
            Http1SerializationOptions options = new Http1SerializationOptions();
            options.HasBodyFraming = true;
            options.BodyFraming = Http1BodyFraming.Chunked;
            options.ChunkSize = random.Next(1, 17);
            options.ChunkExtension = "seed=" + index;
            Http1SerializationResult serialized = new Http1Serializer()
                .SerializeHttp1Request(request, options);

            Assert.That(serialized.Status, Is.EqualTo(HttpOperationStatus.Done));
            Http1Parser parser = new Http1Parser();
            List<Http1ParseEvent> events = ParseFragments(
                parser, serialized.Bytes, random);
            Http1ParseEvent complete = events.Last(
                item => item.EventKind == Http1ParseEventKind.Complete);

            Assert.Multiple(() =>
            {
                Assert.That(complete.Request!.Content, Is.EqualTo(content));
                Assert.That(complete.Request.Trailers.Fields.Single().Name,
                    Is.EqualTo("X-Seed"));
                Assert.That(Text(complete.Request.Trailers.Fields.Single().Value),
                    Is.EqualTo(index.ToString()));
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.Content),
                    Is.GreaterThanOrEqualTo(1));
            });

            byte[] mutated = serialized.Bytes.ToArray();
            int firstChunk = FindHeaderEnd(mutated);
            mutated[firstChunk] = (byte)'G';
            Http1Parser recovery = new Http1Parser();
            Http1ParseResult rejected = recovery.ParseHttp1Bytes(mutated);
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.Consumed, Is.InRange(0, mutated.Length));

            recovery.ResetHttp1Parser(new Http1ParserOptions());
            Http1ParseResult reparsed = recovery.ParseHttp1Bytes(serialized.Bytes);
            Assert.That(reparsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reparsed.Events.Last().Request!.Content,
                Is.EqualTo(content));
        }
    }

    [Test]
    [CancelAfter(5000)]
    public void SeededHpackBlocksRoundTripAndMalformedSuffixesDoNotDesynchronize()
    {
        Random random = new Random(14903);

        for (int index = 0; index < Iterations; index++)
        {
            HttpHeaders headers = new HttpHeaders();
            int count = random.Next(1, 8);
            for (int fieldIndex = 0; fieldIndex < count; fieldIndex++)
            {
                headers.Fields.Add(Field(
                    "x-random-" + fieldIndex,
                    RandomToken(random, 0, 32)));
            }

            HpackOptions encodeOptions = new HpackOptions();
            encodeOptions.IndexingMode = HpackIndexingMode.WithoutIndexing;
            encodeOptions.UseHuffman = random.Next(0, 2) == 0;
            HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
                headers, encodeOptions);
            HpackDecoder decoder = new HpackDecoder();
            HpackResult decoded = decoder.DecodeHeaderBlock(
                encoded.Bytes, new HpackOptions());

            Assert.Multiple(() =>
            {
                Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
                AssertHeadersEqual(headers, decoded.Headers);
            });

            byte[] mutated = encoded.Bytes.Concat(new byte[] { 0x80 }).ToArray();
            HpackResult rejected = decoder.DecodeHeaderBlock(
                mutated, new HpackOptions());
            HpackResult afterFailure = decoder.InspectDecoderTable();
            HpackResult recovered = decoder.DecodeHeaderBlock(
                encoded.Bytes, new HpackOptions());
            HpackResult canonical = new HpackEncoder().EncodeHeaderBlock(
                recovered.Headers, encodeOptions);
            HpackResult canonicalDecoded = new HpackDecoder().DecodeHeaderBlock(
                canonical.Bytes, new HpackOptions());

            Assert.Multiple(() =>
            {
                Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
                Assert.That(rejected.IsCompressionError, Is.True);
                Assert.That(rejected.Consumed, Is.InRange(0, mutated.Length));
                Assert.That(afterFailure.DynamicTable, Is.Empty);
                Assert.That(recovered.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(canonicalDecoded.Status,
                    Is.EqualTo(HttpOperationStatus.Done));
                AssertHeadersEqual(headers, recovered.Headers);
                AssertHeadersEqual(headers, canonicalDecoded.Headers);
            });
        }
    }

    [Test]
    [CancelAfter(5000)]
    public void SeededHttp2FramesRoundTripAndLengthMutationsStayBounded()
    {
        Random random = new Random(14904);
        IHttp2FrameCodec codec = new Http2FrameCodec();

        for (int index = 0; index < Iterations; index++)
        {
            Http2Frame frame = RandomFrame(random);
            Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
            Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);
            Http2FrameResult canonical = codec.SerializeHttp2Frame(parsed.Frame!);

            Assert.Multiple(() =>
            {
                Assert.That(serialized.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(parsed.Consumed, Is.EqualTo(serialized.Bytes.Length));
                Assert.That(canonical.Bytes, Is.EqualTo(serialized.Bytes));
            });

            byte[] mutated = serialized.Bytes.ToArray();
            int declaredLength = ReadFrameLength(mutated) + 1;
            WriteFrameLength(mutated, declaredLength);
            Http2FrameResult incomplete = codec.ParseHttp2Frame(mutated);
            Http2FrameResult recovered = codec.ParseHttp2Frame(serialized.Bytes);

            Assert.Multiple(() =>
            {
                Assert.That(incomplete.Status,
                    Is.EqualTo(HttpOperationStatus.NeedInput));
                Assert.That(incomplete.Consumed, Is.Zero);
                Assert.That(incomplete.RequiredSize,
                    Is.EqualTo(mutated.Length + 1));
                Assert.That(recovered.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(codec.SerializeHttp2Frame(recovered.Frame!).Bytes,
                    Is.EqualTo(serialized.Bytes));
            });
        }
    }

    [Test]
    [CancelAfter(5000)]
    public void SeededConnectionActionSequencesRemainSynchronizedAndResetCleanly()
    {
        Random random = new Random(14905);
        Http2Connection client = new Http2Connection();
        Http2Connection server = new Http2Connection();
        MakeReady(client, server);
        List<int> streams = new List<int>();

        for (int index = 0; index < 24; index++)
        {
            HttpRequest request = Http2Request(
                "/random/" + index + "?value=" + random.Next(0, 10000));
            Http2ConnectionInput input = new Http2ConnectionInput();
            input.Request = request;
            input.EndStream = true;
            Http2ConnectionResult sent = client.ProcessHttp2Input(input);
            streams.Add(sent.Actions.Single().StreamId);
            List<Http2Event> received = TransmitFragments(sent, server, random);

            Assert.Multiple(() =>
            {
                Assert.That(sent.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(sent.Actions.Single().ActionKind,
                    Is.EqualTo(Http2ActionKind.SendMessage));
                Assert.That(received.Select(item => item.EventKind),
                    Is.EqualTo(new[]
                    {
                        Http2EventKind.MessageHead,
                        Http2EventKind.StreamComplete
                    }));
                Assert.That(received[0].Request!.Target, Is.EqualTo(request.Target));
            });
        }

        Shuffle(streams, random);
        foreach (int streamId in streams)
        {
            bool hasContent = random.Next(0, 2) == 0;
            HttpResponse response = Http2Response(
                hasContent ? HttpStatusCodes.Ok : HttpStatusCodes.NoContent);
            Http2ConnectionInput head = new Http2ConnectionInput();
            head.Response = response;
            head.HasStreamId = true;
            head.StreamId = streamId;
            head.EndStream = !hasContent;
            Http2ConnectionResult sentHead = server.ProcessHttp2Input(head);
            List<Http2Event> headEvents = TransmitFragments(
                sentHead, client, random);

            if (hasContent)
            {
                byte[] body = Bytes("body-" + streamId);
                Http2ConnectionResult sentBody = QueueContent(
                    server, streamId, body);
                List<Http2Event> bodyEvents = TransmitFragments(
                    sentBody, client, random);
                Assert.That(headEvents.Select(item => item.EventKind),
                    Is.EqualTo(new[] { Http2EventKind.MessageHead }));
                Assert.That(bodyEvents.Select(item => item.EventKind),
                    Is.EqualTo(new[]
                    {
                        Http2EventKind.Content,
                        Http2EventKind.StreamComplete
                    }));
                Assert.That(bodyEvents.Last().Response!.Content,
                    Is.EqualTo(body));
            }
            else
            {
                Assert.That(headEvents.Select(item => item.EventKind),
                    Is.EqualTo(new[]
                    {
                        Http2EventKind.MessageHead,
                        Http2EventKind.StreamComplete
                    }));
            }
        }

        Http2ConnectionSnapshot completed = client.InspectHttp2Connection();
        Assert.That(completed.Streams, Has.Count.EqualTo(streams.Count));
        Assert.That(completed.Streams.Select(item => item.StreamState),
            Is.All.EqualTo(Http2StreamState.Closed));

        Http2Frame ping = new Http2Frame();
        ping.FrameType = Http2FrameType.Ping;
        ping.OpaqueData = RandomBytes(random, 8);
        byte[] invalidPing = new Http2FrameCodec()
            .SerializeHttp2Frame(ping).Bytes;
        invalidPing[8] = 1;
        Http2ConnectionResult terminal = ProcessBytes(server, invalidPing);

        Assert.Multiple(() =>
        {
            Assert.That(terminal.IsTerminal, Is.True);
            Assert.That(terminal.ErrorScope, Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(terminal.ErrorCode, Is.EqualTo(Http2ErrorCode.ProtocolError));
        });

        MakeReady(client, server);
        Assert.Multiple(() =>
        {
            Assert.That(client.InspectHttp2Connection().IsReady, Is.True);
            Assert.That(server.InspectHttp2Connection().IsReady, Is.True);
            Assert.That(client.InspectHttp2Connection().Streams, Is.Empty);
            Assert.That(server.InspectHttp2Connection().Streams, Is.Empty);
        });
    }

    static List<Http1ParseEvent> ParseFragments(
        Http1Parser parser,
        byte[] wire,
        Random random)
    {
        List<Http1ParseEvent> events = new List<Http1ParseEvent>();
        int offset = 0;
        while (offset < wire.Length)
        {
            int count = random.Next(1, Math.Min(13, wire.Length - offset) + 1);
            Http1ParseResult result = parser.ParseHttp1Bytes(
                wire.AsSpan(offset, count).ToArray());
            Assert.That(result.Status, Is.Not.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.EqualTo(count));
            events.AddRange(result.Events);
            offset += count;
        }

        Assert.That(events.Last().EventKind,
            Is.EqualTo(Http1ParseEventKind.Complete));
        return events;
    }

    static int FindHeaderEnd(byte[] wire)
    {
        for (int index = 0; index <= wire.Length - 4; index++)
        {
            if (wire[index] == 13 && wire[index + 1] == 10 &&
                wire[index + 2] == 13 && wire[index + 3] == 10)
            {
                return index + 4;
            }
        }

        Assert.Fail("Serialized HTTP/1 message has no field terminator.");
        return -1;
    }

    static Http2Frame RandomFrame(Random random)
    {
        int kind = random.Next(0, 3);
        Http2Frame frame = new Http2Frame();
        if (kind == 0)
        {
            frame.FrameType = Http2FrameType.Data;
            frame.StreamId = 1 + random.Next(0, 1000) * 2;
            frame.Data = RandomBytes(random, random.Next(0, 65));
            frame.EndStream = random.Next(0, 2) == 0;
        }
        else if (kind == 1)
        {
            frame.FrameType = Http2FrameType.Ping;
            frame.OpaqueData = RandomBytes(random, 8);
            frame.Acknowledgement = random.Next(0, 2) == 0;
        }
        else
        {
            frame.FrameType = Http2FrameType.Unknown;
            frame.TypeCode = (byte)random.Next(0x20, 0x100);
            frame.Flags = (byte)random.Next(0, 0x100);
            frame.StreamId = random.Next(0, 1000);
            frame.Payload = RandomBytes(random, random.Next(0, 65));
        }

        return frame;
    }

    static int ReadFrameLength(byte[] wire)
    {
        return wire[0] << 16 | wire[1] << 8 | wire[2];
    }

    static void WriteFrameLength(byte[] wire, int length)
    {
        wire[0] = (byte)(length >> 16);
        wire[1] = (byte)(length >> 8);
        wire[2] = (byte)length;
    }

    static void MakeReady(Http2Connection client, Http2Connection server)
    {
        Http2ConnectionOptions clientOptions = new Http2ConnectionOptions();
        clientOptions.EndpointRole = Http2EndpointRole.Client;
        Http2ConnectionOptions serverOptions = new Http2ConnectionOptions();
        serverOptions.EndpointRole = Http2EndpointRole.Server;
        Http2ConnectionResult clientReset =
            client.ResetHttp2Connection(clientOptions);
        Http2ConnectionResult serverReset =
            server.ResetHttp2Connection(serverOptions);
        Http2ConnectionResult serverReady = Transmit(clientReset, server);
        Http2ConnectionResult clientReady = Transmit(serverReset, client);
        Transmit(clientReady, server);
        Transmit(serverReady, client);
    }

    static List<Http2Event> TransmitFragments(
        Http2ConnectionResult source,
        Http2Connection target,
        Random random)
    {
        byte[] wire = Wire(source);
        List<Http2Event> events = new List<Http2Event>();
        int offset = 0;
        while (offset < wire.Length)
        {
            int count = random.Next(1, Math.Min(11, wire.Length - offset) + 1);
            Http2ConnectionResult result = ProcessBytes(
                target, wire.AsSpan(offset, count).ToArray());
            Assert.That(result.IsTerminal, Is.False);
            events.AddRange(result.Events);
            offset += count;
        }

        return events;
    }

    static Http2ConnectionResult Transmit(
        Http2ConnectionResult source,
        Http2Connection target)
    {
        return ProcessBytes(target, Wire(source));
    }

    static Http2ConnectionResult ProcessBytes(
        Http2Connection connection,
        byte[] bytes)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.HasBytes = true;
        input.Bytes = bytes;
        return connection.ProcessHttp2Input(input);
    }

    static Http2ConnectionResult QueueContent(
        Http2Connection connection,
        int streamId,
        byte[] content)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.StreamId = streamId;
        message.MessageKind = HttpMessageKind.Response;
        message.Content = content;
        message.EndStream = true;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendMessage;
        action.Message = message;
        return connection.QueueHttp2Action(action);
    }

    static byte[] Wire(Http2ConnectionResult result)
    {
        List<byte> bytes = new List<byte>();
        foreach (Http2Action action in result.Actions)
        {
            bytes.AddRange(action.Bytes);
            if (action.Frame != null)
            {
                Http2FrameResult serialized = new Http2FrameCodec()
                    .SerializeHttp2Frame(action.Frame);
                Assert.That(serialized.Status,
                    Is.EqualTo(HttpOperationStatus.Done));
                bytes.AddRange(serialized.Bytes);
            }
        }

        return bytes.ToArray();
    }

    static HttpRequest Http1Request(string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = HttpMethods.Post;
        request.Target = target;
        request.Scheme = "https";
        request.Headers.Fields.Add(Field("Host", "example.test"));
        request.IsComplete = true;
        return request;
    }

    static HttpRequest Http2Request(string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.Method = HttpMethods.Get;
        request.Target = target;
        request.TargetForm = HttpRequestTargetForm.Origin;
        request.Scheme = "https";
        request.Authority = "example.test";
        request.Headers.Fields.Add(Field("x-sequence", target));
        return request;
    }

    static HttpResponse Http2Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http2;
        response.StatusCode = statusCode;
        response.IsComplete = true;
        return response;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Bytes(value);
        return field;
    }

    static void AssertHeadersEqual(HttpHeaders expected, HttpHeaders actual)
    {
        Assert.That(actual.Fields, Has.Count.EqualTo(expected.Fields.Count));
        for (int index = 0; index < expected.Fields.Count; index++)
        {
            Assert.That(actual.Fields[index].Name,
                Is.EqualTo(expected.Fields[index].Name));
            Assert.That(actual.Fields[index].Value,
                Is.EqualTo(expected.Fields[index].Value));
        }
    }

    static void Shuffle(List<int> values, Random random)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int other = random.Next(0, index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }

    static byte[] RandomBytes(Random random, int count)
    {
        byte[] result = new byte[count];
        random.NextBytes(result);
        return result;
    }

    static string RandomToken(Random random, int minimum, int maximum)
    {
        int count = random.Next(minimum, maximum + 1);
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder result = new StringBuilder();
        for (int index = 0; index < count; index++)
        {
            result.Append(alphabet[random.Next(0, alphabet.Length)]);
        }

        return result.ToString();
    }

    static string RandomLetters(Random random, int minimum, int maximum)
    {
        int count = random.Next(minimum, maximum + 1);
        const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        StringBuilder result = new StringBuilder();
        for (int index = 0; index < count; index++)
        {
            result.Append(alphabet[random.Next(0, alphabet.Length)]);
        }

        return result.ToString();
    }

    static string QualityText(int quality)
    {
        if (quality == 0)
        {
            return "0";
        }

        if (quality == 1000)
        {
            return "1";
        }

        return "0." + quality.ToString("D3").TrimEnd('0');
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
