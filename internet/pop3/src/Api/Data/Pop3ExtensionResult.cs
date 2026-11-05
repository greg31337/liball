namespace pop3;

/// <summary>Describes the response produced by a POP3 server extension.</summary>
public class Pop3ExtensionResult
{
    /// <summary>The single-line response to send to the client.</summary>
    public Pop3Response Response = new Pop3Response();

    /// <summary>The multiline payload stream, or <c>null</c> for a single-line response.</summary>
    public System.IO.Stream? MultilineData;

    /// <summary>Indicates whether the session must close after the extension response.</summary>
    public bool CloseSession;
}
