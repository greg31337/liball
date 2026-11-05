using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace jmap;

internal class PatchApplicator
{
    internal PatchValidator Validator = new PatchValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);

    internal JsonElement ApplyPatch(JsonElement target, JmapPatchObject? patch, Dictionary<string, JsonElement>? defaultValues)
    {
        List<string> invalid = Validator.ValidatePatch(target, patch);
        if (invalid.Count != 0)
        {
            throw new JmapValidationException("The PatchObject contains invalid or conflicting paths.", invalid, default(JsonElement));
        }

        if (patch == null || patch.Values == null)
        {
            throw new JmapValidationException("A PatchObject is required.", new List<string> { "" }, default(JsonElement));
        }

        JsonNode? parsedTarget = JsonNode.Parse(target.GetRawText());
        if (parsedTarget == null)
        {
            throw new JmapValidationException("The object to patch is not a JSON object.", new List<string> { "" }, default(JsonElement));
        }

        JsonObject mutableTarget = (JsonObject)parsedTarget;
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            ApplyPatchPath(mutableTarget, item.Key, item.Value, defaultValues);
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(mutableTarget.ToJsonString());
        return Reader.ReadUtf8Memory(utf8);
    }

    internal void ApplyPatchPath(JsonObject target, string path, JsonElement value, Dictionary<string, JsonElement>? defaultValues)
    {
        List<string> tokens = Validator.Pointers.ParsePointer("/" + path);
        JsonObject parent = target;
        int index = 0;
        while (index < tokens.Count - 1)
        {
            JsonNode? child = parent[tokens[index]];
            if (child == null)
            {
                throw new JmapValidationException("A PatchObject parent path does not exist.", new List<string> { path }, default(JsonElement));
            }

            parent = (JsonObject)child;
            index = index + 1;
        }

        string memberName = tokens[tokens.Count - 1];
        if (value.ValueKind == JsonValueKind.Null)
        {
            if (defaultValues != null && defaultValues.ContainsKey(path))
            {
                JsonElement defaultValue = defaultValues[path];
                if (defaultValue.ValueKind == JsonValueKind.Undefined)
                {
                    throw new JmapValidationException("A PatchObject default value is undefined.", new List<string> { path }, default(JsonElement));
                }

                parent[memberName] = ParseElementNode(defaultValue);
            }
            else
            {
                parent.Remove(memberName);
            }
        }
        else
        {
            parent[memberName] = ParseElementNode(value);
        }
    }

    internal JsonNode? ParseElementNode(JsonElement value)
    {
        return JsonNode.Parse(value.GetRawText());
    }
}
