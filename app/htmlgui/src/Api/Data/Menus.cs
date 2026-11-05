using System;

namespace htmlgui;

/// <summary>Describes a menu bar, popup menu, context menu, or menu-button popup.</summary>
public class UiMenu
{
    /// <summary>The stable menu key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional accessible label.</summary>
    public string? Label;
    /// <summary>The menu items.</summary>
    public UiMenuItem[] Items = Array.Empty<UiMenuItem>();
    /// <summary>The menu orientation.</summary>
    public UiOrientation Orientation = UiOrientation.Vertical;
    /// <summary>The optional active item key.</summary>
    public string? ActiveItemKey;
    /// <summary>The current type-ahead matching state.</summary>
    public UiTypeAheadState TypeAhead = new UiTypeAheadState();
    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen;
    /// <summary>Whether focus wraps at either end.</summary>
    public bool WrapFocus = true;
    /// <summary>Whether Escape dismisses an open popup menu.</summary>
    public bool DismissOnEscape = true;
    /// <summary>Whether an outside click dismisses an open popup menu.</summary>
    public bool DismissOnOutsideClick = true;
    /// <summary>The optional focus target restored after dismissal.</summary>
    public string? RestoreFocusId;
}

/// <summary>Describes one command, check, radio, submenu, or separator menu item.</summary>
public class UiMenuItem
{
    /// <summary>The menu-item behavior.</summary>
    public UiMenuItemKind Kind;
    /// <summary>The stable item key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible label.</summary>
    public string Label = string.Empty;
    /// <summary>The optional stable command identifier.</summary>
    public string? CommandId;
    /// <summary>The optional icon key.</summary>
    public string? IconKey;
    /// <summary>The optional accelerator presentation.</summary>
    public string? Accelerator;
    /// <summary>The optional mnemonic character.</summary>
    public string? Mnemonic;
    /// <summary>The optional nested submenu.</summary>
    public UiMenu? Submenu;
    /// <summary>Whether a check state is supplied.</summary>
    public bool HasCheckState;
    /// <summary>The current check state.</summary>
    public UiCommandCheckState CheckState;
    /// <summary>Whether the item is enabled.</summary>
    public bool IsEnabled = true;
    /// <summary>Whether the item is visible.</summary>
    public bool IsVisible = true;
}

/// <summary>Describes a tab list and its associated panels.</summary>
public class UiTabs
{
    /// <summary>The stable tab-list key.</summary>
    public string Key = string.Empty;
    /// <summary>The tabs.</summary>
    public UiTab[] Tabs = Array.Empty<UiTab>();
    /// <summary>The optional active tab key.</summary>
    public string? ActiveTabKey;
    /// <summary>The tab-list orientation.</summary>
    public UiOrientation Orientation;
    /// <summary>Whether focus automatically activates a tab.</summary>
    public bool AutomaticActivation = true;
    /// <summary>Whether tab reordering is enabled.</summary>
    public bool CanReorder;
    /// <summary>Whether excess tabs use an overflow menu.</summary>
    public bool HasOverflow;
}

/// <summary>Describes one tab and its panel.</summary>
public class UiTab
{
    /// <summary>The stable tab key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible tab label.</summary>
    public string Label = string.Empty;
    /// <summary>The optional icon key.</summary>
    public string? IconKey;
    /// <summary>The optional panel content.</summary>
    public UiNode? Panel;
    /// <summary>Whether the panel is selected.</summary>
    public bool IsSelected;
    /// <summary>Whether the tab is disabled.</summary>
    public bool IsDisabled;
    /// <summary>Whether the tab can be closed.</summary>
    public bool CanClose;
    /// <summary>Whether panel content is materialized lazily.</summary>
    public bool IsLazy;
}

/// <summary>Describes a toolbar and its roving-focus tools.</summary>
public class UiToolbar
{
    /// <summary>The stable toolbar key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional accessible label.</summary>
    public string? Label;
    /// <summary>The tools in visual order.</summary>
    public UiTool[] Tools = Array.Empty<UiTool>();
    /// <summary>The toolbar orientation.</summary>
    public UiOrientation Orientation;
    /// <summary>The optional active tool key.</summary>
    public string? ActiveToolKey;
    /// <summary>Whether excess tools use an overflow menu.</summary>
    public bool HasOverflow;
}

/// <summary>Describes one toolbar command, toggle, separator, or embedded field.</summary>
public class UiTool
{
    /// <summary>The stable tool key.</summary>
    public string Key = string.Empty;
    /// <summary>The tool kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The visible label.</summary>
    public string Label = string.Empty;
    /// <summary>The optional stable command identifier.</summary>
    public string? CommandId;
    /// <summary>The optional contiguous toolbar group key.</summary>
    public string? GroupKey;
    /// <summary>The optional embedded button.</summary>
    public UiButton? Button;
    /// <summary>The optional embedded input.</summary>
    public UiInput? Input;
    /// <summary>The optional embedded choice.</summary>
    public UiChoice? Choice;
    /// <summary>Whether the tool is enabled.</summary>
    public bool IsEnabled = true;
    /// <summary>Whether the tool is visible.</summary>
    public bool IsVisible = true;
}

/// <summary>Describes breadcrumb navigation using ordered link choices.</summary>
public class UiBreadcrumb
{
    /// <summary>The stable breadcrumb key.</summary>
    public string Key = string.Empty;
    /// <summary>The ordered breadcrumb links.</summary>
    public UiChoice[] Items = Array.Empty<UiChoice>();
    /// <summary>The zero-based current item index.</summary>
    public int CurrentIndex;
    /// <summary>Whether the current index is supplied.</summary>
    public bool HasCurrentIndex;
    /// <summary>The accessible label.</summary>
    public string Label = "Breadcrumb";
}

/// <summary>Describes page navigation controls and known logical counts.</summary>
public class UiPagination
{
    /// <summary>The stable pagination key.</summary>
    public string Key = string.Empty;
    /// <summary>The zero-based current page index.</summary>
    public int PageIndex;
    /// <summary>The requested page size.</summary>
    public int PageSize;
    /// <summary>Whether the total page count is known.</summary>
    public bool HasPageCount;
    /// <summary>The total page count.</summary>
    public int PageCount;
    /// <summary>Whether a previous page is available.</summary>
    public bool HasPrevious;
    /// <summary>Whether a next page is available.</summary>
    public bool HasNext;
}
