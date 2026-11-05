using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class AdvancedExtensionJsonReader
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal bool HandlesAdvancedExtension(string name)
    {
        return name == GltfExtensionNames.KhrGaussianSplatting
            || name == GltfExtensionNames.KhrInteractivity;
    }

    internal GltfExtensionResult ReadAdvancedExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        if (context.Json == null || context.Json.GetValueKind() != JsonValueKind.Object)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The extension payload must be a JSON object.",
                CreatePointer(context));
            return result;
        }

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (context.Name == GltfExtensionNames.KhrGaussianSplatting)
        {
            typed.GaussianSplatting = ReadGaussian(
                context.Json.AsObject(),
                context,
                result);
        }
        else
        {
            typed.Interactivity = ReadInteractivity(
                context.Json.AsObject(),
                context,
                result);
        }

        result.TypedData = typed;
        return result;
    }

    internal GltfGaussianSplattingExtension ReadGaussian(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfGaussianSplattingExtension value = new GltfGaussianSplattingExtension();
        string pointer = CreatePointer(context);
        value.HasKernel = source.ContainsKey("kernel");
        if (value.HasKernel)
        {
            value.Kernel = ReadString(source["kernel"], result, pointer + "/kernel");
        }
        else
        {
            AddMissingIssue(result, "kernel", pointer + "/kernel");
        }

        value.HasColorSpace = source.ContainsKey("colorSpace");
        if (value.HasColorSpace)
        {
            value.ColorSpace = ReadString(
                source["colorSpace"],
                result,
                pointer + "/colorSpace");
        }
        else
        {
            AddMissingIssue(result, "colorSpace", pointer + "/colorSpace");
        }

        value.HasProjection = source.ContainsKey("projection");
        if (value.HasProjection)
        {
            value.Projection = ReadString(
                source["projection"],
                result,
                pointer + "/projection");
        }

        value.HasSortingMethod = source.ContainsKey("sortingMethod");
        if (value.HasSortingMethod)
        {
            value.SortingMethod = ReadString(
                source["sortingMethod"],
                result,
                pointer + "/sortingMethod");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["kernel", "colorSpace", "projection", "sortingMethod"]);
        return value;
    }

    internal GltfInteractivityExtension ReadInteractivity(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfInteractivityExtension value = new GltfInteractivityExtension();
        string pointer = CreatePointer(context);
        value.HasGraphs = source.ContainsKey("graphs");
        if (!value.HasGraphs)
        {
            AddMissingIssue(result, "graphs", pointer + "/graphs");
        }
        else
        {
            JsonArray? graphs = ReadArray(source["graphs"], result, pointer + "/graphs");
            if (graphs != null)
            {
                int index = 0;
                while (index < graphs.Count)
                {
                    JsonObject? graph = ReadObject(
                        graphs[index],
                        result,
                        pointer + "/graphs/" + index);
                    if (graph != null)
                    {
                        value.Graphs.Add(ReadGraph(
                            graph,
                            result,
                            pointer + "/graphs/" + index));
                    }

                    index++;
                }
            }
        }

        value.HasDefaultGraph = source.ContainsKey("graph");
        if (value.HasDefaultGraph)
        {
            value.DefaultGraph = ReadIndex(
                source["graph"],
                result,
                pointer + "/graph");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["graphs", "graph"]);
        return value;
    }

    internal GltfInteractivityGraph ReadGraph(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfInteractivityGraph value = new GltfInteractivityGraph();
        value.HasTypes = source.ContainsKey("types");
        if (value.HasTypes)
        {
            ReadTypes(source["types"], value, result, pointer + "/types");
        }

        value.HasVariables = source.ContainsKey("variables");
        if (value.HasVariables)
        {
            ReadVariables(source["variables"], value, result, pointer + "/variables");
        }

        value.HasEvents = source.ContainsKey("events");
        if (value.HasEvents)
        {
            ReadEvents(source["events"], value, result, pointer + "/events");
        }

        value.HasDeclarations = source.ContainsKey("declarations");
        if (value.HasDeclarations)
        {
            ReadDeclarations(
                source["declarations"],
                value,
                result,
                pointer + "/declarations");
        }

        value.HasNodes = source.ContainsKey("nodes");
        if (value.HasNodes)
        {
            ReadNodes(source["nodes"], value, result, pointer + "/nodes");
        }

        if (source.ContainsKey("name"))
        {
            value.Name = ReadString(source["name"], result, pointer + "/name");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["types", "variables", "events", "declarations", "nodes", "name"]);
        return value;
    }

    internal void ReadTypes(
        JsonNode? node,
        GltfInteractivityGraph target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonArray? values = ReadArray(node, result, pointer);
        if (values == null)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            JsonObject? source = ReadObject(values[index], result, pointer + "/" + index);
            if (source != null)
            {
                GltfInteractivityType value = new GltfInteractivityType();
                value.HasSignature = source.ContainsKey("signature");
                if (value.HasSignature)
                {
                    value.Signature = ReadString(
                        source["signature"],
                        result,
                        pointer + "/" + index + "/signature");
                }
                else
                {
                    AddMissingIssue(
                        result,
                        "signature",
                        pointer + "/" + index + "/signature");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(source, ["signature"]);
                target.Types.Add(value);
            }

            index++;
        }
    }

    internal void ReadVariables(
        JsonNode? node,
        GltfInteractivityGraph target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonArray? values = ReadArray(node, result, pointer);
        if (values == null)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            JsonObject? source = ReadObject(values[index], result, pointer + "/" + index);
            if (source != null)
            {
                GltfInteractivityVariable value = new GltfInteractivityVariable();
                value.HasType = source.ContainsKey("type");
                if (value.HasType)
                {
                    value.Type = ReadIndex(
                        source["type"],
                        result,
                        pointer + "/" + index + "/type");
                }
                else
                {
                    AddMissingIssue(result, "type", pointer + "/" + index + "/type");
                }

                value.HasValue = source.ContainsKey("value");
                if (value.HasValue)
                {
                    value.Value = CloneArray(
                        source["value"],
                        result,
                        pointer + "/" + index + "/value");
                }

                if (source.ContainsKey("name"))
                {
                    value.Name = ReadString(
                        source["name"],
                        result,
                        pointer + "/" + index + "/name");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(
                    source,
                    ["type", "value", "name"]);
                target.Variables.Add(value);
            }

            index++;
        }
    }

    internal void ReadEvents(
        JsonNode? node,
        GltfInteractivityGraph target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonArray? values = ReadArray(node, result, pointer);
        if (values == null)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            JsonObject? source = ReadObject(values[index], result, pointer + "/" + index);
            if (source != null)
            {
                GltfInteractivityEvent value = new GltfInteractivityEvent();
                value.HasId = source.ContainsKey("id");
                if (value.HasId)
                {
                    value.Id = ReadString(
                        source["id"],
                        result,
                        pointer + "/" + index + "/id");
                }

                value.HasValues = source.ContainsKey("values");
                if (value.HasValues)
                {
                    ReadEventValues(
                        source["values"],
                        value,
                        result,
                        pointer + "/" + index + "/values");
                }

                if (source.ContainsKey("name"))
                {
                    value.Name = ReadString(
                        source["name"],
                        result,
                        pointer + "/" + index + "/name");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(
                    source,
                    ["id", "values", "name"]);
                target.Events.Add(value);
            }

            index++;
        }
    }

    internal void ReadEventValues(
        JsonNode? node,
        GltfInteractivityEvent target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonObject? values = ReadObject(node, result, pointer);
        if (values == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> member in values)
        {
            string memberPointer = pointer + "/" + Fallback.EscapePointerSegment(member.Key);
            JsonObject? source = ReadObject(member.Value, result, memberPointer);
            if (source != null)
            {
                GltfInteractivityEventValue value = new GltfInteractivityEventValue();
                value.HasType = source.ContainsKey("type");
                if (value.HasType)
                {
                    value.Type = ReadIndex(source["type"], result, memberPointer + "/type");
                }
                else
                {
                    AddMissingIssue(result, "type", memberPointer + "/type");
                }

                value.HasValue = source.ContainsKey("value");
                if (value.HasValue)
                {
                    value.Value = CloneArray(source["value"], result, memberPointer + "/value");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(source, ["type", "value"]);
                target.Values[member.Key] = value;
            }
        }
    }

    internal void ReadDeclarations(
        JsonNode? node,
        GltfInteractivityGraph target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonArray? values = ReadArray(node, result, pointer);
        if (values == null)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            string itemPointer = pointer + "/" + index;
            JsonObject? source = ReadObject(values[index], result, itemPointer);
            if (source != null)
            {
                GltfInteractivityDeclaration value = new GltfInteractivityDeclaration();
                value.HasOperation = source.ContainsKey("op");
                if (value.HasOperation)
                {
                    value.Operation = ReadString(source["op"], result, itemPointer + "/op");
                }
                else
                {
                    AddMissingIssue(result, "op", itemPointer + "/op");
                }

                value.HasInputValueSockets = source.ContainsKey("inputValueSockets");
                if (value.HasInputValueSockets)
                {
                    ReadDeclarationSockets(
                        source["inputValueSockets"],
                        value.InputValueSockets,
                        result,
                        itemPointer + "/inputValueSockets");
                }

                value.HasOutputValueSockets = source.ContainsKey("outputValueSockets");
                if (value.HasOutputValueSockets)
                {
                    ReadDeclarationSockets(
                        source["outputValueSockets"],
                        value.OutputValueSockets,
                        result,
                        itemPointer + "/outputValueSockets");
                }

                value.HasExtension = source.ContainsKey("extension");
                if (value.HasExtension)
                {
                    value.Extension = ReadString(
                        source["extension"],
                        result,
                        itemPointer + "/extension");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(
                    source,
                    ["op", "inputValueSockets", "outputValueSockets", "extension"]);
                target.Declarations.Add(value);
            }

            index++;
        }
    }

    internal void ReadDeclarationSockets(
        JsonNode? node,
        Dictionary<string, GltfInteractivityDeclarationValueSocket> target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonObject? values = ReadObject(node, result, pointer);
        if (values == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> member in values)
        {
            string memberPointer = pointer + "/" + Fallback.EscapePointerSegment(member.Key);
            JsonObject? source = ReadObject(member.Value, result, memberPointer);
            if (source != null)
            {
                GltfInteractivityDeclarationValueSocket value =
                    new GltfInteractivityDeclarationValueSocket();
                value.HasType = source.ContainsKey("type");
                if (value.HasType)
                {
                    value.Type = ReadIndex(source["type"], result, memberPointer + "/type");
                }
                else
                {
                    AddMissingIssue(result, "type", memberPointer + "/type");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(source, ["type"]);
                target[member.Key] = value;
            }
        }
    }

    internal void ReadNodes(
        JsonNode? node,
        GltfInteractivityGraph target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonArray? values = ReadArray(node, result, pointer);
        if (values == null)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            string itemPointer = pointer + "/" + index;
            JsonObject? source = ReadObject(values[index], result, itemPointer);
            if (source != null)
            {
                GltfInteractivityNode value = new GltfInteractivityNode();
                value.HasDeclaration = source.ContainsKey("declaration");
                if (value.HasDeclaration)
                {
                    value.Declaration = ReadIndex(
                        source["declaration"],
                        result,
                        itemPointer + "/declaration");
                }
                else
                {
                    AddMissingIssue(result, "declaration", itemPointer + "/declaration");
                }

                value.HasConfiguration = source.ContainsKey("configuration");
                if (value.HasConfiguration)
                {
                    ReadConfigurations(
                        source["configuration"],
                        value,
                        result,
                        itemPointer + "/configuration");
                }

                value.HasValues = source.ContainsKey("values");
                if (value.HasValues)
                {
                    ReadValueSockets(
                        source["values"],
                        value,
                        result,
                        itemPointer + "/values");
                }

                value.HasFlows = source.ContainsKey("flows");
                if (value.HasFlows)
                {
                    ReadFlowSockets(
                        source["flows"],
                        value,
                        result,
                        itemPointer + "/flows");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(
                    source,
                    ["declaration", "configuration", "values", "flows"]);
                target.Nodes.Add(value);
            }

            index++;
        }
    }

    internal void ReadConfigurations(
        JsonNode? node,
        GltfInteractivityNode target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonObject? values = ReadObject(node, result, pointer);
        if (values == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> member in values)
        {
            string memberPointer = pointer + "/" + Fallback.EscapePointerSegment(member.Key);
            JsonObject? source = ReadObject(member.Value, result, memberPointer);
            if (source != null)
            {
                GltfInteractivityConfiguration value = new GltfInteractivityConfiguration();
                value.HasValue = source.ContainsKey("value");
                if (value.HasValue)
                {
                    value.Value = CloneArray(source["value"], result, memberPointer + "/value");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(source, ["value"]);
                target.Configuration[member.Key] = value;
            }
        }
    }

    internal void ReadValueSockets(
        JsonNode? node,
        GltfInteractivityNode target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonObject? values = ReadObject(node, result, pointer);
        if (values == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> member in values)
        {
            string memberPointer = pointer + "/" + Fallback.EscapePointerSegment(member.Key);
            JsonObject? source = ReadObject(member.Value, result, memberPointer);
            if (source != null)
            {
                GltfInteractivityValueSocket value = new GltfInteractivityValueSocket();
                value.HasNode = source.ContainsKey("node");
                if (value.HasNode)
                {
                    value.Node = ReadIndex(source["node"], result, memberPointer + "/node");
                }

                value.HasSocket = source.ContainsKey("socket");
                if (value.HasSocket)
                {
                    value.Socket = ReadString(
                        source["socket"],
                        result,
                        memberPointer + "/socket");
                }

                value.HasType = source.ContainsKey("type");
                if (value.HasType)
                {
                    value.Type = ReadIndex(source["type"], result, memberPointer + "/type");
                }

                value.HasValue = source.ContainsKey("value");
                if (value.HasValue)
                {
                    value.Value = CloneArray(source["value"], result, memberPointer + "/value");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(
                    source,
                    ["node", "socket", "type", "value"]);
                target.Values[member.Key] = value;
            }
        }
    }

    internal void ReadFlowSockets(
        JsonNode? node,
        GltfInteractivityNode target,
        GltfExtensionResult result,
        string pointer)
    {
        JsonObject? values = ReadObject(node, result, pointer);
        if (values == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> member in values)
        {
            string memberPointer = pointer + "/" + Fallback.EscapePointerSegment(member.Key);
            JsonObject? source = ReadObject(member.Value, result, memberPointer);
            if (source != null)
            {
                GltfInteractivityFlowSocket value = new GltfInteractivityFlowSocket();
                value.HasNode = source.ContainsKey("node");
                if (value.HasNode)
                {
                    value.Node = ReadIndex(source["node"], result, memberPointer + "/node");
                }
                else
                {
                    AddMissingIssue(result, "node", memberPointer + "/node");
                }

                value.HasSocket = source.ContainsKey("socket");
                if (value.HasSocket)
                {
                    value.Socket = ReadString(
                        source["socket"],
                        result,
                        memberPointer + "/socket");
                }

                value.PropertyJson = PropertyJson.ReadPropertyData(source, ["node", "socket"]);
                target.Flows[member.Key] = value;
            }
        }
    }

    internal JsonObject? ReadObject(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Object)
        {
            AddIssue(result, GltfIssueCodes.TypeMismatch, "Expected a JSON object.", pointer);
            return null;
        }

        return node.AsObject();
    }

    internal JsonArray? ReadArray(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Array)
        {
            AddIssue(result, GltfIssueCodes.ArrayTypeMismatch, "Expected a JSON array.", pointer);
            return null;
        }

        return node.AsArray();
    }

    internal JsonArray? CloneArray(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        JsonArray? source = ReadArray(node, result, pointer);
        if (source == null)
        {
            return null;
        }

        JsonNode? clone = JsonValueCloner.CloneNode(source);
        if (clone == null)
        {
            return null;
        }

        return clone.AsArray();
    }

    internal string ReadString(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.String)
        {
            AddIssue(result, GltfIssueCodes.TypeMismatch, "Expected a string.", pointer);
            return "";
        }

        return node.GetValue<string>();
    }

    internal int ReadIndex(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Expected a non-negative index.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        double value = double.Parse(
            node.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(value)
            || value != System.Math.Truncate(value)
            || value < 0.0
            || value > int.MaxValue)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Expected a non-negative 32-bit index.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        return (int)value;
    }

    internal string CreatePointer(GltfExtensionContext context)
    {
        if (context.ObjectLocation == "mesh.primitive"
            && context.HasObjectIndex
            && context.HasSubObjectIndex)
        {
            return "/meshes/" + context.ObjectIndex
                + "/primitives/" + context.SubObjectIndex
                + "/extensions/" + context.Name;
        }

        return Fallback.CreateExtensionPointer(context);
    }

    internal void AddMissingIssue(
        GltfExtensionResult result,
        string member,
        string pointer)
    {
        AddIssue(
            result,
            GltfIssueCodes.UndefinedProperty,
            $"The required {member} member is missing.",
            pointer);
    }

    internal void AddIssue(
        GltfExtensionResult result,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.Status = GltfOperationStatus.Invalid;
    }
}
