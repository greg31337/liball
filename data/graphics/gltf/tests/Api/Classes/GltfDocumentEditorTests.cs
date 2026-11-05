using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDocumentEditorTests
{
    [Test]
    public void CreateDocument_ReturnsIndependentEmptyValidDocuments()
    {
        IGltfDocumentEditor editor = new GltfDocumentEditor();

        GltfDocument first = editor.CreateDocument();
        GltfDocument second = editor.CreateDocument();
        GltfValidationReport validation = new GltfValidator().ValidateDocument(
            first,
            new GltfValidationOptions());
        GltfWriteResult written = new GltfWriter().WriteJsonText(
            first,
            new GltfWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Asset, Is.Not.SameAs(second.Asset));
            Assert.That(first.Asset.Version, Is.EqualTo(GltfConstants.CoreVersion));
            Assert.That(first.HasDefaultScene, Is.False);
            Assert.That(first.DefaultScene, Is.EqualTo(GltfConstants.AbsentIndex));
            AssertCoreCollectionsEmpty(first);
            Assert.That(first.Resources.Resources, Is.Empty);
            Assert.That(first.Resources.Cache, Is.Empty);
            Assert.That(first.Container, Is.Null);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(written.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(written.JsonText, Is.EqualTo("{\"asset\":{\"version\":\"2.0\"}}"));
        });

        first.Nodes.Add(new GltfNode());
        first.ExtensionsUsed.Add("EXT_first");

        Assert.Multiple(() =>
        {
            Assert.That(second.Nodes, Is.Empty);
            Assert.That(second.ExtensionsUsed, Is.Empty);
        });
    }

    [Test]
    public void IndexChangingOperations_RejectOptionalUnknownExtensionsUnlessAllowed()
    {
        GltfDocument mergeSource = CreateUnknownExtensionDocument();
        GltfDocument compactSource = CreateUnknownExtensionDocument();
        GltfDocument rebuildSource = CreateUnknownExtensionDocument();
        GltfDocumentEditor editor = new GltfDocumentEditor();
        GltfEditOptions unsafeOptions = new GltfEditOptions
        {
            RequireSafeUnknownExtensionRemap = false,
            ValidateResult = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => editor.MergeDocument([mergeSource], new GltfEditOptions()),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("FUTURE_graph"));
            Assert.That(
                () => editor.CompactDocument(compactSource, new GltfEditOptions()),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("FUTURE_graph"));
            Assert.That(
                () => editor.RebuildReferences(rebuildSource, new GltfEditOptions()),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("FUTURE_graph"));
        });

        GltfDocument merged = editor.MergeDocument(
            [mergeSource],
            unsafeOptions).Document!;
        GltfDocument compacted = editor.CompactDocument(
            compactSource,
            unsafeOptions).Document!;
        GltfDocument rebuilt = editor.RebuildReferences(
            rebuildSource,
            unsafeOptions).Document!;

        Assert.Multiple(() =>
        {
            AssertUnknownExtensionPreserved(merged);
            AssertUnknownExtensionPreserved(compacted);
            AssertUnknownExtensionPreserved(rebuilt);
            AssertUnknownExtensionPreserved(mergeSource);
            AssertUnknownExtensionPreserved(compactSource);
            AssertUnknownExtensionPreserved(rebuildSource);
        });
    }

    [Test]
    public void DocumentOperations_ValidateNullArgumentsAndMergeElements()
    {
        GltfDocumentEditor editor = new GltfDocumentEditor();
        GltfDocument document = new GltfDocument();
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => editor.CloneDocument(null!, options),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => editor.CloneDocument(document, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => editor.MergeDocument(null!, options),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("documents"));
            Assert.That(
                () => editor.MergeDocument([document], null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => editor.MergeDocument([document, null!], options),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("documents"));
            Assert.That(
                () => editor.CompactDocument(null!, options),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => editor.CompactDocument(document, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => editor.RebuildReferences(null!, options),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => editor.RebuildReferences(document, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        });
    }

    [Test]
    public void MergeDocument_AcceptsNoSourcesAsAnEmptyIndependentResult()
    {
        GltfMergeResult result = new GltfDocumentEditor().MergeDocument(
            [],
            new GltfEditOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Document, Is.Not.Null);
            AssertCoreCollectionsEmpty(result.Document!);
            Assert.That(result.SourceRemaps, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static GltfDocument CreateUnknownExtensionDocument()
    {
        GltfDocument result = new GltfDocument();
        result.ExtensionsUsed.Add("FUTURE_graph");
        result.PropertyJson.Extensions["FUTURE_graph"] = new JsonObject
        {
            ["node"] = 0
        };
        result.Nodes.Add(new GltfNode { Name = "source" });
        return result;
    }

    private static void AssertUnknownExtensionPreserved(GltfDocument document)
    {
        Assert.That(document.Nodes, Has.Count.EqualTo(1));
        Assert.That(document.Nodes[0].Name, Is.EqualTo("source"));
        Assert.That(document.ExtensionsUsed, Does.Contain("FUTURE_graph"));
        Assert.That(document.PropertyJson.Extensions["FUTURE_graph"]!["node"]!
            .GetValue<int>(), Is.Zero);
    }

    private static void AssertCoreCollectionsEmpty(GltfDocument document)
    {
        Assert.That(document.Accessors, Is.Empty);
        Assert.That(document.Animations, Is.Empty);
        Assert.That(document.Buffers, Is.Empty);
        Assert.That(document.BufferViews, Is.Empty);
        Assert.That(document.Cameras, Is.Empty);
        Assert.That(document.Images, Is.Empty);
        Assert.That(document.Materials, Is.Empty);
        Assert.That(document.Meshes, Is.Empty);
        Assert.That(document.Nodes, Is.Empty);
        Assert.That(document.Samplers, Is.Empty);
        Assert.That(document.Scenes, Is.Empty);
        Assert.That(document.Skins, Is.Empty);
        Assert.That(document.Textures, Is.Empty);
        Assert.That(document.ExtensionsUsed, Is.Empty);
        Assert.That(document.ExtensionsRequired, Is.Empty);
    }
}
