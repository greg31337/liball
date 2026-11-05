using System;
using System.Collections.Generic;

namespace htmlgui;

internal class SemanticExpander
{
    internal Dictionary<string, List<DomNode>> PortalRegions;
    internal bool PortalCollectionEnabled;
    internal string[] ResourceCapabilities;

    internal SemanticExpander()
    {
        PortalRegions = new Dictionary<string, List<DomNode>>(StringComparer.Ordinal);
        ResourceCapabilities = Array.Empty<string>();
    }

    internal DomNode ExpandApplication(UiApplication? application)
    {
        if (application == null)
        {
            throw new ArgumentNullException("application");
        }

        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = application.Key,
            Id = application.Id,
            Children = ExpandWindows(application.Windows)
        };
    }

    internal DomNode[] ExpandWindows(UiWindow[]? windows)
    {
        if (windows == null)
        {
            return Array.Empty<DomNode>();
        }

        DomNode[] result = new DomNode[windows.Length];
        int index = 0;
        while (index < windows.Length)
        {
            UiWindow? window = windows[index];
            if (window == null)
            {
                result[index] = new DomNode { Kind = DomNodeKind.Fragment };
            }
            else
            {
                result[index] = ExpandWindow(window);
            }

            index++;
        }

        return result;
    }

    internal DomNode ExpandWindow(UiWindow window)
    {
        ResourceCapabilities = window.HostCapabilities ?? Array.Empty<string>();
        BeginPortalCollection(window.Overlays);
        List<DomNode> children = new List<DomNode>();
        UiPage[]? pages = window.Pages;
        if (pages != null)
        {
            int pageIndex = 0;
            while (pageIndex < pages.Length)
            {
                UiPage? page = pages[pageIndex];
                if (page != null)
                {
                    children.Add(ExpandPage(page));
                }

                pageIndex++;
            }
        }

        UiView[]? overlays = window.Overlays;
        if (overlays != null)
        {
            List<DomNode> expandedOverlays = new List<DomNode>();
            int expansionIndex = 0;
            while (expansionIndex < overlays.Length)
            {
                UiView? overlay = overlays[expansionIndex];
                if (overlay != null)
                {
                    expandedOverlays.Add(ExpandView(overlay));
                }

                expansionIndex++;
            }

            int overlayIndex = 0;
            int expandedIndex = 0;
            while (overlayIndex < overlays.Length)
            {
                UiView? overlay = overlays[overlayIndex];
                if (overlay != null)
                {
                    DomNode expandedOverlay = expandedOverlays[expandedIndex];
                    AppendChildren(expandedOverlay, TakePortalContent(overlay.Key));
                    children.Add(expandedOverlay);
                    expandedIndex++;
                }

                overlayIndex++;
            }
        }

        EndPortalCollection();
        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = window.Key,
            Id = window.Id,
            Children = children.ToArray()
        };
    }

    internal DomNode ExpandPage(UiPage page)
    {
        List<DomNode> children = new List<DomNode>();
        AddView(page.ShellView, children);
        AddView(page.ContentView, children);
        AddView(page.NotFoundView, children);
        AddView(page.ErrorView, children);
        AddView(page.LoadingView, children);
        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = page.Key,
            Id = page.Id,
            Children = children.ToArray()
        };
    }

    internal void AddView(UiView? view, List<DomNode> children)
    {
        if (view != null)
        {
            children.Add(ExpandView(view));
        }
    }

    internal DomNode ExpandView(UiView view)
    {
        DomNode[] children = Array.Empty<DomNode>();
        if (view.ContentRoot != null)
        {
            children = new DomNode[] { ExpandNodeWithProjection(view.ContentRoot, Array.Empty<UiSlot>()) };
        }

        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = view.Key,
            Id = view.Id,
            Children = children
        };
    }

    internal DomNode ExpandNode(UiNode node)
    {
        return ExpandNodeWithProjection(node, Array.Empty<UiSlot>());
    }

    internal DomNode ExpandNodeWithProjection(UiNode node, UiSlot[]? projectedSlots)
    {
        switch (node.Kind)
        {
            case UiNodeKind.Element:
            case UiNodeKind.Text:
            case UiNodeKind.Comment:
            case UiNodeKind.RawContent:
                DomNode[] lowLevelChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
                RenderTreeBuilder builder = new RenderTreeBuilder();
                return builder.BuildLowLevelNode(node, lowLevelChildren);
            case UiNodeKind.Conditional:
                return ExpandConditional(node, projectedSlots);
            case UiNodeKind.Repeat:
                return ExpandRepeat(node, projectedSlots);
            case UiNodeKind.Slot:
                return ExpandSlot(node, projectedSlots);
            case UiNodeKind.Portal:
                return ExpandPortal(node, projectedSlots);
            case UiNodeKind.Layout:
                return ExpandLayout(node, projectedSlots);
            case UiNodeKind.Component:
                return CreateFragment(node, ExpandNodesWithProjection(node.Children, node.Slots));
            case UiNodeKind.Fragment:
                return CreateFragment(node, ExpandNodesWithProjection(node.Children, projectedSlots));
            case UiNodeKind.Control:
                DomNode expandedControl = ExpandControl(node, projectedSlots);
                return new AccessibilityProjector().ProjectControl(node, expandedControl);
            default:
                throw new ArgumentOutOfRangeException("kind", node.Kind, "The UI node kind is not supported.");
        }
    }

    internal DomNode ExpandControl(UiNode node, UiSlot[]? projectedSlots)
    {
        ButtonTemplateExpander buttonExpander = new ButtonTemplateExpander();
        if (node.Button != null)
        {
            DomNode[] buttonChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            return buttonExpander.ExpandButton(node, buttonChildren);
        }

        InputTemplateExpander inputExpander = new InputTemplateExpander();
        ChoiceTemplateExpander choiceExpander = new ChoiceTemplateExpander();
        if (node.Input != null)
        {
            if (choiceExpander.IsChoiceControlName(node.Name))
            {
                DomNode choiceControl = choiceExpander.ExpandChoiceControl(node);
                return new InputOptionExpander().ApplyChoiceOptions(node, node.Input, choiceControl);
            }

            return inputExpander.ExpandInput(node);
        }

        CollectionTemplateExpander collectionExpander = new CollectionTemplateExpander();
        if (node.Collection != null)
        {
            return ExpandCollectionControl(node, projectedSlots, collectionExpander);
        }

        TableTemplateExpander tableExpander = new TableTemplateExpander();
        if (node.Table != null)
        {
            return ExpandTableControl(node, projectedSlots, tableExpander);
        }

        TreeTemplateExpander treeExpander = new TreeTemplateExpander();
        if (node.Tree != null)
        {
            return ExpandTreeControl(node, projectedSlots, treeExpander);
        }

        MenuTemplateExpander menuExpander = new MenuTemplateExpander();
        if (node.Menu != null)
        {
            return menuExpander.ExpandMenu(node);
        }

        TabsTemplateExpander tabsExpander = new TabsTemplateExpander();
        if (node.Tabs != null)
        {
            return ExpandTabsControl(node, projectedSlots, tabsExpander);
        }

        NavigationTemplateExpander navigationExpander = new NavigationTemplateExpander();
        if (node.Toolbar != null)
        {
            return navigationExpander.ExpandToolbar(node);
        }

        if (node.Breadcrumb != null)
        {
            return navigationExpander.ExpandNavigation(node);
        }

        if (node.Pagination != null)
        {
            return navigationExpander.ExpandPagination(node);
        }

        OverlayTemplateExpander overlayExpander = new OverlayTemplateExpander();
        if (node.Dialog != null)
        {
            return ExpandDialogControl(node, projectedSlots, overlayExpander);
        }

        if (node.Popover != null)
        {
            return ExpandPopoverControl(node, projectedSlots, overlayExpander);
        }

        if (node.Tooltip != null)
        {
            return overlayExpander.ExpandTooltip(node, overlayExpander.PrepareTooltip(node));
        }

        MediaTemplateExpander mediaExpander = new MediaTemplateExpander();
        if (node.Media != null)
        {
            DomNode[] mediaChildren = Array.Empty<DomNode>();
            if (mediaExpander.HasMediaCapability(node.Media, ResourceCapabilities))
            {
                mediaChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            }

            DomNode[] fallbackChildren = ExpandNodesWithProjection(node.Media.FallbackContent, projectedSlots);
            return mediaExpander.ExpandMedia(node, mediaChildren, fallbackChildren, ResourceCapabilities);
        }

        FeedbackTemplateExpander feedbackExpander = new FeedbackTemplateExpander();
        if (node.Toast != null)
        {
            DomNode[] toastChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            return feedbackExpander.ExpandToastOverlay(node, toastChildren);
        }

        if (node.Feedback != null)
        {
            DomNode[] feedbackChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            return feedbackExpander.ExpandFeedback(node, feedbackChildren);
        }

        ContentTemplateExpander contentExpander = new ContentTemplateExpander();
        if (node.TextContent != null)
        {
            DomNode[] contentChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            return contentExpander.ExpandTextContent(node, contentChildren);
        }

        if (node.Announcements != null && node.Announcements.Length > 0)
        {
            return feedbackExpander.ExpandLiveAnnouncements(node);
        }

        if (buttonExpander.IsButtonControlName(node.Name))
        {
            throw CreateRenderException(node, "A button control requires a button description.");
        }

        if (inputExpander.IsInputControlName(node.Name))
        {
            throw CreateRenderException(node, "An input control requires an input description.");
        }

        if (choiceExpander.IsChoiceControlName(node.Name))
        {
            throw CreateRenderException(node, "A choice control requires an input description.");
        }

        if (collectionExpander.IsCollectionControlName(node.Name))
        {
            throw CreateRenderException(node, "A list control requires a collection description.");
        }

        if (tableExpander.IsTableControlName(node.Name))
        {
            throw CreateRenderException(node, "A table control requires a table description.");
        }

        if (treeExpander.IsTreeControlName(node.Name))
        {
            throw CreateRenderException(node, "A tree control requires a tree description.");
        }

        if (menuExpander.IsMenuControlName(node.Name))
        {
            throw CreateRenderException(node, "A menu control requires a menu description.");
        }

        if (tabsExpander.IsTabsControlName(node.Name))
        {
            throw CreateRenderException(node, "A tabs control requires a tabs description.");
        }

        if (navigationExpander.IsToolbarControlName(node.Name))
        {
            throw CreateRenderException(node, "A toolbar control requires a toolbar description.");
        }

        if (navigationExpander.IsNavigationControlName(node.Name))
        {
            throw CreateRenderException(node, "A navigation control requires a breadcrumb description.");
        }

        if (navigationExpander.IsPaginationControlName(node.Name))
        {
            throw CreateRenderException(node, "A pagination control requires a pagination description.");
        }

        if (overlayExpander.IsDialogControlName(node.Name))
        {
            throw CreateRenderException(node, "A dialog control requires a dialog description.");
        }

        if (overlayExpander.IsPopoverControlName(node.Name))
        {
            throw CreateRenderException(node, "A popover control requires a popover description.");
        }

        if (overlayExpander.IsTooltipControlName(node.Name))
        {
            throw CreateRenderException(node, "A tooltip control requires a tooltip description.");
        }

        if (mediaExpander.IsMediaControlName(node.Name))
        {
            throw CreateRenderException(node, "A media control requires a media description.");
        }

        if (feedbackExpander.IsFeedbackControlName(node.Name))
        {
            DomNode[] feedbackChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            return feedbackExpander.ExpandFeedback(node, feedbackChildren);
        }

        if (contentExpander.IsTextControlName(node.Name))
        {
            DomNode[] contentChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
            return contentExpander.ExpandTextContent(node, contentChildren);
        }

        ControlTemplateExpander expander = new ControlTemplateExpander();
        DomNode[] logicalChildren = Array.Empty<DomNode>();
        if (!expander.IgnoresLogicalChildren(node.Name))
        {
            logicalChildren = ExpandNodesWithProjection(node.Children, projectedSlots);
        }

        DomNode[] disclosureChildren = Array.Empty<DomNode>();
        if (expander.UsesDisclosureContent(node.Name))
        {
            if (node.Disclosure == null)
            {
                throw CreateRenderException(node, "A disclosure control requires a disclosure description.");
            }

            disclosureChildren = ExpandNodesWithProjection(node.Disclosure.Content, projectedSlots);
        }

        return expander.ExpandControl(node, logicalChildren, disclosureChildren);
    }

    internal DomNode ExpandCollectionControl(UiNode node, UiSlot[]? projectedSlots, CollectionTemplateExpander expander)
    {
        UiCollection? suppliedCollection = node.Collection;
        if (suppliedCollection == null)
        {
            throw CreateRenderException(node, "A list control requires a collection description.");
        }

        UiCollection collection = suppliedCollection;
        UiCollectionItem[]? items = collection.Items;
        int itemCount = 0;
        if (items != null)
        {
            itemCount = items.Length;
        }

        DomNode?[] itemContents = new DomNode?[itemCount];
        if (items != null && !collection.HasError)
        {
            int index = 0;
            while (index < itemCount)
            {
                UiCollectionItem? item = items[index];
                if (item != null && item.Content != null)
                {
                    itemContents[index] = ExpandNodeWithProjection(item.Content, projectedSlots);
                }

                index++;
            }
        }

        DomNode? emptyContent = null;
        DomNode? loadingContent = null;
        DomNode? errorContent = null;
        if (collection.HasError)
        {
            if (collection.ErrorContent != null)
            {
                errorContent = ExpandNodeWithProjection(collection.ErrorContent, projectedSlots);
            }
        }
        else if (collection.IsLoading)
        {
            if (collection.LoadingContent != null)
            {
                loadingContent = ExpandNodeWithProjection(collection.LoadingContent, projectedSlots);
            }
        }
        else if (itemCount == 0 && collection.EmptyContent != null)
        {
            emptyContent = ExpandNodeWithProjection(collection.EmptyContent, projectedSlots);
        }

        return expander.ExpandCollection(node, itemContents, emptyContent, loadingContent, errorContent);
    }

    internal DomNode ExpandTableControl(UiNode node, UiSlot[]? projectedSlots, TableTemplateExpander expander)
    {
        UiTable? suppliedTable = node.Table;
        if (suppliedTable == null)
        {
            throw CreateRenderException(node, "A table control requires a table description.");
        }

        UiTable table = suppliedTable;
        UiTableRow[]? rows = table.Rows;
        int rowCount = 0;
        if (rows != null)
        {
            rowCount = rows.Length;
        }

        DomNode?[][] cellContents = new DomNode?[rowCount][];
        DomNode?[] detailContents = new DomNode?[rowCount];
        int rowIndex = 0;
        while (rows != null && rowIndex < rowCount)
        {
            UiTableRow? row = rows[rowIndex];
            UiTableCell[]? cells = null;
            int cellCount = 0;
            if (row != null && row.Cells != null)
            {
                cells = row.Cells;
                cellCount = cells.Length;
            }

            cellContents[rowIndex] = new DomNode?[cellCount];
            int cellIndex = 0;
            while (cells != null && cellIndex < cellCount)
            {
                UiTableCell? cell = cells[cellIndex];
                if (cell != null && cell.Content != null)
                {
                    cellContents[rowIndex][cellIndex] = ExpandNodeWithProjection(cell.Content, projectedSlots);
                }

                cellIndex++;
            }

            if (row != null && row.DetailContent != null)
            {
                detailContents[rowIndex] = ExpandNodeWithProjection(row.DetailContent, projectedSlots);
            }

            rowIndex++;
        }

        return expander.ExpandTable(node, cellContents, detailContents);
    }

    internal DomNode ExpandTreeControl(UiNode node, UiSlot[]? projectedSlots, TreeTemplateExpander expander)
    {
        TreeFlatItem[] flattened = expander.PrepareTree(node);
        DomNode?[] contents = new DomNode?[flattened.Length];
        int index = 0;
        while (index < flattened.Length)
        {
            UiTreeItem item = flattened[index].Item;
            if (flattened[index].IsVisible && item.Content != null)
            {
                contents[index] = ExpandNodeWithProjection(item.Content, projectedSlots);
            }

            index++;
        }

        return expander.ExpandTree(node, flattened, contents);
    }

    internal DomNode ExpandTabsControl(UiNode node, UiSlot[]? projectedSlots, TabsTemplateExpander expander)
    {
        int selectedIndex = expander.PrepareTabs(node);
        UiTabs? suppliedTabs = node.Tabs;
        if (suppliedTabs == null)
        {
            throw CreateRenderException(node, "A tabs control requires a tabs description.");
        }

        UiTabs tabs = suppliedTabs;
        DomNode?[] panels = new DomNode?[tabs.Tabs.Length];
        int index = 0;
        while (index < tabs.Tabs.Length)
        {
            UiTab tab = tabs.Tabs[index];
            if (tab.Panel != null && (!tab.IsLazy || index == selectedIndex))
            {
                panels[index] = ExpandNodeWithProjection(tab.Panel, projectedSlots);
            }

            index++;
        }

        return expander.ExpandTabs(node, panels, selectedIndex);
    }

    internal DomNode ExpandDialogControl(UiNode node, UiSlot[]? projectedSlots, OverlayTemplateExpander expander)
    {
        UiDialog dialog = expander.PrepareDialog(node);
        DomNode[] content = ExpandNodesWithProjection(dialog.Content, projectedSlots);
        DomNode[] actions = expander.ExpandDialogActions(node, dialog);
        return expander.ExpandDialog(node, dialog, content, actions);
    }

    internal DomNode ExpandPopoverControl(UiNode node, UiSlot[]? projectedSlots, OverlayTemplateExpander expander)
    {
        UiPopover popover = expander.PreparePopover(node);
        DomNode[] content = ExpandNodesWithProjection(popover.Content, projectedSlots);
        return expander.ExpandPopover(node, popover, content);
    }

    internal DomNode ExpandLayout(UiNode node, UiSlot[]? projectedSlots)
    {
        UiLayout? layout = node.Layout;
        if (layout == null)
        {
            throw CreateRenderException(node, "A layout node requires a layout description.");
        }

        DomNode[] children = ExpandNodesWithProjection(layout.Children, projectedSlots);
        LayoutExpander expander = new LayoutExpander();
        return expander.ExpandLayout(node, children);
    }

    internal DomNode ExpandConditional(UiNode node, UiSlot[]? projectedSlots)
    {
        UiConditional? conditional = node.Conditional;
        if (conditional == null)
        {
            throw CreateRenderException(node, "A conditional node requires a conditional description.");
        }

        UiNode[]? selected = conditional.WhenFalse;
        if (conditional.Condition)
        {
            selected = conditional.WhenTrue;
        }

        return CreateFragment(node, ExpandNodesWithProjection(selected, projectedSlots));
    }

    internal DomNode ExpandRepeat(UiNode node, UiSlot[]? projectedSlots)
    {
        UiRepeat? repeat = node.Repeat;
        if (repeat == null)
        {
            throw CreateRenderException(node, "A repeated node requires a repeat description.");
        }

        UiCollectionItem[]? items = repeat.Items;
        if (items == null)
        {
            return CreateFragment(node, Array.Empty<DomNode>());
        }

        DomNode[] children = new DomNode[items.Length];
        Dictionary<string, string> keys = new Dictionary<string, string>(StringComparer.Ordinal);
        int index = 0;
        while (index < items.Length)
        {
            UiCollectionItem? item = items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                throw CreateRenderException(node, "Repeated items require non-empty keys.");
            }

            if (keys.ContainsKey(item.Key))
            {
                throw CreateRenderException(node, "Repeated item keys must be unique.");
            }

            keys.Add(item.Key, item.Key);
            DomNode[] itemChildren = Array.Empty<DomNode>();
            if (item.Content != null)
            {
                itemChildren = new DomNode[] { ExpandNodeWithProjection(item.Content, projectedSlots) };
            }

            children[index] = new DomNode
            {
                Kind = DomNodeKind.Fragment,
                Key = item.Key,
                Id = CreateRepeatItemId(node, item.Key),
                Children = itemChildren,
                Version = GetNodeVersion(node)
            };
            index++;
        }

        return CreateFragment(node, children);
    }

    internal DomNode ExpandSlot(UiNode node, UiSlot[]? projectedSlots)
    {
        UiNode[]? content = node.Children;
        UiSlot? selected = FindSlot(projectedSlots, node.Name);
        if (selected != null)
        {
            if (selected.Content != null && selected.Content.Length > 0)
            {
                content = selected.Content;
            }
            else
            {
                content = selected.FallbackContent;
            }
        }

        return CreateFragment(node, ExpandNodesWithProjection(content, projectedSlots));
    }

    internal UiSlot? FindSlot(UiSlot[]? slots, string name)
    {
        if (slots == null)
        {
            return null;
        }

        int index = 0;
        while (index < slots.Length)
        {
            UiSlot? slot = slots[index];
            if (slot != null && slot.Name == name)
            {
                return slot;
            }

            index++;
        }

        return null;
    }

    internal DomNode ExpandPortal(UiNode node, UiSlot[]? projectedSlots)
    {
        UiPortal? portal = node.Portal;
        if (portal == null || string.IsNullOrWhiteSpace(portal.Key) || string.IsNullOrWhiteSpace(portal.TargetRegion))
        {
            throw CreateRenderException(node, "A portal node requires a key and target region.");
        }

        DomNode[] content = Array.Empty<DomNode>();
        if (portal.IsActive)
        {
            content = ExpandNodesWithProjection(portal.Content, projectedSlots);
        }

        if (!PortalCollectionEnabled)
        {
            return new DomNode
            {
                Kind = DomNodeKind.Fragment,
                Key = portal.Key,
                Id = GetNodeId(node),
                Children = content,
                Version = GetNodeVersion(node)
            };
        }

        if (!PortalRegions.ContainsKey(portal.TargetRegion))
        {
            throw CreateRenderException(node, "The named portal target region does not exist.");
        }

        DomNode relocated = new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = portal.Key,
            Id = CreatePortalId(node, portal),
            Children = content,
            Version = GetNodeVersion(node)
        };
        PortalRegions[portal.TargetRegion].Add(relocated);
        return CreateFragment(node, Array.Empty<DomNode>());
    }

    internal DomNode[] ExpandNodes(UiNode[]? nodes)
    {
        return ExpandNodesWithProjection(nodes, Array.Empty<UiSlot>());
    }

    internal DomNode[] ExpandNodesWithProjection(UiNode[]? nodes, UiSlot[]? projectedSlots)
    {
        if (nodes == null)
        {
            return Array.Empty<DomNode>();
        }

        DomNode[] result = new DomNode[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            UiNode? node = nodes[index];
            if (node == null)
            {
                result[index] = new DomNode { Kind = DomNodeKind.Fragment };
            }
            else
            {
                result[index] = ExpandNodeWithProjection(node, projectedSlots);
            }

            index++;
        }

        return result;
    }

    internal DomNode CreateFragment(UiNode node, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = GetNodeKey(node.Identity),
            Id = GetNodeId(node),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal void BeginPortalCollection(UiView[]? overlays)
    {
        PortalRegions.Clear();
        PortalCollectionEnabled = true;
        if (overlays == null)
        {
            return;
        }

        int index = 0;
        while (index < overlays.Length)
        {
            UiView? overlay = overlays[index];
            if (overlay != null && !PortalRegions.ContainsKey(overlay.Key))
            {
                PortalRegions.Add(overlay.Key, new List<DomNode>());
            }

            index++;
        }
    }

    internal DomNode[] TakePortalContent(string region)
    {
        if (!PortalRegions.ContainsKey(region))
        {
            return Array.Empty<DomNode>();
        }

        List<DomNode> content = PortalRegions[region];
        DomNode[] result = content.ToArray();
        content.Clear();
        return result;
    }

    internal void EndPortalCollection()
    {
        PortalCollectionEnabled = false;
        PortalRegions.Clear();
    }

    internal void AppendChildren(DomNode parent, DomNode[] additions)
    {
        if (additions.Length == 0)
        {
            return;
        }

        DomNode[] combined = new DomNode[parent.Children.Length + additions.Length];
        Array.Copy(parent.Children, combined, parent.Children.Length);
        Array.Copy(additions, 0, combined, parent.Children.Length, additions.Length);
        parent.Children = combined;
    }

    internal string CreateRepeatItemId(UiNode node, string key)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), "repeat-item", key });
        return "ui-repeat-item-" + token.Substring(0, 20);
    }

    internal string CreatePortalId(UiNode node, UiPortal portal)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), "portal", portal.TargetRegion, portal.Key });
        return "ui-portal-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNodeIdentity? identity)
    {
        if (identity == null)
        {
            return string.Empty;
        }

        if (identity.AuthorKey.Length > 0)
        {
            return identity.AuthorKey;
        }

        return identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
