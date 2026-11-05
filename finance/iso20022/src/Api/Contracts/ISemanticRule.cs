using System.Collections.Generic;

namespace iso20022;

/// <summary>Evaluates one caller-supplied semantic rule against a validation request and applicable path.</summary>
public interface ISemanticRule
{
    /// <summary>Returns the stable identity, ordering, severity, and applicability of this rule.</summary>
    /// <returns>The semantic rule descriptor.</returns>
    SemanticRuleDescriptor GetSemanticRuleDescriptor();

    /// <summary>Evaluates this rule for one applicable whole-message or qualified-path scope.</summary>
    /// <param name="request">The complete message representation, policy, and caller context.</param>
    /// <param name="path">The applicable descriptor path, or an empty path for whole-message evaluation.</param>
    /// <returns>The ordered diagnostics produced by the rule, or an empty list when the rule passes.</returns>
    List<ValidationDiagnostic> ValidateSemanticRule(
        ValidationRequest? request,
        XmlPathData? path);
}
