using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfReaderTests
{
    private static readonly byte[] MinimalJson = Encoding.UTF8.GetBytes(
        "{\"asset\":{\"version\":\"2.0\"},\"extras\":{\"owned\":true}}");

    [Test]
    public void DocumentEntryPoints_ConstructModelsWhileRawEntryPointsRemainContainerOnly()
    {
        byte[] bytes = (byte[])MinimalJson.Clone();
        GltfReader reader = new GltfReader();
        GltfReadResult byteResult = reader.ReadBytes(bytes, new GltfReadOptions());
        using MemoryStream documentStream = new MemoryStream(MinimalJson);
        GltfReadResult streamResult = reader.ReadStream(documentStream, new GltfReadOptions());
        GltfContainerData suppliedContainer = new GltfContainerData();
        suppliedContainer.Format = GltfContainerFormat.Json;
        suppliedContainer.JsonBytes = (byte[])MinimalJson.Clone();
        GltfReadResult containerResult = reader.ReadContainer(suppliedContainer, new GltfReadOptions());
        GltfReadResult rawByteResult = reader.ReadContainerBytes(MinimalJson, new GltfReadOptions());
        using MemoryStream rawStream = new MemoryStream(MinimalJson);
        GltfReadResult rawStreamResult = reader.ReadContainerStream(rawStream, new GltfReadOptions());
        bytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(byteResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(byteResult.Document, Is.Not.Null);
            Assert.That(byteResult.Document!.Asset.Version, Is.EqualTo("2.0"));
            Assert.That(byteResult.Container, Is.SameAs(byteResult.Document.Container));
            Assert.That(byteResult.Resources, Is.SameAs(byteResult.Document.Resources));
            Assert.That(byteResult.Container!.JsonBytes[0], Is.EqualTo((byte)'{'));
            Assert.That(byteResult.Document.PropertyJson.Extras!["owned"]!.GetValue<bool>(), Is.True);
            Assert.That(streamResult.Document, Is.Not.Null);
            Assert.That(documentStream.CanRead, Is.True);
            Assert.That(containerResult.Document, Is.Not.Null);
            Assert.That(containerResult.Container, Is.SameAs(suppliedContainer));
            Assert.That(rawByteResult.Document, Is.Null);
            Assert.That(rawByteResult.Container, Is.Not.Null);
            Assert.That(rawStreamResult.Document, Is.Null);
            Assert.That(rawStreamResult.Container, Is.Not.Null);
        });
    }

    [Test]
    public void ReadBytes_OwnsGlbProvenanceAndEmbeddedBinResource()
    {
        GltfContainerData source = new GltfContainerData();
        source.Format = GltfContainerFormat.Glb;
        source.JsonBytes = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":3}]}");
        source.HasBinChunk = true;
        source.BinBytes = [1, 2, 3];
        byte[] bytes = new GlbWriter().WriteBytes(source, true);
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;

        GltfReadResult result = new GltfReader().ReadBytes(bytes, options);
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Container, Is.Not.Null);
        GltfResource resource = result.Resources.Resources[0];
        result.Container!.BinBytes[0] = 9;
        bytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Container.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(result.Document!.Container, Is.SameAs(result.Container));
            Assert.That(result.Document.Resources, Is.SameAs(result.Resources));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(resource.Kind, Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(resource.UriKind, Is.EqualTo(GltfUriKind.Absent));
            Assert.That(resource.Embedded, Is.True);
            Assert.That(resource.HasDeclaredByteLength, Is.True);
            Assert.That(resource.DeclaredByteLength, Is.EqualTo(3));
            Assert.That(resource.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(result.Container.JsonBytes[0], Is.EqualTo((byte)'{'));
        });
    }

    [Test]
    public void ReadBytes_DispatchesBufferAndImageUrisToTheConfiguredResolver()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "asset": { "version": "2.0" },
              "buffers": [
                { "uri": "first.bin", "byteLength": 2 },
                { "uri": "second.bin", "byteLength": 3 }
              ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": 1 }
              ],
              "images": [
                { "uri": "image.png", "mimeType": "image/png" },
                { "bufferView": 0, "mimeType": "image/png" }
              ]
            }
            """);
        RecordingResolver resolver = new RecordingResolver();
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        options.BaseUri = "file:///models/model.gltf";
        options.Limits.MaximumResourceByteLength = 10;
        options.ResourcePolicy.FileRoots.Add("/models");
        options.ResourcePolicy.AllowAbsolutePaths = true;
        options.ResourcePolicy.AllowParentPathSegments = true;

        GltfReadResult result = new GltfReader().ReadBytes(json, options);
        resolver.Results[0].Bytes[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Requests, Has.Count.EqualTo(3));
            Assert.That(resolver.Requests.Select(request => request.SourceUri),
                Is.EqualTo(new[] { "first.bin", "second.bin", "image.png" }));
            Assert.That(resolver.Requests.Select(request => request.Kind),
                Is.EqualTo(new[] { GltfResourceKind.Buffer, GltfResourceKind.Buffer, GltfResourceKind.Image }));
            Assert.That(resolver.Requests[0].BaseUri, Is.EqualTo("file:///models/model.gltf"));
            Assert.That(resolver.Requests[0].HasDeclaredByteLength, Is.True);
            Assert.That(resolver.Requests[0].DeclaredByteLength, Is.EqualTo(2));
            Assert.That(resolver.Requests[2].HasDeclaredByteLength, Is.False);
            Assert.That(resolver.Requests[2].DeclaredMimeType, Is.EqualTo("image/png"));
            Assert.That(resolver.Requests[2].MaximumByteLength, Is.EqualTo(10));
            Assert.That(resolver.Requests[0].Policy, Is.Not.SameAs(options.ResourcePolicy));
            Assert.That(resolver.Requests[0].Policy.FileRoots, Is.EqualTo(new[] { "/models" }));
            Assert.That(resolver.Requests[0].Policy.FileRoots, Is.Not.SameAs(options.ResourcePolicy.FileRoots));
            Assert.That(resolver.Requests[0].Policy.AllowAbsolutePaths, Is.True);
            Assert.That(resolver.Requests[0].Policy.AllowParentPathSegments, Is.True);
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(4));
            Assert.That(result.Resources.Resources[0].Bytes[0], Is.EqualTo((byte)1));
            Assert.That(result.Document!.Resources, Is.SameAs(result.Resources));
            Assert.That(result.Resources.Cache.Keys,
                Is.EqualTo(new[] { "resource:0", "resource:1", "resource:2", "bufferView:0" }));
        });
    }

    [Test]
    public void ReadBytes_EnforcesPerResourceAndTotalResolutionLimits()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"a.bin\",\"byteLength\":2},{\"uri\":\"b.bin\",\"byteLength\":2}]}");
        GltfReadOptions perResource = new GltfReadOptions();
        perResource.ResolveResources = true;
        perResource.ResourceResolver = new RecordingResolver();
        perResource.Limits.MaximumResourceByteLength = 1;
        GltfReadOptions total = new GltfReadOptions();
        total.ResolveResources = true;
        total.ResourceResolver = new RecordingResolver();
        total.Limits.MaximumResourceByteLength = 10;
        total.Limits.MaximumTotalResourceByteLength = 3;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadBytes(json, perResource),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Resolved resource length 2 exceeds the configured maximum 1."));
            Assert.That(
                () => new GltfReader().ReadBytes(json, total),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Total resolved resource length 4 exceeds the configured maximum 3."));
        });
    }

    [Test]
    public void ReadBytes_ReusesResolvedContentWhileKeepingEachRequestContext()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"shared.bin\",\"byteLength\":2}],\"images\":[{\"uri\":\"shared.bin\",\"mimeType\":\"image/png\"}]}");
        RecordingResolver resolver = new RecordingResolver();
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        options.BaseUri = "file:///models/model.gltf";
        options.Limits.MaximumTotalResourceByteLength = 2;

        GltfReadResult result = new GltfReader().ReadBytes(json, options);
        GltfResource buffer = result.Resources.Resources[0];
        GltfResource image = result.Resources.Resources[1];
        buffer.Bytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Cache, Has.Count.EqualTo(1));
            Assert.That(buffer.Kind, Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(buffer.SourceUri, Is.EqualTo("shared.bin"));
            Assert.That(buffer.HasDeclaredByteLength, Is.True);
            Assert.That(buffer.DeclaredByteLength, Is.EqualTo(2));
            Assert.That(image.Kind, Is.EqualTo(GltfResourceKind.Image));
            Assert.That(image.SourceUri, Is.EqualTo("shared.bin"));
            Assert.That(image.MimeType, Is.EqualTo("image/png"));
            Assert.That(image.HasDeclaredByteLength, Is.False);
            Assert.That(image.CacheKey, Is.EqualTo(buffer.CacheKey));
            Assert.That(image.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(image.Bytes, Is.Not.SameAs(buffer.Bytes));
        });
    }

    [Test]
    public void ReadBytes_DeduplicatesSharedCustomResolverIdentitiesAcrossDistinctUris()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"asset:first\",\"byteLength\":2},{\"uri\":\"asset:second\",\"byteLength\":2}]}");
        SharedIdentityResolver resolver = new SharedIdentityResolver();
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        options.Limits.MaximumTotalResourceByteLength = 2;

        GltfReadResult result = new GltfReader().ReadBytes(json, options);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(2));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Cache.Keys, Is.EqualTo(new[] { "shared-custom" }));
            Assert.That(
                result.Resources.Resources.Select(resource => resource.SourceUri),
                Is.EqualTo(new[] { "asset:first", "asset:second" }));
            Assert.That(
                result.Resources.Resources.Select(resource => resource.EffectiveUri),
                Is.EqualTo(new[]
                {
                    "https://cdn.invalid/asset:first",
                    "https://cdn.invalid/asset:second"
                }));
            Assert.That(result.Document!.Buffers[0].ResolvedBytes, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.Document.Buffers[1].ResolvedBytes, Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void ReadBytes_InvokesOptionalValidationAndMapsInvalidStatus()
    {
        RecordingValidator validator = new RecordingValidator();
        validator.Report.IsValid = false;
        GltfReader reader = new GltfReader();
        reader.Validator = validator;
        GltfReadOptions options = new GltfReadOptions();
        options.Validate = true;

        GltfReadResult result = reader.ReadBytes(MinimalJson, options);

        Assert.Multiple(() =>
        {
            Assert.That(validator.DocumentCallCount, Is.EqualTo(1));
            Assert.That(validator.Document, Is.SameAs(result.Document));
            Assert.That(validator.Options, Is.SameAs(options.ValidationOptions));
            Assert.That(result.Validation, Is.SameAs(validator.Report));
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
        });

        options.Validate = false;
        reader.ReadBytes(MinimalJson, options);
        Assert.That(validator.DocumentCallCount, Is.EqualTo(1));
    }

    [Test]
    public void ReadFile_UsesTheDocumentFileUriAsTheDefaultResourceBaseWithoutMutatingOptions()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gltf-reader-{Guid.NewGuid():N}.gltf");
        RecordingResolver resolver = new RecordingResolver();
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        File.WriteAllText(
            path,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":2}]}",
            new UTF8Encoding(false));

        try
        {
            GltfReadResult result = new GltfReader().ReadFile(path, options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Document, Is.Not.Null);
                Assert.That(resolver.Requests, Has.Count.EqualTo(1));
                Assert.That(
                    resolver.Requests[0].BaseUri,
                    Is.EqualTo(new Uri(Path.GetFullPath(path)).AbsoluteUri));
                Assert.That(options.BaseUri, Is.Null);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReadFile_UsesTheBuiltInResolverForResourcesBesideTheDocument()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"gltf-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string documentPath = Path.Combine(directory, "model.gltf");
        string resourcePath = Path.Combine(directory, "buffer.bin");
        File.WriteAllText(
            documentPath,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":3}]}",
            new UTF8Encoding(false));
        File.WriteAllBytes(resourcePath, [1, 2, 3]);
        try
        {
            GltfReadOptions options = new GltfReadOptions();
            options.ResolveResources = true;

            GltfReadResult result = new GltfReader().ReadFile(documentPath, options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
                Assert.That(result.Resources.Resources[0].Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(result.Resources.Resources[0].EffectiveUri, Is.EqualTo(new Uri(resourcePath).AbsoluteUri));
                Assert.That(options.BaseUri, Is.Null);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ReadContainer_AppliesContainerAndOptionLimits()
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        container.JsonBytes = (byte[])MinimalJson.Clone();
        container.OriginalByteLength = MinimalJson.Length;
        GltfReadOptions inputLimit = new GltfReadOptions();
        inputLimit.Limits.MaximumInputByteLength = 1;
        GltfReadOptions invalidResourceLimit = new GltfReadOptions();
        invalidResourceLimit.Limits.MaximumResourceByteLength = -1;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadContainer(container, inputLimit),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        $"Input length {MinimalJson.Length} exceeds the configured maximum 1 at byte 1."));
            Assert.That(
                () => new GltfReader().ReadBytes(MinimalJson, invalidResourceLimit),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("options"));
        });
    }

    private class RecordingResolver : IGltfResourceResolver
    {
        internal List<GltfResourceRequest> Requests = new List<GltfResourceRequest>();
        internal List<GltfResource> Results = new List<GltfResource>();

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            Requests.Add(request);
            GltfResource result = new GltfResource();
            result.Kind = request.Kind;
            result.SourceUri = request.SourceUri;
            result.MimeType = request.DeclaredMimeType;
            if (request.Kind == GltfResourceKind.Buffer)
            {
                if (request.SourceUri == "second.bin")
                {
                    result.Bytes = [2, 3, 4];
                }
                else if (request.SourceUri == "b.bin")
                {
                    result.Bytes = [2, 3];
                }
                else
                {
                    result.Bytes = [1, 2];
                }
            }
            else
            {
                result.Bytes = [3];
            }
            result.CacheKey = $"resource:{Results.Count}";
            result.HasDeclaredByteLength = request.HasDeclaredByteLength;
            result.DeclaredByteLength = request.DeclaredByteLength;
            Results.Add(result);
            return result;
        }
    }

    private class RecordingValidator : IGltfValidator
    {
        internal GltfValidationReport Report = new GltfValidationReport();
        internal int DocumentCallCount;
        internal GltfDocument? Document;
        internal GltfValidationOptions? Options;

        public GltfValidationReport ValidateDocument(GltfDocument document, GltfValidationOptions options)
        {
            DocumentCallCount++;
            Document = document;
            Options = options;
            return Report;
        }

        public GltfValidationReport ValidateFile(string path, GltfValidationOptions options)
        {
            throw new InvalidOperationException();
        }

        public GltfValidationReport ValidateBytes(byte[] bytes, GltfValidationOptions options)
        {
            throw new InvalidOperationException();
        }

        public GltfValidationReport ValidateContainer(GltfContainerData container, GltfValidationOptions options)
        {
            throw new InvalidOperationException();
        }
    }

    private class SharedIdentityResolver : IGltfResourceResolver
    {
        internal int CallCount;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            CallCount++;
            GltfResource result = new GltfResource();
            result.SourceUri = request.SourceUri;
            result.EffectiveUri = "https://cdn.invalid/" + request.SourceUri;
            result.CacheKey = "shared-custom";
            result.Bytes = [1, 2];
            return result;
        }
    }
}
