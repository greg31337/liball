namespace iso20022;

/// <summary>Validates raw, generic, or typed ISO 20022 messages through a bounded deterministic stage pipeline.</summary>
public interface IMessageValidator
{
    /// <summary>Validates one message request and returns expected-invalid outcomes without throwing.</summary>
    /// <param name="request">The message representation, validation policy, profile, and caller context.</param><returns>The stable validation report.</returns>
    ValidationReport ValidateMessage(ValidationRequest? request);
}
