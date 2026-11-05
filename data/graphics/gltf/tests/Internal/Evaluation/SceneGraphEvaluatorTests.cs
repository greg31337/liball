namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class SceneGraphEvaluatorTests
{
    [Test]
    public void AnalyzeDocument_BuildsParentMapsAndDeclaredOrderPreorderTraversal()
    {
        GltfDocument document = CreateDocumentWithNodes(4);
        document.Nodes[0].Children.Add(2);
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].Children.Add(3);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);

        SceneGraphAnalysis result = new SceneGraphEvaluator().AnalyzeDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasCycles, Is.False);
            Assert.That(
                result.Parents,
                Is.EqualTo(new int[] { -1, 0, 0, 1 }));
            Assert.That(result.ParentReferences[0], Is.Empty);
            Assert.That(result.ParentReferences[1], Is.EqualTo(new int[] { 0 }));
            Assert.That(result.ParentReferences[2], Is.EqualTo(new int[] { 0 }));
            Assert.That(result.ParentReferences[3], Is.EqualTo(new int[] { 1 }));
            Assert.That(result.SceneTraversals, Has.Count.EqualTo(1));
            Assert.That(result.SceneTraversals[0].Roots, Is.EqualTo(new int[] { 0 }));
            Assert.That(result.SceneTraversals[0].Nodes, Is.EqualTo(new int[] { 0, 2, 1, 3 }));
            Assert.That(result.TraversalOrder, Is.EqualTo(new int[] { 0, 2, 1, 3 }));
            Assert.That(result.OrphanNodes, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AnalyzeDocument_TracksSharedRootsAndTransitiveMembershipAcrossScenes()
    {
        GltfDocument document = CreateDocumentWithNodes(4);
        document.Nodes[0].Children.Add(1);
        document.Nodes[2].Children.Add(3);
        GltfScene first = new GltfScene();
        first.Nodes.Add(0);
        first.Nodes.Add(2);
        GltfScene second = new GltfScene();
        second.Nodes.Add(2);
        GltfScene third = new GltfScene();
        third.Nodes.Add(0);
        document.Scenes.Add(first);
        document.Scenes.Add(second);
        document.Scenes.Add(third);

        SceneGraphAnalysis result = new SceneGraphEvaluator().AnalyzeDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.RootSceneReferences[0], Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(result.RootSceneReferences[1], Is.Empty);
            Assert.That(result.RootSceneReferences[2], Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.NodeScenes[0], Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(result.NodeScenes[1], Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(result.NodeScenes[2], Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.NodeScenes[3], Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.SceneTraversals[0].Nodes, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(result.SceneTraversals[1].Nodes, Is.EqualTo(new int[] { 2, 3 }));
            Assert.That(result.SceneTraversals[2].Nodes, Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.TraversalOrder, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(result.OrphanNodes, Is.Empty);
        });
    }

    [Test]
    public void AnalyzeDocument_AppendsUnreferencedForestsInNodeIndexOrder()
    {
        GltfDocument document = CreateDocumentWithNodes(5);
        document.Nodes[0].Children.Add(1);
        document.Nodes[3].Children.Add(4);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(3);
        document.Scenes.Add(scene);

        SceneGraphAnalysis result = new SceneGraphEvaluator().AnalyzeDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.SceneTraversals[0].Nodes, Is.EqualTo(new int[] { 3, 4 }));
            Assert.That(result.TraversalOrder, Is.EqualTo(new int[] { 3, 4, 0, 1, 2 }));
            Assert.That(result.OrphanNodes, Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(result.NodeScenes[3], Is.EqualTo(new int[] { 0 }));
            Assert.That(result.NodeScenes[4], Is.EqualTo(new int[] { 0 }));
        });
    }

    [Test]
    public void AnalyzeDocument_DetectsCyclesOutsideScenesWithoutRecursion()
    {
        GltfDocument document = CreateDocumentWithNodes(4);
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].Children.Add(2);
        document.Nodes[2].Children.Add(0);
        document.Nodes[3].Children.Add(3);

        SceneGraphAnalysis result = new SceneGraphEvaluator().AnalyzeDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.HasCycles, Is.True);
            Assert.That(result.CycleNodes, Is.EqualTo(new bool[] { true, true, true, true }));
            Assert.That(result.TraversalOrder, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(result.OrphanNodes, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.NodeLoop));
            Assert.That(result.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/2/children/0"));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(GltfIssueCodes.NodeLoop));
            Assert.That(result.Diagnostics[1].JsonPointer, Is.EqualTo("/nodes/3/children/0"));
        });
    }

    [Test]
    public void AnalyzeDocument_ReportsReferenceParentAndSceneRootErrorsDeterministically()
    {
        GltfDocument document = CreateDocumentWithNodes(3);
        document.Nodes[0].Children.Add(2);
        document.Nodes[0].Children.Add(2);
        document.Nodes[0].Children.Add(99);
        document.Nodes[1].Children.Add(2);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(2);
        scene.Nodes.Add(2);
        scene.Nodes.Add(-1);
        document.Scenes.Add(scene);

        SceneGraphAnalysis result = new SceneGraphEvaluator().AnalyzeDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Parents, Is.EqualTo(new int[] { -1, -1, 0 }));
            Assert.That(result.ParentReferences[2], Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.InvalidNodes, Is.EqualTo(new bool[] { true, false, true }));
            Assert.That(result.RootSceneReferences[2], Is.EqualTo(new int[] { 0 }));
            Assert.That(result.SceneTraversals[0].IsValid, Is.False);
            Assert.That(result.SceneTraversals[0].Roots, Is.EqualTo(new int[] { 2 }));
            Assert.That(result.SceneTraversals[0].Nodes, Is.EqualTo(new int[] { 2 }));
            Assert.That(
                result.Diagnostics.ConvertAll(issue => issue.Code),
                Is.EqualTo(new string[]
                {
                    GltfIssueCodes.DuplicateElements,
                    GltfIssueCodes.UnresolvedReference,
                    GltfIssueCodes.NodeParentOverride,
                    GltfIssueCodes.SceneNonRootNode,
                    GltfIssueCodes.DuplicateElements,
                    GltfIssueCodes.UnresolvedReference
                }));
            Assert.That(result.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/0/children/1"));
            Assert.That(result.Diagnostics[1].JsonPointer, Is.EqualTo("/nodes/0/children/2"));
            Assert.That(result.Diagnostics[2].JsonPointer, Is.EqualTo("/nodes/1/children/0"));
            Assert.That(result.Diagnostics[3].JsonPointer, Is.EqualTo("/scenes/0/nodes/0"));
            Assert.That(result.Diagnostics[4].JsonPointer, Is.EqualTo("/scenes/0/nodes/1"));
            Assert.That(result.Diagnostics[5].JsonPointer, Is.EqualTo("/scenes/0/nodes/2"));
        });
    }

    [Test]
    public void AnalyzeDocument_HandlesNullCollectionsAndElementsAsInvalidInput()
    {
        SceneGraphEvaluator evaluator = new SceneGraphEvaluator();
        GltfDocument missingNodes = new GltfDocument();
        missingNodes.Nodes = null!;
        GltfDocument missingScenes = CreateDocumentWithNodes(1);
        missingScenes.Scenes = null!;
        GltfDocument nullElements = CreateDocumentWithNodes(1);
        nullElements.Nodes[0] = null!;
        nullElements.Scenes.Add(null!);

        SceneGraphAnalysis missingNodeResult = evaluator.AnalyzeDocument(missingNodes);
        SceneGraphAnalysis missingSceneResult = evaluator.AnalyzeDocument(missingScenes);
        SceneGraphAnalysis nullElementResult = evaluator.AnalyzeDocument(nullElements);

        Assert.Multiple(() =>
        {
            Assert.That(missingNodeResult.IsValid, Is.False);
            Assert.That(missingNodeResult.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes"));
            Assert.That(missingSceneResult.IsValid, Is.False);
            Assert.That(missingSceneResult.TraversalOrder, Is.EqualTo(new int[] { 0 }));
            Assert.That(missingSceneResult.Diagnostics[0].JsonPointer, Is.EqualTo("/scenes"));
            Assert.That(nullElementResult.IsValid, Is.False);
            Assert.That(nullElementResult.TraversalOrder, Is.EqualTo(new int[] { 0 }));
            Assert.That(nullElementResult.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(nullElementResult.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/0"));
            Assert.That(nullElementResult.Diagnostics[1].JsonPointer, Is.EqualTo("/scenes/0"));
        });
    }

    [Test]
    public void AnalyzeDocument_ThrowsForNullDocument()
    {
        Assert.That(
            () => new SceneGraphEvaluator().AnalyzeDocument(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
    }

    private static GltfDocument CreateDocumentWithNodes(int nodeCount)
    {
        GltfDocument result = new GltfDocument();
        int nodeIndex = 0;
        while (nodeIndex < nodeCount)
        {
            result.Nodes.Add(new GltfNode());
            nodeIndex++;
        }

        return result;
    }
}
