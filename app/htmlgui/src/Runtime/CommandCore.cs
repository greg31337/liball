using System;
using System.Collections.Generic;

namespace htmlgui;

internal class CommandScopedStatus
{
    internal string CommandId = string.Empty;
    internal string ScopeId = string.Empty;
    internal UiCommandStatus Status = new UiCommandStatus();
}

internal class CommandActiveExecution
{
    internal string CommandId = string.Empty;
    internal string ScopeId = string.Empty;
    internal UiCommandInvocation Invocation = new UiCommandInvocation();
}

internal class CommandChangeNotification
{
    internal int Sequence;
    internal string Kind = string.Empty;
    internal string CommandId = string.Empty;
    internal string ScopeId = string.Empty;
    internal UiCommandStatus Status = new UiCommandStatus();
    internal UiCommandInvocation? Invocation;
    internal bool ExecutionSucceeded;
}

internal class CommandOperationResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiCommandStatus Status = new UiCommandStatus();
}

internal class CommandStatusResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiCommandStatus Status = new UiCommandStatus();
}

internal class CommandExecutionResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiCommandInvocation? Invocation;
    internal UiCommandStatus Status = new UiCommandStatus();
}

internal class CommandShortcutMatch
{
    internal string CommandId = string.Empty;
    internal string ShortcutId = string.Empty;
    internal bool AllowRepeat;
    internal bool IsComplete;
}

internal class CommandShortcutResult
{
    internal bool Success;
    internal bool Handled;
    internal bool Pending;
    internal bool Suppressed;
    internal string Code = string.Empty;
    internal string NormalizedKey = string.Empty;
    internal string Chord = string.Empty;
    internal string? CommandId;
    internal string? ShortcutId;
    internal string[] ConflictingCommandIds = Array.Empty<string>();
    internal string[] PendingChords = Array.Empty<string>();
    internal CommandExecutionResult Execution = new CommandExecutionResult();
}

internal class CommandCore
{
    internal const int MaximumValueDepth = 64;
    internal const int MaximumValueCollectionCount = 10000;
    internal const int MaximumValueStringLength = 1048576;
    internal const int MaximumValueBinaryLength = 1048576;
    internal const string RegisteredChange = "registered";
    internal const string StatusChange = "status";
    internal const string ExecutionStartedChange = "execution-started";
    internal const string ExecutionCompletedChange = "execution-completed";
    internal const string UnregisteredChange = "unregistered";

    internal List<UiCommand> Commands = new List<UiCommand>();
    internal List<CommandScopedStatus> ScopedStatuses = new List<CommandScopedStatus>();
    internal List<CommandActiveExecution> ActiveExecutions = new List<CommandActiveExecution>();
    internal List<CommandChangeNotification> Changes = new List<CommandChangeNotification>();
    internal List<string> PendingShortcutChords = new List<string>();
    internal string PendingShortcutScopeId = string.Empty;
    internal int ChangeSequence;

    internal CommandOperationResult RegisterCommand(UiCommand? command)
    {
        string validationCode = ValidateCommand(command);
        if (validationCode.Length > 0 || command == null)
        {
            return CreateCommandOperationFailure(validationCode);
        }

        if (FindCommandIndex(command.Id) >= 0)
        {
            return CreateCommandOperationFailure("command.duplicate");
        }

        UiCommand copy = CopyCommand(command);
        Commands.Add(copy);
        ResetCommandShortcutSequence();
        AddCommandChange(RegisteredChange, copy.Id, string.Empty, copy.Status, null, false);
        return new CommandOperationResult { Success = true, Status = CopyCommandStatus(copy.Status) };
    }

    internal CommandOperationResult UnregisterCommand(string? commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return CreateCommandOperationFailure("command.invalid-id");
        }

