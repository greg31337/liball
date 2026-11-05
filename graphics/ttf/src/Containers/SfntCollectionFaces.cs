namespace ttf;

internal class SfntCollectionFaceInfo
{
    int faceIndex;
    long directoryOffset;
    uint scalerType;
    int tableCount;

    internal SfntCollectionFaceInfo(
        int faceIndex,
        long directoryOffset,
        uint scalerType,
        int tableCount)
    {
        if (faceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }

        if (directoryOffset < 0)
        {
            throw new System.ArgumentOutOfRangeException("directoryOffset");
        }

        if (tableCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("tableCount");
        }

        this.faceIndex = faceIndex;
        this.directoryOffset = directoryOffset;
        this.scalerType = scalerType;
        this.tableCount = tableCount;
    }

    internal int GetFaceIndex()
    {
        return faceIndex;
    }

    internal long GetDirectoryOffset()
    {
        return directoryOffset;
    }

    internal uint GetScalerType()
    {
        return scalerType;
    }

    internal int GetTableCount()
    {
        return tableCount;
    }
}

internal class SfntSelectedCollectionFace
{
    SfntCollectionFaceInfo info;
    SfntCollectionFace collectionFace;
    SfntValidationResult validationResult;
    SfntTableStore tableStore;

    internal SfntSelectedCollectionFace(
        SfntCollectionFaceInfo info,
        SfntCollectionFace collectionFace,
        SfntValidationResult validationResult,
        SfntTableStore tableStore)
    {
        if (info == null)
        {
            throw new System.ArgumentNullException("info");
        }

        if (collectionFace == null)
        {
            throw new System.ArgumentNullException("collectionFace");
        }

        if (validationResult == null)
        {
            throw new System.ArgumentNullException("validationResult");
        }

        if (tableStore == null)
        {
            throw new System.ArgumentNullException("tableStore");
        }

        this.info = info;
        this.collectionFace = collectionFace;
        this.validationResult = validationResult;
        this.tableStore = tableStore;
    }

    internal int GetFaceIndex()
    {
        return info.GetFaceIndex();
    }

    internal long GetDirectoryOffset()
    {
        return info.GetDirectoryOffset();
    }

    internal uint GetScalerType()
    {
        return info.GetScalerType();
    }

    internal int GetDeclaredTableCount()
    {
        return info.GetTableCount();
    }

    internal SfntDirectory GetDirectory()
    {
        return collectionFace.GetDirectory();
    }

    internal SfntValidationResult GetValidationResult()
    {
        return validationResult;
    }

    internal SfntTableStore GetTableStore()
    {
        return tableStore;
    }

    internal SfntCollectionTableRegion GetTableRegion(int index)
    {
        return collectionFace.GetTableRegion(index);
    }
}

internal class SfntCollectionFaceCatalog
{
    FontData fontData;
    SfntCollection collection;
    FontLoadOptions options;
    SfntCollectionFaceInfo[] faceInfos;

    internal SfntCollectionFaceCatalog(
        FontData fontData,
        SfntCollection collection,
        FontLoadOptions options)
    {
        if (fontData == null)
        {
            throw new System.ArgumentNullException("fontData");
        }

        if (collection == null)
        {
            throw new System.ArgumentNullException("collection");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        this.fontData = fontData;
        this.collection = collection;
        this.options = options;
        faceInfos = CreateFaceInfos(collection);
    }

    internal int GetFaceCount()
    {
        return faceInfos.Length;
    }

    internal SfntCollectionFaceInfo GetFaceInfo(int index)
    {
        ValidateFaceIndex(index);
        return faceInfos[index];
    }

    internal SfntSelectedCollectionFace SelectFace(int index)
    {
        ValidateFaceIndex(index);
        options.GetCancellationToken().ThrowIfCancellationRequested();

        SfntCollectionFace collectionFace = collection.GetFace(index);
        SfntDirectory directory = collectionFace.GetDirectory();
        SfntValidationResult validationResult = SfntDirectoryValidator.Validate(
            fontData,
            directory,
            options,
            index);
        SfntTableStore tableStore = SfntTableStoreBuilder.Create(
            fontData,
            directory,
            options,
            index);
        return new SfntSelectedCollectionFace(
            faceInfos[index],
            collectionFace,
            validationResult,
            tableStore);
    }

    static SfntCollectionFaceInfo[] CreateFaceInfos(SfntCollection collection)
    {
        SfntCollectionFaceInfo[] infos =
            new SfntCollectionFaceInfo[collection.GetFaceCount()];
        int index = 0;

        while (index < infos.Length)
        {
            SfntCollectionFace face = collection.GetFace(index);
            SfntDirectory directory = face.GetDirectory();
            infos[index] = new SfntCollectionFaceInfo(
                index,
                face.GetDirectoryOffset(),
                directory.GetScalerType(),
                directory.GetTableCount());
            index += 1;
        }

        return infos;
    }

    void ValidateFaceIndex(int index)
    {
        if (index < 0 || index >= faceInfos.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
