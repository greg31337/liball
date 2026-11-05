namespace json.Tests;

public class JsonRandomizedPartitionParityTests
{
    [TestCase(31)]
    [TestCase(127)]
    [TestCase(1021)]
    [TestCase(4093)]
    [TestCase(16381)]
    [TestCase(65521)]
    [TestCase(262139)]
    [TestCase(1048573)]
    public void RandomPartitionsMatchOneShotStreamPushPullAndDom(int seed)
    {
        List<InputCase> cases = CreateCases();
        int index = 0;
        while (index < cases.Count)
        {
            InputCase current = cases[index];
            Random random = new Random(seed ^ (index * 7919));
            List<int> chunks = CreateRandomPartition(
                current.Input.Length,
                random);
            Observation oneShot = ObserveOneShot(current);
            Observation dom = ObserveDom(current);
            Observation pull = ObservePull(current, chunks);
            Observation push = ObservePush(current, chunks);
            Observation stream = ObserveStream(
                current,
                seed + (index * 104729));
            string context = Context(seed, current, chunks);

            AssertEquivalent(oneShot, dom, "DOM", context);
            AssertEquivalent(oneShot, pull, "pull", context);
            AssertEquivalent(oneShot, push, "push", context);
            AssertEquivalent(oneShot, stream, "Stream", context);
            index++;
        }
    }

    static List<InputCase> CreateCases()
    {
        List<InputCase> cases = new List<InputCase>();
        cases.Add(Input("root-null", " \r\nnull\t"));
        cases.Add(Input("number", "-1234567890.0123E-45"));
        cases.Add(Input("raw-unicode", "\"é€😀\""));
        cases.Add(Input(
            "escapes",
            "\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0041\\uD834\\uDD1E\""));
        cases.Add(Input(
            "nested",
            "{\"a\":[null,true,false,-0.5e+2,{\"x/y~z\":\"€\"}]}"));
        cases.Add(Input("long-token", "\"" + new string('x', 257) + "😀\""));
        cases.Add(Input("unexpected-character", "/"));
        cases.Add(Input("invalid-whitespace", "[\v0]"));
        cases.Add(Input("invalid-literal", "truX"));
        cases.Add(Input("invalid-number", "01"));
        cases.Add(Input("raw-control", "\"\u0001\""));
        cases.Add(Input("invalid-escape", "\"\\q\""));
        cases.Add(Input("invalid-unicode-escape", "\"\\u12X4\""));
        cases.Add(Input("unexpected-end", "{\"a\":[0"));
        cases.Add(Input("unexpected-token", "[}"));
        cases.Add(Input("missing-colon", "{\"a\" 0}"));
        cases.Add(Input("missing-comma", "[0 1]"));
        cases.Add(Input("trailing-comma", "{\"a\":0,}"));
        cases.Add(Input("trailing-content", "[]x"));
        cases.Add(Input(
            "duplicate",
            "{\"a\":0,\"\\u0061\":1}",
            options => options.DuplicateKeyPolicy =
                JsonDuplicateKeyPolicy.Reject));
        cases.Add(Input(
            "lone-surrogate",
            "\"\\ud800\"",
            options => options.UnicodePolicy =
                JsonUnicodePolicy.UnicodeScalar));
        cases.Add(Input(
            "invalid-utf8",
            new byte[] { 0x22, 0xF0, 0x28, 0x8C, 0x28, 0x22 }));
        cases.Add(Input(
            "initial-bom",
            new byte[] { 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C }));
        return cases;
    }

    static InputCase Input(string name, string text)
    {
        return Input(
            name,
            System.Text.Encoding.UTF8.GetBytes(text),
            null);
    }

    static InputCase Input(
        string name,
        string text,
        System.Action<JsonReaderOptions> configure)
    {
        return Input(
            name,
            System.Text.Encoding.UTF8.GetBytes(text),
            configure);
    }

    static InputCase Input(string name, byte[] input)
    {
        return Input(name, input, null);
    }

