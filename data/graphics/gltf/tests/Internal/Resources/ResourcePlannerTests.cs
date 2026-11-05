namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class ResourcePlannerTests
{
    [Test]
    public void PlanResources_PreservesDistinctResourcesAndCreatesOwnedHashedManifestEntries()
    {
        GltfResource first = CreateResource("images/a.png", [1, 2, 3]);
        GltfResource second = CreateResource("images/b.png", [1, 2, 3]);
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(first);
        source.Resources.Add(second);

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.Preserve,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Resources[0], Is.Not.SameAs(first));
            Assert.That(result.Resources.Resources[0].Bytes, Is.Not.SameAs(first.Bytes));
            Assert.That(result.SourceResourceMap, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Manifest, Has.Count.EqualTo(2));
            Assert.That(result.Manifest[0].Uri, Is.EqualTo("images/a.png"));
            Assert.That(result.Manifest[0].ContentHash,
                Is.EqualTo("039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81"));
            Assert.That(result.Manifest[0].Mode, Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(result.Manifest[0].ByteLength, Is.EqualTo(3));
        });

        first.Bytes[0] = 9;
        Assert.That(result.Resources.Resources[0].Bytes[0], Is.EqualTo(1));
    }

    [Test]
    public void PlanResources_ExternalizesAndDeduplicatesByteIdenticalResources()
    {
        GltfResource first = CreateResource("source/first.png", [4, 5, 6]);
        GltfResource second = CreateResource("source/second.png", [4, 5, 6]);
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(first);
        source.Resources.Add(second);

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.External,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.SourceResourceMap, Is.EqualTo(new[] { 0, 0 }));
            Assert.That(result.SourceResourceUris,
                Is.EqualTo(new[] { "source/first.png", "source/first.png" }));
            Assert.That(result.Manifest, Has.Count.EqualTo(1));
            Assert.That(result.Manifest[0].Embedded, Is.False);
            Assert.That(result.Manifest[0].Mode, Is.EqualTo(GltfResourceMode.External));
            Assert.That(result.Resources.Resources[0].UriKind, Is.EqualTo(GltfUriKind.Relative));
            Assert.That(first.SourceUri, Is.EqualTo("source/first.png"));
            Assert.That(second.SourceUri, Is.EqualTo("source/second.png"));
        });
    }

    [Test]
    public void PlanResources_AllocatesCollisionSuffixesForDifferentExternalBytes()
    {
        GltfResource first = CreateResource("textures/color.jpeg", [1]);
        GltfResource second = CreateResource("textures/color.png", [2]);
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(first);
        source.Resources.Add(second);

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.External,
            false);

        Assert.That(
            result.SourceResourceUris,
            Is.EqualTo(new[] { "textures/color.png", "textures/color-1.png" }));
    }

    [Test]
    public void PlanResources_UsesContentHashesForExternalNamesWhenRequested()
    {
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(CreateResource("textures/color.png", [1, 2, 3]));

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.External,
            true);

        Assert.That(
            result.SourceResourceUris[0],
            Is.EqualTo(
                "textures/039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81.png"));
    }

    [Test]
    public void PlanResources_EmbedsCanonicalBase64DataUrisAndDeduplicatesThem()
    {
        GltfResource first = CreateResource("first.png", [1, 2, 3]);
        GltfResource second = CreateResource("second.png", [1, 2, 3]);
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(first);
        source.Resources.Add(second);

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.DataUri,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.SourceResourceUris,
                Is.EqualTo(new[] {
                    "data:image/png;base64,AQID",
                    "data:image/png;base64,AQID"
                }));
            Assert.That(result.Resources.Resources[0].Embedded, Is.True);
            Assert.That(result.Resources.Resources[0].UriKind, Is.EqualTo(GltfUriKind.Data));
            Assert.That(result.Manifest[0].Uri, Is.EqualTo("data:image/png;base64,AQID"));
            Assert.That(result.Manifest[0].Embedded, Is.True);
            Assert.That(result.Manifest[0].Mode, Is.EqualTo(GltfResourceMode.DataUri));
            Assert.That(first.Embedded, Is.False);
            Assert.That(first.EffectiveUri, Is.Null);
        });
    }

    [Test]
    public void PlanResources_DoesNotDeduplicateIncompatibleMetadata()
    {
        GltfResource first = CreateResource("first.png", [7, 8]);
        GltfResource second = CreateResource("second.jpg", [7, 8]);
        second.MimeType = GltfMimeTypes.Jpeg;
        second.ImageFormat = GltfImageFormat.Jpeg;
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(first);
        source.Resources.Add(second);

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.DataUri,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.SourceResourceMap, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.SourceResourceUris[0], Does.StartWith("data:image/png;base64,"));
            Assert.That(result.SourceResourceUris[1], Does.StartWith("data:image/jpeg;base64,"));
        });
    }

    [Test]
    public void PlanResources_RebuildsCacheWithoutAliasingSourceResources()
    {
        GltfResource resource = CreateResource("asset.png", [3]);
        resource.CacheKey = "source-key";
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(resource);
        source.Cache[resource.CacheKey] = resource;

        ResourceWritePlan result = new ResourcePlanner().PlanResources(
            source,
            GltfResourceMode.External,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Cache["source-key"],
                Is.SameAs(result.Resources.Resources[0]));
            Assert.That(result.Resources.Cache["source-key"], Is.Not.SameAs(resource));
        });
    }

    [Test]
    public void PlanResources_ValidatesEverySourceBeforePlanningAnyOutput()
    {
        GltfResource first = CreateResource("first.png", [1]);
        GltfResource second = CreateResource("second.png", [2]);
        second.Bytes = null!;
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(first);
        source.Resources.Add(second);

        Assert.That(
            () => new ResourcePlanner().PlanResources(
                source,
                GltfResourceMode.External,
                false),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Document resource 1 bytes are null."));
        Assert.Multiple(() =>
        {
            Assert.That(first.SourceUri, Is.EqualTo("first.png"));
            Assert.That(first.EffectiveUri, Is.Null);
            Assert.That(first.Embedded, Is.False);
            Assert.That(first.Bytes, Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void PlanResources_LeavesGlbPackingForTheDedicatedPlanner()
    {
        GltfResourceSet source = new GltfResourceSet();
        source.Resources.Add(CreateResource("asset.png", [1]));

        Assert.That(
            () => new ResourcePlanner().PlanResources(
                source,
                GltfResourceMode.GlbBin,
                false),
            Throws.TypeOf<GltfUnsupportedFeatureException>().With.Message.EqualTo(
                "Resource layout mode GlbBin is not implemented."));
    }

    private static GltfResource CreateResource(string sourceUri, byte[] bytes)
    {
        GltfResource resource = new GltfResource();
        resource.Kind = GltfResourceKind.Image;
        resource.UriKind = GltfUriKind.Relative;
        resource.SourceUri = sourceUri;
        resource.MimeType = GltfMimeTypes.Png;
        resource.ImageFormat = GltfImageFormat.Png;
        resource.Bytes = bytes;
        return resource;
    }
}
