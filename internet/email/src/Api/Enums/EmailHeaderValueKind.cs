namespace email;
/// <summary>
/// Specifies the supported header value values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHeaderValueKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the unstructured choice or state.
    /// </summary>
    Unstructured = 1,
    /// <summary>
    /// Represents the mailbox choice or state.
    /// </summary>
    Mailbox = 2,
    /// <summary>
    /// Represents the address group choice or state.
    /// </summary>
    AddressGroup = 3,
    /// <summary>
    /// Represents the address list choice or state.
    /// </summary>
    AddressList = 4,
    /// <summary>
    /// Represents the date choice or state.
    /// </summary>
    Date = 5,
    /// <summary>
    /// Represents the message ID choice or state.
    /// </summary>
    MessageId = 6,
    /// <summary>
    /// Represents the content ID choice or state.
    /// </summary>
    ContentId = 7,
    /// <summary>
    /// Represents the references choice or state.
    /// </summary>
    References = 8,
    /// <summary>
    /// Represents the in reply to choice or state.
    /// </summary>
    InReplyTo = 9,
    /// <summary>
    /// Represents the parameters choice or state.
    /// </summary>
    Parameters = 10,
    /// <summary>
    /// Represents the media type choice or state.
    /// </summary>
    MediaType = 11,
    /// <summary>
    /// Represents the disposition choice or state.
    /// </summary>
    Disposition = 12,
    /// <summary>
    /// Represents the content language choice or state.
    /// </summary>
    ContentLanguage = 13,
    /// <summary>
    /// Represents the received choice or state.
    /// </summary>
    Received = 14,
    /// <summary>
    /// Represents the authentication results choice or state.
    /// </summary>
    AuthenticationResults = 15,
    /// <summary>
    /// Represents the received spf choice or state.
    /// </summary>
    ReceivedSpf = 16,
    /// <summary>
    /// Represents the DKIM signature choice or state.
    /// </summary>
    DkimSignature = 17,
    /// <summary>
    /// Represents the ARC message signature choice or state.
    /// </summary>
    ArcMessageSignature = 18,
    /// <summary>
    /// Represents the ARC seal choice or state.
    /// </summary>
    ArcSeal = 19,
    /// <summary>
    /// Represents the ARC authentication results choice or state.
    /// </summary>
    ArcAuthenticationResults = 20,
    /// <summary>
    /// Represents the keywords choice or state.
    /// </summary>
    Keywords = 21,
    /// <summary>
    /// Represents the priority choice or state.
    /// </summary>
    Priority = 22,
    /// <summary>
    /// Represents the x priority choice or state.
    /// </summary>
    XPriority = 23,
    /// <summary>
    /// Represents the importance choice or state.
    /// </summary>
    Importance = 24,
    /// <summary>
    /// Represents the sensitivity choice or state.
    /// </summary>
    Sensitivity = 25,
    /// <summary>
    /// Represents the auto submitted choice or state.
    /// </summary>
    AutoSubmitted = 26,
    /// <summary>
    /// Represents the list help choice or state.
    /// </summary>
    ListHelp = 27,
    /// <summary>
    /// Represents the list unsubscribe choice or state.
    /// </summary>
    ListUnsubscribe = 28,
    /// <summary>
    /// Represents the list subscribe choice or state.
    /// </summary>
    ListSubscribe = 29,
    /// <summary>
    /// Represents the list post choice or state.
    /// </summary>
    ListPost = 30,
    /// <summary>
    /// Represents the list owner choice or state.
    /// </summary>
    ListOwner = 31,
    /// <summary>
    /// Represents the list archive choice or state.
    /// </summary>
    ListArchive = 32,
    /// <summary>
    /// Represents the list unsubscribe post choice or state.
    /// </summary>
    ListUnsubscribePost = 33,
    /// <summary>
    /// Represents the resent field choice or state.
    /// </summary>
    ResentField = 34,
    /// <summary>
    /// Represents the return path choice or state.
    /// </summary>
    ReturnPath = 35,
    /// <summary>
    /// Represents the MIME version choice or state.
    /// </summary>
    MimeVersion = 36,
    /// <summary>
    /// Represents the content transfer encoding choice or state.
    /// </summary>
    ContentTransferEncoding = 37,
    /// <summary>
    /// Represents the content location choice or state.
    /// </summary>
    ContentLocation = 38,
    /// <summary>
    /// Represents the newsgroups choice or state.
    /// </summary>
    Newsgroups = 39,
    /// <summary>
    /// Represents the followup-to choice or state.
    /// </summary>
    FollowupTo = 40,
    /// <summary>
    /// Represents the list identifier choice or state.
    /// </summary>
    ListId = 41,
    /// <summary>
    /// Represents a common trace field choice or state.
    /// </summary>
    Trace = 42
}
