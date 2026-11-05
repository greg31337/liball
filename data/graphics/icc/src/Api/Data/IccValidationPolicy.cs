namespace icc;

/// <summary>Defines built-in and caller-selected profile validation policy.</summary>
public class IccValidationPolicy
{
    /// <summary>Gets or sets the base validation mode.</summary>
    public IccValidationMode Mode;
    /// <summary>Gets or sets additional required rule identifiers.</summary>
    public string[] RequiredRuleIds;
    /// <summary>Gets or sets rule identifiers to suppress.</summary>
    public string[] SuppressedRuleIds;
    /// <summary>Gets or sets whether private signatures are permitted.</summary>
    public bool AllowPrivateSignatures;
    /// <summary>Gets or sets additional ICC.2 tag signatures that must be present.</summary>
    public uint[] Icc2RequiredTagSignatures;
    /// <summary>Gets or sets ICC.2 tag signatures permitted by an optional tag allow-list.</summary>
    public uint[] Icc2OptionalTagSignatures;
    /// <summary>Gets or sets additional ICC.2 tag signatures that must be absent.</summary>
    public uint[] Icc2ForbiddenTagSignatures;
    /// <summary>Gets or sets whether ICC.2 tags absent from the required and optional lists are forbidden.</summary>
    public bool Icc2ForbidUnlistedTags;
    /// <summary>Gets or sets the ICC.2 processing-element signatures supported by the selected policy.</summary>
    public uint[] Icc2SupportedProcessingElementSignatures;
    /// <summary>Gets or sets whether ICC.2 processing elements absent from the support list are forbidden.</summary>
    public bool Icc2ForbidUnlistedProcessingElements;
    /// <summary>Gets or sets additional ICC.2 transform and processing-element channel rules.</summary>
    public IccValidationChannelRule[] Icc2ChannelRules;
    /// <summary>Gets or sets whether the selected ICC.2 policy requires a profile subclass.</summary>
    public bool Icc2RequireProfileSubclass;
    /// <summary>Gets or sets accepted ICC.2 profile-subclass signatures and contexts.</summary>
    public IccValidationSubclassRule[] Icc2ProfileSubclassRules;
    /// <summary>Gets or sets operation resource limits.</summary>
    public IccResourceLimits ResourceLimits;

    /// <summary>Initializes conforming validation with conservative limits.</summary>
    public IccValidationPolicy()
    {
        Mode = IccValidationMode.Conforming;
        RequiredRuleIds = new string[0];
        SuppressedRuleIds = new string[0];
        AllowPrivateSignatures = true;
        Icc2RequiredTagSignatures = new uint[0];
        Icc2OptionalTagSignatures = new uint[0];
        Icc2ForbiddenTagSignatures = new uint[0];
        Icc2ForbidUnlistedTags = false;
        Icc2SupportedProcessingElementSignatures = new uint[0];
        Icc2ForbidUnlistedProcessingElements = false;
        Icc2ChannelRules = new IccValidationChannelRule[0];
        Icc2RequireProfileSubclass = false;
        Icc2ProfileSubclassRules = new IccValidationSubclassRule[0];
        ResourceLimits = new IccResourceLimits();
    }
}
