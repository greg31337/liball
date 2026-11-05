using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiEnumsTests
{
    [Test]
    public void EnumMembersHaveStableNamesAndSequentialValues()
    {
        AssertMembers<UiValueKind>("Null", "Boolean", "Integer", "FloatingPoint", "Decimal", "String", "DateTime", "Binary", "Array", "Object");
        AssertMembers<UiNodeKind>("Element", "Text", "Comment", "Fragment", "Component", "Control", "Layout", "Conditional", "Repeat", "Slot", "Portal", "RawContent");
        AssertMembers<DomNodeKind>("Element", "Text", "Comment", "Fragment", "RawContent");
        AssertMembers<UiLayoutKind>("Flow", "Inline", "VerticalStack", "HorizontalStack", "FlexRow", "FlexColumn", "Wrap", "Grid", "UniformGrid", "Dock", "Overlay", "Absolute", "Split", "Scroll", "ResponsiveShell");
        AssertMembers<UiOrientation>("Horizontal", "Vertical");
        AssertMembers<UiAlignment>("Start", "Center", "End", "Stretch", "Baseline");
        AssertMembers<UiJustification>("Start", "Center", "End", "SpaceBetween", "SpaceAround", "SpaceEvenly");
        AssertMembers<UiOverflowMode>("Visible", "Hidden", "Clip", "Scroll", "Auto");
        AssertMembers<UiLengthUnit>("Auto", "Pixel", "Percent", "Em", "Rem", "ViewportWidth", "ViewportHeight", "Fraction", "Character", "Centimeter", "Millimeter", "Inch", "Point", "Pica");
        AssertMembers<UiTextKind>("Plain", "Heading", "Paragraph", "Label", "Code", "Preformatted", "Quote", "Link", "Icon", "Image", "Figure", "Badge", "Chip", "Status", "Landmark", "Localized");
        AssertMembers<UiButtonKind>("Push", "Submit", "Reset", "Toggle", "Split", "Icon", "Command");
        AssertMembers<UiInputKind>("Text", "Password", "Search", "Email", "Url", "Telephone", "Multiline", "Number", "Range", "Date", "Time", "Month", "Week", "LocalDateTime", "Color", "File", "Hidden", "ReadOnly");
        AssertMembers<UiSelectionMode>("None", "Single", "Multiple", "Range");
        AssertMembers<UiSortDirection>("None", "Ascending", "Descending");
        AssertMembers<UiMenuItemKind>("Command", "Checkbox", "Radio", "Submenu", "Separator");
        AssertMembers<UiDialogKind>("Modal", "NonModal", "Alert", "Confirm", "Prompt", "Sheet", "Drawer");
        AssertMembers<UiValidationSeverity>("Information", "Warning", "Error");
        AssertMembers<UiValidationTrigger>("Input", "Change", "Blur", "Submit", "Manual");
        AssertMembers<UiEventKind>("Click", "DoubleClick", "ContextMenu", "PointerDown", "PointerUp", "PointerMove", "PointerEnter", "PointerLeave", "PointerCancel", "MouseDown", "MouseUp", "MouseMove", "MouseEnter", "MouseLeave", "Wheel", "KeyDown", "KeyUp", "Focus", "Blur", "FocusIn", "FocusOut", "BeforeInput", "Input", "Change", "CompositionStart", "CompositionUpdate", "CompositionEnd", "SelectionChange", "Submit", "Reset", "DragStart", "Drag", "DragEnter", "DragOver", "DragLeave", "Drop", "DragEnd", "Copy", "Cut", "Paste", "Scroll", "Resize", "VisibilityChange", "Toggle");
        AssertMembers<UiEventPhase>("Capture", "Target", "Bubble");
        AssertMembers<UiCommandCheckState>("Unchecked", "Checked", "Mixed");
        AssertMembers<UiBindingMode>("OneTime", "OneWay", "TwoWay", "OneWayToSource");
        AssertMembers<UiBindingTrigger>("Immediate", "Input", "Change", "Blur", "Explicit");
        AssertMembers<UiLifecycleStage>("Create", "Attach", "ParametersChanged", "BeforeRender", "AfterRender", "Activate", "Deactivate", "Detach", "Dispose", "HydrationStart", "HydrationComplete", "Error");
        AssertMembers<UiNavigationKind>("Push", "Replace", "Back", "Forward", "Reload", "Link", "Fragment", "External");
        AssertMembers<UiNavigationStatus>("Pending", "Resolved", "Redirected", "Cancelled", "NotFound", "External", "Failed");
        AssertMembers<UiFocusDirection>("None", "First", "Last", "Next", "Previous", "Up", "Down", "Left", "Right");
        AssertMembers<UiFocusOperation>("Focus", "Blur", "Move", "Restore", "SetActiveDescendant");
        AssertMembers<UiAriaLiveMode>("Off", "Polite", "Assertive");
        AssertMembers<UiAriaCurrentKind>("None", "Page", "Step", "Location", "Date", "Time", "True");
        AssertMembers<UiAccessibilityFeature>("ForcedColors", "Zoom", "Reflow", "FocusVisible", "TargetSize", "ReducedMotion", "ReducedTransparency", "ScreenReaderOnly", "Busy", "NonColorStatus");
        AssertMembers<UiThemeMode>("Auto", "Light", "Dark", "HighContrast");
        AssertMembers<UiResponsiveOperator>("MinWidth", "MaxWidth", "MinHeight", "MaxHeight", "Orientation", "Pointer", "Hover", "ColorScheme", "Contrast", "Motion", "ForcedColors");
        AssertMembers<UiAnimationStage>("Idle", "Enter", "Leave", "Move", "Completed", "Cancelled");
        AssertMembers<UiMutationKind>("Insert", "Remove", "Move", "Replace", "SetText", "SetAttribute", "RemoveAttribute", "SetProperty", "RemoveProperty", "AddClass", "RemoveClass", "SetStyle", "RemoveStyle", "SubscribeEvent", "UnsubscribeEvent");
        AssertMembers<UiHydrationMode>("Exact", "Repair", "Replace");
        AssertMembers<UiHydrationStatus>("Matched", "Repaired", "Replaced", "Mismatch", "Failed");
        AssertMembers<UiUpdatePriority>("Immediate", "UserInteraction", "Normal", "Background");
        AssertMembers<UiBridgeMessageKind>("Initialize", "Ready", "RenderSnapshot", "DomPatch", "Event", "Command", "StateMutation", "Validation", "Navigation", "Focus", "Measurement", "VirtualizationRange", "VirtualizationResult", "Lifecycle", "Log", "Diagnostic", "HostCall", "HostResult", "Request", "Response", "Error", "Cancel", "Acknowledgement", "Heartbeat", "Shutdown", "Chunk");
        AssertMembers<UiTransportState>("Stopped", "Starting", "Handshaking", "Ready", "Reconnecting", "Closing", "Faulted");
        AssertMembers<UiDiagnosticSeverity>("Information", "Warning", "Error");
    }

    [Test]
    public void EveryEnumRejectsUndefinedValuesDuringRuntimeInspection()
    {
        Type[] enumTypes = typeof(UiValueKind).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsEnum && type.Namespace == "htmlgui")
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(enumTypes, Has.Length.EqualTo(41));
        foreach (Type enumType in enumTypes)
        {
            object invalid = Enum.ToObject(enumType, int.MaxValue);
            Assert.That(Enum.IsDefined(enumType, invalid), Is.False, enumType.Name);
            Assert.That(Enum.GetName(enumType, invalid), Is.Null, enumType.Name);
        }
    }

    private static void AssertMembers<T>(params string[] expectedNames) where T : struct, Enum
    {
        Assert.That(Enum.GetNames<T>(), Is.EqualTo(expectedNames));
        T[] values = Enum.GetValues<T>();
        for (int index = 0; index < values.Length; index++)
        {
            Assert.That(Convert.ToInt32(values[index]), Is.EqualTo(index), $"{typeof(T).Name}.{expectedNames[index]}");
        }
    }
}
