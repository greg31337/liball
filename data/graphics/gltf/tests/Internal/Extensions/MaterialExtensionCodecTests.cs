using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class MaterialExtensionCodecTests
{
    [Test]
    public void ReaderAndWriter_RoundTripAllRatifiedMaterialPayloads()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"textures\":[{},{},{},{},{},{},{},{},{},{},{},{}],"
            + "\"materials\":[{\"emissiveFactor\":[1,0.5,0.25],\"extensions\":{"
            + "\"KHR_materials_anisotropy\":{\"anisotropyStrength\":0.5,"
            + "\"anisotropyRotation\":0.25,\"anisotropyTexture\":{\"index\":0,"
            + "\"extensions\":{\"KHR_texture_transform\":{\"texCoord\":2}}}},"
            + "\"KHR_materials_clearcoat\":{\"clearcoatFactor\":0.4,"
            + "\"clearcoatTexture\":{\"index\":1},\"clearcoatRoughnessFactor\":0.3,"
            + "\"clearcoatRoughnessTexture\":{\"index\":2},"
            + "\"clearcoatNormalTexture\":{\"index\":3,\"scale\":0.8}},"
            + "\"KHR_materials_dispersion\":{\"dispersion\":20},"
            + "\"KHR_materials_emissive_strength\":{\"emissiveStrength\":2},"
            + "\"KHR_materials_ior\":{\"ior\":1.4},"
            + "\"KHR_materials_iridescence\":{\"iridescenceFactor\":0.6,"
            + "\"iridescenceTexture\":{\"index\":4},\"iridescenceIor\":1.2,"
            + "\"iridescenceThicknessMinimum\":80,\"iridescenceThicknessMaximum\":300,"
            + "\"iridescenceThicknessTexture\":{\"index\":5}},"
            + "\"KHR_materials_sheen\":{\"sheenColorFactor\":[0.1,0.2,0.3],"
            + "\"sheenColorTexture\":{\"index\":6},\"sheenRoughnessFactor\":0.7,"
            + "\"sheenRoughnessTexture\":{\"index\":7}},"
            + "\"KHR_materials_specular\":{\"specularFactor\":0.9,"
            + "\"specularTexture\":{\"index\":8},\"specularColorFactor\":[1,2,3],"
            + "\"specularColorTexture\":{\"index\":9}},"
            + "\"KHR_materials_transmission\":{\"transmissionFactor\":0.75,"
            + "\"transmissionTexture\":{\"index\":10}},"
            + "\"KHR_materials_unlit\":{\"future\":true},"
            + "\"KHR_materials_volume\":{\"thicknessFactor\":0.2,"
            + "\"thicknessTexture\":{\"index\":11},\"attenuationDistance\":4,"
            + "\"attenuationColor\":[0.8,0.7,0.6]}}}]}";

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());
        GltfDocument document = read.Document!;
        GltfPropertyJson property = document.Materials[0].PropertyJson;
        GltfMaterialsAnisotropyExtension anisotropy = property.TypedExtensions[
            GltfExtensionNames.KhrMaterialsAnisotropy].MaterialsAnisotropy!;
        GltfMaterialsClearcoatExtension clearcoat = property.TypedExtensions[
            GltfExtensionNames.KhrMaterialsClearcoat].MaterialsClearcoat!;
        GltfMaterialsIridescenceExtension iridescence = property.TypedExtensions[
            GltfExtensionNames.KhrMaterialsIridescence].MaterialsIridescence!;
        GltfMaterialsSheenExtension sheen = property.TypedExtensions[
            GltfExtensionNames.KhrMaterialsSheen].MaterialsSheen!;
        GltfMaterialsSpecularExtension specular = property.TypedExtensions[
            GltfExtensionNames.KhrMaterialsSpecular].MaterialsSpecular!;
        GltfMaterialsVolumeExtension volume = property.TypedExtensions[
            GltfExtensionNames.KhrMaterialsVolume].MaterialsVolume!;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        JsonNode written = JsonNode.Parse(
            new GltfWriter().WriteJsonText(document, options).JsonText!)!;
        JsonNode extensions = written["materials"]![0]!["extensions"]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(anisotropy.AnisotropyStrength, Is.EqualTo(0.5));
            Assert.That(anisotropy.AnisotropyTexture!.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrTextureTransform].TextureTransform!.TextureCoordinate,
                Is.EqualTo(2));
            Assert.That(clearcoat.ClearcoatNormalTexture!.Scale, Is.EqualTo(0.8));
            Assert.That(property.TypedExtensions[GltfExtensionNames.KhrMaterialsDispersion]
                .MaterialsDispersion!.Dispersion, Is.EqualTo(20));
            Assert.That(property.TypedExtensions[GltfExtensionNames.KhrMaterialsEmissiveStrength]
                .MaterialsEmissiveStrength!.EmissiveStrength, Is.EqualTo(2));
            Assert.That(property.TypedExtensions[GltfExtensionNames.KhrMaterialsIor]
                .MaterialsIor!.Ior, Is.EqualTo(1.4));
            Assert.That(iridescence.IridescenceThicknessMaximum, Is.EqualTo(300));
            Assert.That(sheen.SheenColorFactor, Is.EqualTo(new[] { 0.1, 0.2, 0.3 }));
            Assert.That(specular.SpecularColorFactor, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
            Assert.That(property.TypedExtensions[GltfExtensionNames.KhrMaterialsTransmission]
                .MaterialsTransmission!.TransmissionFactor, Is.EqualTo(0.75));
            Assert.That(property.TypedExtensions[GltfExtensionNames.KhrMaterialsUnlit]
                .MaterialsUnlit!.PropertyJson.UnknownMembers["future"]!.GetValue<bool>(), Is.True);
            Assert.That(volume.AttenuationColor, Is.EqualTo(new[] { 0.8, 0.7, 0.6 }));
            Assert.That(extensions[GltfExtensionNames.KhrMaterialsAnisotropy]![
                "anisotropyTexture"]!["extensions"]![GltfExtensionNames.KhrTextureTransform]![
                "texCoord"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(extensions[GltfExtensionNames.KhrMaterialsVolume]![
                "attenuationDistance"]!.GetValue<double>(), Is.EqualTo(4));
        });
    }

    [Test]
    public void Variants_ParseWriteValidateAndRemapReferences()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"accessors\":[{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\"}],"
            + "\"materials\":[{}],\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},"
            + "\"extensions\":{\"KHR_materials_variants\":{\"mappings\":[{"
            + "\"variants\":[0,1],\"material\":0,\"name\":\"map\"}]}}}]}],"
            + "\"extensions\":{\"KHR_materials_variants\":{\"variants\":["
            + "{\"name\":\"Day\"},{\"name\":\"Night\"}]}}}";
        GltfDocument document = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions()).Document!;
        GltfMaterialsVariantsExtension root = document.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsVariants].MaterialsVariants!;
        GltfMaterialsVariantsPrimitiveExtension primitive = document.Meshes[0].Primitives[0]
            .PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            .MaterialsVariantsPrimitive!;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Document = document;
        context.Name = GltfExtensionNames.KhrMaterialsVariants;
        context.ObjectLocation = "mesh.primitive";
        context.HasObjectIndex = true;
        context.ObjectIndex = 0;
        context.Json = document.Meshes[0].Primitives[0].PropertyJson.Extensions[
            GltfExtensionNames.KhrMaterialsVariants];
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Materials[0] = 3;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;

        GltfExtensionResult validated = new GltfExtensionHandler().ValidateExtension(context);
        GltfExtensionResult remapped = new GltfExtensionHandler().RemapExtensionReferences(
            context,
            remap);
        JsonNode written = JsonNode.Parse(
            new GltfWriter().WriteJsonText(document, options).JsonText!)!;

        Assert.Multiple(() =>
        {
            Assert.That(root.Variants.Select(value => value.Name),
                Is.EqualTo(new[] { "Day", "Night" }));
            Assert.That(primitive.Mappings[0].Variants, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(primitive.Mappings[0].Material, Is.Zero);
            Assert.That(validated.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(validated.References.Select(value => value.TargetCollection),
                Is.EqualTo(new[]
                {
                    "extensions/KHR_materials_variants/variants",
                    "extensions/KHR_materials_variants/variants",
                    "materials"
                }));
            Assert.That(remapped.Json!["mappings"]![0]!["material"]!.GetValue<int>(),
                Is.EqualTo(3));
            Assert.That(context.Json!["mappings"]![0]!["material"]!.GetValue<double>(), Is.Zero);
            Assert.That(written["extensions"]![GltfExtensionNames.KhrMaterialsVariants]![
                "variants"]![1]!["name"]!.GetValue<string>(), Is.EqualTo("Night"));
            Assert.That(written["meshes"]![0]!["primitives"]![0]!["extensions"]![
                GltfExtensionNames.KhrMaterialsVariants]!["mappings"]![0]![
                "name"]!.GetValue<string>(), Is.EqualTo("map"));
        });
    }

    [Test]
    public void Validation_ReportsMaterialExtensionRangesAndSemanticRelationships()
    {
        GltfDocument document = new GltfDocument();
        document.Textures.Add(new GltfTexture());
        GltfMaterial material = new GltfMaterial();
        material.HasDoubleSided = true;
        material.DoubleSided = true;
        GltfNormalTextureInfo normal = new GltfNormalTextureInfo();
        normal.Index = 0;
        material.NormalTexture = normal;
        document.Materials.Add(material);
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfMaterialsAnisotropyExtension anisotropy = new GltfMaterialsAnisotropyExtension();
        anisotropy.HasAnisotropyStrength = true;
        anisotropy.AnisotropyStrength = 2.0;
        anisotropy.AnisotropyTexture = CreateTexture(0, 1);
        GltfExtensionResult anisotropyResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsAnisotropy,
                CreateTypedAnisotropy(anisotropy)));

        GltfMaterialsClearcoatExtension clearcoat = new GltfMaterialsClearcoatExtension();
        GltfNormalTextureInfo clearcoatNormal = new GltfNormalTextureInfo();
        clearcoatNormal.Index = 0;
        clearcoatNormal.HasTextureCoordinate = true;
        clearcoatNormal.TextureCoordinate = 2;
        clearcoat.ClearcoatNormalTexture = clearcoatNormal;
        GltfExtensionResult clearcoatResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsClearcoat,
                CreateTypedClearcoat(clearcoat)));

        GltfMaterialsDispersionExtension dispersion = new GltfMaterialsDispersionExtension();
        GltfExtensionResult dispersionResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsDispersion,
                CreateTypedDispersion(dispersion)));

        GltfMaterialsEmissiveStrengthExtension emissive =
            new GltfMaterialsEmissiveStrengthExtension();
        GltfExtensionResult emissiveResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsEmissiveStrength,
                CreateTypedEmissiveStrength(emissive)));

        GltfMaterialsIridescenceExtension iridescence =
            new GltfMaterialsIridescenceExtension();
        iridescence.HasIridescenceThicknessMinimum = true;
        GltfExtensionResult iridescenceRangeResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsIridescence,
                CreateTypedIridescence(iridescence)));
        GltfMaterialsIridescenceExtension unusedIridescence =
            new GltfMaterialsIridescenceExtension();
        unusedIridescence.HasIridescenceThicknessMinimum = true;
        unusedIridescence.IridescenceThicknessMinimum = 100.0;
        unusedIridescence.HasIridescenceThicknessMaximum = true;
        unusedIridescence.IridescenceThicknessMaximum = 100.0;
        unusedIridescence.IridescenceThicknessTexture = CreateTexture(0, 0);
        GltfExtensionResult iridescenceTextureResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsIridescence,
                CreateTypedIridescence(unusedIridescence)));

        GltfMaterialsVolumeExtension volume = new GltfMaterialsVolumeExtension();
        GltfExtensionResult volumeResult = handler.ValidateExtension(
            CreateMaterialContext(document, GltfExtensionNames.KhrMaterialsVolume,
                CreateTypedVolume(volume)));

        Assert.Multiple(() =>
        {
            Assert.That(anisotropyResult.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(anisotropyResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.ValueNotInRange), Is.True);
            Assert.That(anisotropyResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsAnisotropyAnisotropyTextureTexcoord),
                Is.True);
            Assert.That(clearcoatResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsClearcoatClearcoatNormalTextureTexcoord),
                Is.True);
            Assert.That(dispersionResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsDispersionNoVolume), Is.True);
            Assert.That(emissiveResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsEmissiveStrengthZeroFactor), Is.True);
            Assert.That(iridescenceRangeResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsIridescenceThicknessRangeWithoutTexture),
                Is.True);
            Assert.That(iridescenceTextureResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsIridescenceThicknessTextureUnused),
                Is.True);
            Assert.That(volumeResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsVolumeDoubleSided), Is.True);
            Assert.That(volumeResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsVolumeNoTransmission), Is.True);
        });
    }

    [Test]
    public void RemapExtensionReferences_UpdatesTextureIndicesWithoutMutatingTypedInput()
    {
        GltfMaterialsAnisotropyExtension anisotropy = new GltfMaterialsAnisotropyExtension();
        anisotropy.AnisotropyTexture = CreateTexture(2, 0);
        GltfTypedExtensionData typed = CreateTypedAnisotropy(anisotropy);
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = GltfExtensionNames.KhrMaterialsAnisotropy;
        context.ObjectLocation = "material";
        context.HasObjectIndex = true;
        context.ObjectIndex = 1;
        context.TypedData = typed;
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Textures[2] = 5;

        GltfExtensionHandler handler = new GltfExtensionHandler();
        GltfExtensionResult validated = handler.ValidateExtension(context);
        GltfExtensionResult result = handler.RemapExtensionReferences(context, remap);

        Assert.Multiple(() =>
        {
            Assert.That(validated.References, Has.Count.EqualTo(1));
            Assert.That(validated.References[0].TargetIndex, Is.EqualTo(2));
            Assert.That(result.Json!["anisotropyTexture"]!["index"]!.GetValue<int>(),
                Is.EqualTo(5));
            Assert.That(anisotropy.AnisotropyTexture!.Index, Is.EqualTo(2));
            Assert.That(result.References, Has.Count.EqualTo(1));
            Assert.That(result.References[0].TargetIndex, Is.EqualTo(5));
        });
    }

    [Test]
    public void DocumentValidator_ValidatesTypedOnlyMaterialExtensions()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsAnisotropy);
        GltfMaterialsAnisotropyExtension anisotropy = new GltfMaterialsAnisotropyExtension();
        anisotropy.HasAnisotropyStrength = true;
        anisotropy.AnisotropyStrength = 2.0;
        GltfTypedExtensionData typed = CreateTypedAnisotropy(anisotropy);
        GltfMaterial material = new GltfMaterial();
        material.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsAnisotropy] = typed;
        document.Materials.Add(material);
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateResources = false;

        GltfValidationReport result = new GltfValidator().ValidateDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.Code == GltfIssueCodes.ValueNotInRange
                && issue.JsonPointer.EndsWith(
                    "/KHR_materials_anisotropy/anisotropyStrength",
                    StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void Validation_RejectsDuplicateVariantUseAcrossMappings()
    {
        GltfDocument document = new GltfDocument();
        document.Materials.Add(new GltfMaterial());
        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        variants.Variants.Add(new GltfMaterialVariant());
        GltfTypedExtensionData rootTyped = new GltfTypedExtensionData();
        rootTyped.MaterialsVariants = variants;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants] = rootTyped;
        GltfMaterialsVariantsPrimitiveExtension primitive =
            new GltfMaterialsVariantsPrimitiveExtension();
        GltfMaterialVariantMapping first = new GltfMaterialVariantMapping();
        first.Material = 0;
        first.Variants.Add(0);
        GltfMaterialVariantMapping second = new GltfMaterialVariantMapping();
        second.Material = 0;
        second.Variants.Add(0);
        primitive.Mappings.Add(first);
        primitive.Mappings.Add(second);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsVariantsPrimitive = primitive;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Document = document;
        context.Name = GltfExtensionNames.KhrMaterialsVariants;
        context.ObjectLocation = "mesh.primitive";
        context.TypedData = typed;

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Count(issue =>
                issue.Code == GltfIssueCodes.KhrMaterialsVariantsNonUniqueVariant), Is.EqualTo(1));
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

    private static GltfExtensionContext CreateMaterialContext(
        GltfDocument document,
        string name,
        GltfTypedExtensionData typed)
    {
        GltfExtensionContext result = new GltfExtensionContext();
        result.Document = document;
        result.Name = name;
        result.ObjectLocation = "material";
        result.HasObjectIndex = true;
        result.ObjectIndex = 0;
        result.TypedData = typed;
        return result;
    }

    private static GltfTypedExtensionData CreateTypedAnisotropy(
        GltfMaterialsAnisotropyExtension value)
    {
        GltfTypedExtensionData result = new GltfTypedExtensionData();
        result.MaterialsAnisotropy = value;
        return result;
    }

    private static GltfTypedExtensionData CreateTypedDispersion(
        GltfMaterialsDispersionExtension value)
    {
        GltfTypedExtensionData result = new GltfTypedExtensionData();
        result.MaterialsDispersion = value;
        return result;
    }

    private static GltfTypedExtensionData CreateTypedClearcoat(
        GltfMaterialsClearcoatExtension value)
    {
        GltfTypedExtensionData result = new GltfTypedExtensionData();
        result.MaterialsClearcoat = value;
        return result;
    }

    private static GltfTypedExtensionData CreateTypedEmissiveStrength(
        GltfMaterialsEmissiveStrengthExtension value)
    {
        GltfTypedExtensionData result = new GltfTypedExtensionData();
        result.MaterialsEmissiveStrength = value;
        return result;
    }

    private static GltfTypedExtensionData CreateTypedIridescence(
        GltfMaterialsIridescenceExtension value)
    {
        GltfTypedExtensionData result = new GltfTypedExtensionData();
        result.MaterialsIridescence = value;
        return result;
    }

    private static GltfTypedExtensionData CreateTypedVolume(
        GltfMaterialsVolumeExtension value)
    {
        GltfTypedExtensionData result = new GltfTypedExtensionData();
        result.MaterialsVolume = value;
        return result;
    }
}
