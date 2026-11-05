using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatusRecipient"/>.
/// </summary>
public interface IEmailDeliveryStatusRecipientContract
{
    /// <summary>
    /// Gets the field block.
    /// </summary>
    /// <returns>The current field block.</returns>
    EmailReportFieldBlock GetFieldBlock();
    /// <summary>
    /// Determines whether the original recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original recipient is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalRecipient();
    /// <summary>
    /// Gets the original recipient.
    /// </summary>
    /// <returns>The current original recipient.</returns>
    EmailDeliveryStatusTypedValue? GetOriginalRecipient();
    /// <summary>
    /// Determines whether the final recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the final recipient is available; otherwise, <c>false</c>.</returns>
    bool HasFinalRecipient();
    /// <summary>
    /// Gets the final recipient.
    /// </summary>
    /// <returns>The current final recipient.</returns>
    EmailDeliveryStatusTypedValue? GetFinalRecipient();
    /// <summary>
    /// Determines whether the action is present or available.
    /// </summary>
    /// <returns><c>true</c> when the action is available; otherwise, <c>false</c>.</returns>
    bool HasAction();
    /// <summary>
    /// Gets the action.
    /// </summary>
    /// <returns>The current action.</returns>
    EmailDeliveryStatusAction GetAction();
    /// <summary>
    /// Gets the action text.
    /// </summary>
    /// <returns>The current action text.</returns>
    string? GetActionText();
    /// <summary>
    /// Determines whether the status is present or available.
    /// </summary>
    /// <returns><c>true</c> when the status is available; otherwise, <c>false</c>.</returns>
    bool HasStatus();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailDeliveryStatusCode? GetStatus();
    /// <summary>
    /// Determines whether the remote mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the remote mta is available; otherwise, <c>false</c>.</returns>
    bool HasRemoteMta();
    /// <summary>
    /// Gets the remote mta.
    /// </summary>
    /// <returns>The current remote mta.</returns>
    EmailDeliveryStatusTypedValue? GetRemoteMta();
    /// <summary>
    /// Determines whether the diagnostic code is present or available.
    /// </summary>
    /// <returns><c>true</c> when the diagnostic code is available; otherwise, <c>false</c>.</returns>
    bool HasDiagnosticCode();
    /// <summary>
    /// Gets the diagnostic code.
    /// </summary>
    /// <returns>The current diagnostic code.</returns>
    EmailDeliveryStatusTypedValue? GetDiagnosticCode();
    /// <summary>
    /// Determines whether the last attempt date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the last attempt date is available; otherwise, <c>false</c>.</returns>
    bool HasLastAttemptDate();
    /// <summary>
    /// Gets the last attempt date.
    /// </summary>
    /// <returns>The current last attempt date.</returns>
    EmailDateTime? GetLastAttemptDate();
    /// <summary>
    /// Determines whether the will retry until is present or available.
    /// </summary>
    /// <returns><c>true</c> when the will retry until is available; otherwise, <c>false</c>.</returns>
    bool HasWillRetryUntil();
    /// <summary>
    /// Gets the will retry until.
    /// </summary>
    /// <returns>The current will retry until.</returns>
    EmailDateTime? GetWillRetryUntil();
    /// <summary>
    /// Determines whether the final log ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the final log ID is available; otherwise, <c>false</c>.</returns>
    bool HasFinalLogId();
    /// <summary>
    /// Gets the final log ID.
    /// </summary>
    /// <returns>The current final log ID.</returns>
    string? GetFinalLogId();
    /// <summary>
    /// Gets the extension fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension fields.</returns>
    List<EmailHeader> GetExtensionFields();
}
