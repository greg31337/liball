using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class StatePathTokenResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Token = string.Empty;
}

internal class StatePathResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string[] Tokens = Array.Empty<string>();
}

internal class StateArrayIndexResult
{
    internal bool Success;
    internal bool Append;
    internal string Code = string.Empty;
    internal int Index;
}

internal class StatePathValueResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiValue? Value;
}

internal class StatePathParentResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiValue? Parent;
    internal string Token = string.Empty;
}

internal class StatePatchStepResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal UiValue? Value;
}

internal class StatePatchApplicationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal int OperationIndex = -1;
    internal UiValue? Value;
    internal UiStatePatch[] Patches = Array.Empty<UiStatePatch>();
}

internal class StatePatchCore
{
    internal const int MaximumPatchOperationCount = 256;
    internal const int MaximumStatePathLength = 4096;
    internal const int MaximumStatePathSegmentCount = 128;

    internal StatePatchApplicationResult ApplyStatePatches(StateStoreCore store, UiValue? source, UiStatePatch[]? patches)
    {
        if (source == null)
        {
            return CreateStatePatchFailure("state.null-value", -1);
        }

        string sourceCode = store.ValidateStateValue(source);
        if (sourceCode.Length > 0)
        {
            return CreateStatePatchFailure(sourceCode, -1);
        }

        if (patches == null)
        {
            return CreateStatePatchFailure("state.null-patches", -1);
        }

        if (patches.Length > MaximumPatchOperationCount)
        {
            return CreateStatePatchFailure("state.too-many-patches", -1);
        }

        UiValue working = store.CopyStateValue(source);
        UiStatePatch[] normalizedPatches = new UiStatePatch[patches.Length];
        bool applied = false;
        int index = 0;
        while (index < patches.Length)
        {
            UiStatePatch? patch = patches[index];
            if (patch == null)
            {
                return CreateStatePatchFailure("state.null-patch", index);
            }

            StatePathResult path = ParseStatePath(patch.Path);
            if (!path.Success)
            {
                return CreateStatePatchFailure(path.Code, index);
            }

            string operation = patch.Operation ?? string.Empty;
            StatePatchStepResult step = ApplyStatePatchStep(store, working, operation, path.Tokens, patch);
            if (!step.Success || step.Value == null)
            {
                return CreateStatePatchFailure(step.Code, index);
            }

            string resultCode = store.ValidateStateValue(step.Value);
            if (resultCode.Length > 0)
            {
                return CreateStatePatchFailure(resultCode, index);
            }

            working = step.Value;
            applied = applied || step.Applied;
            normalizedPatches[index] = CopyAppliedStatePatch(store, patch);
            index++;
        }

        StatePatchApplicationResult result = new StatePatchApplicationResult();
        result.Success = true;
        result.Applied = applied;
        result.Value = working;
        result.Patches = normalizedPatches;
        return result;
    }

    internal StatePatchStepResult ApplyStatePatchStep(StateStoreCore store, UiValue root, string operation, string[] tokens, UiStatePatch patch)
    {
        switch (operation)
        {
            case "add":
                if (patch.Value == null)
                {
                    return CreateStatePatchStepFailure("state.null-patch-value");
                }

                string addValueCode = store.ValidateStateValue(patch.Value);
                if (addValueCode.Length > 0)
                {
                    return CreateStatePatchStepFailure(addValueCode);
                }

                return AddStatePathValue(store, root, tokens, patch.Value);
            case "remove":
                return RemoveStatePathValue(root, tokens);
            case "replace":
                if (patch.Value == null)
                {
                    return CreateStatePatchStepFailure("state.null-patch-value");
                }

                string replaceValueCode = store.ValidateStateValue(patch.Value);
                if (replaceValueCode.Length > 0)
                {
                    return CreateStatePatchStepFailure(replaceValueCode);
                }

                return ReplaceStatePathValue(store, root, tokens, patch.Value);
            case "move":
                return MoveStatePathValue(store, root, tokens, patch.FromPath);
            case "copy":
                return CopyStatePathValue(store, root, tokens, patch.FromPath);
            case "test":
                if (patch.Value == null)
                {
                    return CreateStatePatchStepFailure("state.null-patch-value");
                }

                string testValueCode = store.ValidateStateValue(patch.Value);
                if (testValueCode.Length > 0)
                {
                    return CreateStatePatchStepFailure(testValueCode);
                }

                return TestStatePathValue(store, root, tokens, patch.Value);
            default:
                return CreateStatePatchStepFailure("state.invalid-patch-operation");
        }
    }

