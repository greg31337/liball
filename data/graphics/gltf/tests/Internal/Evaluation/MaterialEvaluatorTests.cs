namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class MaterialEvaluatorTests
{
    [Test]
    public void EvaluateMaterial_ReturnsTheCoreDefaultMaterialWhenAbsent()
    {
        MaterialEvaluation result = new MaterialEvaluator().EvaluateMaterial(null);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsesDefaultMaterial, Is.True);
            Assert.That(result.BaseColorFactor, Is.EqualTo(new double[] { 1.0, 1.0, 1.0, 1.0 }));
            Assert.That(result.MetallicFactor, Is.EqualTo(1.0));
            Assert.That(result.RoughnessFactor, Is.EqualTo(1.0));
            Assert.That(result.EmissiveFactor, Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }));
            Assert.That(result.AlphaMode, Is.EqualTo(GltfAlphaMode.Opaque));
            Assert.That(result.AlphaCutoff, Is.EqualTo(0.5));
            Assert.That(result.DoubleSided, Is.False);
            Assert.That(result.FullyOpaque, Is.True);
            Assert.That(result.Textures, Is.Empty);
        });
    }

    [Test]
    public void EvaluateMaterial_UsesDefaultsForFieldsWhosePresenceFlagsAreFalse()
    {
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.BaseColorFactor = new double[] { 0.1, 0.2, 0.3, 0.4 };
        pbr.MetallicFactor = 0.2;
        pbr.RoughnessFactor = 0.3;
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = pbr;
        material.EmissiveFactor = new double[] { 1.0, 1.0, 1.0 };
        material.AlphaMode = GltfAlphaMode.Blend;
        material.AlphaCutoff = 0.1;
        material.DoubleSided = true;

        MaterialEvaluation result = new MaterialEvaluator().EvaluateMaterial(material);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsesDefaultMaterial, Is.False);
            Assert.That(result.BaseColorFactor, Is.EqualTo(new double[] { 1.0, 1.0, 1.0, 1.0 }));
            Assert.That(result.MetallicFactor, Is.EqualTo(1.0));
            Assert.That(result.RoughnessFactor, Is.EqualTo(1.0));
            Assert.That(result.EmissiveFactor, Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }));
            Assert.That(result.AlphaMode, Is.EqualTo(GltfAlphaMode.Opaque));
            Assert.That(result.AlphaCutoff, Is.EqualTo(0.5));
            Assert.That(result.DoubleSided, Is.False);
        });
    }

    [Test]
    public void EvaluateMaterial_UsesExplicitFactorsAndOwnsEvaluatedArrays()
    {
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.HasBaseColorFactor = true;
        pbr.BaseColorFactor = new double[] { 0.1, 0.2, 0.3, 0.4 };
        pbr.HasMetallicFactor = true;
        pbr.MetallicFactor = 0.25;
        pbr.HasRoughnessFactor = true;
        pbr.RoughnessFactor = 0.75;
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = pbr;
        material.HasEmissiveFactor = true;
        material.EmissiveFactor = new double[] { 0.4, 0.5, 0.6 };
        material.HasDoubleSided = true;
        material.DoubleSided = true;

        MaterialEvaluation result = new MaterialEvaluator().EvaluateMaterial(material);
        pbr.BaseColorFactor[0] = 1.0;
        material.EmissiveFactor[0] = 1.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.BaseColorFactor, Is.EqualTo(new double[] { 0.1, 0.2, 0.3, 0.4 }));
            Assert.That(result.MetallicFactor, Is.EqualTo(0.25));
            Assert.That(result.RoughnessFactor, Is.EqualTo(0.75));
            Assert.That(result.EmissiveFactor, Is.EqualTo(new double[] { 0.4, 0.5, 0.6 }));
            Assert.That(result.DoubleSided, Is.True);
        });
    }

    [Test]
    public void EvaluateMaterial_ClassifiesTextureSlotsChannelsColorSpacesAndCoordinates()
    {
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.BaseColorTexture = CreateTextureInfo(1, false, 7);
        pbr.MetallicRoughnessTexture = CreateTextureInfo(2, true, 2);
        GltfNormalTextureInfo normal = new GltfNormalTextureInfo();
        normal.Index = 3;
        normal.HasTextureCoordinate = true;
        normal.TextureCoordinate = 3;
        GltfOcclusionTextureInfo occlusion = new GltfOcclusionTextureInfo();
        occlusion.Index = 4;
        occlusion.HasTextureCoordinate = true;
        occlusion.TextureCoordinate = 4;
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = pbr;
        material.NormalTexture = normal;
        material.OcclusionTexture = occlusion;
        material.EmissiveTexture = CreateTextureInfo(5, true, 5);

        MaterialEvaluation result = new MaterialEvaluator().EvaluateMaterial(material);
        MaterialTextureBinding baseColor = result.Textures[0];
        MaterialTextureBinding metallicRoughness = result.Textures[1];
        MaterialTextureBinding normalBinding = result.Textures[2];
        MaterialTextureBinding occlusionBinding = result.Textures[3];
        MaterialTextureBinding emissive = result.Textures[4];

        Assert.Multiple(() =>
        {
            Assert.That(result.Textures, Has.Count.EqualTo(5));
            Assert.That(baseColor.Slot, Is.EqualTo(MaterialTextureSlot.BaseColor));
            Assert.That(baseColor.Texture, Is.EqualTo(1));
            Assert.That(baseColor.TextureCoordinate, Is.Zero);
            Assert.That(baseColor.ColorSpace, Is.EqualTo(MaterialTextureColorSpace.Srgb));
            Assert.That(baseColor.Red, Is.EqualTo(MaterialTextureChannelUse.BaseColorRed));
            Assert.That(baseColor.Green, Is.EqualTo(MaterialTextureChannelUse.BaseColorGreen));
            Assert.That(baseColor.Blue, Is.EqualTo(MaterialTextureChannelUse.BaseColorBlue));
            Assert.That(baseColor.Alpha, Is.EqualTo(MaterialTextureChannelUse.Alpha));
            Assert.That(metallicRoughness.TextureCoordinate, Is.EqualTo(2));
            Assert.That(metallicRoughness.ColorSpace, Is.EqualTo(MaterialTextureColorSpace.Linear));
            Assert.That(metallicRoughness.Red, Is.EqualTo(MaterialTextureChannelUse.Unused));
            Assert.That(metallicRoughness.Green, Is.EqualTo(MaterialTextureChannelUse.Roughness));
            Assert.That(metallicRoughness.Blue, Is.EqualTo(MaterialTextureChannelUse.Metallic));
            Assert.That(normalBinding.Red, Is.EqualTo(MaterialTextureChannelUse.NormalX));
            Assert.That(normalBinding.Green, Is.EqualTo(MaterialTextureChannelUse.NormalY));
            Assert.That(normalBinding.Blue, Is.EqualTo(MaterialTextureChannelUse.NormalZ));
            Assert.That(occlusionBinding.Red, Is.EqualTo(MaterialTextureChannelUse.Occlusion));
            Assert.That(emissive.ColorSpace, Is.EqualTo(MaterialTextureColorSpace.Srgb));
            Assert.That(emissive.Red, Is.EqualTo(MaterialTextureChannelUse.EmissiveRed));
            Assert.That(emissive.Green, Is.EqualTo(MaterialTextureChannelUse.EmissiveGreen));
            Assert.That(emissive.Blue, Is.EqualTo(MaterialTextureChannelUse.EmissiveBlue));
        });
    }

    [Test]
    public void EvaluateMaterial_ExposesTextureTransformMetadataAndCoordinateOverride()
    {
        GltfTextureInfo textureInfo = CreateTextureInfo(1, true, 2);
        GltfTextureTransformExtension transform = new GltfTextureTransformExtension();
        transform.HasOffset = true;
        transform.Offset = new double[] { 0.25, 0.5 };
        transform.HasRotation = true;
        transform.Rotation = 0.75;
        transform.HasScale = true;
        transform.Scale = new double[] { 2.0, 3.0 };
        transform.HasTextureCoordinate = true;
        transform.TextureCoordinate = 4;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.TextureTransform = transform;
        textureInfo.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrTextureTransform] = typed;
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.BaseColorTexture = textureInfo;
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = pbr;

        MaterialTextureBinding result = new MaterialEvaluator()
            .EvaluateMaterial(material).Textures[0];
        transform.Offset[0] = 1.0;
        transform.Scale[0] = 1.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasTextureTransform, Is.True);
            Assert.That(result.TextureCoordinate, Is.EqualTo(4));
            Assert.That(result.TextureOffset, Is.EqualTo(new double[] { 0.25, 0.5 }));
            Assert.That(result.TextureRotation, Is.EqualTo(0.75));
            Assert.That(result.TextureScale, Is.EqualTo(new double[] { 2.0, 3.0 }));
        });
    }

    [Test]
    public void EvaluateMaterialAlpha_AppliesOpaqueMaskAndBlendBehavior()
    {
        MaterialEvaluator evaluator = new MaterialEvaluator();
        GltfMaterial opaqueMaterial = CreateAlphaMaterial(GltfAlphaMode.Opaque, 0.5, 0.5);
        GltfMaterial maskMaterial = CreateAlphaMaterial(GltfAlphaMode.Mask, 0.25, 0.5);
        GltfMaterial blendMaterial = CreateAlphaMaterial(GltfAlphaMode.Blend, 0.5, 0.5);

        MaterialAlphaEvaluation opaque = evaluator.EvaluateMaterialAlpha(
            evaluator.EvaluateMaterial(opaqueMaterial),
            0.5,
            0.5);
        MaterialEvaluation maskEvaluation = evaluator.EvaluateMaterial(maskMaterial);
        MaterialAlphaEvaluation maskedOut = evaluator.EvaluateMaterialAlpha(
            maskEvaluation,
            0.5,
            0.5);
        MaterialAlphaEvaluation maskKept = evaluator.EvaluateMaterialAlpha(
            maskEvaluation,
            1.0,
            0.5);
        MaterialAlphaEvaluation blend = evaluator.EvaluateMaterialAlpha(
            evaluator.EvaluateMaterial(blendMaterial),
            0.5,
            0.5);

        Assert.Multiple(() =>
        {
            Assert.That(opaque.SourceAlpha, Is.EqualTo(0.125));
            Assert.That(opaque.OutputAlpha, Is.EqualTo(1.0));
            Assert.That(opaque.Discarded, Is.False);
            Assert.That(maskedOut.SourceAlpha, Is.EqualTo(0.125));
            Assert.That(maskedOut.OutputAlpha, Is.Zero);
            Assert.That(maskedOut.Discarded, Is.True);
            Assert.That(maskKept.SourceAlpha, Is.EqualTo(0.25));
            Assert.That(maskKept.OutputAlpha, Is.EqualTo(1.0));
            Assert.That(maskKept.Discarded, Is.False);
            Assert.That(blend.OutputAlpha, Is.EqualTo(0.125));
            Assert.That(blend.RequiresBlending, Is.True);
        });
    }

    [Test]
    public void EvaluateMaterial_AppliesNormalScaleAndOcclusionStrength()
    {
        GltfNormalTextureInfo normal = new GltfNormalTextureInfo();
        normal.Index = 0;
        normal.HasScale = true;
        normal.Scale = 2.0;
        GltfOcclusionTextureInfo occlusion = new GltfOcclusionTextureInfo();
        occlusion.Index = 1;
        occlusion.HasStrength = true;
        occlusion.Strength = 0.25;
        GltfMaterial material = new GltfMaterial();
        material.NormalTexture = normal;
        material.OcclusionTexture = occlusion;
        MaterialEvaluator evaluator = new MaterialEvaluator();

        MaterialEvaluation result = evaluator.EvaluateMaterial(material);
        double[] decodedNormal = evaluator.EvaluateNormalTextureSample(
            result,
            0.75,
            0.25,
            1.0);
        double occlusionValue = evaluator.EvaluateOcclusionTextureSample(result, 0.2);
        double component = 1.0 / Math.Sqrt(3.0);

        Assert.Multiple(() =>
        {
            Assert.That(result.NormalScale, Is.EqualTo(2.0));
            Assert.That(result.OcclusionStrength, Is.EqualTo(0.25));
            Assert.That(decodedNormal[0], Is.EqualTo(component).Within(0.0000001));
            Assert.That(decodedNormal[1], Is.EqualTo(-component).Within(0.0000001));
            Assert.That(decodedNormal[2], Is.EqualTo(component).Within(0.0000001));
            Assert.That(occlusionValue, Is.EqualTo(0.8).Within(0.0000001));
        });
    }

    private static GltfTextureInfo CreateTextureInfo(
        int index,
        bool hasTextureCoordinate,
        int textureCoordinate)
    {
        GltfTextureInfo result = new GltfTextureInfo();
        result.Index = index;
        result.HasTextureCoordinate = hasTextureCoordinate;
        result.TextureCoordinate = textureCoordinate;
        return result;
    }

    private static GltfMaterial CreateAlphaMaterial(
        GltfAlphaMode mode,
        double cutoff,
        double factorAlpha)
    {
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.HasBaseColorFactor = true;
        pbr.BaseColorFactor = new double[] { 1.0, 1.0, 1.0, factorAlpha };
        GltfMaterial result = new GltfMaterial();
        result.PbrMetallicRoughness = pbr;
        result.HasAlphaMode = true;
        result.AlphaMode = mode;
        result.HasAlphaCutoff = true;
        result.AlphaCutoff = cutoff;
        return result;
    }
}
