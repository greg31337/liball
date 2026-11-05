using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a disposition notification disposition.
/// </summary>
public class EmailDispositionNotificationDisposition : IEmailDispositionNotificationDispositionContract
{
    EmailDispositionActionMode actionMode;
    EmailDispositionSendingMode sendingMode;
    EmailDispositionNotificationType type;
    string? typeName;
    List<string> modifiers;
    /// <summary>
    /// Creates a new disposition notification disposition.
    /// </summary>
    /// <param name = "actionMode">The action mode used by the operation.</param>
    /// <param name = "sendingMode">The sending mode used by the operation.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "modifiers">The modifiers used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDispositionNotificationDisposition(EmailDispositionActionMode actionMode, EmailDispositionSendingMode sendingMode, EmailDispositionNotificationType type, List<string> modifiers)
    {
        if (actionMode != EmailDispositionActionMode.Manual && actionMode != EmailDispositionActionMode.Automatic)
            throw new ArgumentOutOfRangeException("actionMode");
        if (sendingMode != EmailDispositionSendingMode.Manual && sendingMode != EmailDispositionSendingMode.Automatic)
            throw new ArgumentOutOfRangeException("sendingMode");
        if (type < EmailDispositionNotificationType.Displayed || type > EmailDispositionNotificationType.Failed)
            throw new ArgumentOutOfRangeException("type");
        if (modifiers == null)
            throw new ArgumentNullException("modifiers");
        int index = 0;
        while (index < modifiers.Count)
        {
            if (!EmailDispositionNotificationValueParser.IsToken(modifiers[index]))
                throw new ArgumentException("Disposition modifiers must be nonempty tokens.", "modifiers");
            index++;
        }

        this.actionMode = actionMode;
        this.sendingMode = sendingMode;
        this.type = type;
        typeName = EmailDispositionNotificationValueParser.TypeName(type);
        this.modifiers = Normalize(modifiers);
    }

    /// <summary>
    /// Gets the action mode.
    /// </summary>
    /// <returns>The current action mode.</returns>
    public EmailDispositionActionMode GetActionMode()
    {
        return actionMode;
    }

    /// <summary>
    /// Gets the sending mode.
    /// </summary>
    /// <returns>The current sending mode.</returns>
    public EmailDispositionSendingMode GetSendingMode()
    {
        return sendingMode;
    }

    /// <summary>
    /// Gets the disposition type.
    /// </summary>
    /// <returns>The current disposition type.</returns>
    public EmailDispositionNotificationType GetDispositionType()
    {
        return type;
    }

    /// <summary>
    /// Gets the disposition type name.
    /// </summary>
    /// <returns>The current disposition type name.</returns>
    public string? GetDispositionTypeName()
    {
        return typeName;
    }

    /// <summary>
    /// Gets the modifier count.
    /// </summary>
    /// <returns>The current modifier count.</returns>
    public int GetModifierCount()
    {
        return modifiers.Count;
    }

    /// <summary>
    /// Gets the modifier at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested modifier.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetModifierAt(int index)
    {
        if (index < 0 || index >= modifiers.Count)
            throw new ArgumentOutOfRangeException("index");
        return modifiers[index];
    }

    /// <summary>
    /// Gets the modifiers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current modifiers.</returns>
    public List<string> GetModifiers()
    {
        return new List<string>(modifiers);
    }

    /// <summary>
    /// Determines whether the modifier is present or available.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the modifier is available; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool HasModifier(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        int index = 0;
        while (index < modifiers.Count)
        {
            if (string.Equals(modifiers[index], value, StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
    }

    static List<string> Normalize(List<string> values)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < values.Count)
        {
            result.Add(values[index].ToLowerInvariant());
            index++;
        }

        return result;
    }
}
