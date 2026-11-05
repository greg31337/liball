using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class PatchValidator
{
    internal JsonPointer Pointers = new JsonPointer();

    internal List<string> ValidatePatchPaths(JsonElement target, JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null || patch.Values == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (target.ValueKind != JsonValueKind.Object)
        {
            invalid.Add("");
            return invalid;
        }

        if (patch.Values.Count > SecurityLimits.MaximumPatchPaths)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (!IsValidPatchPath(target, path))
            {
                invalid.Add(path);
            }
        }

        return invalid;
    }

    internal List<string> ValidatePatch(JsonElement target, JmapPatchObject? patch)
    {
        List<string> invalid = ValidatePatchPaths(target, patch);
        if (patch == null || patch.Values == null)
        {
            return invalid;
        }

        List<string> conflicts = FindConflictingPaths(patch);
        foreach (string conflict in conflicts)
        {
            AddInvalidPath(invalid, conflict);
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Value.ValueKind == JsonValueKind.Undefined)
            {
                AddInvalidPath(invalid, item.Key);
            }
        }

        return invalid;
    }

    internal List<string> FindConflictingPaths(JmapPatchObject? patch)
    {
        List<string> paths = new List<string>();
        List<List<string>> tokens = new List<List<string>>();
        List<string> conflicts = new List<string>();
        if (patch == null || patch.Values == null)
        {
            return conflicts;
        }

        if (patch.Values.Count > SecurityLimits.MaximumPatchPaths)
        {
            conflicts.Add("");
            return conflicts;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (IsPatchPath(path))
            {
                string pointer = "/" + path;
                paths.Add(path);
                tokens.Add(Pointers.ParsePointer(pointer));
            }
        }

        int leftIndex = 0;
        while (leftIndex < paths.Count)
        {
            int rightIndex = leftIndex + 1;
            while (rightIndex < paths.Count)
            {
                bool conflict = IsProperTokenPrefix(tokens[leftIndex], tokens[rightIndex]);
                if (!conflict)
                {
                    conflict = IsProperTokenPrefix(tokens[rightIndex], tokens[leftIndex]);
                }

                if (conflict)
                {
                    AddInvalidPath(conflicts, paths[leftIndex]);
                    AddInvalidPath(conflicts, paths[rightIndex]);
                }

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return conflicts;
    }

    internal bool IsProperTokenPrefix(List<string> prefix, List<string> path)
    {
        if (prefix.Count >= path.Count)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Count)
        {
            if (prefix[index] != path[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void AddInvalidPath(List<string> invalid, string path)
    {
        if (!invalid.Contains(path))
        {
            invalid.Add(path);
        }
    }

    internal bool IsValidPatchPath(JsonElement target, string? path)
    {
        if (!IsPatchPath(path) || path == null)
        {
            return false;
        }

        string pointer = "/" + path;
        if (!Pointers.IsPointer(pointer))
        {
            return false;
        }

        List<string> tokens = Pointers.ParsePointer(pointer);
        JsonElement current = target;
        int index = 0;
        while (index < tokens.Count - 1)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            current = ReadUniqueObjectMember(current, tokens[index]);
            if (current.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            index = index + 1;
        }

        if (current.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return CountObjectMemberMatches(current, tokens[tokens.Count - 1]) <= 1;
    }

    internal bool IsPatchPath(string? path)
    {
        return path != null && path.Length != 0 && Pointers.IsPointer("/" + path);
    }

    internal JsonElement ReadUniqueObjectMember(JsonElement value, string name)
    {
        JsonElement result = default(JsonElement);
        int matches = 0;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                result = property.Value;
                matches = matches + 1;
            }
        }

        if (matches == 1)
        {
            return result;
        }

        return default(JsonElement);
    }

    internal int CountObjectMemberMatches(JsonElement value, string name)
    {
        int matches = 0;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                matches = matches + 1;
            }
        }

        return matches;
    }
}
