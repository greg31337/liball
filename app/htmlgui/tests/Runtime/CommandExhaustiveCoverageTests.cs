using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class CommandExhaustiveCoverageTests
{
    [Test]
    public void ReportsEveryMalformedShortcutRegistrationShape()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        UiCommand nullShortcut = Command("null-shortcut");
        nullShortcut.Shortcuts = new UiShortcut[] { null! };
        UiCommand blankShortcutId = Command("blank-shortcut-id");
        blankShortcutId.Shortcuts = new UiShortcut[] { Shortcut(" ", null, Chord("A", false)) };
        UiCommand blankScope = Command("blank-scope");
        blankScope.Shortcuts = new UiShortcut[] { Shortcut("blank-scope-key", " ", Chord("A", false)) };
        UiCommand nullChords = Command("null-chords");
        nullChords.Shortcuts = new UiShortcut[] { new UiShortcut { Id = "null-chords-key", Chords = null! } };
        UiCommand emptyChords = Command("empty-chords");
        emptyChords.Shortcuts = new UiShortcut[] { new UiShortcut { Id = "empty-chords-key" } };
        UiCommand nullChord = Command("null-chord");
        nullChord.Shortcuts = new UiShortcut[] { new UiShortcut { Id = "null-chord-key", Chords = new UiShortcutChord[] { null! } } };
        UiCommand blankKey = Command("blank-key");
        blankKey.Shortcuts = new UiShortcut[] { Shortcut("blank-key-shortcut", null, Chord(" 	", false)) };
        UiCommand duplicateId = Command("duplicate-id");
        duplicateId.Shortcuts = new UiShortcut[]
        {
            Shortcut("same", null, Chord("A", false)),
            Shortcut("same", null, Chord("B", false))
        };

        UiResult[] results = new UiResult[]
        {
            dispatcher.RegisterUiCommand(nullShortcut, handler),
            dispatcher.RegisterUiCommand(blankShortcutId, handler),
            dispatcher.RegisterUiCommand(blankScope, handler),
            dispatcher.RegisterUiCommand(nullChords, handler),
            dispatcher.RegisterUiCommand(emptyChords, handler),
            dispatcher.RegisterUiCommand(nullChord, handler),
            dispatcher.RegisterUiCommand(blankKey, handler),
            dispatcher.RegisterUiCommand(duplicateId, handler)
        };

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.All.Matches<UiResult>(item => !item.Success));
            Assert.That(results.Select(item => item.Diagnostics.Single().Code), Is.EqualTo(new string[]
            {
                "command.null-shortcut", "command.invalid-shortcut-id", "command.invalid-shortcut-scope",
                "command.empty-shortcut", "command.empty-shortcut", "command.null-shortcut-chord",
                "command.invalid-shortcut-key", "command.duplicate-shortcut-id"
            }));
        });
    }

    [Test]
    public void RefreshesDynamicStatusAcrossScopesAndRejectsNullStatusCallbacks()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        UiCommand command = Command("dynamic");
        command.StatusHandlerId = "dynamic-status";
        dispatcher.RegisterUiCommand(command, handler);
        handler.Status = new UiCommandStatus { IsEnabled = false, IsVisible = true };
        UiCommandResult left = dispatcher.QueryUiCommandStatus("dynamic", "left");
        handler.Status = new UiCommandStatus
        {
            IsEnabled = true,
            IsVisible = false,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Checked
        };
        UiCommandResult right = dispatcher.QueryUiCommandStatus("dynamic", "right");
        UiCommandResult global = dispatcher.QueryUiCommandStatus("dynamic", null);

        IUiCommandDispatcher nullDispatcher = new UiCommandDispatcher();
        UiCommand nullCommand = Command("null-status");
        nullCommand.StatusHandlerId = "null-status-handler";
        nullDispatcher.RegisterUiCommand(nullCommand, new NullStatusCommandHandler());
        UiCommandResult nullStatus = nullDispatcher.QueryUiCommandStatus("null-status", null);

        Assert.Multiple(() =>
        {
            Assert.That(left.Success, Is.True);
            Assert.That(left.Status.IsEnabled, Is.False);
            Assert.That(left.Status.IsVisible, Is.True);
            Assert.That(right.Status.IsEnabled, Is.True);
            Assert.That(right.Status.IsVisible, Is.False);
            Assert.That(right.Status.CheckState, Is.EqualTo(UiCommandCheckState.Checked));
            Assert.That(global.Status.IsVisible, Is.False);
            Assert.That(handler.StatusQueries.Select(item => item.ScopeId), Is.EqualTo(new string?[] { "left", "right", null }));
            Assert.That(nullStatus.Success, Is.False);
            Assert.That(nullStatus.Diagnostics.Single().Code, Is.EqualTo("command.null-status-result"));
        });
    }

    [Test]
    public void ClearsBusyStateAfterAnInvalidExecuteCallbackResult()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        handler.Result = new UiCommandHandlerResult
        {
            Success = true,
            HasCheckState = true,
            CheckState = (UiCommandCheckState)999
        };
        dispatcher.RegisterUiCommand(Command("toggle"), handler);

        UiCommandResult first = dispatcher.ExecuteUiCommand(Invocation("toggle", "scope"));
        handler.Result = new UiCommandHandlerResult { Success = true };
        UiCommandResult retry = dispatcher.ExecuteUiCommand(Invocation("toggle", "scope"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Diagnostics.Single().Code, Is.EqualTo("command.invalid-check-state"));
            Assert.That(first.Status.IsBusy, Is.False);
            Assert.That(retry.Success, Is.True);
            Assert.That(retry.Status.IsBusy, Is.False);
            Assert.That(handler.Executions, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void UnregisterClearsPendingShortcutsAndDisconnectsTheOldHandler()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler oldHandler = new RecordingCommandHandler();
        UiCommand oldCommand = Command("action");
        oldCommand.Shortcuts = new UiShortcut[]
        {
            Shortcut("old-sequence", null, Chord("k", true), Chord("c", true))
        };
        dispatcher.RegisterUiCommand(oldCommand, oldHandler);
        UiCommandShortcutResult pending = dispatcher.DispatchCommandShortcut(Key("k", true, false), null, null, null, false);

        UiResult removed = dispatcher.UnregisterUiCommand("action");
        RecordingCommandHandler newHandler = new RecordingCommandHandler();
        UiCommand replacement = Command("action");
        replacement.Shortcuts = new UiShortcut[] { Shortcut("new-key", null, Chord("x", true)) };
        UiResult registered = dispatcher.RegisterUiCommand(replacement, newHandler);
        UiCommandShortcutResult staleCompletion = dispatcher.DispatchCommandShortcut(Key("c", true, false), null, null, null, false);
        UiCommandShortcutResult replacementResult = dispatcher.DispatchCommandShortcut(Key("x", true, false), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Pending, Is.True);
            Assert.That(removed.Success, Is.True);
            Assert.That(registered.Success, Is.True);
            Assert.That(staleCompletion.Handled, Is.False);
            Assert.That(oldHandler.Executions, Is.Empty);
            Assert.That(replacementResult.Success, Is.True);
            Assert.That(newHandler.Executions, Has.Count.EqualTo(1));
        });
    }

    [TestCase("Esc", "Escape", false, "Escape")]
    [TestCase("Spacebar", " ", false, " ")]
    [TestCase("Return", "Enter", false, "Enter")]
    [TestCase("Left", "ArrowLeft", false, "ArrowLeft")]
    [TestCase("Right", "ArrowRight", false, "ArrowRight")]
    [TestCase("Up", "ArrowUp", false, "ArrowUp")]
    [TestCase("Down", "ArrowDown", false, "ArrowDown")]
    [TestCase("Del", "Delete", false, "Delete")]
    [TestCase("a", "a", true, "Primary+A")]
    public void MatchesEveryShortcutAlias(string configuredKey, string inputKey, bool primary, string expectedChord)
    {
        CommandCore core = new CommandCore();
        UiCommand command = Command("alias");
        command.Shortcuts = new UiShortcut[] { Shortcut("alias-key", null, Chord(configuredKey, primary)) };
        core.RegisterCommand(command);

        CommandShortcutResult result = core.ProcessCommandShortcut(Key(inputKey, primary, false), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.CommandId, Is.EqualTo("alias"));
            Assert.That(result.Chord, Is.EqualTo(expectedChord));
        });
    }

    [Test]
    public void MatchesPhysicalCodeFallbackAndExplicitSequenceReset()
    {
        CommandCore codeCore = new CommandCore();
        UiCommand codeCommand = Command("code");
        codeCommand.Shortcuts = new UiShortcut[] { Shortcut("code-key", null, Chord("q", true)) };
        codeCore.RegisterCommand(codeCommand);
        UiKeyboardData codeInput = new UiKeyboardData { Code = "KeyQ", PrimaryModifier = true };
        CommandShortcutResult code = codeCore.ProcessCommandShortcut(codeInput, null, null, null, false);

        CommandCore sequenceCore = new CommandCore();
        UiCommand sequence = Command("sequence");
        sequence.Shortcuts = new UiShortcut[]
        {
            Shortcut("three-chords", null, Chord("k", true), Chord("c", true), Chord("u", true))
        };
        sequenceCore.RegisterCommand(sequence);
        CommandShortcutResult first = sequenceCore.ProcessCommandShortcut(Key("k", true, false), null, null, null, false);
        CommandShortcutResult reset = sequenceCore.ProcessCommandShortcut(Key("c", true, false), null, null, null, true);

        Assert.Multiple(() =>
        {
            Assert.That(code.Success, Is.True);
            Assert.That(code.Chord, Is.EqualTo("Primary+Q"));
            Assert.That(first.Pending, Is.True);
            Assert.That(reset.Handled, Is.False);
            Assert.That(sequenceCore.PendingShortcutChords, Is.Empty);
        });
    }

    [Test]
    public void GivesScopedConflictsPrecedenceAndPreservesSequenceOnSuppressedRepeat()
    {
        CommandCore conflictCore = new CommandCore();
        UiCommand global = Command("global");
        global.Shortcuts = new UiShortcut[] { Shortcut("global-key", null, Chord("g", true)) };
        UiCommand scopedA = Command("scoped-a");
        scopedA.Shortcuts = new UiShortcut[] { Shortcut("scoped-a-key", "editor", Chord("g", true)) };
        UiCommand scopedB = Command("scoped-b");
        scopedB.Shortcuts = new UiShortcut[] { Shortcut("scoped-b-key", "editor", Chord("g", true)) };
        conflictCore.RegisterCommand(global);
        conflictCore.RegisterCommand(scopedA);
        conflictCore.RegisterCommand(scopedB);
        CommandShortcutResult otherScope = conflictCore.ProcessCommandShortcut(Key("g", true, false), "dialog", null, null, false);
        conflictCore.CompleteCommandExecution("global", "dialog", true, false, UiCommandCheckState.Unchecked);
        CommandShortcutResult activeScope = conflictCore.ProcessCommandShortcut(Key("g", true, false), "editor", null, null, false);

        CommandCore repeatCore = new CommandCore();
        UiCommand sequence = Command("sequence");
        sequence.Shortcuts = new UiShortcut[]
        {
            Shortcut("sequence-key", null, Chord("k", true), Chord("c", true))
        };
        repeatCore.RegisterCommand(sequence);
        repeatCore.ProcessCommandShortcut(Key("k", true, false), null, null, null, false);
        CommandShortcutResult suppressed = repeatCore.ProcessCommandShortcut(Key("c", true, true), null, null, null, false);
        CommandShortcutResult completed = repeatCore.ProcessCommandShortcut(Key("c", true, false), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(otherScope.CommandId, Is.EqualTo("global"));
            Assert.That(activeScope.Code, Is.EqualTo("command.shortcut-conflict"));
            Assert.That(activeScope.ConflictingCommandIds, Is.EqualTo(new string[] { "scoped-a", "scoped-b" }));
            Assert.That(suppressed.Suppressed, Is.True);
            Assert.That(suppressed.Pending, Is.True);
            Assert.That(suppressed.PendingChords, Is.EqualTo(new string[] { "Primary+K" }));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.CommandId, Is.EqualTo("sequence"));
        });
    }

    [Test]
    public void ProjectsUpdatedStatusAndPreservesUnknownCommandFallbacks()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        UiCommand command = Command("known");
        command.Label = "Known command";
        command.IconKey = "known-icon";
        dispatcher.RegisterUiCommand(command, new RecordingCommandHandler());
        UiButton bound = new UiButton { Kind = UiButtonKind.Command, Label = "Fallback", CommandId = "known" };
        UiButton unknown = new UiButton
        {
            Kind = UiButtonKind.Command,
            Label = "Unknown fallback",
            CommandId = "missing",
            IsEnabled = false,
            IsVisible = false,
            IsBusy = true,
            HasPressed = true,
            IsPressed = true
        };

        UiButton initial = dispatcher.ProjectCommandButton(bound, "scope");
        dispatcher.UpdateUiCommandStatus("known", "scope", new UiCommandStatus
        {
            IsEnabled = false,
            IsVisible = true,
            IsBusy = true,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Mixed
        });
        UiButton updated = dispatcher.ProjectCommandButton(bound, "scope");
        UiButton preserved = dispatcher.ProjectCommandButton(unknown, "scope");

        Assert.Multiple(() =>
        {
            Assert.That(initial.Label, Is.EqualTo("Known command"));
            Assert.That(initial.IsEnabled, Is.True);
            Assert.That(updated.IsEnabled, Is.False);
            Assert.That(updated.IsBusy, Is.True);
            Assert.That(updated.HasPressed, Is.True);
            Assert.That(updated.IsPressed, Is.True);
            Assert.That(preserved.Label, Is.EqualTo("Unknown fallback"));
            Assert.That(preserved.IsEnabled, Is.False);
            Assert.That(preserved.IsVisible, Is.False);
            Assert.That(preserved.IsBusy, Is.True);
            Assert.That(preserved.IsPressed, Is.True);
            Assert.That(unknown.Label, Is.EqualTo("Unknown fallback"));
        });
    }

    private static UiCommand Command(string id)
    {
        return new UiCommand { Id = id, Label = "Command " + id, HandlerId = id + "-handler" };
    }

    private static UiCommandInvocation Invocation(string commandId, string? scopeId)
    {
        return new UiCommandInvocation { CommandId = commandId, ScopeId = scopeId, Parameter = Text("parameter") };
    }

    private static UiShortcut Shortcut(string id, string? scopeId, params UiShortcutChord[] chords)
    {
        return new UiShortcut { Id = id, ScopeId = scopeId, Chords = chords };
    }

    private static UiShortcutChord Chord(string key, bool primary)
    {
        return new UiShortcutChord { Key = key, PrimaryModifier = primary };
    }

    private static UiKeyboardData Key(string key, bool primary, bool repeat)
    {
        return new UiKeyboardData { Key = key, PrimaryModifier = primary, Repeat = repeat };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}

public class NullStatusCommandHandler : IUiCommandHandler
{
    public UiCommandStatus QueryUiCommandStatus(UiCommandInvocation invocation)
    {
        return null!;
    }

    public UiCommandHandlerResult HandleUiCommand(UiCommandInvocation invocation)
    {
        return new UiCommandHandlerResult();
    }
}
