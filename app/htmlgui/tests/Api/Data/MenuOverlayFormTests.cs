using System;
using htmlgui;

namespace htmlgui.Tests;

public class MenuOverlayFormTests
{
    [Test]
    public void MenuAndNavigationModelsHaveSafeDefaults()
    {
        UiMenu menu = new UiMenu();
        UiMenuItem item = new UiMenuItem();
        UiTabs tabs = new UiTabs();
        UiTab tab = new UiTab();
        UiToolbar toolbar = new UiToolbar();
        UiTool tool = new UiTool();
        UiBreadcrumb breadcrumb = new UiBreadcrumb();
        UiPagination pagination = new UiPagination();

        Assert.Multiple(() =>
        {
            Assert.That(menu.Items, Is.Empty);
            Assert.That(menu.Orientation, Is.EqualTo(UiOrientation.Vertical));
            Assert.That(menu.TypeAhead, Is.Not.Null);
            Assert.That(menu.WrapFocus, Is.True);
            Assert.That(menu.DismissOnEscape, Is.True);
            Assert.That(menu.DismissOnOutsideClick, Is.True);
            Assert.That(menu.RestoreFocusId, Is.Null);
            Assert.That(item.Kind, Is.EqualTo(UiMenuItemKind.Command));
            Assert.That(item.Submenu, Is.Null);
            Assert.That(item.IsEnabled, Is.True);
            Assert.That(item.IsVisible, Is.True);
            Assert.That(tabs.Tabs, Is.Empty);
            Assert.That(tabs.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(tabs.AutomaticActivation, Is.True);
            Assert.That(tab.Panel, Is.Null);
            Assert.That(toolbar.Tools, Is.Empty);
            Assert.That(toolbar.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(tool.Button, Is.Null);
            Assert.That(tool.Input, Is.Null);
            Assert.That(tool.Choice, Is.Null);
            Assert.That(tool.GroupKey, Is.Null);
            Assert.That(tool.IsEnabled, Is.True);
            Assert.That(breadcrumb.Items, Is.Empty);
            Assert.That(breadcrumb.HasCurrentIndex, Is.False);
            Assert.That(breadcrumb.Label, Is.EqualTo("Breadcrumb"));
            Assert.That(pagination.HasPageCount, Is.False);
            Assert.That(pagination.HasPrevious, Is.False);
            Assert.That(pagination.HasNext, Is.False);
        });
    }

    [Test]
    public void PopulatedMenusTabsToolbarBreadcrumbAndPaginationCompose()
    {
        UiMenu submenu = new UiMenu { Key = "recent", Label = "Recent", IsOpen = true };
        UiMenuItem item = new UiMenuItem { Kind = UiMenuItemKind.Submenu, Key = "file", Label = "File", CommandId = "open", IconKey = "folder", Accelerator = "Ctrl+O", Mnemonic = "F", Submenu = submenu, HasCheckState = true, CheckState = UiCommandCheckState.Checked, IsEnabled = false, IsVisible = false };
        UiTypeAheadState typeAhead = new UiTypeAheadState { Buffer = "fi", MatchedKey = "file", Generation = 2 };
        UiMenu menu = new UiMenu { Key = "main", Label = "Main menu", Items = new UiMenuItem[] { item }, Orientation = UiOrientation.Horizontal, ActiveItemKey = "file", TypeAhead = typeAhead, IsOpen = true, WrapFocus = false, DismissOnEscape = false, DismissOnOutsideClick = false, RestoreFocusId = "menu-trigger" };
        UiNode panel = new UiNode { Text = "Panel" };
        UiTab tab = new UiTab { Key = "general", Label = "General", IconKey = "gear", Panel = panel, IsSelected = true, IsDisabled = true, CanClose = true, IsLazy = true };
        UiTabs tabs = new UiTabs { Key = "settings", Tabs = new UiTab[] { tab }, ActiveTabKey = "general", Orientation = UiOrientation.Vertical, AutomaticActivation = false, CanReorder = true, HasOverflow = true };
        UiButton button = new UiButton { Label = "Save" };
        UiInput input = new UiInput { Name = "search" };
        UiChoice choice = new UiChoice { Key = "choice", Label = "Choice" };
        UiTool tool = new UiTool { Key = "save", Kind = "button", Label = "Save", CommandId = "save", GroupKey = "file", Button = button, Input = input, Choice = choice, IsEnabled = false, IsVisible = false };
        UiToolbar toolbar = new UiToolbar { Key = "edit", Label = "Edit", Tools = new UiTool[] { tool }, Orientation = UiOrientation.Vertical, ActiveToolKey = "save", HasOverflow = true };
        UiBreadcrumb breadcrumb = new UiBreadcrumb { Key = "crumbs", Items = new UiChoice[] { new UiChoice { Key = "home", Label = "Home", Url = "/" } }, CurrentIndex = 0, HasCurrentIndex = true, Label = "Location" };
        UiPagination pagination = new UiPagination { Key = "pages", PageIndex = 2, PageSize = 25, HasPageCount = true, PageCount = 4, HasPrevious = true, HasNext = true };

        Assert.Multiple(() =>
        {
            Assert.That(menu.Items[0], Is.SameAs(item));
            Assert.That(item.Submenu, Is.SameAs(submenu));
            Assert.That(item.CheckState, Is.EqualTo(UiCommandCheckState.Checked));
            Assert.That(menu.WrapFocus, Is.False);
            Assert.That(menu.TypeAhead, Is.SameAs(typeAhead));
            Assert.That(menu.DismissOnEscape, Is.False);
            Assert.That(menu.DismissOnOutsideClick, Is.False);
            Assert.That(menu.RestoreFocusId, Is.EqualTo("menu-trigger"));
            Assert.That(tabs.Tabs[0].Panel, Is.SameAs(panel));
            Assert.That(tabs.AutomaticActivation, Is.False);
            Assert.That(tab.IsLazy, Is.True);
            Assert.That(toolbar.Tools[0], Is.SameAs(tool));
            Assert.That(tool.Button, Is.SameAs(button));
            Assert.That(tool.Input, Is.SameAs(input));
            Assert.That(tool.Choice, Is.SameAs(choice));
            Assert.That(tool.GroupKey, Is.EqualTo("file"));
            Assert.That(breadcrumb.HasCurrentIndex, Is.True);
            Assert.That(breadcrumb.CurrentIndex, Is.Zero);
            Assert.That(pagination.PageCount, Is.EqualTo(4));
            Assert.That(pagination.HasNext, Is.True);
        });
    }

    [Test]
    public void OverlayModelsHaveSafeDefaultsAndRetainDismissalState()
    {
        UiDialog dialog = new UiDialog();
        UiPopover popover = new UiPopover();
        UiTooltip tooltip = new UiTooltip();
        UiToast toast = new UiToast();
        UiAnchor anchor = new UiAnchor();
        UiOverlayState state = new UiOverlayState();

        Assert.Multiple(() =>
        {
            Assert.That(dialog.Kind, Is.EqualTo(UiDialogKind.Modal));
            Assert.That(dialog.Content, Is.Empty);
            Assert.That(dialog.Actions, Is.Empty);
            Assert.That(dialog.State, Is.Not.Null);
            Assert.That(popover.Anchor, Is.Not.Null);
            Assert.That(popover.Content, Is.Empty);
            Assert.That(tooltip.Text, Is.Empty);
            Assert.That(tooltip.HasDelayMilliseconds, Is.False);
            Assert.That(toast.LiveMode, Is.EqualTo(UiAriaLiveMode.Polite));
            Assert.That(toast.Action, Is.Null);
            Assert.That(anchor.Placement, Is.EqualTo("bottom"));
            Assert.That(anchor.Offset, Is.Not.SameAs(anchor.CrossOffset));
            Assert.That(anchor.AllowFlip, Is.True);
            Assert.That(anchor.AllowShift, Is.True);
            Assert.That(state.IsOpen, Is.False);
            Assert.That(state.DismissOnEscape, Is.True);
            Assert.That(state.DismissOnOutsideClick, Is.True);
            Assert.That(state.RestoreFocusId, Is.Null);
        });

        UiOverlayState populatedState = new UiOverlayState { IsOpen = true, IsModal = true, HasBackdrop = true, DismissOnEscape = false, DismissOnOutsideClick = false, TrapFocus = true, RestoreFocusId = "open-button", StackOrder = 3 };
        UiAnchor populatedAnchor = new UiAnchor { TargetId = "anchor", Placement = "top-start", Offset = Length(4), CrossOffset = Length(2), AllowFlip = false, AllowShift = false };
        UiNode content = new UiNode { Text = "Overlay" };
        UiButton action = new UiButton { Label = "Undo" };
        dialog = new UiDialog { Kind = UiDialogKind.Alert, Key = "alert", Title = "Alert", Content = new UiNode[] { content }, Actions = new UiButton[] { action }, State = populatedState, InitialFocusId = "close" };
        popover = new UiPopover { Key = "popover", Anchor = populatedAnchor, Content = new UiNode[] { content }, State = populatedState };
        tooltip = new UiTooltip { Key = "tip", Text = "Help", Anchor = populatedAnchor, State = populatedState, HasDelayMilliseconds = true, DelayMilliseconds = 0 };
        toast = new UiToast { Key = "toast", Message = "Saved", Severity = UiValidationSeverity.Information, LiveMode = UiAriaLiveMode.Assertive, Action = action, State = populatedState, HasDurationMilliseconds = true, DurationMilliseconds = 0 };

        Assert.Multiple(() =>
        {
            Assert.That(dialog.State, Is.SameAs(populatedState));
            Assert.That(dialog.Content[0], Is.SameAs(content));
            Assert.That(dialog.Actions[0], Is.SameAs(action));
            Assert.That(popover.Anchor, Is.SameAs(populatedAnchor));
            Assert.That(populatedAnchor.AllowFlip, Is.False);
            Assert.That(tooltip.HasDelayMilliseconds, Is.True);
            Assert.That(tooltip.DelayMilliseconds, Is.Zero);
            Assert.That(toast.Action, Is.SameAs(action));
            Assert.That(toast.HasDurationMilliseconds, Is.True);
            Assert.That(toast.DurationMilliseconds, Is.Zero);
            Assert.That(populatedState.TrapFocus, Is.True);
            Assert.That(populatedState.StackOrder, Is.EqualTo(3));
        });
    }

    [Test]
    public void FormModelsHaveSafeDefaultsAndRetainValidationState()
    {
        UiForm form = new UiForm();
        UiField field = new UiField();
        UiValidationRule rule = new UiValidationRule();
        UiValidationIssue issue = new UiValidationIssue();
        UiValidationReport report = new UiValidationReport();
        UiFormState state = new UiFormState();

        Assert.Multiple(() =>
        {
            Assert.That(form.Method, Is.EqualTo("post"));
            Assert.That(form.Fields, Is.Empty);
            Assert.That(form.State, Is.Not.Null);
            Assert.That(form.Validation, Is.Not.Null);
            Assert.That(field.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(field.Choices, Is.Empty);
            Assert.That(field.Rules, Is.Empty);
            Assert.That(field.Issues, Is.Empty);
            Assert.That(field.IsValid, Is.True);
            Assert.That(rule.Trigger, Is.EqualTo(UiValidationTrigger.Input));
            Assert.That(rule.Severity, Is.EqualTo(UiValidationSeverity.Error));
            Assert.That(rule.Parameters, Is.Empty);
            Assert.That(issue.Severity, Is.EqualTo(UiValidationSeverity.Error));
            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.IsValid, Is.True);
            Assert.That(state.InitialValues, Is.Empty);
            Assert.That(state.CurrentValues, Is.Empty);
            Assert.That(state.CanSubmit, Is.True);
        });

        rule = new UiValidationRule { Key = "required", Kind = "required", Trigger = UiValidationTrigger.Blur, Severity = UiValidationSeverity.Warning, Message = "Required", HandlerId = "validator", Parameters = new UiValueEntry[] { Entry("minimum", IntegerValue(1)) } };
        issue = new UiValidationIssue { Id = "issue-1", FieldKey = "name", Code = "required", Message = "Required", Severity = UiValidationSeverity.Warning, Source = "remote", ValueVersion = 2 };
        UiInput input = new UiInput { Name = "name" };
        field = new UiField { Key = "name", Name = "name", Label = "Name", Value = TextValue(""), Input = input, Choices = new UiChoice[] { new UiChoice { Key = "a" } }, Rules = new UiValidationRule[] { rule }, Issues = new UiValidationIssue[] { issue }, IsDirty = true, IsTouched = true, IsVisited = true, IsPending = true, IsValid = false };
        state = new UiFormState { InitialValues = new UiValueEntry[] { Entry("name", TextValue("")) }, CurrentValues = new UiValueEntry[] { Entry("name", TextValue("Ada")) }, IsDirty = true, IsPending = true, IsSubmitting = true, CanSubmit = false, Version = 2 };
        report = new UiValidationReport { Issues = new UiValidationIssue[] { issue }, IsValid = false, IsPending = true, FirstInvalidFieldKey = "name", Version = 2 };
        form = new UiForm { Key = "profile", Action = "/save", Method = "put", Fields = new UiField[] { field }, State = state, Validation = report, SubmitCommandId = "submit", ResetCommandId = "reset", CancelCommandId = "cancel", DisableNativeValidation = true };

        Assert.Multiple(() =>
        {
            Assert.That(form.Fields[0], Is.SameAs(field));
            Assert.That(field.Input, Is.SameAs(input));
            Assert.That(field.Rules[0], Is.SameAs(rule));
            Assert.That(rule.Parameters[0].Value.IntegerValue, Is.EqualTo(1));
            Assert.That(field.Issues[0], Is.SameAs(issue));
            Assert.That(field.IsValid, Is.False);
            Assert.That(issue.ValueVersion, Is.EqualTo(2));
            Assert.That(form.State, Is.SameAs(state));
            Assert.That(state.CanSubmit, Is.False);
            Assert.That(form.Validation, Is.SameAs(report));
            Assert.That(report.FirstInvalidFieldKey, Is.EqualTo("name"));
            Assert.That(form.DisableNativeValidation, Is.True);
        });
    }

    [Test]
    public void SemanticNodeCanCarryEveryTaskModelRoot()
    {
        UiNode node = new UiNode
        {
            Layout = new UiLayout(),
            TextContent = new UiText(),
            Button = new UiButton(),
            Input = new UiInput(),
            Media = new UiMedia(),
            Feedback = new UiFeedback(),
            Disclosure = new UiDisclosure(),
            Collection = new UiCollection(),
            Table = new UiTable(),
            Tree = new UiTree(),
            Menu = new UiMenu(),
            Tabs = new UiTabs(),
            Toolbar = new UiToolbar(),
            Breadcrumb = new UiBreadcrumb(),
            Pagination = new UiPagination(),
            Dialog = new UiDialog(),
            Popover = new UiPopover(),
            Tooltip = new UiTooltip(),
            Toast = new UiToast(),
            Form = new UiForm()
        };

        Assert.Multiple(() =>
        {
            Assert.That(node.Layout, Is.Not.Null);
            Assert.That(node.TextContent, Is.Not.Null);
            Assert.That(node.Button, Is.Not.Null);
            Assert.That(node.Input, Is.Not.Null);
            Assert.That(node.Media, Is.Not.Null);
            Assert.That(node.Feedback, Is.Not.Null);
            Assert.That(node.Disclosure, Is.Not.Null);
            Assert.That(node.Collection, Is.Not.Null);
            Assert.That(node.Table, Is.Not.Null);
            Assert.That(node.Tree, Is.Not.Null);
            Assert.That(node.Menu, Is.Not.Null);
            Assert.That(node.Tabs, Is.Not.Null);
            Assert.That(node.Toolbar, Is.Not.Null);
            Assert.That(node.Breadcrumb, Is.Not.Null);
            Assert.That(node.Pagination, Is.Not.Null);
            Assert.That(node.Dialog, Is.Not.Null);
            Assert.That(node.Popover, Is.Not.Null);
            Assert.That(node.Tooltip, Is.Not.Null);
            Assert.That(node.Toast, Is.Not.Null);
            Assert.That(node.Form, Is.Not.Null);
        });
    }

    private static UiLength Length(double value)
    {
        return new UiLength { HasValue = true, Value = value, Unit = UiLengthUnit.Pixel };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue IntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }
}
