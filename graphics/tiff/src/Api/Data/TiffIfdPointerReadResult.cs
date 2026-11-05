namespace tiff;

/// <summary>Contains nested IFD pointers and diagnostics produced by pointer traversal.</summary>
public class TiffIfdPointerReadResult
{
    /// <summary>Indicates whether pointer traversal completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains EXIF, GPS, and interoperability pointers in traversal order.</summary>
    public TiffIfdPointer[] Pointers;
    /// <summary>Contains diagnostics produced while resolving pointers.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty successful pointer traversal result.</summary>
    public TiffIfdPointerReadResult()
    {
        Success = true;
        Pointers = new TiffIfdPointer[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffIfdPointerReadResult Create(TiffIfdPointer[] pointers, TiffDiagnostics diagnostics)
    {
        TiffIfdPointerReadResult value = new TiffIfdPointerReadResult();
        value.Initialize(pointers, diagnostics);
        return value;
    }

    internal void Initialize(TiffIfdPointer[] pointers, TiffDiagnostics diagnostics)
    {
        Pointers = CopyPointers(pointers);
        if (diagnostics == null)
        {
            Diagnostics = new TiffDiagnostics();
        }
        else
        {
            Diagnostics = diagnostics;
        }

        Success = !Diagnostics.HasErrors();
    }

    internal int PointerCount()
    {
        return Pointers.Length;
    }

    internal static TiffIfdPointer[] CopyPointers(TiffIfdPointer[] pointers)
    {
        if (pointers == null)
        {
            return new TiffIfdPointer[0];
        }

        TiffIfdPointer[] copy = new TiffIfdPointer[pointers.Length];
        int index = 0;
        while (index < pointers.Length)
        {
            if (pointers[index] == null)
            {
                copy[index] = new TiffIfdPointer();
            }
            else
            {
                copy[index] = pointers[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
