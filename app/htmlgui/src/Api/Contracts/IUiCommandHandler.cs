namespace htmlgui;

/// <summary>Receives command status and execute callbacks without public delegates or C# events.</summary>
public interface IUiCommandHandler
{
    /// <summary>Queries command status for one invocation scope.</summary>
    /// <param name="invocation">The normalized command and scope context.</param>
    /// <returns>The current command status.</returns>
    UiCommandStatus QueryUiCommandStatus(UiCommandInvocation invocation);

    /// <summary>Executes one normalized command invocation.</summary>
    /// <param name="invocation">The command invocation.</param>
    /// <returns>The callback result and optional new check state.</returns>
    UiCommandHandlerResult HandleUiCommand(UiCommandInvocation invocation);
}
