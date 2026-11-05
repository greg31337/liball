using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfJsonToken
{
    internal JsonTokenType Type;
    internal string JsonPointer = "";
    internal int ByteOffset;
    internal int ByteLength;
    internal string? Text;
    internal bool HasNumberValue;
    internal double NumberValue;
    internal bool DuplicateMember;
}

internal class GltfJsonReadResult
{
    internal bool HasRoot;
    internal JsonNode? Root;
    internal List<GltfJsonToken> Tokens = new List<GltfJsonToken>();
}

internal class GltfJsonString
{
    internal string Value = "";
    internal int ByteOffset;
    internal int ByteLength;
    internal int DecodedByteLength;
}
