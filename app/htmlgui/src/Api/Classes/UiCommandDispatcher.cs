using System;
using System.Collections.Generic;

namespace htmlgui;

/// <summary>Provides deterministic command callbacks, shortcut dispatch, and control projection.</summary>
public class UiCommandDispatcher : IUiCommandDispatcher
{
    internal CommandCore Core;
    internal List<string> CommandIds;
    internal List<IUiCommandHandler> Handlers;

    /// <summary>Initializes an empty command dispatcher.</summary>
    public UiCommandDispatcher()
    {
        Core = new CommandCore();
        CommandIds = new List<string>();
        Handlers = new List<IUiCommandHandler>();
    }

    /// <summary>Registers a command and its callback handler.</summary>
    public UiResult RegisterUiCommand(UiCommand? command, IUiCommandHandler? handler)
    {
        if (handler == null)
        {
            return CreateCommandOperationFailure("command.null-handler", "A command callback handler is required.", "/command/handler");
        }

        CommandOperationResult registered = Core.RegisterCommand(command);
        if (!registered.Success || command == null)
        {
            return CreateCommandOperationFailure(registered.Code, GetCommandDiagnosticMessage(registered.Code), "/command");
        }

        CommandIds.Add(command.Id);
        Handlers.Add(handler);
        return new UiResult { Success = true };
    }

    /// <summary>Unregisters a command and its callback handler.</summary>
    public UiResult UnregisterUiCommand(string? commandId)
    {
        CommandOperationResult removed = Core.UnregisterCommand(commandId);
        if (!removed.Success)
        {
            return CreateCommandOperationFailure(removed.Code, GetCommandDiagnosticMessage(removed.Code), "/command/commandId");
        }

        int index = FindCommandHandlerIndex(commandId);
        if (index >= 0)
        {
            CommandIds.RemoveAt(index);
            Handlers.RemoveAt(index);
        }

        return new UiResult { Success = true };
    }

    /// <summary>Queries command status in an optional active scope.</summary>
    public UiCommandResult QueryUiCommandStatus(string? commandId, string? scopeId)
    {
        CommandStatusResult queried = Core.QueryCommandStatus(commandId, scopeId);
        if (!queried.Success)
        {
            return CreatePublicCommandFailure(queried.Code, "/command/status");
        }

        int commandIndex = Core.FindCommandIndex(commandId ?? string.Empty);
        UiCommand command = Core.Commands[commandIndex];
        if (command.StatusHandlerId == null)
        {
            return new UiCommandResult { Success = true, Status = Core.CopyCommandStatus(queried.Status) };
        }

        int handlerIndex = FindCommandHandlerIndex(command.Id);
        if (handlerIndex < 0)
        {
            return CreatePublicCommandFailure("command.missing-handler", "/command/handler");
        }

        UiCommandInvocation invocation = new UiCommandInvocation();
        invocation.CommandId = command.Id;
        invocation.ScopeId = NormalizePublicCommandScope(scopeId);
        invocation.Parameter = new UiValue();
        UiCommandStatus? callbackStatus = Handlers[handlerIndex].QueryUiCommandStatus(Core.CopyCommandInvocation(invocation));
        if (callbackStatus == null)
        {
            return CreatePublicCommandFailure("command.null-status-result", "/command/statusHandler");
        }

        if (!Core.IsValidCommandStatus(callbackStatus))
        {
            return CreatePublicCommandFailure("command.invalid-check-state", "/command/statusHandler/checkState");
        }

        CommandOperationResult updated = Core.UpdateCommandStatus(command.Id, scopeId, callbackStatus);
        if (!updated.Success)
        {
            return CreatePublicCommandFailure(updated.Code, "/command/status");
        }

        return new UiCommandResult { Success = true, Status = Core.CopyCommandStatus(updated.Status) };
    }

    /// <summary>Stores command status in an optional active scope.</summary>
    public UiResult UpdateUiCommandStatus(string? commandId, string? scopeId, UiCommandStatus? status)
    {
        CommandOperationResult updated = Core.UpdateCommandStatus(commandId, scopeId, status);
        if (!updated.Success)
        {
            return CreateCommandOperationFailure(updated.Code, GetCommandDiagnosticMessage(updated.Code), "/command/status");
        }

        return new UiResult { Success = true };
    }

