using System.Reflection;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class ExtensionRegistryTests
{
    [Test]
    public void ListDescriptors_CoversTheNamedSnapshotInDeterministicOrder()
    {
        List<string> expectedNames = typeof(GltfExtensionNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        List<GltfExtensionDescriptor> descriptors = new ExtensionRegistry().ListDescriptors();

        Assert.Multiple(() =>
        {
            Assert.That(descriptors.Select(value => value.Name), Is.EqualTo(expectedNames));
            Assert.That(descriptors, Has.Count.EqualTo(69));
            Assert.That(descriptors.Sum(value => value.SchemaResources.Count), Is.EqualTo(109));
            Assert.That(descriptors.All(value => value.Status != GltfExtensionStatus.Unknown), Is.True);
            Assert.That(descriptors.Where(value =>
                value.Name != GltfExtensionNames.KhrMaterialsDiffuseTransmission
                && value.Name != GltfExtensionNames.KhrGaussianSplatting
                && value.Name != GltfExtensionNames.KhrInteractivity).All(
                    value => value.Version == "2.0"), Is.True);
            Assert.That(descriptors.Single(value =>
                value.Name == GltfExtensionNames.KhrMaterialsDiffuseTransmission).Version,
                Is.EqualTo(DiffuseTransmissionSnapshot.Version));
            Assert.That(descriptors.Single(value =>
                value.Name == GltfExtensionNames.KhrGaussianSplatting).Version,
                Is.EqualTo(AdvancedExtensionsSnapshot.Version));
            Assert.That(descriptors.Single(value =>
                value.Name == GltfExtensionNames.KhrInteractivity).Version,
                Is.EqualTo(AdvancedExtensionsSnapshot.Version));
            Assert.That(descriptors.All(value => value.ObjectLocations.Count > 0), Is.True);
        });
    }

    [Test]
    public void FindDescriptor_ReportsSnapshotStatusAndCurrentTypedCapabilities()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        GltfExtensionDescriptor animation = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrAnimationPointer);
        GltfExtensionDescriptor diffuse = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMaterialsDiffuseTransmission);
        GltfExtensionDescriptor vendor = RequireDescriptor(
            registry,
            GltfExtensionNames.MsftLod);
        GltfExtensionDescriptor archived = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrTechniquesWebgl);
        GltfExtensionDescriptor archivedPbr = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness);
        GltfExtensionDescriptor draft = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrAccessorFloat64);
        GltfExtensionDescriptor textureTransform = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrTextureTransform);
        GltfExtensionDescriptor volume = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMaterialsVolume);
        GltfExtensionDescriptor quantization = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMeshQuantization);
        GltfExtensionDescriptor draco = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrDracoMeshCompression);
        GltfExtensionDescriptor meshopt = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMeshoptCompression);
        GltfExtensionDescriptor legacyMeshopt = RequireDescriptor(
            registry,
            GltfExtensionNames.ExtMeshoptCompression);
        GltfExtensionDescriptor basisu = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrTextureBasisu);
        GltfExtensionDescriptor webp = RequireDescriptor(
            registry,
            GltfExtensionNames.ExtTextureWebp);
        GltfExtensionDescriptor xmpJsonLd = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrXmpJsonLd);
        GltfExtensionDescriptor gaussian = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrGaussianSplatting);
        GltfExtensionDescriptor interactivity = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrInteractivity);

        Assert.Multiple(() =>
        {
            Assert.That(animation.Status, Is.EqualTo(GltfExtensionStatus.Ratified));
            Assert.That(animation.HasTypedModel, Is.True);
            Assert.That(animation.HasSemanticValidation, Is.True);
            Assert.That(animation.CanRemapReferences, Is.True);
            Assert.That(diffuse.Status, Is.EqualTo(DiffuseTransmissionSnapshot.Status));
            Assert.That(diffuse.Version, Is.EqualTo(
                "2.0@fdb8ce0e2e0b7ecf3466f8dacb9f1385257b8276"));
            Assert.That(diffuse.HasTypedModel, Is.True);
            Assert.That(diffuse.HasSemanticValidation, Is.True);
            Assert.That(diffuse.CanRemapReferences, Is.True);
            Assert.That(vendor.Status, Is.EqualTo(GltfExtensionStatus.Vendor));
            Assert.That(archived.Status, Is.EqualTo(GltfExtensionStatus.Archived));
            Assert.That(archivedPbr.Status, Is.EqualTo(GltfExtensionStatus.Archived));
            Assert.That(archivedPbr.HasTypedModel, Is.True);
            Assert.That(archivedPbr.HasSemanticValidation, Is.True);
            Assert.That(archivedPbr.CanRemapReferences, Is.True);
            Assert.That(draft.Status, Is.EqualTo(GltfExtensionStatus.Draft));
            Assert.That(draft.SchemaResources, Is.Empty);
            Assert.That(textureTransform.HasSemanticValidation, Is.True);
            Assert.That(textureTransform.CanRemapReferences, Is.True);
            Assert.That(volume.HasSemanticValidation, Is.True);
            Assert.That(volume.CanRemapReferences, Is.True);
            Assert.That(quantization.HasSemanticValidation, Is.True);
            Assert.That(quantization.CanRemapReferences, Is.False);
            Assert.That(draco.HasTypedModel, Is.True);
            Assert.That(draco.HasSemanticValidation, Is.True);
            Assert.That(draco.CanRemapReferences, Is.True);
            Assert.That(meshopt.HasTypedModel, Is.True);
            Assert.That(meshopt.HasSemanticValidation, Is.True);
            Assert.That(meshopt.CanRemapReferences, Is.True);
            Assert.That(legacyMeshopt.HasTypedModel, Is.True);
            Assert.That(legacyMeshopt.HasSemanticValidation, Is.True);
            Assert.That(legacyMeshopt.CanRemapReferences, Is.True);
            Assert.That(basisu.HasTypedModel, Is.True);
            Assert.That(basisu.HasSemanticValidation, Is.True);
            Assert.That(basisu.CanRemapReferences, Is.True);
            Assert.That(webp.HasTypedModel, Is.True);
            Assert.That(webp.HasSemanticValidation, Is.True);
            Assert.That(webp.CanRemapReferences, Is.True);
            Assert.That(xmpJsonLd.HasTypedModel, Is.True);
            Assert.That(xmpJsonLd.HasSemanticValidation, Is.True);
            Assert.That(xmpJsonLd.CanRemapReferences, Is.True);
            Assert.That(gaussian.Status, Is.EqualTo(AdvancedExtensionsSnapshot.Status));
            Assert.That(gaussian.Version, Is.EqualTo(
                "2.0@fdb8ce0e2e0b7ecf3466f8dacb9f1385257b8276"));
            Assert.That(gaussian.HasSemanticValidation, Is.True);
            Assert.That(gaussian.CanRemapReferences, Is.True);
            Assert.That(interactivity.Status, Is.EqualTo(AdvancedExtensionsSnapshot.Status));
            Assert.That(interactivity.Version, Is.EqualTo(AdvancedExtensionsSnapshot.Version));
            Assert.That(interactivity.HasSemanticValidation, Is.True);
            Assert.That(interactivity.CanRemapReferences, Is.True);
        });
    }

    [Test]
    public void FindDescriptor_ProvidesLocationsSchemasDependenciesAndConflicts()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        GltfExtensionDescriptor volume = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMaterialsVolume);
        GltfExtensionDescriptor quantization = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMeshQuantization);
        GltfExtensionDescriptor meshopt = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrMeshoptCompression);
        GltfExtensionDescriptor legacyMeshopt = RequireDescriptor(
            registry,
            GltfExtensionNames.ExtMeshoptCompression);

        Assert.Multiple(() =>
        {
            Assert.That(volume.ObjectLocations, Is.EqualTo(new[] { "material" }));
            Assert.That(volume.Dependencies, Does.Contain(GltfExtensionNames.KhrMaterialsTransmission));
            Assert.That(volume.Conflicts, Does.Contain(GltfExtensionNames.KhrMaterialsUnlit));
            Assert.That(volume.Conflicts, Does.Contain(GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness));
            Assert.That(volume.SchemaResources, Does.Contain("material.KHR_materials_volume.schema.json"));
            Assert.That(volume.AllowsFallback, Is.True);
            Assert.That(quantization.Dependencies, Does.Contain(GltfExtensionNames.KhrTextureTransform));
            Assert.That(quantization.AllowsFallback, Is.False);
            Assert.That(meshopt.RequiredCodec, Is.EqualTo(GltfExtensionNames.KhrMeshoptCompression));
            Assert.That(meshopt.Conflicts, Does.Contain(GltfExtensionNames.ExtMeshoptCompression));
            Assert.That(legacyMeshopt.Conflicts, Does.Contain(GltfExtensionNames.KhrMeshoptCompression));
        });
    }

    [Test]
    public void FindDescriptor_DistinguishesFallbackAndCodecRequirements()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        GltfExtensionDescriptor instancing = RequireDescriptor(
            registry,
            GltfExtensionNames.ExtMeshGpuInstancing);
        GltfExtensionDescriptor draco = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrDracoMeshCompression);
        GltfExtensionDescriptor basisu = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrTextureBasisu);
        GltfExtensionDescriptor webp = RequireDescriptor(
            registry,
            GltfExtensionNames.ExtTextureWebp);

        Assert.Multiple(() =>
        {
            Assert.That(instancing.HasTypedModel, Is.True);
            Assert.That(instancing.AllowsFallback, Is.False);
            Assert.That(instancing.RequiredCodec, Is.Null);
            Assert.That(draco.AllowsFallback, Is.True);
            Assert.That(draco.RequiredCodec, Is.EqualTo(GltfExtensionNames.KhrDracoMeshCompression));
            Assert.That(basisu.RequiredCodec, Is.EqualTo(GltfExtensionNames.KhrTextureBasisu));
            Assert.That(webp.RequiredCodec, Is.EqualTo(GltfExtensionNames.ExtTextureWebp));
        });
    }

    [Test]
    public void RegistryResults_AreCaseSensitiveAndIndependentlyOwned()
    {
        ExtensionRegistry registry = new ExtensionRegistry();
        List<GltfExtensionDescriptor> first = registry.ListDescriptors();
        List<GltfExtensionDescriptor> second = registry.ListDescriptors();
        GltfExtensionDescriptor textureTransform = RequireDescriptor(
            registry,
            GltfExtensionNames.KhrTextureTransform);

        first[0].ObjectLocations.Clear();
        textureTransform.SchemaResources.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(second[0].ObjectLocations, Is.Not.Empty);
            Assert.That(
                RequireDescriptor(registry, GltfExtensionNames.KhrTextureTransform).SchemaResources,
                Is.Not.Empty);
            Assert.That(registry.FindDescriptor("khr_texture_transform"), Is.Null);
            Assert.That(registry.FindDescriptor("UNKNOWN_extension"), Is.Null);
        });
    }

    [Test]
    public void BuiltInHandler_OwnsTheSnapshotRegistry()
    {
        GltfExtensionHandler handler = new GltfExtensionHandler();

        Assert.That(handler.Registry.ListDescriptors(), Has.Count.EqualTo(69));
    }

    private static GltfExtensionDescriptor RequireDescriptor(
        ExtensionRegistry registry,
        string name)
    {
        GltfExtensionDescriptor? descriptor = registry.FindDescriptor(name);
        Assert.That(descriptor, Is.Not.Null, $"Missing registry descriptor for {name}.");
        return descriptor!;
    }
}
