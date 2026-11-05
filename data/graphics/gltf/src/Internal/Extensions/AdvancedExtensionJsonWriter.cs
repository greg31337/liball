using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class AdvancedExtensionJsonWriter
{
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal JsonNode? WriteAdvancedExtension(
        string name,
        GltfTypedExtensionData typed)
    {
        if (name == GltfExtensionNames.KhrGaussianSplatting
            && typed.GaussianSplatting != null)
        {
            return WriteGaussian(typed.GaussianSplatting);
        }

        if (name == GltfExtensionNames.KhrInteractivity
            && typed.Interactivity != null)
        {
            return WriteInteractivity(typed.Interactivity);
        }

        return null;
    }

    internal JsonObject WriteGaussian(GltfGaussianSplattingExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasKernel)
        {
            result["kernel"] = JsonValue.Create(source.Kernel);
        }

        if (source.HasColorSpace)
        {
            result["colorSpace"] = JsonValue.Create(source.ColorSpace);
        }

        if (source.HasProjection)
        {
            result["projection"] = JsonValue.Create(source.Projection);
        }

        if (source.HasSortingMethod)
        {
            result["sortingMethod"] = JsonValue.Create(source.SortingMethod);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteInteractivity(GltfInteractivityExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasGraphs)
        {
            JsonArray graphs = new JsonArray();
            int index = 0;
            while (index < source.Graphs.Count)
            {
                graphs.Add(WriteGraph(source.Graphs[index]));
                index++;
            }

            result["graphs"] = graphs;
        }

        if (source.HasDefaultGraph)
        {
            result["graph"] = JsonValue.Create(source.DefaultGraph);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteGraph(GltfInteractivityGraph source)
    {
        JsonObject result = new JsonObject();
        if (source.HasTypes)
        {
            JsonArray values = new JsonArray();
            int index = 0;
            while (index < source.Types.Count)
            {
                values.Add(WriteType(source.Types[index]));
                index++;
            }

            result["types"] = values;
        }

        if (source.HasVariables)
        {
            JsonArray values = new JsonArray();
            int index = 0;
            while (index < source.Variables.Count)
            {
                values.Add(WriteVariable(source.Variables[index]));
                index++;
            }

            result["variables"] = values;
        }

        if (source.HasEvents)
        {
            JsonArray values = new JsonArray();
            int index = 0;
            while (index < source.Events.Count)
            {
                values.Add(WriteEvent(source.Events[index]));
                index++;
            }

            result["events"] = values;
        }

        if (source.HasDeclarations)
        {
            JsonArray values = new JsonArray();
            int index = 0;
            while (index < source.Declarations.Count)
            {
                values.Add(WriteDeclaration(source.Declarations[index]));
                index++;
            }

            result["declarations"] = values;
        }

        if (source.HasNodes)
        {
            JsonArray values = new JsonArray();
            int index = 0;
            while (index < source.Nodes.Count)
            {
                values.Add(WriteNode(source.Nodes[index]));
                index++;
            }

            result["nodes"] = values;
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteType(GltfInteractivityType source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSignature)
        {
            result["signature"] = JsonValue.Create(source.Signature);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteVariable(GltfInteractivityVariable source)
    {
        JsonObject result = new JsonObject();
        if (source.HasType)
        {
            result["type"] = JsonValue.Create(source.Type);
        }

        if (source.HasValue)
        {
            result["value"] = JsonValueCloner.CloneNode(source.Value);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteEvent(GltfInteractivityEvent source)
    {
        JsonObject result = new JsonObject();
        if (source.HasId)
        {
            result["id"] = JsonValue.Create(source.Id);
        }

        if (source.HasValues)
        {
            JsonObject values = new JsonObject();
            List<string> names = GetSortedNames(source.Values.Keys);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                values[name] = WriteEventValue(source.Values[name]);
                index++;
            }

            result["values"] = values;
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteEventValue(GltfInteractivityEventValue source)
    {
        JsonObject result = new JsonObject();
        if (source.HasType)
        {
            result["type"] = JsonValue.Create(source.Type);
        }

        if (source.HasValue)
        {
            result["value"] = JsonValueCloner.CloneNode(source.Value);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteDeclaration(GltfInteractivityDeclaration source)
    {
        JsonObject result = new JsonObject();
        if (source.HasOperation)
        {
            result["op"] = JsonValue.Create(source.Operation);
        }

        if (source.HasInputValueSockets)
        {
            result["inputValueSockets"] = WriteDeclarationSockets(
                source.InputValueSockets);
        }

        if (source.HasOutputValueSockets)
        {
            result["outputValueSockets"] = WriteDeclarationSockets(
                source.OutputValueSockets);
        }

        if (source.HasExtension)
        {
            result["extension"] = JsonValue.Create(source.Extension);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteDeclarationSockets(
        Dictionary<string, GltfInteractivityDeclarationValueSocket> source)
    {
        JsonObject result = new JsonObject();
        List<string> names = GetSortedNames(source.Keys);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            GltfInteractivityDeclarationValueSocket socket = source[name];
            JsonObject value = new JsonObject();
            if (socket.HasType)
            {
                value["type"] = JsonValue.Create(socket.Type);
            }

            PropertyJson.AppendPropertyData(value, socket.PropertyJson);
            result[name] = value;
            index++;
        }

        return result;
    }

    internal JsonObject WriteNode(GltfInteractivityNode source)
    {
        JsonObject result = new JsonObject();
        if (source.HasDeclaration)
        {
            result["declaration"] = JsonValue.Create(source.Declaration);
        }

        if (source.HasConfiguration)
        {
            JsonObject values = new JsonObject();
            List<string> names = GetSortedNames(source.Configuration.Keys);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                values[name] = WriteConfiguration(source.Configuration[name]);
                index++;
            }

            result["configuration"] = values;
        }

        if (source.HasValues)
        {
            JsonObject values = new JsonObject();
            List<string> names = GetSortedNames(source.Values.Keys);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                values[name] = WriteValueSocket(source.Values[name]);
                index++;
            }

            result["values"] = values;
        }

        if (source.HasFlows)
        {
            JsonObject values = new JsonObject();
            List<string> names = GetSortedNames(source.Flows.Keys);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                values[name] = WriteFlowSocket(source.Flows[name]);
                index++;
            }

            result["flows"] = values;
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteConfiguration(GltfInteractivityConfiguration source)
    {
        JsonObject result = new JsonObject();
        if (source.HasValue)
        {
            result["value"] = JsonValueCloner.CloneNode(source.Value);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteValueSocket(GltfInteractivityValueSocket source)
    {
        JsonObject result = new JsonObject();
        if (source.HasNode)
        {
            result["node"] = JsonValue.Create(source.Node);
        }

        if (source.HasSocket)
        {
            result["socket"] = JsonValue.Create(source.Socket);
        }

        if (source.HasType)
        {
            result["type"] = JsonValue.Create(source.Type);
        }

        if (source.HasValue)
        {
            result["value"] = JsonValueCloner.CloneNode(source.Value);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteFlowSocket(GltfInteractivityFlowSocket source)
    {
        JsonObject result = new JsonObject();
        if (source.HasNode)
        {
            result["node"] = JsonValue.Create(source.Node);
        }

        if (source.HasSocket)
        {
            result["socket"] = JsonValue.Create(source.Socket);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal List<string> GetSortedNames(IEnumerable<string> source)
    {
        List<string> result = new List<string>(source);
        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
