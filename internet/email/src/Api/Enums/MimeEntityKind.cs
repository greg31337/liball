namespace email;
/// <summary>
/// Specifies the supported MIME entity values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum MimeEntityKind
{
    /// <summary>
    /// Represents the leaf choice or state.
    /// </summary>
    Leaf = 0,
    /// <summary>
    /// Represents the multipart choice or state.
    /// </summary>
    Multipart = 1,
    /// <summary>
    /// Represents the message choice or state.
    /// </summary>
    Message = 2,
    /// <summary>
    /// Represents the partial message choice or state.
    /// </summary>
    PartialMessage = 3,
    /// <summary>
    /// Represents the external body choice or state.
    /// </summary>
    ExternalBody = 4,
    /// <summary>
    /// Represents the delivery status choice or state.
    /// </summary>
    DeliveryStatus = 5,
    /// <summary>
    /// Represents the disposition notification choice or state.
    /// </summary>
    DispositionNotification = 6,
    /// <summary>
    /// Represents the feedback report choice or state.
    /// </summary>
    FeedbackReport = 7,
    /// <summary>
    /// Represents the calendar choice or state.
    /// </summary>
    Calendar = 8,
    /// <summary>
    /// Represents the TNEF choice or state.
    /// </summary>
    Tnef = 9,
    /// <summary>
    /// Represents the signed choice or state.
    /// </summary>
    Signed = 10,
    /// <summary>
    /// Represents the encrypted choice or state.
    /// </summary>
    Encrypted = 11
}
