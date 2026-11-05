using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a message classification reason.
/// </summary>
public class EmailMessageClassificationReason : IEmailMessageClassificationReasonContract
{
    EmailMessageClassificationKind classificationKind;
    EmailMessageClassificationReasonCode code;
    EmailMessageClassificationEvidenceStrength strength;
    EmailMessageClassificationEvidenceEffect effect;
    string description;
    string standard;
    string? fieldName;
    /// <summary>
    /// Creates a new message classification reason.
    /// </summary>
    /// <param name = "classificationKind">The classification kind used by the operation.</param>
    /// <param name = "code">The code used by the operation.</param>
    /// <param name = "strength">The strength used by the operation.</param>
    /// <param name = "effect">The effect used by the operation.</param>
    /// <param name = "description">The description used by the operation.</param>
    /// <param name = "standard">The standard used by the operation.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMessageClassificationReason(EmailMessageClassificationKind classificationKind, EmailMessageClassificationReasonCode code, EmailMessageClassificationEvidenceStrength strength, EmailMessageClassificationEvidenceEffect effect, string description, string standard, string fieldName)
    {
        if (classificationKind < EmailMessageClassificationKind.Automated || classificationKind > EmailMessageClassificationKind.Receipt)
            throw new ArgumentOutOfRangeException("classificationKind");
        if (code < EmailMessageClassificationReasonCode.AutoSubmittedGenerated || code > EmailMessageClassificationReasonCode.ReceiptSubject)
            throw new ArgumentOutOfRangeException("code");
        if (strength < EmailMessageClassificationEvidenceStrength.Weak || strength > EmailMessageClassificationEvidenceStrength.Strong)
            throw new ArgumentOutOfRangeException("strength");
        if (effect < EmailMessageClassificationEvidenceEffect.Supports || effect > EmailMessageClassificationEvidenceEffect.Contradicts)
            throw new ArgumentOutOfRangeException("effect");
        if (description == null || description.Length == 0)
            throw new ArgumentException("A classification reason requires a description.", "description");
        if (standard == null || standard.Length == 0)
            throw new ArgumentException("A classification reason requires a standards reference.", "standard");
        if (fieldName != null && fieldName.Length == 0)
            throw new ArgumentException("A field name cannot be empty when supplied.", "fieldName");
        this.classificationKind = classificationKind;
        this.code = code;
        this.strength = strength;
        this.effect = effect;
        this.description = description;
        this.standard = standard;
        this.fieldName = fieldName;
    }

    /// <summary>
    /// Gets the classification kind.
    /// </summary>
    /// <returns>The current classification kind.</returns>
    public EmailMessageClassificationKind GetClassificationKind()
    {
        return classificationKind;
    }

    /// <summary>
    /// Gets the code.
    /// </summary>
    /// <returns>The current code.</returns>
    public EmailMessageClassificationReasonCode GetCode()
    {
        return code;
    }

    /// <summary>
    /// Gets the strength.
    /// </summary>
    /// <returns>The current strength.</returns>
    public EmailMessageClassificationEvidenceStrength GetStrength()
    {
        return strength;
    }

    /// <summary>
    /// Gets the effect.
    /// </summary>
    /// <returns>The current effect.</returns>
    public EmailMessageClassificationEvidenceEffect GetEffect()
    {
        return effect;
    }

    /// <summary>
    /// Determines whether the current value or state is supporting.
    /// </summary>
    /// <returns><c>true</c> when the value or state is supporting; otherwise, <c>false</c>.</returns>
    public bool IsSupporting()
    {
        return effect == EmailMessageClassificationEvidenceEffect.Supports;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    public string GetDescription()
    {
        return description;
    }

    /// <summary>
    /// Gets the standard.
    /// </summary>
    /// <returns>The current standard.</returns>
    public string GetStandard()
    {
        return standard;
    }

    /// <summary>
    /// Determines whether the field name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the field name is available; otherwise, <c>false</c>.</returns>
    public bool HasFieldName()
    {
        return fieldName != null;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    public string? GetFieldName()
    {
        return fieldName;
    }
}
