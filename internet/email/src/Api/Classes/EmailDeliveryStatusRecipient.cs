using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a delivery status recipient.
/// </summary>
public class EmailDeliveryStatusRecipient : IEmailDeliveryStatusRecipientContract
{
    EmailReportFieldBlock fields;
    /// <summary>
    /// Creates a new delivery status recipient.
    /// </summary>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDeliveryStatusRecipient(EmailReportFieldBlock fields)
    {
        if (fields == null)
            throw new ArgumentNullException("fields");
        this.fields = fields;
    }

    /// <summary>
    /// Gets the field block.
    /// </summary>
    /// <returns>The current field block.</returns>
    public EmailReportFieldBlock GetFieldBlock()
    {
        return fields;
    }

    /// <summary>
    /// Determines whether the original recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original recipient is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalRecipient()
    {
        return GetOriginalRecipient() != null;
    }

    /// <summary>
    /// Gets the original recipient.
    /// </summary>
    /// <returns>The current original recipient.</returns>
    public EmailDeliveryStatusTypedValue? GetOriginalRecipient()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("Original-Recipient"));
    }

    /// <summary>
    /// Determines whether the final recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the final recipient is available; otherwise, <c>false</c>.</returns>
    public bool HasFinalRecipient()
    {
        return GetFinalRecipient() != null;
    }

    /// <summary>
    /// Gets the final recipient.
    /// </summary>
    /// <returns>The current final recipient.</returns>
    public EmailDeliveryStatusTypedValue? GetFinalRecipient()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("Final-Recipient"));
    }

    /// <summary>
    /// Determines whether the action is present or available.
    /// </summary>
    /// <returns><c>true</c> when the action is available; otherwise, <c>false</c>.</returns>
    public bool HasAction()
    {
        return fields.GetFirstField("Action") != null;
    }

    /// <summary>
    /// Gets the action.
    /// </summary>
    /// <returns>The current action.</returns>
    public EmailDeliveryStatusAction GetAction()
    {
        return EmailDeliveryStatusValueParser.ParseAction(fields.GetFirstField("Action"));
    }

    /// <summary>
    /// Gets the action text.
    /// </summary>
    /// <returns>The current action text.</returns>
    public string? GetActionText()
    {
        return EmailDeliveryStatusValueParser.GetText(fields.GetFirstField("Action"));
    }

    /// <summary>
    /// Determines whether the status is present or available.
    /// </summary>
    /// <returns><c>true</c> when the status is available; otherwise, <c>false</c>.</returns>
    public bool HasStatus()
    {
        return GetStatus() != null;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailDeliveryStatusCode? GetStatus()
    {
        return EmailDeliveryStatusValueParser.ParseStatus(fields.GetFirstField("Status"));
    }

    /// <summary>
    /// Determines whether the remote mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the remote mta is available; otherwise, <c>false</c>.</returns>
    public bool HasRemoteMta()
    {
        return GetRemoteMta() != null;
    }

    /// <summary>
    /// Gets the remote mta.
    /// </summary>
    /// <returns>The current remote mta.</returns>
    public EmailDeliveryStatusTypedValue? GetRemoteMta()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("Remote-MTA"));
    }

    /// <summary>
    /// Determines whether the diagnostic code is present or available.
    /// </summary>
    /// <returns><c>true</c> when the diagnostic code is available; otherwise, <c>false</c>.</returns>
    public bool HasDiagnosticCode()
    {
        return GetDiagnosticCode() != null;
    }

    /// <summary>
    /// Gets the diagnostic code.
    /// </summary>
    /// <returns>The current diagnostic code.</returns>
    public EmailDeliveryStatusTypedValue? GetDiagnosticCode()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("Diagnostic-Code"));
    }

    /// <summary>
    /// Determines whether the last attempt date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the last attempt date is available; otherwise, <c>false</c>.</returns>
    public bool HasLastAttemptDate()
    {
        return GetLastAttemptDate() != null;
    }

    /// <summary>
    /// Gets the last attempt date.
    /// </summary>
    /// <returns>The current last attempt date.</returns>
    public EmailDateTime? GetLastAttemptDate()
    {
        return EmailDeliveryStatusValueParser.ParseDate(fields.GetFirstField("Last-Attempt-Date"));
    }

    /// <summary>
    /// Determines whether the will retry until is present or available.
    /// </summary>
    /// <returns><c>true</c> when the will retry until is available; otherwise, <c>false</c>.</returns>
    public bool HasWillRetryUntil()
    {
        return GetWillRetryUntil() != null;
    }

    /// <summary>
    /// Gets the will retry until.
    /// </summary>
    /// <returns>The current will retry until.</returns>
    public EmailDateTime? GetWillRetryUntil()
    {
        return EmailDeliveryStatusValueParser.ParseDate(fields.GetFirstField("Will-Retry-Until"));
    }

    /// <summary>
    /// Determines whether the final log ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the final log ID is available; otherwise, <c>false</c>.</returns>
    public bool HasFinalLogId()
    {
        return fields.GetFirstField("Final-Log-ID") != null;
    }

    /// <summary>
    /// Gets the final log ID.
    /// </summary>
    /// <returns>The current final log ID.</returns>
    public string? GetFinalLogId()
    {
        return EmailDeliveryStatusValueParser.GetText(fields.GetFirstField("Final-Log-ID"));
    }

    /// <summary>
    /// Gets the extension fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension fields.</returns>
    public List<EmailHeader> GetExtensionFields()
    {
        return EmailDeliveryStatusValueParser.GetExtensions(fields, EmailDeliveryStatusValueParser.RecipientFieldNames());
    }
}
