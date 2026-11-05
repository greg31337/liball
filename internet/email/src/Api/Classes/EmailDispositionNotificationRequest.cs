using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a disposition notification request.
/// </summary>
public class EmailDispositionNotificationRequest : IEmailDispositionNotificationRequestContract
{
    bool internationalized;
    EmailAddressList notificationTo;
    EmailDispositionNotificationTypedValue? originalRecipient;
    List<EmailDispositionNotificationOption> options;
    /// <summary>
    /// Creates a new disposition notification request.
    /// </summary>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "notificationTo">The notification to used by the operation.</param>
    /// <param name = "originalRecipient">The original recipient used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationRequest(bool internationalized, EmailAddressList notificationTo, EmailDispositionNotificationTypedValue? originalRecipient, List<EmailDispositionNotificationOption> options)
    {
        if (notificationTo == null)
            throw new ArgumentNullException("notificationTo");
        if (notificationTo.GetMailboxCount() == 0 || notificationTo.GetGroupCount() > 0)
            throw new ArgumentException("Disposition notifications require a nonempty mailbox list.", "notificationTo");
        if (options == null)
            throw new ArgumentNullException("options");
        int index = 0;
        while (index < options.Count)
        {
            if (options[index] == null)
                throw new ArgumentException("Notification options cannot contain null entries.", "options");
            index++;
        }

        this.internationalized = internationalized;
        this.notificationTo = notificationTo;
        this.originalRecipient = originalRecipient;
        this.options = new List<EmailDispositionNotificationOption>(options);
    }

    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    public bool IsInternationalized()
    {
        return internationalized;
    }

    /// <summary>
    /// Gets the notification to.
    /// </summary>
    /// <returns>The current notification to.</returns>
    public EmailAddressList GetNotificationTo()
    {
        return notificationTo;
    }

    /// <summary>
    /// Determines whether the original recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original recipient is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalRecipient()
    {
        return originalRecipient != null;
    }

    /// <summary>
    /// Gets the original recipient.
    /// </summary>
    /// <returns>The current original recipient.</returns>
    public EmailDispositionNotificationTypedValue? GetOriginalRecipient()
    {
        return originalRecipient;
    }

    /// <summary>
    /// Gets the option count.
    /// </summary>
    /// <returns>The current option count.</returns>
    public int GetOptionCount()
    {
        return options.Count;
    }

    /// <summary>
    /// Gets the option at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested option.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDispositionNotificationOption GetOptionAt(int index)
    {
        if (index < 0 || index >= options.Count)
            throw new ArgumentOutOfRangeException("index");
        return options[index];
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current options.</returns>
    public List<EmailDispositionNotificationOption> GetOptions()
    {
        return new List<EmailDispositionNotificationOption>(options);
    }
}