    static InputCase Input(
        string name,
        byte[] input,
        System.Action<JsonReaderOptions>? configure)
    {
        InputCase result = new InputCase();
        result.Name = name;
        result.Input = input;
        result.Configure = configure;
        return result;
    }

    static JsonReaderOptions Options(InputCase current)
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumDiagnosticExcerptBytes = 32;
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        options.CaptureRawTokens = true;
        options.CaptureSourceRanges = true;
        options.StreamBufferBytes = 7;
        options.Limits = limits;
        if (current.Configure != null)
        {
            current.Configure(options);
        }

        return options;
    }

    static List<int> CreateRandomPartition(int length, Random random)
    {
        List<int> chunks = new List<int>();
        int remaining = length;
        while (remaining > 0)
        {
            if (random.Next(4) == 0)
            {
                chunks.Add(0);
            }

            int maximum = Math.Min(11, remaining);
            int size = random.Next(maximum) + 1;
            chunks.Add(size);
            remaining -= size;
        }

        if (random.Next(2) == 0 || length == 0)
        {
            chunks.Add(0);
        }

        return chunks;
    }

    static Observation ObserveOneShot(InputCase current)
    {
        JsonOperationResult result = new JsonUtf8Validator().ValidateUtf8(
            current.Input,
            Options(current));
        return Observation.From(result.Success, result.Diagnostic);
    }

    static Observation ObserveDom(InputCase current)
    {
        JsonDocumentResult result = new JsonDocumentParser().TryParseUtf8(
            current.Input,
            Options(current));
        if (result.Success)
        {
            Assert.That(result.Document?.Root, Is.Not.Null, current.Name);
        }

        return Observation.From(result.Success, result.Diagnostic);
    }

    static Observation ObservePull(InputCase current, List<int> chunks)
    {
        IJsonPullReader reader = new JsonPullReader(Options(current));
        int offset = 0;
        foreach (int size in chunks)
        {
            JsonFeedResult feed = reader.FeedUtf8(
                new global::System.ReadOnlySpan<byte>(current.Input, offset, size));
            if (feed.Diagnostic != null)
            {
                return Observation.From(false, feed.Diagnostic);
            }

            Assert.That(feed.BytesAccepted, Is.EqualTo(size), current.Name);
            offset += size;
            Observation? drained = DrainPull(reader, false);
            if (drained != null)
            {
                return drained;
            }
        }

        Assert.That(offset, Is.EqualTo(current.Input.Length), current.Name);
        JsonFeedResult completed = reader.CompleteInput();
        if (completed.Diagnostic != null)
        {
            return Observation.From(false, completed.Diagnostic);
        }

        Observation terminal = DrainPull(reader, true)
            ?? throw new InvalidOperationException("Expected terminal pull result.");
        if (terminal.Success)
        {
            Assert.Multiple(() =>
            {
                Assert.That(reader.GetBytesAccepted(),
                    Is.EqualTo(current.Input.Length), current.Name);
                Assert.That(reader.GetBytesConsumed(),
                    Is.EqualTo(current.Input.Length), current.Name);
            });
        }

        return terminal;
    }

    static Observation? DrainPull(IJsonPullReader reader, bool finalInput)
    {
        int count = 0;
        while (count < 10000)
        {
            count++;
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.Error)
            {
                return Observation.From(
                    false,
                    read.Diagnostic ?? throw new InvalidOperationException(
                        "Expected pull diagnostic."));
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                return Observation.From(true, null);
            }

            if (read.Status == JsonReadStatus.NeedMoreData)
            {
                if (finalInput)
                {
                    throw new InvalidOperationException(
                        "Final pull input requested more data.");
                }

                return null;
            }
        }

        throw new InvalidOperationException("Pull reader did not terminate.");
    }

    static Observation ObservePush(InputCase current, List<int> chunks)
    {
        IJsonPushParser parser = new JsonPushParser(
            Options(current),
            new ContinuingHandler());
        int offset = 0;
        foreach (int size in chunks)
        {
            JsonFeedResult feed = parser.FeedPushUtf8(
                new global::System.ReadOnlySpan<byte>(current.Input, offset, size));
            if (feed.Diagnostic != null)
            {
                return Observation.From(false, feed.Diagnostic);
            }

            Assert.That(feed.BytesAccepted, Is.EqualTo(size), current.Name);
            offset += size;
        }

        Assert.That(offset, Is.EqualTo(current.Input.Length), current.Name);
        JsonFeedResult completed = parser.CompletePushInput();
        if (completed.Diagnostic != null)
        {
            return Observation.From(false, completed.Diagnostic);
        }

        Assert.That(completed.ReadStatus,
            Is.EqualTo(JsonReadStatus.StreamComplete), current.Name);
        Assert.Multiple(() =>
        {
            Assert.That(parser.GetPushBytesAccepted(),
                Is.EqualTo(current.Input.Length), current.Name);
            Assert.That(parser.GetPushBytesConsumed(),
                Is.EqualTo(current.Input.Length), current.Name);
        });
        return Observation.From(true, null);
    }

    static Observation ObserveStream(InputCase current, int seed)
    {
        RandomReadStream stream = new RandomReadStream(current.Input, seed);
        JsonOperationResult result = new JsonUtf8Validator().ValidateStream(
            stream,
            Options(current));
        Assert.That(stream.WasDisposed, Is.False, current.Name);
        return Observation.From(result.Success, result.Diagnostic);
    }

    static void AssertEquivalent(
        Observation expected,
        Observation actual,
        string path,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Success, Is.EqualTo(expected.Success),
                path + ": " + context);
            Assert.That(actual.DiagnosticSignature,
                Is.EqualTo(expected.DiagnosticSignature),
                path + ": " + context);
        });
    }

    static string Context(int seed, InputCase current, List<int> chunks)
    {
        return "seed=" + seed +
            ", case=" + current.Name +
            ", chunks=" + string.Join(",", chunks) +
            ", input=" + Convert.ToHexString(current.Input);
    }

    class InputCase
    {
        internal string Name = "";
        internal byte[] Input = global::System.Array.Empty<byte>();
        internal System.Action<JsonReaderOptions>? Configure;
    }

    class Observation
    {
        internal bool Success;
        internal string DiagnosticSignature = "success";

        internal static Observation From(
            bool success,
            JsonDiagnostic? diagnostic)
        {
            Observation result = new Observation();
            result.Success = success;
            result.DiagnosticSignature = Signature(diagnostic);
            return result;
        }

        static string Signature(JsonDiagnostic? diagnostic)
        {
            if (diagnostic == null)
            {
                return "success";
            }

            return diagnostic.Code + "|" +
                Position(diagnostic.Range?.Start) + "|" +
                Position(diagnostic.Range?.End) + "|" +
                (diagnostic.Pointer ?? "<null>") + "|" +
                diagnostic.Depth + "|" +
                (diagnostic.ExpectedTokens ?? "<null>") + "|" +
                (diagnostic.ActualToken ?? "<null>");
        }

        static string Position(JsonSourcePosition? position)
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

    class ContinuingHandler : IJsonTokenHandler
    {
        public JsonHandlerAction HandleToken(JsonToken token)
        {
            return JsonHandlerAction.Continue;
        }
    }

    class RandomReadStream : Stream
    {
        readonly byte[] input;
        readonly Random random;
        int position;

        internal bool WasDisposed;

        internal RandomReadStream(byte[] input, int seed)
        {
            this.input = input;
            random = new Random(seed);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => input.Length;
        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = input.Length - position;
            if (remaining == 0)
            {
                return 0;
            }

            int maximum = Math.Min(Math.Min(count, remaining), 11);
            int copied = random.Next(maximum) + 1;
            global::System.Array.Copy(
                input,
                position,
                buffer,
                offset,
                copied);
            position += copied;
            return copied;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
