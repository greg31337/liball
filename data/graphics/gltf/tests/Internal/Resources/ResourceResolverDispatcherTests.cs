namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class ResourceResolverDispatcherTests
{
    [Test]
    public void DispatchResource_ReusesAnEffectiveIdentityAndPreservesCurrentContext()
    {
        ProbeResolver resolver = new ProbeResolver();
        resolver.EffectiveUri = "FILE:///assets/shared.bin";
        resolver.CacheKey = "uri:file:///assets/shared.bin";
        resolver.Bytes = [1, 2, 3];
        resolver.MimeType = "application/octet-stream";
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest bufferRequest = new GltfResourceRequest();
        bufferRequest.BaseUri = "file:///assets/model.gltf";
        bufferRequest.SourceUri = "shared.bin";
        bufferRequest.Kind = GltfResourceKind.Buffer;
        bufferRequest.HasDeclaredByteLength = true;
        bufferRequest.DeclaredByteLength = 3;
        GltfResourceRequest imageRequest = new GltfResourceRequest();
        imageRequest.BaseUri = bufferRequest.BaseUri;
        imageRequest.SourceUri = bufferRequest.SourceUri;
        imageRequest.Kind = GltfResourceKind.Image;
        imageRequest.DeclaredMimeType = "image/png";

        ResourceDispatchData first = dispatcher.DispatchResource(bufferRequest);
        resolver.Bytes[0] = 8;
        first.Resource.Bytes[1] = 9;
        ResourceDispatchData second = dispatcher.DispatchResource(imageRequest);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(first.CacheHit, Is.False);
            Assert.That(first.NewContent, Is.True);
            Assert.That(first.Resource.Kind, Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(first.Resource.SourceUri, Is.EqualTo("shared.bin"));
            Assert.That(first.Resource.HasDeclaredByteLength, Is.True);
            Assert.That(first.Resource.DeclaredByteLength, Is.EqualTo(3));
            Assert.That(second.CacheHit, Is.True);
            Assert.That(second.NewContent, Is.False);
            Assert.That(second.Resource.Kind, Is.EqualTo(GltfResourceKind.Image));
            Assert.That(second.Resource.SourceUri, Is.EqualTo("shared.bin"));
            Assert.That(second.Resource.MimeType, Is.EqualTo("image/png"));
            Assert.That(second.Resource.HasDeclaredByteLength, Is.False);
            Assert.That(second.Resource.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(second.Resource.Bytes, Is.Not.SameAs(first.Resource.Bytes));
        });
    }

    [Test]
    public void DispatchResource_UsesContentIdentityWhenNoUriIdentityIsAvailable()
    {
        ProbeResolver resolver = new ProbeResolver();
        resolver.Bytes = [7, 8];
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest firstRequest = new GltfResourceRequest();
        firstRequest.SourceUri = "custom:first";
        firstRequest.Kind = GltfResourceKind.Buffer;
        GltfResourceRequest secondRequest = new GltfResourceRequest();
        secondRequest.SourceUri = "custom:second";
        secondRequest.Kind = GltfResourceKind.Image;

        ResourceDispatchData first = dispatcher.DispatchResource(firstRequest);
        ResourceDispatchData second = dispatcher.DispatchResource(secondRequest);
        ResourceDispatchData repeated = dispatcher.DispatchResource(secondRequest);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(2));
            Assert.That(first.NewContent, Is.True);
            Assert.That(second.NewContent, Is.False);
            Assert.That(repeated.CacheHit, Is.True);
            Assert.That(first.Resource.CacheKey, Does.StartWith("content:sha256:"));
            Assert.That(first.Resource.CacheKey, Is.EqualTo(second.Resource.CacheKey));
            Assert.That(second.Resource.Kind, Is.EqualTo(GltfResourceKind.Image));
            Assert.That(second.Resource.SourceUri, Is.EqualTo("custom:second"));
            Assert.That(second.Resource.Bytes, Is.EqualTo(new byte[] { 7, 8 }));
        });
    }

    [Test]
    public void DispatchResource_DerivesACacheKeyFromTheNormalizedEffectiveUri()
    {
        ProbeResolver resolver = new ProbeResolver();
        resolver.EffectiveUri = "FILE:///assets/%73hared.bin";
        resolver.Bytes = [3];
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest request = new GltfResourceRequest();
        request.SourceUri = "custom:asset";

        ResourceDispatchData first = dispatcher.DispatchResource(request);
        ResourceDispatchData second = dispatcher.DispatchResource(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Resource.CacheKey, Is.EqualTo("uri:file:///assets/shared.bin"));
            Assert.That(second.Resource.CacheKey, Is.EqualTo(first.Resource.CacheKey));
            Assert.That(second.CacheHit, Is.True);
            Assert.That(resolver.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DispatchResource_DoesNotReuseAcrossDifferentPolicies()
    {
        ProbeResolver resolver = new ProbeResolver();
        resolver.CacheKey = "shared";
        resolver.Bytes = [1];
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest restrictive = new GltfResourceRequest();
        restrictive.SourceUri = "asset.bin";
        restrictive.Policy.FileRoots.Add("/first");
        GltfResourceRequest permissive = new GltfResourceRequest();
        permissive.SourceUri = "asset.bin";
        permissive.Policy.FileRoots.Add("/second");
        permissive.Policy.AllowParentPathSegments = true;

        dispatcher.DispatchResource(restrictive);
        dispatcher.DispatchResource(permissive);

        Assert.That(resolver.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void DispatchResource_PreservesPerRequestEffectiveUriForASharedResolverIdentity()
    {
        SharedIdentityResolver resolver = new SharedIdentityResolver();
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest firstRequest = new GltfResourceRequest();
        firstRequest.SourceUri = "custom:first";
        GltfResourceRequest secondRequest = new GltfResourceRequest();
        secondRequest.SourceUri = "custom:second";

        ResourceDispatchData first = dispatcher.DispatchResource(firstRequest);
        ResourceDispatchData second = dispatcher.DispatchResource(secondRequest);
        ResourceDispatchData repeated = dispatcher.DispatchResource(secondRequest);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(2));
            Assert.That(first.Resource.EffectiveUri, Is.EqualTo("https://cdn.invalid/custom:first"));
            Assert.That(second.Resource.EffectiveUri, Is.EqualTo("https://cdn.invalid/custom:second"));
            Assert.That(repeated.Resource.EffectiveUri, Is.EqualTo(second.Resource.EffectiveUri));
            Assert.That(second.Resource.CacheKey, Is.EqualTo("shared"));
            Assert.That(repeated.CacheHit, Is.True);
        });
    }

    [Test]
    public void DispatchResource_RejectsCacheIdentityContentChanges()
    {
        ChangingResolver resolver = new ChangingResolver();
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest first = new GltfResourceRequest();
        first.SourceUri = "custom:first";
        GltfResourceRequest second = new GltfResourceRequest();
        second.SourceUri = "custom:second";

        dispatcher.DispatchResource(first);

        Assert.That(
            () => dispatcher.DispatchResource(second),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo("Resource cache identity 'shared' was returned with different content."));
    }

    [Test]
    public void DispatchResource_DoesNotCacheUnavailableResults()
    {
        ProbeResolver resolver = new ProbeResolver();
        resolver.Status = GltfOperationStatus.Unavailable;
        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = resolver;
        GltfResourceRequest request = new GltfResourceRequest();
        request.SourceUri = "custom:missing";
        request.Kind = GltfResourceKind.Schema;

        ResourceDispatchData first = dispatcher.DispatchResource(request);
        ResourceDispatchData second = dispatcher.DispatchResource(request);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(2));
            Assert.That(first.Resource.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(first.Resource.Kind, Is.EqualTo(GltfResourceKind.Schema));
            Assert.That(first.Resource.SourceUri, Is.EqualTo("custom:missing"));
            Assert.That(first.CacheHit, Is.False);
            Assert.That(second.CacheHit, Is.False);
        });
    }

    private class ProbeResolver : IGltfResourceResolver
    {
        internal int CallCount;
        internal GltfOperationStatus Status = GltfOperationStatus.Success;
        internal string? EffectiveUri;
        internal string? CacheKey;
        internal string? MimeType;
        internal byte[] Bytes = [];

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            CallCount++;
            GltfResource result = new GltfResource();
            result.Status = Status;
            result.EffectiveUri = EffectiveUri;
            result.CacheKey = CacheKey;
            result.MimeType = MimeType;
            result.Bytes = Bytes;
            return result;
        }
    }

    private class ChangingResolver : IGltfResourceResolver
    {
        internal int CallCount;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            GltfResource result = new GltfResource();
            result.CacheKey = "shared";
            result.Bytes = CallCount == 0 ? new byte[] { 1 } : new byte[] { 2 };
            CallCount++;
            return result;
        }
    }

    private class SharedIdentityResolver : IGltfResourceResolver
    {
        internal int CallCount;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            CallCount++;
            GltfResource result = new GltfResource();
            result.EffectiveUri = "https://cdn.invalid/" + request.SourceUri;
            result.CacheKey = "shared";
            result.Bytes = [4];
            return result;
        }
    }
}
