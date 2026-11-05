using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class GltfJsonOutputTests
{
    [Test]
    public void WriteOutput_ReturnsExactCompactUtf8BytesAndOptionalText()
    {
        GltfDocument document = CreateDocument();
        GltfWriteOptions options = new GltfWriteOptions();

        GltfWriteResult result = new GltfJsonWriter().WriteOutput(document, options, true);
        const string expected =
            """{"asset":{"version":"2.0","generator":"é\nwriter"},"accessors":[{"componentType":5126,"count":1,"type":"SCALAR"}],"extras":{"a":[1,{"b":true}],"emptyArray":[],"emptyObject":{},"z":0}}""";

        Assert.Multiple(() =>
        {
            Assert.That(result.JsonText, Is.EqualTo(expected));
            Assert.That(result.PrimaryBytes, Is.EqualTo(Encoding.UTF8.GetBytes(expected)));
            Assert.That(Encoding.UTF8.GetString(result.PrimaryBytes), Is.EqualTo(result.JsonText));
            Assert.That(result.PrimaryBytes[0], Is.EqualTo((byte)'{'));
        });
    }

    [Test]
    public void WriteOutput_ReturnsStableTwoSpaceIndentedJsonWithoutTrailingNewline()
    {
        GltfDocument document = CreateDocument();
        GltfWriteOptions options = new GltfWriteOptions();
        options.Indented = true;

        GltfWriteResult result = new GltfJsonWriter().WriteOutput(document, options, true);
        const string expected = """
            {
              "asset": {
                "version": "2.0",
                "generator": "é\nwriter"
              },
              "accessors": [
                {
                  "componentType": 5126,
                  "count": 1,
                  "type": "SCALAR"
                }
              ],
              "extras": {
                "a": [
                  1,
                  {
                    "b": true
                  }
                ],
                "emptyArray": [],
                "emptyObject": {},
                "z": 0
              }
            }
            """;

        Assert.Multiple(() =>
        {
            Assert.That(result.JsonText, Is.EqualTo(expected));
            Assert.That(result.PrimaryBytes, Is.EqualTo(Encoding.UTF8.GetBytes(expected)));
            Assert.That(result.JsonText, Does.Not.EndWith("\n"));
            Assert.That(result.JsonText, Does.Not.Contain("\r"));
        });
    }

    [Test]
    public void WriteOutput_FormattingDoesNotChangeValuesOrMemberOrder()
    {
        GltfDocument document = CreateDocument();
        GltfWriteOptions compactOptions = new GltfWriteOptions();
        GltfWriteOptions indentedOptions = new GltfWriteOptions();
        indentedOptions.Indented = true;

        GltfWriteResult compact = new GltfJsonWriter().WriteOutput(document, compactOptions, true);
        GltfWriteResult indented = new GltfJsonWriter().WriteOutput(document, indentedOptions, true);
        JsonObject compactJson = JsonNode.Parse(compact.JsonText!)!.AsObject();
        JsonObject indentedJson = JsonNode.Parse(indented.JsonText!)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(JsonNode.DeepEquals(compactJson, indentedJson), Is.True);
            Assert.That(
                compactJson.Select(member => member.Key),
                Is.EqualTo(indentedJson.Select(member => member.Key)));
            Assert.That(
                compactJson["extras"]!.AsObject().Select(member => member.Key),
                Is.EqualTo(indentedJson["extras"]!.AsObject().Select(member => member.Key)));
            Assert.That(
                compactJson.Select(member => member.Key),
                Is.EqualTo(new[] { "asset", "accessors", "extras" }));
            Assert.That(
                compactJson["extras"]!.AsObject().Select(member => member.Key),
                Is.EqualTo(new[] { "a", "emptyArray", "emptyObject", "z" }));
        });
    }

    [Test]
    public void WriteOutput_CanOmitTextAndReturnsIndependentOwnedByteArrays()
    {
        GltfDocument document = CreateDocument();
        GltfJsonWriter writer = new GltfJsonWriter();

        GltfWriteResult first = writer.WriteOutput(document, new GltfWriteOptions(), false);
        GltfWriteResult second = writer.WriteOutput(document, new GltfWriteOptions(), false);
        byte originalFirstByte = second.PrimaryBytes[0];
        first.PrimaryBytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(first.JsonText, Is.Null);
            Assert.That(second.JsonText, Is.Null);
            Assert.That(first.PrimaryBytes, Is.Not.SameAs(second.PrimaryBytes));
            Assert.That(second.PrimaryBytes[0], Is.EqualTo(originalFirstByte));
            Assert.That(document.PropertyJson.Extras!["z"]!.GetValue<double>(), Is.EqualTo(-0.0));
        });
    }

    [Test]
    public void WriteOutput_PreservesInsertionOrderWhenDeterministicModeIsDisabled()
    {
        GltfDocument document = CreateDocument();
        GltfWriteOptions compactOptions = new GltfWriteOptions();
        compactOptions.Deterministic = false;
        GltfWriteOptions indentedOptions = new GltfWriteOptions();
        indentedOptions.Deterministic = false;
        indentedOptions.Indented = true;

        GltfWriteResult compact = new GltfJsonWriter().WriteOutput(document, compactOptions, true);
        GltfWriteResult indented = new GltfJsonWriter().WriteOutput(document, indentedOptions, true);
        JsonObject compactExtras = JsonNode.Parse(compact.JsonText!)!["extras"]!.AsObject();
        JsonObject indentedExtras = JsonNode.Parse(indented.JsonText!)!["extras"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(
                compactExtras.Select(member => member.Key),
                Is.EqualTo(new[] { "z", "emptyObject", "a", "emptyArray" }));
            Assert.That(
                indentedExtras.Select(member => member.Key),
                Is.EqualTo(compactExtras.Select(member => member.Key)));
        });
    }

    [Test]
    public void WriteOutput_RejectsNullInputsBeforeProducingOutput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfJsonWriter().WriteOutput(null!, new GltfWriteOptions(), true),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => new GltfJsonWriter().WriteOutput(new GltfDocument(), null!, true),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        });
    }

    private static GltfDocument CreateDocument()
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Generator = "é\nwriter";
        GltfAccessor accessor = new GltfAccessor();
        accessor.Count = 1;
        document.Accessors.Add(accessor);
        document.PropertyJson.HasExtras = true;
        document.PropertyJson.Extras = new JsonObject
        {
            ["z"] = JsonValue.Create(-0.0),
            ["emptyObject"] = new JsonObject(),
            ["a"] = new JsonArray(JsonValue.Create(1), new JsonObject { ["b"] = true }),
            ["emptyArray"] = new JsonArray()
        };
        return document;
    }
}