    /// <summary>Executes a command through its registered callback.</summary>
    public UiCommandResult ExecuteUiCommand(UiCommandInvocation? invocation)
    {
        CommandExecutionResult started = Core.BeginCommandExecution(invocation);
        if (!started.Success)
        {
            return CreatePublicCommandFailureWithStatus(started.Code, "/command/invocation", started.Status);
        }

        return CompletePublicCommandExecution(started);
    }

    /// <summary>Matches normalized keyboard input and executes a complete shortcut.</summary>
    public UiCommandShortcutResult DispatchCommandShortcut(UiKeyboardData? keyboard, string? activeScopeId, string? sourceNodeId, UiValue? parameter, bool resetSequence)
    {
        CommandShortcutResult processed = Core.ProcessCommandShortcut(keyboard, activeScopeId, sourceNodeId, parameter, resetSequence);
        UiCommandShortcutResult result = new UiCommandShortcutResult();
        result.Success = processed.Success;
        result.Handled = processed.Handled;
        result.Pending = processed.Pending;
        result.Suppressed = processed.Suppressed;
        result.NormalizedKey = processed.NormalizedKey;
        result.Chord = processed.Chord;
        result.CommandId = processed.CommandId;
        result.ShortcutId = processed.ShortcutId;
        result.ConflictingCommandIds = CopyCommandStrings(processed.ConflictingCommandIds);
        result.PendingChords = CopyCommandStrings(processed.PendingChords);
        if (processed.Code.Length > 0)
        {
            result.Diagnostics = new UiDiagnostic[]
            {
                CreateCommandDiagnostic(processed.Code, GetCommandDiagnosticMessage(processed.Code), "/command/shortcut")
            };
        }

        if (processed.Execution.Success)
        {
            result.CommandResult = CompletePublicCommandExecution(processed.Execution);
            result.Success = result.CommandResult.Success;
            result.Diagnostics = CopyCommandDiagnostics(result.CommandResult.Diagnostics);
        }

        return result;
    }

    /// <summary>Returns a command-projected copy of a button.</summary>
    public UiButton ProjectCommandButton(UiButton? button, string? scopeId)
    {
        if (button == null)
        {
            throw new ArgumentNullException("button");
        }

        return ProjectCommandButtonCopy(button, scopeId);
    }

    /// <summary>Returns a command-projected copy of a menu and its nested menus.</summary>
    public UiMenu ProjectCommandMenu(UiMenu? menu, string? scopeId)
    {
        if (menu == null)
        {
            throw new ArgumentNullException("menu");
        }

        return ProjectCommandMenuCopy(menu, scopeId);
    }

    /// <summary>Returns a command-projected copy of a toolbar.</summary>
    public UiToolbar ProjectCommandToolbar(UiToolbar? toolbar, string? scopeId)
    {
        if (toolbar == null)
        {
            throw new ArgumentNullException("toolbar");
        }

        return ProjectCommandToolbarCopy(toolbar, scopeId);
    }

    internal UiCommandResult CompletePublicCommandExecution(CommandExecutionResult started)
    {
        UiCommandInvocation invocation = started.Invocation ?? new UiCommandInvocation();
        int handlerIndex = FindCommandHandlerIndex(invocation.CommandId);
        if (handlerIndex < 0)
        {
            CommandOperationResult missingCompletion = Core.CompleteCommandExecution(invocation.CommandId, invocation.ScopeId, false, false, UiCommandCheckState.Unchecked);
            return CreatePublicCommandFailureWithStatus("command.missing-handler", "/command/handler", missingCompletion.Status);
        }

        UiCommandHandlerResult? callbackResult = Handlers[handlerIndex].HandleUiCommand(Core.CopyCommandInvocation(invocation));
        if (callbackResult == null)
        {
            CommandOperationResult nullCompletion = Core.CompleteCommandExecution(invocation.CommandId, invocation.ScopeId, false, false, UiCommandCheckState.Unchecked);
            return CreatePublicCommandFailureWithStatus("command.null-handler-result", "/command/handler", nullCompletion.Status);
        }

        UiCommandHandlerResult copied = CopyCommandHandlerResult(callbackResult);
        if (copied.HasCheckState && !Core.IsValidCommandCheckState(copied.CheckState))
        {
            CommandOperationResult invalidCompletion = Core.CompleteCommandExecution(invocation.CommandId, invocation.ScopeId, false, false, UiCommandCheckState.Unchecked);
            return CreatePublicCommandFailureWithStatus("command.invalid-check-state", "/command/handler/checkState", invalidCompletion.Status);
        }

        CommandOperationResult completed = Core.CompleteCommandExecution(invocation.CommandId, invocation.ScopeId, copied.Success, copied.HasCheckState, copied.CheckState);
        UiCommandResult result = new UiCommandResult();
        result.Success = copied.Success && completed.Success;
        result.Status = Core.CopyCommandStatus(completed.Status);
        result.Invocation = Core.CopyCommandInvocation(invocation);
        result.HandlerResult = copied;
        result.Diagnostics = CopyCommandDiagnostics(copied.Diagnostics);
        return result;
    }

