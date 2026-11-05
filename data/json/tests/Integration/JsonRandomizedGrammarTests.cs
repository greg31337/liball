namespace json.Tests;

public class JsonRandomizedGrammarTests
{
    const int MaximumDepth = 5;
    const int MaximumNodes = 48;
    const int MaximumDocumentBytes = 8192;
    const int DocumentsPerSeed = 64;

    [TestCase(17)]
    [TestCase(101)]
    [TestCase(997)]
    [TestCase(4099)]
    [TestCase(65537)]
    [TestCase(104729)]
    public void GrammarGeneratedDocumentsRoundTripWithinBounds(int seed)
    {
        Random random = new Random(seed);
        IJsonDocumentParser parser = new JsonDocumentParser();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        IJsonValueComparer comparer = new JsonValueComparer();
        int index = 0;
        while (index < DocumentsPerSeed)
        {
            GrammarGenerator generator = new GrammarGenerator(random);
            string text = generator.GenerateDocument(index % 6);
            byte[] input = System.Text.Encoding.UTF8.GetBytes(text);
            JsonReaderOptions readerOptions = new JsonReaderOptions();
            JsonOperationResult validation = validator.ValidateUtf8(
                input,
                readerOptions);
            JsonDocument first = parser.ParseUtf8(input, readerOptions);
            byte[] serialized = serializer.SerializeUtf8(
                first,
                new JsonWriterOptions());
            JsonDocument second = parser.ParseUtf8(
                serialized,
                readerOptions);
            byte[] repeated = serializer.SerializeUtf8(
                second,
                new JsonWriterOptions());
            DepthVisitor visitor = new DepthVisitor();
            JsonValue root = first.Root
                ?? throw new InvalidOperationException("Expected root value.");
            new JsonDepthFirstTraversal().TraverseValues(root, visitor);

            Assert.Multiple(() =>
            {
                Assert.That(input.Length,
                    Is.LessThanOrEqualTo(MaximumDocumentBytes),
                    Context(seed, index, text));
                Assert.That(generator.GeneratedNodes,
                    Is.LessThanOrEqualTo(MaximumNodes),
                    Context(seed, index, text));
                Assert.That(visitor.MaximumObservedDepth,
                    Is.LessThanOrEqualTo(MaximumDepth),
                    Context(seed, index, text));
                Assert.That(validation.Success, Is.True,
                    Context(seed, index, text));
                Assert.That(first.SourceByteCount, Is.EqualTo(input.Length),
                    Context(seed, index, text));
                Assert.That(comparer.CompareValues(
                    root,
                    second.Root ?? throw new InvalidOperationException(
                        "Expected reparsed root value."),
                    JsonComparisonMode.ExactModel),
                    Is.True,
                    Context(seed, index, text));
                Assert.That(repeated, Is.EqualTo(serialized),
                    Context(seed, index, text));
                Assert.That(validator.ValidateUtf8(
                    serialized,
                    readerOptions).Success,
                    Is.True,
                    Context(seed, index, text));
            });
            index++;
        }
    }

    [TestCase(23)]
    [TestCase(211)]
    [TestCase(2017)]
    [TestCase(8191)]
    [TestCase(131071)]
    [TestCase(524287)]
    public void RandomSingleFaultMutationsAreRejected(int seed)
    {
        Random random = new Random(seed);
        IJsonDocumentParser parser = new JsonDocumentParser();
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        int index = 0;
        while (index < DocumentsPerSeed)
        {
            GrammarGenerator firstGenerator = new GrammarGenerator(random);
            GrammarGenerator secondGenerator = new GrammarGenerator(random);
            string first = firstGenerator.GenerateDocument(index % 6);
            string second = secondGenerator.GenerateDocument((index + 3) % 6);
            string valid = "[" + first + "," + second + "]";
            int delimiterIndex = first.Length + 1;
            Mutation mutation = MutateOnce(
                valid,
                delimiterIndex,
                random.Next(7));
            byte[] validBytes = System.Text.Encoding.UTF8.GetBytes(valid);
            byte[] invalidBytes = System.Text.Encoding.UTF8.GetBytes(mutation.Text);

            JsonDocumentResult baseline = parser.TryParseUtf8(
                validBytes,
                new JsonReaderOptions());
            JsonDocumentResult parsed = parser.TryParseUtf8(
                invalidBytes,
                new JsonReaderOptions());
            JsonOperationResult validated = validator.ValidateUtf8(
                invalidBytes,
                new JsonReaderOptions());

            Assert.Multiple(() =>
            {
                Assert.That(baseline.Success, Is.True,
                    MutationContext(seed, index, mutation, valid));
                Assert.That(mutation.EditCount, Is.EqualTo(1),
                    MutationContext(seed, index, mutation, valid));
                Assert.That(parsed.Success, Is.False,
                    MutationContext(seed, index, mutation, valid));
                Assert.That(validated.Success, Is.False,
                    MutationContext(seed, index, mutation, valid));
                Assert.That(parsed.Diagnostic, Is.Not.Null,
                    MutationContext(seed, index, mutation, valid));
                Assert.That(validated.Diagnostic?.Code,
                    Is.EqualTo(parsed.Diagnostic?.Code),
                    MutationContext(seed, index, mutation, valid));
                Assert.That(parsed.Diagnostic?.Message,
                    Is.Not.Null.And.Not.Empty,
                    MutationContext(seed, index, mutation, valid));
                Assert.That(invalidBytes.Length,
                    Is.LessThanOrEqualTo(MaximumDocumentBytes * 2),
                    MutationContext(seed, index, mutation, valid));
            });
            index++;
        }
    }

