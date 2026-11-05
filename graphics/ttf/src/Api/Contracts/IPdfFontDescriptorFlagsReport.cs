namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontDescriptorFlagsReport"/>.</summary>
public interface IPdfFontDescriptorFlagsReport
{
    /// <summary>Gets the complete PDF FontDescriptor <c>Flags</c> value.</summary>
    /// <returns>The bitwise combination of all set flags.</returns>
    int GetFlags();

    /// <summary>Reports whether one descriptor flag is set.</summary>
    /// <param name="flag">The individual descriptor flag.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>flag</c> is not an individual defined flag.
    /// </exception>
    bool HasFlag(PdfFontDescriptorFlag flag);

    /// <summary>Gets the number of reported flag inferences.</summary>
    /// <returns>Nine, one for every defined descriptor flag.</returns>
    int GetInferenceCount();

    /// <summary>Gets one inference in PDF flag order.</summary>
    /// <param name="index">The zero-based inference index.</param>
    /// <returns>The immutable inference.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the report.
    /// </exception>
    PdfFontDescriptorFlagInference GetInference(int index);

    /// <summary>Finds the inference for one descriptor flag.</summary>
    /// <param name="flag">The individual descriptor flag.</param>
    /// <returns>The matching immutable inference.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>flag</c> is not an individual defined flag.
    /// </exception>
    PdfFontDescriptorFlagInference FindInference(PdfFontDescriptorFlag flag);

    /// <summary>Copies every inference in PDF flag order.</summary>
    /// <returns>A new array containing immutable inferences.</returns>
    PdfFontDescriptorFlagInference[] GetInferences();
}
