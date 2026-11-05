namespace avif;

/// <summary>Reports deterministic coded order and reference decisions for an authored sequence.</summary>
public class AvifSequenceEncodingPlan
{
    /// <summary>The planning result.</summary>
    public AvifResultCode Code;

    /// <summary>The decisions in coded order.</summary>
    public AvifFrameEncodingDecision[] Frames;

    internal AvifSequenceEncodingPlan(AvifResultCode code)
    {
        Code = code;
        Frames = System.Array.Empty<AvifFrameEncodingDecision>();
    }
}
