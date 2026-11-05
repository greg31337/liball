namespace json.Tests;

public class JsonIncrementalBoundaryTests
{
    [Test]
    public void RepresentativeDocumentMatchesOneShotAtEveryByteBoundary()
    {
        string jsonText =
            " \r\n{\"utf8\":\"€😀\",\"escape\":\"\\\"\\\\\\/\\b\\f\\n\\r\\t" +
            "\\u0041\\uD83D\\uDE00\",\"number\":-12.34e+56," +
            "\"literal\":[true,false,null],\"a/b~c\":{\"x\":0}}\r\n";
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(jsonText);
        List<string> expected = ReadEvents(
            utf8,
            new List<int> { utf8.Length },
            JsonInputMode.SingleValue);

        for (int split = 0; split <= utf8.Length; split++)
        {
            List<string> actual = ReadEvents(
                utf8,
                new List<int> { split, utf8.Length - split },
                JsonInputMode.SingleValue);
            Assert.That(actual, Is.EqualTo(expected), $"split {split}");
        }

        Assert.That(
            ReadEvents(utf8, CreateBytewiseChunks(utf8.Length), JsonInputMode.SingleValue),
            Is.EqualTo(expected),
            "bytewise");
        Assert.That(
            ReadEvents(utf8, CreatePatternedChunks(utf8.Length), JsonInputMode.SingleValue),
            Is.EqualTo(expected),
            "patterned");
    }