    internal StatePatchStepResult AddStatePathValue(StateStoreCore store, UiValue root, string[] tokens, UiValue value)
    {
        UiValue copiedValue = store.CopyStateValue(value);
        if (tokens.Length == 0)
        {
            return CreateStatePatchStepSuccess(copiedValue, true);
        }

        StatePathParentResult location = ResolveStatePathParent(root, tokens);
        if (!location.Success || location.Parent == null)
        {
            return CreateStatePatchStepFailure(location.Code);
        }

        if (location.Parent.Kind == UiValueKind.Object)
        {
            UiValueEntry[] entries = location.Parent.ObjectValue ?? Array.Empty<UiValueEntry>();
            int entryIndex = FindStateObjectEntryIndex(entries, location.Token);
            if (entryIndex >= 0)
            {
                entries[entryIndex].Value = copiedValue;
            }
            else
            {
                UiValueEntry[] expanded = new UiValueEntry[entries.Length + 1];
                Array.Copy(entries, expanded, entries.Length);
                expanded[entries.Length] = new UiValueEntry { Key = location.Token, Value = copiedValue };
                location.Parent.ObjectValue = expanded;
            }

            return CreateStatePatchStepSuccess(root, true);
        }

        if (location.Parent.Kind == UiValueKind.Array)
        {
            UiValue[] items = location.Parent.ArrayValue ?? Array.Empty<UiValue>();
            StateArrayIndexResult arrayIndex = ParseStateArrayIndex(location.Token, items.Length, true);
            if (!arrayIndex.Success)
            {
                return CreateStatePatchStepFailure(arrayIndex.Code);
            }

            UiValue[] expanded = new UiValue[items.Length + 1];
            int index = 0;
            while (index < arrayIndex.Index)
            {
                expanded[index] = items[index];
                index++;
            }

            expanded[arrayIndex.Index] = copiedValue;
            while (index < items.Length)
            {
                expanded[index + 1] = items[index];
                index++;
            }

            location.Parent.ArrayValue = expanded;
            return CreateStatePatchStepSuccess(root, true);
        }

        return CreateStatePatchStepFailure("state.path-not-container");
    }

    internal StatePatchStepResult RemoveStatePathValue(UiValue root, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return CreateStatePatchStepFailure("state.cannot-remove-root");
        }

        StatePathParentResult location = ResolveStatePathParent(root, tokens);
        if (!location.Success || location.Parent == null)
        {
            return CreateStatePatchStepFailure(location.Code);
        }

        if (location.Parent.Kind == UiValueKind.Object)
        {
            UiValueEntry[] entries = location.Parent.ObjectValue ?? Array.Empty<UiValueEntry>();
            int entryIndex = FindStateObjectEntryIndex(entries, location.Token);
            if (entryIndex < 0)
            {
                return CreateStatePatchStepFailure("state.path-not-found");
            }

            UiValueEntry[] reduced = new UiValueEntry[entries.Length - 1];
            int sourceIndex = 0;
            int targetIndex = 0;
            while (sourceIndex < entries.Length)
            {
                if (sourceIndex != entryIndex)
                {
                    reduced[targetIndex] = entries[sourceIndex];
                    targetIndex++;
                }

                sourceIndex++;
            }

            location.Parent.ObjectValue = reduced;
            return CreateStatePatchStepSuccess(root, true);
        }

