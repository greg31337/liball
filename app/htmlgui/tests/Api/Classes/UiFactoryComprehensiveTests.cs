using System;
using htmlgui;

namespace htmlgui.Tests;

public class UiFactoryComprehensiveTests
{
    [Test]
    public void EveryCreationMethodReturnsADistinctTopLevelInstance()
    {
        IUiFactory factory = new UiFactory();
        object[] first = CreateEveryModel(factory);
        object[] second = CreateEveryModel(factory);

        Assert.That(first, Has.Length.EqualTo(37));
        Assert.That(second, Has.Length.EqualTo(first.Length));
        int index = 0;
        while (index < first.Length)
        {
            Assert.That(second[index].GetType(), Is.EqualTo(first[index].GetType()), "type at index " + index);
            Assert.That(second[index], Is.Not.SameAs(first[index]), "instance at index " + index);
            index++;
        }
    }

    [Test]
    public void EveryDefinedFactoryEnumValueIsAcceptedAndRetained()
    {
        IUiFactory factory = new UiFactory();

        foreach (UiLayoutKind kind in Enum.GetValues<UiLayoutKind>())
        {
            Assert.That(factory.CreateLayout(kind).Kind, Is.EqualTo(kind));
        }

        foreach (UiTextKind kind in Enum.GetValues<UiTextKind>())
        {
            Assert.That(factory.CreateText(kind, "text").Kind, Is.EqualTo(kind));
        }

        foreach (UiButtonKind kind in Enum.GetValues<UiButtonKind>())
        {
            Assert.That(factory.CreateButton(kind, "button").Kind, Is.EqualTo(kind));
        }

        foreach (UiInputKind kind in Enum.GetValues<UiInputKind>())
        {
            Assert.That(factory.CreateInput(kind, "input").Kind, Is.EqualTo(kind));
        }

        foreach (UiMenuItemKind kind in Enum.GetValues<UiMenuItemKind>())
        {
            Assert.That(factory.CreateMenuItem(kind, "item", string.Empty).Kind, Is.EqualTo(kind));
        }

        foreach (UiDialogKind kind in Enum.GetValues<UiDialogKind>())
        {
            Assert.That(factory.CreateDialog(kind, "dialog", string.Empty).Kind, Is.EqualTo(kind));
        }

        foreach (UiBindingMode mode in Enum.GetValues<UiBindingMode>())
        {
            Assert.That(factory.CreateBinding("binding", "state", string.Empty, "node", "value", mode, UiBindingTrigger.Change).Mode, Is.EqualTo(mode));
        }

        foreach (UiBindingTrigger trigger in Enum.GetValues<UiBindingTrigger>())
        {
            Assert.That(factory.CreateBinding("binding", "state", string.Empty, "node", "value", UiBindingMode.OneWay, trigger).Trigger, Is.EqualTo(trigger));
        }
    }

    [Test]
    public void PresentationTextAndRootJsonPointerMayBeEmpty()
    {
        IUiFactory factory = new UiFactory();

        UiNode textNode = factory.CreateTextNode("seed", "parent", "text", string.Empty);
        UiNode commentNode = factory.CreateCommentNode("seed", "parent", "comment", string.Empty);
        UiText text = factory.CreateText(UiTextKind.Plain, string.Empty);
        UiButton button = factory.CreateButton(UiButtonKind.Icon, string.Empty);
        UiChoice choice = factory.CreateChoice("choice", string.Empty);
        UiFeedback feedback = factory.CreateFeedback("status", string.Empty);
        UiDisclosure disclosure = factory.CreateDisclosure("disclosure", string.Empty);
        UiMenuItem menuItem = factory.CreateMenuItem(UiMenuItemKind.Separator, "separator", string.Empty);
        UiTab tab = factory.CreateTab("tab", string.Empty);
        UiTool tool = factory.CreateTool("tool", "separator", string.Empty);
        UiDialog dialog = factory.CreateDialog(UiDialogKind.Modal, "dialog", string.Empty);
        UiTooltip tooltip = factory.CreateTooltip("tooltip", "target", string.Empty);
        UiToast toast = factory.CreateToast("toast", string.Empty);
        UiField field = factory.CreateField("field", "field", string.Empty);
        UiValidationRule rule = factory.CreateValidationRule("rule", "custom", string.Empty);
        UiBinding binding = factory.CreateBinding("binding", "state", string.Empty, "node", "value", UiBindingMode.OneWay, UiBindingTrigger.Change);

        Assert.Multiple(() =>
        {
            Assert.That(textNode.Text, Is.Empty);
            Assert.That(commentNode.Text, Is.Empty);
            Assert.That(text.Text, Is.Empty);
            Assert.That(button.Label, Is.Empty);
            Assert.That(choice.Label, Is.Empty);
            Assert.That(feedback.Message, Is.Empty);
            Assert.That(disclosure.Summary, Is.Empty);
            Assert.That(menuItem.Label, Is.Empty);
            Assert.That(tab.Label, Is.Empty);
            Assert.That(tool.Label, Is.Empty);
            Assert.That(dialog.Title, Is.Empty);
            Assert.That(tooltip.Text, Is.Empty);
            Assert.That(toast.Message, Is.Empty);
            Assert.That(field.Label, Is.Empty);
            Assert.That(rule.Message, Is.Empty);
            Assert.That(binding.Path, Is.Empty);
        });
    }

