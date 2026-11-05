using htmlgui;

namespace htmlgui.Tests;

public class UiEnumProfileTests
{
    [Test]
    public void DefaultsAreValidZeroValues()
    {
        UiEnumProfile profile = new UiEnumProfile();

        Assert.Multiple(() =>
        {
            Assert.That(profile.ValueKind, Is.EqualTo(UiValueKind.Null));
            Assert.That(profile.NodeKind, Is.EqualTo(UiNodeKind.Element));
            Assert.That(profile.DomNodeKind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(profile.LayoutKind, Is.EqualTo(UiLayoutKind.Flow));
            Assert.That(profile.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(profile.Alignment, Is.EqualTo(UiAlignment.Start));
            Assert.That(profile.Justification, Is.EqualTo(UiJustification.Start));
            Assert.That(profile.OverflowMode, Is.EqualTo(UiOverflowMode.Visible));
            Assert.That(profile.LengthUnit, Is.EqualTo(UiLengthUnit.Auto));
            Assert.That(profile.TextKind, Is.EqualTo(UiTextKind.Plain));
            Assert.That(profile.ButtonKind, Is.EqualTo(UiButtonKind.Push));
            Assert.That(profile.InputKind, Is.EqualTo(UiInputKind.Text));
            Assert.That(profile.SelectionMode, Is.EqualTo(UiSelectionMode.None));
            Assert.That(profile.SortDirection, Is.EqualTo(UiSortDirection.None));
            Assert.That(profile.MenuItemKind, Is.EqualTo(UiMenuItemKind.Command));
            Assert.That(profile.DialogKind, Is.EqualTo(UiDialogKind.Modal));
            Assert.That(profile.ValidationSeverity, Is.EqualTo(UiValidationSeverity.Information));
            Assert.That(profile.ValidationTrigger, Is.EqualTo(UiValidationTrigger.Input));
            Assert.That(profile.EventKind, Is.EqualTo(UiEventKind.Click));
            Assert.That(profile.EventPhase, Is.EqualTo(UiEventPhase.Capture));
            Assert.That(profile.CommandCheckState, Is.EqualTo(UiCommandCheckState.Unchecked));
            Assert.That(profile.BindingMode, Is.EqualTo(UiBindingMode.OneTime));
            Assert.That(profile.BindingTrigger, Is.EqualTo(UiBindingTrigger.Immediate));
            Assert.That(profile.LifecycleStage, Is.EqualTo(UiLifecycleStage.Create));
            Assert.That(profile.NavigationKind, Is.EqualTo(UiNavigationKind.Push));
            Assert.That(profile.NavigationStatus, Is.EqualTo(UiNavigationStatus.Pending));
            Assert.That(profile.FocusDirection, Is.EqualTo(UiFocusDirection.None));
            Assert.That(profile.AriaLiveMode, Is.EqualTo(UiAriaLiveMode.Off));
            Assert.That(profile.AriaCurrentKind, Is.EqualTo(UiAriaCurrentKind.None));
            Assert.That(profile.ThemeMode, Is.EqualTo(UiThemeMode.Auto));
            Assert.That(profile.ResponsiveOperator, Is.EqualTo(UiResponsiveOperator.MinWidth));
            Assert.That(profile.AnimationStage, Is.EqualTo(UiAnimationStage.Idle));
            Assert.That(profile.MutationKind, Is.EqualTo(UiMutationKind.Insert));
            Assert.That(profile.HydrationMode, Is.EqualTo(UiHydrationMode.Exact));
            Assert.That(profile.HydrationStatus, Is.EqualTo(UiHydrationStatus.Matched));
            Assert.That(profile.UpdatePriority, Is.EqualTo(UiUpdatePriority.Immediate));
            Assert.That(profile.BridgeMessageKind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(profile.TransportState, Is.EqualTo(UiTransportState.Stopped));
            Assert.That(profile.DiagnosticSeverity, Is.EqualTo(UiDiagnosticSeverity.Information));
        });
    }

    [Test]
    public void FieldsAreMutableAndProfileCanFlowThroughContract()
    {
        UiEnumProfile profile = new UiEnumProfile
        {
            ValueKind = UiValueKind.Object,
            NodeKind = UiNodeKind.RawContent,
            DomNodeKind = DomNodeKind.RawContent,
            LayoutKind = UiLayoutKind.ResponsiveShell,
            Orientation = UiOrientation.Vertical,
            Alignment = UiAlignment.Baseline,
            Justification = UiJustification.SpaceEvenly,
            OverflowMode = UiOverflowMode.Auto,
            LengthUnit = UiLengthUnit.Pica,
            TextKind = UiTextKind.Localized,
            ButtonKind = UiButtonKind.Command,
            InputKind = UiInputKind.ReadOnly,
            SelectionMode = UiSelectionMode.Range,
            SortDirection = UiSortDirection.Descending,
            MenuItemKind = UiMenuItemKind.Separator,
            DialogKind = UiDialogKind.Drawer,
            ValidationSeverity = UiValidationSeverity.Error,
            ValidationTrigger = UiValidationTrigger.Manual,
            EventKind = UiEventKind.Toggle,
            EventPhase = UiEventPhase.Bubble,
            CommandCheckState = UiCommandCheckState.Mixed,
            BindingMode = UiBindingMode.OneWayToSource,
            BindingTrigger = UiBindingTrigger.Explicit,
            LifecycleStage = UiLifecycleStage.Error,
            NavigationKind = UiNavigationKind.External,
            NavigationStatus = UiNavigationStatus.Failed,
            FocusDirection = UiFocusDirection.Right,
            AriaLiveMode = UiAriaLiveMode.Assertive,
            AriaCurrentKind = UiAriaCurrentKind.True,
            ThemeMode = UiThemeMode.HighContrast,
            ResponsiveOperator = UiResponsiveOperator.Motion,
            AnimationStage = UiAnimationStage.Cancelled,
            MutationKind = UiMutationKind.UnsubscribeEvent,
            HydrationMode = UiHydrationMode.Replace,
            HydrationStatus = UiHydrationStatus.Failed,
            UpdatePriority = UiUpdatePriority.Background,
            BridgeMessageKind = UiBridgeMessageKind.Chunk,
            TransportState = UiTransportState.Faulted,
            DiagnosticSeverity = UiDiagnosticSeverity.Error
        };
        IUiEnumProfileSource source = new TestEnumProfileSource(profile);

        UiEnumProfile result = source.GetEnumProfile();

        Assert.That(result, Is.SameAs(profile));
        Assert.That(result.ValueKind, Is.EqualTo(UiValueKind.Object));
        Assert.That(result.LayoutKind, Is.EqualTo(UiLayoutKind.ResponsiveShell));
        Assert.That(result.EventKind, Is.EqualTo(UiEventKind.Toggle));
        Assert.That(result.BridgeMessageKind, Is.EqualTo(UiBridgeMessageKind.Chunk));
        Assert.That(result.DiagnosticSeverity, Is.EqualTo(UiDiagnosticSeverity.Error));
    }

    private sealed class TestEnumProfileSource : IUiEnumProfileSource
    {
        private readonly UiEnumProfile profile;

        public TestEnumProfileSource(UiEnumProfile profile)
        {
            this.profile = profile;
        }

        public UiEnumProfile GetEnumProfile()
        {
            return profile;
        }
    }
}
