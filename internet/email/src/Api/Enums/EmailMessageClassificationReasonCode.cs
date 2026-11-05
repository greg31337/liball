using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported message classification reason code values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMessageClassificationReasonCode
{
    /// <summary>
    /// Represents the auto submitted generated choice or state.
    /// </summary>
    AutoSubmittedGenerated = 0,
    /// <summary>
    /// Represents the auto submitted replied choice or state.
    /// </summary>
    AutoSubmittedReplied = 1,
    /// <summary>
    /// Represents the auto submitted extension choice or state.
    /// </summary>
    AutoSubmittedExtension = 2,
    /// <summary>
    /// Represents the auto submitted no choice or state.
    /// </summary>
    AutoSubmittedNo = 3,
    /// <summary>
    /// Represents the auto submitted malformed choice or state.
    /// </summary>
    AutoSubmittedMalformed = 4,
    /// <summary>
    /// Represents the list identifier choice or state.
    /// </summary>
    ListIdentifier = 5,
    /// <summary>
    /// Represents the list identifier malformed choice or state.
    /// </summary>
    ListIdentifierMalformed = 6,
    /// <summary>
    /// Represents the list identifier duplicated choice or state.
    /// </summary>
    ListIdentifierDuplicated = 7,
    /// <summary>
    /// Represents the list management header choice or state.
    /// </summary>
    ListManagementHeader = 8,
    /// <summary>
    /// Represents the list management header malformed choice or state.
    /// </summary>
    ListManagementHeaderMalformed = 9,
    /// <summary>
    /// Represents the precedence list choice or state.
    /// </summary>
    PrecedenceList = 10,
    /// <summary>
    /// Represents the precedence bulk choice or state.
    /// </summary>
    PrecedenceBulk = 11,
    /// <summary>
    /// Represents the delivery status failure choice or state.
    /// </summary>
    DeliveryStatusFailure = 12,
    /// <summary>
    /// Represents the delivery status delay choice or state.
    /// </summary>
    DeliveryStatusDelay = 13,
    /// <summary>
    /// Represents the delivery status success choice or state.
    /// </summary>
    DeliveryStatusSuccess = 14,
    /// <summary>
    /// Represents the delivery status report choice or state.
    /// </summary>
    DeliveryStatusReport = 15,
    /// <summary>
    /// Represents the delivery status malformed choice or state.
    /// </summary>
    DeliveryStatusMalformed = 16,
    /// <summary>
    /// Represents the disposition notification report choice or state.
    /// </summary>
    DispositionNotificationReport = 17,
    /// <summary>
    /// Represents the disposition notification malformed choice or state.
    /// </summary>
    DispositionNotificationMalformed = 18,
    /// <summary>
    /// Represents the disposition notification request choice or state.
    /// </summary>
    DispositionNotificationRequest = 19,
    /// <summary>
    /// Represents the feedback report choice or state.
    /// </summary>
    FeedbackReport = 20,
    /// <summary>
    /// Represents the multipart report choice or state.
    /// </summary>
    MultipartReport = 21,
    /// <summary>
    /// Represents the machine delivery status part choice or state.
    /// </summary>
    MachineDeliveryStatusPart = 22,
    /// <summary>
    /// Represents the machine disposition notification part choice or state.
    /// </summary>
    MachineDispositionNotificationPart = 23,
    /// <summary>
    /// Represents the null return path choice or state.
    /// </summary>
    NullReturnPath = 24,
    /// <summary>
    /// Represents the bounce subject choice or state.
    /// </summary>
    BounceSubject = 25,
    /// <summary>
    /// Represents the receipt subject choice or state.
    /// </summary>
    ReceiptSubject = 26
}
