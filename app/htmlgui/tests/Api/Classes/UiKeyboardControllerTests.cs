using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiKeyboardControllerTests
{
    [Test]
    public void CreatesRegistersAndProcessesAnIndependentApgMap()
    {
        IUiKeyboardController controller = new UiKeyboardController();
        UiKeyboardMap map = controller.CreateKeyboardMap("save", "toggle-button", UiOrientation.Horizontal, false);

        UiResult registration = controller.RegisterKeyboardMap(map);
        map.Mappings[0].Value.StringValue = "changed";
        UiKeyboardResult result = controller.ProcessKeyboardInput(
            "save",
            new UiKeyboardData { Key = AriaKeyNames.Enter },
            new UiTypeAheadState(),
            Array.Empty<UiValueEntry>(),
            0,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(map.Widget, Is.EqualTo("button"));
            Assert.That(registration.Success, Is.True);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Suppressed, Is.False);
            Assert.That(result.NormalizedKey, Is.EqualTo(AriaKeyNames.Enter));
            Assert.That(result.Chord, Is.EqualTo(AriaKeyNames.Enter));
            Assert.That(result.Action, Is.EqualTo("activate"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ReplacesMapsAndProcessesModifierShortcuts()
    {
        IUiKeyboardController controller = new UiKeyboardController();
        controller.RegisterKeyboardMap(controller.CreateKeyboardMap("commands", "button", UiOrientation.Horizontal, false));
        UiKeyboardMap replacement = new UiKeyboardMap
        {
            Id = "commands",
            Widget = "shortcut-scope",
            Orientation = UiOrientation.Horizontal,
            Mappings = new UiValueEntry[]
            {
                Mapping("Primary+K", "open-command-palette")
            }
        };

        UiResult registration = controller.RegisterKeyboardMap(replacement);
        UiKeyboardResult result = controller.ProcessKeyboardInput(
            "commands",
            new UiKeyboardData { Key = "k", PrimaryModifier = true },
            new UiTypeAheadState(),
            Array.Empty<UiValueEntry>(),
            0,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(registration.Success, Is.True);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.NormalizedKey, Is.EqualTo("K"));
            Assert.That(result.Chord, Is.EqualTo("Primary+K"));
            Assert.That(result.Action, Is.EqualTo("open-command-palette"));
        });
    }

    [Test]
    public void ProcessesTypeAheadAndResetsIndependentState()
    {
        IUiKeyboardController controller = new UiKeyboardController();
        controller.RegisterKeyboardMap(controller.CreateKeyboardMap("cities", "listbox", UiOrientation.Vertical, false));
        UiTypeAheadState original = new UiTypeAheadState { Buffer = "", Generation = 4, IgnoreCase = true };
        UiValueEntry[] items = new UiValueEntry[]
        {
            Mapping("alpha", "Alpha"),
            Mapping("beta", "Beta")
        };

        UiKeyboardResult typed = controller.ProcessKeyboardInput(
            "cities",
            new UiKeyboardData { Key = "b" },
            original,
            items,
            4,
            true,
            false);
        UiTypeAheadState reset = controller.ResetKeyboardState(typed.TypeAhead, 5);

        Assert.Multiple(() =>
        {
            Assert.That(typed.Handled, Is.True);
            Assert.That(typed.IsTypeAhead, Is.True);
            Assert.That(typed.Action, Is.EqualTo("type-ahead"));
            Assert.That(typed.MatchedKey, Is.EqualTo("beta"));
            Assert.That(typed.TypeAhead.Buffer, Is.EqualTo("b"));
            Assert.That(original.Buffer, Is.Empty);
            Assert.That(reset.Buffer, Is.Empty);
            Assert.That(reset.MatchedKey, Is.Null);
            Assert.That(reset.Generation, Is.EqualTo(5));
            Assert.That(reset, Is.Not.SameAs(typed.TypeAhead));
        });
    }

    [Test]
    public void AppliesRtlCompositionAndRepeatPolicies()
    {
        IUiKeyboardController controller = new UiKeyboardController();
        controller.RegisterKeyboardMap(controller.CreateKeyboardMap("toolbar", "toolbar", UiOrientation.Horizontal, true));
        UiTypeAheadState state = new UiTypeAheadState { Buffer = "a", MatchedKey = "alpha", Generation = 2 };

        UiKeyboardResult rtl = Process(controller, new UiKeyboardData { Key = AriaKeyNames.ArrowRight }, state, true);
        UiKeyboardResult composing = Process(controller, new UiKeyboardData { Key = AriaKeyNames.ArrowRight, IsComposing = true }, state, true);
        UiKeyboardResult repeated = Process(controller, new UiKeyboardData { Key = AriaKeyNames.ArrowRight, Repeat = true }, state, false);

        Assert.Multiple(() =>
        {
            Assert.That(rtl.Action, Is.EqualTo("previous"));
            Assert.That(rtl.Chord, Is.EqualTo(AriaKeyNames.ArrowLeft));
            Assert.That(composing.Suppressed, Is.True);
            Assert.That(composing.Handled, Is.False);
            Assert.That(composing.TypeAhead.Buffer, Is.EqualTo("a"));
            Assert.That(repeated.Suppressed, Is.True);
            Assert.That(repeated.Handled, Is.False);
        });
    }

    [Test]
    public void ReportsInvalidRegistrationsAndUnknownMaps()
    {
        IUiKeyboardController controller = new UiKeyboardController();
        UiKeyboardMap invalid = new UiKeyboardMap
        {
            Id = "bad",
            Widget = "custom",
            Orientation = (UiOrientation)99,
            Mappings = new UiValueEntry[]
            {
                Mapping("Enter", "first"),
                Mapping("Enter", "second"),
                Mapping("Space", "")
            }
        };

        UiResult missing = controller.RegisterKeyboardMap(null);
        UiResult malformed = controller.RegisterKeyboardMap(invalid);
        UiKeyboardResult unknown = controller.ProcessKeyboardInput(
            "missing",
            new UiKeyboardData { Key = AriaKeyNames.Enter },
            new UiTypeAheadState { Buffer = "a" },
            Array.Empty<UiValueEntry>(),
            0,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("keyboard.null-map"));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics.Select(item => item.Code), Does.Contain("keyboard.invalid-orientation"));
            Assert.That(malformed.Diagnostics.Select(item => item.Code), Does.Contain("keyboard.duplicate-key"));
            Assert.That(malformed.Diagnostics.Select(item => item.Code), Does.Contain("keyboard.invalid-action"));
            Assert.That(unknown.Handled, Is.False);
            Assert.That(unknown.Diagnostics.Single().Code, Is.EqualTo("keyboard.unknown-map"));
            Assert.That(unknown.TypeAhead.Buffer, Is.EqualTo("a"));
        });
    }

    [Test]
    public void RejectsMalformedProcessingArguments()
    {
        IUiKeyboardController controller = new UiKeyboardController();
        controller.RegisterKeyboardMap(controller.CreateKeyboardMap("button", "button", UiOrientation.Horizontal, false));
        UiKeyboardData keyboard = new UiKeyboardData { Key = AriaKeyNames.Enter };
        UiTypeAheadState state = new UiTypeAheadState();

        Assert.Multiple(() =>
        {
            Assert.That(() => controller.ProcessKeyboardInput("button", null, state, Array.Empty<UiValueEntry>(), 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("keyboard"));
            Assert.That(() => controller.ProcessKeyboardInput("button", keyboard, null, Array.Empty<UiValueEntry>(), 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("typeAhead"));
            Assert.That(() => controller.ProcessKeyboardInput("button", keyboard, state, null, 0, true, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("items"));
            Assert.That(() => controller.ProcessKeyboardInput("button", keyboard, state, Array.Empty<UiValueEntry>(), -1, true, false), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("generation"));
            Assert.That(() => controller.ResetKeyboardState(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("typeAhead"));
        });
    }

    private static UiKeyboardResult Process(IUiKeyboardController controller, UiKeyboardData keyboard, UiTypeAheadState state, bool allowRepeat)
    {
        return controller.ProcessKeyboardInput("toolbar", keyboard, state, Array.Empty<UiValueEntry>(), 2, allowRepeat, false);
    }

    private static UiValueEntry Mapping(string key, string action)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = action }
        };
    }
}
