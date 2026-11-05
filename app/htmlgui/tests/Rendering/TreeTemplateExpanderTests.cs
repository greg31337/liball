using System;
using htmlgui;

namespace htmlgui.Tests;

public class TreeTemplateExpanderTests
{
    [Test]
    public void FlattensVisibleTreeItemsWithRelationshipsSelectionTypeAheadAndDragIntent()
    {
        UiTreeItem grandchild = Item("grandchild", "Grandchild");
        grandchild.Relationship = Relationship("child", 3, 1, 1);
        grandchild.Content = TextNode("grandchild-content", "grandchild-content-id", "Templated grandchild");
        UiTreeItem child = Item("child", "Child");
        child.Relationship = Relationship("root", 2, 1, 2);
        child.Children = new UiTreeItem[] { grandchild };
        child.IsExpanded = true;
        child.HasCheckState = true;
        child.CheckState = UiCommandCheckState.Mixed;
        UiTreeItem lazy = Item("lazy", "Lazy");
        lazy.Relationship = Relationship("root", 2, 2, 2);
        lazy.HasLazyChildren = true;
        UiTreeItem root = Item("root", "Root");
        root.Children = new UiTreeItem[] { child, lazy };
        root.IsExpanded = true;
        UiTreeItem other = Item("other", "Other");
        other.Relationship = Relationship(null, 1, 2, 2);
        other.IsSelected = true;
        UiNode node = TreeControl("tree", new UiTreeItem[] { root, other });
        node.Tree!.Selection = new UiSelection
        {
            Mode = UiSelectionMode.Multiple,
            SelectedKeys = new string[] { "child" },
            ActiveKey = "grandchild"
        };
        node.Tree.Range = new UiTreeRange { Start = 5, Count = 5, HasTotalCount = true, TotalCount = 10, AnchorKey = "child" };
        node.Tree.TypeAhead = new UiTypeAheadState { Buffer = "gr", MatchedKey = "grandchild", Generation = 4, IgnoreCase = false };
        node.Tree.CanReorder = true;
        node.State.Version = 8;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(result.Key, Is.EqualTo("tree-node"));
            Assert.That(result.Id, Is.EqualTo("tree-node-id"));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Tree));
            Assert.That(AttributeValue(result, AriaAttributes.MultiSelectable), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-range-start"), Is.EqualTo("5"));
            Assert.That(AttributeValue(result, "data-range-count"), Is.EqualTo("5"));
            Assert.That(AttributeValue(result, "data-total-count"), Is.EqualTo("10"));
            Assert.That(AttributeValue(result, "data-range-anchor"), Is.EqualTo("child"));
            Assert.That(AttributeValue(result, "data-typeahead-buffer"), Is.EqualTo("gr"));
            Assert.That(AttributeValue(result, "data-typeahead-generation"), Is.EqualTo("4"));
            Assert.That(AttributeValue(result, "data-typeahead-ignore-case"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-typeahead-match"), Is.EqualTo("grandchild"));
            Assert.That(AttributeValue(result, "data-typeahead-match-id"), Is.EqualTo(result.Children[2].Id));
            Assert.That(AttributeValue(result, AriaAttributes.ActiveDescendant), Is.EqualTo(result.Children[2].Id));
            Assert.That(AttributeValue(result, "data-reorder"), Is.EqualTo("true"));
            Assert.That(Array.ConvertAll(result.Children, childNode => childNode.Key), Is.EqualTo(new string[] { "root", "child", "grandchild", "lazy", "other" }));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Level), Is.EqualTo("1"));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[1], "data-parent-key"), Is.EqualTo("root"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Level), Is.EqualTo("2"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.PositionInSet), Is.EqualTo("1"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.SetSize), Is.EqualTo("2"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Checked), Is.EqualTo("mixed"));
            Assert.That(result.Children[2].Children[0].Text, Is.EqualTo("Templated grandchild"));
            Assert.That(AttributeValue(result.Children[2], "data-active"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[3], "data-lazy-children"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[3], AriaAttributes.Expanded), Is.EqualTo("false"));
            Assert.That(AttributeValue(result.Children[4], AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[4], "data-flat-index"), Is.EqualTo("9"));
            Assert.That(AttributeValue(result.Children[4], "draggable"), Is.EqualTo("true"));
            Assert.That(result.Children[4].Version, Is.EqualTo(8));
        });
    }

    [Test]
    public void ExpandsLazyTreeGridRowsWithRangeIndicesAndNodeState()
    {
        UiTreeItem lazy = Item("lazy", string.Empty);
        lazy.Value = TextValue("Lazy value");
        lazy.Relationship = Relationship(null, 1, 21, 100);
        lazy.HasLazyChildren = true;
        lazy.IsExpanded = true;
        lazy.IsSelected = true;
        lazy.HasCheckState = true;
        lazy.CheckState = UiCommandCheckState.Checked;
        UiNode node = TreeControl("tree-grid", new UiTreeItem[] { lazy });
        node.Tree!.IsTreeGrid = true;
        node.Tree.Selection.Mode = UiSelectionMode.Single;
        node.Tree.Range = new UiTreeRange { Start = 20, Count = 1, HasTotalCount = true, TotalCount = 100 };
        node.State.IsEnabled = false;
        node.State.IsVisible = false;
        node.State.IsBusy = true;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode row = result.Children[0];
        DomNode cell = row.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.TreeGrid));
            Assert.That(AttributeValue(result, AriaAttributes.ColumnCount), Is.EqualTo("1"));
            Assert.That(AttributeValue(result, AriaAttributes.RowCount), Is.EqualTo("100"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(FindAttribute(result, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(row, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Row));
            Assert.That(AttributeValue(row, AriaAttributes.RowIndex), Is.EqualTo("21"));
            Assert.That(AttributeValue(row, AriaAttributes.Level), Is.EqualTo("1"));
            Assert.That(AttributeValue(row, AriaAttributes.PositionInSet), Is.EqualTo("21"));
            Assert.That(AttributeValue(row, AriaAttributes.SetSize), Is.EqualTo("100"));
            Assert.That(AttributeValue(row, AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(row, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(row, AriaAttributes.Checked), Is.EqualTo("true"));
            Assert.That(AttributeValue(row, AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(row, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(cell, HtmlAttributes.Role), Is.EqualTo(AriaRoles.GridCell));
            Assert.That(AttributeValue(cell, AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(cell.Children[0].Text, Is.EqualTo("Lazy value"));
        });
    }

    [TestCase(UiSelectionMode.None, "none", false, false, false)]
    [TestCase(UiSelectionMode.Single, "single", false, true, false)]
    [TestCase(UiSelectionMode.Multiple, "multiple", true, true, true)]
    [TestCase(UiSelectionMode.Range, "range", true, true, true)]
    public void ProjectsEveryTreeSelectionMode(UiSelectionMode mode, string token, bool multiSelectable, bool firstSelected, bool secondSelected)
    {
        UiNode node = TreeControl("tree", new UiTreeItem[] { Item("first", "First"), Item("second", "Second") });
        string[] selectedKeys = Array.Empty<string>();
        if (firstSelected)
        {
            selectedKeys = secondSelected ? new string[] { "first", "second" } : new string[] { "first" };
        }

        node.Tree!.Selection = new UiSelection
        {
            Mode = mode,
            SelectedKeys = selectedKeys,
            AnchorKey = mode == UiSelectionMode.Range ? "first" : null
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-selection-mode"), Is.EqualTo(token));
            Assert.That(HasAttribute(result, AriaAttributes.MultiSelectable), Is.EqualTo(multiSelectable));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Selected), Is.EqualTo(firstSelected ? "true" : "false"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Selected), Is.EqualTo(secondSelected ? "true" : "false"));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Level), Is.EqualTo("1"));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.PositionInSet), Is.EqualTo("1"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.PositionInSet), Is.EqualTo("2"));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.SetSize), Is.EqualTo("2"));
        });
    }

    [Test]
    public void CollapsedBranchesAreNotExpandedAndStableIdentitiesSurviveExpansionAndReordering()
    {
        UiTreeItem child = Item("child", "Child");
        child.Relationship.ParentKey = "root";
        child.Content = TextNode("child-content", "child-content-id", "Child content");
        UiTreeItem root = Item("root", "Root");
        root.Children = new UiTreeItem[] { child };
        UiTreeItem other = Item("other", "Other");
        UiNode node = TreeControl("tree", new UiTreeItem[] { root, other });
        SemanticExpander expander = new SemanticExpander();

        DomNode collapsed = expander.ExpandNode(node);
        root.IsExpanded = true;
        DomNode expanded = expander.ExpandNode(node);
        node.Tree!.Items = new UiTreeItem[] { other, root };
        DomNode reordered = expander.ExpandNode(node);
        expanded.Children[1].Children[0].Text = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(collapsed.Children, item => item.Key), Is.EqualTo(new string[] { "root", "other" }));
            Assert.That(Array.ConvertAll(expanded.Children, item => item.Key), Is.EqualTo(new string[] { "root", "child", "other" }));
            Assert.That(Array.ConvertAll(reordered.Children, item => item.Key), Is.EqualTo(new string[] { "other", "root", "child" }));
            Assert.That(collapsed.Children[0].Id, Is.EqualTo(expanded.Children[0].Id));
            Assert.That(expanded.Children[0].Id, Is.EqualTo(reordered.Children[1].Id));
            Assert.That(expanded.Children[1].Id, Is.EqualTo(reordered.Children[2].Id));
            Assert.That(reordered.Children[2].Children[0].Text, Is.EqualTo("Child content"));
            Assert.That(root.Relationship.Depth, Is.Zero);
            Assert.That(child.Relationship.Depth, Is.Zero);
            Assert.That(other.Relationship.PositionInSet, Is.Zero);
        });
    }

    [Test]
    public void CollapsedContentBranchesRemainInactive()
    {
        UiTreeItem child = Item("child", "Child");
        child.Relationship.ParentKey = "root";
        child.Content = InvalidRawNode("inactive");
        UiTreeItem root = Item("root", "Root");
        root.Children = new UiTreeItem[] { child };
        UiNode node = TreeControl("tree", new UiTreeItem[] { root });

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.That(result.Children, Has.Length.EqualTo(1));
    }

    [Test]
    public void RejectsMalformedHierarchyRelationshipLazyAndCheckState()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "treegrid",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode blankKey = TreeControl("tree", Array.Empty<UiTreeItem>());
        blankKey.Tree!.Key = " ";
        UiNode nullItems = TreeControl("tree", Array.Empty<UiTreeItem>());
        nullItems.Tree!.Items = null!;
        UiNode duplicate = TreeControl("tree", new UiTreeItem[] { Item("same", "One"), Item("same", "Two") });
        UiTreeItem mismatchedChild = Item("child", "Child");
        mismatchedChild.Relationship.ParentKey = "wrong";
        UiTreeItem mismatchRoot = Item("root", "Root");
        mismatchRoot.Children = new UiTreeItem[] { mismatchedChild };
        UiNode mismatch = TreeControl("tree", new UiTreeItem[] { mismatchRoot });
        UiTreeItem wrongDepthItem = Item("depth", "Depth");
        wrongDepthItem.Relationship.Depth = 2;
        UiNode wrongDepth = TreeControl("tree", new UiTreeItem[] { wrongDepthItem });
        UiTreeItem wrongPositionItem = Item("position", "Position");
        wrongPositionItem.Relationship = Relationship(null, 1, 3, 2);
        UiNode wrongPosition = TreeControl("tree", new UiTreeItem[] { wrongPositionItem });
        UiTreeItem negativePositionItem = Item("negative-position", "Negative position");
        negativePositionItem.Relationship.PositionInSet = -1;
        UiNode negativePosition = TreeControl("tree", new UiTreeItem[] { negativePositionItem });
        UiTreeItem emptySetItem = Item("empty-set", "Empty set");
        emptySetItem.Relationship = Relationship(null, 1, 1, 0);
        UiNode emptySet = TreeControl("tree", new UiTreeItem[] { emptySetItem });
        UiTreeItem nullRelationshipItem = Item("null-relationship", "Null relationship");
        nullRelationshipItem.Relationship = null!;
        UiNode nullRelationship = TreeControl("tree", new UiTreeItem[] { nullRelationshipItem });
        UiTreeItem lazyItem = Item("lazy", "Lazy");
        UiTreeItem loadedChild = Item("loaded", "Loaded");
        loadedChild.Relationship.ParentKey = "lazy";
        lazyItem.Children = new UiTreeItem[] { loadedChild };
        lazyItem.HasLazyChildren = true;
        UiNode lazyAndLoaded = TreeControl("tree", new UiTreeItem[] { lazyItem });
        UiTreeItem badCheckItem = Item("checked", "Checked");
        badCheckItem.HasCheckState = true;
        badCheckItem.CheckState = (UiCommandCheckState)999;
        UiNode badCheck = TreeControl("tree", new UiTreeItem[] { badCheckItem });
        UiTreeItem cyclic = Item("cycle", "Cycle");
        cyclic.Relationship.ParentKey = "cycle";
        cyclic.Children = new UiTreeItem[] { cyclic };
        UiNode cycle = TreeControl("tree", new UiTreeItem[] { cyclic });
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankKey));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullItems));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicate));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(mismatch));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongDepth));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongPosition));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(negativePosition));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(emptySet));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullRelationship));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(lazyAndLoaded));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(badCheck))!.ParamName, Is.EqualTo("checkState"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(cycle));
        });
    }

    [Test]
    public void RejectsMalformedRangeSelectionAndTypeAheadState()
    {
        UiTreeItem child = Item("child", "Child");
        child.Relationship.ParentKey = "root";
        UiTreeItem root = Item("root", "Root");
        root.Children = new UiTreeItem[] { child };
        UiNode mismatchedCount = TreeControl("tree", new UiTreeItem[] { root });
        mismatchedCount.Tree!.Range.Count = 2;
        UiNode outsideTotal = TreeControl("tree", new UiTreeItem[] { Item("item", "Item") });
        outsideTotal.Tree!.Range = new UiTreeRange { Start = 1, Count = 1, HasTotalCount = true, TotalCount = 1 };
        UiNode hiddenRangeAnchor = TreeControl("tree", new UiTreeItem[] { root });
        hiddenRangeAnchor.Tree!.Range.AnchorKey = "child";
        UiNode selectedWithoutMode = TreeControl("tree", new UiTreeItem[] { Item("item", "Item") });
        selectedWithoutMode.Tree!.Selection.SelectedKeys = new string[] { "item" };
        UiNode unknownSelected = TreeControl("tree", new UiTreeItem[] { Item("item", "Item") });
        unknownSelected.Tree!.Selection = new UiSelection { Mode = UiSelectionMode.Multiple, SelectedKeys = new string[] { "missing" } };
        UiNode activeHidden = TreeControl("tree", new UiTreeItem[] { root });
        activeHidden.Tree!.Selection = new UiSelection { Mode = UiSelectionMode.Single, ActiveKey = "child" };
        UiNode wrongAnchor = TreeControl("tree", new UiTreeItem[] { Item("item", "Item") });
        wrongAnchor.Tree!.Selection = new UiSelection { Mode = UiSelectionMode.Single, AnchorKey = "item" };
        UiNode nullTypeAhead = TreeControl("tree", Array.Empty<UiTreeItem>());
        nullTypeAhead.Tree!.TypeAhead = null!;
        UiNode negativeGeneration = TreeControl("tree", Array.Empty<UiTreeItem>());
        negativeGeneration.Tree!.TypeAhead.Generation = -1;
        UiNode hiddenMatch = TreeControl("tree", new UiTreeItem[] { root });
        hiddenMatch.Tree!.TypeAhead.MatchedKey = "child";
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(mismatchedCount));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(outsideTotal));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(hiddenRangeAnchor));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(selectedWithoutMode));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownSelected));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(activeHidden));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongAnchor));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullTypeAhead));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(negativeGeneration));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(hiddenMatch));
        });
    }

    private static UiNode TreeControl(string name, UiTreeItem[] items)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = "tree-node",
                SiblingIdentity = "tree-node",
                StableId = "tree-node-id"
            },
            Tree = new UiTree { Key = "files", Items = items }
        };
    }

    private static UiTreeItem Item(string key, string label)
    {
        return new UiTreeItem { Key = key, Label = label, Value = TextValue(label) };
    }

    private static UiTreeRelationship Relationship(string? parentKey, int depth, int position, int setSize)
    {
        return new UiTreeRelationship
        {
            ParentKey = parentKey,
            Depth = depth,
            PositionInSet = position,
            HasSetSize = true,
            SetSize = setSize
        };
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

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }
}
