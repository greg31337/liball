namespace htmlgui;

/// <summary>Registers, queries, executes, matches, and projects commands.</summary>
public interface IUiCommandDispatcher
{
    /// <summary>Registers a command and its callback handler.</summary>
    /// <param name="command">The command definition.</param>
    /// <param name="handler">The status and execute callback handler.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiResult RegisterUiCommand(UiCommand? command, IUiCommandHandler? handler);

    /// <summary>Unregisters a command and its callback handler.</summary>
    /// <param name="commandId">The stable command identifier.</param>
    /// <returns>The exceptionless unregistration result.</returns>
    UiResult UnregisterUiCommand(string? commandId);

    /// <summary>Queries command status in an optional active scope.</summary>
    /// <param name="commandId">The stable command identifier.</param>
    /// <param name="scopeId">The optional active scope.</param>
    /// <returns>The status query result.</returns>
    UiCommandResult QueryUiCommandStatus(string? commandId, string? scopeId);

    /// <summary>Stores command status in an optional active scope.</summary>
    /// <param name="commandId">The stable command identifier.</param>
    /// <param name="scopeId">The optional active scope.</param>
    /// <param name="status">The status to store.</param>
    /// <returns>The exceptionless update result.</returns>
    UiResult UpdateUiCommandStatus(string? commandId, string? scopeId, UiCommandStatus? status);

    /// <summary>Executes a command through its registered callback.</summary>
    /// <param name="invocation">The command invocation.</param>
    /// <returns>The completed execution result.</returns>
    UiCommandResult ExecuteUiCommand(UiCommandInvocation? invocation);

    /// <summary>Matches normalized keyboard input and executes a complete shortcut.</summary>
    /// <param name="keyboard">The normalized keyboard input.</param>
    /// <param name="activeScopeId">The optional active scope.</param>
    /// <param name="sourceNodeId">The optional source node.</param>
    /// <param name="parameter">The optional portable command parameter.</param>
    /// <param name="resetSequence">Whether to discard a pending chord prefix first.</param>
    /// <returns>The shortcut matching and execution result.</returns>
    UiCommandShortcutResult DispatchCommandShortcut(UiKeyboardData? keyboard, string? activeScopeId, string? sourceNodeId, UiValue? parameter, bool resetSequence);

    /// <summary>Returns a command-projected copy of a button.</summary>
    /// <param name="button">The source button.</param>
    /// <param name="scopeId">The optional active scope.</param>
    /// <returns>The projected button.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the button is null.</exception>
    UiButton ProjectCommandButton(UiButton? button, string? scopeId);

    /// <summary>Returns a command-projected copy of a menu and its nested menus.</summary>
    /// <param name="menu">The source menu.</param>
    /// <param name="scopeId">The optional active scope.</param>
    /// <returns>The projected menu.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the menu is null.</exception>
    UiMenu ProjectCommandMenu(UiMenu? menu, string? scopeId);

    /// <summary>Returns a command-projected copy of a toolbar.</summary>
    /// <param name="toolbar">The source toolbar.</param>
    /// <param name="scopeId">The optional active scope.</param>
    /// <returns>The projected toolbar.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the toolbar is null.</exception>
    UiToolbar ProjectCommandToolbar(UiToolbar? toolbar, string? scopeId);
}
