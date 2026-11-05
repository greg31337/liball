using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class XmpJsonLdExtensionJsonWriter
{
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal JsonNode? WriteXmpJsonLdExtension(
        GltfExtensionContext context,
        GltfTypedExtensionData typed)
    {
        if (context.ObjectLocation == "glTF" && typed.XmpJsonLd != null)
        {
            return WriteRoot(typed.XmpJsonLd);
        }

        if (context.ObjectLocation != "glTF" && typed.XmpJsonLdAssignment != null)
        {
            return WriteAssignment(typed.XmpJsonLdAssignment);
        }

        return null;
    }

    internal JsonObject WriteRoot(GltfXmpJsonLdExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasPackets)
        {
            JsonArray packets = new JsonArray();
            int index = 0;
            while (index < source.Packets.Count)
            {
                packets.Add(WritePacket(source.Packets[index]));
                index++;
            }

            result["packets"] = packets;
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePacket(GltfXmpJsonLdPacket source)
    {
        JsonObject result = new JsonObject();
        if (source.HasContext)
        {
            JsonObject context = new JsonObject();
            List<string> contextNames = GetSortedStringKeys(source.Context);
            int contextIndex = 0;
            while (contextIndex < contextNames.Count)
            {
                string name = contextNames[contextIndex];
                context[name] = JsonValue.Create(source.Context[name]);
                contextIndex++;
            }

            result["@context"] = context;
        }

        List<string> propertyNames = PropertyJson.GetSortedNodeKeys(source.Properties);
        int propertyIndex = 0;
        while (propertyIndex < propertyNames.Count)
        {
            string name = propertyNames[propertyIndex];
            if (name != "@context")
            {
                result[name] = JsonValueCloner.CloneNode(source.Properties[name]);
            }

            propertyIndex++;
        }

        return result;
    }

    internal JsonObject WriteAssignment(GltfXmpJsonLdAssignmentExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasPacket)
        {
            result["packet"] = JsonValue.Create(source.Packet);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal List<string> GetSortedStringKeys(Dictionary<string, string> source)
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, string> member in source)
        {
            result.Add(member.Key);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
