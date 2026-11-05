namespace molecule;


/// <summary>
/// Reads molfile data into molecular objects.
/// </summary>
public class MolfileReader : IMoleculeReader, IMolfileReader
{
    internal V2000Reader V2000;
    internal V3000Reader V3000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MolfileReader"/> class.
    /// </summary>
    public MolfileReader()
    {
        V2000 = new V2000Reader();
        V3000 = new V3000Reader();
    }

    /// <summary>
    /// Reads one molecule from the supplied format text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Read(string? text)
    {
        MolfileReadResult result = ReadWithResult(text);
        return result.Molecule;
    }

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public MolfileReadResult ReadWithResult(string? text)
    {
        return ReadMolfileWithOptions(text, null);
    }

    /// <summary>
    /// Reads a molfile using explicit version, feature, and count policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The molfile read options; <c>null</c> uses automatic feature-preserving defaults.</param>
    /// <returns>The molecule, selected version, feature counts, and diagnostics.</returns>
    public MolfileReadResult ReadMolfileWithOptions(string? text, MolfileReadOptions? options)
    {
        if (options != null && options.Version == MolfileVersion.V2000)
        {
            return V2000.ReadV2000WithOptions(text, options);
        }

        if (options != null && options.Version == MolfileVersion.V3000)
        {
            return V3000.ReadV3000WithOptions(text, options);
        }

        if (IsV3000(text))
        {
            return V3000.ReadV3000WithOptions(text, options);
        }

        return V2000.ReadV2000WithOptions(text, options);
    }

    internal bool IsV3000(string? text)
    {
        string value = NormalizeString(text);
        if (value.Contains("V3000") || value.Contains("M  V30"))
        {
            return true;
        }

        return false;
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
