namespace gltf.Tests.Api.Exceptions;

[TestFixture]
public class GltfExceptionTests
{
    [Test]
    public void PublicExceptionInventory_ContainsEveryExpectedFailureType()
    {
        string[] expected =
        [
            nameof(GltfParseException),
            nameof(GltfResourceException),
            nameof(GltfUnsupportedFeatureException),
            nameof(GltfValidationException),
            nameof(GltfWriteException)
        ];
        string[] actual = typeof(GltfDocument).Assembly.GetExportedTypes()
            .Where(type => type.IsSubclassOf(typeof(Exception)))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ParseException_IsRaisedByPublicReaderFailure()
    {
        byte[] malformedJson = System.Text.Encoding.UTF8.GetBytes("{");

        Assert.That(
            () => new GltfReader().ReadBytes(malformedJson, new GltfReadOptions()),
            Throws.TypeOf<GltfParseException>()
                .With.Message.Contains("JSON"));
    }

    [Test]
    public void ResourceException_IsRaisedByPublicResolverFailure()
    {
        GltfResourceRequest request = new GltfResourceRequest
        {
            SourceUri = "data:application/octet-stream;base64,AAEC",
            MaximumByteLength = 2
        };

        Assert.That(
            () => new FileGltfResourceResolver().ResolveResource(request),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo(
                    "Base64 decoded length 3 exceeds the configured maximum 2."));
    }

    [Test]
    public void ValidationException_IsRaisedByStrictPublicEditFailure()
    {
        GltfDocument document = new GltfDocument();
        document.Nodes.Add(new GltfNode { HasMesh = true, Mesh = 3 });

        Assert.That(
            () => new GltfDocumentEditor().CloneDocument(
                document,
                new GltfEditOptions()),
            Throws.TypeOf<GltfValidationException>());
    }

    [Test]
    public void WriteException_IsRaisedByPublicWriterFailure()
    {
        GltfDocument document = new GltfDocument();
        document.Nodes.Add(new GltfNode
        {
            HasTranslation = true,
            Translation = [0.0, double.NaN, 0.0]
        });
        GltfWriteOptions options = new GltfWriteOptions { Validate = false };

        Assert.That(
            () => new GltfWriter().WriteJsonText(document, options),
            Throws.TypeOf<GltfWriteException>()
                .With.Message.EqualTo(
                    "Numeric value /nodes/0/translation/1 must be finite."));
    }

    [Test]
    public void UnsupportedFeatureException_IsRaisedByPublicWriterFailure()
    {
        GltfDocument document = new GltfDocument();
        document.Resources.Resources.Add(new GltfResource());
        GltfWriteOptions options = new GltfWriteOptions
        {
            ResourceMode = GltfResourceMode.GlbBin,
            Validate = false
        };

        Assert.That(
            () => new GltfWriter().WriteBytes(document, options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.EqualTo(
                    "Resource layout mode GlbBin is not implemented."));
    }

    [Test]
    public void ParseException_PreservesDiagnosticMessage()
    {
        string message = "GLB chunk at byte 24 exceeds the declared container length.";

        Assert.That(
            () => throw new GltfParseException(message),
            Throws.TypeOf<GltfParseException>().With.Message.EqualTo(message));
    }

    [Test]
    public void ResourceException_PreservesDiagnosticMessage()
    {
        string message = "Required image resource 'texture.png' could not be resolved.";

        Assert.That(
            () => throw new GltfResourceException(message),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [Test]
    public void ValidationException_PreservesDiagnosticMessage()
    {
        string message = "Validation failed with 3 error-severity issues.";

        Assert.That(
            () => throw new GltfValidationException(message),
            Throws.TypeOf<GltfValidationException>().With.Message.EqualTo(message));
    }

    [Test]
    public void WriteException_PreservesDiagnosticMessage()
    {
        string message = "The selected resource layout cannot be represented as GLB.";

        Assert.That(
            () => throw new GltfWriteException(message),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(message));
    }

    [Test]
    public void UnsupportedFeatureException_PreservesDiagnosticMessage()
    {
        string message = "Required codec 'KHR_draco_mesh_compression' is unavailable.";

        Assert.That(
            () => throw new GltfUnsupportedFeatureException(message),
            Throws.TypeOf<GltfUnsupportedFeatureException>().With.Message.EqualTo(message));
    }
}
