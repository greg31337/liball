namespace htmlgui;

/// <summary>Collects categorical API selections for compatibility exchange and inspection.</summary>
/// <remarks>This field-based model provides a portable consumer for every public UI enumeration.</remarks>
public class UiEnumProfile
{
    /// <summary>The selected portable value kind.</summary>
    public UiValueKind ValueKind;
    /// <summary>The selected model node kind.</summary>
    public UiNodeKind NodeKind;
    /// <summary>The selected DOM node kind.</summary>
    public DomNodeKind DomNodeKind;
    /// <summary>The selected layout kind.</summary>
    public UiLayoutKind LayoutKind;
    /// <summary>The selected orientation.</summary>
    public UiOrientation Orientation;
    /// <summary>The selected alignment.</summary>
    public UiAlignment Alignment;
    /// <summary>The selected justification.</summary>
    public UiJustification Justification;
    /// <summary>The selected overflow mode.</summary>
    public UiOverflowMode OverflowMode;
    /// <summary>The selected length unit.</summary>
    public UiLengthUnit LengthUnit;
    /// <summary>The selected text kind.</summary>
    public UiTextKind TextKind;
    /// <summary>The selected button kind.</summary>
    public UiButtonKind ButtonKind;
    /// <summary>The selected input kind.</summary>
    public UiInputKind InputKind;
    /// <summary>The selected selection mode.</summary>
    public UiSelectionMode SelectionMode;
    /// <summary>The selected sort direction.</summary>
    public UiSortDirection SortDirection;
    /// <summary>The selected menu-item kind.</summary>
    public UiMenuItemKind MenuItemKind;
    /// <summary>The selected dialog kind.</summary>
    public UiDialogKind DialogKind;
    /// <summary>The selected validation severity.</summary>
    public UiValidationSeverity ValidationSeverity;
    /// <summary>The selected validation trigger.</summary>
    public UiValidationTrigger ValidationTrigger;
    /// <summary>The selected event kind.</summary>
    public UiEventKind EventKind;
    /// <summary>The selected event phase.</summary>
    public UiEventPhase EventPhase;
    /// <summary>The selected command check state.</summary>
    public UiCommandCheckState CommandCheckState;
    /// <summary>The selected binding mode.</summary>
    public UiBindingMode BindingMode;
    /// <summary>The selected binding trigger.</summary>
    public UiBindingTrigger BindingTrigger;
    /// <summary>The selected lifecycle stage.</summary>
    public UiLifecycleStage LifecycleStage;
    /// <summary>The selected navigation kind.</summary>
    public UiNavigationKind NavigationKind;
    /// <summary>The selected navigation status.</summary>
    public UiNavigationStatus NavigationStatus;
    /// <summary>The selected focus direction.</summary>
    public UiFocusDirection FocusDirection;
    /// <summary>The selected ARIA live-region mode.</summary>
    public UiAriaLiveMode AriaLiveMode;
    /// <summary>The selected ARIA current-item kind.</summary>
    public UiAriaCurrentKind AriaCurrentKind;
    /// <summary>The selected theme mode.</summary>
    public UiThemeMode ThemeMode;
    /// <summary>The selected responsive operator.</summary>
    public UiResponsiveOperator ResponsiveOperator;
    /// <summary>The selected animation stage.</summary>
    public UiAnimationStage AnimationStage;
    /// <summary>The selected mutation kind.</summary>
    public UiMutationKind MutationKind;
    /// <summary>The selected hydration mode.</summary>
    public UiHydrationMode HydrationMode;
    /// <summary>The selected hydration status.</summary>
    public UiHydrationStatus HydrationStatus;
    /// <summary>The selected update priority.</summary>
    public UiUpdatePriority UpdatePriority;
    /// <summary>The selected bridge message kind.</summary>
    public UiBridgeMessageKind BridgeMessageKind;
    /// <summary>The selected transport state.</summary>
    public UiTransportState TransportState;
    /// <summary>The selected public diagnostic severity.</summary>
    public UiDiagnosticSeverity DiagnosticSeverity;
}
