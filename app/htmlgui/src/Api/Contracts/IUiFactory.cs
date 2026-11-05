namespace htmlgui;

/// <summary>Creates portable UI descriptions with validated inputs and safe defaults.</summary>
public interface IUiFactory
{
    /// <summary>Creates an application with a deterministic root identity.</summary>
    UiApplication CreateApplication(string rootSeed, string key);
    /// <summary>Creates a window with a deterministic identity beneath an application.</summary>
    UiWindow CreateWindow(string rootSeed, string parentId, string key);
    /// <summary>Creates a page with a deterministic identity beneath a window.</summary>
    UiPage CreatePage(string rootSeed, string parentId, string key);
    /// <summary>Creates a view with a deterministic identity beneath a page or view.</summary>
    UiView CreateView(string rootSeed, string parentId, string key);
    /// <summary>Creates a low-level element node with deterministic sibling identity.</summary>
    UiNode CreateElementNode(string rootSeed, string parentId, string key, string? namespaceUri, string name);
    /// <summary>Creates a low-level text node with deterministic sibling identity.</summary>
    UiNode CreateTextNode(string rootSeed, string parentId, string key, string text);
    /// <summary>Creates a low-level comment node with deterministic sibling identity.</summary>
    UiNode CreateCommentNode(string rootSeed, string parentId, string key, string text);
    /// <summary>Creates a layout description.</summary>
    UiLayout CreateLayout(UiLayoutKind kind);
    /// <summary>Creates semantic text content.</summary>
    UiText CreateText(UiTextKind kind, string text);
    /// <summary>Creates a button.</summary>
    UiButton CreateButton(UiButtonKind kind, string label);
    /// <summary>Creates an input control.</summary>
    UiInput CreateInput(UiInputKind kind, string name);
    /// <summary>Creates a selectable choice.</summary>
    UiChoice CreateChoice(string key, string label);
    /// <summary>Creates safe media content.</summary>
    UiMedia CreateMedia(string kind, string source);
    /// <summary>Creates feedback content.</summary>
    UiFeedback CreateFeedback(string kind, string message);
    /// <summary>Creates a disclosure control.</summary>
    UiDisclosure CreateDisclosure(string key, string summary);
    /// <summary>Creates a collection control.</summary>
    UiCollection CreateCollection(string key);
    /// <summary>Creates a table control.</summary>
    UiTable CreateTable(string key);
    /// <summary>Creates a tree control.</summary>
    UiTree CreateTree(string key);
    /// <summary>Creates a menu.</summary>
    UiMenu CreateMenu(string key);
    /// <summary>Creates a menu item.</summary>
    UiMenuItem CreateMenuItem(UiMenuItemKind kind, string key, string label);
    /// <summary>Creates a tab list.</summary>
    UiTabs CreateTabs(string key);
    /// <summary>Creates a tab.</summary>
    UiTab CreateTab(string key, string label);
    /// <summary>Creates a toolbar.</summary>
    UiToolbar CreateToolbar(string key);
    /// <summary>Creates a toolbar item.</summary>
    UiTool CreateTool(string key, string kind, string label);
    /// <summary>Creates breadcrumb navigation.</summary>
    UiBreadcrumb CreateBreadcrumb(string key);
    /// <summary>Creates pagination controls.</summary>
    UiPagination CreatePagination(string key);
    /// <summary>Creates a dialog.</summary>
    UiDialog CreateDialog(UiDialogKind kind, string key, string title);
    /// <summary>Creates an anchored popover.</summary>
    UiPopover CreatePopover(string key, string targetId);
    /// <summary>Creates an anchored tooltip.</summary>
    UiTooltip CreateTooltip(string key, string targetId, string text);
    /// <summary>Creates a transient notification.</summary>
    UiToast CreateToast(string key, string message);
    /// <summary>Creates a form.</summary>
    UiForm CreateForm(string key);
    /// <summary>Creates a form field.</summary>
    UiField CreateField(string key, string name, string label);
    /// <summary>Creates a validation rule.</summary>
    UiValidationRule CreateValidationRule(string key, string kind, string message);
    /// <summary>Creates a binding description.</summary>
    UiBinding CreateBinding(string id, string stateId, string path, string targetNodeId, string target, UiBindingMode mode, UiBindingTrigger trigger);
    /// <summary>Creates a style description.</summary>
    UiStyle CreateStyle(string key);
    /// <summary>Creates a route description.</summary>
    UiRoute CreateRoute(string id, string template);
    /// <summary>Creates bridge options for an expected origin.</summary>
    UiBridgeOptions CreateBridgeOptions(string origin);
}
