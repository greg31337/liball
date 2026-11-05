using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfExtensionHandlerCoverageTests
{
    private static readonly string[] TypedExtensionNames =
    [
        GltfExtensionNames.ExtMeshGpuInstancing,
        GltfExtensionNames.ExtMeshoptCompression,
        GltfExtensionNames.ExtTextureWebp,
        GltfExtensionNames.KhrAnimationPointer,
        GltfExtensionNames.KhrDracoMeshCompression,
        GltfExtensionNames.KhrGaussianSplatting,
        GltfExtensionNames.KhrInteractivity,
        GltfExtensionNames.KhrLightsPunctual,
        GltfExtensionNames.KhrMaterialsAnisotropy,
        GltfExtensionNames.KhrMaterialsClearcoat,
        GltfExtensionNames.KhrMaterialsDiffuseTransmission,
        GltfExtensionNames.KhrMaterialsDispersion,
        GltfExtensionNames.KhrMaterialsEmissiveStrength,
        GltfExtensionNames.KhrMaterialsIor,
        GltfExtensionNames.KhrMaterialsIridescence,
        GltfExtensionNames.KhrMaterialsSheen,
        GltfExtensionNames.KhrMaterialsSpecular,
        GltfExtensionNames.KhrMaterialsTransmission,
        GltfExtensionNames.KhrMaterialsUnlit,
        GltfExtensionNames.KhrMaterialsVariants,
        GltfExtensionNames.KhrMaterialsVolume,
        GltfExtensionNames.KhrMeshQuantization,
        GltfExtensionNames.KhrMeshoptCompression,
        GltfExtensionNames.KhrNodeHoverability,
        GltfExtensionNames.KhrNodeSelectability,
        GltfExtensionNames.KhrNodeVisibility,
        GltfExtensionNames.KhrTextureBasisu,
        GltfExtensionNames.KhrTextureTransform,
        GltfExtensionNames.KhrXmpJsonLd
    ];

    private static readonly string[] PinnedSnapshotExtensions =
    [
        GltfExtensionNames.KhrGaussianSplatting,
        GltfExtensionNames.KhrInteractivity,
        GltfExtensionNames.KhrMaterialsDiffuseTransmission
    ];

    [Test]
    public void DescribeExtension_DescribesTheBuiltInFallbackHandler()
    {
        GltfExtensionDescriptor descriptor = new GltfExtensionHandler().DescribeExtension();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Name, Is.EqualTo("*"));
            Assert.That(descriptor.Version, Is.EqualTo(GltfConstants.CoreVersion));
            Assert.That(descriptor.ObjectLocations, Is.EqualTo(new[] { "glTFProperty" }));
            Assert.That(descriptor.AllowsFallback, Is.True);
        });
    }

    [Test]
    public void RegistryInventory_CoversEveryTypedRatifiedAndReleaseCandidateExtension()
    {
        List<GltfExtensionDescriptor> descriptors = new ExtensionRegistry()
            .ListDescriptors()
            .Where(descriptor => descriptor.HasTypedModel
                && (descriptor.Status == GltfExtensionStatus.Ratified
                    || descriptor.Status == GltfExtensionStatus.ReleaseCandidate))
            .ToList();
        string[] expectedNames = TypedExtensionNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                descriptors.Select(descriptor => descriptor.Name),
                Is.EqualTo(expectedNames));
            Assert.That(descriptors, Has.Count.EqualTo(29));
            Assert.That(descriptors.All(descriptor => descriptor.HasSemanticValidation), Is.True);
            Assert.That(
                descriptors.Where(descriptor => descriptor.Name !=
                    GltfExtensionNames.KhrMeshQuantization)
                    .All(descriptor => descriptor.SchemaResources.Count > 0),
                Is.True);
            Assert.That(
                descriptors.Where(descriptor => descriptor.Name !=
                    GltfExtensionNames.KhrMeshQuantization)
                    .All(descriptor => descriptor.CanRemapReferences),
                Is.True);
            Assert.That(
                descriptors.Single(descriptor => descriptor.Name ==
                    GltfExtensionNames.KhrMeshQuantization).CanRemapReferences,
                Is.False);
        });
    }

    [Test]
    public void RegistryInventory_PinsReleaseCandidateSnapshotStatusesAndVersions()
    {
        List<GltfExtensionDescriptor> descriptors = new ExtensionRegistry()
            .ListDescriptors();
        List<GltfExtensionDescriptor> candidates = descriptors
            .Where(descriptor => descriptor.Status == GltfExtensionStatus.ReleaseCandidate)
            .ToList();
        string[] expectedCandidates =
        [
            GltfExtensionNames.KhrGaussianSplatting,
            GltfExtensionNames.KhrInteractivity,
            GltfExtensionNames.KhrMaterialsDiffuseTransmission,
            GltfExtensionNames.KhrMeshoptCompression,
            GltfExtensionNames.KhrNodeHoverability,
            GltfExtensionNames.KhrNodeSelectability
        ];

        Assert.Multiple(() =>
        {
            Assert.That(
                candidates.Select(descriptor => descriptor.Name),
                Is.EqualTo(expectedCandidates));
            Assert.That(
                candidates.Where(descriptor => PinnedSnapshotExtensions.Contains(
                    descriptor.Name)).Select(descriptor => descriptor.Version).Distinct(),
                Is.EqualTo(new[]
                {
                    "2.0@fdb8ce0e2e0b7ecf3466f8dacb9f1385257b8276"
                }));
            Assert.That(
                candidates.Where(descriptor => !PinnedSnapshotExtensions.Contains(
                    descriptor.Name)).All(descriptor => descriptor.Version == "2.0"),
                Is.True);
        });
    }

    [TestCaseSource(nameof(TypedPayloadCases))]
    public void TypedPayloads_ExerciseEveryHandlerOperationAndRoundTripStably(
        string name,
        string location,
        string json)
    {
        GltfDocument document = CreateContextDocument(name);
        GltfExtensionContext context = CreateContext(document, name, location, json);
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfExtensionResult read = handler.ReadExtension(context);
        context.TypedData = read.TypedData;
        GltfExtensionResult written = handler.WriteExtension(context);
        GltfExtensionResult validation = handler.ValidateExtension(context);
        GltfIndexRemap remap = CreateRemap();
        GltfExtensionResult remapped = handler.RemapExtensionReferences(context, remap);
        GltfExtensionContext rereadContext = CreateContext(
            document,
            name,
            location,
            written.Json!.ToJsonString());
        GltfExtensionResult reread = handler.ReadExtension(rereadContext);
        rereadContext.TypedData = reread.TypedData;
        GltfExtensionResult rewritten = handler.WriteExtension(rereadContext);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success), Format(read));
            Assert.That(read.Descriptor!.Name, Is.EqualTo(name));
            Assert.That(read.Descriptor.Status, Is.AnyOf(
                GltfExtensionStatus.Ratified,
                GltfExtensionStatus.ReleaseCandidate));
            Assert.That(read.TypedData, Is.Not.Null);
            Assert.That(written.Status, Is.EqualTo(GltfOperationStatus.Success), Format(written));
            Assert.That(written.Json, Is.Not.Null);
            Assert.That(written.Json!["future"]!.GetValue<bool>(), Is.True);
            Assert.That(validation.Status, Is.Not.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(validation.Descriptor!.HasSemanticValidation, Is.True);
            Assert.That(validation.TypedData, Is.Not.Null);
            Assert.That(remapped.Status, Is.EqualTo(GltfOperationStatus.Success), Format(remapped));
            Assert.That(remapped.TypedData, Is.Not.Null);
            Assert.That(remapped.Json, Is.Not.Null);
            Assert.That(remapped.Json!["future"]!.GetValue<bool>(), Is.True);
            Assert.That(reread.TypedData, Is.Not.Null);
            Assert.That(JsonNode.DeepEquals(written.Json, rewritten.Json), Is.True);
        });
    }

    [Test]
    public void MeshQuantizationMarker_IsValidatedThroughUsedAndRequiredDeclarations()
    {
        GltfDocument missingRequired = CreateQuantizedDocument();
        GltfDocument declared = CreateQuantizedDocument();
        declared.ExtensionsUsed.Add(GltfExtensionNames.KhrMeshQuantization);
        declared.ExtensionsRequired.Add(GltfExtensionNames.KhrMeshQuantization);

        GltfValidationReport missingReport = Validate(missingRequired);
        GltfValidationReport declaredReport = Validate(declared);

        Assert.Multiple(() =>
        {
            Assert.That(
                missingReport.Issues.Any(issue => issue.Code ==
                    GltfIssueCodes.UndeclaredExtension),
                Is.True,
                Format(missingReport));
            Assert.That(
                missingReport.Issues.Any(issue => issue.Code ==
                    GltfIssueCodes.UnsatisfiedDependency),
                Is.True,
                Format(missingReport));
            Assert.That(declaredReport.IsValid, Is.True, Format(declaredReport));
            Assert.That(
                declaredReport.Issues.Any(issue => issue.Code ==
                    GltfIssueCodes.UndeclaredExtension),
                Is.False,
                Format(declaredReport));
            Assert.That(
                declaredReport.Issues.Any(issue => issue.Code ==
                    GltfIssueCodes.UnsatisfiedDependency),
                Is.False,
                Format(declaredReport));
        });
    }

    private static IEnumerable<TestCaseData> TypedPayloadCases()
    {
        yield return Case(
            GltfExtensionNames.ExtMeshGpuInstancing,
            "node",
            "{\"attributes\":{\"TRANSLATION\":0}}");
        yield return Case(
            GltfExtensionNames.ExtMeshoptCompression,
            "bufferView",
            MeshoptJson());
        yield return Case(GltfExtensionNames.ExtTextureWebp, "texture", "{\"source\":0}");
        yield return Case(
            GltfExtensionNames.KhrAnimationPointer,
            "animation.channel.target",
            "{\"pointer\":\"/nodes/0/translation\"}");
        yield return Case(
            GltfExtensionNames.KhrDracoMeshCompression,
            "mesh.primitive",
            "{\"bufferView\":0,\"attributes\":{\"POSITION\":0}}");
        yield return Case(
            GltfExtensionNames.KhrGaussianSplatting,
            "mesh.primitive",
            "{\"kernel\":\"ellipse\",\"colorSpace\":\"srgb_rec709_display\"}");
        yield return Case(
            GltfExtensionNames.KhrInteractivity,
            "glTF",
            "{\"graphs\":[{}],\"graph\":0}");
        yield return Case(
            GltfExtensionNames.KhrLightsPunctual,
            "glTF",
            "{\"lights\":[{\"type\":\"point\"}]}");
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsAnisotropy);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsClearcoat);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsDiffuseTransmission);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsDispersion);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsEmissiveStrength);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsIor);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsIridescence);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsSheen);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsSpecular);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsTransmission);
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsUnlit);
        yield return Case(
            GltfExtensionNames.KhrMaterialsVariants,
            "glTF",
            "{\"variants\":[{\"name\":\"Default\"}]}");
        yield return MaterialCase(GltfExtensionNames.KhrMaterialsVolume);
        yield return Case(
            GltfExtensionNames.KhrMeshoptCompression,
            "bufferView",
            MeshoptJson());
        yield return Case(
            GltfExtensionNames.KhrNodeHoverability,
            "node",
            "{\"hoverable\":false}");
        yield return Case(
            GltfExtensionNames.KhrNodeSelectability,
            "node",
            "{\"selectable\":false}");
        yield return Case(
            GltfExtensionNames.KhrNodeVisibility,
            "node",
            "{\"visible\":false}");
        yield return Case(GltfExtensionNames.KhrTextureBasisu, "texture", "{\"source\":0}");
        yield return Case(
            GltfExtensionNames.KhrTextureTransform,
            "textureInfo",
            "{\"offset\":[0.25,0.5],\"rotation\":0.5,\"scale\":[2,3],\"texCoord\":0}");
        yield return Case(
            GltfExtensionNames.KhrXmpJsonLd,
            "glTF",
            "{\"packets\":[{\"@context\":{\"dc\":\"https://example.invalid/dc/\"},\"dc:title\":\"Example\"}]}");
    }

    private static TestCaseData Case(string name, string location, string json)
    {
        return new TestCaseData(name, location, json).SetName(
            $"TypedPayloads_{name}_{location.Replace('.', '_')}");
    }

    private static TestCaseData MaterialCase(string name)
    {
        return Case(name, "material", "{}");
    }

    private static string MeshoptJson()
    {
        return "{\"buffer\":0,\"byteOffset\":0,\"byteLength\":4,"
            + "\"byteStride\":4,\"count\":1,\"mode\":\"ATTRIBUTES\","
            + "\"filter\":\"NONE\"}";
    }

    private static GltfExtensionContext CreateContext(
        GltfDocument document,
        string name,
        string location,
        string json)
    {
        JsonObject payload = JsonNode.Parse(json)!.AsObject();
        payload["future"] = true;
        GltfExtensionContext result = new GltfExtensionContext();
        result.Document = document;
        result.Name = name;
        result.ObjectLocation = location;
        result.HasObjectIndex = location != "glTF" && location != "textureInfo";
        result.ObjectIndex = 0;
        result.HasSubObjectIndex = location == "mesh.primitive";
        result.SubObjectIndex = 0;
        result.Json = payload;
        GltfResource resource = new GltfResource();
        resource.SourceUri = "extension-resource.bin";
        resource.Bytes = new byte[] { 1, 2, 3 };
        result.Resources.Resources.Add(resource);
        return result;
    }

    private static GltfDocument CreateContextDocument(string extension)
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(extension);
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 12;
        buffer.Resolved = true;
        buffer.ResolvedBytes = new byte[12];
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = 12;
        document.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Vector3;
        accessor.Minimum = new double[] { 0.0, 0.0, 0.0 };
        accessor.Maximum = new double[] { 0.0, 0.0, 0.0 };
        document.Accessors.Add(accessor);
        GltfImage image = new GltfImage();
        image.MimeType = extension == GltfExtensionNames.ExtTextureWebp
            ? GltfMimeTypes.WebP
            : GltfMimeTypes.Ktx2;
        document.Images.Add(image);
        GltfTexture texture = new GltfTexture();
        texture.HasSource = true;
        texture.Source = 0;
        document.Textures.Add(texture);
        document.Materials.Add(new GltfMaterial());
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfAttribute position = new GltfAttribute();
        position.Semantic = "POSITION";
        position.Accessor = 0;
        primitive.Attributes.Add(position);
        primitive.HasMode = true;
        primitive.Mode = GltfPrimitiveMode.Points;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfNode node = new GltfNode();
        node.HasMesh = true;
        node.Mesh = 0;
        node.HasTranslation = true;
        document.Nodes.Add(node);
        return document;
    }

    private static GltfIndexRemap CreateRemap()
    {
        GltfIndexRemap result = new GltfIndexRemap();
        result.Accessors[0] = 1;
        result.Buffers[0] = 1;
        result.BufferViews[0] = 1;
        result.Images[0] = 1;
        result.Materials[0] = 1;
        result.Nodes[0] = 1;
        result.PunctualLights[0] = 1;
        result.Textures[0] = 1;
        result.XmpJsonLdPackets[0] = 1;
        return result;
    }

    private static GltfDocument CreateQuantizedDocument()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.SignedShort;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Vector3;
        accessor.Minimum = new double[] { 0.0, 0.0, 0.0 };
        accessor.Maximum = new double[] { 0.0, 0.0, 0.0 };
        document.Accessors.Add(accessor);
        GltfAttribute position = new GltfAttribute();
        position.Semantic = "POSITION";
        position.Accessor = 0;
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasMode = true;
        primitive.Mode = GltfPrimitiveMode.Points;
        primitive.Attributes.Add(position);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        return document;
    }

    private static GltfValidationReport Validate(GltfDocument document)
    {
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateStructure = false;
        options.ValidateResources = false;
        options.ValidateExtensions = true;
        return new GltfValidator().ValidateDocument(document, options);
    }

    private static string Format(GltfExtensionResult result)
    {
        return string.Join(
            ", ",
            result.Diagnostics.Select(issue =>
                $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }

    private static string Format(GltfValidationReport report)
    {
        return string.Join(
            ", ",
            report.Issues.Select(issue =>
                $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }
}
