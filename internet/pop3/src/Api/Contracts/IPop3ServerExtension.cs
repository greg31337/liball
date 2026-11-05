namespace pop3;

/// <summary>Handles a caller-defined POP3 server command.</summary>
public interface IPop3ServerExtension
{
    /// <summary>Gets the capabilities advertised by this extension in the current context.</summary>
    /// <param name="context">The current safe session context.</param>
    /// <returns>The extension capabilities in advertisement order.</returns>
    Pop3Capability[] GetCapabilities(Pop3ExtensionContext context);

    /// <summary>Gets the nonstandard command names handled by this extension.</summary>
    /// <returns>The canonical extension command names.</returns>
    string[] GetCommandNames();

    /// <summary>Gets the session states in which one extension command is legal.</summary>
    /// <param name="commandName">The canonical extension command name.</param>
    /// <returns>The legal session states.</returns>
    Pop3SessionState[] GetAllowedStates(string commandName);

    /// <summary>Executes an extension command in the supplied safe session context.</summary>
    /// <param name="context">The current safe session context.</param>
    /// <param name="command">The parsed extension command.</param>
    /// <param name="cancellationToken">A token that can cancel extension work.</param>
    /// <returns>The response and optional multiline data.</returns>
    Pop3ExtensionResult ExecuteCommand(Pop3ExtensionContext context, Pop3Command command, System.Threading.CancellationToken cancellationToken);
}
