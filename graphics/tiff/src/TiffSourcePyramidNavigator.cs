namespace tiff;

internal class TiffSourcePyramidNavigator
{
    internal static TiffPyramidDetectionResult Detect(TiffDirectory parent, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (parent == null || source == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory and source are required to navigate a TIFF pyramid.");
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        TiffImageInfoResult baseResult = TiffSourceImageInfoExtractor.Extract(source, parent, byteOrder, options);
        diagnostics.Merge(baseResult.Diagnostics);
        TiffSubIfdReadResult subIfdResult = TiffSourceSubIfdReader.Read(parent, source, byteOrder, fileKind, options);
        diagnostics.Merge(subIfdResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        TiffPyramidLevel[] levels = new TiffPyramidLevel[0];
        int subIfdIndex = 0;
        while (subIfdIndex < subIfdResult.SubIfds.Length)
        {
            TiffSubIfd subIfd = subIfdResult.SubIfds[subIfdIndex];
            int directoryIndex = 0;
            while (directoryIndex < subIfd.Directories.Length)
            {
                TiffDirectory directory = subIfd.Directories[directoryIndex];
                TiffImageInfoResult levelResult = TiffSourceImageInfoExtractor.Extract(source, directory, byteOrder, options);
                diagnostics.Merge(levelResult.Diagnostics);
                if (diagnostics.HasErrors())
                {
                    return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
                }

                TiffImageInfo levelInfo = levelResult.Info;
                bool reduced = levelInfo.IsReducedResolution;
                if (!reduced && levelInfo.Width > 0 && levelInfo.Length > 0 && levelInfo.Width < baseResult.Info.Width && levelInfo.Length < baseResult.Info.Length)
                {
                    reduced = true;
                }

                if (reduced)
                {
                    TiffPyramidLevel level = TiffPyramidLevel.Create(levels.Length, parent.ImageIndex, subIfd.SubIfdIndex, directory.ImageIndex, levelInfo.Width, levelInfo.Length, true, directory);
                    levels = TiffPyramidDetector.AppendLevel(levels, level);
                }

                directoryIndex = directoryIndex + 1;
            }

            subIfdIndex = subIfdIndex + 1;
        }

        TiffPyramid pyramid = TiffPyramid.Create(levels.Length > 0, parent.ImageIndex, baseResult.Info.Width, baseResult.Info.Length, levels);
        return TiffPyramidDetectionResult.Create(pyramid, diagnostics);
    }
}
