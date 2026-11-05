using System;
using System.Collections.Generic;

namespace htmlgui;

internal class HostScheduleResult
{
    internal bool Success = true;
    internal UiEventDispatchResult EventResult = new UiEventDispatchResult();
    internal UiBindingResult BindingResult = new UiBindingResult { Success = true };
    internal UiValidationResult ValidationResult = new UiValidationResult { Success = true, IsValid = true, CanSubmit = true };
    internal UiCommandResult CommandResult = new UiCommandResult { Success = true };
    internal UiComponentResult ComponentResult = new UiComponentResult { Success = true };
    internal UiNavigationResult NavigationResult = new UiNavigationResult { Success = true };
    internal UiFocusResult FocusResult = new UiFocusResult { Success = true };
    internal List<string> Stages = new List<string>();
    internal bool RecoveredByComponentBoundary;
    internal string StateSnapshotId = string.Empty;
}

internal class UpdateScheduler
{
    internal IUiEventDispatcher Events;
    internal IUiBindingEngine Bindings;
    internal IUiValidationEngine Validation;
    internal IUiCommandDispatcher Commands;
    internal IUiComponentRuntime Components;
    internal IUiRouter Router;
    internal IUiFocusController Focus;
    internal bool TransactionActive;
    internal int TransactionGeneration;
    internal List<string> StateIds;
    internal List<string> BindingIds;
    internal List<string> FormKeys;
    internal string StateScope = string.Empty;
    internal UiValueEntry[] StateCheckpoint = Array.Empty<UiValueEntry>();

    internal UpdateScheduler(IUiEventDispatcher events, IUiBindingEngine bindings, IUiValidationEngine validation, IUiCommandDispatcher commands, IUiComponentRuntime components, IUiRouter router, IUiFocusController focus)
    {
        Events = events;
        Bindings = bindings;
        Validation = validation;
        Commands = commands;
        Components = components;
        Router = router;
        Focus = focus;
        StateIds = new List<string>();
        BindingIds = new List<string>();
        FormKeys = new List<string>();
    }

    internal HostScheduleResult ExecuteHostEvent(UiApplication application, UiEventData? eventData)
    {
        HostScheduleResult result = new HostScheduleResult();
        if (TransactionActive)
        {
            result.Success = false;
            result.EventResult.Success = false;
            result.EventResult.Diagnostics = new UiDiagnostic[]
            {
                CreateSchedulerDiagnostic("host.transaction-reentrant", "A host update transaction is already active.", "/host/transaction")
            };
            return result;
        }

        TransactionActive = true;
        if (TransactionGeneration < int.MaxValue)
        {
            TransactionGeneration++;
        }

        string snapshotId = "host-event-" + InvariantFormatter.FormatInt32(TransactionGeneration);
        result.StateSnapshotId = snapshotId;
        UiResult stateCheckpoint = CaptureHostState();
        if (!stateCheckpoint.Success)
        {
            result.Success = false;
            result.EventResult.Success = false;
            result.EventResult.Diagnostics = stateCheckpoint.Diagnostics;
            TransactionActive = false;
            return result;
        }

        result.Stages.Add("event");
        result.EventResult = Events.DispatchUiEvent(eventData);
        if (!result.EventResult.Success || eventData == null)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
            TransactionActive = false;
            return result;
        }

