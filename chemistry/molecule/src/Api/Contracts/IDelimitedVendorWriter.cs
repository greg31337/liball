using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines delimited vendor output operations provided by <see cref="DelimitedVendorWriter"/>.
/// </summary>
public interface IDelimitedVendorWriter
{
    /// <summary>
    /// Writes one molecule using explicit delimited vendor options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The output options; <c>null</c> uses CSV with embedded SMILES.</param>
    /// <returns>The resulting delimited text; an empty string indicates that no value could be produced.</returns>
    string WriteDelimitedVendor(Molecule? molecule, VendorWriteOptions? options);

    /// <summary>
    /// Writes molecule records using explicit columns, quoting, delimiter, and embedded format policies.
    /// </summary>
    /// <param name="molecules">The molecules to write in record order.</param>
    /// <param name="options">The output options; <c>null</c> uses CSV with embedded SMILES.</param>
    /// <returns>The output text, selected columns, record count, and diagnostics.</returns>
    VendorWriteResult WriteDelimitedVendorRecords(List<Molecule>? molecules, VendorWriteOptions? options);
}
