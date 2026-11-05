using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ThemeResolution
{
    internal string ThemeKey = string.Empty;
    internal string? VariantKey;
    internal UiThemeMode Mode;
    internal UiValueEntry[] Tokens = Array.Empty<UiValueEntry>();
    internal DiagnosticEntry[] Diagnostics = Array.Empty<DiagnosticEntry>();
    internal bool Success;
}

internal class ThemeVariantState
{
    internal UiThemeVariant Variant;
    internal string Location;
    internal int Status;

    internal ThemeVariantState(UiThemeVariant variant, string location)
    {
        Variant = variant;
        Location = location;
        Status = 0;
    }
}

internal class ThemeTokenState
{
    internal string Key;
    internal UiValue Source;
    internal UiValue? Resolved;
    internal string Location;
    internal int Status;

    internal ThemeTokenState(string key, UiValue source, string location)
    {
        Key = key;
        Source = source;
        Location = location;
        Status = 0;
    }
}

internal class ThemeResolver
{
    internal DiagnosticSink Diagnostics;
    internal List<ThemeVariantState> VariantStates;
    internal List<ThemeTokenState> TokenStates;

    internal ThemeResolver(int maximumDiagnosticCount)
    {
        Diagnostics = new DiagnosticSink(maximumDiagnosticCount);
        VariantStates = new List<ThemeVariantState>();
        TokenStates = new List<ThemeTokenState>();
    }

    internal ThemeResolution ResolveTheme(UiTheme? theme, UiThemeMode hostMode, string? requestedVariantKey, UiValueEntry[]? hostOverrides)
    {
        Diagnostics.Clear();
        VariantStates.Clear();
        TokenStates.Clear();
        ThemeResolution result = new ThemeResolution();
        if (theme == null)
        {
            AddThemeError("theme.null", "The theme cannot be null.", "/theme");
            return CompleteResolution(result);
        }

        result.ThemeKey = theme.Key;
        if (string.IsNullOrWhiteSpace(theme.Key))
        {
            AddThemeError("theme.invalid-key", "The theme key cannot be empty.", "/theme/key");
        }

        UiThemeMode selectedMode = SelectThemeMode(theme.Mode, hostMode);
        BuildVariantStates(theme.Variants);
        ValidateVariantParents();
        ThemeVariantState? selectedVariant = SelectVariant(selectedMode, requestedVariantKey);
        if (selectedVariant != null && selectedVariant.Variant.Mode != UiThemeMode.Auto)
        {
            selectedMode = selectedVariant.Variant.Mode;
        }

        result.Mode = selectedMode;
        if (selectedVariant != null)
        {
            result.VariantKey = selectedVariant.Variant.Key;
        }

        AddTokenLayer(theme.Tokens, "/theme/tokens");
        AddVariantTokenLayers(selectedVariant);
        if (hostOverrides != null)
        {
            AddTokenLayer(hostOverrides, "/host-overrides");
        }

        ResolveAllTokens();
        result.Tokens = CreateResolvedTokens();
        return CompleteResolution(result);
    }

    internal ThemeResolution CompleteResolution(ThemeResolution result)
    {
        result.Diagnostics = Diagnostics.CreateSnapshot();
        result.Success = !Diagnostics.HasErrors();
        return result;
    }

    internal UiThemeMode SelectThemeMode(UiThemeMode themeMode, UiThemeMode hostMode)
    {
        UiThemeMode checkedHostMode = hostMode;
        if (!IsSupportedThemeMode(hostMode))
        {
            AddThemeError("theme.invalid-host-mode", "The host theme mode is not supported.", "/host-mode");
            checkedHostMode = UiThemeMode.Light;
        }

        if (!IsSupportedThemeMode(themeMode))
        {
            AddThemeError("theme.invalid-mode", "The theme mode is not supported.", "/theme/mode");
            themeMode = UiThemeMode.Auto;
        }

        if (themeMode != UiThemeMode.Auto)
        {
            return themeMode;
        }

        if (checkedHostMode == UiThemeMode.Auto)
        {
            return UiThemeMode.Light;
        }

        return checkedHostMode;
    }

    internal bool IsSupportedThemeMode(UiThemeMode mode)
    {
        return mode == UiThemeMode.Auto || mode == UiThemeMode.Light || mode == UiThemeMode.Dark || mode == UiThemeMode.HighContrast;
    }

