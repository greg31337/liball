namespace gltf.Tests.Api.Data;

[TestFixture]
public class MaterialExtensionDataTests
{
    [Test]
    public void MaterialScalarExtensions_MatchSchemaDefaults()
    {
        GltfMaterialsAnisotropyExtension anisotropy = new GltfMaterialsAnisotropyExtension();
        GltfMaterialsDispersionExtension dispersion = new GltfMaterialsDispersionExtension();
        GltfMaterialsEmissiveStrengthExtension emissiveStrength = new GltfMaterialsEmissiveStrengthExtension();
        GltfMaterialsIorExtension ior = new GltfMaterialsIorExtension();
        GltfMaterialsTransmissionExtension transmission = new GltfMaterialsTransmissionExtension();
        GltfMaterialsVolumeExtension volume = new GltfMaterialsVolumeExtension();

        Assert.Multiple(() =>
        {
            Assert.That(anisotropy.HasAnisotropyStrength, Is.False);
            Assert.That(anisotropy.AnisotropyStrength, Is.Zero);
            Assert.That(anisotropy.HasAnisotropyRotation, Is.False);
            Assert.That(anisotropy.AnisotropyRotation, Is.Zero);
            Assert.That(anisotropy.AnisotropyTexture, Is.Null);
            Assert.That(dispersion.HasDispersion, Is.False);
            Assert.That(dispersion.Dispersion, Is.Zero);
            Assert.That(emissiveStrength.HasEmissiveStrength, Is.False);
            Assert.That(emissiveStrength.EmissiveStrength, Is.EqualTo(1.0));
            Assert.That(ior.HasIor, Is.False);
            Assert.That(ior.Ior, Is.EqualTo(1.5));
            Assert.That(transmission.HasTransmissionFactor, Is.False);
            Assert.That(transmission.TransmissionFactor, Is.Zero);
            Assert.That(transmission.TransmissionTexture, Is.Null);
            Assert.That(volume.HasThicknessFactor, Is.False);
            Assert.That(volume.ThicknessFactor, Is.Zero);
            Assert.That(volume.ThicknessTexture, Is.Null);
            Assert.That(volume.HasAttenuationDistance, Is.False);
            Assert.That(volume.AttenuationDistance, Is.Zero);
            Assert.That(volume.HasAttenuationColor, Is.False);
            Assert.That(volume.AttenuationColor, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
        });
    }

    [Test]
    public void MaterialLayerExtensions_OwnColorDefaultsAndTextureSlots()
    {
        GltfMaterialsClearcoatExtension clearcoat = new GltfMaterialsClearcoatExtension();
        GltfMaterialsIridescenceExtension iridescence = new GltfMaterialsIridescenceExtension();
        GltfMaterialsSheenExtension firstSheen = new GltfMaterialsSheenExtension();
        GltfMaterialsSheenExtension secondSheen = new GltfMaterialsSheenExtension();
        GltfMaterialsSpecularExtension firstSpecular = new GltfMaterialsSpecularExtension();
        GltfMaterialsSpecularExtension secondSpecular = new GltfMaterialsSpecularExtension();
        GltfMaterialsDiffuseTransmissionExtension diffuseTransmission = new GltfMaterialsDiffuseTransmissionExtension();

        firstSheen.SheenColorFactor[0] = 0.5;
        firstSpecular.SpecularColorFactor[0] = 0.25;

        Assert.Multiple(() =>
        {
            Assert.That(clearcoat.ClearcoatFactor, Is.Zero);
            Assert.That(clearcoat.ClearcoatTexture, Is.Null);
            Assert.That(clearcoat.ClearcoatRoughnessFactor, Is.Zero);
            Assert.That(clearcoat.ClearcoatRoughnessTexture, Is.Null);
            Assert.That(clearcoat.ClearcoatNormalTexture, Is.Null);
            Assert.That(iridescence.IridescenceFactor, Is.Zero);
            Assert.That(iridescence.IridescenceIor, Is.EqualTo(1.3));
            Assert.That(iridescence.IridescenceThicknessMinimum, Is.EqualTo(100.0));
            Assert.That(iridescence.IridescenceThicknessMaximum, Is.EqualTo(400.0));
            Assert.That(iridescence.IridescenceTexture, Is.Null);
            Assert.That(iridescence.IridescenceThicknessTexture, Is.Null);
            Assert.That(secondSheen.SheenColorFactor, Is.EqualTo(new[] { 0.0, 0.0, 0.0 }));
            Assert.That(firstSheen.SheenColorFactor, Is.Not.SameAs(secondSheen.SheenColorFactor));
            Assert.That(firstSheen.SheenRoughnessFactor, Is.Zero);
            Assert.That(firstSheen.SheenColorTexture, Is.Null);
            Assert.That(firstSheen.SheenRoughnessTexture, Is.Null);
            Assert.That(firstSpecular.SpecularFactor, Is.EqualTo(1.0));
            Assert.That(secondSpecular.SpecularColorFactor, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(firstSpecular.SpecularColorFactor, Is.Not.SameAs(secondSpecular.SpecularColorFactor));
            Assert.That(firstSpecular.SpecularTexture, Is.Null);
            Assert.That(firstSpecular.SpecularColorTexture, Is.Null);
            Assert.That(diffuseTransmission.DiffuseTransmissionFactor, Is.Zero);
            Assert.That(diffuseTransmission.DiffuseTransmissionColorFactor, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(diffuseTransmission.DiffuseTransmissionTexture, Is.Null);
            Assert.That(diffuseTransmission.DiffuseTransmissionColorTexture, Is.Null);
        });
    }

    [Test]
    public void MaterialExtensionPayloads_ContainIndependentPropertyState()
    {
        GltfMaterialsUnlitExtension unlit = new GltfMaterialsUnlitExtension();
        GltfMaterialsPbrSpecularGlossinessExtension firstArchived = new GltfMaterialsPbrSpecularGlossinessExtension();
        GltfMaterialsPbrSpecularGlossinessExtension secondArchived = new GltfMaterialsPbrSpecularGlossinessExtension();

        unlit.PropertyJson.UnknownMembers["future"] = null;
        firstArchived.DiffuseFactor[0] = 0.5;
        firstArchived.SpecularFactor[0] = 0.25;

        Assert.Multiple(() =>
        {
            Assert.That(unlit.PropertyJson.UnknownMembers, Has.Count.EqualTo(1));
            Assert.That(firstArchived.HasDiffuseFactor, Is.False);
            Assert.That(secondArchived.DiffuseFactor, Is.EqualTo(new[] { 1.0, 1.0, 1.0, 1.0 }));
            Assert.That(firstArchived.DiffuseFactor, Is.Not.SameAs(secondArchived.DiffuseFactor));
            Assert.That(firstArchived.DiffuseTexture, Is.Null);
            Assert.That(firstArchived.HasSpecularFactor, Is.False);
            Assert.That(secondArchived.SpecularFactor, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(firstArchived.SpecularFactor, Is.Not.SameAs(secondArchived.SpecularFactor));
            Assert.That(firstArchived.HasGlossinessFactor, Is.False);
            Assert.That(firstArchived.GlossinessFactor, Is.EqualTo(1.0));
            Assert.That(firstArchived.SpecularGlossinessTexture, Is.Null);
            Assert.That(firstArchived.PropertyJson, Is.Not.SameAs(secondArchived.PropertyJson));
        });
    }
}
