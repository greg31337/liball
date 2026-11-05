using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageClassificationReason"/>.
/// </summary>
public interface IEmailMessageClassificationReasonContract
{
    /// <summary>
    /// Gets the classification kind.
    /// </summary>
    /// <returns>The current classification kind.</returns>
    EmailMessageClassificationKind GetClassificationKind();
    /// <summary>
    /// Gets the code.
    /// </summary>
    /// <returns>The current code.</returns>
    EmailMessageClassificationReasonCode GetCode();
    /// <summary>
    /// Gets the strength.
    /// </summary>
    /// <returns>The current strength.</returns>
    EmailMessageClassificationEvidenceStrength GetStrength();
    /// <summary>
    /// Gets the effect.
    /// </summary>
    /// <returns>The current effect.</returns>
    EmailMessageClassificationEvidenceEffect GetEffect();
    /// <summary>
    /// Determines whether the current value or state is supporting.
    /// </summary>
    /// <returns><c>true</c> when the value or state is supporting; otherwise, <c>false</c>.</returns>
    bool IsSupporting();
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    string GetDescription();
    /// <summary>
    /// Gets the standard.
    /// </summary>
    /// <returns>The current standard.</returns>
    string GetStandard();
    /// <summary>
    /// Determines whether the field name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the field name is available; otherwise, <c>false</c>.</returns>
    bool HasFieldName();
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    string? GetFieldName();
}
