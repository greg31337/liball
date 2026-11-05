namespace ttf;

internal class SfntCollection
{
    uint version;
    SfntCollectionFace[] faces;
    SfntCollectionTableRegion[] tableRegions;
    SfntCollectionTableRegion[] sharedTableRegions;
    SfntCollectionDsig dsig;

    internal SfntCollection(
        uint version,
        SfntCollectionFace[] faces,
        SfntCollectionTableRegion[] tableRegions,
        SfntCollectionTableRegion[] sharedTableRegions,
        SfntCollectionDsig dsig)
    {
        if (faces == null)
        {
            throw new System.ArgumentNullException("faces");
        }

        if (tableRegions == null)
        {
            throw new System.ArgumentNullException("tableRegions");
        }

        if (sharedTableRegions == null)
        {
            throw new System.ArgumentNullException("sharedTableRegions");
        }

        if (dsig == null)
        {
            throw new System.ArgumentNullException("dsig");
        }

        this.version = version;
        this.faces = faces;
        this.tableRegions = tableRegions;
        this.sharedTableRegions = sharedTableRegions;
        this.dsig = dsig;
    }

    internal uint GetVersion()
    {
        return version;
    }

    internal int GetFaceCount()
    {
        return faces.Length;
    }

    internal SfntCollectionFace GetFace(int index)
    {
        if (index < 0 || index >= faces.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return faces[index];
    }

    internal int GetTableRegionCount()
    {
        return tableRegions.Length;
    }

    internal SfntCollectionTableRegion GetTableRegion(int index)
    {
        if (index < 0 || index >= tableRegions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tableRegions[index];
    }

    internal int GetSharedTableRegionCount()
    {
        return sharedTableRegions.Length;
    }

    internal SfntCollectionTableRegion GetSharedTableRegion(int index)
    {
        if (index < 0 || index >= sharedTableRegions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sharedTableRegions[index];
    }

    internal SfntCollectionDsig GetDsig()
    {
        return dsig;
    }
}

internal class SfntCollectionFace
{
    long directoryOffset;
    SfntDirectory directory;
    SfntCollectionTableRegion[] tableRegions;

    internal SfntCollectionFace(
        long directoryOffset,
        SfntDirectory directory,
        SfntCollectionTableRegion[] tableRegions)
    {
        if (directory == null)
        {
            throw new System.ArgumentNullException("directory");
        }

        if (tableRegions == null)
        {
            throw new System.ArgumentNullException("tableRegions");
        }

        this.directoryOffset = directoryOffset;
        this.directory = directory;
        this.tableRegions = tableRegions;
    }

    internal long GetDirectoryOffset()
    {
        return directoryOffset;
    }

    internal SfntDirectory GetDirectory()
    {
        return directory;
    }

    internal int GetTableCount()
    {
        return tableRegions.Length;
    }

    internal SfntCollectionTableRegion GetTableRegion(int index)
    {
        if (index < 0 || index >= tableRegions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tableRegions[index];
    }
}

internal class SfntCollectionTableRegion
{
    FontDataSlice data;
    int faceReferenceCount;
    int lastFaceIndex;

    internal SfntCollectionTableRegion(FontDataSlice data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.data = data;
        faceReferenceCount = 0;
        lastFaceIndex = -1;
    }

    internal long GetOffset()
    {
        return data.GetAbsoluteOffset();
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal int GetFaceReferenceCount()
    {
        return faceReferenceCount;
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }

    internal FontDataSlice GetDataSlice()
    {
        return data;
    }

    internal void AddFaceReference(int faceIndex)
    {
        if (faceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }

        if (lastFaceIndex != faceIndex)
        {
            faceReferenceCount += 1;
            lastFaceIndex = faceIndex;
        }
    }
}

internal class SfntCollectionDsig
{
    bool present;
    uint tag;
    FontDataSlice data;

    internal SfntCollectionDsig(bool present, uint tag, FontDataSlice data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.present = present;
        this.tag = tag;
        this.data = data;
    }

    internal bool IsPresent()
    {
        return present;
    }

    internal uint GetTag()
    {
        return tag;
    }

    internal long GetOffset()
    {
        return data.GetAbsoluteOffset();
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal byte[] CopyData()
    {
        if (!present)
        {
            throw new System.InvalidOperationException("The collection does not contain DSIG data.");
        }

        return data.CopyBytes();
    }
}