    [Test]
    public void GeneratedValuesMatchOneShotAcrossEveryLexicalSplitState()
    {
        List<string> documents = CreateGeneratedDocuments();
        foreach (string document in documents)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(document);
            List<string> expected = ReadEvents(
                utf8,
                new List<int> { utf8.Length },
                JsonInputMode.SingleValue);

            for (int split = 0; split <= utf8.Length; split++)
            {
                List<string> actual = ReadEvents(
                    utf8,
                    new List<int> { split, utf8.Length - split },
                    JsonInputMode.SingleValue);
                Assert.That(actual, Is.EqualTo(expected), $"{document}, split {split}");
            }
        }
    }

    [Test]
    public void CrLfWhitespaceAndStructuralDelimitersMatchUnderBytewiseFeeds()
    {
        string jsonText = "\r\n {\r\n\"a\" : [ 0 ,\r\n 1 , { } , [ ] ]\r\n} \r\n";
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(jsonText);
        List<string> expected = ReadEvents(
            utf8,
            new List<int> { utf8.Length },
            JsonInputMode.SingleValue);

        Assert.That(
            ReadEvents(utf8, CreateBytewiseChunks(utf8.Length), JsonInputMode.SingleValue),
            Is.EqualTo(expected));

        for (int split = 0; split <= utf8.Length; split++)
        {
            Assert.That(
                ReadEvents(
                    utf8,
                    new List<int> { split, utf8.Length - split },
                    JsonInputMode.SingleValue),
                Is.EqualTo(expected),
                $"CRLF/delimiter split {split}");
        }
    }

    [TestCase(JsonInputMode.WhitespaceSeparated, "0 \r\n{}\t\"€\" ")]
    [TestCase(JsonInputMode.JsonTextSequence, "\u001e0\n\u001e{}\r\n\u001e\"€\"\n")]
    public void MultipleValueFramingMatchesAtEveryByteBoundary(
        JsonInputMode inputMode,
        string jsonText)
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(jsonText);
        List<string> expected = ReadEvents(
            utf8,
            new List<int> { utf8.Length },
            inputMode);

        for (int split = 0; split <= utf8.Length; split++)
        {
            List<string> actual = ReadEvents(
                utf8,
                new List<int> { split, utf8.Length - split },
                inputMode);
            Assert.That(actual, Is.EqualTo(expected), $"{inputMode}, split {split}");
        }

        Assert.That(
            ReadEvents(utf8, CreateBytewiseChunks(utf8.Length), inputMode),
            Is.EqualTo(expected),
            $"{inputMode}, bytewise");
    }

    static List<string> CreateGeneratedDocuments()
    {
        List<string> documents = new List<string>
        {
            "true",
            "false",
            "null",
            "0",
            "-0",
            "1234567890",
            "-12.345",
            "6.022e23",
            "1E-9",
            "\"\"",
            "\"ascii\"",
            "\"€\"",
            "\"😀\"",
            "\"a\\\"b\\\\c\\/d\\b\\f\\n\\r\\t\"",
            "\"\\u0000\\u0041\\u20AC\"",
            "\"\\uD83D\\uDE00\"",
            "[]",
            "{}",
            "[null,false,true,0,\"x\",[],{}]",
            "{\"\":0,\"a/b\":1,\"~\":2,\"€\":3}"
        };

        string deep = "0";
        for (int depth = 0; depth < 8; depth++)
        {
            deep = "{\"v\":[" + deep + "]}";
        }

        documents.Add(deep);
        documents.Add(" \t\r\n[1,2,3]\r\n ");
        return documents;
    }

    static List<int> CreateBytewiseChunks(int length)
    {
        List<int> chunks = new List<int>();
        for (int index = 0; index < length; index++)
        {
            chunks.Add(1);
        }

        if (length == 0)
        {
            chunks.Add(0);
        }

        return chunks;
    }

    static List<int> CreatePatternedChunks(int length)
    {
        int[] pattern = new[] { 1, 2, 3, 5, 8 };
        List<int> chunks = new List<int>();
        int remaining = length;
        int patternIndex = 0;
        while (remaining > 0)
        {
            int size = pattern[patternIndex % pattern.Length];
            if (size > remaining)
            {
                size = remaining;
            }

            chunks.Add(size);
            remaining -= size;
            patternIndex++;
        }

        if (length == 0)
        {
            chunks.Add(0);
        }

        return chunks;
    }

    static List<string> ReadEvents(
        byte[] utf8,
        List<int> chunkLengths,
        JsonInputMode inputMode)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = inputMode;
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        IJsonPullReader reader = new JsonPullReader(options);
        List<string> events = new List<string>();
        int offset = 0;
        foreach (int chunkLength in chunkLengths)
        {
            Assert.That(chunkLength, Is.GreaterThanOrEqualTo(0));
            Assert.That(chunkLength, Is.LessThanOrEqualTo(utf8.Length - offset));
            JsonFeedResult feed = reader.FeedUtf8(
                new global::System.ReadOnlySpan<byte>(utf8, offset, chunkLength));
            Assert.Multiple(() =>
            {
                Assert.That(feed.BytesAccepted, Is.EqualTo(chunkLength));
                Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
                Assert.That(feed.Diagnostic, Is.Null);
            });
            offset += chunkLength;
            DrainAvailableEvents(reader, events);
        }

        Assert.That(offset, Is.EqualTo(utf8.Length));
        JsonFeedResult completed = reader.CompleteInput();
        Assert.Multiple(() =>
        {
            Assert.That(completed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
            Assert.That(completed.Diagnostic, Is.Null);
        });
        DrainAvailableEvents(reader, events);

        Assert.Multiple(() =>
        {
            Assert.That(events[^1], Is.EqualTo("S"));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(utf8.Length));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(utf8.Length));
        });
        return events;
    }

    static void DrainAvailableEvents(IJsonPullReader reader, List<string> events)
    {
        int eventCount = 0;
        while (true)
        {
            JsonTokenResult read = reader.ReadToken();
            eventCount++;
            Assert.That(eventCount, Is.LessThan(1000));
            if (read.Status == JsonReadStatus.NeedMoreData)
            {
                return;
            }

            if (read.Status == JsonReadStatus.Error)
            {
                Assert.Fail("Unexpected incremental error: " + read.Diagnostic?.Code);
            }

            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = read.Token
                    ?? throw new InvalidOperationException("Expected token.");
                events.Add(CreateTokenSignature(token));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                events.Add("D");
                continue;
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                events.Add("S");
                return;
            }

            Assert.Fail("Unexpected read status: " + read.Status);
        }
    }

    static string CreateTokenSignature(JsonToken token)
    {
        JsonSourcePosition? start = token.Range?.Start;
        JsonSourcePosition? end = token.Range?.End;
        string raw = token.RawUtf8 == null ? "<null>" : Convert.ToHexString(token.RawUtf8);
        return token.Kind + "|" +
            (token.StringValue ?? "<null>") + "|" +
            (token.Number?.Lexeme ?? "<null>") + "|" +
            token.BooleanValue + "|" +
            token.Depth + "|" +
            (token.Pointer?.OriginalRepresentation ?? "<null>") + "|" +
            raw + "|" +
            FormatPosition(start) + "|" +
            FormatPosition(end);
    }

    static string FormatPosition(JsonSourcePosition? position)
    {
        if (position == null)
        {
            return "<null>";
        }

        return position.ByteOffset + ":" +
            position.Line + ":" +
            position.ByteColumn + ":" +
            position.ScalarColumn;
    }
}
