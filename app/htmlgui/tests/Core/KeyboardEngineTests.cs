using System;
using htmlgui;

namespace htmlgui.Tests;

public class KeyboardEngineTests
{
    [Test]
    public void ButtonAndDisclosureMapsExposeTheirRequiredActivationKeys()
    {
        KeyboardEngine engine = new KeyboardEngine();

        UiKeyboardMap button = engine.CreateKeyboardMap("save", "toggle-button", UiOrientation.Horizontal, false);
        UiKeyboardMap disclosure = engine.CreateKeyboardMap("details", "collapsible-region", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            Assert.That(button.Widget, Is.EqualTo("button"));
            AssertMap(button, AriaKeyNames.Enter, "activate", AriaKeyNames.Space, "activate");
            Assert.That(disclosure.Widget, Is.EqualTo("disclosure"));
            AssertMap(disclosure, AriaKeyNames.Enter, "toggle", AriaKeyNames.Space, "toggle");
        });
    }

    [Test]
    public void RadioAndListBoxMapsProvideOrientationSelectionAndBoundaryActions()
    {
        KeyboardEngine engine = new KeyboardEngine();

        UiKeyboardMap radio = engine.CreateKeyboardMap("choices", "radiogroup", UiOrientation.Horizontal, true);
        UiKeyboardMap listBox = engine.CreateKeyboardMap("results", "list-box", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            Assert.That(radio.Widget, Is.EqualTo("radio"));
            Assert.That(radio.IsRightToLeft, Is.True);
            AssertMap(
                radio,
                AriaKeyNames.ArrowRight, "next-select",
                AriaKeyNames.ArrowDown, "next-select",
                AriaKeyNames.ArrowLeft, "previous-select",
                AriaKeyNames.ArrowUp, "previous-select",
                AriaKeyNames.Space, "select");
            AssertMap(
                listBox,
                AriaKeyNames.ArrowDown, "next",
                AriaKeyNames.ArrowUp, "previous",
                AriaKeyNames.Home, "first",
                AriaKeyNames.End, "last",
                AriaKeyNames.Space, "toggle-selection",
                "Shift+ArrowDown", "next-toggle-selection",
                "Shift+ArrowUp", "previous-toggle-selection",
                "Shift+Space", "extend-selection",
                "Primary+Shift+Home", "extend-selection-first",
                "Primary+Shift+End", "extend-selection-last",
                "Primary+A", "toggle-all-selection");
        });
    }

    [Test]
    public void ComboMapProvidesPopupTraversalAcceptanceAndDismissalActions()
    {
        UiKeyboardMap map = new KeyboardEngine().CreateKeyboardMap("city", "combobox-input", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            Assert.That(map.Widget, Is.EqualTo("combo"));
            AssertMap(
                map,
                AriaKeyNames.ArrowDown, "open-or-next",
                AriaKeyNames.ArrowUp, "open-last-or-previous",
                AriaKeyNames.Enter, "accept",
                AriaKeyNames.Escape, "close",
                "Alt+ArrowDown", "open",
                "Alt+ArrowUp", "accept-close");
        });
    }

    [Test]
    public void MenuMapsDistinguishItemTraversalFromSubmenuTraversal()
    {
        KeyboardEngine engine = new KeyboardEngine();

        UiKeyboardMap menu = engine.CreateKeyboardMap("file", "popup-menu", UiOrientation.Vertical, false);
        UiKeyboardMap menuBar = engine.CreateKeyboardMap("main", "menubar", UiOrientation.Horizontal, false);
        UiKeyboardMap menuButton = engine.CreateKeyboardMap("actions", "menu-button", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            AssertMap(
                menu,
                AriaKeyNames.ArrowDown, "next",
                AriaKeyNames.ArrowUp, "previous",
                AriaKeyNames.ArrowRight, "open-submenu",
                AriaKeyNames.ArrowLeft, "close-submenu",
                AriaKeyNames.Home, "first",
                AriaKeyNames.End, "last",
                AriaKeyNames.Enter, "activate-or-open",
                AriaKeyNames.Space, "activate-or-open",
                AriaKeyNames.Escape, "close",
                AriaKeyNames.Tab, "exit",
                "Shift+Tab", "exit");
            AssertMap(
                menuButton,
                AriaKeyNames.Enter, "open-first",
                AriaKeyNames.Space, "open-first",
                AriaKeyNames.ArrowDown, "open-first",
                AriaKeyNames.ArrowUp, "open-last");
            Assert.That(menuBar.Widget, Is.EqualTo("menu-bar"));
            AssertMap(
                menuBar,
                AriaKeyNames.ArrowRight, "next",
                AriaKeyNames.ArrowLeft, "previous",
                AriaKeyNames.ArrowDown, "open-first",
                AriaKeyNames.ArrowUp, "open-last",
                AriaKeyNames.Home, "first",
                AriaKeyNames.End, "last",
                AriaKeyNames.Enter, "activate-or-open",
                AriaKeyNames.Space, "activate-or-open",
                AriaKeyNames.Escape, "close",
                AriaKeyNames.Tab, "exit",
                "Shift+Tab", "exit");
        });
    }

    [Test]
    public void AlternateOrientationsSwapCompositeNavigationAxes()
    {
        KeyboardEngine engine = new KeyboardEngine();

        UiKeyboardMap menu = engine.CreateKeyboardMap("format", "menu", UiOrientation.Horizontal, false);
        UiKeyboardMap menuBar = engine.CreateKeyboardMap("side", "menu-bar", UiOrientation.Vertical, false);
        UiKeyboardMap tree = engine.CreateKeyboardMap("outline", "tree", UiOrientation.Horizontal, false);

        Assert.Multiple(() =>
        {
            Assert.That(Action(menu, AriaKeyNames.ArrowRight), Is.EqualTo("next"));
            Assert.That(Action(menu, AriaKeyNames.ArrowLeft), Is.EqualTo("previous"));
            Assert.That(Action(menu, AriaKeyNames.ArrowDown), Is.EqualTo("open-submenu"));
            Assert.That(Action(menu, AriaKeyNames.ArrowUp), Is.EqualTo("close-submenu"));
            Assert.That(Action(menuBar, AriaKeyNames.ArrowDown), Is.EqualTo("next"));
            Assert.That(Action(menuBar, AriaKeyNames.ArrowUp), Is.EqualTo("previous"));
            Assert.That(Action(menuBar, AriaKeyNames.ArrowRight), Is.EqualTo("open-first"));
            Assert.That(Action(menuBar, AriaKeyNames.ArrowLeft), Is.EqualTo("close-submenu"));
            Assert.That(Action(tree, AriaKeyNames.ArrowRight), Is.EqualTo("next"));
            Assert.That(Action(tree, AriaKeyNames.ArrowLeft), Is.EqualTo("previous"));
            Assert.That(Action(tree, AriaKeyNames.ArrowDown), Is.EqualTo("expand-or-child"));
            Assert.That(Action(tree, AriaKeyNames.ArrowUp), Is.EqualTo("collapse-or-parent"));
            Assert.That(Action(tree, "Shift+ArrowRight"), Is.EqualTo("next-toggle-selection"));
        });
    }

    [Test]
    public void TabsToolbarAndDialogMapsProvideCompositeFocusNavigation()
    {
        KeyboardEngine engine = new KeyboardEngine();

        UiKeyboardMap tabs = engine.CreateKeyboardMap("account-tabs", "tablist", UiOrientation.Vertical, false);
        UiKeyboardMap toolbar = engine.CreateKeyboardMap("editor", "toolbar", UiOrientation.Horizontal, false);
        UiKeyboardMap dialog = engine.CreateKeyboardMap("confirm", "modal-dialog", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            AssertMap(
                tabs,
                AriaKeyNames.ArrowDown, "next",
                AriaKeyNames.ArrowUp, "previous",
                AriaKeyNames.Home, "first",
                AriaKeyNames.End, "last",
                AriaKeyNames.Enter, "activate",
                AriaKeyNames.Space, "activate",
                "Shift+F10", "open-menu",
                AriaKeyNames.Delete, "delete");
            AssertMap(
                toolbar,
                AriaKeyNames.ArrowRight, "next",
                AriaKeyNames.ArrowLeft, "previous",
                AriaKeyNames.Home, "first",
                AriaKeyNames.End, "last");
            AssertMap(
                dialog,
                AriaKeyNames.Tab, "next",
                "Shift+Tab", "previous",
                AriaKeyNames.Escape, "close");
        });
    }

    [Test]
    public void GridMapProvidesTwoDimensionalBoundaryPagingAndActivationActions()
    {
        UiKeyboardMap map = new KeyboardEngine().CreateKeyboardMap("orders", "data-grid", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            Assert.That(map.Widget, Is.EqualTo("grid"));
            AssertMap(
                map,
                AriaKeyNames.ArrowRight, "next-column",
                AriaKeyNames.ArrowLeft, "previous-column",
                AriaKeyNames.ArrowDown, "next-row",
                AriaKeyNames.ArrowUp, "previous-row",
                AriaKeyNames.Home, "first-column",
                AriaKeyNames.End, "last-column",
                "Primary+Home", "first-cell",
                "Primary+End", "last-cell",
                AriaKeyNames.PageUp, "page-previous",
                AriaKeyNames.PageDown, "page-next",
                "Primary+Space", "select-column",
                "Shift+Space", "select-row",
                "Primary+A", "select-all",
                AriaKeyNames.Enter, "enter-cell",
                AriaKeyNames.F2, "enter-cell",
                AriaKeyNames.Escape, "exit-cell",
                AriaKeyNames.Tab, "next-control",
                "Shift+Tab", "previous-control");
        });
    }

    [Test]
    public void TreeAndTreeGridMapsProvideHierarchyAndCellTraversalActions()
    {
        KeyboardEngine engine = new KeyboardEngine();

        UiKeyboardMap tree = engine.CreateKeyboardMap("files", "tree-view", UiOrientation.Vertical, false);
        UiKeyboardMap treeGrid = engine.CreateKeyboardMap("outline", "tree-grid", UiOrientation.Vertical, false);

        Assert.Multiple(() =>
        {
            AssertMap(
                tree,
                AriaKeyNames.ArrowDown, "next",
                AriaKeyNames.ArrowUp, "previous",
                AriaKeyNames.ArrowRight, "expand-or-child",
                AriaKeyNames.ArrowLeft, "collapse-or-parent",
                AriaKeyNames.Home, "first",
                AriaKeyNames.End, "last",
                AriaKeyNames.Enter, "activate",
                AriaKeyNames.Space, "toggle-selection",
                "*", "expand-siblings",
                "Shift+ArrowDown", "next-toggle-selection",
                "Shift+ArrowUp", "previous-toggle-selection",
                "Shift+Space", "extend-selection",
                "Primary+Shift+Home", "extend-selection-first",
                "Primary+Shift+End", "extend-selection-last",
                "Primary+A", "toggle-all-selection");
            Assert.That(treeGrid.Widget, Is.EqualTo("treegrid"));
            AssertMap(
                treeGrid,
                AriaKeyNames.ArrowDown, "next-row",
                AriaKeyNames.ArrowUp, "previous-row",
                AriaKeyNames.ArrowRight, "expand-or-next-column",
                AriaKeyNames.ArrowLeft, "collapse-or-previous-column",
                AriaKeyNames.Home, "first-column",
                AriaKeyNames.End, "last-column",
                "Primary+Home", "first-cell",
                "Primary+End", "last-cell",
                AriaKeyNames.PageUp, "page-previous",
                AriaKeyNames.PageDown, "page-next",
                AriaKeyNames.Enter, "activate-or-toggle",
                AriaKeyNames.Tab, "next-control",
                "Primary+Space", "select-column",
                "Shift+Space", "select-row",
                "Primary+A", "select-all",
                "Shift+ArrowRight", "extend-next-column",
                "Shift+ArrowLeft", "extend-previous-column",
                "Shift+ArrowDown", "extend-next-row",
                "Shift+ArrowUp", "extend-previous-row");
        });
    }

    [Test]
    public void SliderMapProvidesSmallLargeAndBoundaryValueChanges()
    {
        UiKeyboardMap map = new KeyboardEngine().CreateKeyboardMap("volume", "slider", UiOrientation.Horizontal, false);

        AssertMap(
            map,
            AriaKeyNames.ArrowRight, "increment",
            AriaKeyNames.ArrowUp, "increment",
            AriaKeyNames.ArrowLeft, "decrement",
            AriaKeyNames.ArrowDown, "decrement",
            AriaKeyNames.Home, "minimum",
            AriaKeyNames.End, "maximum",
            AriaKeyNames.PageUp, "increment-large",
            AriaKeyNames.PageDown, "decrement-large");
    }

    [TestCase("command-button", "button")]
    [TestCase("radio-group", "radio")]
    [TestCase("autocomplete", "combo")]
    [TestCase("context-menu", "menu")]
    [TestCase("menu-bar", "menu-bar")]
    [TestCase("tab-view", "tabs")]
    [TestCase("prompt-dialog", "dialog")]
    [TestCase("data-table", "grid")]
    [TestCase("collapsible", "disclosure")]
    public void NormalizesExistingControlAliases(string widget, string canonicalWidget)
    {
        UiKeyboardMap map = new KeyboardEngine().CreateKeyboardMap("map", widget, UiOrientation.Horizontal, false);

        Assert.Multiple(() =>
        {
            Assert.That(map.Id, Is.EqualTo("map"));
            Assert.That(map.Widget, Is.EqualTo(canonicalWidget));
            Assert.That(map.Mappings, Is.Not.Empty);
        });
    }

    [Test]
    public void RejectsInvalidIdentifiersWidgetsAndOrientations()
    {
        KeyboardEngine engine = new KeyboardEngine();

        Assert.Multiple(() =>
        {
            Assert.That(() => engine.CreateKeyboardMap(null, "button", UiOrientation.Horizontal, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("id"));
            Assert.That(() => engine.CreateKeyboardMap(" ", "button", UiOrientation.Horizontal, false), Throws.ArgumentException.With.Property("ParamName").EqualTo("id"));
            Assert.That(() => engine.CreateKeyboardMap("map", null, UiOrientation.Horizontal, false), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("widget"));
            Assert.That(() => engine.CreateKeyboardMap("map", "unknown", UiOrientation.Horizontal, false), Throws.ArgumentException.With.Property("ParamName").EqualTo("widget"));
            Assert.That(() => engine.CreateKeyboardMap("map", "button", (UiOrientation)99, false), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("orientation"));
        });
    }

    private static void AssertMap(UiKeyboardMap map, params string[] expected)
    {
        Assert.That(expected.Length % 2, Is.Zero);
        Assert.That(map.Mappings, Has.Length.EqualTo(expected.Length / 2));
        int index = 0;
        while (index < map.Mappings.Length)
        {
            UiValueEntry mapping = map.Mappings[index];
            Assert.Multiple(() =>
            {
                Assert.That(mapping.Key, Is.EqualTo(expected[index * 2]));
                Assert.That(mapping.Value.Kind, Is.EqualTo(UiValueKind.String));
                Assert.That(mapping.Value.StringValue, Is.EqualTo(expected[index * 2 + 1]));
            });
            index++;
        }
    }

    private static string? Action(UiKeyboardMap map, string key)
    {
        int index = 0;
        while (index < map.Mappings.Length)
        {
            UiValueEntry mapping = map.Mappings[index];
            if (mapping.Key == key)
            {
                return mapping.Value.StringValue;
            }

            index++;
        }

        return null;
    }
}
