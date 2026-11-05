using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class DiffuseTransmissionExtensionTests
{
    [Test]
    public void ReaderAndWriter_RoundTripPinnedSnapshotPayload()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"textures\":[{},{}],\"materials\":[{\"extensions\":{"
            + "\"KHR_materials_diffuse_transmission\":{"
            + "\"diffuseTransmissionFactor\":0.6,"
            + "\"diffuseTransmissionTexture\":{\"index\":0,\"extensions\":{"
            + "\"KHR_texture_transform\":{\"texCoord\":2}}},"
            + "\"diffuseTransmissionColorFactor\":[0.8,0.7,0.6],"
            + "\"diffuseTransmissionColorTexture\":{\"index\":1},"
            + "\"future\":true}}}]}";

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());
        GltfDocument document = read.Document!;
        GltfMaterialsDiffuseTransmissionExtension value = document.Materials[0]
            .PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMaterialsDiffuseTransmission]
            .MaterialsDiffuseTransmission!;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        JsonNode written = JsonNode.Parse(
            new GltfWriter().WriteJsonText(document, options).JsonText!)!;
        JsonNode payload = written["materials"]![0]!["extensions"]![
            GltfExtensionNames.KhrMaterialsDiffuseTransmission]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(value.HasDiffuseTransmissionFactor, Is.True);
            Assert.That(value.DiffuseTransmissionFactor, Is.EqualTo(0.6));
            Assert.That(value.DiffuseTransmissionTexture!.Index, Is.Zero);
            Assert.That(value.DiffuseTransmissionTexture.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrTextureTransform].TextureTransform!
                .TextureCoordinate, Is.EqualTo(2));
            Assert.That(value.HasDiffuseTransmissionColorFactor, Is.True);
            Assert.That(value.DiffuseTransmissionColorFactor,
                Is.EqualTo(new[] { 0.8, 0.7, 0.6 }));
            Assert.That(value.DiffuseTransmissionColorTexture!.Index, Is.EqualTo(1));
            Assert.That(value.PropertyJson.UnknownMembers["future"]!.GetValue<bool>(),
                Is.True);
            Assert.That(payload["diffuseTransmissionFactor"]!.GetValue<double>(),
                Is.EqualTo(0.6));
            Assert.That(payload["diffuseTransmissionTexture"]!["extensions"]![
                GltfExtensionNames.KhrTextureTransform]!["texCoord"]!.GetValue<int>(),
                Is.EqualTo(2));
            Assert.That(payload["diffuseTransmissionColorFactor"]![2]!
                .GetValue<double>(), Is.EqualTo(0.6));
            Assert.That(payload["future"]!.GetValue<bool>(), Is.True);
        });
    }

    [Test]
    public void Validation_EnforcesPinnedRangesReferencesAndExclusions()
    {
        GltfDocument document = new GltfDocument();
        document.Textures.Add(new GltfTexture());
        GltfMaterial material = new GltfMaterial();
        material.PropertyJson.Extensions[GltfExtensionNames.KhrMaterialsUnlit] =
            new JsonObject();
        document.Materials.Add(material);
        GltfMaterialsDiffuseTransmissionExtension value =
            new GltfMaterialsDiffuseTransmissionExtension();
        value.HasDiffuseTransmissionFactor = true;
        value.DiffuseTransmissionFactor = 1.1;
        value.DiffuseTransmissionTexture = CreateTexture(0, -1);
        value.HasDiffuseTransmissionColorFactor = true;
        value.DiffuseTransmissionColorFactor = [-0.1, 0.5, 1.1];
        value.DiffuseTransmissionColorTexture = CreateTexture(2, 0);

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(
            CreateContext(document, value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Count(issue =>
                issue.Code == GltfIssueCodes.ValueNotInRange), Is.EqualTo(4));
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnresolvedReference
                && issue.JsonPointer.EndsWith(
                    "/diffuseTransmissionColorTexture/index",
                    StringComparison.Ordinal)), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.MultipleExtensions
                && issue.Severity == GltfValidationSeverity.Warning), Is.True);
            Assert.That(result.References.Select(reference => reference.TargetIndex),
                Is.EqualTo(new[] { 0, 2 }));
        });
    }

    [Test]
    public void RemapExtensionReferences_UpdatesBothTexturesWithoutMutatingInput()
    {
        GltfMaterialsDiffuseTransmissionExtension value =
            new GltfMaterialsDiffuseTransmissionExtension();
        value.DiffuseTransmissionTexture = CreateTexture(1, 0);
        value.DiffuseTransmissionColorTexture = CreateTexture(2, 1);
        GltfExtensionContext context = CreateContext(new GltfDocument(), value);
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Textures[1] = 4;
        remap.Textures[2] = 5;

        GltfExtensionResult result = new GltfExtensionHandler()
            .RemapExtensionReferences(context, remap);

        Assert.Multiple(() =>
        {
            Assert.That(result.Json!["diffuseTransmissionTexture"]!["index"]!
                .GetValue<int>(), Is.EqualTo(4));
            Assert.That(result.Json["diffuseTransmissionColorTexture"]!["index"]!
                .GetValue<int>(), Is.EqualTo(5));
            Assert.That(value.DiffuseTransmissionTexture.Index, Is.EqualTo(1));
            Assert.That(value.DiffuseTransmissionColorTexture.Index, Is.EqualTo(2));
            Assert.That(result.References.Select(reference => reference.TargetIndex),
                Is.EqualTo(new[] { 4, 5 }));
        });
    }

    private static GltfTextureInfo CreateTexture(int index, int coordinate)
    {
        GltfTextureInfo result = new GltfTextureInfo();
        result.Index = index;
        result.HasTextureCoordinate = true;
        result.TextureCoordinate = coordinate;
        return result;
    }

    private static GltfExtensionContext CreateContext(
        GltfDocument document,
        GltfMaterialsDiffuseTransmissionExtension value)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsDiffuseTransmission = value;
        GltfExtensionContext result = new GltfExtensionContext();
        result.Document = document;
        result.Name = GltfExtensionNames.KhrMaterialsDiffuseTransmission;
        result.ObjectLocation = "material";
        result.HasObjectIndex = true;
        result.ObjectIndex = 0;
        result.TypedData = typed;
        return result;
    }
}
