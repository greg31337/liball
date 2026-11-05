using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class CommandCoreTests
{
    [Test]
    public void RegistersAnIndependentCommandAndRejectsInvalidRegistrations()
    {
        CommandCore core = new CommandCore();
        UiCommand source = Command("save");
        source.Description = "Save document";
        source.IconKey = "disk";
        source.StatusHandlerId = "save-status";
        source.Status.HasCheckState = true;
        source.Status.CheckState = UiCommandCheckState.Checked;

        CommandOperationResult registered = core.RegisterCommand(source);
        source.Label = "Changed";
        source.Status.CheckState = UiCommandCheckState.Unchecked;
        CommandStatusResult retained = core.QueryCommandStatus("save", null);
        CommandOperationResult duplicate = core.RegisterCommand(Command("save"));
        UiCommand blankLabel = Command("blank-label");
        blankLabel.Label = " ";
        UiCommand blankHandler = Command("blank-handler");
        blankHandler.HandlerId = " ";
        UiCommand invalidCheck = Command("invalid-check");
        invalidCheck.Status.HasCheckState = true;
        invalidCheck.Status.CheckState = (UiCommandCheckState)999;

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(retained.Success, Is.True);
            Assert.That(retained.Status.HasCheckState, Is.True);
            Assert.That(retained.Status.CheckState, Is.EqualTo(UiCommandCheckState.Checked));
            Assert.That(duplicate.Code, Is.EqualTo("command.duplicate"));
            Assert.That(core.RegisterCommand(null).Code, Is.EqualTo("command.null"));
            Assert.That(core.RegisterCommand(new UiCommand()).Code, Is.EqualTo("command.invalid-id"));
            Assert.That(core.RegisterCommand(blankLabel).Code, Is.EqualTo("command.invalid-label"));
            Assert.That(core.RegisterCommand(blankHandler).Code, Is.EqualTo("command.invalid-handler"));
            Assert.That(core.RegisterCommand(invalidCheck).Code, Is.EqualTo("command.invalid-check-state"));
        });
    }

    [Test]
    public void MaintainsIndependentScopedStatusWithGlobalFallback()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("format"));
        core.DrainCommandChanges();
        UiCommandStatus pageStatus = new UiCommandStatus
        {
            IsEnabled = false,
            IsVisible = true,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Mixed
        };

        CommandOperationResult updated = core.UpdateCommandStatus("format", "page-a", pageStatus);
        pageStatus.CheckState = UiCommandCheckState.Unchecked;
        CommandStatusResult page = core.QueryCommandStatus("format", "page-a");
        CommandStatusResult other = core.QueryCommandStatus("format", "page-b");
        page.Status.IsEnabled = true;
        CommandStatusResult retained = core.QueryCommandStatus("format", "page-a");

        Assert.Multiple(() =>
        {
            Assert.That(updated.Success, Is.True);
            Assert.That(page.Status.IsEnabled, Is.True);
            Assert.That(page.Status.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
            Assert.That(other.Status.IsEnabled, Is.True);
            Assert.That(other.Status.HasCheckState, Is.False);
            Assert.That(retained.Status.IsEnabled, Is.False);
            Assert.That(retained.Status.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
            Assert.That(core.DrainCommandChanges().Select(item => item.Kind), Is.EqualTo(new string[] { CommandCore.StatusChange }));
        });
    }

    [Test]
    public void DoesNotNotifyForAnIdempotentStatusUpdate()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("save"));
        core.DrainCommandChanges();

        CommandOperationResult result = core.UpdateCommandStatus("save", null, new UiCommandStatus());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(core.DrainCommandChanges(), Is.Empty);
        });
    }

    [Test]
    public void BeginsExecutionWithAnIndependentParameterAndSetsBusyState()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("save"));
        core.DrainCommandChanges();
        UiCommandInvocation invocation = new UiCommandInvocation
        {
            CommandId = "save",
            ScopeId = "page",
            SourceNodeId = "save-button",
            Parameter = Text("original"),
            IsRepeat = true
        };

        CommandExecutionResult result = core.BeginCommandExecution(invocation);
        invocation.Parameter.StringValue = "mutated";
        CommandStatusResult status = core.QueryCommandStatus("save", "page");
        CommandChangeNotification change = core.DrainCommandChanges().Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Invocation, Is.Not.Null.And.Not.SameAs(invocation));
            Assert.That(result.Invocation!.Parameter.StringValue, Is.EqualTo("original"));
            Assert.That(result.Invocation.ScopeId, Is.EqualTo("page"));
            Assert.That(result.Invocation.SourceNodeId, Is.EqualTo("save-button"));
            Assert.That(result.Invocation.IsRepeat, Is.True);
            Assert.That(result.Status.IsBusy, Is.True);
            Assert.That(status.Status.IsBusy, Is.True);
            Assert.That(change.Kind, Is.EqualTo(CommandCore.ExecutionStartedChange));
            Assert.That(change.Invocation!.Parameter.StringValue, Is.EqualTo("original"));
        });
    }

    [Test]
    public void RejectsUnavailableUnknownAndRepeatedExecutions()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("active"));
        core.RegisterCommand(CommandWithStatus("disabled", false, true, false));
        core.RegisterCommand(CommandWithStatus("hidden", true, false, false));
        core.RegisterCommand(CommandWithStatus("busy", true, true, true));
        CommandExecutionResult first = core.BeginCommandExecution(Invocation("active", null));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(core.BeginCommandExecution(null).Code, Is.EqualTo("command.null-invocation"));
            Assert.That(core.BeginCommandExecution(Invocation(" ", null)).Code, Is.EqualTo("command.invalid-id"));
            Assert.That(core.BeginCommandExecution(Invocation("missing", null)).Code, Is.EqualTo("command.unknown"));
            Assert.That(core.BeginCommandExecution(Invocation("disabled", null)).Code, Is.EqualTo("command.disabled"));
            Assert.That(core.BeginCommandExecution(Invocation("hidden", null)).Code, Is.EqualTo("command.hidden"));
            Assert.That(core.BeginCommandExecution(Invocation("busy", null)).Code, Is.EqualTo("command.busy"));
            Assert.That(core.BeginCommandExecution(Invocation("active", null)).Code, Is.EqualTo("command.busy"));
        });
    }

    [Test]
    public void CompletesExecutionClearsBusyAndAppliesCheckedState()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("toggle"));
        core.DrainCommandChanges();
        core.BeginCommandExecution(Invocation("toggle", "toolbar"));

        CommandOperationResult completed = core.CompleteCommandExecution("toggle", "toolbar", true, true, UiCommandCheckState.Checked);
        CommandStatusResult status = core.QueryCommandStatus("toggle", "toolbar");
        CommandChangeNotification[] changes = core.DrainCommandChanges();

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Status.IsBusy, Is.False);
            Assert.That(completed.Status.HasCheckState, Is.True);
            Assert.That(completed.Status.CheckState, Is.EqualTo(UiCommandCheckState.Checked));
            Assert.That(status.Status.IsBusy, Is.False);
            Assert.That(changes.Select(item => item.Kind), Is.EqualTo(new string[] { CommandCore.ExecutionStartedChange, CommandCore.ExecutionCompletedChange }));
            Assert.That(changes[1].ExecutionSucceeded, Is.True);
            Assert.That(changes[1].Invocation!.ScopeId, Is.EqualTo("toolbar"));
            Assert.That(core.CompleteCommandExecution("toggle", "toolbar", true, false, UiCommandCheckState.Unchecked).Code, Is.EqualTo("command.not-executing"));
        });
    }

    [Test]
    public void AllowsTheSameCommandToExecuteConcurrentlyInDifferentScopes()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("refresh"));

        CommandExecutionResult first = core.BeginCommandExecution(Invocation("refresh", "left"));
        CommandExecutionResult second = core.BeginCommandExecution(Invocation("refresh", "right"));
        CommandStatusResult global = core.QueryCommandStatus("refresh", null);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(core.QueryCommandStatus("refresh", "left").Status.IsBusy, Is.True);
            Assert.That(core.QueryCommandStatus("refresh", "right").Status.IsBusy, Is.True);
            Assert.That(global.Status.IsBusy, Is.False);
        });
    }

    [Test]
    public void UnregisterRemovesScopedAndExecutionStateBeforeReregistration()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("save"));
        core.UpdateCommandStatus("save", "page", new UiCommandStatus { IsEnabled = false });
        core.UpdateCommandStatus("save", "active-page", new UiCommandStatus());
        core.BeginCommandExecution(Invocation("save", "active-page"));

        CommandOperationResult removed = core.UnregisterCommand("save");
        CommandStatusResult missing = core.QueryCommandStatus("save", "page");
        CommandOperationResult registered = core.RegisterCommand(Command("save"));
        CommandStatusResult cleanPage = core.QueryCommandStatus("save", "page");
        CommandExecutionResult cleanExecution = core.BeginCommandExecution(Invocation("save", "active-page"));

        Assert.Multiple(() =>
        {
            Assert.That(removed.Success, Is.True);
            Assert.That(missing.Code, Is.EqualTo("command.unknown"));
            Assert.That(registered.Success, Is.True);
            Assert.That(cleanPage.Status.IsEnabled, Is.True);
            Assert.That(cleanPage.Status.IsBusy, Is.False);
            Assert.That(cleanExecution.Success, Is.True);
            Assert.That(core.UnregisterCommand("missing").Code, Is.EqualTo("command.unknown"));
            Assert.That(core.UnregisterCommand(" ").Code, Is.EqualTo("command.invalid-id"));
        });
    }

    [Test]
    public void QueuesOrderedIndependentChangesAndDrainsThemExactlyOnce()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("save"));
        core.UpdateCommandStatus("save", "page", new UiCommandStatus { HasCheckState = true, CheckState = UiCommandCheckState.Mixed });
        core.BeginCommandExecution(Invocation("save", "page"));
        core.CompleteCommandExecution("save", "page", false, false, UiCommandCheckState.Unchecked);
        core.UnregisterCommand("save");

        CommandChangeNotification[] changes = core.DrainCommandChanges();
        changes[1].Status.CheckState = UiCommandCheckState.Checked;
        CommandChangeNotification[] empty = core.DrainCommandChanges();

        Assert.Multiple(() =>
        {
            Assert.That(changes.Select(item => item.Sequence), Is.EqualTo(new int[] { 1, 2, 3, 4, 5 }));
            Assert.That(changes.Select(item => item.Kind), Is.EqualTo(new string[]
            {
                CommandCore.RegisteredChange, CommandCore.StatusChange, CommandCore.ExecutionStartedChange,
                CommandCore.ExecutionCompletedChange, CommandCore.UnregisteredChange
            }));
            Assert.That(changes[3].ExecutionSucceeded, Is.False);
            Assert.That(empty, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidStatusCompletionScopeAndPortableValues()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(Command("save"));
        UiCommandStatus invalidStatus = new UiCommandStatus { HasCheckState = true, CheckState = (UiCommandCheckState)999 };
        UiCommandInvocation malformed = Invocation("save", null);
        malformed.Parameter = new UiValue { Kind = UiValueKind.String };

        Assert.Multiple(() =>
        {
            Assert.That(core.QueryCommandStatus(null, null).Code, Is.EqualTo("command.invalid-id"));
            Assert.That(core.QueryCommandStatus("missing", null).Code, Is.EqualTo("command.unknown"));
            Assert.That(core.UpdateCommandStatus("save", null, null).Code, Is.EqualTo("command.null-status"));
            Assert.That(core.UpdateCommandStatus("save", null, invalidStatus).Code, Is.EqualTo("command.invalid-check-state"));
            Assert.That(core.CompleteCommandExecution("save", null, true, true, (UiCommandCheckState)999).Code, Is.EqualTo("command.invalid-check-state"));
            Assert.That(core.QueryCommandStatus("save", " ").Code, Is.EqualTo("command.invalid-scope"));
            Assert.That(core.UpdateCommandStatus("save", " ", new UiCommandStatus()).Code, Is.EqualTo("command.invalid-scope"));
            Assert.That(core.BeginCommandExecution(Invocation("save", " ")).Code, Is.EqualTo("command.invalid-scope"));
            Assert.That(core.CompleteCommandExecution("save", " ", true, false, UiCommandCheckState.Unchecked).Code, Is.EqualTo("command.invalid-scope"));
            Assert.Throws<ArgumentException>(() => core.BeginCommandExecution(malformed));
        });
    }

    [Test]
    public void NormalizesRegisteredShortcutAliasesAndRejectsMalformedShortcuts()
    {
        CommandCore core = new CommandCore();
        UiCommand normalized = ShortcutCommand("normalized", Shortcut("sequence", null, false,
            Chord("Esc", false, false, false), Chord("a", true, false, true)));
        CommandOperationResult registered = core.RegisterCommand(normalized);
        UiShortcut stored = core.Commands.Single().Shortcuts.Single();
        UiCommand missingChords = ShortcutCommand("missing-chords", new UiShortcut { Id = "empty", Chords = Array.Empty<UiShortcutChord>() });
        UiCommand blankKey = ShortcutCommand("blank-key", Shortcut("blank", null, false, Chord("  ", false, false, false)));
        UiCommand duplicateIds = ShortcutCommand("duplicate-ids",
            Shortcut("same", null, false, Chord("A", false, false, false)),
            Shortcut("same", null, false, Chord("B", false, false, false)));

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(stored.Chords.Select(item => item.Key), Is.EqualTo(new string[] { "Escape", "A" }));
            Assert.That(stored.Chords[1].PrimaryModifier, Is.True);
            Assert.That(stored.Chords[1].Shift, Is.True);
            Assert.That(core.RegisterCommand(missingChords).Code, Is.EqualTo("command.empty-shortcut"));
            Assert.That(core.RegisterCommand(blankKey).Code, Is.EqualTo("command.invalid-shortcut-key"));
            Assert.That(core.RegisterCommand(duplicateIds).Code, Is.EqualTo("command.duplicate-shortcut-id"));
        });
    }

    [Test]
    public void InvokesSingleShortcutAsASemanticCommandWithIndependentContext()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(ShortcutCommand("save", Shortcut("save-shortcut", null, false, Chord("s", true, false, false))));
        UiValue parameter = Text("original");

        CommandShortcutResult result = core.ProcessCommandShortcut(Key("s", true, false, false, false), "editor", "save-button", parameter, false);
        parameter.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Pending, Is.False);
            Assert.That(result.Chord, Is.EqualTo("Primary+S"));
            Assert.That(result.CommandId, Is.EqualTo("save"));
            Assert.That(result.ShortcutId, Is.EqualTo("save-shortcut"));
            Assert.That(result.Execution.Invocation!.ScopeId, Is.EqualTo("editor"));
            Assert.That(result.Execution.Invocation.SourceNodeId, Is.EqualTo("save-button"));
            Assert.That(result.Execution.Invocation.Parameter.StringValue, Is.EqualTo("original"));
        });
    }

    [Test]
    public void AdvancesChordSequencesAndRetriesAMismatchAsANewSequence()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(ShortcutCommand("comment", Shortcut("comment-sequence", null, false,
            Chord("k", true, false, false), Chord("c", true, false, false))));
        core.RegisterCommand(ShortcutCommand("cut", Shortcut("cut-shortcut", null, false, Chord("x", true, false, false))));

        CommandShortcutResult pending = core.ProcessCommandShortcut(Key("k", true, false, false, false), null, null, null, false);
        CommandShortcutResult retried = core.ProcessCommandShortcut(Key("x", true, false, false, false), null, null, null, false);
        core.CompleteCommandExecution("cut", null, true, false, UiCommandCheckState.Unchecked);
        CommandShortcutResult pendingAgain = core.ProcessCommandShortcut(Key("k", true, false, false, false), null, null, null, false);
        CommandShortcutResult completed = core.ProcessCommandShortcut(Key("c", true, false, false, false), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Pending, Is.True);
            Assert.That(pending.PendingChords, Is.EqualTo(new string[] { "Primary+K" }));
            Assert.That(retried.CommandId, Is.EqualTo("cut"));
            Assert.That(pendingAgain.Pending, Is.True);
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.CommandId, Is.EqualTo("comment"));
            Assert.That(core.PendingShortcutChords, Is.Empty);
        });
    }

    [Test]
    public void GivesTheActiveScopePrecedenceOverAGlobalShortcut()
    {
        CommandCore scopedCore = new CommandCore();
        scopedCore.RegisterCommand(ShortcutCommand("global", Shortcut("global-shortcut", null, false, Chord("Enter", false, false, false))));
        scopedCore.RegisterCommand(ShortcutCommand("dialog", Shortcut("dialog-shortcut", "dialog-a", false, Chord("Return", false, false, false))));
        CommandShortcutResult scoped = scopedCore.ProcessCommandShortcut(Key("Enter", false, false, false, false), "dialog-a", null, null, false);

        CommandCore globalCore = new CommandCore();
        globalCore.RegisterCommand(ShortcutCommand("global", Shortcut("global-shortcut", null, false, Chord("Enter", false, false, false))));
        globalCore.RegisterCommand(ShortcutCommand("dialog", Shortcut("dialog-shortcut", "dialog-a", false, Chord("Enter", false, false, false))));
        CommandShortcutResult global = globalCore.ProcessCommandShortcut(Key("Enter", false, false, false, false), "dialog-b", null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(scoped.CommandId, Is.EqualTo("dialog"));
            Assert.That(scoped.Execution.Invocation!.ScopeId, Is.EqualTo("dialog-a"));
            Assert.That(global.CommandId, Is.EqualTo("global"));
            Assert.That(global.Execution.Invocation!.ScopeId, Is.EqualTo("dialog-b"));
        });
    }

    [Test]
    public void ReportsDeterministicExactAndPrefixShortcutConflicts()
    {
        CommandCore exactCore = new CommandCore();
        exactCore.RegisterCommand(ShortcutCommand("zeta", Shortcut("zeta-shortcut", null, false, Chord("F2", false, false, false))));
        exactCore.RegisterCommand(ShortcutCommand("alpha", Shortcut("alpha-shortcut", null, false, Chord("F2", false, false, false))));
        CommandShortcutResult exact = exactCore.ProcessCommandShortcut(Key("F2", false, false, false, false), null, null, null, false);

        CommandCore prefixCore = new CommandCore();
        prefixCore.RegisterCommand(ShortcutCommand("short", Shortcut("short-shortcut", null, false, Chord("k", true, false, false))));
        prefixCore.RegisterCommand(ShortcutCommand("long", Shortcut("long-shortcut", null, false,
            Chord("k", true, false, false), Chord("c", true, false, false))));
        CommandShortcutResult prefix = prefixCore.ProcessCommandShortcut(Key("k", true, false, false, false), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.False);
            Assert.That(exact.Handled, Is.True);
            Assert.That(exact.Code, Is.EqualTo("command.shortcut-conflict"));
            Assert.That(exact.ConflictingCommandIds, Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(prefix.Code, Is.EqualTo("command.shortcut-prefix-conflict"));
            Assert.That(prefix.ConflictingCommandIds, Is.EqualTo(new string[] { "long", "short" }));
        });
    }

    [Test]
    public void AppliesRepeatPolicyBeforeInvokingACommand()
    {
        CommandCore blockedCore = new CommandCore();
        blockedCore.RegisterCommand(ShortcutCommand("once", Shortcut("once-shortcut", null, false, Chord("ArrowDown", false, false, false))));
        CommandShortcutResult blocked = blockedCore.ProcessCommandShortcut(Key("Down", false, false, false, true), null, null, null, false);

        CommandCore allowedCore = new CommandCore();
        allowedCore.RegisterCommand(ShortcutCommand("repeat", Shortcut("repeat-shortcut", null, true, Chord("ArrowDown", false, false, false))));
        CommandShortcutResult allowed = allowedCore.ProcessCommandShortcut(Key("Down", false, false, false, true), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Success, Is.True);
            Assert.That(blocked.Suppressed, Is.True);
            Assert.That(blocked.Handled, Is.False);
            Assert.That(blockedCore.ActiveExecutions, Is.Empty);
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Handled, Is.True);
            Assert.That(allowed.Execution.Invocation!.IsRepeat, Is.True);
        });
    }

    [Test]
    public void PreservesPendingSequencesForCompositionAndResetsThemAcrossScopes()
    {
        CommandCore core = new CommandCore();
        core.RegisterCommand(ShortcutCommand("sequence", Shortcut("sequence-shortcut", null, false,
            Chord("k", true, false, false), Chord("c", true, false, false))));
        core.ProcessCommandShortcut(Key("k", true, false, false, false), "left", null, null, false);
        UiKeyboardData composingKey = Key("Process", false, false, false, false);
        composingKey.IsComposing = true;

        CommandShortcutResult composing = core.ProcessCommandShortcut(composingKey, "left", null, null, false);
        CommandShortcutResult changedScope = core.ProcessCommandShortcut(Key("c", true, false, false, false), "right", null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(composing.Suppressed, Is.True);
            Assert.That(composing.Pending, Is.True);
            Assert.That(composing.PendingChords, Is.EqualTo(new string[] { "Primary+K" }));
            Assert.That(changedScope.Handled, Is.False);
            Assert.That(core.PendingShortcutChords, Is.Empty);
        });
    }

    [Test]
    public void ReturnsCommandAvailabilityFailuresFromShortcutInvocation()
    {
        CommandCore core = new CommandCore();
        UiCommand disabled = ShortcutCommand("disabled", Shortcut("disabled-shortcut", null, false, Chord("d", true, false, false)));
        disabled.Status.IsEnabled = false;
        core.RegisterCommand(disabled);

        CommandShortcutResult result = core.ProcessCommandShortcut(Key("d", true, false, false, false), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Code, Is.EqualTo("command.disabled"));
            Assert.That(result.Execution.Status.IsEnabled, Is.False);
        });
    }

    private static UiCommand Command(string id)
    {
        return new UiCommand { Id = id, Label = "Command " + id, HandlerId = id + "-handler" };
    }

    private static UiCommand CommandWithStatus(string id, bool enabled, bool visible, bool busy)
    {
        UiCommand command = Command(id);
        command.Status = new UiCommandStatus { IsEnabled = enabled, IsVisible = visible, IsBusy = busy };
        return command;
    }

    private static UiCommand ShortcutCommand(string id, params UiShortcut[] shortcuts)
    {
        UiCommand command = Command(id);
        command.Shortcuts = shortcuts;
        return command;
    }

    private static UiShortcut Shortcut(string id, string? scopeId, bool allowRepeat, params UiShortcutChord[] chords)
    {
        return new UiShortcut { Id = id, ScopeId = scopeId, AllowRepeat = allowRepeat, Chords = chords };
    }

    private static UiShortcutChord Chord(string key, bool primary, bool alt, bool shift)
    {
        return new UiShortcutChord { Key = key, PrimaryModifier = primary, Alt = alt, Shift = shift };
    }

    private static UiKeyboardData Key(string key, bool primary, bool alt, bool shift, bool repeat)
    {
        return new UiKeyboardData { Key = key, PrimaryModifier = primary, Alt = alt, Shift = shift, Repeat = repeat };
    }

    private static UiCommandInvocation Invocation(string commandId, string? scopeId)
    {
        return new UiCommandInvocation { CommandId = commandId, ScopeId = scopeId, Parameter = Text("parameter") };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
