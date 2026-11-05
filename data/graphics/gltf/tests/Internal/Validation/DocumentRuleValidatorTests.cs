using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class DocumentRuleValidatorTests
{
    [Test]
    public void ValidateDocumentRules_AcceptsAnEmptyCoreDocument()
    {
        GltfDocument document = new GltfDocument();

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateDocumentRules_ReportsAssetCompatibilityAtExactPointers()
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Version = "3.0";
        document.Asset.MinimumVersion = "3.1";

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.UnknownAssetMajorVersion,
                GltfValidationSeverity.Error,
                "/asset/version");
            AssertIssue(
                report,
                GltfIssueCodes.UnknownAssetMajorVersion,
                GltfValidationSeverity.Error,
                "/asset/minVersion");
            AssertIssue(
                report,
                GltfIssueCodes.AssetMinVersionGreaterThanVersion,
                GltfValidationSeverity.Error,
                "/asset/minVersion");
        });

        document.Asset.Version = "2.1";
        document.Asset.MinimumVersion = null;
        report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            AssertIssue(
                report,
                GltfIssueCodes.UnknownAssetMinorVersion,
                GltfValidationSeverity.Warning,
                "/asset/version");
        });
    }

    [Test]
    public void ValidateDocumentRules_ReportsNullRootAndNestedObjects()
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Version = null!;
        document.Buffers.Add(null!);
        GltfAnimation animation = new GltfAnimation();
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Target = null!;
        animation.Channels.Add(channel);
        animation.Samplers.Add(null!);
        document.Animations.Add(animation);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(null!);
        document.Meshes.Add(mesh);
        GltfSkin skin = new GltfSkin();
        skin.Joints = null!;
        document.Skins.Add(skin);

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            AssertIssue(report, GltfIssueCodes.UndefinedProperty, "/asset/version");
            AssertIssue(report, GltfIssueCodes.TypeMismatch, "/buffers/0");
            AssertIssue(report, GltfIssueCodes.TypeMismatch, "/animations/0/channels/0/target");
            AssertIssue(report, GltfIssueCodes.TypeMismatch, "/animations/0/samplers/0");
            AssertIssue(report, GltfIssueCodes.TypeMismatch, "/meshes/0/primitives/0");
            AssertIssue(report, GltfIssueCodes.ArrayTypeMismatch, "/skins/0/joints");
        });
    }

    [Test]
    public void ValidateDocumentRules_ReportsEveryCoreReferenceFamily()
    {
        GltfDocument document = CreateInvalidReferenceDocument();

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        string[] expectedPointers =
        [
            "/scene",
            "/accessors/0/bufferView",
            "/accessors/0/sparse/indices/bufferView",
            "/accessors/0/sparse/values/bufferView",
            "/animations/0/samplers/0/input",
            "/animations/0/samplers/0/output",
            "/animations/0/channels/0/sampler",
            "/animations/0/channels/0/target/node",
            "/bufferViews/0/buffer",
            "/images/0/bufferView",
            "/materials/0/pbrMetallicRoughness/baseColorTexture/index",
            "/materials/0/pbrMetallicRoughness/metallicRoughnessTexture/index",
            "/materials/0/normalTexture/index",
            "/materials/0/occlusionTexture/index",
            "/materials/0/emissiveTexture/index",
            "/meshes/0/primitives/0/indices",
            "/meshes/0/primitives/0/material",
            "/meshes/0/primitives/0/attributes/POSITION",
            "/meshes/0/primitives/0/targets/0/POSITION",
            "/nodes/0/camera",
            "/nodes/0/skin",
            "/nodes/0/mesh",
            "/skins/0/inverseBindMatrices",
            "/skins/0/skeleton",
            "/skins/0/joints/0",
            "/textures/0/sampler",
            "/textures/0/source",
            "/nodes/0/children/0",
            "/scenes/0/nodes/0"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            foreach (string pointer in expectedPointers)
            {
                AssertIssue(report, GltfIssueCodes.UnresolvedReference, pointer);
            }
        });
    }

    [Test]
    public void ValidateDocumentRules_ReportsCyclesMultipleParentsAndNonRootSceneNodes()
    {
        GltfDocument document = new GltfDocument();
        GltfNode first = new GltfNode();
        GltfNode second = new GltfNode();
        GltfNode third = new GltfNode();
        first.Children.Add(1);
        second.Children.Add(0);
        third.Children.Add(1);
        document.Nodes.Add(first);
        document.Nodes.Add(second);
        document.Nodes.Add(third);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(1);
        document.Scenes.Add(scene);

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.NodeLoop, "/nodes/1/children/0");
            AssertIssue(report, GltfIssueCodes.NodeParentOverride, "/nodes/2/children/0");
            AssertIssue(report, GltfIssueCodes.SceneNonRootNode, "/scenes/0/nodes/0");
        });
    }

    [Test]
    public void ValidateDocumentRules_ReconcilesExtensionDeclarationsAndNames()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureTransform);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureTransform);
        document.ExtensionsUsed.Add("bad-name");
        document.ExtensionsUsed.Add("ACME_unknown");
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrTextureTransform);
        document.ExtensionsRequired.Add("ACME_unknown");
        document.ExtensionsRequired.Add("VENDOR_missing");
        document.PropertyJson.Extensions.Add("EXT/undeclared", new JsonObject());

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.DuplicateElements,
                "/extensionsUsed/1");
            AssertIssue(
                report,
                GltfIssueCodes.InvalidExtensionNameFormat,
                GltfValidationSeverity.Warning,
                "/extensionsUsed/2");
            AssertIssue(
                report,
                GltfIssueCodes.UnsupportedExtension,
                GltfValidationSeverity.Warning,
                "/extensionsRequired/1");
            AssertIssue(
                report,
                GltfIssueCodes.UnusedExtensionRequired,
                "/extensionsRequired/2");
            AssertIssue(
                report,
                GltfIssueCodes.UnsupportedExtension,
                GltfValidationSeverity.Warning,
                "/extensionsRequired/2");
            AssertIssue(
                report,
                GltfIssueCodes.UndeclaredExtension,
                "/extensions/EXT~1undeclared");
            AssertIssue(
                report,
                GltfIssueCodes.InvalidExtensionNameFormat,
                GltfValidationSeverity.Warning,
                "/extensions/EXT~1undeclared");
        });
    }

    [Test]
    public void ValidateDocumentRules_DoesNotMutateExtensionOrHierarchyState()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrLightsPunctual);
        JsonObject payload = new JsonObject { ["lights"] = new JsonArray() };
        document.PropertyJson.Extensions.Add(GltfExtensionNames.KhrLightsPunctual, payload);
        GltfNode node = new GltfNode();
        document.Nodes.Add(node);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);

        GltfValidationReport report = new DocumentRuleValidator().ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
            Assert.That(document.ExtensionsUsed, Is.EqualTo(
                new[] { GltfExtensionNames.KhrLightsPunctual }));
            Assert.That(document.ExtensionsRequired, Is.EqualTo(
                new[] { GltfExtensionNames.KhrLightsPunctual }));
            Assert.That(document.PropertyJson.Extensions[GltfExtensionNames.KhrLightsPunctual],
                Is.SameAs(payload));
            Assert.That(node.Children, Is.Empty);
            Assert.That(scene.Nodes, Is.EqualTo(new[] { 0 }));
        });
    }

    [Test]
    public void ValidateDocumentRules_RejectsNullInput()
    {
        Assert.That(
            () => new DocumentRuleValidator().ValidateDocumentRules(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
    }

    private static GltfDocument CreateInvalidReferenceDocument()
    {
        GltfDocument document = new GltfDocument();
        document.HasDefaultScene = true;
        document.DefaultScene = 4;

        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 4;
        accessor.Sparse = new GltfAccessorSparse();
        accessor.Sparse.Indices.BufferView = 4;
        accessor.Sparse.Values.BufferView = 4;
        document.Accessors.Add(accessor);

        GltfAnimation animation = new GltfAnimation();
        GltfAnimationSampler animationSampler = new GltfAnimationSampler();
        animationSampler.Input = 4;
        animationSampler.Output = 4;
        animation.Samplers.Add(animationSampler);
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Sampler = 4;
        channel.Target.HasNode = true;
        channel.Target.Node = 4;
        animation.Channels.Add(channel);
        document.Animations.Add(animation);

        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 4;
        document.BufferViews.Add(bufferView);

        GltfImage image = new GltfImage();
        image.HasBufferView = true;
        image.BufferView = 4;
        document.Images.Add(image);

        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        material.PbrMetallicRoughness.BaseColorTexture = new GltfTextureInfo();
        material.PbrMetallicRoughness.BaseColorTexture.Index = 4;
        material.PbrMetallicRoughness.MetallicRoughnessTexture = new GltfTextureInfo();
        material.PbrMetallicRoughness.MetallicRoughnessTexture.Index = 4;
        material.NormalTexture = new GltfNormalTextureInfo();
        material.NormalTexture.Index = 4;
        material.OcclusionTexture = new GltfOcclusionTextureInfo();
        material.OcclusionTexture.Index = 4;
        material.EmissiveTexture = new GltfTextureInfo();
        material.EmissiveTexture.Index = 4;
        document.Materials.Add(material);

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasIndices = true;
        primitive.Indices = 4;
        primitive.HasMaterial = true;
        primitive.Material = 4;
        primitive.Attributes.Add(CreateAttribute("POSITION", 4));
        GltfMorphTarget target = new GltfMorphTarget();
        target.Attributes.Add(CreateAttribute("POSITION", 4));
        primitive.Targets.Add(target);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfNode node = new GltfNode();
        node.HasCamera = true;
        node.Camera = 4;
        node.HasSkin = true;
        node.Skin = 4;
        node.HasMesh = true;
        node.Mesh = 4;
        node.Children.Add(4);
        document.Nodes.Add(node);

        GltfScene scene = new GltfScene();
        scene.Nodes.Add(4);
        document.Scenes.Add(scene);

        GltfSkin skin = new GltfSkin();
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = 4;
        skin.HasSkeleton = true;
        skin.Skeleton = 4;
        skin.Joints.Add(4);
        document.Skins.Add(skin);

        GltfTexture texture = new GltfTexture();
        texture.HasSampler = true;
        texture.Sampler = 4;
        texture.HasSource = true;
        texture.Source = 4;
        document.Textures.Add(texture);
        return document;
    }

    private static GltfAttribute CreateAttribute(string semantic, int accessor)
    {
        GltfAttribute result = new GltfAttribute();
        result.Semantic = semantic;
        result.Accessor = accessor;
        return result;
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {code} at '{pointer}', got: "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Code}@{issue.JsonPointer}")));
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code
                && issue.Severity == severity
                && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {severity} {code} at '{pointer}', got: "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}")));
    }
}
