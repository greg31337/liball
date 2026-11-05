namespace pop3;

/// <summary>Provides a stateful POP3 client over host or caller-supplied transports.</summary>
public class Pop3Client : IPop3Client
{
    Pop3ClientEngine Engine;

    /// <summary>Creates a disconnected POP3 client.</summary>
    public Pop3Client()
    {
        Engine = new Pop3ClientEngine();
    }

    /// <summary>Connects to the configured POP3 host.</summary>
    /// <param name="options">The connection, security, and behavior options.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking connection.</param>
    public void ConnectHost(
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.ConnectHost(options, cancellationToken);
    }

    /// <summary>Attaches a caller-supplied connected transport.</summary>
    /// <param name="stream">The connected transport stream.</param>
    /// <param name="options">The security, ownership, and behavior options.</param>
    /// <param name="cancellationToken">A token that can cancel greeting and setup work.</param>
    public void ConnectStream(
        System.IO.Stream stream,
        Pop3ClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.ConnectStream(stream, options, cancellationToken);
    }

    /// <summary>Gets a defensive snapshot of the current client session.</summary>
    /// <returns>The current client session snapshot.</returns>
    public Pop3ClientSnapshot GetSnapshot()
    {
        return Engine.GetSnapshot();
    }

    /// <summary>Refreshes the current server capabilities.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The refreshed capability set.</returns>
    public Pop3CapabilitySet RefreshCapabilities(
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RefreshCapabilities(cancellationToken);
    }

    /// <summary>Upgrades an authorization-state plaintext connection with STLS.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking upgrade.</param>
    public void UpgradeToTls(
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.UpgradeToTls(cancellationToken);
    }

