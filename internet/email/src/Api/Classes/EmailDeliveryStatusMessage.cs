using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a delivery status message.
/// </summary>
public class EmailDeliveryStatusMessage : IEmailDeliveryStatusMessageContract
{
    EmailReportFieldBlock fields;
    /// <summary>
    /// Creates a new delivery status message.
    /// </summary>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDeliveryStatusMessage(EmailReportFieldBlock fields)
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
    /// Determines whether the reporting mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reporting mta is available; otherwise, <c>false</c>.</returns>
    public bool HasReportingMta()
    {
        return GetReportingMta() != null;
    }

    /// <summary>
    /// Gets the reporting mta.
    /// </summary>
    /// <returns>The current reporting mta.</returns>
    public EmailDeliveryStatusTypedValue? GetReportingMta()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("Reporting-MTA"));
    }

    /// <summary>
    /// Determines whether the dsn gateway is present or available.
    /// </summary>
    /// <returns><c>true</c> when the dsn gateway is available; otherwise, <c>false</c>.</returns>
    public bool HasDsnGateway()
    {
        return GetDsnGateway() != null;
    }

    /// <summary>
    /// Gets the dsn gateway.
    /// </summary>
    /// <returns>The current dsn gateway.</returns>
    public EmailDeliveryStatusTypedValue? GetDsnGateway()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("DSN-Gateway"));
    }

    /// <summary>
    /// Determines whether the received from mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the received from mta is available; otherwise, <c>false</c>.</returns>
    public bool HasReceivedFromMta()
    {
        return GetReceivedFromMta() != null;
    }

    /// <summary>
    /// Gets the received from mta.
    /// </summary>
    /// <returns>The current received from mta.</returns>
    public EmailDeliveryStatusTypedValue? GetReceivedFromMta()
    {
        return EmailDeliveryStatusValueParser.ParseTyped(fields.GetFirstField("Received-From-MTA"));
    }

    /// <summary>
    /// Determines whether the original envelope ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original envelope ID is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalEnvelopeId()
    {
        return fields.GetFirstField("Original-Envelope-Id") != null;
    }

    /// <summary>
    /// Gets the original envelope ID.
    /// </summary>
    /// <returns>The current original envelope ID.</returns>
    public string? GetOriginalEnvelopeId()
    {
        return EmailDeliveryStatusValueParser.GetText(fields.GetFirstField("Original-Envelope-Id"));
    }

    /// <summary>
    /// Determines whether the arrival date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the arrival date is available; otherwise, <c>false</c>.</returns>
    public bool HasArrivalDate()
    {
        return GetArrivalDate() != null;
    }

    /// <summary>
    /// Gets the arrival date.
    /// </summary>
    /// <returns>The current arrival date.</returns>
    public EmailDateTime? GetArrivalDate()
    {
        return EmailDeliveryStatusValueParser.ParseDate(fields.GetFirstField("Arrival-Date"));
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
        return EmailDeliveryStatusValueParser.GetExtensions(fields, EmailDeliveryStatusValueParser.MessageFieldNames());
    }
}
