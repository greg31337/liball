using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Data;

[TestFixture]
public class CompressionMetadataExtensionDataTests
{
    [Test]
    public void CompressionExtensions_ExposeSchemaDefaultsAndIndependentMappings()
    {
        GltfDracoMeshCompressionExtension firstDraco = new GltfDracoMeshCompressionExtension();
        GltfDracoMeshCompressionExtension secondDraco = new GltfDracoMeshCompressionExtension();
        GltfMeshQuantizationExtension quantization = new GltfMeshQuantizationExtension();
        GltfMeshoptCompressionBufferExtension meshoptBuffer = new GltfMeshoptCompressionBufferExtension();
        GltfMeshoptCompressionBufferViewExtension meshoptView = new GltfMeshoptCompressionBufferViewExtension();

        firstDraco.Attributes[GltfSemantics.Position] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(firstDraco.BufferView, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(secondDraco.Attributes, Is.Empty);
            Assert.That(firstDraco.Attributes, Is.Not.SameAs(secondDraco.Attributes));
            Assert.That(quantization.PropertyJson, Is.Not.Null);
            Assert.That(meshoptBuffer.HasFallback, Is.False);
            Assert.That(meshoptBuffer.Fallback, Is.False);
            Assert.That(meshoptView.Buffer, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(meshoptView.HasByteOffset, Is.False);
            Assert.That(meshoptView.ByteOffset, Is.Zero);
            Assert.That(meshoptView.ByteLength, Is.Zero);
            Assert.That(meshoptView.ByteStride, Is.Zero);
            Assert.That(meshoptView.Count, Is.Zero);
            Assert.That(meshoptView.Mode, Is.Empty);
            Assert.That(meshoptView.HasFilter, Is.False);
            Assert.That(meshoptView.Filter, Is.EqualTo("NONE"));
        });
    }

    [Test]
    public void AlternativeImageSources_PreserveOptionalSourcePresence()
    {
        GltfTextureBasisuExtension basisu = new GltfTextureBasisuExtension();
        GltfTextureWebpExtension webp = new GltfTextureWebpExtension();

        Assert.Multiple(() =>
        {
            Assert.That(basisu.HasSource, Is.False);
            Assert.That(basisu.Source, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(basisu.PropertyJson, Is.Not.SameAs(webp.PropertyJson));
            Assert.That(webp.HasSource, Is.False);
            Assert.That(webp.Source, Is.EqualTo(GltfConstants.AbsentIndex));
        });
    }

    [Test]
    public void XmpJsonLdData_OwnsContextsPropertiesAndAssignments()
    {
        GltfXmpJsonLdExtension firstRoot = new GltfXmpJsonLdExtension();
        GltfXmpJsonLdExtension secondRoot = new GltfXmpJsonLdExtension();
        GltfXmpJsonLdPacket firstPacket = new GltfXmpJsonLdPacket();
        GltfXmpJsonLdPacket secondPacket = new GltfXmpJsonLdPacket();
        GltfXmpJsonLdAssignmentExtension assignment = new GltfXmpJsonLdAssignmentExtension();

        firstPacket.Context["dc"] = "http://purl.org/dc/elements/1.1/";
        firstPacket.Properties["dc:title"] = JsonValue.Create("Example");
        firstRoot.Packets.Add(firstPacket);

        Assert.Multiple(() =>
        {
            Assert.That(secondRoot.Packets, Is.Empty);
            Assert.That(firstRoot.Packets, Is.Not.SameAs(secondRoot.Packets));
            Assert.That(secondPacket.Context, Is.Empty);
            Assert.That(secondPacket.Properties, Is.Empty);
            Assert.That(firstPacket.Context, Is.Not.SameAs(secondPacket.Context));
            Assert.That(firstPacket.Properties, Is.Not.SameAs(secondPacket.Properties));
            Assert.That(firstRoot.HasPackets, Is.False);
            Assert.That(firstPacket.HasContext, Is.False);
            Assert.That(assignment.HasPacket, Is.False);
            Assert.That(assignment.Packet, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(assignment.PropertyJson, Is.Not.Null);
        });
    }

    [Test]
    public void ExtensionResult_CarriesOneTypedPayloadContainer()
    {
        GltfTypedExtensionData typedData = new GltfTypedExtensionData();
        GltfExtensionResult result = new GltfExtensionResult();
        GltfMaterialsAnisotropyExtension material = new GltfMaterialsAnisotropyExtension();
        GltfNodeVisibilityExtension node = new GltfNodeVisibilityExtension();
        GltfDracoMeshCompressionExtension compression = new GltfDracoMeshCompressionExtension();
        GltfXmpJsonLdExtension metadata = new GltfXmpJsonLdExtension();

        typedData.MaterialsAnisotropy = material;
        typedData.NodeVisibility = node;
        typedData.DracoMeshCompression = compression;
        typedData.XmpJsonLd = metadata;
        result.TypedData = typedData;

        Assert.Multiple(() =>
        {
            Assert.That(result.TypedData, Is.SameAs(typedData));
            Assert.That(result.TypedData?.MaterialsAnisotropy, Is.SameAs(material));
            Assert.That(result.TypedData?.NodeVisibility, Is.SameAs(node));
            Assert.That(result.TypedData?.DracoMeshCompression, Is.SameAs(compression));
            Assert.That(result.TypedData?.XmpJsonLd, Is.SameAs(metadata));
            Assert.That(typedData.MaterialsClearcoat, Is.Null);
            Assert.That(typedData.LightsPunctual, Is.Null);
            Assert.That(typedData.Interactivity, Is.Null);
            Assert.That(typedData.KhrMeshoptCompressionBufferView, Is.Null);
            Assert.That(typedData.ExtMeshoptCompressionBufferView, Is.Null);
            Assert.That(typedData.TextureBasisu, Is.Null);
            Assert.That(typedData.TextureWebp, Is.Null);
        });
    }
}
