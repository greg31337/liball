namespace json.Tests;

public class JsonCanonicalizationGeneratedTests
{
    static readonly string[] NumberLexemes = new string[]
    {
        "0",
        "-0",
        "1.0",
        "-42.50",
        "1E+30",
        "0.000001",
        "1e-7",
        "333333333.33333329",
        "5e-324",
        "1.7976931348623157e308"
    };

    static readonly string[] StringValues = new string[]
    {
        "",
        "plain ASCII",
        "\0\b\t\n\f\r\u001f",
        "quote \" and slash / and reverse solidus \\",
        "é",
        "e\u0301",
        "€",
        "😀",
        "\uE000"
    };

    static readonly string[] PropertyNames = new string[]
    {
        "z",
        "a",
        "aa",
        "\n",
        "é",
        "e\u0301",
        "€",
        "😀",
        "\uE000"
    };

    [Test]
    public void GeneratedJcsModelsAreIdempotentAndVerifyCanonically()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        int seed = 0;
        while (seed < 48)
        {
            Random random = new Random(seed);
            JsonValueKind rootKind = (JsonValueKind)((seed % 6) + 1);
            JsonDocument document = Document(CreateValue(
                random,
                0,
                rootKind));

            byte[] first = canonicalizer.CanonicalizeDocument(document);
            byte[] second = canonicalizer.CanonicalizeUtf8(
                first,
                new JsonReaderOptions());
            byte[] third = canonicalizer.CanonicalizeUtf8(
                second,
                new JsonReaderOptions());
            JsonOperationResult verification =
                canonicalizer.VerifyCanonicalUtf8(
                    first,
                    new JsonReaderOptions());

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.EqualTo(first),
                    $"Second canonicalization changed seed {seed}.");
                Assert.That(third, Is.EqualTo(first),
                    $"Third canonicalization changed seed {seed}.");
                Assert.That(verification.Success, Is.True,
                    $"Canonical verification failed for seed {seed}.");
                Assert.That(verification.BytesConsumed,
                    Is.EqualTo(first.Length),
                    $"Consumed byte count differed for seed {seed}.");
                Assert.That(verification.Diagnostic, Is.Null,
                    $"Canonical verification diagnosed seed {seed}.");
            });
            seed++;
        }
    }

    [Test]
    public void GeneratedModelsRejectValidNoncanonicalByteVariants()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        int seed = 0;
        while (seed < 48)
        {
            Random random = new Random(seed);
            JsonValueKind rootKind = (JsonValueKind)((seed % 6) + 1);
            byte[] canonical = canonicalizer.CanonicalizeDocument(
                Document(CreateValue(random, 0, rootKind)));
            byte[] decorated = AddJsonWhitespace(canonical);

            JsonOperationResult verification =
                canonicalizer.VerifyCanonicalUtf8(
                    decorated,
                    new JsonReaderOptions());

            Assert.Multiple(() =>
            {
                Assert.That(verification.Success, Is.False,
                    $"Whitespace was accepted for seed {seed}.");
                Assert.That(verification.BytesConsumed,
                    Is.EqualTo(decorated.Length),
                    $"Consumed byte count differed for seed {seed}.");
                Assert.That(verification.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.InvalidRawValue),
                    $"Diagnostic differed for seed {seed}.");
            });
            seed++;
        }
    }

    static JsonValue CreateValue(
        Random random,
        int depth,
        JsonValueKind kind)
    {
        JsonValueFactory factory = new JsonValueFactory();
        if (depth >= 4 &&
            (kind == JsonValueKind.Array || kind == JsonValueKind.Object))
        {
            kind = (JsonValueKind)(random.Next(4) + 1);
        }

        if (kind == JsonValueKind.Null)
        {
            return factory.CreateNull();
        }

        if (kind == JsonValueKind.Boolean)
        {
            return factory.CreateBoolean(random.Next(2) == 1);
        }

        if (kind == JsonValueKind.Number)
        {
            return factory.CreateNumber(
                NumberLexemes[random.Next(NumberLexemes.Length)]);
        }

        if (kind == JsonValueKind.String)
        {
            return factory.CreateString(
                StringValues[random.Next(StringValues.Length)]);
        }

        if (kind == JsonValueKind.Array)
        {
            return factory.CreateArray(CreateArrayItems(random, depth));
        }

        return factory.CreateObject(CreateObjectMembers(random, depth));
    }

    static List<JsonValue> CreateArrayItems(Random random, int depth)
    {
        List<JsonValue> items = new List<JsonValue>();
        int count = random.Next(3) + 1;
        int index = 0;
        while (index < count)
        {
            JsonValueKind kind = index == 0 && depth < 3
                ? JsonValueKind.Object
                : (JsonValueKind)(random.Next(6) + 1);
            items.Add(CreateValue(random, depth + 1, kind));
            index++;
        }

        return items;
    }

    static List<JsonMember> CreateObjectMembers(Random random, int depth)
    {
        List<JsonMember> members = new List<JsonMember>();
        int count = random.Next(3) + 1;
        int nameOffset = random.Next(PropertyNames.Length);
        int index = 0;
        while (index < count)
        {
            JsonValueKind kind = index == 0 && depth < 3
                ? JsonValueKind.Array
                : (JsonValueKind)(random.Next(6) + 1);
            JsonMember member = new JsonMember();
            member.Name = PropertyNames[
                (nameOffset + index) % PropertyNames.Length];
            member.Value = CreateValue(random, depth + 1, kind);
            member.SourceOrdinal = index;
            members.Add(member);
            index++;
        }

        members.Reverse();
        return members;
    }

    static byte[] AddJsonWhitespace(byte[] canonical)
    {
        byte[] decorated = new byte[canonical.Length + 3];
        decorated[0] = 0x20;
        decorated[1] = 0x0A;
        global::System.Buffer.BlockCopy(
            canonical,
            0,
            decorated,
            2,
            canonical.Length);
        decorated[decorated.Length - 1] = 0x09;
        return decorated;
    }

    static JsonDocument Document(JsonValue root)
    {
        JsonDocument document = new JsonDocument();
        document.Root = root;
        return document;
    }
}
