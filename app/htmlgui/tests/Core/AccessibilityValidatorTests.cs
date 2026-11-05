using System;
using htmlgui;

namespace htmlgui.Tests;

public class AccessibilityValidatorTests
{
    [Test]
    public void ReportsInvalidRedundantAndProhibitedRolesAtStableLocations()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode invalid = CreateNode("invalid", "invalid-id", root.Identity.StableId, "div");
        invalid.Accessibility = new UiAccessibility { Role = "imaginary" };
        UiNode redundant = CreateNode("redundant", "redundant-id", root.Identity.StableId, HtmlElements.Button);
        redundant.Accessibility = new UiAccessibility { Role = AriaRoles.Button, Label = "Save" };
        UiNode prohibited = CreateNode("prohibited", "prohibited-id", root.Identity.StableId, "div");
        prohibited.Accessibility = new UiAccessibility { Role = AriaRoles.Presentation, Label = "Decorative" };
        root.Children = new UiNode[] { invalid, redundant, prohibited };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(result, "accessibility.invalid-role", "/children/0/accessibility/role"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.redundant-role", "/children/1/accessibility/role"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.prohibited-aria", "/children/2"), Is.True);
        });
    }

    [Test]
    public void ReportsMissingRoleContextsOwnedChildrenAndProperties()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode listBox = CreateNode("list", "list-id", root.Identity.StableId, "div");
        listBox.Accessibility = new UiAccessibility { Role = AriaRoles.ListBox, Label = "Results" };
        listBox.State.IsFocusable = true;
        UiNode option = CreateNode("option", "option-id", root.Identity.StableId, "div");
        option.Accessibility = new UiAccessibility { Role = AriaRoles.Option, Label = "Result" };
        option.State.IsFocusable = true;
        UiNode checkbox = CreateNode("check", "check-id", root.Identity.StableId, "div");
        checkbox.Accessibility = new UiAccessibility { Role = AriaRoles.Checkbox, Label = "Enable" };
        checkbox.State.IsFocusable = true;
        root.Children = new UiNode[] { listBox, option, checkbox };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(result, "accessibility.required-owned", "/children/0/accessibility/role"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.required-context", "/children/1/accessibility/role"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.required-property", "/children/2/accessibility/role"), Is.True);
        });
    }

    [Test]
    public void ReportsDuplicateElementIdsAndBrokenIdReferences()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode first = CreateNode("first", "first-id", root.Identity.StableId, "div");
        first.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Id, Value = "shared" } };
        first.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.Controls, TargetIds = new string[] { "missing" } }
            }
        };
        UiNode second = CreateNode("second", "second-id", root.Identity.StableId, "div");
        second.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Id, Value = "shared" } };
        root.Children = new UiNode[] { first, second };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(result, "accessibility.duplicate-id", "/children/1/attributes/0/value"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.broken-reference", "/children/0/accessibility/relationships/0/targetIds/0"), Is.True);
        });
    }

    [Test]
    public void ReportsMissingNamesHiddenFocusAndPointerOnlyActions()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode unnamed = CreateNode("unnamed", "unnamed-id", root.Identity.StableId, "div");
        unnamed.Accessibility = new UiAccessibility { Role = AriaRoles.Button };
        unnamed.State.IsFocusable = true;
        unnamed.EventBindings = new UiEventBinding[] { new UiEventBinding { Id = "key", HandlerId = "key-handler", Kind = UiEventKind.KeyDown } };
        UiNode hidden = CreateNode("hidden", "hidden-id", root.Identity.StableId, HtmlElements.Button);
        hidden.Accessibility = new UiAccessibility { Label = "Hidden action", IsHidden = true };
        UiNode pointerOnly = CreateNode("pointer", "pointer-id", root.Identity.StableId, "div");
        pointerOnly.State.IsFocusable = true;
        pointerOnly.EventBindings = new UiEventBinding[] { new UiEventBinding { Id = "click", HandlerId = "click-handler", Kind = UiEventKind.Click } };
        root.Children = new UiNode[] { unnamed, hidden, pointerOnly };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(result, "accessibility.missing-name", "/children/0/accessibility/role"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.hidden-focus", "/children/1/state"), Is.True);
            Assert.That(HasDiagnostic(result, "accessibility.keyboard-access", "/children/2/eventBindings"), Is.True);
        });
    }

    [Test]
    public void AcceptsNamedFocusableListBoxWithOwnedOptionAndValidReference()
    {
        UiApplication application = CreateApplication();
        UiNode root = GetRoot(application);
        UiNode listBox = CreateNode("list", "list-id", root.Identity.StableId, "div");
        listBox.Accessibility = new UiAccessibility
        {
            Role = AriaRoles.ListBox,
            Label = "Results",
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.ActiveDescendant, TargetIds = new string[] { "option-id" } }
            }
        };
        listBox.State.IsFocusable = true;
        UiNode group = CreateNode("group", "group-id", listBox.Identity.StableId, "div");
        group.Accessibility = new UiAccessibility { Role = AriaRoles.Group };
        UiNode option = CreateNode("option", "option-id", group.Identity.StableId, "div");
        option.Accessibility = new UiAccessibility { Role = AriaRoles.Option, Label = "First result" };
        option.State.HasSelected = true;
        group.Children = new UiNode[] { option };
        listBox.Children = new UiNode[] { group };
        root.Children = new UiNode[] { listBox };

        UiResult result = new UiModelValidator().TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static bool HasDiagnostic(UiResult result, string code, string locationEnding)
    {
        return Array.Exists(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Location != null && diagnostic.Location.EndsWith(locationEnding, StringComparison.Ordinal));
    }

    private static UiApplication CreateApplication()
    {
        UiNode root = CreateNode("root", "root-id", "view-id", HtmlElements.Main);
        UiView view = new UiView { Id = "view-id", Key = "view", ContentRoot = root };
        UiPage page = new UiPage { Id = "page-id", Key = "page", ContentView = view };
        UiWindow window = new UiWindow { Id = "window-id", Key = "window", Pages = new UiPage[] { page } };
        return new UiApplication { Id = "application-id", Key = "application", RootSeed = "seed", Windows = new UiWindow[] { window } };
    }

    private static UiNode GetRoot(UiApplication application)
    {
        return application.Windows[0].Pages[0].ContentView!.ContentRoot!;
    }

    private static UiNode CreateNode(string key, string id, string parentId, string name)
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
}
