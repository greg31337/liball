namespace ttf;

/// <summary>Contains a PDF FontDescriptor flag value and every inference.</summary>
public class PdfFontDescriptorFlagsReport : IPdfFontDescriptorFlagsReport
{
    int flags;
    PdfFontDescriptorFlagInference[] inferences;

    internal PdfFontDescriptorFlagsReport(
        int flags,
        PdfFontDescriptorFlagInference[] inferences)
    {
        this.flags = flags;
        this.inferences = CopyInferences(inferences);
    }

    /// <summary>Gets the complete PDF FontDescriptor <c>Flags</c> value.</summary>
    /// <returns>The bitwise combination of all set flags.</returns>
    public int GetFlags()
    {
        return flags;
    }

    /// <summary>Reports whether one descriptor flag is set.</summary>
    /// <param name="flag">The individual descriptor flag.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>flag</c> is not an individual defined flag.
    /// </exception>
    public bool HasFlag(PdfFontDescriptorFlag flag)
    {
        return FindInference(flag).IsSet();
    }

    /// <summary>Gets the number of reported flag inferences.</summary>
    /// <returns>Nine, one for every defined descriptor flag.</returns>
    public int GetInferenceCount()
    {
        return inferences.Length;
    }

    /// <summary>Gets one inference in PDF flag order.</summary>
    /// <param name="index">The zero-based inference index.</param>
    /// <returns>The immutable inference.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the report.
    /// </exception>
    public PdfFontDescriptorFlagInference GetInference(int index)
    {
        if (index < 0 || index >= inferences.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return inferences[index];
    }

    /// <summary>Finds the inference for one descriptor flag.</summary>
    /// <param name="flag">The individual descriptor flag.</param>
    /// <returns>The matching immutable inference.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>flag</c> is not an individual defined flag.
    /// </exception>
    public PdfFontDescriptorFlagInference FindInference(
        PdfFontDescriptorFlag flag)
    {
        int index = 0;

        while (index < inferences.Length)
        {
            if (inferences[index].GetFlag() == flag)
            {
                return inferences[index];
            }

            index += 1;
        }

        throw new System.ArgumentOutOfRangeException("flag");
    }

    /// <summary>Copies every inference in PDF flag order.</summary>
    /// <returns>A new array containing immutable inferences.</returns>
    public PdfFontDescriptorFlagInference[] GetInferences()
    {
        return CopyInferences(inferences);
    }

    static PdfFontDescriptorFlagInference[] CopyInferences(
        PdfFontDescriptorFlagInference[] values)
    {
        PdfFontDescriptorFlagInference[] copy =
            new PdfFontDescriptorFlagInference[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
