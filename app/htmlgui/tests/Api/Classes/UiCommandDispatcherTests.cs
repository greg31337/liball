using System;
using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiCommandDispatcherTests
{
    [Test]
    public void RegistersAndExecutesThroughThePublicInterfaces()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        handler.Result = new UiCommandHandlerResult
        {
            Success = true,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Checked,
            Value = Text("callback")
        };
        UiCommand command = Command("save");
        UiResult registered = dispatcher.RegisterUiCommand(command, handler);
        command.Label = "Changed";
        UiCommandInvocation invocation = new UiCommandInvocation
        {
            CommandId = "save",
            ScopeId = "editor",
            SourceNodeId = "save-button",
            Parameter = Text("original")
        };

        UiCommandResult result = dispatcher.ExecuteUiCommand(invocation);
        invocation.Parameter.StringValue = "changed";
        handler.Result.Value!.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Invocation!.Parameter.StringValue, Is.EqualTo("original"));
            Assert.That(result.Invocation.ScopeId, Is.EqualTo("editor"));
            Assert.That(result.Invocation.SourceNodeId, Is.EqualTo("save-button"));
            Assert.That(result.HandlerResult, Is.Not.SameAs(handler.Result));
            Assert.That(result.HandlerResult!.Value!.StringValue, Is.EqualTo("callback"));
            Assert.That(result.Status.IsBusy, Is.False);
            Assert.That(result.Status.CheckState, Is.EqualTo(UiCommandCheckState.Checked));
            Assert.That(handler.Executions.Single().Parameter.StringValue, Is.EqualTo("original"));
        });
    }

    [Test]
    public void QueriesStatusCallbacksAndStoresIndependentScopedStatus()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        handler.Status = new UiCommandStatus
        {
            IsEnabled = false,
            IsVisible = true,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Mixed
        };
        UiCommand command = Command("format");
        command.StatusHandlerId = "format-status";
        dispatcher.RegisterUiCommand(command, handler);

        UiCommandResult queried = dispatcher.QueryUiCommandStatus("format", "selection");
        handler.Status.CheckState = UiCommandCheckState.Unchecked;
        queried.Status.IsEnabled = true;
        UiCommandResult retained = dispatcher.QueryUiCommandStatus("format", "selection");

        Assert.Multiple(() =>
        {
            Assert.That(queried.Success, Is.True);
            Assert.That(queried.Status.IsEnabled, Is.True);
            Assert.That(queried.Status.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
            Assert.That(retained.Status.IsEnabled, Is.False);
            Assert.That(retained.Status.CheckState, Is.EqualTo(UiCommandCheckState.Unchecked));
            Assert.That(handler.StatusQueries.Select(item => item.ScopeId), Is.EqualTo(new string?[] { "selection", "selection" }));
        });
    }

    [Test]
    public void DispatchesSequencesAndCompletesTheMatchedCallback()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        UiCommand command = Command("comment");
        command.Shortcuts = new UiShortcut[]
        {
            Shortcut("comment-shortcut", null, false, Chord("k", true), Chord("c", true))
        };
        dispatcher.RegisterUiCommand(command, handler);

        UiCommandShortcutResult pending = dispatcher.DispatchCommandShortcut(Key("k", true, false), "editor", "surface", Text("selection"), false);
        UiCommandShortcutResult completed = dispatcher.DispatchCommandShortcut(Key("c", true, false), "editor", "surface", Text("selection"), false);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Success, Is.True);
            Assert.That(pending.Pending, Is.True);
            Assert.That(pending.PendingChords, Is.EqualTo(new string[] { "Primary+K" }));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Handled, Is.True);
            Assert.That(completed.CommandId, Is.EqualTo("comment"));
            Assert.That(completed.ShortcutId, Is.EqualTo("comment-shortcut"));
            Assert.That(completed.CommandResult!.Status.IsBusy, Is.False);
            Assert.That(handler.Executions.Single().ScopeId, Is.EqualTo("editor"));
            Assert.That(handler.Executions.Single().SourceNodeId, Is.EqualTo("surface"));
            Assert.That(handler.Executions.Single().Parameter.StringValue, Is.EqualTo("selection"));
        });
    }

    [Test]
    public void ReportsShortcutConflictsAndRepeatSuppression()
    {
        IUiCommandDispatcher conflictDispatcher = new UiCommandDispatcher();
        UiCommand alpha = Command("alpha");
        alpha.Shortcuts = new UiShortcut[] { Shortcut("alpha-key", null, false, Chord("F2", false)) };
        UiCommand zeta = Command("zeta");
        zeta.Shortcuts = new UiShortcut[] { Shortcut("zeta-key", null, false, Chord("F2", false)) };
        conflictDispatcher.RegisterUiCommand(zeta, new RecordingCommandHandler());
        conflictDispatcher.RegisterUiCommand(alpha, new RecordingCommandHandler());
        UiCommandShortcutResult conflict = conflictDispatcher.DispatchCommandShortcut(Key("F2", false, false), null, null, null, false);

        IUiCommandDispatcher repeatDispatcher = new UiCommandDispatcher();
        UiCommand once = Command("once");
        once.Shortcuts = new UiShortcut[] { Shortcut("once-key", null, false, Chord("Down", false)) };
        RecordingCommandHandler repeatHandler = new RecordingCommandHandler();
        repeatDispatcher.RegisterUiCommand(once, repeatHandler);
        UiCommandShortcutResult repeated = repeatDispatcher.DispatchCommandShortcut(Key("ArrowDown", false, true), null, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(conflict.Success, Is.False);
            Assert.That(conflict.Diagnostics.Single().Code, Is.EqualTo("command.shortcut-conflict"));
            Assert.That(conflict.ConflictingCommandIds, Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(repeated.Success, Is.True);
            Assert.That(repeated.Suppressed, Is.True);
            Assert.That(repeatHandler.Executions, Is.Empty);
        });
    }

    [Test]
    public void CompletesFailedAndMalformedCallbacksWithoutLeavingCommandsBusy()
    {
        IUiCommandDispatcher failedDispatcher = new UiCommandDispatcher();
        RecordingCommandHandler failedHandler = new RecordingCommandHandler();
        failedHandler.Result = new UiCommandHandlerResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic { Severity = UiDiagnosticSeverity.Warning, Code = "save.failed", Message = "Save failed", Location = "/save" }
            }
        };
        failedDispatcher.RegisterUiCommand(Command("save"), failedHandler);
        UiCommandResult failed = failedDispatcher.ExecuteUiCommand(Invocation("save"));
        failedHandler.Result.Diagnostics[0].Message = "changed";
        UiCommandResult retried = failedDispatcher.ExecuteUiCommand(Invocation("save"));

        IUiCommandDispatcher nullDispatcher = new UiCommandDispatcher();
        NullCommandHandler nullHandler = new NullCommandHandler();
        nullDispatcher.RegisterUiCommand(Command("null"), nullHandler);
        UiCommandResult nullResult = nullDispatcher.ExecuteUiCommand(Invocation("null"));
        UiCommandResult afterNull = nullDispatcher.ExecuteUiCommand(Invocation("null"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Diagnostics.Single().Message, Is.EqualTo("Save failed"));
            Assert.That(failed.Status.IsBusy, Is.False);
            Assert.That(retried.Diagnostics.Single().Code, Is.EqualTo("save.failed"));
            Assert.That(nullResult.Diagnostics.Single().Code, Is.EqualTo("command.null-handler-result"));
            Assert.That(afterNull.Diagnostics.Single().Code, Is.EqualTo("command.null-handler-result"));
        });
    }

    [Test]
    public void ReturnsExceptionlessOperationAndStatusFailures()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler handler = new RecordingCommandHandler();
        UiCommand registeredCommand = Command("registered");
        dispatcher.RegisterUiCommand(registeredCommand, handler);
        UiCommand invalidCheck = Command("invalid-check");
        invalidCheck.Status.HasCheckState = true;
        invalidCheck.Status.CheckState = (UiCommandCheckState)999;

        UiResult[] failures = new UiResult[]
        {
            dispatcher.RegisterUiCommand(null, handler),
            dispatcher.RegisterUiCommand(Command("missing-handler"), null),
            dispatcher.RegisterUiCommand(registeredCommand, handler),
            dispatcher.RegisterUiCommand(invalidCheck, handler),
            dispatcher.UpdateUiCommandStatus("missing", null, new UiCommandStatus()),
            dispatcher.UpdateUiCommandStatus("registered", null, null),
            dispatcher.UnregisterUiCommand("missing")
        };
        UiCommandResult unknown = dispatcher.QueryUiCommandStatus("missing", null);
        UiCommandResult removedExecution = dispatcher.ExecuteUiCommand(Invocation("missing"));
        UiResult removed = dispatcher.UnregisterUiCommand("registered");

        Assert.Multiple(() =>
        {
            Assert.That(failures, Has.All.Matches<UiResult>(item => !item.Success));
            Assert.That(failures.Select(item => item.Diagnostics.Single().Code), Is.EqualTo(new string[]
            {
                "command.null", "command.null-handler", "command.duplicate", "command.invalid-check-state",
                "command.unknown", "command.null-status", "command.unknown"
            }));
            Assert.That(unknown.Diagnostics.Single().Code, Is.EqualTo("command.unknown"));
            Assert.That(removedExecution.Diagnostics.Single().Code, Is.EqualTo("command.unknown"));
            Assert.That(removed.Success, Is.True);
            Assert.That(dispatcher.ExecuteUiCommand(Invocation("registered")).Diagnostics.Single().Code, Is.EqualTo("command.unknown"));
        });
    }

    [Test]
    public void ProjectsCommandMetadataAndScopedStatusOntoButtons()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        UiCommand command = Command("toggle");
        command.Label = "Pinned";
        command.IconKey = "pin";
        dispatcher.RegisterUiCommand(command, new RecordingCommandHandler());
        dispatcher.UpdateUiCommandStatus("toggle", "toolbar", new UiCommandStatus
        {
            IsEnabled = false,
            IsVisible = true,
            IsBusy = true,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Checked
        });
        UiButton source = new UiButton { Kind = UiButtonKind.Command, Label = "Fallback", CommandId = "toggle", IconKey = "old" };

        UiButton result = dispatcher.ProjectCommandButton(source, "toolbar");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result.Label, Is.EqualTo("Pinned"));
            Assert.That(result.IconKey, Is.EqualTo("pin"));
            Assert.That(result.IsEnabled, Is.False);
            Assert.That(result.IsVisible, Is.True);
            Assert.That(result.IsBusy, Is.True);
            Assert.That(result.HasPressed, Is.True);
            Assert.That(result.IsPressed, Is.True);
            Assert.That(source.Label, Is.EqualTo("Fallback"));
            Assert.That(source.IsBusy, Is.False);
        });
    }

    [Test]
    public void ProjectsNestedMenusAndScopePreferredAccelerators()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        UiCommand command = Command("save");
        command.Label = "Save document";
        command.IconKey = "disk";
        command.Shortcuts = new UiShortcut[]
        {
            Shortcut("global", null, false, Chord("s", true)),
            Shortcut("editor", "editor", false, Chord("k", true), Chord("s", true))
        };
        command.Status = new UiCommandStatus { IsEnabled = false, IsVisible = true, HasCheckState = true, CheckState = UiCommandCheckState.Mixed };
        dispatcher.RegisterUiCommand(command, new RecordingCommandHandler());
        UiMenu source = new UiMenu
        {
            Key = "root",
            Items = new UiMenuItem[]
            {
                new UiMenuItem
                {
                    Kind = UiMenuItemKind.Submenu,
                    Key = "file",
                    Label = "File",
                    Submenu = new UiMenu
                    {
                        Key = "file-menu",
                        Items = new UiMenuItem[]
                        {
                            new UiMenuItem { Kind = UiMenuItemKind.Command, Key = "save", Label = "Fallback", CommandId = "save" }
                        }
                    }
                }
            }
        };

        UiMenu result = dispatcher.ProjectCommandMenu(source, "editor");
        UiMenuItem projected = result.Items[0].Submenu!.Items[0];

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result.Items[0].Submenu, Is.Not.SameAs(source.Items[0].Submenu));
            Assert.That(projected.Label, Is.EqualTo("Save document"));
            Assert.That(projected.IconKey, Is.EqualTo("disk"));
            Assert.That(projected.Accelerator, Is.EqualTo("Primary+K Primary+S"));
            Assert.That(projected.IsEnabled, Is.False);
            Assert.That(projected.HasCheckState, Is.True);
            Assert.That(projected.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
            Assert.That(source.Items[0].Submenu!.Items[0].Label, Is.EqualTo("Fallback"));
        });
    }

    [Test]
    public void ProjectsToolbarToolsAndEmbeddedButtons()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        UiCommand command = Command("refresh");
        command.Label = "Refresh data";
        command.IconKey = "refresh-icon";
        command.Status = new UiCommandStatus { IsEnabled = true, IsVisible = false, IsBusy = true };
        dispatcher.RegisterUiCommand(command, new RecordingCommandHandler());
        UiToolbar source = new UiToolbar
        {
            Key = "main",
            Tools = new UiTool[]
            {
                new UiTool
                {
                    Key = "refresh-tool",
                    Kind = "command",
                    Label = "Fallback",
                    CommandId = "refresh",
                    Button = new UiButton { Kind = UiButtonKind.Command, Label = "Button fallback" }
                }
            }
        };

        UiToolbar result = dispatcher.ProjectCommandToolbar(source, null);
        UiTool tool = result.Tools.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(tool.Label, Is.EqualTo("Refresh data"));
            Assert.That(tool.IsEnabled, Is.True);
            Assert.That(tool.IsVisible, Is.False);
            Assert.That(tool.Button, Is.Not.Null.And.Not.SameAs(source.Tools[0].Button));
            Assert.That(tool.Button!.Label, Is.EqualTo("Refresh data"));
            Assert.That(tool.Button.IconKey, Is.EqualTo("refresh-icon"));
            Assert.That(tool.Button.IsBusy, Is.True);
            Assert.That(tool.Button.IsVisible, Is.False);
            Assert.That(source.Tools[0].Label, Is.EqualTo("Fallback"));
        });
    }

    [Test]
    public void RejectsNullProjectionModelsAndInvalidStatusCallbacks()
    {
        IUiCommandDispatcher dispatcher = new UiCommandDispatcher();
        RecordingCommandHandler invalid = new RecordingCommandHandler();
        invalid.Status = new UiCommandStatus { HasCheckState = true, CheckState = (UiCommandCheckState)999 };
        UiCommand command = Command("status");
        command.StatusHandlerId = "status-handler";
        dispatcher.RegisterUiCommand(command, invalid);

        UiCommandResult status = dispatcher.QueryUiCommandStatus("status", null);

        Assert.Multiple(() =>
        {
            Assert.That(status.Success, Is.False);
            Assert.That(status.Diagnostics.Single().Code, Is.EqualTo("command.invalid-check-state"));
            Assert.That(() => dispatcher.ProjectCommandButton(null, null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("button"));
            Assert.That(() => dispatcher.ProjectCommandMenu(null, null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("menu"));
            Assert.That(() => dispatcher.ProjectCommandToolbar(null, null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("toolbar"));
        });
    }

    private static UiCommand Command(string id)
    {
        return new UiCommand { Id = id, Label = "Command " + id, HandlerId = id + "-handler" };
    }

    private static UiCommandInvocation Invocation(string commandId)
    {
        return new UiCommandInvocation { CommandId = commandId, Parameter = Text("parameter") };
    }

    private static UiShortcut Shortcut(string id, string? scopeId, bool allowRepeat, params UiShortcutChord[] chords)
    {
        return new UiShortcut { Id = id, ScopeId = scopeId, AllowRepeat = allowRepeat, Chords = chords };
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

public class RecordingCommandHandler : IUiCommandHandler
{
    public UiCommandStatus Status = new UiCommandStatus();
    public UiCommandHandlerResult Result = new UiCommandHandlerResult();
    public List<UiCommandInvocation> StatusQueries = new List<UiCommandInvocation>();
    public List<UiCommandInvocation> Executions = new List<UiCommandInvocation>();

    public UiCommandStatus QueryUiCommandStatus(UiCommandInvocation invocation)
    {
        StatusQueries.Add(invocation);
        return Status;
    }

    public UiCommandHandlerResult HandleUiCommand(UiCommandInvocation invocation)
    {
        Executions.Add(invocation);
        return Result;
    }
}

public class NullCommandHandler : IUiCommandHandler
{
    public UiCommandStatus QueryUiCommandStatus(UiCommandInvocation invocation)
    {
        return new UiCommandStatus();
    }

    public UiCommandHandlerResult HandleUiCommand(UiCommandInvocation invocation)
    {
        return null!;
    }
}
