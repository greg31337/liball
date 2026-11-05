using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class KeyboardCoverageTests
{
    [TestCase("button", 2, AriaKeyNames.Enter, "activate")]
    [TestCase("radio", 5, AriaKeyNames.ArrowRight, "next-select")]
    [TestCase("listbox", 11, AriaKeyNames.ArrowRight, "next")]
    [TestCase("combo", 6, AriaKeyNames.ArrowDown, "open-or-next")]
    [TestCase("menu", 11, AriaKeyNames.ArrowRight, "next")]
    [TestCase("menu-button", 4, AriaKeyNames.Enter, "open-first")]
    [TestCase("menu-bar", 11, AriaKeyNames.ArrowRight, "next")]
    [TestCase("tabs", 8, AriaKeyNames.ArrowRight, "next")]
    [TestCase("toolbar", 4, AriaKeyNames.ArrowRight, "next")]
    [TestCase("dialog", 3, AriaKeyNames.Tab, "next")]
    [TestCase("grid", 18, AriaKeyNames.ArrowRight, "next-column")]
    [TestCase("tree", 15, AriaKeyNames.ArrowRight, "next")]
    [TestCase("treegrid", 19, AriaKeyNames.ArrowRight, "expand-or-next-column")]
    [TestCase("slider", 8, AriaKeyNames.ArrowRight, "increment")]
    [TestCase("disclosure", 2, AriaKeyNames.Enter, "toggle")]
    public void CreatesEveryApgMapWithItsCanonicalIdentity(string widget, int mappingCount, string key, string action)
    {
        UiKeyboardMap map = new KeyboardEngine().CreateKeyboardMap("map-" + widget, widget, UiOrientation.Horizontal, false);

        Assert.Multiple(() =>
        {
            Assert.That(map.Id, Is.EqualTo("map-" + widget));
            Assert.That(map.Widget, Is.EqualTo(widget));
            Assert.That(map.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(map.Mappings, Has.Length.EqualTo(mappingCount));
            Assert.That(Action(map, key), Is.EqualTo(action));
        });
    }

    [TestCase("listbox", "next")]
    [TestCase("menu", "next")]
    [TestCase("menu-bar", "next")]
    [TestCase("tabs", "next")]
    [TestCase("toolbar", "next")]
    [TestCase("tree", "next")]
    public void MapsEveryAxisSensitiveWidgetInBothOrientations(string widget, string nextAction)
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap horizontal = engine.CreateKeyboardMap("horizontal", widget, UiOrientation.Horizontal, false);
        UiKeyboardMap vertical = engine.CreateKeyboardMap("vertical", widget, UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            Assert.That(Action(horizontal, AriaKeyNames.ArrowRight), Is.EqualTo(nextAction));
            Assert.That(Action(horizontal, AriaKeyNames.ArrowLeft), Is.Not.Null);
            Assert.That(Action(vertical, AriaKeyNames.ArrowDown), Is.EqualTo(nextAction));
            Assert.That(Action(vertical, AriaKeyNames.ArrowUp), Is.Not.Null);
        });
    }

    [TestCase("radio", UiOrientation.Horizontal, "previous-select")]
    [TestCase("listbox", UiOrientation.Horizontal, "previous")]
    [TestCase("menu", UiOrientation.Vertical, "close-submenu")]
    [TestCase("menu-bar", UiOrientation.Horizontal, "previous")]
    [TestCase("tabs", UiOrientation.Horizontal, "previous")]
    [TestCase("toolbar", UiOrientation.Horizontal, "previous")]
    [TestCase("grid", UiOrientation.Horizontal, "previous-column")]
    [TestCase("tree", UiOrientation.Vertical, "collapse-or-parent")]
    [TestCase("treegrid", UiOrientation.Horizontal, "collapse-or-previous-column")]
    [TestCase("slider", UiOrientation.Horizontal, "decrement")]
    public void ReversesEveryHorizontalActionFamilyForRightToLeftMaps(string widget, UiOrientation orientation, string expectedAction)
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("rtl-" + widget, widget, orientation, true);

        KeyboardProcessingResult result = engine.ProcessKeyboardInput(
            map,
            new UiKeyboardData { Key = AriaKeyNames.ArrowRight },
            new UiTypeAheadState(),
            Array.Empty<UiValueEntry>(),
            0,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.NormalizedKey, Is.EqualTo(AriaKeyNames.ArrowRight));
            Assert.That(result.Chord, Is.EqualTo(AriaKeyNames.ArrowLeft));
            Assert.That(result.Action, Is.EqualTo(expectedAction));
            Assert.That(result.Handled, Is.True);
        });
    }

    [TestCase(false, false, false, "X")]
    [TestCase(true, false, false, "Primary+X")]
    [TestCase(false, true, false, "Alt+X")]
    [TestCase(false, false, true, "Shift+X")]
    [TestCase(true, true, false, "Primary+Alt+X")]
    [TestCase(true, false, true, "Primary+Shift+X")]
    [TestCase(false, true, true, "Alt+Shift+X")]
    [TestCase(true, true, true, "Primary+Alt+Shift+X")]
    public void CreatesModifierChordsInCanonicalPlatformOrder(bool primary, bool alt, bool shift, string expected)
    {
        string result = new KeyboardEngine().CreateKeyboardChord("X", primary, alt, shift);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(false, false, false, " ")]
    [TestCase(true, false, false, "Primary+Space")]
    [TestCase(false, true, false, "Alt+Space")]
    [TestCase(false, false, true, "Shift+Space")]
    [TestCase(true, true, true, "Primary+Alt+Shift+Space")]
    public void UsesTheCanonicalSpaceTokenOnlyInsideModifierChords(bool primary, bool alt, bool shift, string expected)
    {
        string result = new KeyboardEngine().CreateKeyboardChord(AriaKeyNames.Space, primary, alt, shift);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Enter", AriaKeyNames.Enter)]
    [TestCase("NumpadEnter", AriaKeyNames.Enter)]
    [TestCase("Space", AriaKeyNames.Space)]
    [TestCase("Escape", AriaKeyNames.Escape)]
    [TestCase("ArrowLeft", AriaKeyNames.ArrowLeft)]
    [TestCase("ArrowRight", AriaKeyNames.ArrowRight)]
    [TestCase("ArrowUp", AriaKeyNames.ArrowUp)]
    [TestCase("ArrowDown", AriaKeyNames.ArrowDown)]
    [TestCase("Home", AriaKeyNames.Home)]
    [TestCase("End", AriaKeyNames.End)]
    [TestCase("PageUp", AriaKeyNames.PageUp)]
    [TestCase("PageDown", AriaKeyNames.PageDown)]
    [TestCase("Tab", AriaKeyNames.Tab)]
    [TestCase("Backspace", AriaKeyNames.Backspace)]
    [TestCase("Delete", AriaKeyNames.Delete)]
    [TestCase("F2", AriaKeyNames.F2)]
    [TestCase("NumpadMultiply", "*")]
    [TestCase("KeyZ", "Z")]
    [TestCase("Digit7", "7")]
    [TestCase("CustomCode", "CustomCode")]
    [TestCase(null, "")]
    public void NormalizesEverySupportedPhysicalKeyCode(string? code, string expected)
    {
        string result = new KeyboardEngine().GetKeyboardCodeKey(code);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ResolvesMappedKeyConflictsBeforeTypeAheadAndRejectsDuplicateSequences()
    {
        UiKeyboardMap map = new UiKeyboardMap
        {
            Id = "menu",
            Widget = "menu",
            Orientation = UiOrientation.Vertical,
            Mappings = new UiValueEntry[]
            {
                Mapping("a", "activate-alpha"),
                Mapping("Primary+A", "select-all")
            }
        };
        KeyboardEngine engine = new KeyboardEngine();
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "b", MatchedKey = "beta", Generation = 1 };

        KeyboardProcessingResult plain = engine.ProcessKeyboardInput(map, Key("a"), state, Items(), 1, true, false);
        KeyboardProcessingResult modified = engine.ProcessKeyboardInput(map, new UiKeyboardData { Key = "a", PrimaryModifier = true }, state, Items(), 1, true, false);
        IUiKeyboardController controller = new UiKeyboardController();
        map.Mappings = new UiValueEntry[]
        {
            Mapping("a", "first"),
            Mapping("a", "second")
        };
        UiResult duplicate = controller.RegisterKeyboardMap(map);

        Assert.Multiple(() =>
        {
            Assert.That(plain.Handled, Is.True);
            Assert.That(plain.IsTypeAhead, Is.False);
            Assert.That(plain.Action, Is.EqualTo("activate-alpha"));
            Assert.That(plain.TypeAhead.Buffer, Is.Empty);
            Assert.That(modified.Action, Is.EqualTo("select-all"));
            Assert.That(modified.Chord, Is.EqualTo("Primary+A"));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Diagnostics.Single().Code, Is.EqualTo("keyboard.duplicate-key"));
        });
    }

    [TestCase("listbox")]
    [TestCase("menu")]
    [TestCase("menu-bar")]
    [TestCase("tree")]
    public void SupportsTypeAheadForEveryCompositeThatRequiresIt(string widget)
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("map", widget, UiOrientation.Vertical, false);

        KeyboardProcessingResult result = engine.ProcessKeyboardInput(
            map,
            Key("b"),
            new UiTypeAheadState(),
            Items(),
            0,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTypeAhead, Is.True);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Action, Is.EqualTo("type-ahead"));
            Assert.That(result.MatchedKey, Is.EqualTo("beta"));
        });
    }

    [TestCase("button")]
    [TestCase("radio")]
    [TestCase("combo")]
    [TestCase("menu-button")]
    [TestCase("tabs")]
    [TestCase("toolbar")]
    [TestCase("dialog")]
    [TestCase("grid")]
    [TestCase("treegrid")]
    [TestCase("slider")]
    [TestCase("disclosure")]
    public void DoesNotApplyTypeAheadToOtherApgMaps(string widget)
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("map", widget, UiOrientation.Vertical, false);

        KeyboardProcessingResult result = engine.ProcessKeyboardInput(
            map,
            Key("b"),
            new UiTypeAheadState(),
            Items(),
            0,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTypeAhead, Is.False);
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Action, Is.Null);
            Assert.That(result.TypeAhead.Buffer, Is.Empty);
        });
    }

    [Test]
    public void AppliesRepeatPolicyToTypeAheadAndCyclesWhenRepeatIsAllowed()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "a", MatchedKey = "alpha", Generation = 3 };
        UiKeyboardData repeated = new UiKeyboardData { Key = "a", Repeat = true };

        KeyboardProcessingResult suppressed = engine.ProcessKeyboardInput(map, repeated, state, Items(), 3, false, false);
        KeyboardProcessingResult allowed = engine.ProcessKeyboardInput(map, repeated, state, Items(), 3, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(suppressed.Suppressed, Is.True);
            Assert.That(suppressed.Handled, Is.False);
            Assert.That(suppressed.TypeAhead.Buffer, Is.EqualTo("a"));
            Assert.That(suppressed.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(allowed.Suppressed, Is.False);
            Assert.That(allowed.Handled, Is.True);
            Assert.That(allowed.TypeAhead.Buffer, Is.EqualTo("aa"));
            Assert.That(allowed.MatchedKey, Is.EqualTo("alpine"));
        });
    }

    [Test]
    public void ResetsTypeAheadOnGenerationTimeoutExplicitResetAndNonCharacterInput()
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "al", MatchedKey = "alpha", Generation = 4, IgnoreCase = false };

        KeyboardProcessingResult timedOut = engine.ProcessKeyboardInput(map, Key("b"), state, Items(), 5, true, false);
        KeyboardProcessingResult reset = engine.ProcessKeyboardInput(map, Key("b"), state, Items(), 4, true, true);
        KeyboardProcessingResult nonCharacter = engine.ProcessKeyboardInput(map, Key("F9"), state, Items(), 4, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(timedOut.TypeAhead.Buffer, Is.EqualTo("b"));
            Assert.That(timedOut.TypeAhead.Generation, Is.EqualTo(5));
            Assert.That(timedOut.TypeAhead.IgnoreCase, Is.False);
            Assert.That(reset.TypeAhead.Buffer, Is.EqualTo("b"));
            Assert.That(reset.TypeAhead.Generation, Is.EqualTo(4));
            Assert.That(nonCharacter.Handled, Is.False);
            Assert.That(nonCharacter.TypeAhead.Buffer, Is.Empty);
            Assert.That(nonCharacter.TypeAhead.MatchedKey, Is.Null);
            Assert.That(state.Buffer, Is.EqualTo("al"));
        });
    }

    [TestCase("Dead", false)]
    [TestCase("Process", false)]
    [TestCase("Unidentified", false)]
    [TestCase(AriaKeyNames.ArrowDown, true)]
    public void SuppressesEveryImeCompositionFormWithoutChangingState(string key, bool isComposing)
    {
        KeyboardEngine engine = new KeyboardEngine();
        UiKeyboardMap map = engine.CreateKeyboardMap("list", "listbox", UiOrientation.Vertical, false);
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "a", MatchedKey = "alpha", Generation = 2 };

        KeyboardProcessingResult result = engine.ProcessKeyboardInput(
            map,
            new UiKeyboardData { Key = key, IsComposing = isComposing },
            state,
            Items(),
            2,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Suppressed, Is.True);
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Action, Is.Null);
            Assert.That(result.TypeAhead.Buffer, Is.EqualTo("a"));
            Assert.That(result.TypeAhead.MatchedKey, Is.EqualTo("alpha"));
            Assert.That(result.TypeAhead, Is.Not.SameAs(state));
        });
    }

    private static string? Action(UiKeyboardMap map, string key)
    {
        return map.Mappings.SingleOrDefault(mapping => mapping.Key == key)?.Value.StringValue;
    }

    private static UiKeyboardData Key(string key)
    {
        return new UiKeyboardData { Key = key };
    }

    private static UiValueEntry Mapping(string key, string action)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = action }
        };
    }

    private static UiValueEntry[] Items()
    {
        return new UiValueEntry[]
        {
            Mapping("alpha", "Alpha"),
            Mapping("alpine", "Alpine"),
            Mapping("beta", "Beta")
        };
    }
}
