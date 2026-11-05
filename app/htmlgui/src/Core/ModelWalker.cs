using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ModelWalker
{
    internal DiagnosticSink Diagnostics;
    internal HashSet<object> ActiveObjects;
    internal HashSet<object> VisitedObjects;
    internal Dictionary<string, string> IdentityLocations;
    internal List<UiNode> Nodes;
    internal List<string> NodeLocations;
    internal int FormDepth;

    internal ModelWalker(int maximumDiagnosticCount)
    {
        Diagnostics = new DiagnosticSink(maximumDiagnosticCount);
        ActiveObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        VisitedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        IdentityLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        Nodes = new List<UiNode>();
        NodeLocations = new List<string>();
        FormDepth = 0;
    }

    internal DiagnosticEntry[] ValidateApplication(UiApplication? application)
    {
        Diagnostics.Clear();
        ActiveObjects.Clear();
        VisitedObjects.Clear();
        IdentityLocations.Clear();
        Nodes.Clear();
        NodeLocations.Clear();
        FormDepth = 0;
        if (application == null)
        {
            AddError("model.null-root", "The application cannot be null.", "/");
            return Diagnostics.CreateSnapshot();
        }

        WalkApplication(application, "/application");
        return Diagnostics.CreateSnapshot();
    }

    internal void WalkApplication(UiApplication application, string location)
    {
        RegisterIdentity(application.Id, location + "/id");
        if (!EnterObject(application, location))
        {
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        UiWindow[]? windows = application.Windows;
        if (windows == null)
        {
            AddError("model.null-collection", "The window collection cannot be null.", location + "/windows");
        }
        else
        {
            int index = 0;
            while (index < windows.Length)
            {
                UiWindow? window = windows[index];
                string childLocation = location + "/windows/" + index;
                if (window == null)
                {
                    AddError("model.null-child", "A window cannot be null.", childLocation);
                }
                else
                {
                    RegisterKey(keys, window.Key, childLocation + "/key", "window");
                    WalkWindow(window, childLocation);
                }

                index++;
            }
        }

        ExitObject(application);
    }

    internal void WalkWindow(UiWindow window, string location)
    {
        RegisterIdentity(window.Id, location + "/id");
        if (!EnterObject(window, location))
        {
            return;
        }

        Dictionary<string, string> pageKeys = CreateKeyMap();
        UiPage[]? pages = window.Pages;
        if (pages == null)
        {
            AddError("model.null-collection", "The page collection cannot be null.", location + "/pages");
        }
        else
        {
            int pageIndex = 0;
            while (pageIndex < pages.Length)
            {
                UiPage? page = pages[pageIndex];
                string pageLocation = location + "/pages/" + pageIndex;
                if (page == null)
                {
                    AddError("model.null-child", "A page cannot be null.", pageLocation);
                }
                else
                {
                    RegisterKey(pageKeys, page.Key, pageLocation + "/key", "page");
                    WalkPage(page, pageLocation);
                }

                pageIndex++;
            }
        }

        Dictionary<string, string> overlayKeys = CreateKeyMap();
        UiView[]? overlays = window.Overlays;
        if (overlays == null)
        {
            AddError("model.null-collection", "The overlay collection cannot be null.", location + "/overlays");
        }
        else
        {
            int overlayIndex = 0;
            while (overlayIndex < overlays.Length)
            {
                UiView? overlay = overlays[overlayIndex];
                string overlayLocation = location + "/overlays/" + overlayIndex;
                if (overlay == null)
                {
                    AddError("model.null-child", "An overlay view cannot be null.", overlayLocation);
                }
                else
                {
                    RegisterKey(overlayKeys, overlay.Key, overlayLocation + "/key", "overlay view");
                    WalkView(overlay, overlayLocation);
                }

                overlayIndex++;
            }
        }

        ExitObject(window);
    }

    internal void WalkPage(UiPage page, string location)
    {
        RegisterIdentity(page.Id, location + "/id");
        if (!EnterObject(page, location))
        {
            return;
        }

        WalkOptionalView(page.ShellView, location + "/shellView");
        WalkOptionalView(page.ContentView, location + "/contentView");
        WalkOptionalView(page.NotFoundView, location + "/notFoundView");
        WalkOptionalView(page.ErrorView, location + "/errorView");
        WalkOptionalView(page.LoadingView, location + "/loadingView");
        ExitObject(page);
    }

    internal void WalkOptionalView(UiView? view, string location)
    {
        if (view != null)
        {
            WalkView(view, location);
        }
    }

    internal void WalkView(UiView view, string location)
    {
        RegisterIdentity(view.Id, location + "/id");
        if (!EnterObject(view, location))
        {
            return;
        }

        if (view.ContentRoot != null)
        {
            WalkNode(view.ContentRoot, view.Id, location + "/contentRoot");
        }

        if (view.ComponentContext != null)
        {
            WalkSlots(view.ComponentContext.Slots, view.Id, location + "/componentContext/slots");
        }

        if (view.RenderRequest != null && view.RenderRequest.Context != null)
        {
            WalkSlots(view.RenderRequest.Context.Slots, view.Id, location + "/renderRequest/context/slots");
        }

        if (view.ErrorBoundaryState != null)
        {
            WalkOptionalNode(view.ErrorBoundaryState.Fallback, view.Id, location + "/errorBoundaryState/fallback");
        }

        WalkOptionalView(view.ErrorBoundaryView, location + "/errorBoundaryView");
        ExitObject(view);
    }

    internal void WalkNode(UiNode? node, string expectedParentId, string location)
    {
        if (node == null)
        {
            AddError("model.null-child", "A child node cannot be null.", location);
            return;
        }

        UiNodeIdentity? identity = node.Identity;
        string stableId = string.Empty;
        if (identity == null)
        {
            AddError("model.null-child", "The node identity cannot be null.", location + "/identity");
        }
        else
        {
            stableId = identity.StableId;
            RegisterIdentity(stableId, location + "/identity/stableId");
        }

        if (identity != null && identity.ParentStableId != expectedParentId)
        {
            AddError("model.parent-mismatch", "The node parent identity does not match its containing parent.", location + "/identity/parentStableId");
        }

        if (!EnterObject(node, location))
        {
            return;
        }

        Nodes.Add(node);
        NodeLocations.Add(location);

        if (!IsSupportedNodeKind(node.Kind))
        {
            AddError("model.invalid-node-kind", "The UI node kind is not supported.", location + "/kind");
        }

        bool hasForm = node.Form != null;
        if (hasForm)
        {
            if (FormDepth > 0)
            {
                AddError("model.nested-form", "A form cannot be nested inside another form.", location + "/form");
            }

            FormDepth++;
        }

        bool hasNestedContent = HasNestedContent(node);
        if (hasNestedContent && IsLeafKind(node.Kind))
        {
            AddError("model.illegal-child", "A leaf node cannot contain child content.", location);
        }

        WalkNodeArray(node.Children, stableId, location + "/children");
        WalkSlots(node.Slots, stableId, location + "/slots");
        WalkConditional(node.Conditional, stableId, location + "/conditional");
        WalkRepeat(node.Repeat, stableId, location + "/repeat");
        WalkPortal(node.Portal, stableId, location + "/portal");
        WalkLayout(node.Layout, stableId, location + "/layout");
        WalkMedia(node.Media, stableId, location + "/media");
        WalkDisclosure(node.Disclosure, stableId, location + "/disclosure");
        WalkCollection(node.Collection, stableId, location + "/collection");
        WalkTable(node.Table, stableId, location + "/table");
        WalkTree(node.Tree, stableId, location + "/tree");
        WalkMenu(node.Menu, location + "/menu");
        WalkButton(node.Button, location + "/button");
        if (node.Input != null)
        {
            WalkChoices(node.Input.Choices, location + "/input/choices");
        }

        WalkTabs(node.Tabs, stableId, location + "/tabs");
        WalkToolbar(node.Toolbar, location + "/toolbar");
        if (node.Breadcrumb != null)
        {
            WalkChoices(node.Breadcrumb.Items, location + "/breadcrumb/items");
        }

        WalkDialog(node.Dialog, stableId, location + "/dialog");
        WalkPopover(node.Popover, stableId, location + "/popover");
        if (node.Toast != null)
        {
            WalkButton(node.Toast.Action, location + "/toast/action");
        }

        WalkForm(node.Form, location + "/form");
        if (node.Virtualization != null)
        {
            WalkRangeResult(node.Virtualization.Result, stableId, location + "/virtualization/result");
        }

        if (hasForm)
        {
            FormDepth--;
        }

        ExitObject(node);
    }

    internal void WalkNodeArray(UiNode[]? nodes, string expectedParentId, string location)
    {
        if (nodes == null)
        {
            AddError("model.null-collection", "A node collection cannot be null.", location);
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        int index = 0;
        while (index < nodes.Length)
        {
            UiNode? node = nodes[index];
            string childLocation = location + "/" + index;
            if (node != null && node.Identity != null)
            {
                string? key = node.Identity.AuthorKey;
                if (key == null || key.Length == 0)
                {
                    key = node.Identity.SiblingIdentity;
                }

                RegisterKey(keys, key, childLocation + "/identity/authorKey", "sibling node");
            }

            WalkNode(node, expectedParentId, childLocation);
            index++;
        }
    }

    internal void WalkSlots(UiSlot[]? slots, string expectedParentId, string location)
    {
        if (slots == null)
        {
            AddError("model.null-collection", "The slot collection cannot be null.", location);
            return;
        }

        Dictionary<string, string> names = CreateKeyMap();
        int index = 0;
        while (index < slots.Length)
        {
            UiSlot? slot = slots[index];
            string slotLocation = location + "/" + index;
            if (slot == null)
            {
                AddError("model.null-child", "A slot cannot be null.", slotLocation);
            }
            else
            {
                RegisterKey(names, slot.Name, slotLocation + "/name", "slot");
                WalkNodeArray(slot.Content, expectedParentId, slotLocation + "/content");
                WalkNodeArray(slot.FallbackContent, expectedParentId, slotLocation + "/fallbackContent");
            }

            index++;
        }
    }

    internal void WalkPortal(UiPortal? portal, string expectedParentId, string location)
    {
        if (portal != null)
        {
            WalkNodeArray(portal.Content, expectedParentId, location + "/content");
        }
    }

    internal void WalkConditional(UiConditional? conditional, string expectedParentId, string location)
    {
        if (conditional != null)
        {
            WalkNodeArray(conditional.WhenTrue, expectedParentId, location + "/whenTrue");
            WalkNodeArray(conditional.WhenFalse, expectedParentId, location + "/whenFalse");
        }
    }

    internal void WalkRepeat(UiRepeat? repeat, string expectedParentId, string location)
    {
        if (repeat == null)
        {
            return;
        }

        UiCollectionItem[]? items = repeat.Items;
        if (items == null)
        {
            AddError("model.null-collection", "The repeated items cannot be null.", location + "/items");
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        int index = 0;
        while (index < items.Length)
        {
            UiCollectionItem? item = items[index];
            string itemLocation = location + "/items/" + index;
            if (item == null)
            {
                AddError("model.null-child", "A repeated item cannot be null.", itemLocation);
            }
            else
            {
                RegisterKey(keys, item.Key, itemLocation + "/key", "repeated item");
                WalkOptionalNode(item.Content, expectedParentId, itemLocation + "/content");
            }

            index++;
        }
    }

    internal void WalkLayout(UiLayout? layout, string expectedParentId, string location)
    {
        if (layout != null)
        {
            WalkNodeArray(layout.Children, expectedParentId, location + "/children");
        }
    }

    internal void WalkMedia(UiMedia? media, string expectedParentId, string location)
    {
        if (media != null)
        {
            WalkNodeArray(media.FallbackContent, expectedParentId, location + "/fallbackContent");
        }
    }

    internal void WalkDisclosure(UiDisclosure? disclosure, string expectedParentId, string location)
    {
        if (disclosure != null)
        {
            WalkNodeArray(disclosure.Content, expectedParentId, location + "/content");
        }
    }

    internal void WalkCollection(UiCollection? collection, string expectedParentId, string location)
    {
        if (collection == null)
        {
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        UiCollectionItem[]? items = collection.Items;
        if (items == null)
        {
            AddError("model.null-collection", "The collection items cannot be null.", location + "/items");
        }
        else
        {
            int index = 0;
            while (index < items.Length)
            {
                UiCollectionItem? item = items[index];
                string itemLocation = location + "/items/" + index;
                if (item == null)
                {
                    AddError("model.null-child", "A collection item cannot be null.", itemLocation);
                }
                else
                {
                    RegisterKey(keys, item.Key, itemLocation + "/key", "collection item");
                    if (item.Content != null)
                    {
                        WalkNode(item.Content, expectedParentId, itemLocation + "/content");
                    }
                }

                index++;
            }
        }

        WalkOptionalNode(collection.EmptyContent, expectedParentId, location + "/emptyContent");
        WalkOptionalNode(collection.LoadingContent, expectedParentId, location + "/loadingContent");
        WalkOptionalNode(collection.ErrorContent, expectedParentId, location + "/errorContent");
    }

    internal void WalkOptionalNode(UiNode? node, string expectedParentId, string location)
    {
        if (node != null)
        {
            WalkNode(node, expectedParentId, location);
        }
    }

    internal void WalkRangeResult(UiRangeResult? result, string expectedParentId, string location)
    {
        if (result == null)
        {
            return;
        }

        UiCollectionItem[]? items = result.Items;
        if (items == null)
        {
            AddError("model.null-collection", "The range result items cannot be null.", location + "/items");
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        int index = 0;
        while (index < items.Length)
        {
            UiCollectionItem? item = items[index];
            string itemLocation = location + "/items/" + index;
            if (item == null)
            {
                AddError("model.null-child", "A range result item cannot be null.", itemLocation);
            }
            else
            {
                RegisterKey(keys, item.Key, itemLocation + "/key", "range result item");
                WalkOptionalNode(item.Content, expectedParentId, itemLocation + "/content");
            }

            index++;
        }
    }

    internal void WalkTable(UiTable? table, string expectedParentId, string location)
    {
        if (table == null)
        {
            return;
        }

        Dictionary<string, string> columnKeys = CreateKeyMap();
        UiTableColumn[]? columns = table.Columns;
        if (columns == null)
        {
            AddError("model.null-collection", "The table columns cannot be null.", location + "/columns");
        }
        else
        {
            int columnIndex = 0;
            while (columnIndex < columns.Length)
            {
                UiTableColumn? column = columns[columnIndex];
                string columnLocation = location + "/columns/" + columnIndex;
                if (column == null)
                {
                    AddError("model.null-child", "A table column cannot be null.", columnLocation);
                }
                else
                {
                    RegisterKey(columnKeys, column.Key, columnLocation + "/key", "table column");
                }

                columnIndex++;
            }
        }

        Dictionary<string, string> rowKeys = CreateKeyMap();
        UiTableRow[]? rows = table.Rows;
        if (rows == null)
        {
            AddError("model.null-collection", "The table rows cannot be null.", location + "/rows");
        }
        else
        {
            int rowIndex = 0;
            while (rowIndex < rows.Length)
            {
                UiTableRow? row = rows[rowIndex];
                string rowLocation = location + "/rows/" + rowIndex;
                if (row == null)
                {
                    AddError("model.null-child", "A table row cannot be null.", rowLocation);
                }
                else
                {
                    RegisterKey(rowKeys, row.Key, rowLocation + "/key", "table row");
                    WalkTableCells(row.Cells, expectedParentId, rowLocation + "/cells");
                    WalkOptionalNode(row.DetailContent, expectedParentId, rowLocation + "/detailContent");
                }

                rowIndex++;
            }
        }
    }

    internal void WalkTableCells(UiTableCell[]? cells, string expectedParentId, string location)
    {
        if (cells == null)
        {
            AddError("model.null-collection", "The table cells cannot be null.", location);
            return;
        }

        Dictionary<string, string> columnKeys = CreateKeyMap();
        int index = 0;
        while (index < cells.Length)
        {
            UiTableCell? cell = cells[index];
            string cellLocation = location + "/" + index;
            if (cell == null)
            {
                AddError("model.null-child", "A table cell cannot be null.", cellLocation);
            }
            else
            {
                RegisterKey(columnKeys, cell.ColumnKey, cellLocation + "/columnKey", "table cell column");
                WalkOptionalNode(cell.Content, expectedParentId, cellLocation + "/content");
            }

            index++;
        }
    }

    internal void WalkTree(UiTree? tree, string expectedParentId, string location)
    {
        if (tree != null)
        {
            Dictionary<string, string> keys = CreateKeyMap();
            WalkTreeItems(tree.Items, null, expectedParentId, keys, location + "/items");
        }
    }

    internal void WalkTreeItems(UiTreeItem[]? items, string? expectedParentKey, string expectedNodeParentId, Dictionary<string, string> keys, string location)
    {
        if (items == null)
        {
            AddError("model.null-collection", "The tree items cannot be null.", location);
            return;
        }

        int index = 0;
        while (index < items.Length)
        {
            UiTreeItem? item = items[index];
            string itemLocation = location + "/" + index;
            if (item == null)
            {
                AddError("model.null-child", "A tree item cannot be null.", itemLocation);
            }
            else
            {
                RegisterKey(keys, item.Key, itemLocation + "/key", "tree item");
                if (!EnterObject(item, itemLocation))
                {
                    index++;
                    continue;
                }

                if (item.Relationship == null)
                {
                    AddError("model.null-child", "The tree item relationship cannot be null.", itemLocation + "/relationship");
                }
                else if (item.Relationship.ParentKey != expectedParentKey)
                {
                    AddError("model.parent-mismatch", "The tree item parent key does not match its containing item.", itemLocation + "/relationship/parentKey");
                }

                WalkOptionalNode(item.Content, expectedNodeParentId, itemLocation + "/content");
                WalkTreeItems(item.Children, item.Key, expectedNodeParentId, keys, itemLocation + "/children");
                ExitObject(item);
            }

            index++;
        }
    }

    internal void WalkMenu(UiMenu? menu, string location)
    {
        if (menu == null)
        {
            return;
        }

        if (!EnterObject(menu, location))
        {
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        UiMenuItem[]? items = menu.Items;
        if (items == null)
        {
            AddError("model.null-collection", "The menu items cannot be null.", location + "/items");
        }
        else
        {
            int index = 0;
            while (index < items.Length)
            {
                UiMenuItem? item = items[index];
                string itemLocation = location + "/items/" + index;
                if (item == null)
                {
                    AddError("model.null-child", "A menu item cannot be null.", itemLocation);
                }
                else
                {
                    RegisterKey(keys, item.Key, itemLocation + "/key", "menu item");
                    WalkMenu(item.Submenu, itemLocation + "/submenu");
                }

                index++;
            }
        }

        ExitObject(menu);
    }

    internal void WalkButton(UiButton? button, string location)
    {
        if (button != null)
        {
            WalkMenu(button.Menu, location + "/menu");
        }
    }

    internal void WalkTabs(UiTabs? tabs, string expectedParentId, string location)
    {
        if (tabs == null)
        {
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        UiTab[]? items = tabs.Tabs;
        if (items == null)
        {
            AddError("model.null-collection", "The tabs cannot be null.", location + "/tabs");
            return;
        }

        int index = 0;
        while (index < items.Length)
        {
            UiTab? tab = items[index];
            string tabLocation = location + "/tabs/" + index;
            if (tab == null)
            {
                AddError("model.null-child", "A tab cannot be null.", tabLocation);
            }
            else
            {
                RegisterKey(keys, tab.Key, tabLocation + "/key", "tab");
                WalkOptionalNode(tab.Panel, expectedParentId, tabLocation + "/panel");
            }

            index++;
        }
    }

    internal void WalkDialog(UiDialog? dialog, string expectedParentId, string location)
    {
        if (dialog != null)
        {
            WalkNodeArray(dialog.Content, expectedParentId, location + "/content");
            UiButton[]? actions = dialog.Actions;
            if (actions == null)
            {
                AddError("model.null-collection", "The dialog actions cannot be null.", location + "/actions");
            }
            else
            {
                int index = 0;
                while (index < actions.Length)
                {
                    UiButton? action = actions[index];
                    string actionLocation = location + "/actions/" + index;
                    if (action == null)
                    {
                        AddError("model.null-child", "A dialog action cannot be null.", actionLocation);
                    }
                    else
                    {
                        WalkButton(action, actionLocation);
                    }

                    index++;
                }
            }
        }
    }

    internal void WalkPopover(UiPopover? popover, string expectedParentId, string location)
    {
        if (popover != null)
        {
            WalkNodeArray(popover.Content, expectedParentId, location + "/content");
        }
    }

    internal void WalkForm(UiForm? form, string location)
    {
        if (form == null)
        {
            return;
        }

        Dictionary<string, string> fieldKeys = CreateKeyMap();
        Dictionary<string, string> fieldNames = CreateKeyMap();
        UiField[]? fields = form.Fields;
        if (fields == null)
        {
            AddError("model.null-collection", "The form fields cannot be null.", location + "/fields");
            return;
        }

        int index = 0;
        while (index < fields.Length)
        {
            UiField? field = fields[index];
            string fieldLocation = location + "/fields/" + index;
            if (field == null)
            {
                AddError("model.null-child", "A form field cannot be null.", fieldLocation);
            }
            else
            {
                RegisterKey(fieldKeys, field.Key, fieldLocation + "/key", "form field");
                RegisterKey(fieldNames, field.Name, fieldLocation + "/name", "form field name");
                WalkChoices(field.Choices, fieldLocation + "/choices");
                WalkRules(field.Rules, fieldLocation + "/rules");
                if (field.Input != null)
                {
                    WalkChoices(field.Input.Choices, fieldLocation + "/input/choices");
                }
            }

            index++;
        }
    }

    internal void WalkToolbar(UiToolbar? toolbar, string location)
    {
        if (toolbar == null)
        {
            return;
        }

        UiTool[]? tools = toolbar.Tools;
        if (tools == null)
        {
            AddError("model.null-collection", "The toolbar tools cannot be null.", location + "/tools");
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        int index = 0;
        while (index < tools.Length)
        {
            UiTool? tool = tools[index];
            string toolLocation = location + "/tools/" + index;
            if (tool == null)
            {
                AddError("model.null-child", "A toolbar tool cannot be null.", toolLocation);
            }
            else
            {
                RegisterKey(keys, tool.Key, toolLocation + "/key", "toolbar tool");
                WalkButton(tool.Button, toolLocation + "/button");
                if (tool.Input != null)
                {
                    WalkChoices(tool.Input.Choices, toolLocation + "/input/choices");
                }
            }

            index++;
        }
    }

    internal void WalkChoices(UiChoice[]? choices, string location)
    {
        if (choices == null)
        {
            AddError("model.null-collection", "The choices cannot be null.", location);
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        int index = 0;
        while (index < choices.Length)
        {
            UiChoice? choice = choices[index];
            string choiceLocation = location + "/" + index;
            if (choice == null)
            {
                AddError("model.null-child", "A choice cannot be null.", choiceLocation);
            }
            else
            {
                RegisterKey(keys, choice.Key, choiceLocation + "/key", "choice");
            }

            index++;
        }
    }

    internal void WalkRules(UiValidationRule[]? rules, string location)
    {
        if (rules == null)
        {
            AddError("model.null-collection", "The validation rules cannot be null.", location);
            return;
        }

        Dictionary<string, string> keys = CreateKeyMap();
        int index = 0;
        while (index < rules.Length)
        {
            UiValidationRule? rule = rules[index];
            string ruleLocation = location + "/" + index;
            if (rule == null)
            {
                AddError("model.null-child", "A validation rule cannot be null.", ruleLocation);
            }
            else
            {
                RegisterKey(keys, rule.Key, ruleLocation + "/key", "validation rule");
            }

            index++;
        }
    }

    internal bool HasNestedContent(UiNode node)
    {
        if (node.Children != null && node.Children.Length > 0)
        {
            return true;
        }

        if (node.Slots != null && node.Slots.Length > 0)
        {
            return true;
        }

        if (node.Conditional != null)
        {
            if (node.Conditional.WhenTrue != null && node.Conditional.WhenTrue.Length > 0)
            {
                return true;
            }

            if (node.Conditional.WhenFalse != null && node.Conditional.WhenFalse.Length > 0)
            {
                return true;
            }
        }

        if (node.Repeat != null && node.Repeat.Items != null && node.Repeat.Items.Length > 0)
        {
            return true;
        }

        if (node.Portal != null && node.Portal.Content != null && node.Portal.Content.Length > 0)
        {
            return true;
        }

        if (node.Layout != null && node.Layout.Children != null && node.Layout.Children.Length > 0)
        {
            return true;
        }

        if (node.Media != null && node.Media.FallbackContent != null && node.Media.FallbackContent.Length > 0)
        {
            return true;
        }

        if (node.Disclosure != null && node.Disclosure.Content != null && node.Disclosure.Content.Length > 0)
        {
            return true;
        }

        return false;
    }

    internal bool IsLeafKind(UiNodeKind kind)
    {
        return kind == UiNodeKind.Text || kind == UiNodeKind.Comment || kind == UiNodeKind.RawContent;
    }

    internal bool IsSupportedNodeKind(UiNodeKind kind)
    {
        switch (kind)
        {
            case UiNodeKind.Element:
            case UiNodeKind.Text:
            case UiNodeKind.Comment:
            case UiNodeKind.Fragment:
            case UiNodeKind.Component:
            case UiNodeKind.Control:
            case UiNodeKind.Layout:
            case UiNodeKind.Conditional:
            case UiNodeKind.Repeat:
            case UiNodeKind.Slot:
            case UiNodeKind.Portal:
            case UiNodeKind.RawContent:
                return true;
            default:
                return false;
        }
    }

    internal bool EnterObject(object value, string location)
    {
        if (ActiveObjects.Contains(value))
        {
            AddError("model.cycle", "The model graph contains a cycle.", location);
            return false;
        }

        if (VisitedObjects.Contains(value))
        {
            AddError("model.shared-reference", "A structural model object is used by more than one parent.", location);
            return false;
        }

        ActiveObjects.Add(value);
        VisitedObjects.Add(value);
        return true;
    }

    internal void ExitObject(object value)
    {
        ActiveObjects.Remove(value);
    }

    internal void RegisterIdentity(string? identity, string location)
    {
        if (identity == null || identity.Length == 0)
        {
            AddError("model.missing-id", "A structural model identity is required.", location);
            return;
        }

        if (IdentityLocations.ContainsKey(identity))
        {
            AddError("model.duplicate-id", "The identity is already used at " + IdentityLocations[identity] + ".", location);
            return;
        }

        IdentityLocations.Add(identity, location);
    }

    internal void RegisterKey(Dictionary<string, string> keys, string? key, string location, string kind)
    {
        if (key == null || key.Length == 0)
        {
            AddError("model.missing-key", "A " + kind + " key is required.", location);
            return;
        }

        if (keys.ContainsKey(key))
        {
            AddError("model.duplicate-key", "The " + kind + " key is already used at " + keys[key] + ".", location);
            return;
        }

        keys.Add(key, location);
    }

    internal Dictionary<string, string> CreateKeyMap()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal void AddError(string code, string message, string location)
    {
        Diagnostics.Add(DiagnosticLevel.Error, code, message, location);
    }
}
