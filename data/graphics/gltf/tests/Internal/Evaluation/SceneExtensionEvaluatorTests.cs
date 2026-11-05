namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class SceneExtensionEvaluatorTests
{
    [Test]
    public void EvaluateDocumentScene_ProducesInheritedCapabilityAndAttachmentMetadata()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor instances = new GltfAccessor();
        instances.Count = 3;
        document.Accessors.Add(instances);
        GltfNode parent = new GltfNode();
        parent.Children.Add(1);
        AddVisibility(parent, false);
        AddSelectability(parent, false);
        GltfNode child = new GltfNode();
        AddVisibility(child, true);
        AddHoverability(child, false);
        AddSelectability(child, true);
        AddLight(child, 2);
        AddInstancing(child, 0);
        document.Nodes.Add(parent);
        document.Nodes.Add(child);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        GltfSceneEvaluationOptions options = new GltfSceneEvaluationOptions();
        options.CalculateBounds = false;
        options.CalculateCameras = false;
        options.CalculateSkins = false;

        GltfScenePose pose = new SceneEvaluator().EvaluateDocumentScene(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(pose.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(pose.Nodes[0].Visible, Is.False);
            Assert.That(pose.Nodes[1].Visible, Is.False);
            Assert.That(pose.Nodes[1].Hoverable, Is.False);
            Assert.That(pose.Nodes[1].Selectable, Is.False);
            Assert.That(pose.Nodes[1].HasPunctualLight, Is.True);
            Assert.That(pose.Nodes[1].PunctualLight, Is.EqualTo(2));
            Assert.That(pose.Nodes[1].InstanceCount, Is.EqualTo(3));
            Assert.That(pose.Nodes[1].InstanceAttributes["TRANSLATION"], Is.Zero);
        });
    }

    private static void AddVisibility(GltfNode node, bool value)
    {
        GltfNodeVisibilityExtension extension = new GltfNodeVisibilityExtension();
        extension.HasVisible = true;
        extension.Visible = value;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.NodeVisibility = extension;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrNodeVisibility] = typed;
    }

    private static void AddHoverability(GltfNode node, bool value)
    {
        GltfNodeHoverabilityExtension extension = new GltfNodeHoverabilityExtension();
        extension.HasHoverable = true;
        extension.Hoverable = value;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.NodeHoverability = extension;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrNodeHoverability] = typed;
    }

    private static void AddSelectability(GltfNode node, bool value)
    {
        GltfNodeSelectabilityExtension extension = new GltfNodeSelectabilityExtension();
        extension.HasSelectable = true;
        extension.Selectable = value;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.NodeSelectability = extension;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrNodeSelectability] = typed;
    }

    private static void AddLight(GltfNode node, int light)
    {
        GltfPunctualLightNodeExtension extension = new GltfPunctualLightNodeExtension();
        extension.Light = light;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.PunctualLightNode = extension;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual] = typed;
    }

    private static void AddInstancing(GltfNode node, int accessor)
    {
        GltfMeshGpuInstancingExtension extension = new GltfMeshGpuInstancingExtension();
        extension.Attributes["TRANSLATION"] = accessor;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MeshGpuInstancing = extension;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.ExtMeshGpuInstancing] = typed;
    }
}