    static Mutation MutateOnce(string valid, int delimiterIndex, int mutation)
    {
        Mutation result = new Mutation();
        result.EditCount = 1;
        if (mutation == 0)
        {
            result.Name = "replace delimiter with semicolon";
            result.Text = ReplaceCharacter(valid, delimiterIndex, ';');
        }
        else if (mutation == 1)
        {
            result.Name = "insert adjacent delimiter";
            result.Text = valid.Insert(delimiterIndex, ",");
        }
        else if (mutation == 2)
        {
            result.Name = "insert trailing delimiter";
            result.Text = valid.Insert(valid.Length - 1, ",");
        }
        else if (mutation == 3)
        {
            result.Name = "replace closing container";
            result.Text = ReplaceCharacter(valid, valid.Length - 1, '}');
        }
        else if (mutation == 4)
        {
            result.Name = "insert non-JSON whitespace";
            result.Text = valid.Insert(1, "\v");
        }
        else if (mutation == 5)
        {
            result.Name = "append trailing content";
            result.Text = valid + "x";
        }
        else
        {
            result.Name = "insert invalid token prefix";
            result.Text = valid.Insert(1, "/");
        }

        return result;
    }

    static string ReplaceCharacter(string value, int index, char replacement)
    {
        char[] characters = value.ToCharArray();
        characters[index] = replacement;
        return new string(characters);
    }

    static string Context(int seed, int index, string text)
    {
        return "seed=" + seed + ", index=" + index + ", json=" + text;
    }

    static string MutationContext(
        int seed,
        int index,
        Mutation mutation,
        string valid)
    {
        return "seed=" + seed +
            ", index=" + index +
            ", mutation=" + mutation.Name +
            ", valid=" + valid +
            ", invalid=" + mutation.Text;
    }

    class GrammarGenerator
    {
        static readonly string[] StringFragments = new string[]
        {
            "a",
            "Z",
            "0",
            " ",
            "/",
            "\\\"",
            "\\\\",
            "\\b",
            "\\f",
            "\\n",
            "\\r",
            "\\t",
            "\\u0000",
            "\\u001f",
            "\\u00e9",
            "\\u20AC",
            "\\uD83D\\uDE00",
            "é",
            "e\u0301",
            "€",
            "😀",
            "\u2028",
            "\u2029"
        };

        readonly Random random;
        int remainingNodes;

        internal int GeneratedNodes;

        internal GrammarGenerator(Random random)
        {
            this.random = random;
            remainingNodes = MaximumNodes;
        }

        internal string GenerateDocument(int rootKind)
        {
            return GenerateValue(0, rootKind);
        }

        string GenerateValue(int depth, int requestedKind)
        {
            remainingNodes--;
            GeneratedNodes++;
            int kind = requestedKind;
            if (kind < 0)
            {
                int kindCount = depth < MaximumDepth && remainingNodes > 0
                    ? 6
                    : 4;
                kind = random.Next(kindCount);
            }

            if (depth >= MaximumDepth || remainingNodes <= 0)
            {
                kind %= 4;
            }

            if (kind == 0)
            {
                return "null";
            }

            if (kind == 1)
            {
                return random.Next(2) == 0 ? "false" : "true";
            }

            if (kind == 2)
            {
                return GenerateNumber();
            }

            if (kind == 3)
            {
                return GenerateString();
            }

            if (kind == 4)
            {
                return GenerateArray(depth);
            }

            return GenerateObject(depth);
        }

        string GenerateArray(int depth)
        {
            int count = NextChildCount();
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append('[');
            int index = 0;
            while (index < count && remainingNodes > 0)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(GenerateValue(depth + 1, -1));
                index++;
            }

            builder.Append(']');
            return builder.ToString();
        }

        string GenerateObject(int depth)
        {
            int count = NextChildCount();
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append('{');
            int index = 0;
            while (index < count && remainingNodes > 0)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(GenerateString());
                builder.Append(':');
                builder.Append(GenerateValue(depth + 1, -1));
                index++;
            }

            builder.Append('}');
            return builder.ToString();
        }

        int NextChildCount()
        {
            int maximum = Math.Min(4, remainingNodes);
            return random.Next(maximum + 1);
        }

        string GenerateNumber()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (random.Next(4) == 0)
            {
                builder.Append('-');
            }

            if (random.Next(4) == 0)
            {
                builder.Append('0');
            }
            else
            {
                builder.Append((char)('1' + random.Next(9)));
                AppendDigits(builder, random.Next(7));
            }

            if (random.Next(2) == 0)
            {
                builder.Append('.');
                AppendDigits(builder, random.Next(6) + 1);
            }

            if (random.Next(2) == 0)
            {
                builder.Append(random.Next(2) == 0 ? 'e' : 'E');
                int sign = random.Next(3);
                if (sign == 1)
                {
                    builder.Append('+');
                }
                else if (sign == 2)
                {
                    builder.Append('-');
                }

                AppendDigits(builder, random.Next(4) + 1);
            }

            return builder.ToString();
        }

        void AppendDigits(System.Text.StringBuilder builder, int count)
        {
            int index = 0;
            while (index < count)
            {
                builder.Append((char)('0' + random.Next(10)));
                index++;
            }
        }

        string GenerateString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append('"');
            int count = random.Next(13);
            int index = 0;
            while (index < count)
            {
                builder.Append(StringFragments[random.Next(StringFragments.Length)]);
                index++;
            }

            builder.Append('"');
            return builder.ToString();
        }
    }

    class DepthVisitor : IJsonValueVisitor
    {
        internal int MaximumObservedDepth;

        public JsonVisitAction VisitValue(JsonVisit visit)
        {
            MaximumObservedDepth = Math.Max(MaximumObservedDepth, visit.Depth);
            return JsonVisitAction.Continue;
        }
    }

    class Mutation
    {
        internal string Name = "";
        internal string Text = "";
        internal int EditCount;
    }
}
