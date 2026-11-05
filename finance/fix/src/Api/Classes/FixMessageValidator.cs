using System.Collections.Generic;

namespace fix;

/// <summary>Validates FIX messages across the selected validation layers.</summary>
public class FixMessageValidator : IMessageValidator
{
    /// <summary>Creates a FIX message validator.</summary>
    public FixMessageValidator()
    {
    }

    /// <summary>Validates one message without mutating it.</summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="dictionary">The selected canonical dictionary.</param>
    /// <param name="options">The selected layers and validation behavior.</param>
    /// <param name="context">The session and external validation state.</param>
    /// <returns>All retained validation issues in deterministic layer and emission order.</returns>
    public ValidationResult ValidateMessage(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        List<ValidationIssue> candidates = [];
        candidates.AddRange(EnvelopeValidator.ValidateEnvelopeAndSession(message, dictionary, options, context));
        candidates.AddRange(DictionaryStructureValidator.ValidateDictionaryStructure(message, dictionary, options, context));
        candidates.AddRange(ScalarValueValidator.ValidateScalarValues(message, dictionary, options));
        candidates.AddRange(RepeatingGroupValidator.ValidateRepeatingGroups(message, dictionary, options, context));
        candidates.AddRange(ConditionalValidator.ValidateConditionalRules(message, dictionary, options, context));
        RejectMapper.MapValidationIssues(candidates, message);
        return ValidationCoordinator.CoordinateValidation(candidates, options);
    }
}