    internal UiButton ProjectCommandButtonCopy(UiButton source, string? scopeId)
    {
        UiButton result = new UiButton();
        result.Kind = source.Kind;
        result.Label = source.Label;
        result.CommandId = source.CommandId;
        result.IconKey = source.IconKey;
        result.IsEnabled = source.IsEnabled;
        result.IsVisible = source.IsVisible;
        result.IsBusy = source.IsBusy;
        result.HasPressed = source.HasPressed;
        result.IsPressed = source.IsPressed;
        if (source.Menu != null)
        {
            result.Menu = ProjectCommandMenuCopy(source.Menu, scopeId);
        }

        if (source.CommandId != null)
        {
            ApplyCommandToButton(result, source.CommandId, scopeId);
        }

        return result;
    }

    internal UiMenu ProjectCommandMenuCopy(UiMenu source, string? scopeId)
    {
        UiMenu result = new UiMenu();
        result.Key = source.Key;
        result.Label = source.Label;
        result.Orientation = source.Orientation;
        result.ActiveItemKey = source.ActiveItemKey;
        result.TypeAhead = new UiTypeAheadState
        {
            Buffer = source.TypeAhead.Buffer,
            MatchedKey = source.TypeAhead.MatchedKey,
            Generation = source.TypeAhead.Generation,
            IgnoreCase = source.TypeAhead.IgnoreCase
        };
        result.IsOpen = source.IsOpen;
        result.WrapFocus = source.WrapFocus;
        result.DismissOnEscape = source.DismissOnEscape;
        result.DismissOnOutsideClick = source.DismissOnOutsideClick;
        result.RestoreFocusId = source.RestoreFocusId;
        UiMenuItem[] items = source.Items ?? Array.Empty<UiMenuItem>();
        result.Items = new UiMenuItem[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            UiMenuItem? item = items[index];
            if (item == null)
            {
                throw new ArgumentException("A projected menu cannot contain a null item.", "menu");
            }

            result.Items[index] = ProjectCommandMenuItemCopy(item, scopeId);
            index++;
        }

        return result;
    }

    internal UiMenuItem ProjectCommandMenuItemCopy(UiMenuItem source, string? scopeId)
    {
        UiMenuItem result = new UiMenuItem();
        result.Kind = source.Kind;
        result.Key = source.Key;
        result.Label = source.Label;
        result.CommandId = source.CommandId;
        result.IconKey = source.IconKey;
        result.Accelerator = source.Accelerator;
        result.Mnemonic = source.Mnemonic;
        result.HasCheckState = source.HasCheckState;
        result.CheckState = source.CheckState;
        result.IsEnabled = source.IsEnabled;
        result.IsVisible = source.IsVisible;
        if (source.Submenu != null)
        {
            result.Submenu = ProjectCommandMenuCopy(source.Submenu, scopeId);
        }

        if (source.CommandId != null)
        {
            ApplyCommandToMenuItem(result, source.CommandId, scopeId);
        }

        return result;
    }

    internal UiToolbar ProjectCommandToolbarCopy(UiToolbar source, string? scopeId)
    {
        UiToolbar result = new UiToolbar();
        result.Key = source.Key;
        result.Label = source.Label;
        result.Orientation = source.Orientation;
        result.ActiveToolKey = source.ActiveToolKey;
        result.HasOverflow = source.HasOverflow;
        UiTool[] tools = source.Tools ?? Array.Empty<UiTool>();
        result.Tools = new UiTool[tools.Length];
        int index = 0;
        while (index < tools.Length)
        {
            UiTool? tool = tools[index];
            if (tool == null)
            {
                throw new ArgumentException("A projected toolbar cannot contain a null tool.", "toolbar");
            }

            result.Tools[index] = ProjectCommandToolCopy(tool, scopeId);
            index++;
        }

        return result;
    }

