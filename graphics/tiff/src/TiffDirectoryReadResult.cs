namespace tiff;

internal class TiffDirectoryReadResult
{
    internal bool Success;
    internal TiffDirectory[] Directories;
    internal TiffDiagnostics Diagnostics;

    internal TiffDirectoryReadResult()
    {
        Success = true;
        Directories = new TiffDirectory[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDirectoryReadResult Create(TiffDirectory[] directories, TiffDiagnostics diagnostics)
    {
        TiffDirectoryReadResult value = new TiffDirectoryReadResult();
        value.Initialize(directories, diagnostics);
        return value;
    }

    internal void Initialize(TiffDirectory[] directories, TiffDiagnostics diagnostics)
    {
        Directories = CopyDirectories(directories);
        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }

    internal int DirectoryCount()
    {
        return Directories.Length;
    }

    internal static TiffDirectory[] CopyDirectories(TiffDirectory[] directories)
    {
        if (directories == null)
        {
            return new TiffDirectory[0];
        }

        TiffDirectory[] copy = new TiffDirectory[directories.Length];
        int index = 0;
        while (index < directories.Length)
        {
            if (directories[index] == null)
            {
                copy[index] = new TiffDirectory();
            }
            else
            {
                copy[index] = directories[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