    [Test]
    public void StableIdentitiesChangeForEveryIdentityInputDimension()
    {
        IUiFactory factory = new UiFactory();
        UiApplication baselineApplication = factory.CreateApplication("seed", "application");
        UiApplication changedSeedApplication = factory.CreateApplication("other-seed", "application");
        UiApplication changedKeyApplication = factory.CreateApplication("seed", "other-application");
        UiWindow baselineWindow = factory.CreateWindow("seed", baselineApplication.Id, "window");
        UiWindow changedParentWindow = factory.CreateWindow("seed", "other-parent", "window");
        UiWindow changedKeyWindow = factory.CreateWindow("seed", baselineApplication.Id, "other-window");
        UiPage baselinePage = factory.CreatePage("seed", baselineWindow.Id, "page");
        UiPage changedParentPage = factory.CreatePage("seed", "other-parent", "page");
        UiView baselineView = factory.CreateView("seed", baselinePage.Id, "view");
        UiView changedKeyView = factory.CreateView("seed", baselinePage.Id, "other-view");
        UiNode element = factory.CreateElementNode("seed", baselineView.Id, "node", null, "div");
        UiNode changedParentElement = factory.CreateElementNode("seed", "other-parent", "node", null, "div");
        UiNode changedKeyElement = factory.CreateElementNode("seed", baselineView.Id, "other-node", null, "div");
        UiNode sameIdentityDifferentElementName = factory.CreateElementNode("seed", baselineView.Id, "node", null, "section");
        UiNode text = factory.CreateTextNode("seed", baselineView.Id, "node", "text");
        UiNode comment = factory.CreateCommentNode("seed", baselineView.Id, "node", "comment");

        Assert.Multiple(() =>
        {
            Assert.That(baselineApplication.Id, Is.Not.EqualTo(changedSeedApplication.Id));
            Assert.That(baselineApplication.Id, Is.Not.EqualTo(changedKeyApplication.Id));
            Assert.That(baselineWindow.Id, Is.Not.EqualTo(changedParentWindow.Id));
            Assert.That(baselineWindow.Id, Is.Not.EqualTo(changedKeyWindow.Id));
            Assert.That(baselinePage.Id, Is.Not.EqualTo(changedParentPage.Id));
            Assert.That(baselineView.Id, Is.Not.EqualTo(changedKeyView.Id));
            Assert.That(element.Identity.StableId, Is.Not.EqualTo(changedParentElement.Identity.StableId));
            Assert.That(element.Identity.StableId, Is.Not.EqualTo(changedKeyElement.Identity.StableId));
            Assert.That(element.Identity.StableId, Is.EqualTo(sameIdentityDifferentElementName.Identity.StableId));
            Assert.That(element.Identity.StableId, Is.Not.EqualTo(text.Identity.StableId));
            Assert.That(element.Identity.StableId, Is.Not.EqualTo(comment.Identity.StableId));
            Assert.That(text.Identity.StableId, Is.Not.EqualTo(comment.Identity.StableId));
        });
    }

