using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Writes delivery status in its wire or storage format.
/// </summary>
public class EmailDeliveryStatusWriter : IEmailDeliveryStatusWriterContract
{
    internal static byte[] WriteImplementationWithDeliveryStatus(EmailDeliveryStatus deliveryStatus)
    {
        return WriteImplementationWithDeliveryStatusAndMaximumBytes(deliveryStatus, EmailReportOptionsConstants.DefaultMaximumReportBytes);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] WriteWithDeliveryStatus(EmailDeliveryStatus deliveryStatus)
    {
        return EmailDeliveryStatusWriter.WriteImplementationWithDeliveryStatus(deliveryStatus);
    }

    internal static byte[] WriteImplementationWithDeliveryStatusAndMaximumBytes(EmailDeliveryStatus deliveryStatus, int maximumBytes)
    {
        if (deliveryStatus == null)
            throw new ArgumentNullException("deliveryStatus");
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException("maximumBytes");
        Validate(deliveryStatus);
        List<byte> output = new List<byte>();
        AppendBlock(output, (deliveryStatus.GetMessageFields() ?? throw new InvalidOperationException()).GetFieldBlock().GetRawBytes(), deliveryStatus.IsInternationalized(), maximumBytes);
        int index = 0;
        while (index < deliveryStatus.GetRecipientCount())
        {
            AppendByte(output, 13, maximumBytes);
            AppendByte(output, 10, maximumBytes);
            AppendBlock(output, deliveryStatus.GetRecipientAt(index).GetFieldBlock().GetRawBytes(), deliveryStatus.IsInternationalized(), maximumBytes);
            index++;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte[] WriteWithDeliveryStatusAndMaximumBytes(EmailDeliveryStatus deliveryStatus, int maximumBytes)
    {
        return EmailDeliveryStatusWriter.WriteImplementationWithDeliveryStatusAndMaximumBytes(deliveryStatus, maximumBytes);
    }

    internal static EmailComposer CreateReportImplementationWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadable(EmailDeliveryStatus deliveryStatus, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable)
    {
        return CreateReportImplementationWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(deliveryStatus, original, reporter, recipient, humanReadable, new EmailReportOptions());
    }

    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadable(EmailDeliveryStatus deliveryStatus, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable)
    {
        return EmailDeliveryStatusWriter.CreateReportImplementationWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadable(deliveryStatus, original, reporter, recipient, humanReadable);
    }

    internal static EmailComposer CreateReportImplementationWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailDeliveryStatus deliveryStatus, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options)
    {
        if (deliveryStatus == null)
            throw new ArgumentNullException("deliveryStatus");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailReportOptions snapshot = options.Clone();
        byte[] fields = WriteImplementationWithDeliveryStatusAndMaximumBytes(deliveryStatus, snapshot.GetMaximumReportBytes());
        if (deliveryStatus.IsInternationalized())
        {
            return new EmailReportBuilder().CreateInternationalDeliveryStatus(original, reporter, recipient, humanReadable, fields, snapshot);
        }

        return new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, reporter, recipient, humanReadable, fields, snapshot);
    }

    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailComposer CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailDeliveryStatus deliveryStatus, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options)
    {
        return EmailDeliveryStatusWriter.CreateReportImplementationWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(deliveryStatus, original, reporter, recipient, humanReadable, options);
    }

    static void Validate(EmailDeliveryStatus value)
    {
        EmailDeliveryStatusMessage? message = value.GetMessageFields();
        if (!(message ?? throw new InvalidOperationException()).GetFieldBlock().IsSuccess())
            throw Invalid("The per-message field block contains malformed field syntax.");
        if (!message.HasReportingMta())
            throw Invalid("A Reporting-MTA field with a valid typed value is required.");
        ValidateSingleton(message.GetFieldBlock(), "Reporting-MTA");
        ValidateOptionalTyped(message.GetFieldBlock(), "DSN-Gateway", message.GetDsnGateway());
        ValidateOptionalTyped(message.GetFieldBlock(), "Received-From-MTA", message.GetReceivedFromMta());
        ValidateOptionalText(message.GetFieldBlock(), "Original-Envelope-Id", message.GetOriginalEnvelopeId());
        ValidateOptionalDate(message.GetFieldBlock(), "Arrival-Date", message.GetArrivalDate());
        if (value.GetRecipientCount() == 0)
            throw Invalid("At least one per-recipient field block is required.");
        int index = 0;
        while (index < value.GetRecipientCount())
        {
            ValidateRecipient(value.GetRecipientAt(index));
            index++;
        }
    }

    static void ValidateRecipient(EmailDeliveryStatusRecipient value)
    {
        EmailReportFieldBlock block = value.GetFieldBlock();
        if (!block.IsSuccess())
            throw Invalid("A per-recipient field block contains malformed field syntax.");
        if (!value.HasFinalRecipient())
            throw Invalid("Each per-recipient block requires a valid Final-Recipient field.");
        if (!value.HasAction() || value.GetAction() == EmailDeliveryStatusAction.Unknown)
            throw Invalid("Each per-recipient block requires a recognized Action field.");
        EmailDeliveryStatusCode? status = value.GetStatus();
        if (status == null)
            throw Invalid("Each per-recipient block requires a valid enhanced Status field.");
        if (!StatusMatchesAction(value.GetAction(), status.GetStatusClass()))
            throw Invalid("The Action and enhanced Status class conflict.");
        ValidateSingleton(block, "Final-Recipient");
        ValidateSingleton(block, "Action");
        ValidateSingleton(block, "Status");
        ValidateOptionalTyped(block, "Original-Recipient", value.GetOriginalRecipient());
        ValidateOptionalTyped(block, "Remote-MTA", value.GetRemoteMta());
        ValidateOptionalTyped(block, "Diagnostic-Code", value.GetDiagnosticCode());
        ValidateOptionalDate(block, "Last-Attempt-Date", value.GetLastAttemptDate());
        ValidateOptionalDate(block, "Will-Retry-Until", value.GetWillRetryUntil());
        ValidateOptionalText(block, "Final-Log-ID", value.GetFinalLogId());
    }

    static void ValidateSingleton(EmailReportFieldBlock block, string name)
    {
        if (block.GetFieldsNamed(name).Count > 1)
            throw Invalid("Delivery-status singleton fields cannot be duplicated.");
    }

    static void ValidateOptionalTyped(EmailReportFieldBlock block, string name, EmailDeliveryStatusTypedValue? value)
    {
        ValidateSingleton(block, name);
        if (block.GetFirstField(name) != null && value == null)
            throw Invalid("A delivery-status typed field is malformed.");
    }

    static void ValidateOptionalDate(EmailReportFieldBlock block, string name, EmailDateTime? value)
    {
        ValidateSingleton(block, name);
        if (block.GetFirstField(name) != null && value == null)
            throw Invalid("A delivery-status date field is malformed.");
    }

    static void ValidateOptionalText(EmailReportFieldBlock block, string name, string? value)
    {
        ValidateSingleton(block, name);
        if (block.GetFirstField(name) != null && value == null)
            throw Invalid("A delivery-status text field cannot be empty.");
    }

    static bool StatusMatchesAction(EmailDeliveryStatusAction action, int statusClass)
    {
        if (action == EmailDeliveryStatusAction.Failed)
            return statusClass == 5;
        if (action == EmailDeliveryStatusAction.Delayed)
            return statusClass == 4;
        return statusClass == 2;
    }

    static void AppendBlock(List<byte> output, byte[] raw, bool internationalized, int maximumBytes)
    {
        ValidateEncoding(raw, internationalized);
        int index = 0;
        while (index < raw.Length)
        {
            byte current = raw[index];
            if (current == 0)
                throw Invalid("Delivery-status fields cannot contain NUL bytes.");
            if (current == 13 || current == 10)
            {
                if (current == 13 && index + 1 < raw.Length && raw[index + 1] == 10)
                    index++;
                AppendByte(output, 13, maximumBytes);
                AppendByte(output, 10, maximumBytes);
            }
            else
            {
                AppendByte(output, current, maximumBytes);
            }

            index++;
        }

        if (output.Count == 0 || output[output.Count - 1] != 10)
        {
            AppendByte(output, 13, maximumBytes);
            AppendByte(output, 10, maximumBytes);
        }
    }

    static void ValidateEncoding(byte[] value, bool internationalized)
    {
        if (internationalized)
        {
            if (!new EmailUtf8().IsValid(value))
                throw Invalid("Internationalized delivery-status fields must contain valid UTF-8.");
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                throw Invalid("Noninternationalized delivery-status fields must contain only ASCII bytes.");
            index++;
        }
    }

    static void AppendByte(List<byte> output, byte value, int maximumBytes)
    {
        if (output.Count >= maximumBytes)
            throw new EmailLimitException("MaxReportBytes", maximumBytes, output.Count + 1);
        output.Add(value);
    }

    static InvalidOperationException Invalid(string message)
    {
        return new InvalidOperationException(message);
    }
}
