namespace htmlgui;

/// <summary>Creates fresh portable UI descriptions with validated required values.</summary>
public class UiFactory : IUiFactory
{
    /// <summary>Initializes a UI factory.</summary>
    public UiFactory()
    {
    }

    /// <summary>Creates an application with a deterministic root identity.</summary>
    public UiApplication CreateApplication(string rootSeed, string key)
    {
        string seed = Guard.ArgumentNotWhiteSpace(rootSeed, "rootSeed");
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        return new UiApplication { Id = StableId.CreateRoot(seed, "application", actualKey), Key = actualKey, RootSeed = seed };
    }

    /// <summary>Creates a window with a deterministic identity beneath an application.</summary>
    public UiWindow CreateWindow(string rootSeed, string parentId, string key)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        return new UiWindow { Id = StableId.CreateChild(rootSeed, parentId, "window", actualKey), Key = actualKey };
    }

    /// <summary>Creates a page with a deterministic identity beneath a window.</summary>
    public UiPage CreatePage(string rootSeed, string parentId, string key)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        return new UiPage { Id = StableId.CreateChild(rootSeed, parentId, "page", actualKey), Key = actualKey };
    }

    /// <summary>Creates a view with a deterministic identity beneath a page or view.</summary>
    public UiView CreateView(string rootSeed, string parentId, string key)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        return new UiView { Id = StableId.CreateChild(rootSeed, parentId, "view", actualKey), Key = actualKey };
    }

    /// <summary>Creates a low-level element node with deterministic sibling identity.</summary>
    public UiNode CreateElementNode(string rootSeed, string parentId, string key, string? namespaceUri, string name)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        string actualName = Guard.ArgumentNotWhiteSpace(name, "name");
        UiNode node = CreateNodeIdentity(rootSeed, parentId, actualKey, "element", UiNodeKind.Element);
        node.NamespaceUri = namespaceUri;
        node.Name = actualName;
        return node;
    }

    /// <summary>Creates a low-level text node with deterministic sibling identity.</summary>
    public UiNode CreateTextNode(string rootSeed, string parentId, string key, string text)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(text, "text");
        UiNode node = CreateNodeIdentity(rootSeed, parentId, actualKey, "text", UiNodeKind.Text);
        node.Text = text;
        return node;
    }

    /// <summary>Creates a low-level comment node with deterministic sibling identity.</summary>
    public UiNode CreateCommentNode(string rootSeed, string parentId, string key, string text)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(text, "text");
        UiNode node = CreateNodeIdentity(rootSeed, parentId, actualKey, "comment", UiNodeKind.Comment);
        node.Text = text;
        return node;
    }

    /// <summary>Creates a layout description.</summary>
    public UiLayout CreateLayout(UiLayoutKind kind)
    {
        Guard.ArgumentInRange((int)kind, (int)UiLayoutKind.Flow, (int)UiLayoutKind.ResponsiveShell, "kind");
        return new UiLayout { Kind = kind };
    }

    /// <summary>Creates semantic text content.</summary>
    public UiText CreateText(UiTextKind kind, string text)
    {
        Guard.ArgumentInRange((int)kind, (int)UiTextKind.Plain, (int)UiTextKind.Localized, "kind");
        Guard.ArgumentNotNull(text, "text");
        return new UiText { Kind = kind, Text = text };
    }

    /// <summary>Creates a button.</summary>
    public UiButton CreateButton(UiButtonKind kind, string label)
    {
        Guard.ArgumentInRange((int)kind, (int)UiButtonKind.Push, (int)UiButtonKind.Command, "kind");
        Guard.ArgumentNotNull(label, "label");
        return new UiButton { Kind = kind, Label = label };
    }

    /// <summary>Creates an input control.</summary>
    public UiInput CreateInput(UiInputKind kind, string name)
    {
        Guard.ArgumentInRange((int)kind, (int)UiInputKind.Text, (int)UiInputKind.ReadOnly, "kind");
        string actualName = Guard.ArgumentNotWhiteSpace(name, "name");
        return new UiInput { Kind = kind, Name = actualName };
    }

    /// <summary>Creates a selectable choice.</summary>
    public UiChoice CreateChoice(string key, string label)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(label, "label");
        return new UiChoice { Key = actualKey, Label = label };
    }

    /// <summary>Creates safe media content.</summary>
    public UiMedia CreateMedia(string kind, string source)
    {
        string actualKind = Guard.ArgumentNotWhiteSpace(kind, "kind");
        string actualSource = Guard.ArgumentNotWhiteSpace(source, "source");
        return new UiMedia { Kind = actualKind, Source = actualSource };
    }

    /// <summary>Creates feedback content.</summary>
    public UiFeedback CreateFeedback(string kind, string message)
    {
        string actualKind = Guard.ArgumentNotWhiteSpace(kind, "kind");
        Guard.ArgumentNotNull(message, "message");
        return new UiFeedback { Kind = actualKind, Message = message };
    }

    /// <summary>Creates a disclosure control.</summary>
    public UiDisclosure CreateDisclosure(string key, string summary)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(summary, "summary");
        return new UiDisclosure { Key = actualKey, Summary = summary };
    }

    /// <summary>Creates a collection control.</summary>
    public UiCollection CreateCollection(string key)
    {
        return new UiCollection { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a table control.</summary>
    public UiTable CreateTable(string key)
    {
        return new UiTable { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a tree control.</summary>
    public UiTree CreateTree(string key)
    {
        return new UiTree { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a menu.</summary>
    public UiMenu CreateMenu(string key)
    {
        return new UiMenu { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a menu item.</summary>
    public UiMenuItem CreateMenuItem(UiMenuItemKind kind, string key, string label)
    {
        Guard.ArgumentInRange((int)kind, (int)UiMenuItemKind.Command, (int)UiMenuItemKind.Separator, "kind");
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(label, "label");
        return new UiMenuItem { Kind = kind, Key = actualKey, Label = label };
    }

    /// <summary>Creates a tab list.</summary>
    public UiTabs CreateTabs(string key)
    {
        return new UiTabs { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a tab.</summary>
    public UiTab CreateTab(string key, string label)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(label, "label");
        return new UiTab { Key = actualKey, Label = label };
    }

    /// <summary>Creates a toolbar.</summary>
    public UiToolbar CreateToolbar(string key)
    {
        return new UiToolbar { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a toolbar item.</summary>
    public UiTool CreateTool(string key, string kind, string label)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        string actualKind = Guard.ArgumentNotWhiteSpace(kind, "kind");
        Guard.ArgumentNotNull(label, "label");
        return new UiTool { Key = actualKey, Kind = actualKind, Label = label };
    }

    /// <summary>Creates breadcrumb navigation.</summary>
    public UiBreadcrumb CreateBreadcrumb(string key)
    {
        return new UiBreadcrumb { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates pagination controls.</summary>
    public UiPagination CreatePagination(string key)
    {
        return new UiPagination { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a dialog.</summary>
    public UiDialog CreateDialog(UiDialogKind kind, string key, string title)
    {
        Guard.ArgumentInRange((int)kind, (int)UiDialogKind.Modal, (int)UiDialogKind.Drawer, "kind");
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(title, "title");
        return new UiDialog { Kind = kind, Key = actualKey, Title = title };
    }

    /// <summary>Creates an anchored popover.</summary>
    public UiPopover CreatePopover(string key, string targetId)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        string actualTargetId = Guard.ArgumentNotWhiteSpace(targetId, "targetId");
        UiPopover popover = new UiPopover { Key = actualKey };
        popover.Anchor.TargetId = actualTargetId;
        return popover;
    }

    /// <summary>Creates an anchored tooltip.</summary>
    public UiTooltip CreateTooltip(string key, string targetId, string text)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        string actualTargetId = Guard.ArgumentNotWhiteSpace(targetId, "targetId");
        Guard.ArgumentNotNull(text, "text");
        UiTooltip tooltip = new UiTooltip { Key = actualKey, Text = text };
        tooltip.Anchor.TargetId = actualTargetId;
        return tooltip;
    }

    /// <summary>Creates a transient notification.</summary>
    public UiToast CreateToast(string key, string message)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        Guard.ArgumentNotNull(message, "message");
        return new UiToast { Key = actualKey, Message = message };
    }

    /// <summary>Creates a form.</summary>
    public UiForm CreateForm(string key)
    {
        return new UiForm { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a form field.</summary>
    public UiField CreateField(string key, string name, string label)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        string actualName = Guard.ArgumentNotWhiteSpace(name, "name");
        Guard.ArgumentNotNull(label, "label");
        return new UiField { Key = actualKey, Name = actualName, Label = label };
    }

    /// <summary>Creates a validation rule.</summary>
    public UiValidationRule CreateValidationRule(string key, string kind, string message)
    {
        string actualKey = Guard.ArgumentNotWhiteSpace(key, "key");
        string actualKind = Guard.ArgumentNotWhiteSpace(kind, "kind");
        Guard.ArgumentNotNull(message, "message");
        return new UiValidationRule { Key = actualKey, Kind = actualKind, Message = message };
    }

    /// <summary>Creates a binding description.</summary>
    public UiBinding CreateBinding(string id, string stateId, string path, string targetNodeId, string target, UiBindingMode mode, UiBindingTrigger trigger)
    {
        Guard.ArgumentInRange((int)mode, (int)UiBindingMode.OneTime, (int)UiBindingMode.OneWayToSource, "mode");
        Guard.ArgumentInRange((int)trigger, (int)UiBindingTrigger.Immediate, (int)UiBindingTrigger.Explicit, "trigger");
        Guard.ArgumentNotNull(path, "path");
        return new UiBinding
        {
            Id = Guard.ArgumentNotWhiteSpace(id, "id"),
            StateId = Guard.ArgumentNotWhiteSpace(stateId, "stateId"),
            Path = path,
            TargetNodeId = Guard.ArgumentNotWhiteSpace(targetNodeId, "targetNodeId"),
            Target = Guard.ArgumentNotWhiteSpace(target, "target"),
            Mode = mode,
            Trigger = trigger
        };
    }

    /// <summary>Creates a style description.</summary>
    public UiStyle CreateStyle(string key)
    {
        return new UiStyle { Key = Guard.ArgumentNotWhiteSpace(key, "key") };
    }

    /// <summary>Creates a route description.</summary>
    public UiRoute CreateRoute(string id, string template)
    {
        return new UiRoute
        {
            Id = Guard.ArgumentNotWhiteSpace(id, "id"),
            Template = Guard.ArgumentNotWhiteSpace(template, "template")
        };
    }

    /// <summary>Creates bridge options for an expected origin.</summary>
    public UiBridgeOptions CreateBridgeOptions(string origin)
    {
        return new UiBridgeOptions { Origin = Guard.ArgumentNotWhiteSpace(origin, "origin") };
    }

    internal UiNode CreateNodeIdentity(string rootSeed, string parentId, string key, string kind, UiNodeKind nodeKind)
    {
        UiNode node = new UiNode { Kind = nodeKind };
        node.Identity.AuthorKey = key;
        node.Identity.ParentStableId = Guard.ArgumentNotWhiteSpace(parentId, "parentId");
        node.Identity.SiblingIdentity = key;
        node.Identity.StableId = StableId.CreateChild(rootSeed, parentId, kind, key);
        return node;
    }
}
