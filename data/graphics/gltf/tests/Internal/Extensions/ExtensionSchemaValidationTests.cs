using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class ExtensionSchemaValidationTests
{
    [TestCase(
        GltfExtensionNames.ExtLightsImageBased,
        "scene",
        "{}",
        GltfIssueCodes.UndefinedProperty,
        "/light")]
    [TestCase(
        GltfExtensionNames.MsftLod,
        "node",
        "{\"ids\":[]}",
        GltfIssueCodes.ArrayLengthNotInList,
        "/ids")]
    [TestCase(
        GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness,
        "material",
        "{\"diffuseFactor\":[1,1]}",
        GltfIssueCodes.ArrayLengthNotInList,
        "/diffuseFactor")]
    public void ValidateExtension_AppliesAvailableRegistrySchemas(
        string extensionName,
        string objectLocation,
        string json,
        string issueCode,
        string issueSuffix)
    {
        JsonNode source = JsonNode.Parse(json)!;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = extensionName;
        context.ObjectLocation = objectLocation;
        context.HasObjectIndex = true;
        context.ObjectIndex = 2;
        context.Json = source;

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(JsonNode.DeepEquals(result.Json, source), Is.True);
            Assert.That(result.Json, Is.Not.SameAs(source));
            Assert.That(result.References, Is.Empty);
            Assert.That(result.Diagnostics.Any(issue => issue.Code == issueCode
                && issue.JsonPointer.EndsWith(issueSuffix, StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void ValidateExtension_PreservesUnknownSemanticsWithoutInventingReferences()
    {
        JsonObject source = new JsonObject
        {
            ["ids"] = new JsonArray(4),
            ["futureReference"] = 9,
            ["vendorData"] = new JsonObject { ["enabled"] = true }
        };
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = GltfExtensionNames.MsftLod;
        context.ObjectLocation = "node";
        context.Json = source;

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.References, Is.Empty);
            Assert.That(JsonNode.DeepEquals(result.Json, source), Is.True);
            Assert.That(result.Json!["futureReference"]!.GetValue<int>(), Is.EqualTo(9));
        });

        result.Json!["ids"]![0] = 7;
        Assert.That(source["ids"]![0]!.GetValue<int>(), Is.EqualTo(4));
    }

    [Test]
    public void ValidateDocument_ReportsSchemaFailuresAtTheOriginalExtensionPointer()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.ExtLightsImageBased);
        GltfScene scene = new GltfScene();
        scene.PropertyJson.Extensions[GltfExtensionNames.ExtLightsImageBased] =
            new JsonObject();
        document.Scenes.Add(scene);
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateStructure = false;
        options.ValidateSemantics = false;
        options.ValidateResources = false;

        GltfValidationReport report = new GltfValidator().ValidateDocument(document, options);

        Assert.That(report.Issues.Any(issue =>
            issue.Code == GltfIssueCodes.UndefinedProperty
            && issue.JsonPointer
                == "/scenes/0/extensions/EXT_lights_image_based/light"), Is.True);
    }
}
