using System;
using htmlgui;

namespace htmlgui.Tests;

public class UiFactoryTests
{
    [Test]
    public void HierarchyAndLowLevelNodesReceiveStableParentSensitiveIdentities()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("sample-seed", "sample");
        UiApplication repeatedApplication = factory.CreateApplication("sample-seed", "sample");
        UiWindow window = factory.CreateWindow("sample-seed", application.Id, "main");
        UiWindow repeatedWindow = factory.CreateWindow("sample-seed", application.Id, "main");
        UiWindow reparentedWindow = factory.CreateWindow("sample-seed", "another-parent", "main");
        UiPage page = factory.CreatePage("sample-seed", window.Id, "home");
        UiView view = factory.CreateView("sample-seed", page.Id, "content");
        UiNode element = factory.CreateElementNode("sample-seed", view.Id, "greeting", "http://www.w3.org/2000/svg", "svg");
        UiNode repeatedElement = factory.CreateElementNode("sample-seed", view.Id, "greeting", "http://www.w3.org/2000/svg", "svg");
        UiNode text = factory.CreateTextNode("sample-seed", element.Identity.StableId, "label", "Hello");
        UiNode comment = factory.CreateCommentNode("sample-seed", element.Identity.StableId, "note", string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(application.Id, Does.StartWith("ui-application-"));
            Assert.That(application.Id, Is.EqualTo(repeatedApplication.Id));
            Assert.That(application.Key, Is.EqualTo("sample"));
            Assert.That(application.RootSeed, Is.EqualTo("sample-seed"));
            Assert.That(window.Id, Does.StartWith("ui-window-"));
            Assert.That(window.Id, Is.EqualTo(repeatedWindow.Id));
            Assert.That(window.Id, Is.Not.EqualTo(reparentedWindow.Id));
            Assert.That(page.Id, Does.StartWith("ui-page-"));
            Assert.That(view.Id, Does.StartWith("ui-view-"));
            Assert.That(element.Kind, Is.EqualTo(UiNodeKind.Element));
            Assert.That(element.Identity.StableId, Is.EqualTo(repeatedElement.Identity.StableId));
            Assert.That(element.Identity.AuthorKey, Is.EqualTo("greeting"));
            Assert.That(element.Identity.SiblingIdentity, Is.EqualTo("greeting"));
            Assert.That(element.Identity.ParentStableId, Is.EqualTo(view.Id));
            Assert.That(element.NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(element.Name, Is.EqualTo("svg"));
            Assert.That(element.State.IsVisible, Is.True);
            Assert.That(element.State.IsEnabled, Is.True);
            Assert.That(text.Kind, Is.EqualTo(UiNodeKind.Text));
            Assert.That(text.Text, Is.EqualTo("Hello"));
            Assert.That(comment.Kind, Is.EqualTo(UiNodeKind.Comment));
            Assert.That(comment.Text, Is.Empty);
            Assert.That(text.Identity.StableId, Is.Not.EqualTo(comment.Identity.StableId));
        });
    }

    [Test]
    public void CreatesEveryControlAndSupportingModelFamilyWithSuppliedValues()
    {
        IUiFactory factory = new UiFactory();
        UiLayout layout = factory.CreateLayout(UiLayoutKind.Grid);
        UiText text = factory.CreateText(UiTextKind.Heading, "Welcome");
        UiButton button = factory.CreateButton(UiButtonKind.Submit, "Save");
        UiInput input = factory.CreateInput(UiInputKind.Email, "email");
        UiChoice choice = factory.CreateChoice("daily", "Daily");
        UiMedia media = factory.CreateMedia("video", "/intro.mp4");
        UiFeedback feedback = factory.CreateFeedback("progress", "Loading");
        UiDisclosure disclosure = factory.CreateDisclosure("details", "Details");
        UiCollection collection = factory.CreateCollection("results");
        UiTable table = factory.CreateTable("orders");
        UiTree tree = factory.CreateTree("files");
        UiMenu menu = factory.CreateMenu("main-menu");
        UiMenuItem menuItem = factory.CreateMenuItem(UiMenuItemKind.Command, "open", "Open");
        UiTabs tabs = factory.CreateTabs("settings");
        UiTab tab = factory.CreateTab("general", "General");
        UiToolbar toolbar = factory.CreateToolbar("editor");
        UiTool tool = factory.CreateTool("save", "button", "Save");
        UiBreadcrumb breadcrumb = factory.CreateBreadcrumb("location");
        UiPagination pagination = factory.CreatePagination("pages");
        UiDialog dialog = factory.CreateDialog(UiDialogKind.Modal, "confirm", "Confirm");
        UiPopover popover = factory.CreatePopover("actions", "actions-button");
        UiTooltip tooltip = factory.CreateTooltip("save-help", "save-button", "Save changes");
        UiToast toast = factory.CreateToast("saved", "Changes saved");
        UiForm form = factory.CreateForm("profile");
        UiField field = factory.CreateField("email-field", "email", "Email");
        UiValidationRule rule = factory.CreateValidationRule("required-email", "required", "Email is required");
        UiBinding binding = factory.CreateBinding("email-binding", "profile-state", "/email", "email-node", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input);
        UiStyle style = factory.CreateStyle("profile-style");
        UiRoute route = factory.CreateRoute("profile-route", "/profile");
        UiBridgeOptions bridge = factory.CreateBridgeOptions("https://example.test");

        Assert.Multiple(() =>
        {
            Assert.That(layout.Kind, Is.EqualTo(UiLayoutKind.Grid));
            Assert.That(layout.Children, Is.Empty);
            Assert.That(text.Kind, Is.EqualTo(UiTextKind.Heading));
            Assert.That(text.Text, Is.EqualTo("Welcome"));
            Assert.That(button.Kind, Is.EqualTo(UiButtonKind.Submit));
            Assert.That(button.Label, Is.EqualTo("Save"));
            Assert.That(button.IsEnabled, Is.True);
            Assert.That(input.Kind, Is.EqualTo(UiInputKind.Email));
            Assert.That(input.Name, Is.EqualTo("email"));
            Assert.That(input.UpdateTrigger, Is.EqualTo(UiBindingTrigger.Change));
            Assert.That(choice.Key, Is.EqualTo("daily"));
            Assert.That(choice.Label, Is.EqualTo("Daily"));
            Assert.That(media.Kind, Is.EqualTo("video"));
            Assert.That(media.Source, Is.EqualTo("/intro.mp4"));
            Assert.That(feedback.Kind, Is.EqualTo("progress"));
            Assert.That(feedback.Message, Is.EqualTo("Loading"));
            Assert.That(disclosure.Key, Is.EqualTo("details"));
            Assert.That(disclosure.Summary, Is.EqualTo("Details"));
            Assert.That(collection.Key, Is.EqualTo("results"));
            Assert.That(table.Key, Is.EqualTo("orders"));
            Assert.That(tree.Key, Is.EqualTo("files"));
            Assert.That(menu.Key, Is.EqualTo("main-menu"));
            Assert.That(menu.WrapFocus, Is.True);
            Assert.That(menuItem.Kind, Is.EqualTo(UiMenuItemKind.Command));
            Assert.That(menuItem.Key, Is.EqualTo("open"));
            Assert.That(tabs.Key, Is.EqualTo("settings"));
            Assert.That(tab.Key, Is.EqualTo("general"));
            Assert.That(tab.Label, Is.EqualTo("General"));
            Assert.That(toolbar.Key, Is.EqualTo("editor"));
            Assert.That(tool.Key, Is.EqualTo("save"));
            Assert.That(tool.Kind, Is.EqualTo("button"));
            Assert.That(breadcrumb.Key, Is.EqualTo("location"));
            Assert.That(breadcrumb.Label, Is.EqualTo("Breadcrumb"));
            Assert.That(pagination.Key, Is.EqualTo("pages"));
            Assert.That(dialog.Kind, Is.EqualTo(UiDialogKind.Modal));
            Assert.That(dialog.Title, Is.EqualTo("Confirm"));
            Assert.That(popover.Anchor.TargetId, Is.EqualTo("actions-button"));
            Assert.That(popover.Anchor.AllowFlip, Is.True);
            Assert.That(tooltip.Anchor.TargetId, Is.EqualTo("save-button"));
            Assert.That(tooltip.Text, Is.EqualTo("Save changes"));
            Assert.That(toast.Message, Is.EqualTo("Changes saved"));
            Assert.That(toast.LiveMode, Is.EqualTo(UiAriaLiveMode.Polite));
            Assert.That(form.Key, Is.EqualTo("profile"));
            Assert.That(form.Method, Is.EqualTo("post"));
            Assert.That(form.State.CanSubmit, Is.True);
            Assert.That(field.Key, Is.EqualTo("email-field"));
            Assert.That(field.Name, Is.EqualTo("email"));
            Assert.That(field.Label, Is.EqualTo("Email"));
            Assert.That(rule.Key, Is.EqualTo("required-email"));
            Assert.That(rule.Kind, Is.EqualTo("required"));
            Assert.That(rule.Message, Is.EqualTo("Email is required"));
            Assert.That(rule.Severity, Is.EqualTo(UiValidationSeverity.Error));
            Assert.That(binding.Id, Is.EqualTo("email-binding"));
            Assert.That(binding.StateId, Is.EqualTo("profile-state"));
            Assert.That(binding.Path, Is.EqualTo("/email"));
            Assert.That(binding.TargetNodeId, Is.EqualTo("email-node"));
            Assert.That(binding.Target, Is.EqualTo("value"));
            Assert.That(binding.Mode, Is.EqualTo(UiBindingMode.TwoWay));
            Assert.That(binding.Trigger, Is.EqualTo(UiBindingTrigger.Input));
            Assert.That(style.Key, Is.EqualTo("profile-style"));
            Assert.That(style.Declarations, Is.Empty);
            Assert.That(route.Id, Is.EqualTo("profile-route"));
            Assert.That(route.Template, Is.EqualTo("/profile"));
            Assert.That(bridge.Origin, Is.EqualTo("https://example.test"));
            Assert.That(bridge.Version, Is.EqualTo(BridgeProtocolVersions.Current));
            Assert.That(bridge.RequireMainFrame, Is.True);
        });
    }

    [Test]
    public void EveryCallReturnsFreshNestedDefaults()
    {
        IUiFactory factory = new UiFactory();
        UiApplication firstApplication = factory.CreateApplication("seed", "app");
        UiApplication secondApplication = factory.CreateApplication("seed", "app");
        UiInput firstInput = factory.CreateInput(UiInputKind.Text, "query");
        UiInput secondInput = factory.CreateInput(UiInputKind.Text, "query");
        UiCollection firstCollection = factory.CreateCollection("items");
        UiCollection secondCollection = factory.CreateCollection("items");
        UiDialog firstDialog = factory.CreateDialog(UiDialogKind.Modal, "dialog", "Dialog");
        UiDialog secondDialog = factory.CreateDialog(UiDialogKind.Modal, "dialog", "Dialog");
        UiPopover firstPopover = factory.CreatePopover("popover", "anchor");
        UiPopover secondPopover = factory.CreatePopover("popover", "anchor");
        UiForm firstForm = factory.CreateForm("form");
        UiForm secondForm = factory.CreateForm("form");

        firstApplication.RenderOptions.FullDocument = false;
        firstInput.Value.Kind = UiValueKind.String;
        firstCollection.Selection.ActiveKey = "first";
        firstDialog.State.IsOpen = true;
        firstPopover.Anchor.Placement = "top";
        firstForm.State.IsDirty = true;
        firstForm.Validation.IsValid = false;

        Assert.Multiple(() =>
        {
            Assert.That(firstApplication, Is.Not.SameAs(secondApplication));
            Assert.That(firstApplication.RenderOptions, Is.Not.SameAs(secondApplication.RenderOptions));
            Assert.That(secondApplication.RenderOptions.FullDocument, Is.True);
            Assert.That(firstInput.Value, Is.Not.SameAs(secondInput.Value));
            Assert.That(secondInput.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(firstCollection.Selection, Is.Not.SameAs(secondCollection.Selection));
            Assert.That(secondCollection.Selection.ActiveKey, Is.Null);
            Assert.That(firstDialog.State, Is.Not.SameAs(secondDialog.State));
            Assert.That(secondDialog.State.IsOpen, Is.False);
            Assert.That(firstPopover.Anchor, Is.Not.SameAs(secondPopover.Anchor));
            Assert.That(secondPopover.Anchor.Placement, Is.EqualTo("bottom"));
            Assert.That(firstForm.State, Is.Not.SameAs(secondForm.State));
            Assert.That(secondForm.State.IsDirty, Is.False);
            Assert.That(firstForm.Validation, Is.Not.SameAs(secondForm.Validation));
            Assert.That(secondForm.Validation.IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsMissingRequiredIdentityAndConfigurationValues()
    {
        IUiFactory factory = new UiFactory();

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateApplication(" ", "app"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateWindow("seed", string.Empty, "window"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreatePage("seed", "parent", null!), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => factory.CreateElementNode("seed", "parent", "element", null, " "), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateTextNode("seed", "parent", "text", null!), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => factory.CreateInput(UiInputKind.Text, " "), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateChoice(string.Empty, "Choice"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateMedia("video", " "), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreatePopover("popover", " "), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateField("field", string.Empty, "Field"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateBinding("binding", "state", null!, "node", "value", UiBindingMode.OneWay, UiBindingTrigger.Change), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => factory.CreateRoute("route", string.Empty), Throws.TypeOf<ArgumentException>());
            Assert.That(() => factory.CreateBridgeOptions(null!), Throws.TypeOf<ArgumentNullException>());
        });
    }

    [Test]
    public void RejectsUndefinedEnumValues()
    {
        IUiFactory factory = new UiFactory();

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateLayout((UiLayoutKind)(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateText((UiTextKind)16, "text"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateButton((UiButtonKind)7, "button"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateInput((UiInputKind)18, "input"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateMenuItem((UiMenuItemKind)5, "item", "Item"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateDialog((UiDialogKind)7, "dialog", "Dialog"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateBinding("binding", "state", "/value", "node", "value", (UiBindingMode)4, UiBindingTrigger.Change), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => factory.CreateBinding("binding", "state", "/value", "node", "value", UiBindingMode.OneWay, (UiBindingTrigger)5), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
