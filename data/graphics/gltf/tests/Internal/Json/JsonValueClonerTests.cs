using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class JsonValueClonerTests
{
    [Test]
    public void CloneNode_CopiesNestedValuesWithoutSharingMutableNodes()
    {
        JsonObject source = JsonNode.Parse(
            """
            {
              "object": { "text": "value", "enabled": true },
              "array": [1, null, { "nested": 2 }]
            }
            """)!.AsObject();

        JsonObject clone = JsonValueCloner.CloneNode(source)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(clone.ToJsonString(), Is.EqualTo(source.ToJsonString()));
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone["object"], Is.Not.SameAs(source["object"]));
            Assert.That(clone["array"], Is.Not.SameAs(source["array"]));
            Assert.That(clone["array"]![2], Is.Not.SameAs(source["array"]![2]));
        });

        clone["object"]!["text"] = "changed";
        source["array"]![2]!["nested"] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(source["object"]!["text"]!.GetValue<string>(), Is.EqualTo("value"));
            Assert.That(clone["array"]![2]!["nested"]!.GetValue<int>(), Is.EqualTo(2));
        });
    }

    [Test]
    public void CloneNode_OwnsValuesCreatedFromDisposableJsonDocuments()
    {
        JsonNode clone;
        using (JsonDocument document = JsonDocument.Parse("{\"value\":\"owned\"}"))
        {
            JsonNode source = JsonValue.Create(document.RootElement.GetProperty("value"))!;
            clone = JsonValueCloner.CloneNode(source)!;
        }

        Assert.That(clone.GetValue<JsonElement>().GetString(), Is.EqualTo("owned"));
    }

    [Test]
    public void ClonePropertyJson_CopiesEveryContainerAndPreservesNullValues()
    {
        GltfPropertyJson source = new GltfPropertyJson();
        source.Extensions["EXT_test"] = JsonNode.Parse("{\"enabled\":true}");
        source.Extensions["EXT_null"] = null;
        source.Extras = JsonNode.Parse("[1,{\"tag\":\"source\"}]");
        source.HasExtras = true;
        source.UnknownMembers["future"] = JsonNode.Parse("{\"value\":4}");
        source.UnknownMembers["futureNull"] = null;

        GltfPropertyJson clone = JsonValueCloner.ClonePropertyJson(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Extensions, Is.Not.SameAs(source.Extensions));
            Assert.That(clone.UnknownMembers, Is.Not.SameAs(source.UnknownMembers));
            Assert.That(clone.Extensions["EXT_test"], Is.Not.SameAs(source.Extensions["EXT_test"]));
            Assert.That(clone.Extras, Is.Not.SameAs(source.Extras));
            Assert.That(clone.HasExtras, Is.True);
            Assert.That(clone.UnknownMembers["future"], Is.Not.SameAs(source.UnknownMembers["future"]));
            Assert.That(clone.Extensions["EXT_null"], Is.Null);
            Assert.That(clone.UnknownMembers["futureNull"], Is.Null);
        });

        clone.Extensions["EXT_test"]!["enabled"] = false;
        clone.Extras![1]!["tag"] = "clone";
        clone.UnknownMembers["future"]!["value"] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(source.Extensions["EXT_test"]!["enabled"]!.GetValue<bool>(), Is.True);
            Assert.That(source.Extras![1]!["tag"]!.GetValue<string>(), Is.EqualTo("source"));
            Assert.That(source.UnknownMembers["future"]!["value"]!.GetValue<int>(), Is.EqualTo(4));
        });
    }

    [Test]
    public void ReadDocument_DetachesPreservedJsonFromTheParserTree()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            """
            {
              "asset": { "version": "2.0" },
              "extensions": { "EXT_test": { "enabled": true } },
              "extras": { "tag": "owned" },
              "future": { "value": 7 }
            }
            """);
        GltfReadLimits limits = new GltfReadLimits();
        GltfJsonReadResult parsed = new GltfJsonReader().Read(
            bytes,
            limits,
            GltfDuplicateMemberPolicy.Reject);
        GltfDocument document = new GltfSchemaReader().ReadDocument(
            parsed,
            limits,
            true,
            new List<IGltfExtensionHandler>());
        JsonObject root = parsed.Root!.AsObject();

        root["extensions"]!["EXT_test"]!["enabled"] = false;
        root["extras"]!["tag"] = "parser";
        root["future"]!["value"] = 9;
        root.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(document.PropertyJson.Extensions["EXT_test"]!["enabled"]!.GetValue<bool>(), Is.True);
            Assert.That(document.PropertyJson.Extras!["tag"]!.GetValue<string>(), Is.EqualTo("owned"));
            Assert.That(document.PropertyJson.UnknownMembers["future"]!["value"]!.GetValue<double>(), Is.EqualTo(7.0));
        });
    }

    [Test]
    public void CloneNode_ReturnsNullForNullAndClonePropertyJsonRejectsNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonValueCloner.CloneNode(null), Is.Null);
            Assert.That(
                () => JsonValueCloner.ClonePropertyJson(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("source"));
        });
    }
}
