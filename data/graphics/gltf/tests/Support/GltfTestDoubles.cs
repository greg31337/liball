using System.Text.Json.Nodes;

namespace gltf.Tests.Support;

internal sealed class TestResourceResolver : IGltfResourceResolver
{
    internal Dictionary<string, GltfResource> Resources =
        new Dictionary<string, GltfResource>(StringComparer.Ordinal);
    internal List<GltfResourceRequest> Requests = new List<GltfResourceRequest>();

    internal void Add(string uri, GltfResourceKind kind, string mimeType, byte[] bytes)
    {
        Resources[uri] = new GltfResource
        {
            Kind = kind,
            UriKind = GltfUriKind.Relative,
            SourceUri = uri,
            EffectiveUri = uri,
            MimeType = mimeType,
            Bytes = bytes.ToArray()
        };
    }

    public GltfResource ResolveResource(GltfResourceRequest request)
    {
        Requests.Add(request);
        string key = request.SourceUri ?? "";
        if (!Resources.ContainsKey(key))
        {
            return new GltfResource
            {
                Status = GltfOperationStatus.Unavailable,
                SourceUri = request.SourceUri
            };
        }

        GltfResource source = Resources[key];
        return new GltfResource
        {
            Status = source.Status,
            Kind = source.Kind,
            UriKind = source.UriKind,
            SourceUri = source.SourceUri,
            EffectiveUri = source.EffectiveUri,
            MimeType = source.MimeType,
            Bytes = source.Bytes.ToArray()
        };
    }
}

internal sealed class TestCodec : IGltfCompressionCodec, IGltfImageCodec
{
    internal List<string> Calls = new List<string>();
    internal GltfCompressionResult CompressionResult = new GltfCompressionResult();
    internal GltfImageResult ImageResult = new GltfImageResult();
    internal GltfImageInfo ImageInfo = new GltfImageInfo();

    public GltfCompressionResult DecodeGeometry(GltfCompressionRequest request)
    {
        Calls.Add("DecodeGeometry");
        return CompressionResult;
    }

    public GltfCompressionResult EncodeGeometry(GltfCompressionRequest request)
    {
        Calls.Add("EncodeGeometry");
        return CompressionResult;
    }

    public GltfCompressionResult DecodeMeshoptData(GltfCompressionRequest request)
    {
        Calls.Add("DecodeMeshoptData");
        return CompressionResult;
    }

    public GltfCompressionResult EncodeMeshoptData(GltfCompressionRequest request)
    {
        Calls.Add("EncodeMeshoptData");
        return CompressionResult;
    }

    public GltfImageInfo InspectImage(GltfImageRequest request)
    {
        Calls.Add("InspectImage");
        return ImageInfo;
    }

    public GltfImageResult DecodeImage(GltfImageRequest request)
    {
        Calls.Add("DecodeImage");
        return ImageResult;
    }

    public GltfImageResult EncodeImage(GltfImageRequest request)
    {
        Calls.Add("EncodeImage");
        return ImageResult;
    }

    public GltfImageResult TranscodeImage(GltfImageRequest request)
    {
        Calls.Add("TranscodeImage");
        return ImageResult;
    }
}

internal sealed class TestExtensionHandler : IGltfExtensionHandler
{
    internal GltfExtensionDescriptor Descriptor = new GltfExtensionDescriptor();
    internal List<string> Calls = new List<string>();
    internal GltfOperationStatus Status = GltfOperationStatus.Success;

    internal TestExtensionHandler(string name)
    {
        Descriptor.Name = name;
        Descriptor.HasSemanticValidation = true;
        Descriptor.CanRemapReferences = true;
        Descriptor.AllowsFallback = true;
    }

    public GltfExtensionDescriptor DescribeExtension()
    {
        Calls.Add("DescribeExtension");
        return Descriptor;
    }

    public GltfExtensionResult ReadExtension(GltfExtensionContext context)
    {
        Calls.Add("ReadExtension");
        return CreateResult(context.Json);
    }

    public GltfExtensionResult WriteExtension(GltfExtensionContext context)
    {
        Calls.Add("WriteExtension");
        return CreateResult(context.Json);
    }

    public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
    {
        Calls.Add("ValidateExtension");
        return CreateResult(context.Json);
    }

    public GltfExtensionResult RemapExtensionReferences(
        GltfExtensionContext context,
        GltfIndexRemap remap)
    {
        Calls.Add("RemapExtensionReferences");
        return CreateResult(context.Json);
    }

    private GltfExtensionResult CreateResult(JsonNode? json)
    {
        return new GltfExtensionResult
        {
            Status = Status,
            Json = json
        };
    }
}

internal sealed class TestTransformHook : IGltfTransformHook
{
    internal int Calls;
    internal string? NodeName;
    internal string? Codec;
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal GltfTransformContext? LastContext;

    public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
    {
        Calls++;
        LastContext = context;
        if (NodeName != null
            && context.Document != null
            && context.Document.Nodes.Count > 0)
        {
            context.Document.Nodes[0].Name = NodeName;
        }

        GltfTransformStepResult result = new GltfTransformStepResult();
        result.Status = Status;
        if (NodeName != null)
        {
            result.ChangedObjects.Add("nodes/0");
        }

        if (Codec != null)
        {
            result.Codecs.Add(Codec);
        }

        return result;
    }
}
