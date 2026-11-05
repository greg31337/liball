using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

/// <summary>Represents root <c>KHR_xmp_json_ld</c> packet data.</summary>
public class GltfXmpJsonLdExtension
{
    /// <summary>Whether the required packets member was explicitly present.</summary>
    public bool HasPackets;

    /// <summary>The embedded XMP JSON-LD packets.</summary>
    public List<GltfXmpJsonLdPacket> Packets = new List<GltfXmpJsonLdPacket>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one owned XMP JSON-LD metadata packet.</summary>
public class GltfXmpJsonLdPacket
{
    /// <summary>Whether the required JSON-LD context member was explicitly present.</summary>
    public bool HasContext;

    /// <summary>JSON-LD namespace prefixes mapped to context identifiers.</summary>
    public Dictionary<string, string> Context = new Dictionary<string, string>();

    /// <summary>Owned XMP properties keyed by their JSON-LD names.</summary>
    public Dictionary<string, JsonNode?> Properties = new Dictionary<string, JsonNode?>();
}

/// <summary>Represents an object assignment to a root XMP JSON-LD packet.</summary>
public class GltfXmpJsonLdAssignmentExtension
{
    /// <summary>Whether the required packet member was explicitly present.</summary>
    public bool HasPacket;

    /// <summary>The referenced root packet index.</summary>
    public int Packet = GltfConstants.AbsentIndex;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
