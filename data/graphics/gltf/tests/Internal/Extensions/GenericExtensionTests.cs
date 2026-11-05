using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class GenericExtensionTests
{
    [Test]
    public void ReadAndWrite_PreserveExtensionsExtrasAndUnknownMembersIndependently()
    {
        const string source =
            "{\"asset\":{\"version\":\"2.0\",\"assetFuture\":null},"
            + "\"nodes\":[{\"extensions\":{\"EXT_node\":{\"values\":[1,true,null]}},"
            + "\"extras\":\"node extras\",\"nodeFuture\":{\"x\":2}}],"
            + "\"extensionsUsed\":[\"EXT_node\"],"
            + "\"extensions\":{\"EXT_root\":{\"enabled\":true}},"
            + "\"extras\":null,\"rootFuture\":[\"text\",3]}";
        GltfReadOptions readOptions = new GltfReadOptions();
        GltfDocument document = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(source),
            readOptions).Document!;
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Validate = false;

        GltfWriteResult written = new GltfWriter().WriteJsonText(document, writeOptions);
        JsonObject output = JsonNode.Parse(written.JsonText!)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(document.PropertyJson.HasExtras, Is.True);
            Assert.That(document.PropertyJson.Extras, Is.Null);
            Assert.That(document.Asset.PropertyJson.UnknownMembers.ContainsKey("assetFuture"), Is.True);
            Assert.That(document.Nodes[0].PropertyJson.Extras!.GetValue<string>(), Is.EqualTo("node extras"));
            Assert.That(
                document.Nodes[0].PropertyJson.Extensions["EXT_node"]!["values"]![1]!.GetValue<bool>(),
                Is.True);
            Assert.That(output.ContainsKey("rootFuture"), Is.True);
            Assert.That(output.ContainsKey("extras"), Is.True);
            Assert.That(output["extras"], Is.Null);
            Assert.That(output["asset"]!.AsObject().ContainsKey("assetFuture"), Is.True);
            Assert.That(output["nodes"]![0]!["nodeFuture"]!["x"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(output["extensions"]!["EXT_root"]!["enabled"]!.GetValue<bool>(), Is.True);
        });

        output["rootFuture"]![0] = "changed";
        Assert.That(
            document.PropertyJson.UnknownMembers["rootFuture"]![0]!.GetValue<string>(),
            Is.EqualTo("text"));
    }

    [Test]
    public void WriteDocument_PreservesExtensionsAndExtrasWhenUnknownMembersAreOmitted()
    {
        GltfDocument document = new GltfDocument();
        document.PropertyJson.Extensions["EXT_example"] = new JsonObject { ["value"] = 1 };
        document.PropertyJson.HasExtras = true;
        document.PropertyJson.Extras = new JsonArray(2);
        document.PropertyJson.UnknownMembers["future"] = JsonValue.Create(3);
        GltfWriteOptions options = new GltfWriteOptions();
        options.PreserveUnknownMembers = false;

        JsonObject output = new GltfJsonWriter().WriteDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(output.ContainsKey("extensions"), Is.True);
            Assert.That(output.ContainsKey("extras"), Is.True);
            Assert.That(output.ContainsKey("future"), Is.False);
        });
    }

    [Test]
    public void CollectEncounteredExtensionLocations_FindsNestedPayloadsAndSkipsExtras()
    {
        GltfDocument document = new GltfDocument();
        JsonObject rootPayload = new JsonObject();
        rootPayload["extensions"] = new JsonObject
        {
            ["EXT_nested"] = new JsonObject()
        };
        document.PropertyJson.Extensions["EXT_root"] = rootPayload;
        document.PropertyJson.Extras = new JsonObject
        {
            ["extensions"] = new JsonObject
            {
                ["EXT_extras_only"] = new JsonObject()
            }
        };
        GltfNode node = new GltfNode();
        node.PropertyJson.Extensions["EXT_node"] = new JsonObject();
        document.Nodes.Add(node);

        ExtensionUsageCollector collector = new ExtensionUsageCollector();
        List<EncounteredExtension> locations = collector
            .CollectEncounteredExtensionLocations(document);
        List<string> names = collector.CollectEncounteredDocumentExtensions(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                locations.Select(value => value.JsonPointer),
                Is.EqualTo(new[]
                {
                    "/extensions/EXT_root",
                    "/extensions/EXT_root/extensions/EXT_nested",
                    "/nodes/0/extensions/EXT_node"
                }));
            Assert.That(
                names,
                Is.EqualTo(new[] { "EXT_nested", "EXT_node", "EXT_root" }));
            Assert.That(names, Does.Not.Contain("EXT_extras_only"));
        });
    }

    [Test]
    public void ValidateDocumentRules_ReportsEachUndeclaredExtensionAtItsExactLocation()
    {
        GltfDocument document = new GltfDocument();
        document.PropertyJson.Extensions["EXT_root"] = new JsonObject();
        GltfNode node = new GltfNode();
        node.PropertyJson.Extensions["EXT_node"] = new JsonObject();
        document.Nodes.Add(node);

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                report.Issues.Any(issue =>
                    issue.Code == GltfIssueCodes.UndeclaredExtension
                    && issue.JsonPointer == "/extensions/EXT_root"),
                Is.True);
            Assert.That(
                report.Issues.Any(issue =>
                    issue.Code == GltfIssueCodes.UndeclaredExtension
                    && issue.JsonPointer == "/nodes/0/extensions/EXT_node"),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentRules_RejectsEmptyExtensionNameSegments()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add("EXT__leading");
        document.ExtensionsUsed.Add("EXT_middle__empty");
        document.ExtensionsUsed.Add("EXT_trailing_");
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness);

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                report.Issues.Count(issue =>
                    issue.Code == GltfIssueCodes.InvalidExtensionNameFormat),
                Is.EqualTo(3));
            Assert.That(
                report.Issues.Any(issue => issue.JsonPointer == "/extensionsUsed/3"
                    && issue.Code == GltfIssueCodes.InvalidExtensionNameFormat),
                Is.False);
        });
    }

    [Test]
    public void WriteDocument_DeterministicallyOrdersExtensionSetsAndPropertyMaps()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add("EXT_zeta");
        document.ExtensionsUsed.Add("EXT_alpha");
        document.ExtensionsRequired.Add("EXT_zeta");
        document.ExtensionsRequired.Add("EXT_alpha");
        document.PropertyJson.Extensions["EXT_zeta"] = new JsonObject();
        document.PropertyJson.Extensions["EXT_alpha"] = new JsonObject();
        document.PropertyJson.UnknownMembers["zFuture"] = JsonValue.Create(1);
        document.PropertyJson.UnknownMembers["aFuture"] = JsonValue.Create(2);
        GltfWriteOptions deterministic = new GltfWriteOptions();
        GltfWriteOptions preserved = new GltfWriteOptions();
        preserved.Deterministic = false;

        JsonObject ordered = new GltfJsonWriter().WriteDocument(document, deterministic);
        JsonObject insertionOrdered = new GltfJsonWriter().WriteDocument(document, preserved);

        Assert.Multiple(() =>
        {
            Assert.That(
                ordered["extensionsUsed"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EqualTo(new[] { "EXT_alpha", "EXT_zeta" }));
            Assert.That(
                ordered["extensionsRequired"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EqualTo(new[] { "EXT_alpha", "EXT_zeta" }));
            Assert.That(
                ordered["extensions"]!.AsObject().Select(value => value.Key),
                Is.EqualTo(new[] { "EXT_alpha", "EXT_zeta" }));
            Assert.That(
                ordered.Select(value => value.Key).TakeLast(2),
                Is.EqualTo(new[] { "aFuture", "zFuture" }));
            Assert.That(
                insertionOrdered["extensionsUsed"]!.AsArray()
                    .Select(value => value!.GetValue<string>()),
                Is.EqualTo(new[] { "EXT_zeta", "EXT_alpha" }));
            Assert.That(
                insertionOrdered["extensions"]!.AsObject().Select(value => value.Key),
                Is.EqualTo(new[] { "EXT_zeta", "EXT_alpha" }));
            Assert.That(
                insertionOrdered.Select(value => value.Key).TakeLast(2),
                Is.EqualTo(new[] { "zFuture", "aFuture" }));
        });
    }
}
