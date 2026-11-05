namespace pop3;

/// <summary>Provides operations for a POP3 client session.</summary>
public interface IPop3Client
{
    /// <summary>Connects to the host and port specified by the client options.</summary>
    /// <param name="options">The connection, security, and behavior options.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking connection.</param>
    void ConnectHost(Pop3ClientOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Attaches a caller-supplied connected transport.</summary>
    /// <param name="stream">The connected transport stream.</param>
    /// <param name="options">The security, ownership, and behavior options.</param>
    /// <param name="cancellationToken">A token that can cancel greeting and setup work.</param>
    void ConnectStream(System.IO.Stream stream, Pop3ClientOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Gets a snapshot of the current client session state.</summary>
    /// <returns>The current client session snapshot.</returns>
    Pop3ClientSnapshot GetSnapshot();

    /// <summary>Refreshes and returns the current CAPA response.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The refreshed capability set.</returns>
    Pop3CapabilitySet RefreshCapabilities(System.Threading.CancellationToken cancellationToken);

    /// <summary>Upgrades an authorization-state plaintext connection with STLS.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking upgrade.</param>
    void UpgradeToTls(System.Threading.CancellationToken cancellationToken);

    /// <summary>Authenticates using the request's automatic selection policy.</summary>
    /// <param name="request">The credentials, hooks, and authentication policy.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    Pop3AuthenticationResult AuthenticateAutomatic(Pop3AuthenticationRequest request, System.Threading.CancellationToken cancellationToken);

    /// <summary>Authenticates with the USER and PASS commands.</summary>
    /// <param name="credentials">The credentials to authenticate.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    Pop3AuthenticationResult AuthenticateUserPassword(Pop3Credentials credentials, System.Threading.CancellationToken cancellationToken);

    /// <summary>Authenticates with the legacy APOP mechanism.</summary>
    /// <param name="credentials">The user name and password used to calculate the digest.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    Pop3AuthenticationResult AuthenticateApop(Pop3Credentials credentials, System.Threading.CancellationToken cancellationToken);

    /// <summary>Authenticates with a caller-supplied SASL mechanism.</summary>
    /// <param name="saslClient">The configured SASL client hook.</param>
    /// <param name="cancellationToken">A token that can cancel the authentication exchange.</param>
    /// <returns>The successful classified result.</returns>
    Pop3AuthenticationResult AuthenticateSasl(IPop3SaslClient saslClient, System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes STAT and returns the visible mailbox status.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The visible message count and total size.</returns>
    Pop3MailboxStatus GetMailboxStatus(System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes single-message LIST.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The message metadata.</returns>
    Pop3MessageInfo GetMessageInfo(int messageNumber, System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes multiline LIST.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The visible messages in server order.</returns>
    Pop3MessageInfo[] GetAllMessageInfo(System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes single-message UIDL.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The message unique identifier.</returns>
    Pop3UniqueId GetUniqueId(int messageNumber, System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes multiline UIDL.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The visible unique identifiers in server order.</returns>
    Pop3UniqueId[] GetAllUniqueIds(System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves one complete message to a caller-owned stream.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="destination">The destination stream, which remains caller-owned.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The number of message octets written.</returns>
    long RetrieveMessageToStream(int messageNumber, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves one complete message into bounded memory.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The exact unstuffed message representation.</returns>
    byte[] RetrieveMessageToMemory(int messageNumber, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves a validated batch of complete messages to caller-owned streams.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="destinations">The corresponding caller-owned destination streams.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The number of octets written to each destination.</returns>
    long[] RetrieveMessagesToStreams(int[] messageNumbers, System.IO.Stream[] destinations, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves a validated batch of complete messages into bounded memory.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The exact unstuffed message representations in caller order.</returns>
    byte[][] RetrieveMessagesToMemory(int[] messageNumbers, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves message headers and a requested number of body lines to a stream.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="bodyLineCount">The nonnegative body-line count.</param>
    /// <param name="destination">The destination stream, which remains caller-owned.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The number of message octets written.</returns>
    long RetrieveTopToStream(int messageNumber, int bodyLineCount, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves message headers and a requested number of body lines into bounded memory.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="bodyLineCount">The nonnegative body-line count.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfer.</param>
    /// <returns>The exact unstuffed TOP representation.</returns>
    byte[] RetrieveTopToMemory(int messageNumber, int bodyLineCount, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves a validated TOP batch to caller-owned streams.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="bodyLineCounts">The corresponding nonnegative body-line counts.</param>
    /// <param name="destinations">The corresponding caller-owned destination streams.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The number of octets written to each destination.</returns>
    long[] RetrieveTopBatchToStreams(int[] messageNumbers, int[] bodyLineCounts, System.IO.Stream[] destinations, System.Threading.CancellationToken cancellationToken);

    /// <summary>Retrieves a validated TOP batch into bounded memory.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="bodyLineCounts">The corresponding nonnegative body-line counts.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking transfers.</param>
    /// <returns>The exact unstuffed TOP representations in caller order.</returns>
    byte[][] RetrieveTopBatchToMemory(int[] messageNumbers, int[] bodyLineCounts, System.Threading.CancellationToken cancellationToken);

    /// <summary>Marks one message for deletion.</summary>
    /// <param name="messageNumber">The one-based message number.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    void DeleteMessage(int messageNumber, System.Threading.CancellationToken cancellationToken);

    /// <summary>Marks a validated batch of messages for deletion.</summary>
    /// <param name="messageNumbers">The one-based message numbers in caller order.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operations.</param>
    void DeleteMessages(int[] messageNumbers, System.Threading.CancellationToken cancellationToken);

    /// <summary>Marks every visible message for deletion.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operations.</param>
    void DeleteAllMessages(System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes RSET and returns the restored mailbox status.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The restored visible mailbox status.</returns>
    Pop3MailboxStatus ResetDeletions(System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes NOOP.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    void NoOperation(System.Threading.CancellationToken cancellationToken);

    /// <summary>Enables UTF8 message mode.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    void EnableUtf8(System.Threading.CancellationToken cancellationToken);

    /// <summary>Lists the languages advertised by the server.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The available languages in server order.</returns>
    Pop3Language[] GetLanguages(System.Threading.CancellationToken cancellationToken);

    /// <summary>Selects one advertised response language.</summary>
    /// <param name="languageTag">The language tag to select.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    void SelectLanguage(string languageTag, System.Threading.CancellationToken cancellationToken);

    /// <summary>Resets response language selection to the server default.</summary>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    void ResetLanguage(System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes a caller-defined command with a single-line response.</summary>
    /// <param name="command">The validated extension command.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The extension response.</returns>
    Pop3Response ExecuteSingleLineExtension(Pop3Command command, System.Threading.CancellationToken cancellationToken);

    /// <summary>Executes a caller-defined command and copies its multiline response.</summary>
    /// <param name="command">The validated extension command.</param>
    /// <param name="destination">The destination stream, which remains caller-owned.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking operation.</param>
    /// <returns>The extension response line.</returns>
    Pop3Response ExecuteMultilineExtension(Pop3Command command, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken);

    /// <summary>Sends QUIT, commits transactional deletions, and closes the session.</summary>
    /// <param name="cancellationToken">A token that can cancel work before commit begins.</param>
    void QuitAndCommit(System.Threading.CancellationToken cancellationToken);

    /// <summary>Closes the session without committing deletion marks.</summary>
    void Abort();
}
