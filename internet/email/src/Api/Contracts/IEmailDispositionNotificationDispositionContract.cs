using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationDisposition"/>.
/// </summary>
public interface IEmailDispositionNotificationDispositionContract
{
    /// <summary>
    /// Gets the action mode.
    /// </summary>
    /// <returns>The current action mode.</returns>
    EmailDispositionActionMode GetActionMode();
    /// <summary>
    /// Gets the sending mode.
    /// </summary>
    /// <returns>The current sending mode.</returns>
    EmailDispositionSendingMode GetSendingMode();
    /// <summary>
    /// Gets the disposition type.
    /// </summary>
    /// <returns>The current disposition type.</returns>
    EmailDispositionNotificationType GetDispositionType();
    /// <summary>
    /// Gets the disposition type name.
    /// </summary>
    /// <returns>The current disposition type name.</returns>
    string? GetDispositionTypeName();
    /// <summary>
    /// Gets the modifier count.
    /// </summary>
    /// <returns>The current modifier count.</returns>
    int GetModifierCount();
    /// <summary>
    /// Gets the modifier at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested modifier.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetModifierAt(int index);
    /// <summary>
    /// Gets the modifiers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current modifiers.</returns>
    List<string> GetModifiers();
    /// <summary>
    /// Determines whether the modifier is present or available.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the modifier is available; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool HasModifier(string value);
}
