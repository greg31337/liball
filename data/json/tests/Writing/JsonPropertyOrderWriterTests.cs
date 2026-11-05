namespace json.Tests;

public class JsonPropertyOrderWriterTests
{
    [Test]
    public void Utf16JcsAndScalarOrdersDivergeForSupplementaryNames()
    {
        string supplementary = char.ConvertFromUtf32(0x1D11E);
        string privateUse = "\uE000";
        JsonValue root = Object(new List<JsonMember>
        {
            Member(supplementary, "0"),
            Member("aa", "1"),
            Member(privateUse, "2"),
            Member("b", "3"),
            Member("", "4"),
            Member("a", "5")
        });

        string utf16 = Write(root, JsonPropertyOrder.OrdinalUtf16);
        string jcs = Write(root, JsonPropertyOrder.Jcs);
        string scalar = Write(root, JsonPropertyOrder.OrdinalUnicodeScalar);

        string utf16Expected =
            "{\"\":4,\"a\":5,\"aa\":1,\"b\":3," +
            "\"" + supplementary + "\":0,\"" + privateUse + "\":2}";
        string scalarExpected =
            "{\"\":4,\"a\":5,\"aa\":1,\"b\":3," +
            "\"" + privateUse + "\":2,\"" + supplementary + "\":0}";

        Assert.Multiple(() =>
        {
            Assert.That(utf16, Is.EqualTo(utf16Expected));
            Assert.That(jcs, Is.EqualTo(utf16Expected));
            Assert.That(scalar, Is.EqualTo(scalarExpected));
        });
    }

    [Test]
    public void PrefixAndCombiningNamesAreComparedWithoutNormalization()
    {
        string decomposed = "e\u0301";
        string precomposed = "\u00e9";
        JsonValue root = Object(new List<JsonMember>
        {
            Member(precomposed, "4"),
            Member(decomposed, "3"),
            Member("ea", "2"),
            Member("e", "1"),
            Member("", "0")
        });

        string expected =
            "{\"\":0,\"e\":1,\"ea\":2," +
            "\"" + decomposed + "\":3," +
            "\"" + precomposed + "\":4}";

        Assert.Multiple(() =>
        {
            Assert.That(
                Write(root, JsonPropertyOrder.OrdinalUtf16),
                Is.EqualTo(expected));
            Assert.That(
                Write(root, JsonPropertyOrder.OrdinalUnicodeScalar),
                Is.EqualTo(expected));
            Assert.That(
                Write(root, JsonPropertyOrder.Jcs),
                Is.EqualTo(expected));
        });
    }

    [Test]
    public void EqualDuplicateNamesRetainTheirOriginalRelativeOrder()
    {
        JsonMember bFirst = Member("b", "0");
        JsonMember aFirst = Member("a", "1");
        JsonMember bSecond = Member("b", "2");
        JsonMember aSecond = Member("a", "3");
        List<JsonMember> members = new List<JsonMember>
        {
            bFirst,
            aFirst,
            bSecond,
            aSecond
        };
        JsonValue root = Object(members);

        string utf16 = Write(root, JsonPropertyOrder.OrdinalUtf16);
        string scalar = Write(root, JsonPropertyOrder.OrdinalUnicodeScalar);
        string jcs = Write(root, JsonPropertyOrder.Jcs);

        Assert.Multiple(() =>
        {
            Assert.That(utf16,
                Is.EqualTo("{\"a\":1,\"a\":3,\"b\":0,\"b\":2}"));
            Assert.That(scalar, Is.EqualTo(utf16));
            Assert.That(jcs, Is.EqualTo(utf16));
            Assert.That(
                root.ObjectMembers,
                Is.EqualTo(new[] { bFirst, aFirst, bSecond, aSecond }));
        });
    }

    [Test]
    public void SortingRecursesIntoObjectsWithoutReorderingArrays()
    {
        JsonValue nestedObject = Object(new List<JsonMember>
        {
            Member("z", "2"),
            Member("a", "1")
        });
        JsonValue objectInArray = Object(new List<JsonMember>
        {
            Member("z", "4"),
            Member("a", "3")
        });
        JsonValue orderedNumbers = Array(new List<JsonValue>
        {
            Number("2"),
            Number("1")
        });
        JsonValue array = Array(new List<JsonValue>
        {
            objectInArray,
            orderedNumbers
        });
        JsonMember rootZ = Member("z", "0");
        JsonMember rootM = Member("m", array);
        JsonMember rootA = Member("a", nestedObject);
        JsonValue root = Object(new List<JsonMember>
        {
            rootZ,
            rootM,
            rootA
        });

        string output = Write(root, JsonPropertyOrder.OrdinalUtf16);

        Assert.Multiple(() =>
        {
            Assert.That(
                output,
                Is.EqualTo(
                    "{\"a\":{\"a\":1,\"z\":2}," +
                    "\"m\":[{\"a\":3,\"z\":4},[2,1]],\"z\":0}"));
            Assert.That(
                root.ObjectMembers,
                Is.EqualTo(new[] { rootZ, rootM, rootA }));
            Assert.That(array.ArrayItems,
                Is.EqualTo(new[] { objectInArray, orderedNumbers }));
            Assert.That(
                orderedNumbers.ArrayItems?.Select(value => value.Number?.Lexeme),
                Is.EqualTo(new[] { "2", "1" }));
        });
    }

    [Test]
    public void PreserveModeKeepsObjectAndArrayOrderAtEveryDepth()
    {
        JsonValue nested = Object(new List<JsonMember>
        {
            Member("z", "2"),
            Member("a", "1")
        });
        JsonValue array = Array(new List<JsonValue>
        {
            Number("3"),
            Number("1"),
            Number("2")
        });
        JsonValue root = Object(new List<JsonMember>
        {
            Member("z", nested),
            Member("a", array)
        });

        string output = Write(root, JsonPropertyOrder.Preserve);

        Assert.That(
            output,
            Is.EqualTo("{\"z\":{\"z\":2,\"a\":1},\"a\":[3,1,2]}"));
    }

    static string Write(JsonValue value, JsonPropertyOrder propertyOrder)
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.PropertyOrder = propertyOrder;
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Preserve;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        IJsonTokenWriter writer = new JsonUtf8Writer(options, target);

        AssertSuccess(writer.WriteJsonValue(value));
        AssertSuccess(writer.CompleteWriter());
        return System.Text.Encoding.UTF8.GetString(destination.WrittenSpan);
    }

    static JsonValue Object(List<JsonMember> members)
    {
        JsonValueFactory factory = new JsonValueFactory();
        return factory.CreateObject(members);
    }

    static JsonValue Array(List<JsonValue> items)
    {
        JsonValueFactory factory = new JsonValueFactory();
        return factory.CreateArray(items);
    }

    static JsonValue Number(string lexeme)
    {
        JsonValueFactory factory = new JsonValueFactory();
        return factory.CreateNumber(lexeme);
    }

    static JsonMember Member(string name, string lexeme)
    {
        return Member(name, Number(lexeme));
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }
}
