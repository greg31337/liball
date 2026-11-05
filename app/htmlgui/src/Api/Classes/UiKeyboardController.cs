using System;
using System.Collections.Generic;

namespace htmlgui;

/// <summary>Provides registered keyboard maps and deterministic keyboard input processing.</summary>
public class UiKeyboardController : IUiKeyboardController
{
    internal KeyboardEngine Engine;
    internal List<UiKeyboardMap> Maps;

    /// <summary>Initializes an empty keyboard controller.</summary>
    public UiKeyboardController()
    {
        Engine = new KeyboardEngine();
        Maps = new List<UiKeyboardMap>();
    }

    /// <summary>Creates an APG-compatible keyboard map without registering it.</summary>
    public UiKeyboardMap CreateKeyboardMap(string id, string widget, UiOrientation orientation, bool isRightToLeft)
    {
        return Engine.CreateKeyboardMap(id, widget, orientation, isRightToLeft);
    }

    /// <summary>Registers or replaces a keyboard map after exceptionless validation.</summary>
    public UiResult RegisterKeyboardMap(UiKeyboardMap? map)
    {
        UiResult validation = ValidateKeyboardRegistration(map);
        if (!validation.Success || map == null)
        {
            return validation;
        }

        UiKeyboardMap copy = CopyKeyboardMap(map);
        int index = FindKeyboardMapIndex(map.Id);
        if (index < 0)
        {
            Maps.Add(copy);
        }
        else
        {
            Maps[index] = copy;
        }

        return validation;
    }

    /// <summary>Processes a mapped shortcut, composite-widget key, or type-ahead character.</summary>
    public UiKeyboardResult ProcessKeyboardInput(string? mapId, UiKeyboardData? keyboard, UiTypeAheadState? typeAhead, UiValueEntry[]? items, int generation, bool allowRepeat, bool resetTypeAhead)
    {
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
        UiKeyboardMap? map = FindKeyboardMap(mapId);
        if (map == null)
        {
            return new UiKeyboardResult
            {
                TypeAhead = Engine.CopyTypeAheadState(typeAhead),
                Diagnostics = new UiDiagnostic[]
                {
                    new UiDiagnostic
                    {
                        Severity = UiDiagnosticSeverity.Error,
                        Code = "keyboard.unknown-map",
                        Message = "The keyboard map is not registered.",
                        Location = "/keyboard/mapId"
                    }
                }
            };
        }

        KeyboardProcessingResult processed = Engine.ProcessKeyboardInput(map, keyboard, typeAhead, items, generation, allowRepeat, resetTypeAhead);
        return new UiKeyboardResult
        {
            Handled = processed.Handled,
            Suppressed = processed.Suppressed,
            IsTypeAhead = processed.IsTypeAhead,
            NormalizedKey = processed.NormalizedKey,
            Chord = processed.Chord,
            Action = processed.Action,
            MatchedKey = processed.MatchedKey,
            TypeAhead = processed.TypeAhead
        };
    }

    /// <summary>Returns a copy of type-ahead state with its buffer and match reset.</summary>
    public UiTypeAheadState ResetKeyboardState(UiTypeAheadState typeAhead, int generation)
    {
        return Engine.ResetKeyboardState(typeAhead, generation);
    }

    internal UiResult ValidateKeyboardRegistration(UiKeyboardMap? map)
    {
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        if (map == null)
        {
            AddKeyboardDiagnostic(diagnostics, "keyboard.null-map", "The keyboard map cannot be null.", "/keyboard/map");
            return new UiResult { Success = false, Diagnostics = diagnostics.ToArray() };
        }

        if (string.IsNullOrWhiteSpace(map.Id))
        {
            AddKeyboardDiagnostic(diagnostics, "keyboard.invalid-map", "A keyboard map requires a stable identifier.", "/keyboard/map/id");
        }

        if (string.IsNullOrWhiteSpace(map.Widget))
        {
            AddKeyboardDiagnostic(diagnostics, "keyboard.invalid-widget", "A keyboard map requires a widget token.", "/keyboard/map/widget");
        }

        switch (map.Orientation)
        {
            case UiOrientation.Horizontal:
            case UiOrientation.Vertical:
                break;
            default:
                AddKeyboardDiagnostic(diagnostics, "keyboard.invalid-orientation", "The keyboard-map orientation is not supported.", "/keyboard/map/orientation");
                break;
        }

        ValidateKeyboardMappings(map.Mappings, diagnostics);
        return new UiResult
        {
            Success = diagnostics.Count == 0,
            Diagnostics = diagnostics.ToArray()
        };
    }

    internal void ValidateKeyboardMappings(UiValueEntry[]? mappings, List<UiDiagnostic> diagnostics)
    {
        if (mappings == null)
        {
            AddKeyboardDiagnostic(diagnostics, "keyboard.null-mappings", "The keyboard map mappings cannot be null.", "/keyboard/map/mappings");
            return;
        }

        Dictionary<string, int> keys = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        while (index < mappings.Length)
        {
            UiValueEntry? mapping = mappings[index];
            string location = "/keyboard/map/mappings/" + index;
            if (mapping == null)
            {
                AddKeyboardDiagnostic(diagnostics, "keyboard.null-mapping", "A keyboard mapping cannot be null.", location);
            }
            else if (mapping.Key.Length == 0 || (mapping.Key != AriaKeyNames.Space && string.IsNullOrWhiteSpace(mapping.Key)))
            {
                AddKeyboardDiagnostic(diagnostics, "keyboard.invalid-key", "A keyboard mapping requires a key chord.", location + "/key");
            }
            else if (keys.ContainsKey(mapping.Key))
            {
                AddKeyboardDiagnostic(diagnostics, "keyboard.duplicate-key", "A keyboard key chord can be mapped only once.", location + "/key");
            }
            else
            {
                keys.Add(mapping.Key, index);
                if (mapping.Value == null || mapping.Value.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(mapping.Value.StringValue))
                {
                    AddKeyboardDiagnostic(diagnostics, "keyboard.invalid-action", "A keyboard mapping requires a nonempty string action.", location + "/value");
                }
            }

            index++;
        }
    }

    internal void AddKeyboardDiagnostic(List<UiDiagnostic> diagnostics, string code, string message, string location)
    {
        diagnostics.Add(new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Location = location
        });
    }

    internal UiKeyboardMap CopyKeyboardMap(UiKeyboardMap map)
    {
        UiValueEntry[] mappings = new UiValueEntry[map.Mappings.Length];
        int index = 0;
        while (index < map.Mappings.Length)
        {
            UiValueEntry mapping = map.Mappings[index];
            mappings[index] = new UiValueEntry
            {
                Key = mapping.Key,
                Value = new UiValue
                {
                    Kind = UiValueKind.String,
                    StringValue = mapping.Value.StringValue
                }
            };
            index++;
        }

        return new UiKeyboardMap
        {
            Id = map.Id,
            Widget = map.Widget,
            Orientation = map.Orientation,
            Mappings = mappings,
            IsRightToLeft = map.IsRightToLeft
        };
    }

    internal UiKeyboardMap? FindKeyboardMap(string? mapId)
    {
        int index = FindKeyboardMapIndex(mapId);
        if (index < 0)
        {
            return null;
        }

        return Maps[index];
    }

    internal int FindKeyboardMapIndex(string? mapId)
    {
        int index = 0;
        while (mapId != null && index < Maps.Count)
        {
            if (Maps[index].Id == mapId)
            {
                return index;
            }

            index++;
        }

        return -1;
    }
}
