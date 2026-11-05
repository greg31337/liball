namespace gltf.Tests.Api.Classes;

[TestFixture]
public class FileGltfResourceResolverTests
{
    [Test]
    public void ResolveResource_DecodesDataUrisWithOwnedBytesAndDeterministicProvenance()
    {
        FileGltfResourceResolver resolver = new FileGltfResourceResolver();
        GltfResourceRequest request = new GltfResourceRequest();
        request.SourceUri = "DATA:application/octet-stream;base64,AAEC";
        request.Kind = GltfResourceKind.Buffer;
        request.HasDeclaredByteLength = true;
        request.DeclaredByteLength = 3;
        request.MaximumByteLength = 3;

        GltfResource first = resolver.ResolveResource(request);
        GltfResource second = resolver.ResolveResource(request);
        first.Bytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(first.Kind, Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(first.UriKind, Is.EqualTo(GltfUriKind.Data));
            Assert.That(first.SourceUri, Is.EqualTo(request.SourceUri));
            Assert.That(first.EffectiveUri, Is.EqualTo("data:application/octet-stream;base64,AAEC"));
            Assert.That(first.MimeType, Is.EqualTo("application/octet-stream"));
            Assert.That(first.Embedded, Is.True);
            Assert.That(first.HasDeclaredByteLength, Is.True);
            Assert.That(first.DeclaredByteLength, Is.EqualTo(3));
            Assert.That(first.CacheKey, Is.EqualTo("uri:" + first.EffectiveUri));
            Assert.That(second.CacheKey, Is.EqualTo(first.CacheKey));
            Assert.That(second.Bytes, Is.EqualTo(new byte[] { 0, 1, 2 }));
            Assert.That(second.Bytes, Is.Not.SameAs(first.Bytes));
        });
    }

    [TestCase("data:,abc", 2, "Data URI decoded length exceeds the configured maximum 2.")]
    [TestCase(
        "data:application/octet-stream;base64,AAEC",
        2,
        "Base64 decoded length 3 exceeds the configured maximum 2.")]
    public void ResolveResource_EnforcesTheDecodedDataUriLimit(
        string sourceUri,
        int maximumByteLength,
        string message)
    {
        GltfResourceRequest request = new GltfResourceRequest();
        request.SourceUri = sourceUri;
        request.MaximumByteLength = maximumByteLength;

        Assert.That(
            () => new FileGltfResourceResolver().ResolveResource(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [Test]
    public void ResolveResource_ReadsRelativeFileUrisAndReturnsIndependentBytes()
    {
        string directory = CreateTemporaryDirectory();
        string resourceDirectory = Path.Combine(directory, "textures");
        Directory.CreateDirectory(resourceDirectory);
        string resourcePath = Path.Combine(resourceDirectory, "a b.bin");
        File.WriteAllBytes(resourcePath, [1, 2, 3]);
        try
        {
            GltfResourceRequest request = new GltfResourceRequest();
            request.BaseUri = new Uri(Path.Combine(directory, "model.gltf")).AbsoluteUri;
            request.SourceUri = "textures/a%20b.bin?revision=1#part";
            request.Kind = GltfResourceKind.Buffer;
            request.DeclaredMimeType = "application/octet-stream";
            request.MaximumByteLength = 3;
            FileGltfResourceResolver resolver = new FileGltfResourceResolver();

            GltfResource first = resolver.ResolveResource(request);
            GltfResource second = resolver.ResolveResource(request);
            first.Bytes[0] = 9;

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(first.UriKind, Is.EqualTo(GltfUriKind.Relative));
                Assert.That(
                    first.EffectiveUri,
                    Is.EqualTo(new Uri(resourcePath).AbsoluteUri + "?revision=1#part"));
                Assert.That(first.CacheKey, Is.EqualTo("uri:" + first.EffectiveUri));
                Assert.That(first.MimeType, Is.EqualTo("application/octet-stream"));
                Assert.That(second.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(second.Bytes, Is.Not.SameAs(first.Bytes));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_DecodesUnicodePathsWithoutTreatingQueriesOrFragmentsAsFileNames()
    {
        string directory = CreateTemporaryDirectory();
        string resourceDirectory = Path.Combine(directory, "assets");
        Directory.CreateDirectory(resourceDirectory);
        string resourcePath = Path.Combine(resourceDirectory, "日本 #.bin");
        File.WriteAllBytes(resourcePath, [4, 5, 6]);
        try
        {
            GltfResourceRequest request = new GltfResourceRequest();
            request.BaseUri = new Uri(Path.Combine(directory, "model.gltf")).AbsoluteUri;
            request.SourceUri =
                "assets/%E6%97%A5%E6%9C%AC%20%23.bin?revision=%32#part%2Fone";

            GltfResource result = new FileGltfResourceResolver().ResolveResource(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 5, 6 }));
                Assert.That(
                    result.EffectiveUri,
                    Is.EqualTo(
                        new Uri(resourcePath).AbsoluteUri
                        + "?revision=2#part%2Fone"));
                Assert.That(result.CacheKey, Is.EqualTo("uri:" + result.EffectiveUri));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ReaderAndWriterUseNativePathsAndPortableResourceUris()
    {
        string directory = CreateTemporaryDirectory();
        string modelDirectory = Path.Combine(directory, "mødel space");
        string bufferDirectory = Path.Combine(modelDirectory, "buffers");
        string outputDirectory = Path.Combine(directory, "output space");
        Directory.CreateDirectory(bufferDirectory);
        Directory.CreateDirectory(outputDirectory);
        string resourcePath = Path.Combine(bufferDirectory, "data file.bin");
        string inputPath = Path.Combine(modelDirectory, "scene.GLTF");
        string outputPath = Path.Combine(outputDirectory, "round trip.GLTF");
        File.WriteAllBytes(resourcePath, [7, 8, 9]);
        File.WriteAllText(
            inputPath,
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"uri\":\"buffers/data%20file.bin\",\"byteLength\":3}]}");
        try
        {
            GltfReadOptions readOptions = new GltfReadOptions();
            readOptions.ResolveResources = true;
            readOptions.Validate = true;
            GltfReadResult read = new GltfReader().ReadFile(inputPath, readOptions);

            GltfWriteOptions writeOptions = new GltfWriteOptions();
            writeOptions.Format = GltfContainerFormat.Auto;
            writeOptions.ResourceMode = GltfResourceMode.DataUri;
            writeOptions.Validate = false;
            GltfWriteResult write = new GltfWriter().WriteFile(
                read.Document!,
                outputPath,
                writeOptions);

            GltfReadOptions repeatedOptions = new GltfReadOptions();
            repeatedOptions.ResolveResources = true;
            GltfReadResult repeated = new GltfReader().ReadFile(
                outputPath,
                repeatedOptions);

            Assert.Multiple(() =>
            {
                Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(readOptions.BaseUri, Is.Null);
                Assert.That(
                    read.Document!.Buffers[0].ResolvedBytes,
                    Is.EqualTo(new byte[] { 7, 8, 9 }));
                Assert.That(
                    read.Document.Resources.Resources[0].EffectiveUri,
                    Is.EqualTo(new Uri(resourcePath).AbsoluteUri));
                Assert.That(write.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(write.PrimaryBytes, Is.Empty);
                Assert.That(File.Exists(outputPath), Is.True);
                Assert.That(repeated.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(
                    repeated.Document!.Buffers[0].ResolvedBytes,
                    Is.EqualTo(new byte[] { 7, 8, 9 }));
                Assert.That(
                    repeated.Document.Buffers[0].Uri,
                    Does.StartWith("data:"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_SearchesConfiguredRootsInOrder()
    {
        string directory = CreateTemporaryDirectory();
        string firstRoot = Path.Combine(directory, "first");
        string secondRoot = Path.Combine(directory, "second");
        Directory.CreateDirectory(firstRoot);
        Directory.CreateDirectory(Path.Combine(secondRoot, "assets"));
        string resourcePath = Path.Combine(secondRoot, "assets", "buffer.bin");
        File.WriteAllBytes(resourcePath, [4, 5]);
        try
        {
            GltfResourceRequest request = new GltfResourceRequest();
            request.SourceUri = "assets/buffer.bin";
            request.Policy.FileRoots.Add(firstRoot);
            request.Policy.FileRoots.Add(new Uri(secondRoot).AbsoluteUri);

            GltfResource result = new FileGltfResourceResolver().ResolveResource(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 5 }));
                Assert.That(result.EffectiveUri, Is.EqualTo(new Uri(resourcePath).AbsoluteUri));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_EnforcesParentTraversalAndRootBoundaries()
    {
        string directory = CreateTemporaryDirectory();
        string root = Path.Combine(directory, "root");
        Directory.CreateDirectory(Path.Combine(root, "nested"));
        string insidePath = Path.Combine(root, "inside.bin");
        string outsidePath = Path.Combine(directory, "outside.bin");
        File.WriteAllBytes(insidePath, [1]);
        File.WriteAllBytes(outsidePath, [2]);
        try
        {
            GltfResourceRequest deniedTraversal = new GltfResourceRequest();
            deniedTraversal.SourceUri = "nested/../inside.bin";
            deniedTraversal.Policy.FileRoots.Add(root);
            GltfResourceRequest allowedTraversal = new GltfResourceRequest();
            allowedTraversal.SourceUri = "nested/%2E%2E/inside.bin";
            allowedTraversal.Policy.FileRoots.Add(root);
            allowedTraversal.Policy.AllowParentPathSegments = true;
            GltfResourceRequest escapedRoot = new GltfResourceRequest();
            escapedRoot.SourceUri = "../outside.bin";
            escapedRoot.Policy.FileRoots.Add(root);
            escapedRoot.Policy.AllowParentPathSegments = true;
            FileGltfResourceResolver resolver = new FileGltfResourceResolver();

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => resolver.ResolveResource(deniedTraversal),
                    Throws.TypeOf<GltfResourceException>().With.Message.Contains("Parent path traversal"));
                Assert.That(
                    resolver.ResolveResource(allowedTraversal).Bytes,
                    Is.EqualTo(new byte[] { 1 }));
                Assert.That(
                    () => resolver.ResolveResource(escapedRoot),
                    Throws.TypeOf<GltfResourceException>().With.Message.Contains("outside the configured file roots"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_DeniesParentTraversalHiddenByEncodedSeparators()
    {
        string directory = CreateTemporaryDirectory();
        string root = Path.Combine(directory, "root");
        Directory.CreateDirectory(Path.Combine(root, "nested"));
        File.WriteAllBytes(Path.Combine(directory, "outside.bin"), [9]);
        try
        {
            GltfResourceRequest traversalDenied = new GltfResourceRequest();
            traversalDenied.SourceUri = "nested/%2E%2E%2F%2E%2E/outside.bin";
            traversalDenied.Policy.FileRoots.Add(root);
            GltfResourceRequest rootEscapeDenied = new GltfResourceRequest();
            rootEscapeDenied.SourceUri = traversalDenied.SourceUri;
            rootEscapeDenied.Policy.FileRoots.Add(root);
            rootEscapeDenied.Policy.AllowParentPathSegments = true;
            FileGltfResourceResolver resolver = new FileGltfResourceResolver();

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => resolver.ResolveResource(traversalDenied),
                    Throws.TypeOf<GltfResourceException>()
                        .With.Message.Contains("Parent path traversal"));
                Assert.That(
                    () => resolver.ResolveResource(rootEscapeDenied),
                    Throws.TypeOf<GltfResourceException>()
                        .With.Message.Contains("outside the configured file roots"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_RequiresAbsolutePathOptInAndStillEnforcesRoots()
    {
        string directory = CreateTemporaryDirectory();
        string root = Path.Combine(directory, "root");
        Directory.CreateDirectory(root);
        string resourcePath = Path.Combine(root, "buffer.bin");
        string outsidePath = Path.Combine(directory, "outside.bin");
        File.WriteAllBytes(resourcePath, [6]);
        File.WriteAllBytes(outsidePath, [7]);
        try
        {
            GltfResourceRequest denied = new GltfResourceRequest();
            denied.SourceUri = new Uri(resourcePath).AbsoluteUri;
            denied.Policy.FileRoots.Add(root);
            GltfResourceRequest allowed = new GltfResourceRequest();
            allowed.SourceUri = denied.SourceUri;
            allowed.Policy.FileRoots.Add(root);
            allowed.Policy.AllowAbsolutePaths = true;
            GltfResourceRequest outside = new GltfResourceRequest();
            outside.SourceUri = new Uri(outsidePath).AbsoluteUri;
            outside.Policy.FileRoots.Add(root);
            outside.Policy.AllowAbsolutePaths = true;
            FileGltfResourceResolver resolver = new FileGltfResourceResolver();

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => resolver.ResolveResource(denied),
                    Throws.TypeOf<GltfResourceException>().With.Message.Contains("Absolute resource path"));
                Assert.That(resolver.ResolveResource(allowed).Bytes, Is.EqualTo(new byte[] { 6 }));
                Assert.That(
                    () => resolver.ResolveResource(outside),
                    Throws.TypeOf<GltfResourceException>().With.Message.Contains("outside the configured file roots"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_RejectsAFileReachedThroughASymlinkOutsideTheRoot()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Symbolic-link creation is not consistently available to unprivileged Windows tests.");
        }

        string directory = CreateTemporaryDirectory();
        string root = Path.Combine(directory, "root");
        string outside = Path.Combine(directory, "outside");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        File.WriteAllBytes(Path.Combine(outside, "secret.bin"), [8]);
        Directory.CreateSymbolicLink(Path.Combine(root, "linked"), outside);
        try
        {
            GltfResourceRequest request = new GltfResourceRequest();
            request.SourceUri = "linked/secret.bin";
            request.Policy.FileRoots.Add(root);

            Assert.That(
                () => new FileGltfResourceResolver().ResolveResource(request),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.Contains("outside the configured file roots"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_ReturnsUnavailableWithoutAccessingNetworkOrMissingFiles()
    {
        GltfResourceRequest network = new GltfResourceRequest();
        network.SourceUri = "https://example.invalid/buffer.bin";
        GltfResourceRequest missing = new GltfResourceRequest();
        missing.SourceUri = "missing.bin";
        missing.Policy.FileRoots.Add(CreateTemporaryDirectory());
        try
        {
            FileGltfResourceResolver resolver = new FileGltfResourceResolver();
            GltfResource networkResult = resolver.ResolveResource(network);
            GltfResource missingResult = resolver.ResolveResource(missing);

            Assert.Multiple(() =>
            {
                Assert.That(networkResult.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
                Assert.That(networkResult.UriKind, Is.EqualTo(GltfUriKind.Network));
                Assert.That(networkResult.Bytes, Is.Empty);
                Assert.That(missingResult.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
                Assert.That(missingResult.CacheKey, Does.StartWith("uri:file:"));
            });
        }
        finally
        {
            Directory.Delete(missing.Policy.FileRoots[0], true);
        }
    }

    [Test]
    public void ResolveResource_RejectsOversizedFilesBeforeReturningBytes()
    {
        string directory = CreateTemporaryDirectory();
        string resourcePath = Path.Combine(directory, "buffer.bin");
        File.WriteAllBytes(resourcePath, [1, 2, 3]);
        try
        {
            GltfResourceRequest request = new GltfResourceRequest();
            request.SourceUri = "buffer.bin";
            request.MaximumByteLength = 2;
            request.Policy.FileRoots.Add(directory);

            Assert.That(
                () => new FileGltfResourceResolver().ResolveResource(request),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("Resource length 3 exceeds the configured maximum 2."));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolveResource_RejectsRelativeFilesWithoutABaseOrRoot()
    {
        GltfResourceRequest request = new GltfResourceRequest();
        request.SourceUri = "buffer.bin";

        Assert.That(
            () => new FileGltfResourceResolver().ResolveResource(request),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo(
                    "Relative resource URI 'buffer.bin' requires a file base URI or configured file root."));
    }

    [Test]
    public void ResolveResource_RejectsInvalidPolicyAndSizeInputs()
    {
        GltfResourceRequest missingRoots = new GltfResourceRequest();
        missingRoots.Policy.FileRoots = null!;
        GltfResourceRequest negativeLimit = new GltfResourceRequest();
        negativeLimit.MaximumByteLength = -1;
        FileGltfResourceResolver resolver = new FileGltfResourceResolver();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => resolver.ResolveResource(missingRoots),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("request"));
            Assert.That(
                () => resolver.ResolveResource(negativeLimit),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("request"));
        });
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "gltf-resolver-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
