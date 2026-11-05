namespace tiff;

internal class TiffOmeDatasetNavigator
{
    internal static TiffOmeMetadata ResolvePyramids(TiffOmeMetadata metadata, TiffDirectory[] directories, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffOmeMetadata result = metadata.Copy();
        result.PyramidLevels = new TiffOmePyramidLevel[0];
        int imageIndex = 0;
        while (imageIndex < result.Images.Length)
        {
            int planeIndex = 0;
            while (planeIndex < result.Images[imageIndex].Planes.Length)
            {
                TiffOmePlane basePlane = result.Images[imageIndex].Planes[planeIndex];
                if (!basePlane.IsExternal && basePlane.HasIfd && basePlane.IfdIndex >= 0 && basePlane.IfdIndex < directories.Length)
                {
                    TiffPyramidDetectionResult pyramidResult = TiffSourcePyramidNavigator.Detect(directories[basePlane.IfdIndex], source, byteOrder, fileKind, options);
                    diagnostics.Merge(pyramidResult.Diagnostics);
                    if (pyramidResult.Success && pyramidResult.Pyramid.HasPyramid)
                    {
                        result.PyramidLevels = AppendPyramidLevel(result.PyramidLevels, TiffOmePyramidLevel.Create(imageIndex, basePlane.IfdIndex, 0, pyramidResult.Pyramid.BaseWidth, pyramidResult.Pyramid.BaseLength));
                        int levelIndex = 0;
                        while (levelIndex < pyramidResult.Pyramid.Levels.Length)
                        {
                            TiffPyramidLevel level = pyramidResult.Pyramid.Levels[levelIndex];
                            result.PyramidLevels = AppendPyramidLevel(result.PyramidLevels, TiffOmePyramidLevel.Create(imageIndex, basePlane.IfdIndex, levelIndex + 1, level.Width, level.Length));
                            levelIndex = levelIndex + 1;
                        }
                    }
                }

                planeIndex = planeIndex + 1;
            }

            imageIndex = imageIndex + 1;
        }

        return result;
    }

    internal static TiffOmePlane FindPlane(TiffOmeMetadata metadata, int omeImageIndex, int channelIndex, int zIndex, int timeIndex, TiffDiagnostics diagnostics)
    {
        if (metadata == null || omeImageIndex < 0 || omeImageIndex >= metadata.Images.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "OME image index is outside the available range.");
            return new TiffOmePlane();
        }

        TiffOmePlane[] planes = metadata.Images[omeImageIndex].Planes;
        int planeIndex = 0;
        while (planeIndex < planes.Length)
        {
            TiffOmePlane plane = planes[planeIndex];
            if (plane.ChannelIndex == channelIndex && plane.ZIndex == zIndex && plane.TimeIndex == timeIndex)
            {
                return plane.Copy();
            }

            planeIndex = planeIndex + 1;
        }

        diagnostics.AddError(TiffErrorCode.InvalidOffset, "OME plane coordinates do not map to an available TIFF IFD.");
        return new TiffOmePlane();
    }

    internal static TiffOmePyramidLevel[] AppendPyramidLevel(TiffOmePyramidLevel[] levels, TiffOmePyramidLevel level)
    {
        TiffOmePyramidLevel[] result = new TiffOmePyramidLevel[levels.Length + 1];
        int index = 0;
        while (index < levels.Length)
        {
            result[index] = levels[index].Copy();
            index = index + 1;
        }

        result[index] = level.Copy();
        return result;
    }
}
