using System;
using System.Collections.Generic;

namespace htmlgui;

internal class KeyboardProcessingResult
{
    internal bool Handled;
    internal bool Suppressed;
    internal bool IsTypeAhead;
    internal string NormalizedKey = string.Empty;
    internal string Chord = string.Empty;
    internal string? Action;
    internal string? MatchedKey;
    internal UiTypeAheadState TypeAhead = new UiTypeAheadState();
}

internal class KeyboardEngine
{
    internal UiKeyboardMap CreateKeyboardMap(string? id, string? widget, UiOrientation orientation, bool isRightToLeft)
    {
        string checkedId = Guard.ArgumentNotWhiteSpace(id, "id");
        string checkedWidget = Guard.ArgumentNotWhiteSpace(widget, "widget");
        ValidateKeyboardOrientation(orientation);
        string canonicalWidget = GetCanonicalWidget(checkedWidget);
        List<UiValueEntry> mappings = new List<UiValueEntry>();

        switch (canonicalWidget)
        {
            case "button":
                AddButtonMappings(mappings);
                break;
            case "radio":
                AddRadioMappings(mappings);
                break;
            case "listbox":
                AddListBoxMappings(mappings, orientation);
                break;
            case "combo":
                AddComboMappings(mappings);
                break;
            case "menu":
                AddMenuMappings(mappings, orientation);
                break;
            case "menu-button":
                AddMenuButtonMappings(mappings);
                break;
            case "menu-bar":
                AddMenuBarMappings(mappings, orientation);
                break;
            case "tabs":
                AddTabsMappings(mappings, orientation);
                break;
            case "toolbar":
                AddToolbarMappings(mappings, orientation);
                break;
            case "dialog":
                AddDialogMappings(mappings);
                break;
            case "grid":
                AddGridMappings(mappings);
                break;
            case "tree":
                AddTreeMappings(mappings, orientation);
                break;
            case "treegrid":
                AddTreeGridMappings(mappings);
                break;
            case "slider":
                AddSliderMappings(mappings);
                break;
            case "disclosure":
                AddDisclosureMappings(mappings);
                break;
        }

        return new UiKeyboardMap
        {
            Id = checkedId,
            Widget = canonicalWidget,
            Orientation = orientation,
            Mappings = mappings.ToArray(),
            IsRightToLeft = isRightToLeft
        };
    }

    internal void ValidateKeyboardOrientation(UiOrientation orientation)
    {
        switch (orientation)
        {
            case UiOrientation.Horizontal:
            case UiOrientation.Vertical:
                return;
            default:
                throw new ArgumentOutOfRangeException("orientation", orientation, "The keyboard-map orientation is not supported.");
        }
    }

    internal string GetCanonicalWidget(string widget)
    {
        switch (widget)
        {
            case "button":
            case "push-button":
            case "submit-button":
            case "reset-button":
            case "toggle-button":
            case "split-button":
            case "icon-button":
            case "command-button":
                return "button";
            case "radio":
            case "radio-input":
            case "radio-group":
            case "radiogroup":
                return "radio";
            case "listbox":
            case "list-box":
            case "listbox-input":
                return "listbox";
            case "combo":
            case "combo-box":
            case "combobox":
            case "combobox-input":
            case "autocomplete":
            case "autocomplete-input":
            case "editable-combo":
            case "editable-combo-box":
            case "editable-combobox":
                return "combo";
            case "menu":
            case "popup-menu":
            case "context-menu":
                return "menu";
            case "menu-button":
                return "menu-button";
            case "menu-bar":
            case "menubar":
                return "menu-bar";
            case "tabs":
            case "tab-view":
            case "tablist":
            case "stepper":
                return "tabs";
            case "toolbar":
                return "toolbar";
            case "dialog":
            case "modal-dialog":
            case "alert-dialog":
            case "confirm-dialog":
            case "prompt-dialog":
                return "dialog";
            case "grid":
            case "data-grid":
            case "data-table":
                return "grid";
            case "tree":
            case "tree-view":
                return "tree";
            case "treegrid":
            case "tree-grid":
                return "treegrid";
            case "slider":
                return "slider";
            case "disclosure":
            case "collapsible-region":
            case "collapsible":
                return "disclosure";
            default:
                throw new ArgumentException("The widget does not have a supported APG keyboard map.", "widget");
        }
    }

