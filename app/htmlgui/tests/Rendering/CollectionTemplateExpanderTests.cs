using System;
using htmlgui;

namespace htmlgui.Tests;

public class CollectionTemplateExpanderTests
{
    [TestCase("list", HtmlElements.UnorderedList, "htmlgui-list htmlgui-unordered-list")]
    [TestCase("unordered-list", HtmlElements.UnorderedList, "htmlgui-list htmlgui-unordered-list")]
    [TestCase("ordered-list", HtmlElements.OrderedList, "htmlgui-list htmlgui-ordered-list")]
    public void ExpandsStaticListsWithTemplatesAndValueFallbacks(string name, string elementName, string className)
    {
        UiCollectionItem templated = Item("templated", "ignored", TextNode("template", "template-id", "Template"));
        UiCollectionItem fallback = Item("fallback", "Fallback", null);
        fallback.IsPlaceholder = true;
        UiNode node = CollectionControl(name, new UiCollectionItem[] { templated, fallback });
        node.State.Version = 7;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(result.Key, Is.EqualTo("collection-node"));
            Assert.That(result.Id, Is.EqualTo("collection-node-id"));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo(className));
            Assert.That(AttributeValue(result, "data-collection-key"), Is.EqualTo("items"));
            Assert.That(AttributeValue(result, "data-collection-state"), Is.EqualTo("ready"));
            Assert.That(AttributeValue(result, "data-page-count"), Is.EqualTo("2"));
            Assert.That(AttributeValue(result, "data-selection-mode"), Is.EqualTo("none"));
            Assert.That(result.Children, Has.Length.EqualTo(2));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.ListItem));
            Assert.That(result.Children[0].Key, Is.EqualTo("templated"));
            Assert.That(result.Children[0].Children[0].Text, Is.EqualTo("Template"));
            Assert.That(result.Children[1].Children[0].Text, Is.EqualTo("Fallback"));
            Assert.That(AttributeValue(result.Children[1], "data-placeholder"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(result.Children[1].Version, Is.EqualTo(7));
        });
    }

    [Test]
    public void ExpandsDescriptionListTermsAndTemplatedDetails()
    {
        UiCollectionItem item = Item("language", "Language", TextNode("details", "details-id", "C#"));
        UiNode node = CollectionControl("description-list", new UiCollectionItem[] { item });

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode entry = result.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.DescriptionList));
            Assert.That(entry.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(entry.Key, Is.EqualTo("language"));
            Assert.That(entry.Children[0].Name, Is.EqualTo(HtmlElements.DescriptionTerm));
            Assert.That(entry.Children[0].Children[0].Text, Is.EqualTo("Language"));
            Assert.That(entry.Children[1].Name, Is.EqualTo(HtmlElements.DescriptionDetails));
            Assert.That(entry.Children[1].Children[0].Text, Is.EqualTo("C#"));
        });
    }

    [Test]
    public void ExpandsInteractiveSelectionPagingActiveStateAndReorderIntent()
    {
        UiCollectionItem first = Item("first", "First", null);
        UiCollectionItem second = Item("second", "Second", null);
        second.IsDisabled = true;
        UiNode node = CollectionControl("interactive-list", new UiCollectionItem[] { first, second });
        node.Collection!.Selection = new UiSelection
        {
            Mode = UiSelectionMode.Range,
            SelectedKeys = new string[] { "second" },
            ActiveKey = "second",
            AnchorKey = "first"
        };
        node.Collection.CanReorder = true;
        node.Collection.PageRange = new UiPageRange { Start = 10, Count = 2, HasTotalCount = true, TotalCount = 20 };
        node.State.IsEnabled = false;
        node.State.IsVisible = false;
        node.State.IsBusy = true;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode firstResult = result.Children[0];
        DomNode secondResult = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ListBox));
            Assert.That(AttributeValue(result, AriaAttributes.MultiSelectable), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.ActiveDescendant), Is.EqualTo(secondResult.Id));
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(FindAttribute(result, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-selection-anchor"), Is.EqualTo("first"));
            Assert.That(AttributeValue(result, "data-reorder"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-page-start"), Is.EqualTo("10"));
            Assert.That(AttributeValue(result, "data-page-count"), Is.EqualTo("2"));
            Assert.That(AttributeValue(result, "data-total-count"), Is.EqualTo("20"));
            Assert.That(AttributeValue(firstResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Option));
            Assert.That(AttributeValue(firstResult, AriaAttributes.Selected), Is.EqualTo("false"));
            Assert.That(AttributeValue(firstResult, AriaAttributes.PositionInSet), Is.EqualTo("11"));
            Assert.That(AttributeValue(firstResult, AriaAttributes.SetSize), Is.EqualTo("20"));
            Assert.That(AttributeValue(firstResult, "draggable"), Is.EqualTo("true"));
            Assert.That(AttributeValue(secondResult, AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(secondResult, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(secondResult, "data-active"), Is.EqualTo("true"));
            Assert.That(AttributeValue(secondResult, AriaAttributes.PositionInSet), Is.EqualTo("12"));
        });
    }

    [TestCase(UiSelectionMode.None, "none", false, false, false)]
    [TestCase(UiSelectionMode.Single, "single", false, true, false)]
    [TestCase(UiSelectionMode.Multiple, "multiple", true, true, true)]
    [TestCase(UiSelectionMode.Range, "range", true, true, true)]
    public void ProjectsEverySelectionModeAndOpenEndedPageMetadata(UiSelectionMode mode, string token, bool multiSelectable, bool firstSelected, bool secondSelected)
    {
        UiNode node = CollectionControl(
            "interactive-list",
            new UiCollectionItem[] { Item("first", "First", null), Item("second", "Second", null) });
        string[] selectedKeys = Array.Empty<string>();
        if (firstSelected)
        {
            selectedKeys = secondSelected ? new string[] { "first", "second" } : new string[] { "first" };
        }

        node.Collection!.Selection = new UiSelection
        {
            Mode = mode,
            SelectedKeys = selectedKeys,
            AnchorKey = mode == UiSelectionMode.Range ? "first" : null
        };
        node.Collection.PageRange = new UiPageRange { IsEnd = true };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-selection-mode"), Is.EqualTo(token));
            Assert.That(HasAttribute(result, AriaAttributes.MultiSelectable), Is.EqualTo(multiSelectable));
            Assert.That(AttributeValue(result, "data-page-start"), Is.EqualTo("0"));
            Assert.That(AttributeValue(result, "data-page-count"), Is.EqualTo("2"));
            Assert.That(AttributeValue(result, "data-page-end"), Is.EqualTo("true"));
            Assert.That(HasAttribute(result, "data-total-count"), Is.False);
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Selected), Is.EqualTo(firstSelected ? "true" : "false"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Selected), Is.EqualTo(secondSelected ? "true" : "false"));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.PositionInSet), Is.EqualTo("1"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.PositionInSet), Is.EqualTo("2"));
            Assert.That(HasAttribute(result.Children[0], AriaAttributes.SetSize), Is.False);
        });
    }

    [Test]
    public void ProjectsSortAndFilterDescriptionsWithoutMutatingTheirOrder()
    {
        UiSort later = new UiSort { Key = "name", Direction = UiSortDirection.Descending, Priority = 2 };
        UiSort first = new UiSort { Key = "date", Direction = UiSortDirection.Ascending, Priority = 0 };
        UiFilter filter = new UiFilter
        {
            Key = "status",
            Operator = "in",
            Value = new UiValue
            {
                Kind = UiValueKind.Array,
                ArrayValue = new UiValue[] { TextValue("open"), TextValue("pending") }
            },
            IgnoreCase = true
        };
        UiNode node = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null) });
        node.Collection!.Sorts = new UiSort[] { later, first };
        node.Collection.Filters = new UiFilter[] { filter };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-sort-0-key"), Is.EqualTo("date"));
            Assert.That(AttributeValue(result, "data-sort-0-direction"), Is.EqualTo("ascending"));
            Assert.That(AttributeValue(result, "data-sort-0-priority"), Is.EqualTo("0"));
            Assert.That(AttributeValue(result, "data-sort-1-key"), Is.EqualTo("name"));
            Assert.That(AttributeValue(result, "data-sort-1-direction"), Is.EqualTo("descending"));
            Assert.That(AttributeValue(result, "data-filter-0-key"), Is.EqualTo("status"));
            Assert.That(AttributeValue(result, "data-filter-0-operator"), Is.EqualTo("in"));
            Assert.That(AttributeValue(result, "data-filter-0-ignore-case"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-filter-0-value"), Is.EqualTo("{\"kind\":\"array\",\"value\":[{\"kind\":\"string\",\"value\":\"open\"},{\"kind\":\"string\",\"value\":\"pending\"}]}"));
            Assert.That(node.Collection.Sorts[0], Is.SameAs(later));
            Assert.That(node.Collection.Sorts[1], Is.SameAs(first));
        });
    }

    [Test]
    public void ExpandsGroupsInDeclaredItemOrderAndPreservesCollapsedContent()
    {
        UiCollectionItem alpha = Item("alpha", "Alpha", null);
        alpha.GroupKey = "letters";
        UiCollectionItem beta = Item("beta", "Beta", null);
        beta.GroupKey = "letters";
        UiCollectionItem other = Item("other", "Other", null);
        UiGroup group = new UiGroup
        {
            Key = "letters",
            Label = "Letters",
            ItemKeys = new string[] { "beta", "alpha" },
            IsExpanded = false
        };
        UiNode node = CollectionControl("list", new UiCollectionItem[] { alpha, beta, other });
        node.Collection!.Groups = new UiGroup[] { group };
        SemanticExpander expander = new SemanticExpander();

        DomNode collapsed = expander.ExpandNode(node);
        DomNode collapsedGroup = collapsed.Children[0];
        DomNode collapsedItems = collapsedGroup.Children[1];
        group.IsExpanded = true;
        DomNode expanded = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(collapsed.Children, Has.Length.EqualTo(2));
            Assert.That(collapsedGroup.Name, Is.EqualTo(HtmlElements.ListItem));
            Assert.That(AttributeValue(collapsedGroup, "data-group-key"), Is.EqualTo("letters"));
            Assert.That(AttributeValue(collapsedGroup, "data-expanded"), Is.EqualTo("false"));
            Assert.That(collapsedGroup.Children[0].Children[0].Text, Is.EqualTo("Letters"));
            Assert.That(collapsedItems.Name, Is.EqualTo(HtmlElements.UnorderedList));
            Assert.That(FindAttribute(collapsedItems, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(collapsedItems.Children[0].Key, Is.EqualTo("beta"));
            Assert.That(collapsedItems.Children[1].Key, Is.EqualTo("alpha"));
            Assert.That(collapsed.Children[1].Key, Is.EqualTo("other"));
            Assert.That(HasAttribute(expanded.Children[0].Children[1], HtmlAttributes.Hidden), Is.False);
            Assert.That(expanded.Children[0].Children[1].Children[0].Id, Is.EqualTo(collapsedItems.Children[0].Id));
        });
    }

    [Test]
    public void DerivesGroupMembershipFromItemGroupKeys()
    {
        UiCollectionItem item = Item("member", "Member", null);
        item.GroupKey = "group";
        UiNode node = CollectionControl("interactive-list", new UiCollectionItem[] { item });
        node.Collection!.Groups = new UiGroup[] { new UiGroup { Key = "group", Label = "Group" } };
        node.Collection.Selection.Mode = UiSelectionMode.Single;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.Role), Is.EqualTo(AriaRoles.Group));
            Assert.That(result.Children[0].Children[1].Children[0].Key, Is.EqualTo("member"));
        });
    }

    [Test]
    public void ExpandsEmptyLoadingErrorAndIncrementalLoadingStates()
    {
        UiNode empty = CollectionControl("list", Array.Empty<UiCollectionItem>());
        empty.Collection!.EmptyContent = TextNode("empty", "empty-id", "Nothing here");
        UiNode loading = CollectionControl("list", Array.Empty<UiCollectionItem>());
        loading.Collection!.IsLoading = true;
        loading.Collection.LoadingContent = TextNode("loading", "loading-id", "Loading");
        loading.Collection.ErrorContent = InvalidRawNode("unused-error");
        UiNode error = CollectionControl("list", new UiCollectionItem[] { Item("ignored", "Ignored", InvalidRawNode("ignored-item")) });
        error.Collection!.HasError = true;
        error.Collection.ErrorContent = TextNode("error", "error-id", "Failed");
        UiNode incremental = CollectionControl("list", new UiCollectionItem[] { Item("ready", "Ready", null) });
        incremental.Collection!.IsLoading = true;
        incremental.Collection.LoadingContent = TextNode("more", "more-id", "Loading more");
        SemanticExpander expander = new SemanticExpander();

        DomNode emptyResult = expander.ExpandNode(empty);
        DomNode loadingResult = expander.ExpandNode(loading);
        DomNode errorResult = expander.ExpandNode(error);
        DomNode incrementalResult = expander.ExpandNode(incremental);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(emptyResult, "data-collection-state"), Is.EqualTo("empty"));
            Assert.That(emptyResult.Children[0].Key, Is.EqualTo("$collection-empty"));
            Assert.That(emptyResult.Children[0].Children[0].Text, Is.EqualTo("Nothing here"));
            Assert.That(AttributeValue(loadingResult, "data-collection-state"), Is.EqualTo("loading"));
            Assert.That(AttributeValue(loadingResult, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(loadingResult.Children[0].Children[0].Text, Is.EqualTo("Loading"));
            Assert.That(AttributeValue(errorResult, "data-collection-state"), Is.EqualTo("error"));
            Assert.That(AttributeValue(errorResult.Children[0], HtmlAttributes.Role), Is.EqualTo(AriaRoles.Alert));
            Assert.That(errorResult.Children[0].Children[0].Text, Is.EqualTo("Failed"));
            Assert.That(incrementalResult.Children, Has.Length.EqualTo(2));
            Assert.That(incrementalResult.Children[0].Key, Is.EqualTo("ready"));
            Assert.That(incrementalResult.Children[1].Key, Is.EqualTo("$collection-loading"));
        });
    }

    [Test]
    public void StableItemIdentitiesSurviveReorderingWithoutModelMutation()
    {
        UiCollectionItem alpha = Item("alpha", "Alpha", null);
        UiCollectionItem beta = Item("beta", "Beta", null);
        UiNode node = CollectionControl("list", new UiCollectionItem[] { alpha, beta });
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        node.Collection!.Items = new UiCollectionItem[] { beta, alpha };
        DomNode reordered = expander.ExpandNode(node);
        first.Children[0].Children[0].Text = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.Children[0].Key, Is.EqualTo("alpha"));
            Assert.That(reordered.Children[1].Key, Is.EqualTo("alpha"));
            Assert.That(first.Children[0].Id, Is.EqualTo(reordered.Children[1].Id));
            Assert.That(first.Children[1].Id, Is.EqualTo(reordered.Children[0].Id));
            Assert.That(reordered.Children[1].Children[0].Text, Is.EqualTo("Alpha"));
            Assert.That(alpha.Value.StringValue, Is.EqualTo("Alpha"));
        });
    }

    [Test]
    public void RejectsMalformedItemsSelectionAndPaging()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "collection",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode blankCollectionKey = CollectionControl("list", Array.Empty<UiCollectionItem>());
        blankCollectionKey.Collection!.Key = " ";
        UiNode nullItems = CollectionControl("list", Array.Empty<UiCollectionItem>());
        nullItems.Collection!.Items = null!;
        UiNode nullItem = CollectionControl("list", new UiCollectionItem[] { null! });
        UiNode blankItem = CollectionControl("list", new UiCollectionItem[] { Item(" ", "Value", null) });
        UiNode duplicateItems = CollectionControl("list", new UiCollectionItem[] { Item("same", "One", null), Item("same", "Two", null) });
        UiNode nullValue = CollectionControl("list", new UiCollectionItem[] { Item("value", "Value", null) });
        nullValue.Collection!.Items[0].Value = null!;
        UiNode nullSelection = CollectionControl("list", Array.Empty<UiCollectionItem>());
        nullSelection.Collection!.Selection = null!;
        UiNode disabledSelection = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null) });
        disabledSelection.Collection!.Selection.SelectedKeys = new string[] { "one" };
        UiNode tooManySelected = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null), Item("two", "Two", null) });
        tooManySelected.Collection!.Selection = new UiSelection { Mode = UiSelectionMode.Single, SelectedKeys = new string[] { "one", "two" } };
        UiNode unknownSelected = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null) });
        unknownSelected.Collection!.Selection = new UiSelection { Mode = UiSelectionMode.Multiple, SelectedKeys = new string[] { "missing" } };
        UiNode wrongAnchor = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null) });
        wrongAnchor.Collection!.Selection = new UiSelection { Mode = UiSelectionMode.Single, AnchorKey = "one" };
        UiNode unknownActive = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null) });
        unknownActive.Collection!.Selection.ActiveKey = "missing";
        UiNode negativePage = CollectionControl("list", Array.Empty<UiCollectionItem>());
        negativePage.Collection!.PageRange.Start = -1;
        UiNode outsideTotal = CollectionControl("list", new UiCollectionItem[] { Item("one", "One", null) });
        outsideTotal.Collection!.PageRange = new UiPageRange { Start = 1, Count = 1, HasTotalCount = true, TotalCount = 1 };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankCollectionKey));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullItems));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullItem));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankItem));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateItems));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullValue));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullSelection));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(disabledSelection));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(tooManySelected));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownSelected));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongAnchor));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownActive));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(negativePage));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(outsideTotal));
        });
    }

    [Test]
    public void RejectsMalformedSortFilterAndGroupMetadata()
    {
        UiNode nullSorts = CollectionControl("list", Array.Empty<UiCollectionItem>());
        nullSorts.Collection!.Sorts = null!;
        UiNode badSort = CollectionControl("list", Array.Empty<UiCollectionItem>());
        badSort.Collection!.Sorts = new UiSort[] { new UiSort { Key = "field", Direction = (UiSortDirection)999 } };
        UiNode duplicateSortPriority = CollectionControl("list", Array.Empty<UiCollectionItem>());
        duplicateSortPriority.Collection!.Sorts = new UiSort[]
        {
            new UiSort { Key = "one", Priority = 0 },
            new UiSort { Key = "two", Priority = 0 }
        };
        UiNode nullFilters = CollectionControl("list", Array.Empty<UiCollectionItem>());
        nullFilters.Collection!.Filters = null!;
        UiNode badFilter = CollectionControl("list", Array.Empty<UiCollectionItem>());
        badFilter.Collection!.Filters = new UiFilter[] { new UiFilter { Key = "field", Operator = " " } };
        UiCollectionItem member = Item("member", "Member", null);
        member.GroupKey = "other";
        UiNode mismatch = CollectionControl("list", new UiCollectionItem[] { member });
        mismatch.Collection!.Groups = new UiGroup[] { new UiGroup { Key = "group", Label = "Group", ItemKeys = new string[] { "member" } } };
        UiNode unknownGroup = CollectionControl("list", new UiCollectionItem[] { Item("member", "Member", null) });
        unknownGroup.Collection!.Items[0].GroupKey = "missing";
        UiNode duplicateGroups = CollectionControl("list", Array.Empty<UiCollectionItem>());
        duplicateGroups.Collection!.Groups = new UiGroup[] { new UiGroup { Key = "same" }, new UiGroup { Key = "same" } };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullSorts));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(badSort))!.ParamName, Is.EqualTo("sortDirection"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateSortPriority));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullFilters));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badFilter));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(mismatch));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownGroup));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateGroups));
        });
    }

    [Test]
    public void RejectsConflictingOrIncompleteErrorState()
    {
        UiNode missingError = CollectionControl("list", Array.Empty<UiCollectionItem>());
        missingError.Collection!.HasError = true;
        UiNode conflicting = CollectionControl("list", Array.Empty<UiCollectionItem>());
        conflicting.Collection!.HasError = true;
        conflicting.Collection.IsLoading = true;
        conflicting.Collection.ErrorContent = TextNode("error", "error-id", "Error");
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingError));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(conflicting));
        });
    }

    private static UiNode CollectionControl(string name, UiCollectionItem[] items)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = "collection-node",
                SiblingIdentity = "collection-node",
                StableId = "collection-node-id"
            },
            Collection = new UiCollection { Key = "items", Items = items }
        };
    }

    private static UiCollectionItem Item(string key, string value, UiNode? content)
    {
        return new UiCollectionItem { Key = key, Value = TextValue(value), Content = content };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }

    private static UiNode InvalidRawNode(string key)
    {
        return new UiNode
        {
            Kind = UiNodeKind.RawContent,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" },
            Text = "<b>unsafe</b>"
        };
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index];
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }
}
