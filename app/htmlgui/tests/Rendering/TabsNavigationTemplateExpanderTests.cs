using System;
using htmlgui;

namespace htmlgui.Tests;

public class TabsNavigationTemplateExpanderTests
{
    [Test]
    public void ExpandsTabsWithCloseReorderOverflowAndLazyPanels()
    {
        UiTab general = new UiTab
        {
            Key = "general",
            Label = "General",
            IconKey = "gear",
            Panel = TextNode("general-panel", "General panel"),
            IsSelected = true,
            CanClose = true
        };
        UiTab advanced = new UiTab
        {
            Key = "advanced",
            Label = "Advanced",
            Panel = new UiNode { Kind = UiNodeKind.Control, Name = "button" },
            IsLazy = true
        };
        UiNode node = Control("tabs");
        node.Tabs = new UiTabs
        {
            Key = "settings",
            Tabs = new UiTab[] { general, advanced },
            ActiveTabKey = "general",
            Orientation = UiOrientation.Vertical,
            AutomaticActivation = false,
            CanReorder = true,
            HasOverflow = true
        };
        node.State.IsBusy = true;
        node.State.Version = 4;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode list = result.Children[0];
        DomNode owner = list.Children[0];
        DomNode action = owner.Children[0];
        DomNode close = owner.Children[1];
        DomNode overflow = result.Children[1];
        DomNode selectedPanel = result.Children[2];
        DomNode lazyPanel = result.Children[3];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-automatic-activation"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(list, HtmlAttributes.Role), Is.EqualTo(AriaRoles.TabList));
            Assert.That(AttributeValue(list, AriaAttributes.Orientation), Is.EqualTo("vertical"));
            Assert.That(AttributeValue(list, "data-reorder"), Is.EqualTo("true"));
            Assert.That(AttributeValue(owner, HtmlAttributes.Role), Is.EqualTo(AriaRoles.None));
            Assert.That(AttributeValue(action, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Tab));
            Assert.That(AttributeValue(action, AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(action, HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(action, "draggable"), Is.EqualTo("true"));
            Assert.That(AttributeValue(action.Children[0], "data-icon"), Is.EqualTo("gear"));
            Assert.That(AttributeValue(action, AriaAttributes.Controls), Is.EqualTo(selectedPanel.Id));
            Assert.That(AttributeValue(close, "data-close-tab"), Is.EqualTo("general"));
            Assert.That(AttributeValue(overflow, AriaAttributes.HasPopup), Is.EqualTo("menu"));
            Assert.That(AttributeValue(selectedPanel, HtmlAttributes.Role), Is.EqualTo(AriaRoles.TabPanel));
            Assert.That(AttributeValue(selectedPanel, AriaAttributes.LabelledBy), Is.EqualTo(action.Id));
            Assert.That(AttributeValue(selectedPanel, "data-materialized"), Is.EqualTo("true"));
            Assert.That(selectedPanel.Children[0].Text, Is.EqualTo("General panel"));
            Assert.That(AttributeValue(lazyPanel, "data-lazy"), Is.EqualTo("true"));
            Assert.That(AttributeValue(lazyPanel, "data-materialized"), Is.EqualTo("false"));
            Assert.That(HasAttribute(lazyPanel, HtmlAttributes.Hidden), Is.True);
            Assert.That(lazyPanel.Children, Is.Empty);
            Assert.That(result.Version, Is.EqualTo(4));
        });
    }

