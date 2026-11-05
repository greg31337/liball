namespace asn1;

/// <summary>
/// Contains the ordered operations in an encoding plan.
/// </summary>
public class AsnEncodingPlan
{
    /// <summary>
    /// Contains the stable plan identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Selects the encoding rules executed by the plan.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Contains the ordered plan steps.
    /// </summary>
    public AsnEncodingPlanStep[] Steps = [];

    /// <summary>
    /// Contains final encoding instructions applied to the plan.
    /// </summary>
    public AsnEncodingInstruction[] Instructions = [];

    /// <summary>
    /// Contains referenced ECN classes.
    /// </summary>
    public AsnEcnClass[] EcnClasses = [];

    /// <summary>
    /// Contains referenced ECN objects.
    /// </summary>
    public AsnEcnObject[] EcnObjects = [];

    /// <summary>
    /// Contains application PER instruction registrations captured by the plan.
    /// </summary>
    public AsnPerInstructionRegistration[] PerInstructionRegistrations = [];
}
