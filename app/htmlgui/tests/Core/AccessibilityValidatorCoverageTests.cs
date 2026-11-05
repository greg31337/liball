using System;
using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class AccessibilityValidatorCoverageTests
{
    [Test]
    public void AccumulatesEveryAccessibilityReportCategoryAtStableLocations()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode invalidRole = Node("invalid-role", "invalid-role-id", root.Identity.StableId, "div");
        invalidRole.Accessibility = new UiAccessibility { Role = "imaginary" };
        UiNode redundantRole = Node("redundant", "redundant-id", root.Identity.StableId, HtmlElements.Button);
        redundantRole.Text = "Save";
        redundantRole.Accessibility = new UiAccessibility { Role = AriaRoles.Button };
        UiNode prohibited = RoleNode("prohibited", "prohibited-id", root.Identity.StableId, AriaRoles.Presentation, "Decorative");
        UiNode listBox = RoleNode("listbox", "listbox-id", root.Identity.StableId, AriaRoles.ListBox, "Results");
        listBox.State.IsFocusable = true;
        UiNode option = RoleNode("option", "option-id", root.Identity.StableId, AriaRoles.Option, "Result");
        option.State.IsFocusable = true;
        option.State.HasSelected = true;
        UiNode checkbox = RoleNode("checkbox", "checkbox-id", root.Identity.StableId, AriaRoles.Checkbox, "Enable");
        checkbox.State.IsFocusable = true;
        UiNode firstId = Node("first-id", "first-id-node", root.Identity.StableId, "div");
        firstId.Attributes = new DomAttribute[] { Attribute(HtmlAttributes.Id, "shared-id") };
        UiNode duplicateId = Node("duplicate-id", "duplicate-id-node", root.Identity.StableId, "div");
        duplicateId.Attributes = new DomAttribute[] { Attribute(HtmlAttributes.Id, "shared-id") };
        UiNode brokenReference = Node("broken", "broken-id", root.Identity.StableId, "div");
        brokenReference.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                Relationship(AriaAttributes.Controls, "missing-id")
            }
        };
        UiNode unnamed = RoleNode("unnamed", "unnamed-id", root.Identity.StableId, AriaRoles.Button, null);
        unnamed.State.IsFocusable = true;
        unnamed.EventBindings = new UiEventBinding[] { Binding(UiEventKind.KeyDown) };
        UiNode hidden = Node("hidden", "hidden-id", root.Identity.StableId, HtmlElements.Button);
        hidden.Text = "Hidden action";
        hidden.Accessibility = new UiAccessibility { IsHidden = true };
        UiNode pointerOnly = Node("pointer", "pointer-id", root.Identity.StableId, "div");
        pointerOnly.State.IsFocusable = true;
        pointerOnly.EventBindings = new UiEventBinding[] { Binding(UiEventKind.Click) };
        UiNode support = Node("support", "support-id", root.Identity.StableId, "div");
        support.Accessibility = new UiAccessibility
        {
            Features = new UiAccessibilityFeature[]
            {
                (UiAccessibilityFeature)99,
                UiAccessibilityFeature.TargetSize,
                UiAccessibilityFeature.NonColorStatus
            },
            MinimumTargetSizePixels = 20,
            StatusText = " "
        };
        root.Children = new UiNode[]
        {
            invalidRole,
            redundantRole,
            prohibited,
            listBox,
            option,
            checkbox,
            firstId,
            duplicateId,
            brokenReference,
            unnamed,
            hidden,
            pointerOnly,
            support
        };

        UiResult result = new UiModelValidator().TryValidateModel(application);
        Dictionary<string, string> expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "accessibility.invalid-role", "/children/0/accessibility/role" },
            { "accessibility.redundant-role", "/children/1/accessibility/role" },
            { "accessibility.prohibited-aria", "/children/2" },
            { "accessibility.required-owned", "/children/3/accessibility/role" },
            { "accessibility.required-context", "/children/4/accessibility/role" },
            { "accessibility.required-property", "/children/5/accessibility/role" },
            { "accessibility.duplicate-id", "/children/7/attributes/0/value" },
            { "accessibility.broken-reference", "/children/8/accessibility/relationships/0/targetIds/0" },
            { "accessibility.missing-name", "/children/9/accessibility/role" },
            { "accessibility.hidden-focus", "/children/10/state" },
            { "accessibility.keyboard-access", "/children/11/eventBindings" },
            { "accessibility.invalid-feature", "/children/12/accessibility/features/0" },
            { "accessibility.target-size", "/children/12/accessibility/minimumTargetSizePixels" },
            { "accessibility.non-color-status", "/children/12/accessibility/statusText" }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(
                result.Diagnostics
                    .Where(diagnostic => diagnostic.Code.StartsWith("accessibility.", StringComparison.Ordinal))
                    .Select(diagnostic => diagnostic.Code)
                    .Distinct(),
                Is.EquivalentTo(expected.Keys));
            Assert.That(result.Diagnostics.Single(diagnostic => diagnostic.Code == "accessibility.redundant-role").Severity, Is.EqualTo(UiDiagnosticSeverity.Warning));
            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Code != "accessibility.redundant-role").All(diagnostic => diagnostic.Severity == UiDiagnosticSeverity.Error), Is.True);
            foreach (KeyValuePair<string, string> item in expected)
            {
                Assert.That(HasDiagnostic(result, item.Key, item.Value), Is.True, item.Key);
            }
        });
    }

    [TestCase(AriaRoles.Option)]
    [TestCase(AriaRoles.MenuItem)]
    [TestCase(AriaRoles.MenuItemCheckbox)]
    [TestCase(AriaRoles.MenuItemRadio)]
    [TestCase(AriaRoles.Radio)]
    [TestCase(AriaRoles.Tab)]
    [TestCase(AriaRoles.TreeItem)]
    [TestCase(AriaRoles.Row)]
    [TestCase(AriaRoles.Cell)]
    [TestCase(AriaRoles.GridCell)]
    [TestCase(AriaRoles.RowHeader)]
    [TestCase(AriaRoles.ColumnHeader)]
    public void ReportsEveryMissingRequiredContext(string role)
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode target = RoleNode("target", "target-id", root.Identity.StableId, role, "Target");
        target.State.IsFocusable = true;
        target.State.HasSelected = true;
        target.State.HasCheckState = true;
        root.Children = new UiNode[] { target };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.That(HasDiagnostic(result, "accessibility.required-context", "/children/0/accessibility/role"), Is.True);
    }

    [TestCase(AriaRoles.ListBox)]
    [TestCase(AriaRoles.Menu)]
    [TestCase(AriaRoles.MenuBar)]
    [TestCase(AriaRoles.RadioGroup)]
    [TestCase(AriaRoles.TabList)]
    [TestCase(AriaRoles.Tree)]
    [TestCase(AriaRoles.Grid)]
    [TestCase(AriaRoles.TreeGrid)]
    [TestCase(AriaRoles.Row)]
    public void ReportsEveryMissingRequiredOwnedRole(string role)
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode owner = RoleNode("owner", "owner-id", root.Identity.StableId, role, "Owner");
        owner.State.IsFocusable = true;
        root.Children = new UiNode[] { owner };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.That(HasDiagnostic(result, "accessibility.required-owned", "/children/0/accessibility/role"), Is.True);
    }

    [TestCase(AriaRoles.Checkbox, 1)]
    [TestCase(AriaRoles.Radio, 1)]
    [TestCase(AriaRoles.Switch, 1)]
    [TestCase(AriaRoles.MenuItemCheckbox, 1)]
    [TestCase(AriaRoles.MenuItemRadio, 1)]
    [TestCase(AriaRoles.ComboBox, 2)]
    [TestCase(AriaRoles.Tab, 1)]
    [TestCase(AriaRoles.Option, 1)]
    [TestCase(AriaRoles.Heading, 1)]
    [TestCase(AriaRoles.ScrollBar, 2)]
    [TestCase(AriaRoles.Slider, 1)]
    [TestCase(AriaRoles.SpinButton, 1)]
    public void ReportsEveryMissingRequiredProperty(string role, int expectedCount)
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode target = RoleNode("target", "target-id", root.Identity.StableId, role, "Target");
        target.State.IsFocusable = true;
        root.Children = new UiNode[] { target };

        UiResult result = new UiModelValidator().TryValidateModel(application);
        UiDiagnostic[] required = result.Diagnostics
            .Where(diagnostic => diagnostic.Code == "accessibility.required-property")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(required, Has.Length.EqualTo(expectedCount));
            Assert.That(required.All(diagnostic => diagnostic.Location != null && diagnostic.Location.EndsWith("/children/0/accessibility/role", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AcceptsValidApgCompositeStructuresAndAriaOwnership()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);

        UiNode listBox = Composite("listbox", "listbox-id", root.Identity.StableId, AriaRoles.ListBox, "Results");
        UiNode option = Item("option", "option-id", listBox.Identity.StableId, AriaRoles.Option, "First result");
        option.State.HasSelected = true;
        listBox.Children = new UiNode[] { option };

        UiNode menu = RoleNode("menu", "menu-id", root.Identity.StableId, AriaRoles.Menu, "Actions");
        UiNode menuItem = Item("menu-item", "menu-item-id", root.Identity.StableId, AriaRoles.MenuItem, "Open");
        menu.Accessibility!.Relationships = new UiAriaRelationship[]
        {
            Relationship(AriaAttributes.Owns, "menu-item-id")
        };

        UiNode radioGroup = RoleNode("radios", "radios-id", root.Identity.StableId, AriaRoles.RadioGroup, "Theme");
        UiNode radio = Item("radio", "radio-id", radioGroup.Identity.StableId, AriaRoles.Radio, "Dark");
        radio.State.HasCheckState = true;
        radioGroup.Children = new UiNode[] { radio };

        UiNode tabList = RoleNode("tabs", "tabs-id", root.Identity.StableId, AriaRoles.TabList, null);
        UiNode tab = Item("tab", "tab-id", tabList.Identity.StableId, AriaRoles.Tab, "Overview");
        tab.State.HasSelected = true;
        tabList.Children = new UiNode[] { tab };

        UiNode tree = RoleNode("tree", "tree-id", root.Identity.StableId, AriaRoles.Tree, "Files");
        UiNode treeItem = Item("tree-item", "tree-item-id", tree.Identity.StableId, AriaRoles.TreeItem, "Documents");
        tree.Children = new UiNode[] { treeItem };

        UiNode grid = RoleNode("grid", "grid-id", root.Identity.StableId, AriaRoles.Grid, "Data");
        UiNode row = RoleNode("row", "row-id", grid.Identity.StableId, AriaRoles.Row, null);
        UiNode cell = RoleNode("cell", "cell-id", row.Identity.StableId, AriaRoles.GridCell, null);
        row.Children = new UiNode[] { cell };
        grid.Children = new UiNode[] { row };

        UiNode treeGrid = RoleNode("treegrid", "treegrid-id", root.Identity.StableId, AriaRoles.TreeGrid, "Outline");
        UiNode treeRow = RoleNode("tree-row", "tree-row-id", treeGrid.Identity.StableId, AriaRoles.Row, null);
        UiNode treeCell = RoleNode("tree-cell", "tree-cell-id", treeRow.Identity.StableId, AriaRoles.GridCell, null);
        treeRow.Children = new UiNode[] { treeCell };
        treeGrid.Children = new UiNode[] { treeRow };

        UiNode combo = Composite("combo", "combo-id", root.Identity.StableId, AriaRoles.ComboBox, "City");
        combo.State.HasExpanded = true;
        combo.Accessibility!.Relationships = new UiAriaRelationship[]
        {
            Relationship(AriaAttributes.Controls, "listbox-id")
        };

        root.Children = new UiNode[]
        {
            listBox,
            menu,
            menuItem,
            radioGroup,
            tabList,
            tree,
            grid,
            treeGrid,
            combo
        };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static UiNode Composite(string key, string id, string parentId, string role, string label)
    {
        UiNode result = RoleNode(key, id, parentId, role, label);
        result.State.IsFocusable = true;
        return result;
    }

    private static UiNode Item(string key, string id, string parentId, string role, string label)
    {
        UiNode result = RoleNode(key, id, parentId, role, label);
        result.State.IsFocusable = true;
        return result;
    }

    private static UiNode RoleNode(string key, string id, string parentId, string role, string? label)
    {
        UiNode result = Node(key, id, parentId, "div");
        result.Accessibility = new UiAccessibility { Role = role, Label = label };
        return result;
    }

    private static UiNode Node(string key, string id, string parentId, string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Element,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = key,
                SiblingIdentity = key,
                StableId = id,
                ParentStableId = parentId
            }
        };
    }

    private static UiEventBinding Binding(UiEventKind kind)
    {
        return new UiEventBinding { Id = "binding-" + kind, HandlerId = "handler", Kind = kind };
    }

    private static DomAttribute Attribute(string name, string value)
    {
        return new DomAttribute { Name = name, Value = value };
    }

    private static UiAriaRelationship Relationship(string name, string targetId)
    {
        return new UiAriaRelationship { Name = name, TargetIds = new string[] { targetId } };
    }

    private static bool HasDiagnostic(UiResult result, string code, string locationEnding)
    {
        return Array.Exists(
            result.Diagnostics,
            diagnostic => diagnostic.Code == code && diagnostic.Location != null && diagnostic.Location.EndsWith(locationEnding, StringComparison.Ordinal));
    }

    private static UiApplication CreateApplication()
    {
        UiNode root = Node("root", "root-id", "view-id", HtmlElements.Main);
        UiView view = new UiView { Id = "view-id", Key = "view", ContentRoot = root };
        UiPage page = new UiPage { Id = "page-id", Key = "page", ContentView = view };
        UiWindow window = new UiWindow { Id = "window-id", Key = "window", Pages = new UiPage[] { page } };
        return new UiApplication { Id = "application-id", Key = "application", RootSeed = "seed", Windows = new UiWindow[] { window } };
    }

    private static UiNode GetRoot(UiApplication application)
    {
        return application.Windows[0].Pages[0].ContentView!.ContentRoot!;
    }
}
