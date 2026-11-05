using System.IO;
using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class ImplementationShellTests
{
    [Test]
    public void Reader_CompletesStreamBytesAndContainerOperations()
    {
        IGltfReader reader = new GltfReader();
        GltfReadOptions options = new GltfReadOptions();
        byte[] json = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}");
        using MemoryStream stream = new MemoryStream(json);
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        container.JsonBytes = (byte[])json.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadStream(stream, options).Document, Is.Not.Null);
            Assert.That(reader.ReadBytes(json, options).Document, Is.Not.Null);
            Assert.That(reader.ReadContainer(container, options).Document, Is.Not.Null);
        });
    }

    [Test]
    public void Writer_CompletesStreamBytesAndJsonTextOperations()
    {
        IGltfWriter writer = new GltfWriter();
        GltfDocument document = new GltfDocument();
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        using MemoryStream stream = new MemoryStream();

        GltfWriteResult streamResult = writer.WriteStream(document, stream, options);
        GltfWriteResult bytesResult = writer.WriteBytes(document, options);
        GltfWriteResult textResult = writer.WriteJsonText(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(stream.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}")));
            Assert.That(bytesResult.PrimaryBytes, Is.EqualTo(stream.ToArray()));
            Assert.That(textResult.JsonText, Is.EqualTo("{\"asset\":{\"version\":\"2.0\"}}"));
        });
    }

    [Test]
    public void FileResourceResolver_ReturnsInvalidForAnAbsentUri()
    {
        IGltfResourceResolver resolver = new FileGltfResourceResolver();

        GltfResource result = resolver.ResolveResource(new GltfResourceRequest());

        Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
    }

    [Test]
    public void Validator_CompletesEveryOperation()
    {
        IGltfValidator validator = new GltfValidator();
        GltfValidationOptions options = new GltfValidationOptions();
        GltfContainerData container = new GltfContainerData();

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateDocument(new GltfDocument(), options).IsValid, Is.True);
            Assert.That(validator.ValidateFile("model.gltf", options).IsValid, Is.False);
            Assert.That(validator.ValidateBytes([], options).IsValid, Is.False);
            Assert.That(validator.ValidateContainer(container, options).IsValid, Is.False);
        });
    }

    [Test]
    public void AccessorCodec_ReportsInvalidRequestsWithoutDocuments()
    {
        IGltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorReadRequest readRequest = new GltfAccessorReadRequest();
        GltfAccessorWriteRequest writeRequest = new GltfAccessorWriteRequest();

        Assert.Multiple(() =>
        {
            Assert.That(codec.ReadFloatingValues(readRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.ReadSignedValues(readRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.ReadUnsignedValues(readRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.ReadIndices(readRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.ReadMatrices(readRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.ReadRawElements(readRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.CreateDenseAccessor(writeRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(codec.CreateSparseAccessor(writeRequest).Status, Is.EqualTo(GltfOperationStatus.Invalid));
        });
    }

    [Test]
    public void ExtensionHandler_PreservesJsonAndRefusesUnsafeReferenceRemapping()
    {
        IGltfExtensionHandler handler = new GltfExtensionHandler();
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = "EXT_unknown";
        context.Json = new System.Text.Json.Nodes.JsonObject { ["value"] = 1 };

        Assert.Multiple(() =>
        {
            Assert.That(handler.DescribeExtension().Name, Is.EqualTo("*"));
            Assert.That(handler.ReadExtension(context).Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(handler.WriteExtension(context).Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(handler.ValidateExtension(context).Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(
                handler.RemapExtensionReferences(context, new GltfIndexRemap()).Status,
                Is.EqualTo(GltfOperationStatus.Unavailable));
        });
    }

    [Test]
    public void DocumentEditor_CompletesEveryOperation()
    {
        IGltfDocumentEditor editor = new GltfDocumentEditor();
        GltfDocument document = new GltfDocument();
        GltfEditOptions options = new GltfEditOptions();
        GltfDocument created = editor.CreateDocument();
        GltfDocument clone = editor.CloneDocument(document, options);
        GltfMergeResult merge = editor.MergeDocument([document], options);
        GltfMergeResult compact = editor.CompactDocument(document, options);
        GltfMergeResult rebuilt = editor.RebuildReferences(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Asset.Version, Is.EqualTo(GltfConstants.CoreVersion));
            Assert.That(clone, Is.Not.SameAs(document));
            Assert.That(merge.Document, Is.Not.SameAs(document));
            Assert.That(compact.Document, Is.Not.SameAs(document));
            Assert.That(rebuilt.Document, Is.Not.SameAs(document));
        });
    }

    [Test]
    public void Transformer_ForwardsOperationsToTheInternalShell()
    {
        IGltfTransformer transformer = new GltfTransformer();

        GltfTransformReport report = transformer.TransformDocument(
            new GltfDocument(),
            new GltfTransformPlan(),
            new GltfTransformOptions());

        Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
        Assert.That(report.Document, Is.Not.Null);
    }

    [Test]
    public void Converter_CompletesDocumentByteAndStreamEntryPoints()
    {
        IGltfConverter converter = new GltfConverter();
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Json;
        options.WriteOptions.Validate = false;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"}}");
        using MemoryStream input = new MemoryStream(bytes);
        using MemoryStream output = new MemoryStream();
        GltfConversionResult documentResult = converter.ConvertDocument(
            new GltfDocument(),
            options);
        GltfConversionResult byteResult = converter.ConvertBytes(
            bytes,
            options);
        GltfConversionResult streamResult = converter.ConvertStream(
            input,
            output,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(documentResult.Document, Is.Not.Null);
            Assert.That(documentResult.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(byteResult.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(byteResult.PrimaryBytes, Is.EqualTo(bytes));
            Assert.That(streamResult.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(streamResult.PrimaryBytes, Is.Empty);
            Assert.That(output.ToArray(), Is.EqualTo(bytes));
        });
    }

    private static void AssertUnavailable(TestDelegate action, string operation)
    {
        Assert.That(
            action,
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.EqualTo($"The {operation} operation is not implemented."));
    }
}
