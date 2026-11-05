using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class ExtensionHandlerIntegrationTests
{
    [Test]
    public void Reader_StoresTypedPayloadAndCollectsHandlerResources()
    {
        IntegrationHandler handler = new IntegrationHandler();
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.NodeVisibility = new GltfNodeVisibilityExtension();
        handler.ReadResult.TypedData = typed;
        GltfResource resource = new GltfResource();
        resource.SourceUri = "custom.bin";
        resource.Bytes = [4, 5];
        handler.ReadResult.Resources.Resources.Add(resource);
        GltfReadOptions options = new GltfReadOptions();
        options.ExtensionHandlers.Add(handler);
        const string source = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"nodes\":[{\"extensions\":{\"VENDOR_custom\":{\"value\":1}}}],"
            + "\"extensionsUsed\":[\"VENDOR_custom\"]}";

        GltfReadResult result = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(source),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(handler.ReadCount, Is.EqualTo(1));
            Assert.That(handler.ReadContext!.Name, Is.EqualTo("VENDOR_custom"));
            Assert.That(handler.ReadContext.ObjectLocation, Is.EqualTo("node"));
            Assert.That(handler.ReadContext.HasObjectIndex, Is.True);
            Assert.That(handler.ReadContext.ObjectIndex, Is.Zero);
            Assert.That(
                result.Document!.Nodes[0].PropertyJson.TypedExtensions["VENDOR_custom"],
                Is.SameAs(typed));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.Resources.Resources[0].Bytes, Is.EqualTo(new byte[] { 4, 5 }));
        });
    }

    [Test]
    public void Reader_ProvidesMeshLocalPrimitiveIndexToExtensionHandlers()
    {
        IntegrationHandler handler = new IntegrationHandler();
        GltfReadOptions options = new GltfReadOptions();
        options.ExtensionHandlers.Add(handler);
        const string source = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},\"extensions\":{"
            + "\"VENDOR_custom\":{}}}]}],\"extensionsUsed\":[\"VENDOR_custom\"]}";

        new GltfReader().ReadBytes(Encoding.UTF8.GetBytes(source), options);

        Assert.Multiple(() =>
        {
            Assert.That(handler.ReadContext!.ObjectLocation, Is.EqualTo("mesh.primitive"));
            Assert.That(handler.ReadContext.HasObjectIndex, Is.True);
            Assert.That(handler.ReadContext.ObjectIndex, Is.Zero);
            Assert.That(handler.ReadContext.HasSubObjectIndex, Is.True);
            Assert.That(handler.ReadContext.SubObjectIndex, Is.Zero);
        });
    }

    [Test]
    public void Writer_UsesTypedPayloadAndEmitsHandlerJsonWithoutMutatingRawJson()
    {
        IntegrationHandler handler = new IntegrationHandler();
        handler.WriteResult.Json = new JsonObject { ["written"] = true };
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add("VENDOR_custom");
        document.PropertyJson.Extensions["VENDOR_custom"] = new JsonObject { ["raw"] = 1 };
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.NodeVisibility = new GltfNodeVisibilityExtension();
        document.PropertyJson.TypedExtensions["VENDOR_custom"] = typed;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        options.ExtensionHandlers.Add(handler);

        GltfWriteResult result = new GltfWriter().WriteJsonText(document, options);
        JsonObject output = JsonNode.Parse(result.JsonText!)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(handler.WriteCount, Is.EqualTo(1));
            Assert.That(handler.WriteContext!.Document, Is.SameAs(document));
            Assert.That(handler.WriteContext.ObjectLocation, Is.EqualTo("glTF"));
            Assert.That(handler.WriteContext.TypedData, Is.SameAs(typed));
            Assert.That(output["extensions"]!["VENDOR_custom"]!["written"]!.GetValue<bool>(), Is.True);
            Assert.That(document.PropertyJson.Extensions["VENDOR_custom"]!["raw"]!.GetValue<int>(), Is.EqualTo(1));
        });
    }

    [Test]
    public void Validator_AppendsRegisteredHandlerSemanticDiagnostics()
    {
        IntegrationHandler handler = new IntegrationHandler();
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = "CUSTOM_EXTENSION_INVALID";
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = "The custom payload is invalid.";
        issue.JsonPointer = "/nodes/0/extensions/VENDOR_custom/value";
        handler.ValidationResult.Status = GltfOperationStatus.Invalid;
        handler.ValidationResult.Diagnostics.Add(issue);
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add("VENDOR_custom");
        GltfNode node = new GltfNode();
        node.PropertyJson.Extensions["VENDOR_custom"] = new JsonObject { ["value"] = -1 };
        document.Nodes.Add(node);
        GltfValidationOptions options = new GltfValidationOptions();
        options.ExtensionHandlers.Add(handler);

        GltfValidationReport report = new GltfValidator().ValidateDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(handler.ValidationCount, Is.EqualTo(1));
            Assert.That(handler.ValidationContext!.Document, Is.SameAs(document));
            Assert.That(handler.ValidationContext.ObjectLocation, Is.EqualTo("node"));
            Assert.That(report.IsValid, Is.False);
            Assert.That(
                report.Issues.Any(value => value.Code == "CUSTOM_EXTENSION_INVALID"
                    && value.JsonPointer == "/nodes/0/extensions/VENDOR_custom/value"),
                Is.True);
        });
    }

    private class IntegrationHandler : IGltfExtensionHandler
    {
        internal GltfExtensionDescriptor Descriptor = new GltfExtensionDescriptor();
        internal GltfExtensionResult ReadResult = new GltfExtensionResult();
        internal GltfExtensionResult WriteResult = new GltfExtensionResult();
        internal GltfExtensionResult ValidationResult = new GltfExtensionResult();
        internal GltfExtensionContext? ReadContext;
        internal GltfExtensionContext? WriteContext;
        internal GltfExtensionContext? ValidationContext;
        internal int ReadCount;
        internal int WriteCount;
        internal int ValidationCount;

        internal IntegrationHandler()
        {
            Descriptor.Name = "VENDOR_custom";
        }

        public GltfExtensionDescriptor DescribeExtension()
        {
            return Descriptor;
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            ReadContext = context;
            ReadCount++;
            return ReadResult;
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            WriteContext = context;
            WriteCount++;
            return WriteResult;
        }

        public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
        {
            ValidationContext = context;
            ValidationCount++;
            return ValidationResult;
        }

        public GltfExtensionResult RemapExtensionReferences(
            GltfExtensionContext context,
            GltfIndexRemap remap)
        {
            return new GltfExtensionResult();
        }
    }
}
