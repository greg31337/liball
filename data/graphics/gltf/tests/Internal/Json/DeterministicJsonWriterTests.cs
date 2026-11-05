using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class DeterministicJsonWriterTests
{
    [Test]
    public void WriteCanonicalValue_MatchesTheRfc8785PrimitiveExample()
    {
        JsonObject source = new JsonObject();
        source["numbers"] = new JsonArray(
            JsonValue.Create(333333333.33333329),
            JsonValue.Create(1E30),
            JsonValue.Create(4.50),
            JsonValue.Create(2e-3),
            JsonValue.Create(0.000000000000000000000000001));
        source["string"] = JsonValue.Create("€$\u000f\nA'B\"\\\\\"/");
        source["literals"] = new JsonArray(null, JsonValue.Create(true), JsonValue.Create(false));

        string result = new DeterministicJsonWriter().WriteCanonicalValue(source);

        Assert.That(
            result,
            Is.EqualTo(
                """{"literals":[null,true,false],"numbers":[333333333.3333333,1e+30,4.5,0.002,1e-27],"string":"€$\u000f\nA'B\"\\\\\"/"}"""));
    }

    [Test]
    public void OrderValue_UsesOrdinalUtf16NamesRecursivelyAndKeepsArrayOrder()
    {
        JsonObject source = new JsonObject();
        source["€"] = "Euro Sign";
        source["\r"] = "Carriage Return";
        source["דּ"] = "Hebrew Letter Dalet With Dagesh";
        source["1"] = "One";
        source["😀"] = "Emoji: Grinning Face";
        source["\u0080"] = "Control";
        source["ö"] = "Latin Small Letter O With Diaeresis";
        source["array"] = new JsonArray(
            JsonValue.Create(3),
            new JsonObject { ["z"] = 2, ["a"] = 1 },
            JsonValue.Create(1));

        JsonObject ordered = new DeterministicJsonWriter().OrderValue(source)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(
                ordered.Select(member => member.Key),
                Is.EqualTo(new[] { "\r", "1", "array", "\u0080", "ö", "€", "😀", "דּ" }));
            Assert.That(
                ordered["array"]![1]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "a", "z" }));
            Assert.That(
                ordered["array"]!.AsArray().Select(item => item!.GetValueKind()),
                Is.EqualTo(new[]
                {
                    System.Text.Json.JsonValueKind.Number,
                    System.Text.Json.JsonValueKind.Object,
                    System.Text.Json.JsonValueKind.Number
                }));
            Assert.That(ordered["array"]![0]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(ordered["array"]![2]!.GetValue<int>(), Is.EqualTo(1));
        });
    }

    [Test]
    public void WriteCanonicalValue_IsIndependentOfObjectInsertionOrder()
    {
        JsonObject first = new JsonObject
        {
            ["z"] = new JsonObject { ["two"] = 2, ["one"] = 1 },
            ["a"] = new JsonArray(new JsonObject { ["d"] = 4, ["c"] = 3 })
        };
        JsonObject second = new JsonObject
        {
            ["a"] = new JsonArray(new JsonObject { ["c"] = 3, ["d"] = 4 }),
            ["z"] = new JsonObject { ["one"] = 1, ["two"] = 2 }
        };
        DeterministicJsonWriter writer = new DeterministicJsonWriter();

        string firstJson = writer.WriteCanonicalValue(first);
        string secondJson = writer.WriteCanonicalValue(second);

        Assert.That(firstJson, Is.EqualTo(secondJson));
        Assert.That(firstJson, Is.EqualTo("{\"a\":[{\"c\":3,\"d\":4}],\"z\":{\"one\":1,\"two\":2}}"));
    }

    [Test]
    public void WriteCanonicalValue_SupportsJsonElementBackedParsedScalars()
    {
        JsonNode source = JsonNode.Parse(
            "{\"z\":1E30,\"a\":\"text\",\"values\":[true,false,null,0.000001]}")!;

        string result = new DeterministicJsonWriter().WriteCanonicalValue(source);

        Assert.That(
            result,
            Is.EqualTo("{\"a\":\"text\",\"values\":[true,false,null,0.000001],\"z\":1e+30}"));
    }

    [Test]
    public void WriteCanonicalValue_UsesRfc8785NumberThresholdsAndNormalizesNegativeZero()
    {
        double negativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL));
        double powerOfTwo68 = BitConverter.Int64BitsToDouble(0x4430000000000000L);
        JsonArray source = new JsonArray(
            JsonValue.Create(0.0),
            JsonValue.Create(negativeZero),
            JsonValue.Create(double.Epsilon),
            JsonValue.Create(double.MaxValue),
            JsonValue.Create(9007199254740992.0),
            JsonValue.Create(powerOfTwo68),
            JsonValue.Create(1e-6),
            JsonValue.Create(1e-7),
            JsonValue.Create(1e20),
            JsonValue.Create(1e21),
            JsonValue.Create(9007199254740993L));

        string result = new DeterministicJsonWriter().WriteCanonicalValue(source);

        Assert.That(
            result,
            Is.EqualTo(
                "[0,0,5e-324,1.7976931348623157e+308,9007199254740992,295147905179352830000,0.000001,1e-7,100000000000000000000,1e+21,9007199254740992]"));
    }

    [Test]
    public void WriteCanonicalValue_EscapesOnlyJsonSyntaxAndRejectsInvalidUnicode()
    {
        JsonObject source = new JsonObject();
        source["value"] = JsonValue.Create("<>/&\b\t\n\f\r\u0001\u001f\"\\😀");

        string result = new DeterministicJsonWriter().WriteCanonicalValue(source);

        Assert.That(
            result,
            Is.EqualTo("""{"value":"<>/&\b\t\n\f\r\u0001\u001f\"\\😀"}"""));
        Assert.That(
            () => new DeterministicJsonWriter().WriteCanonicalValue(JsonValue.Create("\udead")),
            Throws.TypeOf<GltfWriteException>()
                .With.Message.EqualTo("JSON strings cannot contain lone UTF-16 surrogates."));
    }

    [Test]
    public void WriteCanonicalValue_RejectsNonFiniteNumbers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new DeterministicJsonWriter().WriteCanonicalValue(JsonValue.Create(double.NaN)),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo("JSON numbers must be finite."));
            Assert.That(
                () => new DeterministicJsonWriter().WriteCanonicalValue(JsonValue.Create(double.PositiveInfinity)),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo("JSON numbers must be finite."));
            Assert.That(
                () => new DeterministicJsonWriter().WriteCanonicalValue(JsonValue.Create(float.NegativeInfinity)),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo("JSON numbers must be finite."));
        });
    }

    [Test]
    public void CoreWriter_OrdersOnlyFreeFormSubtreesAndKeepsSchemaOrder()
    {
        GltfDocument document = new GltfDocument();
        document.Accessors.Add(new GltfAccessor());
        document.PropertyJson.HasExtras = true;
        document.PropertyJson.Extras = new JsonObject
        {
            ["z"] = new JsonObject { ["two"] = 2, ["one"] = 1 },
            ["a"] = new JsonArray(new JsonObject { ["d"] = 4, ["c"] = 3 })
        };

        JsonObject modelJson = new GltfJsonWriter().WriteDocument(document, new GltfWriteOptions());
        string output = new DeterministicJsonWriter().WriteOrderedValue(modelJson);

        Assert.Multiple(() =>
        {
            Assert.That(modelJson.Select(member => member.Key), Is.EqualTo(new[] { "asset", "accessors", "extras" }));
            Assert.That(
                modelJson["extras"]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "a", "z" }));
            Assert.That(
                modelJson["extras"]!["a"]![0]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "c", "d" }));
            Assert.That(output, Does.StartWith("{\"asset\":{\"version\":\"2.0\"},\"accessors\":"));
            Assert.That(output, Does.EndWith("\"extras\":{\"a\":[{\"c\":3,\"d\":4}],\"z\":{\"one\":1,\"two\":2}}}"));
        });
    }

    [Test]
    public void CoreWriter_CanonicalizesFreeFormObjectsOnlyWhenDeterministicIsEnabled()
    {
        GltfDocument document = new GltfDocument();
        document.PropertyJson.HasExtras = true;
        document.PropertyJson.Extras = new JsonObject { ["z"] = 2, ["a"] = 1 };
        GltfWriteOptions deterministic = new GltfWriteOptions();
        GltfWriteOptions insertionOrdered = new GltfWriteOptions();
        insertionOrdered.Deterministic = false;

        JsonObject ordered = new GltfJsonWriter().WriteDocument(document, deterministic);
        JsonObject preserved = new GltfJsonWriter().WriteDocument(document, insertionOrdered);

        Assert.Multiple(() =>
        {
            Assert.That(
                ordered["extras"]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "a", "z" }));
            Assert.That(
                preserved["extras"]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "z", "a" }));
        });
    }
}
