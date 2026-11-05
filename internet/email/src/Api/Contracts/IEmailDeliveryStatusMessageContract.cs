using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatusMessage"/>.
/// </summary>
public interface IEmailDeliveryStatusMessageContract
{
    /// <summary>
    /// Gets the field block.
    /// </summary>
    /// <returns>The current field block.</returns>
    EmailReportFieldBlock GetFieldBlock();
    /// <summary>
    /// Determines whether the reporting mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reporting mta is available; otherwise, <c>false</c>.</returns>
    bool HasReportingMta();
    /// <summary>
    /// Gets the reporting mta.
    /// </summary>
    /// <returns>The current reporting mta.</returns>
    EmailDeliveryStatusTypedValue? GetReportingMta();
    /// <summary>
    /// Determines whether the dsn gateway is present or available.
    /// </summary>
    /// <returns><c>true</c> when the dsn gateway is available; otherwise, <c>false</c>.</returns>
    bool HasDsnGateway();
    /// <summary>
    /// Gets the dsn gateway.
    /// </summary>
    /// <returns>The current dsn gateway.</returns>
    EmailDeliveryStatusTypedValue? GetDsnGateway();
    /// <summary>
    /// Determines whether the received from mta is present or available.
    /// </summary>
    /// <returns><c>true</c> when the received from mta is available; otherwise, <c>false</c>.</returns>
    bool HasReceivedFromMta();
    /// <summary>
    /// Gets the received from mta.
    /// </summary>
    /// <returns>The current received from mta.</returns>
    EmailDeliveryStatusTypedValue? GetReceivedFromMta();
    /// <summary>
    /// Determines whether the original envelope ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original envelope ID is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalEnvelopeId();
    /// <summary>
    /// Gets the original envelope ID.
    /// </summary>
    /// <returns>The current original envelope ID.</returns>
    string? GetOriginalEnvelopeId();
    /// <summary>
    /// Determines whether the arrival date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the arrival date is available; otherwise, <c>false</c>.</returns>
    bool HasArrivalDate();
    /// <summary>
    /// Gets the arrival date.
    /// </summary>
    /// <returns>The current arrival date.</returns>
    EmailDateTime? GetArrivalDate();
    /// <summary>
    /// Gets the extension fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension fields.</returns>
    List<EmailHeader> GetExtensionFields();
}
