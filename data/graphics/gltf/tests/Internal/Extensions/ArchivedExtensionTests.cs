using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class ArchivedExtensionTests
{
    [Test]
    public void PbrSpecularGlossiness_ReadsAndWritesExplicitValuesLosslessly()
    {
        const string source = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"KHR_materials_pbrSpecularGlossiness\"],"
            + "\"textures\":[{},{}],\"materials\":[{\"extensions\":{"
            + "\"KHR_materials_pbrSpecularGlossiness\":{"
            + "\"diffuseFactor\":[0.8,0.7,0.6,0.5],"
            + "\"diffuseTexture\":{\"index\":0,\"texCoord\":1,\"futureTexture\":true},"
            + "\"specularFactor\":[0.2,0.3,0.4],\"glossinessFactor\":0.75,"
            + "\"specularGlossinessTexture\":{\"index\":1},"
            + "\"extras\":{\"author\":\"legacy\"},\"future\":[1,2]}}}]}";
        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(source),
            new GltfReadOptions());
        GltfDocument document = read.Document!;
        GltfMaterialsPbrSpecularGlossinessExtension typed = document.Materials[0]
            .PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness]
            .MaterialsPbrSpecularGlossiness!;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        GltfWriteResult write = new GltfWriter().WriteJsonText(document, options);
        JsonNode output = JsonNode.Parse(write.JsonText!)!;
        JsonNode originalPayload = JsonNode.Parse(source)!["materials"]![0]!["extensions"]![
            GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness]!;
        JsonNode outputPayload = output["materials"]![0]!["extensions"]![
            GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Diagnostics.Any(IsArchivedWarning), Is.True);
            Assert.That(typed.HasDiffuseFactor, Is.True);
            Assert.That(typed.DiffuseFactor, Is.EqualTo(new[] { 0.8, 0.7, 0.6, 0.5 }));
            Assert.That(typed.DiffuseTexture!.Index, Is.Zero);
            Assert.That(typed.DiffuseTexture.HasTextureCoordinate, Is.True);
            Assert.That(typed.DiffuseTexture.TextureCoordinate, Is.EqualTo(1));
            Assert.That(typed.DiffuseTexture.PropertyJson.UnknownMembers[
                "futureTexture"]!.GetValue<bool>(), Is.True);
            Assert.That(typed.HasSpecularFactor, Is.True);
            Assert.That(typed.SpecularFactor, Is.EqualTo(new[] { 0.2, 0.3, 0.4 }));
            Assert.That(typed.HasGlossinessFactor, Is.True);
            Assert.That(typed.GlossinessFactor, Is.EqualTo(0.75));
            Assert.That(typed.SpecularGlossinessTexture!.Index, Is.EqualTo(1));
            Assert.That(typed.PropertyJson.HasExtras, Is.True);
            Assert.That(typed.PropertyJson.UnknownMembers.ContainsKey("future"), Is.True);
            Assert.That(write.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(write.Diagnostics.Any(IsArchivedWarning), Is.True);
            Assert.That(JsonNode.DeepEquals(outputPayload, originalPayload), Is.True);
        });
    }

    [Test]
    public void PbrSpecularGlossiness_DoesNotEmitImplicitDefaultMembers()
    {
        GltfMaterialsPbrSpecularGlossinessExtension value =
            new GltfMaterialsPbrSpecularGlossinessExtension();
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsPbrSpecularGlossiness = value;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness;
        context.ObjectLocation = "material";
        context.TypedData = typed;

        GltfExtensionResult result = new GltfExtensionHandler().WriteExtension(context);
        JsonObject payload = result.Json!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(payload, Is.Empty);
            Assert.That(result.Diagnostics.Any(IsArchivedWarning), Is.True);
        });
    }

    [Test]
    public void PbrSpecularGlossiness_ValidatesAndRemapsTextureReferences()
    {
        GltfDocument document = new GltfDocument();
        document.Textures.Add(new GltfTexture());
        GltfMaterialsPbrSpecularGlossinessExtension value =
            new GltfMaterialsPbrSpecularGlossinessExtension();
        value.HasDiffuseFactor = true;
        value.DiffuseFactor = [1.2, 0.8, 0.7, 0.6];
        value.DiffuseTexture = CreateTexture(0);
        value.HasGlossinessFactor = true;
        value.GlossinessFactor = 2.0;
        value.SpecularGlossinessTexture = CreateTexture(3);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsPbrSpecularGlossiness = value;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Document = document;
        context.Name = GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness;
        context.ObjectLocation = "material";
        context.HasObjectIndex = true;
        context.ObjectIndex = 0;
        context.TypedData = typed;
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Textures[0] = 4;
        remap.Textures[3] = 5;
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfExtensionResult validation = handler.ValidateExtension(context);
        GltfExtensionResult remapped = handler.RemapExtensionReferences(context, remap);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(validation.Diagnostics.Count(issue =>
                issue.Code == GltfIssueCodes.ValueNotInRange), Is.EqualTo(2));
            Assert.That(validation.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnresolvedReference), Is.True);
            Assert.That(validation.Diagnostics.Any(IsArchivedWarning), Is.True);
            Assert.That(validation.References.Select(reference => reference.TargetIndex),
                Is.EqualTo(new[] { 0, 3 }));
            Assert.That(remapped.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(remapped.Json!["diffuseTexture"]!["index"]!.GetValue<int>(),
                Is.EqualTo(4));
            Assert.That(remapped.Json!["specularGlossinessTexture"]!["index"]!
                .GetValue<int>(), Is.EqualTo(5));
            Assert.That(remapped.References.Select(reference => reference.TargetIndex),
                Is.EqualTo(new[] { 4, 5 }));
            Assert.That(remapped.Diagnostics.Any(IsArchivedWarning), Is.True);
            Assert.That(value.DiffuseTexture!.Index, Is.Zero);
            Assert.That(value.SpecularGlossinessTexture!.Index, Is.EqualTo(3));
        });
    }

    [Test]
    public void LegacyOpaqueExtensions_ReadAndWritePreservePayloadsAndWarn()
    {
        const string source = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"KHR_techniques_webgl\",\"KHR_xmp\"],"
            + "\"extensions\":{\"KHR_techniques_webgl\":{"
            + "\"programs\":[{\"vertexShader\":0,\"fragmentShader\":1}],"
            + "\"future\":{\"enabled\":true}},\"KHR_xmp\":{"
            + "\"@context\":{\"dc\":\"http://purl.org/dc/elements/1.1/\"},"
            + "\"packets\":[{\"dc:title\":\"Archived\"}],\"future\":7}}}";
        JsonNode original = JsonNode.Parse(source)!;
        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(source),
            new GltfReadOptions());
        GltfDocument document = read.Document!;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        GltfWriteResult write = new GltfWriter().WriteJsonText(document, options);
        JsonNode output = JsonNode.Parse(write.JsonText!)!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Diagnostics.Count(IsArchivedWarning), Is.EqualTo(2));
            Assert.That(document.PropertyJson.TypedExtensions.ContainsKey(
                GltfExtensionNames.KhrTechniquesWebgl), Is.False);
            Assert.That(document.PropertyJson.TypedExtensions.ContainsKey(
                GltfExtensionNames.KhrXmp), Is.False);
            Assert.That(write.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(write.Diagnostics.Count(IsArchivedWarning), Is.EqualTo(2));
            Assert.That(JsonNode.DeepEquals(
                output["extensions"]![GltfExtensionNames.KhrTechniquesWebgl],
                original["extensions"]![GltfExtensionNames.KhrTechniquesWebgl]), Is.True);
            Assert.That(JsonNode.DeepEquals(
                output["extensions"]![GltfExtensionNames.KhrXmp],
                original["extensions"]![GltfExtensionNames.KhrXmp]), Is.True);
        });
    }

    private static GltfTextureInfo CreateTexture(int index)
    {
        GltfTextureInfo result = new GltfTextureInfo();
        result.Index = index;
        return result;
    }

    private static bool IsArchivedWarning(GltfValidationIssue issue)
    {
        return issue.Code == GltfIssueCodes.ArchivedExtension
            && issue.Severity == GltfValidationSeverity.Warning;
    }
}
