namespace email;
/// <summary>
/// Specifies the supported report values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailReportKind
{
    /// <summary>
    /// Represents the delivery status choice or state.
    /// </summary>
    DeliveryStatus = 0,
    /// <summary>
    /// Represents the disposition notification choice or state.
    /// </summary>
    DispositionNotification = 1,
    /// <summary>
    /// Represents the feedback report choice or state.
    /// </summary>
    FeedbackReport = 2
}
