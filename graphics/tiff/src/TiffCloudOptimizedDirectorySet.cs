namespace tiff;

internal class TiffCloudOptimizedDirectorySet
{
    internal TiffDirectory[] Directories;
    internal TiffImageInfo[] Infos;
    internal bool[] Reduced;
    internal bool[] Internal;
    internal int[] ParentBaseIndices;

    internal TiffCloudOptimizedDirectorySet()
    {
        Directories = new TiffDirectory[0];
        Infos = new TiffImageInfo[0];
        Reduced = new bool[0];
        Internal = new bool[0];
        ParentBaseIndices = new int[0];
    }

    internal void Append(TiffDirectory directory, TiffImageInfo info, bool reduced, bool isInternal, int parentBaseIndex)
    {
        int length = Directories.Length;
        TiffDirectory[] directories = new TiffDirectory[length + 1];
        TiffImageInfo[] infos = new TiffImageInfo[length + 1];
        bool[] reducedValues = new bool[length + 1];
        bool[] internalValues = new bool[length + 1];
        int[] parentIndices = new int[length + 1];
        int index = 0;
        while (index < length)
        {
            directories[index] = Directories[index];
            infos[index] = Infos[index];
            reducedValues[index] = Reduced[index];
            internalValues[index] = Internal[index];
            parentIndices[index] = ParentBaseIndices[index];
            index = index + 1;
        }

        directories[length] = directory.Copy();
        infos[length] = info.Copy();
        reducedValues[length] = reduced;
        internalValues[length] = isInternal;
        parentIndices[length] = parentBaseIndex;
        Directories = directories;
        Infos = infos;
        Reduced = reducedValues;
        Internal = internalValues;
        ParentBaseIndices = parentIndices;
    }
}