        if (location.Parent.Kind == UiValueKind.Array)
        {
            UiValue[] items = location.Parent.ArrayValue ?? Array.Empty<UiValue>();
            StateArrayIndexResult arrayIndex = ParseStateArrayIndex(location.Token, items.Length, false);
            if (!arrayIndex.Success)
            {
                return CreateStatePatchStepFailure(arrayIndex.Code);
            }

            UiValue[] reduced = new UiValue[items.Length - 1];
            int sourceIndex = 0;
            int targetIndex = 0;
            while (sourceIndex < items.Length)
            {
                if (sourceIndex != arrayIndex.Index)
                {
                    reduced[targetIndex] = items[sourceIndex];
                    targetIndex++;
                }

                sourceIndex++;
            }

            location.Parent.ArrayValue = reduced;
            return CreateStatePatchStepSuccess(root, true);
        }

        return CreateStatePatchStepFailure("state.path-not-container");
    }

    internal StatePatchStepResult ReplaceStatePathValue(StateStoreCore store, UiValue root, string[] tokens, UiValue value)
    {
        UiValue copiedValue = store.CopyStateValue(value);
        if (tokens.Length == 0)
        {
            return CreateStatePatchStepSuccess(copiedValue, true);
        }

        StatePathParentResult location = ResolveStatePathParent(root, tokens);
        if (!location.Success || location.Parent == null)
        {
            return CreateStatePatchStepFailure(location.Code);
        }

        if (location.Parent.Kind == UiValueKind.Object)
        {
            UiValueEntry[] entries = location.Parent.ObjectValue ?? Array.Empty<UiValueEntry>();
            int entryIndex = FindStateObjectEntryIndex(entries, location.Token);
            if (entryIndex < 0)
            {
                return CreateStatePatchStepFailure("state.path-not-found");
            }

            entries[entryIndex].Value = copiedValue;
            return CreateStatePatchStepSuccess(root, true);
        }

        if (location.Parent.Kind == UiValueKind.Array)
        {
            UiValue[] items = location.Parent.ArrayValue ?? Array.Empty<UiValue>();
            StateArrayIndexResult arrayIndex = ParseStateArrayIndex(location.Token, items.Length, false);
            if (!arrayIndex.Success)
            {
                return CreateStatePatchStepFailure(arrayIndex.Code);
            }

            items[arrayIndex.Index] = copiedValue;
            return CreateStatePatchStepSuccess(root, true);
        }

        return CreateStatePatchStepFailure("state.path-not-container");
    }

    internal StatePatchStepResult MoveStatePathValue(StateStoreCore store, UiValue root, string[] targetTokens, string? fromPath)
    {
        if (fromPath == null)
        {
            return CreateStatePatchStepFailure("state.missing-from-path");
        }

        StatePathResult sourcePath = ParseStatePath(fromPath);
        if (!sourcePath.Success)
        {
            return CreateStatePatchStepFailure(sourcePath.Code);
        }

        if (StatePathTokensEqual(sourcePath.Tokens, targetTokens))
        {
            return CreateStatePatchStepSuccess(root, false);
        }

        if (StatePathIsDescendant(sourcePath.Tokens, targetTokens))
        {
            return CreateStatePatchStepFailure("state.patch-descendant-move");
        }

        StatePathValueResult source = ResolveStatePathValue(root, sourcePath.Tokens);
        if (!source.Success || source.Value == null)
        {
            return CreateStatePatchStepFailure(source.Code);
        }

        UiValue copiedValue = store.CopyStateValue(source.Value);
        StatePatchStepResult removed = RemoveStatePathValue(root, sourcePath.Tokens);
        if (!removed.Success || removed.Value == null)
        {
            return removed;
        }

        StatePatchStepResult added = AddStatePathValue(store, removed.Value, targetTokens, copiedValue);
        if (!added.Success)
        {
            return added;
        }

        added.Applied = true;
        return added;
    }

    internal StatePatchStepResult CopyStatePathValue(StateStoreCore store, UiValue root, string[] targetTokens, string? fromPath)
    {
        if (fromPath == null)
        {
            return CreateStatePatchStepFailure("state.missing-from-path");
        }

        StatePathResult sourcePath = ParseStatePath(fromPath);
        if (!sourcePath.Success)
        {
            return CreateStatePatchStepFailure(sourcePath.Code);
        }

        StatePathValueResult source = ResolveStatePathValue(root, sourcePath.Tokens);
        if (!source.Success || source.Value == null)
        {
            return CreateStatePatchStepFailure(source.Code);
        }

        return AddStatePathValue(store, root, targetTokens, source.Value);
    }

    internal StatePatchStepResult TestStatePathValue(StateStoreCore store, UiValue root, string[] tokens, UiValue expected)
    {
        StatePathValueResult actual = ResolveStatePathValue(root, tokens);
        if (!actual.Success || actual.Value == null)
        {
            return CreateStatePatchStepFailure(actual.Code);
        }

        if (!store.StateValuesEqual(actual.Value, expected))
        {
            return CreateStatePatchStepFailure("state.patch-test-failed");
        }

        return CreateStatePatchStepSuccess(root, false);
    }

    internal StatePathResult ParseStatePath(string? path)
    {
        if (path == null)
        {
            return CreateStatePathFailure("state.invalid-path");
        }

        if (path.Length > MaximumStatePathLength)
        {
            return CreateStatePathFailure("state.path-too-long");
        }

        if (path.Length == 0)
        {
            return new StatePathResult { Success = true };
        }

        if (path[0] != '/')
        {
            return CreateStatePathFailure("state.invalid-path");
        }

        List<string> tokens = new List<string>();
        int tokenStart = 1;
        while (tokenStart <= path.Length)
        {
            if (tokens.Count == MaximumStatePathSegmentCount)
            {
                return CreateStatePathFailure("state.path-too-deep");
            }

            int separatorIndex = path.IndexOf('/', tokenStart);
            int tokenEnd = separatorIndex < 0 ? path.Length : separatorIndex;
            StatePathTokenResult token = DecodeStatePathToken(path.Substring(tokenStart, tokenEnd - tokenStart));
            if (!token.Success)
            {
                return CreateStatePathFailure(token.Code);
            }

            tokens.Add(token.Token);
            if (separatorIndex < 0)
            {
                break;
            }

            tokenStart = separatorIndex + 1;
        }

        return new StatePathResult { Success = true, Tokens = tokens.ToArray() };
    }

    internal StatePathTokenResult DecodeStatePathToken(string encoded)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < encoded.Length)
        {
            char character = encoded[index];
            if (character != '~')
            {
                result.Append(character);
                index++;
                continue;
            }

            if (index + 1 >= encoded.Length)
            {
                return CreateStatePathTokenFailure("state.invalid-path-escape");
            }

            char escaped = encoded[index + 1];
            if (escaped == '0')
            {
                result.Append('~');
            }
            else if (escaped == '1')
            {
                result.Append('/');
            }
            else
            {
                return CreateStatePathTokenFailure("state.invalid-path-escape");
            }

            index += 2;
        }

        return new StatePathTokenResult { Success = true, Token = result.ToString() };
    }

    internal StatePathValueResult ResolveStatePathValue(UiValue root, string[] tokens)
    {
        UiValue current = root;
        int index = 0;
        while (index < tokens.Length)
        {
            if (current.Kind == UiValueKind.Object)
            {
                UiValueEntry[] entries = current.ObjectValue ?? Array.Empty<UiValueEntry>();
                int entryIndex = FindStateObjectEntryIndex(entries, tokens[index]);
                if (entryIndex < 0)
                {
                    return CreateStatePathValueFailure("state.path-not-found");
                }

                current = entries[entryIndex].Value;
            }
            else if (current.Kind == UiValueKind.Array)
            {
                UiValue[] items = current.ArrayValue ?? Array.Empty<UiValue>();
                StateArrayIndexResult arrayIndex = ParseStateArrayIndex(tokens[index], items.Length, false);
                if (!arrayIndex.Success)
                {
                    return CreateStatePathValueFailure(arrayIndex.Code);
                }

                current = items[arrayIndex.Index];
            }
            else
            {
                return CreateStatePathValueFailure("state.path-not-container");
            }

            index++;
        }

        return new StatePathValueResult { Success = true, Value = current };
    }

    internal StatePathParentResult ResolveStatePathParent(UiValue root, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return CreateStatePathParentFailure("state.invalid-path");
        }

        string[] parentTokens = new string[tokens.Length - 1];
        int index = 0;
        while (index < parentTokens.Length)
        {
            parentTokens[index] = tokens[index];
            index++;
        }

        StatePathValueResult parent = ResolveStatePathValue(root, parentTokens);
        if (!parent.Success || parent.Value == null)
        {
            return CreateStatePathParentFailure(parent.Code);
        }

        StatePathParentResult result = new StatePathParentResult();
        result.Success = true;
        result.Parent = parent.Value;
        result.Token = tokens[tokens.Length - 1];
        return result;
    }

    internal StateArrayIndexResult ParseStateArrayIndex(string token, int itemCount, bool allowAppend)
    {
        if (allowAppend && token == "-")
        {
            return new StateArrayIndexResult { Success = true, Append = true, Index = itemCount };
        }

        if (token.Length == 0 || token == "-" || token.Length > 1 && token[0] == '0')
        {
            return CreateStateArrayIndexFailure("state.invalid-array-index");
        }

        int value = 0;
        int index = 0;
        while (index < token.Length)
        {
            char character = token[index];
            if (character < '0' || character > '9')
            {
                return CreateStateArrayIndexFailure("state.invalid-array-index");
            }

            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return CreateStateArrayIndexFailure("state.invalid-array-index");
            }

            value = value * 10 + digit;
            index++;
        }

        if (allowAppend && value > itemCount || !allowAppend && value >= itemCount)
        {
            return CreateStateArrayIndexFailure("state.path-not-found");
        }

        return new StateArrayIndexResult { Success = true, Index = value };
    }

    internal int FindStateObjectEntryIndex(UiValueEntry[] entries, string key)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (ValueComparer.TextEquals(entries[index].Key, key))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool StatePathTokensEqual(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!ValueComparer.TextEquals(left[index], right[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool StatePathIsDescendant(string[] parent, string[] candidate)
    {
        if (candidate.Length <= parent.Length)
        {
            return false;
        }

        int index = 0;
        while (index < parent.Length)
        {
            if (!ValueComparer.TextEquals(parent[index], candidate[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal UiStatePatch CopyAppliedStatePatch(StateStoreCore store, UiStatePatch source)
    {
        UiStatePatch result = new UiStatePatch();
        result.Operation = source.Operation;
        result.Path = source.Path;
        result.FromPath = source.FromPath;
        if (source.Value != null)
        {
            result.Value = store.CopyStateValue(source.Value);
        }

        return result;
    }

    internal StatePatchApplicationResult CreateStatePatchFailure(string code, int operationIndex)
    {
        return new StatePatchApplicationResult { Success = false, Code = code, OperationIndex = operationIndex };
    }

    internal StatePatchStepResult CreateStatePatchStepSuccess(UiValue value, bool applied)
    {
        return new StatePatchStepResult { Success = true, Applied = applied, Value = value };
    }

    internal StatePatchStepResult CreateStatePatchStepFailure(string code)
    {
        return new StatePatchStepResult { Success = false, Code = code };
    }

    internal StatePathResult CreateStatePathFailure(string code)
    {
        return new StatePathResult { Success = false, Code = code };
    }

    internal StatePathTokenResult CreateStatePathTokenFailure(string code)
    {
        return new StatePathTokenResult { Success = false, Code = code };
    }

    internal StatePathValueResult CreateStatePathValueFailure(string code)
    {
        return new StatePathValueResult { Success = false, Code = code };
    }

    internal StatePathParentResult CreateStatePathParentFailure(string code)
    {
        return new StatePathParentResult { Success = false, Code = code };
    }

    internal StateArrayIndexResult CreateStateArrayIndexFailure(string code)
    {
        return new StateArrayIndexResult { Success = false, Code = code };
    }
}