    /// <summary>Authenticates using the requested automatic selection policy.</summary>
    /// <param name="request">The credentials, hooks, and authentication policy.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    public Pop3AuthenticationResult AuthenticateAutomatic(
        Pop3AuthenticationRequest request,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.AuthenticateAutomatic(request, cancellationToken);
    }

    /// <summary>Authenticates with USER and PASS.</summary>
    /// <param name="credentials">The credentials to authenticate.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    public Pop3AuthenticationResult AuthenticateUserPassword(
        Pop3Credentials credentials,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.AuthenticateUserPassword(credentials, cancellationToken);
    }

    /// <summary>Authenticates with the legacy APOP mechanism.</summary>
    /// <param name="credentials">The credentials used to calculate the APOP digest.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    public Pop3AuthenticationResult AuthenticateApop(
        Pop3Credentials credentials,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.AuthenticateApop(credentials, cancellationToken);
    }

    /// <summary>Authenticates with a caller-supplied SASL mechanism.</summary>
    /// <param name="saslClient">The configured SASL client hook.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    public Pop3AuthenticationResult AuthenticateSasl(
        IPop3SaslClient saslClient,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.AuthenticateSasl(saslClient, cancellationToken);
    }

    /// <summary>Executes STAT for the visible maildrop.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The visible mailbox status.</returns>
    public Pop3MailboxStatus GetMailboxStatus(
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.GetMailboxStatus(cancellationToken);
    }

    /// <summary>Executes LIST for one message.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The message metadata.</returns>
    public Pop3MessageInfo GetMessageInfo(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.GetMessageInfo(messageNumber, cancellationToken);
    }

    /// <summary>Executes multiline LIST.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The visible messages in server order.</returns>
    public Pop3MessageInfo[] GetAllMessageInfo(
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.GetAllMessageInfo(cancellationToken);
    }

    /// <summary>Executes UIDL for one message.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The message unique identifier.</returns>
    public Pop3UniqueId GetUniqueId(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.GetUniqueId(messageNumber, cancellationToken);
    }

    /// <summary>Executes multiline UIDL.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The visible unique identifiers in server order.</returns>
    public Pop3UniqueId[] GetAllUniqueIds(
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.GetAllUniqueIds(cancellationToken);
    }

    /// <summary>Retrieves one complete message to a caller-owned stream.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="destination">The caller-owned destination stream.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The number of message octets written.</returns>
    public long RetrieveMessageToStream(
        int messageNumber,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveMessageToStream(
            messageNumber,
            destination,
            cancellationToken);
    }

    /// <summary>Retrieves one complete message into bounded memory.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The exact unstuffed message representation.</returns>
    public byte[] RetrieveMessageToMemory(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveMessageToMemory(messageNumber, cancellationToken);
    }

    /// <summary>Retrieves a validated message batch to caller-owned streams.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="destinations">The corresponding caller-owned destination streams.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The number of octets written to each destination.</returns>
    public long[] RetrieveMessagesToStreams(
        int[] messageNumbers,
        System.IO.Stream[] destinations,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveMessagesToStreams(
            messageNumbers,
            destinations,
            cancellationToken);
    }

    /// <summary>Retrieves a validated message batch into bounded memory.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The exact unstuffed messages in caller order.</returns>
    public byte[][] RetrieveMessagesToMemory(
        int[] messageNumbers,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveMessagesToMemory(messageNumbers, cancellationToken);
    }

    /// <summary>Retrieves TOP data for one message to a caller-owned stream.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="bodyLineCount">The nonnegative body-line count.</param>
    /// <param name="destination">The caller-owned destination stream.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The number of message octets written.</returns>
    public long RetrieveTopToStream(
        int messageNumber,
        int bodyLineCount,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveTopToStream(
            messageNumber,
            bodyLineCount,
            destination,
            cancellationToken);
    }

    /// <summary>Retrieves TOP data for one message into bounded memory.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="bodyLineCount">The nonnegative body-line count.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The exact unstuffed TOP representation.</returns>
    public byte[] RetrieveTopToMemory(
        int messageNumber,
        int bodyLineCount,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveTopToMemory(
            messageNumber,
            bodyLineCount,
            cancellationToken);
    }

    /// <summary>Retrieves a validated TOP batch to caller-owned streams.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="bodyLineCounts">The corresponding nonnegative body-line counts.</param>
    /// <param name="destinations">The corresponding caller-owned destination streams.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The number of octets written to each destination.</returns>
    public long[] RetrieveTopBatchToStreams(
        int[] messageNumbers,
        int[] bodyLineCounts,
        System.IO.Stream[] destinations,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveTopBatchToStreams(
            messageNumbers,
            bodyLineCounts,
            destinations,
            cancellationToken);
    }

    /// <summary>Retrieves a validated TOP batch into bounded memory.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="bodyLineCounts">The corresponding nonnegative body-line counts.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The exact unstuffed TOP representations in caller order.</returns>
    public byte[][] RetrieveTopBatchToMemory(
        int[] messageNumbers,
        int[] bodyLineCounts,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.RetrieveTopBatchToMemory(
            messageNumbers,
            bodyLineCounts,
            cancellationToken);
    }

    /// <summary>Marks one message for deletion.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    public void DeleteMessage(
        int messageNumber,
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.DeleteMessage(messageNumber, cancellationToken);
    }

    /// <summary>Marks a validated message batch for deletion.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operations.</param>
    public void DeleteMessages(
        int[] messageNumbers,
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.DeleteMessages(messageNumbers, cancellationToken);
    }

    /// <summary>Marks every visible message for deletion.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operations.</param>
    public void DeleteAllMessages(
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.DeleteAllMessages(cancellationToken);
    }

    /// <summary>Resets deletion marks and refreshes the restored status.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The restored visible mailbox status.</returns>
    public Pop3MailboxStatus ResetDeletions(
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.ResetDeletions(cancellationToken);
    }

    /// <summary>Executes NOOP.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    public void NoOperation(
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.NoOperation(cancellationToken);
    }

    /// <summary>Enables UTF8 message mode.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    public void EnableUtf8(
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.EnableUtf8(cancellationToken);
    }

    /// <summary>Lists the server response languages.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The available languages in server order.</returns>
    public Pop3Language[] GetLanguages(
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.GetLanguages(cancellationToken);
    }

    /// <summary>Selects one server response language.</summary>
    /// <param name="languageTag">The language tag to select.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    public void SelectLanguage(
        string languageTag,
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.SelectLanguage(languageTag, cancellationToken);
    }

    /// <summary>Resets response language selection to the server default.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    public void ResetLanguage(
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.ResetLanguage(cancellationToken);
    }

    /// <summary>Executes a caller-defined command with a single-line response.</summary>
    /// <param name="command">The validated extension command.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The extension response.</returns>
    public Pop3Response ExecuteSingleLineExtension(
        Pop3Command command,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.ExecuteSingleLineExtension(command, cancellationToken);
    }

    /// <summary>Executes a caller-defined command with a multiline response.</summary>
    /// <param name="command">The validated extension command.</param>
    /// <param name="destination">The caller-owned destination stream.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The extension response line.</returns>
    public Pop3Response ExecuteMultilineExtension(
        Pop3Command command,
        System.IO.Stream destination,
        System.Threading.CancellationToken cancellationToken)
    {
        return Engine.ExecuteMultilineExtension(
            command,
            destination,
            cancellationToken);
    }

    /// <summary>Sends QUIT, commits transactional deletions, and closes the session.</summary>
    /// <param name="cancellationToken">A token that can cancel work before commit begins.</param>
    public void QuitAndCommit(
        System.Threading.CancellationToken cancellationToken)
    {
        Engine.QuitAndCommit(cancellationToken);
    }

    /// <summary>Closes the session without committing deletion marks.</summary>
    public void Abort()
    {
        Engine.Abort();
    }
}