    [Test]
    public void ValidationExceptionsIdentifyTheRejectedArgument()
    {
        IUiFactory factory = new UiFactory();

        ArgumentException? rootSeed = Assert.Throws<ArgumentException>(() => factory.CreateApplication(" ", "application"));
        ArgumentNullException? key = Assert.Throws<ArgumentNullException>(() => factory.CreateCollection(null!));
        ArgumentException? parentId = Assert.Throws<ArgumentException>(() => factory.CreateView("seed", string.Empty, "view"));
        ArgumentException? name = Assert.Throws<ArgumentException>(() => factory.CreateElementNode("seed", "parent", "node", null, " "));
        ArgumentNullException? label = Assert.Throws<ArgumentNullException>(() => factory.CreateButton(UiButtonKind.Push, null!));
        ArgumentException? targetId = Assert.Throws<ArgumentException>(() => factory.CreateTooltip("tooltip", " ", "text"));
        ArgumentNullException? path = Assert.Throws<ArgumentNullException>(() => factory.CreateBinding("binding", "state", null!, "node", "value", UiBindingMode.OneWay, UiBindingTrigger.Change));
        ArgumentOutOfRangeException? kind = Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateLayout((UiLayoutKind)100));

        Assert.Multiple(() =>
        {
            Assert.That(rootSeed!.ParamName, Is.EqualTo("rootSeed"));
            Assert.That(key!.ParamName, Is.EqualTo("key"));
            Assert.That(parentId!.ParamName, Is.EqualTo("parentId"));
            Assert.That(name!.ParamName, Is.EqualTo("name"));
            Assert.That(label!.ParamName, Is.EqualTo("label"));
            Assert.That(targetId!.ParamName, Is.EqualTo("targetId"));
            Assert.That(path!.ParamName, Is.EqualTo("path"));
            Assert.That(kind!.ParamName, Is.EqualTo("kind"));
        });
    }

    private static object[] CreateEveryModel(IUiFactory factory)
    {
        return new object[]
        {
            factory.CreateApplication("seed", "application"),
            factory.CreateWindow("seed", "application-id", "window"),
            factory.CreatePage("seed", "window-id", "page"),
            factory.CreateView("seed", "page-id", "view"),
            factory.CreateElementNode("seed", "view-id", "element", null, "div"),
            factory.CreateTextNode("seed", "element-id", "text-node", "Text"),
            factory.CreateCommentNode("seed", "element-id", "comment", "Comment"),
            factory.CreateLayout(UiLayoutKind.Flow),
            factory.CreateText(UiTextKind.Plain, "Text"),
            factory.CreateButton(UiButtonKind.Push, "Button"),
            factory.CreateInput(UiInputKind.Text, "input"),
            factory.CreateChoice("choice", "Choice"),
            factory.CreateMedia("video", "/video.mp4"),
            factory.CreateFeedback("status", "Ready"),
            factory.CreateDisclosure("disclosure", "Details"),
            factory.CreateCollection("collection"),
            factory.CreateTable("table"),
            factory.CreateTree("tree"),
            factory.CreateMenu("menu"),
            factory.CreateMenuItem(UiMenuItemKind.Command, "menu-item", "Item"),
            factory.CreateTabs("tabs"),
            factory.CreateTab("tab", "Tab"),
            factory.CreateToolbar("toolbar"),
            factory.CreateTool("tool", "button", "Tool"),
            factory.CreateBreadcrumb("breadcrumb"),
            factory.CreatePagination("pagination"),
            factory.CreateDialog(UiDialogKind.Modal, "dialog", "Dialog"),
            factory.CreatePopover("popover", "target"),
            factory.CreateTooltip("tooltip", "target", "Tooltip"),
            factory.CreateToast("toast", "Toast"),
            factory.CreateForm("form"),
            factory.CreateField("field", "field", "Field"),
            factory.CreateValidationRule("rule", "required", "Required"),
            factory.CreateBinding("binding", "state", "/value", "node", "value", UiBindingMode.OneWay, UiBindingTrigger.Change),
            factory.CreateStyle("style"),
            factory.CreateRoute("route", "/route"),
            factory.CreateBridgeOptions("https://example.test")
        };
    }
}
