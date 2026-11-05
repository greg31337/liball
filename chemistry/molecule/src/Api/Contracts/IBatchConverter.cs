using System.IO;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="BatchConverter"/>.
/// </summary>
public interface IBatchConverter
{
    /// <summary>
    /// Configures the format service used by subsequent conversions.
    /// </summary>
    /// <param name="formats">The format service used by the input and output streams.</param>
    void ConfigureFormats(IMoleculeFormatService? formats);

    /// <summary>
    /// Reads molecules from one text stream, writes them in another format, and returns conversion totals and diagnostics.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="inputOptions">The options used to read the input.</param>
    /// <param name="outputOptions">The options used to write the output.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    ConversionReport Convert(TextReader reader, TextWriter writer, FormatOptions inputOptions, FormatOptions outputOptions);

    /// <summary>
    /// Reads, optionally filters, and writes molecule records while reporting deterministic progress snapshots.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="inputOptions">The options used to read the input.</param>
    /// <param name="outputOptions">The options used to write the output.</param>
    /// <param name="conversionOptions">The optional filter and progress contracts; <c>null</c> disables both.</param>
    /// <returns>Deterministic read, written, skipped, warning, and failure totals and all read diagnostics.</returns>
    ConversionReport ConvertWithOptions(TextReader reader, TextWriter writer, FormatOptions inputOptions, FormatOptions outputOptions, BatchConversionOptions? conversionOptions);
}