        int commandIndex = FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return CreateCommandOperationFailure("command.unknown");
        }

        UiCommand command = Commands[commandIndex];
        UiCommandStatus finalStatus = CopyCommandStatus(command.Status);
        Commands.RemoveAt(commandIndex);
        RemoveCommandScopedStatuses(commandId);
        RemoveCommandActiveExecutions(commandId);
        ResetCommandShortcutSequence();
        AddCommandChange(UnregisteredChange, commandId, string.Empty, finalStatus, null, false);
        return new CommandOperationResult { Success = true, Status = finalStatus };
    }

    internal CommandStatusResult QueryCommandStatus(string? commandId, string? scopeId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return CreateCommandStatusFailure("command.invalid-id");
        }

        int commandIndex = FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return CreateCommandStatusFailure("command.unknown");
        }

        if (IsInvalidCommandScope(scopeId))
        {
            return CreateCommandStatusFailure("command.invalid-scope");
        }

        string checkedScope = NormalizeCommandScope(scopeId);
        return new CommandStatusResult
        {
            Success = true,
            Status = GetCommandStatus(Commands[commandIndex], checkedScope)
        };
    }

    internal CommandOperationResult UpdateCommandStatus(string? commandId, string? scopeId, UiCommandStatus? status)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return CreateCommandOperationFailure("command.invalid-id");
        }

        int commandIndex = FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return CreateCommandOperationFailure("command.unknown");
        }

        if (status == null)
        {
            return CreateCommandOperationFailure("command.null-status");
        }

        if (!IsValidCommandStatus(status))
        {
            return CreateCommandOperationFailure("command.invalid-check-state");
        }

        if (IsInvalidCommandScope(scopeId))
        {
            return CreateCommandOperationFailure("command.invalid-scope");
        }

        string checkedScope = NormalizeCommandScope(scopeId);
        UiCommandStatus current = GetCommandStatus(Commands[commandIndex], checkedScope);
        UiCommandStatus normalized = CopyCommandStatus(status);
        if (!CommandStatusesEqual(current, normalized))
        {
            StoreCommandStatus(Commands[commandIndex], checkedScope, normalized);
            AddCommandChange(StatusChange, commandId, checkedScope, normalized, null, false);
        }

        return new CommandOperationResult { Success = true, Status = CopyCommandStatus(normalized) };
    }

    internal CommandExecutionResult BeginCommandExecution(UiCommandInvocation? invocation)
    {
        if (invocation == null)
        {
            return CreateCommandExecutionFailure("command.null-invocation");
        }

        if (string.IsNullOrWhiteSpace(invocation.CommandId))
        {
            return CreateCommandExecutionFailure("command.invalid-id");
        }

        int commandIndex = FindCommandIndex(invocation.CommandId);
        if (commandIndex < 0)
        {
            return CreateCommandExecutionFailure("command.unknown");
        }

        if (IsInvalidCommandScope(invocation.ScopeId))
        {
            return CreateCommandExecutionFailure("command.invalid-scope");
        }

        string checkedScope = NormalizeCommandScope(invocation.ScopeId);
        UiCommand command = Commands[commandIndex];
        UiCommandStatus status = GetCommandStatus(command, checkedScope);
        if (!status.IsVisible)
        {
            return CreateCommandExecutionFailureWithStatus("command.hidden", status);
        }

        if (!status.IsEnabled)
        {
            return CreateCommandExecutionFailureWithStatus("command.disabled", status);
        }

        if (status.IsBusy || FindActiveExecutionIndex(command.Id, checkedScope) >= 0)
        {
            return CreateCommandExecutionFailureWithStatus("command.busy", status);
        }

        UiCommandInvocation copy = CopyCommandInvocation(invocation);
        copy.ScopeId = checkedScope.Length == 0 ? null : checkedScope;
        CommandActiveExecution execution = new CommandActiveExecution();
        execution.CommandId = command.Id;
        execution.ScopeId = checkedScope;
        execution.Invocation = copy;
        ActiveExecutions.Add(execution);
        status.IsBusy = true;
        StoreCommandStatus(command, checkedScope, status);
        AddCommandChange(ExecutionStartedChange, command.Id, checkedScope, status, copy, false);
        return new CommandExecutionResult
        {
            Success = true,
            Invocation = CopyCommandInvocation(copy),
            Status = CopyCommandStatus(status)
        };
    }

    internal CommandExecutionResult InvokeSemanticCommand(string? commandId, string? scopeId, string? sourceNodeId, UiValue? parameter, bool isRepeat)
    {
        UiCommandInvocation invocation = new UiCommandInvocation();
        invocation.CommandId = commandId ?? string.Empty;
        invocation.ScopeId = scopeId;
        invocation.SourceNodeId = sourceNodeId;
        invocation.Parameter = parameter ?? new UiValue();
        invocation.IsRepeat = isRepeat;
        return BeginCommandExecution(invocation);
    }

    internal CommandShortcutResult ProcessCommandShortcut(UiKeyboardData? keyboard, string? activeScopeId, string? sourceNodeId, UiValue? parameter, bool resetSequence)
    {
        CommandShortcutResult result = new CommandShortcutResult();
        if (keyboard == null)
        {
            result.Code = "command.null-keyboard";
            return result;
        }

        if (IsInvalidCommandScope(activeScopeId))
        {
            result.Code = "command.invalid-scope";
            return result;
        }

        string checkedScope = NormalizeCommandScope(activeScopeId);
        if (resetSequence || !ValueComparer.TextEquals(PendingShortcutScopeId, checkedScope))
        {
            ResetCommandShortcutSequence();
        }

        KeyboardEngine keyboardEngine = new KeyboardEngine();
        result.NormalizedKey = keyboardEngine.NormalizeKeyboardKey(keyboard);
        if (keyboard.IsComposing || keyboardEngine.IsCompositionKey(result.NormalizedKey))
        {
            result.Success = true;
            result.Suppressed = true;
            result.Pending = PendingShortcutChords.Count > 0;
            result.PendingChords = CopyPendingCommandShortcutChords();
            return result;
        }

        if (result.NormalizedKey.Length == 0)
        {
            result.Code = "command.invalid-shortcut-key";
            ResetCommandShortcutSequence();
            return result;
        }

        result.Chord = keyboardEngine.CreateKeyboardChord(result.NormalizedKey, keyboard.PrimaryModifier, keyboard.Alt, keyboard.Shift);
        List<string> attempted = CopyPendingCommandShortcutChordList();
        attempted.Add(result.Chord);
        List<CommandShortcutMatch> matches = FindCommandShortcutMatches(attempted, checkedScope);
        if (matches.Count == 0 && PendingShortcutChords.Count > 0)
        {
            attempted.Clear();
            attempted.Add(result.Chord);
            matches = FindCommandShortcutMatches(attempted, checkedScope);
        }

        if (matches.Count == 0)
        {
            ResetCommandShortcutSequence();
            result.Success = true;
            return result;
        }

        if (keyboard.Repeat)
        {
            List<CommandShortcutMatch> repeatMatches = FilterRepeatCommandShortcutMatches(matches);
            if (repeatMatches.Count == 0)
            {
                result.Success = true;
                result.Suppressed = true;
                result.Pending = PendingShortcutChords.Count > 0;
                result.PendingChords = CopyPendingCommandShortcutChords();
                return result;
            }

            matches = repeatMatches;
        }

        int completeCount = CountCompleteCommandShortcutMatches(matches);
        if (completeCount > 1 || completeCount == 1 && completeCount < matches.Count)
        {
            result.Code = completeCount > 1 ? "command.shortcut-conflict" : "command.shortcut-prefix-conflict";
            result.Handled = true;
            result.ConflictingCommandIds = GetConflictingCommandIds(matches);
            ResetCommandShortcutSequence();
            return result;
        }

        if (completeCount == 0)
        {
            StorePendingCommandShortcutSequence(attempted, checkedScope);
            result.Success = true;
            result.Handled = true;
            result.Pending = true;
            result.PendingChords = CopyPendingCommandShortcutChords();
            return result;
        }

        CommandShortcutMatch match = GetCompleteCommandShortcutMatch(matches);
        ResetCommandShortcutSequence();
        result.Handled = true;
        result.CommandId = match.CommandId;
        result.ShortcutId = match.ShortcutId;
        result.Execution = InvokeSemanticCommand(match.CommandId, checkedScope, sourceNodeId, parameter, keyboard.Repeat);
        result.Success = result.Execution.Success;
        result.Code = result.Execution.Code;
        return result;
    }

    internal void ResetCommandShortcutSequence()
    {
        PendingShortcutChords.Clear();
        PendingShortcutScopeId = string.Empty;
    }

    internal CommandOperationResult CompleteCommandExecution(string? commandId, string? scopeId, bool executionSucceeded, bool hasCheckState, UiCommandCheckState checkState)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return CreateCommandOperationFailure("command.invalid-id");
        }

        int commandIndex = FindCommandIndex(commandId);
        if (commandIndex < 0)
        {
            return CreateCommandOperationFailure("command.unknown");
        }

        if (hasCheckState && !IsValidCommandCheckState(checkState))
        {
            return CreateCommandOperationFailure("command.invalid-check-state");
        }

        if (IsInvalidCommandScope(scopeId))
        {
            return CreateCommandOperationFailure("command.invalid-scope");
        }

        string checkedScope = NormalizeCommandScope(scopeId);
        int executionIndex = FindActiveExecutionIndex(commandId, checkedScope);
        if (executionIndex < 0)
        {
            return CreateCommandOperationFailure("command.not-executing");
        }

        CommandActiveExecution execution = ActiveExecutions[executionIndex];
        ActiveExecutions.RemoveAt(executionIndex);
        UiCommand command = Commands[commandIndex];
        UiCommandStatus status = GetCommandStatus(command, checkedScope);
        status.IsBusy = false;
        if (hasCheckState)
        {
            status.HasCheckState = true;
            status.CheckState = checkState;
        }

        StoreCommandStatus(command, checkedScope, status);
        AddCommandChange(ExecutionCompletedChange, commandId, checkedScope, status, execution.Invocation, executionSucceeded);
        return new CommandOperationResult { Success = true, Status = CopyCommandStatus(status) };
    }

    internal CommandChangeNotification[] DrainCommandChanges()
    {
        CommandChangeNotification[] result = new CommandChangeNotification[Changes.Count];
        int index = 0;
        while (index < Changes.Count)
        {
            result[index] = CopyCommandChange(Changes[index]);
            index++;
        }

        Changes.Clear();
        return result;
    }

    internal string ValidateCommand(UiCommand? command)
    {
        if (command == null)
        {
            return "command.null";
        }

        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return "command.invalid-id";
        }

        if (string.IsNullOrWhiteSpace(command.Label))
        {
            return "command.invalid-label";
        }

        if (string.IsNullOrWhiteSpace(command.HandlerId))
        {
            return "command.invalid-handler";
        }

        if (command.StatusHandlerId != null && string.IsNullOrWhiteSpace(command.StatusHandlerId))
        {
            return "command.invalid-status-handler";
        }

        if (command.Status == null)
        {
            return "command.null-status";
        }

        if (!IsValidCommandStatus(command.Status))
        {
            return "command.invalid-check-state";
        }

        return ValidateCommandShortcuts(command.Shortcuts);
    }

    internal string ValidateCommandShortcuts(UiShortcut[]? shortcuts)
    {
        UiShortcut[] source = shortcuts ?? Array.Empty<UiShortcut>();
        int shortcutIndex = 0;
        while (shortcutIndex < source.Length)
        {
            UiShortcut? shortcut = source[shortcutIndex];
            if (shortcut == null)
            {
                return "command.null-shortcut";
            }

            if (string.IsNullOrWhiteSpace(shortcut.Id))
            {
                return "command.invalid-shortcut-id";
            }

            if (IsInvalidCommandScope(shortcut.ScopeId))
            {
                return "command.invalid-shortcut-scope";
            }

            int priorIndex = 0;
            while (priorIndex < shortcutIndex)
            {
                UiShortcut? prior = source[priorIndex];
                if (prior != null && ValueComparer.TextEquals(prior.Id, shortcut.Id))
                {
                    return "command.duplicate-shortcut-id";
                }

                priorIndex++;
            }

            UiShortcutChord[]? chords = shortcut.Chords;
            if (chords == null || chords.Length == 0)
            {
                return "command.empty-shortcut";
            }

            int chordIndex = 0;
            while (chordIndex < chords.Length)
            {
                UiShortcutChord? chord = chords[chordIndex];
                if (chord == null)
                {
                    return "command.null-shortcut-chord";
                }

                if (chord.Key == null || chord.Key.Length == 0 || chord.Key != " " && chord.Key.Trim().Length == 0)
                {
                    return "command.invalid-shortcut-key";
                }

                chordIndex++;
            }

            shortcutIndex++;
        }

        return string.Empty;
    }

    internal bool IsValidCommandStatus(UiCommandStatus status)
    {
        return !status.HasCheckState || IsValidCommandCheckState(status.CheckState);
    }

    internal bool IsValidCommandCheckState(UiCommandCheckState checkState)
    {
        switch (checkState)
        {
            case UiCommandCheckState.Unchecked:
            case UiCommandCheckState.Checked:
            case UiCommandCheckState.Mixed:
                return true;
            default:
                return false;
        }
    }

    internal string NormalizeCommandScope(string? scopeId)
    {
        if (scopeId == null || scopeId.Length == 0)
        {
            return string.Empty;
        }

        return Guard.ArgumentNotWhiteSpace(scopeId, "scopeId");
    }

    internal bool IsInvalidCommandScope(string? scopeId)
    {
        return scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0;
    }

    internal int FindCommandIndex(string commandId)
    {
        int index = 0;
        while (index < Commands.Count)
        {
            if (ValueComparer.TextEquals(Commands[index].Id, commandId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindScopedStatusIndex(string commandId, string scopeId)
    {
        int index = 0;
        while (index < ScopedStatuses.Count)
        {
            CommandScopedStatus scoped = ScopedStatuses[index];
            if (ValueComparer.TextEquals(scoped.CommandId, commandId) && ValueComparer.TextEquals(scoped.ScopeId, scopeId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindActiveExecutionIndex(string commandId, string scopeId)
    {
        int index = 0;
        while (index < ActiveExecutions.Count)
        {
            CommandActiveExecution execution = ActiveExecutions[index];
            if (ValueComparer.TextEquals(execution.CommandId, commandId) && ValueComparer.TextEquals(execution.ScopeId, scopeId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiCommandStatus GetCommandStatus(UiCommand command, string scopeId)
    {
        if (scopeId.Length > 0)
        {
            int scopedIndex = FindScopedStatusIndex(command.Id, scopeId);
            if (scopedIndex >= 0)
            {
                return CopyCommandStatus(ScopedStatuses[scopedIndex].Status);
            }
        }

        return CopyCommandStatus(command.Status);
    }

    internal void StoreCommandStatus(UiCommand command, string scopeId, UiCommandStatus status)
    {
        UiCommandStatus copy = CopyCommandStatus(status);
        if (scopeId.Length == 0)
        {
            command.Status = copy;
            return;
        }

        int index = FindScopedStatusIndex(command.Id, scopeId);
        if (index < 0)
        {
            ScopedStatuses.Add(new CommandScopedStatus { CommandId = command.Id, ScopeId = scopeId, Status = copy });
        }
        else
        {
            ScopedStatuses[index].Status = copy;
        }
    }

    internal bool CommandStatusesEqual(UiCommandStatus left, UiCommandStatus right)
    {
        return left.IsEnabled == right.IsEnabled && left.IsVisible == right.IsVisible && left.IsBusy == right.IsBusy && left.HasCheckState == right.HasCheckState && left.CheckState == right.CheckState;
    }

    internal List<CommandShortcutMatch> FindCommandShortcutMatches(List<string> attempted, string activeScopeId)
    {
        List<CommandShortcutMatch> scoped = new List<CommandShortcutMatch>();
        List<CommandShortcutMatch> global = new List<CommandShortcutMatch>();
        int commandIndex = 0;
        while (commandIndex < Commands.Count)
        {
            UiCommand command = Commands[commandIndex];
            UiShortcut[] shortcuts = command.Shortcuts ?? Array.Empty<UiShortcut>();
            int shortcutIndex = 0;
            while (shortcutIndex < shortcuts.Length)
            {
                UiShortcut? shortcut = shortcuts[shortcutIndex];
                if (shortcut != null && IsCommandShortcutPrefix(shortcut, attempted))
                {
                    string shortcutScope = NormalizeCommandScope(shortcut.ScopeId);
                    CommandShortcutMatch match = new CommandShortcutMatch();
                    match.CommandId = command.Id;
                    match.ShortcutId = shortcut.Id;
                    match.AllowRepeat = shortcut.AllowRepeat;
                    match.IsComplete = shortcut.Chords.Length == attempted.Count;
                    if (activeScopeId.Length > 0 && ValueComparer.TextEquals(shortcutScope, activeScopeId))
                    {
                        scoped.Add(match);
                    }
                    else if (shortcutScope.Length == 0)
                    {
                        global.Add(match);
                    }
                }

                shortcutIndex++;
            }

            commandIndex++;
        }

        return scoped.Count > 0 ? scoped : global;
    }

    internal bool IsCommandShortcutPrefix(UiShortcut shortcut, List<string> attempted)
    {
        UiShortcutChord[] chords = shortcut.Chords ?? Array.Empty<UiShortcutChord>();
        if (attempted.Count > chords.Length)
        {
            return false;
        }

        int index = 0;
        while (index < attempted.Count)
        {
            UiShortcutChord? chord = chords[index];
            if (chord == null || !ValueComparer.TextEquals(FormatCommandShortcutChord(chord), attempted[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal string FormatCommandShortcutChord(UiShortcutChord chord)
    {
        return new KeyboardEngine().CreateKeyboardChord(chord.Key, chord.PrimaryModifier, chord.Alt, chord.Shift);
    }

    internal List<CommandShortcutMatch> FilterRepeatCommandShortcutMatches(List<CommandShortcutMatch> matches)
    {
        List<CommandShortcutMatch> result = new List<CommandShortcutMatch>();
        int index = 0;
        while (index < matches.Count)
        {
            if (matches[index].AllowRepeat)
            {
                result.Add(matches[index]);
            }

            index++;
        }

        return result;
    }

    internal int CountCompleteCommandShortcutMatches(List<CommandShortcutMatch> matches)
    {
        int count = 0;
        int index = 0;
        while (index < matches.Count)
        {
            if (matches[index].IsComplete)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal CommandShortcutMatch GetCompleteCommandShortcutMatch(List<CommandShortcutMatch> matches)
    {
        int index = 0;
        while (index < matches.Count)
        {
            if (matches[index].IsComplete)
            {
                return matches[index];
            }

            index++;
        }

        throw new InvalidOperationException("A complete command shortcut match was required.");
    }

    internal string[] GetConflictingCommandIds(List<CommandShortcutMatch> matches)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < matches.Count)
        {
            string commandId = matches[index].CommandId;
            if (!ContainsCommandId(result, commandId))
            {
                result.Add(commandId);
            }

            index++;
        }

        result.Sort(StringComparer.Ordinal);
        return result.ToArray();
    }

    internal bool ContainsCommandId(List<string> commandIds, string commandId)
    {
        int index = 0;
        while (index < commandIds.Count)
        {
            if (ValueComparer.TextEquals(commandIds[index], commandId))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal List<string> CopyPendingCommandShortcutChordList()
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < PendingShortcutChords.Count)
        {
            result.Add(PendingShortcutChords[index]);
            index++;
        }

        return result;
    }

    internal string[] CopyPendingCommandShortcutChords()
    {
        return CopyPendingCommandShortcutChordList().ToArray();
    }

    internal void StorePendingCommandShortcutSequence(List<string> chords, string scopeId)
    {
        PendingShortcutChords.Clear();
        int index = 0;
        while (index < chords.Count)
        {
            PendingShortcutChords.Add(chords[index]);
            index++;
        }

        PendingShortcutScopeId = scopeId;
    }

    internal UiCommand CopyCommand(UiCommand source)
    {
        UiCommand result = new UiCommand();
        result.Id = source.Id;
        result.Label = source.Label;
        result.Description = source.Description;
        result.IconKey = source.IconKey;
        result.HandlerId = source.HandlerId;
        result.StatusHandlerId = source.StatusHandlerId;
        result.Shortcuts = CopyCommandShortcuts(source.Shortcuts);
        result.Status = CopyCommandStatus(source.Status);
        if (source.Invocation != null)
        {
            result.Invocation = CopyCommandInvocation(source.Invocation);
        }

        return result;
    }

    internal UiShortcut[] CopyCommandShortcuts(UiShortcut[]? shortcuts)
    {
        UiShortcut[] source = shortcuts ?? Array.Empty<UiShortcut>();
        UiShortcut[] result = new UiShortcut[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiShortcut? shortcut = source[index];
            if (shortcut == null)
            {
                throw new ArgumentException("Command shortcuts cannot contain null.", "shortcuts");
            }

            UiShortcut copy = new UiShortcut();
            copy.Id = shortcut.Id ?? string.Empty;
            copy.ScopeId = shortcut.ScopeId;
            copy.AllowRepeat = shortcut.AllowRepeat;
            copy.Chords = CopyCommandChords(shortcut.Chords);
            result[index] = copy;
            index++;
        }

        return result;
    }

    internal UiShortcutChord[] CopyCommandChords(UiShortcutChord[]? chords)
    {
        UiShortcutChord[] source = chords ?? Array.Empty<UiShortcutChord>();
        UiShortcutChord[] result = new UiShortcutChord[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiShortcutChord? chord = source[index];
            if (chord == null)
            {
                throw new ArgumentException("Command shortcut chords cannot contain null.", "chords");
            }

            result[index] = new UiShortcutChord
            {
                Key = NormalizeCommandShortcutKey(chord),
                PrimaryModifier = chord.PrimaryModifier,
                Alt = chord.Alt,
                Shift = chord.Shift
            };
            index++;
        }

        return result;
    }

    internal string NormalizeCommandShortcutKey(UiShortcutChord chord)
    {
        UiKeyboardData keyboard = new UiKeyboardData();
        keyboard.Key = chord.Key ?? string.Empty;
        keyboard.PrimaryModifier = chord.PrimaryModifier;
        keyboard.Alt = chord.Alt;
        keyboard.Shift = chord.Shift;
        return new KeyboardEngine().NormalizeKeyboardKey(keyboard);
    }

    internal UiCommandStatus CopyCommandStatus(UiCommandStatus source)
    {
        UiCommandStatus result = new UiCommandStatus();
        result.IsEnabled = source.IsEnabled;
        result.IsVisible = source.IsVisible;
        result.IsBusy = source.IsBusy;
        result.HasCheckState = source.HasCheckState;
        result.CheckState = source.HasCheckState ? source.CheckState : UiCommandCheckState.Unchecked;
        return result;
    }

    internal UiCommandInvocation CopyCommandInvocation(UiCommandInvocation source)
    {
        UiCommandInvocation result = new UiCommandInvocation();
        result.CommandId = source.CommandId;
        result.ScopeId = source.ScopeId;
        result.SourceNodeId = source.SourceNodeId;
        result.Parameter = ValueJsonCodec.CloneValue(source.Parameter, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength);
        result.IsRepeat = source.IsRepeat;
        return result;
    }

    internal void AddCommandChange(string kind, string commandId, string scopeId, UiCommandStatus status, UiCommandInvocation? invocation, bool executionSucceeded)
    {
        if (ChangeSequence == int.MaxValue)
        {
            throw new InvalidOperationException("The command change sequence is exhausted.");
        }

        ChangeSequence++;
        CommandChangeNotification change = new CommandChangeNotification();
        change.Sequence = ChangeSequence;
        change.Kind = kind;
        change.CommandId = commandId;
        change.ScopeId = scopeId;
        change.Status = CopyCommandStatus(status);
        change.ExecutionSucceeded = executionSucceeded;
        if (invocation != null)
        {
            change.Invocation = CopyCommandInvocation(invocation);
        }

        Changes.Add(change);
    }

    internal CommandChangeNotification CopyCommandChange(CommandChangeNotification source)
    {
        CommandChangeNotification result = new CommandChangeNotification();
        result.Sequence = source.Sequence;
        result.Kind = source.Kind;
        result.CommandId = source.CommandId;
        result.ScopeId = source.ScopeId;
        result.Status = CopyCommandStatus(source.Status);
        result.ExecutionSucceeded = source.ExecutionSucceeded;
        if (source.Invocation != null)
        {
            result.Invocation = CopyCommandInvocation(source.Invocation);
        }

        return result;
    }

    internal void RemoveCommandScopedStatuses(string commandId)
    {
        int index = ScopedStatuses.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(ScopedStatuses[index].CommandId, commandId))
            {
                ScopedStatuses.RemoveAt(index);
            }

            index--;
        }
    }

    internal void RemoveCommandActiveExecutions(string commandId)
    {
        int index = ActiveExecutions.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(ActiveExecutions[index].CommandId, commandId))
            {
                ActiveExecutions.RemoveAt(index);
            }

            index--;
        }
    }

    internal CommandOperationResult CreateCommandOperationFailure(string code)
    {
        return new CommandOperationResult { Success = false, Code = code };
    }

    internal CommandStatusResult CreateCommandStatusFailure(string code)
    {
        return new CommandStatusResult { Success = false, Code = code };
    }

    internal CommandExecutionResult CreateCommandExecutionFailure(string code)
    {
        return new CommandExecutionResult { Success = false, Code = code };
    }

    internal CommandExecutionResult CreateCommandExecutionFailureWithStatus(string code, UiCommandStatus status)
    {
        return new CommandExecutionResult { Success = false, Code = code, Status = CopyCommandStatus(status) };
    }
}