        SetNodeEventData(application, eventData);
        result.Stages.Add("state");
        result.BindingResult = ProcessEventBindings(application, eventData);
        if (!result.BindingResult.Success)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
            TransactionActive = false;
            return result;
        }

        result.Stages.Add("bindings");
        ApplyBindingUpdates(application, result.BindingResult.Updates);
        result.Stages.Add("validation");
        result.ValidationResult = ProcessEventValidation(application, eventData);
        if (!result.ValidationResult.Success)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
            TransactionActive = false;
            return result;
        }

        result.Stages.Add("commands");
        result.CommandResult = ProcessEventCommand(application, eventData);
        if (!result.CommandResult.Success)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
            TransactionActive = false;
            return result;
        }

        result.Stages.Add("components");
        result.ComponentResult = Components.ProcessUiComponentUpdates().GetAwaiter().GetResult();
        ApplyComponentResult(application, result.ComponentResult);
        if (!result.ComponentResult.Success && result.ComponentResult.ErrorBoundary != null && result.ComponentResult.ErrorBoundary.HasError && result.ComponentResult.Content != null)
        {
            result.RecoveredByComponentBoundary = true;
        }
        else if (!result.ComponentResult.Success)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
            TransactionActive = false;
            return result;
        }

        result.Stages.Add("route");
        result.NavigationResult = ProcessPendingNavigation(application);
        if (!result.NavigationResult.Success)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
            TransactionActive = false;
            return result;
        }

        result.Stages.Add("focus");
        result.FocusResult = ProcessPendingFocus(application);
        if (!result.FocusResult.Success)
        {
            result.Success = false;
            RestoreHostState(snapshotId);
        }

        TransactionActive = false;
        return result;
    }

    internal UiResult InitializeHostServices(UiApplication application)
    {
        StateScope = application.Id;
        IUiStateStore state = Bindings.GetBindingStateStore();
        int stateIndex = 0;
        while (stateIndex < application.StateEntries.Length)
        {
            UiStateEntry entry = application.StateEntries[stateIndex];
            UiStateResult created = state.CreateState(application.Id, entry.Id, entry.Value, entry.Origin);
            if (!created.Success)
            {
                return CreateSchedulerFailure(created.Diagnostics);
            }

            StateIds.Add(entry.Id);
            stateIndex++;
        }

        ModelWalker walker = WalkApplication(application);
        int nodeIndex = 0;
        while (nodeIndex < walker.Nodes.Count)
        {
            UiNode node = walker.Nodes[nodeIndex];
            int bindingIndex = 0;
            while (bindingIndex < node.Bindings.Length)
            {
                UiBindingResult attached = Bindings.AttachBinding(application.Id, node.Bindings[bindingIndex]);
                if (!attached.Success)
                {
                    return CreateSchedulerFailure(attached.Diagnostics);
                }

                BindingIds.Add(node.Bindings[bindingIndex].Id);
                ApplyBindingUpdates(application, attached.Updates);
                bindingIndex++;
            }

            if (node.Form != null)
            {
                UiValidationResult registered = Validation.RegisterValidationForm(node.Form);
                if (!registered.Success)
                {
                    return CreateSchedulerFailure(registered.Diagnostics);
                }

                if (registered.Form != null)
                {
                    node.Form = registered.Form;
                }

                FormKeys.Add(node.Form.Key);
            }

            nodeIndex++;
        }

        UiWindow? window = SelectActiveWindow(application);
        if (window != null)
        {
            UiResult focusRegistered = Focus.RegisterFocusScopes(window.FocusScopes, window.FocusTargets);
            if (!focusRegistered.Success)
            {
                return focusRegistered;
            }
        }

        return new UiResult { Success = true };
    }

    internal UiResult CleanupHostServices()
    {
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        int bindingIndex = BindingIds.Count - 1;
        while (bindingIndex >= 0)
        {
            UiBindingResult detached = Bindings.DetachBinding(StateScope, BindingIds[bindingIndex]);
            AppendSchedulerDiagnostics(diagnostics, detached.Diagnostics);
            bindingIndex--;
        }

        int formIndex = FormKeys.Count - 1;
        while (formIndex >= 0)
        {
            UiValidationResult removedForm = Validation.UnregisterValidationForm(FormKeys[formIndex]);
            AppendSchedulerDiagnostics(diagnostics, removedForm.Diagnostics);
            formIndex--;
        }

        IUiStateStore state = Bindings.GetBindingStateStore();
        int stateIndex = StateIds.Count - 1;
        while (stateIndex >= 0)
        {
            UiStateResult removedState = state.RemoveState(StateScope, StateIds[stateIndex], "host-shutdown");
            AppendSchedulerDiagnostics(diagnostics, removedState.Diagnostics);
            stateIndex--;
        }

        UiResult focusCleared = Focus.RegisterFocusScopes(Array.Empty<UiFocusScope>(), Array.Empty<UiFocusTarget>());
        AppendSchedulerDiagnostics(diagnostics, focusCleared.Diagnostics);
        StateIds.Clear();
        BindingIds.Clear();
        FormKeys.Clear();
        StateScope = string.Empty;
        return new UiResult { Success = !HasSchedulerErrors(diagnostics), Diagnostics = diagnostics.ToArray() };
    }

    internal void RestoreHostState(string snapshotId)
    {
        IUiStateStore state = Bindings.GetBindingStateStore();
        int index = 0;
        while (index < StateCheckpoint.Length)
        {
            state.WriteState(StateScope, StateCheckpoint[index].Key, StateCheckpoint[index].Value, "host-rollback");
            index++;
        }

        StateCheckpoint = Array.Empty<UiValueEntry>();
    }

    internal UiResult CommitHostState(string snapshotId)
    {
        StateCheckpoint = Array.Empty<UiValueEntry>();
        return new UiResult { Success = true };
    }

    internal UiResult CaptureHostState()
    {
        IUiStateStore state = Bindings.GetBindingStateStore();
        UiValueEntry[] checkpoint = new UiValueEntry[StateIds.Count];
        int index = 0;
        while (index < StateIds.Count)
        {
            UiStateResult current = state.ReadState(StateScope, StateIds[index]);
            if (!current.Success || current.Entry == null)
            {
                return new UiResult { Success = false, Diagnostics = current.Diagnostics };
            }

            checkpoint[index] = CreateValueEntry(StateIds[index], CopyValue(current.Entry.Value));
            index++;
        }

        StateCheckpoint = checkpoint;
        return new UiResult { Success = true };
    }

    internal void AppendSchedulerDiagnostics(List<UiDiagnostic> target, UiDiagnostic[]? source)
    {
        UiDiagnostic[] diagnostics = source ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length)
        {
            object? copied = new ModelNormalizer().CopyValue(diagnostics[index]);
            if (copied != null)
            {
                target.Add((UiDiagnostic)copied);
            }

            index++;
        }
    }

    internal bool HasSchedulerErrors(List<UiDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == UiDiagnosticSeverity.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal UiBindingResult ProcessEventBindings(UiApplication application, UiEventData eventData)
    {
        if (eventData.Kind == UiEventKind.Input || eventData.Kind == UiEventKind.Change || eventData.Kind == UiEventKind.Blur || eventData.Kind == UiEventKind.FocusOut)
        {
            return Bindings.ProcessBindingEvent(application.Id, eventData, "event");
        }

        return Bindings.EvaluateStateBindings(application.Id);
    }

    internal UiValidationResult ProcessEventValidation(UiApplication application, UiEventData eventData)
    {
        ModelWalker walker = WalkApplication(application);
        UiNode? target = FindNode(walker, eventData.TargetId);
        if (eventData.Kind == UiEventKind.Submit && target != null && target.Form != null)
        {
            UiValidationResult submitted = Validation.ValidateUiForm(target.Form.Key, UiValidationTrigger.Submit).GetAwaiter().GetResult();
            ApplyValidationForm(target, submitted);
            return submitted;
        }

        if (eventData.Input == null || target == null || target.Input == null)
        {
            return new UiValidationResult { Success = true, IsValid = true, CanSubmit = true };
        }

        UiValidationTrigger trigger = GetValidationTrigger(eventData.Kind);
        int nodeIndex = 0;
        while (nodeIndex < walker.Nodes.Count)
        {
            UiNode formNode = walker.Nodes[nodeIndex];
            UiForm? form = formNode.Form;
            if (form != null)
            {
                int fieldIndex = 0;
                while (fieldIndex < form.Fields.Length)
                {
                    UiField field = form.Fields[fieldIndex];
                    if (string.Equals(field.Name, target.Input.Name, StringComparison.Ordinal))
                    {
                        UiValidationResult updated = Validation.UpdateValidationFieldValue(form.Key, field.Key, eventData.Input.Value);
                        if (!updated.Success)
                        {
                            return updated;
                        }

                        UiValidationResult validated = Validation.ValidateUiField(form.Key, field.Key, trigger).GetAwaiter().GetResult();
                        ApplyValidationForm(formNode, validated);
                        return validated;
                    }

                    fieldIndex++;
                }
            }

            nodeIndex++;
        }

        return new UiValidationResult { Success = true, IsValid = true, CanSubmit = true };
    }

    internal UiCommandResult ProcessEventCommand(UiApplication application, UiEventData eventData)
    {
        UiCommandInvocation? invocation = null;
        int commandIndex = 0;
        while (commandIndex < application.Commands.Length && invocation == null)
        {
            invocation = application.Commands[commandIndex].Invocation;
            commandIndex++;
        }

        ModelWalker walker = WalkApplication(application);
        UiNode? target = FindNode(walker, eventData.TargetId);
        if (invocation == null && target != null && target.Button != null && target.Button.CommandId != null)
        {
            invocation = new UiCommandInvocation { CommandId = target.Button.CommandId, SourceNodeId = target.Identity.StableId };
        }

        if (invocation == null)
        {
            return new UiCommandResult { Success = true };
        }

        UiCommandResult result = Commands.ExecuteUiCommand(invocation);
        if (result.Success && target != null && target.Button != null)
        {
            target.Button = Commands.ProjectCommandButton(target.Button, invocation.ScopeId);
        }

        return result;
    }

    internal UiNavigationResult ProcessPendingNavigation(UiApplication application)
    {
        UiWindow? window = SelectActiveWindow(application);
        if (window == null || window.NavigationRequest == null)
        {
            return new UiNavigationResult { Success = true };
        }

        UiNavigationResult result = Router.NavigateUi(window.NavigationRequest).GetAwaiter().GetResult();
        window.NavigationResult = CopyNavigationResult(result);
        window.History = CopyHistory(result.History);
        if (result.Page != null)
        {
            window.CurrentPageKey = result.Page.Key;
        }

        window.NavigationRequest = null;
        return result;
    }

    internal UiFocusResult ProcessPendingFocus(UiApplication application)
    {
        UiWindow? window = SelectActiveWindow(application);
        if (window == null)
        {
            return new UiFocusResult { Success = true };
        }

        UiView? view = SelectCurrentView(window);
        if (view == null || view.FocusRequest == null)
        {
            object? copiedState = new ModelNormalizer().CopyValue(window.Focus);
            return new UiFocusResult { Success = true, State = copiedState == null ? new UiFocusState() : (UiFocusState)copiedState };
        }

        UiFocusResult result = Focus.ApplyFocusRequest(window.Focus, view.FocusRequest);
        if (result.Success)
        {
            object? copiedState = new ModelNormalizer().CopyValue(result.State);
            window.Focus = copiedState == null ? new UiFocusState() : (UiFocusState)copiedState;
            window.FocusResult = CopyFocusResult(result);
            view.FocusRequest = null;
        }

        return result;
    }

    internal void ApplyBindingUpdates(UiApplication application, UiBindingUpdate[]? updates)
    {
        ModelWalker walker = WalkApplication(application);
        UiBindingUpdate[] values = updates ?? Array.Empty<UiBindingUpdate>();
        int index = 0;
        while (index < values.Length)
        {
            UiBindingUpdate update = values[index];
            UiNode? node = FindNode(walker, update.TargetNodeId);
            if (node != null)
            {
                ApplyBindingUpdate(node, update);
            }

            index++;
        }
    }

    internal void ApplyBindingUpdate(UiNode node, UiBindingUpdate update)
    {
        UiValue value = CopyValue(update.Value);
        if (update.Target == "text")
        {
            node.Text = value.StringValue;
            if (node.TextContent != null)
            {
                node.TextContent.Text = value.StringValue ?? string.Empty;
            }
        }
        else if (update.Target == "value" && node.Input != null)
        {
            node.Input.Value = value;
        }
        else if (update.Target == "visibility" && value.Kind == UiValueKind.Boolean)
        {
            node.State.IsVisible = value.BooleanValue;
        }
        else if (update.Target == "enabled" && value.Kind == UiValueKind.Boolean)
        {
            node.State.IsEnabled = value.BooleanValue;
            if (node.Button != null)
            {
                node.Button.IsEnabled = value.BooleanValue;
            }
        }
        else if (update.Target == "command" && node.Button != null)
        {
            node.Button.CommandId = value.StringValue;
        }
        else if (update.Target.StartsWith("property:", StringComparison.Ordinal))
        {
            SetNodeProperty(node, update.Target.Substring(9), value);
        }
        else if (update.Target.StartsWith("attribute:", StringComparison.Ordinal))
        {
            SetNodeAttribute(node, update.Target.Substring(10), value);
        }
    }

    internal void SetNodeProperty(UiNode node, string name, UiValue value)
    {
        int index = 0;
        while (index < node.Properties.Length)
        {
            if (string.Equals(node.Properties[index].Name, name, StringComparison.Ordinal))
            {
                node.Properties[index].Value = value;
                return;
            }

            index++;
        }

        DomProperty[] properties = new DomProperty[node.Properties.Length + 1];
        Array.Copy(node.Properties, properties, node.Properties.Length);
        properties[node.Properties.Length] = new DomProperty { Name = name, Value = value };
        node.Properties = properties;
    }

    internal void SetNodeAttribute(UiNode node, string name, UiValue value)
    {
        string text = ConvertAttributeValue(value);
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (string.Equals(node.Attributes[index].Name, name, StringComparison.Ordinal))
            {
                node.Attributes[index].Value = text;
                node.Attributes[index].IsBoolean = value.Kind == UiValueKind.Boolean;
                return;
            }

            index++;
        }

        DomAttribute[] attributes = new DomAttribute[node.Attributes.Length + 1];
        Array.Copy(node.Attributes, attributes, node.Attributes.Length);
        attributes[node.Attributes.Length] = new DomAttribute { Name = name, Value = text, IsBoolean = value.Kind == UiValueKind.Boolean };
        node.Attributes = attributes;
    }

    internal string ConvertAttributeValue(UiValue value)
    {
        if (value.Kind == UiValueKind.String)
        {
            return value.StringValue ?? string.Empty;
        }

        if (value.Kind == UiValueKind.Boolean)
        {
            return value.BooleanValue ? "true" : "false";
        }

        return string.Empty;
    }

    internal void ApplyComponentResult(UiApplication application, UiComponentResult result)
    {
        if (result.Content == null || result.RenderRequests.Length == 0)
        {
            return;
        }

        string componentId = result.RenderRequests[result.RenderRequests.Length - 1].Context.ComponentId;
        int windowIndex = 0;
        while (windowIndex < application.Windows.Length)
        {
            UiWindow window = application.Windows[windowIndex];
            int pageIndex = 0;
            while (pageIndex < window.Pages.Length)
            {
                ApplyComponentToView(window.Pages[pageIndex].ShellView, componentId, result.Content);
                ApplyComponentToView(window.Pages[pageIndex].ContentView, componentId, result.Content);
                ApplyComponentToView(window.Pages[pageIndex].NotFoundView, componentId, result.Content);
                ApplyComponentToView(window.Pages[pageIndex].ErrorView, componentId, result.Content);
                ApplyComponentToView(window.Pages[pageIndex].LoadingView, componentId, result.Content);
                pageIndex++;
            }

            int overlayIndex = 0;
            while (overlayIndex < window.Overlays.Length)
            {
                ApplyComponentToView(window.Overlays[overlayIndex], componentId, result.Content);
                overlayIndex++;
            }

            windowIndex++;
        }
    }

    internal void ApplyComponentToView(UiView? view, string componentId, UiNode content)
    {
        if (view != null && view.ComponentContext != null && string.Equals(view.ComponentContext.ComponentId, componentId, StringComparison.Ordinal))
        {
            object? copied = new ModelNormalizer().CopyValue(content);
            view.ContentRoot = copied == null ? null : (UiNode)copied;
        }
    }

    internal void SetNodeEventData(UiApplication application, UiEventData eventData)
    {
        ModelWalker walker = WalkApplication(application);
        UiNode? node = FindNode(walker, eventData.TargetId);
        if (node != null)
        {
            object? copied = new ModelNormalizer().CopyValue(eventData);
            node.EventData = copied == null ? null : (UiEventData)copied;
        }
    }

    internal ModelWalker WalkApplication(UiApplication application)
    {
        int maximum = application.RenderOptions.MaximumDiagnostics;
        if (maximum <= 0)
        {
            maximum = 100;
        }

        ModelWalker walker = new ModelWalker(maximum);
        walker.ValidateApplication(application);
        return walker;
    }

    internal UiNode? FindNode(ModelWalker walker, string? nodeId)
    {
        int index = 0;
        while (index < walker.Nodes.Count)
        {
            UiNode node = walker.Nodes[index];
            if (string.Equals(node.Identity.StableId, nodeId, StringComparison.Ordinal))
            {
                return node;
            }

            index++;
        }

        return null;
    }

    internal UiWindow? SelectActiveWindow(UiApplication application)
    {
        int index = 0;
        while (index < application.Windows.Length)
        {
            UiWindow window = application.Windows[index];
            if (window.IsActive)
            {
                return window;
            }

            index++;
        }

        return null;
    }

    internal UiView? SelectCurrentView(UiWindow window)
    {
        int index = 0;
        while (index < window.Pages.Length)
        {
            UiPage page = window.Pages[index];
            if (string.Equals(page.Key, window.CurrentPageKey, StringComparison.Ordinal))
            {
                return page.ContentView;
            }

            index++;
        }

        return null;
    }

    internal UiValidationTrigger GetValidationTrigger(UiEventKind kind)
    {
        if (kind == UiEventKind.Input)
        {
            return UiValidationTrigger.Input;
        }

        if (kind == UiEventKind.Blur || kind == UiEventKind.FocusOut)
        {
            return UiValidationTrigger.Blur;
        }

        return UiValidationTrigger.Change;
    }

    internal void ApplyValidationForm(UiNode node, UiValidationResult result)
    {
        if (result.Form != null)
        {
            object? copied = new ModelNormalizer().CopyValue(result.Form);
            node.Form = copied == null ? null : (UiForm)copied;
        }
    }

    internal UiValue CopyValue(UiValue value)
    {
        object? copied = new ModelNormalizer().CopyValue(value);
        return copied == null ? new UiValue() : (UiValue)copied;
    }

    internal UiNavigationResult CopyNavigationResult(UiNavigationResult source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? new UiNavigationResult() : (UiNavigationResult)copied;
    }

    internal UiFocusResult CopyFocusResult(UiFocusResult source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? new UiFocusResult() : (UiFocusResult)copied;
    }

    internal UiHistoryEntry[] CopyHistory(UiHistoryEntry[] source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? Array.Empty<UiHistoryEntry>() : (UiHistoryEntry[])copied;
    }

    internal UiValue CreateUpdateBatchValue(UiUpdateBatch batch)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("id", CreateTextValue(batch.Id)));
        entries.Add(CreateValueEntry("baseRevision", CreateIntegerValue(batch.BaseRevision)));
        entries.Add(CreateValueEntry("revision", CreateIntegerValue(batch.Revision)));
        entries.Add(CreateValueEntry("priority", CreateIntegerValue((int)batch.Priority)));
        UiValue[] mutations = new UiValue[batch.Mutations.Length];
        int index = 0;
        while (index < batch.Mutations.Length)
        {
            mutations[index] = CreateMutationValue(batch.Mutations[index]);
            index++;
        }

        entries.Add(CreateValueEntry("mutations", CreateArrayValue(mutations)));
        entries.Add(CreateValueEntry("preservation", CreatePreservationValue(batch.Preservation)));
        return CreateObjectValue(entries);
    }

    internal UiValue CreateSnapshotValue(DomSnapshot snapshot)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("sessionId", CreateTextValue(snapshot.SessionId)));
        entries.Add(CreateValueEntry("rootSeed", CreateTextValue(snapshot.RootSeed)));
        entries.Add(CreateValueEntry("revision", CreateIntegerValue(snapshot.Revision)));
        entries.Add(CreateValueEntry("contentHash", CreateTextValue(snapshot.ContentHash)));
        UiValue[] roots = new UiValue[snapshot.Roots.Length];
        int index = 0;
        while (index < snapshot.Roots.Length)
        {
            roots[index] = CreateDomNodeValue(snapshot.Roots[index]);
            index++;
        }

        entries.Add(CreateValueEntry("roots", CreateArrayValue(roots)));
        return CreateObjectValue(entries);
    }

    internal UiValue CreateMutationValue(DomMutation mutation)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("kind", CreateIntegerValue((int)mutation.Kind)));
        entries.Add(CreateValueEntry("sequence", CreateIntegerValue(mutation.Sequence)));
        entries.Add(CreateValueEntry("targetId", CreateTextValue(mutation.TargetId)));
        if (mutation.ParentId != null)
        {
            entries.Add(CreateValueEntry("parentId", CreateTextValue(mutation.ParentId)));
        }

        if (mutation.ReferenceId != null)
        {
            entries.Add(CreateValueEntry("referenceId", CreateTextValue(mutation.ReferenceId)));
        }

        if (mutation.Node != null)
        {
            entries.Add(CreateValueEntry("node", CreateDomNodeValue(mutation.Node)));
        }

        if (mutation.Attribute != null)
        {
            entries.Add(CreateValueEntry("attribute", CreateAttributeValue(mutation.Attribute)));
        }

        if (mutation.Property != null)
        {
            entries.Add(CreateValueEntry("property", CreatePropertyValue(mutation.Property)));
        }

        if (mutation.EventSubscription != null)
        {
            entries.Add(CreateValueEntry("eventSubscription", CreateSubscriptionValue(mutation.EventSubscription)));
        }

        if (mutation.Name != null)
        {
            entries.Add(CreateValueEntry("name", CreateTextValue(mutation.Name)));
        }

        if (mutation.Value != null)
        {
            entries.Add(CreateValueEntry("value", CreateTextValue(mutation.Value)));
        }

        if (mutation.HasIndex)
        {
            entries.Add(CreateValueEntry("index", CreateIntegerValue(mutation.Index)));
        }

        if (mutation.HasFromIndex)
        {
            entries.Add(CreateValueEntry("fromIndex", CreateIntegerValue(mutation.FromIndex)));
        }

        if (mutation.HasToIndex)
        {
            entries.Add(CreateValueEntry("toIndex", CreateIntegerValue(mutation.ToIndex)));
        }

        return CreateObjectValue(entries);
    }

    internal UiValue CreateDomNodeValue(DomNode node)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("sequence", CreateIntegerValue(node.Sequence)));
        entries.Add(CreateValueEntry("kind", CreateIntegerValue((int)node.Kind)));
        entries.Add(CreateValueEntry("id", CreateTextValue(node.Id)));
        entries.Add(CreateValueEntry("key", CreateTextValue(node.Key)));
        entries.Add(CreateValueEntry("name", CreateTextValue(node.Name)));
        if (node.NamespaceUri != null)
        {
            entries.Add(CreateValueEntry("namespaceUri", CreateTextValue(node.NamespaceUri)));
        }

        if (node.Text != null)
        {
            if (node.Text.Length <= 65536)
            {
                entries.Add(CreateValueEntry("text", CreateTextValue(node.Text)));
            }
            else
            {
                entries.Add(CreateValueEntry("textChunks", CreateTextChunksValue(node.Text)));
            }
        }

        if (node.RawContentPolicyId != null)
        {
            entries.Add(CreateValueEntry("rawContentPolicyId", CreateTextValue(node.RawContentPolicyId)));
        }

        if (node.StyleScope != null)
        {
            entries.Add(CreateValueEntry("styleScope", CreateTextValue(node.StyleScope)));
        }

        if (node.HydrationMarker != null)
        {
            entries.Add(CreateValueEntry("hydrationMarker", CreateTextValue(node.HydrationMarker)));
        }

        entries.Add(CreateValueEntry("version", CreateIntegerValue(node.Version)));
        if (node.SubtreeHash != null)
        {
            entries.Add(CreateValueEntry("subtreeHash", CreateTextValue(node.SubtreeHash)));
        }

        UiValue[] attributes = new UiValue[node.Attributes.Length];
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            attributes[attributeIndex] = CreateAttributeValue(node.Attributes[attributeIndex]);
            attributeIndex++;
        }

        entries.Add(CreateValueEntry("attributes", CreateArrayValue(attributes)));
        UiValue[] properties = new UiValue[node.Properties.Length];
        int propertyIndex = 0;
        while (propertyIndex < node.Properties.Length)
        {
            properties[propertyIndex] = CreatePropertyValue(node.Properties[propertyIndex]);
            propertyIndex++;
        }

        entries.Add(CreateValueEntry("properties", CreateArrayValue(properties)));
        UiValue[] subscriptions = new UiValue[node.EventSubscriptions.Length];
        int subscriptionIndex = 0;
        while (subscriptionIndex < node.EventSubscriptions.Length)
        {
            subscriptions[subscriptionIndex] = CreateSubscriptionValue(node.EventSubscriptions[subscriptionIndex]);
            subscriptionIndex++;
        }

        entries.Add(CreateValueEntry("eventSubscriptions", CreateArrayValue(subscriptions)));
        UiValue[] children = new UiValue[node.Children.Length];
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            children[childIndex] = CreateDomNodeValue(node.Children[childIndex]);
            childIndex++;
        }

        entries.Add(CreateValueEntry("children", CreateArrayValue(children)));
        return CreateObjectValue(entries);
    }

    internal UiValue CreateTextChunksValue(string text)
    {
        int chunkCount = (text.Length + 65535) / 65536;
        UiValue[] chunks = new UiValue[chunkCount];
        int index = 0;
        while (index < chunkCount)
        {
            int offset = index * 65536;
            int length = Math.Min(65536, text.Length - offset);
            chunks[index] = CreateTextValue(text.Substring(offset, length));
            index++;
        }

        return CreateArrayValue(chunks);
    }

    internal UiValue CreateAttributeValue(DomAttribute attribute)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("name", CreateTextValue(attribute.Name)));
        entries.Add(CreateValueEntry("value", CreateTextValue(attribute.Value)));
        entries.Add(CreateValueEntry("isBoolean", CreateBooleanValue(attribute.IsBoolean)));
        if (attribute.NamespaceUri != null)
        {
            entries.Add(CreateValueEntry("namespaceUri", CreateTextValue(attribute.NamespaceUri)));
        }

        if (attribute.Prefix != null)
        {
            entries.Add(CreateValueEntry("prefix", CreateTextValue(attribute.Prefix)));
        }

        return CreateObjectValue(entries);
    }

    internal UiValue CreatePropertyValue(DomProperty property)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("name", CreateTextValue(property.Name)));
        entries.Add(CreateValueEntry("value", CopyValue(property.Value)));
        return CreateObjectValue(entries);
    }

    internal UiValue CreateSubscriptionValue(DomEventSubscription subscription)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("id", CreateTextValue(subscription.Id)));
        entries.Add(CreateValueEntry("handlerId", CreateTextValue(subscription.HandlerId)));
        entries.Add(CreateValueEntry("kind", CreateIntegerValue((int)subscription.Kind)));
        entries.Add(CreateValueEntry("eventName", CreateTextValue(subscription.EventName)));
        entries.Add(CreateValueEntry("phase", CreateIntegerValue((int)subscription.Phase)));
        entries.Add(CreateValueEntry("preventDefault", CreateBooleanValue(subscription.PreventDefault)));
        entries.Add(CreateValueEntry("stopPropagation", CreateBooleanValue(subscription.StopPropagation)));
        entries.Add(CreateValueEntry("once", CreateBooleanValue(subscription.Once)));
        entries.Add(CreateValueEntry("passive", CreateBooleanValue(subscription.Passive)));
        return CreateObjectValue(entries);
    }

    internal UiValue CreatePreservationValue(UiPreservationState preservation)
    {
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateValueEntry("preserveFocus", CreateBooleanValue(preservation.PreserveFocus)));
        entries.Add(CreateValueEntry("preserveSelection", CreateBooleanValue(preservation.PreserveSelection)));
        entries.Add(CreateValueEntry("preserveComposition", CreateBooleanValue(preservation.PreserveComposition)));
        entries.Add(CreateValueEntry("preserveScroll", CreateBooleanValue(preservation.PreserveScroll)));
        entries.Add(CreateValueEntry("focusedNodeId", CreateOptionalTextValue(preservation.FocusedNodeId)));
        entries.Add(CreateValueEntry("hasSelection", CreateBooleanValue(preservation.HasSelection)));
        entries.Add(CreateValueEntry("selectionStart", CreateIntegerValue(preservation.SelectionStart)));
        entries.Add(CreateValueEntry("selectionEnd", CreateIntegerValue(preservation.SelectionEnd)));
        entries.Add(CreateValueEntry("isComposing", CreateBooleanValue(preservation.IsComposing)));
        entries.Add(CreateValueEntry("preserveScrollAnchor", CreateBooleanValue(preservation.PreserveScrollAnchor)));
        entries.Add(CreateValueEntry("scrollAnchorNodeId", CreateOptionalTextValue(preservation.ScrollAnchorNodeId)));
        entries.Add(CreateValueEntry("preserveMediaPlayback", CreateBooleanValue(preservation.PreserveMediaPlayback)));
        entries.Add(CreateValueEntry("preserveOpenOverlays", CreateBooleanValue(preservation.PreserveOpenOverlays)));
        entries.Add(CreateValueEntry("preserveUserEditedFields", CreateBooleanValue(preservation.PreserveUserEditedFields)));
        entries.Add(CreateValueEntry("scrollOffsets", CreateValueEntryArrayValue(preservation.ScrollOffsets)));
        entries.Add(CreateValueEntry("mediaPlaybackStates", CreateValueEntryArrayValue(preservation.MediaPlaybackStates)));
        entries.Add(CreateValueEntry("userEditedFieldValues", CreateValueEntryArrayValue(preservation.UserEditedFieldValues)));
        UiValue[] overlays = new UiValue[preservation.OpenOverlayIds.Length];
        int index = 0;
        while (index < preservation.OpenOverlayIds.Length)
        {
            overlays[index] = CreateTextValue(preservation.OpenOverlayIds[index]);
            index++;
        }

        entries.Add(CreateValueEntry("openOverlayIds", CreateArrayValue(overlays)));
        return CreateObjectValue(entries);
    }

    internal UiValue CreateValueEntryArrayValue(UiValueEntry[] values)
    {
        List<UiValueEntry> copied = new List<UiValueEntry>();
        int index = 0;
        while (index < values.Length)
        {
            copied.Add(CreateValueEntry(values[index].Key, CopyValue(values[index].Value)));
            index++;
        }

        return CreateObjectValue(copied);
    }

    internal UiValueEntry CreateValueEntry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    internal UiValue CreateObjectValue(List<UiValueEntry> entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries.ToArray() };
    }

    internal UiValue CreateArrayValue(UiValue[] values)
    {
        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    internal UiValue CreateTextValue(string text)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = text };
    }

    internal UiValue CreateOptionalTextValue(string? text)
    {
        if (text == null)
        {
            return new UiValue();
        }

        return CreateTextValue(text);
    }

    internal UiValue CreateIntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    internal UiValue CreateBooleanValue(bool value)
    {
        return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value };
    }

    internal UiDiagnostic CreateSchedulerDiagnostic(string code, string message, string location)
    {
        return new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message, Location = location };
    }

    internal UiResult CreateSchedulerFailure(UiDiagnostic[] diagnostics)
    {
        object? copied = new ModelNormalizer().CopyValue(diagnostics);
        return new UiResult { Success = false, Diagnostics = copied == null ? Array.Empty<UiDiagnostic>() : (UiDiagnostic[])copied };
    }
}
