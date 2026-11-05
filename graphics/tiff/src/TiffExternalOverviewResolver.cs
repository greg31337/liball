using System.Collections.Generic;
using System.IO;

namespace tiff;

internal static class TiffExternalOverviewResolver
{
    internal static TiffExternalOverviewSet ResolvePaths(TiffImage baseImage, string[] paths, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (paths == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "An external overview path array is required.");
            return CreateResult(baseImage, new TiffFile[0], new TiffExternalOverviewRelationship[0], diagnostics);
        }

        TiffFile[] files = new TiffFile[paths.Length];
        int index = 0;
        while (index < paths.Length)
        {
            files[index] = TiffFile.OpenRead(paths[index], options);
            index = index + 1;
        }

        return Resolve(baseImage, files, paths, true, diagnostics);
    }

    internal static TiffExternalOverviewSet ResolveStreams(TiffImage baseImage, Stream[] streams, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (streams == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "An external overview stream array is required.");
            return CreateResult(baseImage, new TiffFile[0], new TiffExternalOverviewRelationship[0], diagnostics);
        }

        TiffFile[] files = new TiffFile[streams.Length];
        int index = 0;
        while (index < streams.Length)
        {
            files[index] = TiffFile.OpenRead(streams[index], options);
            index = index + 1;
        }

        return Resolve(baseImage, files, new string[streams.Length], false, diagnostics);
    }

    internal static TiffExternalOverviewSet Resolve(TiffImage baseImage, TiffFile[] files, string[] paths, bool isPathSource, TiffDiagnostics diagnostics)
    {
        List<TiffExternalOverviewRelationship> relationships = new List<TiffExternalOverviewRelationship>();
        if (baseImage == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A base image is required to resolve external overviews.");
            MergeFileDiagnostics(files, diagnostics);
            return CreateResult(new TiffImage(), files, relationships.ToArray(), diagnostics);
        }

        if (!baseImage.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The base image must be open to resolve external overviews.");
            MergeFileDiagnostics(files, diagnostics);
            return CreateResult(baseImage, files, relationships.ToArray(), diagnostics);
        }

        TiffImageInfo baseInfo = baseImage.GetInfo();
        if (!baseImage.GetSuccess())
        {
            diagnostics.Merge(baseImage.GetDiagnostics());
            MergeFileDiagnostics(files, diagnostics);
            return CreateResult(baseImage, files, relationships.ToArray(), diagnostics);
        }

        int sourceIndex = 0;
        while (sourceIndex < files.Length)
        {
            TiffFile file = files[sourceIndex];
            if (file.GetSuccess())
            {
                int relationshipCountBeforeSource = relationships.Count;
                int imageIndex = 0;
                while (imageIndex < file.GetImageCount())
                {
                    TiffImage image = file.GetImage(imageIndex);
                    TiffImageInfo overviewInfo = image.GetInfo();
                    if (image.GetSuccess() && IsOverview(baseInfo, overviewInfo) && IsCompatible(baseInfo, overviewInfo))
                    {
                        string path = "";
                        if (isPathSource)
                        {
                            path = paths[sourceIndex];
                        }

                        InsertRelationship(relationships, TiffExternalOverviewRelationship.Create(baseImage.GetIndex(), sourceIndex, imageIndex, isPathSource, path, baseInfo, overviewInfo));
                    }

                    imageIndex = imageIndex + 1;
                }

                if (file.GetSuccess() && relationshipCountBeforeSource == relationships.Count)
                {
                    diagnostics.AddWarning(TiffErrorCode.InvalidTagValue, "An external overview source contained no compatible image smaller than the base image.");
                }
            }

            diagnostics.Merge(file.GetDiagnostics());
            sourceIndex = sourceIndex + 1;
        }

        int levelIndex = 0;
        while (levelIndex < relationships.Count)
        {
            relationships[levelIndex].LevelIndex = levelIndex;
            levelIndex = levelIndex + 1;
        }

        return CreateResult(baseImage, files, relationships.ToArray(), diagnostics);
    }

    internal static bool IsOverview(TiffImageInfo baseInfo, TiffImageInfo overviewInfo)
    {
        if (overviewInfo.Width == 0 || overviewInfo.Length == 0)
        {
            return false;
        }

        if (overviewInfo.Width > baseInfo.Width || overviewInfo.Length > baseInfo.Length)
        {
            return false;
        }

        return overviewInfo.Width < baseInfo.Width || overviewInfo.Length < baseInfo.Length;
    }

    internal static bool IsCompatible(TiffImageInfo baseInfo, TiffImageInfo overviewInfo)
    {
        if (baseInfo.SamplesPerPixel != overviewInfo.SamplesPerPixel || baseInfo.Photometric != overviewInfo.Photometric || baseInfo.PlanarConfiguration != overviewInfo.PlanarConfiguration || baseInfo.Orientation != overviewInfo.Orientation || baseInfo.ImageDepth != overviewInfo.ImageDepth)
        {
            return false;
        }

        if (!EqualIntegers(baseInfo.BitsPerSample, overviewInfo.BitsPerSample))
        {
            return false;
        }

        if (!EqualSampleFormats(baseInfo.SampleFormats, overviewInfo.SampleFormats))
        {
            return false;
        }

        return EqualExtraSamples(baseInfo.ExtraSamples, overviewInfo.ExtraSamples);
    }

    internal static bool EqualIntegers(int[] left, int[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool EqualSampleFormats(TiffSampleFormat[] left, TiffSampleFormat[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool EqualExtraSamples(TiffExtraSample[] left, TiffExtraSample[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void InsertRelationship(List<TiffExternalOverviewRelationship> relationships, TiffExternalOverviewRelationship relationship)
    {
        int index = 0;
        while (index < relationships.Count)
        {
            TiffExternalOverviewRelationship existing = relationships[index];
            if (relationship.OverviewWidth > existing.OverviewWidth || relationship.OverviewWidth == existing.OverviewWidth && relationship.OverviewLength > existing.OverviewLength)
            {
                relationships.Insert(index, relationship);
                return;
            }

            index = index + 1;
        }

        relationships.Add(relationship);
    }

    internal static void MergeFileDiagnostics(TiffFile[] files, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < files.Length)
        {
            diagnostics.Merge(files[index].GetDiagnostics());
            index = index + 1;
        }
    }

    internal static TiffExternalOverviewSet CreateResult(TiffImage baseImage, TiffFile[] files, TiffExternalOverviewRelationship[] relationships, TiffDiagnostics diagnostics)
    {
        TiffExternalOverviewSet result = new TiffExternalOverviewSet();
        result.Initialize(baseImage, files, relationships, diagnostics);
        return result;
    }
}
