namespace pop3;

/// <summary>Represents a negative response to a valid POP3 command.</summary>
public class Pop3CommandException : System.Exception
{
    /// <summary>The command that received the negative response.</summary>
    public Pop3Command Command;

    /// <summary>The complete negative response returned by the peer.</summary>
    public Pop3Response Response;

    /// <summary>Initializes a command exception with its command and response data.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="command">The command that failed.</param>
    /// <param name="response">The negative response returned by the peer.</param>
    public Pop3CommandException(string message, Pop3Command command, Pop3Response response) : base(message)
    {
        Command = command;
        Response = response;
    }
}