    [Test]
    public void TabAndPanelIdentitiesSurviveReorderingAndSelectionChanges()
    {
        UiTab firstTab = new UiTab { Key = "first", Label = "First", Panel = TextNode("first-panel", "First"), IsSelected = true };
        UiTab secondTab = new UiTab { Key = "second", Label = "Second", Panel = TextNode("second-panel", "Second") };
        UiNode node = Control("tab-view");
        node.Tabs = new UiTabs { Key = "stable-tabs", Tabs = new UiTab[] { firstTab, secondTab } };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        string firstActionId = first.Children[0].Children[0].Id;
        string secondActionId = first.Children[0].Children[1].Id;
        string firstPanelId = first.Children[1].Id;
        string secondPanelId = first.Children[2].Id;
        firstTab.IsSelected = false;
        secondTab.IsSelected = true;
        node.Tabs.Tabs = new UiTab[] { secondTab, firstTab };
        DomNode reordered = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.Children[0].Children[0].Id, Is.EqualTo(secondActionId));
            Assert.That(reordered.Children[0].Children[1].Id, Is.EqualTo(firstActionId));
            Assert.That(reordered.Children[1].Id, Is.EqualTo(secondPanelId));
            Assert.That(reordered.Children[2].Id, Is.EqualTo(firstPanelId));
            Assert.That(AttributeValue(reordered.Children[0].Children[0], AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(reordered.Children[1], "data-materialized"), Is.EqualTo("true"));
            Assert.That(AttributeValue(reordered.Children[2], "data-materialized"), Is.EqualTo("true"));
        });
    }

    [Test]
    public void LazyPanelMaterializesOnlyWhileActiveAndKeepsStableRelationships()
    {
        UiTab eager = new UiTab { Key = "eager", Label = "Eager", Panel = TextNode("eager-panel", "Eager panel") };
        UiTab lazy = new UiTab { Key = "lazy", Label = "Lazy", Panel = TextNode("lazy-panel", "Lazy panel"), IsLazy = true };
        UiNode node = Control("tabs");
        node.Tabs = new UiTabs { Key = "lazy-tabs", Tabs = new UiTab[] { eager, lazy }, ActiveTabKey = "eager" };
        SemanticExpander expander = new SemanticExpander();

        DomNode eagerResult = expander.ExpandNode(node);
        DomNode eagerLazyPanel = eagerResult.Children[2];
        string lazyActionId = eagerResult.Children[0].Children[1].Id;
        string lazyPanelId = eagerLazyPanel.Id;
        node.Tabs.ActiveTabKey = "lazy";
        DomNode lazyResult = expander.ExpandNode(node);
        DomNode activeLazyAction = lazyResult.Children[0].Children[1];
        DomNode activeLazyPanel = lazyResult.Children[2];
        node.Tabs.ActiveTabKey = "eager";
        DomNode restored = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(eagerLazyPanel, "data-materialized"), Is.EqualTo("false"));
            Assert.That(HasAttribute(eagerLazyPanel, HtmlAttributes.Hidden), Is.True);
            Assert.That(eagerLazyPanel.Children, Is.Empty);
            Assert.That(activeLazyAction.Id, Is.EqualTo(lazyActionId));
            Assert.That(activeLazyPanel.Id, Is.EqualTo(lazyPanelId));
            Assert.That(AttributeValue(activeLazyAction, AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(activeLazyAction, AriaAttributes.Controls), Is.EqualTo(activeLazyPanel.Id));
            Assert.That(AttributeValue(activeLazyPanel, AriaAttributes.LabelledBy), Is.EqualTo(activeLazyAction.Id));
            Assert.That(AttributeValue(activeLazyPanel, "data-materialized"), Is.EqualTo("true"));
            Assert.That(HasAttribute(activeLazyPanel, HtmlAttributes.Hidden), Is.False);
            Assert.That(activeLazyPanel.Children[0].Text, Is.EqualTo("Lazy panel"));
            Assert.That(restored.Children[2].Id, Is.EqualTo(lazyPanelId));
            Assert.That(AttributeValue(restored.Children[2], "data-materialized"), Is.EqualTo("false"));
            Assert.That(restored.Children[2].Children, Is.Empty);
        });
    }

    [Test]
    public void ExpandsStepperProgressAndPanels()
    {
        UiTab account = new UiTab { Key = "account", Label = "Account", Panel = TextNode("account-panel", "Account") };
        UiTab profile = new UiTab { Key = "profile", Label = "Profile", Panel = TextNode("profile-panel", "Profile") };
        UiTab confirm = new UiTab { Key = "confirm", Label = "Confirm", Panel = TextNode("confirm-panel", "Confirm"), IsDisabled = true };
        UiNode node = Control("stepper");
        node.Tabs = new UiTabs
        {
            Key = "signup",
            Tabs = new UiTab[] { account, profile, confirm },
            ActiveTabKey = "profile",
            Orientation = UiOrientation.Vertical,
            CanReorder = true
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode steps = result.Children[0];
        DomNode accountButton = steps.Children[0].Children[0];
        DomNode profileButton = steps.Children[1].Children[0];
        DomNode confirmButton = steps.Children[2].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(result, AriaAttributes.Orientation), Is.EqualTo("vertical"));
            Assert.That(steps.Name, Is.EqualTo(HtmlElements.OrderedList));
            Assert.That(AttributeValue(accountButton, "data-complete"), Is.EqualTo("true"));
            Assert.That(AttributeValue(accountButton, "draggable"), Is.EqualTo("true"));
            Assert.That(AttributeValue(profileButton, AriaAttributes.Current), Is.EqualTo("step"));
            Assert.That(AttributeValue(profileButton, HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(HasAttribute(confirmButton, HtmlAttributes.Disabled), Is.True);
            Assert.That(HasAttribute(result.Children[1], HtmlAttributes.Hidden), Is.True);
            Assert.That(HasAttribute(result.Children[2], HtmlAttributes.Hidden), Is.False);
            Assert.That(AttributeValue(result.Children[2], AriaAttributes.LabelledBy), Is.EqualTo(profileButton.Id));
            Assert.That(result.Children[2].Children[0].Text, Is.EqualTo("Profile"));
        });
    }

    [Test]
    public void ExpandsToolbarGroupsToolsOverflowAndRovingFocus()
    {
        UiTool save = new UiTool { Key = "save", Kind = "command", Label = "Save", CommandId = "file.save", GroupKey = "file" };
        UiTool pin = new UiTool { Key = "pin", Kind = "toggle", Label = "Pin", GroupKey = "file", Button = new UiButton { HasPressed = true, IsPressed = true } };
        UiTool separator = new UiTool { Key = "separator", Kind = "separator", Label = "" };
        UiTool search = new UiTool { Key = "search", Kind = "input", Label = "Search", Input = new UiInput { Name = "query", Value = StringValue("term") } };
        UiTool mode = new UiTool { Key = "mode", Kind = "choice", Label = "Mode", Choice = new UiChoice { Key = "compact", Label = "Compact", Value = StringValue("compact") } };
        UiNode node = Control("toolbar");
        node.Toolbar = new UiToolbar
        {
            Key = "main-tools",
            Label = "Editing tools",
            Tools = new UiTool[] { save, pin, separator, search, mode },
            Orientation = UiOrientation.Vertical,
            ActiveToolKey = "pin",
            HasOverflow = true
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode group = result.Children[0];
        DomNode saveResult = group.Children[0];
        DomNode pinResult = group.Children[1];
        DomNode separatorResult = result.Children[1];
        DomNode searchResult = result.Children[2];
        DomNode choiceResult = result.Children[3];
        DomNode overflow = result.Children[4];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ToolBar));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Editing tools"));
            Assert.That(AttributeValue(result, AriaAttributes.Orientation), Is.EqualTo("vertical"));
            Assert.That(AttributeValue(group, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Group));
            Assert.That(AttributeValue(group, "data-group-key"), Is.EqualTo("file"));
            Assert.That(AttributeValue(saveResult, "data-command-id"), Is.EqualTo("file.save"));
            Assert.That(AttributeValue(saveResult, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(pinResult, AriaAttributes.Pressed), Is.EqualTo("true"));
            Assert.That(AttributeValue(pinResult, HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(separatorResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Separator));
            Assert.That(searchResult.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(searchResult, HtmlAttributes.Name), Is.EqualTo("query"));
            Assert.That(AttributeValue(searchResult, HtmlAttributes.Value), Is.EqualTo("term"));
            Assert.That(AttributeValue(choiceResult, "data-choice-key"), Is.EqualTo("compact"));
            Assert.That(choiceResult.Children[0].Text, Is.EqualTo("Compact"));
            Assert.That(AttributeValue(overflow, AriaAttributes.HasPopup), Is.EqualTo("menu"));
        });
    }

    [Test]
    public void ToolbarCommandActiveStateAndOverflowChangesPreserveToolIdentity()
    {
        UiTool first = new UiTool { Key = "first", Kind = "command", Label = "First", CommandId = "command.first" };
        UiTool second = new UiTool { Key = "second", Kind = "command", Label = "Second", CommandId = "command.second" };
        UiNode node = Control("toolbar");
        node.Toolbar = new UiToolbar
        {
            Key = "commands",
            Tools = new UiTool[] { first, second },
            ActiveToolKey = "first"
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode original = expander.ExpandNode(node);
        string firstId = original.Children[0].Id;
        string secondId = original.Children[1].Id;
        node.Toolbar.ActiveToolKey = "second";
        node.Toolbar.HasOverflow = true;
        second.CommandId = "command.changed";
        DomNode changed = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(original.Children, Has.Length.EqualTo(2));
            Assert.That(AttributeValue(original.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(original.Children[1], "data-command-id"), Is.EqualTo("command.second"));
            Assert.That(changed.Children, Has.Length.EqualTo(3));
            Assert.That(changed.Children[0].Id, Is.EqualTo(firstId));
            Assert.That(changed.Children[1].Id, Is.EqualTo(secondId));
            Assert.That(AttributeValue(changed.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(changed.Children[1], HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(changed.Children[1], "data-command-id"), Is.EqualTo("command.changed"));
            Assert.That(changed.Children[2].Key, Is.EqualTo("$overflow"));
            Assert.That(AttributeValue(changed.Children[2], "data-overflow"), Is.EqualTo("commands"));
            Assert.That(AttributeValue(changed.Children[2], AriaAttributes.HasPopup), Is.EqualTo("menu"));
        });
    }

    [Test]
    public void ExpandsBreadcrumbLinksAndCurrentPage()
    {
        UiNode node = NavigationControl("breadcrumbs");
        node.Breadcrumb!.Items = new UiChoice[]
        {
            Choice("home", "Home", "/"),
            Choice("library", "Library", "/library")
        };
        node.Breadcrumb.HasCurrentIndex = true;
        node.Breadcrumb.CurrentIndex = 1;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode list = result.Children[0];
        DomNode home = list.Children[0].Children[0];
        DomNode current = list.Children[1].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Navigation));
            Assert.That(AttributeValue(result, "data-navigation-kind"), Is.EqualTo("breadcrumb"));
            Assert.That(list.Name, Is.EqualTo(HtmlElements.OrderedList));
            Assert.That(home.Name, Is.EqualTo(HtmlElements.Anchor));
            Assert.That(AttributeValue(home, HtmlAttributes.HyperlinkReference), Is.EqualTo("/"));
            Assert.That(AttributeValue(current, AriaAttributes.Current), Is.EqualTo("page"));
            Assert.That(current.Children[0].Text, Is.EqualTo("Library"));
        });
    }

    [TestCase("navigation-rail", "rail")]
    [TestCase("sidebar", "sidebar")]
    public void ExpandsRailAndSidebarNavigation(string name, string kind)
    {
        UiNode node = NavigationControl(name);
        UiChoice dashboard = Choice("dashboard", "Dashboard", null);
        dashboard.IsSelected = true;
        dashboard.IsDisabled = true;
        node.Breadcrumb!.Items = new UiChoice[] { dashboard };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode link = result.Children[0].Children[0].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-navigation-kind"), Is.EqualTo(kind));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.UnorderedList));
            Assert.That(link.Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(AttributeValue(link, AriaAttributes.Current), Is.EqualTo("true"));
            Assert.That(AttributeValue(link, AriaAttributes.Disabled), Is.EqualTo("true"));
        });
    }

    [Test]
    public void NavigationCurrentStateChangesWithoutChangingItemIdentity()
    {
        UiNode node = NavigationControl("breadcrumbs");
        node.Breadcrumb!.Items = new UiChoice[]
        {
            Choice("home", "Home", "/"),
            Choice("library", "Library", "/library")
        };
        node.Breadcrumb.HasCurrentIndex = true;
        node.Breadcrumb.CurrentIndex = 0;
        SemanticExpander expander = new SemanticExpander();

        DomNode original = expander.ExpandNode(node);
        string homeId = original.Children[0].Children[0].Id;
        string libraryId = original.Children[0].Children[1].Id;
        node.Breadcrumb.CurrentIndex = 1;
        DomNode changed = expander.ExpandNode(node);
        DomNode changedHome = changed.Children[0].Children[0];
        DomNode changedLibrary = changed.Children[0].Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(original.Children[0].Children[0].Children[0], AriaAttributes.Current), Is.EqualTo("page"));
            Assert.That(HasAttribute(changedHome.Children[0], AriaAttributes.Current), Is.False);
            Assert.That(AttributeValue(changedLibrary.Children[0], AriaAttributes.Current), Is.EqualTo("page"));
            Assert.That(changedHome.Id, Is.EqualTo(homeId));
            Assert.That(changedLibrary.Id, Is.EqualTo(libraryId));
        });
    }

    [Test]
    public void ExpandsKnownAndUnknownPaginationStates()
    {
        UiNode known = Control("pagination");
        known.Pagination = new UiPagination { Key = "known", PageIndex = 1, PageSize = 20, HasPageCount = true, PageCount = 3, HasPrevious = true, HasNext = true };
        known.State.IsBusy = true;
        UiNode unknown = Control("pager");
        unknown.Pagination = new UiPagination { Key = "unknown", PageIndex = 0, PageSize = 50, HasNext = true };
        SemanticExpander expander = new SemanticExpander();

        DomNode knownResult = expander.ExpandNode(known);
        DomNode unknownResult = expander.ExpandNode(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(knownResult.Name, Is.EqualTo(HtmlElements.Navigation));
            Assert.That(AttributeValue(knownResult, "data-page-count"), Is.EqualTo("3"));
            Assert.That(AttributeValue(knownResult, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(knownResult.Children, Has.Length.EqualTo(5));
            Assert.That(AttributeValue(knownResult.Children[2], AriaAttributes.Current), Is.EqualTo("page"));
            Assert.That(AttributeValue(knownResult.Children[2], "data-target-page"), Is.EqualTo("1"));
            Assert.That(HasAttribute(knownResult.Children[2], HtmlAttributes.Disabled), Is.True);
            Assert.That(HasAttribute(knownResult.Children[0], HtmlAttributes.Disabled), Is.False);
            Assert.That(HasAttribute(knownResult.Children[4], HtmlAttributes.Disabled), Is.False);
            Assert.That(HasAttribute(unknownResult, "data-page-count"), Is.False);
            Assert.That(unknownResult.Children, Has.Length.EqualTo(3));
            Assert.That(AttributeValue(unknownResult.Children[1], AriaAttributes.Current), Is.EqualTo("page"));
            Assert.That(HasAttribute(unknownResult.Children[0], HtmlAttributes.Disabled), Is.True);
            Assert.That(HasAttribute(unknownResult.Children[2], HtmlAttributes.Disabled), Is.False);
        });
    }

    [Test]
    public void RejectsMalformedTabsToolbarNavigationAndPagination()
    {
        UiNode missingTabs = Control("tabs");
        UiNode duplicateTabs = TabsControl(new UiTab[] { new UiTab { Key = "same", Label = "One" }, new UiTab { Key = "same", Label = "Two" } });
        UiNode disabledActive = TabsControl(new UiTab[] { new UiTab { Key = "disabled", Label = "Disabled", IsDisabled = true } });
        disabledActive.Tabs!.ActiveTabKey = "disabled";
        UiNode missingToolbar = Control("toolbar");
        UiNode invalidTool = Control("toolbar");
        invalidTool.Toolbar = new UiToolbar { Key = "tools", Tools = new UiTool[] { new UiTool { Key = "input", Kind = "input", Label = "Input" } } };
        UiNode splitGroups = Control("toolbar");
        splitGroups.Toolbar = new UiToolbar
        {
            Key = "tools",
            Tools = new UiTool[]
            {
                new UiTool { Key = "one", Kind = "button", Label = "One", GroupKey = "a" },
                new UiTool { Key = "two", Kind = "button", Label = "Two", GroupKey = "b" },
                new UiTool { Key = "three", Kind = "button", Label = "Three", GroupKey = "a" }
            }
        };
        UiNode missingNavigation = Control("rail");
        UiNode badNavigation = NavigationControl("breadcrumb");
        badNavigation.Breadcrumb!.Items = new UiChoice[] { Choice("home", "Home", "/") };
        badNavigation.Breadcrumb.HasCurrentIndex = true;
        badNavigation.Breadcrumb.CurrentIndex = 2;
        UiNode missingPagination = Control("pagination");
        UiNode badPagination = Control("pagination");
        badPagination.Pagination = new UiPagination { Key = "pages", PageIndex = 1, PageSize = 10, HasPageCount = true, PageCount = 2, HasPrevious = false, HasNext = false };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingTabs));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateTabs));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(disabledActive));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingToolbar));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidTool));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(splitGroups));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingNavigation));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badNavigation));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPagination));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badPagination));
        });
    }

    private static UiNode Control(string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = name + "-node",
                SiblingIdentity = name + "-node",
                StableId = name + "-node-id"
            }
        };
    }

    private static UiNode TabsControl(UiTab[] tabs)
    {
        UiNode node = Control("tabs");
        node.Tabs = new UiTabs { Key = "tabs", Tabs = tabs };
        return node;
    }

    private static UiNode NavigationControl(string name)
    {
        UiNode node = Control(name);
        node.Breadcrumb = new UiBreadcrumb { Key = "navigation", Label = "Application navigation" };
        return node;
    }

    private static UiNode TextNode(string key, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Text = text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" }
        };
    }

    private static UiChoice Choice(string key, string label, string? url)
    {
        return new UiChoice { Key = key, Label = label, Value = StringValue(key), Url = url };
    }

    private static UiValue StringValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return attribute;
            }
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }
}
