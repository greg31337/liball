namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class SkinValidatorTests
{
    [Test]
    public void ValidateDocumentSkinRules_AcceptsValidSkinAndInfluences()
    {
        GltfDocument document = CreateInfluenceDocument(
            new uint[] { 0U, 0U, 0U, 0U },
            new double[] { 1.0, 0.0, 0.0, 0.0 });

        GltfValidationReport report = new SkinValidator().ValidateDocumentSkinRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateDocumentSkinRules_ForwardsHierarchySkeletonAndSceneDiagnostics()
    {
        GltfDocument document = new GltfDocument();
        GltfNode root = new GltfNode();
        root.Children.Add(1);
        document.Nodes.Add(root);
        document.Nodes.Add(new GltfNode());
        document.Nodes.Add(new GltfNode());
        GltfNode skinned = new GltfNode();
        skinned.HasSkin = true;
        skinned.Skin = 0;
        document.Nodes.Add(skinned);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(3);
        document.Scenes.Add(scene);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        skin.Joints.Add(1);
        skin.HasSkeleton = true;
        skin.Skeleton = 2;
        document.Skins.Add(skin);

        GltfValidationReport report = new SkinValidator().ValidateDocumentSkinRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(report, GltfIssueCodes.SkinSkeletonInvalid), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.NodeSkinNoScene), Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.NodeSkinWithNonSkinnedMesh),
                Is.True);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentSkinRules_DistinguishesInverseBindCountFromFormat()
    {
        GltfDocument document = new GltfDocument();
        GltfNode root = new GltfNode();
        root.Children.Add(1);
        document.Nodes.Add(root);
        document.Nodes.Add(new GltfNode());
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Type = GltfAccessorType.Matrix4;
        accessor.Count = 1;
        document.Accessors.Add(accessor);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        skin.Joints.Add(1);
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = 0;
        document.Skins.Add(skin);

        GltfValidationReport report = new SkinValidator().ValidateDocumentSkinRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(report, GltfIssueCodes.InvalidIbmAccessorCount), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.SkinIbmInvalidFormat), Is.False);
        });
    }

    [Test]
    public void ValidateDocumentSkinRules_ReportsSkinnedNodeRecommendations()
    {
        GltfDocument document = CreateInfluenceDocument(
            new uint[] { 0U, 0U, 0U, 0U },
            new double[] { 1.0, 0.0, 0.0, 0.0 });
        GltfNode parent = new GltfNode();
        parent.HasTranslation = true;
        parent.Translation = new double[] { 1.0, 0.0, 0.0 };
        parent.Children.Add(1);
        document.Nodes.Add(parent);
        document.Scenes[0].Nodes.Remove(1);
        document.Scenes[0].Nodes.Add(2);
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = new double[] { 0.0, 1.0, 0.0 };
        GltfNode missingSkin = new GltfNode();
        missingSkin.HasMesh = true;
        missingSkin.Mesh = 0;
        document.Nodes.Add(missingSkin);
        document.Scenes[0].Nodes.Add(3);

        GltfValidationReport report = new SkinValidator().ValidateDocumentSkinRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(report, GltfIssueCodes.NodeSkinnedMeshLocalTransforms),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.NodeSkinnedMeshNonRoot),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.NodeSkinnedMeshParentTransforms),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.NodeSkinnedMeshWithoutSkin),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentSkinRules_ValidatesDecodedVertexInfluences()
    {
        GltfDocument document = CreateInfluenceDocument(
            new uint[] { 0U, 0U, 3U, 1U },
            new double[] { 0.5, 0.25, 0.25, 0.0 });
        document.Nodes.Add(new GltfNode());
        document.Skins[0].Joints.Add(2);

        GltfValidationReport report = new SkinValidator().ValidateDocumentSkinRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(report, GltfIssueCodes.AccessorJointsIndexDuplicate),
                Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.AccessorJointsIndexOob), Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.AccessorJointsUsedZeroWeight),
                Is.True);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentSkinRules_ReportsUnpairedPrimitiveInfluences()
    {
        GltfDocument document = new GltfDocument();
        document.Nodes.Add(new GltfNode());
        GltfNode skinned = new GltfNode();
        skinned.HasMesh = true;
        skinned.Mesh = 0;
        skinned.HasSkin = true;
        skinned.Skin = 0;
        document.Nodes.Add(skinned);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, "JOINTS_0", 0);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        document.Skins.Add(skin);

        GltfValidationReport report = new SkinValidator().ValidateDocumentSkinRules(document);

        Assert.That(
            HasIssue(report, GltfIssueCodes.NodeSkinWithNonSkinnedMesh),
            Is.True);
    }

    private static GltfDocument CreateInfluenceDocument(
        uint[] joints,
        double[] weights)
    {
        GltfDocument document = new GltfDocument();
        document.Nodes.Add(new GltfNode());
        GltfNode skinned = new GltfNode();
        skinned.HasMesh = true;
        skinned.Mesh = 0;
        skinned.HasSkin = true;
        skinned.Skin = 0;
        document.Nodes.Add(skinned);
        GltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorWriteRequest jointWrite = new GltfAccessorWriteRequest();
        jointWrite.Document = document;
        jointWrite.ComponentType = GltfComponentType.UnsignedByte;
        jointWrite.Type = GltfAccessorType.Vector4;
        jointWrite.UnsignedValues = joints;
        int jointAccessor = codec.CreateDenseAccessor(jointWrite).Accessor;
        GltfAccessorWriteRequest weightWrite = new GltfAccessorWriteRequest();
        weightWrite.Document = document;
        weightWrite.Type = GltfAccessorType.Vector4;
        weightWrite.FloatingValues = weights;
        int weightAccessor = codec.CreateDenseAccessor(weightWrite).Accessor;
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, "JOINTS_0", jointAccessor);
        AddAttribute(primitive, "WEIGHTS_0", weightAccessor);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        document.Skins.Add(skin);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        scene.Nodes.Add(1);
        document.Scenes.Add(scene);
        return document;
    }

    private static void AddAttribute(
        GltfMeshPrimitive primitive,
        string semantic,
        int accessor)
    {
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = accessor;
        primitive.Attributes.Add(attribute);
    }

    private static bool HasIssue(GltfValidationReport report, string code)
    {
        return report.Issues.Any(issue => issue.Code == code);
    }
}
