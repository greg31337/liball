using System;
using System.IO;
using System.Threading;

namespace tiff;

/// <summary>Builds a TIFF or BigTIFF file from image definitions and raw strip or tile payloads.</summary>
/// <remarks>Image definitions and segment bytes are buffered until <c>Flush</c>. The writer supplies baseline structural tags and can upgrade classic TIFF output to BigTIFF when configured.</remarks>
public class TiffWriter : ITiffWriter
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    internal bool Success;
    /// <summary>Indicates whether the file or writer is still open for operations.</summary>
    internal bool IsOpen;
    internal TiffWriteOptions Options;
    internal TiffDiagnostics Diagnostics;
    internal TiffWriterImageState[] Images;
    internal byte[] FileBytes;
    internal string Path;
    internal bool HasPath;
    internal Stream OutputStream;
    internal bool HasStream;
    internal long StreamStartPosition;

    internal TiffWriter(TiffWriteOptions options)
    {
        Success = true;
        IsOpen = true;
        Options = NormalizeOptions(options);
        Diagnostics = new TiffDiagnostics();
        Images = new TiffWriterImageState[0];
        FileBytes = new byte[0];
        Path = "";
        HasPath = false;
        OutputStream = Stream.Null;
        HasStream = false;
        StreamStartPosition = 0;
    }

    /// <summary>Indicates whether the most recent operation completed without error diagnostics.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    public bool GetSuccess()
    {
        return Success;
    }

    /// <summary>Indicates whether the writer accepts additional operations.</summary>
    /// <returns><c>true</c> while the writer is open.</returns>
    public bool GetIsOpen()
    {
        return IsOpen;
    }

    internal void SetPath(string path)
    {
        Path = NormalizePath(path);
        HasPath = Path.Length > 0;
        if (!HasPath)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "A file path is required to create a TIFF writer.");
            Success = false;
        }
    }

    internal void SetStream(Stream stream)
    {
        if (stream == null)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "A stream is required to create a TIFF writer.");
            Success = false;
        }
        else if (!stream.CanWrite)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "The TIFF writer stream must be writable.");
            Success = false;
        }
        else
        {
            OutputStream = stream;
            HasStream = true;
            if (stream.CanSeek)
            {
                StreamStartPosition = stream.Position;
            }
        }
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriter Create()
    {
        return new TiffWriter(new TiffWriteOptions());
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriter Create(TiffWriteOptions options)
    {
        return new TiffWriter(options);
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriter Create(string path)
    {
        return Create(path, new TiffWriteOptions());
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriter Create(string path, TiffWriteOptions options)
    {
        TiffWriter writer = new TiffWriter(options);
        writer.SetPath(path);
        return writer;
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriter Create(Stream stream)
    {
        return Create(stream, new TiffWriteOptions());
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriter Create(Stream stream, TiffWriteOptions options)
    {
        TiffWriter writer = new TiffWriter(options);
        writer.SetStream(stream);
        return writer;
    }

    /// <summary>Adds image to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="image">The image.</param>
    /// <returns>The zero-based index assigned to the new image.</returns>
    public int AddImage(TiffImageDefinition image)
    {
        if (!IsOpen)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot add an image to a closed TIFF writer.");
            Success = false;
            return -1;
        }

        TiffWriterImageState state = TiffWriterImageState.Create(image);
        Images = AppendState(Images, state);
        return Images.Length - 1;
    }

    /// <summary>Adds an image whose required structural tags and segment layout are derived from configuration.</summary>
    /// <param name="configuration">The source pixel, dimensions, and physical layout configuration.</param>
    /// <returns>The zero-based image index, or <c>-1</c> when validation fails.</returns>
    public int AddConfiguredImage(TiffWriterImageConfiguration configuration)
    {
        if (!IsOpen)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot add a configured image to a closed TIFF writer.");
            Success = false;
            return -1;
        }

        int imageIndex = Images.Length;
        TiffDiagnostics validationDiagnostics = new TiffDiagnostics();
        TiffWriterImageState? state = CreateConfiguredState(configuration, imageIndex, validationDiagnostics);
        if (state == null)
        {
            Diagnostics.Merge(validationDiagnostics);
            Success = false;
            return -1;
        }

        TiffByteOrder configuredByteOrder = configuration.ByteOrder;
        if (configuredByteOrder != TiffByteOrder.Unknown && Images.Length > 0 && configuredByteOrder != GetByteOrder())
        {
            Diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "All configured images in one TIFF file must use the same byte order.");
            Success = false;
            return -1;
        }

        if (configuredByteOrder != TiffByteOrder.Unknown)
        {
            Options.ByteOrder = configuredByteOrder;
        }

        Images = AppendState(Images, state);
        return Images.Length - 1;
    }

    /// <summary>Adds sub image to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="image">The image.</param>
    /// <returns>The zero-based index assigned to the new image.</returns>
    public int AddSubImage(int parentImageIndex, TiffImageDefinition image)
    {
        if (!IsOpen)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot add a subimage to a closed TIFF writer.");
            Success = false;
            return -1;
        }

        if (parentImageIndex < 0 || parentImageIndex >= Images.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Parent image index is outside the available image range.");
            Success = false;
            return -1;
        }

        TiffWriterImageState state = TiffWriterImageState.Create(image);
        Images[parentImageIndex].Children = AppendState(Images[parentImageIndex].Children, state);
        return Images[parentImageIndex].Children.Length - 1;
    }

    /// <summary>Adds a configured child image whose required tags and segment layout are derived from configuration.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="configuration">The child pixel, dimensions, and physical layout configuration.</param>
    /// <returns>The assigned child index, or <c>-1</c> when validation fails.</returns>
    public int AddConfiguredSubImage(int parentImageIndex, TiffWriterImageConfiguration configuration)
    {
        return AddConfiguredChild(parentImageIndex, configuration, false);
    }

    /// <summary>Adds a reduced-resolution configured child after all previously added pyramid levels.</summary>
    /// <param name="parentImageIndex">The configured base image index.</param>
    /// <param name="configuration">The overview pixel, dimensions, compression, and physical layout configuration.</param>
    /// <returns>The assigned child index, or <c>-1</c> when validation fails.</returns>
    public int AddPyramidLevel(int parentImageIndex, TiffWriterImageConfiguration configuration)
    {
        return AddConfiguredChild(parentImageIndex, configuration, true);
    }

    /// <summary>Adds a tiled georeferenced base image and ordered internal overviews using Cloud Optimized GeoTIFF layout.</summary>
    /// <param name="configuration">The base image, overview, and GeoTIFF metadata configuration.</param>
    /// <returns>The assigned base and child image indices with validation diagnostics.</returns>
    public TiffCloudOptimizedGeoTiffWriteResult AddCloudOptimizedGeoTiff(TiffCloudOptimizedGeoTiffWriteConfiguration configuration)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateCloudOptimizedWriteConfiguration(configuration, diagnostics))
        {
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffCloudOptimizedGeoTiffWriteResult.Create(-1, new int[0], diagnostics);
        }

        Options.CloudOptimizedGeoTiffLayout = true;
        int baseImageIndex = AddConfiguredImage(configuration.BaseImage);
        if (baseImageIndex < 0)
        {
            diagnostics.Merge(Diagnostics);
            return TiffCloudOptimizedGeoTiffWriteResult.Create(-1, new int[0], diagnostics);
        }

        diagnostics.Merge(WriteGeoTiffMetadata(baseImageIndex, configuration.Model, configuration.KeyDirectory, configuration.Parameters));
        int[] overviewIndices = new int[configuration.Overviews.Length];
        int index = 0;
        while (index < configuration.Overviews.Length && !diagnostics.HasErrors())
        {
            overviewIndices[index] = AddPyramidLevel(baseImageIndex, configuration.Overviews[index]);
            if (overviewIndices[index] < 0)
            {
                diagnostics.Merge(Diagnostics);
            }

            index = index + 1;
        }

        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffCloudOptimizedGeoTiffWriteResult.Create(baseImageIndex, overviewIndices, diagnostics);
    }

    internal bool ValidateCloudOptimizedWriteConfiguration(TiffCloudOptimizedGeoTiffWriteConfiguration configuration, TiffDiagnostics diagnostics)
    {
        if (!IsOpen)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot configure Cloud Optimized GeoTIFF output on a closed writer.");
            return false;
        }

        if (configuration == null || configuration.BaseImage == null)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "A Cloud Optimized GeoTIFF base image configuration is required.");
            return false;
        }

        if (configuration.BaseImage.Layout == null || configuration.BaseImage.Layout.Kind != TiffWriterLayoutKind.Tiles)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF base images must use tiled storage.", Images.Length, TiffTagId.TileOffsets);
        }

        TiffWriterConfigurationBuilder.Validate(configuration.BaseImage, Options, Images.Length, diagnostics);
        if (configuration.Model == null || configuration.KeyDirectory == null || configuration.Parameters == null)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF geographic model, key-directory, and parameter models are required.");
        }
        else
        {
            TiffGeoTiffMetadataWriter.BuildValues(new TiffTagValue[0], configuration.Model, configuration.KeyDirectory, configuration.Parameters, diagnostics);
            if (!TiffCloudOptimizedGeoTiffValidator.HasUsableModelGeoreference(configuration.Model) || !configuration.KeyDirectory.HasDirectory)
            {
                diagnostics.AddError(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF output requires a GeoKey directory and usable model georeference.");
            }
        }

        TiffWriterImageConfiguration[] overviews = configuration.Overviews;
        if (overviews == null)
        {
            overviews = new TiffWriterImageConfiguration[0];
            configuration.Overviews = overviews;
        }

        TiffWriterImageConfiguration previous = configuration.BaseImage;
        int index = 0;
        while (index < overviews.Length)
        {
            TiffWriterImageConfiguration overview = overviews[index];
            if (overview == null || overview.Layout == null || overview.Layout.Kind != TiffWriterLayoutKind.Tiles)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overviews must use tiled storage.", Images.Length, TiffTagId.TileOffsets);
            }
            else
            {
                TiffWriterConfigurationBuilder.Validate(overview, Options, Images.Length, diagnostics);
                if (overview.Width >= previous.Width || overview.Length >= previous.Length)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overviews must be supplied in decreasing resolution order.");
                }

                previous = overview;
            }

            index = index + 1;
        }

        return !diagnostics.HasErrors();
    }

    internal int AddConfiguredChild(int parentImageIndex, TiffWriterImageConfiguration configuration, bool pyramidLevel)
    {
        if (!IsOpen)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot add a configured subimage to a closed TIFF writer.");
            Success = false;
            return -1;
        }

        if (parentImageIndex < 0 || parentImageIndex >= Images.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Parent image index is outside the available image range.");
            Success = false;
            return -1;
        }

        TiffDiagnostics validationDiagnostics = new TiffDiagnostics();
        TiffWriterImageState? state = CreateConfiguredState(configuration, TiffDiagnostic.NoImageIndex, validationDiagnostics);
        if (state == null)
        {
            Diagnostics.Merge(validationDiagnostics);
            Success = false;
            return -1;
        }

        TiffByteOrder configuredByteOrder = configuration.ByteOrder;
        if (configuredByteOrder != TiffByteOrder.Unknown && configuredByteOrder != GetByteOrder())
        {
            Diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "All configured images in one TIFF file must use the same byte order.");
            Success = false;
            return -1;
        }

        if (pyramidLevel && !ValidatePyramidLevel(Images[parentImageIndex], state))
        {
            return -1;
        }

        if (pyramidLevel)
        {
            state.IsPyramidLevel = true;
            state.Definition.SetTag(TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 1));
        }

        Images[parentImageIndex].Children = AppendState(Images[parentImageIndex].Children, state);
        return Images[parentImageIndex].Children.Length - 1;
    }

    internal TiffWriterImageState? CreateConfiguredState(TiffWriterImageConfiguration configuration, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!TiffWriterConfigurationBuilder.Validate(configuration, Options, imageIndex, diagnostics))
        {
            return null;
        }

        TiffImageDefinition definition = TiffWriterConfigurationBuilder.BuildDefinition(configuration);
        TiffWriterImageState state = TiffWriterImageState.Create(definition);
        state.IsConfigured = true;
        state.UsesConfiguredTiles = configuration.Layout.Kind == TiffWriterLayoutKind.Tiles;
        state.AllowsSparseSegments = configuration.Layout.AllowSparseSegments;
        state.ConfiguredSegmentCount = TiffWriterConfigurationBuilder.CalculateSegmentCount(configuration, diagnostics);
        state.ConfiguredInfo = TiffWriterConfigurationBuilder.BuildImageInfo(configuration, imageIndex);
        state.DecodedStrips = TiffWriterConfigurationBuilder.CreateEmptySegments(definition.RawStrips.Length);
        state.DecodedTiles = TiffWriterConfigurationBuilder.CreateEmptySegments(definition.RawTiles.Length);
        TiffRequiredTagValidationResult requiredTags = TiffRequiredTagValidator.ValidateValuesForImage(BuildValues(state, GetFileKind(), false), imageIndex);
        diagnostics.Merge(requiredTags.Diagnostics);
        if (!requiredTags.Success || diagnostics.HasErrors())
        {
            return null;
        }

        return state;
    }

    internal bool ValidatePyramidLevel(TiffWriterImageState parent, TiffWriterImageState level)
    {
        if (!parent.IsConfigured)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Pyramid levels require a base image added with AddConfiguredImage.");
            Success = false;
            return false;
        }

        TiffImageInfo previousInfo = parent.ConfiguredInfo;
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index].IsPyramidLevel)
            {
                previousInfo = parent.Children[index].ConfiguredInfo;
            }

            index = index + 1;
        }

        bool noLargerDimension = level.ConfiguredInfo.Width <= previousInfo.Width && level.ConfiguredInfo.Length <= previousInfo.Length;
        bool oneSmallerDimension = level.ConfiguredInfo.Width < previousInfo.Width || level.ConfiguredInfo.Length < previousInfo.Length;
        if (!noLargerDimension || !oneSmallerDimension)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Pyramid levels must be added in decreasing resolution order.");
            Success = false;
            return false;
        }

        return true;
    }

    /// <summary>Writes tag to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="value">The value.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public TiffDiagnostics WriteTag(int imageIndex, TiffTagValue value)
    {
        if (!ValidateImageIndex(imageIndex))
        {
            return Diagnostics;
        }

        Images[imageIndex].Definition.SetTag(value);
        return Diagnostics;
    }

    /// <summary>Adds or replaces a batch of non-structural metadata tags after validating the complete batch.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="values">The metadata values to add or replace.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics UpdateMetadata(int imageIndex, TiffTagValue[] values)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (values == null)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Metadata update values are required.");
            }
            else
            {
                int index = 0;
                while (index < values.Length)
                {
                    if (values[index] == null || values[index].TagId <= 0 || values[index].TagId > 65535 || TiffCopy.IsProtectedRewriteTag(values[index].TagId))
                    {
                        int tagId = values[index] == null ? 0 : values[index].TagId;
                        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Metadata updates cannot change structural image-layout or IFD-pointer tags.", imageIndex, tagId);
                        break;
                    }

                    TiffExifMetadataWriter.EncodeValue(values[index], GetByteOrder(), diagnostics);
                    if (diagnostics.HasErrors())
                    {
                        break;
                    }

                    index = index + 1;
                }

                if (!diagnostics.HasErrors())
                {
                    index = 0;
                    while (index < values.Length)
                    {
                        Images[imageIndex].Definition.SetTag(values[index]);
                        index = index + 1;
                    }
                }
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Removes a batch of metadata tags while protecting image-layout tags.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="tagIds">The metadata tag identifiers to remove.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics RemoveMetadata(int imageIndex, int[] tagIds)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (tagIds == null)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Metadata tag identifiers are required.");
            }
            else
            {
                int index = 0;
                while (index < tagIds.Length)
                {
                    if (tagIds[index] <= 0 || tagIds[index] > 65535 || TiffCopy.IsProtectedMetadataRemovalTag(tagIds[index]))
                    {
                        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Metadata removal cannot remove structural image-layout tags.", imageIndex, tagIds[index]);
                        break;
                    }

                    index = index + 1;
                }

                if (!diagnostics.HasErrors())
                {
                    index = 0;
                    while (index < tagIds.Length)
                    {
                        RemoveMetadataTag(Images[imageIndex], tagIds[index]);
                        index = index + 1;
                    }
                }
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    internal void RemoveMetadataTag(TiffWriterImageState state, int tagId)
    {
        state.Definition.RemoveTag(tagId);
        if (tagId == TiffTagId.ExifIfd)
        {
            state.HasExifMetadata = false;
            state.ExifValues = new TiffTagValue[0];
        }
        else if (tagId == TiffTagId.GpsIfd)
        {
            state.HasGpsMetadata = false;
            state.GpsValues = new TiffTagValue[0];
        }
    }

    /// <summary>Writes or replaces the EXIF child IFD associated with an image.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The EXIF metadata values.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteExif(int imageIndex, TiffExifMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (metadata == null || !metadata.HasExif || metadata.Values == null || metadata.Values.Length == 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "EXIF writing requires at least one EXIF value.", imageIndex, TiffTagId.ExifIfd);
            }
            else
            {
                TiffExifMetadataWriter.EncodeValues(metadata.Values, GetByteOrder(), diagnostics);
                if (!diagnostics.HasErrors())
                {
                    Images[imageIndex].HasExifMetadata = true;
                    Images[imageIndex].ExifValues = TiffExifMetadata.CopyValues(metadata.Values);
                }
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces the GPS child IFD associated with an image.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The GPS metadata values.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteGps(int imageIndex, TiffGpsMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (metadata == null || !metadata.HasGps || metadata.Values == null || metadata.Values.Length == 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GPS writing requires at least one GPS value.", imageIndex, TiffTagId.GpsIfd);
            }
            else
            {
                TiffExifMetadataWriter.EncodeValues(metadata.Values, GetByteOrder(), diagnostics);
                if (!diagnostics.HasErrors())
                {
                    Images[imageIndex].HasGpsMetadata = true;
                    Images[imageIndex].GpsValues = TiffExifMetadata.CopyValues(metadata.Values);
                }
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces an XMP packet.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="packet">The XMP packet.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteXmp(int imageIndex, TiffXmpPacket packet)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            TiffTagValue value = TiffHighLevelMetadataEncoder.CreateXmpValue(packet, diagnostics);
            SetMetadataTag(imageIndex, value, diagnostics);
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces an ICC profile.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="profile">The ICC profile.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteIccProfile(int imageIndex, TiffIccProfile profile)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            TiffTagValue value = TiffHighLevelMetadataEncoder.CreateIccValue(profile, diagnostics);
            SetMetadataTag(imageIndex, value, diagnostics);
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces Photoshop image-resource metadata.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The Photoshop metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WritePhotoshop(int imageIndex, TiffPhotoshopMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            TiffTagValue value = TiffHighLevelMetadataEncoder.CreatePhotoshopValue(metadata, diagnostics);
            SetMetadataTag(imageIndex, value, diagnostics);
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces IPTC metadata.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The IPTC metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteIptc(int imageIndex, TiffIptcMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            TiffTagValue value = TiffHighLevelMetadataEncoder.CreateIptcValue(metadata, diagnostics);
            SetMetadataTag(imageIndex, value, diagnostics);
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces ImageDescription text.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="description">The description text.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteImageDescription(int imageIndex, string description)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (description == null)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageDescription text is required.", imageIndex, TiffTagId.ImageDescription);
            }
            else
            {
                SetMetadataTag(imageIndex, TiffImageDescriptionWriter.CreateValue(description), diagnostics);
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces OME-XML in ImageDescription.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The OME metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteOmeMetadata(int imageIndex, TiffOmeMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            string xml = "";
            if (metadata != null)
            {
                xml = metadata.Xml;
            }

            if (metadata == null || !metadata.HasOmeXml || !TiffOmeMetadataReader.IsOmeXml(xml))
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "OME writing requires valid OME XML.", imageIndex, TiffTagId.ImageDescription);
            }
            else
            {
                SetMetadataTag(imageIndex, TiffOmeMetadataWriter.CreateValue(xml), diagnostics);
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces ImageJ metadata in ImageDescription.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The ImageJ metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteImageJMetadata(int imageIndex, TiffImageJMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            TiffTagValue value = TiffImageJMetadataWriter.CreateValue(metadata);
            if (metadata == null || !metadata.HasImageJ || !TiffImageJMetadataReader.Parse(value.Text, new byte[0]).HasImageJ)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ writing requires ImageJ metadata with a version entry.", imageIndex, TiffTagId.ImageDescription);
            }
            else
            {
                SetMetadataTag(imageIndex, value, diagnostics);
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces GDAL metadata and no-data tags.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The GDAL metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteGdalMetadata(int imageIndex, TiffGdalMetadata metadata)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (metadata == null || (!metadata.HasMetadata && !metadata.HasNoData))
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "GDAL writing requires metadata or a no-data value.");
            }
            else
            {
                TiffTagValue[] values = TiffGdalMetadataWriter.BuildValues(Images[imageIndex].Definition.Tags, metadata, diagnostics);
                if (!diagnostics.HasErrors())
                {
                    Images[imageIndex].Definition.Tags = TiffExifMetadata.CopyValues(values);
                }
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    /// <summary>Writes or replaces GeoTIFF model, key-directory, and parameter tags.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="model">The geographic model metadata.</param>
    /// <param name="keyDirectory">The geographic key directory.</param>
    /// <param name="parameters">The geographic parameter pools.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteGeoTiffMetadata(int imageIndex, TiffGeoModelMetadata model, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (ValidateMetadataWrite(imageIndex, diagnostics))
        {
            if (model == null || keyDirectory == null || parameters == null)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "GeoTIFF model, key-directory, and parameter models are required.");
            }
            else
            {
                TiffTagValue[] values = TiffGeoTiffMetadataWriter.BuildValues(Images[imageIndex].Definition.Tags, model, keyDirectory, parameters, diagnostics);
                if (!diagnostics.HasErrors())
                {
                    Images[imageIndex].Definition.Tags = TiffExifMetadata.CopyValues(values);
                }
            }
        }

        return CompleteMetadataWrite(diagnostics);
    }

    internal bool ValidateMetadataWrite(int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!IsOpen)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot write metadata to a closed TIFF writer.");
        }
        else if (imageIndex < 0 || imageIndex >= Images.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
        }

        return !diagnostics.HasErrors();
    }

    internal void SetMetadataTag(int imageIndex, TiffTagValue value, TiffDiagnostics diagnostics)
    {
        if (!diagnostics.HasErrors())
        {
            Images[imageIndex].Definition.SetTag(value);
        }
    }

    internal TiffDiagnostics CompleteMetadataWrite(TiffDiagnostics diagnostics)
    {
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    /// <summary>Stores the supplied bytes as a raw TIFF image segment without applying a codec.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawStripWriteResult WriteRawStrip(int imageIndex, int stripIndex, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = ValidateImageAndStrileIndex(imageIndex, stripIndex, false, source.Length, "Strip index is outside the writable range.");
        if (diagnostics.HasErrors())
        {
            return TiffRawStripWriteResult.Create(0, new TiffByteRange(), 0, diagnostics);
        }

        Images[imageIndex].Definition.SetRawStrip(stripIndex, source.ToArray());
        if (Images[imageIndex].IsConfigured && stripIndex < Images[imageIndex].DecodedStrips.Length)
        {
            Images[imageIndex].DecodedStrips[stripIndex] = new byte[0];
        }

        return TiffRawStripWriteResult.Create((ulong)stripIndex, TiffByteRange.Create(0, (ulong)source.Length, (ulong)source.Length, source.Length == 0), (ulong)source.Length, diagnostics);
    }

    /// <summary>Encodes and stores the supplied decoded strip bytes.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawStripWriteResult WriteEncodedStrip(int imageIndex, int stripIndex, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = ValidateImageAndStrileIndex(imageIndex, stripIndex, false, source.Length, "Strip index is outside the writable range.");
        bool validationFailed = diagnostics.HasErrors();
        ulong bytesWritten = 0;
        bool isSparse = false;
        if (!diagnostics.HasErrors())
        {
            TiffWriterImageState state = Images[imageIndex];
            if (!state.IsConfigured)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Encoded strip writing requires an image added with AddConfiguredImage.");
            }
            else
            {
                ulong expectedByteCount = TiffStrileLayout.CalculateStripByteCount(state.ConfiguredInfo, (ulong)stripIndex, diagnostics);
                if ((ulong)source.Length != expectedByteCount)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Decoded strip byte count does not match the configured strip size.");
                }

                if (!diagnostics.HasErrors())
                {
                    byte[] decoded = source.ToArray();
                    TiffConfiguredRasterWriter.EncodeStrip(state, stripIndex, decoded, GetByteOrder(), Options.Limits, diagnostics, Options.CompressionCodec);
                    if (!diagnostics.HasErrors())
                    {
                        state.DecodedStrips[stripIndex] = decoded;
                        bytesWritten = (ulong)state.Definition.RawStrips[stripIndex].Length;
                        isSparse = bytesWritten == 0;
                    }
                }
            }
        }

        if (!validationFailed)
        {
            Diagnostics.Merge(diagnostics);
        }

        Success = !Diagnostics.HasErrors();
        ulong resultIndex = 0;
        if (stripIndex >= 0)
        {
            resultIndex = (ulong)stripIndex;
        }

        return TiffRawStripWriteResult.Create(resultIndex, TiffByteRange.Create(0, bytesWritten, bytesWritten, isSparse), bytesWritten, diagnostics);
    }

    /// <summary>Stores the supplied bytes as a raw TIFF image segment without applying a codec.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawTileWriteResult WriteRawTile(int imageIndex, int tileIndex, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = ValidateImageAndStrileIndex(imageIndex, tileIndex, true, source.Length, "Tile index is outside the writable range.");
        if (diagnostics.HasErrors())
        {
            return TiffRawTileWriteResult.Create(0, new TiffByteRange(), 0, diagnostics);
        }

        Images[imageIndex].Definition.SetRawTile(tileIndex, source.ToArray());
        if (Images[imageIndex].IsConfigured && tileIndex < Images[imageIndex].DecodedTiles.Length)
        {
            Images[imageIndex].DecodedTiles[tileIndex] = new byte[0];
        }

        return TiffRawTileWriteResult.Create((ulong)tileIndex, TiffByteRange.Create(0, (ulong)source.Length, (ulong)source.Length, source.Length == 0), (ulong)source.Length, diagnostics);
    }

    /// <summary>Encodes and stores the supplied decoded tile bytes.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawTileWriteResult WriteEncodedTile(int imageIndex, int tileIndex, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = ValidateImageAndStrileIndex(imageIndex, tileIndex, true, source.Length, "Tile index is outside the writable range.");
        bool validationFailed = diagnostics.HasErrors();
        ulong bytesWritten = 0;
        bool isSparse = false;
        if (!diagnostics.HasErrors())
        {
            TiffWriterImageState state = Images[imageIndex];
            if (!state.IsConfigured)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Encoded tile writing requires an image added with AddConfiguredImage.");
            }
            else
            {
                ulong expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(state.ConfiguredInfo, (ulong)tileIndex, diagnostics);
                if ((ulong)source.Length != expectedByteCount)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Decoded tile byte count does not match the configured tile size.");
                }

                if (!diagnostics.HasErrors())
                {
                    byte[] decoded = source.ToArray();
                    TiffConfiguredRasterWriter.EncodeTile(state, tileIndex, decoded, GetByteOrder(), Options.Limits, diagnostics, Options.CompressionCodec);
                    if (!diagnostics.HasErrors())
                    {
                        state.DecodedTiles[tileIndex] = decoded;
                        bytesWritten = (ulong)state.Definition.RawTiles[tileIndex].Length;
                        isSparse = bytesWritten == 0;
                    }
                }
            }
        }

        if (!validationFailed)
        {
            Diagnostics.Merge(diagnostics);
        }

        Success = !Diagnostics.HasErrors();
        ulong resultIndex = 0;
        if (tileIndex >= 0)
        {
            resultIndex = (ulong)tileIndex;
        }

        return TiffRawTileWriteResult.Create(resultIndex, TiffByteRange.Create(0, bytesWritten, bytesWritten, isSparse), bytesWritten, diagnostics);
    }

    /// <summary>Writes one complete source scanline in interleaved pixel order and maps it to the configured strip or tile organization.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="source">The byte-aligned source samples in pixel and sample order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteScanline(int imageIndex, ulong row, ReadOnlySpan<byte> source)
    {
        TiffRegion region = new TiffRegion();
        if (imageIndex >= 0 && imageIndex < Images.Length)
        {
            region.Width = Images[imageIndex].ConfiguredInfo.Width;
        }

        region.Row = row;
        region.Length = 1;
        return WriteConfiguredRegionOperation(imageIndex, region, -1, source);
    }

    /// <summary>Writes one sample from every pixel in a scanline in increasing column order.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="source">The byte-aligned sample values in increasing column order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSampleScanline(int imageIndex, ulong row, int sampleIndex, ReadOnlySpan<byte> source)
    {
        TiffRegion region = new TiffRegion();
        if (imageIndex >= 0 && imageIndex < Images.Length)
        {
            region.Width = Images[imageIndex].ConfiguredInfo.Width;
        }

        region.Row = row;
        region.Length = 1;
        return WriteConfiguredRegionOperation(imageIndex, region, sampleIndex, source);
    }

    /// <summary>Writes a rectangular source region in row-major interleaved pixel order and updates only intersecting configured segments.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="region">The destination image bounds.</param>
    /// <param name="source">The byte-aligned source samples in row, pixel, and sample order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteRegion(int imageIndex, TiffRegion region, ReadOnlySpan<byte> source)
    {
        return WriteConfiguredRegionOperation(imageIndex, region, -1, source);
    }

    /// <summary>Writes one sample across a rectangular region in row-major pixel order and maps it to the configured planar organization.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="region">The destination image bounds.</param>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="source">The byte-aligned sample values in row-major pixel order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSampleRegion(int imageIndex, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source)
    {
        return WriteConfiguredRegionOperation(imageIndex, region, sampleIndex, source);
    }

    /// <summary>Writes one complete sample plane in row-major pixel order and maps it to contiguous or separate configured segments.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="source">The byte-aligned sample values in row-major pixel order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSamplePlane(int imageIndex, int sampleIndex, ReadOnlySpan<byte> source)
    {
        TiffRegion region = new TiffRegion();
        if (imageIndex >= 0 && imageIndex < Images.Length)
        {
            region.Width = Images[imageIndex].ConfiguredInfo.Width;
            region.Length = Images[imageIndex].ConfiguredInfo.Length;
        }

        return WriteConfiguredRegionOperation(imageIndex, region, sampleIndex, source);
    }

    /// <summary>Writes the complete source raster in row-major interleaved pixel order and maps it to the configured physical layout.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="source">The byte-aligned source samples, with multibyte values in host byte order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSourceRaster(int imageIndex, ReadOnlySpan<byte> source)
    {
        TiffRegion region = CreateFullRasterRegion(imageIndex);
        return WriteConfiguredRegionOperation(imageIndex, region, -1, source);
    }

    /// <summary>Writes the complete source raster with segment progress and cancellation.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="source">The byte-aligned source samples, with multibyte values in host byte order.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Operation diagnostics.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    public TiffDiagnostics WriteSourceRasterWithCancellation(int imageIndex, ReadOnlySpan<byte> source, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        TiffOperationMonitor monitor = new TiffOperationMonitor(TiffProgressOperation.Write, progress, cancellationToken);
        monitor.CheckCancellation();
        TiffRegion region = CreateFullRasterRegion(imageIndex);
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        if (!IsOpen)
        {
            monitor.Start(0);
            operationDiagnostics.AddError(TiffErrorCode.UnknownError, "Cannot write image samples to a closed TIFF writer.");
        }
        else if (imageIndex < 0 || imageIndex >= Images.Length)
        {
            monitor.Start(0);
            operationDiagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
        }
        else
        {
            operationDiagnostics = TiffConfiguredRasterWriter.WriteWithOperation(Images[imageIndex], region, -1, source, GetByteOrder(), Options.Limits, Options.CompressionCodec, monitor);
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    /// <summary>Converts and writes a complete row-major RGBA8 raster to the configured source photometric and sample layout.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha bytes for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteRgba8(int imageIndex, ReadOnlySpan<byte> rgba)
    {
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        if (ValidateFullRasterDestination(imageIndex, operationDiagnostics))
        {
            TiffWriterImageState state = Images[imageIndex];
            byte[] source = TiffRgba8Writer.ConvertRgba8ToSourceBytes(state.ConfiguredInfo, rgba, Options.Limits, operationDiagnostics);
            if (!operationDiagnostics.HasErrors())
            {
                TiffDiagnostics writeDiagnostics = TiffConfiguredRasterWriter.WriteWithCodec(state, CreateFullRasterRegion(imageIndex), -1, source, GetByteOrder(), Options.Limits, Options.CompressionCodec);
                operationDiagnostics.Merge(writeDiagnostics);
            }
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    /// <summary>Converts and writes a complete row-major RGBA16 raster to the configured source photometric and sample layout.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha samples for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteRgba16(int imageIndex, ReadOnlySpan<ushort> rgba)
    {
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        if (ValidateFullRasterDestination(imageIndex, operationDiagnostics))
        {
            TiffWriterImageState state = Images[imageIndex];
            byte[] source = TiffRgba16Writer.ConvertRgba16ToSourceBytes(state.ConfiguredInfo, rgba, Options.Limits, operationDiagnostics);
            if (!operationDiagnostics.HasErrors())
            {
                TiffDiagnostics writeDiagnostics = TiffConfiguredRasterWriter.WriteWithCodec(state, CreateFullRasterRegion(imageIndex), -1, source, GetByteOrder(), Options.Limits, Options.CompressionCodec);
                operationDiagnostics.Merge(writeDiagnostics);
            }
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    /// <summary>Writes the complete source raster of a configured child image in row-major interleaved pixel order.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subImageIndex">The configured child image index.</param>
    /// <param name="source">The byte-aligned source samples, with multibyte values in host byte order.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSubImageSourceRaster(int parentImageIndex, int subImageIndex, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        TiffWriterImageState? state = FindConfiguredSubImageState(parentImageIndex, subImageIndex, operationDiagnostics);
        if (state != null)
        {
            TiffDiagnostics writeDiagnostics = TiffConfiguredRasterWriter.WriteWithCodec(state, CreateFullRasterRegionForState(state), -1, source, GetByteOrder(), Options.Limits, Options.CompressionCodec);
            operationDiagnostics.Merge(writeDiagnostics);
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    /// <summary>Converts and writes a complete row-major RGBA8 raster to a configured child image.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subImageIndex">The configured child image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha bytes for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSubImageRgba8(int parentImageIndex, int subImageIndex, ReadOnlySpan<byte> rgba)
    {
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        TiffWriterImageState? state = FindConfiguredSubImageState(parentImageIndex, subImageIndex, operationDiagnostics);
        if (state != null)
        {
            byte[] source = TiffRgba8Writer.ConvertRgba8ToSourceBytes(state.ConfiguredInfo, rgba, Options.Limits, operationDiagnostics);
            if (!operationDiagnostics.HasErrors())
            {
                TiffDiagnostics writeDiagnostics = TiffConfiguredRasterWriter.WriteWithCodec(state, CreateFullRasterRegionForState(state), -1, source, GetByteOrder(), Options.Limits, Options.CompressionCodec);
                operationDiagnostics.Merge(writeDiagnostics);
            }
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    /// <summary>Converts and writes a complete row-major RGBA16 raster to a configured child image.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subImageIndex">The configured child image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha samples for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics WriteSubImageRgba16(int parentImageIndex, int subImageIndex, ReadOnlySpan<ushort> rgba)
    {
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        TiffWriterImageState? state = FindConfiguredSubImageState(parentImageIndex, subImageIndex, operationDiagnostics);
        if (state != null)
        {
            byte[] source = TiffRgba16Writer.ConvertRgba16ToSourceBytes(state.ConfiguredInfo, rgba, Options.Limits, operationDiagnostics);
            if (!operationDiagnostics.HasErrors())
            {
                TiffDiagnostics writeDiagnostics = TiffConfiguredRasterWriter.WriteWithCodec(state, CreateFullRasterRegionForState(state), -1, source, GetByteOrder(), Options.Limits, Options.CompressionCodec);
                operationDiagnostics.Merge(writeDiagnostics);
            }
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    internal TiffWriterImageState? FindConfiguredSubImageState(int parentImageIndex, int subImageIndex, TiffDiagnostics diagnostics)
    {
        if (!IsOpen)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot write a child raster to a closed TIFF writer.");
            return null;
        }

        if (parentImageIndex < 0 || parentImageIndex >= Images.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Parent image index is outside the available image range.");
            return null;
        }

        if (subImageIndex < 0 || subImageIndex >= Images[parentImageIndex].Children.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Subimage index is outside the available child image range.");
            return null;
        }

        TiffWriterImageState state = Images[parentImageIndex].Children[subImageIndex];
        if (!state.IsConfigured)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Child full-raster writing requires a child added with AddConfiguredSubImage or AddPyramidLevel.");
            return null;
        }

        return state;
    }

    internal TiffRegion CreateFullRasterRegionForState(TiffWriterImageState state)
    {
        TiffRegion region = new TiffRegion();
        region.Width = state.ConfiguredInfo.Width;
        region.Length = state.ConfiguredInfo.Length;
        return region;
    }

    internal TiffRegion CreateFullRasterRegion(int imageIndex)
    {
        TiffRegion region = new TiffRegion();
        if (imageIndex >= 0 && imageIndex < Images.Length)
        {
            region.Width = Images[imageIndex].ConfiguredInfo.Width;
            region.Length = Images[imageIndex].ConfiguredInfo.Length;
        }

        return region;
    }

    internal bool ValidateFullRasterDestination(int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!IsOpen)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Cannot write a full raster to a closed TIFF writer.");
        }
        else if (imageIndex < 0 || imageIndex >= Images.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
        }
        else if (!Images[imageIndex].IsConfigured)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Full-raster writing requires an image added with AddConfiguredImage.");
        }

        return !diagnostics.HasErrors();
    }

    internal TiffDiagnostics WriteConfiguredRegionOperation(int imageIndex, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics operationDiagnostics = new TiffDiagnostics();
        if (!IsOpen)
        {
            operationDiagnostics.AddError(TiffErrorCode.UnknownError, "Cannot write image samples to a closed TIFF writer.");
        }
        else if (imageIndex < 0 || imageIndex >= Images.Length)
        {
            operationDiagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
        }
        else
        {
            operationDiagnostics = TiffConfiguredRasterWriter.WriteWithCodec(Images[imageIndex], region, sampleIndex, source, GetByteOrder(), Options.Limits, Options.CompressionCodec);
        }

        Diagnostics.Merge(operationDiagnostics);
        Success = !Diagnostics.HasErrors();
        return operationDiagnostics;
    }

    /// <summary>Validates and materializes the complete TIFF file, then writes it to the configured destination when one exists.</summary>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public TiffDiagnostics Flush()
    {
        FileBytes = new byte[0];
        if (Options.CloudOptimizedGeoTiffLayout && !ValidateCloudOptimizedTreeBeforeWrite())
        {
            return Diagnostics;
        }

        TiffByteOrder byteOrder = GetByteOrder();
        TiffFileKind requestedFileKind = GetFileKind();
        TiffFileKind fileKind = requestedFileKind;
        ulong dataEnd = PrepareWriteLayout(byteOrder, fileKind);
        fileKind = SelectFileKindForWrite(dataEnd);
        if (fileKind != requestedFileKind)
        {
            dataEnd = PrepareWriteLayout(byteOrder, fileKind);
        }

        if (!ValidateFileKindCapacity(fileKind, dataEnd))
        {
            return Diagnostics;
        }

        if (Options.ValidateBeforeWrite && !ValidateTreeBeforeWrite(fileKind))
        {
            return Diagnostics;
        }

        PrepareTree(byteOrder, fileKind, true);
        ulong firstIfdOffset = GetFirstIfdOffset(fileKind);
        if (Images.Length == 0)
        {
            firstIfdOffset = 0;
        }

        if (dataEnd > int.MaxValue)
        {
            Diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "TIFF writer output is too large to allocate.");
            Success = false;
            return Diagnostics;
        }

        FileBytes = new byte[(int)dataEnd];
        if (fileKind == TiffFileKind.BigTiff)
        {
            Diagnostics.Merge(TiffHeader.WriteBigTiff(FileBytes.AsSpan(0, 16), byteOrder, firstIfdOffset));
        }
        else
        {
            Diagnostics.Merge(TiffHeader.WriteClassic(FileBytes.AsSpan(0, 8), byteOrder, firstIfdOffset));
        }

        WriteTree(byteOrder, fileKind);
        CopyRawDataTree();
        Success = !Diagnostics.HasErrors();
        if (Success && HasPath)
        {
            File.WriteAllBytes(Path, FileBytes);
        }

        if (Success && HasStream)
        {
            WriteFileBytesToStream();
        }

        return Diagnostics;
    }

    internal ulong PrepareWriteLayout(TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        ulong firstIfdOffset = GetFirstIfdOffset(fileKind);
        if (Images.Length == 0)
        {
            firstIfdOffset = 0;
        }

        PrepareTree(byteOrder, fileKind, false);
        ulong directoryEnd = AssignDirectoryOffsets(firstIfdOffset, fileKind);
        ulong dataEnd = AssignRawDataOffsetsForLayout(AlignOffset(directoryEnd, GetAlignment(fileKind)));
        ulong minimumByteCount = GetFirstIfdOffset(fileKind);
        if (dataEnd < minimumByteCount)
        {
            dataEnd = minimumByteCount;
        }

        return dataEnd;
    }

    internal bool ValidateFileKindCapacity(TiffFileKind fileKind, ulong byteCount)
    {
        if (fileKind == TiffFileKind.Classic && ClassicOffsetsWouldOverflow(byteCount))
        {
            Diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic TIFF output requires offsets larger than 32 bits. Enable BigTIFF or AutoUpgradeToBigTiff.");
            Success = false;
            return false;
        }

        return true;
    }

    internal bool ValidateCloudOptimizedTreeBeforeWrite()
    {
        if (Images.Length == 0)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Cloud Optimized GeoTIFF output requires at least one image.");
            Success = false;
            return false;
        }

        bool hasFullResolution = false;
        ulong previousWidth = 0;
        ulong previousLength = 0;
        int index = 0;
        while (index < Images.Length)
        {
            TiffWriterImageState state = Images[index];
            bool reduced = IsReducedResolutionState(state);
            ulong width = ReadUnsignedTagValue(state.Definition.Tags, TiffTagId.ImageWidth, 0);
            ulong length = ReadUnsignedTagValue(state.Definition.Tags, TiffTagId.ImageLength, 0);
            ValidateCloudOptimizedStateStorage(state, index, Diagnostics);
            if (reduced)
            {
                if (!hasFullResolution)
                {
                    Diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overview images must follow a full-resolution image.");
                }
                else if (width >= previousWidth || length >= previousLength)
                {
                    Diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overview dimensions must decrease.", index, TiffTagId.NewSubfileType);
                }
            }
            else
            {
                hasFullResolution = true;
                ValidateCloudOptimizedGeoTags(state, index, Diagnostics);
            }

            ValidateCloudOptimizedChildren(state, width, length, index, Diagnostics);
            previousWidth = width;
            previousLength = length;
            index = index + 1;
        }

        if (!hasFullResolution)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF output requires a full-resolution image.");
        }

        Success = !Diagnostics.HasErrors();
        return Success;
    }

    internal static void ValidateCloudOptimizedChildren(TiffWriterImageState parent, ulong parentWidth, ulong parentLength, int imageIndex, TiffDiagnostics diagnostics)
    {
        ulong previousWidth = parentWidth;
        ulong previousLength = parentLength;
        int index = 0;
        while (index < parent.Children.Length)
        {
            TiffWriterImageState child = parent.Children[index];
            ulong width = ReadUnsignedTagValue(child.Definition.Tags, TiffTagId.ImageWidth, 0);
            ulong length = ReadUnsignedTagValue(child.Definition.Tags, TiffTagId.ImageLength, 0);
            ValidateCloudOptimizedStateStorage(child, imageIndex, diagnostics);
            if (!IsReducedResolutionState(child))
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF internal images must be marked as reduced-resolution overviews.", imageIndex, TiffTagId.NewSubfileType);
            }

            if (width >= previousWidth || length >= previousLength)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF internal overview dimensions must decrease.", imageIndex, TiffTagId.NewSubfileType);
            }

            ValidateCloudOptimizedChildren(child, width, length, imageIndex, diagnostics);
            previousWidth = width;
            previousLength = length;
            index = index + 1;
        }
    }

    internal static void ValidateCloudOptimizedStateStorage(TiffWriterImageState state, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (state.Definition.RawTiles.Length == 0 || state.Definition.RawStrips.Length != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF images must use tiled storage.", imageIndex, TiffTagId.TileOffsets);
        }
    }

    internal static void ValidateCloudOptimizedGeoTags(TiffWriterImageState state, int imageIndex, TiffDiagnostics diagnostics)
    {
        bool hasKeys = ContainsTag(state.Definition.Tags, TiffTagId.GeoKeyDirectory);
        bool hasScaleAndTiepoint = ContainsTag(state.Definition.Tags, TiffTagId.ModelPixelScale) && ContainsTag(state.Definition.Tags, TiffTagId.ModelTiepoint);
        bool hasTransformation = ContainsTag(state.Definition.Tags, TiffTagId.ModelTransformation);
        if (!hasKeys)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF full-resolution images require GeoKeyDirectoryTag.", imageIndex, TiffTagId.GeoKeyDirectory);
        }

        if (!hasScaleAndTiepoint && !hasTransformation)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF full-resolution images require model georeference tags.", imageIndex, TiffTagId.ModelTiepoint);
        }
    }

    internal static bool ContainsTag(TiffTagValue[] values, int tagId)
    {
        int index = 0;
        while (values != null && index < values.Length)
        {
            if (values[index] != null && values[index].TagId == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal ulong AssignRawDataOffsetsForLayout(ulong startOffset)
    {
        if (Options.CloudOptimizedGeoTiffLayout)
        {
            return AssignCloudOptimizedRawDataOffsets(startOffset);
        }

        return AssignRawDataOffsets(startOffset);
    }

    /// <summary>Closes the handle and releases any owned stream resources; repeated calls have no additional effect.</summary>
    public void Close()
    {
        if (IsOpen)
        {
            Flush();
        }

        if (HasStream && !Options.LeaveOpen)
        {
            OutputStream.Dispose();
        }

        IsOpen = false;
    }

    /// <summary>Returns the file bytes.</summary>
    /// <returns>The requested value or model.</returns>
    public byte[] GetFileBytes()
    {
        return TiffIfdEntry.CopyBytes(FileBytes);
    }

    /// <summary>Returns the number of available get file byte entries.</summary>
    /// <returns>The requested value or model.</returns>
    public int GetFileByteCount()
    {
        return FileBytes.Length;
    }

    /// <summary>Returns the diagnostics.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffDiagnostics GetDiagnostics()
    {
        return Diagnostics;
    }

    /// <summary>Returns the options.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffWriteOptions GetOptions()
    {
        return Options.Copy();
    }

    /// <summary>Returns the path.</summary>
    /// <returns>The requested value or model.</returns>
    public string GetPath()
    {
        return Path;
    }

    /// <summary>Returns the stream.</summary>
    /// <returns>The requested value or model.</returns>
    public Stream GetStream()
    {
        return OutputStream;
    }

    /// <summary>Returns the file kind.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffFileKind GetFileKind()
    {
        if (Options.FileKind == TiffFileKind.BigTiff)
        {
            return TiffFileKind.BigTiff;
        }

        return TiffFileKind.Classic;
    }

    internal TiffFileKind SelectFileKindForWrite(ulong classicByteCount)
    {
        return SelectFileKindForWrite(GetFileKind(), Options.AutoUpgradeToBigTiff, classicByteCount);
    }

    internal static TiffFileKind SelectFileKindForWrite(TiffFileKind requestedFileKind, bool autoUpgradeToBigTiff, ulong classicByteCount)
    {
        if (requestedFileKind == TiffFileKind.Classic && autoUpgradeToBigTiff && ClassicOffsetsWouldOverflow(classicByteCount))
        {
            return TiffFileKind.BigTiff;
        }

        return requestedFileKind;
    }

    internal static bool ClassicOffsetsWouldOverflow(ulong byteCount)
    {
        return byteCount > uint.MaxValue;
    }

    /// <summary>Returns the byte order.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffByteOrder GetByteOrder()
    {
        if (Options.ByteOrder == TiffByteOrder.BigEndian)
        {
            return TiffByteOrder.BigEndian;
        }

        return TiffByteOrder.LittleEndian;
    }

    internal bool ValidateImageIndex(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= Images.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
            Success = false;
            return false;
        }

        return true;
    }

    internal TiffDiagnostics ValidateImageAndStrileIndex(int imageIndex, int strileIndex, bool isTile, int sourceByteCount, string message)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (imageIndex < 0 || imageIndex >= Images.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
        }

        if (strileIndex < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, message);
        }

        if (imageIndex >= 0 && imageIndex < Images.Length && Images[imageIndex].IsConfigured)
        {
            TiffWriterImageState state = Images[imageIndex];
            if (state.UsesConfiguredTiles != isTile)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "The requested segment type does not match the configured writer layout.");
            }
            else if (strileIndex >= state.ConfiguredSegmentCount)
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, message);
            }
            else if (sourceByteCount == 0 && !state.AllowsSparseSegments)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "An empty segment requires AllowSparseSegments in the writer layout configuration.");
            }
        }

        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    internal void PrepareTree(TiffByteOrder byteOrder, TiffFileKind fileKind, bool useAssignedOffsets)
    {
        int index = 0;
        while (index < Images.Length)
        {
            PrepareState(Images[index], byteOrder, fileKind, useAssignedOffsets);
            index = index + 1;
        }
    }

    internal void PrepareState(TiffWriterImageState state, TiffByteOrder byteOrder, TiffFileKind fileKind, bool useAssignedOffsets)
    {
        int index = 0;
        while (index < state.Children.Length)
        {
            PrepareState(state.Children[index], byteOrder, fileKind, useAssignedOffsets);
            index = index + 1;
        }

        if (state.HasExifMetadata)
        {
            state.ExifEntries = TiffExifMetadataWriter.CreateEntries(state.ExifValues);
            state.ExifValueBytes = TiffExifMetadataWriter.EncodeValues(state.ExifValues, byteOrder, Diagnostics);
        }

        if (state.HasGpsMetadata)
        {
            state.GpsEntries = TiffExifMetadataWriter.CreateEntries(state.GpsValues);
            state.GpsValueBytes = TiffExifMetadataWriter.EncodeValues(state.GpsValues, byteOrder, Diagnostics);
        }

        TiffTagValue[] values = BuildValues(state, fileKind, useAssignedOffsets);
        state.Entries = TiffExifMetadataWriter.CreateEntries(values);
        state.ValueBytes = TiffExifMetadataWriter.EncodeValues(values, byteOrder, Diagnostics);
    }

    internal TiffTagValue[] BuildValues(TiffWriterImageState state, TiffFileKind fileKind, bool useAssignedOffsets)
    {
        TiffTagValue[] values = ApplyBaselineWriterDefaults(FilterGeneratedTags(state.Definition.Tags), state);
        if (state.Definition.RawStrips.Length > 0)
        {
            values = TiffImageDefinition.SetTagValue(values, CreateUnsignedArrayTag(TiffTagId.StripOffsets, GetOffsetDataType(fileKind), GetRawOffsets(state.RawStripOffsets, state.Definition.RawStrips.Length, useAssignedOffsets)));
            values = TiffImageDefinition.SetTagValue(values, CreateUnsignedArrayTag(TiffTagId.StripByteCounts, GetOffsetDataType(fileKind), GetByteCounts(state.Definition.RawStrips)));
        }

        if (state.Definition.RawTiles.Length > 0)
        {
            values = TiffImageDefinition.SetTagValue(values, CreateUnsignedArrayTag(TiffTagId.TileOffsets, GetOffsetDataType(fileKind), GetRawOffsets(state.RawTileOffsets, state.Definition.RawTiles.Length, useAssignedOffsets)));
            values = TiffImageDefinition.SetTagValue(values, CreateUnsignedArrayTag(TiffTagId.TileByteCounts, GetOffsetDataType(fileKind), GetByteCounts(state.Definition.RawTiles)));
        }

        if (state.Children.Length > 0)
        {
            values = TiffImageDefinition.SetTagValue(values, CreateUnsignedArrayTag(TiffTagId.SubIFDs, GetOffsetDataType(fileKind), GetChildOffsets(state, useAssignedOffsets)));
        }

        if (state.HasExifMetadata)
        {
            ulong offset = useAssignedOffsets ? state.ExifDirectoryOffset : 0;
            values = TiffImageDefinition.SetTagValue(values, TiffTagValue.CreateUnsignedScalar(TiffTagId.ExifIfd, GetIfdPointerDataType(fileKind), offset));
        }

        if (state.HasGpsMetadata)
        {
            ulong offset = useAssignedOffsets ? state.GpsDirectoryOffset : 0;
            values = TiffImageDefinition.SetTagValue(values, TiffTagValue.CreateUnsignedScalar(TiffTagId.GpsIfd, GetIfdPointerDataType(fileKind), offset));
        }

        return values;
    }

    internal bool ValidateTreeBeforeWrite(TiffFileKind fileKind)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int index = 0;
        while (index < Images.Length)
        {
            ValidateStateBeforeWrite(Images[index], fileKind, index, diagnostics);
            index = index + 1;
        }

        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return !diagnostics.HasErrors();
    }

    internal void ValidateStateBeforeWrite(TiffWriterImageState state, TiffFileKind fileKind, int imageIndex, TiffDiagnostics diagnostics)
    {
        ValidateConfiguredSegments(state, imageIndex, diagnostics);
        if (HasRawImageData(state))
        {
            TiffTagValue[] values = BuildValues(state, fileKind, false);
            TiffRequiredTagValidationResult result = TiffRequiredTagValidator.ValidateValuesForImage(values, imageIndex);
            diagnostics.Merge(result.Diagnostics);
        }

        int index = 0;
        while (index < state.Children.Length)
        {
            ValidateStateBeforeWrite(state.Children[index], fileKind, TiffDiagnostic.NoImageIndex, diagnostics);
            index = index + 1;
        }
    }

    internal void ValidateConfiguredSegments(TiffWriterImageState state, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (state == null || !state.IsConfigured || state.AllowsSparseSegments)
        {
            return;
        }

        byte[][] segments = state.Definition.RawStrips;
        int tagId = TiffTagId.StripByteCounts;
        if (state.UsesConfiguredTiles)
        {
            segments = state.Definition.RawTiles;
            tagId = TiffTagId.TileByteCounts;
        }

        int index = 0;
        while (index < state.ConfiguredSegmentCount)
        {
            if (index >= segments.Length || segments[index] == null || segments[index].Length == 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "Every configured segment must be written unless sparse segments are allowed.", imageIndex, tagId);
                return;
            }

            index = index + 1;
        }
    }

    internal bool HasRawImageData(TiffWriterImageState state)
    {
        if (state == null || state.Definition == null)
        {
            return false;
        }

        return (state.Definition.RawStrips != null && state.Definition.RawStrips.Length > 0) || (state.Definition.RawTiles != null && state.Definition.RawTiles.Length > 0);
    }

    internal TiffTagValue[] ApplyBaselineWriterDefaults(TiffTagValue[] values, TiffWriterImageState state)
    {
        TiffTagValue[] actualValues = SetDefaultValueIfMissing(values, TiffTagId.BitsPerSample);

        if (state != null && state.Definition != null && state.Definition.RawStrips != null && state.Definition.RawStrips.Length == 1 && (state.Definition.RawTiles == null || state.Definition.RawTiles.Length == 0))
        {
            actualValues = SetDefaultValueIfMissing(actualValues, TiffTagId.RowsPerStrip);
        }

        return actualValues;
    }

    internal TiffTagValue[] SetDefaultValueIfMissing(TiffTagValue[] values, int tagId)
    {
        if (TiffTagDefaultLookup.FindValue(values, tagId) != null)
        {
            return TiffImageDefinition.CopyTags(values);
        }

        TiffTagDefaultResult result = TiffTagDefaultLookup.Find(tagId);
        if (!result.Found)
        {
            return TiffImageDefinition.CopyTags(values);
        }

        return TiffImageDefinition.SetTagValue(values, result.Value);
    }

    internal TiffTagValue[] FilterGeneratedTags(TiffTagValue[] values)
    {
        TiffTagValue[] filtered = new TiffTagValue[0];
        int index = 0;
        while (values != null && index < values.Length)
        {
            if (values[index] != null && !IsGeneratedTag(values[index].TagId))
            {
                filtered = TiffImageDefinition.SetTagValue(filtered, values[index]);
            }

            index = index + 1;
        }

        return filtered;
    }

    internal bool IsGeneratedTag(int tagId)
    {
        return tagId == TiffTagId.StripOffsets || tagId == TiffTagId.StripByteCounts || tagId == TiffTagId.TileOffsets || tagId == TiffTagId.TileByteCounts || tagId == TiffTagId.SubIFDs;
    }

    internal ulong AssignDirectoryOffsets(ulong firstIfdOffset, TiffFileKind fileKind)
    {
        ulong current = firstIfdOffset;
        int index = 0;
        while (index < Images.Length)
        {
            Images[index].DirectoryOffset = current;
            current = AssignStateDirectoryOffsets(Images[index], current, fileKind);
            index = index + 1;
        }

        index = 0;
        while (index < Images.Length)
        {
            if (index + 1 < Images.Length)
            {
                Images[index].NextIfdOffset = Images[index + 1].DirectoryOffset;
            }
            else
            {
                Images[index].NextIfdOffset = 0;
            }

            index = index + 1;
        }

        return current;
    }

    internal ulong AssignStateDirectoryOffsets(TiffWriterImageState state, ulong current, TiffFileKind fileKind)
    {
        state.DirectoryOffset = current;
        current = AlignOffset(current + (ulong)GetDirectoryLayoutByteCount(state, fileKind), GetAlignment(fileKind));
        if (state.HasExifMetadata)
        {
            state.ExifDirectoryOffset = current;
            current = AlignOffset(current + (ulong)GetMetadataDirectoryLayoutByteCount(state.ExifEntries, state.ExifValueBytes, fileKind), GetAlignment(fileKind));
        }

        if (state.HasGpsMetadata)
        {
            state.GpsDirectoryOffset = current;
            current = AlignOffset(current + (ulong)GetMetadataDirectoryLayoutByteCount(state.GpsEntries, state.GpsValueBytes, fileKind), GetAlignment(fileKind));
        }

        int index = 0;
        while (index < state.Children.Length)
        {
            state.Children[index].DirectoryOffset = current;
            current = AssignStateDirectoryOffsets(state.Children[index], current, fileKind);
            index = index + 1;
        }

        return current;
    }

    internal ulong AssignRawDataOffsets(ulong startOffset)
    {
        ulong current = startOffset;
        int index = 0;
        while (index < Images.Length)
        {
            current = AssignStateRawDataOffsets(Images[index], current);
            index = index + 1;
        }

        return current;
    }

    internal ulong AssignCloudOptimizedRawDataOffsets(ulong startOffset)
    {
        ulong current = startOffset;
        int index = Images.Length - 1;
        while (index >= 0)
        {
            current = AssignReducedResolutionRawDataOffsets(Images[index], current);
            index = index - 1;
        }

        index = 0;
        while (index < Images.Length)
        {
            current = AssignFullResolutionRawDataOffsets(Images[index], current);
            index = index + 1;
        }

        return current;
    }

    internal ulong AssignReducedResolutionRawDataOffsets(TiffWriterImageState state, ulong current)
    {
        int index = state.Children.Length - 1;
        while (index >= 0)
        {
            current = AssignReducedResolutionRawDataOffsets(state.Children[index], current);
            index = index - 1;
        }

        if (IsReducedResolutionState(state))
        {
            current = AssignStateRawDataOffsetsOnly(state, current);
        }

        return current;
    }

    internal ulong AssignFullResolutionRawDataOffsets(TiffWriterImageState state, ulong current)
    {
        if (!IsReducedResolutionState(state))
        {
            current = AssignStateRawDataOffsetsOnly(state, current);
        }

        int index = 0;
        while (index < state.Children.Length)
        {
            current = AssignFullResolutionRawDataOffsets(state.Children[index], current);
            index = index + 1;
        }

        return current;
    }

    internal ulong AssignStateRawDataOffsets(TiffWriterImageState state, ulong current)
    {
        current = AssignStateRawDataOffsetsOnly(state, current);
        int index = 0;
        while (index < state.Children.Length)
        {
            current = AssignStateRawDataOffsets(state.Children[index], current);
            index = index + 1;
        }

        return current;
    }

    internal ulong AssignStateRawDataOffsetsOnly(TiffWriterImageState state, ulong current)
    {
        state.RawStripOffsets = new ulong[state.Definition.RawStrips.Length];
        int index = 0;
        while (index < state.Definition.RawStrips.Length)
        {
            state.RawStripOffsets[index] = current;
            current = current + (ulong)state.Definition.RawStrips[index].Length;
            index = index + 1;
        }

        state.RawTileOffsets = new ulong[state.Definition.RawTiles.Length];
        index = 0;
        while (index < state.Definition.RawTiles.Length)
        {
            state.RawTileOffsets[index] = current;
            current = current + (ulong)state.Definition.RawTiles[index].Length;
            index = index + 1;
        }

        return current;
    }

    internal static bool IsReducedResolutionState(TiffWriterImageState state)
    {
        if (state == null || state.Definition == null)
        {
            return false;
        }

        ulong newSubfileType = ReadUnsignedTagValue(state.Definition.Tags, TiffTagId.NewSubfileType, 0);
        if ((newSubfileType & TiffCloudOptimizedGeoTiffValidator.ReducedResolutionMask) != 0)
        {
            return true;
        }

        ulong subfileType = ReadUnsignedTagValue(state.Definition.Tags, TiffTagId.SubfileType, 0);
        return subfileType == 2;
    }

    internal static ulong ReadUnsignedTagValue(TiffTagValue[] values, int tagId, ulong defaultValue)
    {
        int index = 0;
        while (values != null && index < values.Length)
        {
            if (values[index] != null && values[index].TagId == tagId && values[index].UnsignedValues != null && values[index].UnsignedValues.Length > 0)
            {
                return values[index].UnsignedValues[0];
            }

            index = index + 1;
        }

        return defaultValue;
    }

    internal void WriteTree(TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        int index = 0;
        while (index < Images.Length)
        {
            WriteState(Images[index], byteOrder, fileKind);
            index = index + 1;
        }
    }

    internal void WriteState(TiffWriterImageState state, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        int byteCount = GetDirectoryLayoutByteCount(state, fileKind);
        if (state.DirectoryOffset > int.MaxValue || state.DirectoryOffset + (ulong)byteCount > (ulong)FileBytes.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidByteRange, "TIFF writer directory range is outside the output buffer.");
            Success = false;
            return;
        }

        if (fileKind == TiffFileKind.BigTiff)
        {
            Diagnostics.Merge(TiffDirectoryWriter.WriteBigTiffWithValueBytes(FileBytes.AsSpan((int)state.DirectoryOffset, byteCount), state.Entries, state.ValueBytes, byteOrder, state.DirectoryOffset, state.NextIfdOffset, Options));
        }
        else
        {
            Diagnostics.Merge(TiffDirectoryWriter.WriteClassicWithValueBytes(FileBytes.AsSpan((int)state.DirectoryOffset, byteCount), state.Entries, state.ValueBytes, byteOrder, state.DirectoryOffset, state.NextIfdOffset, Options));
        }

        if (state.HasExifMetadata)
        {
            WriteMetadataDirectory(state.ExifDirectoryOffset, state.ExifEntries, state.ExifValueBytes, byteOrder, fileKind);
        }

        if (state.HasGpsMetadata)
        {
            WriteMetadataDirectory(state.GpsDirectoryOffset, state.GpsEntries, state.GpsValueBytes, byteOrder, fileKind);
        }

        int index = 0;
        while (index < state.Children.Length)
        {
            WriteState(state.Children[index], byteOrder, fileKind);
            index = index + 1;
        }
    }

    internal void CopyRawDataTree()
    {
        int index = 0;
        while (index < Images.Length)
        {
            CopyRawDataState(Images[index]);
            index = index + 1;
        }
    }

    internal void CopyRawDataState(TiffWriterImageState state)
    {
        int index = 0;
        while (index < state.Definition.RawStrips.Length)
        {
            CopyBytesToFile(state.Definition.RawStrips[index], state.RawStripOffsets[index]);
            index = index + 1;
        }

        index = 0;
        while (index < state.Definition.RawTiles.Length)
        {
            CopyBytesToFile(state.Definition.RawTiles[index], state.RawTileOffsets[index]);
            index = index + 1;
        }

        index = 0;
        while (index < state.Children.Length)
        {
            CopyRawDataState(state.Children[index]);
            index = index + 1;
        }
    }

    internal void CopyBytesToFile(byte[] bytes, ulong offset)
    {
        if (offset > int.MaxValue || offset + (ulong)bytes.Length > (ulong)FileBytes.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidByteRange, "TIFF writer raw data range is outside the output buffer.");
            Success = false;
            return;
        }

        int index = 0;
        while (index < bytes.Length)
        {
            FileBytes[(int)offset + index] = bytes[index];
            index = index + 1;
        }
    }

    internal int GetDirectoryLayoutByteCount(TiffWriterImageState state, TiffFileKind fileKind)
    {
        if (fileKind == TiffFileKind.BigTiff)
        {
            return TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(state.Entries, state.ValueBytes);
        }

        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(state.Entries, state.ValueBytes);
    }

    internal int GetMetadataDirectoryLayoutByteCount(TiffIfdEntry[] entries, byte[][] valueBytes, TiffFileKind fileKind)
    {
        if (fileKind == TiffFileKind.BigTiff)
        {
            return TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(entries, valueBytes);
        }

        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
    }

    internal void WriteMetadataDirectory(ulong offset, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        int byteCount = GetMetadataDirectoryLayoutByteCount(entries, valueBytes, fileKind);
        if (offset > int.MaxValue || offset + (ulong)byteCount > (ulong)FileBytes.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidByteRange, "TIFF writer metadata directory range is outside the output buffer.");
            Success = false;
            return;
        }

        if (fileKind == TiffFileKind.BigTiff)
        {
            Diagnostics.Merge(TiffDirectoryWriter.WriteBigTiffWithValueBytes(FileBytes.AsSpan((int)offset, byteCount), entries, valueBytes, byteOrder, offset, 0, Options));
        }
        else
        {
            Diagnostics.Merge(TiffDirectoryWriter.WriteClassicWithValueBytes(FileBytes.AsSpan((int)offset, byteCount), entries, valueBytes, byteOrder, offset, 0, Options));
        }
    }

    internal static TiffWriterImageState[] AppendState(TiffWriterImageState[] states, TiffWriterImageState state)
    {
        if (states == null)
        {
            TiffWriterImageState[] single = new TiffWriterImageState[1];
            single[0] = state.Copy();
            return single;
        }

        TiffWriterImageState[] copy = new TiffWriterImageState[states.Length + 1];
        int index = 0;
        while (index < states.Length)
        {
            copy[index] = states[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = state.Copy();
        return copy;
    }

    internal static TiffTagValue CreateUnsignedArrayTag(int tagId, TiffDataType dataType, ulong[] values)
    {
        return TiffTagValue.CreateUnsignedArray(tagId, dataType, values);
    }

    internal static ulong[] GetRawOffsets(ulong[] offsets, int count, bool useAssignedOffsets)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            if (useAssignedOffsets && offsets != null && index < offsets.Length)
            {
                values[index] = offsets[index];
            }
            else
            {
                values[index] = 0;
            }

            index = index + 1;
        }

        return values;
    }

    internal static ulong[] GetByteCounts(byte[][] bytes)
    {
        if (bytes == null)
        {
            return new ulong[0];
        }

        ulong[] counts = new ulong[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == null)
            {
                counts[index] = 0;
            }
            else
            {
                counts[index] = (ulong)bytes[index].Length;
            }

            index = index + 1;
        }

        return counts;
    }

    internal static ulong[] GetChildOffsets(TiffWriterImageState state, bool useAssignedOffsets)
    {
        ulong[] offsets = new ulong[state.Children.Length];
        int index = 0;
        while (index < state.Children.Length)
        {
            if (useAssignedOffsets)
            {
                offsets[index] = state.Children[index].DirectoryOffset;
            }
            else
            {
                offsets[index] = 0;
            }

            index = index + 1;
        }

        return offsets;
    }

    internal static ulong AlignOffset(ulong offset, ulong alignment)
    {
        return TiffDirectoryWriter.AlignExternalValueOffset(offset, alignment);
    }

    internal static ulong GetAlignment(TiffFileKind fileKind)
    {
        if (fileKind == TiffFileKind.BigTiff)
        {
            return 8;
        }

        return 2;
    }

    internal static ulong GetFirstIfdOffset(TiffFileKind fileKind)
    {
        if (fileKind == TiffFileKind.BigTiff)
        {
            return 16;
        }

        return 8;
    }

    internal static TiffDataType GetOffsetDataType(TiffFileKind fileKind)
    {
        if (fileKind == TiffFileKind.BigTiff)
        {
            return TiffDataType.Long8;
        }

        return TiffDataType.Long;
    }

    internal static TiffDataType GetIfdPointerDataType(TiffFileKind fileKind)
    {
        if (fileKind == TiffFileKind.BigTiff)
        {
            return TiffDataType.Ifd8;
        }

        return TiffDataType.Long;
    }

    internal static TiffWriteOptions NormalizeOptions(TiffWriteOptions options)
    {
        if (options == null)
        {
            return new TiffWriteOptions();
        }

        return options.Copy();
    }

    internal void WriteFileBytesToStream()
    {
        if (!OutputStream.CanWrite)
        {
            Diagnostics.AddError(TiffErrorCode.UnknownError, "The TIFF writer stream is not writable.");
            Success = false;
            return;
        }

        if (OutputStream.CanSeek)
        {
            OutputStream.Position = StreamStartPosition;
            OutputStream.SetLength(StreamStartPosition);
        }

        OutputStream.Write(FileBytes, 0, FileBytes.Length);
        OutputStream.Flush();
    }

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        return path;
    }
}
