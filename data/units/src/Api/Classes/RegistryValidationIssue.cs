namespace units;

/// <summary>
/// Describes one consistency problem found in a unit registry.
/// </summary>
public class RegistryValidationIssue : IRegistryValidationIssue
{
    internal RegistryValidationIssueKind _kind;
    internal string _subjectId;
    internal string _detail;
    internal string _message;

    internal RegistryValidationIssue(RegistryValidationIssueKind kind, string subjectId, string? detail, string? message)
    {
        if (!IsValidKind(kind))
        {
            throw new System.ArgumentException("Registry validation issue kind is not supported.");
        }

        ValidateRequiredText(subjectId, "subjectId");

        _kind = kind;
        _subjectId = subjectId;
        if (detail == null)
        {
            _detail = "";
        }
        else
        {
            _detail = detail;
        }

        if (message == null)
        {
            _message = "";
        }
        else
        {
            _message = message;
        }
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The resulting registry validation issue kind.</returns>
    public RegistryValidationIssueKind GetKind()
    {
        return _kind;
    }

    /// <summary>
    /// Gets the subject id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSubjectId()
    {
        return _subjectId;
    }

    /// <summary>
    /// Gets the detail.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDetail()
    {
        return _detail;
    }

    /// <summary>
    /// Gets the developer-readable result message, which is empty for success.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetMessage()
    {
        return _message;
    }

    /// <summary>
    /// Reports whether the issue has the supplied kind.
    /// </summary>
    /// <param name="kind">The metadata or result kind.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsKind(RegistryValidationIssueKind kind)
    {
        return _kind == kind;
    }

    internal static bool IsValidKind(RegistryValidationIssueKind kind)
    {
        return kind == RegistryValidationIssueKind.MissingDimension
            || kind == RegistryValidationIssueKind.MissingDefaultUnit
            || kind == RegistryValidationIssueKind.DuplicateAlias
            || kind == RegistryValidationIssueKind.IncompatibleQuantityKindMapping
            || kind == RegistryValidationIssueKind.MissingAcceptedUnit
            || kind == RegistryValidationIssueKind.MissingSystemBaseUnit
            || kind == RegistryValidationIssueKind.MissingPreferredQuantityKind
            || kind == RegistryValidationIssueKind.MissingPreferredUnit
            || kind == RegistryValidationIssueKind.IncompatiblePreferredUnitMapping
            || kind == RegistryValidationIssueKind.DuplicateMetadataId
            || kind == RegistryValidationIssueKind.DuplicateMetadataAlias
            || kind == RegistryValidationIssueKind.InvalidEquivalency
            || kind == RegistryValidationIssueKind.InvalidSpecialConversionMetadata
            || kind == RegistryValidationIssueKind.InvalidPhysicalConstantMetadata;
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Registry validation issue " + fieldName + " must not be empty.");
        }
    }
}