    internal void BuildVariantStates(UiThemeVariant[]? variants)
    {
        if (variants == null)
        {
            AddThemeError("theme.null-variants", "The theme variant collection cannot be null.", "/theme/variants");
            return;
        }

        int index = 0;
        while (index < variants.Length)
        {
            UiThemeVariant? variant = variants[index];
            string location = "/theme/variants/" + InvariantFormatter.FormatInt32(index);
            if (variant == null)
            {
                AddThemeError("theme.null-variant", "A theme variant cannot be null.", location);
            }
            else
            {
                ThemeVariantState state = new ThemeVariantState(variant, location);
                if (string.IsNullOrWhiteSpace(variant.Key))
                {
                    AddThemeError("theme.invalid-variant-key", "Theme variant keys cannot be empty.", location + "/key");
                    state.Status = 3;
                }

                if (!IsSupportedThemeMode(variant.Mode))
                {
                    AddThemeError("theme.invalid-variant-mode", "The theme variant mode is not supported.", location + "/mode");
                    state.Status = 3;
                }

                ThemeVariantState? existing = FindVariantState(variant.Key);
                if (existing != null)
                {
                    AddThemeError("theme.duplicate-variant", "Theme variant keys must be unique.", location + "/key");
                    existing.Status = 3;
                    state.Status = 3;
                }

                VariantStates.Add(state);
            }

            index++;
        }
    }

    internal void ValidateVariantParents()
    {
        int index = 0;
        while (index < VariantStates.Count)
        {
            ThemeVariantState state = VariantStates[index];
            if (state.Status == 0)
            {
                ResolveVariantParent(state);
            }

            index++;
        }
    }

    internal bool ResolveVariantParent(ThemeVariantState state)
    {
        if (state.Status == 2)
        {
            return true;
        }

        if (state.Status == 3)
        {
            return false;
        }

        if (state.Status == 1)
        {
            AddThemeError("theme.variant-cycle", "Theme variant inheritance cannot contain a cycle.", state.Location + "/parentKey");
            state.Status = 3;
            return false;
        }

        state.Status = 1;
        string? parentKey = state.Variant.ParentKey;
        if (parentKey != null)
        {
            if (string.IsNullOrWhiteSpace(parentKey))
            {
                AddThemeError("theme.invalid-parent", "A theme variant parent key cannot be empty.", state.Location + "/parentKey");
                state.Status = 3;
                return false;
            }

            ThemeVariantState? parent = FindVariantState(parentKey);
            if (parent == null)
            {
                AddThemeError("theme.missing-parent", "The parent theme variant does not exist.", state.Location + "/parentKey");
                state.Status = 3;
                return false;
            }

            if (!ResolveVariantParent(parent))
            {
                state.Status = 3;
                return false;
            }
        }

        state.Status = 2;
        return true;
    }

    internal ThemeVariantState? SelectVariant(UiThemeMode mode, string? requestedVariantKey)
    {
        if (requestedVariantKey != null)
        {
            if (string.IsNullOrWhiteSpace(requestedVariantKey))
            {
                AddThemeError("theme.invalid-requested-variant", "The requested theme variant key cannot be empty.", "/requested-variant");
                return null;
            }

            ThemeVariantState? requested = FindVariantState(requestedVariantKey);
            if (requested == null)
            {
                AddThemeError("theme.missing-variant", "The requested theme variant does not exist.", "/requested-variant");
                return null;
            }

            if (requested.Status != 2)
            {
                return null;
            }

            return requested;
        }

        ThemeVariantState? selected = FindVariantForMode(mode);
        if (selected == null)
        {
            selected = FindVariantForMode(UiThemeMode.Auto);
        }

        return selected;
    }

    internal ThemeVariantState? FindVariantState(string key)
    {
        int index = 0;
        while (index < VariantStates.Count)
        {
            if (string.Equals(VariantStates[index].Variant.Key, key, StringComparison.Ordinal))
            {
                return VariantStates[index];
            }

            index++;
        }

        return null;
    }

    internal ThemeVariantState? FindVariantForMode(UiThemeMode mode)
    {
        ThemeVariantState? selected = null;
        int index = 0;
        while (index < VariantStates.Count)
        {
            ThemeVariantState candidate = VariantStates[index];
            if (candidate.Status == 2 && candidate.Variant.Mode == mode)
            {
                if (selected == null || string.CompareOrdinal(candidate.Variant.Key, selected.Variant.Key) < 0)
                {
                    selected = candidate;
                }
            }

            index++;
        }

        return selected;
    }

    internal void AddVariantTokenLayers(ThemeVariantState? selectedVariant)
    {
        if (selectedVariant == null)
        {
            return;
        }

        List<ThemeVariantState> chain = new List<ThemeVariantState>();
        ThemeVariantState? current = selectedVariant;
        while (current != null)
        {
            chain.Add(current);
            string? parentKey = current.Variant.ParentKey;
            if (parentKey == null)
            {
                current = null;
            }
            else
            {
                current = FindVariantState(parentKey);
            }
        }

        int index = chain.Count - 1;
        while (index >= 0)
        {
            ThemeVariantState state = chain[index];
            AddTokenLayer(state.Variant.Tokens, state.Location + "/tokens");
            index--;
        }
    }

