using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class ExtensionHandlerDispatcherTests
{
    [Test]
    public void DispatchOperations_SelectARegisteredHandlerUnknownToTheCore()
    {
        ProbeHandler handler = new ProbeHandler("VENDOR_custom");
        GltfExtensionContext context = CreateContext("VENDOR_custom");
        ExtensionHandlerDispatcher dispatcher = new ExtensionHandlerDispatcher();
        List<IGltfExtensionHandler> handlers = new List<IGltfExtensionHandler> { handler };
        GltfIndexRemap remap = new GltfIndexRemap();

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.DispatchReadExtension(context, handlers), Is.SameAs(handler.Result));
            Assert.That(dispatcher.DispatchWriteExtension(context, handlers), Is.SameAs(handler.Result));
            Assert.That(dispatcher.DispatchValidation(context, handlers), Is.SameAs(handler.Result));
            Assert.That(dispatcher.DispatchReferenceRemap(context, remap, handlers), Is.SameAs(handler.Result));
            Assert.That(handler.ReadContext, Is.SameAs(context));
            Assert.That(handler.WriteContext, Is.SameAs(context));
            Assert.That(handler.ValidationContext, Is.SameAs(context));
            Assert.That(handler.RemapContext, Is.SameAs(context));
            Assert.That(handler.Remap, Is.SameAs(remap));
            Assert.That(handler.DescribeCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void DispatchReadExtension_PreservesUnhandledJsonWithIndependentOwnership()
    {
        GltfExtensionContext context = CreateContext("FUTURE_unknown");
        context.Json = new JsonObject { ["items"] = new JsonArray(1, 2) };
        ExtensionHandlerDispatcher dispatcher = new ExtensionHandlerDispatcher();

        GltfExtensionResult result = dispatcher.DispatchReadExtension(
            context,
            new List<IGltfExtensionHandler>());
        result.Json!["items"]![0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Descriptor!.Name, Is.EqualTo("FUTURE_unknown"));
            Assert.That(result.Descriptor.Status, Is.EqualTo(GltfExtensionStatus.Unknown));
            Assert.That(result.Descriptor.AllowsFallback, Is.True);
            Assert.That(context.Json!["items"]![0]!.GetValue<int>(), Is.EqualTo(1));
        });
    }

    [Test]
    public void DispatchReadExtension_UsesRegistryMetadataForKnownFallbacks()
    {
        GltfExtensionContext context = CreateContext(GltfExtensionNames.KhrTextureTransform);
        ExtensionHandlerDispatcher dispatcher = new ExtensionHandlerDispatcher();

        GltfExtensionResult result = dispatcher.DispatchReadExtension(
            context,
            new List<IGltfExtensionHandler>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor!.Name, Is.EqualTo(GltfExtensionNames.KhrTextureTransform));
            Assert.That(result.Descriptor.Status, Is.EqualTo(GltfExtensionStatus.Ratified));
            Assert.That(result.Descriptor.HasTypedModel, Is.True);
        });
    }

    [Test]
    public void Enumeration_ReturnsHandlerResourcesAndReferences()
    {
        ProbeHandler handler = new ProbeHandler("VENDOR_custom");
        GltfResource resource = new GltfResource();
        resource.SourceUri = "payload.bin";
        handler.Result.Resources.Resources.Add(resource);
        GltfDependency reference = new GltfDependency();
        reference.TargetCollection = "nodes";
        reference.TargetIndex = 3;
        handler.Result.References.Add(reference);
        ExtensionHandlerDispatcher dispatcher = new ExtensionHandlerDispatcher();
        List<IGltfExtensionHandler> handlers = new List<IGltfExtensionHandler> { handler };
        GltfExtensionContext context = CreateContext("VENDOR_custom");

        GltfResourceSet resources = dispatcher.EnumerateExtensionResources(context, handlers);
        List<GltfDependency> references = dispatcher.EnumerateExtensionReferences(context, handlers);

        Assert.Multiple(() =>
        {
            Assert.That(resources.Resources, Is.EqualTo(new[] { resource }));
            Assert.That(references, Is.EqualTo(new[] { reference }));
            Assert.That(handler.ValidationCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void DispatchReferenceRemap_ReportsUnhandledPayloadAsUnsafe()
    {
        GltfExtensionContext context = CreateContext("FUTURE_unknown");
        context.ObjectLocation = "nodes";
        context.HasObjectIndex = true;
        context.ObjectIndex = 4;
        ExtensionHandlerDispatcher dispatcher = new ExtensionHandlerDispatcher();

        GltfExtensionResult result = dispatcher.DispatchReferenceRemap(
            context,
            new GltfIndexRemap(),
            new List<IGltfExtensionHandler>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.ExtensionHandlerUnavailable));
            Assert.That(result.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/4/extensions/FUTURE_unknown"));
        });
    }

    [Test]
    public void DispatchReadExtension_RejectsAmbiguousRegistrations()
    {
        GltfExtensionContext context = CreateContext("VENDOR_custom");
        ExtensionHandlerDispatcher dispatcher = new ExtensionHandlerDispatcher();
        List<IGltfExtensionHandler> handlers = new List<IGltfExtensionHandler>
        {
            new ProbeHandler("VENDOR_custom"),
            new ProbeHandler("VENDOR_custom")
        };

        Assert.That(
            () => dispatcher.DispatchReadExtension(context, handlers),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.EqualTo(
                    "More than one extension handler is registered for 'VENDOR_custom'."));
    }

    private static GltfExtensionContext CreateContext(string name)
    {
        GltfExtensionContext result = new GltfExtensionContext();
        result.Name = name;
        return result;
    }

    private class ProbeHandler : IGltfExtensionHandler
    {
        internal GltfExtensionDescriptor Descriptor = new GltfExtensionDescriptor();
        internal GltfExtensionResult Result = new GltfExtensionResult();
        internal GltfExtensionContext? ReadContext;
        internal GltfExtensionContext? WriteContext;
        internal GltfExtensionContext? ValidationContext;
        internal GltfExtensionContext? RemapContext;
        internal GltfIndexRemap? Remap;
        internal int ValidationCount;
        internal int DescribeCount;

        internal ProbeHandler(string name)
        {
            Descriptor.Name = name;
        }

        public GltfExtensionDescriptor DescribeExtension()
        {
            DescribeCount++;
            return Descriptor;
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            ReadContext = context;
            return Result;
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            WriteContext = context;
            return Result;
        }

        public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
        {
            ValidationContext = context;
            ValidationCount++;
            return Result;
        }

        public GltfExtensionResult RemapExtensionReferences(
            GltfExtensionContext context,
            GltfIndexRemap remap)
        {
            RemapContext = context;
            Remap = remap;
            return Result;
        }
    }
}
