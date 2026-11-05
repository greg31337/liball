namespace fix;

/// <summary>Validates a FIX message against selected dictionary layers.</summary>
public interface IMessageValidator
{
    /// <summary>Validates one message without mutating it.</summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="dictionary">The selected canonical dictionary.</param>
    /// <param name="options">The selected layers and validation behavior.</param>
    /// <param name="context">The session and external validation state.</param>
    /// <returns>All retained validation issues.</returns>
    ValidationResult ValidateMessage(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context);
}
