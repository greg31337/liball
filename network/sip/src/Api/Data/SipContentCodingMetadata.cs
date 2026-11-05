namespace sip;

/// <summary>Identifies transport content coding and its optional SigComp compartment.</summary>
public class SipContentCodingMetadata
{
    /// <summary>The case-insensitive transport content-coding name.</summary>
    public string? Name;
    /// <summary>The SigComp compartment metadata when the coding is <c>sigcomp</c>.</summary>
    public SipSigCompCompartment? SigCompCompartment;
}
