namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontDescriptorFlagInference"/>.</summary>
public interface IPdfFontDescriptorFlagInference
{
    /// <summary>Gets the descriptor flag being inferred.</summary>
    /// <returns>One PDF descriptor flag.</returns>
    PdfFontDescriptorFlag GetFlag();

    /// <summary>Reports whether the descriptor flag is set.</summary>
    /// <returns>True when the bit occurs in the final value.</returns>
    bool IsSet();

    /// <summary>Gets the metadata category used by the inference.</summary>
    /// <returns>The evidence category.</returns>
    PdfFontDescriptorFlagEvidence GetEvidence();

    /// <summary>Gets a stable explanation of the inference.</summary>
    /// <returns>The evidence and decision summary.</returns>
    string GetExplanation();
}
