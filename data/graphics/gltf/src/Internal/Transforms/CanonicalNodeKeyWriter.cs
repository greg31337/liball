using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class CanonicalNodeKeyWriter
{
    internal GltfJsonWriter ModelWriter = new GltfJsonWriter();
    internal DeterministicJsonWriter JsonWriter = new DeterministicJsonWriter();
    internal GltfDocument? Document;
    internal List<string?> Keys = new List<string?>();
    internal bool[] Active = [];

    internal List<string> CreateNodeKeys(GltfDocument document)
    {
        Document = document;
        ModelWriter.Document = document;
        ModelWriter.Options = new GltfWriteOptions();
        ModelWriter.ExtensionResources = new GltfResourceSet();
        ModelWriter.ExtensionDiagnostics = new List<GltfValidationIssue>();
        ModelWriter.ExtensionStatus = GltfOperationStatus.Success;
        ModelWriter.DispatchExtensionWrites = true;
        Keys.Clear();
        Active = new bool[document.Nodes.Count];
        int index = 0;
        while (index < document.Nodes.Count)
        {
            Keys.Add(null);
            index++;
        }

        List<string> result = new List<string>();
        index = 0;
        while (index < document.Nodes.Count)
        {
            result.Add(CreateNodeKey(index));
            index++;
        }

        return result;
    }

    internal string CreateNodeKey(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= Keys.Count || Document == null)
        {
            return "invalid-node:" + nodeIndex;
        }

        if (Keys[nodeIndex] != null)
        {
            return Keys[nodeIndex] ?? "";
        }

        if (Active[nodeIndex])
        {
            return "node-cycle";
        }

        Active[nodeIndex] = true;
        GltfNode node = Document.Nodes[nodeIndex];
        JsonObject value = ModelWriter.WriteNode(node);
        value.Remove("children");
        value.Remove("skin");
        JsonArray children = new JsonArray();
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            children.Add(JsonValue.Create(CreateNodeKey(node.Children[childIndex])));
            childIndex++;
        }

        if (children.Count > 0)
        {
            value["$children"] = children;
        }

        if (node.HasSkin)
        {
            value["$skin"] = JsonValue.Create(CreateSkinSeedKey(node.Skin));
        }

        string result = JsonWriter.WriteCanonicalValue(value);
        Keys[nodeIndex] = result;
        Active[nodeIndex] = false;
        return result;
    }

    internal string CreateSkinSeedKey(int skinIndex)
    {
        if (Document == null
            || skinIndex < 0
            || skinIndex >= Document.Skins.Count)
        {
            return "invalid-skin:" + skinIndex;
        }

        GltfSkin skin = Document.Skins[skinIndex];
        JsonObject value = ModelWriter.WriteSkin(skin);
        value.Remove("joints");
        value.Remove("skeleton");
        JsonArray joints = new JsonArray();
        int index = 0;
        while (index < skin.Joints.Count)
        {
            joints.Add(JsonValue.Create(CreateNodeLocalKey(skin.Joints[index])));
            index++;
        }

        value["$joints"] = joints;
        if (skin.HasSkeleton)
        {
            value["$skeleton"] = JsonValue.Create(CreateNodeLocalKey(skin.Skeleton));
        }

        return JsonWriter.WriteCanonicalValue(value);
    }

    internal string CreateNodeLocalKey(int nodeIndex)
    {
        if (Document == null
            || nodeIndex < 0
            || nodeIndex >= Document.Nodes.Count)
        {
            return "invalid-node:" + nodeIndex;
        }

        JsonObject value = ModelWriter.WriteNode(Document.Nodes[nodeIndex]);
        value.Remove("children");
        value.Remove("skin");
        return JsonWriter.WriteCanonicalValue(value);
    }
}
