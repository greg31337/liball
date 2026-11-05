using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfGenericExtensionPolicyTests
{
    [TestCase(GltfExtensionNames.ExtLightsImageBased, GltfExtensionStatus.MultiVendor)]
    [TestCase(GltfExtensionNames.MsftLod, GltfExtensionStatus.Vendor)]
    [TestCase(GltfExtensionNames.ExtTextureAstc, GltfExtensionStatus.Draft)]
    [TestCase(GltfExtensionNames.KhrXmp, GltfExtensionStatus.Archived)]
    [TestCase("FUTURE_render_graph", GltfExtensionStatus.Unknown)]
    public void ReadWrite_PreservesOpaqueExtensionsExtrasAndUnknownMembersByStatus(
        string extension,
        GltfExtensionStatus status)
    {
        string source = CreateOpaqueDocumentJson(extension, false);

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(source),
            new GltfReadOptions());
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        GltfWriteResult written = new GltfWriter().WriteJsonText(read.Document!, options);
        JsonNode original = JsonNode.Parse(source)!;
        JsonNode output = JsonNode.Parse(written.JsonText!)!;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = extension;
        context.Json = original["extensions"]![extension];
        GltfExtensionResult described = new GltfExtensionHandler().ReadExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(written.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(described.Descriptor!.Status, Is.EqualTo(status));
            Assert.That(read.Document!.PropertyJson.TypedExtensions.ContainsKey(extension), Is.False);
            Assert.That(
                JsonNode.DeepEquals(
                    output["extensions"]![extension],
                    original["extensions"]![extension]),
                Is.True);
            Assert.That(JsonNode.DeepEquals(output["extras"], original["extras"]), Is.True);
            Assert.That(
                JsonNode.DeepEquals(output["futureRoot"], original["futureRoot"]),
                Is.True);
            Assert.That(
                read.Diagnostics.Any(issue => issue.Code == GltfIssueCodes.ArchivedExtension),
                Is.EqualTo(status == GltfExtensionStatus.Archived));
            Assert.That(
                written.Diagnostics.Any(issue => issue.Code == GltfIssueCodes.ArchivedExtension),
                Is.EqualTo(status == GltfExtensionStatus.Archived));
        });

        output["extensions"]![extension]!["values"]![0] = 99;
        Assert.That(
            read.Document!.PropertyJson.Extensions[extension]!["values"]![0]!.GetValue<double>(),
            Is.EqualTo(1.0));
    }

    [TestCase(
        GltfExtensionNames.ExtLightsImageBased,
        "scene",
        "{}",
        GltfIssueCodes.UndefinedProperty)]
    [TestCase(
        GltfExtensionNames.MsftLod,
        "node",
        "{\"ids\":[]}",
        GltfIssueCodes.ArrayLengthNotInList)]
    [TestCase(
        GltfExtensionNames.ExtTextureAstc,
        "texture",
        "{\"source\":-1}",
        GltfIssueCodes.ValueNotInRange)]
    [TestCase(
        GltfExtensionNames.KhrXmp,
        "glTF",
        "{}",
        GltfIssueCodes.UndefinedProperty)]
    public void ValidateExtension_AppliesSchemasToOpaqueKnownStatuses(
        string extension,
        string location,
        string json,
        string expectedCode)
    {
        JsonNode payload = JsonNode.Parse(json)!;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = extension;
        context.ObjectLocation = location;
        context.HasObjectIndex = location != "glTF";
        context.ObjectIndex = 0;
        context.Json = payload;

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Any(issue => issue.Code == expectedCode), Is.True);
            Assert.That(JsonNode.DeepEquals(result.Json, payload), Is.True);
            Assert.That(result.Json, Is.Not.SameAs(payload));
            Assert.That(result.TypedData, Is.Null);
            Assert.That(result.References, Is.Empty);
        });
    }

    [Test]
    public void ValidateExtension_LeavesFuturePayloadSemanticsOpaque()
    {
        JsonObject payload = new JsonObject
        {
            ["reference"] = 5,
            ["mode"] = "future",
            ["nested"] = new JsonObject { ["enabled"] = true }
        };
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = "FUTURE_render_graph";
        context.ObjectLocation = "node";
        context.HasObjectIndex = true;
        context.ObjectIndex = 0;
        context.Json = payload;

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Descriptor!.Status, Is.EqualTo(GltfExtensionStatus.Unknown));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.References, Is.Empty);
            Assert.That(JsonNode.DeepEquals(result.Json, payload), Is.True);
        });
    }

    [Test]
    public void ReadBytes_DistinguishesOptionalFutureDataFromUnknownRequiredData()
    {
        string optionalJson = CreateOpaqueDocumentJson("FUTURE_render_graph", false);
        string requiredJson = CreateOpaqueDocumentJson("FUTURE_render_graph", true);
        GltfReadOptions options = new GltfReadOptions();
        options.Validate = true;
        options.ValidationOptions.ValidateStructure = false;
        options.ValidationOptions.ValidateResources = false;

        GltfReadResult optional = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(optionalJson),
            options);
        GltfReadResult required = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(requiredJson),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(optional.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(optional.Validation.IsValid, Is.True, Format(optional.Validation));
            Assert.That(optional.Document!.PropertyJson.Extensions.ContainsKey(
                "FUTURE_render_graph"), Is.True);
            Assert.That(optional.Diagnostics.Any(issue => issue.Code.StartsWith(
                "REQUIRED_EXTENSION_",
                StringComparison.Ordinal)), Is.False);
            Assert.That(required.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(required.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.RequiredExtensionMetadataUnavailable
                && issue.JsonPointer == "/extensionsRequired/0"), Is.True);
            Assert.That(required.Document!.PropertyJson.Extensions.ContainsKey(
                "FUTURE_render_graph"), Is.True);
        });
    }

    [Test]
    public void WriteJsonText_DeterministicallyOrdersMixedStatusExtensionsAndFreeFormKeys()
    {
        GltfDocument document = new GltfDocument();
        string[] extensions =
        [
            "FUTURE_render_graph",
            GltfExtensionNames.KhrXmp,
            GltfExtensionNames.ExtTextureAstc,
            GltfExtensionNames.MsftLod,
            GltfExtensionNames.ExtLightsImageBased
        ];
        int index = 0;
        while (index < extensions.Length)
        {
            string extension = extensions[index];
            document.ExtensionsUsed.Add(extension);
            document.PropertyJson.Extensions[extension] = new JsonObject
            {
                ["zeta"] = index,
                ["alpha"] = -index
            };
            index++;
        }

        document.PropertyJson.HasExtras = true;
        document.PropertyJson.Extras = new JsonObject
        {
            ["zeta"] = true,
            ["alpha"] = false
        };
        document.PropertyJson.UnknownMembers["zFuture"] = 1;
        document.PropertyJson.UnknownMembers["aFuture"] = 2;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;

        GltfWriteResult first = new GltfWriter().WriteJsonText(document, options);
        GltfWriteResult second = new GltfWriter().WriteJsonText(document, options);
        JsonObject output = JsonNode.Parse(first.JsonText!)!.AsObject();
        string[] sortedExtensions = extensions.OrderBy(
            value => value,
            StringComparer.Ordinal).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first.PrimaryBytes, Is.EqualTo(second.PrimaryBytes));
            Assert.That(
                output["extensionsUsed"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EqualTo(sortedExtensions));
            Assert.That(
                output["extensions"]!.AsObject().Select(value => value.Key),
                Is.EqualTo(sortedExtensions));
            Assert.That(
                output["extensions"]![GltfExtensionNames.MsftLod]!.AsObject()
                    .Select(value => value.Key),
                Is.EqualTo(new[] { "alpha", "zeta" }));
            Assert.That(
                output["extras"]!.AsObject().Select(value => value.Key),
                Is.EqualTo(new[] { "alpha", "zeta" }));
            Assert.That(
                output.Select(value => value.Key).TakeLast(2),
                Is.EqualTo(new[] { "aFuture", "zFuture" }));
        });
    }

    [TestCase(GltfExtensionNames.ExtLightsImageBased, "cannot safely remap")]
    [TestCase(GltfExtensionNames.MsftLod, "cannot safely remap")]
    [TestCase(GltfExtensionNames.ExtTextureAstc, "cannot safely remap")]
    [TestCase(GltfExtensionNames.KhrXmp, "cannot safely remap")]
    [TestCase("FUTURE_render_graph", "no registered metadata support")]
    public void CompactDocument_RefusesUnsafeRequiredExtensionsTransactionally(
        string extension,
        string expectedMessage)
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(extension);
        document.ExtensionsRequired.Add(extension);
        document.PropertyJson.Extensions[extension] = new JsonObject
        {
            ["node"] = 0
        };
        document.Nodes.Add(new GltfNode { Name = "original" });
        GltfEditOptions options = new GltfEditOptions();
        options.ValidateResult = false;

        Assert.That(
            () => new GltfDocumentEditor().CompactDocument(document, options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains(expectedMessage));
        Assert.Multiple(() =>
        {
            Assert.That(document.Nodes, Has.Count.EqualTo(1));
            Assert.That(document.Nodes[0].Name, Is.EqualTo("original"));
            Assert.That(
                document.PropertyJson.Extensions[extension]!["node"]!.GetValue<int>(),
                Is.Zero);
        });
    }

    [Test]
    public void Canonicalize_RefusesOptionalFutureReferencesTransactionally()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add("FUTURE_render_graph");
        document.PropertyJson.Extensions["FUTURE_render_graph"] = new JsonObject
        {
            ["sampler"] = 0
        };
        document.Samplers.Add(new GltfSampler { Name = "zeta" });
        document.Samplers.Add(new GltfSampler { Name = "alpha" });
        GltfTransformPlan plan = new GltfTransformPlan();
        plan.Steps.Add(new GltfTransformStep { Kind = GltfTransformKind.Canonicalize });
        GltfTransformOptions options = new GltfTransformOptions();
        options.ValidateBefore = false;
        options.ValidateAfter = false;

        Assert.That(
            () => new GltfTransformer().TransformDocument(document, plan, options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("cannot safely remap references"));
        Assert.Multiple(() =>
        {
            Assert.That(document.Samplers.Select(sampler => sampler.Name),
                Is.EqualTo(new[] { "zeta", "alpha" }));
            Assert.That(
                document.PropertyJson.Extensions["FUTURE_render_graph"]!["sampler"]!
                    .GetValue<int>(),
                Is.Zero);
        });
    }

    private static string CreateOpaqueDocumentJson(string extension, bool required)
    {
        string requiredMember = required
            ? $",\"extensionsRequired\":[\"{extension}\"]"
            : "";
        return "{\"asset\":{\"version\":\"2.0\"},"
            + $"\"extensionsUsed\":[\"{extension}\"]"
            + requiredMember
            + $",\"extensions\":{{\"{extension}\":{{"
            + "\"values\":[1,true,null],"
            + "\"nested\":{\"zeta\":2,\"alpha\":1},"
            + "\"extras\":{\"payload\":true}}},"
            + "\"extras\":{\"owner\":\"root\"},"
            + "\"futureRoot\":{\"kept\":true}}";
    }

    private static string Format(GltfValidationReport report)
    {
        return string.Join(
            ", ",
            report.Issues.Select(issue =>
                $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }
}
