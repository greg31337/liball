using System.Text.Json.Nodes;

namespace gltf.Tests.Support;

internal sealed class GltfTestDocumentBuilder
{
    private readonly GltfDocument document;

    internal GltfTestDocumentBuilder()
    {
        document = new GltfDocument();
        document.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
    }

    internal GltfTestDocumentBuilder AddResolvedBuffer(
        string uri,
        byte[] bytes)
    {
        byte[] ownedBytes = bytes.ToArray();
        int resourceIndex = document.Resources.Resources.Count;
        document.Resources.Resources.Add(new GltfResource
        {
            Kind = GltfResourceKind.Buffer,
            UriKind = GltfUriKind.Relative,
            SourceUri = uri,
            EffectiveUri = uri,
            MimeType = GltfMimeTypes.GltfBuffer,
            Bytes = ownedBytes
        });
        document.Buffers.Add(new GltfBuffer
        {
            Uri = uri,
            ByteLength = ownedBytes.Length,
            Resolved = true,
            ResolvedBytes = ownedBytes,
            HasResource = true,
            Resource = resourceIndex
        });
        return this;
    }

    internal GltfTestDocumentBuilder AddResolvedImage(
        string uri,
        string mimeType,
        GltfImageFormat format,
        byte[] bytes)
    {
        byte[] ownedBytes = bytes.ToArray();
        int resourceIndex = document.Resources.Resources.Count;
        document.Resources.Resources.Add(new GltfResource
        {
            Kind = GltfResourceKind.Image,
            UriKind = GltfUriKind.Relative,
            SourceUri = uri,
            EffectiveUri = uri,
            MimeType = mimeType,
            ImageFormat = format,
            Bytes = ownedBytes
        });
        document.Images.Add(new GltfImage
        {
            Uri = uri,
            MimeType = mimeType,
            Resolved = true,
            ResolvedBytes = ownedBytes,
            ResolvedMimeType = mimeType,
            HasResource = true,
            Resource = resourceIndex
        });
        return this;
    }

    internal GltfTestDocumentBuilder AddNode(string name)
    {
        document.Nodes.Add(new GltfNode
        {
            Name = name
        });
        return this;
    }

    internal GltfTestDocumentBuilder AddExtension(
        string name,
        JsonNode payload,
        bool required)
    {
        document.PropertyJson.Extensions[name] = payload.DeepClone();
        if (!document.ExtensionsUsed.Contains(name))
        {
            document.ExtensionsUsed.Add(name);
        }

        if (required && !document.ExtensionsRequired.Contains(name))
        {
            document.ExtensionsRequired.Add(name);
        }

        return this;
    }

    internal GltfDocument Build()
    {
        return document;
    }
}
