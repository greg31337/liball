using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

/// <summary>Stores independently mutable extension, extras, and unknown JSON members for a glTF property.</summary>
public class GltfPropertyJson
{
    /// <summary>Extension payloads keyed by extension name.</summary>
    public Dictionary<string, JsonNode?> Extensions = new Dictionary<string, JsonNode?>();

    /// <summary>Typed extension payloads produced by registered handlers, keyed by extension name.</summary>
    public Dictionary<string, GltfTypedExtensionData> TypedExtensions = new Dictionary<string, GltfTypedExtensionData>();

    /// <summary>The optional free-form extras value.</summary>
    public JsonNode? Extras;

    /// <summary>Whether the extras member was explicitly present, including with a JSON null value.</summary>
    public bool HasExtras;

    /// <summary>Unrecognized object members keyed by their original JSON names.</summary>
    public Dictionary<string, JsonNode?> UnknownMembers = new Dictionary<string, JsonNode?>();
}
