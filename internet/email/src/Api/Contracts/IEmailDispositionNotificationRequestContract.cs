using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationRequest"/>.
/// </summary>
public interface IEmailDispositionNotificationRequestContract
{
    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    bool IsInternationalized();
    /// <summary>
    /// Gets the notification to.
    /// </summary>
    /// <returns>The current notification to.</returns>
    EmailAddressList GetNotificationTo();
    /// <summary>
    /// Determines whether the original recipient is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original recipient is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalRecipient();
    /// <summary>
    /// Gets the original recipient.
    /// </summary>
    /// <returns>The current original recipient.</returns>
    EmailDispositionNotificationTypedValue? GetOriginalRecipient();
    /// <summary>
    /// Gets the option count.
    /// </summary>
    /// <returns>The current option count.</returns>
    int GetOptionCount();
    /// <summary>
    /// Gets the option at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested option.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailDispositionNotificationOption GetOptionAt(int index);
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current options.</returns>
    List<EmailDispositionNotificationOption> GetOptions();
}