    internal void AddTokenLayer(UiValueEntry[]? entries, string location)
    {
        if (entries == null)
        {
            AddThemeError("theme.null-tokens", "A theme token collection cannot be null.", location);
            return;
        }

        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry? entry = entries[index];
            string entryLocation = location + "/" + InvariantFormatter.FormatInt32(index);
            if (entry == null)
            {
                AddThemeError("theme.null-token", "A theme token cannot be null.", entryLocation);
            }
            else if (string.IsNullOrWhiteSpace(entry.Key))
            {
                AddThemeError("theme.invalid-token-key", "Theme token keys cannot be empty.", entryLocation + "/key");
            }
            else if (entry.Value == null)
            {
                AddThemeError("theme.null-token-value", "A theme token value cannot be null.", entryLocation + "/value");
            }
            else if (CountTokenKey(entries, entry.Key) > 1)
            {
                if (IsFirstTokenKey(entries, index, entry.Key))
                {
                    AddThemeError("theme.duplicate-token", "Theme token keys must be unique within a layer.", entryLocation + "/key");
                }
            }
            else
            {
                SetTokenState(entry.Key, entry.Value, entryLocation);
            }

            index++;
        }
    }

    internal int CountTokenKey(UiValueEntry[] entries, string key)
    {
        int count = 0;
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry? entry = entries[index];
            if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal bool IsFirstTokenKey(UiValueEntry[] entries, int candidateIndex, string key)
    {
        int index = 0;
        while (index < candidateIndex)
        {
            UiValueEntry? entry = entries[index];
            if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal void SetTokenState(string key, UiValue value, string location)
    {
        ThemeTokenState? existing = FindTokenState(key);
        ThemeTokenState replacement = new ThemeTokenState(key, value, location);
        if (existing == null)
        {
            TokenStates.Add(replacement);
            return;
        }

        int index = TokenStates.IndexOf(existing);
        TokenStates[index] = replacement;
    }

    internal ThemeTokenState? FindTokenState(string key)
    {
        int index = 0;
        while (index < TokenStates.Count)
        {
            if (string.Equals(TokenStates[index].Key, key, StringComparison.Ordinal))
            {
                return TokenStates[index];
            }

            index++;
        }

        return null;
    }

    internal void ResolveAllTokens()
    {
        int index = 0;
        while (index < TokenStates.Count)
        {
            ResolveTokenState(TokenStates[index]);
            index++;
        }
    }

    internal bool ResolveTokenState(ThemeTokenState state)
    {
        if (state.Status == 2)
        {
            return true;
        }

        if (state.Status == 3)
        {
            return false;
        }

        if (state.Status == 1)
        {
            AddThemeError("theme.token-cycle", "Theme token references cannot contain a cycle.", state.Location + "/value");
            state.Status = 3;
            return false;
        }

        state.Status = 1;
        string? referenceKey = GetTokenReference(state.Source);
        if (referenceKey == null)
        {
            state.Resolved = ValueJsonCodec.CloneNode(state.Source);
            state.Status = 2;
            return true;
        }

        if (string.IsNullOrWhiteSpace(referenceKey))
        {
            AddThemeError("theme.invalid-token-reference", "A theme token reference cannot be empty.", state.Location + "/value");
            state.Status = 3;
            return false;
        }

        ThemeTokenState? referenced = FindTokenState(referenceKey);
        if (referenced == null)
        {
            AddThemeError("theme.missing-token", "A referenced theme token does not exist.", state.Location + "/value");
            state.Status = 3;
            return false;
        }

        if (!ResolveTokenState(referenced) || referenced.Resolved == null)
        {
            state.Status = 3;
            return false;
        }

        state.Resolved = ValueJsonCodec.CloneNode(referenced.Resolved);
        state.Status = 2;
        return true;
    }

    internal string? GetTokenReference(UiValue value)
    {
        if (value.Kind != UiValueKind.String || value.StringValue == null)
        {
            return null;
        }

        string text = value.StringValue;
        if (text.Length >= 2 && text[0] == '{' && text[text.Length - 1] == '}')
        {
            return text.Substring(1, text.Length - 2);
        }

        return null;
    }

    internal UiValueEntry[] CreateResolvedTokens()
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        int index = 0;
        while (index < TokenStates.Count)
        {
            ThemeTokenState state = TokenStates[index];
            if (state.Status == 2 && state.Resolved != null)
            {
                UiValueEntry entry = new UiValueEntry();
                entry.Key = state.Key;
                entry.Value = ValueJsonCodec.CloneNode(state.Resolved);
                InsertResolvedToken(entries, entry);
            }

            index++;
        }

        return entries.ToArray();
    }

    internal void InsertResolvedToken(List<UiValueEntry> entries, UiValueEntry entry)
    {
        int index = entries.Count;
        entries.Add(entry);
        while (index > 0 && string.CompareOrdinal(entries[index - 1].Key, entry.Key) > 0)
        {
            entries[index] = entries[index - 1];
            index--;
        }

        entries[index] = entry;
    }

    internal void AddThemeError(string code, string message, string location)
    {
        Diagnostics.Add(DiagnosticLevel.Error, code, message, location);
    }
}