    internal void AddButtonMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "activate");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "activate");
    }

    internal void AddRadioMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "next-select");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "next-select");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "previous-select");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "previous-select");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "select");
    }

    internal void AddListBoxMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        AddAxisMappings(mappings, orientation, "next", "previous");
        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "toggle-selection");
        AddMultiSelectionMappings(mappings, orientation);
    }

    internal void AddComboMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "open-or-next");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "open-last-or-previous");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "accept");
        AddKeyboardMapping(mappings, AriaKeyNames.Escape, "close");
        AddKeyboardMapping(mappings, "Alt+ArrowDown", "open");
        AddKeyboardMapping(mappings, "Alt+ArrowUp", "accept-close");
    }

    internal void AddMenuMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        AddAxisMappings(mappings, orientation, "next", "previous");
        if (orientation == UiOrientation.Vertical)
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "open-submenu");
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "close-submenu");
        }
        else
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "open-submenu");
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "close-submenu");
        }

        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "activate-or-open");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "activate-or-open");
        AddKeyboardMapping(mappings, AriaKeyNames.Escape, "close");
        AddKeyboardMapping(mappings, AriaKeyNames.Tab, "exit");
        AddKeyboardMapping(mappings, "Shift+Tab", "exit");
    }

    internal void AddMenuBarMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        AddAxisMappings(mappings, orientation, "next", "previous");
        if (orientation == UiOrientation.Horizontal)
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "open-first");
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "open-last");
        }
        else
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "open-first");
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "close-submenu");
        }

        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "activate-or-open");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "activate-or-open");
        AddKeyboardMapping(mappings, AriaKeyNames.Escape, "close");
        AddKeyboardMapping(mappings, AriaKeyNames.Tab, "exit");
        AddKeyboardMapping(mappings, "Shift+Tab", "exit");
    }

    internal void AddMenuButtonMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "open-first");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "open-first");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "open-first");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "open-last");
    }

    internal void AddTabsMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        AddAxisMappings(mappings, orientation, "next", "previous");
        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "activate");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "activate");
        AddKeyboardMapping(mappings, "Shift+F10", "open-menu");
        AddKeyboardMapping(mappings, AriaKeyNames.Delete, "delete");
    }

    internal void AddToolbarMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        AddAxisMappings(mappings, orientation, "next", "previous");
        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last");
    }

    internal void AddDialogMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.Tab, "next");
        AddKeyboardMapping(mappings, "Shift+Tab", "previous");
        AddKeyboardMapping(mappings, AriaKeyNames.Escape, "close");
    }

    internal void AddGridMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "next-column");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "previous-column");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "next-row");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "previous-row");
        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first-column");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last-column");
        AddKeyboardMapping(mappings, "Primary+Home", "first-cell");
        AddKeyboardMapping(mappings, "Primary+End", "last-cell");
        AddKeyboardMapping(mappings, AriaKeyNames.PageUp, "page-previous");
        AddKeyboardMapping(mappings, AriaKeyNames.PageDown, "page-next");
        AddKeyboardMapping(mappings, "Primary+Space", "select-column");
        AddKeyboardMapping(mappings, "Shift+Space", "select-row");
        AddKeyboardMapping(mappings, "Primary+A", "select-all");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "enter-cell");
        AddKeyboardMapping(mappings, AriaKeyNames.F2, "enter-cell");
        AddKeyboardMapping(mappings, AriaKeyNames.Escape, "exit-cell");
        AddKeyboardMapping(mappings, AriaKeyNames.Tab, "next-control");
        AddKeyboardMapping(mappings, "Shift+Tab", "previous-control");
    }

    internal void AddTreeMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        AddAxisMappings(mappings, orientation, "next", "previous");
        if (orientation == UiOrientation.Vertical)
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "expand-or-child");
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "collapse-or-parent");
        }
        else
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "expand-or-child");
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "collapse-or-parent");
        }

        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "activate");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "toggle-selection");
        AddKeyboardMapping(mappings, "*", "expand-siblings");
        AddMultiSelectionMappings(mappings, orientation);
    }

    internal void AddTreeGridMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "next-row");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "previous-row");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "expand-or-next-column");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "collapse-or-previous-column");
        AddKeyboardMapping(mappings, AriaKeyNames.Home, "first-column");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "last-column");
        AddKeyboardMapping(mappings, "Primary+Home", "first-cell");
        AddKeyboardMapping(mappings, "Primary+End", "last-cell");
        AddKeyboardMapping(mappings, AriaKeyNames.PageUp, "page-previous");
        AddKeyboardMapping(mappings, AriaKeyNames.PageDown, "page-next");
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "activate-or-toggle");
        AddKeyboardMapping(mappings, AriaKeyNames.Tab, "next-control");
        AddKeyboardMapping(mappings, "Primary+Space", "select-column");
        AddKeyboardMapping(mappings, "Shift+Space", "select-row");
        AddKeyboardMapping(mappings, "Primary+A", "select-all");
        AddKeyboardMapping(mappings, "Shift+ArrowRight", "extend-next-column");
        AddKeyboardMapping(mappings, "Shift+ArrowLeft", "extend-previous-column");
        AddKeyboardMapping(mappings, "Shift+ArrowDown", "extend-next-row");
        AddKeyboardMapping(mappings, "Shift+ArrowUp", "extend-previous-row");
    }

    internal void AddSliderMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, "increment");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, "increment");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, "decrement");
        AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, "decrement");
        AddKeyboardMapping(mappings, AriaKeyNames.Home, "minimum");
        AddKeyboardMapping(mappings, AriaKeyNames.End, "maximum");
        AddKeyboardMapping(mappings, AriaKeyNames.PageUp, "increment-large");
        AddKeyboardMapping(mappings, AriaKeyNames.PageDown, "decrement-large");
    }

    internal void AddDisclosureMappings(List<UiValueEntry> mappings)
    {
        AddKeyboardMapping(mappings, AriaKeyNames.Enter, "toggle");
        AddKeyboardMapping(mappings, AriaKeyNames.Space, "toggle");
    }

    internal void AddAxisMappings(List<UiValueEntry> mappings, UiOrientation orientation, string nextAction, string previousAction)
    {
        if (orientation == UiOrientation.Horizontal)
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowRight, nextAction);
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowLeft, previousAction);
        }
        else
        {
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowDown, nextAction);
            AddKeyboardMapping(mappings, AriaKeyNames.ArrowUp, previousAction);
        }
    }

    internal void AddMultiSelectionMappings(List<UiValueEntry> mappings, UiOrientation orientation)
    {
        if (orientation == UiOrientation.Horizontal)
        {
            AddKeyboardMapping(mappings, "Shift+ArrowRight", "next-toggle-selection");
            AddKeyboardMapping(mappings, "Shift+ArrowLeft", "previous-toggle-selection");
        }
        else
        {
            AddKeyboardMapping(mappings, "Shift+ArrowDown", "next-toggle-selection");
            AddKeyboardMapping(mappings, "Shift+ArrowUp", "previous-toggle-selection");
        }

        AddKeyboardMapping(mappings, "Shift+Space", "extend-selection");
        AddKeyboardMapping(mappings, "Primary+Shift+Home", "extend-selection-first");
        AddKeyboardMapping(mappings, "Primary+Shift+End", "extend-selection-last");
        AddKeyboardMapping(mappings, "Primary+A", "toggle-all-selection");
    }

    internal void AddKeyboardMapping(List<UiValueEntry> mappings, string key, string action)
    {
        mappings.Add(new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = action }
        });
    }

    internal KeyboardProcessingResult ProcessKeyboardInput(UiKeyboardMap? map, UiKeyboardData? keyboard, UiTypeAheadState? typeAhead, UiValueEntry[]? items, int generation, bool allowRepeat, bool resetTypeAhead)
    {
        if (map == null)
        {
            throw new ArgumentNullException("map");
        }

        if (keyboard == null)
        {
            throw new ArgumentNullException("keyboard");
        }

        if (typeAhead == null)
        {
            throw new ArgumentNullException("typeAhead");
        }

        if (items == null)
        {
            throw new ArgumentNullException("items");
        }

        Guard.ArgumentNonNegative(generation, "generation");
        if (map.Mappings == null)
        {
            throw new ArgumentException("The keyboard map mappings cannot be null.", "map");
        }

        KeyboardProcessingResult result = new KeyboardProcessingResult();
        result.TypeAhead = CopyTypeAheadState(typeAhead);
        if (resetTypeAhead || typeAhead.Generation != generation)
        {
            ClearTypeAheadState(result.TypeAhead, generation);
        }
        else
        {
            result.TypeAhead.Generation = generation;
        }

        result.NormalizedKey = NormalizeKeyboardKey(keyboard);
        if (keyboard.IsComposing || IsCompositionKey(result.NormalizedKey))
        {
            result.Suppressed = true;
            result.MatchedKey = result.TypeAhead.MatchedKey;
            return result;
        }

        if (keyboard.Repeat && !allowRepeat)
        {
            result.Suppressed = true;
            result.MatchedKey = result.TypeAhead.MatchedKey;
            return result;
        }

        string logicalKey = GetLogicalKeyboardKey(map, result.NormalizedKey);
        result.Chord = CreateKeyboardChord(logicalKey, keyboard.PrimaryModifier, keyboard.Alt, keyboard.Shift);
        result.Action = FindKeyboardAction(map, result.Chord);
        if (result.Action != null)
        {
            result.Handled = true;
            ClearTypeAheadState(result.TypeAhead, generation);
            return result;
        }

        if (SupportsTypeAhead(map.Widget) && IsTypeAheadCharacter(result.NormalizedKey, keyboard))
        {
            result.IsTypeAhead = true;
            AdvanceTypeAheadState(result.TypeAhead, items, result.NormalizedKey, generation);
            result.MatchedKey = result.TypeAhead.MatchedKey;
            if (result.MatchedKey != null)
            {
                result.Handled = true;
                result.Action = "type-ahead";
            }

            return result;
        }

        if (ShouldClearTypeAhead(result.NormalizedKey, keyboard))
        {
            ClearTypeAheadState(result.TypeAhead, generation);
        }

        result.MatchedKey = result.TypeAhead.MatchedKey;
        return result;
    }

    internal UiTypeAheadState ResetKeyboardState(UiTypeAheadState? typeAhead, int generation)
    {
        if (typeAhead == null)
        {
            throw new ArgumentNullException("typeAhead");
        }

        Guard.ArgumentNonNegative(generation, "generation");
        UiTypeAheadState result = CopyTypeAheadState(typeAhead);
        ClearTypeAheadState(result, generation);
        return result;
    }

    internal UiTypeAheadState CopyTypeAheadState(UiTypeAheadState state)
    {
        return new UiTypeAheadState
        {
            Buffer = state.Buffer ?? string.Empty,
            MatchedKey = state.MatchedKey,
            Generation = state.Generation,
            IgnoreCase = state.IgnoreCase
        };
    }

    internal void ClearTypeAheadState(UiTypeAheadState state, int generation)
    {
        state.Buffer = string.Empty;
        state.MatchedKey = null;
        state.Generation = generation;
    }

    internal string NormalizeKeyboardKey(UiKeyboardData keyboard)
    {
        string? candidate = keyboard.Key;
        if (string.IsNullOrEmpty(candidate))
        {
            candidate = GetKeyboardCodeKey(keyboard.Code);
        }
        else if (candidate == "Unidentified")
        {
            string codeKey = GetKeyboardCodeKey(keyboard.Code);
            if (codeKey.Length > 0)
            {
                candidate = codeKey;
            }
        }

        if (candidate == null)
        {
            return string.Empty;
        }

        switch (candidate)
        {
            case "Esc":
                return AriaKeyNames.Escape;
            case "Space":
            case "Spacebar":
                return AriaKeyNames.Space;
            case "Return":
                return AriaKeyNames.Enter;
            case "Left":
                return AriaKeyNames.ArrowLeft;
            case "Right":
                return AriaKeyNames.ArrowRight;
            case "Up":
                return AriaKeyNames.ArrowUp;
            case "Down":
                return AriaKeyNames.ArrowDown;
            case "Del":
                return AriaKeyNames.Delete;
            default:
                if (candidate.Length == 1 && (keyboard.PrimaryModifier || keyboard.Alt))
                {
                    return candidate.ToUpperInvariant();
                }

                return candidate;
        }
    }

    internal string GetKeyboardCodeKey(string? code)
    {
        switch (code)
        {
            case "Enter":
            case "NumpadEnter":
                return AriaKeyNames.Enter;
            case "Space":
                return AriaKeyNames.Space;
            case "Escape":
                return AriaKeyNames.Escape;
            case "ArrowLeft":
                return AriaKeyNames.ArrowLeft;
            case "ArrowRight":
                return AriaKeyNames.ArrowRight;
            case "ArrowUp":
                return AriaKeyNames.ArrowUp;
            case "ArrowDown":
                return AriaKeyNames.ArrowDown;
            case "Home":
                return AriaKeyNames.Home;
            case "End":
                return AriaKeyNames.End;
            case "PageUp":
                return AriaKeyNames.PageUp;
            case "PageDown":
                return AriaKeyNames.PageDown;
            case "Tab":
                return AriaKeyNames.Tab;
            case "Backspace":
                return AriaKeyNames.Backspace;
            case "Delete":
                return AriaKeyNames.Delete;
            case "F2":
                return AriaKeyNames.F2;
            case "NumpadMultiply":
                return "*";
        }

        if (code != null && code.Length == 4 && code.StartsWith("Key", StringComparison.Ordinal))
        {
            return code.Substring(3, 1);
        }

        if (code != null && code.Length == 6 && code.StartsWith("Digit", StringComparison.Ordinal))
        {
            return code.Substring(5, 1);
        }

        return code ?? string.Empty;
    }

    internal bool IsCompositionKey(string key)
    {
        return key == "Dead" || key == "Process" || key == "Unidentified";
    }

    internal string GetLogicalKeyboardKey(UiKeyboardMap map, string key)
    {
        if (!map.IsRightToLeft)
        {
            return key;
        }

        if (key == AriaKeyNames.ArrowRight)
        {
            return AriaKeyNames.ArrowLeft;
        }

        if (key == AriaKeyNames.ArrowLeft)
        {
            return AriaKeyNames.ArrowRight;
        }

        return key;
    }

    internal string CreateKeyboardChord(string key, bool primaryModifier, bool alt, bool shift)
    {
        string chord = string.Empty;
        if (primaryModifier)
        {
            chord = "Primary+";
        }

        if (alt)
        {
            chord += "Alt+";
        }

        if (shift)
        {
            chord += "Shift+";
        }

        if (key == AriaKeyNames.Space && chord.Length > 0)
        {
            return chord + "Space";
        }

        return chord + key;
    }

    internal string? FindKeyboardAction(UiKeyboardMap map, string chord)
    {
        UiValueEntry[]? mappings = map.Mappings;
        int index = 0;
        while (mappings != null && index < mappings.Length)
        {
            UiValueEntry? mapping = mappings[index];
            if (mapping != null && mapping.Key == chord && mapping.Value != null && mapping.Value.Kind == UiValueKind.String && !string.IsNullOrEmpty(mapping.Value.StringValue))
            {
                return mapping.Value.StringValue;
            }

            index++;
        }

        return null;
    }

    internal bool SupportsTypeAhead(string widget)
    {
        switch (widget)
        {
            case "listbox":
            case "menu":
            case "menu-bar":
            case "tree":
                return true;
            default:
                return false;
        }
    }

    internal bool IsTypeAheadCharacter(string key, UiKeyboardData keyboard)
    {
        return key.Length == 1 && key != AriaKeyNames.Space && !char.IsControl(key[0]) && !keyboard.PrimaryModifier && !keyboard.Alt;
    }

    internal void AdvanceTypeAheadState(UiTypeAheadState state, UiValueEntry[] items, string character, int generation)
    {
        string priorBuffer = state.Buffer;
        string candidateBuffer = priorBuffer + character;
        string query = candidateBuffer;
        if (AreAllTypeAheadCharactersSame(candidateBuffer))
        {
            query = character;
        }

        string? match = FindTypeAheadItem(items, query, state.MatchedKey, state.IgnoreCase);
        if (match == null && priorBuffer.Length > 0)
        {
            candidateBuffer = character;
            query = character;
            match = FindTypeAheadItem(items, query, state.MatchedKey, state.IgnoreCase);
        }

        state.Buffer = candidateBuffer;
        state.MatchedKey = match;
        state.Generation = generation;
    }

    internal bool AreAllTypeAheadCharactersSame(string value)
    {
        int index = 1;
        while (value.Length > 0 && index < value.Length)
        {
            if (value[index] != value[0])
            {
                return false;
            }

            index++;
        }

        return value.Length > 0;
    }

    internal string? FindTypeAheadItem(UiValueEntry[] items, string query, string? previousMatch, bool ignoreCase)
    {
        if (items.Length == 0 || query.Length == 0)
        {
            return null;
        }

        int previousIndex = FindTypeAheadItemIndex(items, previousMatch);
        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (query.Length > 1 && previousIndex >= 0)
        {
            string? previousLabel = GetTypeAheadLabel(items[previousIndex]);
            if (previousLabel != null && previousLabel.StartsWith(query, comparison))
            {
                return items[previousIndex].Key;
            }
        }

        int offset = 1;
        while (offset <= items.Length)
        {
            int index = (previousIndex + offset) % items.Length;
            UiValueEntry? item = items[index];
            string? label = GetTypeAheadLabel(item);
            if (item != null && label != null && label.StartsWith(query, comparison))
            {
                return item.Key;
            }

            offset++;
        }

        return null;
    }

    internal int FindTypeAheadItemIndex(UiValueEntry[] items, string? key)
    {
        int index = 0;
        while (key != null && index < items.Length)
        {
            UiValueEntry? item = items[index];
            if (item != null && item.Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal string? GetTypeAheadLabel(UiValueEntry? item)
    {
        if (item == null || item.Value == null || item.Value.Kind != UiValueKind.String)
        {
            return null;
        }

        return item.Value.StringValue;
    }

    internal bool ShouldClearTypeAhead(string key, UiKeyboardData keyboard)
    {
        return key.Length != 1 || keyboard.PrimaryModifier || keyboard.Alt;
    }
}
