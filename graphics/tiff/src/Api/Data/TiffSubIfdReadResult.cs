namespace tiff;

/// <summary>Contains the values and diagnostics produced by a sub IFD read operation.</summary>
public class TiffSubIfdReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the sub IFD s.</summary>
    public TiffSubIfd[] SubIfds;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Sub IFD Read Result class.</summary>
    public TiffSubIfdReadResult()
    {
        Success = true;
        SubIfds = new TiffSubIfd[0];
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Sub IFD Read Result class.</summary>
    /// <param name="subIfds">The sub IFD s.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffSubIfdReadResult Create(TiffSubIfd[] subIfds, TiffDiagnostics diagnostics)
    {
        TiffSubIfdReadResult value = new TiffSubIfdReadResult();
        value.Initialize(subIfds, diagnostics);
        return value;
    }

    internal void Initialize(TiffSubIfd[] subIfds, TiffDiagnostics diagnostics)
    {
        SubIfds = CopySubIfds(subIfds);
        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }

    /// <summary>Returns the number of available sub IFD entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int SubIfdCount()
    {
        return SubIfds.Length;
    }

    internal static TiffSubIfd[] CopySubIfds(TiffSubIfd[] subIfds)
    {
        if (subIfds == null)
        {
            return new TiffSubIfd[0];
        }

        TiffSubIfd[] copy = new TiffSubIfd[subIfds.Length];
        int index = 0;
        while (index < subIfds.Length)
        {
            if (subIfds[index] == null)
            {
                copy[index] = new TiffSubIfd();
            }
            else
            {
                copy[index] = subIfds[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
