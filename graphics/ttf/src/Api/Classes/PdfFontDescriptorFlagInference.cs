namespace ttf;

/// <summary>Explains one PDF FontDescriptor flag decision.</summary>
public class PdfFontDescriptorFlagInference : IPdfFontDescriptorFlagInference
{
    PdfFontDescriptorFlag flag;
    bool set;
    PdfFontDescriptorFlagEvidence evidence;
    string explanation;

    internal PdfFontDescriptorFlagInference(
        PdfFontDescriptorFlag flag,
        bool set,
        PdfFontDescriptorFlagEvidence evidence,
        string explanation)
    {
        this.flag = flag;
        this.set = set;
        this.evidence = evidence;
        this.explanation = explanation;
    }

    /// <summary>Gets the descriptor flag being inferred.</summary>
    /// <returns>One PDF descriptor flag.</returns>
    public PdfFontDescriptorFlag GetFlag()
    {
        return flag;
    }

    /// <summary>Reports whether the descriptor flag is set.</summary>
    /// <returns>True when the bit occurs in the final value.</returns>
    public bool IsSet()
    {
        return set;
    }

    /// <summary>Gets the metadata category used by the inference.</summary>
    /// <returns>The evidence category.</returns>
    public PdfFontDescriptorFlagEvidence GetEvidence()
    {
        return evidence;
    }

    /// <summary>Gets a stable explanation of the inference.</summary>
    /// <returns>The evidence and decision summary.</returns>
    public string GetExplanation()
    {
        return explanation;
    }
}