    internal UiTool ProjectCommandToolCopy(UiTool source, string? scopeId)
    {
        UiTool result = new UiTool();
        result.Key = source.Key;
        result.Kind = source.Kind;
        result.Label = source.Label;
        result.CommandId = source.CommandId;
        result.GroupKey = source.GroupKey;
        result.Input = source.Input;
        result.Choice = source.Choice;
        result.IsEnabled = source.IsEnabled;
        result.IsVisible = source.IsVisible;
        if (source.Button != null)
        {
            result.Button = ProjectCommandButtonCopy(source.Button, scopeId);
        }

        if (source.CommandId != null)
        {
            ApplyCommandToTool(result, source.CommandId, scopeId);
        }

        return result;
    }

    internal void ApplyCommandToButton(UiButton button, string commandId, string? scopeId)
    {
        int commandIndex = Core.FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return;
        }

        UiCommandResult queried = QueryUiCommandStatus(commandId, scopeId);
        if (!queried.Success)
        {
            return;
        }

        UiCommand command = Core.Commands[commandIndex];
        button.Label = command.Label;
        button.IconKey = command.IconKey;
        button.IsEnabled = queried.Status.IsEnabled;
        button.IsVisible = queried.Status.IsVisible;
        button.IsBusy = queried.Status.IsBusy;
        button.HasPressed = queried.Status.HasCheckState;
        button.IsPressed = queried.Status.HasCheckState && queried.Status.CheckState != UiCommandCheckState.Unchecked;
    }

    internal void ApplyCommandToMenuItem(UiMenuItem item, string commandId, string? scopeId)
    {
        int commandIndex = Core.FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return;
        }

        UiCommandResult queried = QueryUiCommandStatus(commandId, scopeId);
        if (!queried.Success)
        {
            return;
        }

        UiCommand command = Core.Commands[commandIndex];
        item.Label = command.Label;
        item.IconKey = command.IconKey;
        item.IsEnabled = queried.Status.IsEnabled;
        item.IsVisible = queried.Status.IsVisible;
        item.HasCheckState = queried.Status.HasCheckState;
        item.CheckState = queried.Status.CheckState;
        UiShortcut? shortcut = FindProjectedCommandShortcut(command, scopeId);
        if (shortcut != null)
        {
            item.Accelerator = FormatProjectedCommandShortcut(shortcut);
        }
    }

    internal void ApplyCommandToTool(UiTool tool, string commandId, string? scopeId)
    {
        int commandIndex = Core.FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return;
        }

        UiCommandResult queried = QueryUiCommandStatus(commandId, scopeId);
        if (!queried.Success)
        {
            return;
        }

        UiCommand command = Core.Commands[commandIndex];
        tool.Label = command.Label;
        tool.IsEnabled = queried.Status.IsEnabled;
        tool.IsVisible = queried.Status.IsVisible;
        if (tool.Button != null)
        {
            tool.Button.Label = command.Label;
            tool.Button.IconKey = command.IconKey;
            tool.Button.IsEnabled = queried.Status.IsEnabled;
            tool.Button.IsVisible = queried.Status.IsVisible;
            tool.Button.IsBusy = queried.Status.IsBusy;
            tool.Button.HasPressed = queried.Status.HasCheckState;
            tool.Button.IsPressed = queried.Status.HasCheckState && queried.Status.CheckState != UiCommandCheckState.Unchecked;
        }
    }

    internal UiShortcut? FindProjectedCommandShortcut(UiCommand command, string? scopeId)
    {
        string checkedScope = NormalizePublicCommandScope(scopeId) ?? string.Empty;
        UiShortcut? global = null;
        int index = 0;
        while (index < command.Shortcuts.Length)
        {
            UiShortcut shortcut = command.Shortcuts[index];
            string shortcutScope = NormalizePublicCommandScope(shortcut.ScopeId) ?? string.Empty;
            if (checkedScope.Length > 0 && ValueComparer.TextEquals(checkedScope, shortcutScope))
            {
                return shortcut;
            }

            if (shortcutScope.Length == 0 && global == null)
            {
                global = shortcut;
            }

            index++;
        }

        return global;
    }

    internal string FormatProjectedCommandShortcut(UiShortcut shortcut)
    {
        string result = string.Empty;
        int index = 0;
        while (index < shortcut.Chords.Length)
        {
            if (index > 0)
            {
                result += " ";
            }

            result += Core.FormatCommandShortcutChord(shortcut.Chords[index]);
            index++;
        }

        return result;
    }

    internal int FindCommandHandlerIndex(string? commandId)
    {
        int index = 0;
        while (commandId != null && index < CommandIds.Count)
        {
            if (ValueComparer.TextEquals(CommandIds[index], commandId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal string? NormalizePublicCommandScope(string? scopeId)
    {
        if (scopeId == null || scopeId.Length == 0)
        {
            return null;
        }

        return scopeId;
    }

    internal UiCommandHandlerResult CopyCommandHandlerResult(UiCommandHandlerResult source)
    {
        UiCommandHandlerResult result = new UiCommandHandlerResult();
        result.Success = source.Success;
        result.HasCheckState = source.HasCheckState;
        result.CheckState = source.HasCheckState ? source.CheckState : UiCommandCheckState.Unchecked;
        if (source.Value != null)
        {
            result.Value = ValueJsonCodec.CloneValue(source.Value, CommandCore.MaximumValueDepth, CommandCore.MaximumValueCollectionCount, CommandCore.MaximumValueStringLength, CommandCore.MaximumValueBinaryLength);
        }

        result.Diagnostics = CopyCommandDiagnostics(source.Diagnostics);
        return result;
    }

    internal UiDiagnostic[] CopyCommandDiagnostics(UiDiagnostic[]? source)
    {
        UiDiagnostic[] diagnostics = source ?? Array.Empty<UiDiagnostic>();
        UiDiagnostic[] result = new UiDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            UiDiagnostic? diagnostic = diagnostics[index];
            if (diagnostic == null)
            {
                result[index] = CreateCommandDiagnostic("command.null-handler-diagnostic", "A command handler returned a null diagnostic.", "/command/handler/diagnostics/" + index);
            }
            else
            {
                result[index] = CreateCommandDiagnostic(diagnostic.Code ?? string.Empty, diagnostic.Message ?? string.Empty, diagnostic.Location ?? string.Empty);
                result[index].Severity = diagnostic.Severity;
                if (diagnostic.Location == null)
                {
                    result[index].Location = null;
                }
            }

            index++;
        }

        return result;
    }

    internal string[] CopyCommandStrings(string[]? source)
    {
        string[] values = source ?? Array.Empty<string>();
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index++;
        }

        return result;
    }

    internal UiResult CreateCommandOperationFailure(string code, string message, string location)
    {
        return new UiResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[] { CreateCommandDiagnostic(code, message, location) }
        };
    }

    internal UiCommandResult CreatePublicCommandFailure(string code, string location)
    {
        return CreatePublicCommandFailureWithStatus(code, location, new UiCommandStatus());
    }

    internal UiCommandResult CreatePublicCommandFailureWithStatus(string code, string location, UiCommandStatus status)
    {
        return new UiCommandResult
        {
            Success = false,
            Status = Core.CopyCommandStatus(status),
            Diagnostics = new UiDiagnostic[] { CreateCommandDiagnostic(code, GetCommandDiagnosticMessage(code), location) }
        };
    }

    internal UiDiagnostic CreateCommandDiagnostic(string code, string message, string location)
    {
        return new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Location = location
        };
    }

    internal string GetCommandDiagnosticMessage(string code)
    {
        switch (code)
        {
            case "command.null":
                return "A command definition is required.";
            case "command.invalid-id":
                return "A stable command identifier is required.";
            case "command.duplicate":
                return "The command identifier is already registered.";
            case "command.unknown":
                return "The command is not registered.";
            case "command.disabled":
                return "The command is disabled.";
            case "command.hidden":
                return "The command is hidden.";
            case "command.busy":
                return "The command is already executing.";
            case "command.shortcut-conflict":
            case "command.shortcut-prefix-conflict":
                return "The shortcut conflicts with another active command shortcut.";
            case "command.null-keyboard":
                return "Keyboard data is required.";
            case "command.invalid-scope":
                return "The command scope is invalid.";
            case "command.null-handler-result":
                return "The command handler returned a null result.";
            case "command.null-status-result":
                return "The command status handler returned a null status.";
            case "command.missing-handler":
                return "The command callback handler is not registered.";
            case "command.invalid-check-state":
                return "The command check state is invalid.";
            default:
                return "The command operation failed validation.";
        }
    }
}
