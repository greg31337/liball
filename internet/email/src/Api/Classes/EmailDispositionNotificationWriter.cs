using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Writes disposition notification in its wire or storage format.
/// </summary>
public class EmailDispositionNotificationWriter : IEmailDispositionNotificationWriterContract
{
    internal static byte[] WriteImplementationWithNotification(EmailDispositionNotification notification)
    {
        return WriteImplementationWithNotificationAndMaximumBytes(notification, EmailReportOptionsConstants.DefaultMaximumReportBytes);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "notification">The notification used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] WriteWithNotification(EmailDispositionNotification notification)
    {
        return EmailDispositionNotificationWriter.WriteImplementationWithNotification(notification);
    }

    internal static byte[] WriteImplementationWithNotificationAndMaximumBytes(EmailDispositionNotification notification, int maximumBytes)
    {
        if (notification == null)
            throw new ArgumentNullException("notification");
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException("maximumBytes");
        Validate(notification);
        byte[] raw = (notification.GetFieldBlock() ?? throw new InvalidOperationException()).GetRawBytes();
        ValidateEncoding(raw, notification.IsInternationalized());
        return Normalize(raw, maximumBytes);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "notification">The notification used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte[] WriteWithNotificationAndMaximumBytes(EmailDispositionNotification notification, int maximumBytes)
    {
        return EmailDispositionNotificationWriter.WriteImplementationWithNotificationAndMaximumBytes(notification, maximumBytes);
    }

    internal static EmailComposer CreateReportImplementationWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadable(EmailDispositionNotification notification, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable)
    {
        return CreateReportImplementationWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(notification, original, reporter, recipient, humanReadable, new EmailReportOptions());
    }

    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "notification">The notification used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadable(EmailDispositionNotification notification, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable)
    {
        return EmailDispositionNotificationWriter.CreateReportImplementationWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadable(notification, original, reporter, recipient, humanReadable);
    }

    internal static EmailComposer CreateReportImplementationWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailDispositionNotification notification, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options)
    {
        if (notification == null)
            throw new ArgumentNullException("notification");
        if (options == null)
            throw new ArgumentNullException("options");
        ValidateOriginalMessageId(notification, original);
        EmailReportOptions snapshot = options.Clone();
        byte[] fields = WriteImplementationWithNotificationAndMaximumBytes(notification, snapshot.GetMaximumReportBytes());
        if (notification.IsInternationalized())
            return new EmailReportBuilder().CreateInternationalDispositionNotification(original, reporter, recipient, humanReadable, fields, snapshot);
        return new EmailReportBuilder().CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, reporter, recipient, humanReadable, fields, snapshot);
    }

    /// <summary>
    /// Creates a report configured for the requested behavior.
    /// </summary>
    /// <param name = "notification">The notification used by the operation.</param>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "reporter">The reporter used by the operation.</param>
    /// <param name = "recipient">The recipient used by the operation.</param>
    /// <param name = "humanReadable">The human readable used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailComposer CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(EmailDispositionNotification notification, EmailMessage original, EmailAddress reporter, EmailAddress recipient, string humanReadable, EmailReportOptions options)
    {
        return EmailDispositionNotificationWriter.CreateReportImplementationWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(notification, original, reporter, recipient, humanReadable, options);
    }

    internal static void ApplyRequestImplementation(EmailMessage message, EmailDispositionNotificationRequest request)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (request == null)
            throw new ArgumentNullException("request");
        EmailAddressFormatOptions addressOptions = new EmailAddressFormatOptions();
        addressOptions.SetFirstLineColumn("Disposition-Notification-To: ".Length);
        if (request.IsInternationalized())
        {
            addressOptions.SetAllowInternationalized(true);
            addressOptions.SetDisplayNameFormat(EmailAddressDisplayNameFormat.Unicode);
            addressOptions.SetDomainFormat(EmailAddressDomainFormat.Unicode);
        }

        EmailAddressFormatResult formatted = request.GetNotificationTo().Format(addressOptions);
        if (!formatted.IsSuccess())
            throw new InvalidOperationException("The notification mailbox list cannot be formatted safely.");
        List<EmailHeader> replacements = new List<EmailHeader>();
        replacements.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Disposition-Notification-To", formatted.GetBytes(), EmailHeaderValueKind.AddressList));
        if (request.HasOriginalRecipient())
        {
            EmailDispositionNotificationTypedValue? original = request.GetOriginalRecipient();
            string text = (original ?? throw new InvalidOperationException()).GetTypeName() + "; " + original.GetValue();
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (!request.IsInternationalized() && ContainsNonAscii(bytes))
                throw new InvalidOperationException("A noninternationalized MDN request cannot contain a UTF-8 Original-Recipient value.");
            replacements.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Original-Recipient", bytes));
        }

        if (request.GetOptionCount() > 0)
        {
            string text = EmailDispositionNotificationRequestValueParser.FormatOptions(request.GetOptions());
            replacements.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Disposition-Notification-Options", Encoding.ASCII.GetBytes(text)));
        }

        EmailHeaders? headers = message.GetHeaders();
        (headers ?? throw new InvalidOperationException()).RemoveAll("Disposition-Notification-To");
        headers.RemoveAll("Original-Recipient");
        headers.RemoveAll("Disposition-Notification-Options");
        int index = 0;
        while (index < replacements.Count)
        {
            headers.Add(replacements[index]);
            index++;
        }
    }

    /// <summary>
    /// Applies the request.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "request">The request used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void ApplyRequest(EmailMessage message, EmailDispositionNotificationRequest request)
    {
        EmailDispositionNotificationWriter.ApplyRequestImplementation(message, request);
    }

    static void Validate(EmailDispositionNotification value)
    {
        EmailReportFieldBlock? fields = value.GetFieldBlock();
        if (!(fields ?? throw new InvalidOperationException()).IsSuccess())
            throw new InvalidOperationException("The MDN field block contains malformed field syntax.");
        ValidateOptional(fields, "Reporting-UA", value.GetReportingUserAgent() != null);
        ValidateOptional(fields, "MDN-Gateway", value.GetGateway() != null);
        ValidateOptional(fields, "Original-Recipient", value.GetOriginalRecipient() != null);
        ValidateRequired(fields, "Final-Recipient", value.GetFinalRecipient() != null);
        ValidateOptional(fields, "Original-Message-ID", value.GetOriginalMessageId() != null);
        ValidateRequired(fields, "Disposition", value.GetDisposition() != null);
    }

    static void ValidateOriginalMessageId(EmailDispositionNotification notification, EmailMessage original)
    {
        if (original == null)
            throw new ArgumentNullException("original");
        EmailHeader? field = (original.GetHeaders() ?? throw new InvalidOperationException()).GetFirst("Message-ID");
        if (field == null)
        {
            if (notification.HasOriginalMessageId())
                throw new InvalidOperationException("Original-Message-ID cannot be emitted when the original message has no Message-ID.");
            return;
        }

        EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseMessageIdWithInputAndMode(field.GetUnfoldedValue(), EmailMessageIdParseMode.Strict);
        if (!parsed.IsSuccess() || !parsed.IsComplete() || !parsed.HasSingleIdentifier())
            throw new InvalidOperationException("The original message has a malformed Message-ID.");
        if (!notification.HasOriginalMessageId())
            throw new InvalidOperationException("Original-Message-ID is required when the original message has a Message-ID.");
        EmailMessageId? expected = notification.GetOriginalMessageId();
        EmailMessageId? actual = parsed.GetIdentifier();
        if (!string.Equals((expected ?? throw new InvalidOperationException()).GetIdLeft(), (actual ?? throw new InvalidOperationException()).GetIdLeft(), StringComparison.Ordinal) || !string.Equals(expected.GetIdRight(), actual.GetIdRight(), StringComparison.OrdinalIgnoreCase) || expected.IsRightDomainLiteral() != actual.IsRightDomainLiteral())
        {
            throw new InvalidOperationException("Original-Message-ID must match the original message.");
        }
    }

    static void ValidateRequired(EmailReportFieldBlock fields, string name, bool valid)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count != 1 || !valid)
            throw new InvalidOperationException("The MDN requires exactly one valid " + name + " field.");
    }

    static void ValidateOptional(EmailReportFieldBlock fields, string name, bool valid)
    {
        int count = fields.GetFieldsNamed(name).Count;
        if (count > 1 || count == 1 && !valid)
            throw new InvalidOperationException("An optional MDN field is duplicated or malformed.");
    }

    static byte[] Normalize(byte[] value, int maximumBytes)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current == 0)
                throw new InvalidOperationException("MDN fields cannot contain NUL bytes.");
            if (current == 13 || current == 10)
            {
                if (current == 13 && index + 1 < value.Length && value[index + 1] == 10)
                    index++;
                Append(output, 13, maximumBytes);
                Append(output, 10, maximumBytes);
            }
            else
                Append(output, current, maximumBytes);
            index++;
        }

        if (output.Count == 0 || output[output.Count - 1] != 10)
        {
            Append(output, 13, maximumBytes);
            Append(output, 10, maximumBytes);
        }

        return output.ToArray();
    }

    static void ValidateEncoding(byte[] value, bool internationalized)
    {
        if (internationalized)
        {
            if (!new EmailUtf8().IsValid(value))
                throw new InvalidOperationException("Internationalized MDN fields must contain valid UTF-8.");
            return;
        }

        if (ContainsNonAscii(value))
            throw new InvalidOperationException("Noninternationalized MDN fields must contain only ASCII bytes.");
    }

    static bool ContainsNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                return true;
            index++;
        }

        return false;
    }

    static void Append(List<byte> output, byte value, int maximumBytes)
    {
        if (output.Count >= maximumBytes)
            throw new EmailLimitException("MaxReportBytes", maximumBytes, output.Count + 1);
        output.Add(value);
    }
}
