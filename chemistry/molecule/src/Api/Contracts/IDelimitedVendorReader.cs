using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="DelimitedVendorReader"/>.
/// </summary>
public interface IDelimitedVendorReader
{

    /// <summary>
    /// Reads all available records and returns their molecules in input order.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<Molecule> ReadAll(string? text);

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    VendorReadResult ReadWithResult(string? text);

    /// <summary>
    /// Reads delimited vendor data using the supplied delimiter and molecule-column configuration.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="format">The format.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    VendorReadResult ReadWithFormat(string? text, VendorFormat? format);
}
