using System;
using htmlgui;

namespace htmlgui.Tests;

public class KeyboardProcessingTests
{
    [Test]
    public void ResolvesHorizontalKeysLogicallyForRightToLeftMaps()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap menu = engine.CreateKeyboardMap("menu", "menu", UiOrientation.Vertical, true);
        UiKeyboardMap menuBar = engine.CreateKeyboardMap("bar", "menu-bar", UiOrientation.Horizontal, true);
        UiKeyboardMap listBox = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Horizontal, true);

        KeyboardProcessingResult menuResult = Process(engine, menu, Key(AriaKeyNames.ArrowLeft), new UiTypeAheadState());
        KeyboardProcessingResult barResult = Process(engine, menuBar, Key(AriaKeyNames.ArrowRight), new UiTypeAheadState());
        KeyboardProcessingResult selectionResult = Process(engine, listBox, new UiKeyboardData { Key = AriaKeyNames.ArrowRight, Shift = true }, new UiTypeAheadState());
        KeyboardProcessingResult verticalResult = Process(engine, menu, Key(AriaKeyNames.ArrowUp), new UiTypeAheadState());

        Assert.Multiple(() =>
        {
            Assert.That(menuResult.Action, Is.EqualTo("open-submenu"));
            Assert.That(menuResult.NormalizedKey, Is.EqualTo(AriaKeyNames.ArrowLeft));
            Assert.That(menuResult.Chord, Is.EqualTo(AriaKeyNames.ArrowRight));
            Assert.That(barResult.Action, Is.EqualTo("previous"));
            Assert.That(selectionResult.Action, Is.EqualTo("previous-toggle-selection"));
            Assert.That(selectionResult.Chord, Is.EqualTo("Shift+ArrowLeft"));
            Assert.That(verticalResult.Action, Is.EqualTo("previous"));
        });
    }

    [Test]
    public void NormalizesLegacyKeysPhysicalCodesAndModifierChords()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiTypeAheadState state = new UiTypeAheadState();

        KeyboardProcessingResult escape = Process(engine, engine.CreateKeyboardMap("combo", "combo", UiOrientation.Vertical, false), Key("Esc"), state);
        KeyboardProcessingResult enter = Process(engine, engine.CreateKeyboardMap("combo", "combo", UiOrientation.Vertical, false), new UiKeyboardData { Key = "Unidentified", Code = "NumpadEnter" }, state);
        KeyboardProcessingResult space = Process(engine, engine.CreateKeyboardMap("button", "button", UiOrientation.Horizontal, false), Key("Spacebar"), state);
        KeyboardProcessingResult range = Process(engine, engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false), new UiKeyboardData { Key = AriaKeyNames.Home, PrimaryModifier = true, Shift = true }, state);
        KeyboardProcessingResult star = Process(engine, engine.CreateKeyboardMap("tree", "tree", UiOrientation.Vertical, false), new UiKeyboardData { Code = "NumpadMultiply" }, state);
        KeyboardProcessingResult selectAll = Process(engine, engine.CreateKeyboardMap("grid", "grid", UiOrientation.Vertical, false), new UiKeyboardData { Code = "KeyA", PrimaryModifier = true }, state);

        Assert.Multiple(() =>
        {
            Assert.That(escape.NormalizedKey, Is.EqualTo(AriaKeyNames.Escape));
            Assert.That(escape.Action, Is.EqualTo("close"));
            Assert.That(enter.Action, Is.EqualTo("accept"));
            Assert.That(space.NormalizedKey, Is.EqualTo(AriaKeyNames.Space));
            Assert.That(space.Action, Is.EqualTo("activate"));
            Assert.That(range.Chord, Is.EqualTo("Primary+Shift+Home"));
            Assert.That(range.Action, Is.EqualTo("extend-selection-first"));
            Assert.That(star.Action, Is.EqualTo("expand-siblings"));
            Assert.That(selectAll.Chord, Is.EqualTo("Primary+A"));
            Assert.That(selectAll.Action, Is.EqualTo("select-all"));
        });
    }

    [Test]
    public void SuppressesCompositionDeadAndUnidentifiedKeyInputWithoutChangingTypeAhead()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "al", MatchedKey = "alpha", Generation = 2 };

        KeyboardProcessingResult composing = Process(engine, map, new UiKeyboardData { Key = AriaKeyNames.ArrowDown, IsComposing = true }, state, 2, true, false);
        KeyboardProcessingResult dead = Process(engine, map, Key("Dead"), state, 2, true, false);
        KeyboardProcessingResult unidentified = Process(engine, map, Key("Unidentified"), state, 2, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(composing.Suppressed, Is.True);
            Assert.That(composing.Handled, Is.False);
            Assert.That(dead.Suppressed, Is.True);
            Assert.That(unidentified.Suppressed, Is.True);
            Assert.That(composing.TypeAhead.Buffer, Is.EqualTo("al"));
            Assert.That(composing.TypeAhead.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(composing.TypeAhead, Is.Not.SameAs(state));
            Assert.That(state.Buffer, Is.EqualTo("al"));
        });
    }

    [Test]
    public void AppliesRepeatPolicyBeforeDispatchingMappedActions()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardData repeatedEnter = new UiKeyboardData { Key = AriaKeyNames.Enter, Repeat = true };
        UiKeyboardData repeatedArrow = new UiKeyboardData { Key = AriaKeyNames.ArrowRight, Repeat = true };

        KeyboardProcessingResult suppressed = Process(engine, engine.CreateKeyboardMap("button", "button", UiOrientation.Horizontal, false), repeatedEnter, new UiTypeAheadState(), 0, false, false);
        KeyboardProcessingResult allowed = Process(engine, engine.CreateKeyboardMap("slider", "slider", UiOrientation.Horizontal, false), repeatedArrow, new UiTypeAheadState(), 0, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(suppressed.Suppressed, Is.True);
            Assert.That(suppressed.Handled, Is.False);
            Assert.That(suppressed.Action, Is.Null);
            Assert.That(allowed.Suppressed, Is.False);
            Assert.That(allowed.Handled, Is.True);
            Assert.That(allowed.Action, Is.EqualTo("increment"));
        });
    }

    [Test]
    public void AccumulatesTypeAheadAndResetsTheBufferAcrossGenerations()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiValueEntry[] items = Items("alpha", "Alpha", "alpine", "Alpine", "beta", "Beta");
        UiTypeAheadState original = new UiTypeAheadState { Generation = 1 };

        KeyboardProcessingResult first = engine.ProcessKeyboardInput(map, Key("a"), original, items, 1, true, false);
        KeyboardProcessingResult second = engine.ProcessKeyboardInput(map, Key("l"), first.TypeAhead, items, 1, true, false);
        KeyboardProcessingResult explicitlyReset = engine.ProcessKeyboardInput(map, Key("b"), second.TypeAhead, items, 1, true, true);
        KeyboardProcessingResult timedOut = engine.ProcessKeyboardInput(map, Key("b"), second.TypeAhead, items, 2, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsTypeAhead, Is.True);
            Assert.That(first.Action, Is.EqualTo("type-ahead"));
            Assert.That(first.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(first.TypeAhead.Buffer, Is.EqualTo("a"));
            Assert.That(second.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(second.TypeAhead.Buffer, Is.EqualTo("al"));
            Assert.That(explicitlyReset.MatchedKey, Is.EqualTo("beta"));
            Assert.That(explicitlyReset.TypeAhead.Buffer, Is.EqualTo("b"));
            Assert.That(explicitlyReset.TypeAhead.Generation, Is.EqualTo(1));
            Assert.That(timedOut.MatchedKey, Is.EqualTo("beta"));
            Assert.That(timedOut.TypeAhead.Buffer, Is.EqualTo("b"));
            Assert.That(timedOut.TypeAhead.Generation, Is.EqualTo(2));
            Assert.That(original.Buffer, Is.Empty);
        });
    }

    [Test]
    public void RepeatedTypeAheadCharactersCycleThroughMatchingItems()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiValueEntry[] items = Items("alpha", "Alpha", "alpine", "Alpine", "beta", "Beta");

        KeyboardProcessingResult first = engine.ProcessKeyboardInput(map, Key("a"), new UiTypeAheadState(), items, 0, true, false);
        KeyboardProcessingResult second = engine.ProcessKeyboardInput(map, Key("a"), first.TypeAhead, items, 0, true, false);
        KeyboardProcessingResult third = engine.ProcessKeyboardInput(map, Key("a"), second.TypeAhead, items, 0, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(first.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(second.MatchedKey, Is.EqualTo("alpine"));
            Assert.That(third.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(third.TypeAhead.Buffer, Is.EqualTo("aaa"));
        });
    }

    [Test]
    public void HonorsCaseSensitivityAndLeavesUnmatchedCharactersUnhandled()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("menu", "menu", UiOrientation.Vertical, false);
        UiValueEntry[] items = Items("beta", "Beta");
        UiTypeAheadState state = new UiTypeAheadState { IgnoreCase = false, Generation = 4 };

        KeyboardProcessingResult lower = engine.ProcessKeyboardInput(map, Key("b"), state, items, 4, true, false);
        KeyboardProcessingResult upper = engine.ProcessKeyboardInput(map, Key("B"), lower.TypeAhead, items, 4, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(lower.IsTypeAhead, Is.True);
            Assert.That(lower.Handled, Is.False);
            Assert.That(lower.MatchedKey, Is.Null);
            Assert.That(lower.TypeAhead.Buffer, Is.EqualTo("b"));
            Assert.That(upper.Handled, Is.True);
            Assert.That(upper.MatchedKey, Is.EqualTo("beta"));
            Assert.That(upper.TypeAhead.Buffer, Is.EqualTo("B"));
            Assert.That(upper.TypeAhead.IgnoreCase, Is.False);
        });
    }

    [Test]
    public void MappedActionsAndExplicitResetClearTypeAheadState()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "al", MatchedKey = "alpha", Generation = 3, IgnoreCase = false };

        KeyboardProcessingResult action = Process(engine, map, Key(AriaKeyNames.ArrowDown), state, 3, true, false);
        UiTypeAheadState reset = engine.ResetKeyboardState(state, 4);

        Assert.Multiple(() =>
        {
            Assert.That(action.Handled, Is.True);
            Assert.That(action.Action, Is.EqualTo("next"));
            Assert.That(action.TypeAhead.Buffer, Is.Empty);
            Assert.That(action.TypeAhead.MatchedKey, Is.Null);
            Assert.That(reset.Buffer, Is.Empty);
            Assert.That(reset.MatchedKey, Is.Null);
            Assert.That(reset.Generation, Is.EqualTo(4));
            Assert.That(reset.IgnoreCase, Is.False);
            Assert.That(reset, Is.Not.SameAs(state));
            Assert.That(state.Buffer, Is.EqualTo("al"));
        });
    }

    [Test]
    public void RejectsInvalidProcessingInputs()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("button", "button", UiOrientation.Horizontal, false);
        UiKeyboardData key = Key(AriaKeyNames.Enter);
        UiTypeAheadState state = new UiTypeAheadState();
        UiValueEntry[] items = Array.Empty<UiValueEntry>();

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.ProcessKeyboardInput(null, key, state, items, 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("map"));
            Assert.That(() => engine.ProcessKeyboardInput(map, null, state, items, 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("keyboard"));
            Assert.That(() => engine.ProcessKeyboardInput(map, key, null, items, 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("typeAhead"));
            Assert.That(() => engine.ProcessKeyboardInput(map, key, state, null, 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("items"));
            Assert.That(() => engine.ProcessKeyboardInput(map, key, state, items, -1, true, false), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("generation"));
            Assert.That(() => engine.ResetKeyboardState(null, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("typeAhead"));
            Assert.That(() => engine.ResetKeyboardState(state, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });

        map.Mappings = null!;
        Assert.That(() => engine.ProcessKeyboardInput(map, key, state, items, 0, true, false), Throws.ArgumentException.With.Property("ParamName").EqualTo("map"));
    }

    private static KeyboardProcessingResult Process(KeyboardEngine engine, UiKeyboardMap map, UiKeyboardData keyboard, UiTypeAheadState state)
    {
        return engine.ProcessKeyboardInput(map, keyboard, state, Array.Empty<UiValueEntry>(), 0, true, false);
    }

    private static KeyboardProcessingResult Process(KeyboardEngine engine, UiKeyboardMap map, UiKeyboardData keyboard, UiTypeAheadState state, int generation, bool allowRepeat, bool resetTypeAhead)
    {
        return engine.ProcessKeyboardInput(map, keyboard, state, Array.Empty<UiValueEntry>(), generation, allowRepeat, resetTypeAhead);
    }

    private static UiKeyboardData Key(string key)
    {
        return new UiKeyboardData { Key = key };
    }

    private static UiValueEntry[] Items(params string[] values)
    {
        UiValueEntry[] result = new UiValueEntry[values.Length / 2];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new UiValueEntry
            {
                Key = values[index * 2],
                Value = new UiValue { Kind = UiValueKind.String, StringValue = values[index * 2 + 1] }
            };
            index++;
        }

        return result;
    }
}
